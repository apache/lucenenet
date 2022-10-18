// LUCENENET specific - factored out this class and replaced it with ConditionalWeakTable<TKey, TValue>.
// ConditionalWeakTable<TKey, TValue> is thread-safe and internally uses RuntimeHelpers.GetHashCode()
// to lookup the key, so it can be used as a direct replacement for WeakIdentityMap<TKey, TValue>
// in most cases.

//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Runtime.CompilerServices;
//using System.Collections;

//namespace Lucene.Net.Util
//{
//    /*
//	 * Licensed to the Apache Software Foundation (ASF) under one or more
//	 * contributor license agreements.  See the NOTICE file distributed with
//	 * this work for additional information regarding copyright ownership.
//	 * The ASF licenses this file to You under the Apache License, Version 2.0
//	 * (the "License"); you may not use this file except in compliance with
//	 * the License.  You may obtain a copy of the License at
//	 *
//	 *     http://www.apache.org/licenses/LICENSE-2.0
//	 *
//	 * Unless required by applicable law or agreed to in writing, software
//	 * distributed under the License is distributed on an "AS IS" BASIS,
//	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//	 * See the License for the specific language governing permissions and
//	 * limitations under the License.
//	 */

//    /// <summary>
//    /// Implements a combination of <c>java.util.WeakHashMap</c> and
//    /// <c>java.util.IdentityHashMap</c>.
//    /// Useful for caches that need to key off of a <c>==</c> comparison
//    /// instead of a <c>.Equals(object)</c>.
//    ///
//    /// <para/>This class is not a general-purpose <see cref="IDictionary{TKey, TValue}"/>
//    /// implementation! It intentionally violates
//    /// <see cref="IDictionary{TKey, TValue}"/>'s general contract, which mandates the use of the <see cref="object.Equals(object)"/> method
//    /// when comparing objects. This class is designed for use only in the
//    /// rare cases wherein reference-equality semantics are required.
//    ///
//    /// <para/>This implementation was forked from <a href="http://cxf.apache.org/">Apache CXF</a>
//    /// but modified to <b>not</b> implement the <see cref="IDictionary{TKey, TValue}"/> interface and
//    /// without any set views on it, as those are error-prone and inefficient,
//    /// if not implemented carefully. The map only contains <see cref="IEnumerable{T}.GetEnumerator()"/> implementations
//    /// on the values and not-GCed keys. Lucene's implementation also supports <c>null</c>
//    /// keys, but those are never weak!
//    ///
//    /// <para/><a name="reapInfo" />The map supports two modes of operation:
//    /// <list type="bullet">
//    ///     <item><term><c>reapOnRead = true</c>:</term><description> This behaves identical to a <c>java.util.WeakHashMap</c>
//    ///         where it also cleans up the reference queue on every read operation (<see cref="Get(object)"/>,
//    ///         <see cref="ContainsKey(object)"/>, <see cref="Count"/>, <see cref="GetValueEnumerator()"/>), freeing map entries
//    ///         of already GCed keys.</description></item>
//    ///     <item><term><c>reapOnRead = false</c>:</term><description> This mode does not call <see cref="Reap()"/> on every read
//    ///         operation. In this case, the reference queue is only cleaned up on write operations
//    ///         (like <see cref="Put(TKey, TValue)"/>). This is ideal for maps with few entries where
//    ///         the keys are unlikely be garbage collected, but there are lots of <see cref="Get(object)"/>
//    ///         operations. The code can still call <see cref="Reap()"/> to manually clean up the queue without
//    ///         doing a write operation.</description></item>
//    /// </list>
//    /// <para/>
//    /// @lucene.internal
//    /// </summary>
//    public sealed class WeakIdentityMap<TKey, TValue>
//         where TKey : class
//    {
//        private readonly IDictionary<IdentityWeakReference, TValue> backingStore;

//        private readonly bool reapOnRead;

//        /// <summary>
//        /// Creates a new <see cref="WeakIdentityMap{TKey, TValue}"/> based on a non-synchronized <see cref="Dictionary{TKey, TValue}"/>.
//        /// The map <a href="#reapInfo">cleans up the reference queue on every read operation</a>.
//        /// </summary>
//        public static WeakIdentityMap<TKey, TValue> NewHashMap()
//        {
//            return NewHashMap(false);
//        }

//        /// <summary>
//        /// Creates a new <see cref="WeakIdentityMap{TKey, TValue}"/> based on a non-synchronized <see cref="Dictionary{TKey, TValue}"/>. </summary>
//        /// <param name="reapOnRead"> controls if the map <a href="#reapInfo">cleans up the reference queue on every read operation</a>. </param>
//        public static WeakIdentityMap<TKey, TValue> NewHashMap(bool reapOnRead)
//        {
//            return new WeakIdentityMap<TKey, TValue>(new Dictionary<IdentityWeakReference, TValue>(), reapOnRead);
//        }

//        /// <summary>
//        /// Creates a new <see cref="WeakIdentityMap{TKey, TValue}"/> based on a <see cref="ConcurrentDictionary{TKey, TValue}"/>.
//        /// The map <a href="#reapInfo">cleans up the reference queue on every read operation</a>.
//        /// </summary>
//        public static WeakIdentityMap<TKey, TValue> NewConcurrentHashMap()
//        {
//            return NewConcurrentHashMap(true);
//        }

//        /// <summary>
//        /// Creates a new <see cref="WeakIdentityMap{TKey, TValue}"/> based on a <see cref="ConcurrentDictionary{TKey, TValue}"/>. </summary>
//        /// <param name="reapOnRead"> controls if the map <a href="#reapInfo">cleans up the reference queue on every read operation</a>. </param>
//        public static WeakIdentityMap<TKey, TValue> NewConcurrentHashMap(bool reapOnRead)
//        {
//            return new WeakIdentityMap<TKey, TValue>(new ConcurrentDictionary<IdentityWeakReference, TValue>(), reapOnRead);
//        }

//        /// <summary>
//        /// Private only constructor, to create use the static factory methods. </summary>
//        private WeakIdentityMap(IDictionary<IdentityWeakReference, TValue> backingStore, bool reapOnRead)
//        {
//            this.backingStore = backingStore;
//            this.reapOnRead = reapOnRead;
//        }

//        /// <summary>
//        /// Removes all of the mappings from this map. </summary>
//        public void Clear()
//        {
//            backingStore.Clear();
//            Reap();
//        }

//        /// <summary>
//        /// Returns <c>true</c> if this map contains a mapping for the specified key. </summary>
//        public bool ContainsKey(object key)
//        {
//            if (reapOnRead)
//            {
//                Reap();
//            }
//            return backingStore.ContainsKey(new IdentityWeakReference(key));
//        }

//        /// <summary>
//        /// Returns the value to which the specified key is mapped. </summary>
//        public TValue Get(object key)
//        {
//            if (reapOnRead)
//            {
//                Reap();
//            }

//            TValue val;
//            if (backingStore.TryGetValue(new IdentityWeakReference(key), out val))
//            {
//                return val;
//            }
//            else
//            {
//                return default;
//            }
//        }

//        /// <summary>
//        /// Associates the specified value with the specified key in this map.
//        /// If the map previously contained a mapping for this key, the old value
//        /// is replaced.
//        /// </summary>
//        public TValue Put(TKey key, TValue value)
//        {
//            Reap();
//            return backingStore[new IdentityWeakReference(key)] = value;
//        }

//        /// <summary>
//        /// Gets an <see cref="IEnumerable{TKey}"/> object containing the keys of the <see cref="WeakIdentityMap{TKey, TValue}"/>.
//        /// </summary>
//        public IEnumerable<TKey> Keys
//        {
//            get
//            {
//                return new KeyWrapper(this);
//            }
//        }

//        /// <summary>
//        /// LUCENENET specific class to allow the 
//        /// GetEnumerator() method to be overridden
//        /// for the keys so we can return an enumerator
//        /// that is smart enough to clean up the dead keys
//        /// and also so that MoveNext() returns false in the
//        /// event there are no more values left (instead of returning
//        /// a null value in an extra enumeration).
//        /// </summary>
//        private class KeyWrapper : IEnumerable<TKey>
//        {
//            private readonly WeakIdentityMap<TKey, TValue> outerInstance;
//            public KeyWrapper(WeakIdentityMap<TKey, TValue> outerInstance)
//            {
//                this.outerInstance = outerInstance;
//            }
//            public IEnumerator<TKey> GetEnumerator()
//            {
//                outerInstance.Reap();
//                return new EnumeratorAnonymousClass(outerInstance);
//            }

//            IEnumerator IEnumerable.GetEnumerator()
//            {
//                return GetEnumerator();
//            }
//        }

//        /// <summary>
//        /// Gets an <see cref="IEnumerable{TKey}"/> object containing the values of the <see cref="WeakIdentityMap{TKey, TValue}"/>.
//        /// </summary>
//        public IEnumerable<TValue> Values
//        {
//            get
//            {
//                if (reapOnRead) Reap();
//                return backingStore.Values;
//            }
//        }

//        /// <summary>
//        /// Returns <c>true</c> if this map contains no key-value mappings. </summary>
//        public bool IsEmpty
//        {
//            get
//            {
//                return Count == 0;
//            }
//        }

//        /// <summary>
//        /// Removes the mapping for a key from this weak hash map if it is present.
//        /// Returns the value to which this map previously associated the key,
//        /// or <c>null</c> if the map contained no mapping for the key.
//        /// A return value of <c>null</c> does not necessarily indicate that
//        /// the map contained.
//        /// </summary>
//        public bool Remove(object key)
//        {
//            Reap();
//            return backingStore.Remove(new IdentityWeakReference(key));
//        }

//        /// <summary>
//        /// Returns the number of key-value mappings in this map. This result is a snapshot,
//        /// and may not reflect unprocessed entries that will be removed before next
//        /// attempted access because they are no longer referenced.
//        /// <para/>
//        /// NOTE: This was size() in Lucene.
//        /// </summary>
//        public int Count
//        {
//            get
//            {
//                if (backingStore.Count == 0)
//                {
//                    return 0;
//                }
//                if (reapOnRead)
//                {
//                    Reap();
//                }
//                return backingStore.Count;
//            }
//        }

//        private sealed class EnumeratorAnonymousClass : IEnumerator<TKey>
//        {
//            private readonly WeakIdentityMap<TKey, TValue> outerInstance;
//            private readonly IEnumerator<KeyValuePair<IdentityWeakReference, TValue>> enumerator;

//            public EnumeratorAnonymousClass(WeakIdentityMap<TKey, TValue> outerInstance)
//            {
//                this.outerInstance = outerInstance;
//                enumerator = outerInstance.backingStore.GetEnumerator();
//            }

//            // holds strong reference to next element in backing iterator:
//            private object next = null;

//            public TKey Current
//            {
//                get
//                {
//                    return (TKey)next;
//                }
//            }

//            object IEnumerator.Current
//            {
//                get
//                {
//                    return Current;
//                }
//            }


//            public void Dispose()
//            {
//                enumerator.Dispose();
//            }


//            public bool MoveNext()
//            {
//                while (enumerator.MoveNext())
//                {
//                    next = enumerator.Current.Key.Target;
//                    if (next != null)
//                    {
//                        // unfold "null" special value:
//                        if (next == NULL)
//                            next = null;
//                        return true;
//                    }
//                }
//                return false;
//            }

//            public void Reset()
//            {
//                enumerator.Reset();
//            }
//        }

//        /// <summary>
//        /// Returns an iterator over all values of this map.
//        /// This iterator may return values whose key is already
//        /// garbage collected while iterator is consumed,
//        /// especially if <see cref="reapOnRead"/> is <c>false</c>.
//        /// <para/>
//        /// NOTE: This was valueIterator() in Lucene.
//        /// </summary>
//        public IEnumerator<TValue> GetValueEnumerator()
//        {
//            if (reapOnRead)
//            {
//                Reap();
//            }
//            return backingStore.Values.GetEnumerator();
//        }

//        /// <summary>
//        /// This method manually cleans up the reference queue to remove all garbage
//        /// collected key/value pairs from the map. Calling this method is not needed
//        /// if <c>reapOnRead = true</c>. Otherwise it might be a good idea
//        /// to call this method when there is spare time (e.g. from a background thread). 
//        /// <a href="#reapInfo">Information about the <c>reapOnRead</c> setting</a>		
//        /// </summary>
//        public void Reap()
//        {
//            List<IdentityWeakReference> keysToRemove = null;
//            foreach (var item in backingStore)
//            {
//                if (!item.Key.IsAlive)
//                {
//                    // create the list of keys to remove only if there are keys to remove.
//                    // this reduces heap pressure
//                    if (keysToRemove is null)
//                        keysToRemove = new List<IdentityWeakReference>();
//                    keysToRemove.Add(item.Key);
//                }
//            }

//            if (keysToRemove != null)
//                foreach (var key in keysToRemove)
//                {
//                    backingStore.Remove(key);
//                }
//        }

//        // we keep a hard reference to our NULL key, so map supports null keys that never get GCed:
//        internal static readonly object NULL = new object();

//        private sealed class IdentityWeakReference : WeakReference
//        {
//            private readonly int hash;

//            internal IdentityWeakReference(object obj/*, ReferenceQueue<object> queue*/)
//                : base(obj is null ? NULL : obj/*, queue*/)
//            {
//                hash = RuntimeHelpers.GetHashCode(obj);
//            }

//            public override int GetHashCode()
//            {
//                return hash;
//            }

//            public override bool Equals(object o)
//            {
//                if (this == o)
//                {
//                    return true;
//                }
//                IdentityWeakReference @ref = o as IdentityWeakReference;
//                if (@ref != null)
//                {
//                    if (this.Target == @ref.Target)
//                    {
//                        return true;
//                    }
//                }
//                return false;
//            }
//        }
//    }
//}