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
using System.IO;
using System.Text;

using TreeLib;

namespace TextEditor
{
    public abstract class Utf8GapBuffer
    {
        public abstract int Current { get; }
        public abstract int Count { get; }

        public abstract void Clear();
        public abstract void MoveTo(int index);
        public abstract byte[] GetLine(int index);
        public abstract void SetLine(int index, byte[] buffer);
        public abstract void InsertLine(int index, byte[] buffer);
        public abstract void RemoveLine(int index);
    }

    public class Utf8SplayGapBuffer : Utf8GapBuffer
    {
#if DEBUG
        public static readonly bool EnableValidate = true;
#else
        public const bool EnableValidate = false;
#endif
        public const int ValidateCutoffLines1 = 100; // cutoff for very slow thorough validation
        public const int ValidateCutoffLines2 = 500; // cutoff for slow moderate validation

        private const int BlockSize = 4096;

        private static readonly byte[] WindowsLF = new byte[] { (byte)'\r', (byte)'\n' };
        private static readonly byte[] MacintoshLF = new byte[] { (byte)'\r' };
        private static readonly byte[] UnixLF = new byte[] { (byte)'\n' };
        private static readonly byte[][] LineEndings = new byte[3][] { WindowsLF, MacintoshLF, UnixLF }; // must match enum LineEndings

        private static readonly byte[] LineEndingChars = WindowsLF; // just both

        private byte[] defaultLineEnding = WindowsLF; // TODO: code for changing default line ending
        private HugeList<byte> vector = new HugeList<byte>(typeof(SplayTreeRangeMap<>), BlockSize);
        private SkipList skipList = new SkipList();

        private int totalLines;
        private int currentLine;
        private int currentOffset;

        private byte prefixLength;
        private byte suffixLength;
        private byte bomLength; // TODO: code for enable/disable of bom inclusion

        public Utf8SplayGapBuffer()
        {
            Clear();
        }

        public override void Clear()
        {
            vector.Clear();

            bomLength = 0;

            // invariant: require separators at ends
            Debug.Assert(WindowsLF.Length == 2);
            prefixLength = 2;
            vector.InsertRange(0, WindowsLF);
            suffixLength = 2;
            vector.InsertRange(2, WindowsLF);

            totalLines = 1;
            currentLine = 0;
            currentOffset = 2;

            skipList.Reset(prefixLength, suffixLength);

            if (EnableValidate)
            {
                Validate();
            }
        }

        public override int Current
        {
            get
            {
                return currentLine;
            }
        }

        public override int Count
        {
            get
            {
                return totalLines;
            }
        }

#if DEBUG
        private void Dump()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < vector.Count + 1; i++)
            {
                sb.Append(i % 10 == 0 ? '0' : ' ');
            }
            sb.AppendLine();
            for (int i = 0; i < vector.Count; i++)
            {
                if (vector[i] == '\r')
                {
                    sb.Append('_');
                }
                else if (vector[i] == '\n')
                {
                    sb.Append('|');
                }
                else
                {
                    sb.Append((char)vector[i]);
                }
            }
            sb.AppendLine();
            for (int i = 0; i < vector.Count + 1; i++)
            {
                sb.Append(i == currentOffset ? '<' : ' ');
            }
            sb.AppendLine();
            sb.AppendFormat("currentLine={0} currentOffset={3} length={2} totalLines={1}" + Environment.NewLine, currentLine, totalLines, vector.Count, currentOffset);
            int startLine = 0;
            do
            {
                int numLines, charOffset, charLength;
                skipList.GetCountYExtent(startLine, out numLines, out charOffset, out charLength);
                sb.AppendFormat("  skip: line={0}..{1} ({2}) char={3}..{4} ({5})" + Environment.NewLine, startLine, startLine + numLines - 1, numLines, charOffset, charOffset + charLength - 1, charLength);
            } while (skipList.Next(startLine, out startLine));
            Debugger.Log(0, null, sb.ToString());
        }
#endif

        public void Validate()
        {
            Debug.Assert(EnableValidate);

            //Dump();

            List<int> lineOffsets = new List<int>();
            int offset = prefixLength;
            while (offset != vector.Count)
            {
                lineOffsets.Add(offset);
                int next = vector.IndexOfAny(LineEndingChars, offset, vector.Count - offset);
                if (!(next >= offset))
                {
                    Debugger.Break();
                }
                Debug.Assert(next >= offset);
                offset = FindAfterOfLineBreak(next);
                Debug.Assert(offset > next);
            }

            byte[] ifix = new byte[2];
            vector.CopyTo(prefixLength - WindowsLF.Length, ifix, 0, ifix.Length);
            Debug.Assert((ifix[0] == WindowsLF[0]) && (ifix[1] == WindowsLF[1]));
            ifix = new byte[2];
            vector.CopyTo(vector.Count - suffixLength, ifix, 0, ifix.Length);
            Debug.Assert((ifix[0] == WindowsLF[0]) && (ifix[1] == WindowsLF[1]));

            Debug.Assert(lineOffsets.Count == totalLines);
            lineOffsets.Add(vector.Count);
            Debug.Assert((currentLine >= 0) && (currentLine <= totalLines));
            Debug.Assert(currentOffset == lineOffsets[currentLine]);

            Debug.Assert(skipList.LineCount == totalLines);
            Debug.Assert(skipList.CharCount == vector.Count);
            int startLine = 0;
            do
            {
                int numLines, charOffset, charLength;
                skipList.GetCountYExtent(startLine, out numLines, out charOffset, out charLength);
                Debug.Assert(lineOffsets[startLine] == charOffset);
            } while (skipList.Next(startLine, out startLine));
        }

        public override void MoveTo(int targetLine)
        {
            if (unchecked((uint)targetLine) > unchecked((uint)totalLines))
            {
                Debug.Assert(false);
                throw new ArgumentOutOfRangeException();
            }

            if (EnableValidate)
            {
                if (totalLines < ValidateCutoffLines1)
                {
                    Validate();
                }
            }

            Debug.Assert(skipList.LineCount == totalLines);
            Debug.Assert(skipList.CharCount == vector.Count);

            int startLine, numLines, charOffset, charLength;
            skipList.NearestLessOrEqualCountYExtent(targetLine, out startLine, out numLines, out charOffset, out charLength);
            if (Math.Abs(startLine - targetLine) < Math.Abs(currentLine - targetLine))
            {
                currentLine = startLine;
                currentOffset = charOffset;

                if (EnableValidate)
                {
                    if (totalLines < ValidateCutoffLines2)
                    {
                        Validate();
                    }
                }
            }

            MoveTo(targetLine, ref currentLine, ref currentOffset);

            if (EnableValidate)
            {
                if (totalLines < ValidateCutoffLines2)
                {
                    Validate();
                }
            }
        }

        private void MoveTo(int targetLine, ref int currentLine, ref int currentOffset)
        {
            Debug.Assert(IsAtLineEnding(currentOffset - 1));
            while (targetLine > currentLine)
            {
                int lineLength, lineEndingLength;
                GetCurrentLineExtent(currentOffset, out lineLength, out lineEndingLength);
                currentOffset += lineLength + lineEndingLength;
                currentLine++;
            }
            while (targetLine < currentLine)
            {
                int lineStart, lineLength, lineEndingLength;
                GetPreviousLineExtent(currentOffset, out lineStart, out lineLength, out lineEndingLength);
                Debug.Assert(lineStart + lineLength + lineEndingLength == currentOffset);
                currentOffset = lineStart;
                currentLine--;
            }
        }

        private bool IsAtLineEnding(int offset)
        {
            byte b = vector[offset];
            return (b == (byte)'\r') || (b == (byte)'\n');
        }

        private int FindStartOfLineBreak(int offset)
        {
            byte b1 = vector[offset];
            if (b1 == (byte)'\n')
            {
                byte b2 = vector[offset - 1];
                if (b2 == (byte)'\r')
                {
                    return offset - 1;
                }
                else
                {
                    return offset;
                }
            }
            else if (b1 == (byte)'\r')
            {
                return offset;
            }
            else
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
        }

        private int FindAfterOfLineBreak(int offset)
        {
            byte b1 = vector[offset];
            if (b1 == (byte)'\r')
            {
                byte b2 = vector[offset + 1];
                if (b2 == (byte)'\n')
                {
                    return offset + 2;
                }
                else
                {
                    return offset + 1;
                }
            }
            else if (b1 == (byte)'\n')
            {
                return offset + 1;
            }
            else
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
        }

        private void GetPreviousLineExtent(int offset, out int start, out int lineBodyLength, out int lineEndingLength)
        {
            Debug.Assert(IsAtLineEnding(offset - 1));
            Debug.Assert(offset - 1 >= prefixLength);
            int lineBreakStart = FindStartOfLineBreak(offset - 1);
            int precedingLineBreakLast = vector.LastIndexOfAny(
                LineEndingChars,
                lineBreakStart - 1,
                lineBreakStart);
            Debug.Assert(IsAtLineEnding(precedingLineBreakLast));
            Debug.Assert(precedingLineBreakLast >= prefixLength - 1);
            start = precedingLineBreakLast + 1;
            lineBodyLength = lineBreakStart - start;
            lineEndingLength = offset - lineBreakStart;
        }

        private void GetCurrentLineExtent(int offset, out int lineBodyLength, out int lineEndingLength)
        {
            Debug.Assert(IsAtLineEnding(offset - 1));
            Debug.Assert(offset <= vector.Count - suffixLength);
            int lineBreakStart = vector.IndexOfAny(
                LineEndingChars,
                offset,
                vector.Count - offset);
            Debug.Assert(lineBreakStart <= vector.Count - suffixLength);
            int afterLineBreak = FindAfterOfLineBreak(lineBreakStart);
            lineEndingLength = afterLineBreak - lineBreakStart;
            lineBodyLength = lineBreakStart - offset;
        }

        public override byte[] GetLine(int index)
        {
            MoveTo(index);
            int lineBodyLength, lineEndingLength;
            GetCurrentLineExtent(currentOffset, out lineBodyLength, out lineEndingLength);
            byte[] bytes = new byte[lineBodyLength];
            vector.CopyTo(currentOffset, bytes, 0, bytes.Length);
            return bytes;
        }

        public override void SetLine(int index, byte[] buffer)
        {
            if ((Array.IndexOf(buffer, (byte)'\r') >= 0) || (Array.IndexOf(buffer, (byte)'\n') >= 0))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            MoveTo(index);
            int lineBodyLength, lineEndingLength;
            GetCurrentLineExtent(currentOffset, out lineBodyLength, out lineEndingLength);
            vector.ReplaceRange(currentOffset, lineBodyLength, buffer);

            skipList.LineLengthChanged(currentLine, buffer.Length - lineBodyLength);

            if (EnableValidate)
            {
                if (totalLines < ValidateCutoffLines2)
                {
                    Validate();
                }
            }
        }

        private int GetStartIndexOfLineRelative(int numLines, int startIndex)
        {
            int currentLine = 0;
            MoveTo(numLines, ref currentLine, ref startIndex);
            return startIndex;
        }

        public override void InsertLine(int index, byte[] buffer)
        {
            if ((Array.IndexOf(buffer, (byte)'\r') >= 0) || (Array.IndexOf(buffer, (byte)'\n') >= 0))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            MoveTo(index);

            int precedingLineBreakStart = FindStartOfLineBreak(currentOffset - 1);
            int precedingLineBreakLength = currentOffset - precedingLineBreakStart;
            vector.ReplaceRange(precedingLineBreakStart, precedingLineBreakLength, defaultLineEnding);
            skipList.LineLengthChanged(currentLine - 1, defaultLineEnding.Length - precedingLineBreakLength);
            currentOffset = currentOffset - precedingLineBreakLength + defaultLineEnding.Length;

            vector.InsertRange(currentOffset, buffer);
            vector.InsertRange(currentOffset + buffer.Length, WindowsLF);
            skipList.LineInserted(
                currentLine,
                buffer.Length + WindowsLF.Length,
                delegate (int line)
                {
                    return GetStartIndexOfLineRelative(line - currentLine, currentOffset);
                });

            totalLines++;

            Debug.Assert(IsAtLineEnding(currentOffset - 1));
            if (EnableValidate)
            {
                if (totalLines < ValidateCutoffLines2)
                {
                    Validate();
                }
            }
        }

        public override void RemoveLine(int index)
        {
            MoveTo(index);

            int lineBodyLength, lineEndingLength;
            GetCurrentLineExtent(currentOffset, out lineBodyLength, out lineEndingLength);

            vector.RemoveRange(currentOffset, lineBodyLength + lineEndingLength);
            skipList.LineRemoved(currentLine, -(lineBodyLength + lineEndingLength));

            int precedingLineBreakStart = FindStartOfLineBreak(currentOffset - 1);
            int precedingLineBreakLength = currentOffset - precedingLineBreakStart;
            Debug.Assert(precedingLineBreakStart >= prefixLength);
            vector.ReplaceRange(precedingLineBreakStart, precedingLineBreakLength, WindowsLF);
            skipList.LineLengthChanged(currentLine - 1, WindowsLF.Length - precedingLineBreakLength);
            currentOffset += WindowsLF.Length - precedingLineBreakLength;

            totalLines--;

            Debug.Assert(currentOffset <= vector.Count);
            Debug.Assert(IsAtLineEnding(currentOffset - 1));
            if (EnableValidate)
            {
                if (totalLines < ValidateCutoffLines2)
                {
                    Validate();
                }
            }
        }

        public Utf8SplayGapBuffer(
            Stream stream,
            bool detectBom,
            Encoding encoding,
            out LineEndingInfo lineEndingInfo)
        {
            byte[] buffer = new byte[vector.MaxBlockSize];
            while (true)
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }
                vector.InsertRange(vector.Count, buffer, 0, read);
            }

            if ((vector.Count >= 3) && ((vector[0] == 0xEF) && (vector[1] == 0xBB) && (vector[2] == 0xBF)))
            {
                bomLength = 3;
            }

            lineEndingInfo = new LineEndingInfo();

            // invariant: require separators at ends
            Debug.Assert(WindowsLF.Length == 2);
            prefixLength = (byte)(bomLength + 2);
            vector.InsertRange(bomLength, WindowsLF);
            suffixLength = 2;
            vector.InsertRange(vector.Count, WindowsLF);

            totalLines = 0;
            currentLine = 0;
            currentOffset = prefixLength;

            skipList.Reset(prefixLength, suffixLength);

            bool ignoreEncoding = (encoding == null) || (encoding is UTF8Encoding);

            int lineEndingCount = 0;
            int endOfData = vector.Count - suffixLength/*avoid our artifical addition*/;
            //
            int currentSkipStartLine = 0;
            int currentSkipNumLines = 0;
            int currentSkipCharOffset = prefixLength;
            int currentSkipCharLength = 0;
            //          
            while (currentOffset < endOfData)
            {
                int textEnd = vector.IndexOfAny(LineEndingChars, currentOffset, endOfData - currentOffset);
                if (textEnd < 0)
                {
                    textEnd = endOfData;
                }
                int textLength = textEnd - currentOffset;

                // TODO: can extend to also handle line terminators
                if (!ignoreEncoding)
                {
                    byte[] bytes = new byte[textLength];
                    string s = encoding.GetString(bytes);
                    int c = Encoding.UTF8.GetByteCount(s);
                    if (c != bytes.Length)
                    {
                        byte[] bytes2 = Encoding.UTF8.GetBytes(s);
                        vector.ReplaceRange(currentOffset, bytes.Length, bytes2);
                        currentSkipCharLength += bytes2.Length - bytes.Length;
                    }
                }

                Debug.Assert(IsAtLineEnding(textEnd));
                int nextStart = textEnd;
                if (nextStart < endOfData)
                {
                    lineEndingCount++;
                    if (vector[nextStart] == (byte)'\r')
                    {
                        if (vector[nextStart + 1] == (byte)'\n')
                        {
                            nextStart++;
                            lineEndingInfo.windowsLFCount++;
                        }
                        else
                        {
                            lineEndingInfo.macintoshLFCount++;
                        }
                    }
                    else
                    {
                        Debug.Assert(vector[nextStart] == (byte)'\n');
                        lineEndingInfo.unixLFCount++;
                    }
                    nextStart++;
                }

                currentSkipNumLines++;
                currentSkipCharLength += nextStart - currentOffset;
                if (currentSkipNumLines > SkipList.SkipListSparseness)
                {
                    skipList.BulkLinesInserted(currentSkipStartLine, currentSkipNumLines, currentSkipCharOffset, currentSkipCharLength);
                    currentSkipStartLine += currentSkipNumLines;
                    currentSkipNumLines = 0;
                    currentSkipCharOffset += currentSkipCharLength;
                    currentSkipCharLength = 0;
                }

                currentLine++;
                totalLines++;
                currentOffset = nextStart;
            }
            if (currentSkipNumLines != 0)
            {
                skipList.BulkLinesInserted(currentSkipStartLine, currentSkipNumLines, currentSkipCharOffset, currentSkipCharLength);
            }

            if (lineEndingCount == totalLines)
            {
                // file ends with blank line
                totalLines++;
            }
            else
            {
                // last line was unterminated - back it out
                skipList.LineRemoved(currentLine, -suffixLength);
                skipList.LineLengthChanged(currentLine, suffixLength);

                currentOffset += suffixLength;
            }

            if (EnableValidate)
            {
                Validate();
            }
        }

        public Utf8SplayGapBuffer(Utf8SplayGapBuffer source, int startLine, int countLines, Encoding encoding, out LineEndingInfo lineEndingInfo)
            : this(
                new VectorReadStream(
                    source.vector,
                    source.GetStartIndexOfLineRelative(startLine, source.prefixLength),
                    source.GetStartIndexOfLineRelative(startLine + countLines, source.prefixLength)
                        - source.GetStartIndexOfLineRelative(startLine, source.prefixLength)),
                false/*detectBom*/,
                encoding,
                out lineEndingInfo)
        {
        }

        private class VectorReadStream : Stream
        {
            private HugeList<byte> vector;
            private int start;
            private int length;
            private int position;

            public VectorReadStream(HugeList<byte> vector, int start, int length)
            {
                if (unchecked((uint)start > (uint)vector.Count) || unchecked((uint)start + (uint)length > (uint)vector.Count))
                {
                    Debug.Assert(false);
                    throw new ArgumentOutOfRangeException();
                }
                this.vector = vector;
                this.start = start;
                this.length = length;
            }

            public override bool CanRead { get { return true; } }

            public override bool CanSeek { get { return true; } }

            public override bool CanWrite { get { return false; } }

            public override void Flush()
            {
            }

            public override long Length { get { return length; } }

            public override long Position
            {
                get
                {
                    return position;
                }
                set
                {
                    if (unchecked((ulong)value > (ulong)length))
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    position = (int)value;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int read = 0;
                int c;
                do
                {
                    c = Math.Min(count, length - position);
                    vector.CopyTo(start + position, buffer, offset, c);
                    read += c;
                    count -= c;
                    offset += c;
                    position += c;
                } while (c != 0);
                return read;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                switch (origin)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case SeekOrigin.Begin:
                        this.Position = offset;
                        break;
                    case SeekOrigin.Current:
                        this.Position = offset + position;
                        break;
                    case SeekOrigin.End:
                        this.Position = start + length + offset;
                        break;
                }
                return position - start;
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
