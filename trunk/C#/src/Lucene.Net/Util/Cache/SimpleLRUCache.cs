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

using Hashtable = System.Collections.Hashtable;
using LinkedList = System.Collections.Generic.LinkedList<object>;
using Math = System.Math;

namespace Lucene.Net.Util.Cache
{
    public class SimpleLRUCache : SimpleMapCache
    {
        private const float LOADFACTOR = 0.75f;

        private int cacheSize;
        private LinkedList lru;

        public SimpleLRUCache(int cacheSize)
            : base(null)
        {
            this.cacheSize = cacheSize;
            int capacity = (int)Math.Ceiling(cacheSize / LOADFACTOR) + 1;
            base.map = new System.Collections.Hashtable(capacity, LOADFACTOR);

            lru = new LinkedList();
        }

        public override void Put(object key, object value)
        {
            if (lru.Contains(key))
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
            if (lru.Contains(key))
            {
                // if LRU data structure contains key, move the key
                // to the "most recently used" position
                lru.Remove(key);
                lru.AddFirst(key);
            }

            return base.Get(key);
        }
    }
}
