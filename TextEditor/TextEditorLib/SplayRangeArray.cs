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

namespace TextEditor
{
    // An adaptation of SplayTree to provide a fast array of ranges with cumulative offsets

    // http://www.link.cs.cmu.edu/link/ftp-site/splaying/top-down-splay.c
    //
    //            An implementation of top-down splaying
    //                D. Sleator <sleator@cs.cmu.edu>
    //                         March 1992
    //
    //"Splay trees", or "self-adjusting search trees" are a simple and
    //efficient data structure for storing an ordered set.  The data
    //structure consists of a binary tree, without parent pointers, and no
    //additional fields.  It allows searching, insertion, deletion,
    //deletemin, deletemax, splitting, joining, and many other operations,
    //all with amortized logarithmic performance.  Since the trees adapt to
    //the sequence of requests, their performance on real access patterns is
    //typically even better.  Splay trees are described in a number of texts
    //and papers [1,2,3,4,5].
    //
    //The code here is adapted from simple top-down splay, at the bottom of
    //page 669 of [3].  It can be obtained via anonymous ftp from
    //spade.pc.cs.cmu.edu in directory /usr/sleator/public.
    //
    //The chief modification here is that the splay operation works even if the
    //item being splayed is not in the tree, and even if the tree root of the
    //tree is NULL.  So the line:
    //
    //                          t = splay(i, t);
    //
    //causes it to search for item with key i in the tree rooted at t.  If it's
    //there, it is splayed to the root.  If it isn't there, then the node put
    //at the root is the last one before NULL that would have been reached in a
    //normal binary search for i.  (It's a neighbor of i in the tree.)  This
    //allows many other operations to be easily implemented, as shown below.
    //
    //[1] "Fundamentals of data structures in C", Horowitz, Sahni,
    //   and Anderson-Freed, Computer Science Press, pp 542-547.
    //[2] "Data Structures and Their Algorithms", Lewis and Denenberg,
    //   Harper Collins, 1991, pp 243-251.
    //[3] "Self-adjusting Binary Search Trees" Sleator and Tarjan,
    //   JACM Volume 32, No 3, July 1985, pp 652-686.
    //[4] "Data Structure and Algorithm Analysis", Mark Weiss,
    //   Benjamin Cummins, 1992, pp 119-130.
    //[5] "Data Structures, Algorithms, and Performance", Derick Wood,
    //   Addison-Wesley, 1993, pp 367-375.
    public class SplayRangeArray
    {
        private readonly SplayNode Nil;

        private SplayNode root;

        private class SplayNode
        {
            public SplayNode left;
            public SplayNode right;

            public int sparseTotalLength; // total size of tree
            public int sparseNodeLength; // size of only this node

            public int totalCount; // total cardinality of tree
        }

        public SplayRangeArray()
        {
            Nil = new SplayNode();
            Nil.left = Nil;
            Nil.right = Nil;
            root = Nil;
        }

        public SplayRangeArray(SplayRangeArray original)
        {
            throw new NotImplementedException("clone is not implemented");
        }

        public int TotalSparseLength
        {
            get
            {
                return root.sparseTotalLength;
            }
        }

        public int Count
        {
            get
            {
                return root.totalCount;
            }
        }

        public void Insert(int index, int sparseLength)
        {
            if ((sparseLength < 0) || (unchecked((uint)index) > (uint)root.totalCount))
            {
                throw new ArgumentException();
            }

            Splay(ref root, index);
            int rootIndex = root.left.totalCount;
            Debug.Assert((index == rootIndex) || ((index == rootIndex + 1) && (root.right == Nil)));

            if (index == rootIndex)
            {
                // insert item just in front of current root

                SplayNode i = new SplayNode();
                i.left = Nil;
                i.right = Nil;
                i.sparseTotalLength = sparseLength;
                i.sparseNodeLength = sparseLength;
                i.totalCount = 1;

                int leftSparseSize = root.left.sparseTotalLength;
                int leftDenseSize = root.left.totalCount;
                i.left = root.left;
                i.right = root;
                i.sparseTotalLength += root.sparseTotalLength;
                i.totalCount += root.totalCount;

                root.left = Nil;
                root.sparseTotalLength -= leftSparseSize;
                root.totalCount -= leftDenseSize;

                root = i;
            }
            else
            {
                Debug.Assert(root.right == Nil);
                Debug.Assert(index == root.totalCount); // if not in tree, then must be next element after highest in tree

                // append

                SplayNode i = new SplayNode();
                i.left = Nil;
                i.right = Nil;
                i.sparseTotalLength = sparseLength;
                i.sparseNodeLength = sparseLength;
                i.totalCount = 1;

                if (root != Nil)
                {
                    root.right = i;
                    root.sparseTotalLength += i.sparseTotalLength;
                    root.totalCount += i.totalCount;
                }
                else
                {
                    root = i;
                }
            }
        }

        public void RemoveAt(int index)
        {
            if (unchecked((uint)index >= unchecked((uint)root.totalCount)))
            {
                throw new ArgumentException();
            }

            Splay(ref root, index);
            int rootIndex = root.left.totalCount;
            Debug.Assert(index == rootIndex);

            SplayRemove(root, ref root);
        }

        public void RemoveRange(int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                RemoveAt(index);
            }
        }

        public void SetSparseLength(int index, int count)
        {
            RemoveAt(index);
            Insert(index, count);
        }

        public int GetSparseLength(int index)
        {
            if (unchecked((uint)index) >= unchecked((uint)root.totalCount))
            {
                throw new ArgumentException();
            }

            Splay(ref root, index);
            int rootIndex = root.left.totalCount;
            Debug.Assert(index == rootIndex);

            return root.sparseNodeLength;
        }

        public int GetSparseEndBoundAt(int index)
        {
            if (unchecked((uint)index) >= unchecked((uint)root.totalCount))
            {
                throw new ArgumentException();
            }

            Splay(ref root, index);
            int rootIndex = root.left.totalCount;
            Debug.Assert(index == rootIndex);

            return root.left.sparseTotalLength + root.sparseNodeLength;
        }

#if DEBUG
        public delegate void Traverse2Action(int totalCount, int sparseTotalLength, int sparseNodeLength);

        public void Traverse2(ref int maxDepth, int limitDepth, Traverse2Action action)
        {
            Traverse2(action, root, 0, ref maxDepth, limitDepth);
        }

        private void Traverse2(Traverse2Action action, SplayNode n, int depth, ref int maxDepth, int limitDepth)
        {
            if (maxDepth < depth)
            {
                maxDepth = depth;
                //Debugger.Log(0, null, String.Format("Traverse2 Stack Depth: {0}" + Environment.NewLine, depth));
            }
            if (depth > limitDepth)
            {
                throw new ApplicationException("SplaySparseArray.Traverse2: stack overflow anticipated");
            }
            if (n.left != n)
            {
                Traverse2(action, n.left, depth + 1, ref maxDepth, limitDepth);
            }
            if (n != Nil)
            {
                action(n.totalCount, n.sparseTotalLength, n.sparseNodeLength);
            }
            if (n.right != n)
            {
                Traverse2(action, n.right, depth + 1, ref maxDepth, limitDepth);
            }
        }
#endif

        private delegate int SplayCompare(SplayNode r);
        private void Splay(
            ref SplayNode root,
            int index)
        {
            SplayNode N = new SplayNode();
            SplayNode t;
            SplayNode l;
            SplayNode r;
            SplayNode y;

            t = root;
            if (t == Nil)
            {
                return;
            }
            N.left = Nil;
            N.right = Nil;

            l = N;
            int lSparseTotalLength = 0;
            int lTotalCount = 0;
            r = N;
            int rSparseTotalLength = 0;
            int rTotalCount = 0;

            while (true)
            {
                int c;

                c = index.CompareTo(t.left.totalCount);
                if (c < 0)
                {
                    if (t.left == Nil)
                    {
                        break;
                    }
                    c = index.CompareTo(t.left.left.totalCount);
                    if (c < 0)
                    {
                        // rotate right
                        y = t.left;
                        t.left = y.right;
                        y.right = t;
                        t.sparseTotalLength = t.left.sparseTotalLength + t.right.sparseTotalLength + t.sparseNodeLength;
                        t.totalCount = t.left.totalCount + t.right.totalCount + 1;
                        t = y;
                        if (t.left == Nil)
                        {
                            break;
                        }
                    }
                    /* link right */
                    r.left = t;
                    r = t;
                    t = t.left;
                    rSparseTotalLength += r.sparseNodeLength + r.right.sparseTotalLength;
                    rTotalCount += 1 + r.right.totalCount;
                }
                else if (c > 0)
                {
                    if (t.right == Nil)
                    {
                        break;
                    }
                    c = index.CompareTo(t.left.totalCount + 1 + t.right.left.totalCount);
                    if (c > 0)
                    {
                        // rotate left
                        y = t.right;
                        t.right = y.left;
                        y.left = t;
                        t.sparseTotalLength = t.left.sparseTotalLength + t.right.sparseTotalLength + t.sparseNodeLength;
                        t.totalCount = t.left.totalCount + t.right.totalCount + 1;
                        t = y;
                        if (t.right == Nil)
                        {
                            break;
                        }
                    }
                    // link left
                    l.right = t;
                    l = t;
                    index -= t.left.totalCount + 1;
                    t = t.right;
                    lSparseTotalLength += l.sparseNodeLength + l.left.sparseTotalLength;
                    lTotalCount += 1 + l.left.totalCount;
                }
                else
                {
                    break;
                }
            }
            lSparseTotalLength += t.left.sparseTotalLength;  // Now lsize and rsize are the sizes of
            lTotalCount += t.left.totalCount;
            rSparseTotalLength += t.right.sparseTotalLength; // the left and right trees we just built.
            rTotalCount += t.right.totalCount;
            t.sparseTotalLength = lSparseTotalLength + rSparseTotalLength + t.sparseNodeLength;
            t.totalCount = lTotalCount + rTotalCount + 1;
            l.right = Nil;
            r.left = Nil;
            // The following two loops correct the size fields of the right path
            // from the left child of the root and the left path from the right
            // child of the root.
            for (y = N.right; y != Nil; y = y.right)
            {
                y.sparseTotalLength = lSparseTotalLength;
                y.totalCount = lTotalCount;
                lSparseTotalLength -= y.sparseNodeLength + y.left.sparseTotalLength;
                lTotalCount -= 1 + y.left.totalCount;
            }
            for (y = N.left; y != Nil; y = y.left)
            {
                y.sparseTotalLength = rSparseTotalLength;
                y.totalCount = rTotalCount;
                rSparseTotalLength -= y.sparseNodeLength + y.right.sparseTotalLength;
                rTotalCount -= 1 + y.right.totalCount;
            }
            // reassemble
            l.right = t.left;
            r.left = t.right;
            t.left = N.right;
            t.right = N.left;
            root = t;
        }

        // remove node from tree.
        private void SplayRemove(
            SplayNode i,
            ref SplayNode root)
        {
            Debug.Assert(root != Nil);
            int index = i.left.totalCount;
            Splay(ref root, index);
            Debug.Assert(i.totalCount.CompareTo(root.totalCount) == 0);

            SplayNode x;

            if (i.left == Nil)
            {
                x = root.right;
            }
            else
            {
                x = root.left;
                Splay(ref x, index);
                x.right = root.right;
            }
            if (x != Nil)
            {
                x.sparseTotalLength = root.sparseTotalLength - root.sparseNodeLength;
                x.totalCount = root.totalCount - 1;
            }
            root = x;
        }

        public void Clear()
        {
            root = Nil;
        }
    }
}
