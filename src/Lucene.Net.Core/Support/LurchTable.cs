#region Copyright 2012-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

 // 2016-10-03: Modified from the original version by Shad Storhaug to 
 //             allow read-write access to the Limit property
#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Defines if and how items added to a LurchTable are linked together, this defines
    /// the value returned from Peek/Dequeue as the oldest entry of the specified operation.
    /// </summary>
    public enum LurchTableOrder
    {
        /// <summary> No linking </summary>
        None,
        /// <summary> Linked in insertion order </summary>
        Insertion,
        /// <summary> Linked by most recently inserted or updated </summary>
        Modified,
        /// <summary> Linked by most recently inserted, updated, or fetched </summary>
        Access,
    }

    /// <summary>
    /// LurchTable stands for "Least Used Recently Concurrent Hash Table" and has definate
    /// similarities to both the .NET 4 ConcurrentDictionary as well as Java's LinkedHashMap.
    /// This gives you a thread-safe dictionary/hashtable that stores element ordering by
    /// insertion, updates, or access.  In addition it can be configured to use a 'hard-limit'
    /// count of items that will automatically 'pop' the oldest item in the collection.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    public class LurchTable<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        /// <summary> Method signature for the ItemUpdated event </summary>
        public delegate void ItemUpdatedMethod(KeyValuePair<TKey, TValue> previous, KeyValuePair<TKey, TValue> next);

        /// <summary> Event raised after an item is removed from the collection </summary>
        public event Action<KeyValuePair<TKey, TValue>> ItemRemoved;
        /// <summary> Event raised after an item is updated in the collection </summary>
        public event ItemUpdatedMethod ItemUpdated;
        /// <summary> Event raised after an item is added to the collection </summary>
        public event Action<KeyValuePair<TKey, TValue>> ItemAdded;

        private const int OverAlloc = 128;
        private const int FreeSlots = 32;

        private readonly IEqualityComparer<TKey> _comparer;
        private readonly int _hsize, _lsize;
        private int _limit; // LUCENENET: Changed to read-write
        private readonly int _allocSize, _shift, _shiftMask;
        private readonly LurchTableOrder _ordering;
        private readonly object[] _locks;
        private readonly int[] _buckets;
        private readonly FreeList[] _free;

        private Entry[][] _entries;
        private int _used, _count;
        private int _allocNext, _freeVersion;

        /// <summary>Creates a LurchTable that can store up to (capacity) items efficiently.</summary>
        public LurchTable(int capacity)
            : this(LurchTableOrder.None, int.MaxValue, capacity >> 1, capacity >> 4, capacity >> 8, EqualityComparer<TKey>.Default) { }

        /// <summary>Creates a LurchTable that can store up to (capacity) items efficiently.</summary>
        public LurchTable(int capacity, LurchTableOrder ordering)
            : this(ordering, int.MaxValue, capacity >> 1, capacity >> 4, capacity >> 8, EqualityComparer<TKey>.Default) { }

        /// <summary>Creates a LurchTable that can store up to (capacity) items efficiently.</summary>
        public LurchTable(int capacity, LurchTableOrder ordering, IEqualityComparer<TKey> comparer)
            : this(ordering, int.MaxValue, capacity >> 1, capacity >> 4, capacity >> 8, comparer) { }

        /// <summary>Creates a LurchTable that orders items by (ordering) and removes items once the specified (limit) is reached.</summary>
        public LurchTable(LurchTableOrder ordering, int limit)
            : this(ordering, limit, limit >> 1, limit >> 4, limit >> 8, EqualityComparer<TKey>.Default) { }

        /// <summary>Creates a LurchTable that orders items by (ordering) and removes items once the specified (limit) is reached.</summary>
        public LurchTable(LurchTableOrder ordering, int limit, IEqualityComparer<TKey> comparer)
            : this(ordering, limit, limit >> 1, limit >> 4, limit >> 8, comparer) { }

        /// <summary>
        /// Creates a LurchTable that orders items by (ordering) and removes items once the specified (limit) is reached.
        /// </summary>
        /// <param name="ordering">The type of linking for the items</param>
        /// <param name="limit">The maximum allowable number of items, or int.MaxValue for unlimited</param>
        /// <param name="hashSize">The number of hash buckets to use for the collection, usually 1/2 estimated capacity</param>
        /// <param name="allocSize">The number of entries to allocate at a time, usually 1/16 estimated capacity</param>
        /// <param name="lockSize">The number of concurrency locks to preallocate, usually 1/256 estimated capacity</param>
        /// <param name="comparer">The element hash generator for keys</param>
        public LurchTable(LurchTableOrder ordering, int limit, int hashSize, int allocSize, int lockSize, IEqualityComparer<TKey> comparer)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException("limit");
            if (ordering == LurchTableOrder.None && limit < int.MaxValue)
                throw new ArgumentOutOfRangeException("ordering");

            _limit = limit <= 0 ? int.MaxValue : limit;
            _comparer = comparer;
            _ordering = ordering;

            allocSize = (int)Math.Min((long)allocSize + OverAlloc, 0x3fffffff);
            //last power of 2 that is less than allocSize
            for (_shift = 7; _shift < 24 && (1 << (_shift + 1)) < allocSize; _shift++) { }
            _allocSize = 1 << _shift;
            _shiftMask = _allocSize - 1;

            _hsize = HashUtilities.SelectPrimeNumber(Math.Max(127, hashSize));
            _buckets = new int[_hsize];

            _lsize = HashUtilities.SelectPrimeNumber(lockSize);
            _locks = new object[_lsize];
            for (int i = 0; i < _lsize; i++)
                _locks[i] = new object();

            _free = new FreeList[FreeSlots];
            Initialize();
        }

        #region IDisposable Members

        /// <summary>
        /// Clears references to all objects and invalidates the collection
        /// </summary>
        public void Dispose()
        {
            _entries = null;
            _used = _count = 0;
        }

        #endregion

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        public int Count { get { return _count; } }
        /// <summary>
        /// Retrieves the LurchTableOrder Ordering enumeration this instance was created with.
        /// </summary>
        public LurchTableOrder Ordering { get { return _ordering; } }
        /// <summary>
        /// Retrives the key comparer being used by this instance.
        /// </summary>
        public IEqualityComparer<TKey> Comparer { get { return _comparer; } }
        /// <summary>
        /// Gets or Sets the record limit allowed in this instance.
        /// </summary>
        public int Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// WARNING: not thread-safe, reinitializes all internal structures.  Use Clear() for a thread-safe
        /// delete all.  If you have externally provided exclusive access this method may be used to more
        /// efficiently clear the collection.
        /// </summary>
        public void Initialize()
        {
            lock (this)
            {
                _freeVersion = _allocNext = 0;
                _count = 0;
                _used = 1;

                Array.Clear(_buckets, 0, _hsize);
                _entries = new[] { new Entry[_allocSize] };
                for (int slot = 0; slot < FreeSlots; slot++)
                {
                    var index = Interlocked.CompareExchange(ref _used, _used + 1, _used);
                    if (index != slot + 1)
                        throw new LurchTableCorruptionException();

                    _free[slot].Tail = index;
                    _free[slot].Head = index;
                }

                if (_count != 0 || _used != FreeSlots + 1)
                    throw new LurchTableCorruptionException();
            }
        }

        #region IDictionary<TKey,TValue> Members

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        public void Clear()
        {
            if (_entries == null) throw new ObjectDisposedException(GetType().Name);
            foreach (var item in this)
                Remove(item.Key);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            if (_entries == null) throw new ObjectDisposedException(GetType().Name);
            TValue value;
            return TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        public TValue this[TKey key]
        {
            set
            {
                var info = new AddInfo { Value = value, CanUpdate = true };
                Insert(key, ref info);
            }
            get
            {
                TValue value;
                if (!TryGetValue(key, out value))
                    throw new ArgumentOutOfRangeException();
                return value;
            }
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <returns>
        /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.
        /// </returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            int hash = _comparer.GetHashCode(key) & int.MaxValue;
            return InternalGetValue(hash, key, out value);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            var info = new AddInfo { Value = value };
            if (InsertResult.Inserted != Insert(key, ref info))
                throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        /// <param name="key">The key of the element to remove.</param>
        public bool Remove(TKey key)
        {
            var del = new DelInfo();
            return Delete(key, ref del);
        }

        #endregion

        #region IDictionaryEx<TKey,TValue> Members

        /// <summary>
        /// Adds a key/value pair to the  <see cref="T:System.Collections.Generic.IDictionary`2"/> if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value to be added, if the key does not already exist.</param>
        public TValue GetOrAdd(TKey key, TValue value)
        {
            var info = new AddInfo { Value = value, CanUpdate = false };
            if (InsertResult.Exists == Insert(key, ref info))
                return info.Value;
            return value;
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        public bool TryAdd(TKey key, TValue value)
        {
            var info = new AddInfo { Value = value, CanUpdate = false };
            return InsertResult.Inserted == Insert(key, ref info);
        }

        /// <summary>
        /// Updates an element with the provided key to the value if it exists.
        /// </summary>
        /// <returns>Returns true if the key provided was found and updated to the value.</returns>
        /// <param name="key">The object to use as the key of the element to update.</param>
        /// <param name="value">The new value for the key if found.</param>
        public bool TryUpdate(TKey key, TValue value)
        {
            var info = new UpdateInfo { Value = value };
            return InsertResult.Updated == Insert(key, ref info);
        }

        /// <summary>
        /// Updates an element with the provided key to the value if it exists.
        /// </summary>
        /// <returns>Returns true if the key provided was found and updated to the value.</returns>
        /// <param name="key">The object to use as the key of the element to update.</param>
        /// <param name="value">The new value for the key if found.</param>
        /// <param name="comparisonValue">The value that is compared to the value of the element with key.</param>
        public bool TryUpdate(TKey key, TValue value, TValue comparisonValue)
        {
            var info = new UpdateInfo(comparisonValue) { Value = value };
            return InsertResult.Updated == Insert(key, ref info);
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        /// <param name="key">The key of the element to remove.</param>
        /// <param name="value">The value that was removed.</param>
        public bool TryRemove(TKey key, out TValue value)
        {
            var info = new DelInfo();
            if (Delete(key, ref info))
            {
                value = info.Value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        #endregion

        #region IConcurrentDictionary<TKey,TValue> Members

        /// <summary>
        /// Adds a key/value pair to the  <see cref="T:System.Collections.Generic.IDictionary`2"/> if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="fnCreate">Constructs a new value for the key.</param>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> fnCreate)
        {
            var info = new Add2Info { Create = fnCreate };
            Insert(key, ref info);
            return info.Value;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="T:System.Collections.Generic.IDictionary`2"/> if the key does not already exist, 
        /// or updates a key/value pair if the key already exists.
        /// </summary>
        public TValue AddOrUpdate(TKey key, TValue addValue, KeyValueUpdate<TKey, TValue> fnUpdate)
        {
            var info = new Add2Info(addValue) { Update = fnUpdate };
            Insert(key, ref info);
            return info.Value;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="T:System.Collections.Generic.IDictionary`2"/> if the key does not already exist, 
        /// or updates a key/value pair if the key already exists.
        /// </summary>
        /// <remarks>
        /// Adds or modifies an element with the provided key and value.  If the key does not exist in the collection,
        /// the factory method fnCreate will be called to produce the new value, if the key exists, the converter method
        /// fnUpdate will be called to create an updated value.
        /// </remarks>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> fnCreate, KeyValueUpdate<TKey, TValue> fnUpdate)
        {
            var info = new Add2Info { Create = fnCreate, Update = fnUpdate };
            Insert(key, ref info);
            return info.Value;
        }

        /// <summary>
        /// Add, update, or fetche a key/value pair from the dictionary via an implementation of the
        /// <see cref="T:CSharpTest.Net.Collections.ICreateOrUpdateValue`2"/> interface.
        /// </summary>
        public bool AddOrUpdate<T>(TKey key, ref T createOrUpdateValue) where T : ICreateOrUpdateValue<TKey, TValue>
        {
            var result = Insert(key, ref createOrUpdateValue);
            return result == InsertResult.Inserted || result == InsertResult.Updated;
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>
        /// by calling the provided factory method to construct the value if the key is not already present in the collection.
        /// </summary>
        public bool TryAdd(TKey key, Func<TKey, TValue> fnCreate)
        {
            var info = new Add2Info { Create = fnCreate };
            return InsertResult.Inserted == Insert(key, ref info);
        }

        /// <summary>
        /// Modify the value associated with the result of the provided update method
        /// as an atomic operation, Allows for reading/writing a single record within
        /// the syncronization lock.
        /// </summary>
        public bool TryUpdate(TKey key, KeyValueUpdate<TKey, TValue> fnUpdate)
        {
            var info = new Add2Info { Update = fnUpdate };
            return InsertResult.Updated == Insert(key, ref info);
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>
        /// if the fnCondition predicate is null or returns true.
        /// </summary>
        public bool TryRemove(TKey key, KeyValuePredicate<TKey, TValue> fnCondition)
        {
            var info = new DelInfo { Condition = fnCondition };
            return Delete(key, ref info);
        }

        /// <summary>
        /// Conditionally removes a key/value pair from the dictionary via an implementation of the
        /// <see cref="T:CSharpTest.Net.Collections.IRemoveValue`2"/> interface.
        /// </summary>
        public bool TryRemove<T>(TKey key, ref T removeValue) where T : IRemoveValue<TKey, TValue>
        {
            return Delete(key, ref removeValue);
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue test;
            if (TryGetValue(item.Key, out test))
                return EqualityComparer<TValue>.Default.Equals(item.Value, test);
            return false;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var item in this)
                array[arrayIndex++] = item;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            var del = new DelInfo(item.Value);
            return Delete(item.Key, ref del);
        }

        #endregion

        #region IEnumerator<KeyValuePair<TKey, TValue>>

        private bool MoveNext(ref EnumState state)
        {
            if (_entries == null) throw new ObjectDisposedException(GetType().Name);

            if (state.Current > 0)
                state.Current = state.Next;

            if (state.Current > 0)
            {
                state.Next = _entries[state.Current >> _shift][state.Current & _shiftMask].Link;
                return true;
            }

            state.Unlock();
            while (++state.Bucket < _hsize)
            {
                if (_buckets[state.Bucket] == 0)
                    continue;

                state.Lock(_locks[state.Bucket % _lsize]);

                state.Current = _buckets[state.Bucket];
                if (state.Current > 0)
                {
                    state.Next = _entries[state.Current >> _shift][state.Current & _shiftMask].Link;
                    return true;
                }

                state.Unlock();
            }

            return false;
        }

        /// <summary>
        /// Provides an enumerator that iterates through the collection.
        /// </summary>
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly LurchTable<TKey, TValue> _owner;
            private EnumState _state;

            internal Enumerator(LurchTable<TKey, TValue> owner)
            {
                _owner = owner;
                _state = new EnumState();
                _state.Init();
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                _state.Unlock();
            }

            object IEnumerator.Current { get { return Current; } }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    int index = _state.Current;
                    if (index <= 0)
                        throw new InvalidOperationException();
                    if (_owner._entries == null)
                        throw new ObjectDisposedException(GetType().Name);

                    return new KeyValuePair<TKey, TValue>
                        (
                            _owner._entries[index >> _owner._shift][index & _owner._shiftMask].Key,
                            _owner._entries[index >> _owner._shift][index & _owner._shiftMask].Value
                        );
                }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            public bool MoveNext()
            {
                return _owner.MoveNext(ref _state);
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                _state.Unlock();
                _state.Init();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public Enumerator GetEnumerator() { return new Enumerator(this); }
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator()
        { return GetEnumerator(); }
        #endregion

        #region KeyCollection
        /// <summary>
        /// Provides the collection of Keys for the LurchTable
        /// </summary>
        public class KeyCollection : ICollection<TKey>
        {
            private readonly LurchTable<TKey, TValue> _owner;

            internal KeyCollection(LurchTable<TKey, TValue> owner)
            {
                _owner = owner;
            }

            #region ICollection<TKey> Members

            /// <summary>
            /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
            /// </summary>
            public bool Contains(TKey item)
            {
                return _owner.ContainsKey(item);
            }

            /// <summary>
            /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
            /// </summary>
            public void CopyTo(TKey[] array, int arrayIndex)
            {
                foreach (var item in _owner)
                    array[arrayIndex++] = item.Key;
            }

            /// <summary>
            /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            public int Count
            {
                get { return _owner.Count; }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(_owner);
            }

            /// <summary>
            /// Provides an enumerator that iterates through the collection.
            /// </summary>
            public struct Enumerator : IEnumerator<TKey>
            {
                private readonly LurchTable<TKey, TValue> _owner;
                private EnumState _state;

                internal Enumerator(LurchTable<TKey, TValue> owner)
                {
                    _owner = owner;
                    _state = new EnumState();
                    _state.Init();
                }

                /// <summary>
                /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
                /// </summary>
                public void Dispose()
                {
                    _state.Unlock();
                }

                object IEnumerator.Current { get { return Current; } }

                /// <summary>
                /// Gets the element in the collection at the current position of the enumerator.
                /// </summary>
                public TKey Current
                {
                    get
                    {
                        int index = _state.Current;
                        if (index <= 0)
                            throw new InvalidOperationException();
                        if (_owner._entries == null)
                            throw new ObjectDisposedException(GetType().Name);
                        return _owner._entries[index >> _owner._shift][index & _owner._shiftMask].Key;
                    }
                }

                /// <summary>
                /// Advances the enumerator to the next element of the collection.
                /// </summary>
                public bool MoveNext()
                {
                    return _owner.MoveNext(ref _state);
                }

                /// <summary>
                /// Sets the enumerator to its initial position, which is before the first element in the collection.
                /// </summary>
                public void Reset()
                {
                    _state.Unlock();
                    _state.Init();
                }
            }
            [Obsolete]
            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            {
                return new Enumerator(_owner);
            }
            [Obsolete]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(_owner);
            }
            [Obsolete]
            bool ICollection<TKey>.IsReadOnly
            {
                get { return true; }
            }
            [Obsolete]
            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotSupportedException();
            }
            [Obsolete]
            void ICollection<TKey>.Clear()
            {
                throw new NotSupportedException();
            }
            [Obsolete]
            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException();
            }

            #endregion
        }

        private KeyCollection _keyCollection;
        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        public KeyCollection Keys { get { return _keyCollection ?? (_keyCollection = new KeyCollection(this)); } }
        [Obsolete]
        ICollection<TKey> IDictionary<TKey, TValue>.Keys { get { return Keys; } }
        #endregion

        #region ValueCollection
        /// <summary>
        /// Provides the collection of Values for the LurchTable
        /// </summary>
        public class ValueCollection : ICollection<TValue>
        {
            private readonly LurchTable<TKey, TValue> _owner;

            internal ValueCollection(LurchTable<TKey, TValue> owner)
            {
                _owner = owner;
            }

            #region ICollection<TValue> Members

            /// <summary>
            /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
            /// </summary>
            public bool Contains(TValue value)
            {
                var comparer = EqualityComparer<TValue>.Default;
                foreach (var item in _owner)
                {
                    if (comparer.Equals(item.Value, value))
                        return true;
                }
                return false;
            }

            /// <summary>
            /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
            /// </summary>
            public void CopyTo(TValue[] array, int arrayIndex)
            {
                foreach (var item in _owner)
                    array[arrayIndex++] = item.Value;
            }

            /// <summary>
            /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            public int Count
            {
                get { return _owner.Count; }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(_owner);
            }

            /// <summary>
            /// Provides an enumerator that iterates through the collection.
            /// </summary>
            public struct Enumerator : IEnumerator<TValue>
            {
                private readonly LurchTable<TKey, TValue> _owner;
                private EnumState _state;

                internal Enumerator(LurchTable<TKey, TValue> owner)
                {
                    _owner = owner;
                    _state = new EnumState();
                    _state.Init();
                }

                /// <summary>
                /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
                /// </summary>
                public void Dispose()
                {
                    _state.Unlock();
                }

                object IEnumerator.Current { get { return Current; } }

                /// <summary>
                /// Gets the element in the collection at the current position of the enumerator.
                /// </summary>
                public TValue Current
                {
                    get
                    {
                        int index = _state.Current;
                        if (index <= 0)
                            throw new InvalidOperationException();
                        if (_owner._entries == null)
                            throw new ObjectDisposedException(GetType().Name);
                        return _owner._entries[index >> _owner._shift][index & _owner._shiftMask].Value;
                    }
                }

                /// <summary>
                /// Advances the enumerator to the next element of the collection.
                /// </summary>
                public bool MoveNext()
                {
                    return _owner.MoveNext(ref _state);
                }

                /// <summary>
                /// Sets the enumerator to its initial position, which is before the first element in the collection.
                /// </summary>
                public void Reset()
                {
                    _state.Unlock();
                    _state.Init();
                }
            }
            [Obsolete]
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            {
                return new Enumerator(_owner);
            }
            [Obsolete]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(_owner);
            }
            [Obsolete]
            bool ICollection<TValue>.IsReadOnly
            {
                get { return true; }
            }
            [Obsolete]
            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException();
            }
            [Obsolete]
            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException();
            }
            [Obsolete]
            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            #endregion
        }

        private ValueCollection _valueCollection;
        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        public ValueCollection Values { get { return _valueCollection ?? (_valueCollection = new ValueCollection(this)); } }
        [Obsolete]
        ICollection<TValue> IDictionary<TKey, TValue>.Values { get { return Values; } }

        #endregion

        #region Peek/Dequeue

        /// <summary>
        /// Retrieves the oldest entry in the collection based on the ordering supplied to the constructor.
        /// </summary>
        /// <returns>True if the out parameter value was set.</returns>
        /// <exception cref="System.InvalidOperationException">Raised if the table is unordered</exception>
        public bool Peek(out KeyValuePair<TKey, TValue> value)
        {
            if (_ordering == LurchTableOrder.None)
                throw new InvalidOperationException();
            if (_entries == null)
                throw new ObjectDisposedException(GetType().Name);

            while (true)
            {
                int index = Interlocked.CompareExchange(ref _entries[0][0].Prev, 0, 0);
                if (index == 0)
                {
                    value = default(KeyValuePair<TKey, TValue>);
                    return false;
                }

                int hash = _entries[index >> _shift][index & _shiftMask].Hash;
                if (hash >= 0)
                {
                    int bucket = hash % _hsize;
                    lock (_locks[bucket % _lsize])
                    {
                        if (index == _entries[0][0].Prev &&
                            hash == _entries[index >> _shift][index & _shiftMask].Hash)
                        {
                            value = new KeyValuePair<TKey, TValue>(
                                _entries[index >> _shift][index & _shiftMask].Key,
                                _entries[index >> _shift][index & _shiftMask].Value
                            );
                            return true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes the oldest entry in the collection based on the ordering supplied to the constructor.
        /// If an item is not available a busy-wait loop is used to wait for for an item.
        /// </summary>
        /// <returns>The Key/Value pair removed.</returns>
        /// <exception cref="System.InvalidOperationException">Raised if the table is unordered</exception>
        public KeyValuePair<TKey, TValue> Dequeue()
        {
            if (_ordering == LurchTableOrder.None)
                throw new InvalidOperationException();
            if (_entries == null)
                throw new ObjectDisposedException(GetType().Name);

            KeyValuePair<TKey, TValue> value;
            while (!TryDequeue(out value))
            {
                while (0 == Interlocked.CompareExchange(ref _entries[0][0].Prev, 0, 0))
                    Thread.Sleep(0);
            }
            return value;
        }

        /// <summary>
        /// Removes the oldest entry in the collection based on the ordering supplied to the constructor.
        /// </summary>
        /// <returns>False if no item was available</returns>
        /// <exception cref="System.InvalidOperationException">Raised if the table is unordered</exception>
        public bool TryDequeue(out KeyValuePair<TKey, TValue> value)
        {
            return TryDequeue(null, out value);
        }

        /// <summary>
        /// Removes the oldest entry in the collection based on the ordering supplied to the constructor.
        /// </summary>
        /// <returns>False if no item was available</returns>
        /// <exception cref="System.InvalidOperationException">Raised if the table is unordered</exception>
        public bool TryDequeue(Predicate<KeyValuePair<TKey, TValue>> predicate, out KeyValuePair<TKey, TValue> value)
        {
            if (_ordering == LurchTableOrder.None)
                throw new InvalidOperationException();
            if (_entries == null)
                throw new ObjectDisposedException(GetType().Name);

            while (true)
            {
                int index = Interlocked.CompareExchange(ref _entries[0][0].Prev, 0, 0);
                if (index == 0)
                {
                    value = default(KeyValuePair<TKey, TValue>);
                    return false;
                }

                int hash = _entries[index >> _shift][index & _shiftMask].Hash;
                if (hash >= 0)
                {
                    int bucket = hash % _hsize;
                    lock (_locks[bucket % _lsize])
                    {
                        if (index == _entries[0][0].Prev &&
                            hash == _entries[index >> _shift][index & _shiftMask].Hash)
                        {
                            if (predicate != null)
                            {
                                var item = new KeyValuePair<TKey, TValue>(
                                    _entries[index >> _shift][index & _shiftMask].Key,
                                    _entries[index >> _shift][index & _shiftMask].Value
                                );
                                if (!predicate(item))
                                {
                                    value = item;
                                    return false;
                                }
                            }

                            int next = _entries[index >> _shift][index & _shiftMask].Link;
                            bool removed = false;

                            if (_buckets[bucket] == index)
                            {
                                _buckets[bucket] = next;
                                removed = true;
                            }
                            else
                            {
                                int test = _buckets[bucket];
                                while (test != 0)
                                {
                                    int cmp = _entries[test >> _shift][test & _shiftMask].Link;
                                    if (cmp == index)
                                    {
                                        _entries[test >> _shift][test & _shiftMask].Link = next;
                                        removed = true;
                                        break;
                                    }
                                    test = cmp;
                                }
                            }
                            if (!removed)
                                throw new LurchTableCorruptionException();

                            value = new KeyValuePair<TKey, TValue>(
                                _entries[index >> _shift][index & _shiftMask].Key,
                                _entries[index >> _shift][index & _shiftMask].Value
                            );
                            Interlocked.Decrement(ref _count);
                            if (_ordering != LurchTableOrder.None)
                                InternalUnlink(index);
                            FreeSlot(ref index, Interlocked.Increment(ref _freeVersion));

                            var handler = ItemRemoved;
                            if (handler != null)
                                handler(value);

                            return true;
                        }
                    }
                }
            }
        }

        #endregion

        #region Internal Implementation

        enum InsertResult { Inserted = 1, Updated = 2, Exists = 3, NotFound = 4 }

        bool InternalGetValue(int hash, TKey key, out TValue value)
        {
            if (_entries == null)
                throw new ObjectDisposedException(GetType().Name);

            int bucket = hash % _hsize;
            lock (_locks[bucket % _lsize])
            {
                int index = _buckets[bucket];
                while (index != 0)
                {
                    if (hash == _entries[index >> _shift][index & _shiftMask].Hash &&
                        _comparer.Equals(key, _entries[index >> _shift][index & _shiftMask].Key))
                    {
                        value = _entries[index >> _shift][index & _shiftMask].Value;
                        if (hash == _entries[index >> _shift][index & _shiftMask].Hash)
                        {
                            if (_ordering == LurchTableOrder.Access)
                            {
                                InternalUnlink(index);
                                InternalLink(index);
                            }
                            return true;
                        }
                    }
                    index = _entries[index >> _shift][index & _shiftMask].Link;
                }

                value = default(TValue);
                return false;
            }
        }

        InsertResult Insert<T>(TKey key, ref T value) where T : ICreateOrUpdateValue<TKey, TValue>
        {
            if (_entries == null)
                throw new ObjectDisposedException(GetType().Name);

            int hash = _comparer.GetHashCode(key) & int.MaxValue;
            int added;

            InsertResult result = InternalInsert(hash, key, out added, ref value);

            if (added > _limit && _ordering != LurchTableOrder.None)
            {
                KeyValuePair<TKey, TValue> ignore;
                TryDequeue(out ignore);
            }
            return result;
        }

        InsertResult InternalInsert<T>(int hash, TKey key, out int added, ref T value) where T : ICreateOrUpdateValue<TKey, TValue>
        {
            int bucket = hash % _hsize;
            lock (_locks[bucket % _lsize])
            {
                TValue temp;
                int index = _buckets[bucket];
                while (index != 0)
                {
                    if (hash == _entries[index >> _shift][index & _shiftMask].Hash &&
                        _comparer.Equals(key, _entries[index >> _shift][index & _shiftMask].Key))
                    {
                        temp = _entries[index >> _shift][index & _shiftMask].Value;
                        var original = temp;
                        if (value.UpdateValue(key, ref temp))
                        {
                            _entries[index >> _shift][index & _shiftMask].Value = temp;

                            if (_ordering == LurchTableOrder.Modified || _ordering == LurchTableOrder.Access)
                            {
                                InternalUnlink(index);
                                InternalLink(index);
                            }

                            var handler = ItemUpdated;
                            if (handler != null)
                                handler(new KeyValuePair<TKey, TValue>(key, original), new KeyValuePair<TKey, TValue>(key, temp));

                            added = -1;
                            return InsertResult.Updated;
                        }

                        added = -1;
                        return InsertResult.Exists;
                    }
                    index = _entries[index >> _shift][index & _shiftMask].Link;
                }
                if (value.CreateValue(key, out temp))
                {
#pragma warning disable 612,618
                    index = AllocSlot();
#pragma warning restore 612,618
                    _entries[index >> _shift][index & _shiftMask].Hash = hash;
                    _entries[index >> _shift][index & _shiftMask].Key = key;
                    _entries[index >> _shift][index & _shiftMask].Value = temp;
                    _entries[index >> _shift][index & _shiftMask].Link = _buckets[bucket];
                    _buckets[bucket] = index;

                    added = Interlocked.Increment(ref _count);
                    if (_ordering != LurchTableOrder.None)
                        InternalLink(index);

                    var handler = ItemAdded;
                    if (handler != null)
                        handler(new KeyValuePair<TKey, TValue>(key, temp));

                    return InsertResult.Inserted;
                }
            }

            added = -1;
            return InsertResult.NotFound;
        }

        bool Delete<T>(TKey key, ref T value) where T : IRemoveValue<TKey, TValue>
        {
            if (_entries == null)
                throw new ObjectDisposedException(GetType().Name);

            int hash = _comparer.GetHashCode(key) & int.MaxValue;
            int bucket = hash % _hsize;
            lock (_locks[bucket % _lsize])
            {
                int prev = 0;
                int index = _buckets[bucket];
                while (index != 0)
                {
                    if (hash == _entries[index >> _shift][index & _shiftMask].Hash &&
                        _comparer.Equals(key, _entries[index >> _shift][index & _shiftMask].Key))
                    {
                        TValue temp = _entries[index >> _shift][index & _shiftMask].Value;

                        if (value.RemoveValue(key, temp))
                        {
                            int next = _entries[index >> _shift][index & _shiftMask].Link;
                            if (prev == 0)
                                _buckets[bucket] = next;
                            else
                                _entries[prev >> _shift][prev & _shiftMask].Link = next;

                            Interlocked.Decrement(ref _count);
                            if (_ordering != LurchTableOrder.None)
                                InternalUnlink(index);
                            FreeSlot(ref index, Interlocked.Increment(ref _freeVersion));

                            var handler = ItemRemoved;
                            if (handler != null)
                                handler(new KeyValuePair<TKey, TValue>(key, temp));

                            return true;
                        }
                        return false;
                    }

                    prev = index;
                    index = _entries[index >> _shift][index & _shiftMask].Link;
                }
            }
            return false;
        }

        void InternalLink(int index)
        {
            Interlocked.Exchange(ref _entries[index >> _shift][index & _shiftMask].Prev, 0);
            Interlocked.Exchange(ref _entries[index >> _shift][index & _shiftMask].Next, ~0);
            int next = Interlocked.Exchange(ref _entries[0][0].Next, index);
            if (next < 0)
                throw new LurchTableCorruptionException();

            while (0 != Interlocked.CompareExchange(ref _entries[next >> _shift][next & _shiftMask].Prev, index, 0))
            { }

            Interlocked.Exchange(ref _entries[index >> _shift][index & _shiftMask].Next, next);
        }

        void InternalUnlink(int index)
        {
            while (true)
            {
                int cmp;
                int prev = _entries[index >> _shift][index & _shiftMask].Prev;
                while (prev >= 0 && prev != (cmp = Interlocked.CompareExchange(
                            ref _entries[index >> _shift][index & _shiftMask].Prev, ~prev, prev)))
                    prev = cmp;
                if (prev < 0)
                    throw new LurchTableCorruptionException();

                int next = _entries[index >> _shift][index & _shiftMask].Next;
                while (next >= 0 && next != (cmp = Interlocked.CompareExchange(
                            ref _entries[index >> _shift][index & _shiftMask].Next, ~next, next)))
                    next = cmp;
                if (next < 0)
                    throw new LurchTableCorruptionException();

                if ((Interlocked.CompareExchange(
                        ref _entries[prev >> _shift][prev & _shiftMask].Next, next, index) == index))
                {
                    while (Interlocked.CompareExchange(
                               ref _entries[next >> _shift][next & _shiftMask].Prev, prev, index) != index)
                    { }
                    return;
                }

                //cancel the delete markers and retry
                if (~next != Interlocked.CompareExchange(
                        ref _entries[index >> _shift][index & _shiftMask].Next, next, ~next))
                    throw new LurchTableCorruptionException();
                if (~prev != Interlocked.CompareExchange(
                        ref _entries[index >> _shift][index & _shiftMask].Prev, prev, ~prev))
                    throw new LurchTableCorruptionException();
            }
        }

        [Obsolete("Release build inlining, so we need to ignore for testing.")]
        int AllocSlot()
        {
            while (true)
            {
                int allocated = _entries.Length * _allocSize;
                var previous = _entries;

                while (_count + OverAlloc < allocated || _used < allocated)
                {
                    int next;
                    if (_count + FreeSlots < _used)
                    {
                        int freeSlotIndex = Interlocked.Increment(ref _allocNext);
                        int slot = (freeSlotIndex & int.MaxValue) % FreeSlots;
                        next = Interlocked.Exchange(ref _free[slot].Head, 0);
                        if (next != 0)
                        {
                            int nextFree = _entries[next >> _shift][next & _shiftMask].Link;
                            if (nextFree == 0)
                            {
                                Interlocked.Exchange(ref _free[slot].Head, next);
                            }
                            else
                            {
                                Interlocked.Exchange(ref _free[slot].Head, nextFree);
                                return next;
                            }
                        }
                    }

                    next = _used;
                    if (next < allocated)
                    {
                        int alloc = Interlocked.CompareExchange(ref _used, next + 1, next);
                        if (alloc == next)
                        {
                            return next;
                        }
                    }
                }

                lock (this)
                {
                    //time to grow...
                    if (ReferenceEquals(_entries, previous))
                    {
                        Entry[][] arentries = new Entry[_entries.Length + 1][];
                        _entries.CopyTo(arentries, 0);
                        arentries[arentries.Length - 1] = new Entry[_allocSize];

                        Interlocked.CompareExchange(ref _entries, arentries, previous);
                    }
                }
            }
        }

        void FreeSlot(ref int index, int ver)
        {
            _entries[index >> _shift][index & _shiftMask].Key = default(TKey);
            _entries[index >> _shift][index & _shiftMask].Value = default(TValue);
            Interlocked.Exchange(ref _entries[index >> _shift][index & _shiftMask].Link, 0);

            int slot = (ver & int.MaxValue) % FreeSlots;
            int prev = Interlocked.Exchange(ref _free[slot].Tail, index);

            if (prev <= 0 || 0 != Interlocked.CompareExchange(ref _entries[prev >> _shift][prev & _shiftMask].Link, index, 0))
            {
                throw new LurchTableCorruptionException();
            }
        }

        #endregion

        #region Internal Structures

        struct FreeList
        {
            public int Head;
            public int Tail;
        }

        struct Entry
        {
            public int Prev, Next; // insertion/access sequence ordering
            public int Link;
            public int Hash; // hash value of entry's Key
            public TKey Key; // key of entry
            public TValue Value; // value of entry
        }

        struct EnumState
        {
            private object _locked;
            public int Bucket, Current, Next;
            public void Init()
            {
                Bucket = -1;
                Current = 0;
                Next = 0;
                _locked = null;
            }

            public void Unlock()
            {
                if (_locked != null)
                {
                    Monitor.Exit(_locked);
                    _locked = null;
                }
            }

            public void Lock(object lck)
            {
                if (_locked != null)
                    Monitor.Exit(_locked);
                Monitor.Enter(_locked = lck);
            }
        }

        struct DelInfo : IRemoveValue<TKey, TValue>
        {
            public TValue Value;
            readonly bool _hasTestValue;
            readonly TValue _testValue;
            public KeyValuePredicate<TKey, TValue> Condition;

            public DelInfo(TValue expected)
            {
                Value = default(TValue);
                _testValue = expected;
                _hasTestValue = true;
                Condition = null;
            }

            public bool RemoveValue(TKey key, TValue value)
            {
                Value = value;

                if (_hasTestValue && !EqualityComparer<TValue>.Default.Equals(_testValue, value))
                    return false;
                if (Condition != null && !Condition(key, value))
                    return false;

                return true;
            }
        }

        struct AddInfo : ICreateOrUpdateValue<TKey, TValue>
        {
            public bool CanUpdate;
            public TValue Value;
            public bool CreateValue(TKey key, out TValue value)
            {
                value = Value;
                return true;
            }

            public bool UpdateValue(TKey key, ref TValue value)
            {
                if (!CanUpdate)
                {
                    Value = value;
                    return false;
                }

                value = Value;
                return true;
            }
        }

        struct Add2Info : ICreateOrUpdateValue<TKey, TValue>
        {
            readonly bool _hasAddValue;
            readonly TValue _addValue;
            public TValue Value;
            public Func<TKey, TValue> Create;
            public KeyValueUpdate<TKey, TValue> Update;

            public Add2Info(TValue addValue) : this()
            {
                _hasAddValue = true;
                _addValue = addValue;
            }

            public bool CreateValue(TKey key, out TValue value)
            {
                if (_hasAddValue)
                {
                    value = Value = _addValue;
                    return true;
                }
                if (Create != null)
                {
                    value = Value = Create(key);
                    return true;
                }
                value = Value = default(TValue);
                return false;
            }

            public bool UpdateValue(TKey key, ref TValue value)
            {
                if (Update == null)
                {
                    Value = value;
                    return false;
                }

                value = Value = Update(key, value);
                return true;
            }
        }

        struct UpdateInfo : ICreateOrUpdateValue<TKey, TValue>
        {
            public TValue Value;
            readonly bool _hasTestValue;
            readonly TValue _testValue;

            public UpdateInfo(TValue expected)
            {
                Value = default(TValue);
                _testValue = expected;
                _hasTestValue = true;
            }

            bool ICreateValue<TKey, TValue>.CreateValue(TKey key, out TValue value)
            {
                value = default(TValue);
                return false;
            }
            public bool UpdateValue(TKey key, ref TValue value)
            {
                if (_hasTestValue && !EqualityComparer<TValue>.Default.Equals(_testValue, value))
                    return false;

                value = Value;
                return true;
            }
        }
        #endregion
    }

    #region LurchTable Support

    #region Exceptions

    /// <summary>
    /// Exception class: LurchTableCorruptionException
    /// The LurchTable internal datastructure appears to be corrupted.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [System.SerializableAttribute()]
#endif
    [global::System.Diagnostics.DebuggerStepThroughAttribute()]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("CSharpTest.Net.Generators", "2.13.222.435")]
    public partial class LurchTableCorruptionException : Exception
    {
#if FEATURE_SERIALIZABLE
        /// <summary>
        /// Serialization constructor
        /// </summary>
        protected LurchTableCorruptionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
#endif
        /// <summary>
        /// Used to create this exception from an hresult and message bypassing the message formatting
        /// </summary>
        internal static System.Exception Create(int hResult, string message)
        {
            return new LurchTableCorruptionException((System.Exception)null, hResult, message);
        }
        /// <summary>
        /// Constructs the exception from an hresult and message bypassing the message formatting
        /// </summary>
        protected LurchTableCorruptionException(System.Exception innerException, int hResult, string message) : base(message, innerException)
        {
            base.HResult = hResult;
        }
        /// <summary>
        /// The LurchTable internal datastructure appears to be corrupted.
        /// </summary>
        public LurchTableCorruptionException()
            : this((System.Exception)null, -1, "The LurchTable internal datastructure appears to be corrupted.")
        {
        }
        /// <summary>
        /// The LurchTable internal datastructure appears to be corrupted.
        /// </summary>
        public LurchTableCorruptionException(System.Exception innerException)
            : this(innerException, -1, "The LurchTable internal datastructure appears to be corrupted.")
        {
        }
        /// <summary>
        /// if(condition == false) throws The LurchTable internal datastructure appears to be corrupted.
        /// </summary>
        public static void Assert(bool condition)
        {
            if (!condition) throw new LurchTableCorruptionException();
        }
    }

#endregion // Exceptions

#region Delegates

    /// <summary> Provides a delegate that performs an atomic update of a key/value pair </summary>
    public delegate TValue KeyValueUpdate<TKey, TValue>(TKey key, TValue original);

    /// <summary> Provides a delegate that performs a test on key/value pair </summary>
    public delegate bool KeyValuePredicate<TKey, TValue>(TKey key, TValue original);

#endregion // Delegates

#region Interfaces

    /// <summary>
    /// An interface to provide conditional or custom creation logic to a concurrent dictionary.
    /// </summary>
    public interface ICreateValue<TKey, TValue>
    {
        /// <summary>
        /// Called when the key was not found within the dictionary to produce a new value that can be added.
        /// Return true to continue with the insertion, or false to prevent the key/value from being inserted.
        /// </summary>
        bool CreateValue(TKey key, out TValue value);
    }
    /// <summary>
    /// An interface to provide conditional or custom update logic to a concurrent dictionary.
    /// </summary>
    public interface IUpdateValue<TKey, TValue>
    {
        /// <summary>
        /// Called when the key was found within the dictionary to produce a modified value to update the item
        /// to. Return true to continue with the update, or false to prevent the key/value from being updated.
        /// </summary>
        bool UpdateValue(TKey key, ref TValue value);
    }
    /// <summary>
    /// An interface to provide conditional or custom creation or update logic to a concurrent dictionary.
    /// </summary>
    /// <remarks>
    /// Generally implemented as a struct and passed by ref to save stack space and to retrieve the values
    /// that where inserted or updated.
    /// </remarks>
    public interface ICreateOrUpdateValue<TKey, TValue> : ICreateValue<TKey, TValue>, IUpdateValue<TKey, TValue>
    {
    }

    /// <summary>
    /// An interface to provide conditional removal of an item from a concurrent dictionary.
    /// </summary>
    /// <remarks>
    /// Generally implemented as a struct and passed by ref to save stack space and to retrieve the values
    /// that where inserted or updated.
    /// </remarks>
    public interface IRemoveValue<TKey, TValue>
    {
        /// <summary>
        /// Called when the dictionary is about to remove the key/value pair provided, return true to allow
        /// it's removal, or false to prevent it from being removed.
        /// </summary>
        bool RemoveValue(TKey key, TValue value);
    }

#endregion // interfaces

#region Classes

    internal class HashUtilities
    {
        private static readonly int[] PrimeNumbers =
            new[]
                {
                    17, 37, 67, 131, 257, 521, // ROK - Added smaller primes
                    1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
                    17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
                    187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687,
                    1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
                };

        internal static int SelectPrimeNumber(int hashSize)
        {
            int offset = Array.BinarySearch(PrimeNumbers, hashSize);
            if (offset < 0)
                offset = ~offset;
            return PrimeNumbers[Math.Min(offset, PrimeNumbers.Length - 1)];
        }
    }

#endregion // Classes

#endregion // LurchTable Support
}
