using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Support
{
    /*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

    public sealed class ConcurrentHashSet<T> : ISet<T>
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly ISet<T> _set;

        public ConcurrentHashSet()
        {
            _set = new HashSet<T>();
        }

        public ConcurrentHashSet(IEnumerable<T> collection)
        {
            _set = new HashSet<T>(collection);
        }

        public ConcurrentHashSet(IEqualityComparer<T> comparer)
        {
            _set = new HashSet<T>(comparer);
        }

        public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            _set = new HashSet<T>(collection, comparer);
        }

        public bool Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                return _set.Add(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                _set.ExceptWith(other);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                _set.IntersectWith(other);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.IsProperSubsetOf(other);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.IsProperSupersetOf(other);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.IsSubsetOf(other); ;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.IsSupersetOf(other);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.Overlaps(other);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.SetEquals(other);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                _set.SymmetricExceptWith(other);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void UnionWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                _set.UnionWith(other);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        void ICollection<T>.Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                _set.Add(item);
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
                _set.Clear();
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
                return _set.Contains(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _lock.EnterReadLock();
            try
            {
                _set.CopyTo(array, arrayIndex);
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
                    return _set.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return _set.IsReadOnly;
            }
        }

        public bool Remove(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                return _set.Remove(item);
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
                // Make a copy of the contents since enumeration is lazy and not thread-safe
                T[] array = new T[_set.Count];
                _set.CopyTo(array, 0);
                return ((IEnumerable<T>)array).GetEnumerator();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}