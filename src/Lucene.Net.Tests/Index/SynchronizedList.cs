using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.Index
{

    internal class SynchronizedList<T> : IList<T>
    {
        private readonly List<T> _list = new List<T>();

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public T this[int index]
        {
            get { return _list[index]; }
            set
            {
                _list[index] = value;
            }
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(T item)
        {
            _lock.EnterWriteLock();

            try
            {
                _list.Add(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();

            try
            {
                _list.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            _lock.EnterReadLock();

            try
            {
                return _list.Contains(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _lock.EnterWriteLock();

            try
            {
                _list.CopyTo(array, arrayIndex);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            _lock.EnterReadLock();

            try
            {
                return _list.GetEnumerator();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int IndexOf(T item)
        {
            _lock.EnterReadLock();

            try
            {
                return _list.IndexOf(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Insert(int index, T item)
        {
            _lock.EnterWriteLock();

            try
            {
                _list.Insert(index, item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Remove(T item)
        {
            _lock.EnterWriteLock();

            try
            {
                return _list.Remove(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RemoveAt(int index)
        {
            _lock.EnterWriteLock();

            try
            {
                _list.RemoveAt(index);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
