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

namespace TextEditor
{
    // A replacement for List<T> that tries to stay off the large object heap for big collections
    // but minimizes allocations for tiny collections
    public struct FragmentList<T>
    {
        private const int BlockSize = 4096; // must be integer power of 2

        private int count;
        private T[] simpleArray; // exactly one of these is not null
        private T[][] fragmentArray; // exactly one of these is not null

#if DEBUG
        private const bool Validate = true;
        private List<T> validateList;

        private void DoValidate()
        {
            DoValidate(0);
        }

        private void DoValidate(int start)
        {
            int validateListCount = validateList != null ? validateList.Count : 0;
            if (this.count != validateListCount)
            {
                Debug.Assert(false);
            }
            for (int i = start; i < count; i++)
            {
                T[] vector;
                int offset;
                GetEffectiveAddress(i, out vector, out offset);
                if (!vector[offset].Equals(validateList[i]))
                {
                    Debug.Assert(false);
                }
            }
        }
#endif

        public int Count
        {
            get
            {
                return count;
            }
        }

        public T this[int index]
        {
            get
            {
                T[] vector;
                int offset;
                GetEffectiveAddress(index, out vector, out offset);
#if DEBUG
                if (Validate)
                {
                    if (validateList == null)
                    {
                        validateList = new List<T>();
                    }
                    Debug.Assert(validateList[index].Equals(vector[offset]));
                }
#endif
                return vector[offset];
            }
            set
            {
                T[] vector;
                int offset;
                GetEffectiveAddress(index, out vector, out offset);
#if DEBUG
                if (Validate)
                {
                    if (validateList == null)
                    {
                        validateList = new List<T>();
                    }
                    validateList[index] = value;
                }
#endif
                vector[offset] = value;
            }
        }

        private void GetEffectiveAddress(int index, out T[] vector, out int offset)
        {
            if (unchecked((uint)index) >= unchecked((uint)this.count))
            {
                throw new ArgumentOutOfRangeException();
            }
            Debug.Assert(!((simpleArray != null) && (fragmentArray != null)));
            if (simpleArray != null)
            {
                vector = simpleArray;
                offset = index;
            }
            else
            {
                vector = fragmentArray[index / BlockSize];
                offset = index & (BlockSize - 1);
            }
        }

        public void Add(T item)
        {
            Insert(this.count, item);
#if DEBUG
            if (Validate)
            {
                DoValidate();
            }
#endif
        }

        public void Insert(int index, T item)
        {
            InsertRange(index, new T[1] { item });
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            T[] collectionArray = collection as T[];
            if (collectionArray != null)
            {
                InsertRange(index, collectionArray, 0, collectionArray.Length);
                return;
            }

            T[] buffer = new T[4];
            int count = 0;
            foreach (T item in collection)
            {
                buffer[count++] = item;
                if (count == buffer.Length)
                {
                    InsertRange(index, buffer, 0, buffer.Length);
                    index += buffer.Length;
                    count = 0;
                    if (buffer.Length < BlockSize)
                    {
                        Array.Resize(ref buffer, buffer.Length * 2);
                    }
                }
            }
            if (count != 0)
            {
                InsertRange(index, buffer, 0, count);
            }
        }

        public void InsertRange(int index, T[] collection, int offset, int count)
        {
            if ((unchecked((uint)index) > unchecked((uint)this.count)) || (count < 0))
            {
                throw new ArgumentOutOfRangeException();
            }
            if (count == 0)
            {
                return;
            }
            Debug.Assert(!((simpleArray != null) && (fragmentArray != null)));

            int after = this.count - index;

            EnsureCapacity(this.count + count);
            this.count += count;

            for (int i = after - 1; i >= 0; i--)
            {
                T[] vector;
                int vectorOffset;
                GetEffectiveAddress(i + index, out vector, out vectorOffset);
                T t = vector[vectorOffset];
                GetEffectiveAddress(i + index + count, out vector, out vectorOffset);
                vector[vectorOffset] = t;
            }
            for (int i = 0; i < count; i++)
            {
                T[] vector;
                int vectorOffset;
                GetEffectiveAddress(i + index, out vector, out vectorOffset);
                vector[vectorOffset] = collection[i + offset];
            }
#if DEBUG
            if (Validate)
            {
                if (validateList == null)
                {
                    validateList = new List<T>();
                }
                List<T> add = new List<T>();
                for (int i = 0; i < count; i++)
                {
                    add.Add(collection[i + offset]);
                }
                validateList.InsertRange(index, add);
                DoValidate(index);
            }
#endif
        }

        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        public void RemoveRange(int index, int count)
        {
            if ((count < 0) || (unchecked((uint)(index + count)) > unchecked((uint)this.count)))
            {
                throw new ArgumentOutOfRangeException();
            }
            Debug.Assert(!((simpleArray != null) && (fragmentArray != null)));
            for (int i = index; i < this.count - count; i++)
            {
                T[] vector;
                int vectorOffset;
                GetEffectiveAddress(i + count, out vector, out vectorOffset);
                T t = vector[vectorOffset];
                GetEffectiveAddress(i, out vector, out vectorOffset);
                vector[vectorOffset] = t;
            }
            for (int i = this.count - count; i < this.count; i++)
            {
                // clear dead slots for garbage collector
                T[] vector;
                int vectorOffset;
                GetEffectiveAddress(i, out vector, out vectorOffset);
                vector[vectorOffset] = default(T);
            }
            this.count -= count;
#if DEBUG
            if (Validate)
            {
                if (validateList == null)
                {
                    validateList = new List<T>();
                }
                validateList.RemoveRange(index, count);
                DoValidate(index);
            }
#endif
        }

        private int Capacity
        {
            get
            {
                Debug.Assert(!((simpleArray != null) && (fragmentArray != null)));
                if (simpleArray != null)
                {
                    return simpleArray.Length;
                }
                else if (fragmentArray != null)
                {
                    return fragmentArray.Length * BlockSize;
                }
                return 0;
            }
        }

        private void SetCapacity(int capacity)
        {
            Debug.Assert(!((simpleArray != null) && (fragmentArray != null)));
            if (capacity <= BlockSize)
            {
                if (simpleArray == null)
                {
                    if (fragmentArray != null)
                    {
                        simpleArray = fragmentArray[0];
                    }
                    else
                    {
                        simpleArray = new T[capacity];
                    }
                    fragmentArray = null;
                }

                if (simpleArray.Length < capacity)
                {
                    int up = capacity;
                    up |= up >> 1;
                    up |= up >> 2;
                    up |= up >> 4;
                    up |= up >> 8;
                    up |= up >> 16;
                    up = up + 1;
                    Array.Resize(ref simpleArray, Math.Min(up, BlockSize));
                }
            }
            else
            {
                int oldEnd = fragmentArray != null ? fragmentArray.Length * BlockSize : 0;

                int c = fragmentArray != null ? fragmentArray.Length * BlockSize : 0;
                if (c < capacity)
                {
                    c = (capacity + (BlockSize - 1)) & ~(BlockSize - 1); // round up
                }

                if (fragmentArray == null)
                {
                    fragmentArray = new T[c / BlockSize][];
                    fragmentArray[0] = simpleArray;
                    simpleArray = null;
                    Array.Resize(ref fragmentArray[0], BlockSize);
                }
                else
                {
                    Debug.Assert(simpleArray == null);
                    Array.Resize(ref fragmentArray, c / BlockSize);
                }

                for (int i = oldEnd; i < c; i += BlockSize)
                {
                    if (fragmentArray[i / BlockSize] == null)
                    {
                        fragmentArray[i / BlockSize] = new T[BlockSize];
                    }
                }
            }
            Debug.Assert(!((simpleArray != null) && (fragmentArray != null)));
#if DEBUG
            if (Validate)
            {
                DoValidate();
            }
#endif
        }

        private void EnsureCapacity(int capacity)
        {
            if (capacity > Capacity)
            {
                SetCapacity(capacity);
            }
        }

        public void Clear()
        {
            Debug.Assert(!((simpleArray != null) && (fragmentArray != null)));
            for (int i = 0; i < count; i++)
            {
                // clear dead slots for garbage collector
                T[] vector;
                int vectorOffset;
                GetEffectiveAddress(i, out vector, out vectorOffset);
                vector[vectorOffset] = default(T);
            }
            count = 0;
#if DEBUG
            if (Validate)
            {
                if (validateList == null)
                {
                    validateList = new List<T>();
                }
                validateList.Clear();
                DoValidate();
            }
#endif
        }
    }
}
