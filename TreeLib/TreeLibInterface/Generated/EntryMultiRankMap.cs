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

using TreeLib.Internal;

#pragma warning disable CS1591

namespace TreeLib
{
    /// <summary>
    /// A type defining the struct returned for each item in a tree by an enumerator. The struct contains properties
    /// for all relevant per-item data, including one or more of key, value, rank/count, and/or range start/length, as
    /// appropriate for the type of collection.
    /// </summary>
    public struct EntryMultiRankMap<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType>
    {
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly KeyType key;

        /// <summary>
        /// Returns the key associated with a key-value pair mapping, or the key associated with a key-only collection.
        /// </summary>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public KeyType Key { get { return key; } }


        [Payload(Payload.Value)]
        private ValueType value;

        /// <summary>
        /// Returns the value associated with a key-value pair mapping.
        /// </summary>
        [Payload(Payload.Value)]
        public ValueType Value { get { return value; } }


        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private int xStart;

        /// <summary>
        /// Returns the rank of an item in a rank collection, or the start of a range in a range collection
        /// (for range-to-range mapping, returns the X side start)
        /// </summary>
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public int Rank { get { return xStart; } }


        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private int xLength;

        /// <summary>
        /// Returns the count of an item in a multi-rank collection, or the length of a range in a range collection
        /// (for range-to-range mapping, returns the X side length)
        /// </summary>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public int Count { get { return xLength; } }


        public EntryMultiRankMap(            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Payload(Payload.Value)] ValueType value,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xStart,            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xLength)
        {
            this.key = key;
            this.value = value;
            this.xStart = xStart;
            this.xLength = xLength;
        }
    }
}