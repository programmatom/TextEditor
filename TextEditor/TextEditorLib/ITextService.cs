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
using System.Drawing;

namespace TextEditor
{
    public interface ITextInfo : IDisposable
    {
        void DrawText(
            Graphics graphics,
            Bitmap backing,
            Point position,
            Color foreColor,
            Color backColor);

        Region BuildRegion(
            Graphics graphics,
            Point position,
            int startPos,
            int endPosPlusOne);

        Size GetExtent(
            Graphics graphics);

        void CharPosToX(
            Graphics graphics,
            int offset,
            bool trailing,
            out int x);

        void XToCharPos(
            Graphics graphics,
            int x,
            out int offset,
            out bool trailing);

        void NextCharBoundary(
            int offset,
            out int nextOffset);

        void PreviousCharBoundary(
            int offset,
            out int prevOffset);

        void NextWordBoundary(
            int offset,
            out int nextOffset);

        void PreviousWordBoundary(
            int offset,
            out int prevOffset);
    }

    public enum TextService // known services - mostly for designer property editor
    {
        Simple,
        Uniscribe,
        DirectWrite,
    }

    public interface ITextService : IDisposable
    {
        TextService Service { get; }

        bool Hardened { get; }

        void Reset(
            Font font,
            int visibleWidth);

        ITextInfo AnalyzeText(
            Graphics graphics,
            Font font,
            int fontHeight,
            string line);
    }
}
