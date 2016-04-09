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
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TextEditor
{
    // Simple text service using the TextRenderer class, made to look like Uniscribe to facilitate conversion
    public class TextServiceSimple : ITextService, IDisposable
    {
        private const TextFormatFlags textFormatFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

        private class TextInfoSimple : ITextInfo, IDisposable
        {
            private readonly Pin<string> pinLine;
            private readonly Font font;
            private readonly int fontHeight;
            private readonly Size size;

            public TextInfoSimple(string line, Font font, int fontHeight, Size size)
            {
                this.font = font;
                this.fontHeight = fontHeight;
                this.size = size;

#if WINDOWS
                pinLine = new Pin<string>(new String((char)0, line.Length));
                bool success = false;
                try
                {
                    Marshal2.Copy(line, 0, pinLine.AddrOfPinnedObject(), line.Length);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        Marshal2.SecureZero(pinLine.Ref, pinLine.AddrOfPinnedObject());
                        pinLine.Dispose();
                    }
                }
#else
                pinLine = new Pin<string>(line);
#endif
            }

            public void Dispose()
            {
                Marshal2.SecureZero(pinLine.Ref, pinLine.AddrOfPinnedObject());
                pinLine.Dispose();
            }

#if WINDOWS
            private class DeviceContext : IDeviceContext
            {
                private readonly Graphics graphics;

                public DeviceContext(Graphics graphics)
                {
                    this.graphics = graphics;
                }

                public IntPtr GetHdc()
                {
                    IntPtr hdc;
                    using (GDIRegion gdiRgnClip = new GDIRegion(graphics.Clip.GetHrgn(graphics)))
                    {
                        hdc = graphics.GetHdc();

                        // Graphics/GDI+ doesn't pass clip region through so we have to reset it explicitly
                        GDI.SelectClipRgn(hdc, gdiRgnClip);
                    }
                    return hdc;
                }

                public void ReleaseHdc()
                {
                    graphics.ReleaseHdc();
                }

                public void Dispose()
                {
                    // not owned
                }
            }
#endif

            public void DrawText(
                Graphics graphics,
                Bitmap backing,
                Point position,
                Color foreColor,
                Color backColor)
            {
#if WINDOWS
                using (DeviceContext dc = new DeviceContext(graphics))
#else
#endif
                {
                    TextRenderer.DrawText(
#if WINDOWS
                        dc,
#else
                        graphics, // Mono's TextRenderer reverses IDeviceContext back to Graphics, but GDI is off the table for Mono so we don't need it.
#endif
                        pinLine.Ref,
                        font,
                        position,
                        foreColor,
                        backColor,
#if WINDOWS
                        textFormatFlags
#else
                        textFormatFlags | TextFormatFlags.PreserveGraphicsClipping
#endif
                        );
                }
            }

            public Region BuildRegion(
                Graphics graphics,
                Point position,
                int startPos,
                int endPosPlusOne)
            {
                Rectangle rect;

                if ((startPos == 0) && (endPosPlusOne == pinLine.Ref.Length))
                {
                    rect = new Rectangle(
                        position,
                        new Size(GetExtent(graphics).Width, fontHeight));
                }
                else
                {
                    int prefixWidth = MeasureTextPrefix(
                        graphics,
                        font,
                        pinLine.Ref,
                        startPos);
                    int twoWidth = MeasureTextPrefix(
                        graphics,
                        font,
                        pinLine.Ref,
                        endPosPlusOne);
                    rect = new Rectangle(
                        new Point(prefixWidth + position.X, position.Y),
                        new Size(twoWidth - prefixWidth, fontHeight));
                }

                return new Region(rect);
            }

            public Size GetExtent(
                Graphics graphics)
            {
                return size;
            }

            private static int MeasureTextPrefix(
                Graphics graphics,
                Font font,
                string text,
                int count)
            {
                Size size;

                char[] chars = null;
                GCHandle hChars = new GCHandle();
                string substr = null;
                GCHandle hSubstr = new GCHandle();
                char[] zero = null;

                try
                {
                    zero = new char[count];
                    chars = new char[count];
                    hChars = GCHandle.Alloc(chars, GCHandleType.Pinned);
                    for (int i = 0; i < count; i++)
                    {
                        chars[i] = text[i];
                    }

                    substr = new String((char)0, count);
                    hSubstr = GCHandle.Alloc(substr, GCHandleType.Pinned);
                    Marshal.Copy(chars, 0, hSubstr.AddrOfPinnedObject(), count);

                    size = TextRenderer.MeasureText(
                        graphics,
                        substr,
                        font,
                        new Size(Int32.MaxValue, Int32.MaxValue),
                        textFormatFlags);
                }
                finally
                {
                    if (substr != null)
                    {
                        Marshal.Copy(zero, 0, hSubstr.AddrOfPinnedObject(), count);
                    }
                    if (chars != null)
                    {
                        Array.Clear(chars, 0, count);
                    }

                    if (hChars.IsAllocated)
                    {
                        hChars.Free();
                    }
                    if (hSubstr.IsAllocated)
                    {
                        hSubstr.Free();
                    }
                }

                return size.Width;
            }

            public void CharPosToX(
                Graphics graphics,
                int offset,
                bool trailing,
                out int x)
            {
                if (trailing)
                {
                    offset++;
                }
                float indent = MeasureTextPrefix(
                    graphics,
                    font,
                    pinLine.Ref,
                    offset);
                x = (int)indent;
            }

            public void XToCharPos(
                Graphics graphics,
                int x,
                out int offset,
                out bool trailing)
            {
                int columnIndex = 0;

                int limit = pinLine.Ref.Length;
                int i = 0;
                float length = 0;
                while (i < limit)
                {
                    int length2 = MeasureTextPrefix(graphics, font, pinLine.Ref, i + 1);
                    float center = (length + length2) / 2;
                    if (x <= length2)
                    {
                        if (x - center < 0)
                        {
                            columnIndex = i;

                            offset = columnIndex;
                            trailing = false;
                            return;
                        }
                        else
                        {
                            columnIndex = i + 1;
                        }
                    }
                    i++;
                    length = length2;
                }
                columnIndex = limit;

                offset = columnIndex;
                trailing = false;
                return;
            }

            public void NextCharBoundary(
                int offset,
                out int nextOffset)
            {
                offset++;
                if ((offset < pinLine.Ref.Length) && Char.IsLowSurrogate(pinLine.Ref[offset]))
                {
                    offset++;
                }
                nextOffset = offset;
            }

            public void PreviousCharBoundary(
                int offset,
                out int prevOffset)
            {
                offset--;
                if (Char.IsLowSurrogate(pinLine.Ref[offset]))
                {
                    offset--;
                }
                prevOffset = offset;
            }

            public void NextWordBoundary(
                int offset,
                out int nextOffset)
            {
                while ((offset < pinLine.Ref.Length) && !Char.IsLetterOrDigit(pinLine.Ref[offset]))
                {
                    /* skipping white space between cursor & next word */
                    offset++;
                }
                while ((offset < pinLine.Ref.Length) && Char.IsLetterOrDigit(pinLine.Ref[offset]))
                {
                    /* skipping over the word itself */
                    offset++;
                }
                Debug.Assert((offset == pinLine.Ref.Length) || !Char.IsLowSurrogate(pinLine.Ref[offset]));
                nextOffset = offset;
            }

            public void PreviousWordBoundary(
                int offset,
                out int prevOffset)
            {
                while ((offset > 0) && !Char.IsLetterOrDigit(pinLine.Ref[offset - 1]))
                {
                    /* skipping white space between cursor & previous word */
                    offset--;
                }
                while ((offset > 0) && Char.IsLetterOrDigit(pinLine.Ref[offset - 1]))
                {
                    /* skipping over the word itself */
                    offset--;
                }
                Debug.Assert((offset == 0) || !Char.IsLowSurrogate(pinLine.Ref[offset]));
                prevOffset = offset;
            }
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
            Size size = TextRenderer.MeasureText(
                graphics,
                line,
                font,
                new Size(Int32.MaxValue, Int32.MaxValue),
                textFormatFlags);
            return new TextInfoSimple(
                line,
                font,
                fontHeight,
                size);
        }

        public TextService Service { get { return TextService.Simple; } }

        public bool Hardened { get { return true; } }

        public void Reset(
            Font font,
            int visibleWidth)
        {
        }

        public void Dispose()
        {
        }
    }
}
