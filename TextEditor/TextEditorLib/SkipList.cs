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
using System.Text;

using TreeLib;

namespace TextEditor
{
    public class SkipList
    {
#if DEBUG
        public const int SkipListSparseness = 5;
#else
        public const int SkipListSparseness = 4096;
#endif
        // index 0 is reserved for prefix placeholder, so all lines are offset by +1
        private readonly SplayTreeRange2List skipList = new SplayTreeRange2List();

        public void Reset(int prefixLength, int suffixLength)
        {
            skipList.Clear();
            skipList.Insert(0, Side.X, 1, prefixLength);
            skipList.Insert(1, Side.X, 1, suffixLength);
        }

        public void GetCountYExtent(int line, out int numLines, out int charIndex, out int charLength)
        {
            line++;
            skipList.Get(line, Side.X, out charIndex, out numLines, out charLength);
        }

        public bool Next(int line, out int nextLine)
        {
            line++;
            bool result = skipList.NearestGreater(line, Side.X, out nextLine);
            nextLine--;
            return result;
        }

        public int LineCount { get { return skipList.GetExtent(Side.X) - 1; } }

        public int CharCount { get { return skipList.GetExtent(Side.Y); } }

        public void NearestLessOrEqualCountYExtent(int line, out int startLine, out int numLines, out int charIndex, out int charCount)
        {
            line++;
            skipList.NearestLessOrEqual(line, Side.X, out startLine);
            skipList.Get(startLine, Side.X, out charIndex, out numLines, out charCount);
            startLine--;
        }

        public void LineLengthChanged(int line, int charDelta)
        {
            line++;
            int startLine, numLines, charIndex, charLength;
            skipList.NearestLessOrEqual(line, Side.X, out startLine);
            skipList.Get(startLine, Side.X, out charIndex, out numLines, out charLength);
            skipList.Delete(startLine, Side.X);
            skipList.Insert(startLine, Side.X, numLines, charLength + charDelta);
        }

        public void BulkLinesInserted(int startLine, int numLines, int charOffset, int charLength)
        {
            startLine++;

#if DEBUG
            int previousStartLine, previousNumLines, previousCharOffset, previousCharLength;
            skipList.NearestLessOrEqual(startLine, Side.X, out previousStartLine);
            skipList.Get(previousStartLine, Side.X, out previousCharOffset, out previousNumLines, out previousCharLength);
            if (previousStartLine != startLine)
            {
                // this general case should never happen in our scenario
#if false
                skipList.Remove(previousStartLine, previousNumLines);
                skipList.Insert(previousStartLine, startLine - previousStartLine, charOffset - previousCharOffset);
                skipList.Insert(startLine, previousNumLines - (startLine - previousStartLine), previousCharLength - (charOffset - previousCharOffset));
#else
                Debug.Assert(false);
#endif
            }
#endif

            skipList.Insert(startLine, Side.X, numLines, charLength);
        }

        public delegate int GetOffsetOfLineMethod(int line);

        public void LineInserted(int lineEndOf, int charsAdded, GetOffsetOfLineMethod getOffsetOfLine)
        {
            Debug.Assert(charsAdded != 0);
            lineEndOf++;
            int startLine, numLines, charIndex, charLength;
            skipList.NearestLessOrEqual(lineEndOf, Side.X, out startLine);
            skipList.Get(startLine, Side.X, out charIndex, out numLines, out charLength);
            skipList.Delete(startLine, Side.X);
            numLines++;
            charLength += charsAdded;
            skipList.Insert(startLine, Side.X, numLines, charLength);

            if (numLines > SkipListSparseness)
            {
                int midpointLine = startLine + numLines / 2;
                midpointLine--;
                int midpointIndex = getOffsetOfLine(midpointLine);
                midpointLine++;
                int firstHalfCharCount = midpointIndex - charIndex;

                skipList.Delete(startLine, Side.X);
                skipList.Insert(startLine, Side.X, midpointLine - startLine, firstHalfCharCount);
                skipList.Insert(midpointLine, Side.X, startLine + numLines - midpointLine, charLength - firstHalfCharCount);
            }
        }

        public void LineRemoved(int lineEndOf, int charsAdded)
        {
            Debug.Assert(charsAdded < 0);
            lineEndOf++;
            int startLine, numLines, charIndex, charLength;
            skipList.NearestLessOrEqual(lineEndOf, Side.X, out startLine);
            skipList.Get(startLine, Side.X, out charIndex, out numLines, out charLength);
            skipList.Delete(startLine, Side.X);
            numLines--;
            charLength += charsAdded;
            Debug.Assert((numLines == 0) == (charLength == 0));
            if (numLines != 0)
            {
                skipList.Insert(startLine, Side.X, numLines, charLength);

                int nextStartLine;
                if ((numLines <= SkipListSparseness / 2) && skipList.NearestGreater(startLine, Side.X, out nextStartLine))
                {
                    int nextNumLines, nextCharIndex, nextCharLength;
                    skipList.Get(nextStartLine, Side.X, out nextCharIndex, out nextNumLines, out nextCharLength);
                    skipList.Delete(nextStartLine, Side.X);
                    skipList.Delete(startLine, Side.X);
                    skipList.Insert(startLine, Side.X, numLines + nextNumLines, charLength + nextCharLength);
                }
            }
        }
    }
}
