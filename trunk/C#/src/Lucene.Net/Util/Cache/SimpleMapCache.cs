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
using ICollection = System.Collections.ICollection;

namespace Lucene.Net.Util.Cache
{
    /**
     * Simple cache implementation that uses a HashMap to store (key, value) pairs.
     * This cache is not synchronized, use {@link Cache#synchronizedCache(Cache)}
     * if needed.
     */
    public class SimpleMapCache : Cache
    {
        protected Hashtable map;

        public SimpleMapCache()
            : this(new Hashtable())
        {
        }

        public SimpleMapCache(Hashtable map)
        {
            this.map = map;
        }

        public override object Get(object key)
        {
            return map[key];
        }

        public override void Put(object key, object value)
        {
            map[key] = value;
        }

        public override void Close()
        {
            // NOOP
        }

        public override bool ContainsKey(object key)
        {
            return map.ContainsKey(key);
        }

        /**
         * Returns a Set containing all keys in this cache.
         */
        public virtual ICollection keySet()
        {
            return map.Keys;
        }

        protected internal override Cache GetSynchronizedCache()
        {
            return new SynchronizedSimpleMapCache(this);
        }

        private class SynchronizedSimpleMapCache : SimpleMapCache
        {
            object mutex;
            SimpleMapCache cache;

            protected internal SynchronizedSimpleMapCache(SimpleMapCache cache)
            {
                this.cache = cache;
                this.mutex = this;
            }

            public override void Put(object key, object value)
            {
                lock (mutex) { cache.Put(key, value); }
            }

            public override object Get(object key)
            {
                lock (mutex) { return cache.Get(key); }
            }

            public override bool ContainsKey(object key)
            {
                lock (mutex) { return cache.ContainsKey(key); }
            }

            public override void Close()
            {
                lock (mutex) { cache.Close(); }
            }

            public override ICollection keySet()
            {
                lock (mutex) { return cache.keySet(); }
            }

            protected internal override Cache GetSynchronizedCache()
            {
                return this;
            }
        }
    }
}
