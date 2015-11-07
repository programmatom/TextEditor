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
    public class SplayVector<T>
    {
        public const int DefaultTargetBlockSize = 4096;

        private SplaySparseArray<T[]> tree = new SplaySparseArray<T[]>();
        private int targetBlockSize;
        private bool internalFragmentation;

        public SplayVector()
        {
            this.targetBlockSize = Math.Max(2, DefaultTargetBlockSize / Marshal.SizeOf(typeof(T)));
        }

        public SplayVector(int targetBlockSize)
        {
            this.targetBlockSize = targetBlockSize;
        }

        public SplayVector(int targetBlockSize, bool internalFragmentation)
        {
            this.targetBlockSize = targetBlockSize;
            this.internalFragmentation = internalFragmentation;
        }

        public int TargetBlockSize
        {
            get
            {
                return targetBlockSize;
            }
        }

        public int Count
        {
            get
            {
                return tree.Count;
            }
        }

        private T[] Select(int index, out int offset, out int count)
        {
            if (unchecked((uint)index) >= unchecked((uint)tree.Count))
            {
                throw new ArgumentException();
            }

            int start;
            T[] segment;
            tree.NearestLessOrEqualCountValue(index, out start, out count, out segment);
            offset = index - start;
            return segment;
        }

        public T this[int index]
        {
            get
            {
                int offset, unused;
                T[] segment = Select(index, out offset, out unused);
                return segment[offset];
            }

            set
            {
                int offset, unused;
                T[] segment = Select(index, out offset, out unused);
                segment[offset] = value;
            }
        }

        private void SplitAt(int index)
        {
            int start;
            tree.NearestLessOrEqual(index, out start);
            if (start < index)
            {
                int segmentCount;
                T[] segment;
                tree.GetCountValue(start, out segmentCount, out segment);

                int countL = index - start;
                T[] segmentL;
                if (!internalFragmentation)
                {
                    segmentL = new T[countL];
                    Array.Copy(segment, 0, segmentL, 0, countL);
                }
                else
                {
                    segmentL = segment;
                }

                int countR = segmentCount - countL;
                T[] segmentR = new T[!internalFragmentation ? countR : targetBlockSize];
                Array.Copy(segment, countL, segmentR, 0, countR);

                tree.RemoveRange(start, segmentCount);
                tree.InsertRange(start, countL, segmentL);
                tree.InsertRange(start + countL, countR, segmentR);
            }
        }

        private bool TryJoinNext(int index)
        {
            int countL;
            T[] segmentL;
            tree.GetCountValue(index, out countL, out segmentL);

            int countR;
            T[] segmentR;
            tree.GetCountValue(index + countL, out countR, out segmentR);

            if (countL + countR <= (!internalFragmentation ? targetBlockSize : targetBlockSize / 2))
            {
                T[] segment;
                if (!internalFragmentation)
                {
                    segment = new T[countL + countR];
                    Array.Copy(segmentL, 0, segment, 0, countL);
                    Array.Copy(segmentR, 0, segment, countL, countR);
                }
                else
                {
                    segment = segmentL;
                    Array.Copy(segmentR, 0, segmentL, countL, countR);
                }
                tree.RemoveRange(index, countL);
                tree.RemoveRange(index, countR);
                tree.InsertRange(index, countL + countR, segment);
                return true;
            }
            return false;
        }

        private void Join(int index)
        {
            int start;
            tree.NearestLessOrEqual(index, out start);
            if (start != index)
            {
                return;
            }

            if (index > 0)
            {
                int previous;
                tree.Previous(index, out previous);
                if (TryJoinNext(previous))
                {
                    index = previous;
                }
            }
            int count = tree.GetCount(index);
            if (index + count < Count)
            {
                TryJoinNext(index);
            }
        }

        public void InsertRange(int index, int count)
        {
            if ((unchecked((uint)index) > unchecked((uint)tree.Count)) || (count < 0))
            {
                throw new ArgumentException();
            }

            bool simple = false;
            int start = 0;
            int segmentCount = 0;
            T[] segment = null;
            if (internalFragmentation)
            {
                tree.NearestLessOrEqualCountValue(index, out start, out segmentCount, out segment);
                simple = segmentCount + count > targetBlockSize;
            }

            if (!internalFragmentation || simple)
            {
                SplitAt(index);

                int remaining = count;
                while (remaining > 0)
                {
                    int effectiveCount = Math.Min(remaining, targetBlockSize);
                    segment = new T[!internalFragmentation ? effectiveCount : targetBlockSize]; // zeroed automatically
                    tree.InsertRange(index, effectiveCount, segment);
                    remaining -= effectiveCount;
                }
            }
            else
            {
                if (segment == null)
                {
                    segment = new T[targetBlockSize];
                }
                int offset = index - start;
                Array.Copy(segment, offset, segment, offset + count, segmentCount - offset);
                Array.Clear(segment, offset, count); // zero new element range
                tree.RemoveRange(start, segmentCount);
                tree.InsertRange(start, segmentCount + count, segment);
            }

            Join(index);
            Join(index + count);
        }

        public void InsertRange(int index, T[] items, int offset, int count)
        {
            InsertRange(
                index,
                count);
            IterateRangeBatch(
                index,
                items,
                offset,
                count,
                delegate(T[] v, int vOffset, T[] x, int xOffset, int count1)
                {
                    Array.Copy(x, xOffset, v, vOffset, count1);
                });
        }

        public void InsertRange(int index, T[] items)
        {
            InsertRange(index, items, 0, items.Length);
        }

        public void Insert(int index, T item)
        {
            // TODO: optimize
            InsertRange(index, new T[1] { item }, 0, 1);
        }

        public void RemoveRange(int index, int count)
        {
            if ((index < 0) || (count < 0) || (index + count > Count))
            {
                throw new ArgumentException();
            }

            if (!internalFragmentation)
            {
                SplitAt(index);
                SplitAt(index + count);

                while (count > 0)
                {
                    int segmentCount = tree.GetCount(index);
                    tree.RemoveRange(index, segmentCount);
                    count -= segmentCount;
                }
            }
            else
            {
                int remaining = count;
                while (remaining > 0)
                {
                    int start;
                    int segmentCount;
                    T[] segment;
                    tree.NearestLessOrEqualCountValue(index, out start, out segmentCount, out segment);
                    if (start < index)
                    {
                        int offset = index - start;
                        int removedOne = Math.Min(remaining, segmentCount - offset);
                        Array.Copy(segment, offset + removedOne, segment, offset, segmentCount - removedOne - offset);
                        Array.Clear(segment, segmentCount - removedOne, removedOne); // zero old range to allow gc to reclaim any formerly referenced elements
                        tree.RemoveRange(start, segmentCount);
                        tree.InsertRange(start, segmentCount - removedOne, segment);
                        remaining -= removedOne;
                    }
                    else if (remaining >= segmentCount)
                    {
                        tree.RemoveRange(index, segmentCount);
                        remaining -= segmentCount;
                    }
                    else
                    {
                        Array.Copy(segment, remaining, segment, 0, segmentCount - remaining);
                        Array.Clear(segment, segmentCount - remaining, remaining); // zero old range to allow gc to reclaim any formerly referenced elements
                        tree.RemoveRange(start, segmentCount);
                        tree.InsertRange(start, segmentCount - remaining, segment);
                        remaining -= remaining;
                    }
                }
            }

            Join(index);
        }

        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        public void ReplaceRange(int index, int count, T[] items, int offset, int count2)
        {
            // TODO: optimize
            RemoveRange(index, count);
            InsertRange(index, items, offset, count2);
        }

        public void ReplaceRange(int index, int count, T[] items)
        {
            ReplaceRange(index, count, items, 0, items.Length);
        }

        public void Clear()
        {
            RemoveRange(0, Count);
        }

        public void CopyIn(int index, T[] items, int offset, int count)
        {
            IterateRangeBatch(
                index,
                items,
                offset,
                count,
                delegate(T[] v, int vOffset, T[] x, int xOffset, int count1)
                {
                    Array.Copy(x, xOffset, v, vOffset, count1);
                });
        }

        public void CopyIn(int index, T[] items)
        {
            CopyIn(index, items, 0, items.Length);
        }

        public void CopyOut(int index, T[] items, int offset, int count)
        {
            IterateRangeBatch(
                index,
                items,
                offset,
                count,
                delegate(T[] v, int vOffset, T[] x, int xOffset, int count1)
                {
                    Array.Copy(v, vOffset, x, xOffset, count1);
                });
        }

        public void CopyOut(int index, T[] items)
        {
            CopyOut(index, items, 0, items.Length);
        }

        public delegate void IterateOperator(ref T v, ref T x);
        public void IterateRange(int index, T[] external, int externalOffset, int count, IterateOperator op)
        {
            IterateRangeBatch(
                index,
                external,
                externalOffset,
                count,
                delegate(T[] v, int vOffset, T[] x, int xOffset, int count1)
                {
                    for (int i = 0; i < count1; i++)
                    {
                        op(ref v[i + vOffset], ref x[i + xOffset]);
                    }
                });
        }

        public delegate void IterateOperatorBatch(T[] v, int vOffset, T[] x, int xOffset, int count);
        public void IterateRangeBatch(int index, T[] external, int externalOffset, int count, IterateOperatorBatch op)
        {
            if (index + count > this.Count)
            {
                Debug.Assert(false);
                throw new ArgumentOutOfRangeException();
            }

            int j = externalOffset;
            while (count > 0)
            {
                int start;
                tree.NearestLessOrEqual(index, out start);
                int offset = index - start;

                int segmentCount;
                T[] segment;
                tree.GetCountValue(start, out segmentCount, out segment);

                int contiguous = Math.Min(count, segmentCount - offset);
                op(segment, offset, external, j, contiguous);
                j += contiguous;

                count -= contiguous;
                index += contiguous;
            }
        }

        public delegate int Comparer(T l, T r);
        public int BinarySearch(T value, int start, int count, bool multi, Comparer comparer)
        {
            int lower = start;
            int upper = start + count - 1;
            while (lower <= upper)
            {
                int middle = (upper + lower) / 2;

                int c = comparer(this[middle], value);
                if (c == 0)
                {
                    if (multi)
                    {
                        while ((middle > start) && (0 == comparer(this[middle - 1], value)))
                        {
                            middle--;
                        }
                    }
                    return middle;
                }
                else if (c < 0)
                {
                    lower = middle + 1;
                }
                else
                {
                    upper = middle - 1;
                }
            }
            return ~lower;
        }

        public int IndexOfAny(T[] value, int start, int count)
        {
            if (unchecked((uint)start > (uint)tree.Count)
                || unchecked((uint)start + (uint)count > (uint)tree.Count))
            {
                throw new ArgumentException();
            }

            while (count != 0)
            {
                int offset, segmentLength;
                T[] segment = Select(start, out offset, out segmentLength);

                int c = Math.Min(segmentLength - offset, count);
                int bestIndex = segmentLength;
                for (int i = 0; i < value.Length; i++)
                {
                    int index = Array.IndexOf<T>(segment, value[i], offset, c);
                    if ((index >= 0) && (bestIndex > index))
                    {
                        bestIndex = index;
                    }
                }
                if (bestIndex < segmentLength)
                {
                    return start + bestIndex - offset;
                }

                start += c;
                count -= c;
            }

            return -1;
        }

        public int IndexOf(T value, int start, int count)
        {
            if (unchecked((uint)start > (uint)tree.Count)
                || unchecked((uint)start + (uint)count > (uint)tree.Count))
            {
                throw new ArgumentException();
            }

            while (count != 0)
            {
                int offset, segmentLength;
                T[] segment = Select(start, out offset, out segmentLength);

                int c = Math.Min(segmentLength - offset, count);
                int index = Array.IndexOf<T>(segment, value, offset, c);
                if (index >= 0)
                {
                    return start + index - offset;
                }

                start += c;
                count -= c;
            }

            return -1;
        }

        // Same semantics as Array.LastIndexOf: The one-dimensional Array is searched backward starting at
        // startIndex and ending at startIndex minus count plus 1, if count is greater than 0.
        public int LastIndexOfAny(T[] value, int end, int count)
        {
            if (unchecked((uint)end > (uint)tree.Count)
                || unchecked((uint)end - (uint)count + 1 > (uint)tree.Count))
            {
                throw new ArgumentException();
            }

            while (count != 0)
            {
                int offset, segmentLength;
                T[] segment = Select(end, out offset, out segmentLength);

                int c = Math.Min(offset + 1, count);
                int bestIndex = -1;
                for (int i = 0; i < value.Length; i++)
                {
                    int index = Array.LastIndexOf<T>(segment, value[i], offset, c);
                    if (bestIndex < index)
                    {
                        bestIndex = index;
                    }
                }
                if (bestIndex >= 0)
                {
                    return bestIndex - c + end + 1;
                }

                end -= c;
                count -= c;
            }

            return -1;
        }

        // Same semantics as Array.LastIndexOf: The one-dimensional Array is searched backward starting at
        // startIndex and ending at startIndex minus count plus 1, if count is greater than 0.
        public int LastIndexOf(T value, int end, int count)
        {
            if (unchecked((uint)end > (uint)tree.Count)
                || unchecked((uint)end - (uint)count + 1 > (uint)tree.Count))
            {
                throw new ArgumentException();
            }

            while (count != 0)
            {
                int offset, segmentLength;
                T[] segment = Select(end, out offset, out segmentLength);

                int c = Math.Min(offset + 1, count);
                int index = Array.LastIndexOf<T>(segment, value, offset, c);
                if (index >= 0)
                {
                    return index - c + end + 1;
                }

                end -= c;
                count -= c;
            }

            return -1;
        }
    }
}
