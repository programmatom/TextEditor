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
    public class LineWidthCache
    {
        private const int MaxCount = 1000;
        private static bool EnableLogging = false; // TODO: make 'const' once code is stabilized

        private int start;
        private readonly List<int> widths = new List<int>(MaxCount); // as 1s-complement, so default (0) can mean invalid

        public void Clear()
        {
#if DEBUG
            if (EnableLogging)
            {
                Debugger.Log(0, "TextViewControl.LineWidthCache", "LineWidthCache.Clear()" + Environment.NewLine);
            }
#endif
            start = 0;
            widths.Clear();
        }

        // Commentary on InsertRange(..., new int[count]) below:
        // It's a pity they don't support InsertRangeDefaultValue() or something to make space and zero-init.
        // One could write a special enumerator that returns 'count' zeroes. However, as the interface takes an IEnumerable,
        // that involves allocating TWO objects, each of which having at least one 'int' member variable and customary
        // heap overhead. So it turns out to be cheaper in the common case (inserting a line or two) to just do
        // 'new int[count]'. In the worst case, the allocated array will never exceed MaxCount elements. And there is less
        // code to have bugs in.

        public void Set(int index, int width)
        {
#if DEBUG
            if (EnableLogging)
            {
                Debugger.Log(0, "TextViewControl.LineWidthCache", String.Format("LineWidthCache.Set(index={0}, width={1})" + Environment.NewLine, index, width));
            }
#endif

            Debug.Assert(width >= 0);
            if ((widths.Count != 0) && ((index - start >= MaxCount) || (start + widths.Count - index >= MaxCount)))
            {
                Clear();
            }

            if (widths.Count == 0)
            {
                start = index;
                widths.Add(~width);
            }
            else
            {
                if (index < start)
                {
                    widths.InsertRange(0, new int[start - index]);
                    start = index;
                }
                else if (index < start + widths.Count)
                {
                }
                else
                {
                    widths.AddRange(new int[index + 1 - (start + widths.Count)]);
                }
                widths[index - start] = ~width;
            }
#if DEBUG
            if (EnableLogging)
            {
                Dump();
            }
#endif
        }

        public void Invalidate(int index)
        {
#if DEBUG
            if (EnableLogging)
            {
                Debugger.Log(0, "TextViewControl.LineWidthCache", String.Format("LineWidthCache.Invalidate(index={0})" + Environment.NewLine, index));
            }
#endif

            if (index < start)
            {
            }
            else if (index < start + widths.Count)
            {
                widths[index - start] = 0;
            }
            else
            {
            }
#if DEBUG
            if (EnableLogging)
            {
                Dump();
            }
#endif
        }

        public bool TryGet(int index, out int width)
        {
            bool result = false;
            width = 0;
            if (index < start)
            {
            }
            else if (index < start + widths.Count)
            {
                int w = widths[index - start];
                if (~w >= 0)
                {
                    width = ~w;
                    result = true;
                }
            }
            else
            {
            }
            return result;
        }

        public void Insert(int index, int count)
        {
#if DEBUG
            if (EnableLogging)
            {
                Debugger.Log(0, "TextViewControl.LineWidthCache", String.Format("LineWidthCache.Insert(index={0}, count={1})" + Environment.NewLine, index, count));
            }
#endif

            if (widths.Count + count > MaxCount)
            {
                Clear();
            }
            else
            {
                if (index <= start)
                {
                    start += count;
                }
                else if (index <= start + widths.Count)
                {
                    widths.InsertRange(index - start, new int[count]);
                }
                else
                {
                }
            }
#if DEBUG
            if (EnableLogging)
            {
                Dump();
            }
#endif
        }

        public void Delete(int index, int count)
        {
#if DEBUG
            if (EnableLogging)
            {
                Debugger.Log(0, "TextViewControl.LineWidthCache", String.Format("LineWidthCache.Delete(index={0}, count={1})" + Environment.NewLine, index, count));
            }
#endif

            if (index + count <= start)
            {
                start -= count;
            }
            else if (index <= start)
            {
                int before = start - index;
                int after = count - before;
                widths.RemoveRange(start, after);
                start -= before;
            }
            else if (index < start + widths.Count)
            {
                int within = widths.Count - (index - start);
                if (within > count)
                {
                    within = count;
                }
                widths.RemoveRange(index - start, within);
            }
            else
            {
            }
#if DEBUG
            if (EnableLogging)
            {
                Dump();
            }
#endif
        }

#if DEBUG
        private void Dump()
        {
            Debug.Assert(EnableLogging);
            Debugger.Log(0, "TextViewControl.LineWidthCache", String.Format("LineWidthCache: start={0}, count={1}" + Environment.NewLine, start, widths.Count));
            StringBuilder indices = new StringBuilder();
            StringBuilder values = new StringBuilder();
            for (int i = 0; i < widths.Count; i++)
            {
                indices.AppendFormat("{0,-8}", i + start);
                values.AppendFormat("{0,-8}", widths[i] != 0 ? (~widths[i]).ToString() : "inv");
            }
            indices.AppendLine();
            values.AppendLine();
            Debugger.Log(0, "TextViewControl.LineWidthCache", indices.ToString());
            Debugger.Log(0, "TextViewControl.LineWidthCache", values.ToString());
        }
#endif
    }
}
