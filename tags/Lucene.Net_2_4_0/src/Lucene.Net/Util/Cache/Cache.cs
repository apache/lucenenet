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

namespace Lucene.Net.Util.Cache
{
    /**
     * Base class for cache implementations.
     */
    public abstract class Cache
    {
        /**
         * Returns a thread-safe cache backed by the specified cache. 
         * In order to guarantee thread-safety, all access to the backed cache must
         * be accomplished through the returned cache.
         */
        public static Cache SynchronizedCache_Renamed(Cache cache)
        {
            return cache.GetSynchronizedCache();
        }

        /**
         * Puts a (key, value)-pair into the cache. 
         */
        public abstract void Put(object key, object value);

        /**
         * Returns the value for the given key. 
         */
        public abstract object Get(object key);

        /**
         * Returns whether the given key is in this cache. 
         */
        public abstract bool ContainsKey(object key);

        /**
         * Closes the cache.
         */
        public abstract void Close();

        /**
         * Called by {@link #synchronizedCache(Cache)}. This method
         * returns a {@link SynchronizedCache} instance that wraps
         * this instance by default and can be overridden to return
         * e. g. subclasses of {@link SynchronizedCache} or this
         * in case this cache is already synchronized.
         */
        protected internal virtual Cache GetSynchronizedCache()
        {
            return new SynchronizedCache(this);
        }

        /**
         * Simple Cache wrapper that synchronizes all
         * calls that access the cache. 
         */
        public class SynchronizedCache : Cache
        {
            object mutex;
            Cache cache;

            protected internal SynchronizedCache(Cache cache)
            {
                this.cache = cache;
                this.mutex = this;
            }

            protected internal SynchronizedCache(Cache cache, object mutex)
            {
                this.cache = cache;
                this.mutex = mutex;
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

            protected internal override Cache GetSynchronizedCache()
            {
                return this;
            }
        }
    }
}
