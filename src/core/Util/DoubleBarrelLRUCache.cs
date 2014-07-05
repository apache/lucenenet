using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace Lucene.Net.Util
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
    /// Simple concurrent LRU cache, using a "double barrel"
    /// approach where two ConcurrentHashMaps record entries.
    /// 
    /// <p>At any given time, one hash is primary and the other
    /// is secondary.  <seealso cref="#get"/> first checks primary, and if
    /// that's a miss, checks secondary.  If secondary has the
    /// entry, it's promoted to primary (<b>NOTE</b>: the key is
    /// cloned at this point).  Once primary is full, the
    /// secondary is cleared and the two are swapped.</p>
    /// 
    /// <p>this is not as space efficient as other possible
    /// concurrent approaches (see LUCENE-2075): to achieve
    /// perfect LRU(N) it requires 2*N storage.  But, this
    /// approach is relatively simple and seems in practice to
    /// not grow unbounded in size when under hideously high
    /// load.</p>
    /// 
    /// @lucene.internal
    /// </summary>

    public sealed class DoubleBarrelLRUCache<K, V> where K : DoubleBarrelLRUCache.CloneableKey
    {

        /// <summary>
        /// Object providing clone(); the key class must subclass this. </summary>
        public abstract class CloneableKey
        {
            public abstract CloneableKey Clone();
        }

        private readonly IDictionary<K, V> Cache1;
        private readonly IDictionary<K, V> Cache2;
        //private readonly AtomicInteger Countdown;
        private int Countdown;
        private volatile bool Swapped;
        private readonly int MaxSize;

        public DoubleBarrelLRUCache(int maxSize)
        {
            this.MaxSize = maxSize;
            Interlocked.Exchange(ref Countdown, maxSize);
            Cache1 = new ConcurrentDictionary<K, V>();
            Cache2 = new ConcurrentDictionary<K, V>();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @SuppressWarnings("unchecked") public V get(K key)
        public V Get(K key)
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<K,V> primary;
            IDictionary<K, V> primary;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<K,V> secondary;
            IDictionary<K, V> secondary;
            if (Swapped)
            {
                primary = Cache2;
                secondary = Cache1;
            }
            else
            {
                primary = Cache1;
                secondary = Cache2;
            }

            // Try primary first
            V result;
            primary.TryGetValue(key, out result);
            if (result == null)
            {
                // Not found -- try secondary
                result = secondary[key];
                if (result != null)
                {
                    // Promote to primary
                    Put((K)key.Clone(), result);
                }
            }
            return result;
        }

        public void Put(K key, V value)
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<K,V> primary;
            IDictionary<K, V> primary;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<K,V> secondary;
            IDictionary<K, V> secondary;
            if (Swapped)
            {
                primary = Cache2;
                secondary = Cache1;
            }
            else
            {
                primary = Cache1;
                secondary = Cache2;
            }
            primary[key] = value;

            if (Interlocked.Decrement(ref Countdown) == 0)
            {
                // Time to swap

                // NOTE: there is saturation risk here, that the
                // thread that's doing the clear() takes too long to
                // do so, while other threads continue to add to
                // primary, but in practice this seems not to be an
                // issue (see LUCENE-2075 for benchmark & details)

                // First, clear secondary
                secondary.Clear();

                // Second, swap
                Swapped = !Swapped;

                // Third, reset countdown
                Interlocked.Exchange(ref Countdown, MaxSize);
            }
        }
    }

    // .NET Port: non-generic base class to hold nested type
    public abstract class DoubleBarrelLRUCache
    {
        public abstract class CloneableKey
        {
            public abstract CloneableKey Clone();
        }
    }

}