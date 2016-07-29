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
    public class Utf8SplayGapStorageFactory : TextStorage.TextStorageFactory
    {
        public class Utf8GapStorageDecodedLine : StringStorageFactory.StringStorageDecodedLine
        {
            public Utf8GapStorageDecodedLine(string line)
                : base(line)
            {
            }

#if DEBUG
            public override string ToString()
            {
                return Value;
            }
#endif
        }

        public class Utf8GapStorageLine : ITextLine
        {
            public readonly byte[] bytes;

            public Utf8GapStorageLine(byte[] bytes)
            {
                this.bytes = bytes;
            }

            public int Length { get { return Encoding.UTF8.GetCharCount(bytes); } }

            public IDecodedTextLine Decode_MustDispose()
            {
                return new Utf8GapStorageDecodedLine(Encoding.UTF8.GetString(bytes));
            }

#if DEBUG
            public override string ToString()
            {
                return Encoding.UTF8.GetString(bytes);
            }
#endif
        }

        protected class Utf8GapStorage : TextStorage
        {
            private Utf8SplayGapBuffer buffer;

            public Utf8GapStorage(Utf8SplayGapStorageFactory factory, Utf8SplayGapBuffer buffer)
                : base(factory)
            {
                this.buffer = buffer;
            }

            public static Utf8GapStorage Take(
                Utf8GapStorage source)
            {
                Utf8GapStorage taker = new Utf8GapStorage((Utf8SplayGapStorageFactory)source.factory, source.buffer);
                source.buffer = null;
                if (Utf8SplayGapBuffer.EnableValidate)
                {
                    taker.buffer.Validate();
                }
                return taker;
            }

            protected override void MakeEmpty()
            {
                buffer.Clear();
            }

            protected override void Insert(int index, ITextLine line)
            {
                if (!(line is Utf8GapStorageLine))
                {
                    throw new ArgumentException();
                }
                byte[] bytes = ((Utf8GapStorageLine)line).bytes;
                buffer.InsertLine(index, bytes);
            }

            protected override void InsertRange(int index, ITextLine[] linesToInsert)
            {
                Utf8GapStorageLine[] linesToInsert2 = new Utf8GapStorageLine[linesToInsert.Length];
                for (int i = 0; i < linesToInsert.Length; i++)
                {
                    if (!(linesToInsert[i] is Utf8GapStorageLine))
                    {
                        throw new ArgumentException();
                    }
                    linesToInsert2[i] = (Utf8GapStorageLine)linesToInsert[i];
                }
                for (int i = 0; i < linesToInsert2.Length; i++)
                {
                    Insert(index + i, linesToInsert2[i]);
                }
            }

            protected override void RemoveRange(int start, int count)
            {
                while (count > 0)
                {
                    buffer.RemoveLine(start);
                    count--;
                }
            }

            protected override int GetLineCount()
            {
                return buffer.Count;
            }

            protected override ITextLine GetLine(int index)
            {
                ITextLine line = new Utf8GapStorageLine(buffer.GetLine(index));
                return line;
            }

            protected override void SetLine(int index, ITextLine line)
            {
                if (!(line is Utf8GapStorageLine))
                {
                    throw new ArgumentException();
                }
                byte[] bytes = ((Utf8GapStorageLine)line).bytes;
                buffer.SetLine(index, bytes);
            }

            public override ITextStorage CloneSection(int startLine, int startChar, int endLine, int endCharPlusOne)
            {
                if (startLine == endLine)
                {
                    return base.CloneSection(startLine, startChar, endLine, endCharPlusOne);
                }
                else
                {
                    LineEndingInfo lineEndingInfo;
                    Utf8SplayGapBuffer bufferCopy = new Utf8SplayGapBuffer(
                        buffer,
                        startLine + 1,
                        endLine - (startLine + 1),
                        null,
                        out lineEndingInfo);
                    string start = Encoding.UTF8.GetString(buffer.GetLine(startLine));
                    string end = Encoding.UTF8.GetString(buffer.GetLine(endLine));
                    bufferCopy.InsertLine(0, Encoding.UTF8.GetBytes(start.Substring(startChar)));
                    bufferCopy.SetLine(endLine - startLine, Encoding.UTF8.GetBytes(end.Substring(0, endCharPlusOne)));
                    return new Utf8GapStorage((Utf8SplayGapStorageFactory)factory, bufferCopy);
                }
            }

            public override void InsertSection(int insertLine, int insertChar, ITextStorage insert)
            {
                // TODO: optimize for copy/paste
                base.InsertSection(insertLine, insertChar, insert);
            }

            public override string GetText(string EOLN)
            {
                // TODO: optimize for copy/paste
                return base.GetText(EOLN);
            }

            public override void DeleteSection(int startLine, int startChar, int endLine, int endCharPlusOne)
            {
                // TODO: optimize for cut/delete
                base.DeleteSection(startLine, startChar, endLine, endCharPlusOne);
            }

            public override void ToStream(Stream stream, Encoding encoding, string EOLN)
            {
                // TODO: save for preserving line breaks
                base.ToStream(stream, encoding, EOLN);
            }
        }


        public override bool PreservesLineEndings { get { return true; } }

        public override Type[] PermittedEncodings { get { return new Type[] { typeof(UTF8Encoding), typeof(ANSIEncoding), }; } }

        public override TextStorage NewStorage()
        {
            return new Utf8GapStorage(this, new Utf8SplayGapBuffer());
        }

        public override ITextStorage Take(
            ITextStorage source)
        {
            if (!(source is Utf8GapStorage))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            return Utf8GapStorage.Take((Utf8GapStorage)source);
        }

        public override ITextStorage FromUtf16Buffer(string utf16, int offset, int count, string EOLN)
        {
            char[] utf16array = utf16.ToCharArray();
            // ignores EOLN because it preserves original line breaks
            byte[] bytes = Encoding.UTF8.GetBytes(utf16array, 0, count); // TODO: make stream for this
            LineEndingInfo lineEndingInfo;
            return FromStream(new MemoryStream(bytes), Encoding.UTF8, out lineEndingInfo);
        }

        public override ITextStorage FromStream(Stream stream, Encoding encoding, out LineEndingInfo lineEndingInfo)
        {
            bool utf8 = encoding is UTF8Encoding;
            if (!utf8 && !(encoding is ANSIEncoding))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            return new Utf8GapStorage(
                this,
                new Utf8SplayGapBuffer(
                    stream,
                    utf8/*detectBom*/,
                    encoding,
                    out lineEndingInfo));
        }

        public override ITextLine Encode(string line)
        {
            return new Utf8GapStorageLine(Encoding.UTF8.GetBytes(line));
        }

        public override ITextLine Encode(char[] chars, int offset, int count)
        {
            return new Utf8GapStorageLine(Encoding.UTF8.GetBytes(chars, offset, count));
        }

        public override IDecodedTextLine NewDecoded_MustDispose(char[] chars, int offset, int count)
        {
            return new Utf8GapStorageDecodedLine(new String(chars, offset, count));
        }

        public override ITextLine Ensure(ITextLine line)
        {
            if (line is Utf8GapStorageLine)
            {
                return line;
            }
            else
            {
                using (IDecodedTextLine decodedLine = line.Decode_MustDispose())
                {
                    return new Utf8GapStorageLine(Encoding.UTF8.GetBytes(decodedLine.Value));
                }
            }
        }
    }
}
