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
}
