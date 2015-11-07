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
    // An adaptation of SplayTree to provide a fast sparse array

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
    public class SplaySparseArray<T>
    {
        private readonly SplayNode Nil;

        private SplayNode root;

        private class SplayNode
        {
            public SplayNode left;
            public SplayNode right;

            public int size; // total size of tree
            public int count; // local count

            public T value;
        }

        public SplaySparseArray()
        {
            Nil = new SplayNode();
            Nil.left = Nil;
            Nil.right = Nil;
            root = Nil;
        }

        public SplaySparseArray(SplaySparseArray<T> original)
        {
            throw new NotImplementedException("clone is not implemented");
        }

        private static int Start(SplayNode n)
        {
            return n.left.size;
        }

        public int Count
        {
            get
            {
                return root.size;
            }
        }

        public void InsertRange(int start, int count, T value)
        {
            if ((start < 0) || (count < 0))
            {
                throw new ArgumentException();
            }

            Splay(ref root, start);

            if (start == Start(root))
            {
                // insert item just in front of current root

                if (start != Start(root))
                {
                    throw new KeyNotFoundException("item not in tree");
                }

                if (count == 0)
                {
                    return;
                }

                SplayNode i = new SplayNode();
                i.left = Nil;
                i.right = Nil;
                i.value = value;
                i.size = count;
                i.count = count;

                int leftSize = root.left.size;
                i.left = root.left;
                i.right = root;
                i.size += root.size;

                root.left = Nil;
                root.size -= leftSize;

                root = i;
            }
            else
            {
                // append

                if (root.right != Nil)
                {
                    throw new InvalidOperationException(); // program defect
                }

                if (count == 0)
                {
                    return;
                }

                if (start != root.size)
                {
                    throw new KeyNotFoundException("item not in tree");
                }

                SplayNode i = new SplayNode();
                i.left = Nil;
                i.right = Nil;
                i.value = value;
                i.size = count;
                i.count = count;

                if (root != Nil)
                {
                    root.right = i;
                    root.size += i.size;
                }
                else
                {
                    root = i;
                }
            }
        }

        public void RemoveRange(int start, int count)
        {
            if ((start < 0) || (count < 0))
            {
                throw new ArgumentException();
            }


            Splay(ref root, start);
            if ((start != Start(root)) || ((count != 0) && (count != root.count)))
            {
                throw new KeyNotFoundException("item not in tree");
            }

            if (count == 0)
            {
                return;
            }

            if (!SplayRemove(root, ref root))
            {
                throw new KeyNotFoundException("item not in tree"); // program defect
            }
        }

        public void GetCountValue(int start, out int count, out T value)
        {
            if (start < 0)
            {
                throw new ArgumentException();
            }

            Splay(ref root, start);
            if (start != Start(root))
            {
                throw new KeyNotFoundException("item not in tree");
            }

            count = root.count;
            value = root.value;
        }

        public T GetValue(int start)
        {
            if (start < 0)
            {
                throw new ArgumentException();
            }

            Splay(ref root, start);
            if (start != Start(root))
            {
                throw new KeyNotFoundException("item not in tree");
            }
            return root.value;
        }

        public int GetCount(int start)
        {
            if (start < 0)
            {
                throw new ArgumentException();
            }

            Splay(ref root, start);
            if (start != Start(root))
            {
                throw new KeyNotFoundException("item not in tree");
            }
            return root.count;
        }

        public bool NearestLessOrEqual(int start, out int nearestStart)
        {
            if (start < 0)
            {
                throw new ArgumentException();
            }

            if (root == Nil)
            {
                nearestStart = 0;
                return false;
            }
            Splay(ref root, start);
            if (start.CompareTo(Start(root)) < 0)
            {
                if (root.left == Nil)
                {
                    if (root.size != root.right.size + root.count)
                    {
                        throw new InvalidOperationException(); // program defect
                    }
                    nearestStart = 0;
                    return false;
                }
                int rootStart = Start(root);
                Splay(ref root.left, rootStart);
                nearestStart = root.left.left.size;
                return true;
            }
            else
            {
                nearestStart = Start(root);
                return true;
            }
        }

        public void NearestLessOrEqualCountValue(int index, out int start, out int count, out T value)
        {
            if (index < 0)
            {
                throw new ArgumentException();
            }

            NearestLessOrEqual(index, out start);
            GetCountValue(start, out count, out value);
        }

        public void Previous(int start, out int previous)
        {
            if (start < 0)
            {
                throw new ArgumentException();
            }

            NearestLessOrEqual(start - 1, out previous);
        }

        public bool Next(int start, out int next)
        {
            if (start < 0)
            {
                throw new ArgumentException();
            }

            Splay(ref root, start);
            if (start != Start(root))
            {
                throw new KeyNotFoundException("item not in tree");
            }

            next = start + root.count;
            return root.right != Nil;
        }

        public delegate void TraverseAction(int start, int count, T ValueType);
        public void Traverse(TraverseAction action)
        {
            Traverse(action, root, 0);
        }
        private void Traverse(TraverseAction action, SplayNode n, int offset)
        {
            if (n != Nil)
            {
                Traverse(action, n.left, offset);
                action(offset + Start(n), n.count, n.value);
                Traverse(action, n.right, offset + Start(n) + n.count);
            }
        }

        public delegate void Traverse2Action(int size, int count, int start, T ValueType, int depth, bool nil);
        public void Traverse2(Traverse2Action action)
        {
            Traverse2(action, root, 0, 0);
        }
        private void Traverse2(Traverse2Action action, SplayNode n, int offset, int depth)
        {
            if (n.left != n)
            {
                Traverse2(action, n.left, offset, depth + 1);
            }
            action(n.size, n.count, offset + Start(n), n.value, depth, n == Nil);
            if (n.right != n)
            {
                Traverse2(action, n.right, offset + Start(n) + n.count, depth + 1);
            }
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
            int lsize = 0;
            r = N;
            int rsize = 0;

            while (true)
            {
                int c;

                c = start.CompareTo(t.left.size);
                if (c < 0)
                {
                    if (t.left == Nil)
                    {
                        break;
                    }
                    c = start.CompareTo(t.left.left.size);
                    if (c < 0)
                    {
                        // rotate right
                        y = t.left;
                        t.left = y.right;
                        y.right = t;
                        t.size = t.left.size + t.right.size + t.count;
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
                    rsize += r.count + r.right.size;
                }
                else if (c > 0)
                {
                    if (t.right == Nil)
                    {
                        break;
                    }
                    c = start.CompareTo(t.left.size + t.count + t.right.left.size);
                    if (c > 0)
                    {
                        // rotate left
                        y = t.right;
                        t.right = y.left;
                        y.left = t;
                        t.size = t.left.size + t.right.size + t.count;
                        t = y;
                        if (t.right == Nil)
                        {
                            break;
                        }
                    }
                    // link left
                    l.right = t;
                    l = t;
                    start -= t.left.size + t.count;
                    t = t.right;
                    lsize += l.count + l.left.size;
                }
                else
                {
                    break;
                }
            }
            lsize += t.left.size;  // Now lsize and rsize are the sizes of
            rsize += t.right.size; // the left and right trees we just built.
            t.size = lsize + rsize + t.count;
            l.right = Nil;
            r.left = Nil;
            // The following two loops correct the size fields of the right path
            // from the left child of the root and the left path from the right
            // child of the root.
            for (y = N.right; y != Nil; y = y.right)
            {
                y.size = lsize;
                lsize -= y.count + y.left.size;
            }
            for (y = N.left; y != Nil; y = y.left)
            {
                y.size = rsize;
                rsize -= y.count + y.right.size;
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
            c = i.size.CompareTo(root.size);
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
                    x.size = root.size - root.count;
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
    }
}
