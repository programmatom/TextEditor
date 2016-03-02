/*
 *  Copyright © 1992-2002, 2015 Thomas R. Lawrence
 *
 *  GNU General Public License
 *
 *  This file is part of "Text Editor"
 *
 *  "Text Editor" is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TextEditor
{
    // https://msdn.microsoft.com/en-us/library/windows/desktop/dd374091%28v=vs.85%29.aspx
    public class TextServiceUniscribe : ITextService, IDisposable
    {
        // for testing:
        // Arabic example 1:
        // http://blogs.msdn.com/b/vsarabic/archive/2011/08/21/text-rendering.aspx - U+627 U+644 U+639 U+631 U+628 U+64A U+629
        // Devanagari example 1:
        // http://www.omniglot.com/language/articles/devanagari.htm - U+926 U+947 U+935 U+928 U+93E U+917 U+930 U+940 U+20 U+932 U+93F U+92A U+93F
        // Arabic and Devanagari example 2:
        // http://www.catch22.net/tuts/drawing-styled-text-uniscribe - U+64A U+64F U+633 U+627 U+648 U+650 U+64A ... U+920 U+911 U+915 U+94D U+937 U+91D U+949 

        private FontCacheEntry[] caches = new FontCacheEntry[0];
        private struct FontCacheEntry
        {
            public Font font;
            public SCRIPT_CACHE cache;
        }

        private readonly List<KeyValuePair<GDI.LOGFONT, Font>> fallbackFonts = new List<KeyValuePair<GDI.LOGFONT, Font>>(); // Font:IDisposable
        private readonly Dictionary<Font, GDIFont> fontToHFont = new Dictionary<Font, GDIFont>(); // GDIFont:IDisposable

        private Font font; // not owned

        // Yet another offscreen strip? Uniscribe is very picky:
        // 1. It gets unhappy if the HDC is changed between shaping text and drawing glyphs because the glyph indices
        //    are local to the device context. Even deleting and recreating the DC on a bitmap won't work. Therefore
        //    the DC is kept around here.
        // 2. ExtTextOut won't render transparent text to HDCs obtained from GDI+ (i.e. class Graphics). The text looks
        //    heavy and smudgy because the anti-aliasing goes to all black. The only solution is to get GDI+ out of the
        //    picture and manage our HDCs from start to finish. Since we don't want to expose that requirement to our
        //    System.Windows.Forms client, it means creating another offscreen bitmap to do the work in and then blitting
        //    the result to their Graphics object. Since DirectWrite is the wave of the future and computers are fast
        //    these days, there is no good reason to try to improve efficiency by breaking down the client's layer of
        //    insulation.
        private int visibleWidth;
        private GDIBitmap offscreenStrip; // IDisposable
        private GDIDC hdcOffscreenStrip; // IDisposable


        public TextServiceUniscribe()
        {
        }

        ~TextServiceUniscribe()
        {
#if DEBUG
            Debug.Assert(false, this.GetType().Name + " finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
            Dispose();
        }
#if DEBUG
        private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

        public void Dispose()
        {
            ClearCaches();

            if (hdcOffscreenStrip != null)
            {
                hdcOffscreenStrip.Dispose();
                hdcOffscreenStrip = null;
            }
            if (offscreenStrip != null)
            {
                offscreenStrip.Dispose();
                offscreenStrip = null;
            }

            GC.SuppressFinalize(this);
        }

        private void ClearCaches()
        {
            foreach (KeyValuePair<GDI.LOGFONT, Font> item in fallbackFonts)
            {
                item.Value.Dispose();
            }
            fallbackFonts.Clear();

            foreach (KeyValuePair<Font, GDIFont> fontHFont in fontToHFont)
            {
                fontHFont.Value.Dispose();
            }
            fontToHFont.Clear();

            for (int i = 0; i < caches.Length; i++)
            {
                caches[i].cache.Clear();
            }
            Array.Resize(ref caches, 0);
        }

        private int FontCacheIndex(Font font)
        {
            int index = Array.FindIndex(caches, delegate (FontCacheEntry candidate) { return candidate.font == font; });
            if (index < 0)
            {
                index = caches.Length;
                Array.Resize(ref caches, caches.Length + 1);
                caches[index].font = font;
            }
            return index;
        }

        public TextService Service { get { return TextService.Uniscribe; } }

        public bool Hardened { get { return false; } }

        public void Reset(
            Font font,
            int visibleWidth)
        {
            ClearCaches();

            if (hdcOffscreenStrip != null)
            {
                hdcOffscreenStrip.Dispose();
                hdcOffscreenStrip = null;
            }
            if (offscreenStrip != null)
            {
                offscreenStrip.Dispose();
                offscreenStrip = null;
            }
            this.visibleWidth = visibleWidth;
            // allocation of offscreenStrip is deferred until HDC is available to make compatible with

            this.font = font;
        }

        public ITextInfo AnalyzeText(
            Graphics graphics,
            Font font,
            int fontHeight,
            string line)
        {
            if (line.IndexOfAny(new char[] { '\r', '\n' }) >= 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            if (offscreenStrip == null)
            {
                using (GraphicsHDC hdc = new GraphicsHDC(graphics))
                {
                    offscreenStrip = new GDIBitmap(visibleWidth, fontHeight, hdc);
                }
                Debug.Assert(hdcOffscreenStrip == null);
                hdcOffscreenStrip = GDIDC.Create(offscreenStrip);
            }

            using (Pin<string> pinLine = new Pin<string>(line))
            {
                return TextItems.AnalyzeText(
                    this,
                    hdcOffscreenStrip,
                    pinLine.AddrOfPinnedObject(),
                    new FontRunInfo[] { new FontRunInfo(line.Length, font, fontHeight) });
            }
        }

        private class FontRunInfo
        {
            public readonly int count;
            public readonly Font font;
            public readonly int fontHeight;

            public FontRunInfo(
                int count,
                Font font,
                int fontHeight)
            {
                this.count = count;
                this.font = font;
                this.fontHeight = fontHeight;
            }
        }

        private struct ItemInfo
        {
            public int cGlyphs;
            public short[] glyphs;
            public short[] logicalClusters;
            public SCRIPT_VISATTR[] visAttrs; // superceded by glyphProps (in OpenType version of API)
            public SCRIPT_CHARPROP[] charProps;
            public SCRIPT_GLYPHPROP[] glyphProps;

            public int[] iAdvances;
            public GOFFSET[] goffsets;
            public ABC abc;

            public Font fallbackFont;

            public int totalWidth;
        }

        private class TextItems : ITextInfo, IDisposable
        {
            private readonly TextServiceUniscribe service;

            private int lineHeight;

            private int count;

            private FontRunInfo[] fontRuns;

            private SCRIPT_CONTROL sControl;
            private SCRIPT_STATE sState;

            private SCRIPT_LOGATTR[] logAttrs;

            private SCRIPT_ITEM[] sItems;
            private int cItems;
            private OPENTYPE_TAG[] sTags;

            private ItemInfo[] sItemsExtra;

            private int[] iVisualToLogical;
            private int[] iLogicalToVisual;

            public TextItems(TextServiceUniscribe service)
            {
                this.service = service;
            }

            ~TextItems()
            {
#if DEBUG
                Debug.Assert(false, this.GetType().Name + " finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
                Dispose();
            }
#if DEBUG
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            // This article provides a real-world tutorial of how to actually use the API, including
            // a discussion of many gotchas not identified in the MSDN documentation.
            // http://www.catch22.net/tuts/uniscribe-mysteries
            // http://www.catch22.net/tuts/more-uniscribe-mysteries
            // http://www.catch22.net/tuts/drawing-styled-text-uniscribe
            // From the developer of Google Chrome's uniscribe client:
            // https://maxradi.us/documents/uniscribe/
            // Arabic embedded (RTL) example:
            // http://blogs.msdn.com/b/vsarabic/archive/2011/08/21/text-rendering.aspx
            // Devanagari example:
            // http://www.omniglot.com/language/articles/devanagari.htm
            // UTF16 non-zero plane exahere:
            // https://en.wikipedia.org/wiki/UTF-16#Examples
            // http://www.i18nguy.com/unicode-example-plane1.html

            // "Displaying Text with Uniscribe"
            // https://msdn.microsoft.com/en-us/library/windows/desktop/dd317792%28v=vs.85%29.aspx
            internal static TextItems AnalyzeText(
                TextServiceUniscribe service,
                IntPtr hdc,
                IntPtr hText,
                FontRunInfo[] fontRuns)
            {
                TextItems o = new TextItems(service);
                try
                {

                    o.fontRuns = fontRuns;
                    for (int i = 0; i < fontRuns.Length; i++)
                    {
                        o.count += fontRuns[i].count;
                        o.lineHeight = Math.Max(o.lineHeight, fontRuns[i].fontHeight);
                    }
                    if (o.count == 0)
                    {
                        return o;
                    }


                    int hr;

                    // Lay Out Text Using Uniscribe

                    // 1. Call ScriptRecordDigitSubstitution only when starting or when receiving a WM_SETTINGCHANGE message.

                    // 2. (Optional) Call ScriptIsComplex to determine if the paragraph requires complex processing.
                    hr = ScriptIsComplex(
                        hText,
                        o.count,
                        ScriptIsComplexFlags.SIC_ASCIIDIGIT | ScriptIsComplexFlags.SIC_COMPLEX);
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }
                    // Optional: if S_FALSE (1) is returned, one can fall back to TextRenderer and simplified hit testing

                    // 3. (Optional) If using Uniscribe to handle bidirectional text and/or digit substitution, call
                    // ScriptApplyDigitSubstitution to prepare the SCRIPT_CONTROL and SCRIPT_STATE structures as inputs
                    // to ScriptItemize. If skipping this step, but still requiring digit substitution, substitute
                    // national digits for Unicode U+0030 through U+0039 (European digits). For information about digit
                    // substitution, see Digit Shapes.
                    hr = ScriptApplyDigitSubstitution(
                        IntPtr.Zero,
                        out o.sControl,
                        out o.sState);
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    // 4. Call ScriptItemize to divide the paragraph into items. If not using Uniscribe for digit
                    // substitution and the bidirectional order is known, for example, because of the keyboard layout
                    // used to enter the character, call ScriptItemize. In the call, provide null pointers for the
                    // SCRIPT_CONTROL and SCRIPT_STATE structures. This technique generates items by use of the shaping
                    // engine only, and the items can be reordered using the engine information.
                    // Note: Typically, applications that work only with left-to-right scripts and without any digit
                    // substitution should pass null pointers for the SCRIPT_CONTROL and SCRIPT_STATE structures.

                    {
                        int cMaxItems = 8;
                        SCRIPT_ITEM[] sItems;
                        OPENTYPE_TAG[] sTags;
                        while (true)
                        {
                            sItems = new SCRIPT_ITEM[cMaxItems + 1]; // method adds terminator;
                            sTags = new OPENTYPE_TAG[cMaxItems];
                            hr = ScriptItemizeOpenType(
                                hText,
                                o.count,
                                cMaxItems,
                                ref o.sControl,
                                ref o.sState,
                                sItems,
                                sTags,
                                out o.cItems);
                            if (hr == E_OUTOFMEMORY)
                            {
                                cMaxItems *= 2;
                                continue;
                            }
                            if (hr < 0)
                            {
                                Marshal.ThrowExceptionForHR(hr);
                            }
                            break;
                        }

                        o.sItems = sItems;
                        o.sTags = sTags;
                    }

                    // 5. Merge the item information with the run information to produce ranges.
                    {
                        int fontRunOffset = 0;
                        int f = 0;
                        int i = 0;
                        while ((f < o.fontRuns.Length) && (i < o.cItems))
                        {
                            if (fontRunOffset + o.fontRuns[f].count > o.sItems[i + 1].iCharPos)
                            {
                                // run is longer - current item remains intact; advance to next
                                i++;
                                continue;
                            }
                            else if (fontRunOffset + o.fontRuns[f].count < o.sItems[i + 1].iCharPos)
                            {
                                // item too long - split

                                Array.Resize(ref o.sItems, o.cItems + 1 + 1);
                                Array.Copy(o.sItems, i, o.sItems, i + 1, o.sItems.Length - (i + 1));
                                Array.Resize(ref o.sTags, o.cItems + 1);
                                Array.Copy(o.sTags, i, o.sTags, i + 1, o.sTags.Length - (i + 1));
                                o.cItems++;

                                o.sItems[i + 1].iCharPos = fontRunOffset + o.fontRuns[f].count;
                            }
                            Debug.Assert(fontRunOffset + o.fontRuns[f].count == o.sItems[i + 1].iCharPos);
                            fontRunOffset += o.fontRuns[f].count;
                            f++;
                            i++;
                        }
                        Debug.Assert(fontRunOffset == o.count);
                        Debug.Assert(o.sItems[o.cItems].iCharPos == o.count);
                    }

                    // 6. Call ScriptShape to identify clusters and generate glyphs.
                    o.sItemsExtra = new ItemInfo[o.cItems];
                    o.logAttrs = new SCRIPT_LOGATTR[o.count];
                    for (int i = 0; i < o.cItems; i++)
                    {
                        int start = o.sItems[i].iCharPos;
                        int length = o.sItems[i + 1].iCharPos - start;

                        Font font = null;
                        for (int f = 0, pos = 0; f < o.fontRuns.Length; pos += o.fontRuns[f].count, f++)
                        {
                            font = o.fontRuns[f].font;
                            if (pos < start + length)
                            {
                                break;
                            }
                        }

                        int cMaxGlyphs = (3 * o.count / 2) + 16; // recommended starting value
                        short[] glyphs;
                        short[] logicalClusters;
                        SCRIPT_VISATTR[] visAttrs; // superceded by glyphProps
                        SCRIPT_GLYPHPROP[] glyphProps;
                        int cGlyphs;
                        int fallbackLevel = 0;
                        Font fallbackFont = null;
                        SCRIPT_CHARPROP[] charProps = new SCRIPT_CHARPROP[length];
                        while (true)
                        {
                            glyphs = new short[cMaxGlyphs];
                            logicalClusters = new short[cMaxGlyphs];
                            visAttrs = new SCRIPT_VISATTR[cMaxGlyphs];
                            glyphProps = new SCRIPT_GLYPHPROP[cMaxGlyphs];

                            bool needFallback = false;
                            int fontCacheIndex = service.FontCacheIndex(font);

                            GDIFont gdiFont;
                            if (!service.fontToHFont.TryGetValue(font, out gdiFont))
                            {
                                gdiFont = new GDIFont(font);
                                service.fontToHFont.Add(font, gdiFont);
                            }
                            GDI.SelectObject(hdc, gdiFont);

#if false // choose old or OpenType API
                        hr = ScriptShape(
                            hdc,
                            ref service.caches[fontCacheIndex].cache,
                            new IntPtr(hText.ToInt64() + start * 2),
                            length,
                            cMaxGlyphs,
                            ref o.sItems[i].a,
                            glyphs,
                            logicalClusters,
                            visAttrs,
                            out cGlyphs);
#else
                            hr = ScriptShapeOpenType(
                                hdc,
                                ref service.caches[fontCacheIndex].cache,
                                ref o.sItems[i].a,
                                o.sTags[i], // tagScript
                                o.sTags[i], // tagLangSys -- right thing to pass here?
                                null, // rcRangeChars
                                null, // rpRangeProperties
                                0, // cRanges
                                new IntPtr(hText.ToInt64() + start * 2),
                                length,
                                cMaxGlyphs,
                                logicalClusters,
                                charProps,
                                glyphs,
                                glyphProps, // supercedes visAttrs
                                out cGlyphs);
#endif
                            if (hr == E_OUTOFMEMORY)
                            {
                                cMaxGlyphs *= 2;
                                glyphs = new short[cMaxGlyphs];
                                logicalClusters = new short[cMaxGlyphs];
                                visAttrs = new SCRIPT_VISATTR[cMaxGlyphs];
                                continue;
                            }
                            if (hr == USP_E_SCRIPT_NOT_IN_FONT)
                            {
                                needFallback = true;
                                goto FontFallback;
                            }
                            if (hr < 0)
                            {
                                Marshal.ThrowExceptionForHR(hr);
                            }
                            // 7. If ScriptShape returns the code USP_E_SCRIPT_NOT_IN_FONT or S_OK with the output containing
                            // missing glyphs, select characters from a different font. Either substitute another font or disable
                            // shaping by setting the eScript member of the SCRIPT_ANALYSIS structure passed to ScriptShape to
                            // SCRIPT_UNDEFINED. For more information, see Using Font Fallback.
                            SCRIPT_FONTPROPERTIES sfp;
                            ScriptGetFontProperties(
                                hdc,
                                ref service.caches[fontCacheIndex].cache,
                                out sfp);
                            for (int j = 0; j < cGlyphs; j++)
                            {
                                if (glyphs[j] == sfp.wgDefault)
                                {
                                    needFallback = true;
                                    break;
                                }
                            }
                        FontFallback:
                            if (needFallback)
                            {
                                // What worked:
                                // https://code.google.com/p/chromium/codesearch#chromium/src/ui/gfx/font_fallback_win.cc&q=uniscribe&sq=package:chromium&l=283&dr=CSs
                                // http://stackoverflow.com/questions/16828868/how-to-automatically-choose-most-suitable-font-for-different-language
                                // What didn't work:
                                // The totally unhelpful Uniscribe font/script fallback documentation:
                                // https://msdn.microsoft.com/en-us/library/windows/desktop/dd374105%28v=vs.85%29.aspx
                                // MSDN Globalization how-to page about font fallback:
                                // https://msdn.microsoft.com/en-us/goglobal/bb688134.aspx
                                // ScriptShape page:
                                // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368564(v=vs.85).aspx

                                fallbackLevel++;
                                switch (fallbackLevel)
                                {
                                    case 1:
                                        GDI.LOGFONT fallbackLF;
                                        if (GetUniscribeFallbackFont(
                                            font,
                                            new IntPtr(hText.ToInt64() + 2 * start),
                                            length,
                                            out fallbackLF))
                                        {
                                            fallbackFont = null;
                                            foreach (KeyValuePair<GDI.LOGFONT, Font> item in service.fallbackFonts)
                                            {
                                                if (String.Equals(item.Key.lfFaceName, fallbackLF.lfFaceName)
                                                    && (item.Key.lfCharSet == fallbackLF.lfCharSet)
                                                    && (item.Key.lfHeight == fallbackLF.lfHeight))
                                                {
                                                    fallbackFont = item.Value;
                                                    break;
                                                }
                                            }
                                            if (fallbackFont == null)
                                            {
                                                fallbackFont = Font.FromLogFont(fallbackLF);
                                                service.fallbackFonts.Add(new KeyValuePair<GDI.LOGFONT, Font>(fallbackLF, fallbackFont));
                                            }
                                            font = fallbackFont;
                                            continue;
                                        }
                                        continue;

                                    case 2:
                                        if (o.sItems[i].a.eScript != SCRIPT_UNDEFINED)
                                        {
                                            o.sItems[i].a.eScript = SCRIPT_UNDEFINED;
                                        }
                                        continue;

                                    default:
                                        // give up
                                        break;
                                }
                            }

                            break;
                        }

                        // 8. Call ScriptPlace to generate advance widths and x and y positions for the glyphs in each
                        // successive range. This is the first step for which text size becomes a consideration.
                        int[] iAdvances = new int[cGlyphs];
                        GOFFSET[] goffsets = new GOFFSET[cGlyphs];
                        ABC abc = new ABC();
                        {
                            int fontCacheIndex = service.FontCacheIndex(font);
#if false // choose old or OpenType API
                        hr = ScriptPlace(
                            hdc,
                            ref service.caches[fontCacheIndex].cache,
                            glyphs,
                            cGlyphs,
                            visAttrs,
                            ref o.sItems[i].a,
                            iAdvances,
                            goffsets,
                            out abc);
#else
                            hr = ScriptPlaceOpenType(
                                hdc,
                                ref service.caches[fontCacheIndex].cache,
                                ref o.sItems[i].a,
                                o.sTags[i], // tagScript
                                o.sTags[i], // tagLangSys -- right thing to pass here?
                                null, // rcRangeChars
                                null, // rpRangeProperties
                                0, // cRanges
                                new IntPtr(hText.ToInt64() + start * 2),
                                logicalClusters,
                                charProps,
                                length,
                                glyphs,
                                glyphProps,
                                cGlyphs,
                                iAdvances,
                                goffsets,
                                out abc);
#endif
                            if (hr < 0)
                            {
                                Marshal.ThrowExceptionForHR(hr);
                            }
                        }

                        o.sItemsExtra[i].glyphs = glyphs;
                        o.sItemsExtra[i].logicalClusters = logicalClusters;
                        o.sItemsExtra[i].visAttrs = visAttrs;
                        o.sItemsExtra[i].cGlyphs = cGlyphs;
                        o.sItemsExtra[i].charProps = charProps;
                        o.sItemsExtra[i].glyphProps = glyphProps;

                        o.sItemsExtra[i].iAdvances = iAdvances;
                        o.sItemsExtra[i].goffsets = goffsets;
                        o.sItemsExtra[i].abc = abc;

                        o.sItemsExtra[i].fallbackFont = fallbackFont;

                        // 9. Sum the range sizes until the line overflows.

                        // 10. Break the range on a word boundary by using the fSoftBreak and fWhiteSpace members in the
                        // logical attributes. To break a single character cluster off the run, use the information returned
                        // by calling ScriptBreak.
                        // Note: Decide if the first code point of a range should be a word break point because the last
                        // character of the previous range requires it. For example, if one range ends in a comma, consider
                        // the first character of the next range to be a word break point.
                        SCRIPT_LOGATTR[] logAttrs1 = new SCRIPT_LOGATTR[o.count];
                        hr = ScriptBreak(
                            new IntPtr(hText.ToInt64() + 2 * start),
                            length,
                            ref o.sItems[i].a,
                            logAttrs1);
                        if (hr < 0)
                        {
                            Marshal.ThrowExceptionForHR(hr);
                        }
                        Array.Copy(logAttrs1, 0, o.logAttrs, o.sItems[i].iCharPos, length);

                        // 11. Repeat steps 6 through 10 for each line in the paragraph. However, if breaking the last run
                        // on the line, call ScriptShape to reshape the remaining part of the run as the first run on the
                        // next line.
                    }


                    // Display Text Using Uniscribe

                    // 1. For each run, do the following:
                    // a. If the style has changed since the last run, update the handle to the device context by releasing
                    //    and getting it again.
                    // b. Call ScriptShape to generate glyphs for the run.
                    // c. Call ScriptPlace to generate an advance width and an x,y offset for each glyph.

                    // 2. Do the following to establish the correct visual order for the runs in the line:
                    // a. Extract an array of bidirectional embedding levels, one per range. The embedding level is
                    //    given by (SCRIPT_ITEM) si.(SCRIPT_ANALYSIS) a. (SCRIPT_STATE) s.uBidiLevel.
                    // b. Pass this array to ScriptLayout to generate a map of visual positions to logical positions.
                    byte[] bidiEmbeddingLevels = new byte[o.cItems];
                    for (int i = 0; i < o.cItems; i++)
                    {
                        bidiEmbeddingLevels[i] = (byte)o.sItems[i].a.s.uBidiLevel;
                    }
                    o.iVisualToLogical = new int[o.cItems];
                    o.iLogicalToVisual = new int[o.cItems];
                    hr = ScriptLayout(
                        o.cItems,
                        bidiEmbeddingLevels,
                        o.iVisualToLogical,
                        o.iLogicalToVisual);
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    for (int i = 0; i < o.cItems; i++)
                    {
                        for (int j = 0; j < o.sItemsExtra[i].iAdvances.Length; j++)
                        {
                            o.sItemsExtra[i].totalWidth += o.sItemsExtra[i].iAdvances[j];
                        }
                    }
                }
                catch (Exception)
                {
                    o.Dispose();
                    throw;
                }

                return o;
            }

            // This idea was borrowed from Google Chrome's uniscribe handler:
            // https://code.google.com/p/chromium/codesearch#chromium/src/ui/gfx/font_fallback_win.cc&q=uniscribe&sq=package:chromium&l=283&dr=CSs
            // also as discussed here:
            // http://stackoverflow.com/questions/16828868/how-to-automatically-choose-most-suitable-font-for-different-language
            private static bool GetUniscribeFallbackFont(
                Font font,
                IntPtr text,
                int textLength,
                out GDI.LOGFONT fallbackFont)
            {
                fallbackFont = new GDI.LOGFONT();

                // Figure out what font Uniscribe chooses for fallback by making it write to a
                // metafile and then cracking it open.
                using (GDIDC hdc = GDIDC.CreateCompatibleDC(IntPtr.Zero))
                {
                    IntPtr hdcMF_ = GDI.CreateEnhMetaFile(hdc, null, IntPtr.Zero, null);
                    if (hdcMF_ == IntPtr.Zero)
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                    using (GDIDC hdcMF = new GDIDC(hdcMF_))
                    {
                        using (GDIFont gdiFont = new GDIFont(font))
                        {
                            GDI.SelectObject(hdcMF, gdiFont);

                            IntPtr sa = IntPtr.Zero;
                            try
                            {
                                int hr;

                                hr = ScriptStringAnalyse(
                                    hdcMF,
                                    text,
                                    textLength,
                                    0, // cGlyphs
                                    -1, // iCharSet
                                    SSA_METAFILE | SSA_FALLBACK | SSA_GLYPHS | SSA_LINK,
                                    0, // required width
                                    IntPtr.Zero, // SCRIPT_CONTROL
                                    IntPtr.Zero, // SCRIPT_STATE
                                    null, // piDx
                                    IntPtr.Zero, // SCRIPT_TABDEF
                                    null, // legacy
                                    out sa);
                                if (hr < 0)
                                {
                                    return false;
                                }
                                hr = ScriptStringOut(
                                    sa,
                                    0, // iX
                                    0, // iY
                                    0, // ExtTextOut options
                                    IntPtr.Zero, // rect
                                    0, // iMinSel
                                    0, // iMaxSel
                                    false); // fDisabled
                                if (hr < 0)
                                {
                                    return false;
                                }
                            }
                            finally
                            {
                                ScriptStringFree(ref sa);
                            }

                            IntPtr hMF = GDI.CloseEnhMetaFile(hdcMF);
                            if (hMF == IntPtr.Zero)
                            {
                                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                            }
                            try
                            {
                                GDI.LOGFONT lf = new GDI.LOGFONT();
                                bool r = GDI.EnumEnhMetaFile(
                                    IntPtr.Zero, // hdc
                                    hMF,
                                    delegate (
                                        IntPtr hdc_,
                                        IntPtr[] lpht,// capacity is nHandles
                                        IntPtr lpmr, // ENHMETARECORD
                                        int nHandles,
                                        int data) // LPARAM
                                    {
                                        // read lpmr->iType
                                        int iType = Marshal.ReadInt32(lpmr);
                                        if (iType == GDI.EMR_EXTCREATEFONTINDIRECTW)
                                        {
                                            object cfio = Marshal.PtrToStructure(lpmr, typeof(GDI.EMREXTCREATEFONTINDIRECT));
                                            GDI.EMREXTCREATEFONTINDIRECT cfi = (GDI.EMREXTCREATEFONTINDIRECT)cfio;
                                            // Last one wins!
                                            lf = cfi.elfw.elfLogFont;
                                        }
                                        return 1;
                                    },
                                    IntPtr.Zero, // LPARAM
                                    IntPtr.Zero); // &RECT
                                if (!r)
                                {
                                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                                }
                                if (!String.IsNullOrEmpty(lf.lfFaceName))
                                {
                                    fallbackFont = lf;
                                    return true;
                                }
                            }
                            finally
                            {
                                GDI.DeleteEnhMetaFile(hMF);
                            }
                            return false;
                        }
                    }
                }
            }

            private delegate bool ProcessTextItemMethod(
                int iItem,
                Point where,
                int endX,
                ref SCRIPT_ITEM item,
                ref ItemInfo itemExtra,
                Font font);

            private void ProcessText(
                Point position,
                ProcessTextItemMethod renderTextItem)
            {
                if (cItems == 0)
                {
                    return;
                }

                // Display Text Using Uniscribe

                //...


                // 3. (Optional) To justify the text, either call ScriptJustify or use specialized knowledge of the text.

                // 4. Use the visual-to-logical map to display the runs in visual order. Starting at the left end of the
                // line, call ScriptTextOut to display the run given by the first entry in the map. For each subsequent
                // entry in the map, call ScriptTextOut to display the indicated run to the right of the previously
                // displayed run.
                // If omitting step 2, start at the left end of the line and call ScriptTextOut to display the first
                // logical run, and then to display each logical run to the right of the previous run.
                Point where = position;
                for (int ii = 0; ii < cItems; ii++)
                {
                    int i = iVisualToLogical[ii];

                    int start = sItems[i].iCharPos;
                    int length = sItems[i + 1].iCharPos - start;

                    Font font = null;
                    if (sItemsExtra[i].fallbackFont != null)
                    {
                        font = sItemsExtra[i].fallbackFont;
                    }
                    else
                    {
                        for (int f = 0, pos = 0; f < fontRuns.Length; pos += fontRuns[f].count, f++)
                        {
                            font = fontRuns[f].font;
                            if (pos < start + length)
                            {
                                break;
                            }
                        }
                    }

                    int endX = where.X + sItemsExtra[i].totalWidth;

                    if (!renderTextItem(
                        i,
                        where,
                        endX,
                        ref sItems[i],
                        ref sItemsExtra[i],
                        font))
                    {
                        break;
                    }

                    where.X = endX;
                }

                // 5. Repeat the steps above for all lines in the paragraph.
            }

            public void DrawText(
                Graphics graphics,
                Bitmap backing,
                Point position,
                Color foreColor,
                Color backColor)
            {
                COLORREF foreColorRef = new COLORREF(foreColor);

                int width = backing.Width;
                int height = backing.Height;
                Rectangle bounds = new Rectangle(0, 0, width, height);
                Rectangle margin = new Rectangle(-height, 0, width + 2 * height, height);
                Debug.Assert(service.offscreenStrip != null);
                Debug.Assert(service.offscreenStrip.Width == width);
                Debug.Assert(service.offscreenStrip.Height == height);

                using (GDIBrush backBrush = new GDIBrush(backColor))
                {
                    GDI.FillRect(service.hdcOffscreenStrip, ref bounds, backBrush);
                }

                ProcessText(
                    position,
                    delegate (
                        int iItem,
                        Point where,
                        int endX,
                        ref SCRIPT_ITEM item,
                        ref ItemInfo itemExtra,
                        Font font)
                    {
                        if (!graphics.IsVisible(new Rectangle(where.X - height, 0, endX - where.X + 2 * height, height)))
                        {
                            return true;
                        }

                        int fontCacheIndex = service.FontCacheIndex(font);

                        GDI.SetTextColor(service.hdcOffscreenStrip, foreColorRef);
                        GDI.SetBkMode(service.hdcOffscreenStrip, GDI.TRANSPARENT);
                        GDI.SelectObject(service.hdcOffscreenStrip, service.fontToHFont[font]);

                        int hr = ScriptTextOut(
                            service.hdcOffscreenStrip,
                            ref service.caches[fontCacheIndex].cache,
                            where.X,
                            where.Y,
                            (ScriptTextOutOptions)0,
                            IntPtr.Zero/*cliprect*/,
                            ref item.a,
                            IntPtr.Zero/*reserved*/,
                            0/*reserved*/,
                            itemExtra.glyphs,
                            itemExtra.cGlyphs,
                            itemExtra.iAdvances,
                            null/*piJustify*/,
                            itemExtra.goffsets);
                        if (hr < 0)
                        {
                            Marshal.ThrowExceptionForHR(hr);
                        }

                        return true;
                    });

                using (GDIRegion gdiRgnClip = new GDIRegion(graphics.Clip.GetHrgn(graphics)))
                {
                    using (GraphicsHDC gdiHdcOffscreen = new GraphicsHDC(graphics))
                    {
                        // Graphics/GDI+ doesn't pass clip region through so we have to reset it explicitly
                        GDI.SelectClipRgn(gdiHdcOffscreen, gdiRgnClip);

                        GDI.BitBlt(
                            gdiHdcOffscreen,
                            0,
                            0,
                            width,
                            height,
                            service.hdcOffscreenStrip,
                            0,
                            0,
                            GDI.SRCCOPY);
                    }
                }
            }

            public Region BuildRegion(
                Graphics graphics,
                Point position,
                int startPos,
                int endPosPlusOne)
            {
                Region region = new Region(new Rectangle());

                ProcessText(
                    position,
                    delegate (
                        int iItem,
                        Point where,
                        int endX,
                        ref SCRIPT_ITEM item,
                        ref ItemInfo itemExtra,
                        Font font)
                    {
                        if ((sItems[iItem].iCharPos >= endPosPlusOne)
                            || (sItems[iItem + 1].iCharPos <= startPos))
                        {
                            return true;
                        }

                        int startPosRel = Math.Max(startPos - sItems[iItem].iCharPos, 0);
                        int length = sItems[iItem + 1].iCharPos - sItems[iItem].iCharPos;
                        int endPosP1Rel = Math.Min(endPosPlusOne - sItems[iItem].iCharPos, length);

                        // http://www.catch22.net/tuts/drawing-styled-text-uniscribe
                        int startGlyph = itemExtra.logicalClusters[startPosRel];
                        Debug.Assert(unchecked((uint)startGlyph < (uint)itemExtra.cGlyphs));
                        //int endGlyph = itemExtra.logicalClusters[endPosP1Rel - 1];
                        int endGlyph;
                        if (!sItems[iItem].a.fLayoutRTL)
                        {
                            if (endPosP1Rel < length)
                            {
                                endGlyph = itemExtra.logicalClusters[endPosP1Rel] - 1;
                            }
                            else
                            {
                                endGlyph = itemExtra.cGlyphs - 1;
                            }
                        }
                        else
                        {
                            if (endPosP1Rel < length)
                            {
                                endGlyph = itemExtra.logicalClusters[endPosP1Rel] + 1;
                            }
                            else
                            {
                                endGlyph = 0;
                            }
                        }
                        Debug.Assert(unchecked((uint)endGlyph < (uint)itemExtra.cGlyphs));
                        int x = where.X;
                        int leftX = Int32.MaxValue, rightX = Int32.MinValue;
                        Debug.Assert((startGlyph == endGlyph) || ((startGlyph > endGlyph) == sItems[iItem].a.fLayoutRTL));
                        if (startGlyph > endGlyph)
                        {
                            int t = startGlyph;
                            startGlyph = endGlyph;
                            endGlyph = t;
                        }
                        for (int i = 0; i < itemExtra.cGlyphs; i++)
                        {
                            if (i == startGlyph)
                            {
                                leftX = x;
                            }
                            x += itemExtra.iAdvances[i];
                            if (i == endGlyph)
                            {
                                rightX = x;
                            }
                        }
                        Rectangle rect = new Rectangle(
                            new Point(leftX, 0),
                            new Size(rightX - leftX, lineHeight));
                        region.Union(rect);

                        return true;
                    });

                return region;
            }

            public Size GetExtent(
                Graphics graphics)
            {
                int width = 0;

                ProcessText(
                    new Point(),
                    delegate (
                        int iItem,
                        Point where,
                        int endX,
                        ref SCRIPT_ITEM item,
                        ref ItemInfo itemExtra,
                        Font font)
                    {
                        width = Math.Max(width, Math.Max(where.X, endX));
                        return true;
                    });

                return new Size(width, lineHeight);
            }

            public void CharPosToX(
                Graphics graphics,
                int offset,
                bool trailing,
                out int xOut)
            {
                int x = 0;

                ProcessText(
                    new Point(),
                    delegate (
                        int iItem,
                        Point where,
                        int endX,
                        ref SCRIPT_ITEM item,
                        ref ItemInfo itemExtra,
                        Font font)
                    {
                        if (((offset >= sItems[iItem].iCharPos) && (offset < sItems[iItem + 1].iCharPos))
                            || (!trailing && (offset == sItems[iItem + 1].iCharPos))
                            || (iItem == cItems - 1))
                        {
                            int hr = ScriptCPtoX(
                                offset - sItems[iItem].iCharPos,
                                trailing,
                                sItems[iItem + 1].iCharPos - sItems[iItem].iCharPos,
                                sItemsExtra[iItem].cGlyphs,
                                sItemsExtra[iItem].logicalClusters,
                                sItemsExtra[iItem].visAttrs,
                                sItemsExtra[iItem].iAdvances,
                                ref sItems[iItem].a,
                                out x);
                            if (hr < 0)
                            {
                                Marshal.ThrowExceptionForHR(hr);
                            }
                            x += where.X;
                            return false;
                        }
                        return true;
                    });

                xOut = x;
            }

            // https://msdn.microsoft.com/en-us/library/windows/desktop/dd319054%28v=vs.85%29.aspx
            public void XToCharPos(
                Graphics graphics,
                int x,
                out int offsetOut,
                out bool trailingOut)
            {
                int offset = 0;
                bool trailing = false;

                ProcessText(
                    new Point(),
                    delegate (
                        int iItem,
                        Point where,
                        int endX,
                        ref SCRIPT_ITEM item,
                        ref ItemInfo itemExtra,
                        Font font)
                    {
                        int offset1;
                        int trailing1;
                        int hr = ScriptXtoCP(
                            x - where.X,
                            sItems[iItem + 1].iCharPos - sItems[iItem].iCharPos,
                            sItemsExtra[iItem].cGlyphs,
                            sItemsExtra[iItem].logicalClusters,
                            sItemsExtra[iItem].visAttrs,
                            sItemsExtra[iItem].iAdvances,
                            ref sItems[iItem].a,
                            out offset1,
                            out trailing1);
                        if (hr < 0)
                        {
                            Marshal.ThrowExceptionForHR(hr);
                        }
                        if (offset1 >= 0)
                        {
                            offset = offset1 + sItems[iItem].iCharPos + trailing1;
                            trailing = trailing1 != 0;
                            if ((offset1 < sItems[iItem + 1].iCharPos - sItems[iItem].iCharPos)
                                || ((trailing1 != 0) && (offset1 == sItems[iItem + 1].iCharPos - sItems[iItem].iCharPos)))
                            {
                                return false;
                            }
                        }
                        return true;
                    });

                offsetOut = offset;
                trailingOut = trailing;
            }

            public void NextCharBoundary(int offset, out int nextOffset)
            {
                while (offset < count)
                {
                    offset++;
                    if ((offset == count) || logAttrs[offset].fCharStop)
                    {
                        break;
                    }
                }
                nextOffset = offset;
            }

            public void PreviousCharBoundary(int offset, out int prevOffset)
            {
                while (offset > 0)
                {
                    offset--;
                    if ((offset == 0) || logAttrs[offset].fCharStop)
                    {
                        break;
                    }
                }
                prevOffset = offset;
            }

            public void NextWordBoundary(int offset, out int nextOffset)
            {
                while (offset < count)
                {
                    offset++;
                    if ((offset == count)
                        || logAttrs[offset].fWordStop
                        || logAttrs[offset].fSoftBreak
                        || (logAttrs[offset].fWhiteSpace != logAttrs[offset - 1].fWhiteSpace))
                    {
                        break;
                    }
                }
                nextOffset = offset;
            }

            public void PreviousWordBoundary(int offset, out int prevOffset)
            {
                while (offset > 0)
                {
                    offset--;
                    if ((offset == 0)
                        || logAttrs[offset].fWordStop
                        || logAttrs[offset].fSoftBreak
                        || ((offset - 1 >= 0) && (logAttrs[offset].fWhiteSpace != logAttrs[offset - 1].fWhiteSpace)))
                    {
                        break;
                    }
                }
                prevOffset = offset;
            }
        }


        // Interop goo

#if DEBUG
        private static string B(string name, bool value)
        {
            return String.Concat(Environment.NewLine, value ? name.ToUpper() : String.Concat("!", name));
        }
#endif

        private const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
        private const int E_PENDING = unchecked((int)0x8000000A);
        private const int USP_E_SCRIPT_NOT_IN_FONT = unchecked((int)0x80040200);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368801%28v=vs.85%29.aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct SCRIPT_DIGITSUBSTITUTE
        {
            public short NationalDigitLanguage;
            public short TraditionalDigitLanguage;
            public byte DigitSubstitute;
            public int dwReserved;

#if DEBUG
            public override string ToString()
            {
                return String.Format(
                    "(CONTROL NationalDigitLanguage={0} TraditionalDigitLanguage={1} DigitSubstitute={2})",
                    NationalDigitLanguage,
                    TraditionalDigitLanguage,
                    DigitSubstitute);
            }
#endif
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368800%28v=vs.85%29.aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct SCRIPT_CONTROL
        {
            public ushort uDefaultLanguage;
            private MASK mask;

            private enum MASK : ushort
            {
                ContextDigits = 1 << 0,
                InvertPreBoundDir = 1 << 1,
                InvertPostBoundDir = 1 << 2,
                LinkStringBefore = 1 << 3,
                LinkStringAfter = 1 << 4,
                NeutralOverride = 1 << 5,
                NumericOverride = 1 << 6,
                LegacyBidiClass = 1 << 7,
                MergeNeutralItems = 1 << 8,
                UseStandardBidi = 1 << 9,
            }

            public bool fContextDigits { get { return Get(mask, MASK.ContextDigits); } set { Set(value, ref mask, MASK.ContextDigits); } }
            public bool fInvertPreBoundDir { get { return Get(mask, MASK.InvertPreBoundDir); } set { Set(value, ref mask, MASK.InvertPreBoundDir); } }
            public bool fInvertPostBoundDir { get { return Get(mask, MASK.InvertPostBoundDir); } set { Set(value, ref mask, MASK.InvertPostBoundDir); } }
            public bool fLinkStringBefore { get { return Get(mask, MASK.LinkStringBefore); } set { Set(value, ref mask, MASK.LinkStringBefore); } }
            public bool fLinkStringAfter { get { return Get(mask, MASK.LinkStringAfter); } set { Set(value, ref mask, MASK.LinkStringAfter); } }
            public bool fNeutralOverride { get { return Get(mask, MASK.NeutralOverride); } set { Set(value, ref mask, MASK.NeutralOverride); } }
            public bool fNumericOverride { get { return Get(mask, MASK.NumericOverride); } set { Set(value, ref mask, MASK.NumericOverride); } }
            public bool fLegacyBidiClass { get { return Get(mask, MASK.LegacyBidiClass); } set { Set(value, ref mask, MASK.LegacyBidiClass); } }
            public bool fMergeNeutralItems { get { return Get(mask, MASK.MergeNeutralItems); } set { Set(value, ref mask, MASK.MergeNeutralItems); } }
            public bool fUseStandardBidi { get { return Get(mask, MASK.UseStandardBidi); } set { Set(value, ref mask, MASK.UseStandardBidi); } }

            private static bool Get(MASK flags, MASK mask)
            {
                return (flags & mask) != 0;
            }

            private static void Set(bool f, ref MASK flags, MASK mask)
            {
                flags = f ? (flags | mask) : (flags & ~mask);
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format(
                    "(CONTROL uDefaultLanguage={0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10})",
                    uDefaultLanguage,
                    B("fContextDigits", fContextDigits),
                    B("fInvertPreBoundDir", fInvertPreBoundDir),
                    B("fInvertPostBoundDir", fInvertPostBoundDir),
                    B("fLinkStringBefore", fLinkStringBefore),
                    B("fLinkStringAfter", fLinkStringAfter),
                    B("fNeutralOverride", fNeutralOverride),
                    B("fNumericOverride", fNumericOverride),
                    B("fLegacyBidiClass", fLegacyBidiClass),
                    B("fMergeNeutralItems", fMergeNeutralItems),
                    B("fUseStandardBidi", fUseStandardBidi));
            }
#endif
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd374043(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct SCRIPT_STATE
        {
            private MASK mask;

            private enum MASK : ushort
            {
                BidiLevel_Divisor = 1 << 0,
                BidiLevel_Mask = ((1 << 5) - 1) & ~(BidiLevel_Divisor - 1),
                OverrideDirection = 1 << 5,
                InhibitSymSwap = 1 << 6,
                CharShape = 1 << 7,
                DigitSubstitute = 1 << 8,
                InhibitLigate = 1 << 9,
                DisplayZWG = 1 << 10,
                ArabicNumContext = 1 << 11,
                GcpClusters = 1 << 12,
            }

            public int uBidiLevel
            {
                get
                {
                    return (int)(mask & MASK.BidiLevel_Mask) / (int)MASK.BidiLevel_Divisor;
                }
                set
                {
                    mask = (MASK)((mask & ~MASK.BidiLevel_Mask)
                        | ((MASK)(value * (int)MASK.BidiLevel_Divisor) & MASK.BidiLevel_Mask));
                }
            }

            public bool fOverrideDirection { get { return Get(mask, MASK.OverrideDirection); } set { Set(value, ref mask, MASK.OverrideDirection); } }
            public bool fInhibitSymSwap { get { return Get(mask, MASK.InhibitSymSwap); } set { Set(value, ref mask, MASK.InhibitSymSwap); } }
            public bool fCharShape { get { return Get(mask, MASK.CharShape); } set { Set(value, ref mask, MASK.CharShape); } }
            public bool fDigitSubstitute { get { return Get(mask, MASK.DigitSubstitute); } set { Set(value, ref mask, MASK.DigitSubstitute); } }
            public bool fInhibitLigate { get { return Get(mask, MASK.InhibitLigate); } set { Set(value, ref mask, MASK.InhibitLigate); } }
            public bool fDisplayZWG { get { return Get(mask, MASK.DisplayZWG); } set { Set(value, ref mask, MASK.DisplayZWG); } }
            public bool fArabicNumContext { get { return Get(mask, MASK.ArabicNumContext); } set { Set(value, ref mask, MASK.ArabicNumContext); } }
            public bool fGcpClusters { get { return Get(mask, MASK.GcpClusters); } set { Set(value, ref mask, MASK.GcpClusters); } }

            private static bool Get(MASK flags, MASK mask)
            {
                return (flags & mask) != 0;
            }

            private static void Set(bool f, ref MASK flags, MASK mask)
            {
                flags = f ? (flags | mask) : (flags & ~mask);
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format(
                    "(STATE uBidiLevel={0} {1} {2} {3} {4} {5} {6} {7} {8})",
                    uBidiLevel,
                    B("fOverrideDirection", fOverrideDirection),
                    B("fInhibitSymSwap", fInhibitSymSwap),
                    B("fCharShape", fCharShape),
                    B("fDigitSubstitute", fDigitSubstitute),
                    B("fInhibitLigate", fInhibitLigate),
                    B("fDisplayZWG", fDisplayZWG),
                    B("fArabicNumContext", fArabicNumContext),
                    B("fGcpClusters", fGcpClusters));
            }
#endif
        }

        private const int SCRIPT_UNDEFINED = 0;

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd374039(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct SCRIPT_ITEM
        {
            public int iCharPos;
            public SCRIPT_ANALYSIS a;

#if DEBUG
            public override string ToString()
            {
                return String.Format(
                    "(ITEM iCharPos={0} a={1})",
                    iCharPos,
                    a.ToString());
            }
#endif
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SCRIPT_ANALYSIS
        {
            private MASK mask;
            public SCRIPT_STATE s;

            private enum MASK : ushort
            {
                Script_Divisor = 1 << 0,
                Script_Mask = ((1 << 10) - 1) & ~(Script_Divisor - 1),
                RTL = 1 << 10,
                LayoutRTL = 1 << 11,
                LinkBefore = 1 << 12,
                LinkAfter = 1 << 13,
                LogicalOrder = 1 << 14,
                NoGlyphIndex = 1 << 15,
            }

            public int eScript
            {
                get
                {
                    return (int)(mask & MASK.Script_Mask) / (int)MASK.Script_Divisor;
                }
                set
                {
                    mask = (MASK)((mask & ~MASK.Script_Mask)
                        | ((MASK)(value * (int)MASK.Script_Divisor) & MASK.Script_Mask));
                }
            }

            public bool fRTL { get { return Get(mask, MASK.RTL); } set { Set(value, ref mask, MASK.RTL); } }
            public bool fLayoutRTL { get { return Get(mask, MASK.LayoutRTL); } set { Set(value, ref mask, MASK.LayoutRTL); } }
            public bool fLinkBefore { get { return Get(mask, MASK.LinkBefore); } set { Set(value, ref mask, MASK.LinkBefore); } }
            public bool fLinkAfter { get { return Get(mask, MASK.LinkAfter); } set { Set(value, ref mask, MASK.LinkAfter); } }
            public bool fLogicalOrder { get { return Get(mask, MASK.LogicalOrder); } set { Set(value, ref mask, MASK.LogicalOrder); } }
            public bool fNoGlyphIndex { get { return Get(mask, MASK.NoGlyphIndex); } set { Set(value, ref mask, MASK.NoGlyphIndex); } }

            private static bool Get(MASK flags, MASK mask)
            {
                return (flags & mask) != 0;
            }

            private static void Set(bool f, ref MASK flags, MASK mask)
            {
                flags = f ? (flags | mask) : (flags & ~mask);
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format(
                    "(ANALYSIS eScript={0} {1} {2} {3} {4} {5} {6} {7})",
                    eScript,
                    B("fRTL", fRTL),
                    B("fLayoutRTL", fLayoutRTL),
                    B("fLinkBefore", fLinkBefore),
                    B("fLinkAfter", fLinkAfter),
                    B("fLogicalOrder", fLogicalOrder),
                    B("fNoGlyphIndex", fNoGlyphIndex),
                    s.ToString());
            }
#endif
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd374046%28v=vs.85%29.aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct SCRIPT_VISATTR
        {
            private MASK mask;

            private enum MASK : ushort
            {
                Justification_Divisor = 1 << 0,
                Justification_Mask = ((1 << 4) - 1) & ~(Justification_Divisor - 1),
                ClusterStart = 1 << 4,
                Diacritic = 1 << 5,
                ZeroWidth = 1 << 6,
            }

            public int uJustification
            {
                get
                {
                    return (int)(mask & MASK.Justification_Mask) / (int)MASK.Justification_Divisor;
                }
                set
                {
                    mask = (MASK)((mask & ~MASK.Justification_Mask)
                        | ((MASK)(value * (int)MASK.Justification_Divisor) & MASK.Justification_Mask));
                }
            }

            public bool fClusterStart { get { return Get(mask, MASK.ClusterStart); } set { Set(value, ref mask, MASK.ClusterStart); } }
            public bool fDiacritic { get { return Get(mask, MASK.Diacritic); } set { Set(value, ref mask, MASK.Diacritic); } }
            public bool fZeroWidth { get { return Get(mask, MASK.ZeroWidth); } set { Set(value, ref mask, MASK.ZeroWidth); } }

            private static bool Get(MASK flags, MASK mask)
            {
                return (flags & mask) != 0;
            }

            private static void Set(bool f, ref MASK flags, MASK mask)
            {
                flags = f ? (flags | mask) : (flags & ~mask);
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format(
                    "(VISATTR uJustification={0} {1} {2} {3})",
                    uJustification,
                    B("fClusterStart", fClusterStart),
                    B("fDiacritic", fDiacritic),
                    B("fZeroWidth", fZeroWidth));
            }
#endif
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd374041(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct SCRIPT_LOGATTR
        {
            private MASK mask;

            private enum MASK : byte
            {
                SoftBreak = 1 << 0,
                WhiteSpace = 1 << 1,
                CharStop = 1 << 2,
                WordStop = 1 << 3,
                Invalid = 1 << 4,
            }

            public bool fSoftBreak { get { return Get(mask, MASK.SoftBreak); } set { Set(value, ref mask, MASK.SoftBreak); } }
            public bool fWhiteSpace { get { return Get(mask, MASK.WhiteSpace); } set { Set(value, ref mask, MASK.WhiteSpace); } }
            public bool fCharStop { get { return Get(mask, MASK.CharStop); } set { Set(value, ref mask, MASK.CharStop); } }
            public bool fWordStop { get { return Get(mask, MASK.WordStop); } set { Set(value, ref mask, MASK.WordStop); } }
            public bool fInvalid { get { return Get(mask, MASK.Invalid); } set { Set(value, ref mask, MASK.Invalid); } }

            private static bool Get(MASK flags, MASK mask)
            {
                return (flags & mask) != 0;
            }

            private static void Set(bool f, ref MASK flags, MASK mask)
            {
                flags = f ? (flags | mask) : (flags & ~mask);
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format(
                    "(LOGATTR {0} {1} {2} {3} {4})",
                    B("fSoftBreak", fSoftBreak),
                    B("fWhiteSpace", fWhiteSpace),
                    B("fCharStop", fCharStop),
                    B("fWordStop", fWordStop),
                    B("fInvalid", fInvalid));
            }
#endif
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368798%28v=vs.85%29.aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct SCRIPT_CACHE
        {
            private IntPtr cache;

            public void Clear()
            {
                ScriptFreeCache(ref this);
            }
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd318141%28v=vs.85%29.aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct GOFFSET
        {
            public long du;
            public long dv;
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd162454(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct ABC
        {
            public int abcA;
            public uint abcB;
            public int abcC;
        }

        [Flags]
        private enum ScriptIsComplexFlags : int
        {
            SIC_COMPLEX = 1,
            SIC_ASCIIDIGIT = 2,
            SIC_NEUTRAL = 4,
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368802%28v=vs.85%29.aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct SCRIPT_FONTPROPERTIES
        {
            public int cBytes;
            public short wgBlank;
            public short wgDefault;
            public short wgInvalid;
            public short wgKashida;
            public int iKashidaWidth;
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd319098(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct OPENTYPE_TAG
        {
            public UInt32 tag; // char[4] identifier

#if DEBUG
            public override string ToString()
            {
                if (tag == SCRIPT_TAG_UNKNOWN)
                {
                    return "SCRIPT_TAG_UNKNOWN";
                }
                else
                {
                    return String.Format(
                        "\"{0}{1}{2}{3}\"",
                        (char)(byte)(tag >> 0),
                        (char)(byte)(tag >> 8),
                        (char)(byte)(tag >> 16),
                        (char)(byte)(tag >> 24));
                }
            }
#endif
        }
        private const uint SCRIPT_TAG_UNKNOWN = 0x00000000;

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd374075(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct TEXTRANGE_PROPERTIES
        {
            public OPENTYPE_FEATURE_RECORD[] potfRecords;
            public int cotfRecords;
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd319096(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct OPENTYPE_FEATURE_RECORD
        {
            public OPENTYPE_TAG tagFeature;
            public int lParameter;
        }

        //
        [StructLayout(LayoutKind.Sequential)]
        private struct SCRIPT_CHARPROP
        {
            private MASK mask;

            private enum MASK : ushort
            {
                CanGlyphAlone = 1 << 0,
            }

            public bool fCanGlyphAlone { get { return Get(mask, MASK.CanGlyphAlone); } set { Set(value, ref mask, MASK.CanGlyphAlone); } }

            private static bool Get(MASK flags, MASK mask)
            {
                return (flags & mask) != 0;
            }

            private static void Set(bool f, ref MASK flags, MASK mask)
            {
                flags = f ? (flags | mask) : (flags & ~mask);
            }
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd374038(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct SCRIPT_GLYPHPROP
        {
            public SCRIPT_VISATTR sva;
            public short reserved;
        }


        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368554%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptIsComplex(
            [In] IntPtr pwcInChars,
            [In] int cInChars,
            [In] ScriptIsComplexFlags dwFlags);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd319116%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptApplyDigitSubstitution(
            IntPtr psds, // SCRIPT_DIGITSUBSTITUTE, null ok
            [MarshalAs(UnmanagedType.Struct)] out SCRIPT_CONTROL psc,
            [MarshalAs(UnmanagedType.Struct)] out SCRIPT_STATE pss);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368556%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptItemize(
            [In] IntPtr pwcInChars,
            [In] int cInChars,
            [In] int cMaxItems,
            [In, Optional, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_CONTROL psControl, // SCRIPT_CONTROL, null ok
            [In, Optional, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_STATE psState, // SCRIPT_STATE, null ok
            [Out] SCRIPT_ITEM[] pItems, // of capacity cMaxItems + 1, length pcItems
            out int pcItems);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368557(v=vs.85).aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptItemizeOpenType(
            [In] IntPtr pwcInChars,
            [In] int cInChars,
            [In] int cMaxItems,
            [In, Optional, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_CONTROL psControl,
            [In, Optional, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_STATE psState,
            [Out] SCRIPT_ITEM[] pItems, // of capacity cMaxItems + 1, length pcItems + 1
            [Out] OPENTYPE_TAG[] pScriptTags, // of capacity cMaxItems, length pcItems
            out int pcItems);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd319118%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptBreak(
            [In] IntPtr pwcChars,
            [In] int cChars,
            [In, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_ANALYSIS psa,
            [Out] SCRIPT_LOGATTR[] psla); // of length cChars

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368564%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptShape(
            [In, Optional] IntPtr hdc,
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_CACHE psc,
            [In] IntPtr pwcChars,
            [In] int cChars,
            [In] int cMaxGlyphs, // see documentation for capacity recommendations
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_ANALYSIS psa, // SCRIPT_ANALYSIS
            [Out] short[] pwOutGlyphs, // of capacity cMaxGlyphs
            [Out] short[] pwLogClust, // of capacity cChars
            [Out] SCRIPT_VISATTR[] psva, // of capacity cMaxGlyphs
            out int pcGlyphs);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368565(v=vs.85).aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptShapeOpenType(
            [In, Optional] IntPtr hdc,
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_CACHE psc,
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_ANALYSIS psa,
            [In, MarshalAs(UnmanagedType.Struct)] OPENTYPE_TAG tagScript,
            [In, MarshalAs(UnmanagedType.Struct)] OPENTYPE_TAG tagLangSys,
            [In, Optional] int[] rcRangeChars,
            [In, Optional] TEXTRANGE_PROPERTIES[] rpRangeProperties, // of length cRanges
            [In] int cRanges,
            [In] IntPtr pwcChars,
            [In] int cChars,
            [In] int cMaxGlyphs,
            [Out] short[] pwLogClust, // of length cChars
            [Out] SCRIPT_CHARPROP[] pCharProps, // of length cChars
            [Out] short[] pwOutGlyphs, // of capacity cMaxGlyphs, length pcGlyphs
            [Out] SCRIPT_GLYPHPROP[] pOutGlyphProps, // of capacity cMaxGlyphs, length pcGlyphs
            out int pcGlyphs);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd319121%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptFreeCache(
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_CACHE psc);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368560%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptPlace(
            [In, Optional] IntPtr hdc,
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_CACHE psc,
            [In] short[] pwGlyphs,
            [In] int cGlyphs,
            [In, Out] SCRIPT_VISATTR[] psva,
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_ANALYSIS psa,
            [Out] int[] piAdvance, // of capacity cGlyphs
            [Out] GOFFSET[] pGoffset, // of capacity cGlyphs
            [Out, MarshalAs(UnmanagedType.Struct)] out ABC pABC);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368557(v=vs.85).aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptPlaceOpenType(
            [In, Optional] IntPtr hdc,
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_CACHE psc,
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_ANALYSIS psa,
            [In] OPENTYPE_TAG tagScript,
            [In] OPENTYPE_TAG tagLangSys,
            [In, Optional] int[] rcRangeChars,
            [In, Optional] TEXTRANGE_PROPERTIES[] rpRangeProperties, // of length cRanges
            [In] int cRanges,
            [In] IntPtr pwcChars,
            [Out] short[] pwLogClust, // of length cChars
            [In] SCRIPT_CHARPROP[] pCharProps,
            [In] int cChars,
            [In] short[] pwGlyphs,
            [In] SCRIPT_GLYPHPROP[] pGlyphProps,
            [In] int cGlyphs,
            [Out] int[] piAdvance,
            [Out] GOFFSET[] pGoffset,
            [Out, MarshalAs(UnmanagedType.Struct)] out ABC pABC);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368559%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptLayout(
            [In] int cRuns,
            [In] byte[] pbLevel, // of length cRuns
            [Out, Optional] int[] piVisualToLogical, // of length cRuns
            [Out, Optional] int[] piLogicalToVisual); // of length cRuns

        // TODO: ScriptJustify
        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368558%28v=vs.85%29.aspx

        [Flags]
        private enum ScriptTextOutOptions : uint
        {
            ETO_OPAQUE = 0x0002,
            ETO_CLIPPED = 0x0004,
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368795%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptTextOut(
            [In] IntPtr hdc,
            [In, Out] ref SCRIPT_CACHE psc,
            [In] int x,
            [In] int y,
            [In, MarshalAs(UnmanagedType.I4)] ScriptTextOutOptions fuOptions,
            [In, Optional] IntPtr lprc, // RECT
            [In, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_ANALYSIS psa, // SCRIPT_ANALYSIS
            [In] IntPtr pwcReserved, // must be null
            [In] int iReserved, // must be zero
            [In] short[] pwGlyphs,
            [In] int cGlyphs,
            [In] int[] piAdvance,
            [In, Optional] int[] piJustify, // can be null
            [In] GOFFSET[] pGoffset);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368549%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptGetFontProperties(
            [In] IntPtr hdc,
            [In, Out] ref SCRIPT_CACHE psc,
            [Out, MarshalAs(UnmanagedType.Struct)] out SCRIPT_FONTPROPERTIES sfp);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368796%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptXtoCP(
            [In] int iX,
            [In] int cChars,
            [In] int cGlyphs,
            [In] short[] pwLogClust,
            [In] SCRIPT_VISATTR[] psva,
            [In] int[] piAdvance,
            [In, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_ANALYSIS psa,
            [Out] out int piCP,
            [Out] out int piTrailing);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd319120%28v=vs.85%29.aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptCPtoX(
            [In] int iCP,
            [In] bool fTrailing,
            [In] int cChars,
            [In] int cGlyphs,
            [In] short[] pwLogClust,
            [In] SCRIPT_VISATTR[] psva,
            [In]int[] piAdvance,
            [In, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_ANALYSIS psa,
            [Out] out int piX);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368550(v=vs.85).aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptGetFontScriptTags(
            [In, Optional] IntPtr hdc,
            [In, Out] ref SCRIPT_CACHE psc,
            [In, Optional, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_ANALYSIS psa,
            [In] int cMaxTags,
            [Out] OPENTYPE_TAG[] pScriptTags, // capacity cMaxTags, length pcTags
            out int pcTags);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368547(v=vs.85).aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptGetFontFeatureTags(
            [In, Optional] IntPtr hdc,
            [In, Out] ref SCRIPT_CACHE psc,
            [In, Optional, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_ANALYSIS psa,
            [In] OPENTYPE_TAG tagScript,
            [In] OPENTYPE_TAG tagLangSys,
            [In] int cMaxTags,
            [Out] OPENTYPE_TAG[] pFeatureTags, // capacity cMaxTags, length pcTags
            out int pcTags);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368548(v=vs.85).aspx
        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptGetFontLanguageTags(
            [In, Optional] IntPtr hdc,
            [In, Out] ref SCRIPT_CACHE psc,
            [In, Optional, MarshalAs(UnmanagedType.Struct)] ref SCRIPT_ANALYSIS psa,
            [In] OPENTYPE_TAG tagScript,
            [In] int cMaxTags,
            [Out] OPENTYPE_TAG[] pLangSysTags, // capacity cMaxTags, length pcTags
            out int pcTags);

        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptStringAnalyse(
            IntPtr hdc, //In  Device context (required)
            IntPtr pString, //In  String in 8 or 16 bit characters
            int cString, //In  Length in characters (Must be at least 1)
            int cGlyphs, //In  Required glyph buffer size (default cString*1.5 + 16)
            int iCharset, //In  Charset if an ANSI string, -1 for a Unicode string
            int dwFlags, //In  Analysis required
            int iReqWidth, //In  Required width for fit and/or clip
            [In, Optional] IntPtr/*SCRIPT_CONTROL*/ psControl, //In  Analysis control (optional)
            [In, Optional] IntPtr/*SCRIPT_STATE*/ psState, //In  Analysis initial state (optional)
            [In, Optional, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] int[] piDx, //In  Requested logical dx array
            [In, Optional] IntPtr /*SCRIPT_TABDEF*/ pTabdef, //In  Tab positions (optional)
            [In] byte[] pbInClass, //In  Legacy GetCharacterPlacement character classifications (deprecated)
            [Out] out IntPtr/*SCRIPT_STRING_ANALYSIS*/ pssa); //Out Analysis of string

        private const int SSA_PASSWORD = 0x00000001; // Input string contains a single character to be duplicated iLength times
        private const int SSA_TAB = 0x00000002; // Expand tabs
        private const int SSA_CLIP = 0x00000004; // Clip string at iReqWidth
        private const int SSA_FIT = 0x00000008; // Justify string to iReqWidth
        private const int SSA_DZWG = 0x00000010; // Provide representation glyphs for control characters
        private const int SSA_FALLBACK = 0x00000020; // Use fallback fonts
        private const int SSA_BREAK = 0x00000040; // Return break flags (character and word stops)
        private const int SSA_GLYPHS = 0x00000080; // Generate glyphs, positions and attributes
        private const int SSA_RTL = 0x00000100; // Base embedding level 1
        private const int SSA_GCP = 0x00000200; // Return missing glyphs and LogCLust with GetCharacterPlacement conventions
        private const int SSA_HOTKEY = 0x00000400; // Replace '&' with underline on subsequent codepoint
        private const int SSA_METAFILE = 0x00000800; // Write items with ExtTextOutW Unicode calls, not glyphs
        private const int SSA_LINK = 0x00001000; // Apply FE font linking/association to non-complex text
        private const int SSA_HIDEHOTKEY = 0x00002000; // Remove first '&' from displayed string
        private const int SSA_HOTKEYONLY = 0x00002400; // Display underline only.

        private const int SSA_FULLMEASURE = 0x04000000; // Internal - calculate full width and out the number of chars can fit in iReqWidth.
        private const int SSA_LPKANSIFALLBACK = 0x08000000; // Internal - enable FallBack for all LPK Ansi calls Except BiDi hDC calls
        private const int SSA_PIDX = 0x10000000; // Internal
        private const int SSA_LAYOUTRTL = 0x20000000; // Internal - Used when DC is mirrored
        private const int SSA_DONTGLYPH = 0x40000000; // Internal - Used only by GDI during metafiling - Use ExtTextOutA for positioning
        private const int SSA_NOKASHIDA = unchecked((int)0x80000000); // Internal - Used by GCP to justify the non Arabic glyphs only.

        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptStringOut(
            IntPtr/*SCRIPT_STRING_ANALYSIS*/ ssa, //In  Analysis with glyphs
            int iX,
            int iY,
            int uOptions, //In  ExtTextOut options
            [In, Optional] IntPtr/*RECT*/ prc, //In  Clipping rectangle (iff ETO_CLIPPED)
            int iMinSel, //In  Logical selection. Set iMinSel>=iMaxSel for no selection
            int iMaxSel,
            [In, MarshalAs(UnmanagedType.Bool)] bool fDisabled); //In  If disabled, only the background is highlighted.

        [DllImport("usp10.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int ScriptStringFree(
            [In, Out] ref IntPtr/*SCRIPT_STRING_ANALYSIS*/  pssa); //InOut Address of pointer to analysis
    }
}
