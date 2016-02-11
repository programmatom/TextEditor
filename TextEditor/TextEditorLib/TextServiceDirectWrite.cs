/*
 *  Copyright � 1992-2002, 2015 Thomas R. Lawrence
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
using System.Windows.Forms;

namespace TextEditor
{
    public class TextServiceDirectWrite : ITextService, IDisposable
    {
        private readonly TextServiceDirectWriteInterop interop;
        private readonly ITextService uniscribeService;

        public TextServiceDirectWrite()
        {
            try
            {
                interop = new TextServiceDirectWriteInterop();
            }
            catch (Exception exception)
            {
                // dll load issue
                Debugger.Break();
                MessageBox.Show("Failed to load DirectWrite interop dll: " + exception.ToString());
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
            int hr = interop.Reset(font, visibleWidth);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
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
            private readonly TextServiceLineDirectWriteInterop lineInterop;
            private readonly ITextInfo uniscribeLine;
            private readonly string text;

            public TextLayout(
                TextServiceDirectWrite service,
                string line,
                Graphics graphics_,
                Font font_)
            {
                try
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
                catch (Exception)
                {
                    Dispose();
                    throw;
                }
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
                try
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
                // TODO: figure out why DW returns HR 0x8007007A on garbage strings (e.g. from opening binary files)
                catch (COMException exception)
                {
                    using (Font font = new Font(FontFamily.GenericSansSerif, backing.Height / 2.5f, FontStyle.Regular))
                    {
                        TextRenderer.DrawText(
                            graphics,
                            exception.ToString(),
                            font,
                            position,
                            foreColor,
                            backColor,
                            TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.SingleLine);
                    }
                }
            }

            public Region BuildRegion(
                Graphics graphics,
                Point position,
                int startPos,
                int endPosPlusOne)
            {
                try
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
                // TODO: figure out why DW returns HR 0x8007007A on garbage strings (e.g. from opening binary files)
                catch (COMException)
                {
                    return new Region();
                }
            }

            public Size GetExtent(
                Graphics graphics)
            {
                try
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
                // TODO: figure out why DW returns HR 0x8007007A on garbage strings (e.g. from opening binary files)
                catch (COMException)
                {
                    return new Size();
                }
            }

            public void CharPosToX(
                Graphics graphics,
                int offset,
                bool trailing,
                out int x)
            {
                try
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
                // TODO: figure out why DW returns HR 0x8007007A on garbage strings (e.g. from opening binary files)
                catch (COMException)
                {
                    x = 0;
                }
            }

            public void XToCharPos(
                Graphics graphics,
                int x,
                out int offset,
                out bool trailing)
            {
                try
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
                // TODO: figure out why DW returns HR 0x8007007A on garbage strings (e.g. from opening binary files)
                catch (COMException)
                {
                    offset = 0;
                    trailing = false;
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

    // A rather incomplete wrapper around DirectWrite that provides functions matching what TextRenderer provides.
    public static class DirectWriteTextRenderer
    {
        private static List<KeyValuePair<Font, TextServiceDirectWriteInterop>> interops;
#if false
        private static TextServiceDirectWriteInterop interop;
        private static Font lastFont;
#endif
        private static int lastWidth;
        private const int MaxCachedInterops = 3;

        private static TextServiceDirectWriteInterop EnsureInterop(Font font)
        {
#if false
            if (interop == null)
            {
                interop = new TextServiceDirectWriteInterop();
            }
            if ((lastFont != font) || (lastWidth != Screen.PrimaryScreen.Bounds.Width))
            {
                lastFont = font;
                lastWidth = Screen.PrimaryScreen.Bounds.Width;
                interop.Reset(lastFont, lastWidth);
            }
#endif
            if (interops == null)
            {
                interops = new List<KeyValuePair<Font, TextServiceDirectWriteInterop>>(MaxCachedInterops);
            }

            if (lastWidth != Screen.PrimaryScreen.Bounds.Width)
            {
                ClearCache();
            }
            lastWidth = Screen.PrimaryScreen.Bounds.Width;

            int index = -1;
            for (int i = 0; i < interops.Count; i++)
            {
                if (font.Equals(interops[i].Key))
                {
                    index = i;
                }
            }
            if (index < 0)
            {
                if (interops.Count >= MaxCachedInterops)
                {
                    ClearCache();
                }
                index = interops.Count;
                TextServiceDirectWriteInterop interop = new TextServiceDirectWriteInterop();
                interop.Reset(font, lastWidth);
                interops.Add(new KeyValuePair<Font, TextServiceDirectWriteInterop>(font, interop));
#if DEBUG
                Debugger.Log(
                    1,
                    "TextServiceDirectWrite",
                    String.Concat(
                        DateTime.Now.ToString(),
                        ": Added DirectWrite interop for ",
                        font.ToString(),
                        " ",
                        font.Style.ToString(),
                        Environment.NewLine));
#endif
            }
            return interops[index].Value;
        }

        private static void ClearCache()
        {
            for (int i = 0; i < interops.Count; i++)
            {
                interops[i].Value._Dispose();
            }
            interops.Clear();
#if DEBUG
            Debugger.Log(
                1,
                "TextServiceDirectWrite",
                String.Concat(
                    DateTime.Now.ToString(),
                    ": Flushed DirectWrite interop list",
                    Environment.NewLine));
#endif
        }

        public static void DrawText(Graphics graphics, string text, Font font, Point pt, Color foreColor)
        {
            DrawText(graphics, text, font, new Rectangle(pt, MeasureText(graphics, text, font)), foreColor, Color.Transparent, TextFormatFlags.Default);
        }

        public static void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color foreColor)
        {
            DrawText(graphics, text, font, bounds, foreColor, Color.Transparent, TextFormatFlags.Default);
        }

        public static void DrawText(Graphics graphics, string text, Font font, Point pt, Color foreColor, Color backColor)
        {
            DrawText(graphics, text, font, new Rectangle(pt, MeasureText(graphics, text, font)), foreColor, backColor, TextFormatFlags.Default);
        }

        public static void DrawText(Graphics graphics, string text, Font font, Point pt, Color foreColor, TextFormatFlags flags)
        {
            DrawText(graphics, text, font, new Rectangle(pt, MeasureText(graphics, text, font)), foreColor, Color.Transparent, flags);
        }

        public static void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color foreColor, Color backColor)
        {
            DrawText(graphics, text, font, bounds, foreColor, backColor, TextFormatFlags.Default);
        }

        public static void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color foreColor, TextFormatFlags flags)
        {
            DrawText(graphics, text, font, bounds, foreColor, Color.Transparent, flags);
        }

        public static void DrawText(Graphics graphics, string text, Font font, Point pt, Color foreColor, Color backColor, TextFormatFlags flags)
        {
            DrawText(graphics, text, font, new Rectangle(pt, MeasureText(graphics, text, font)), foreColor, backColor, flags);
        }

        public static void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color foreColor, Color backColor, TextFormatFlags flags)
        {
            TextServiceDirectWriteInterop interop = EnsureInterop(font);

            using (TextServiceLineDirectWriteInterop lineInterop = new TextServiceLineDirectWriteInterop())
            {
                int hr = lineInterop.Init(interop, text);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                if ((flags & TextFormatFlags.HorizontalCenter) != 0)
                {
                    Size size;
                    hr = lineInterop.GetExtent(graphics, out size);
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }
                    bounds.X += (bounds.Width - size.Width) / 2;
                    bounds.Width += size.Width / 2;
                }
                lineInterop.DrawTextWithRenderTarget(graphics, bounds, foreColor, backColor);
            }
        }

        //public static Size MeasureText(string text, Font font);

        public static Size MeasureText(Graphics graphics, string text, Font font)
        {
            TextServiceDirectWriteInterop interop = EnsureInterop(font);

            using (TextServiceLineDirectWriteInterop lineInterop = new TextServiceLineDirectWriteInterop())
            {
                int hr = lineInterop.Init(interop, text);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                Size extent;
                hr = lineInterop.GetExtent(graphics, out extent);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                return extent;
            }
        }

        //public static Size MeasureText(string text, Font font, Size proposedSize);

        public static Size MeasureText(Graphics graphics, string text, Font font, Size proposedSize)
        {
            return MeasureText(graphics, text, font);
        }

        //public static Size MeasureText(string text, Font font, Size proposedSize, TextFormatFlags flags);

        public static Size MeasureText(Graphics graphics, string text, Font font, Size proposedSize, TextFormatFlags flags)
        {
            return MeasureText(graphics, text, font);
        }


        public static void RegisterPrivateMemoryFont(byte[] data)
        {
            GCHandle hData = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                TextServiceFontEnumeratorHelper.AddFont(hData.AddrOfPinnedObject().ToInt64(), data.Length);
            }
            finally
            {
                hData.Free();
            }
        }
    }
}
