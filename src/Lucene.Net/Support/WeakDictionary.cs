﻿/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
using System;
using System.Collections;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Support
{
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class WeakDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : class 
    {
        private IDictionary<WeakKey<TKey>, TValue> _hm;
        private int _gcCollections = 0;

        public WeakDictionary(int initialCapacity)
            : this(initialCapacity, Arrays.Empty<KeyValuePair<TKey, TValue>>())
        { }

        public WeakDictionary()
            : this(32, Arrays.Empty<KeyValuePair<TKey, TValue>>())
        { }

        public WeakDictionary(IEnumerable<KeyValuePair<TKey, TValue>> otherDictionary)
            : this(32, otherDictionary)
        { }

        private WeakDictionary(int initialCapacity, IEnumerable<KeyValuePair<TKey, TValue>> otherDict)
        {
            _hm = new JCG.Dictionary<WeakKey<TKey>, TValue>(initialCapacity);
            foreach (var kvp in otherDict)
            {
                _hm.Add(new WeakKey<TKey>(kvp.Key), kvp.Value);
            }
        }

        // LUCENENET NOTE: Added AddOrUpdate method so we don't need so many conditional compilation blocks.
        // This is just to cascade the call to this[key] = value
        public void AddOrUpdate(TKey key, TValue value) => this[key] = value;

        private void Clean()
        {
            if (_hm.Count == 0) return;
            var newHm = new JCG.Dictionary<WeakKey<TKey>, TValue>(_hm.Count);
            foreach (var kvp in _hm)
            {
                if (kvp.Key.TryGetTarget(out TKey _))
                {
                    // LUCENENET: There is a tiny chance that a call to remove the item
                    // from the dictionary can happen before this line is executed. Therefore,
                    // just discard the reference and add it as is, even if it is no longer valid
                    // in this edge case. It is far more efficient to re-use the same instances, anyway.
                    newHm.Add(kvp.Key, kvp.Value);
                }
            }
            _hm = newHm;
        }

        private void CleanIfNeeded()
        {
            int currentColCount = GC.CollectionCount(0);
            if (currentColCount > _gcCollections)
            {
                Clean();
                _gcCollections = currentColCount;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var kvp in _hm)
            {
                if (kvp.Key.TryGetTarget(out TKey key))
                {
                    yield return new KeyValuePair<TKey, TValue>(key, kvp.Value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            CleanIfNeeded();
            ((ICollection<KeyValuePair<WeakKey<TKey>, TValue>>)_hm).Add(
                new KeyValuePair<WeakKey<TKey>, TValue>(new WeakKey<TKey>(item.Key), item.Value));
        }

        public void Clear()
        {
            _hm.Clear();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<WeakKey<TKey>, TValue>>)_hm).Contains(
                new KeyValuePair<WeakKey<TKey>, TValue>(new WeakKey<TKey>(item.Key), item.Value));
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<WeakKey<TKey>, TValue>>)_hm).Remove(
                new KeyValuePair<WeakKey<TKey>, TValue>(new WeakKey<TKey>(item.Key), item.Value));
        }

        public int Count
        {
            get
            {
                CleanIfNeeded();
                return _hm.Count;
            }
        }

        public bool IsReadOnly => false;

        public bool ContainsKey(TKey key)
        {
            return _hm.ContainsKey(new WeakKey<TKey>(key));
        }

        public void Add(TKey key, TValue value)
        {
            CleanIfNeeded();
            _hm.Add(new WeakKey<TKey>(key), value);
        }

        public bool Remove(TKey key)
        {
            return _hm.Remove(new WeakKey<TKey>(key));
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _hm.TryGetValue(new WeakKey<TKey>(key), out value);
        }

        public TValue this[TKey key]
        {
            get
            {
                if (!this.ContainsKey(key))
                    return default(TValue);
                return _hm[new WeakKey<TKey>(key)];
            }
            set
            {
                CleanIfNeeded();
                _hm[new WeakKey<TKey>(key)] = value;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                CleanIfNeeded();
                return new KeyCollection(_hm);
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                CleanIfNeeded();
                return _hm.Values;
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        #region Nested Class: KeyCollection

        private class KeyCollection : ICollection<TKey>
        {
            private readonly IDictionary<WeakKey<TKey>, TValue> _internalDict;

            public KeyCollection(IDictionary<WeakKey<TKey>, TValue> dict)
            {
                _internalDict = dict;
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                foreach (var key in _internalDict.Keys)
                {
                    if (key.TryGetTarget(out TKey target))
                        yield return target;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                throw new NotImplementedException("Implement this as needed");
            }

            public int Count => _internalDict.Count + 1;

            public bool IsReadOnly => true;

            #region Explicit Interface Definitions

            bool ICollection<TKey>.Contains(TKey item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TKey>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException();
            }

#endregion Explicit Interface Definitions
        }

        #endregion Nested Class: KeyCollection

        /// <summary>
        /// A weak reference wrapper for the hashtable keys. Whenever a key\value pair
        /// is added to the hashtable, the key is wrapped using a WeakKey. WeakKey saves the
        /// value of the original object hashcode for fast comparison.
        /// </summary>
        private class WeakKey<T> where T : class
        {
            private readonly WeakReference<T> reference;
            private readonly int hashCode;

            public WeakKey(T key)
            {
                if (key is null)
                    throw new ArgumentNullException(nameof(key));

                hashCode = key is null ? 0 : key.GetHashCode();
                reference = new WeakReference<T>(key);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (obj is WeakKey<T> other)
                {
                    bool gotThis = this.TryGetTarget(out T thisTarget), gotOther = other.TryGetTarget(out T otherTarget);
                    if (gotThis && gotOther && thisTarget.Equals(otherTarget))
                        return true;
                    else if (gotThis == false && gotOther == false)
                        return true;
                }

                return false;
            }

            public bool TryGetTarget(out T target) => reference.TryGetTarget(out target);
        }
    }
}
#endif