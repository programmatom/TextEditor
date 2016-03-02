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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TextEditor
{
    public class StringStorageFactory : TextStorage.TextStorageFactory
    {
        public class StringStorageDecodedLine : IDecodedTextLine
        {
            private readonly string line;

            public StringStorageDecodedLine(string line)
            {
                this.line = line;
            }

            public int Length { get { return line.Length; } }

            public char this[int index] { get { return line[index]; } }

            public string Value
            {
                get { return line; }
            }

            ~StringStorageDecodedLine()
            {
#if DEBUG
                Debug.Assert(false, this.GetType().Name + " finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
            }
#if DEBUG
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

#if DEBUG
            public override string ToString()
            {
                return line;
            }
#endif
        }

        public class StringStorageLine : ITextLine
        {
            public readonly string line;

            public StringStorageLine(string line)
            {
                this.line = line;
            }

            public int Length { get { return line.Length; } }

            public IDecodedTextLine Decode_MustDispose()
            {
                return NewDecoded(line);
            }

            protected virtual IDecodedTextLine NewDecoded(string line)
            {
                return new StringStorageDecodedLine(line);
            }

#if DEBUG
            public override string ToString()
            {
                return line;
            }
#endif
        }

        protected class StringStorage : TextStorage
        {
            private FragmentList<string> lines;

            public StringStorage(StringStorageFactory factory)
                : base(factory)
            {
                lines.Add(String.Empty); // always has at least one line
            }

            public static StringStorage Take(
                StringStorage source)
            {
                StringStorage taker = new StringStorage((StringStorageFactory)source.factory);
                taker.lines = source.lines;
                source.lines = new FragmentList<string>();
                return taker;
            }

            protected override void MakeEmpty()
            {
                lines.Clear();
                lines.Add(String.Empty);
            }

            protected override void Insert(int index, ITextLine line)
            {
                if (!(line is StringStorageLine))
                {
                    throw new ArgumentException();
                }
                lines.Insert(index, ((StringStorageLine)line).line);
            }

            protected override void InsertRange(int index, ITextLine[] linesToInsert)
            {
                string[] linesToInsert2 = new string[linesToInsert.Length];
                for (int i = 0; i < linesToInsert.Length; i++)
                {
                    if (!(linesToInsert[i] is StringStorageLine))
                    {
                        throw new ArgumentException();
                    }
                    linesToInsert2[i] = ((StringStorageLine)linesToInsert[i]).line;
                }
                lines.InsertRange(index, linesToInsert2);
            }

            protected override void RemoveRange(int start, int count)
            {
                lines.RemoveRange(start, count);
            }

            protected override int GetLineCount()
            {
                return lines.Count;
            }

            protected override ITextLine GetLine(int index)
            {
                return new StringStorageLine(lines[index]);
            }

            protected override void SetLine(int index, ITextLine line)
            {
                if (!(line is StringStorageLine))
                {
                    throw new ArgumentException();
                }
                lines[index] = ((StringStorageLine)line).line;
            }
        }


        public override bool Hardened { get { return false; } }

        public override TextStorage NewStorage()
        {
            return new StringStorage(this);
        }

        public override ITextStorage Take(
            ITextStorage source)
        {
            if (!(source is StringStorage))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            return StringStorage.Take((StringStorage)source);
        }

        public override ITextLine Encode(string line)
        {
            return new StringStorageLine(line);
        }

        public override ITextLine Encode(char[] chars, int offset, int count)
        {
            return new StringStorageLine(new String(chars, offset, count));
        }

        public override IDecodedTextLine NewDecoded_MustDispose(char[] chars, int offset, int count)
        {
            return new StringStorageDecodedLine(new String(chars, offset, count));
        }

        public override ITextLine Ensure(ITextLine line)
        {
            if (line is StringStorageLine)
            {
                return line;
            }
            else
            {
                using (IDecodedTextLine decodedLine = line.Decode_MustDispose())
                {
                    return new StringStorageLine(decodedLine.Value);
                }
            }
        }

        public override ITextLine Substring(
            ITextLine line,
            int offset,
            int count)
        {
            if (line is StringStorageLine)
            {
                return new StringStorageLine(((StringStorageLine)line).line.Substring(offset, count));
            }
            return base.Substring(line, offset, count);
        }

        public override ITextLine Combine(
            ITextLine lineA,
            int offsetA,
            int countA,
            ITextLine lineB,
            ITextLine lineC,
            int offsetC,
            int countC)
        {
            if ((lineA is StringStorageLine) && (lineC is StringStorageLine)
                && ((lineB == null) || (lineB is StringStorageLine)))
            {
                return new StringStorageLine(
                    String.Concat(
                        ((StringStorageLine)lineA).line.Substring(offsetA, countA),
                        lineB != null ? ((StringStorageLine)lineB).line : String.Empty,
                        ((StringStorageLine)lineC).line.Substring(offsetC, countC)));
            }
            return base.Combine(lineA, offsetA, countA, lineB, lineC, offsetC, countC);
        }
    }
}
