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
using System.Runtime.InteropServices;

namespace TextEditor
{
    // Pin a reference object (class, string, array) in memory for use with the using() pattern
    // until it goes out of scope (Dispose() frees the pinning GCHandle)
    public class Pin<T> : IDisposable where T : class
    {
        protected readonly T o;
        private readonly GCHandle h;

        // Pin the object (prevent GC relocation)
        public Pin(T o)
        {
            this.o = o;
            this.h = GCHandle.Alloc(this.o, GCHandleType.Pinned);
        }

        // Get reference to object
        public T Ref { get { return o; } }

        // Get base address of pinned object (for use with System.Runtime.InteropServices.Marshal)
        public IntPtr AddrOfPinnedObject()
        {
            return h.AddrOfPinnedObject();
        }

        // Release the GCHandle, allowing the object to move in memory again.
        public void Dispose()
        {
            //o = default(T);
            if (h.IsAllocated)
            {
                h.Free();
            }
        }
    }

    // Pin a value object (struct) in memory for use with the using() pattern
    // until it goes out of scope (Dispose() frees the pinning GCHandle)
    // Struct object will be on heap, embedded in this object.
    [StructLayout(LayoutKind.Sequential)]
    public class PinStruct<T> : IDisposable where T : struct
    {
        private readonly GCHandle h;
        private T t;

        // Struct is initialized to default value
        public PinStruct()
        {
            this.h = GCHandle.Alloc(this, GCHandleType.Pinned); // pinning container will pin the embedded member
        }

        // Get base address of pinned object (for use with System.Runtime.InteropServices.Marshal)
        public IntPtr AddrOfPinnedObject()
        {
            return new IntPtr(h.AddrOfPinnedObject().ToInt64() + Marshal.OffsetOf(typeof(PinStruct<T>), "t").ToInt64());
        }

        // Get or set members of value object
        public T Value { get { return t; } set { t = value; } }

        // Release the GCHandle to container
        public void Dispose()
        {
            if (h.IsAllocated)
            {
                h.Free();
            }
        }
    }
}
