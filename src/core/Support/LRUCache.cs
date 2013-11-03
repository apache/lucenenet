using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public class LRUCache<TKey, TValue> : IDictionary<TKey, TValue>
    {
        object sync = new object();
        Dictionary<TKey, TValue> data;
        IndexedLinkedList<TKey> lruList = new IndexedLinkedList<TKey>();
        ICollection<KeyValuePair<TKey, TValue>> dataAsCollection;
        int capacity;

        public LRUCache(int capacity)
        {

            if (capacity <= 0)
            {
                throw new ArgumentException("capacity should always be bigger than 0");
            }

            data = new Dictionary<TKey, TValue>(capacity);
            dataAsCollection = data;
            this.capacity = capacity;
        }

        public void Add(TKey key, TValue value)
        {
            if (!ContainsKey(key))
            {
                this[key] = value;
            }
            else
            {
                throw new ArgumentException("An attempt was made to insert a duplicate key in the cache.");
            }
        }

        public bool ContainsKey(TKey key)
        {
            return data.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return data.Keys;
            }
        }

        public bool Remove(TKey key)
        {
            bool existed = data.Remove(key);
            lruList.Remove(key);
            return existed;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return data.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return data.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                var value = data[key];
                lruList.Remove(key);
                lruList.Add(key);
                return value;
            }
            set
            {
                data[key] = value;
                lruList.Remove(key);
                lruList.Add(key);

                if (data.Count > capacity)
                {
                    data.Remove(lruList.First);
                    lruList.RemoveFirst();
                }
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            data.Clear();
            lruList.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dataAsCollection.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            dataAsCollection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return data.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {

            bool removed = dataAsCollection.Remove(item);
            if (removed)
            {
                lruList.Remove(item.Key);
            }
            return removed;
        }


        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dataAsCollection.GetEnumerator();
        }


        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)data).GetEnumerator();
        }

        public int Capacity
        {
            get { return capacity; }
            set
            {
                capacity = value;
            }
        }
    }
}
