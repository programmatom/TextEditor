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
using System.IO;
using System.Text;

namespace TextEditor
{
    public struct SelPoint
    {
        public int Line;
        public int Column;

        public SelPoint(int line, int column)
        {
            this.Line = line;
            this.Column = column;
        }

        public int CompareTo(SelPoint other)
        {
            int c;
            c = this.Line.CompareTo(other.Line);
            if (c == 0)
            {
                c = this.Column.CompareTo(other.Column);
            }
            return c;
        }

        public static bool operator <(SelPoint a, SelPoint b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator <=(SelPoint a, SelPoint b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >(SelPoint a, SelPoint b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator >=(SelPoint a, SelPoint b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator ==(SelPoint a, SelPoint b)
        {
            return a.CompareTo(b) == 0;
        }

        public static bool operator !=(SelPoint a, SelPoint b)
        {
            return a.CompareTo(b) != 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is SelPoint)
            {
                return this.CompareTo((SelPoint)obj) == 0;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return (~Line).GetHashCode() ^ Column.GetHashCode();
        }

#if DEBUG
        public override string ToString()
        {
            return String.Format("({0}, {1})", Line, Column);
        }
#endif
    }

    public struct SelRange
    {
        public SelPoint Start;
        public SelPoint End;

        public SelRange(SelPoint start, SelPoint end)
        {
            if (start.CompareTo(end) > 0)
            {
                throw new ArgumentException();
            }
            this.Start = start;
            this.End = end;
        }

        public SelRange(int startLine, int startChar, int endLine, int endCharPlusOne)
            : this(new SelPoint(startLine, startChar), new SelPoint(endLine, endCharPlusOne))
        {
        }

#if DEBUG
        public override string ToString()
        {
            return String.Format("[{0}..{1}>", Start, End);
        }
#endif
    }

    public interface IDecodedTextLine
    {
        int Length { get; }
        char this[int index] { get; }
        string Value { get; }
    }

    public interface ITextLine
    {
        int Length { get; }
        IDecodedTextLine Decode_MustDispose();
    }

    public enum LineEndings
    {
        Windows = 0,
        Macintosh = 1,
        Unix = 2,
    }

    public interface ITextStorage
    {
        int Count { get; }

        ITextLine this[int index] { get; }

        string GetText(
            string EOLN);

        bool Modified { get; set; }
        bool Empty { get; }

        ITextStorage CloneSection(
            int startLine,
            int startChar,
            int endLine,
            int endCharPlusOne);
        void DeleteSection(
            int startLine,
            int startChar,
            int endLine,
            int endCharPlusOne);
        void InsertSection(
            int insertLine,
            int insertChar,
            ITextStorage insert);

        void ToTextWriter(
            TextWriter writer);
        void ToStream(
            Stream stream,
            Encoding encoding,
            string EOLN);
    }

    public struct LineEndingInfo
    {
        public int unixLFCount;
        public int windowsLFCount;
        public int macintoshLFCount;
    }

    public interface ITextStorageFactory
    {
        bool PreservesLineEndings { get; }
        Type[] PermittedEncodings { get; } // null == any encoding permitted

        ITextStorage New();
        ITextStorage Copy(
            ITextStorage source);
        ITextStorage Take(
            ITextStorage source);
        ITextStorage FromUtf16Buffer(
            string utf16,
            int offset,
            int count,
            string EOLN);
        ITextStorage FromTextReader(
            TextReader reader);
        ITextStorage FromStream(
            Stream stream,
            Encoding encoding,
            out LineEndingInfo lineEndingInfo);

        ITextLine Encode(
            string line);
        ITextLine Encode(
            char[] chars,
            int offset,
            int count);
        IDecodedTextLine NewDecoded_MustDispose(
            char[] chars,
            int offset,
            int count);

        ITextLine Ensure(
            ITextLine line);
        ITextLine Substring(
            ITextLine line,
            int offset,
            int count);
        ITextLine Combine(
            ITextLine lineA,
            int offsetA,
            int countA,
            ITextLine lineB,
            ITextLine lineC,
            int offsetC,
            int countC);
    }
}
