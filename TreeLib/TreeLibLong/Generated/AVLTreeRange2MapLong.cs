// NOTE: This file is auto-generated. DO NOT MAKE CHANGES HERE! They will be overwritten on rebuild.

/*
 *  Copyright © 2016 Thomas R. Lawrence
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

// This implementation is adapted from Glib's AVL tree: https://github.com/GNOME/glib/blob/master/glib/gtree.c
// which is attributed to Maurizio Monge.

// An overview of AVL trees can be found here: https://en.wikipedia.org/wiki/AVL_tree

namespace TreeLib
{

    /// <summary>
    /// Implements a map, list or range collection using an AVL tree. 
    /// </summary>
    
    /// <summary>
    /// Represents an sequenced collection of range-to-range pairs with associated values. Each range pair is defined by two lengths,
    /// one for the X sequence and one for the Y sequence.
    /// With regard to a particular sequence, each range occupies a particular position in the sequence, determined by the location
    /// where it was inserted (and any insertions/deletions that have occurred before or after it in the sequence).
    /// Within the sequence, the start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// The above applies separately to both the X side sequence and the Y side sequence.
    /// All ranges must have a lengths of at least 1, on both sides.
    /// </summary>
    /// <typeparam name="ValueType">type of the value associated with each range pair</typeparam>
    public class AVLTreeRange2MapLong<[Payload(Payload.Value)] ValueType> :

        /*[Feature(Feature.Range2)]*//*[Payload(Payload.Value)]*//*[Widen]*/IRange2MapLong<ValueType>,
        INonInvasiveTreeInspection,
        /*[Feature(Feature.Range, Feature.Range2)]*//*[Widen]*/INonInvasiveRange2MapInspectionLong,
        IEnumerable<EntryRange2MapLong<ValueType>>,
        IEnumerable
    {
        //
        // Object form data structure
        //

        [Storage(Storage.Object)]
        private sealed class Node
        {
            public Node left, right;

            // tree is threaded: left_child/right_child indicate "non-null", if false, left/right point to predecessor/successor
            public bool left_child, right_child;
            public sbyte balance;
            [Payload(Payload.Value)]
            public ValueType value;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public long xOffset;
            [Feature(Feature.Range2)]
            [Widen]
            public long yOffset;
        }

        [ArrayIndexing]
        [Storage(Storage.Object)]
        private Node this[Node node] { get { return node; } }

        [Storage(Storage.Object)]
        private readonly static Node _Null = null;

        //
        // State for both array & object form
        //

        private Node Null { get { return AVLTreeRange2MapLong<ValueType>._Null; } } // allow tree.Null or this.Null in all cases

        private Node root;
        [Count]
        private ulong count;
        private ushort version;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private long xExtent;
        [Feature(Feature.Range2)]
        [Widen]
        private long yExtent;

        private readonly AllocationMode allocationMode;
        private Node freelist;

        private const int MAX_GTREE_HEIGHT = 40; // TODO: not valid for greater than 32 bits addressing
        private readonly WeakReference<Node[]> path = new WeakReference<Node[]>(null);


        //
        // Construction
        //

        // Object

        /// <summary>
        /// Create a new collection based on an AVL tree, explicitly configured.
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
        public AVLTreeRange2MapLong(uint capacity,AllocationMode allocationMode)
        {
            this.root = Null;

            this.allocationMode = allocationMode;
            this.freelist = Null;
            EnsureFree(capacity);
        }

        /// <summary>
        /// Create a new collection based on an AVL tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public AVLTreeRange2MapLong()
            : this(0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public AVLTreeRange2MapLong(AVLTreeRange2MapLong<ValueType> original)
        {
            throw new NotImplementedException(); // TODO: clone
        }


        //
        // IOrderedMap, IOrderedList
        //

        
        /// <summary>
        /// Returns the number of range pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue range pairs.</exception>
        public uint Count { get { return checked((uint)this.count); } }

        
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        public long LongCount { get { return unchecked((long)this.count); } }

        
        /// <summary>
        /// Removes all range pairs from the collection.
        /// </summary>
        public void Clear()
        {
            // no need to do any work for DynamicDiscard mode
            if (allocationMode != AllocationMode.DynamicDiscard)
            {
                // use threaded feature to traverse in O(1) per node with no stack

                Node node = g_tree_first_node();

                while (node != Null)
                {
                    Node next = g_tree_node_next(node);

                    this.count = unchecked(this.count - 1);
                    g_node_free(node);

                    node = next;
                }

                Debug.Assert(this.count == 0);
            }
            else
            {
                /*[Storage(Storage.Object)]*/
                {
#if DEBUG
                    allocateHelper.allocateCount = 0;
#endif
                }
            }

            root = Null;
            this.count = 0;
            this.xExtent = 0;
            this.yExtent = 0;
        }


        //
        // IRange2Map, IRange2List, IRangeMap, IRangeList
        //

        // Count { get; } - reuses Feature.Dict implementation

        
        /// <summary>
        /// Determines if there is a range pair in the collection starting at the index specified, with respect to the side specified.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool Contains([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            /*[Widen]*/
            long yPosition, yLength ;
            return FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition));
        }

        
        /// <summary>
        /// Attempt to insert a range pair defined by the given pair of lengths at the specified start index with respect to
        /// the specified side and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// The sequence of the non-specified side is also updated, by inserting the other length of the pair at the same
        /// rank in the sequence as on the specified side.
        /// </summary>
        /// <param name="start">the specified start index to insert before</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <param name="xLength">the length of the X side of the range pair. the length must be at least 1.</param>
        /// <param name="yLength">the length of the Y side of the range pair. the length must be at least 1.</param>
        /// <param name="value">the value to associate with the range pair</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue on either side</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryInsert([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long xLength,[Feature(Feature.Range2)][Widen] long yLength,[Payload(Payload.Value)] ValueType value)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (xLength <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            /*[Feature(Feature.Range2)]*/
            if (yLength <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return g_tree_insert_internal(
                /*[Payload(Payload.Value)]*/ value,
                start,
                /*[Feature(Feature.Range2)]*/ side,
                xLength,
                /*[Feature(Feature.Range2)]*/ yLength,
                true/*add*/,
                false/*update*/);
        }

        
        /// <summary>
        /// Attempt to delete the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <returns>true if a range pair was successfully deleted</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryDelete([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return g_tree_remove_internal(
                start,
                /*[Feature(Feature.Range2)]*/ side);
        }

        
        /// <summary>
        /// Attempt to query the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <param name="length">the length of the range from the specified side (X or Y)</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetLength([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long length)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            /*[Widen]*/
            long yPosition, yLength ;
            if (FindPosition(
start,
/*[Feature(Feature.Range2)]*/            side,
out node,
out xPosition,
/*[Feature(Feature.Range2)]*/out yPosition,
out xLength,
/*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                length = side == Side.X ? xLength : yLength;
                return true;
            }
            length = 0;
            return false;
        }

        
        /// <summary>
        /// Attempt to change the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to modify</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="length">the new length to apply on the specified side (X or Y) of the range pair. The new length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetLength([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            /*[Widen]*/
            long yPosition, yLength ;
            if (FindPosition(
start,
/*[Feature(Feature.Range2)]*/            side,
out node,
out xPosition,
/*[Feature(Feature.Range2)]*/out yPosition,
out xLength,
/*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                /*[Widen]*/
                long adjust = length - (side == Side.X ? xLength : yLength) ;
                /*[Widen]*/
                long xAdjust = 0 ;
                /*[Widen]*/
                long yAdjust = 0 ;
                if (side == Side.X)
                {
                    xAdjust = adjust;
                }
                else
                {
                    yAdjust = adjust;
                }

                this.xExtent = checked(this.xExtent + xAdjust);
                this.yExtent = checked(this.yExtent + yAdjust);

                ShiftRightOfPath(
start + 1,
/*[Feature(Feature.Range2)]*/                side,
xAdjust,
/*[Feature(Feature.Range2)]*/yAdjust);

                return true;
            }
            return false;
        }

        
        /// <summary>
        /// Attempt to query the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index on the specified side</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetValue([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,out ValueType value)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            /*[Widen]*/
            long yPosition, yLength ;
            if (FindPosition(
start,
/*[Feature(Feature.Range2)]*/            side,
out node,
out xPosition,
/*[Feature(Feature.Range2)]*/out yPosition,
out xLength,
/*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                value = node.value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Attempt to update the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to update</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetValue([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,ValueType value)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            /*[Widen]*/
            long yPosition, yLength ;
            if (FindPosition(
start,
/*[Feature(Feature.Range2)]*/            side,
out node,
out xPosition,
/*[Feature(Feature.Range2)]*/out yPosition,
out xLength,
/*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                node.value = value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Attempt to get the value and lengths associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="otherStart">out parameter receiving the start index of the range from the opposite side of that specified</param>
        /// <param name="xLength">out parameter receiving the length of the range on the X side</param>
        /// <param name="yLength">out parameter receiving the length f the range on the Y side</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Feature(Feature.Range2)] [Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength,[Payload(Payload.Value)] out ValueType value)
        {
            Node node;
            /*[Widen]*/
            long xPosition ;
            /*[Widen]*/
            long yPosition ;
            if (FindPosition(
start,
/*[Feature(Feature.Range2)]*/            side,
out node,
out xPosition,
/*[Feature(Feature.Range2)]*/out yPosition,
out xLength,
/*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                otherStart = side != Side.X ? xPosition : yPosition;
                value = node.value;
                return true;
            }
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Insert a range pair defined by the given pair of lengths at the specified start index with respect to
        /// the specified side and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// The sequence of the non-specified side is also updated, by inserting the other length of the pair at the same
        /// rank in the sequence as on the specified side.
        /// </summary>
        /// <param name="start">the specified start index to insert before</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <param name="xLength">the length of the X side of the range pair. the length must be at least 1.</param>
        /// <param name="yLength">the length of the Y side of the range pair. the length must be at least 1.</param>
        /// <param name="value">the value to associate with the range pair</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue on either side</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Insert([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long xLength,[Feature(Feature.Range2)] [Widen] long yLength,[Payload(Payload.Value)] ValueType value)
        {
            if (!TryInsert(start, /*[Feature(Feature.Range2)]*/side, xLength, /*[Feature(Feature.Range2)]*/yLength, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        
        /// <summary>
        /// Deletes the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Delete([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            if (!TryDelete(start, /*[Feature(Feature.Range2)]*/side))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <returns>the length of the range from the specified side (X or Y)</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public long GetLength([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            /*[Widen]*/
            long length ;
            if (!TryGetLength(
start,
/*[Feature(Feature.Range2)]*/            side,
out length))
            {
                throw new ArgumentException("item not in tree");
            }
            return length;
        }

        
        /// <summary>
        /// Changes the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to modify</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="length">the new length to apply on the specified side (X or Y) of the range pair. The new length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void SetLength([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long length)
        {
            if (!TrySetLength(start, /*[Feature(Feature.Range2)]*/side, length))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <returns>the value associated with the range</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public ValueType GetValue([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            ValueType value;
            if (!TryGetValue(start, /*[Feature(Feature.Range2)]*/side, out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        
        /// <summary>
        /// Updates the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to update</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public void SetValue([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,ValueType value)
        {
            if (!TrySetValue(start, /*[Feature(Feature.Range2)]*/side, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Attempt to get the value and lengths associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; indexes to the interior of a range are not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="otherStart">out parameter receiving the start index of the range from the opposite side of that specified</param>
        /// <param name="xLength">out parameter receiving the length of the range on the X side</param>
        /// <param name="yLength">out parameter receiving the length f the range on the Y side</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Get([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength,[Payload(Payload.Value)] out ValueType value)
        {
            if (!TryGet(start, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the extent of the sequence of ranges on the specified side. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <param name="side">the side (X or Y) to which the query applies.</param>
        /// <returns>the extent of the ranges on the specified side</returns>
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public long GetExtent([Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return side == Side.X ? this.xExtent : this.yExtent;
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index with respect to the specified side.
        /// Use this method to convert indexes to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestLessOrEqual([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long nearestStart)
        {
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Range2)]*/ side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                true/*orEqual*/);
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestLess([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long nearestStart)
        {
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Range2)]*/ side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                false/*orEqual*/);
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestGreaterOrEqual([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long nearestStart)
        {
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Range2)]*/ side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                true/*orEqual*/);
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestGreater([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long nearestStart)
        {
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Range2)]*/ side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                false/*orEqual*/);
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
        private Node g_tree_node_new([Payload(Payload.Value)] ValueType value)
        {
            Node node = freelist;
            if (node != Null)
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
            node.value = value;
            node.left = Null;
            node.left_child = false;
            node.right = Null;
            node.right_child = false;
            node.balance = 0;
            node.xOffset = 0;
            node.yOffset = 0;

            return node;
        }

        [Storage(Storage.Object)]
        private void g_node_free(Node node)
        {
#if DEBUG
            allocateHelper.allocateCount = checked(allocateHelper.allocateCount - 1);
            Debug.Assert(allocateHelper.allocateCount == this.count);

            node.left = Null;
            node.left_child = true;
            node.right = Null;
            node.right_child = true;
            node.balance = SByte.MinValue;
            node.xOffset = Int32.MinValue;
            node.yOffset = Int32.MinValue;
#endif

            if (allocationMode != AllocationMode.DynamicDiscard)
            {
                node.value = default(ValueType); // clear any references for GC

                node.left = freelist;
                freelist = node;
            }
        }

        [Storage(Storage.Object)]
        private void EnsureFree(uint capacity)
        {
            unchecked
            {
                Debug.Assert(freelist == Null);
                for (uint i = 0; i < capacity - this.count; i++)
                {
                    Node node = new Node();
                    node.left = freelist;
                    freelist = node;
                }
            }
        }


        private Node g_tree_first_node()
        {
            if (root == Null)
            {
                return Null;
            }

            Node tmp = root;

            while (tmp.left_child)
            {
                tmp = tmp.left;
            }

            return tmp;
        }

        private Node g_tree_last_node()
        {
            if (root == Null)
            {
                return Null;
            }

            Node tmp = root;

            while (tmp.right_child)
            {
                tmp = tmp.right;
            }

            return tmp;
        }

        private bool NearestLess(
            out Node nearestNode,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,            bool orEqual)
        {
            unchecked
            {
                Node lastLess = Null;
                /*[Widen]*/
                long xPositionLastLess = 0 ;
                /*[Widen]*/
                long yPositionLastLess = 0 ;
                if (root != Null)
                {
                    Node node = root;
                    {
                        /*[Widen]*/
                        long xPosition = 0 ;
                        /*[Widen]*/
                        long yPosition = 0 ;
                        while (true)
                        {
                            xPosition += node.xOffset;
                            yPosition += node.yOffset;

                            int c;
                            {
                                Debug.Assert(CompareKeyMode.Position == CompareKeyMode.Position);
                                c = position.CompareTo(side == Side.X ? xPosition : yPosition);
                            }
                            if (orEqual && (c == 0))
                            {
                                nearestNode = node;
                                nearestStart = side == Side.X ? xPosition : yPosition;
                                return true;
                            }
                            Node next;
                            if (c <= 0)
                            {
                                if (!node.left_child)
                                {
                                    break;
                                }
                                next = node.left;
                            }
                            else
                            {
                                lastLess = node;
                                xPositionLastLess = xPosition;
                                yPositionLastLess = yPosition;

                                if (!node.right_child)
                                {
                                    break;
                                }
                                next = node.right;
                            }
                            if (next == Null)
                            {
                                break;
                            }
                            node = next;
                        }
                    }
                }
                if (lastLess != Null)
                {
                    nearestNode = lastLess;
                    nearestStart = side == Side.X ? xPositionLastLess : yPositionLastLess;
                    return true;
                }
                nearestNode = Null;
                nearestStart = 0;
                return false;
            }
        }

        private bool NearestGreater(
            out Node nearestNode,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,            bool orEqual)
        {
            unchecked
            {
                Node lastGreater = Null;
                /*[Widen]*/
                long xPositionLastGreater = 0 ;
                /*[Widen]*/
                long yPositionLastGreater = 0 ;
                if (root != Null)
                {
                    Node node = root;
                    if (node != Null)
                    {
                        /*[Widen]*/
                        long xPosition = 0 ;
                        /*[Widen]*/
                        long yPosition = 0 ;
                        while (true)
                        {
                            xPosition += node.xOffset;
                            yPosition += node.yOffset;

                            int c;
                            {
                                Debug.Assert(CompareKeyMode.Position == CompareKeyMode.Position);
                                c = position.CompareTo(side == Side.X ? xPosition : yPosition);
                            }
                            if (orEqual && (c == 0))
                            {
                                nearestNode = node;
                                nearestStart = side == Side.X ? xPosition : yPosition;
                                return true;
                            }
                            Node next;
                            if (c < 0)
                            {
                                lastGreater = node;
                                xPositionLastGreater = xPosition;
                                yPositionLastGreater = yPosition;

                                if (!node.left_child)
                                {
                                    break;
                                }
                                next = node.left;
                            }
                            else
                            {
                                if (!node.right_child)
                                {
                                    break;
                                }
                                next = node.right;
                            }
                            if (next == Null)
                            {
                                break;
                            }
                            node = next;
                        }
                    }
                }
                if (lastGreater != Null)
                {
                    nearestNode = lastGreater;
                    nearestStart = side == Side.X ? xPositionLastGreater : yPositionLastGreater;
                    return true;
                }
                nearestNode = Null;
                nearestStart = side == Side.X ? this.xExtent : this.yExtent;
                return false;
            }
        }

        private Node g_tree_node_previous(Node node)
        {
            Node tmp = node.left;

            if (node.left_child)
            {
                while (tmp.right_child)
                {
                    tmp = tmp.right;
                }
            }

            return tmp;
        }

        private Node g_tree_node_next(Node node)
        {
            Node tmp = node.right;

            if (node.right_child)
            {
                while (tmp.left_child)
                {
                    tmp = tmp.left;
                }
            }

            return tmp;
        }

        private Node[] RetrievePathWorkspace()
        {
            Node[] path;
            this.path.TryGetTarget(out path);
            if (path == null)
            {
                path = new Node[MAX_GTREE_HEIGHT];
                this.path.SetTarget(path);
            }
            return path;
        }

        // NOTE: replace mode does *not* adjust for xLength/yLength!
        private bool g_tree_insert_internal(
            [Payload(Payload.Value)] ValueType value,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xLength,            [Feature(Feature.Range2)][Widen] long yLength,            bool add,            bool update)
        {
            unchecked
            {
                if (root == Null)
                {
                    if (!add)
                    {
                        return false;
                    }

                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    {
                        if (position != 0)
                        {
                            return false;
                        }
                    }

                    root = g_tree_node_new(/*[Payload(Payload.Value)]*/value);
                    Debug.Assert(root.xOffset == 0);
                    Debug.Assert(root.yOffset == 0);
                    Debug.Assert(this.xExtent == 0);
                    Debug.Assert(this.yExtent == 0);
                    this.xExtent = xLength;
                    this.yExtent = yLength;

                    Debug.Assert(this.count == 0);
                    this.count = 1;
                    // TODO: this.version = unchecked(this.version + 1);

                    return true;
                }

                Node[] path = RetrievePathWorkspace();
                int idx = 0;
                path[idx++] = Null;
                Node node = root;

                Node successor = Null;
                /*[Widen]*/
                long xPositionSuccessor = 0 ;
                /*[Widen]*/
                long yPositionSuccessor = 0 ;
                /*[Widen]*/
                long xPositionNode = 0 ;
                /*[Widen]*/
                long yPositionNode = 0 ;
                bool addleft = false;
                {
                    Node addBelow = Null;
                    /*[Widen]*/
                    long xPositionAddBelow = 0 ;
                    /*[Widen]*/
                    long yPositionAddBelow = 0 ;
                    while (true)
                    {
                        xPositionNode += node.xOffset;
                        yPositionNode += node.yOffset;

                        int cmp;
                        if (addBelow != Null)
                        {
                            cmp = -1; // we don't need to compare any more once we found the match
                        }
                        else
                        {
                            {
                                Debug.Assert(CompareKeyMode.Position == CompareKeyMode.Position);
                                cmp = position.CompareTo(side == Side.X ? xPositionNode : yPositionNode);
                                if (add && (cmp == 0))
                                {
                                    cmp = -1; // node never found for sparse range mode
                                }
                            }
                        }

                        if (cmp == 0)
                        {
                            if (update)
                            {
                                node.value = value;
                            }
                            return !add;
                        }

                        if (cmp < 0)
                        {
                            successor = node;
                            xPositionSuccessor = xPositionNode;
                            yPositionSuccessor = yPositionNode;

                            if (node.left_child)
                            {
                                bool push = true;
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                                push = addBelow == Null;
                                if (push)
                                {
                                    path[idx++] = node;
                                }
                                node = node.left;
                            }
                            else
                            {
                                // precedes node

                                if (!add)
                                {
                                    return false;
                                }

                                bool setAddBelow = true;
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                                setAddBelow = addBelow == Null;
                                if (setAddBelow)
                                {
                                    addBelow = node;
                                    xPositionAddBelow = xPositionNode;
                                    yPositionAddBelow = yPositionNode;
                                    addleft = true;
                                }

                                // always break:
                                // if inserting as left child of node, node is successor
                                // if ending right subtree successor search, node is successor
                                break;
                            }
                        }
                        else
                        {
                            Debug.Assert(cmp > 0);

                            if (node.right_child)
                            {
                                bool push = true;
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                                push = addBelow == Null;
                                if (push)
                                {
                                    path[idx++] = node;
                                }
                                node = node.right;
                            }
                            else
                            {
                                // follows node

                                if (!add)
                                {
                                    return false;
                                }

                                addBelow = node;
                                xPositionAddBelow = xPositionNode;
                                yPositionAddBelow = yPositionNode;
                                addleft = false;

                                /*Feature(Feature.Dict)*/
                                break; // truncate search early if no augmentation, else...

                                // continue the search in right sub tree after we find a match (to find successor)
                                if (!node.right_child)
                                {
                                    break;
                                }
                                node = node.right;
                            }
                        }
                    }

                    node = addBelow;
                    xPositionNode = xPositionAddBelow;
                    yPositionNode = yPositionAddBelow;
                }

                if (addleft)
                {
                    // precedes node

                    Debug.Assert(node == successor);

                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        long positionNode = side == Side.X ? xPositionNode : yPositionNode ;
                        if (position != positionNode)
                        {
                            return false;
                        }
                    }

                    this.version = unchecked((ushort)(this.version + 1));

                    // throw here before modifying tree
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;
                    /*[Widen]*/
                    long yExtentNew = checked(this.yExtent + yLength) ;
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    Node child = g_tree_node_new(/*[Payload(Payload.Value)]*/value);

                    ShiftRightOfPath(xPositionNode, /*[Feature(Feature.Range2)]*/Side.X, xLength, /*[Feature(Feature.Range2)]*/yLength);

                    child.left = node.left;
                    child.right = node;
                    node.left = child;
                    node.left_child = true;
                    node.balance--;

                    child.xOffset = -xLength;
                    child.yOffset = -yLength;

                    this.xExtent = xExtentNew;
                    this.yExtent = yExtentNew;
                    this.count = countNew;
                }
                else
                {
                    // follows node

                    Debug.Assert(!node.right_child);

                    /*[Widen]*/
                    long xLengthNode ;
                    /*[Widen]*/
                    long yLengthNode ;
                    if (successor != Null)
                    {
                        xLengthNode = xPositionSuccessor - xPositionNode;
                        yLengthNode = yPositionSuccessor - yPositionNode;
                    }
                    else
                    {
                        xLengthNode = this.xExtent - xPositionNode;
                        yLengthNode = this.yExtent - yPositionNode;
                    }

                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    if ((CompareKeyMode.Position == CompareKeyMode.Position)
                        && (position != (side == Side.X ? xPositionNode + xLengthNode : yPositionNode + yLengthNode)))
                    {
                        return false;
                    }

                    this.version = unchecked((ushort)(this.version + 1));

                    // throw here before modifying tree
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;
                    /*[Widen]*/
                    long yExtentNew = checked(this.yExtent + yLength) ;
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    Node child = g_tree_node_new(/*[Payload(Payload.Value)]*/value);

                    ShiftRightOfPath(xPositionNode + 1, /*[Feature(Feature.Range2)]*/Side.X, xLength, /*[Feature(Feature.Range2)]*/yLength);

                    child.right = node.right;
                    child.left = node;
                    node.right = child;
                    node.right_child = true;
                    node.balance++;

                    child.xOffset = xLengthNode;
                    child.yOffset = yLengthNode;

                    this.xExtent = xExtentNew;
                    this.yExtent = yExtentNew;
                    this.count = countNew;
                }

                // Restore balance. This is the goodness of a non-recursive
                // implementation, when we are done with balancing we 'break'
                // the loop and we are done.
                while (true)
                {
                    Node bparent = path[--idx];
                    bool left_node = (bparent != Null) && (node == bparent.left);
                    Debug.Assert((bparent == Null) || (bparent.left == node) || (bparent.right == node));

                    if ((node.balance < -1) || (node.balance > 1))
                    {
                        node = g_tree_node_balance(node);
                        if (bparent == Null)
                        {
                            root = node;
                        }
                        else if (left_node)
                        {
                            bparent.left = node;
                        }
                        else
                        {
                            bparent.right = node;
                        }
                    }

                    if ((node.balance == 0) || (bparent == Null))
                    {
                        break;
                    }

                    if (left_node)
                    {
                        bparent.balance--;
                    }
                    else
                    {
                        bparent.balance++;
                    }

                    node = bparent;
                }

                return true;
            }
        }

        private bool g_tree_remove_internal(
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            unchecked
            {
                if (root == Null)
                {
                    return false;
                }

                Node[] path = RetrievePathWorkspace();
                int idx = 0;
                path[idx++] = Null;

                Node node = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionNode = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionNode = 0 ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionParent = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionParent = 0 ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                Node lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionLastGreaterAncestor = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionLastGreaterAncestor = 0 ;
                while (true)
                {
                    Debug.Assert(node != Null);

                    xPositionNode += node.xOffset;
                    yPositionNode += node.yOffset;

                    int cmp;
                    {
                        Debug.Assert(CompareKeyMode.Position == CompareKeyMode.Position);
                        cmp = position.CompareTo(side == Side.X ? xPositionNode : yPositionNode);
                    }

                    if (cmp == 0)
                    {
                        break;
                    }

                    xPositionParent = xPositionNode;
                    yPositionParent = yPositionNode;

                    if (cmp < 0)
                    {
                        if (!node.left_child)
                        {
                            return false;
                        }

                        lastGreaterAncestor = node;
                        xPositionLastGreaterAncestor = xPositionNode;
                        yPositionLastGreaterAncestor = yPositionNode;

                        path[idx++] = node;
                        node = node.left;
                    }
                    else
                    {
                        if (!node.right_child)
                        {
                            return false;
                        }

                        path[idx++] = node;
                        node = node.right;
                    }
                }

                this.version = unchecked((ushort)(this.version + 1));

                Node successor;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionSuccessor ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionSuccessor ;

                // The following code is almost equal to g_tree_remove_node,
                // except that we do not have to call g_tree_node_parent.
                Node parent, balance;
                balance = parent = path[--idx];
                Debug.Assert((parent == Null) || (parent.left == node) || (parent.right == node));
                bool left_node = (parent != Null) && (node == parent.left);

                if (!node.left_child)
                {
                    if (!node.right_child) // node has no children
                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            successor = lastGreaterAncestor;
                            xPositionSuccessor = xPositionLastGreaterAncestor;
                            yPositionSuccessor = yPositionLastGreaterAncestor;
                            Debug.Assert(successor == g_tree_node_next(node));
                        }

                        if (parent == Null)
                        {
                            root = Null;
                        }
                        else if (left_node)
                        {
                            parent.left_child = false;
                            parent.left = node.left;
                            parent.balance++;
                        }
                        else
                        {
                            parent.right_child = false;
                            parent.right = node.right;
                            parent.balance--;
                        }
                    }
                    else // node has a right child
                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        xPositionSuccessor = xPositionNode;
                        /*[Feature(Feature.Range2)]*/
                        yPositionSuccessor = yPositionNode;

                        /*Feature(Feature.Dict)*/
                        successor = g_tree_node_next(node);
                        // OR
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            successor = node.right;
                            xPositionSuccessor += successor.xOffset;
                            yPositionSuccessor += successor.yOffset;
                            while (successor.left_child)
                            {
                                successor = successor.left;
                                xPositionSuccessor += successor.xOffset;
                                yPositionSuccessor += successor.yOffset;
                            }
                            Debug.Assert(successor == g_tree_node_next(node));
                        }

                        if (node.left_child)
                        {
                            node.left.xOffset += xPositionNode - xPositionSuccessor;
                            node.left.yOffset += yPositionNode - yPositionSuccessor;
                        }
                        successor.left = node.left;

                        Node rightChild = node.right;
                        rightChild.xOffset += node.xOffset;
                        rightChild.yOffset += node.yOffset;
                        if (parent == Null)
                        {
                            root = rightChild;
                        }
                        else if (left_node)
                        {
                            parent.left = rightChild;
                            parent.balance++;
                        }
                        else
                        {
                            parent.right = rightChild;
                            parent.balance--;
                        }
                    }
                }
                else // node has a left child
                {
                    if (!node.right_child)
                    {
                        Node predecessor;
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        long xPositionPredecessor = xPositionNode ;
                        /*[Feature(Feature.Range2)]*/
                        /*[Widen]*/
                        long yPositionPredecessor = yPositionNode ;

                        /*Feature(Feature.Dict)*/
                        predecessor = g_tree_node_previous(node);
                        // OR
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            predecessor = node;
                            xPositionPredecessor += predecessor.xOffset;
                            yPositionPredecessor += predecessor.yOffset;
                            while (predecessor.left_child)
                            {
                                predecessor = predecessor.left;
                                xPositionPredecessor += predecessor.xOffset;
                                yPositionPredecessor += predecessor.yOffset;
                            }
                            Debug.Assert(predecessor == g_tree_node_previous(node));
                        }

                        // and successor
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            successor = lastGreaterAncestor;
                            xPositionSuccessor = xPositionLastGreaterAncestor;
                            yPositionSuccessor = yPositionLastGreaterAncestor;
                            Debug.Assert(successor == g_tree_node_next(node));
                        }

                        if (node.right_child)
                        {
                            node.right.xOffset += xPositionNode - xPositionPredecessor;
                            node.right.yOffset += yPositionNode - yPositionPredecessor;
                        }
                        predecessor.right = node.right;

                        Node leftChild = node.left;
                        leftChild.xOffset += node.xOffset;
                        leftChild.yOffset += node.yOffset;
                        if (parent == Null)
                        {
                            root = leftChild;
                        }
                        else if (left_node)
                        {
                            parent.left = leftChild;
                            parent.balance++;
                        }
                        else
                        {
                            parent.right = leftChild;
                            parent.balance--;
                        }
                    }
                    else // node has a both children (pant, pant!)
                    {
                        Node predecessor = node.left;
                        successor = node.right;
                        Node successorParent = node;
                        int old_idx = idx + 1;
                        idx++;
                        xPositionSuccessor = xPositionNode + successor.xOffset;
                        yPositionSuccessor = yPositionNode + successor.yOffset;

                        /* path[idx] == parent */
                        /* find the immediately next node (and its parent) */
                        while (successor.left_child)
                        {
                            path[++idx] = successorParent = successor;
                            successor = successor.left;

                            xPositionSuccessor += successor.xOffset;
                            yPositionSuccessor += successor.yOffset;
                        }

                        path[old_idx] = successor;
                        balance = path[idx];

                        /* remove 'successor' from the tree */
                        if (successorParent != node)
                        {
                            if (successor.right_child)
                            {
                                Node successorRightChild = successor.right;

                                successorParent.left = successorRightChild;

                                successorRightChild.xOffset += successor.xOffset;
                                successorRightChild.yOffset += successor.yOffset;
                            }
                            else
                            {
                                successorParent.left_child = false;
                            }
                            successorParent.balance++;

                            successor.right_child = true;
                            successor.right = node.right;

                            node.right.xOffset += xPositionNode - xPositionSuccessor;
                            node.right.yOffset += yPositionNode - yPositionSuccessor;
                        }
                        else
                        {
                            node.balance--;
                        }

                        // set the predecessor's successor link to point to the right place
                        while (predecessor.right_child)
                        {
                            predecessor = predecessor.right;
                        }
                        predecessor.right = successor;

                        /* prepare 'successor' to replace 'node' */
                        Node leftChild = node.left;
                        successor.left_child = true;
                        successor.left = leftChild;
                        successor.balance = node.balance;
                        leftChild.xOffset += xPositionNode - xPositionSuccessor;
                        leftChild.yOffset += yPositionNode - yPositionSuccessor;

                        if (parent == Null)
                        {
                            root = successor;
                        }
                        else if (left_node)
                        {
                            parent.left = successor;
                        }
                        else
                        {
                            parent.right = successor;
                        }

                        successor.xOffset = xPositionSuccessor - xPositionParent;
                        successor.yOffset = yPositionSuccessor - yPositionParent;
                    }
                }

                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                {
                    /*[Widen]*/
                    long xLength ;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    long yLength ;

                    if (successor != Null)
                    {
                        xLength = xPositionSuccessor - xPositionNode;
                        yLength = yPositionSuccessor - yPositionNode;
                    }
                    else
                    {
                        xLength = this.xExtent - xPositionNode;
                        yLength = this.yExtent - yPositionNode;
                    }

                    ShiftRightOfPath(xPositionNode + 1, /*[Feature(Feature.Range2)]*/Side.X, -xLength, /*[Feature(Feature.Range2)]*/-yLength);

                    this.xExtent = unchecked(this.xExtent - xLength);
                    this.yExtent = unchecked(this.yExtent - yLength);
                }

                /* restore balance */
                if (balance != Null)
                {
                    while (true)
                    {
                        Node bparent = path[--idx];
                        Debug.Assert((bparent == Null) || (bparent.left == balance) || (bparent.right == balance));
                        left_node = (bparent != Null) && (balance == bparent.left);

                        if ((balance.balance < -1) || (balance.balance > 1))
                        {
                            balance = g_tree_node_balance(balance);
                            if (bparent == Null)
                            {
                                root = balance;
                            }
                            else if (left_node)
                            {
                                bparent.left = balance;
                            }
                            else
                            {
                                bparent.right = balance;
                            }
                        }

                        if ((balance.balance != 0) || (bparent == Null))
                        {
                            break;
                        }

                        if (left_node)
                        {
                            bparent.balance++;
                        }
                        else
                        {
                            bparent.balance--;
                        }

                        balance = bparent;
                    }
                }


                this.count = unchecked(this.count - 1);
                Debug.Assert((this.count == 0) == (root == Null));

                g_node_free(node);

                return true;
            }
        }

        // DOES NOT adjust xExtent and yExtent!
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private void ShiftRightOfPath(
            [Widen] long position,            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,            [Widen] long xAdjust,            [Feature(Feature.Range2)][Widen] long yAdjust)
        {
            unchecked
            {
                this.version = unchecked((ushort)(this.version + 1));

                if (root != Null)
                {
                    /*[Widen]*/
                    long xPositionCurrent = 0 ;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    long yPositionCurrent = 0 ;
                    Node current = root;
                    while (true)
                    {
                        xPositionCurrent += current.xOffset;
                        yPositionCurrent += current.yOffset;

                        int order = position.CompareTo(side == Side.X ? xPositionCurrent : yPositionCurrent);
                        if (order <= 0)
                        {
                            xPositionCurrent += xAdjust;
                            yPositionCurrent += yAdjust;
                            current.xOffset += xAdjust;
                            current.yOffset += yAdjust;
                            if (current.left_child)
                            {
                                current.left.xOffset -= xAdjust;
                                current.left.yOffset -= yAdjust;
                            }

                            if (order == 0)
                            {
                                break;
                            }
                            if (!current.left_child)
                            {
                                break;
                            }
                            current = current.left;
                        }
                        else
                        {
                            if (!current.right_child)
                            {
                                break;
                            }
                            current = current.right;
                        }
                    }
                }
            }
        }

        private int g_tree_height()
        {
            unchecked
            {
                if (root == Null)
                {
                    return 0;
                }

                int height = 0;
                Node node = root;

                while (true)
                {
                    height += 1 + Math.Max((int)node.balance, 0);

                    if (!node.left_child)
                    {
                        return height;
                    }

                    node = node.left;
                }
            }
        }

        private Node g_tree_node_balance(Node node)
        {
            unchecked
            {
                if (node.balance < -1)
                {
                    if (node.left.balance > 0)
                    {
                        node.left = g_tree_node_rotate_left(node.left);
                    }
                    node = g_tree_node_rotate_right(node);
                }
                else if (node.balance > 1)
                {
                    if (node.right.balance < 0)
                    {
                        node.right = g_tree_node_rotate_right(node.right);
                    }
                    node = g_tree_node_rotate_left(node);
                }

                return node;
            }
        }

        private Node g_tree_node_rotate_left(Node node)
        {
            unchecked
            {
                Node right = node.right;

                /*[Widen]*/
                long xOffsetNode = node.xOffset ;
                /*[Widen]*/
                long yOffsetNode = node.yOffset ;
                /*[Widen]*/
                long xOffsetRight = right.xOffset ;
                /*[Widen]*/
                long yOffsetRight = right.yOffset ;
                node.xOffset = -xOffsetRight;
                node.yOffset = -yOffsetRight;
                right.xOffset += xOffsetNode;
                right.yOffset += yOffsetNode;

                if (right.left_child)
                {
                    right.left.xOffset += xOffsetRight;
                    right.left.yOffset += yOffsetRight;

                    node.right = right.left;
                }
                else
                {
                    node.right_child = false;
                    right.left_child = true;
                }
                right.left = node;

                int a_bal = node.balance;
                int b_bal = right.balance;

                if (b_bal <= 0)
                {
                    if (a_bal >= 1)
                    {
                        right.balance = (sbyte)(b_bal - 1);
                    }
                    else
                    {
                        right.balance = (sbyte)(a_bal + b_bal - 2);
                    }
                    node.balance = (sbyte)(a_bal - 1);
                }
                else
                {
                    if (a_bal <= b_bal)
                    {
                        right.balance = (sbyte)(a_bal - 2);
                    }
                    else
                    {
                        right.balance = (sbyte)(b_bal - 1);
                    }
                    node.balance = (sbyte)(a_bal - b_bal - 1);
                }

                return right;
            }
        }

        private Node g_tree_node_rotate_right(Node node)
        {
            unchecked
            {
                Node left = node.left;

                /*[Widen]*/
                long xOffsetNode = node.xOffset ;
                /*[Widen]*/
                long yOffsetNode = node.yOffset ;
                /*[Widen]*/
                long xOffsetLeft = left.xOffset ;
                /*[Widen]*/
                long yOffsetLeft = left.yOffset ;
                node.xOffset = -xOffsetLeft;
                node.yOffset = -yOffsetLeft;
                left.xOffset += xOffsetNode;
                left.yOffset += yOffsetNode;

                if (left.right_child)
                {
                    left.right.xOffset += xOffsetLeft;
                    left.right.yOffset += yOffsetLeft;

                    node.left = left.right;
                }
                else
                {
                    node.left_child = false;
                    left.right_child = true;
                }
                left.right = node;

                int a_bal = node.balance;
                int b_bal = left.balance;

                if (b_bal <= 0)
                {
                    if (b_bal > a_bal)
                    {
                        left.balance = (sbyte)(b_bal + 1);
                    }
                    else
                    {
                        left.balance = (sbyte)(a_bal + 2);
                    }
                    node.balance = (sbyte)(a_bal - b_bal + 1);
                }
                else
                {
                    if (a_bal <= -1)
                    {
                        left.balance = (sbyte)(b_bal + 1);
                    }
                    else
                    {
                        left.balance = (sbyte)(a_bal + b_bal + 2);
                    }
                    node.balance = (sbyte)(a_bal + 1);
                }

                return left;
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool FindPosition(
            [Widen] long position,            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,            out Node lastLessEqual,            [Widen] out long xPositionLastLessEqual,            [Feature(Feature.Range2)][Widen] out long yPositionLastLessEqual,            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long xLength,            [Feature(Feature.Range2)][Widen] out long yLength)
        {
            unchecked
            {
                lastLessEqual = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                xPositionLastLessEqual = 0;
                /*[Feature(Feature.Range2)]*/
                yPositionLastLessEqual = 0;
                xLength = 0;
                yLength = 0;

                Node successor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionSuccessor = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionSuccessor = 0 ;
                if (root != Null)
                {
                    Node current = root;
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    long xPositionCurrent = 0 ;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    long yPositionCurrent = 0 ;
                    while (true)
                    {
                        xPositionCurrent += current.xOffset;
                        yPositionCurrent += current.yOffset;

                        if (position < (side == Side.X ? xPositionCurrent : yPositionCurrent))
                        {
                            successor = current;
                            xPositionSuccessor = xPositionCurrent;
                            yPositionSuccessor = yPositionCurrent;

                            if (!current.left_child)
                            {
                                break;
                            }
                            current = current.left;
                        }
                        else
                        {
                            lastLessEqual = current;
                            xPositionLastLessEqual = xPositionCurrent;
                            yPositionLastLessEqual = yPositionCurrent;

                            if (!current.right_child)
                            {
                                break;
                            }
                            current = current.right; // try to find successor
                        }
                    }
                }
                if ((successor != Null) && (successor != lastLessEqual))
                {
                    xLength = xPositionSuccessor - xPositionLastLessEqual;
                    yLength = yPositionSuccessor - yPositionLastLessEqual;
                }
                else
                {
                    xLength = this.xExtent - xPositionLastLessEqual;
                    yLength = this.yExtent - yPositionLastLessEqual;
                }

                return (position >= 0) && (position < (side == Side.X ? this.xExtent : this.yExtent));
            }
        }


        //
        // Non-invasive tree inspection support
        //

        // Helpers

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private void ValidateRanges([Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            if (root != Null)
            {
                Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>>();

                /*[Widen]*/
                long offset = 0 ;
                /*[Widen]*/
                long leftEdge = 0 ;
                /*[Widen]*/
                long rightEdge = side == Side.X ? this.xExtent : this.yExtent ;

                Node node = root;
                while (node != Null)
                {
                    offset += side == Side.X ? node.xOffset : node.yOffset;
                    stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>(node, offset, leftEdge, rightEdge));
                    rightEdge = offset;
                    node = node.left_child ? node.left : Null;
                }
                while (stack.Count != 0)
                {
                    STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long> t = stack.Pop();
                    node = t.Item1;
                    offset = t.Item2;
                    leftEdge = t.Item3;
                    rightEdge = t.Item4;

                    if ((offset < leftEdge) || (offset >= rightEdge))
                    {
                        throw new InvalidOperationException("range containment invariant");
                    }

                    leftEdge = offset + 1;
                    node = node.right_child ? node.right : Null;
                    while (node != Null)
                    {
                        offset += side == Side.X ? node.xOffset : node.yOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>(node, offset, leftEdge, rightEdge));
                        rightEdge = offset;
                        node = node.left_child ? node.left : Null;
                    }
                }
            }
        }

        // INonInvasiveTreeInspection

        object INonInvasiveTreeInspection.Root { get { return root != Null ? (object)root : null; } }

        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            Node n = (Node)node;
            return n.left_child ? (object)n.left : null;
        }

        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            Node n = (Node)node;
            return n.right_child ? (object)n.right : null;
        }

        object INonInvasiveTreeInspection.GetKey(object node)
        {
            object key = null;
            return key;
        }

        object INonInvasiveTreeInspection.GetValue(object node)
        {
            Node n = (Node)node;
            object value = null;
            value = n.value;
            return value;
        }

        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            Node n = (Node)node;
            return n.balance;
        }

        void INonInvasiveTreeInspection.Validate()
        {
            if (root != Null)
            {
                Dictionary<Node, bool> visited = new Dictionary<Node, bool>();
                Queue<Node> worklist = new Queue<Node>();
                worklist.Enqueue(root);
                while (worklist.Count != 0)
                {
                    Node node = worklist.Dequeue();

                    if (visited.ContainsKey(node))
                    {
                        throw new InvalidOperationException("cycle");
                    }
                    visited.Add(node, false);

                    if (node.left_child)
                    {
                        worklist.Enqueue(node.left);
                    }
                    if (node.right_child)
                    {
                        worklist.Enqueue(node.right);
                    }
                }
            }

            /*[Feature(Feature.Rank, Feature.MultiRank, Feature.Range, Feature.Range2)]*/
            ValidateRanges(/*[Feature(Feature.Range2)]*/Side.X);
            /*[Feature(Feature.Range2)]*/
            ValidateRanges(/*[Feature(Feature.Range2)]*/Side.Y);

            g_tree_node_check(root);
            ValidateDepthInvariant();
        }

        private void ValidateDepthInvariant()
        {
            const double phi = 1.618033988749894848204;
            const double epsilon = .001;

            double max = Math.Log((count + 2) * Math.Sqrt(5)) / Math.Log(phi) - 2;
            int depth = root != Null ? MaxDepth(root) : 0;
            if (depth > max + epsilon)
            {
                throw new InvalidOperationException("max depth invariant");
            }
        }

        private int MaxDepth(Node node)
        {
            int ld = node.left_child ? MaxDepth(node.left) : 0;
            int rd = node.right_child ? MaxDepth(node.right) : 0;
            return 1 + Math.Max(ld, rd);
        }

        private void g_tree_node_check(Node node)
        {
            if (node != Null)
            {
                if (node.left_child)
                {
                    Node tmp = g_tree_node_previous(node);
                    if (!(tmp.right == node))
                    {
                        Debug.Assert(false, "program defect");
                        throw new InvalidOperationException("invariant");
                    }
                }

                if (node.right_child)
                {
                    Node tmp = g_tree_node_next(node);
                    if (!(tmp.left == node))
                    {
                        Debug.Assert(false, "program defect");
                        throw new InvalidOperationException("invariant");
                    }
                }

                int left_height = 0;
                int right_height = 0;

                if (node.left_child)
                {
                    left_height = g_tree_node_height(node.left);
                }
                if (node.right_child)
                {
                    right_height = g_tree_node_height(node.right);
                }

                int balance = right_height - left_height;
                if (!(balance == node.balance))
                {
                    Debug.Assert(false, "program defect");
                    throw new InvalidOperationException("invariant");
                }

                if (node.left_child)
                {
                    g_tree_node_check(node.left);
                }
                if (node.right_child)
                {
                    g_tree_node_check(node.right);
                }
            }
        }

        private int g_tree_node_height(Node node)
        {
            if (node != Null)
            {
                int left_height = 0;
                int right_height = 0;

                if (node.left_child)
                {
                    left_height = g_tree_node_height(node.left);
                }

                if (node.right_child)
                {
                    right_height = g_tree_node_height(node.right);
                }

                return Math.Max(left_height, right_height) + 1;
            }

            return 0;
        }

        // INonInvasiveRange2MapInspection

        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        Range2MapEntryLong[] INonInvasiveRange2MapInspectionLong.GetRanges()
        {
            /*[Widen]*/
            Range2MapEntryLong[] ranges = new Range2MapEntryLong[Count];
            int i = 0;

            if (root != Null)
            {
                Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long>>();

                /*[Widen]*/
                long xOffset = 0 ;
                /*[Widen]*/
                long yOffset = 0 ;

                Node node = root;
                while (node != Null)
                {
                    xOffset += node.xOffset;
                    yOffset += node.yOffset;
                    stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long>(node, xOffset, yOffset));
                    node = node.left_child ? node.left : Null;
                }
                while (stack.Count != 0)
                {
                    STuple<Node,/*[Widen]*/long,/*[Widen]*/long> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;
                    yOffset = t.Item3;

                    object value = null;
                    value = node.value;

                    /*[Widen]*/
                    ranges[i++] = new Range2MapEntryLong(new RangeLong(xOffset, 0), new RangeLong(yOffset, 0), value);

                    node = node.right_child ? node.right : Null;
                    while (node != Null)
                    {
                        xOffset += node.xOffset;
                        yOffset += node.yOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long>(node, xOffset, yOffset));
                        node = node.left_child ? node.left : Null;
                    }
                }
                if (!(i == ranges.Length))
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                for (i = 1; i < ranges.Length; i++)
                {
                    if (!(ranges[i - 1].x.start < ranges[i].x.start))
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    /*[Feature(Feature.Range2)]*/
                    if (!(ranges[i - 1].y.start < ranges[i].y.start))
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ranges[i - 1].x.length = ranges[i].x.start - ranges[i - 1].x.start;
                    /*[Feature(Feature.Range2)]*/
                    ranges[i - 1].y.length = ranges[i].y.start - ranges[i - 1].y.start;
                }

                ranges[i - 1].x.length = this.xExtent - ranges[i - 1].x.start;
                /*[Feature(Feature.Range2)]*/
                ranges[i - 1].y.length = this.yExtent - ranges[i - 1].y.start;
            }

            return ranges;
        }

        [Feature(Feature.Range, Feature.Range2)]
        void INonInvasiveRange2MapInspectionLong.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }


        //
        // Enumeration
        //

        /// <summary>
        /// Get the default enumerator, which is the fast enumerator for AVL trees.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<EntryRange2MapLong<ValueType>> GetEnumerator()
        {
            return GetFastEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Get the robust enumerator. The robust enumerator uses an internal key cursor and queries the tree using the NextGreater()
        /// method to advance the enumerator. This enumerator is robust because it tolerates changes to the underlying tree. If a key
        /// is inserted or removed and it comes before the enumerator’s current key in sorting order, it will have no affect on the
        /// enumerator. If a key is inserted or removed and it comes after the enumerator’s current key (i.e. in the portion of the
        /// collection the enumerator hasn’t visited yet), the enumerator will include the key if inserted or skip the key if removed.
        /// Because the enumerator queries the tree for each element it’s running time per element is O(lg N), or O(N lg N) to
        /// enumerate the entire tree.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        public IEnumerable<EntryRange2MapLong<ValueType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this);
        }

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRange2MapLong<ValueType>>
        {
            private readonly AVLTreeRange2MapLong<ValueType> tree;

            public RobustEnumerableSurrogate(AVLTreeRange2MapLong<ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRange2MapLong<ValueType>> GetEnumerator()
            {
                return new RobustEnumerator(tree);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// Get the fast enumerator. The fast enumerator uses an internal stack of nodes to peform in-order traversal of the
        /// tree structure. Because it uses the tree structure, it is invalidated if the tree is modified by an insertion or
        /// deletion and will throw an InvalidOperationException when next advanced. The complexity of the fast enumerator
        /// is O(1) per element, or O(N) to enumerate the entire tree.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        public IEnumerable<EntryRange2MapLong<ValueType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this);
        }

        public struct FastEnumerableSurrogate : IEnumerable<EntryRange2MapLong<ValueType>>
        {
            private readonly AVLTreeRange2MapLong<ValueType> tree;

            public FastEnumerableSurrogate(AVLTreeRange2MapLong<ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRange2MapLong<ValueType>> GetEnumerator()
            {
                return new FastEnumerator(tree);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// This enumerator is robust in that it can continue to walk the tree even in the face of changes, because
        /// it keeps a current key and uses NearestGreater to find the next one. However, since it uses queries it
        /// is slow, O(n lg(n)) to enumerate the entire tree.
        /// </summary>
        public class RobustEnumerator : IEnumerator<EntryRange2MapLong<ValueType>>
        {
            private readonly AVLTreeRange2MapLong<ValueType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private long currentXStart;
            [Feature(Feature.Range, Feature.Range2)]
            private ushort version; // saving the currentXStart does not work well range collections

            public RobustEnumerator(AVLTreeRange2MapLong<ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRange2MapLong<ValueType> Current
            {
                get
                {
                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    if (version != tree.version)
                    {
                        throw new InvalidOperationException();
                    }

                    if (valid)
                    {

                        // OR

                        /*[Feature(Feature.Range, Feature.Range2)]*/
                        {
                            ValueType value = default(ValueType);
                            /*[Widen]*/
                            long xStart = 0, xLength = 0 ;
                            /*[Widen]*/
                            long yStart = 0, yLength = 0 ;
                            xStart = currentXStart;

                            tree.Get(
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range2)]*/Side.X,
                                /*[Feature(Feature.Range2)]*/out yStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/out xLength,
                                /*[Feature(Feature.Range2)]*/out yLength,
                                /*[Payload(Payload.Value)]*/out value);

                            return new EntryRange2MapLong<ValueType>(
                                /*[Payload(Payload.Value)]*/value,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength,
                                /*[Feature(Feature.Range2)]*/yStart,
                                /*[Feature(Feature.Range2)]*/yLength);
                        }
                    }
                    return new EntryRange2MapLong<ValueType>();
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
                if (version != tree.version)
                {
                    throw new InvalidOperationException();
                }

                if (!started)
                {

                    // OR

                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    valid = tree.xExtent != 0;
                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    Debug.Assert(currentXStart == 0);

                    started = true;
                }
                else if (valid)
                {

                    // OR

                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    valid = tree.NearestGreater(currentXStart, /*[Feature(Feature.Range2)]*/Side.X, out currentXStart);
                }

                return valid;
            }

            public void Reset()
            {
                started = false;
                valid = false;
                currentXStart = 0;
                /*[Feature(Feature.Range, Feature.Range2)]*/
                version = tree.version;
            }
        }

        /// <summary>
        /// This enumerator is fast because it uses an in-order traversal of the tree that has O(1) cost per element.
        /// However, any Add or Remove to the tree invalidates it.
        /// </summary>
        public class FastEnumerator : IEnumerator<EntryRange2MapLong<ValueType>>
        {
            private readonly AVLTreeRange2MapLong<ValueType> tree;
            private ushort version;
            private Node currentNode;
            private Node nextNode;
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private long currentXStart, nextXStart;
            [Feature(Feature.Range2)]
            [Widen]
            private long currentYStart, nextYStart;

            private readonly Stack<STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long>> stack
                = new Stack<STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long>>();

            public FastEnumerator(AVLTreeRange2MapLong<ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRange2MapLong<ValueType> Current
            {
                get
                {
                    if (currentNode != tree.Null)
                    {

                        return new EntryRange2MapLong<ValueType>(
                            /*[Payload(Payload.Value)]*/currentNode.value,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart - currentXStart,
                            /*[Feature(Feature.Range2)]*/currentYStart,
                            /*[Feature(Feature.Range2)]*/nextYStart - currentYStart);
                    }
                    return new EntryRange2MapLong<ValueType>();
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
                return currentNode != tree.Null;
            }

            public void Reset()
            {
                stack.Clear();
                currentNode = tree.Null;
                nextNode = tree.Null;
                currentXStart = 0;
                currentYStart = 0;
                nextXStart = 0;
                nextYStart = 0;

                this.version = tree.version;

                PushSuccessor(
                    tree.root,
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                    /*[Feature(Feature.Range2)]*/0);

                Advance();
            }

            private void PushSuccessor(
                Node node,                [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xPosition,                [Feature(Feature.Range2)][Widen] long yPosition)
            {
                while (node != tree.Null)
                {
                    xPosition += node.xOffset;
                    yPosition += node.yOffset;

                    stack.Push(new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long>(
                        node,
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition,
                        /*[Feature(Feature.Range2)]*/yPosition));
                    node = node.left_child ? node.left : tree.Null;
                }
            }

            private void Advance()
            {
                if (this.version != tree.version)
                {
                    throw new InvalidOperationException();
                }

                currentNode = nextNode;
                currentXStart = nextXStart;
                currentYStart = nextYStart;

                nextNode = tree.Null;
                nextXStart = tree.xExtent;
                nextYStart = tree.yExtent;

                if (stack.Count == 0)
                {
                    nextXStart = tree.xExtent;
                    nextYStart = tree.yExtent;
                    return;
                }

                STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long> cursor
                    = stack.Pop();

                nextNode = cursor.Item1;
                nextXStart = cursor.Item2;
                nextYStart = cursor.Item3;

                if (nextNode.right_child)
                {
                    PushSuccessor(
                        nextNode.right,
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart,
                        /*[Feature(Feature.Range2)]*/nextYStart);
                }
            }
        }
    }
}