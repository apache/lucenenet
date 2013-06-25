using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public class TreeMap<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> dict;

        public TreeMap()
        {
            dict = new SortedDictionary<TKey, TValue>();
        }

        public TreeMap(IComparer<TKey> comparer)
        {
            dict = new SortedDictionary<TKey, TValue>(comparer);
        }

        public TreeMap(IDictionary<TKey, TValue> dict)
        {
            this.dict = new SortedDictionary<TKey, TValue>(dict);
        }

        public TreeMap(IDictionary<TKey, TValue> dict, IComparer<TKey> comparer)
        {
            this.dict = new SortedDictionary<TKey, TValue>(dict, comparer);
        }

        public void Add(TKey key, TValue value)
        {
            dict.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return dict.Keys; }
        }

        public bool Remove(TKey key)
        {
            return dict.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dict.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return dict.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;

                if (dict.TryGetValue(key, out value))
                    return value;
                else
                    return default(TValue);
            }
            set
            {
                dict[key] = value;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            dict.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            dict.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            dict.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return dict.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return dict.Remove(item);   
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
