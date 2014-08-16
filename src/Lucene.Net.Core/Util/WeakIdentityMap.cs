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

using System.Linq;

namespace Lucene.Net.Util
{
    using Lucene.Net.Support;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;



    /// <summary>
    /// Class WeakIdentityMap. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="TKey">The type of the t key.</typeparam>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    public sealed class WeakIdentityMap<TKey, TValue>
        where TKey : class
    {
       
        // we keep a hard reference to our NULL key, so map supports null keys that never get GCed:
        // ReSharper disable once StaticFieldInGenericType
        public static readonly object NULL_VALUE = new object();

        //private readonly ReferenceQueue<object> queue = new ReferenceQueue<object>();
        private readonly  IDictionary<IdentityWeakReference, TValue> backingStore;

        private readonly bool reapOnReadEnabled;

        /// <summary>
        /// Initializes a new instance of <see cref="WeakIdentityMap{TKey, TValue}"/>
        /// </summary>
        /// <param name="backingStore">The backing store.</param>
        /// <param name="reapOnRead">if set to <c>true</c> [reap on read].</param>
        private WeakIdentityMap(IDictionary<IdentityWeakReference, TValue> backingStore, bool reapOnRead)
        {
            this.backingStore = backingStore;
            this.reapOnReadEnabled = reapOnRead;
        }

        /// <summary>
        /// Returns the number of key-value mappings in this map. this result is a snapshot,
        /// and may not reflect unprocessed entries that will be removed before next
        /// attempted access because they are no longer referenced.
        /// </summary>
        public int Count
        {
            get
            {
                if (this.backingStore.Count == 0)
                {
                    return 0;
                }
                this.ReapOnRead();
                return this.backingStore.Count;
            }
        }

        /// <summary>
        /// Returns <c>True</c> when this map contains no key-value mappings. </summary>
        public bool Empty
        {
            get
            {
                return this.Count == 0;
            }
        }

        public IEnumerable<TKey> Keys
        {
            // .NET port: using this method which mimics IDictionary instead of KeyIterator()
            get
            {
                foreach (var key in backingStore.Keys)
                {
                    var target = key.Target;

                    if (target == null)
                        continue;
                    
                    if (Object.ReferenceEquals(target, NULL_VALUE))
                        yield return null;
                    else
                        yield return (TKey)target;
                }
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                this.ReapOnRead();
                return this.backingStore.Values;
            }
        }

        /// <summary>
        /// Initializes a new <see cref="WeakIdentityMap{TKey,TValue}"/> based on a <seealso cref="ConcurrentHashMap"/>.
        /// The map <a href="#reapInfo">cleans up the reference queue on every read operation</a>.
        /// </summary>
        public static WeakIdentityMap<TKey, TValue> NewConcurrentHashMap()
        {
            return NewConcurrentHashMap(true);
        }

        /// <summary>
        /// Creates a new {@code WeakIdentityMap} based on a <seealso cref="ConcurrentHashMap"/>. </summary>
        /// <param name="reapOnRead"> controls if the map <a href="#reapInfo">cleans up the reference queue on every read operation</a>. </param>
        public static WeakIdentityMap<TKey, TValue> NewConcurrentHashMap(bool reapOnRead)
        {
            return new WeakIdentityMap<TKey, TValue>(new ConcurrentDictionary<IdentityWeakReference, TValue>(), reapOnRead);
        }

        /// <summary>
        /// Creates a new {@code WeakIdentityMap} based on a non-synchronized <seealso cref="HashMap"/>.
        /// The map <a href="#reapInfo">cleans up the reference queue on every read operation</a>.
        /// </summary>
        public static WeakIdentityMap<TKey, TValue> NewHashMap()
        {
            return NewHashMap(false);
        }

        /// <summary>
        /// Creates a new {@code WeakIdentityMap} based on a non-synchronized <seealso cref="HashMap"/>. </summary>
        /// <param name="reapOnRead"> controls if the map <a href="#reapInfo">cleans up the reference queue on every read operation</a>. </param>
        public static WeakIdentityMap<TKey, TValue> NewHashMap(bool reapOnRead)
        {
            return new WeakIdentityMap<TKey, TValue>(new HashMap<IdentityWeakReference, TValue>(), reapOnRead);
        }
        /// <summary>
        /// Removes all of the mappings from this map. 
        /// </summary>
        public void Clear()
        {
            this.backingStore.Clear();
            this.Reap();
        }

        /// <summary>
        /// Returns <c>True</c> if this map contains a mapping for the specified key, otherwise <c>False</c>.
        /// </summary>
        public bool ContainsKey(object key)
        {
            this.ReapOnRead();
            return backingStore.ContainsKey(new IdentityWeakReference(key));
        }

        /// <summary>
        /// Returns the value to which the specified key is mapped. 
        /// </summary>
        public TValue Get(object key)
        {
            TValue value;
            
            this.ReapOnRead();
            // Java's collections return null. TryGetValue must be used.
            this.backingStore.TryGetValue(new IdentityWeakReference(key), out value);
            
            return value;
        }

        
        public TValue Put(TKey key, TValue value)
        {
            this.Reap();
            return this.backingStore[new IdentityWeakReference(key)] = value;
        }
        /// <summary>
        /// this method manually cleans up the reference queue to remove all garbage
        /// collected key/value pairs from the map.  
        /// </summary>
        public void Reap()
        {
            lock (backingStore)
            {
                var keysToRemove = backingStore.Keys.Where(o => !o.IsAlive).ToList();

                foreach (var key in keysToRemove)
                {
                    backingStore.Remove(key);
                }
            }
        }

        private void ReapOnRead()
        {
            if(this.reapOnReadEnabled)
                this.Reap();
        }

        /// <summary>
        /// Removes the mapping for a key from this weak hash map if it is present.
        /// Returns the value to which this map previously associated the key,
        /// or {@code null} if the map contained no mapping for the key.
        /// A return value of {@code null} does not necessarily indicate that
        /// the map contained.
        /// </summary>
        public bool Remove(object key)
        {
            this.Reap();
            return backingStore.Remove(new IdentityWeakReference(key));
        }

   


        private sealed class IdentityWeakReference : WeakReference
        {
            private readonly int hash;

            internal IdentityWeakReference(object obj/*, ReferenceQueue<object> queue*/)
                : base(obj ?? NULL_VALUE/*, queue*/)
            {
               

                hash = RuntimeHelpers.GetHashCode(obj);
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }
                if (obj is IdentityWeakReference)
                {
                    var reference = (IdentityWeakReference)obj;
                    if (this.Target == reference.Target)
                    {
                        return true;
                    }
                }
                return false;
            }

            public override int GetHashCode()
            {
                return hash;
            }
        }
    }
}