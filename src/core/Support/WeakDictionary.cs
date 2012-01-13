/*
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Just a wrapper for WeakHashtable
    /// </summary>
    public class WeakDictionary<K, V> : IDictionary<K, V>
    {
        Lucene.Net.Support.WeakHashTable _WeakHashtable = new Lucene.Net.Support.WeakHashTable();

        public void Add(K key, V value)
        {
            _WeakHashtable.Add(key, value);
        }

        public bool ContainsKey(K key)
        {
            return _WeakHashtable.ContainsKey(key);
        }

        public ICollection<K> Keys
        {
            get
            {
                System.Collections.ArrayList list = (System.Collections.ArrayList)_WeakHashtable.Keys;
                K[] keys = new K[list.Count];
                _WeakHashtable.Keys.CopyTo(keys, 0);
                return keys;
            }
        }

        public bool Remove(K key)
        {
            bool b = _WeakHashtable.ContainsKey(key);
            if(b) _WeakHashtable.Remove(key);
            return b;
        }

        public bool TryGetValue(K key, out V value)
        {
            throw new NotImplementedException();
        }

        public ICollection<V> Values
        {
            get { throw new NotImplementedException(); }
        }

        public V this[K key]
        {
            get
            {
                return (V)_WeakHashtable[key];
            }
            set
            {
                _WeakHashtable[key] = value;
            }
        }

        public void Add(KeyValuePair<K, V> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            _WeakHashtable.Clear();
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _WeakHashtable.Count; }
        }

        public bool IsReadOnly
        {
            get { return _WeakHashtable.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return new WeakDictioanaryEnumerator<K, V>(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        class WeakDictioanaryEnumerator<K, V> : IEnumerator<KeyValuePair<K, V>>
        {
            K[] _Keys;
            int _Index = -1;

            WeakDictionary<K, V> _Dict;
            
            public WeakDictioanaryEnumerator(WeakDictionary<K,V> dict)
            {
                _Dict = dict;
                _Keys = (K[])dict.Keys;
            }

            public KeyValuePair<K, V> Current
            {
                get 
                { 
                    return new KeyValuePair<K,V>(_Keys[_Index], _Dict[_Keys[_Index]]);
                }
            }

            public void Dispose()
            {
            
            }

            object System.Collections.IEnumerator.Current
            {
                get { throw new NotImplementedException(); }
            }

            public bool MoveNext()
            {
                if (_Index < _Keys.Length - 1)
                {
                    _Index++;
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
