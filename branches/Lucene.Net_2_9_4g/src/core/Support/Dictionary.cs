/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;

namespace Lucene.Net.Support
{
    [Serializable]
    public class Dictionary<K, V> : System.Collections.Generic.IDictionary<K, V>
    {
        System.Collections.Generic.IDictionary<K, V> _Dict = null;
        
        public Dictionary()
        {
            _Dict = new System.Collections.Generic.Dictionary<K, V>();
        }

        public Dictionary(bool sortedDictionary)
        {
            _Dict = new System.Collections.Generic.SortedDictionary<K, V>();
        }

        public Dictionary(int capacity)
        {
            _Dict = new System.Collections.Generic.Dictionary<K, V>(capacity);
        }

        public Dictionary(System.Collections.Generic.IDictionary<K,V> dict)
        {
            _Dict = new System.Collections.Generic.Dictionary<K, V>(dict);
        }

        public Dictionary(Dictionary<K, V> dict)
        {
            _Dict = new System.Collections.Generic.Dictionary<K, V>(dict);
        }

        public void Add(K key, V value)
        {
            _Dict.Add(key, value);
        }

        public bool ContainsKey(K key)
        {
            return _Dict.ContainsKey(key);
        }

        public System.Collections.Generic.ICollection<K> Keys
        {
            get { return _Dict.Keys; }
        }

        public bool Remove(K key)
        {
            return _Dict.Remove(key);
        }

        public bool TryGetValue(K key, out V value)
        {
            return _Dict.TryGetValue(key, out value);
        }

        public System.Collections.Generic.ICollection<V> Values
        {
            get { return _Dict.Values; }
        }

        public V this[K key]
        {
            get
            {
                V val = default(V);
                _Dict.TryGetValue(key, out val);
                return val;
            }
            set
            {
                _Dict[key] = value;
            }
        }

        public void Add(System.Collections.Generic.IDictionary<K, V> items)
        {
            foreach (K key in items.Keys)
            {
                _Dict.Add(key, items[key]);
            }
        }

        public void Add(System.Collections.Generic.KeyValuePair<K, V> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            _Dict.Clear();
        }

        public bool Contains(System.Collections.Generic.KeyValuePair<K, V> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(System.Collections.Generic.KeyValuePair<K, V>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _Dict.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(System.Collections.Generic.KeyValuePair<K, V> item)
        {
            throw new NotImplementedException();
        }

        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>> GetEnumerator()
        {
            return _Dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}