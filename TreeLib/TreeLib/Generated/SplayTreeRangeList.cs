// NOTE: This file is auto-generated. DO NOT MAKE CHANGES HERE! They will be overwritten on rebuild.

/*
 *  Copyright � 2016 Thomas R. Lawrence
 * 
 *  GNU Lesser General Public License
 * 
 *  This file is part of TreeLib
 * 
 *  TreeLib is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program. If not, see <http://www.gnu.org/licenses/>.
 * 
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using TreeLib.Internal;

#pragma warning disable CS1572 // silence warning: XML comment has a param tag for '...', but there is no parameter by that name
#pragma warning disable CS1573 // silence warning: Parameter '...' has no matching param tag in the XML comment
#pragma warning disable CS1587 // silence warning: XML comment is not placed on a valid language element
#pragma warning disable CS1591 // silence warning: Missing XML comment for publicly visible type or member

//
// Implementation of top-down splay tree written by Daniel Sleator <sleator@cs.cmu.edu>.
// Taken from http://www.link.cs.cmu.edu/link/ftp-site/splaying/top-down-splay.c
//
// An overview of splay trees can be found here: https://en.wikipedia.org/wiki/Splay_tree
//

namespace TreeLib
{

    /// <summary>
    /// Implements a map, list or range collection using a splay tree. 
    /// </summary>
    
    /// <summary>
    /// Represents an sequenced collection of ranges (without associated values). Each range is defined by it's length, and occupies
    /// a particular position in the sequence, determined by the location where it was inserted (and any insertions/deletions that
    /// have occurred before or after it in the sequence). The start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// All ranges must have a length of at least 1.
    /// </summary>
    public class SplayTreeRangeList:
        /*[Feature(Feature.Range)]*//*[Payload(Payload.None)]*//*[Widen]*/IRangeList,

        INonInvasiveTreeInspection,
        /*[Feature(Feature.Range, Feature.Range2)]*//*[Widen]*/INonInvasiveRange2MapInspection,

        IEnumerable<EntryRangeList>,
        IEnumerable,
        ITreeEnumerable<EntryRangeList>,
        /*[Feature(Feature.Range)]*//*[Widen]*/IIndexedTreeEnumerable<EntryRangeList>,

        ICloneable
    {
        //
        // Object form data structure
        //

        [Storage(Storage.Object)]
        private sealed class Node
        {
            public Node left;
            public Node right;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public int xOffset;

            public static Node CreateNil()
            {
                Node nil = new Node();
                nil.left = nil;
                nil.right = nil;
                return nil;
            }

            public override string ToString()
            {
                if (this.IsNil)
                {
                    return "Nil";
                }

                string keyText = null;

                string valueText = null;

                string leftKeyText = null;

                string rightKeyText = null;

                return String.Format("({0})*{2}={3}*({1})", leftKeyText, rightKeyText, keyText, valueText);
            }

            private bool IsNil
            {
                get
                {
                    Debug.Assert((this == left) == (this == right));
                    return this == left;
                }
            }
        }

        // TODO: ensure fields of the Nil object are not written to, then make it a shared static.
        // (Offsets and left/right pointers never change, but there is a chance there are no-op writes to them during the
        // processing. If so, it can't be shared since it would incur a large penalty in concurrent scenarios.)
        [Storage(Storage.Object)]
        private readonly Node Nil = Node.CreateNil();
        [Storage(Storage.Object)]
        private readonly Node N = new Node();

        //
        // State for both array & object form
        //

        private Node root;
        [Count]
        private ulong count;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private int xExtent;

        private readonly AllocationMode allocationMode;
        private Node freelist;

        private ushort version;


        //
        // Construction
        //

        // Object

        /// <summary>
        /// Create a new collection based on a splay tree, explicitly configured.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys (present only for keyed collections)</param>
        /// <param name="capacity">
        /// For PreallocatedFixed mode, the maximum capacity of the tree, the memory for which is
        /// preallocated at construction time; exceeding that capacity will result in an OutOfMemory exception.
        /// For DynamicDiscard or DynamicRetainFreelist, the number of nodes to pre-allocate at construction time (the collection
        /// is permitted to exceed that capacity, in which case additional nodes will be allocated from the heap).
        /// For DynamicDiscard, nodes are unreferenced upon removal, allowing the garbage collector to reclaim the memory at any time.
        /// For DynamicRetainFreelist or PreallocatedFixed, upon removal nodes are returned to a free list from which subsequent
        /// nodes will be allocated.
        /// </param>
        /// <param name="allocationMode">The allocation mode (see capacity)</param>
        [Storage(Storage.Object)]
        public SplayTreeRangeList(uint capacity,AllocationMode allocationMode)
        {
            this.root = Nil;

            this.allocationMode = allocationMode;
            this.freelist = Nil;
            EnsureFree(capacity);
        }

        /// <summary>
        /// Create a new collection based on a splay tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public SplayTreeRangeList()
            : this(0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public SplayTreeRangeList(SplayTreeRangeList original)
        {

            this.count = original.count;
            this.xExtent = original.xExtent;

            this.allocationMode = original.allocationMode;
            this.freelist = this.Nil;
            {
                Node nodeOriginal = original.freelist;
                while (nodeOriginal != original.Nil)
                {
                    nodeOriginal = nodeOriginal.left;
                    Node nodeCopy = new Node();
                    nodeCopy.left = this.freelist;
                    this.freelist = nodeCopy;
                }
            }
#if DEBUG
            this.allocateHelper.allocateCount = original.allocateHelper.allocateCount;
#endif

            // TODO: performance and memory usage
            // Cloning a Splay tree is problematic because of the worst-case O(N) depth. Here we are using a breadth-first
            // traversal to clone, as it is likely to use less memory in a typically "leggy" splay tree (vs. other types)
            // than depth-first. Need to determine if this is sufficient or should be parameterized to give caller control,
            // with the option of an O(N lg N) traversal instead.
            this.root = this.Nil;
            if (original.root != original.Nil)
            {
                this.root = new Node();

                Queue<STuple<Node, Node>> worklist = new Queue<STuple<Node, Node>>();
                worklist.Enqueue(new STuple<Node, Node>(this.root, original.root));
                while (worklist.Count != 0)
                {
                    STuple<Node, Node> item = worklist.Dequeue();

                    Node nodeThis = item.Item1;
                    Node nodeOriginal = item.Item2;
                    nodeThis.xOffset = nodeOriginal.xOffset;
                    nodeThis.left = this.Nil;
                    nodeThis.right = this.Nil;

                    if (nodeOriginal.left != original.Nil)
                    {
                        nodeThis.left = new Node();
                        worklist.Enqueue(new STuple<Node, Node>(nodeThis.left, nodeOriginal.left));
                    }
                    if (nodeOriginal.right != original.Nil)
                    {
                        nodeThis.right = new Node();
                        worklist.Enqueue(new STuple<Node, Node>(nodeThis.right, nodeOriginal.right));
                    }
                }
            }
        }


        //
        // IOrderedMap, IOrderedList
        //

        
        /// <summary>
        /// Returns the number of ranges in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue ranges.</exception>
        public uint Count { get { return checked((uint)this.count); } }

        
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        public long LongCount { get { return unchecked((long)this.count); } }

        
        /// <summary>
        /// Removes all ranges from the collection.
        /// </summary>
        public void Clear()
        {
            // no need to do any work for DynamicDiscard mode
            if (allocationMode != AllocationMode.DynamicDiscard)
            {
                // Since splay trees can be deep and thready, breadth-first is likely to use less memory than
                // depth-first. The worst-case is still N/2, so still risks being expensive.

                Queue<Node> queue = new Queue<Node>();
                if (root != Nil)
                {
                    queue.Enqueue(root);
                    while (queue.Count != 0)
                    {
                        Node node = queue.Dequeue();
                        if (node.left != Nil)
                        {
                            queue.Enqueue(node.left);
                        }
                        if (node.right != Nil)
                        {
                            queue.Enqueue(node.right);
                        }

                        this.count = unchecked(this.count - 1);
                        Free(node);
                    }
                }

                Debug.Assert(this.count == 0);
            }
            else
                /*[Storage(Storage.Object)]*/
                {
#if DEBUG
                    allocateHelper.allocateCount = 0;
#endif
                }

            root = Nil;
            this.count = 0;
            this.xExtent = 0;
        }


        //
        // IRange2Map, IRange2List, IRangeMap, IRangeList
        //

        // Count { get; } - reuses Feature.Dict implementation

        
        /// <summary>
        /// Determines if there is a range in the collection starting at the specified index.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool Contains([Widen] int start)
        {
            if (root != Nil)
            {
                Splay2(ref root, start);
                return start == Start(root);
            }
            return false;
        }

        
        /// <summary>
        /// Attempt to insert a range of a given length at the specified start index.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryInsert([Widen] int start,[Widen] int xLength)
        {
            unchecked
            {
                if (start < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                if (xLength <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                Splay2(ref root, start);
                if (start == Start(root))
                {
                    // insert item just in front of root

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + xLength);

                    Node i = Allocate();
                    i.xOffset = root.xOffset;

                    i.left = root.left;
                    i.right = root;
                    if (root != Nil)
                    {
                        root.xOffset = xLength;
                        root.left = Nil;
                    }

                    root = i;

                    this.count = countNew;
                    this.xExtent = xExtentNew;

                    return true;
                }

                Splay2(ref root.right, 0);
                /*[Widen]*/
                int length = root.right != Nil
                    ? (root.right.xOffset)
                    : (this.xExtent - root.xOffset);
                if (start == Start(root) + length)
                {
                    // append

                    Debug.Assert(root.right == Nil);

                    /*[Widen]*/
                    int xLengthRoot = this.xExtent - root.xOffset;

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + xLength);

                    Node i = Allocate();
                    i.xOffset = xLengthRoot;

                    i.left = Nil;
                    i.right = Nil;
                    Debug.Assert(root != Nil);
                    Debug.Assert(root.right == Nil);
                    root.right = i;

                    this.count = countNew;
                    this.xExtent = xExtentNew;

                    return true;
                }

                return false;
            }
        }

        
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryDelete([Widen] int start)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start);
                    if (start == Start(root))
                    {
                        Splay2(ref root.right, 0);
                        Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                        /*[Widen]*/
                        int xLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset;

                        Node dead, x;

                        dead = root;
                        if (root.left == Nil)
                        {
                            x = root.right;
                            if (x != Nil)
                            {
                                x.xOffset += root.xOffset - xLength;
                            }
                        }
                        else
                        {
                            x = root.left;
                            x.xOffset += root.xOffset;
                            Splay2(ref x, start);
                            Debug.Assert(x.right == Nil);
                            if (root.right != Nil)
                            {
                                root.right.xOffset += root.xOffset - x.xOffset - xLength;
                            }
                            x.right = root.right;
                        }
                        root = x;

                        this.count = unchecked(this.count - 1);
                        this.xExtent = unchecked(this.xExtent - xLength);
                        Free(dead);

                        return true;
                    }
                }
                return false;
            }
        }

        
        /// <summary>
        /// Attempt to query the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetLength([Widen] int start,[Widen] out int length)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start);
                    if (start == Start(root))
                    {
                        Splay2(ref root.right, 0);
                        if (root.right != Nil)
                        {
                            Debug.Assert(root.right.left == Nil);
                            length = root.right.xOffset;
                        }
                        else
                        {
                            length = this.xExtent - start;
                        }
                        return true;
                    }
                }
                length = 0;
                return false;
            }
        }

        
        /// <summary>
        /// Attempt to change the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetLength([Widen] int start,[Widen] int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (root != Nil)
            {
                Splay2(ref root, start);
                if (start == Start(root))
                {
                    /*[Widen]*/
                    int oldLength;
                    if (root.right != Nil)
                    {
                        Splay2(ref root.right, 0);
                        Debug.Assert(root.right.left == Nil);
                        oldLength = root.right.xOffset;
                    }
                    else
                    {
                        oldLength = unchecked(this.xExtent - root.xOffset);
                    }
                    /*[Widen]*/
                    int delta = length - oldLength;
                    {
                        this.xExtent = checked(this.xExtent + delta);

                        if (root.right != Nil)
                        {
                            unchecked
                            {
                                root.right.xOffset += delta;
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] int start,[Widen] out int xLength)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start);
                    if (start == Start(root))
                    {
                        Splay2(ref root.right, 0);
                        Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                        if (root.right != Nil)
                        {
                            xLength = root.right.xOffset;
                        }
                        else
                        {
                            xLength = this.xExtent - root.xOffset;
                        }
                        return true;
                    }
                }
                xLength = 0;
                return false;
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySet([Widen] int start,[Widen] int xLength)
        {
            if (xLength < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (root != Nil)
            {
                Splay2(ref root, start);
                if (start == Start(root))
                {
                    /*[Widen]*/
                    int xLengthOld;
                    if (root.right != Nil)
                    {
                        Splay2(ref root.right, 0);
                        Debug.Assert(root.right.left == Nil);
                        xLengthOld = root.right.xOffset;
                    }
                    else
                    {
                        xLengthOld = unchecked(this.xExtent - root.xOffset);
                    }

                    /*[Widen]*/
                    int xAdjust = xLength != 0 ? xLength - xLengthOld : 0;

                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + xAdjust);
                    // throw overflow before updating anything
                    this.xExtent = xExtentNew;

                    if (root.right != Nil)
                    {
                        unchecked
                        {
                            root.right.xOffset += xAdjust;
                        }
                    }
                    if (root.right != Nil)
                    {
                        unchecked
                        {
                        }
                    }

                    return true;
                }
            }
            return false;
        }

        
        /// <summary>
        /// Inserts a range of a given length at the specified start index.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Insert([Widen] int start,[Widen] int xLength)
        {
            if (!TryInsert(start, xLength))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Delete([Widen] int start)
        {
            if (!TryDelete(start))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <returns>the length of the range found at the specified start index</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public int GetLength([Widen] int start)
        {
            /*[Widen]*/
            int length;
            if (!TryGetLength(start, out length))
            {
                throw new ArgumentException("item not in tree");
            }
            return length;
        }

        
        /// <summary>
        /// Changes the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void SetLength([Widen] int start,[Widen] int length)
        {
            if (!TrySetLength(start, length))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Get([Widen] int start,[Widen] out int xLength)
        {
            if (!TryGet(start, out xLength))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Set([Widen] int start,[Widen] int xLength)
        {
            if (!TrySet(start, xLength))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the extent of the sequence of ranges. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <returns>the extent of the ranges</returns>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public int GetExtent()
        {
            return this.xExtent;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool NearestLess([Widen] int position,[Widen] out int nearestStart,bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position);
                /*[Widen]*/
                int start = Start(root);
                if ((position < start) || (!orEqual && (position == start)))
                {
                    if (root.left != Nil)
                    {
                        Splay2(ref root.left, 0);
                        Debug.Assert(root.left.right == Nil);
                        nearestStart = start + (root.left.xOffset);
                        return true;
                    }
                    nearestStart = 0;
                    return false;
                }
                else
                {
                    nearestStart = Start(root);
                    return true;
                }
            }
            nearestStart = 0;
            return false;
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] int position,[Widen] out int nearestStart,[Widen] out int xLength)
        {
            xLength = 0;
            bool f = NearestLess(position, out nearestStart, true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
                Debug.Assert(g);
            }
            return f;
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] int position,[Widen] out int nearestStart)
        {
            return NearestLess(position, out nearestStart, true/*orEqual*/);
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] int position,[Widen] out int nearestStart,[Widen] out int xLength)
        {
            xLength = 0;
            bool f = NearestLess(position, out nearestStart, false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
                Debug.Assert(g);
            }
            return f;
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] int position,[Widen] out int nearestStart)
        {
            return NearestLess(position, out nearestStart, false/*orEqual*/);
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool NearestGreater([Widen] int position,[Widen] out int nearestStart,bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position);
                /*[Widen]*/
                int start = Start(root);
                if ((position > start) || (!orEqual && (position == start)))
                {
                    if (root.right != Nil)
                    {
                        Splay2(ref root.right, 0);
                        Debug.Assert(root.right.left == Nil);
                        nearestStart = start + (root.right.xOffset);
                        return true;
                    }
                    nearestStart = this.xExtent;
                    return false;
                }
                else
                {
                    nearestStart = start;
                    return true;
                }
            }
            nearestStart = 0;
            return false;
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] int position,[Widen] out int nearestStart,[Widen] out int xLength)
        {
            xLength = 0;
            bool f = NearestGreater(position, out nearestStart, true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
                Debug.Assert(g);
            }
            return f;
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] int position,[Widen] out int nearestStart)
        {
            return NearestGreater(position, out nearestStart, true/*orEqual*/);
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] int position,[Widen] out int nearestStart,[Widen] out int xLength)
        {
            xLength = 0;
            bool f = NearestGreater(position, out nearestStart, false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
                Debug.Assert(g);
            }
            return f;
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] int position,[Widen] out int nearestStart)
        {
            return NearestGreater(position, out nearestStart, false/*orEqual*/);
        }

        
        /// <summary>
        /// Adjust the length of the range starting at 'start' by adding 'adjust' to the current length of the
        /// range. If the length would become 0, the range is removed.
        /// </summary>
        /// <param name="start">the start index of the range to adjust</param>
        /// <param name="adjust">the amount to adjust the length by. Value may be negative to shrink the length</param>
        /// <exception cref="ArgumentException">There is no range starting at the index specified by 'start'.</exception>
        /// <exception cref="ArgumentOutOfRangeException">the length would become negative</exception>
        /// <exception cref="OverflowException">the extent would become larger than Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void AdjustLength([Widen] int startIndex,[Widen] int xAdjust)
        {
            unchecked
            {
                Splay2(ref root, startIndex);
                if ((root == Nil) || (startIndex != (root.xOffset)))
                {
                    throw new ArgumentException();
                }

                Splay2(ref root.right, 0);
                Debug.Assert((root.right == Nil) || (root.right.left == Nil));

                /*[Widen]*/
                int oldXLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset;

                /*[Widen]*/
                int newXLength = checked(oldXLength + xAdjust);

                if (newXLength < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (newXLength != 0)
                {
                    // adjust

                    // throw OverflowException before modifying anything
                    /*[Widen]*/
                    int newXExtent = checked(this.xExtent + xAdjust);
                    this.xExtent = newXExtent;

                    if (root.right != Nil)
                    {
                        root.right.xOffset += xAdjust;
                    }
                }
                else
                {
                    // delete

                    Debug.Assert(xAdjust < 0);
                    Debug.Assert(newXLength == 0);

                    Node dead, x;

                    dead = root;
                    if (root.left == Nil)
                    {
                        x = root.right;
                        if (x != Nil)
                        {
                            Debug.Assert(root.xOffset == 0);
                            x.xOffset = 0; //nodes[x].xOffset = nodes[root].xOffset;
                        }
                    }
                    else
                    {
                        x = root.left;
                        x.xOffset += root.xOffset;
                        Splay2(ref x, startIndex);
                        Debug.Assert(x.right == Nil);
                        if (root.right != Nil)
                        {
                            root.right.xOffset += root.xOffset - x.xOffset + xAdjust;
                        }
                        x.right = root.right;
                    }
                    root = x;

                    this.count = unchecked(this.count - 1);
                    this.xExtent = unchecked(this.xExtent - oldXLength);
                    Free(dead);
                }
            }
        }


        //
        // Internals
        //

        // Object allocation

        [Storage(Storage.Object)]
        private struct AllocateHelper // hack for Roslyn since member removal corrupts following conditional directives
        {
#if DEBUG
            [Count]
            public ulong allocateCount;
#endif
        }
        [Storage(Storage.Object)]
        private AllocateHelper allocateHelper;

        [Storage(Storage.Object)]
        private Node Allocate()
        {
            Node node = freelist;
            if (node != Nil)
            {
                freelist = freelist.left;
            }
            else if (allocationMode == AllocationMode.PreallocatedFixed)
            {
                const string Message = "Tree capacity exhausted but is locked";
                throw new OutOfMemoryException(Message);
            }
            else
            {
                node = new Node();
            }

            {
#if DEBUG
                allocateHelper.allocateCount = checked(allocateHelper.allocateCount + 1);
#endif
            }

            return node;
        }

        [Storage(Storage.Object)]
        private void Free(Node node)
        {
#if DEBUG
            allocateHelper.allocateCount = checked(allocateHelper.allocateCount - 1);
            Debug.Assert(allocateHelper.allocateCount == count);

            node.left = null;
            node.right = null;
            node.xOffset = Int32.MinValue;
#endif

            if (allocationMode != AllocationMode.DynamicDiscard)
            {

                node.left = freelist;
                freelist = node;
            }
        }

        [Storage(Storage.Object)]
        private void EnsureFree(uint capacity)
        {
            unchecked
            {
                Debug.Assert(freelist == Nil);
                for (uint i = 0; i < capacity - this.count; i++)
                {
                    Node node = new Node();
                    node.left = freelist;
                    freelist = node;
                }
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Widen]
        private int Start(Node n)
        {
            return n.xOffset;
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [EnableFixed]
        private void Splay2(ref Node root,[Widen] int position)
        {
            unchecked
            {
                this.version = unchecked((ushort)(this.version + 1));

                if (root == Nil)
                {
                    return;
                }

                Node t = root;

                N.left = Nil;
                N.right = Nil;

                Node l = N;
                /*[Widen]*/
                int lxOffset = 0;
                Node r = N;
                /*[Widen]*/
                int rxOffset = 0;

                while (true)
                {
                    int c;

                    c = position.CompareTo(t.xOffset);
                    if (c < 0)
                    {
                        if (t.left == Nil)
                        {
                            break;
                        }
                        c = position.CompareTo(t.xOffset + t.left.xOffset);
                        if (c < 0)
                        {
                            // rotate right
                            Node u = t.left;
                            /*[Widen]*/
                            int uXPosition = t.xOffset + u.xOffset;
                            if (u.right != Nil)
                            {
                                u.right.xOffset += uXPosition - t.xOffset;
                            }
                            t.xOffset += -uXPosition;
                            u.xOffset = uXPosition;
                            t.left = u.right;
                            u.right = t;
                            t = u;
                            if (t.left == Nil)
                            {
                                break;
                            }
                        }
                        // link right
                        Debug.Assert(t.left != Nil);
                        t.left.xOffset += t.xOffset;
                        t.xOffset -= rxOffset;
                        r.left = t;
                        r = t;
                        rxOffset += r.xOffset;
                        t = t.left;
                    }
                    else if (c > 0)
                    {
                        if (t.right == Nil)
                        {
                            break;
                        }
                        c = position.CompareTo(t.xOffset + t.right.xOffset);
                        if (c > 0)
                        {
                            // rotate left
                            Node u = t.right;
                            /*[Widen]*/
                            int uXPosition = t.xOffset + u.xOffset;
                            if (u.left != Nil)
                            {
                                u.left.xOffset += uXPosition - t.xOffset;
                            }
                            t.xOffset += -uXPosition;
                            u.xOffset = uXPosition;
                            t.right = u.left;
                            u.left = t;
                            t = u;
                            if (t.right == Nil)
                            {
                                break;
                            }
                        }
                        // link left
                        Debug.Assert(t.right != Nil);
                        t.right.xOffset += t.xOffset;
                        t.xOffset -= lxOffset;
                        l.right = t;
                        l = t;
                        lxOffset += l.xOffset;
                        t = t.right;
                    }
                    else
                    {
                        break;
                    }
                }
                // reassemble
                l.right = t.left;
                if (l.right != Nil)
                {
                    l.right.xOffset += t.xOffset - lxOffset;
                }
                r.left = t.right;
                if (r.left != Nil)
                {
                    r.left.xOffset += t.xOffset - rxOffset;
                }
                t.left = N.right;
                if (t.left != Nil)
                {
                    t.left.xOffset -= t.xOffset;
                }
                t.right = N.left;
                if (t.right != Nil)
                {
                    t.right.xOffset -= t.xOffset;
                }
                root = t;
            }
        }


        //
        // Non-invasive tree inspection support
        //

        // Helpers

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private void ValidateRanges()
        {
            if (root != Nil)
            {
                Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int offset = 0;
                /*[Widen]*/
                int leftEdge = 0;
                /*[Widen]*/
                int rightEdge = this.xExtent;

                Node node = root;
                while (node != Nil)
                {
                    offset += node.xOffset;
                    stack.Push(new STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>(node, offset, leftEdge, rightEdge));
                    rightEdge = offset;
                    node = node.left;
                }
                while (stack.Count != 0)
                {
                    STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int> t = stack.Pop();
                    node = t.Item1;
                    offset = t.Item2;
                    leftEdge = t.Item3;
                    rightEdge = t.Item4;

                    Check.Assert((offset >= leftEdge) && (offset < rightEdge), "range containment invariant");

                    leftEdge = offset + 1;
                    node = node.right;
                    while (node != Nil)
                    {
                        offset += node.xOffset;
                        stack.Push(new STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>(node, offset, leftEdge, rightEdge));
                        rightEdge = offset;
                        node = node.left;
                    }
                }
            }
        }

        // INonInvasiveTreeInspection

        /// <summary>
        /// INonInvasiveTreeInspection.Root is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.Root { get { return root != Nil ? (object)root : null; } }

        /// <summary>
        /// INonInvasiveTreeInspection.GetLeftChild() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            Node n = (Node)node;
            return n.left != Nil ? (object)n.left : null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.GetRightChild() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            Node n = (Node)node;
            return n.right != Nil ? (object)n.right : null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.GetKey() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetKey(object node)
        {
            return null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.GetValue() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetValue(object node)
        {
            return null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.GetMetadata() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            return null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.Validate() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        void INonInvasiveTreeInspection.Validate()
        {
            if (root != Nil)
            {
                Dictionary<Node, bool> visited = new Dictionary<Node, bool>();
                Queue<Node> worklist = new Queue<Node>();
                worklist.Enqueue(root);
                while (worklist.Count != 0)
                {
                    Node node = worklist.Dequeue();

                    Check.Assert(!visited.ContainsKey(node), "cycle");
                    visited.Add(node, false);

                    if (node.left != Nil)
                    {
                        worklist.Enqueue(node.left);
                    }
                    if (node.right != Nil)
                    {
                        worklist.Enqueue(node.right);
                    }
                }
            }

            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            ValidateRanges();
        }

        // INonInvasiveRange2MapInspection

        /// <summary>
        /// INonInvasiveRange2MapInspection.GetRanges() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        Range2MapEntry[] INonInvasiveRange2MapInspection.GetRanges()
        {
            /*[Widen]*/
            Range2MapEntry[] ranges = new Range2MapEntry[Count];
            int i = 0;

            if (root != Nil)
            {
                Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int xOffset = 0;
                /*[Widen]*/
                int yOffset = 0;

                Node node = root;
                while (node != Nil)
                {
                    xOffset += node.xOffset;
                    stack.Push(new STuple<Node, /*[Widen]*/int, /*[Widen]*/int>(node, xOffset, yOffset));
                    node = node.left;
                }
                while (stack.Count != 0)
                {
                    STuple<Node, /*[Widen]*/int, /*[Widen]*/int> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;
                    yOffset = t.Item3;

                    object value = null;

                    /*[Widen]*/
                    ranges[i++] = new Range2MapEntry(new Range(xOffset, 0), new Range(yOffset, 0), value);

                    node = node.right;
                    while (node != Nil)
                    {
                        xOffset += node.xOffset;
                        stack.Push(new STuple<Node, /*[Widen]*/int, /*[Widen]*/int>(node, xOffset, yOffset));
                        node = node.left;
                    }
                }
                Check.Assert(i == ranges.Length, "count invariant");

                for (i = 1; i < ranges.Length; i++)
                {
                    Check.Assert(ranges[i - 1].x.start < ranges[i].x.start, "range sequence invariant (X)");
                    ranges[i - 1].x.length = ranges[i].x.start - ranges[i - 1].x.start;
                }

                ranges[i - 1].x.length = this.xExtent - ranges[i - 1].x.start;
            }

            return ranges;
        }

        /// <summary>
        /// INonInvasiveRange2MapInspection.Validate() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [Feature(Feature.Range, Feature.Range2)]
        void INonInvasiveRange2MapInspection.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }


        //
        // IEnumerable
        //

        /// <summary>
        /// Get the default enumerator, which is the robust enumerator for splay trees.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<EntryRangeList> GetEnumerator()
        {
            // For splay trees, the default enumerator is Robust because the Fast enumerator is fragile
            // and potentially consumes huge amounts of memory. For clients that can handle it, the Fast
            // enumerator is available by explicitly calling GetFastEnumerable().
            return GetRobustEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        //
        // ITreeEnumerable
        //

        public IEnumerable<EntryRangeList> GetEnumerable()
        {
            return new RobustEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<EntryRangeList> GetEnumerable(bool forward)
        {
            return new RobustEnumerableSurrogate(this, forward);
        }

        /// <summary>
        /// Get the robust enumerator. The robust enumerator uses an internal key cursor and queries the tree using the NextGreater()
        /// method to advance the enumerator. This enumerator is robust because it tolerates changes to the underlying tree. If a key
        /// is inserted or removed and it comes before the enumerator�s current key in sorting order, it will have no affect on the
        /// enumerator. If a key is inserted or removed and it comes after the enumerator�s current key (i.e. in the portion of the
        /// collection the enumerator hasn�t visited yet), the enumerator will include the key if inserted or skip the key if removed.
        /// Because the enumerator queries the tree for each element it�s running time per element is O(lg N), or O(N lg N) to
        /// enumerate the entire tree.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        public IEnumerable<EntryRangeList> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<EntryRangeList> GetRobustEnumerable(bool forward)
        {
            return new RobustEnumerableSurrogate(this, forward);
        }

        /// <summary>
        /// Get the fast enumerator. The fast enumerator uses an internal stack of nodes to peform in-order traversal of the
        /// tree structure. Because it uses the tree structure, it is invalidated if the tree is modified by an insertion or
        /// deletion and will throw an InvalidOperationException when next advanced. For the Splay tree, all operations modify
        /// the tree structure, include queries, and will invalidate the enumerator. The complexity of the fast enumerator
        /// is O(1) per element, or O(N) to enumerate the entire tree.
        /// 
        /// A note about splay trees and enumeration: Enumeration of splay trees is generally problematic, for two reasons.
        /// First, every operation on a splay tree modifies the structure of the tree, including queries. Second, splay trees
        /// may have depth of N in the worst case (as compared to other trees which are guaranteed to be less deep than
        /// approximately two times the optimal depth, or 2 lg N). The first property makes fast enumeration less useful, and
        /// the second property means fast enumeration may consume up to memory proportional to N for the internal stack used
        /// for traversal. Therefore, the robust enumerator is recommended for splay trees. The drawback is the
        /// robust enumerator�s O(N lg N) complexity.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        public IEnumerable<EntryRangeList> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<EntryRangeList> GetFastEnumerable(bool forward)
        {
            return new FastEnumerableSurrogate(this, forward);
        }

        //
        // IIndexedTreeEnumerable/IIndexed2TreeEnumerable
        //

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeList> GetEnumerable([Widen] int startAt)
        {
            return new RobustEnumerableSurrogate(this, startAt, true/*forward*/); // default
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeList> GetEnumerable([Widen] int startAt,bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward); // default
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeList> GetFastEnumerable([Widen] int startAt)
        {
            return new FastEnumerableSurrogate(this, startAt, true/*forward*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeList> GetFastEnumerable([Widen] int startAt,bool forward)
        {
            return new FastEnumerableSurrogate(this, startAt, forward);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeList> GetRobustEnumerable([Widen] int startAt)
        {
            return new RobustEnumerableSurrogate(this, startAt, true/*forward*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeList> GetRobustEnumerable([Widen] int startAt,bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward);
        }

        //
        // Surrogates
        //

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRangeList>
        {
            private readonly SplayTreeRangeList tree;
            private readonly bool forward;

            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;

            // Construction

            public RobustEnumerableSurrogate(SplayTreeRangeList tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = false;
                this.startStart = 0;
            }

            [Feature(Feature.Range, Feature.Range2)]
            public RobustEnumerableSurrogate(SplayTreeRangeList tree,[Widen] int startStart,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;
            }

            // IEnumerable

            public IEnumerator<EntryRangeList> GetEnumerator()
            {

                /*[Feature(Feature.Range, Feature.Range2)]*/
                if (startIndexed)
                {
                    return new RobustEnumerator(tree, startStart, forward);
                }

                return new RobustEnumerator(tree, forward);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public struct FastEnumerableSurrogate : IEnumerable<EntryRangeList>
        {
            private readonly SplayTreeRangeList tree;
            private readonly bool forward;

            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;

            // Construction

            public FastEnumerableSurrogate(SplayTreeRangeList tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = false;
                this.startStart = 0;
            }

            [Feature(Feature.Range, Feature.Range2)]
            public FastEnumerableSurrogate(SplayTreeRangeList tree,[Widen] int startStart,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;
            }

            // IEnumerable

            public IEnumerator<EntryRangeList> GetEnumerator()
            {

                /*[Feature(Feature.Range, Feature.Range2)]*/
                if (startIndexed)
                {
                    return new FastEnumerator(tree, startStart, forward);
                }

                return new FastEnumerator(tree, forward);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// This enumerator is robust in that it can continue to walk the tree even in the face of changes, because
        /// it keeps a current key and uses NearestGreater to find the next one. The enumerator also uses a constant
        /// amount of memory. However, since it uses queries it is slow, O(n lg(n)) to enumerate the entire tree.
        /// </summary>
        public class RobustEnumerator :
            IEnumerator<EntryRangeList>        {
            private readonly SplayTreeRangeList tree;
            private readonly bool forward;
            //
            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;

            private bool started;
            private bool valid;
            private ushort enumeratorVersion;
            //
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private int currentStart;
            //
            // saving the currentXStart with does not work well for range collections because it may shift, so making updates
            // is not permitted in range trees
            [Feature(Feature.Range, Feature.Range2)]
            private ushort treeVersion;

            public RobustEnumerator(SplayTreeRangeList tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Range, Feature.Range2)]
            public RobustEnumerator(SplayTreeRangeList tree,[Widen] int startStart,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;

                Reset();
            }

            public EntryRangeList Current
            {
                get
                {
                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    if (treeVersion != tree.version)
                    {
                        throw new InvalidOperationException();
                    }

                    if (valid)

                        // OR

                        /*[Feature(Feature.Range, Feature.Range2)]*/
                        {
                            /*[Widen]*/
                            int xStart = 0, xLength = 0;

                            /*[Feature(Feature.Range)]*/
                            xStart = currentStart;
                            tree.Get(
                                /*[Feature(Feature.Range, Feature.Range2)]*/currentStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/out xLength);

                            /*[Feature(Feature.Range, Feature.Range2)]*/
                            treeVersion = tree.version; // our query is ok

                            return new EntryRangeList(
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength);
                        }
                    return new EntryRangeList();
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                /*[Feature(Feature.Range, Feature.Range2)]*/
                if (this.treeVersion != tree.version)
                {
                    throw new InvalidOperationException();
                }

                this.enumeratorVersion = unchecked((ushort)(this.enumeratorVersion + 1));

                if (!started)
                {

                    // OR

                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    {
                        if (!startIndexed)
                        {
                            if (forward)
                            {
                                currentStart = 0;
                                valid = tree.xExtent != 0;
                            }
                            else
                            {
                                /*[Feature(Feature.Range)]*/
                                valid = tree.NearestLess(tree.xExtent, out currentStart);
                            }
                        }
                        else
                        {
                            if (forward)
                            {
                                valid = tree.NearestGreaterOrEqual(startStart, out currentStart);
                            }
                            else
                            {
                                valid = tree.NearestLessOrEqual(startStart, out currentStart);
                            }
                        }
                    }

                    started = true;
                }
                else if (valid)
                {

                    // OR

                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    if (forward)
                    {
                        valid = tree.NearestGreater(currentStart, out currentStart);
                    }
                    else
                    {
                        valid = tree.NearestLess(currentStart, out currentStart);
                    }
                }

                /*[Feature(Feature.Range, Feature.Range2)]*/
                treeVersion = tree.version; // our query is ok

                return valid;
            }

            public void Reset()
            {
                started = false;
                valid = false;

                /*[Feature(Feature.Range, Feature.Range2)]*/
                treeVersion = tree.version;
                this.enumeratorVersion = unchecked((ushort)(this.enumeratorVersion + 1));
            }
        }

        /// <summary>
        /// This enumerator is fast because it uses an in-order traversal of the tree that has O(1) cost per element.
        /// However, any change to the tree invalidates it, and that *includes queries* since a query causes a splay
        /// operation that changes the structure of the tree.
        /// Worse, this enumerator also uses a stack that can be as deep as the tree, and since the depth of a splay
        /// tree is in the worst case n (number of nodes), the stack can potentially be size n.
        /// </summary>
        public class FastEnumerator :
            IEnumerator<EntryRangeList>        {
            private readonly SplayTreeRangeList tree;
            private readonly bool forward;

            private readonly bool startKeyedOrIndexed;
            //
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;

            private ushort treeVersion;
            private ushort enumeratorVersion;

            private Node currentNode;
            private Node leadingNode;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart, nextXStart;
            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private int previousXStart;

            private readonly Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>> stack
                = new Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>>();

            public FastEnumerator(SplayTreeRangeList tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Range, Feature.Range2)]
            public FastEnumerator(SplayTreeRangeList tree,[Widen] int startStart,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyedOrIndexed = true;
                this.startStart = startStart;

                Reset();
            }

            public EntryRangeList Current
            {
                get
                {
                    if (currentNode != tree.Nil)
                    {


                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        int currentXLength = 0;

                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        if (forward)
                        {
                            currentXLength = nextXStart - currentXStart;
                        }
                        else
                        {
                            currentXLength = previousXStart - currentXStart;
                        }

                        return new EntryRangeList(
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXLength);
                    }
                    return new EntryRangeList();
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                Advance();
                return currentNode != tree.Nil;
            }

            public void Reset()
            {
                unchecked
                {
                    stack.Clear();

                    currentNode = tree.Nil;
                    leadingNode = tree.Nil;

                    this.treeVersion = tree.version;

                    // push search path to starting item

                    Node node = tree.root;
                    /*[Widen]*/
                    int xPosition = 0;
                    /*[Widen]*/
                    int yPosition = 0;

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    bool foundMatch = false;

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    bool lastGreaterAncestorValid = false;
                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    int xPositionLastGreaterAncestor = 0;

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    bool successorValid = false;
                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    int xPositionSuccessor = 0;
                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    int yPositionSuccessor = 0;
                    while (node != tree.Nil)
                    {
                        xPosition += node.xOffset;

                        bool foundMatch1 = false;
                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        foundMatch1 = foundMatch;

                        if (foundMatch1)
                        {
                            successorValid = true;
                            xPositionSuccessor = xPosition;
                            yPositionSuccessor = yPosition;
                        }

                        int c;
                        if (foundMatch1)
                        {
                            // don't compare anymore after finding match - only descend successor path
                            c = -1;
                        }
                        else
                        {
                            if (!startKeyedOrIndexed)
                            {
                                c = forward ? -1 : 1;
                            }
                            else
                            {
                                // OR
                                /*[Feature(Feature.Range)]*/
                                c = startStart.CompareTo(xPosition);
                            }
                        }

                        if (!foundMatch1 && (forward && (c <= 0)) || (!forward && (c >= 0)))
                        {
                            stack.Push(new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>(
                                node,
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition));
                        }

                        if (c == 0)
                        {
                            foundMatch = true;

                            // successor not needed for forward traversal
                            if (forward)
                            {
                                break;
                            }
                        }

                        if (c < 0)
                        {
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            if (!foundMatch)
                            {
                                lastGreaterAncestorValid = true;
                                xPositionLastGreaterAncestor = xPosition;
                            }

                            node = node.left;
                        }
                        else
                        {
                            Debug.Assert(c >= 0);
                            node = node.right;
                        }
                    }

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    if (!forward)
                    {
                        // get position of successor for length of initial item
                        if (successorValid)
                        {
                            nextXStart = xPositionSuccessor;
                        }
                        else if (lastGreaterAncestorValid)
                        {
                            nextXStart = xPositionLastGreaterAncestor;
                        }
                        else
                        {
                            nextXStart = tree.xExtent;
                        }
                    }

                    Advance();
                }
            }

            private void Advance()
            {
                unchecked
                {
                    if (this.treeVersion != tree.version)
                    {
                        throw new InvalidOperationException();
                    }

                    this.enumeratorVersion = unchecked((ushort)(this.enumeratorVersion + 1));

                    previousXStart = currentXStart;
                    currentNode = leadingNode;
                    currentXStart = nextXStart;

                    leadingNode = tree.Nil;

                    if (stack.Count == 0)
                    {
                        if (forward)
                        {
                            nextXStart = tree.xExtent;
                        }
                        else
                        {
                            nextXStart = 0;
                        }
                        return;
                    }

                    STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int> cursor
                        = stack.Pop();

                    leadingNode = cursor.Item1;
                    nextXStart = cursor.Item2;

                    Node node = forward ? leadingNode.right : leadingNode.left;
                    /*[Widen]*/
                    int xPosition = nextXStart;
                    while (node != tree.Nil)
                    {
                        xPosition += node.xOffset;

                        stack.Push(new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>(
                            node,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition));
                        node = forward ? node.left : node.right;
                    }
                }
            }
        }


        //
        // Cloning
        //

        public object Clone()
        {
            return new SplayTreeRangeList(this);
        }
    }
}
