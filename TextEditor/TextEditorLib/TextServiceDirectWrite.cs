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
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace TextEditor
{
#if true
    // The Managed C++ Wrapper version

    public class TextServiceDirectWrite : ITextService, IDisposable
    {
        private TextServiceDirectWriteInterop interop;
        private ITextService uniscribeService;

        public TextServiceDirectWrite()
        {
            try
            {
                interop = new TextServiceDirectWriteInterop();
            }
            catch (Exception exception)
            {
                Debugger.Break();
            }

#if true
            uniscribeService = new TextServiceUniscribe();
#else // TODO: support Windows.Data.Text for universal script segmentation support
#endif
        }

        ~TextServiceDirectWrite()
        {
            Debug.Assert(false, "TextServiceDirectWrite: Finalizer invoked - have you forgotten to .Dispose()?");
            Dispose();
        }

        public void Dispose()
        {
            interop._Dispose();
#if true
            uniscribeService.Dispose();
#else // TODO: support Windows.Data.Text for universal script segmentation support
#endif

            GC.SuppressFinalize(this);
        }

        private void ClearCaches()
        {
            interop.ClearCaches();
        }

        public TextService Service { get { return TextService.DirectWrite; } }

        public bool Hardened { get { return false; } }

        public void Reset(
            Font font,
            int visibleWidth)
        {
            interop.Reset(font, visibleWidth);
#if true
            uniscribeService.Reset(font, visibleWidth);
#else // TODO: support Windows.Data.Text for universal script segmentation support
#endif
        }

        public ITextInfo AnalyzeText(
            Graphics graphics,
            Font font,
            string line)
        {
            if (line.IndexOfAny(new char[] { '\r', '\n' }) >= 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            return new TextLayout(
                this,
                line,
                graphics,
                font);
        }


        [ClassInterface(ClassInterfaceType.None)]
        private class TextLayout : ITextInfo, IDisposable
        {
            private TextServiceLineDirectWriteInterop lineInterop;
            private ITextInfo uniscribeLine;
            private string text;

            public TextLayout(
                TextServiceDirectWrite service,
                string line,
                Graphics graphics_,
                Font font_)
            {
                int hr;

                text = line;

                lineInterop = new TextServiceLineDirectWriteInterop();
                hr = lineInterop.Init(service.interop, line);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

#if true
                uniscribeLine = service.uniscribeService.AnalyzeText(graphics_, font_, line);
#else // TODO: support Windows.Data.Text for universal script segmentation support
#endif
            }

            ~TextLayout()
            {
                Debug.Assert(false, "TextServiceDirectWrite.TextItems: Finalizer invoked - have you forgotten to .Dispose()?");
                Dispose();
            }

            public void Dispose()
            {
                lineInterop._Dispose();
#if true
                uniscribeLine.Dispose();
#else // TODO: support Windows.Data.Text for universal script segmentation support
#endif

                GC.SuppressFinalize(this);
            }

            public void DrawText(
                Graphics graphics,
                Bitmap backing,
                Point position,
                Color foreColor,
                Color backColor)
            {
                int hr = lineInterop.DrawText(
                    graphics,
                    position,
                    foreColor,
                    backColor);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

            public Region BuildRegion(
                Graphics graphics,
                Point position,
                int startPos,
                int endPosPlusOne)
            {
                Region region;
                int hr = lineInterop.BuildRegion(
                    graphics,
                    position,
                    startPos,
                    endPosPlusOne,
                    out region);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                return region;
            }

            public Size GetExtent(
                Graphics graphics)
            {
                Size size;
                int hr = lineInterop.GetExtent(
                    graphics,
                    out size);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                return size;
            }

            public void CharPosToX(
                Graphics graphics,
                int offset,
                bool trailing,
                out int x)
            {
                int hr = lineInterop.CharPosToX(
                    graphics,
                    offset,
                    trailing,
                    out x);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

            public void XToCharPos(
                Graphics graphics,
                int x,
                out int offset,
                out bool trailing)
            {
                int hr = lineInterop.XToCharPos(
                    graphics,
                    x,
                    out offset,
                    out trailing);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

            // TODO: Use Windows Text Segmentation API (Windows.Data.Text) for these:
            // https://msdn.microsoft.com/en-us/library/windows/apps/windows.data.text.aspx
            // https://code.msdn.microsoft.com/windowsapps/Text-Segmentation-API-be73de71
            // Probably requires minimum .NET Framework 4.5

            public void NextCharBoundary(
                int offset,
                out int nextOffset)
            {
#if true
                uniscribeLine.NextCharBoundary(offset, out nextOffset);
#else // TODO: support Windows.Data.Text for universal script segmentation support
#endif
            }

            public void PreviousCharBoundary(
                int offset,
                out int prevOffset)
            {
#if true
                uniscribeLine.PreviousCharBoundary(offset, out prevOffset);
#else // TODO: support Windows.Data.Text for universal script segmentation support
#endif
            }

            public void NextWordBoundary(
                int offset,
                out int nextOffset)
            {
#if true
                uniscribeLine.NextWordBoundary(offset, out nextOffset);
#else // TODO: support Windows.Data.Text for universal script segmentation support
#endif
            }

            public void PreviousWordBoundary(
                int offset,
                out int prevOffset)
            {
#if true
                uniscribeLine.PreviousWordBoundary(offset, out prevOffset);
#else // TODO: support Windows.Data.Text for universal script segmentation support
#endif
            }
        }
    }
#else
    // The C# COM-Interop version

    // useful stuff:
    // gflags.exe: https://msdn.microsoft.com/en-us/library/windows/hardware/ff542941%28v=vs.85%29.aspx
    // discussions of Marshal.ReleaseComObject:
    // http://blogs.msdn.com/b/visualstudio/archive/2010/03/01/marshal-releasecomobject-considered-dangerous.aspx
    // http://blogs.msdn.com/b/cbrumme/archive/2003/04/16/51355.aspx
    // http://blogs.msdn.com/b/mbend/archive/2007/04/18/the-mapping-between-interface-pointers-and-runtime-callable-wrappers-rcws.aspx

    public class TextServiceDirectWrite : ITextService, IDisposable
    {
        private IDWriteFactory factory;

        private IDWriteTextFormat textFormat;

        private int visibleWidth;
        private int lineHeight;
        private float baseline;
        private IDWriteBitmapRenderTarget renderTarget; // contains offscreen strip
        private float rdpiX, rdpiY;

        public TextServiceDirectWrite()
        {
            int hr;

            object o;
            hr = DWriteCreateFactory(
                DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED,
                IID_IUnknown,
                out o);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            factory = (IDWriteFactory)o;
        }

        ~TextServiceDirectWrite()
        {
            Debug.Assert(false, "TextServiceDirectWrite: Finalizer invoked - have you forgotten to .Dispose()?");
            Dispose();
        }

        public void Dispose()
        {
            ClearCaches();

            SafeRelease(ref factory);

            GC.SuppressFinalize(this);
        }

        private void ClearCaches()
        {
            SafeRelease(ref textFormat);
            SafeRelease(ref renderTarget);
        }

        public TextService Service { get { return TextService.DirectWrite; } }

        public void Reset(
            Font font,
            int visibleWidth)
        {
            ClearCaches();

            int hr;


            IDWriteFontCollection fontCollection = null;
            IDWriteGdiInterop gdiInterop = null;
            IDWriteFont fontD = null;
            IDWriteFontFamily family = null;
            IDWriteLocalizedStrings familyNames = null;
            try
            {
                hr = factory.GetSystemFontCollection(
                    out fontCollection,
                    false/*checkForUpdates*/);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                hr = factory.GetGdiInterop(out gdiInterop);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }


                //lineHeight = font.Height;
                this.visibleWidth = visibleWidth;
                lineHeight = font.Height;
                using (Image offscreenStrip = new Bitmap(visibleWidth, lineHeight, PixelFormat.Format32bppRgb))
                {
                    using (Graphics offscreenGraphics = Graphics.FromImage(offscreenStrip))
                    {
                        IntPtr hdc = offscreenGraphics.GetHdc();
                        try
                        {
                            // GDI interop: https://msdn.microsoft.com/en-us/library/windows/desktop/dd742734(v=vs.85).aspx
                            // Render to a GDI surface: https://msdn.microsoft.com/en-us/library/windows/desktop/ff485856(v=vs.85).aspx
                            hr = gdiInterop.CreateBitmapRenderTarget(
                                hdc,
                                visibleWidth,
                                lineHeight,
                                out renderTarget);
                            if (hr < 0)
                            {
                                Marshal.ThrowExceptionForHR(hr);
                            }
                        }
                        finally
                        {
                            offscreenGraphics.ReleaseHdc();
                        }
                    }
                }

#if false
            // TODO: this will do the wrong thing since line heights are determined from font size in the winforms
            // code and then imposed upon this code - on non-standard devices text would overflow line or be too small.
#endif
                IntPtr hdcScreen = GetDC(IntPtr.Zero);
                int dpiX = GetDeviceCaps(hdcScreen, LOGPIXELSX);
                int dpiY = GetDeviceCaps(hdcScreen, LOGPIXELSY);
                rdpiX = dpiX / 96f; // TODO: figure out correct thing to put here
                rdpiY = dpiY / 96f; // TODO: figure out correct thing to put here
                hr = renderTarget.SetPixelsPerDip(rdpiY);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                object logFont = new LOGFONT();
                font.ToLogFont(logFont);

                hr = gdiInterop.CreateFontFromLOGFONT(
                    (LOGFONT)logFont,
                    out fontD);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                DWRITE_FONT_METRICS fontMetrics;
                fontD.GetMetrics(out fontMetrics);
                baseline = fontMetrics.ascent * font.Size / fontMetrics.designUnitsPerEm;

                hr = fontD.GetFontFamily(out family);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                hr = family.GetFamilyNames(out familyNames);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                // Get the family name at index zero. If we were going to display the name
                // we'd want to try to find one that matched the use locale, but for purposes
                // of creating a text format object any language will do.
                int familyNameLength;
                hr = familyNames.GetStringLength(0, out familyNameLength);
                char[] familyNameChars = new char[familyNameLength + 1];
                hr = familyNames.GetString(0, familyNameChars, familyNameChars.Length);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                int nul = Array.IndexOf(familyNameChars, (char)0);
                string familyName = new String(familyNameChars, 0, nul >= 0 ? nul : familyNameChars.Length);

                SafeRelease(ref textFormat);
                hr = factory.CreateTextFormat(
                    familyName,
                    null/*service.fontCollection*/, // null: system font collection
                    fontD.GetWeight(),
                    fontD.GetStyle(),
                    fontD.GetStretch(),
                    font.Size,
                    CultureInfo.CurrentCulture.Name,
                    out textFormat);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                hr = textFormat.SetWordWrapping(DWRITE_WORD_WRAPPING.DWRITE_WORD_WRAPPING_NO_WRAP);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
#if false
                DWRITE_TRIMMING trimming = new DWRITE_TRIMMING();
                trimming.granularity = DWRITE_TRIMMING_GRANULARITY.DWRITE_TRIMMING_GRANULARITY_NONE;
                hr = textFormat.SetTrimming(trimming, null);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                // TODO: hr = textFormat.SetReadingDirection(DWRITE_READING_DIRECTION.);
                hr = textFormat.SetTextAlignment(DWRITE_TEXT_ALIGNMENT.DWRITE_TEXT_ALIGNMENT_LEADING);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
#endif
            }
            finally
            {
                SafeRelease(ref familyNames);
                SafeRelease(ref family);
                SafeRelease(ref fontD);
                SafeRelease(ref fontCollection);
                SafeRelease(ref gdiInterop);
            }
        }

        public ITextInfo AnalyzeText(
            Graphics graphics,
            Font font,
            string line)
        {
            return new TextLayout(
                this,
                line,
                font);
        }


        [ClassInterface(ClassInterfaceType.None)]
        private class TextLayout : ITextInfo, IDisposable, IDWritePixelSnapping, IDWriteTextRenderer
        {
            private TextServiceDirectWrite service;
            private IDWriteTextLayout textLayout;

            private readonly int totalChars;

            private uint foreColorRef;

            public TextLayout(
                TextServiceDirectWrite service,
                string line,
                Font font_)
            {
                int hr;

                this.service = service;

                totalChars = line.Length;

#if true
                hr = service.factory.CreateTextLayout(
                    line,
                    line.Length,
                    service.textFormat,
                    50, // layout width - shouldn't matter with DWRITE_WORD_WRAPPING_NO_WRAP specified
                    service.lineHeight,
                    out textLayout);
#else
                hr = service.factory.CreateGdiCompatibleTextLayout(
                    line,
                    line.Length,
                    service.textFormat,
                    50, // layout width - shouldn't matter with DWRITE_WORD_WRAPPING_NO_WRAP specified
                    service.lineHeight,
                    service.rdpiY/*pixelsPerDip*/,
                    DWRITE_MATRIX.Identity,
                    true/*useGdiNatural*/,
                    out textLayout);
#endif
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

#if false // forces layout to happen now. TODO: why does commenting this out cause crashes?
                DWRITE_TEXT_METRICS metrics;
                hr = textLayout.GetMetrics(out metrics);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                Debug.Assert(metrics.lineCount <= 1);
#endif
            }

            ~TextLayout()
            {
                Debug.Assert(false, "TextServiceDirectWrite.TextItems: Finalizer invoked - have you forgotten to .Dispose()?");
                Dispose();
            }

            public void Dispose()
            {
                SafeRelease(ref textLayout);

                service = null;

                GC.SuppressFinalize(this);
            }

            // these values should only be non-null during DrawText()
            private IDWriteRenderingParams renderingParams;

            public void DrawText(
                Graphics graphics_,
                Size backingSize_,
                Point position,
                Color foreColor,
                Color backColor)
            {
                //if (totalChars == 0)
                //{
                //    return;
                //}

                this.foreColorRef = (uint)(foreColor.R | (foreColor.G << 8) | (foreColor.B << 16));

                Debug.Assert(renderingParams == null);
                IntPtr hdc = graphics_.GetHdc();
                try
                {
                    int hr;

                    hr = service.factory.CreateRenderingParams(
                        out renderingParams); // "default settings for the primary monitor"
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    IntPtr hdcMem = service.renderTarget.GetMemoryDC();

                    using (Graphics graphics2 = Graphics.FromHdc(hdcMem))
                    {
                        using (Brush backBrush = new SolidBrush(backColor))
                        {
                            graphics2.FillRectangle(
                                backBrush,
                                new Rectangle(
                                    new Point(),
                                    new Size(service.visibleWidth, service.lineHeight)));
                        }
                    }

                    hr = textLayout.Draw(
                         IntPtr.Zero, // client context
                         this, // IDWriteTextRenderer
                         position.X,
                         position.Y /*+ baseline*/);
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    BitBlt(
                        hdc,
                        0,
                        0,
                        service.visibleWidth,
                        service.lineHeight,
                        hdcMem,
                        0,
                        0,
                        SRCCOPY);
                }
                finally
                {
                    SafeRelease(ref renderingParams);
                    graphics_.ReleaseHdc();
                }
            }

            public Region BuildRegion(
                Graphics graphics,
                Point position,
                int startPos,
                int endPosPlusOne)
            {
                int cMaxMetrics = 8;
                DWRITE_HIT_TEST_METRICS[] metrics;
                int cMetrics;
                while (true)
                {
                    metrics = new DWRITE_HIT_TEST_METRICS[cMaxMetrics];
                    int hr = textLayout.HitTestTextRange(
                        startPos,
                        (endPosPlusOne - startPos),
                        position.X,
                        position.Y /*+ service.baseline*/,
                        metrics,
                        cMaxMetrics,
                        out cMetrics);
                    if (hr == E_NOT_SUFFICIENT_BUFFER)
                    {
                        cMaxMetrics *= 2;
                        continue;
                    }
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }
                    break;
                }

                Region region = new Region(new Rectangle());
                for (int i = 0; i < cMetrics; i++)
                {
                    RectangleF rectF = new RectangleF(
                        metrics[i].left,
                        metrics[i].top,
                        metrics[i].width,
                        metrics[i].height);
                    //rectF.Y -= service.baseline;
                    Rectangle rect = new Rectangle();
                    rect.X = (int)Math.Floor(rectF.Left);
                    rect.Y = (int)Math.Floor(rectF.Top);
                    rect.Width = (int)Math.Ceiling(rectF.Width + rectF.Left - rect.Left);
                    rect.Height = (int)Math.Ceiling(rectF.Height + rectF.Top - rect.Top);
                    region.Union(rect);
                }

                return region;
            }

            public Size GetExtent(
                Graphics graphics)
            {
                DWRITE_TEXT_METRICS metrics;
                int hr = textLayout.GetMetrics(out metrics);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                Debug.Assert(metrics.lineCount <= 1);
                return new Size(
                    (int)Math.Ceiling(metrics.widthIncludingTrailingWhitespace),
                    service.lineHeight);
#if false // hacky
                float x1, y1;
                DWRITE_HIT_TEST_METRICS metrics;
                int hr = textLayout.HitTestTextPosition(
                    totalChars,
                    true/*isTrailingHit*/,
                    out x1,
                    out y1,
                    out metrics);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                return new Size((int)Math.Ceiling(x1), lineHeight);
#endif
            }

            public void CharPosToX(
                Graphics graphics,
                int offset,
                bool trailing,
                out int x)
            {
                float x1, y1;
                DWRITE_HIT_TEST_METRICS metrics;
                int hr = textLayout.HitTestTextPosition(
                    offset,
                    trailing,
                    out x1,
                    out y1,
                    out metrics);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                x = (int)Math.Round(x1);
            }

            public void XToCharPos(
                Graphics graphics,
                int x,
                out int offset,
                out bool trailing)
            {
                bool inside;
                DWRITE_HIT_TEST_METRICS metric;
                int hr = textLayout.HitTestPoint(
                    x,
                    0,
                    out trailing,
                    out inside,
                    out metric);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                offset = (int)metric.textPosition;
            }

            public void NextCharBoundary(
                int offset,
                out int nextOffset)
            {
                nextOffset = offset;
            }

            public void PreviousCharBoundary(
                int offset,
                out int prevOffset)
            {
                prevOffset = offset;
            }

            public void NextWordBoundary(
                int offset,
                out int nextOffset)
            {
                nextOffset = offset;
            }

            public void PreviousWordBoundary(
                int offset,
                out int prevOffset)
            {
                prevOffset = offset;
            }


            //

            public int IsPixelSnappingDisabled(
                IntPtr clientDrawingContext
                /*out bool isDisabled*/)
            {
                //isDisabled = false;
                return S_OK;
            }

            public int GetCurrentTransform(
                IntPtr clientDrawingContext,
                out DWRITE_MATRIX transform)
            {
                transform = DWRITE_MATRIX.Identity;
                return S_OK;
            }

            public int GetPixelsPerDip(
                IntPtr clientDrawingContext,
                out float pixelsPerDip)
            {
                pixelsPerDip = service.rdpiY;
                return S_OK;
            }


            //

            public int DrawGlyphRun(
                IntPtr clientDrawingContext,
                float baselineOriginX,
                float baselineOriginY,
                DWRITE_MEASURING_MODE measuringMode,
                DWRITE_GLYPH_RUN glyphRun,
                DWRITE_GLYPH_RUN_DESCRIPTION glyphRunDescription,
                object clientDrawingEffect)
            {
                Rectangle bb;

                int hr = service.renderTarget.DrawGlyphRun(
                    baselineOriginX,
                    baselineOriginY,
                    measuringMode,
                    glyphRun,
                    renderingParams,
                    foreColorRef,
                    out bb);

                return hr;
            }

            public int DrawUnderline(
                IntPtr clientDrawingContext,
                float baselineOriginX,
                float baselineOriginY,
                DWRITE_UNDERLINE underline,
                object clientDrawingEffect)
            {
                return E_NOTIMPL;
            }

            public int DrawStrikethrough(
                IntPtr clientDrawingContext,
                float baselineOriginX,
                float baselineOriginY,
                DWRITE_STRIKETHROUGH strikethrough,
                object clientDrawingEffect)
            {
                return E_NOTIMPL;
            }

            public int DrawInlineObject(
                IntPtr clientDrawingContext,
                float originX,
                float originY,
                IDWriteInlineObject inlineObject,
                bool isSideways,
                bool isRightToLeft,
                object clientDrawingEffect)
            {
                return E_NOTIMPL;
            }
        }


        // Interop goo


        // short tutorial on .NET COM Interop:
        // https://msdn.microsoft.com/en-us/library/aa645736%28v=vs.71%29.aspx

        private static readonly Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");

        [StructLayout(LayoutKind.Sequential, Size = 8)]
        private struct FILETIME
        {
            public uint DateTimeLow;
            public uint DateTimeHigh;
        }

        private const int S_OK = 0;
        private const int S_FALSE = 1;
        private const int E_FAIL = unchecked((int)0x80004005);
        private const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
        private const int E_NOTIMPL = unchecked((int)0x80004001);
        private const int E_INVALIDARG = unchecked((int)0x80070057);
        private const int E_NOT_SUFFICIENT_BUFFER = unchecked((int)0x8007007a);


        // marshaling enhancements

        private static class Marshal2
        {
            public static float ReadFloat32(IntPtr p, int byteOffset)
            {
                float[] f = new float[1];
                Marshal.Copy(new IntPtr(p.ToInt64() + byteOffset), f, 0, 1);
                return f[0];
            }

            public static float ReadFloat32(IntPtr p)
            {
                return ReadFloat32(p, 0);
            }

            public static void WriteFloat32(IntPtr p, int byteOffset, float value)
            {
                float[] f = new float[1] { value };
                Marshal.Copy(f, 0, new IntPtr(p.ToInt64() + byteOffset), 1);
            }

            public static void WriteFloat32(IntPtr p, float value)
            {
                WriteFloat32(p, 0, value);
            }

            public static void Copy(ushort[] src, int srcOffset, IntPtr dest, int count)
            {
                // Could be made faster using some unsafe code and reinterpret-casting
                for (int i = 0; i < count; i++)
                {
                    short s = unchecked((short)src[i + srcOffset]);
                    Marshal.WriteInt16(dest, i * 2, s);
                }
            }

            public static void Copy(IntPtr src, ushort[] dest, int destOffset, int count)
            {
                // Could be made faster using some unsafe code and reinterpret-casting
                for (int i = 0; i < count; i++)
                {
                    short s = Marshal.ReadInt16(src, i * 2);
                    dest[i + destOffset] = unchecked((ushort)s);
                }
            }

            public static void CopyArrayOfStruct<T>(T[] src, int srcOffset, IntPtr dest, int destByteCapacity, int count) where T : struct
            {
                if ((src == null) || (dest == IntPtr.Zero))
                {
                    Debugger.Break();
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                int elementSize = Marshal.SizeOf(typeof(T));
                int c = count * elementSize;
                if (c > destByteCapacity)
                {
                    Debugger.Break();
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                GCHandle hSrc = GCHandle.Alloc(src, GCHandleType.Pinned);
                try
                {
                    IntPtr bSrc = hSrc.AddrOfPinnedObject();
                    for (int i = 0; i < c; i++)
                    {
                        Marshal.WriteByte(dest, i, Marshal.ReadByte(bSrc, i));
                    }
                }
                catch (Exception e)
                {
                    Debugger.Break();
                    throw;
                }
                finally
                {
                    hSrc.Free();
                }
            }

            public static void CopyArrayOfStruct<T>(IntPtr src, T[] dest, int destOffset, int count) where T : struct
            {
                if ((src == IntPtr.Zero) || (dest == null))
                {
                    Debugger.Break();
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                int elementSize = Marshal.SizeOf(typeof(T));
                int destByteCapacity = (dest.Length - destOffset) * elementSize;
                int c = elementSize * count;
                if (c > destByteCapacity)
                {
                    Debugger.Break();
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                GCHandle hDest = GCHandle.Alloc(dest, GCHandleType.Pinned);
                try
                {
                    int o = destOffset * elementSize;
                    IntPtr bDest = hDest.AddrOfPinnedObject();
                    for (int i = 0; i < c; i++)
                    {
                        Marshal.WriteByte(bDest, i + o, Marshal.ReadByte(src, i));
                    }
                }
                catch (Exception e)
                {
                    Debugger.Break();
                    throw;
                }
                finally
                {
                    hDest.Free();
                }
            }
        }

#if false
        private class SafeRef<T> : IDisposable where T : class
        {
            private T[] holder = new T[1];

            public SafeRef(T reference)
            {
                this.holder[0] = reference;
            }

            public T Ref
            {
                get
                {
                    return holder[0];
                }
            }

            public T[] Take
            {
                get
                {
                    Clear();
                    return holder;
                }
            }

            public void Clear()
            {
                if (holder[0] != null)
                {
                    while (Marshal.ReleaseComObject(holder[0]) != 0) ;
                    holder[0] = null;
                }
            }

            public void Dispose()
            {
                Clear();
            }
        }
#endif

        private static void SafeRelease<T>(ref T t) where T : class
        {
            if (t != null)
            {
                while (Marshal.ReleaseComObject(t) != 0) ;
            }
            t = null;
        }

        private static T SafeDetach<T>(ref T t) where T : class
        {
            T temp = t;
            t = null;
            return temp;
        }

        public class MarshalUnique : ICustomMarshaler
        {
            private Type interfaceType;

            public static ICustomMarshaler GetInstance(string pstrCookie)
            {
                return new MarshalUnique(pstrCookie);
            }

            protected MarshalUnique(string type)
                : this(ResolveType(type))
            {
            }

            private static Type ResolveType(string type)
            {
                if (String.IsNullOrEmpty(type))
                {
                    return Type.GetType("System.Object");
                }
                string[] parts = type.Split('.');
                Type t = Type.GetType(String.Concat(parts[0], ".", parts[1]));
                for (int i = 2; (t != null) && (i < parts.Length); i++)
                {
                    t = t.GetNestedType(parts[i], BindingFlags.NonPublic);
                }
                if (t == null)
                {
                    throw new ArithmeticException(String.Format("Type not found: {0}", type));
                }
                return t;
            }

            protected MarshalUnique(Type interfaceType)
            {
                this.interfaceType = interfaceType;
            }

            public void CleanUpManagedData(object ManagedObj)
            {
            }

            public void CleanUpNativeData(IntPtr pNativeData)
            {
            }

            public int GetNativeDataSize()
            {
                return 8;
            }

            public IntPtr MarshalManagedToNative(object ManagedObj)
            {
                IntPtr pRef = Marshal.GetComInterfaceForObject(ManagedObj, interfaceType);
                return pRef;
            }

            public object MarshalNativeToManaged(IntPtr pNativeData)
            {
                object o = Marshal.GetUniqueObjectForIUnknown(pNativeData);
                return o;
                //object safeRef = Activator.CreateInstance(typeof(SafeRef<>).MakeGenericType(interfaceType), o/*Convert.ChangeType(o, interfaceType.convert)*/);
                //return safeRef;
            }
        }


        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368183%28v=vs.85%29.aspx
        [ComImport, Guid("b859ee5a-d838-4b5b-a2e8-1adc7d93db48"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFactory
        {
            [return: MarshalAs(UnmanagedType.Error)]
            int GetSystemFontCollection(
                [Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique), MarshalCookie = "TextEditor.TextServiceDirectWrite.IDWriteFontCollection")] out IDWriteFontCollection fontCollection,
                [In, MarshalAs(UnmanagedType.Bool)] bool checkForUpdates);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateCustomFontCollection(
                [In] IDWriteFontCollectionLoader collectionLoader,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] collectionKey, // of length collectionKeySize
                [In] int collectionKeySize,
                out IDWriteFontCollection fontCollection);

            [return: MarshalAs(UnmanagedType.Error)]
            int RegisterFontCollectionLoader(
                [In] IDWriteFontCollectionLoader fontCollectionLoader);

            [return: MarshalAs(UnmanagedType.Error)]
            int UnregisterFontCollectionLoader(
                [In] IDWriteFontCollectionLoader fontCollectionLoader);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateFontFileReference(
                [In] string filePath,
                [In, Optional, MarshalAs(UnmanagedType.Struct)] FILETIME lastWriteTime,
                out IDWriteFontFile fontFile);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateCustomFontFileReference(
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] object[] fontFileReferenceKey, // length fontFileReferenceKeySize
                [In] int fontFileReferenceKeySize,
                [In] IDWriteFontFileLoader fontFileLoader,
                out IDWriteFontFile fontFile);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateFontFace(
                [In] DWRITE_FONT_FACE_TYPE fontFaceType,
                [In] int numberOfFiles,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IDWriteFontFile[] fontFiles, // length numberOfFiles
                [In] int faceIndex,
                [In] DWRITE_FONT_SIMULATIONS fontFaceSimulationFlags,
                out IDWriteFontFace fontFace);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateRenderingParams(
                [Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique), MarshalCookie = "TextEditor.TextServiceDirectWrite.IDWriteRenderingParams")] out IDWriteRenderingParams renderingParams);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateMonitorRenderingParams(
                [In] IntPtr monitor, // HMONITOR
                out IDWriteRenderingParams renderingParams);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateCustomRenderingParams(
                [In] float gamma,
                [In] float enhancedContrast,
                [In] float clearTypeLevel,
                [In] DWRITE_PIXEL_GEOMETRY pixelGeometry,
                [In] DWRITE_RENDERING_MODE renderingMode,
                out IDWriteRenderingParams renderingParams);

            [return: MarshalAs(UnmanagedType.Error)]
            int RegisterFontFileLoader(
                [In] IDWriteFontFileLoader fontFileLoader);

            [return: MarshalAs(UnmanagedType.Error)]
            int UnregisterFontFileLoader(
                [In] IDWriteFontFileLoader fontFileLoader);

            // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368203%28v=vs.85%29.aspx
            [return: MarshalAs(UnmanagedType.Error)]
            int CreateTextFormat(
                [In/*, MarshalAs(UnmanagedType.LPWStr)*/] string fontFamilyName,
                [In, Optional, MarshalAs(UnmanagedType.Interface)] IDWriteFontCollection fontCollection, // null == system font collection
                [In] DWRITE_FONT_WEIGHT fontWeight,
                [In] DWRITE_FONT_STYLE fontStyle,
                [In] DWRITE_FONT_STRETCH fontStretch,
                [In] float fontSize,
                [In/*, MarshalAs(UnmanagedType.LPWStr)*/] string localeName,
                [Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique), MarshalCookie = "TextEditor.TextServiceDirectWrite.IDWriteTextFormat")] out IDWriteTextFormat textFormat);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateTypography(
                out IDWriteTypography typography);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetGdiInterop(
                [Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique), MarshalCookie = "TextEditor.TextServiceDirectWrite.IDWriteGdiInterop")] out IDWriteGdiInterop gdiInterop);

            // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368205%28v=vs.85%29.aspx
            [return: MarshalAs(UnmanagedType.Error)]
            int CreateTextLayout(
                [In, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] string _string, // length stringLength
                [In] int stringLength,
                [In, MarshalAs(UnmanagedType.Interface)] IDWriteTextFormat textFormat,
                [In] float maxWidth,
                [In] float maxHeight,
                [Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique), MarshalCookie = "TextEditor.TextServiceDirectWrite.IDWriteTextLayout")] out IDWriteTextLayout textLayout);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateGdiCompatibleTextLayout(
                [In] string _string, // length stringLength
                [In] int stringLength,
                [In] IDWriteTextFormat textFormat,
                [In] float layoutWidth,
                [In] float layoutHeight,
                [In] float pixelsPerDip,
                [In, Optional, MarshalAs(UnmanagedType.Struct)] DWRITE_MATRIX transform,
                [In, MarshalAs(UnmanagedType.Bool)] bool useGdiNatural,
                [Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique), MarshalCookie = "TextEditor.TextServiceDirectWrite.IDWriteTextLayout")] out IDWriteTextLayout textLayout);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateEllipsisTrimmingSign(
                [In] IDWriteTextFormat textFormat,
                out IDWriteInlineObject trimmingSign);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateTextAnalyzer(
                out IDWriteTextAnalyzer textAnalyzer);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateNumberSubstitution(
                [In] DWRITE_NUMBER_SUBSTITUTION_METHOD substitutionMethod,
                [In] string localeName,
                [In, MarshalAs(UnmanagedType.Bool)] bool ignoreUserOverride,
                out IDWriteNumberSubstitution numberSubstitution);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateGlyphRunAnalysis(
                [In, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(DWRITE_GLYPH_RUN_marshaler))] DWRITE_GLYPH_RUN glyphRun,
                [In] float pixelsPerDip,
                [In, Optional, MarshalAs(UnmanagedType.Struct)] DWRITE_MATRIX transform,
                [In] DWRITE_RENDERING_MODE renderingMode,
                [In] DWRITE_MEASURING_MODE measuringMode,
                [In] float baselineOriginX,
                [In] float baselineOriginY,
                out IDWriteGlyphRunAnalysis glyphRunAnalysis);
        }


        [ComImport, Guid("a84cee02-3eea-4eee-a827-87c1a02a0fcc"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFontCollection
        {
            [PreserveSig]
            int GetFontFamilyCount();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontFamily(
                [In] int index,
                [Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique), MarshalCookie = "TextEditor.TextServiceDirectWrite.IDWriteFontFamily")] out IDWriteFontFamily fontFamily);

            [return: MarshalAs(UnmanagedType.Error)]
            int FindFamilyName(
                [In] string familyName,
                out int index,
                [Out, MarshalAs(UnmanagedType.Bool)] out bool exists);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontFromFontFace(
                [In] IDWriteFontFace fontFace,
                out IDWriteFont font);
        }


        [ComImport, Guid("1a0d8438-1d97-4ec1-aef9-a2fb86ed6acb"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFontList
        {
            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontCollection(
                out IDWriteFontCollection fontCollection);

            [PreserveSig]
            int GetFontCount();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFont(
                [In] int index,
                out IDWriteFont font);
        }

        [ComImport, Guid("da20d8ef-812a-4c43-9802-62ec4abd7add"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFontFamily //: IDWriteFontList
        {
            // IDWriteFontList

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontCollection(
                out IDWriteFontCollection fontCollection);

            [PreserveSig]
            int GetFontCount();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFont(
                [In] int index,
                out IDWriteFont font);

            //

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFamilyNames(
                [Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique), MarshalCookie = "TextEditor.TextServiceDirectWrite.IDWriteLocalizedStrings")] out IDWriteLocalizedStrings names);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFirstMatchingFont(
                [In] DWRITE_FONT_WEIGHT weight,
                [In] DWRITE_FONT_STRETCH stretch,
                [In] DWRITE_FONT_STYLE style,
                out IDWriteFont matchingFont);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetMatchingFonts(
                [In] DWRITE_FONT_WEIGHT weight,
                [In] DWRITE_FONT_STRETCH stretch,
                [In] DWRITE_FONT_STYLE style,
                out IDWriteFontList matchingFonts);
        }


        [ComImport, Guid("acd16696-8c14-4f5d-877e-fe3fc1d32737"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFont
        {
            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontFamily(
                out IDWriteFontFamily fontFamily);

            [PreserveSig]
            DWRITE_FONT_WEIGHT GetWeight();

            [PreserveSig]
            DWRITE_FONT_STRETCH GetStretch();

            [PreserveSig]
            DWRITE_FONT_STYLE GetStyle();

            [return: MarshalAs(UnmanagedType.Bool)]
            bool IsSymbolFont();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFaceNames(
                out IDWriteLocalizedStrings names);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetInformationalStrings(
                [In] DWRITE_INFORMATIONAL_STRING_ID informationalStringID,
                out IDWriteLocalizedStrings informationalStrings,
                [Out, MarshalAs(UnmanagedType.Bool)] out bool exists);

            [return: MarshalAs(UnmanagedType.Error)]
            DWRITE_FONT_SIMULATIONS GetSimulations();

            [PreserveSig]
            void GetMetrics(
                [Out, MarshalAs(UnmanagedType.Struct)] out DWRITE_FONT_METRICS fontMetrics);

            [return: MarshalAs(UnmanagedType.Error)]
            int HasCharacter(
                [In] int unicodeValue,
                [Out, MarshalAs(UnmanagedType.Bool)] out bool exists);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateFontFace(
                out IDWriteFontFace fontFace);
        }

        private enum DWRITE_INFORMATIONAL_STRING_ID : int
        {
            DWRITE_INFORMATIONAL_STRING_NONE,
            DWRITE_INFORMATIONAL_STRING_COPYRIGHT_NOTICE,
            DWRITE_INFORMATIONAL_STRING_VERSION_STRINGS,
            DWRITE_INFORMATIONAL_STRING_TRADEMARK,
            DWRITE_INFORMATIONAL_STRING_MANUFACTURER,
            DWRITE_INFORMATIONAL_STRING_DESIGNER,
            DWRITE_INFORMATIONAL_STRING_DESIGNER_URL,
            DWRITE_INFORMATIONAL_STRING_DESCRIPTION,
            DWRITE_INFORMATIONAL_STRING_FONT_VENDOR_URL,
            DWRITE_INFORMATIONAL_STRING_LICENSE_DESCRIPTION,
            DWRITE_INFORMATIONAL_STRING_LICENSE_INFO_URL,
            DWRITE_INFORMATIONAL_STRING_WIN32_FAMILY_NAMES,
            DWRITE_INFORMATIONAL_STRING_WIN32_SUBFAMILY_NAMES,
            DWRITE_INFORMATIONAL_STRING_PREFERRED_FAMILY_NAMES,
            DWRITE_INFORMATIONAL_STRING_PREFERRED_SUBFAMILY_NAMES,
            DWRITE_INFORMATIONAL_STRING_SAMPLE_TEXT,
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368074%28v=vs.85%29.aspx
        [StructLayout(LayoutKind.Sequential, Size = 20)]
        private struct DWRITE_FONT_METRICS
        {
            public UInt16 designUnitsPerEm;
            public UInt16 ascent;
            public UInt16 descent;
            public Int16 lineGap;
            public UInt16 capHeight;
            public UInt16 xHeight;
            public Int16 underlinePosition;
            public UInt16 underlineThickness;
            public Int16 strikethroughPosition;
            public UInt16 strikethroughThickness;
        }


        [ComImport, Guid("08256209-099a-4b34-b86d-c22b110e7771"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteLocalizedStrings
        {
            [PreserveSig]
            int GetCount();

            [return: MarshalAs(UnmanagedType.Error)]
            int FindLocaleName(
                [In] string localeName,
                out int index,
                [Out, MarshalAs(UnmanagedType.Bool)] out bool exists);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetLocaleNameLength(
                [In] int index,
                out int length);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetLocaleName(
                [In] int index,
                [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] out string localeName, // capacity is size; null terminated
                [In] int size);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetStringLength(
                [In] int index,
                out int length);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetString(
                [In] int index,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] char[] stringBuffer, // capacity is size; null terminated
                [In] int size);
        }


        [ComImport, Guid("cca920e4-52f0-492b-bfa8-29c72ee0a468"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFontCollectionLoader
        {
        }


        [ComImport, Guid("739d886a-cef5-47dc-8769-1a8b41bebbb0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFontFile
        {
        }


        [ComImport, Guid("727cad4e-d6af-4c9e-8a08-d695b11caa49"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFontFileLoader
        {
        }


        private enum DWRITE_FONT_FACE_TYPE : int
        {
            DWRITE_FONT_FACE_TYPE_CFF,
            DWRITE_FONT_FACE_TYPE_TRUETYPE,
            DWRITE_FONT_FACE_TYPE_TRUETYPE_COLLECTION,
            DWRITE_FONT_FACE_TYPE_TYPE1,
            DWRITE_FONT_FACE_TYPE_VECTOR,
            DWRITE_FONT_FACE_TYPE_BITMAP,
            DWRITE_FONT_FACE_TYPE_UNKNOWN,
        }

        private enum DWRITE_FONT_SIMULATIONS : int
        {
            DWRITE_FONT_SIMULATIONS_NONE = 0x0000,
            DWRITE_FONT_SIMULATIONS_BOLD = 0x0001,
            DWRITE_FONT_SIMULATIONS_OBLIQUE = 0x0002,
        }


        [ComImport, Guid("5f49804d-7024-4d43-bfa9-d25984f53849"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFontFace
        {
            [PreserveSig]
            DWRITE_FONT_FACE_TYPE GetType();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFiles(
                [In, Out] ref int numberOfFiles,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IDWriteFontFile[] fontFiles); // length is *numberOfFiles

            [PreserveSig]
            int GetIndex();

            [PreserveSig]
            DWRITE_FONT_SIMULATIONS GetSimulations();

            [return: MarshalAs(UnmanagedType.Bool)]
            bool IsSymbolFont();

            [PreserveSig]
            void GetMetrics(
                out DWRITE_FONT_METRICS fontFaceMetrics);

            [PreserveSig]
            UInt16 GetGlyphCount();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetDesignGlyphMetrics(
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] UInt16[] glyphIndices, // length is glyphCount
                [In] int glyphCount,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] DWRITE_GLYPH_METRICS[] glyphMetrics, // length is glyphCount
                [In, MarshalAs(UnmanagedType.Bool)] bool isSideways);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetGlyphIndices(
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] codePoints, // length is codePointCount
                [In] int codePointCount,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] UInt16[] glyphIndices); // length is codePointCount

            [return: MarshalAs(UnmanagedType.Error)]
            int TryGetFontTable(
                [In] uint openTypeTableTag,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] IntPtr[] tableData, // length is tableSize
                out int tableSize,
                out IntPtr tableContext,
                [Out, MarshalAs(UnmanagedType.Bool)] out bool exists);

            [PreserveSig]
            [return: MarshalAs(UnmanagedType.Error)]
            void ReleaseFontTable(
                [In] IntPtr tableContext);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetGlyphRunOutline(
                [In] float emSize,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] UInt16[] glyphIndices, // length is glyphCount
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] float[] glyphAdvances, // length is glyphCount
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] DWRITE_GLYPH_OFFSET[] glyphOffsets, // length is glyphCount
                [In] int glyphCount,
                [In, MarshalAs(UnmanagedType.Bool)] bool isSideways,
                [In, MarshalAs(UnmanagedType.Bool)] bool isRightToLeft,
                [In] IDWriteGeometrySink geometrySink);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetRecommendedRenderingMode(
                [In] float emSize,
                [In] float pixelsPerDip,
                [In] DWRITE_MEASURING_MODE measuringMode,
                [In] IDWriteRenderingParams renderingParams,
                [Out] out DWRITE_RENDERING_MODE renderingMode);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetGdiCompatibleMetrics(
                [In] float emSize,
                [In] float pixelsPerDip,
                [In, Optional, MarshalAs(UnmanagedType.Struct)] DWRITE_MATRIX transform,
                out DWRITE_FONT_METRICS fontFaceMetrics);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetGdiCompatibleGlyphMetrics(
                [In] float emSize,
                [In] float pixelsPerDip,
                [In, Optional, MarshalAs(UnmanagedType.Struct)] DWRITE_MATRIX transform,
                [In, MarshalAs(UnmanagedType.Bool)] bool useGdiNatural,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] UInt16[] glyphIndices, // length is glyphCount
                [In] int glyphCount,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] DWRITE_GLYPH_METRICS[] glyphMetrics, // length is glyphCount
                [In, MarshalAs(UnmanagedType.Bool)] bool isSideways);
        }

        [StructLayout(LayoutKind.Sequential, Size = 28)]
        private struct DWRITE_GLYPH_METRICS
        {
            public int leftSideBearing;
            public int advanceWidth;
            public int rightSideBearing;
            public int topSideBearing;
            public int advanceHeight;
            public int bottomSideBearing;
            public int verticalOriginY;
        }


        [ComImport, Guid("2cd9069e-12e2-11dc-9fed-001143a055f9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteGeometrySink // AKA ID2D1SimplifiedGeometrySink
        {
        }


        [ComImport, Guid("2f0da53a-2add-47cd-82ee-d9ec34688e75"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteRenderingParams
        {
            [PreserveSig]
            float GetGamma();

            [PreserveSig]
            float GetEnhancedContrast();

            [PreserveSig]
            float GetClearTypeLevel();

            [PreserveSig]
            DWRITE_PIXEL_GEOMETRY GetPixelGeometry();

            [PreserveSig]
            DWRITE_RENDERING_MODE GetRenderingMode();
        }


        private enum DWRITE_PIXEL_GEOMETRY : int
        {
            DWRITE_PIXEL_GEOMETRY_FLAT,
            DWRITE_PIXEL_GEOMETRY_RGB,
            DWRITE_PIXEL_GEOMETRY_BGR,
        }

        private enum DWRITE_RENDERING_MODE : int
        {
            DWRITE_RENDERING_MODE_DEFAULT,
            DWRITE_RENDERING_MODE_ALIASED,
            DWRITE_RENDERING_MODE_CLEARTYPE_GDI_CLASSIC,
            DWRITE_RENDERING_MODE_CLEARTYPE_GDI_NATURAL,
            DWRITE_RENDERING_MODE_CLEARTYPE_NATURAL,
            DWRITE_RENDERING_MODE_CLEARTYPE_NATURAL_SYMMETRIC,
            DWRITE_RENDERING_MODE_OUTLINE,
        }


        private enum DWRITE_FONT_WEIGHT : int
        {
            DWRITE_FONT_WEIGHT_THIN = 100,
            DWRITE_FONT_WEIGHT_EXTRA_LIGHT = 200,
            DWRITE_FONT_WEIGHT_ULTRA_LIGHT = 200,
            DWRITE_FONT_WEIGHT_LIGHT = 300,
            DWRITE_FONT_WEIGHT_NORMAL = 400,
            DWRITE_FONT_WEIGHT_REGULAR = 400,
            DWRITE_FONT_WEIGHT_MEDIUM = 500,
            DWRITE_FONT_WEIGHT_DEMI_BOLD = 600,
            DWRITE_FONT_WEIGHT_SEMI_BOLD = 600,
            DWRITE_FONT_WEIGHT_BOLD = 700,
            DWRITE_FONT_WEIGHT_EXTRA_BOLD = 800,
            DWRITE_FONT_WEIGHT_ULTRA_BOLD = 800,
            DWRITE_FONT_WEIGHT_BLACK = 900,
            DWRITE_FONT_WEIGHT_HEAVY = 900,
            DWRITE_FONT_WEIGHT_EXTRA_BLACK = 950,
            DWRITE_FONT_WEIGHT_ULTRA_BLACK = 950,
        }

        private enum DWRITE_FONT_STYLE : int
        {
            DWRITE_FONT_STYLE_NORMAL,
            DWRITE_FONT_STYLE_OBLIQUE,
            DWRITE_FONT_STYLE_ITALIC,

        }

        private enum DWRITE_FONT_STRETCH : int
        {
            DWRITE_FONT_STRETCH_UNDEFINED = 0,
            DWRITE_FONT_STRETCH_ULTRA_CONDENSED = 1,
            DWRITE_FONT_STRETCH_EXTRA_CONDENSED = 2,
            DWRITE_FONT_STRETCH_CONDENSED = 3,
            DWRITE_FONT_STRETCH_SEMI_CONDENSED = 4,
            DWRITE_FONT_STRETCH_NORMAL = 5,
            DWRITE_FONT_STRETCH_MEDIUM = 5,
            DWRITE_FONT_STRETCH_SEMI_EXPANDED = 6,
            DWRITE_FONT_STRETCH_EXPANDED = 7,
            DWRITE_FONT_STRETCH_EXTRA_EXPANDED = 8,
            DWRITE_FONT_STRETCH_ULTRA_EXPANDED = 9,
        }


        [ComImport, Guid("9c906818-31d7-4fd3-a151-7c5e225db55a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteTextFormat
        {
            [return: MarshalAs(UnmanagedType.Error)]
            int SetTextAlignment(
                [In] DWRITE_TEXT_ALIGNMENT textAlignment);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetParagraphAlignment(
                [In] DWRITE_PARAGRAPH_ALIGNMENT paragraphAlignment);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetWordWrapping(
                [In] DWRITE_WORD_WRAPPING wordWrapping);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetReadingDirection(
                [In] DWRITE_READING_DIRECTION readingDirection);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetFlowDirection(
                [In] DWRITE_FLOW_DIRECTION flowDirection);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetIncrementalTabStop(
                [In] float incrementalTabStop);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetTrimming(
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TRIMMING trimmingOptions,
                [In] IDWriteInlineObject trimmingSign);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetLineSpacing(
                [In] DWRITE_LINE_SPACING_METHOD lineSpacingMethod,
                [In] float lineSpacing,
                [In] float baseline);

            [PreserveSig]
            DWRITE_TEXT_ALIGNMENT GetTextAlignment();

            [PreserveSig]
            DWRITE_PARAGRAPH_ALIGNMENT GetParagraphAlignment();

            [PreserveSig]
            DWRITE_WORD_WRAPPING GetWordWrapping();

            [PreserveSig]
            DWRITE_READING_DIRECTION GetReadingDirection();

            [PreserveSig]
            DWRITE_FLOW_DIRECTION GetFlowDirection();

            [PreserveSig]
            float GetIncrementalTabStop();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetTrimming(
                [Out, MarshalAs(UnmanagedType.Struct)] out DWRITE_TRIMMING trimmingOptions,
                out IDWriteInlineObject trimmingSign);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetLineSpacing(
                [Out] out DWRITE_LINE_SPACING_METHOD lineSpacingMethod,
                out float lineSpacing,
                out float baseline);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontCollection(
                out IDWriteFontCollection fontCollection);

            [PreserveSig]
            int GetFontFamilyNameLength();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontFamilyName(
                [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] out string fontFamilyName, // length is nameSize plus null terminator
                [In] int nameSize);

            [PreserveSig]
            DWRITE_FONT_WEIGHT GetFontWeight();

            [PreserveSig]
            DWRITE_FONT_STYLE GetFontStyle();

            [PreserveSig]
            DWRITE_FONT_STRETCH GetFontStretch();

            [PreserveSig]
            float GetFontSize();

            [PreserveSig]
            int GetLocaleNameLength();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetLocaleName(
                [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] out string localeName, // length is nameSize plus null terminator
                [In] int nameSize);
        }

        private enum DWRITE_TEXT_ALIGNMENT : int
        {
            DWRITE_TEXT_ALIGNMENT_LEADING,
            DWRITE_TEXT_ALIGNMENT_TRAILING,
            DWRITE_TEXT_ALIGNMENT_CENTER,
        }

        private enum DWRITE_PARAGRAPH_ALIGNMENT : int
        {
            DWRITE_PARAGRAPH_ALIGNMENT_NEAR,
            DWRITE_PARAGRAPH_ALIGNMENT_FAR,
            DWRITE_PARAGRAPH_ALIGNMENT_CENTER,
        }

        private enum DWRITE_WORD_WRAPPING : int
        {
            DWRITE_WORD_WRAPPING_WRAP,
            DWRITE_WORD_WRAPPING_NO_WRAP,
        }

        private enum DWRITE_READING_DIRECTION : int
        {
            DWRITE_READING_DIRECTION_LEFT_TO_RIGHT,
            DWRITE_READING_DIRECTION_RIGHT_TO_LEFT,
        }

        private enum DWRITE_FLOW_DIRECTION : int
        {
            DWRITE_FLOW_DIRECTION_TOP_TO_BOTTOM,
        }

        private enum DWRITE_LINE_SPACING_METHOD : int
        {
            DWRITE_LINE_SPACING_METHOD_DEFAULT,
            DWRITE_LINE_SPACING_METHOD_UNIFORM,
        }

        private enum DWRITE_TRIMMING_GRANULARITY : int
        {
            DWRITE_TRIMMING_GRANULARITY_NONE,
            DWRITE_TRIMMING_GRANULARITY_CHARACTER,
            DWRITE_TRIMMING_GRANULARITY_WORD,
        }

        [StructLayout(LayoutKind.Sequential, Size = 12)]
        private struct DWRITE_TRIMMING
        {
            public DWRITE_TRIMMING_GRANULARITY granularity;
            public int delimiter;
            public int delimiterCount;
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd316718%28v=vs.85%29.aspx
        [ComImport, Guid("53737037-6d14-410b-9bfe-0b182bb70961"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteTextLayout // : IDWriteTextFormat
        {
            // IDWriteTextFormat

            [return: MarshalAs(UnmanagedType.Error)]
            int SetTextAlignment(
                [In] DWRITE_TEXT_ALIGNMENT textAlignment);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetParagraphAlignment(
                [In] DWRITE_PARAGRAPH_ALIGNMENT paragraphAlignment);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetWordWrapping(
                [In] DWRITE_WORD_WRAPPING wordWrapping);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetReadingDirection(
                [In] DWRITE_READING_DIRECTION readingDirection);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetFlowDirection(
                [In] DWRITE_FLOW_DIRECTION flowDirection);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetIncrementalTabStop(
                [In] float incrementalTabStop);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetTrimming(
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TRIMMING trimmingOptions,
                [In] IDWriteInlineObject trimmingSign);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetLineSpacing(
                [In] DWRITE_LINE_SPACING_METHOD lineSpacingMethod,
                [In] float lineSpacing,
                [In] float baseline);

            [PreserveSig]
            DWRITE_TEXT_ALIGNMENT GetTextAlignment();

            [PreserveSig]
            DWRITE_PARAGRAPH_ALIGNMENT GetParagraphAlignment();

            [PreserveSig]
            DWRITE_WORD_WRAPPING GetWordWrapping();

            [PreserveSig]
            DWRITE_READING_DIRECTION GetReadingDirection();

            [PreserveSig]
            DWRITE_FLOW_DIRECTION GetFlowDirection();

            [PreserveSig]
            float GetIncrementalTabStop();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetTrimming(
                [Out, MarshalAs(UnmanagedType.Struct)] out DWRITE_TRIMMING trimmingOptions,
                out IDWriteInlineObject trimmingSign);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetLineSpacing(
                [Out] out DWRITE_LINE_SPACING_METHOD lineSpacingMethod,
                out float lineSpacing,
                out float baseline);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontCollection(
                out IDWriteFontCollection fontCollection);

            [PreserveSig]
            int GetFontFamilyNameLength();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontFamilyName(
                [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] out string fontFamilyName, // length is nameSize plus null terminator
                [In] int nameSize);

            [PreserveSig]
            DWRITE_FONT_WEIGHT GetFontWeight();

            [PreserveSig]
            DWRITE_FONT_STYLE GetFontStyle();

            [PreserveSig]
            DWRITE_FONT_STRETCH GetFontStretch();

            [PreserveSig]
            float GetFontSize();

            [PreserveSig]
            int GetLocaleNameLength();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetLocaleName(
                [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] out string localeName, // length is nameSize plus null terminator
                [In] int nameSize);

            //

            [return: MarshalAs(UnmanagedType.Error)]
            int SetMaxWidth(
                [In] float maxWidth);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetMaxHeight(
                [In] float maxHeight);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetFontCollection(
                [In] IDWriteFontCollection fontCollection,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetFontFamilyName(
                [In] string fontFamilyName,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetFontWeight(
                [In] DWRITE_FONT_WEIGHT fontWeight,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetFontStyle(
                [In] DWRITE_FONT_STYLE fontStyle,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetFontStretch(
                [In] DWRITE_FONT_STRETCH fontStretch,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetFontSize(
                [In] float fontSize,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetUnderline(
                [In, MarshalAs(UnmanagedType.Bool)] bool hasUnderline,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetStrikethrough(
                [In, MarshalAs(UnmanagedType.Bool)] bool hasStrikethrough,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetDrawingEffect(
                [In, MarshalAs(UnmanagedType.IUnknown)] object drawingEffect,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetInlineObject(
                [In] IDWriteInlineObject inlineObject,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetTypography(
                [In] IDWriteTypography typography,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetLocaleName(
                [In] string localeName,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_TEXT_RANGE textRange);

            [PreserveSig]
            float GetMaxWidth();

            [PreserveSig]
            float GetMaxHeight();

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontCollection(
                [In] int currentPosition,
                out IDWriteFontCollection fontCollection,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontFamilyNameLength(
                [In] int currentPosition,
                out int nameLength,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontFamilyName(
                [In] int currentPosition,
                [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)]out string fontFamilyName, // null-terminated, of length nameSize
                [In] int nameSize,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontWeight(
                [In] int currentPosition,
                [Out] out DWRITE_FONT_WEIGHT fontWeight,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontStyle(
                [In] int currentPosition,
                [Out] out DWRITE_FONT_STYLE fontStyle,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontStretch(
                [In] int currentPosition,
                [Out] out DWRITE_FONT_STRETCH fontStretch,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetFontSize(
                [In] int currentPosition,
                out float fontSize,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetUnderline(
                [In] int currentPosition,
                [Out, MarshalAs(UnmanagedType.Bool)] out bool hasUnderline,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetStrikethrough(
                [In] int currentPosition,
                [Out, MarshalAs(UnmanagedType.Bool)] out bool hasStrikethrough,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetDrawingEffect(
                [In] int currentPosition,
                [Out, MarshalAs(UnmanagedType.IUnknown)] out object drawingEffect,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetInlineObject(
                [In] int currentPosition,
                out IDWriteInlineObject inlineObject,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetTypography(
                [In] int currentPosition,
                out IDWriteTypography typography,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetLocaleNameLength(
                [In] int currentPosition,
                out int nameLength,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetLocaleName(
                [In] int currentPosition,
                [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)]out string localeName, // null-terminated, of length nameSize
                [In] int nameSize,
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_RANGE textRange);

            [return: MarshalAs(UnmanagedType.Error)]
            int Draw( // finally, the payoff!
                [In, Optional] IntPtr clientDrawingContext,
                // [In, MarshalAs(UnmanagedType.Interface)] IDWriteTextRenderer renderer,
                [In, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique), MarshalCookie = "TextEditor.TextServiceDirectWrite.IDWriteTextRenderer")] IDWriteTextRenderer renderer,
                [In] float originX,
                [In] float originY);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetLineMetrics(
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] DWRITE_LINE_METRICS[] lineMetrics, // capacity is maxLineCount
                [In]int maxLineCount,
                out int actualLineCount);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetMetrics(
                [Out, MarshalAs(UnmanagedType.Struct)] out DWRITE_TEXT_METRICS textMetrics);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetOverhangMetrics(
                [Out, MarshalAs(UnmanagedType.Struct)] out DWRITE_OVERHANG_METRICS overhangs);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetClusterMetrics(
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] DWRITE_CLUSTER_METRICS[] clusterMetrics, // capacity is maxClusterCount
                [In]int maxClusterCount,
                out int actualClusterCount);

            [return: MarshalAs(UnmanagedType.Error)]
            int DetermineMinWidth(
                out float minWidth);

            [return: MarshalAs(UnmanagedType.Error)]
            int HitTestPoint(
                [In] float pointX,
                [In] float pointY,
                [Out, MarshalAs(UnmanagedType.Bool)] out bool isTrailingHit,
                [Out, MarshalAs(UnmanagedType.Bool)] out bool isInside,
                [Out, MarshalAs(UnmanagedType.Struct)] out DWRITE_HIT_TEST_METRICS hitTestMetrics);

            [return: MarshalAs(UnmanagedType.Error)]
            int HitTestTextPosition(
                [In] int textPosition,
                [In, MarshalAs(UnmanagedType.Bool)] bool isTrailingHit,
                out float pointX,
                out float pointY,
                [Out, MarshalAs(UnmanagedType.Struct)] out DWRITE_HIT_TEST_METRICS hitTestMetrics);

            [return: MarshalAs(UnmanagedType.Error)]
            int HitTestTextRange(
                [In] int textPosition,
                [In] int textLength,
                [In] float originX,
                [In] float originY,
                [Out, Optional, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 6)] DWRITE_HIT_TEST_METRICS[] hitTestMetrics, // capacity is maxHitTestMetricsCount
                [In] int maxHitTestMetricsCount,
                out int actualHitTestMetricsCount);
        }

        [StructLayout(LayoutKind.Sequential, Size = 8)]
        private struct DWRITE_TEXT_RANGE
        {
            public int startPosition;
            public int length;

            public DWRITE_TEXT_RANGE(
                int startPosition,
                int length)
            {
                this.startPosition = startPosition;
                this.length = length;
            }
        }

        [StructLayout(LayoutKind.Sequential, Size = 24)]
        private struct DWRITE_LINE_METRICS
        {
            public int length;
            public int trailingWhitespaceLength;
            public int newlineLength;
            public float height;
            public float baseline;
            private int i_isTrimmed; public bool isTrimmed { get { return i_isTrimmed != 0; } set { i_isTrimmed = value ? 1 : 0; } }
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368135%28v=vs.85%29.aspx
        [StructLayout(LayoutKind.Sequential, Size = 36)]
        private struct DWRITE_TEXT_METRICS
        {
            public float left;
            public float top;
            public float width;
            public float widthIncludingTrailingWhitespace;
            public float height;
            public float layoutWidth;
            public float layoutHeight;
            public int maxBidiReorderingDepth;
            public int lineCount;
        }

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        private struct DWRITE_OVERHANG_METRICS
        {
            public float left;
            public float top;
            public float right;
            public float bottom;
        }

        [StructLayout(LayoutKind.Sequential, Size = 8)]
        private struct DWRITE_CLUSTER_METRICS
        {
            public float width;
            public UInt16 length;
            private MASK mask;

            public bool canWrapLineAfter { get { return Get(mask, MASK.canWrapLineAfter); } set { Set(value, ref mask, MASK.canWrapLineAfter); } }
            public bool isWhitespace { get { return Get(mask, MASK.isWhitespace); } set { Set(value, ref mask, MASK.isWhitespace); } }
            public bool isNewline { get { return Get(mask, MASK.isNewline); } set { Set(value, ref mask, MASK.isNewline); } }
            public bool isSoftHyphen { get { return Get(mask, MASK.isSoftHyphen); } set { Set(value, ref mask, MASK.isSoftHyphen); } }
            public bool isRightToLeft { get { return Get(mask, MASK.isRightToLeft); } set { Set(value, ref mask, MASK.isRightToLeft); } }

            [Flags]
            private enum MASK : ushort
            {
                canWrapLineAfter = 1 << 0,
                isWhitespace = 1 << 1,
                isNewline = 1 << 2,
                isSoftHyphen = 1 << 3,
                isRightToLeft = 1 << 4,
            }

            private static bool Get(MASK flags, MASK mask)
            {
                return (flags & mask) != 0;
            }

            private static void Set(bool f, ref MASK flags, MASK mask)
            {
                flags = f ? (flags | mask) : (flags & ~mask);
            }
        }

        [StructLayout(LayoutKind.Sequential, Size = 36)]
        private struct DWRITE_HIT_TEST_METRICS
        {
            public int textPosition;
            public int length;
            public float left;
            public float top;
            public float width;
            public float height;
            public int bidiLevel;
            private int i_isText; public bool isText { get { return i_isText != 0; } set { i_isText = value ? 1 : 0; } }
            private int i_isTrimmed; public bool isTrimmed { get { return i_isTrimmed != 0; } set { i_isTrimmed = value ? 1 : 0; } }
        }


        [ComImport, Guid("55f1112b-1dc2-4b3c-9541-f46894ed85b6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteTypography
        {
        }


        [ComImport, Guid("1edd9491-9853-4299-898f-6432983b6f3a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteGdiInterop
        {
            [return: MarshalAs(UnmanagedType.Error)]
            int CreateFontFromLOGFONT(
                [In, MarshalAs(UnmanagedType.Struct)] LOGFONT logFont,
                [Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique), MarshalCookie = "TextEditor.TextServiceDirectWrite.IDWriteFont")] out IDWriteFont font);

            [return: MarshalAs(UnmanagedType.Error)]
            int ConvertFontToLOGFONT(
                [In] IDWriteFont font,
                [Out] out LOGFONT logFont,
                [Out, MarshalAs(UnmanagedType.Bool)] out bool isSystemFont);

            [return: MarshalAs(UnmanagedType.Error)]
            int ConvertFontFaceToLOGFONT(
                [In] IDWriteFontFace font,
                [Out] out LOGFONT logFont);

            [return: MarshalAs(UnmanagedType.Error)]
            int CreateFontFaceFromHdc(
                [In] IntPtr hdc, // HDC
                out IDWriteFontFace fontFace);

            // https://msdn.microsoft.com/en-us/library/windows/desktop/dd371182%28v=vs.85%29.aspx
            [return: MarshalAs(UnmanagedType.Error)]
            int CreateBitmapRenderTarget(
                [In, Optional] IntPtr hdc, // HDC 
                [In] int width,
                [In] int height,
                [Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique), MarshalCookie = "TextEditor.TextServiceDirectWrite.IDWriteBitmapRenderTarget")] out IDWriteBitmapRenderTarget renderTarget);
        }


        [ComImport, Guid("5e5a32a3-8dff-4773-9ff6-0696eab77267"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteBitmapRenderTarget
        {
            [return: MarshalAs(UnmanagedType.Error)]
            int DrawGlyphRun(
                [In] float baselineOriginX,
                [In] float baselineOriginY,
                [In] DWRITE_MEASURING_MODE measuringMode,
                [In, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(DWRITE_GLYPH_RUN_marshaler))] DWRITE_GLYPH_RUN glyphRun,
                [In] IDWriteRenderingParams renderingParams,
                [In] uint textColor, // COLORREF 
                [Out, Optional, MarshalAs(UnmanagedType.Struct)] out Rectangle blackBoxRect);

            [PreserveSig]
            IntPtr GetMemoryDC(); // returns HDC

            [PreserveSig]
            float GetPixelsPerDip();

            [return: MarshalAs(UnmanagedType.Error)]
            int SetPixelsPerDip(
                float pixelsPerDip);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetCurrentTransform(
                [Out, MarshalAs(UnmanagedType.Struct)] out DWRITE_MATRIX transform);

            [return: MarshalAs(UnmanagedType.Error)]
            int SetCurrentTransform(
                [In, Optional, MarshalAs(UnmanagedType.Struct)] DWRITE_MATRIX transform);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetSize(
                out Size size);

            [return: MarshalAs(UnmanagedType.Error)]
            int Resize(
                [In] int width,
                [In] int height);
        }


        [ComImport, Guid("ef8a8135-5cc6-45fe-8825-c5a0724eb819"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteTextRenderer //: IDWritePixelSnapping
        {
            // IDWritePixelSnapping

            [return: MarshalAs(UnmanagedType.Error)]
            int IsPixelSnappingDisabled(
                [In, Optional] IntPtr clientDrawingContext
                /*[Out, MarshalAs(UnmanagedType.Bool)] out bool isDisabled*/);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetCurrentTransform(
                [In, Optional] IntPtr clientDrawingContext,
                [Out, MarshalAs(UnmanagedType.Struct)] out DWRITE_MATRIX transform);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetPixelsPerDip(
                [In, Optional] IntPtr clientDrawingContext,
                out float pixelsPerDip);

            //

            [return: MarshalAs(UnmanagedType.Error)]
            int DrawGlyphRun(
                [In, Optional] IntPtr clientDrawingContext,
                [In] float baselineOriginX,
                [In] float baselineOriginY,
                [In] DWRITE_MEASURING_MODE measuringMode,
                [In, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(DWRITE_GLYPH_RUN_marshaler))] DWRITE_GLYPH_RUN glyphRun,
                [In, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(DWRITE_GLYPH_RUN_DESCRIPTION_marshaler))] DWRITE_GLYPH_RUN_DESCRIPTION glyphRunDescription,
                [In, Optional, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect);

            [return: MarshalAs(UnmanagedType.Error)]
            int DrawUnderline(
                [In, Optional] IntPtr clientDrawingContext,
                [In] float baselineOriginX,
                [In] float baselineOriginY,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_UNDERLINE underline,
                [In, Optional, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect);

            [return: MarshalAs(UnmanagedType.Error)]
            int DrawStrikethrough(
                [In, Optional] IntPtr clientDrawingContext,
                [In] float baselineOriginX,
                [In] float baselineOriginY,
                [In, MarshalAs(UnmanagedType.Struct)] DWRITE_STRIKETHROUGH strikethrough,
                [In, Optional, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect);

            [return: MarshalAs(UnmanagedType.Error)]
            int DrawInlineObject(
                [In, Optional] IntPtr clientDrawingContext,
                [In] float originX,
                [In] float originY,
                [In] IDWriteInlineObject inlineObject,
                [In, MarshalAs(UnmanagedType.Bool)] bool isSideways,
                [In, MarshalAs(UnmanagedType.Bool)] bool isRightToLeft,
                [In, Optional, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect);
        }


        [ComImport, Guid("eaf3a2da-ecf4-4d24-b644-b34f6842024b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWritePixelSnapping
        {
            [return: MarshalAs(UnmanagedType.Error)]
            int IsPixelSnappingDisabled(
                [In, Optional] IntPtr clientDrawingContext
                /*[Out, MarshalAs(UnmanagedType.Bool)] out bool isDisabled*/);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetCurrentTransform(
                [In, Optional] IntPtr clientDrawingContext,
                [Out, MarshalAs(UnmanagedType.Struct)] out DWRITE_MATRIX transform);

            [return: MarshalAs(UnmanagedType.Error)]
            int GetPixelsPerDip(
                [In, Optional] IntPtr clientDrawingContext,
                out float pixelsPerDip);
        }

        [StructLayout(LayoutKind.Sequential, Size = 24)]
        private struct DWRITE_MATRIX
        {
            public float m11;
            public float m12;
            public float m21;
            public float m22;
            public float dx;
            public float dy;

            public DWRITE_MATRIX(float m11, float m12, float m21, float m22, float dx, float dy)
            {
                this.m11 = m11;
                this.m12 = m12;
                this.m21 = m21;
                this.m22 = m22;
                this.dx = dx;
                this.dy = dy;
            }

            public static readonly DWRITE_MATRIX Identity = new DWRITE_MATRIX(1, 0, 0, 1, 0, 0);
        }

        [StructLayout(LayoutKind.Explicit, Size = 40)]
        private struct DWRITE_UNDERLINE // TODO: might need custom marshaling
        {
            [FieldOffset(0)]
            public float width;
            [FieldOffset(4)]
            public float thickness;
            [FieldOffset(8)]
            public float offset;
            [FieldOffset(12)]
            public float runHeight;
            [FieldOffset(16)]
            public DWRITE_READING_DIRECTION readingDirection;
            [FieldOffset(20)]
            public DWRITE_FLOW_DIRECTION flowDirection;
            [FieldOffset(24)]
            public string localeName; // null terminated
            [FieldOffset(32)]
            public DWRITE_MEASURING_MODE measuringMode;
        }

        [StructLayout(LayoutKind.Explicit, Size = 40)]
        private struct DWRITE_STRIKETHROUGH // TODO: might need custom marshaling
        {
            [FieldOffset(0)]
            public float width;
            [FieldOffset(4)]
            public float thickness;
            [FieldOffset(8)]
            public float offset;
            [FieldOffset(12)]
            public DWRITE_READING_DIRECTION readingDirection;
            [FieldOffset(16)]
            public DWRITE_FLOW_DIRECTION flowDirection;
            [FieldOffset(24)]
            public string localeName; // null-terminated
            [FieldOffset(32)]
            public DWRITE_MEASURING_MODE measuringMode;
        }


        [ComImport, Guid("8339FDE3-106F-47ab-8373-1C6295EB10B3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteInlineObject
        {
        }


        [ComImport, Guid("b7e6163e-7f46-43b4-84b3-e4e6249c365d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteTextAnalyzer
        {
        }


        private enum DWRITE_NUMBER_SUBSTITUTION_METHOD : int
        {
            DWRITE_NUMBER_SUBSTITUTION_METHOD_FROM_CULTURE,
            DWRITE_NUMBER_SUBSTITUTION_METHOD_CONTEXTUAL,
            DWRITE_NUMBER_SUBSTITUTION_METHOD_NONE,
            DWRITE_NUMBER_SUBSTITUTION_METHOD_NATIONAL,
            DWRITE_NUMBER_SUBSTITUTION_METHOD_TRADITIONAL,
        }


        [ComImport, Guid("14885CC9-BAB0-4f90-B6ED-5C366A2CD03D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteNumberSubstitution
        {
            // no methods
        }


        //[StructLayout(LayoutKind.Sequential, Size = 48)]
        //private struct DWRITE_GLYPH_RUN_unmanaged
        //{
        //    public IDWriteFontFace fontFace;
        //    public float fontEmSize;
        //    public int glyphCount;
        //    public UInt16[] glyphIndices; // of length glyphCount
        //    public float[] glyphAdvances; // of length glyphCount, optional
        //    public DWRITE_GLYPH_OFFSET[] glyphOffsets; // of length glyphCount, optional
        //    private int i_isSideways; public bool isSideways { get { return i_isSideways != 0; } set { i_isSideways = value ? 1 : 0; } }
        //    public int bidiLevel;
        //}

        private class DWRITE_GLYPH_RUN
        {
            public IDWriteFontFace fontFace;
            public float fontEmSize;
            public int glyphCount;
            public UInt16[] glyphIndices; // of length glyphCount
            public float[] glyphAdvances; // of length glyphCount, optional
            public DWRITE_GLYPH_OFFSET[] glyphOffsets; // of length glyphCount, optional
            public bool isSideways;
            public int bidiLevel;
        }

        public class DWRITE_GLYPH_RUN_marshaler : ICustomMarshaler
        {
            private static List<IntPtr> natives = new List<IntPtr>();

            public static ICustomMarshaler GetInstance(string pstrCookie)
            {
                return new DWRITE_GLYPH_RUN_marshaler();
            }

            public void CleanUpManagedData(object managedObj)
            {
            }

            public void CleanUpNativeData(IntPtr pNativeData)
            {
                IntPtr top = pNativeData;

                lock (natives)
                {
                    int index = natives.IndexOf(top);
                    if (index < 0)
                    {
                        // asked to clean up one we didn't create
                        Debugger.Break();
                        throw new InvalidOperationException();
                    }
                    natives.RemoveAt(index);
                }

                IntPtr pFontFace = Marshal.ReadIntPtr(top, 0);
                IntPtr rgGlyphIndices = Marshal.ReadIntPtr(top, 16);
                IntPtr rgGlyphAdvances = Marshal.ReadIntPtr(top, 24);
                IntPtr rgGlyphOffsets = Marshal.ReadIntPtr(top, 32);

                //Marshal.Release(pFontFace);
                object oFontFace = Marshal.GetObjectForIUnknown(pFontFace);
                while (Marshal.ReleaseComObject(oFontFace) != 0) ;

                Marshal.FreeHGlobal(rgGlyphIndices);
                if (rgGlyphAdvances != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(rgGlyphAdvances);
                }
                if (rgGlyphOffsets != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(rgGlyphOffsets);
                }

                Marshal.FreeHGlobal(top);
            }

            public int GetNativeDataSize()
            {
                return 48;
            }

            public IntPtr MarshalManagedToNative(object managedObj)
            {
                try
                {
                    DWRITE_GLYPH_RUN managed = (DWRITE_GLYPH_RUN)managedObj;

                    IntPtr pFontFace = Marshal.GetComInterfaceForObject(managed.fontFace, typeof(IDWriteFontFace));

                    IntPtr rgGlyphIndices = Marshal.AllocHGlobal(managed.glyphCount * 2);
                    Marshal2.Copy(managed.glyphIndices, 0, rgGlyphIndices, managed.glyphCount);

                    IntPtr rgGlyphAdvances = IntPtr.Zero;
                    if (managed.glyphAdvances != null)
                    {
                        rgGlyphAdvances = Marshal.AllocHGlobal(managed.glyphCount * 4);
                        Marshal.Copy(managed.glyphAdvances, 0, rgGlyphAdvances, managed.glyphCount);
                    }

                    IntPtr rgGlyphOffsets = IntPtr.Zero;
                    if (managed.glyphOffsets != null)
                    {
                        int bytes = managed.glyphCount * Marshal.SizeOf(typeof(DWRITE_GLYPH_OFFSET));
                        rgGlyphOffsets = Marshal.AllocHGlobal(bytes);
                        Marshal2.CopyArrayOfStruct(managed.glyphOffsets, 0, rgGlyphOffsets, bytes, managed.glyphCount);
                    }


                    IntPtr top = Marshal.AllocHGlobal(48);

                    Marshal.WriteIntPtr(top, 0, pFontFace);
                    Marshal2.WriteFloat32(top, 8, managed.fontEmSize);
                    Marshal.WriteInt32(top, 12, managed.glyphCount);
                    Marshal.WriteIntPtr(top, 16, rgGlyphIndices);
                    Marshal.WriteIntPtr(top, 24, rgGlyphAdvances);
                    Marshal.WriteIntPtr(top, 32, rgGlyphOffsets);
                    Marshal.WriteInt32(top, 40, managed.isSideways ? 1 : 0);
                    Marshal.WriteInt32(top, 44, managed.bidiLevel);

                    lock (natives)
                    {
                        natives.Add(top);
                    }

                    return top;
                }
                catch (Exception e)
                {
                    Debugger.Break();
                    throw;
                }
            }

            public object MarshalNativeToManaged(IntPtr pNativeData)
            {
                try
                {
                    DWRITE_GLYPH_RUN managed = new DWRITE_GLYPH_RUN();

                    IntPtr top = pNativeData;

                    IntPtr pFontFace = Marshal.ReadIntPtr(top, 0);
                    //managed.fontFace = (IDWriteFontFace)Marshal.GetObjectForIUnknown(pFontFace);
                    managed.fontFace = (IDWriteFontFace)Marshal.GetUniqueObjectForIUnknown(pFontFace);

                    managed.fontEmSize = Marshal2.ReadFloat32(top, 8);

                    managed.glyphCount = Marshal.ReadInt32(top, 12);

                    IntPtr rgGlyphIndices = Marshal.ReadIntPtr(top, 16);
                    managed.glyphIndices = new ushort[managed.glyphCount];
                    Marshal2.Copy(rgGlyphIndices, managed.glyphIndices, 0, managed.glyphCount);

                    IntPtr rgGlyphAdvances = Marshal.ReadIntPtr(top, 24);
                    if (rgGlyphAdvances != IntPtr.Zero)
                    {
                        managed.glyphAdvances = new float[managed.glyphCount];
                        Marshal.Copy(rgGlyphAdvances, managed.glyphAdvances, 0, managed.glyphCount);
                    }

                    IntPtr rgGlyphOffsets = Marshal.ReadIntPtr(top, 32);
                    if (rgGlyphOffsets != IntPtr.Zero)
                    {
                        managed.glyphOffsets = new DWRITE_GLYPH_OFFSET[managed.glyphCount];
                        Marshal2.CopyArrayOfStruct(rgGlyphOffsets, managed.glyphOffsets, 0, managed.glyphCount);
                    }

                    managed.isSideways = Marshal.ReadInt32(top, 40) != 0;

                    managed.bidiLevel = Marshal.ReadInt32(top, 44);

                    return managed;
                }
                catch (Exception e)
                {
                    Debugger.Break();
                    throw;
                }
            }
        }

        //[StructLayout(LayoutKind.Explicit, Size = 40)]
        //private struct DWRITE_GLYPH_RUN_DESCRIPTION_unmanaged
        //{
        //    [FieldOffset(0)]
        //    public char[] localeName; // null-terminated
        //    [FieldOffset(8)]
        //    public char[] _string; // of length stringLength
        //    [FieldOffset(16)]
        //    public int stringLength;
        //    [FieldOffset(24)]
        //    public UInt16[] clusterMap; // of length stringLength
        //    [FieldOffset(32)]
        //    public int textPosition;
        //}

        private class DWRITE_GLYPH_RUN_DESCRIPTION
        {
            public string localeName;
            public string _string; // of length stringLength
            public int stringLength;
            public UInt16[] clusterMap; // of length stringLength
            public int textPosition;
        }

        public class DWRITE_GLYPH_RUN_DESCRIPTION_marshaler : ICustomMarshaler
        {
            public static ICustomMarshaler GetInstance(string pstrCookie)
            {
                return new DWRITE_GLYPH_RUN_DESCRIPTION_marshaler();
            }

            public void CleanUpManagedData(object managedObj)
            {
            }

            public void CleanUpNativeData(IntPtr pNativeData)
            {
                IntPtr top = pNativeData;

                IntPtr wzLocaleName = Marshal.ReadIntPtr(top, 0);
                IntPtr wzString = Marshal.ReadIntPtr(top, 8);
                IntPtr rgClusterMap = Marshal.ReadIntPtr(top, 24);

                Marshal.FreeHGlobal(rgClusterMap);
                Marshal.FreeHGlobal(wzString);
                Marshal.FreeHGlobal(wzLocaleName);

                Marshal.FreeHGlobal(top);
            }

            public int GetNativeDataSize()
            {
                return 40;
            }

            public IntPtr MarshalManagedToNative(object managedObj)
            {
                try
                {
                    DWRITE_GLYPH_RUN_DESCRIPTION managed = (DWRITE_GLYPH_RUN_DESCRIPTION)managedObj;

                    if ((managed.stringLength != managed._string.Length)
                        || (managed.stringLength != managed.clusterMap.Length))
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }

                    IntPtr wzLocaleName = Marshal.StringToCoTaskMemUni(managed.localeName);
                    IntPtr wzString = Marshal.StringToCoTaskMemUni(managed._string);

                    IntPtr rgClusterMap = Marshal.AllocHGlobal(managed.stringLength * 2);
                    Marshal2.Copy(managed.clusterMap, 0, rgClusterMap, managed.stringLength);


                    IntPtr top = Marshal.AllocHGlobal(40);

                    Marshal.WriteIntPtr(top, 0, wzLocaleName);
                    Marshal.WriteIntPtr(top, 8, wzString);
                    Marshal.WriteInt32(top, 16, managed.stringLength);
                    Marshal.WriteInt32(top, 20, 0); // alignment padding
                    Marshal.WriteIntPtr(top, 24, rgClusterMap);
                    Marshal.WriteInt32(top, 32, managed.textPosition);
                    Marshal.WriteInt32(top, 36, 0); // alignment padding

                    return top;
                }
                catch (Exception e)
                {
                    Debugger.Break();
                    throw;
                }
            }

            public object MarshalNativeToManaged(IntPtr pNativeData)
            {
                try
                {
                    DWRITE_GLYPH_RUN_DESCRIPTION managed = new DWRITE_GLYPH_RUN_DESCRIPTION();

                    IntPtr top = pNativeData;

                    IntPtr wzLocaleName = Marshal.ReadIntPtr(top, 0);
                    managed.localeName = Marshal.PtrToStringUni(wzLocaleName);

                    managed.stringLength = Marshal.ReadInt32(top, 16);

                    IntPtr wzString = Marshal.ReadIntPtr(top, 8);
                    managed._string = Marshal.PtrToStringUni(wzString, managed.stringLength);

                    IntPtr rgClusterMap = Marshal.ReadIntPtr(top, 24);
                    managed.clusterMap = new ushort[managed.stringLength];
                    Marshal2.Copy(rgClusterMap, managed.clusterMap, 0, managed.stringLength);

                    managed.textPosition = Marshal.ReadInt32(top, 32);

                    return managed;
                }
                catch (Exception e)
                {
                    Debugger.Break();
                    throw;
                }
            }
        }


        private enum DWRITE_MEASURING_MODE : int
        {
            DWRITE_MEASURING_MODE_NATURAL,
            DWRITE_MEASURING_MODE_GDI_CLASSIC,
            DWRITE_MEASURING_MODE_GDI_NATURAL,
        }


        [ComImport, Guid("7d97dbf7-e085-42d4-81e3-6a883bded118"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteGlyphRunAnalysis
        {
        }


        [StructLayout(LayoutKind.Sequential, Size = 8)]
        private struct DWRITE_GLYPH_OFFSET
        {
            public float advanceOffset;
            public float ascenderOffset;
        }


        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368040%28v=vs.85%29.aspx
        [DllImport("dwrite.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int DWriteCreateFactory(
            [In] DWRITE_FACTORY_TYPE factoryType,
            [In, MarshalAs(UnmanagedType.Struct)] Guid iid,
            [Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TextEditor.TextServiceDirectWrite.MarshalUnique) /*, MarshalCookie = "IUnknown"*/)] out object factory);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd368057%28v=vs.85%29.aspx
        private enum DWRITE_FACTORY_TYPE : int
        {
            DWRITE_FACTORY_TYPE_SHARED,
            DWRITE_FACTORY_TYPE_ISOLATED,
        }

#if false
        //
        [DllImport("d2d1.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int D2D1CreateFactory(
            [In] D2D1_FACTORY_TYPE factoryType,
            [In, MarshalAs(UnmanagedType.Struct)] Guid riid,
            [In, Optional] D2D1_FACTORY_OPTIONS pFactoryOptions,
            [Out, MarshalAs(UnmanagedType.IUnknown)] out object factory);

        private enum D2D1_FACTORY_TYPE : int
        {
            D2D1_FACTORY_TYPE_SINGLE_THREADED = 0,
            D2D1_FACTORY_TYPE_MULTI_THREADED = 1,
        }

        [StructLayout(LayoutKind.Sequential, Size = 4)]
        private struct D2D1_FACTORY_OPTIONS
        {
            public D2D1_DEBUG_LEVEL debugLevel;
        }

        private enum D2D1_DEBUG_LEVEL : int
        {
            D2D1_DEBUG_LEVEL_NONE = 0,
            D2D1_DEBUG_LEVEL_ERROR = 1,
            D2D1_DEBUG_LEVEL_WARNING = 2,
            D2D1_DEBUG_LEVEL_INFORMATION = 3,
        }

        private static readonly Guid IID_ID2D1Factory = new Guid("06152247-6f50-465a-9245-118bfd3b6007");
#endif
    }
#endif
}
