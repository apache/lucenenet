using System;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Support
{
    public class ConcurrentList<T> : IList<T>
    {
        private IList<T> _list;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public ConcurrentList(IEnumerable<T> enumerable)
        {
            _list = new List<T>(enumerable);
        }

        public void Add(T value)
        {
            _lock.EnterWriteLock();
            try
            {
                _list.Add(value);
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

        public bool Contains(T value)
        {
            _lock.EnterReadLock();
            try
            {
                return _list.Contains(value);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _list.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public void CopyTo(T[] arr, int i)
        {
            _lock.EnterReadLock();
            try
            {
                _list.CopyTo(arr, i);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _list.IsReadOnly;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public bool Remove(T value)
        {
            _lock.EnterWriteLock();
            try
            {
                return _list.Remove(value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public int IndexOf(T value)
        {
            _lock.EnterReadLock();
            try
            {
                return _list.IndexOf(value);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Insert(int i, T value)
        {
            _lock.EnterWriteLock();
            try
            {
                _list.Insert(i, value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RemoveAt(int i)
        {
            _lock.EnterWriteLock();
            try
            {
                _list.RemoveAt(i);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public T this[int i]
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _list[i];
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            set
            {
                _lock.EnterWriteLock();
                try
                {
                    _list[i] = value;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotSupportedException();
        }
    }
}