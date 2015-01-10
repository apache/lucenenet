using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
    internal class UnmodifiableList<TValue> : IList<TValue>
    {
        private IList<TValue> _list;

        public UnmodifiableList(IList<TValue> list)
        {
            _list = list;
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TValue item)
        {
            throw new InvalidOperationException("Unable to modify this list.");
        }

        public void Clear()
        {
            throw new InvalidOperationException("Unable to modify this list.");
        }

        public bool Contains(TValue item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(TValue item)
        {
            throw new InvalidOperationException("Unable to modify this list.");
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public int IndexOf(TValue item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, TValue item)
        {
            throw new InvalidOperationException("Unable to modify this list.");
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("Unable to modify this list.");
        }

        public TValue this[int index]
        {
            get { return _list[index]; }
            set { throw new InvalidOperationException("Unable to modify this list."); }
        }
    }
}