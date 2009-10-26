/**
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using Hashtable = System.Collections.Hashtable;
using LinkedList = System.Collections.Generic.LinkedList<object>;
using Math = System.Math;

namespace Lucene.Net.Util.Cache  
{
    //{{DIGY}}
    //Use SimpleLRUCache_LUCENENET_190_1 for capacity<1536 and
    //SimpleLRUCache_LUCENENET_190_2 for capacity > 1536
    public class SimpleLRUCache : SimpleLRUCache_LUCENENET_190_1
    {
        public SimpleLRUCache(int Capacity)
            : base(Capacity)
        {
        }
    }




    /// <summary>
    /// This class is the original port for 2.4.0
    /// </summary>
    [System.ComponentModel.EditorBrowsable( System.ComponentModel.EditorBrowsableState.Never)]
    public class SimpleLRUCache_OrgPort : SimpleMapCache
    {
        private const float LOADFACTOR = 0.75f;

        private int cacheSize;
        private LinkedList lru;

        public SimpleLRUCache_OrgPort(int cacheSize)
            : base(null)
        {
            this.cacheSize = cacheSize;
            int capacity = (int)Math.Ceiling(cacheSize / LOADFACTOR) + 1;
            base.map = new System.Collections.Hashtable(capacity, LOADFACTOR);

            lru = new LinkedList();
        }

        public override void Put(object key, object value)
        {
            //if (lru.Contains(key))
            if (base.map.ContainsKey(key))
            {
                // move key to most recently used position
                lru.Remove(key);
                lru.AddFirst(key);
            }
            else
            {
                if (lru.Count == cacheSize)
                {
                    object last = lru.Last;
                    lru.Remove(last);
                    // remove least recently used item from cache
                    base.map.Remove(last);
                }
                // place key in most recently used position
                lru.AddFirst(key);
            }

            base.Put(key, value);
        }

        public override object Get(object key)
        {
            //if (lru.Contains(key))
            if (base.map.ContainsKey(key))
            {
                // if LRU data structure contains key, move the key
                // to the "most recently used" position
                lru.Remove(key);
                lru.AddFirst(key);
            }

            return base.Get(key);
        }
    }





    /// <summary>
    /// Implemented for LUCENENET-190. Good for capacity < 1536
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class SimpleLRUCache_LUCENENET_190_1: Cache
    {
        System.Collections.Generic.Dictionary<object, LRUCacheValueEntry> Data = new Dictionary<object, LRUCacheValueEntry>();
        SortedList<long, object> TimeStamps = new SortedList<long, object>();

        long TimeStamp = 0;
        int Capacity = 1024;

        public SimpleLRUCache_LUCENENET_190_1(int Capacity)
        {
            this.Capacity = Capacity;
        }

        public override void Put(object Key, object Value)
        {
            if (Get(Key) == null)
            {
                TimeStamp++;
                Data.Add(Key, new LRUCacheValueEntry(TimeStamp, Value));
                TimeStamps.Add(TimeStamp, Key);

                if (Data.Count > Capacity)
                {
                    long key = TimeStamps.Keys[0];
                    Data.Remove(TimeStamps[key]);
                    TimeStamps.RemoveAt(0);
                }
            }
        }

        public override object Get(object Key)
        {
            LRUCacheValueEntry e = null;
            Data.TryGetValue(Key, out e);
            if (e == null) return null;

            TimeStamps.Remove(e.TimeStamp);
            e.TimeStamp = ++TimeStamp;
            TimeStamps.Add(e.TimeStamp, Key);
            return e.Value;

        }

        public override bool ContainsKey(object key)
        {
            return Data.ContainsKey(key);
        }

        public override void Close()
        {
        }

        class LRUCacheValueEntry
        {
            public long TimeStamp = 0;
            public object Value;

            public LRUCacheValueEntry(long TimeStamp, object Value)
            {
                this.TimeStamp = TimeStamp;
                this.Value = Value;
            }
        }
    }



    /// <summary>
    /// Implemented for LUCENENET-190. Good for capacity > 1536
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class SimpleLRUCache_LUCENENET_190_2 : Lucene.Net.Util.Cache.Cache
    {
        System.Collections.Generic.Dictionary<object, LRUCacheValueEntry> Data = new Dictionary<object, LRUCacheValueEntry>();
        System.Collections.Generic.SortedDictionary<long, object> TimeStamps = new SortedDictionary<long, object>();
        

        long TimeStamp = 0;
        int Capacity = 1024;

        public SimpleLRUCache_LUCENENET_190_2(int Capacity)
        {
            this.Capacity = Capacity;
        }

        public override void Put(object Key, object Value)
        {
            if (Get(Key) == null)
            {
                TimeStamp++;
                Data.Add(Key, new LRUCacheValueEntry(TimeStamp, Value));
                TimeStamps.Add(TimeStamp, Key);

                if (Data.Count > Capacity)
                {
                    SortedDictionary<long, object>.Enumerator enumTimeStamps = TimeStamps.GetEnumerator();
                    enumTimeStamps.MoveNext();
                    long key = enumTimeStamps.Current.Key;
                    Data.Remove(TimeStamps[key]);
                    TimeStamps.Remove(key);
                }

            }
        }

        public override object Get(object Key)
        {
            LRUCacheValueEntry e = null;
            Data.TryGetValue(Key, out e);
            if (e == null) return null;

            TimeStamps.Remove(e.TimeStamp);
            e.TimeStamp = ++TimeStamp;
            TimeStamps.Add(e.TimeStamp, Key);
            return e.Value;
        }

        class LRUCacheValueEntry
        {
            public long TimeStamp = 0;
            public object Value;

            public LRUCacheValueEntry(long TimeStamp, object Value)
            {
                this.TimeStamp = TimeStamp;
                this.Value = Value;
            }
        }

        public override bool ContainsKey(object key)
        {
            return Data.ContainsKey(key);
        }

        public override void Close()
        {
        }
    }
}
