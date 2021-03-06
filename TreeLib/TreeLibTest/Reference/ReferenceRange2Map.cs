﻿/*
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

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    // This is a primary implementation

    public class ReferenceRange2Map<ValueType> :
        IRange2Map<ValueType>,
        INonInvasiveTreeInspection,
        INonInvasiveRange2MapInspection,
        IEnumerable<EntryRange2Map<ValueType>>
    {
        private readonly List<Tuple<int, int, ValueType>> items = new List<Tuple<int, int, ValueType>>();
        private ushort version;

        //
        // Construction
        //

        public ReferenceRange2Map()
        {
        }

        public ReferenceRange2Map(ReferenceRange2Map<ValueType> original)
        {
            items.AddRange(original.items);
        }

        public ReferenceRange2Map<ValueType> Clone()
        {
            return new ReferenceRange2Map<ValueType>(this);
        }

        //
        // IRange2Map
        //

        public uint Count { get { return unchecked((uint)items.Count); } }

        public long LongCount { get { return items.Count; } }

        public void Clear()
        {
            items.Clear();
        }

        private bool Find(int start, Side side, out int index, out int xStart, out int yStart, bool includeEnd)
        {
            xStart = 0;
            yStart = 0;
            for (index = 0; index < items.Count; index++)
            {
                int offset = side == Side.X ? xStart : yStart;
                if (start <= offset)
                {
                    return start == offset;
                }
                xStart += items[index].Item1;
                yStart += items[index].Item2;
            }
            return includeEnd && (start == (side == Side.X ? xStart : yStart));
        }

        public bool Contains(int start, Side side)
        {
            int index, xStart, yStart;
            return Find(start, side, out index, out xStart, out yStart, false/*includeEnd*/);
        }

        public bool TryInsert(int start, Side side, int xLength, int yLength, ValueType value)
        {
            if ((start < 0) || (xLength <= 0) || (yLength <= 0))
            {
                throw new ArgumentOutOfRangeException();
            }

            int index, xStart, yStart;
            if (Find(start, side, out index, out xStart, out yStart, true/*includeEnd*/))
            {
                int overflowX = checked(xLength + GetExtent(Side.X));
                int overflowY = checked(yLength + GetExtent(Side.Y));
                items.Insert(index, new Tuple<int, int, ValueType>(xLength, yLength, value));
                this.version = unchecked((ushort)(this.version + 1));
                return true;
            }
            return false;
        }

        public bool TryDelete(int start, Side side)
        {
            int index, xStart, yStart;
            if (Find(start, side, out index, out xStart, out yStart, false/*includeEnd*/))
            {
                items.RemoveAt(index);
                this.version = unchecked((ushort)(this.version + 1));
                return true;
            }
            return false;
        }

        public bool TryGetLength(int start, Side side, out int length)
        {
            int index, xStart, yStart;
            if (Find(start, side, out index, out xStart, out yStart, false/*includeEnd*/))
            {
                length = side == Side.X ? items[index].Item1 : items[index].Item2;
                return true;
            }
            length = 0;
            return false;
        }

        public bool TrySetLength(int start, Side side, int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            int index, xStart, yStart;
            if (Find(start, side, out index, out xStart, out yStart, false/*includeEnd*/))
            {
                Tuple<int, int, ValueType> old = items[index];
                int overflowX = checked(GetExtent(Side.X) + (side == Side.X ? length - old.Item1 : 0));
                int overflowY = checked(GetExtent(Side.Y) + (side == Side.X ? 0 : length - old.Item2));
                items[index] = new Tuple<int, int, ValueType>(
                    side == Side.X ? length : old.Item1,
                    side == Side.Y ? length : old.Item2,
                    old.Item3);
                return true;
            }
            length = 0;
            return false;
        }

        public bool TryGetValue(int start, Side side, out ValueType value)
        {
            int index, xStart, yStart;
            if (Find(start, side, out index, out xStart, out yStart, false/*includeEnd*/))
            {
                value = items[index].Item3;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        public bool TrySetValue(int start, Side side, ValueType value)
        {
            int index, xStart, yStart;
            if (Find(start, side, out index, out xStart, out yStart, false/*includeEnd*/))
            {
                Tuple<int, int, ValueType> old = items[index];
                items[index] = new Tuple<int, int, ValueType>(old.Item1, old.Item2, value);
                return true;
            }
            value = default(ValueType);
            return false;
        }

        public bool TryGet(int start, Side side, out int otherStart, out int xLength, out int yLength, out ValueType value)
        {
            int index, xStart, yStart;
            if (Find(start, side, out index, out xStart, out yStart, false/*includeEnd*/))
            {
                otherStart = side == Side.X ? yStart : xStart;
                xLength = items[index].Item1;
                yLength = items[index].Item2;
                value = items[index].Item3;
                return true;
            }
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            return false;
        }

        public bool TrySet(int start, Side side, int xLength, int yLength, ValueType value)
        {
            if ((xLength < 0) || (yLength < 0))
            {
                throw new ArgumentOutOfRangeException();
            }

            int index, xStart, yStart;
            if (Find(start, side, out index, out xStart, out yStart, false/*includeEnd*/))
            {
                Tuple<int, int, ValueType> old = items[index];
                items[index] = new Tuple<int, int, ValueType>(xLength != 0 ? xLength : old.Item1, yLength != 0 ? yLength : old.Item2, value);
                return true;
            }
            value = default(ValueType);
            return false;
        }

        public void Insert(int start, Side side, int xLength, int yLength, ValueType value)
        {
            if (!TryInsert(start, side, xLength, yLength, value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        public void Delete(int start, Side side)
        {
            if (!TryDelete(start, side))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        public int GetLength(int start, Side side)
        {
            int length;
            if (!TryGetLength(start, side, out length))
            {
                throw new ArgumentException("item not in tree");
            }
            return length;
        }

        public void SetLength(int start, Side side, int length)
        {
            if (!TrySetLength(start, side, length))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        public ValueType GetValue(int start, Side side)
        {
            ValueType value;
            if (!TryGetValue(start, side, out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        public void SetValue(int start, Side side, ValueType value)
        {
            if (!TrySetValue(start, side, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        public void Get(int start, Side side, out int otherStart, out int xLength, out int yLength, out ValueType value)
        {
            if (!TryGet(start, side, out otherStart, out xLength, out yLength, out value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        public void Set(int start, Side side, int xLength, int yLength, ValueType value)
        {
            if (!TrySet(start, side, xLength, yLength, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        public void AdjustLength(int start, Side side, int xAdjust, int yAdjust)
        {
            int index, xStart, yStart;
            if (!Find(start, side, out index, out xStart, out yStart, false/*includeEnd*/))
            {
                throw new ArgumentOutOfRangeException();
            }

            Tuple<int, int, ValueType> old = items[index];

            int newXLength = checked(old.Item1 + xAdjust);
            int newYLength = checked(old.Item2 + yAdjust);

            if ((newXLength < 0) || (newYLength < 0))
            {
                throw new ArgumentOutOfRangeException();
            }
            if ((newXLength == 0) != (newYLength == 0))
            {
                throw new ArgumentException();
            }

            if (newXLength != 0)
            {
                // throw OverflowException before modifying anything
                int overflowX = checked(GetExtent(Side.X) + xAdjust);
                int overflowY = checked(GetExtent(Side.Y) + yAdjust);

                items[index] = new Tuple<int, int, ValueType>(
                    newXLength,
                    newYLength,
                    old.Item3);
            }
            else
            {
                items.RemoveAt(index);
            }
        }

        public int GetExtent(Side side)
        {
            int index, xStart, yStart;
            Find(Int32.MaxValue, side, out index, out xStart, out yStart, true/*includeEnd*/);
            return side == Side.X ? xStart : yStart;
        }

        private int[] GetStarts(Side side, bool includeEnd)
        {
            int[] starts = new int[items.Count + (includeEnd ? 1 : 0)];
            for (int i = 0, start = 0; i < items.Count; i++)
            {
                start += side == Side.X ? items[i].Item1 : items[i].Item2;
                if (includeEnd || (i + 1 < items.Count))
                {
                    starts[i + 1] = start;
                }
            }
            return starts;
        }

        public bool NearestLessOrEqual(int position, Side side, out int nearestStart)
        {
            int[] starts = GetStarts(side, true/*includeEnd*/);
            int i = Array.BinarySearch(starts, position);
            if (i == items.Count)
            {
                i--;
            }
            if (i >= 0)
            {
                nearestStart = starts[i];
                return true;
            }
            i = ~i;
            if (i == starts.Length)
            {
                i--;
            }
            if (i > 0)
            {
                nearestStart = starts[i - 1];
                return true;
            }
            nearestStart = 0;
            return false;
        }

        public bool NearestLess(int position, Side side, out int nearestStart)
        {
            int[] starts = GetStarts(side, false/*includeEnd*/);
            int i = Array.BinarySearch(starts, position);
            if (i < 0)
            {
                i = ~i;
            }
            if (i > 0)
            {
                nearestStart = starts[i - 1];
                return items.Count != 0;
            }
            nearestStart = 0;
            return false;
        }

        public bool NearestGreaterOrEqual(int position, Side side, out int nearestStart)
        {
            int[] starts = GetStarts(side, true/*includeEnd*/);
            int i = Array.BinarySearch(starts, position);
            if (i >= 0)
            {
                nearestStart = starts[i];
                return i != items.Count;
            }
            i = ~i;
            nearestStart = starts[Math.Min(i, items.Count)];
            return (i < items.Count);
        }

        public bool NearestGreater(int position, Side side, out int nearestStart)
        {
            int[] starts = GetStarts(side, true/*includeEnd*/);
            int i = Array.BinarySearch(starts, position);
            if (i >= 0)
            {
                i++;
            }
            else
            {
                i = ~i;
            }
            nearestStart = starts[Math.Min(i, items.Count)];
            return (i < items.Count);
        }


        public bool NearestLessOrEqual(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength, out ValueType value)
        {
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestLessOrEqual(position, side, out nearestStart);
            if (f)
            {
                bool g = TryGet(nearestStart, side, out otherStart, out xLength, out yLength, out value);
                Debug.Assert(g);
            }
            return f;
        }

        public bool NearestLess(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength, out ValueType value)
        {
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestLess(position, side, out nearestStart);
            if (f)
            {
                bool g = TryGet(nearestStart, side, out otherStart, out xLength, out yLength, out value);
                Debug.Assert(g);
            }
            return f;
        }

        public bool NearestGreaterOrEqual(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength, out ValueType value)
        {
            otherStart = GetExtent(side == Side.X ? Side.Y : Side.X);
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestGreaterOrEqual(position, side, out nearestStart);
            if (f)
            {
                bool g = TryGet(nearestStart, side, out otherStart, out xLength, out yLength, out value);
                Debug.Assert(g);
            }
            return f;
        }

        public bool NearestGreater(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength, out ValueType value)
        {
            otherStart = GetExtent(side == Side.X ? Side.Y : Side.X);
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestGreater(position, side, out nearestStart);
            if (f)
            {
                bool g = TryGet(nearestStart, side, out otherStart, out xLength, out yLength, out value);
                Debug.Assert(g);
            }
            return f;
        }


        //
        // INonInvasiveTreeInspection
        //

        // uint Count { get; }

        object INonInvasiveTreeInspection.Root { get { throw new NotSupportedException(); } }

        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            throw new NotSupportedException();
        }

        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            throw new NotSupportedException();
        }

        object INonInvasiveTreeInspection.GetKey(object node)
        {
            throw new NotSupportedException();
        }

        object INonInvasiveTreeInspection.GetValue(object node)
        {
            throw new NotSupportedException();
        }

        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            throw new NotSupportedException();
        }

        void INonInvasiveTreeInspection.Validate()
        {
        }

        //
        // INonInvasiveRange2MapInspection
        //

        Range2MapEntry[] INonInvasiveRange2MapInspection.GetRanges()
        {
            List<Range2MapEntry> ranges = new List<Range2MapEntry>();
            int xOffset = 0, yOffset = 0;
            for (int i = 0; i < items.Count; i++)
            {
                ranges.Add(
                    new Range2MapEntry(
                        new Range(xOffset, items[i].Item1),
                        new Range(yOffset, items[i].Item2),
                        items[i].Item3));
                xOffset += items[i].Item1;
                yOffset += items[i].Item2;
            }
            return ranges.ToArray();
        }

        void INonInvasiveRange2MapInspection.Validate()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Item1 <= 0)
                {
                    throw new InvalidOperationException("length must be greater than zero");
                }
                if (items[i].Item2 <= 0)
                {
                    throw new InvalidOperationException("length must be greater than zero");
                }
            }
        }


        //
        // IEnumerable
        //

        private class Enumerator : IEnumerator<EntryRange2Map<ValueType>>, ISetValue<ValueType>
        {
            private readonly ReferenceRange2Map<ValueType> map;
            private readonly bool forward;
            private readonly bool robust;
            private readonly bool startIndexed;
            private readonly int startIndex;
            private readonly Side side;

            private int index;
            private ushort mapVersion;
            private ushort enumeratorVersion;

            public Enumerator(ReferenceRange2Map<ValueType> map, bool forward, bool robust, bool startIndexed, int startIndex, Side side)
            {
                this.map = map;
                this.forward = forward;
                this.robust = robust;
                this.startIndexed = startIndexed;
                this.startIndex = startIndex;
                this.side = side;

                Reset();
            }

            public EntryRange2Map<ValueType> Current
            {
                get
                {
                    if ((index >= 0) && (index < map.Count))
                    {
                        int xStart = 0, yStart = 0;
                        for (int i = 0; i < index; i++)
                        {
                            xStart += map.items[i].Item1;
                            yStart += map.items[i].Item2;
                        }
                        return new EntryRange2Map<ValueType>(
                            map.items[index].Item3,
                            this,
                            this.enumeratorVersion,
                            xStart,
                            map.items[index].Item1,
                            yStart,
                            map.items[index].Item2);
                    }
                    return default(EntryRange2Map<ValueType>);
                }
            }

            object IEnumerator.Current { get { return this.Current; } }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (/*!robust && */(mapVersion != map.version))
                {
                    throw new InvalidOperationException();
                }

                this.enumeratorVersion = unchecked((ushort)(this.enumeratorVersion + 1));

                index = index + (forward ? 1 : -1);
                return (index >= 0) && (index < map.items.Count);
            }

            public void Reset()
            {
                this.mapVersion = map.version;
                this.enumeratorVersion = unchecked((ushort)(this.enumeratorVersion + 1));

                if (forward)
                {
                    index = -1;
                    if (startIndexed)
                    {
                        int xStart = 0, yStart = 0;
                        while ((index + 1 < map.items.Count) && (startIndex > (side == Side.X ? xStart : yStart)))
                        {
                            index++;
                            xStart += map.items[index].Item1;
                            yStart += map.items[index].Item2;
                        }
                    }
                }
                else
                {
                    index = map.items.Count;
                    if (startIndexed)
                    {
                        int xStart = map.GetExtent(Side.X);
                        int yStart = map.GetExtent(Side.Y);
                        while ((index - 1 >= 0) && (startIndex < (side == Side.X ? xStart - map.items[index - 1].Item1 : yStart - map.items[index - 1].Item2)))
                        {
                            index--;
                            xStart -= map.items[index].Item1;
                            yStart -= map.items[index].Item2;
                        }
                    }
                }
            }

            public void SetValue(ValueType value, ushort expectedEnumeratorVersion)
            {
                if ((/*!robust && */(this.mapVersion != map.version)) || (this.enumeratorVersion != expectedEnumeratorVersion))
                {
                    throw new InvalidOperationException();
                }

                map.items[index] = new Tuple<int, int, ValueType>(map.items[index].Item1, map.items[index].Item2, value);
            }
        }

        public class EnumerableSurrogate : IEnumerable<EntryRange2Map<ValueType>>
        {
            private readonly ReferenceRange2Map<ValueType> map;
            private readonly bool forward;
            private readonly bool robust;
            private readonly bool startIndexed;
            private readonly int startIndex;
            private readonly Side side;

            public EnumerableSurrogate(ReferenceRange2Map<ValueType> map, bool forward, bool robust, bool startIndexed, int startIndex, Side side)
            {
                this.map = map;
                this.forward = forward;
                this.robust = robust;
                this.startIndexed = startIndexed;
                this.startIndex = startIndex;
                this.side = side;
            }

            public IEnumerator<EntryRange2Map<ValueType>> GetEnumerator()
            {
                return new Enumerator(map, forward, robust, startIndexed, startIndex, side);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public IEnumerator<EntryRange2Map<ValueType>> GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetEnumerable()
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, false/*startIndexed*/, 0, Side.X);
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetEnumerable(bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, false/*startIndexed*/, 0, Side.X);
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetFastEnumerable()
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, false/*startIndexed*/, 0, Side.X);
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetFastEnumerable(bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, false/*startIndexed*/, 0, Side.X);
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetRobustEnumerable()
        {
            return new EnumerableSurrogate(this, true/*forward*/, true/*robust*/, false/*startIndexed*/, 0, Side.X);
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetRobustEnumerable(bool forward)
        {
            return new EnumerableSurrogate(this, forward, true/*robust*/, false/*startIndexed*/, 0, Side.X);
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetEnumerable(int startAt, Side side)
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, true/*startIndexed*/, startAt, side);
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetEnumerable(int startAt, Side side, bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, true/*startIndexed*/, startAt, side);
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetFastEnumerable(int startAt, Side side)
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, true/*startIndexed*/, startAt, side);
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetFastEnumerable(int startAt, Side side, bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, true/*startIndexed*/, startAt, side);
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetRobustEnumerable(int startAt, Side side)
        {
            return new EnumerableSurrogate(this, true/*forward*/, true/*robust*/, true/*startIndexed*/, startAt, side);
        }

        public IEnumerable<EntryRange2Map<ValueType>> GetRobustEnumerable(int startAt, Side side, bool forward)
        {
            return new EnumerableSurrogate(this, forward, true/*robust*/, true/*startIndexed*/, startAt, side);
        }
    }
}
