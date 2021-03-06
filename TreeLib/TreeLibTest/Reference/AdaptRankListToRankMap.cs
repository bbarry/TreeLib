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

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class AdaptRankListToRankMap<KeyType, ValueType> :
        IRankMap<KeyType, ValueType>,
        INonInvasiveTreeInspection,
        INonInvasiveMultiRankMapInspection,
        IEnumerable<EntryRankMap<KeyType, ValueType>>,
        ICloneable
        where KeyType : IComparable<KeyType>
    {
        private readonly IRankList<KeyValue<KeyType, ValueType>> inner;


        //
        // Construction
        //

        // Caller must create inner collection and provide it, since we can't know how to select the implementation.
        public AdaptRankListToRankMap(IRankList<KeyValue<KeyType, ValueType>> inner)
        {
            this.inner = inner;
        }


        //
        // IRankMap
        //

        public uint Count { get { return inner.Count; } }

        public long LongCount { get { return inner.LongCount; } }

        public void Clear()
        {
            inner.Clear();
        }

        public bool ContainsKey(KeyType key)
        {
            return inner.ContainsKey(new KeyValue<KeyType, ValueType>(key));
        }

        public bool TryAdd(KeyType key, ValueType value)
        {
            return inner.TryAdd(new KeyValue<KeyType, ValueType>(key, value));
        }

        public bool TryRemove(KeyType key)
        {
            return inner.TryRemove(new KeyValue<KeyType, ValueType>(key));
        }

        public bool TryGetValue(KeyType key, out ValueType value)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.TryGetKey(new KeyValue<KeyType, ValueType>(key), out kv))
            {
                value = kv.value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        public bool TrySetValue(KeyType key, ValueType value)
        {
            return inner.TrySetKey(new KeyValue<KeyType, ValueType>(key, value));
        }

        public bool TryGet(KeyType key, out ValueType value, out int rank)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.TryGet(new KeyValue<KeyType, ValueType>(key), out kv, out rank))
            {
                value = kv.value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        public bool TryGetKeyByRank(int rank, out KeyType key)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.TryGetKeyByRank(rank, out kv))
            {
                key = kv.key;
                return true;
            }
            key = default(KeyType);
            return false;
        }

        public void Add(KeyType key, ValueType value)
        {
            inner.Add(new KeyValue<KeyType, ValueType>(key, value));
        }

        public void Remove(KeyType key)
        {
            inner.Remove(new KeyValue<KeyType, ValueType>(key));
        }

        public ValueType GetValue(KeyType key)
        {
            KeyValue<KeyType, ValueType> kv = inner.GetKey(new KeyValue<KeyType, ValueType>(key));
            return kv.value;
        }

        public void SetValue(KeyType key, ValueType value)
        {
            inner.SetKey(new KeyValue<KeyType, ValueType>(key, value));
        }

        public void Get(KeyType key, out ValueType value, out int rank)
        {
            KeyValue<KeyType, ValueType> kv;
            inner.Get(new KeyValue<KeyType, ValueType>(key), out kv, out rank);
            value = kv.value;
        }

        public KeyType GetKeyByRank(int rank)
        {
            return inner.GetKeyByRank(rank).key;
        }

        public void AdjustCount(KeyType key, int countAdjust)
        {
            inner.AdjustCount(new KeyValue<KeyType, ValueType>(key), countAdjust);
        }

        public bool Least(out KeyType leastOut, out ValueType valueOut)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.Least(out kv))
            {
                leastOut = kv.key;
                valueOut = kv.value;
                return true;
            }
            leastOut = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool Least(out KeyType leastOut)
        {
            ValueType value;
            return Least(out leastOut, out value);
        }

        public bool Greatest(out KeyType greatestOut, out ValueType valueOut)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.Greatest(out kv))
            {
                greatestOut = kv.key;
                valueOut = kv.value;
                return true;
            }
            greatestOut = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool Greatest(out KeyType greatestOut)
        {
            ValueType value;
            return Greatest(out greatestOut, out value);
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.NearestLessOrEqual(new KeyValue<KeyType, ValueType>(key), out kv))
            {
                nearestKey = kv.key;
                valueOut = kv.value;
                return true;
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestLessOrEqual(key, out nearestKey, out value);
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.NearestLess(new KeyValue<KeyType, ValueType>(key), out kv))
            {
                nearestKey = kv.key;
                valueOut = kv.value;
                return true;
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestLess(key, out nearestKey, out value);
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.NearestGreaterOrEqual(new KeyValue<KeyType, ValueType>(key), out kv))
            {
                nearestKey = kv.key;
                valueOut = kv.value;
                return true;
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestGreaterOrEqual(key, out nearestKey, out value);
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.NearestGreater(new KeyValue<KeyType, ValueType>(key), out kv))
            {
                nearestKey = kv.key;
                valueOut = kv.value;
                return true;
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestGreater(key, out nearestKey, out value);
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out int rank)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.NearestLessOrEqual(new KeyValue<KeyType, ValueType>(key), out kv, out rank))
            {
                nearestKey = kv.key;
                value = kv.value;
                return true;
            }
            nearestKey = default(KeyType);
            value = default(ValueType);
            return false;
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value, out int rank)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.NearestLess(new KeyValue<KeyType, ValueType>(key), out kv, out rank))
            {
                nearestKey = kv.key;
                value = kv.value;
                return true;
            }
            nearestKey = default(KeyType);
            value = default(ValueType);
            return false;
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out int rank)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.NearestGreaterOrEqual(new KeyValue<KeyType, ValueType>(key), out kv, out rank))
            {
                nearestKey = kv.key;
                value = kv.value;
                return true;
            }
            nearestKey = default(KeyType);
            value = default(ValueType);
            return false;
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value, out int rank)
        {
            KeyValue<KeyType, ValueType> kv;
            if (inner.NearestGreater(new KeyValue<KeyType, ValueType>(key), out kv, out rank))
            {
                nearestKey = kv.key;
                value = kv.value;
                return true;
            }
            nearestKey = default(KeyType);
            value = default(ValueType);
            return false;
        }


        //
        // INonInvasiveTreeInspection
        //

        uint INonInvasiveTreeInspection.Count { get { return ((INonInvasiveTreeInspection)inner).Count; } }

        object INonInvasiveTreeInspection.Root { get { return ((INonInvasiveTreeInspection)inner).Root; } }

        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            return ((INonInvasiveTreeInspection)inner).GetLeftChild(node);
        }

        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            return ((INonInvasiveTreeInspection)inner).GetRightChild(node);
        }

        object INonInvasiveTreeInspection.GetKey(object node)
        {
            KeyValue<KeyType, ValueType> kv = (KeyValue<KeyType, ValueType>)((INonInvasiveTreeInspection)inner).GetKey(node);
            return kv.key;
        }

        object INonInvasiveTreeInspection.GetValue(object node)
        {
            KeyValue<KeyType, ValueType> kv = (KeyValue<KeyType, ValueType>)((INonInvasiveTreeInspection)inner).GetKey(node);
            return kv.value;
        }

        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            return ((INonInvasiveTreeInspection)inner).GetMetadata(node);
        }

        void INonInvasiveTreeInspection.Validate()
        {
            ((INonInvasiveTreeInspection)inner).Validate();
        }


        //
        // INonInvasiveMultiRankMapInspection
        //

        uint INonInvasiveMultiRankMapInspection.Count { get { return ((INonInvasiveMultiRankMapInspection)inner).Count; } }

        MultiRankMapEntry[] INonInvasiveMultiRankMapInspection.GetRanks()
        {
            MultiRankMapEntry[] innerRanks = ((INonInvasiveMultiRankMapInspection)inner).GetRanks();
            for (int i = 0; i < innerRanks.Length; i++)
            {
                KeyValue<KeyType, ValueType> kv = (KeyValue<KeyType, ValueType>)innerRanks[i].key;
                innerRanks[i].key = kv.key;
                innerRanks[i].value = kv.value;
            }
            return innerRanks;
        }

        void INonInvasiveMultiRankMapInspection.Validate()
        {
            ((INonInvasiveMultiRankMapInspection)inner).Validate();
        }


        //
        // ICloneable
        //

        public object Clone()
        {
            return new AdaptRankListToRankMap<KeyType, ValueType>((IRankList<KeyValue<KeyType, ValueType>>)((ICloneable)inner).Clone());
        }


        //
        // IEnumerable
        //

        private EntryRankMap<KeyType, ValueType> Convert(EntryRankList<KeyValue<KeyType, ValueType>> entry)
        {
            return new EntryRankMap<KeyType, ValueType>(entry.Key.key, entry.Key.value, null, 0, entry.Rank);
        }

        public IEnumerator<EntryRankMap<KeyType, ValueType>> GetEnumerator()
        {
            return new AdaptEnumerator<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetEnumerator(),
                Convert);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new AdaptEnumeratorOld<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                ((IEnumerable)inner).GetEnumerator(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable()
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable()
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetFastEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetFastEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable()
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetRobustEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetRobustEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable(KeyType startAt)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetEnumerable(new KeyValue<KeyType, ValueType>(startAt)),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable(KeyType startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetEnumerable(new KeyValue<KeyType, ValueType>(startAt), forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable(KeyType startAt)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetFastEnumerable(new KeyValue<KeyType, ValueType>(startAt)),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable(KeyType startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetFastEnumerable(new KeyValue<KeyType, ValueType>(startAt), forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetRobustEnumerable(new KeyValue<KeyType, ValueType>(startAt)),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankList<KeyValue<KeyType, ValueType>>>(
                inner.GetRobustEnumerable(new KeyValue<KeyType, ValueType>(startAt), forward),
                Convert);
        }
    }
}
