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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace TextEditor
{
    public class ProtectedStorageFactory : TextStorage.TextStorageFactory
    {
        protected class ProtectedStorageDecodedLine : IDecodedTextLine
        {
            private readonly Pin<string> pinLine;

            public ProtectedStorageDecodedLine(string line)
            {
                this.pinLine = new Pin<string>(line);
            }

            public int Length { get { return pinLine.Ref.Length; } }

            public char this[int index] { get { return pinLine.Ref[index]; } }

            public string Value { get { return pinLine.Ref; } }

            ~ProtectedStorageDecodedLine()
            {
                Dispose();
#if DEBUG
                Debug.Assert(false, this.GetType().Name + " finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
            }
#if DEBUG
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

            public void Dispose()
            {
                Marshal2.SecureZero(pinLine.Ref, pinLine.AddrOfPinnedObject());
                pinLine.Dispose();
                GC.SuppressFinalize(this);
            }

#if DEBUG
            public override string ToString()
            {
                return Value;
            }
#endif
        }

        protected class ProtectedStorageLine : ITextLine
        {
            public readonly int count; // REVIEW
            public readonly byte[] bytes;

            public ProtectedStorageLine(int count, byte[] bytes)
            {
                this.count = count;
                this.bytes = bytes;
            }

            public static ProtectedStorageLine FromRaw(byte[] plaintextBytes, int plaintextCharCount)
            {
                int plaintextByteCount = plaintextCharCount * 2;

                using (Pin<byte[]> pinPlaintextBytes = new Pin<byte[]>(plaintextBytes))
                {
                    int encryptedByteCount = 4 + plaintextCharCount * 2;
                    const int Alignment = ProtectedDataStorage.CRYPTPROTECTMEMORY_BLOCK_SIZE;
                    encryptedByteCount = (encryptedByteCount + (Alignment - 1)) & ~(Alignment - 1); // round up to required alignment

                    byte[] zero = new byte[encryptedByteCount];
                    byte[] encrypted = new byte[encryptedByteCount];
                    int i = 0;
                    encrypted[i++] = (byte)(plaintextCharCount >> 24);
                    encrypted[i++] = (byte)(plaintextCharCount >> 16);
                    encrypted[i++] = (byte)(plaintextCharCount >> 8);
                    encrypted[i++] = (byte)plaintextCharCount;
                    Marshal.Copy(pinPlaintextBytes.AddrOfPinnedObject(), encrypted, i, plaintextByteCount);

                    if (!ProtectedDataStorage.CryptProtectMemory(
                        encrypted,
                        encrypted.Length,
                        ProtectedDataStorage.CryptProtectMemoryFlags.CRYPTPROTECTMEMORY_SAME_PROCESS))
                    {
                        int hr = Marshal.GetHRForLastWin32Error();
                        Buffer.BlockCopy(zero, 0, encrypted, 0, encryptedByteCount);
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    return new ProtectedStorageLine(plaintextCharCount, encrypted);
                }
            }

            public static ProtectedStorageLine FromString(string plaintextLine)
            {
                int byteCount = plaintextLine.Length * 2;
                using (Pin<byte[]> pinPlaintextBytes = new Pin<byte[]>(new byte[byteCount]))
                {
                    byte[] plaintextBytesRef = pinPlaintextBytes.Ref;
                    try
                    {
                        for (int i = 0; i < plaintextLine.Length; i++)
                        {
                            // in string, char is little-endian
                            plaintextBytesRef[2 * i + 0] = (byte)(plaintextLine[i] & 0xff);
                            plaintextBytesRef[2 * i + 1] = (byte)(plaintextLine[i] >> 8);
                        }
                        return FromRaw(plaintextBytesRef, plaintextLine.Length);
                    }
                    finally
                    {
                        Marshal2.SecureZero(pinPlaintextBytes.Ref, pinPlaintextBytes.AddrOfPinnedObject());
                    }
                }
            }

            public static ProtectedStorageLine FromCharArray(char[] chars, int charOffset, int charCount)
            {
                int byteCount = charCount * 2;
                using (Pin<byte[]> pinPlaintextBytes = new Pin<byte[]>(new byte[byteCount]))
                {
                    try
                    {
                        Marshal.Copy(chars, charOffset, pinPlaintextBytes.AddrOfPinnedObject(), charCount);
                        return FromRaw(pinPlaintextBytes.Ref, charCount);
                    }
                    finally
                    {
                        Marshal2.SecureZero(pinPlaintextBytes.Ref, pinPlaintextBytes.AddrOfPinnedObject());
                    }
                }
            }

            public int Length { get { return count; } }

            public IDecodedTextLine Decode_MustDispose()
            {
                using (Pin<byte[]> pinDecrypted = new Pin<byte[]>((byte[])bytes.Clone()))
                {
                    try
                    {
                        byte[] pinDecryptedRef = pinDecrypted.Ref;
                        if (!ProtectedDataStorage.CryptUnprotectMemory(
                            pinDecryptedRef,
                            pinDecryptedRef.Length,
                            ProtectedDataStorage.CryptProtectMemoryFlags.CRYPTPROTECTMEMORY_SAME_PROCESS))
                        {
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                        }

                        int i = 0;
                        int decryptedCharCount = pinDecryptedRef[i++] << 24;
                        decryptedCharCount |= pinDecryptedRef[i++] << 16;
                        decryptedCharCount |= pinDecryptedRef[i++] << 8;
                        decryptedCharCount |= pinDecryptedRef[i++];

                        using (Pin<string> pinS = new Pin<string>(new String((char)0, decryptedCharCount)))
                        {
                            Marshal.Copy(pinDecryptedRef, i, pinS.AddrOfPinnedObject(), decryptedCharCount * 2);
                            return new ProtectedStorageDecodedLine(pinS.Ref);
                            // SECURITY: plaintext string S lives on owned by ProtectedStorageDecodedLine object
                        }
                    }
                    finally
                    {
                        Marshal2.SecureZero(pinDecrypted.Ref, pinDecrypted.AddrOfPinnedObject());
                    }
                }
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("Length={0}", Length);
            }
#endif
        }

        protected class ProtectedStorage : TextStorage
        {
            private FragmentList<ProtectedStorageLine> lines;

            public ProtectedStorage(ProtectedStorageFactory factory)
                : base(factory)
            {
                lines.Add(ProtectedStorageLine.FromString(String.Empty)); // always has at least one line
            }

            public static ProtectedStorage Take(
                ProtectedStorage source)
            {
                ProtectedStorage taker = new ProtectedStorage((ProtectedStorageFactory)source.factory);
                taker.lines = source.lines;
                source.lines = new FragmentList<ProtectedStorageLine>();
                return taker;
            }

            protected override void MakeEmpty()
            {
                lines.Clear();
                lines.Add(ProtectedStorageLine.FromString(String.Empty));
            }

            protected override void Insert(int index, ITextLine line)
            {
                if (!(line is ProtectedStorageLine))
                {
                    throw new ArgumentException();
                }
                lines.Insert(index, (ProtectedStorageLine)line);
            }

            protected override void InsertRange(int index, ITextLine[] linesToInsert)
            {
                ProtectedStorageLine[] linesToInsert2 = new ProtectedStorageLine[linesToInsert.Length];
                for (int i = 0; i < linesToInsert.Length; i++)
                {
                    if (!(linesToInsert[i] is ProtectedStorageLine))
                    {
                        throw new ArgumentException();
                    }
                    linesToInsert2[i] = (ProtectedStorageLine)linesToInsert[i];
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
                return lines[index];
            }

            protected override void SetLine(int index, ITextLine line)
            {
                if (!(line is ProtectedStorageLine))
                {
                    throw new ArgumentException();
                }
                lines[index] = (ProtectedStorageLine)line;
            }
        }


        public override bool Hardened { get { return true; } }

        public override TextStorage NewStorage()
        {
            return new ProtectedStorage(this);
        }

        public override ITextStorage Take(
            ITextStorage source)
        {
            if (!(source is ProtectedStorage))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            return ProtectedStorage.Take((ProtectedStorage)source);
        }

        public override ITextLine Encode(string line)
        {
            return ProtectedStorageLine.FromString(line);
        }

        public override ITextLine Encode(char[] chars, int offset, int count)
        {
            return ProtectedStorageLine.FromCharArray(chars, offset, count);
        }

        public override IDecodedTextLine NewDecoded_MustDispose(char[] chars, int offset, int count)
        {
            return ProtectedStorageLine.FromCharArray(chars, offset, count).Decode_MustDispose();
        }

        public override ITextLine Ensure(ITextLine line)
        {
            if (line is ProtectedStorageLine)
            {
                return line;
            }
            else
            {
                using (IDecodedTextLine decodedLine = line.Decode_MustDispose())
                {
                    return ProtectedStorageLine.FromString(decodedLine.Value);
                }
            }
        }

        public override ITextLine Substring(
            ITextLine line,
            int offset,
            int count)
        {
            using (IDecodedTextLine decodedLine = line.Decode_MustDispose())
            {
                using (Pin<char[]> pinSubstr = new Pin<char[]>(new char[count]))
                {
                    try
                    {
                        char[] pinSubstrRef = pinSubstr.Ref;
                        string v = decodedLine.Value;
                        for (int i = 0; i < count; i++)
                        {
                            pinSubstrRef[i] = v[i + offset];
                        }
                        return ProtectedStorageLine.FromCharArray(pinSubstrRef, 0, count);
                        // SECURITY: pinSubstr string content lives on owned by ProtectedStorageLine object
                    }
                    finally
                    {
                        Marshal2.SecureZero(pinSubstr.Ref, pinSubstr.AddrOfPinnedObject());
                    }
                }
            }
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
            using (IDecodedTextLine decodedLineA = lineA.Decode_MustDispose())
            {
                using (IDecodedTextLine decodedLineB = lineB != null ? lineB.Decode_MustDispose() : null)
                {
                    using (IDecodedTextLine decodedLineC = lineC.Decode_MustDispose())
                    {
                        int total = countA + (decodedLineB != null ? decodedLineB.Length : 0) + countC;
                        using (Pin<char[]> pinCombined = new Pin<char[]>(new char[total]))
                        {
                            char[] pinCombinedRef = pinCombined.Ref;
                            try
                            {
                                string v;
                                int index = 0;

                                v = decodedLineA.Value;
                                for (int i = 0; i < countA; i++)
                                {
                                    pinCombinedRef[index++] = v[i + offsetA];
                                }

                                if (decodedLineB != null)
                                {
                                    v = decodedLineB.Value;
                                    int l = decodedLineB.Length;
                                    for (int i = 0; i < l; i++)
                                    {
                                        pinCombinedRef[index++] = v[i];
                                    }
                                }

                                v = decodedLineC.Value;
                                for (int i = 0; i < countC; i++)
                                {
                                    pinCombinedRef[index++] = v[i + offsetC];
                                }

                                Debug.Assert(index == total);

                                return ProtectedStorageLine.FromCharArray(pinCombinedRef, 0, total);
                                // SECURITY: content of pinCombined lives on owned by ProtectedStorageLine object
                            }
                            finally
                            {
                                Marshal2.SecureZero(pinCombined.Ref, pinCombined.AddrOfPinnedObject());
                            }
                        }
                    }
                }
            }
        }
    }

    // Windows Protected Storage (DPAPI)
    public static class ProtectedDataStorage
    {
        // http://www.pinvoke.net/default.aspx/crypt32/CryptProtectData.html
        // http://msdn.microsoft.com/en-us/library/ms995355.aspx

        // From WinCrypt.h

        // Just the in-memory protection APIs.

        public const Int32 CRYPTPROTECTMEMORY_BLOCK_SIZE = 16;

        [Flags]
        public enum CryptProtectMemoryFlags
        {
            // Encrypt/Decrypt within current process context.
            CRYPTPROTECTMEMORY_SAME_PROCESS = 0x00,

            // Encrypt/Decrypt across process boundaries.
            // eg: encrypted buffer passed across LPC to another process which calls CryptUnprotectMemory.
            CRYPTPROTECTMEMORY_CROSS_PROCESS = 0x01,

            // Encrypt/Decrypt across callers with same LogonId.
            // eg: encrypted buffer passed across LPC to another process which calls CryptUnprotectMemory whilst impersonating.
            CRYPTPROTECTMEMORY_SAME_LOGON = 0x02,
        }

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptProtectMemory(
            byte[] pData,
            Int32 cbData,
            CryptProtectMemoryFlags dwFlags);

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptUnprotectMemory(
            byte[] pData,
            Int32 cbData,
            CryptProtectMemoryFlags dwFlags);
    }
}
