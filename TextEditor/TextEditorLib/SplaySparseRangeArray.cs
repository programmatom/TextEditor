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
    // An adaptation of SplayTree to provide a fast sparse array of sparse cumulative offsets

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
    public class SplaySparseRangeArray
    {
        private readonly SplayNode Nil;

        private SplayNode root;

        private class SplayNode
        {
            public SplayNode left;
            public SplayNode right;

            public int xSize; // total size of tree
            public int xCount1; // local count

            public int ySize; // total size of tree
            public int yCount1; // size of only this node
        }

        public SplaySparseRangeArray()
        {
            Nil = new SplayNode();
            Nil.left = Nil;
            Nil.right = Nil;
            root = Nil;
        }

        private static int Start(SplayNode n)
        {
            return n.left.xSize;
        }

        public int XSize
        {
            get
            {
                return root.xSize;
            }
        }

        public int YSize
        {
            get
            {
                return root.ySize;
            }
        }

        public void Insert(int xStart, int xCount, int yCount)
        {
            //redundant:
            //if ((xStart < 0) || (xCount <= 0))
            //{
            //    throw new ArgumentException();
            //}

            Splay(ref root, xStart);

            if (xStart == Start(root))
            {
                // insert item just in front of current root

                if (xStart != Start(root))
                {
                    Debug.Assert(false);
                    throw new KeyNotFoundException("item not in tree");
                }

                if (xCount == 0)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }

                SplayNode i = new SplayNode();
                i.left = Nil;
                i.right = Nil;
                i.xSize = xCount;
                i.xCount1 = xCount;
                i.ySize = yCount;
                i.yCount1 = yCount;

                int leftXSize = root.left.xSize;
                int leftYSize = root.left.ySize;
                i.left = root.left;
                i.right = root;
                i.xSize += root.xSize;
                i.ySize += root.ySize;

                root.left = Nil;
                root.xSize -= leftXSize;
                root.ySize -= leftYSize;

                root = i;
            }
            else
            {
                // append

                if (root.right != Nil)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException(); // program defect
                }

                if (xCount == 0)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }

                if (xStart != root.xSize)
                {
                    Debug.Assert(false);
                    throw new KeyNotFoundException("item not in tree");
                }

                SplayNode i = new SplayNode();
                i.left = Nil;
                i.right = Nil;
                i.xSize = xCount;
                i.xCount1 = xCount;
                i.ySize = yCount;
                i.yCount1 = yCount;

                if (root != Nil)
                {
                    root.right = i;
                    root.xSize += i.xSize;
                    root.ySize += i.ySize;
                }
                else
                {
                    root = i;
                }
            }
        }

        // might get rid of xCount as redundant, but it's useful for finding errors
        public void Remove(int xStart, int xCount)
        {
            //redundant:
            //if ((xStart < 0) || (xCount <= 0))
            //{
            //    throw new ArgumentException();
            //}

            Splay(ref root, xStart);
            if ((xStart != Start(root)) || ((xCount != 0) && (xCount != root.xCount1)))
            {
                Debug.Assert(false);
                throw new KeyNotFoundException("item not in tree");
            }

            if (xCount == 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            if (!SplayRemove(root, ref root))
            {
                Debug.Assert(false);
                throw new KeyNotFoundException("item not in tree"); // program defect
            }
        }

        public void GetXCountYStartYCount(int xStart, out int xCount, out int yStart, out int yCount)
        {
            //redundant:
            //if (xStart < 0)
            //{
            //    throw new ArgumentException();
            //}

            Splay(ref root, xStart);
            if (xStart != Start(root))
            {
                Debug.Assert(false);
                throw new KeyNotFoundException("item not in tree");
            }

            xCount = root.xCount1;
            yStart = root.left.ySize;
            yCount = root.yCount1;
        }

        public int GetYCount(int xStart)
        {
            //redundant:
            //if (xStart < 0)
            //{
            //    throw new ArgumentException();
            //}

            Splay(ref root, xStart);
            if (xStart != Start(root))
            {
                Debug.Assert(false);
                throw new KeyNotFoundException("item not in tree");
            }
            return root.yCount1;
        }

        public int GetXCount(int xStart)
        {
            //redundant:
            //if (xStart < 0)
            //{
            //    throw new ArgumentException();
            //}

            Splay(ref root, xStart);
            if (xStart != Start(root))
            {
                Debug.Assert(false);
                throw new KeyNotFoundException("item not in tree");
            }
            return root.xCount1;
        }

        public int GetYEndBoundAt(int xStart)
        {
            //redundant:
            //if (xStart < 0)
            //{
            //    throw new ArgumentException();
            //}

            Splay(ref root, xStart);
            if (xStart != Start(root))
            {
                throw new KeyNotFoundException("item not in tree");
            }

            return root.left.ySize + root.yCount1;
        }

        public bool NearestLessOrEqual(int xStart, out int nearestXStart)
        {
            if (xStart < 0)
            {
                throw new ArgumentException();
            }

            if (root == Nil)
            {
                nearestXStart = 0;
                return false;
            }
            Splay(ref root, xStart);
            if (xStart.CompareTo(Start(root)) < 0)
            {
                if (root.left == Nil)
                {
                    if (root.xSize != root.right.xSize + root.xCount1)
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException(); // program defect
                    }
                    nearestXStart = 0;
                    return false;
                }
                int rootStart = Start(root);
                Splay(ref root.left, rootStart);
                nearestXStart = root.left.left.xSize;
                return true;
            }
            else
            {
                nearestXStart = Start(root);
                return true;
            }
        }

        public void NearestLessOrEqualXCountYStartYCount(int xIndex, out int xStart, out int xCount, out int yStart, out int yCount)
        {
            //redundant:
            //if (xIndex < 0)
            //{
            //    Debug.Assert(false);
            //    throw new ArgumentException();
            //}

            if (!NearestLessOrEqual(xIndex, out xStart))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            GetXCountYStartYCount(xStart, out xCount, out yStart, out yCount);
        }

        public void PreviousX(int xStart, out int previousXStart)
        {
            if (xStart < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            NearestLessOrEqual(xStart - 1, out previousXStart);
        }

        public bool NextX(int xStart, out int nextXStart)
        {
            if (xStart < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            Splay(ref root, xStart);
            if (xStart != Start(root))
            {
                Debug.Assert(false);
                throw new KeyNotFoundException("item not in tree");
            }

            nextXStart = xStart + root.xCount1;
            return root.right != Nil;
        }

        private delegate int SplayCompare(SplayNode r);
        private void Splay(
            ref SplayNode root,
            int start)
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
            int lXSize = 0;
            int lYSize = 0;
            r = N;
            int rXSize = 0;
            int rYSize = 0;

            while (true)
            {
                int c;

                c = start.CompareTo(t.left.xSize);
                if (c < 0)
                {
                    if (t.left == Nil)
                    {
                        break;
                    }
                    c = start.CompareTo(t.left.left.xSize);
                    if (c < 0)
                    {
                        // rotate right
                        y = t.left;
                        t.left = y.right;
                        y.right = t;
                        t.xSize = t.left.xSize + t.right.xSize + t.xCount1;
                        t.ySize = t.left.ySize + t.right.ySize + t.yCount1;
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
                    rXSize += r.xCount1 + r.right.xSize;
                    rYSize += r.yCount1 + r.right.ySize;
                }
                else if (c > 0)
                {
                    if (t.right == Nil)
                    {
                        break;
                    }
                    c = start.CompareTo(t.left.xSize + t.xCount1 + t.right.left.xSize);
                    if (c > 0)
                    {
                        // rotate left
                        y = t.right;
                        t.right = y.left;
                        y.left = t;
                        t.xSize = t.left.xSize + t.right.xSize + t.xCount1;
                        t.ySize = t.left.ySize + t.right.ySize + t.yCount1;
                        t = y;
                        if (t.right == Nil)
                        {
                            break;
                        }
                    }
                    // link left
                    l.right = t;
                    l = t;
                    start -= t.left.xSize + t.xCount1;
                    t = t.right;
                    lXSize += l.xCount1 + l.left.xSize;
                    lYSize += l.yCount1 + l.left.ySize;
                }
                else
                {
                    break;
                }
            }
            lXSize += t.left.xSize;  // Now lsize and rsize are the sizes of
            lYSize += t.left.ySize;
            rXSize += t.right.xSize; // the left and right trees we just built.
            rYSize += t.right.ySize;
            t.xSize = lXSize + rXSize + t.xCount1;
            t.ySize = lYSize + rYSize + t.yCount1;
            l.right = Nil;
            r.left = Nil;
            // The following two loops correct the size fields of the right path
            // from the left child of the root and the left path from the right
            // child of the root.
            for (y = N.right; y != Nil; y = y.right)
            {
                y.xSize = lXSize;
                y.ySize = lYSize;
                lXSize -= y.xCount1 + y.left.xSize;
                lYSize -= y.yCount1 + y.left.ySize;
            }
            for (y = N.left; y != Nil; y = y.left)
            {
                y.xSize = rXSize;
                y.ySize = rYSize;
                rXSize -= y.xCount1 + y.right.xSize;
                rYSize -= y.yCount1 + y.right.ySize;
            }
            // reassemble
            l.right = t.left;
            r.left = t.right;
            t.left = N.right;
            t.right = N.left;
            root = t;
        }

        // remove node from tree.  returns true if successful, or false if
        // item was not in the tree.
        private bool SplayRemove(
            SplayNode i,
            ref SplayNode root)
        {
            int c;

            if (root == Nil)
            {
                return false;
            }
            int start = Start(i);
            Splay(ref root, start);
            c = i.xSize.CompareTo(root.xSize);
            if (c == 0)
            {
                SplayNode x;

                if (i.left == Nil)
                {
                    x = root.right;
                }
                else
                {
                    x = root.left;
                    Splay(ref x, start);
                    x.right = root.right;
                }
                if (x != Nil)
                {
                    x.xSize = root.xSize - root.xCount1;
                    x.ySize = root.ySize - root.yCount1;
                }
                root = x;
                return true;
            }
            return false;
        }

        public void Clear()
        {
            root = Nil;
        }

#if DEBUG
        public delegate void Traverse2Action(int count, int sparseLength, int depth);
        public void Traverse2(Traverse2Action action)
        {
            Traverse2(action, root, 0);
        }
        private void Traverse2(Traverse2Action action, SplayNode n, int depth)
        {
            if (n.left != n)
            {
                Traverse2(action, n.left, depth + 1);
            }
            if (n != Nil)
            {
                action(n.xCount1, n.yCount1, depth);
            }
            if (n.right != n)
            {
                Traverse2(action, n.right, depth + 1);
            }
        }

        public void DumpTree()
        {
            StringWriter output = new StringWriter();
            output.WriteLine("Tree:");
            DumpTree(root, 1, output);
            output.WriteLine();
            if (true)
            {
                Console.Write(output.ToString());
            }
            else
            {
                Debugger.Log(0/*level*/, null/*category*/, output.ToString());
            }
        }

        private void DumpTree(SplayNode n, int depth, TextWriter writer)
        {
            if (n.right != n)
            {
                DumpTree(n.right, depth + 1, writer);
            }
            const int Indent = 4;
            string rep = n == Nil ? "nil" : String.Format("denseSize={0} denseCount={1} sparseSize={2} sparseCount1={3}", n.xSize, n.xCount1, n.ySize, n.yCount1);
            writer.WriteLine(String.Format("{0}{1}", new String(' ', depth * Indent), rep));
            if (n.left != n)
            {
                DumpTree(n.left, depth + 1, writer);
            }
        }
#endif
    }
}
