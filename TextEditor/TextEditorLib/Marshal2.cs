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
using System.Runtime.InteropServices;
using System.Text;

namespace TextEditor
{
    public static class Marshal2
    {
#if false
        public static float ReadFloat32(IntPtr p, int byteOffset)
        {
            float[] f = new float[1];
            Marshal.Copy(new IntPtr(p.ToInt64() + byteOffset), f, 0, 1);
            return f[0];
        }

        public static float ReadFloat32(IntPtr p)
        {
            return ReadFloat32(p, 0);
        }

        public static void WriteFloat32(IntPtr p, int byteOffset, float value)
        {
            float[] f = new float[1] { value };
            Marshal.Copy(f, 0, new IntPtr(p.ToInt64() + byteOffset), 1);
        }

        public static void WriteFloat32(IntPtr p, float value)
        {
            WriteFloat32(p, 0, value);
        }
#endif

#if false
        public static void Copy(ushort[] src, int srcOffset, IntPtr dest, int count)
        {
            // Could be made faster using some unsafe code and reinterpret-casting
            for (int i = 0; i < count; i++)
            {
                short s = unchecked((short)src[i + srcOffset]);
                Marshal.WriteInt16(dest, i * 2, s);
            }
        }

        public static void Copy(IntPtr src, ushort[] dest, int destOffset, int count)
        {
            // Could be made faster using some unsafe code and reinterpret-casting
            for (int i = 0; i < count; i++)
            {
                short s = Marshal.ReadInt16(src, i * 2);
                dest[i + destOffset] = unchecked((ushort)s);
            }
        }
#endif

        public static void Copy(string src, int srcOffset, IntPtr dest, int count)
        {
            // Could be made faster using some unsafe code and reinterpret-casting
            using (Pin<string> pinSrc = new Pin<string>(src))
            {
                IntPtr pSrc = pinSrc.AddrOfPinnedObject();
                for (int i = 0; i < count; i++)
                {
                    short s = Marshal.ReadInt16(pSrc, (i + srcOffset) * 2);
                    char c = unchecked((char)s);
                    Marshal.WriteInt16(dest, i * 2, c);
                }
            }
        }

#if false
        public static void CopyArrayOfStruct<T>(T[] src, int srcOffset, IntPtr dest, int destByteCapacity, int count) where T : struct
        {
            if ((src == null) || (dest == IntPtr.Zero))
            {
                Debugger.Break();
                Debug.Assert(false);
                throw new ArgumentException();
            }
            int elementSize = Marshal.SizeOf(typeof(T));
            int c = count * elementSize;
            if (c > destByteCapacity)
            {
                Debugger.Break();
                Debug.Assert(false);
                throw new ArgumentException();
            }
            GCHandle hSrc = GCHandle.Alloc(src, GCHandleType.Pinned);
            try
            {
                IntPtr bSrc = hSrc.AddrOfPinnedObject();
                for (int i = 0; i < c; i++)
                {
                    Marshal.WriteByte(dest, i, Marshal.ReadByte(bSrc, i));
                }
            }
            catch (Exception e)
            {
                Debugger.Break();
                throw;
            }
            finally
            {
                hSrc.Free();
            }
        }

        public static void CopyArrayOfStruct<T>(IntPtr src, T[] dest, int destOffset, int count) where T : struct
        {
            if ((src == IntPtr.Zero) || (dest == null))
            {
                Debugger.Break();
                Debug.Assert(false);
                throw new ArgumentException();
            }
            int elementSize = Marshal.SizeOf(typeof(T));
            int destByteCapacity = (dest.Length - destOffset) * elementSize;
            int c = elementSize * count;
            if (c > destByteCapacity)
            {
                Debugger.Break();
                Debug.Assert(false);
                throw new ArgumentException();
            }
            GCHandle hDest = GCHandle.Alloc(dest, GCHandleType.Pinned);
            try
            {
                int o = destOffset * elementSize;
                IntPtr bDest = hDest.AddrOfPinnedObject();
                for (int i = 0; i < c; i++)
                {
                    Marshal.WriteByte(bDest, i + o, Marshal.ReadByte(src, i));
                }
            }
            catch (Exception e)
            {
                Debugger.Break();
                throw;
            }
            finally
            {
                hDest.Free();
            }
        }
#endif

        private static void SecureZero(IntPtr baseAddr, int byteCount)
        {
            byte[] zero = new byte[byteCount];
            Marshal.Copy(zero, 0, baseAddr, byteCount);
        }

        private static int UnsafeSizeOfPinnedArrayElement(Array array)
        {
            return (int)(Marshal.UnsafeAddrOfPinnedArrayElement(array, 1).ToInt64()
                - Marshal.UnsafeAddrOfPinnedArrayElement(array, 0).ToInt64());
        }

        public static void SecureZero(string str, IntPtr baseAddr)
        {
            if (str.Length != 0)
            {
                using (Pin<string> pinStr = new Pin<string>(str))
                {
                    const int elementSize = 2;
                    if (pinStr.AddrOfPinnedObject() != baseAddr)
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    IntPtr pinStrAddr0 = pinStr.AddrOfPinnedObject();
                    // try to detect when assumption about internal structure of String object is invalid
                    if ((short)str[0] != Marshal.ReadInt16(pinStrAddr0, 0))
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    SecureZero(pinStrAddr0, str.Length * elementSize);
                }
            }
        }

        public static void SecureZero(byte[] array, IntPtr baseAddr)
        {
            using (Pin<byte[]> pinArray = new Pin<byte[]>(array))
            {
                Debug.Assert(1 == UnsafeSizeOfPinnedArrayElement(array));
                if (pinArray.AddrOfPinnedObject() != baseAddr)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                SecureZero(pinArray.AddrOfPinnedObject(), array.Length);
            }
        }

        public static void SecureZero(char[] array, IntPtr baseAddr)
        {
            using (Pin<char[]> pinArray = new Pin<char[]>(array))
            {
                const int elementSize = 2;
                Debug.Assert(elementSize == UnsafeSizeOfPinnedArrayElement(array));
                if (pinArray.AddrOfPinnedObject() != baseAddr)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                SecureZero(pinArray.AddrOfPinnedObject(), array.Length * elementSize);
            }
        }

        public static void SecureZero<T>(T[] array, IntPtr baseAddr) where T : struct
        {
            using (Pin<T[]> pinArray = new Pin<T[]>(array))
            {
                int elementSize = UnsafeSizeOfPinnedArrayElement(array);
                if (pinArray.AddrOfPinnedObject() != baseAddr)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                SecureZero(pinArray.AddrOfPinnedObject(), array.Length * elementSize);
            }
        }
    }
}
