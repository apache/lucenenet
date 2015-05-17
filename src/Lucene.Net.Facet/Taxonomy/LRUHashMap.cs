using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lucene.Net.Support;

namespace Lucene.Net.Facet.Taxonomy
{

    /*
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


    /// <summary>
    /// LRUHashMap is an extension of Java's HashMap, which has a bounded size();
    /// When it reaches that size, each time a new element is added, the least
    /// recently used (LRU) entry is removed.
    /// <para>
    /// Java makes it very easy to implement LRUHashMap - all its functionality is
    /// already available from <seealso cref="java.util.LinkedHashMap"/>, and we just need to
    /// configure that properly.
    /// </para>
    /// <para>
    /// Note that like HashMap, LRUHashMap is unsynchronized, and the user MUST
    /// synchronize the access to it if used from several threads. Moreover, while
    /// with HashMap this is only a concern if one of the threads is modifies the
    /// map, with LURHashMap every read is a modification (because the LRU order
    /// needs to be remembered) so proper synchronization is always necessary.
    /// </para>
    /// <para>
    /// With the usual synchronization mechanisms available to the user, this
    /// unfortunately means that LRUHashMap will probably perform sub-optimally under
    /// heavy contention: while one thread uses the hash table (reads or writes), any
    /// other thread will be blocked from using it - or even just starting to use it
    /// (e.g., calculating the hash function). A more efficient approach would be not
    /// to use LinkedHashMap at all, but rather to use a non-locking (as much as
    /// possible) thread-safe solution, something along the lines of
    /// java.util.concurrent.ConcurrentHashMap (though that particular class does not
    /// support the additional LRU semantics, which will need to be added separately
    /// using a concurrent linked list or additional storage of timestamps (in an
    /// array or inside the entry objects), or whatever).
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class LRUHashMap<TV, TU> where TU : class //this is implementation of LRU Cache
    {

        public int MaxSize { get; set; }


        private readonly LeastRecentlyUsedCache<TV, TU> _lru;
        public LRUHashMap(int maxSize = 16)
        {
            _lru = new LeastRecentlyUsedCache<TV, TU>(maxSize);
            MaxSize = maxSize;
        }

        
        public bool Put(TV cacheKey, TU value)
        {
            _lru.Set(cacheKey, value);
            return true;
        }

        public TU Get(TV cacheKey)
        {
            TU result;
            if (_lru.TryGetValue(cacheKey,out result))
                return result;

            return default(TU);
         
        }


        public int Size()
        {
            return _lru.Count;
        }

    }

    public class LeastRecentlyUsedCache<TKey, TValue>
    {
        private readonly Dictionary<TKey, Node> entries;
        private readonly int capacity;
        private Node head;
        private Node tail;

        private class Node
        {
            public Node Next { get; set; }
            public Node Previous { get; set; }
            public TKey Key { get; set; }
            public TValue Value { get; set; }
        }

        public LeastRecentlyUsedCache(int capacity = 16)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(
                    "capacity",
                    "Capacity should be greater than zero");
            this.capacity = capacity;
            entries = new Dictionary<TKey, Node>();
            head = null;
        }

        public int Count
        {
            get { return entries.Count; }
        }

        public void Set(TKey key, TValue value)
        {
            Node entry;
            if (!entries.TryGetValue(key, out entry))
            {
                entry = new Node { Key = key, Value = value };
                if (entries.Count == capacity)
                {
                    entries.Remove(tail.Key);
                    tail = tail.Previous;
                    if (tail != null) tail.Next = null;
                }
                entries.Add(key, entry);
            }

            entry.Value = value;
            MoveToHead(entry);
            if (tail == null) tail = head;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            Node entry;
            if (!entries.TryGetValue(key, out entry)) return false;
            MoveToHead(entry);
            value = entry.Value;
            return true;
        }

        private void MoveToHead(Node entry)
        {
            if (entry == head || entry == null) return;

            var next = entry.Next;
            var previous = entry.Previous;

            if (next != null) next.Previous = entry.Previous;
            if (previous != null) previous.Next = entry.Next;

            entry.Previous = null;
            entry.Next = head;

            if (head != null) head.Previous = entry;
            head = entry;

            if (tail == entry) tail = previous;
        }
    }
}