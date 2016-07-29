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
    public abstract class TextStorage : ITextStorage
    {
        // customized support

        protected abstract void MakeEmpty();

        protected abstract void Insert(
            int index,
            ITextLine line);
        protected abstract void InsertRange(
            int index,
            ITextLine[] linesToInsert);
        protected abstract void RemoveRange(
            int start,
            int count);

        protected abstract int GetLineCount();

        protected abstract ITextLine GetLine(
            int index);
        protected abstract void SetLine(
            int index,
            ITextLine line);


        // factored base implementation

        protected readonly TextStorageFactory factory;
        private bool modified;

        public TextStorage(
            TextStorageFactory factory)
        {
            this.factory = factory;
        }

        public void CopyFrom(
            ITextStorage source)
        {
            if (source.Count < 1)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            MakeEmpty();
            SetLine(0, factory.Ensure(source[0]));
            for (int i = 1; i < source.Count; i++)
            {
                Insert(i, factory.Ensure(source[i]));
            }

            this.modified = source.Modified;
        }

        public int Count
        {
            get
            {
                int count = GetLineCount();

                if (count < 1)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                return count;
            }
        }

        ITextLine ITextStorage.this[int index]
        {
            get
            {
                return GetLine(index);
            }
        }

        public virtual string GetText(string EOLN)
        {
            StringBuilder sb = MakeRawBuffer(EOLN);
            return sb.ToString();
        }

        public bool Modified
        {
            get
            {
                return modified;
            }
            set
            {
                modified = value;
            }
        }

        public virtual bool Empty
        {
            get
            {
                return (GetLineCount() == 1) && (GetLine(0).Length == 0);
            }
        }

        /* extract part of the stored data in the form of another text storage object */
        public virtual ITextStorage CloneSection(
            int startLine,
            int startChar,
            int endLine,
            int endCharPlusOne)
        {
            if ((startLine < 0) || (startLine >= GetLineCount())
                || (endLine < 0) || (endLine >= GetLineCount()))
            {
                // Line index out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((startLine > endLine) || ((startLine == endLine) && (startChar > endCharPlusOne)))
            {
                // Inconsistent range specified
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((startChar < 0) || (startChar > GetLine(startLine).Length)
                || (endCharPlusOne < 0) || (endCharPlusOne > GetLine(endLine).Length))
            {
                // Character ranges for lines exceeded
                Debug.Assert(false);
                throw new ArgumentException();
            }

            TextStorage copy = factory.NewStorage();

            if (startLine == endLine)
            {
                /* special case */
                copy.SetLine(
                    0,
                    factory.Substring(
                        GetLine(startLine),
                        startChar,
                        endCharPlusOne - startChar));
            }
            else
            {
                /* initialize line insertion position */
                int targetLineIndex = 0;

                /* second half of first line */
                ITextLine line = GetLine(startLine);
                copy.SetLine(
                    targetLineIndex,
                    factory.Substring(
                        line,
                        startChar,
                        line.Length - startChar));
                targetLineIndex++;

                /* munch middle lines (if any) */
                for (int i = startLine + 1; i <= endLine - 1; i++)
                {
                    Debug.Assert(copy.GetLineCount() == targetLineIndex);
                    copy.Insert(targetLineIndex, GetLine(i));
                    targetLineIndex++;
                }

                /* first half of last line at end of buffer */
                Debug.Assert(copy.GetLineCount() == targetLineIndex);
                copy.Insert(
                    targetLineIndex,
                    factory.Substring(
                        GetLine(endLine),
                        0,
                        endCharPlusOne));
                targetLineIndex++;
            }

            copy.modified = false;
            return copy;
        }

        /* delete the specified range of data from the storage. */
        public virtual void DeleteSection(
            int startLine,
            int startChar,
            int endLine,
            int endCharPlusOne)
        {
            if ((startLine < 0) || (startLine >= GetLineCount())
                || (endLine < 0) || (endLine >= GetLineCount()))
            {
                // Line index out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((startLine > endLine) || ((startLine == endLine) && (startChar > endCharPlusOne)))
            {
                // Inconsistent range specified
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((startChar < 0) || (startChar > GetLine(startLine).Length)
                || (endCharPlusOne < 0) || (endCharPlusOne > GetLine(endLine).Length))
            {
                // Character ranges for lines exceeded
                Debug.Assert(false);
                throw new ArgumentException();
            }

            if (startLine == endLine)
            {
                /* special case of deleting part of a single line */
                ITextLine original = GetLine(startLine);
                SetLine(
                    startLine,
                    factory.Combine(
                        original,
                        0,
                        startChar,
                        null,
                        original,
                        endCharPlusOne,
                        original.Length - endCharPlusOne));
            }
            else
            {
                /* create composite line */
                ITextLine endTextLine = GetLine(endLine);
                ITextLine composite = factory.Combine(
                    GetLine(startLine),
                    0,
                    startChar,
                    null,
                    endTextLine,
                    endCharPlusOne,
                    endTextLine.Length - endCharPlusOne);

                /* delete all lines except first */
                RemoveRange(startLine + 1, endLine - startLine);

                /* replace start line with composite */
                SetLine(startLine, composite);
            }

            modified = true;
        }

        /* insert a storage block at the specified position into this storage block. */
        /* note:  there are (number of lines) - 1 line breaks */
        public virtual void InsertSection(
            int insertLine,
            int insertChar,
            ITextStorage insert)
        {
            if ((insertLine < 0) || (insertLine >= GetLineCount()))
            {
                // Line position out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((insertChar < 0) || (insertChar > GetLine(insertLine).Length))
            {
                // Character position out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }

            /* check for special case where inertion only has 1 line */
            int insertLines = insert.Count;
            if (insertLines == 1)
            {
                /* special case */

                ITextLine origLine = GetLine(insertLine);

                SetLine(
                    insertLine,
                    factory.Combine(
                        origLine,
                        0,
                        insertChar,
                        insert[0],
                        origLine,
                        insertChar,
                        origLine.Length - insertChar));
            }
            else
            {
                /* count characters for resize */
                int charCount = 0;

                ITextLine origLine = GetLine(insertLine);

                /* create first composite line */
                ITextLine insertStart = insert[0];
                int insertStartLength = insertStart.Length;
                ITextLine compositeStart = factory.Combine(
                    origLine,
                    0,
                    insertChar,
                    null,
                    insertStart,
                    0,
                    insertStartLength);
                charCount += insertStartLength;

                /* create second composite line */
                ITextLine insertEnd = insert[insertLines - 1];
                int insertEndLength = insertEnd.Length;
                ITextLine compositeEnd = factory.Combine(
                    insertEnd,
                    0,
                    insertEndLength,
                    null,
                    origLine,
                    insertChar,
                    origLine.Length - insertChar);
                charCount += insertEndLength;

                /* currently there is 1 line. that line will be replaced with composite */
                /* start, then composite end will be inserted (+ 1), and then the middle */
                /* lines will be inserted (+ (StuffLineCount - 2)). */
                int insertedLineCount = 0;

                /* insert inner lines */
#if DEBUG
                const int BlockSize = 16;
#else
                const int BlockSize = 4096;
#endif
                // blocked loop reduces temporary memory demand at cost of multiple insertions
                int innerCount = insertLines - 2;
                for (int ii = 0; ii < innerCount; ii += BlockSize)
                {
                    int c = Math.Min(BlockSize, innerCount - ii);
                    ITextLine[] interiorLines = new ITextLine[c];
                    for (int i = 0; i < c; i++)
                    {
                        ITextLine toInsert = insert[i + ii + 1];
                        interiorLines[i] = factory.Ensure(toInsert);
                        charCount += toInsert.Length;
                    }
                    InsertRange(
                        ii + insertLine + 1/*skip start*/,
                        interiorLines);
                }
                insertedLineCount += innerCount;

                /* insert last composite line */
                Insert(
                    (insertLine + 1/*skip start*/) + insertedLineCount,
                    compositeEnd);
                insertedLineCount++;

                /* replace first line with first composite */
                SetLine(insertLine, compositeStart);
            }

            modified = true;
        }

        /* if the end of line sequence is of the specified length, then calculate how */
        /* many characters a packed buffer of text would contain */
        private int TotalNumChars(int EOLNLength)
        {
            int total = 0;

            int count = GetLineCount();
            for (int i = 0; i < count; i++)
            {
                total += GetLine(i).Length;
            }

            /* all but last line have an eoln marker, so add that in too */
            total += EOLNLength * (count - 1);

            return total;
        }

        /* helper routine to fill in a buffer of raw data. */
        private StringBuilder MakeRawBuffer(string EOLN)
        {
            int totalNumChars = TotalNumChars(EOLN.Length);

            StringBuilder text = new StringBuilder(totalNumChars);

            int count = GetLineCount();
            int i = 0;
            while (true)
            {
                using (IDecodedTextLine decodedLine = GetLine(i).Decode_MustDispose())
                {
                    text.Append(decodedLine.Value);
                }
                if (i == count - 1)
                {
                    break;
                }
                text.Append(EOLN);
                i++;
            }

            /* sanity check */
#if DEBUG
            if (text.Length != totalNumChars)
            {
                // index is not at end of buffer
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif

            return text;
        }

        // use TextWriter.NewLine to configure which newline character sequence to write
        public virtual void ToTextWriter(TextWriter writer)
        {
#if false // TODO: later -- allow plaintext save for testing
            if (factory.Hardened)
            {
                throw new InvalidOperationException("Plaintext operation not permitted on hardened storage object");
            }
#endif

            int count = GetLineCount();
            int i = 0;
            while (true)
            {
                using (IDecodedTextLine decodedLine = GetLine(i).Decode_MustDispose())
                {
                    writer.Write(decodedLine.Value);
                }
                if (i == count - 1)
                {
                    break;
                }
                writer.WriteLine();
                i++;
            }
        }

        // TODO: implement directly
        public virtual void ToStream(Stream stream, Encoding encoding, string EOLN)
        {
            using (TextWriter writer = new StreamWriter(stream, encoding))
            {
                writer.NewLine = EOLN;
                ToTextWriter(writer);
            }
        }


        // factory

        public abstract class TextStorageFactory : Component, ITextStorageFactory
        {
            // customization

            public virtual bool PreservesLineEndings { get { return false; } }

            public virtual Type[] PermittedEncodings { get { return null; } }

            public abstract TextStorage NewStorage();

            public abstract ITextStorage Take(
                ITextStorage source);

            public abstract ITextLine Encode(
                string line);

            public abstract ITextLine Encode(
                char[] chars,
                int offset,
                int count);

            public abstract IDecodedTextLine NewDecoded_MustDispose(
                char[] chars,
                int offset,
                int count);

            public abstract ITextLine Ensure(
                ITextLine line);


            // factored base methods

            public ITextStorage New()
            {
                return NewStorage();
            }

            public ITextStorage Copy(
                ITextStorage source)
            {
                TextStorage copy = NewStorage();
                copy.CopyFrom(source);
                return copy;
            }

            // for short strings
            private static bool StringAt(
                string line,
                int offset,
                string pattern)
            {
                if (offset + pattern.Length > line.Length)
                {
                    return false;
                }
                for (int i = 0; i < pattern.Length; i++)
                {
                    if (pattern[i] != line[i + offset])
                    {
                        return false;
                    }
                }
                return true;
            }

            public virtual ITextStorage FromUtf16Buffer(
                string utf16,
                int offset,
                int count,
                string EOLN)
            {
                TextStorage text = NewStorage();

                int index = offset;
                int lineIndex = 0;
                bool lastLineEndedWithCR = false;
                while (index < count)
                {
                    /* not end of buffer, so we don't know if line ends with CR */
                    lastLineEndedWithCR = false;

                    /* look for end of next line */
                    int currentLineLength = 0;
                    while ((currentLineLength + index < count)
                        && ((EOLN.Length == 0) || !StringAt(utf16, currentLineLength + index, EOLN)))
                    {
                        currentLineLength++;
                    }

                    if (currentLineLength + index < count)
                    {
                        lastLineEndedWithCR = (EOLN.Length != 0) && StringAt(utf16, currentLineLength + index, EOLN);
                    }

                    ITextLine insertion = Encode(utf16.Substring(index, currentLineLength));
                    if (lineIndex != 0)
                    {
                        /* if it isn't the first line, then we need to append a line */
                        text.Insert(lineIndex, insertion);
                    }
                    else
                    {
                        text.SetLine(lineIndex, insertion);
                    }

                    index += currentLineLength + EOLN.Length;
                    lineIndex++;
                }

                /* if the block ended with a carriage return, then add a blank line on the end */
                if (lastLineEndedWithCR)
                {
                    text.Insert(lineIndex, Encode(String.Empty));
                }

                text.modified = false;
                return text;
            }

            public virtual ITextStorage FromTextReader(
                TextReader reader)
            {
                TextStorage text = NewStorage();
                string line;
                int index = 1;
                while ((line = reader.ReadLine()) != null)
                {
                    text.Insert(index, Encode(line));
                    index++;
                }
                text.modified = false;
                return text;
            }

            public virtual ITextStorage FromStream(
                Stream stream,
                Encoding encoding,
                out LineEndingInfo lineEndingInfo)
            {
                lineEndingInfo = new LineEndingInfo();

                TextStorage text = NewStorage();

                Decoder decoder = encoding.GetDecoder();

                byte[] bytes = new byte[4096]; // odd number for testing continuations
                int usedBytes = 0;
                char[] chars = new char[4096];
                int usedChars = 0;
                char[] currentLine = new char[128];
                int currentLineIndex = 0;
                int index = 0;
                while (true)
                {
                    int readBytes = stream.Read(bytes, usedBytes, bytes.Length - usedBytes);
                    usedBytes += readBytes;

                    // if there is an incomplete multi-byte sequence at the end of the file, Convert()
                    // will eat it without reporting.
                    int usedBytes1, usedChars1;
                    bool completed;
                    decoder.Convert(bytes, 0, usedBytes, chars, usedChars, chars.Length - usedChars, readBytes == 0/*flush*/, out usedBytes1, out usedChars1, out completed);
                    Array.Copy(bytes, usedBytes1, bytes, 0, usedBytes - usedBytes1);
                    usedBytes -= usedBytes1;
                    usedChars += usedChars1;

                    int i;
                    for (i = 0; i < usedChars; i++)
                    {
                        bool lineBreak = false;
                        if (chars[i] == '\r')
                        {
                            if (!(i + 1 < usedChars))
                            {
                                if (i == 0)
                                {
                                    lineBreak = true;
                                    lineEndingInfo.macintoshLFCount++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                lineBreak = true;
                                if (chars[i + 1] == '\n')
                                {
                                    i++;
                                    lineEndingInfo.windowsLFCount++;
                                }
                                else
                                {
                                    lineEndingInfo.macintoshLFCount++;
                                }
                            }
                        }
                        else if (chars[i] == '\n')
                        {
                            lineBreak = true;
                            lineEndingInfo.unixLFCount++;
                        }

                        if (lineBreak)
                        {
                            text.Insert(index, Encode(currentLine, 0, currentLineIndex));
                            currentLineIndex = 0;
                            index++;
                        }
                        else
                        {
                            if (currentLine.Length == currentLineIndex)
                            {
                                Array.Resize(ref currentLine, currentLine.Length * 2);
                            }
                            currentLine[currentLineIndex++] = chars[i];
                        }
                    }
                    Array.Copy(chars, i, chars, 0, usedChars - i);
                    usedChars -= i;

                    if ((usedBytes == 0) && (readBytes == 0) && (usedChars == 0))
                    {
                        break;
                    }
                }
                Debug.Assert(index == text.GetLineCount() - 1);
                text.SetLine(index, Encode(currentLine, 0, currentLineIndex));

                text.modified = false;
                return text;
            }

            public virtual ITextLine Substring(
                ITextLine line,
                int offset,
                int count)
            {
                using (IDecodedTextLine decodedLine = line.Decode_MustDispose())
                {
                    return Encode(decodedLine.Value.Substring(offset, count));
                }
            }

            public virtual ITextLine Combine(
                ITextLine lineA,
                int offsetA,
                int countA,
                ITextLine lineB,
                ITextLine lineC,
                int offsetC,
                int countC)
            {
                using (IDecodedTextLine decodedLineA = lineA.Decode_MustDispose())
                {
                    using (IDecodedTextLine decodedLineB = lineB != null ? lineB.Decode_MustDispose() : null)
                    {
                        using (IDecodedTextLine decodedLineC = lineC.Decode_MustDispose())
                        {
                            return Encode(
                                String.Concat(
                                    decodedLineA.Value.Substring(offsetA, countA),
                                    decodedLineB != null ? decodedLineB.Value : String.Empty,
                                    decodedLineC.Value.Substring(offsetC, countC)));
                        }
                    }
                }
            }
        }
    }
}
