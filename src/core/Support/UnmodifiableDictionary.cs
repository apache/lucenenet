using System;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
    internal class UnmodifiableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private IDictionary<TKey, TValue> _dict;

        public UnmodifiableDictionary(IDictionary<TKey, TValue> dict)
        {
            _dict = dict;
        }

        public UnmodifiableDictionary()
        {
            _dict = new Dictionary<TKey, TValue>();
        }

        public void Add(TKey key, TValue value)
        {
            throw new InvalidOperationException("Unable to modify this dictionary.");
        }

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return _dict.Keys; }
        }

        public bool Remove(TKey key)
        {
            throw new InvalidOperationException("Unable to modify this dictionary.");
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return _dict.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue ret;
                _dict.TryGetValue(key, out ret);
                return ret;
            }
            set
            {
                throw new InvalidOperationException("Unable to modify this dictionary.");
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new InvalidOperationException("Unable to modify this dictionary.");
        }

        public void Clear()
        {
            throw new InvalidOperationException("Unable to modify this dictionary.");
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dict.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _dict.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new InvalidOperationException("Unable to modify this dictionary.");
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }
    }
}