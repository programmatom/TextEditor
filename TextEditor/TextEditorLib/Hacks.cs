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
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace TextEditor
{
    public static class FontHeightHack
    {
        public static int GetActualFontHeight(Font font, int fontHeight)
        {
#if true // TODO: font height hack - figure out to do this right
            // For some fonts, the Font.Height value isn't sufficient. notepad.exe adds an extra pixel to the height.
            // How does it know? I can't figure it out, hence this hack to determine it empirically.

            int width = fontHeight * 4;
            using (Bitmap scratch = new Bitmap(width, fontHeight * 2, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            {
                using (Graphics graphics = Graphics.FromImage(scratch))
                {
                    graphics.FillRectangle(Brushes.White, 0, 0, scratch.Width, scratch.Height);
                    TextRenderer.DrawText(graphics, "gjy", font, new Point(), Color.Black);
                }
                int y = scratch.Height;
                while (y > fontHeight)
                {
                    bool nonwhite = false;
                    for (int x = 0; x < width; x++)
                    {
                        if ((scratch.GetPixel(x, y - 1).ToArgb() & 0x00FFFFFF) != 0x00FFFFFF)
                        {
                            nonwhite = true;
                            break;
                        }
                    }
                    if (nonwhite)
                    {
                        break;
                    }
                    y--;
                }
                fontHeight = y;
            }

            return fontHeight;
#endif
        }
    }
}
