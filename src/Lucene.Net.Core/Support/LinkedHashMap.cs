using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
    /*
    The MIT License (MIT)

    Copyright (c) 2014 matarillo

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
    */

    // Source: http://qiita.com/matarillo/items/c09e0f3e5a61f84a51e2
    // 2016-11-05 - Shad Storhaug
    // Modified from its original form to be backed by a 
    // HashMap rather than a Dictionary so it will support null keys.
    // 2016-11-05 - Shad Storhaug
    // Added KeyCollection and ValueCollection classes to prevent having to do
    // an O(n) operation when calling the Keys or Values properties.

    /// <summary>
    /// LinkedHashMap is a specialized dictionary that preserves the entry order of elements.
    /// Like a HashMap, there can be a <c>null</c> key, but it also guarantees that the enumeration
    /// order of the elements are the same as insertion order, regardless of the number of add/remove/update
    /// operations that are performed on it.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary</typeparam>
    /// <remarks>
    /// <h2>Unordered Dictionaries</h2>
    /// <list type="bullet">
    ///     <item><see cref="Dictionary{TKey, TValue}"/> - use when order is not important and all keys are non-null.</item>
    ///     <item><see cref="HashMap{TKey, TValue}"/> - use when order is not important and support for a null key is required.</item>
    /// </list>
    /// <h2>Ordered Dictionaries</h2>
    /// <list type="bullet">
    ///     <item><see cref="LinkedHashMap{TKey, TValue}"/> - use when you need to preserve entry insertion order. Keys are nullable.</item>
    ///     <item><see cref="SortedDictionary{TKey, TValue}"/> - use when you need natural sort order. Keys must be unique.</item>
    ///     <item><see cref="TreeDictionary{K, V}"/> - use when you need natural sort order. Keys may contain duplicates.</item>
    ///     <item><see cref="LurchTable{TKey, TValue}"/> - use when you need to sort by most recent access or most recent update. Works well for LRU caching.</item>
    /// </list>
    /// </remarks>
    public class LinkedHashMap<TKey, TValue> : HashMap<TKey, TValue>, IDictionary<TKey, TValue>
    {
        private readonly HashMap<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> dict;
        private readonly LinkedList<KeyValuePair<TKey, TValue>> list;

        #region Constructors

        public LinkedHashMap()
        {
            dict = new HashMap<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
            list = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        public LinkedHashMap(IEqualityComparer<TKey> comparer)
        {
            dict = new HashMap<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(comparer);
            list = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        public LinkedHashMap(int capacity)
        {
            dict = new HashMap<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(capacity);
            list = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        public LinkedHashMap(int capacity, IEqualityComparer<TKey> comparer)
        {
            dict = new HashMap<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(capacity, comparer);
            list = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        public LinkedHashMap(IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            var countable = source as ICollection;
            if (countable != null)
            {
                dict = new HashMap<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(countable.Count);
            }
            else
            {
                dict = new HashMap<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
            }
            list = new LinkedList<KeyValuePair<TKey, TValue>>();
            foreach (var pair in source)
            {
                this[pair.Key] = pair.Value;
            }
        }

        public LinkedHashMap(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            var countable = source as ICollection;
            if (countable != null)
            {
                dict = new HashMap<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(countable.Count, comparer);
            }
            else
            {
                dict = new HashMap<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(comparer);
            }
            list = new LinkedList<KeyValuePair<TKey, TValue>>();
            foreach (var pair in source)
            {
                this[pair.Key] = pair.Value;
            }
        }

        #endregion

        #region IEnumerable implementation

        public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion

        #region IDictionary implementation

        public override bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public override void Add(TKey key, TValue value)
        {
            DoAdd(key, value);
        }

        private void DoAdd(TKey key, TValue value)
        {
            var pair = new KeyValuePair<TKey, TValue>(key, value);
            var node = new LinkedListNode<KeyValuePair<TKey, TValue>>(pair);
            dict.Add(key, node);
            list.AddLast(node);
        }

        public override bool Remove(TKey key)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> n;
            if (!dict.TryGetValue(key, out n))
            {
                return false;
            }
            DoRemove(n);
            return true;
        }

        private void DoRemove(LinkedListNode<KeyValuePair<TKey, TValue>> node)
        {
            dict.Remove(node.Value.Key);
            list.Remove(node);
        }

        public override bool TryGetValue(TKey key, out TValue value)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> n;
            if (dict.TryGetValue(key, out n))
            {
                value = n.Value.Value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        private bool TryGetNode(TKey key, TValue value, out LinkedListNode<KeyValuePair<TKey, TValue>> node)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> n;
            if (dict.TryGetValue(key, out n) && EqualityComparer<TValue>.Default.Equals(value, n.Value.Value))
            {
                node = n;
                return true;
            }
            node = null;
            return false;
        }

        public override TValue this[TKey key]
        {
            get
            {
                var node = dict[key];
                if (node == null)
                    return default(TValue);

                return node.Value.Value;
            }
            set
            {
                LinkedListNode<KeyValuePair<TKey, TValue>> n;
                if (!dict.TryGetValue(key, out n))
                {
                    DoAdd(key, value);
                    return;
                }
                DoSet(n, key, value);
            }
        }

        private void DoSet(LinkedListNode<KeyValuePair<TKey, TValue>> node, TKey key, TValue value)
        {
            var pair = new KeyValuePair<TKey, TValue>(key, value);
            var newNode = new LinkedListNode<KeyValuePair<TKey, TValue>>(pair);
            dict[key] = newNode;
            list.AddAfter(node, newNode);
            list.Remove(node);
        }

        public override ICollection<TKey> Keys
        {
            get
            {
                return new KeyCollection(this);
            }
        }

        public override ICollection<TValue> Values
        {
            get
            {
                return new ValueCollection(this);
            }
        }

        #endregion

        #region ICollection implementation

        public override void Clear()
        {
            dict.Clear();
            list.Clear();
        }

        public override int Count
        {
            get { return dict.Count; }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public override bool Contains(KeyValuePair<TKey, TValue> item)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> pair;
            return TryGetNode(item.Key, item.Value, out pair);
        }

        public override void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public override bool Remove(KeyValuePair<TKey, TValue> item)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node;
            if (!TryGetNode(item.Key, item.Value, out node))
            {
                return false;
            }
            DoRemove(node);
            return true;
        }

        #endregion

        #region KeyCollection

        internal class KeyCollection : ICollection<TKey>
        {
            private readonly LinkedHashMap<TKey, TValue> outerInstance;

            public KeyCollection(LinkedHashMap<TKey, TValue> outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public int Count
            {
                get
                {
                    return outerInstance.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public void Add(TKey item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                outerInstance.Clear();
            }

            public bool Contains(TKey item)
            {
                return outerInstance.ContainsKey(item);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                foreach (var element in outerInstance.list)
                {
                    array[arrayIndex++] = element.Key;
                }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                return new KeyEnumerator(outerInstance.list.GetEnumerator());
            }

            public bool Remove(TKey item)
            {
                return outerInstance.Remove(item);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #region KeyEnumerator

            internal class KeyEnumerator : IEnumerator<TKey>
            {
                private readonly IEnumerator<KeyValuePair<TKey, TValue>> innerEnumerator;

                public KeyEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> innerEnumerator)
                {
                    this.innerEnumerator = innerEnumerator;
                }

                public TKey Current
                {
                    get
                    {
                        return innerEnumerator.Current.Key;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public void Dispose()
                {
                    innerEnumerator.Dispose();
                }

                public bool MoveNext()
                {
                    return innerEnumerator.MoveNext();
                }

                public void Reset()
                {
                    innerEnumerator.Reset();
                }
            }

            #endregion
        }

        #endregion

        #region ValueCollection

        internal class ValueCollection : ICollection<TValue>
        {
            private readonly LinkedHashMap<TKey, TValue> outerInstance;

            public ValueCollection(LinkedHashMap<TKey, TValue> outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public int Count
            {
                get
                {
                    return outerInstance.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public void Add(TValue item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                outerInstance.Clear();
            }

            public bool Contains(TValue item)
            {
                return outerInstance.ContainsValue(item);
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                foreach (var element in outerInstance.list)
                {
                    array[arrayIndex++] = element.Value;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return new ValueEnumerator(outerInstance.list.GetEnumerator());
            }

            public bool Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }


            internal class ValueEnumerator : IEnumerator<TValue>
            {
                private readonly IEnumerator<KeyValuePair<TKey, TValue>> innerEnumerator;

                public ValueEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> innerEnumerator)
                {
                    this.innerEnumerator = innerEnumerator;
                }

                public TValue Current
                {
                    get
                    {
                        return innerEnumerator.Current.Value;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public void Dispose()
                {
                    innerEnumerator.Dispose();
                }

                public bool MoveNext()
                {
                    return innerEnumerator.MoveNext();
                }

                public void Reset()
                {
                    innerEnumerator.Reset();
                }
            }
        }

        

        #endregion
    }
}
