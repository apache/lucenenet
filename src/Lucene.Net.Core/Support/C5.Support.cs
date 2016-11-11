/*
 Copyright (c) 2003-2016 Niels Kokholm, Peter Sestoft, and Rasmus Lystrøm
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
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 SOFTWARE.
*/

using System;
using System.Linq;
using System.Reflection;
using SCG = System.Collections.Generic;

namespace Lucene.Net.Support.C5
{
    // LUCENENET NOTE: These are support types required by TreeSet{T} and 
    // TreeDictionary{K, V} (which is similar to TreeMap in Java). These were brought
    // over from the C5 library. https://github.com/sestoft/C5

    /// <summary>
    /// Base class for classes implementing ICollectionValue[T]
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class CollectionValueBase<T> : EnumerableBase<T>, ICollectionValue<T>, IShowable
    {
        #region Event handling
        EventBlock<T> eventBlock;
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public virtual EventTypeEnum ListenableEvents { get { return 0; } }

        /// <summary>
        /// A flag bitmap of the events currently subscribed to by this collection.
        /// </summary>
        /// <value></value>
        public virtual EventTypeEnum ActiveEvents { get { return eventBlock == null ? 0 : eventBlock.events; } }

        private void checkWillListen(EventTypeEnum eventType)
        {
            if ((ListenableEvents & eventType) == 0)
                throw new UnlistenableEventException();
        }

        /// <summary>
        /// The change event. Will be raised for every change operation on the collection.
        /// </summary>
        public virtual event CollectionChangedHandler<T> CollectionChanged
        {
            add { checkWillListen(EventTypeEnum.Changed); (eventBlock ?? (eventBlock = new EventBlock<T>())).CollectionChanged += value; }
            remove
            {
                checkWillListen(EventTypeEnum.Changed);
                if (eventBlock != null)
                {
                    eventBlock.CollectionChanged -= value;
                    if (eventBlock.events == 0) eventBlock = null;
                }
            }
        }
        /// <summary>
        /// Fire the CollectionChanged event
        /// </summary>
        protected virtual void raiseCollectionChanged()
        { if (eventBlock != null) eventBlock.raiseCollectionChanged(this); }

        /// <summary>
        /// The clear event. Will be raised for every Clear operation on the collection.
        /// </summary>
        public virtual event CollectionClearedHandler<T> CollectionCleared
        {
            add { checkWillListen(EventTypeEnum.Cleared); (eventBlock ?? (eventBlock = new EventBlock<T>())).CollectionCleared += value; }
            remove
            {
                checkWillListen(EventTypeEnum.Cleared);
                if (eventBlock != null)
                {
                    eventBlock.CollectionCleared -= value;
                    if (eventBlock.events == 0) eventBlock = null;
                }
            }
        }
        /// <summary>
        /// Fire the CollectionCleared event
        /// </summary>
        protected virtual void raiseCollectionCleared(bool full, int count)
        { if (eventBlock != null) eventBlock.raiseCollectionCleared(this, full, count); }

        /// <summary>
        /// Fire the CollectionCleared event
        /// </summary>
        protected virtual void raiseCollectionCleared(bool full, int count, int? offset)
        { if (eventBlock != null) eventBlock.raiseCollectionCleared(this, full, count, offset); }

        /// <summary>
        /// The item added  event. Will be raised for every individual addition to the collection.
        /// </summary>
        public virtual event ItemsAddedHandler<T> ItemsAdded
        {
            add { checkWillListen(EventTypeEnum.Added); (eventBlock ?? (eventBlock = new EventBlock<T>())).ItemsAdded += value; }
            remove
            {
                checkWillListen(EventTypeEnum.Added);
                if (eventBlock != null)
                {
                    eventBlock.ItemsAdded -= value;
                    if (eventBlock.events == 0) eventBlock = null;
                }
            }
        }
        /// <summary>
        /// Fire the ItemsAdded event
        /// </summary>
        /// <param name="item">The item that was added</param>
        /// <param name="count"></param>
        protected virtual void raiseItemsAdded(T item, int count)
        { if (eventBlock != null) eventBlock.raiseItemsAdded(this, item, count); }

        /// <summary>
        /// The item removed event. Will be raised for every individual removal from the collection.
        /// </summary>
        public virtual event ItemsRemovedHandler<T> ItemsRemoved
        {
            add { checkWillListen(EventTypeEnum.Removed); (eventBlock ?? (eventBlock = new EventBlock<T>())).ItemsRemoved += value; }
            remove
            {
                checkWillListen(EventTypeEnum.Removed);
                if (eventBlock != null)
                {
                    eventBlock.ItemsRemoved -= value;
                    if (eventBlock.events == 0) eventBlock = null;
                }
            }
        }
        /// <summary>
        /// Fire the ItemsRemoved event
        /// </summary>
        /// <param name="item">The item that was removed</param>
        /// <param name="count"></param>
        protected virtual void raiseItemsRemoved(T item, int count)
        { if (eventBlock != null) eventBlock.raiseItemsRemoved(this, item, count); }

        /// <summary>
        /// The item added  event. Will be raised for every individual addition to the collection.
        /// </summary>
        public virtual event ItemInsertedHandler<T> ItemInserted
        {
            add { checkWillListen(EventTypeEnum.Inserted); (eventBlock ?? (eventBlock = new EventBlock<T>())).ItemInserted += value; }
            remove
            {
                checkWillListen(EventTypeEnum.Inserted);
                if (eventBlock != null)
                {
                    eventBlock.ItemInserted -= value;
                    if (eventBlock.events == 0) eventBlock = null;
                }
            }
        }
        /// <summary>
        /// Fire the ItemInserted event
        /// </summary>
        /// <param name="item">The item that was added</param>
        /// <param name="index"></param>
        protected virtual void raiseItemInserted(T item, int index)
        { if (eventBlock != null) eventBlock.raiseItemInserted(this, item, index); }

        /// <summary>
        /// The item removed event. Will be raised for every individual removal from the collection.
        /// </summary>
        public virtual event ItemRemovedAtHandler<T> ItemRemovedAt
        {
            add { checkWillListen(EventTypeEnum.RemovedAt); (eventBlock ?? (eventBlock = new EventBlock<T>())).ItemRemovedAt += value; }
            remove
            {
                checkWillListen(EventTypeEnum.RemovedAt);
                if (eventBlock != null)
                {
                    eventBlock.ItemRemovedAt -= value;
                    if (eventBlock.events == 0) eventBlock = null;
                }
            }
        }
        /// <summary> 
        /// Fire the ItemRemovedAt event
        /// </summary>
        /// <param name="item">The item that was removed</param>
        /// <param name="index"></param>
        protected virtual void raiseItemRemovedAt(T item, int index)
        { if (eventBlock != null) eventBlock.raiseItemRemovedAt(this, item, index); }

        #region Event support for IList
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <param name="item"></param>
        protected virtual void raiseForSetThis(int index, T value, T item)
        {
            if (ActiveEvents != 0)
            {
                raiseItemsRemoved(item, 1);
                raiseItemRemovedAt(item, index);
                raiseItemsAdded(value, 1);
                raiseItemInserted(value, index);
                raiseCollectionChanged();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <param name="item"></param>
        protected virtual void raiseForInsert(int i, T item)
        {
            if (ActiveEvents != 0)
            {
                raiseItemInserted(item, i);
                raiseItemsAdded(item, 1);
                raiseCollectionChanged();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        protected void raiseForRemove(T item)
        {
            if (ActiveEvents != 0)
            {
                raiseItemsRemoved(item, 1);
                raiseCollectionChanged();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="count"></param>
        protected void raiseForRemove(T item, int count)
        {
            if (ActiveEvents != 0)
            {
                raiseItemsRemoved(item, count);
                raiseCollectionChanged();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        protected void raiseForRemoveAt(int index, T item)
        {
            if (ActiveEvents != 0)
            {
                raiseItemRemovedAt(item, index);
                raiseItemsRemoved(item, 1);
                raiseCollectionChanged();
            }
        }

        #endregion

        #region Event  Support for ICollection
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newitem"></param>
        /// <param name="olditem"></param>
        protected virtual void raiseForUpdate(T newitem, T olditem)
        {
            if (ActiveEvents != 0)
            {
                raiseItemsRemoved(olditem, 1);
                raiseItemsAdded(newitem, 1);
                raiseCollectionChanged();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newitem"></param>
        /// <param name="olditem"></param>
        /// <param name="count"></param>
        protected virtual void raiseForUpdate(T newitem, T olditem, int count)
        {
            if (ActiveEvents != 0)
            {
                raiseItemsRemoved(olditem, count);
                raiseItemsAdded(newitem, count);
                raiseCollectionChanged();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        protected virtual void raiseForAdd(T item)
        {
            if (ActiveEvents != 0)
            {
                raiseItemsAdded(item, 1);
                raiseCollectionChanged();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wasRemoved"></param>
        protected virtual void raiseForRemoveAll(ICollectionValue<T> wasRemoved)
        {
            if ((ActiveEvents & EventTypeEnum.Removed) != 0)
                foreach (T item in wasRemoved)
                    raiseItemsRemoved(item, 1);
            if (wasRemoved != null && wasRemoved.Count > 0)
                raiseCollectionChanged();
        }

        /// <summary>
        /// 
        /// </summary>
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        protected class RaiseForRemoveAllHandler
        {
            CollectionValueBase<T> collection;
            CircularQueue<T> wasRemoved;
            bool wasChanged = false;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="collection"></param>
            public RaiseForRemoveAllHandler(CollectionValueBase<T> collection)
            {
                this.collection = collection;
                mustFireRemoved = (collection.ActiveEvents & EventTypeEnum.Removed) != 0;
                MustFire = (collection.ActiveEvents & (EventTypeEnum.Removed | EventTypeEnum.Changed)) != 0;
            }

            bool mustFireRemoved;
            /// <summary>
            /// 
            /// </summary>
            public readonly bool MustFire;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="item"></param>
            public void Remove(T item)
            {
                if (mustFireRemoved)
                {
                    if (wasRemoved == null)
                        wasRemoved = new CircularQueue<T>();
                    wasRemoved.Enqueue(item);
                }
                if (!wasChanged)
                    wasChanged = true;
            }

            /// <summary>
            /// 
            /// </summary>
            public void Raise()
            {
                if (wasRemoved != null)
                    foreach (T item in wasRemoved)
                        collection.raiseItemsRemoved(item, 1);
                if (wasChanged)
                    collection.raiseCollectionChanged();
            }
        }
        #endregion

        #endregion

        internal MemoryType MemoryType { get; set; }

        /// <summary>
        /// Check if collection is empty.
        /// </summary>
        /// <value>True if empty</value>
        public abstract bool IsEmpty { get; }

        /// <summary>
        /// The number of items in this collection.
        /// </summary>
        /// <value></value>
        public abstract int Count { get; }

        /// <summary>
        /// The value is symbolic indicating the type of asymptotic complexity
        /// in terms of the size of this collection (worst-case or amortized as
        /// relevant).
        /// </summary>
        /// <value>A characterization of the speed of the 
        /// <code>Count</code> property in this collection.</value>
        public abstract Speed CountSpeed { get; }

        /// <summary>
        /// Copy the items of this collection to part of an array.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"> if <code>index</code> 
        /// is not a valid index
        /// into the array (i.e. negative or greater than the size of the array)
        /// or the array does not have room for the items.</exception>
        /// <param name="array">The array to copy to.</param>
        /// <param name="index">The starting index.</param>
        public virtual void CopyTo(T[] array, int index)
        {
            if (index < 0 || index + Count > array.Length)
                throw new ArgumentOutOfRangeException();

            foreach (T item in this) array[index++] = item;
        }

        /// <summary>
        /// Create an array with the items of this collection (in the same order as an
        /// enumerator would output them).
        /// </summary>
        /// <returns>The array</returns>
        public virtual T[] ToArray()
        {
            T[] res = new T[Count];
            int i = 0;

            foreach (T item in this) res[i++] = item;

            return res;
        }

        /// <summary>
        /// Apply an single argument action, <see cref="T:Action`1"/> to this enumerable
        /// </summary>
        /// <param name="action">The action delegate</param>
        public virtual void Apply(Action<T> action)
        {
            foreach (T item in this)
                action(item);
        }


        /// <summary>
        /// Check if there exists an item  that satisfies a
        /// specific predicate in this collection.
        /// </summary>
        /// <param name="predicate">A delegate 
        /// (<see cref="T:Func`2"/> with <code>R = bool</code>) 
        /// defining the predicate</param>
        /// <returns>True if such an item exists</returns>
        public virtual bool Exists(Func<T, bool> predicate)
        {
            foreach (T item in this)
                if (predicate(item))
                    return true;

            return false;
        }

        /// <summary>
        /// Check if there exists an item  that satisfies a
        /// specific predicate in this collection and return the first one in enumeration order.
        /// </summary>
        /// <param name="predicate">A delegate 
        /// (<see cref="T:Func`2"/> with <code>R == bool</code>) defining the predicate</param>
        /// <param name="item"></param>
        /// <returns>True is such an item exists</returns>
        public virtual bool Find(Func<T, bool> predicate, out T item)
        {
            foreach (T jtem in this)
                if (predicate(jtem))
                {
                    item = jtem;
                    return true;
                }
            item = default(T);
            return false;
        }

        /// <summary>
        /// Check if all items in this collection satisfies a specific predicate.
        /// </summary>
        /// <param name="predicate">A delegate 
        /// (<see cref="T:Func`2"/> with <code>R = bool</code>) 
        /// defining the predicate</param>
        /// <returns>True if all items satisfies the predicate</returns>
        public virtual bool All(Func<T, bool> predicate)
        {
            foreach (T item in this)
                if (!predicate(item))
                    return false;

            return true;
        }

        /// <summary>
        /// Create an enumerable, enumerating the items of this collection that satisfies 
        /// a certain condition.
        /// </summary>
        /// <param name="predicate">A delegate 
        /// (<see cref="T:Func`2"/> with <code>R = bool</code>) 
        /// defining the predicate</param>
        /// <returns>The filtered enumerable</returns>
        public virtual SCG.IEnumerable<T> Filter(Func<T, bool> predicate)
        {
            if (MemoryType == MemoryType.Strict) throw new Exception("This is not a memory safe function and cannot be used in MemoryType.Strict");

            foreach (T item in this)
                if (predicate(item))
                    yield return item;
        }

        /// <summary>
        /// Choose some item of this collection. 
        /// </summary>
        /// <exception cref="NoSuchItemException">if collection is empty.</exception>
        /// <returns></returns>
        public abstract T Choose();


        #region IShowable Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringbuilder"></param>
        /// <param name="rest"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public virtual bool Show(System.Text.StringBuilder stringbuilder, ref int rest, IFormatProvider formatProvider)
        {
            return Showing.ShowCollectionValue<T>(this, stringbuilder, ref rest, formatProvider);
        }
        #endregion

        #region IFormattable Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public virtual string ToString(string format, IFormatProvider formatProvider)
        {
            return Showing.ShowString(this, format, formatProvider);
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(null, null);
        }
    }


    /// <summary>
    /// A base class for implementing a dictionary based on a set collection implementation.
    /// <i>See the source code for <see cref="T:C5.HashDictionary`2"/> for an example</i>
    /// 
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class DictionaryBase<K, V> : CollectionValueBase<KeyValuePair<K, V>>, IDictionary<K, V>
    {
        /// <summary>
        /// The set collection of entries underlying this dictionary implementation
        /// </summary>
        protected ICollection<KeyValuePair<K, V>> pairs;

        SCG.IEqualityComparer<K> keyequalityComparer;

        private readonly KeysCollection _keyCollection;
        private readonly ValuesCollection _valueCollection;

        #region Events

        ProxyEventBlock<KeyValuePair<K, V>> eventBlock;

        /// <summary>
        /// The change event. Will be raised for every change operation on the collection.
        /// </summary>
        public override event CollectionChangedHandler<KeyValuePair<K, V>> CollectionChanged
        {
            add { (eventBlock ?? (eventBlock = new ProxyEventBlock<KeyValuePair<K, V>>(this, pairs))).CollectionChanged += value; }
            remove { if (eventBlock != null) eventBlock.CollectionChanged -= value; }
        }

        /// <summary>
        /// The change event. Will be raised for every change operation on the collection.
        /// </summary>
        public override event CollectionClearedHandler<KeyValuePair<K, V>> CollectionCleared
        {
            add { (eventBlock ?? (eventBlock = new ProxyEventBlock<KeyValuePair<K, V>>(this, pairs))).CollectionCleared += value; }
            remove { if (eventBlock != null) eventBlock.CollectionCleared -= value; }
        }

        /// <summary>
        /// The item added  event. Will be raised for every individual addition to the collection.
        /// </summary>
        public override event ItemsAddedHandler<KeyValuePair<K, V>> ItemsAdded
        {
            add { (eventBlock ?? (eventBlock = new ProxyEventBlock<KeyValuePair<K, V>>(this, pairs))).ItemsAdded += value; }
            remove { if (eventBlock != null) eventBlock.ItemsAdded -= value; }
        }

        /// <summary>
        /// The item added  event. Will be raised for every individual removal from the collection.
        /// </summary>
        public override event ItemsRemovedHandler<KeyValuePair<K, V>> ItemsRemoved
        {
            add { (eventBlock ?? (eventBlock = new ProxyEventBlock<KeyValuePair<K, V>>(this, pairs))).ItemsRemoved += value; }
            remove { if (eventBlock != null) eventBlock.ItemsRemoved -= value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public override EventTypeEnum ListenableEvents
        {
            get
            {
                return EventTypeEnum.Basic;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override EventTypeEnum ActiveEvents
        {
            get
            {
                return pairs.ActiveEvents;
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyequalityComparer"></param>
        /// <param name = "memoryType"></param>
        protected DictionaryBase(SCG.IEqualityComparer<K> keyequalityComparer, MemoryType memoryType)
        {
            if (keyequalityComparer == null)
                throw new NullReferenceException("Key equality comparer cannot be null");
            this.keyequalityComparer = keyequalityComparer;
            MemoryType = memoryType;

            _keyCollection = new KeysCollection(pairs, memoryType);
            _valueCollection = new ValuesCollection(pairs, memoryType);
        }

        #region IDictionary<K,V> Members

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public virtual SCG.IEqualityComparer<K> EqualityComparer { get { return keyequalityComparer; } }


        /// <summary>
        /// Add a new (key, value) pair (a mapping) to the dictionary.
        /// </summary>
        /// <exception cref="DuplicateNotAllowedException"> if there already is an entry with the same key. </exception>
        /// <param name="key">Key to add</param>
        /// <param name="value">Value to add</param>
        public virtual void Add(K key, V value)
        {
            KeyValuePair<K, V> p = new KeyValuePair<K, V>(key, value);

            if (!pairs.Add(p))
                throw new DuplicateNotAllowedException("Key being added: '" + key + "'");
        }

        /// <summary>
        /// Add the entries from a collection of <see cref="T:C5.KeyValuePair`2"/> pairs to this dictionary.
        /// <para><b>TODO: add restrictions L:K and W:V when the .Net SDK allows it </b></para>
        /// </summary>
        /// <exception cref="DuplicateNotAllowedException"> 
        /// If the input contains duplicate keys or a key already present in this dictionary.</exception>
        /// <param name="entries"></param>
        public virtual void AddAll<L, W>(SCG.IEnumerable<KeyValuePair<L, W>> entries)
            where L : K
            where W : V
        {
            foreach (KeyValuePair<L, W> pair in entries)
            {
                KeyValuePair<K, V> p = new KeyValuePair<K, V>(pair.Key, pair.Value);
                if (!pairs.Add(p))
                    throw new DuplicateNotAllowedException("Key being added: '" + pair.Key + "'");
            }
        }

        /// <summary>
        /// Remove an entry with a given key from the dictionary
        /// </summary>
        /// <param name="key">The key of the entry to remove</param>
        /// <returns>True if an entry was found (and removed)</returns>
        public virtual bool Remove(K key)
        {
            KeyValuePair<K, V> p = new KeyValuePair<K, V>(key);

            return pairs.Remove(p);
        }


        /// <summary>
        /// Remove an entry with a given key from the dictionary and report its value.
        /// </summary>
        /// <param name="key">The key of the entry to remove</param>
        /// <param name="value">On exit, the value of the removed entry</param>
        /// <returns>True if an entry was found (and removed)</returns>
        public virtual bool Remove(K key, out V value)
        {
            KeyValuePair<K, V> p = new KeyValuePair<K, V>(key);

            if (pairs.Remove(p, out p))
            {
                value = p.Value;
                return true;
            }
            else
            {
                value = default(V);
                return false;
            }
        }


        /// <summary>
        /// Remove all entries from the dictionary
        /// </summary>
        public virtual void Clear() { pairs.Clear(); }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public virtual Speed ContainsSpeed { get { return pairs.ContainsSpeed; } }

        /// <summary>
        /// Check if there is an entry with a specified key
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <returns>True if key was found</returns>
        public virtual bool Contains(K key)
        {
            KeyValuePair<K, V> p = new KeyValuePair<K, V>(key);

            return pairs.Contains(p);
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        class LiftedEnumerable<H> : SCG.IEnumerable<KeyValuePair<K, V>> where H : K
        {
            SCG.IEnumerable<H> keys;
            public LiftedEnumerable(SCG.IEnumerable<H> keys) { this.keys = keys; }
            public SCG.IEnumerator<KeyValuePair<K, V>> GetEnumerator() { foreach (H key in keys) yield return new KeyValuePair<K, V>(key); }

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public virtual bool ContainsAll<H>(SCG.IEnumerable<H> keys) where H : K
        {
            if (MemoryType == MemoryType.Strict)
                throw new Exception("The use of ContainsAll generates garbage as it still uses a non-memory safe enumerator");
            return pairs.ContainsAll(new LiftedEnumerable<H>(keys));
        }

        /// <summary>
        /// Check if there is an entry with a specified key and report the corresponding
        /// value if found. This can be seen as a safe form of "val = this[key]".
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <param name="value">On exit, the value of the entry</param>
        /// <returns>True if key was found</returns>
        public virtual bool Find(ref K key, out V value)
        {
            KeyValuePair<K, V> p = new KeyValuePair<K, V>(key);

            if (pairs.Find(ref p))
            {
                key = p.Key;
                value = p.Value;
                return true;
            }
            else
            {
                value = default(V);
                return false;
            }
        }


        /// <summary>
        /// Look for a specific key in the dictionary and if found replace the value with a new one.
        /// This can be seen as a non-adding version of "this[key] = val".
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <param name="value">The new value</param>
        /// <returns>True if key was found</returns>
        public virtual bool Update(K key, V value)
        {
            KeyValuePair<K, V> p = new KeyValuePair<K, V>(key, value);

            return pairs.Update(p);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="oldvalue"></param>
        /// <returns></returns>
        public virtual bool Update(K key, V value, out V oldvalue)
        {
            KeyValuePair<K, V> p = new KeyValuePair<K, V>(key, value);

            bool retval = pairs.Update(p, out p);
            oldvalue = p.Value;
            return retval;
        }


        /// <summary>
        /// Look for a specific key in the dictionary. If found, report the corresponding value,
        /// else add an entry with the key and the supplied value.
        /// </summary>
        /// <param name="key">On entry the key to look for</param>
        /// <param name="value">On entry the value to add if the key is not found.
        /// On exit the value found if any.</param>
        /// <returns>True if key was found</returns>
        public virtual bool FindOrAdd(K key, ref V value)
        {
            KeyValuePair<K, V> p = new KeyValuePair<K, V>(key, value);

            if (!pairs.FindOrAdd(ref p))
                return false;
            else
            {
                value = p.Value;
                //key = p.key;
                return true;
            }
        }


        /// <summary>
        /// Update value in dictionary corresponding to key if found, else add new entry.
        /// More general than "this[key] = val;" by reporting if key was found.
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <param name="value">The value to add or replace with.</param>
        /// <returns>True if entry was updated.</returns>
        public virtual bool UpdateOrAdd(K key, V value)
        {
            return pairs.UpdateOrAdd(new KeyValuePair<K, V>(key, value));
        }


        /// <summary>
        /// Update value in dictionary corresponding to key if found, else add new entry.
        /// More general than "this[key] = val;" by reporting if key was found and the old value if any.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="oldvalue"></param>
        /// <returns></returns>
        public virtual bool UpdateOrAdd(K key, V value, out V oldvalue)
        {
            KeyValuePair<K, V> p = new KeyValuePair<K, V>(key, value);
            bool retval = pairs.UpdateOrAdd(p, out p);
            oldvalue = p.Value;
            return retval;
        }



        #region Keys,Values support classes
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        internal class ValuesCollection : CollectionValueBase<V>, ICollectionValue<V>
        {
            private ICollection<KeyValuePair<K, V>> _pairs;
            private readonly ValueEnumerator _valueEnumerator;


            internal ValuesCollection(ICollection<KeyValuePair<K, V>> keyValuePairs, MemoryType memoryType)
            {
                _pairs = keyValuePairs;
                _valueEnumerator = new ValueEnumerator(keyValuePairs, memoryType);
            }


            #region Private Enumerator

#if FEATURE_SERIALIZABLE
            [Serializable]
#endif
            private class ValueEnumerator : MemorySafeEnumerator<V>
            {
                private ICollection<KeyValuePair<K, V>> _keyValuePairs;

                private SCG.IEnumerator<KeyValuePair<K, V>> _keyValuePairEnumerator;

                public ValueEnumerator(ICollection<KeyValuePair<K, V>> keyValuePairs, MemoryType memoryType)
                    : base(memoryType)
                {
                    _keyValuePairs = keyValuePairs;
                }

                internal void UpdateReference(ICollection<KeyValuePair<K, V>> keyValuePairs)
                {
                    _keyValuePairs = keyValuePairs;
                    Current = default(V);
                }

                public override bool MoveNext()
                {
                    ICollection<KeyValuePair<K, V>> list = _keyValuePairs;

                    if (_keyValuePairEnumerator == null)
                        _keyValuePairEnumerator = list.GetEnumerator();

                    if (_keyValuePairEnumerator.MoveNext())
                    {
                        var curr = _keyValuePairEnumerator.Current;
                        Current = curr.Value;
                        return true;
                    }

                    _keyValuePairEnumerator.Dispose();
                    Current = default(V);
                    return false;
                }

                public override void Reset()
                {
                    Current = default(V);
                }
                protected override MemorySafeEnumerator<V> Clone()
                {
                    var enumerator = new ValueEnumerator(_keyValuePairs, MemoryType)
                    {
                        Current = default(V)
                    };
                    return enumerator;
                }
            }

            #endregion


            public override V Choose()
            {
                return _pairs.Choose().Value;
            }

            public override SCG.IEnumerator<V> GetEnumerator()
            {
                //Updatecheck is performed by the pairs enumerator
                var enumerator = (ValueEnumerator)_valueEnumerator.GetEnumerator();
                enumerator.UpdateReference(_pairs);
                return enumerator;
            }

            public override bool IsEmpty { get { return _pairs.IsEmpty; } }

            public override int Count { get { return _pairs.Count; } }

            public override Speed CountSpeed { get { return Speed.Constant; } }

            public void Update(ICollection<KeyValuePair<K, V>> keyValuePairs)
            {
                _pairs = keyValuePairs;
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        internal class KeysCollection : CollectionValueBase<K>, ICollectionValue<K>
        {
            ICollection<KeyValuePair<K, V>> _pairs;

            private readonly KeyEnumerator _keyEnumerator;

            #region Private Enumerator

#if FEATURE_SERIALIZABLE
            [Serializable]
#endif
            private class KeyEnumerator : MemorySafeEnumerator<K>
            {
                private ICollection<KeyValuePair<K, V>> _internalList;

                private SCG.IEnumerator<KeyValuePair<K, V>> _keyValuePairEnumerator;

                public KeyEnumerator(ICollection<KeyValuePair<K, V>> list, MemoryType memoryType)
                    : base(memoryType)
                {
                    _internalList = list;
                }

                internal void UpdateReference(ICollection<KeyValuePair<K, V>> list)
                {
                    _internalList = list;
                    Current = default(K);
                }


                public override bool MoveNext()
                {
                    ICollection<KeyValuePair<K, V>> list = _internalList;

                    if (_keyValuePairEnumerator == null)
                        _keyValuePairEnumerator = list.GetEnumerator();

                    if (_keyValuePairEnumerator.MoveNext())
                    {
                        Current = _keyValuePairEnumerator.Current.Key;
                        return true;
                    }

                    _keyValuePairEnumerator.Dispose();

                    Current = default(K);
                    return false;
                }

                public override void Reset()
                {
                    Current = default(K);
                }

                protected override MemorySafeEnumerator<K> Clone()
                {
                    var enumerator = new KeyEnumerator(_internalList, MemoryType)
                    {
                        Current = default(K)
                    };
                    return enumerator;
                }
            }

            #endregion

            internal KeysCollection(ICollection<KeyValuePair<K, V>> pairs, MemoryType memoryType)
            {
                _pairs = pairs;

                _keyEnumerator = new KeyEnumerator(pairs, memoryType);
            }

            public void Update(ICollection<KeyValuePair<K, V>> pairs)
            {
                _pairs = pairs;
            }

            public override K Choose()
            {
                return _pairs.Choose().Key;
            }

            public override SCG.IEnumerator<K> GetEnumerator()
            {
                var enumerator = (KeyEnumerator)_keyEnumerator.GetEnumerator();
                enumerator.UpdateReference(_pairs);
                return enumerator;
            }

            public override bool IsEmpty { get { return _pairs.IsEmpty; } }

            public override int Count { get { return _pairs.Count; } }

            public override Speed CountSpeed { get { return _pairs.CountSpeed; } }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <value>A collection containing all the keys of the dictionary</value>
        public virtual ICollectionValue<K> Keys
        {
            get
            {
                _keyCollection.Update(pairs);
                return _keyCollection;

            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <value>A collection containing all the values of the dictionary</value>
        public virtual ICollectionValue<V> Values
        {
            get
            {
                _valueCollection.Update(pairs);
                return _valueCollection;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual Func<K, V> Func { get { return delegate (K k) { return this[k]; }; } }

        /// <summary>
        /// Indexer by key for dictionary. 
        /// <para>The get method will throw an exception if no entry is found. </para>
        /// <para>The set method behaves like <see cref="M:C5.DictionaryBase`2.UpdateOrAdd(`0,`1)"/>.</para>
        /// </summary>
        /// <exception cref="NoSuchItemException"> On get if no entry is found. </exception>
        /// <value>The value corresponding to the key</value>
        public virtual V this[K key]
        {
            get
            {
                KeyValuePair<K, V> p = new KeyValuePair<K, V>(key);

                if (pairs.Find(ref p))
                    return p.Value;
                else
                    throw new NoSuchItemException("Key '" + key.ToString() + "' not present in Dictionary");
            }
            set
            { pairs.UpdateOrAdd(new KeyValuePair<K, V>(key, value)); }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <value>True if dictionary is read  only</value>
        public virtual bool IsReadOnly { get { return pairs.IsReadOnly; } }


        /// <summary>
        /// Check the integrity of the internal data structures of this dictionary.
        /// </summary>
        /// <returns>True if check does not fail.</returns>
        public virtual bool Check() { return pairs.Check(); }

        #endregion

        #region ICollectionValue<KeyValuePair<K,V>> Members

        /// <summary>
        /// 
        /// </summary>
        /// <value>True if this collection is empty.</value>
        public override bool IsEmpty { get { return pairs.IsEmpty; } }


        /// <summary>
        /// 
        /// </summary>
        /// <value>The number of entries in the dictionary</value>
        public override int Count { get { return pairs.Count; } }

        /// <summary>
        /// 
        /// </summary>
        /// <value>The number of entries in the dictionary</value>
        public override Speed CountSpeed { get { return pairs.CountSpeed; } }

        /// <summary>
        /// Choose some entry in this Dictionary. 
        /// </summary>
        /// <exception cref="NoSuchItemException">if collection is empty.</exception>
        /// <returns></returns>
        public override KeyValuePair<K, V> Choose() { return pairs.Choose(); }

        /// <summary>
        /// Create an enumerator for the collection of entries of the dictionary
        /// </summary>
        /// <returns>The enumerator</returns>
        public override SCG.IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return pairs.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringbuilder"></param>
        /// <param name="rest"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public override bool Show(System.Text.StringBuilder stringbuilder, ref int rest, IFormatProvider formatProvider)
        {
            return Showing.ShowDictionary<K, V>(this, stringbuilder, ref rest, formatProvider);
        }
    }


    /// <summary>
    /// A base class for implementing a sorted dictionary based on a sorted set collection implementation.
    /// <i>See the source code for <see cref="T:C5.TreeDictionary`2"/> for an example</i>
    /// 
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class SortedDictionaryBase<K, V> : DictionaryBase<K, V>, ISortedDictionary<K, V>
    {
        #region Fields

        /// <summary>
        /// 
        /// </summary>
        protected ISorted<KeyValuePair<K, V>> sortedpairs;
        SCG.IComparer<K> keycomparer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keycomparer"></param>
        /// <param name="keyequalityComparer"></param>
        /// <param name="memoryType">The memory type of the enumerator used to iterate the collection.</param>
        protected SortedDictionaryBase(SCG.IComparer<K> keycomparer, SCG.IEqualityComparer<K> keyequalityComparer, MemoryType memoryType = MemoryType.Normal)
            : base(keyequalityComparer, memoryType)
        {
            this.keycomparer = keycomparer;
            MemoryType = memoryType;
        }

        #endregion

        #region ISortedDictionary<K,V> Members

        /// <summary>
        /// The key comparer used by this dictionary.
        /// </summary>
        /// <value></value>
        public SCG.IComparer<K> Comparer { get { return keycomparer; } }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        /// I should add something to return the same instance
        public new ISorted<K> Keys { get { return new SortedKeysCollection(this, sortedpairs, keycomparer, EqualityComparer, MemoryType); } }

        /// <summary>
        /// Find the entry in the dictionary whose key is the
        /// predecessor of the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="res">The predecessor, if any</param>
        /// <returns>True if key has a predecessor</returns>
        public bool TryPredecessor(K key, out KeyValuePair<K, V> res)
        {
            return sortedpairs.TryPredecessor(new KeyValuePair<K, V>(key), out res);
        }

        /// <summary>
        /// Find the entry in the dictionary whose key is the
        /// successor of the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="res">The successor, if any</param>
        /// <returns>True if the key has a successor</returns>
        public bool TrySuccessor(K key, out KeyValuePair<K, V> res)
        {
            return sortedpairs.TrySuccessor(new KeyValuePair<K, V>(key), out res);
        }

        /// <summary>
        /// Find the entry in the dictionary whose key is the
        /// weak predecessor of the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="res">The predecessor, if any</param>
        /// <returns>True if key has a weak predecessor</returns>
        public bool TryWeakPredecessor(K key, out KeyValuePair<K, V> res)
        {
            return sortedpairs.TryWeakPredecessor(new KeyValuePair<K, V>(key), out res);
        }

        /// <summary>
        /// Find the entry in the dictionary whose key is the
        /// weak successor of the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="res">The weak successor, if any</param>
        /// <returns>True if the key has a weak successor</returns>
        public bool TryWeakSuccessor(K key, out KeyValuePair<K, V> res)
        {
            return sortedpairs.TryWeakSuccessor(new KeyValuePair<K, V>(key), out res);
        }

        /// <summary>
        /// Get the entry in the dictionary whose key is the
        /// predecessor of the specified key.
        /// </summary>
        /// <exception cref="NoSuchItemException"></exception>
        /// <param name="key">The key</param>
        /// <returns>The entry</returns>
        public KeyValuePair<K, V> Predecessor(K key)
        {
            return sortedpairs.Predecessor(new KeyValuePair<K, V>(key));
        }

        /// <summary>
        /// Get the entry in the dictionary whose key is the
        /// successor of the specified key.
        /// </summary>
        /// <exception cref="NoSuchItemException"></exception>
        /// <param name="key">The key</param>
        /// <returns>The entry</returns>
        public KeyValuePair<K, V> Successor(K key)
        {
            return sortedpairs.Successor(new KeyValuePair<K, V>(key));
        }

        /// <summary>
        /// Get the entry in the dictionary whose key is the
        /// weak predecessor of the specified key.
        /// </summary>
        /// <exception cref="NoSuchItemException"></exception>
        /// <param name="key">The key</param>
        /// <returns>The entry</returns>
        public KeyValuePair<K, V> WeakPredecessor(K key)
        {
            return sortedpairs.WeakPredecessor(new KeyValuePair<K, V>(key));
        }

        /// <summary>
        /// Get the entry in the dictionary whose key is the
        /// weak successor of the specified key.
        /// </summary>
        /// <exception cref="NoSuchItemException"></exception>
        /// <param name="key">The key</param>
        /// <returns>The entry</returns>
        public KeyValuePair<K, V> WeakSuccessor(K key)
        {
            return sortedpairs.WeakSuccessor(new KeyValuePair<K, V>(key));
        }

        #endregion

        #region ISortedDictionary<K,V> Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<K, V> FindMin()
        {
            return sortedpairs.FindMin();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<K, V> DeleteMin()
        {
            return sortedpairs.DeleteMin();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<K, V> FindMax()
        {
            return sortedpairs.FindMax();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<K, V> DeleteMax()
        {
            return sortedpairs.DeleteMax();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cutter"></param>
        /// <param name="lowEntry"></param>
        /// <param name="lowIsValid"></param>
        /// <param name="highEntry"></param>
        /// <param name="highIsValid"></param>
        /// <returns></returns>
        public bool Cut(IComparable<K> cutter, out KeyValuePair<K, V> lowEntry, out bool lowIsValid, out KeyValuePair<K, V> highEntry, out bool highIsValid)
        {
            return sortedpairs.Cut(new KeyValuePairComparable(cutter), out lowEntry, out lowIsValid, out highEntry, out highIsValid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        public IDirectedEnumerable<KeyValuePair<K, V>> RangeFrom(K bot)
        {
            return sortedpairs.RangeFrom(new KeyValuePair<K, V>(bot));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        public IDirectedEnumerable<KeyValuePair<K, V>> RangeFromTo(K bot, K top)
        {
            return sortedpairs.RangeFromTo(new KeyValuePair<K, V>(bot), new KeyValuePair<K, V>(top));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        public IDirectedEnumerable<KeyValuePair<K, V>> RangeTo(K top)
        {
            return sortedpairs.RangeTo(new KeyValuePair<K, V>(top));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IDirectedCollectionValue<KeyValuePair<K, V>> RangeAll()
        {
            return sortedpairs.RangeAll();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        public void AddSorted(SCG.IEnumerable<KeyValuePair<K, V>> items)
        {
            sortedpairs.AddSorted(items);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lowKey"></param>
        public void RemoveRangeFrom(K lowKey)
        {
            sortedpairs.RemoveRangeFrom(new KeyValuePair<K, V>(lowKey));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lowKey"></param>
        /// <param name="highKey"></param>
        public void RemoveRangeFromTo(K lowKey, K highKey)
        {
            sortedpairs.RemoveRangeFromTo(new KeyValuePair<K, V>(lowKey), new KeyValuePair<K, V>(highKey));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="highKey"></param>
        public void RemoveRangeTo(K highKey)
        {
            sortedpairs.RemoveRangeTo(new KeyValuePair<K, V>(highKey));
        }

        #endregion
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
        class KeyValuePairComparable : IComparable<KeyValuePair<K, V>>
        {
            IComparable<K> cutter;

            internal KeyValuePairComparable(IComparable<K> cutter) { this.cutter = cutter; }

            public int CompareTo(KeyValuePair<K, V> other) { return cutter.CompareTo(other.Key); }

            public bool Equals(KeyValuePair<K, V> other) { return cutter.Equals(other.Key); }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        class ProjectedDirectedEnumerable : MappedDirectedEnumerable<KeyValuePair<K, V>, K>
        {
            public ProjectedDirectedEnumerable(IDirectedEnumerable<KeyValuePair<K, V>> directedpairs) : base(directedpairs) { }

            public override K Map(KeyValuePair<K, V> pair) { return pair.Key; }

        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        class ProjectedDirectedCollectionValue : MappedDirectedCollectionValue<KeyValuePair<K, V>, K>
        {
            public ProjectedDirectedCollectionValue(IDirectedCollectionValue<KeyValuePair<K, V>> directedpairs) : base(directedpairs) { }

            public override K Map(KeyValuePair<K, V> pair) { return pair.Key; }

        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        sealed class SortedKeysCollection : SequencedBase<K>, ISorted<K>
        {

            #region Private Enumerator

#if FEATURE_SERIALIZABLE
            [Serializable]
#endif
            private class KeyEnumerator : MemorySafeEnumerator<K>
            {
                private ICollection<KeyValuePair<K, V>> _internalList;

                private SCG.IEnumerator<KeyValuePair<K, V>> _internalEnumerator;



                public KeyEnumerator(ICollection<KeyValuePair<K, V>> list, MemoryType memoryType)
                    : base(memoryType)
                {
                    _internalList = list;
                }

                internal void UpdateReference(ICollection<KeyValuePair<K, V>> list)
                {
                    _internalList = list;
                    Current = default(K);
                }


                public override void Dispose()
                {
                    _internalEnumerator.Dispose();
                    _internalEnumerator = null;
                }

                public override bool MoveNext()
                {
                    ICollection<KeyValuePair<K, V>> list = _internalList;

                    if (IteratorState == -1 || IteratorState == 0) // enumerator hasn't initialized yet or it has already run
                        _internalEnumerator = list.GetEnumerator();

                    IteratorState = 1;

                    if (_internalEnumerator.MoveNext())
                    {
                        Current = _internalEnumerator.Current.Key;
                        return true;
                    }

                    IteratorState = 0;
                    return false;
                }
                public override void Reset()
                {
                    try
                    {
                        _internalEnumerator.Reset();
                    }
                    catch (Exception)
                    {
                        //swallow the exception
                    }
                    finally
                    {
                        Current = default(K);
                    }
                }


                protected override MemorySafeEnumerator<K> Clone()
                {
                    var enumerator = new KeyEnumerator(_internalList, MemoryType)
                    {
                        Current = default(K)
                    };
                    return enumerator;
                }
            }

            #endregion

            private readonly KeyEnumerator _internalEnumerator;

            ISortedDictionary<K, V> sorteddict;
            //TODO: eliminate this. Only problem is the Find method because we lack method on dictionary that also
            //      returns the actual key.
            ISorted<KeyValuePair<K, V>> sortedpairs;
            SCG.IComparer<K> comparer;

            internal SortedKeysCollection(ISortedDictionary<K, V> sorteddict, ISorted<KeyValuePair<K, V>> sortedpairs, SCG.IComparer<K> comparer, SCG.IEqualityComparer<K> itemequalityComparer, MemoryType memoryType)
                : base(itemequalityComparer, memoryType)
            {
                this.sorteddict = sorteddict;
                this.sortedpairs = sortedpairs;
                this.comparer = comparer;

                _internalEnumerator = new KeyEnumerator(sortedpairs, memoryType);
            }

            public override K Choose()
            {
                return sorteddict.Choose().Key;
            }

            public override SCG.IEnumerator<K> GetEnumerator()
            {
                _internalEnumerator.UpdateReference(sortedpairs);
                return _internalEnumerator.GetEnumerator();
                //                foreach (KeyValuePair<K, V> p in sorteddict)
                //                    yield return p.Key;
            }

            public override bool IsEmpty { get { return sorteddict.IsEmpty; } }

            public override int Count { get { return sorteddict.Count; } }

            public override Speed CountSpeed { get { return sorteddict.CountSpeed; } }

            #region ISorted<K> Members

            public K FindMin() { return sorteddict.FindMin().Key; }

            public K DeleteMin() { throw new ReadOnlyCollectionException(); }

            public K FindMax() { return sorteddict.FindMax().Key; }

            public K DeleteMax() { throw new ReadOnlyCollectionException(); }

            public SCG.IComparer<K> Comparer { get { return comparer; } }

            public bool TryPredecessor(K item, out K res)
            {
                KeyValuePair<K, V> pRes;
                bool success = sorteddict.TryPredecessor(item, out pRes);
                res = pRes.Key;
                return success;
            }

            public bool TrySuccessor(K item, out K res)
            {
                KeyValuePair<K, V> pRes;
                bool success = sorteddict.TrySuccessor(item, out pRes);
                res = pRes.Key;
                return success;
            }

            public bool TryWeakPredecessor(K item, out K res)
            {
                KeyValuePair<K, V> pRes;
                bool success = sorteddict.TryWeakPredecessor(item, out pRes);
                res = pRes.Key;
                return success;
            }

            public bool TryWeakSuccessor(K item, out K res)
            {
                KeyValuePair<K, V> pRes;
                bool success = sorteddict.TryWeakSuccessor(item, out pRes);
                res = pRes.Key;
                return success;
            }

            public K Predecessor(K item) { return sorteddict.Predecessor(item).Key; }

            public K Successor(K item) { return sorteddict.Successor(item).Key; }

            public K WeakPredecessor(K item) { return sorteddict.WeakPredecessor(item).Key; }

            public K WeakSuccessor(K item) { return sorteddict.WeakSuccessor(item).Key; }

            public bool Cut(IComparable<K> c, out K low, out bool lowIsValid, out K high, out bool highIsValid)
            {
                KeyValuePair<K, V> lowpair, highpair;
                bool retval = sorteddict.Cut(c, out lowpair, out lowIsValid, out highpair, out highIsValid);
                low = lowpair.Key;
                high = highpair.Key;
                return retval;
            }

            public IDirectedEnumerable<K> RangeFrom(K bot)
            {
                return new ProjectedDirectedEnumerable(sorteddict.RangeFrom(bot));
            }

            public IDirectedEnumerable<K> RangeFromTo(K bot, K top)
            {
                return new ProjectedDirectedEnumerable(sorteddict.RangeFromTo(bot, top));
            }

            public IDirectedEnumerable<K> RangeTo(K top)
            {
                return new ProjectedDirectedEnumerable(sorteddict.RangeTo(top));
            }

            public IDirectedCollectionValue<K> RangeAll()
            {
                return new ProjectedDirectedCollectionValue(sorteddict.RangeAll());
            }

            public void AddSorted(SCG.IEnumerable<K> items) { throw new ReadOnlyCollectionException(); }

            public void RemoveRangeFrom(K low) { throw new ReadOnlyCollectionException(); }

            public void RemoveRangeFromTo(K low, K hi) { throw new ReadOnlyCollectionException(); }

            public void RemoveRangeTo(K hi) { throw new ReadOnlyCollectionException(); }
            #endregion

            #region ICollection<K> Members
            public Speed ContainsSpeed { get { return sorteddict.ContainsSpeed; } }

            public bool Contains(K key) { return sorteddict.Contains(key); ; }

            public int ContainsCount(K item) { return sorteddict.Contains(item) ? 1 : 0; }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public ICollectionValue<K> UniqueItems()
            {
                return this;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public ICollectionValue<KeyValuePair<K, int>> ItemMultiplicities()
            {
                return new MultiplicityOne<K>(this);
            }


            public bool ContainsAll(SCG.IEnumerable<K> items)
            {
                //TODO: optimize?
                foreach (K item in items)
                    if (!sorteddict.Contains(item))
                        return false;
                return true;
            }

            public bool Find(ref K item)
            {
                KeyValuePair<K, V> p = new KeyValuePair<K, V>(item);
                bool retval = sortedpairs.Find(ref p);
                item = p.Key;
                return retval;
            }

            public bool FindOrAdd(ref K item) { throw new ReadOnlyCollectionException(); }

            public bool Update(K item) { throw new ReadOnlyCollectionException(); }

            public bool Update(K item, out K olditem) { throw new ReadOnlyCollectionException(); }

            public bool UpdateOrAdd(K item) { throw new ReadOnlyCollectionException(); }

            public bool UpdateOrAdd(K item, out K olditem) { throw new ReadOnlyCollectionException(); }

            public bool Remove(K item) { throw new ReadOnlyCollectionException(); }

            public bool Remove(K item, out K removeditem) { throw new ReadOnlyCollectionException(); }

            public void RemoveAllCopies(K item) { throw new ReadOnlyCollectionException(); }

            public void RemoveAll(SCG.IEnumerable<K> items) { throw new ReadOnlyCollectionException(); }

            public void Clear() { throw new ReadOnlyCollectionException(); }

            public void RetainAll(SCG.IEnumerable<K> items) { throw new ReadOnlyCollectionException(); }

            #endregion

            #region IExtensible<K> Members
            public override bool IsReadOnly { get { return true; } }

            public bool AllowsDuplicates { get { return false; } }

            public bool DuplicatesByCounting { get { return true; } }

            public bool Add(K item) { throw new ReadOnlyCollectionException(); }

            void SCG.ICollection<K>.Add(K item) { throw new ReadOnlyCollectionException(); }

            public void AddAll(System.Collections.Generic.IEnumerable<K> items) { throw new ReadOnlyCollectionException(); }

            public bool Check() { return sorteddict.Check(); }

            #endregion

            #region IDirectedCollectionValue<K> Members

            public override IDirectedCollectionValue<K> Backwards()
            {
                return RangeAll().Backwards();
            }

            #endregion

            #region IDirectedEnumerable<K> Members

            IDirectedEnumerable<K> IDirectedEnumerable<K>.Backwards() { return Backwards(); }
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringbuilder"></param>
        /// <param name="rest"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public override bool Show(System.Text.StringBuilder stringbuilder, ref int rest, IFormatProvider formatProvider)
        {
            return Showing.ShowDictionary<K, V>(this, stringbuilder, ref rest, formatProvider);
        }

    }

    /// <summary>
    /// Base class (abstract) for sequenced collection implementations.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class SequencedBase<T> : DirectedCollectionBase<T>, IDirectedCollectionValue<T>
    {
        #region Fields

        int iSequencedHashCode, iSequencedHashCodeStamp = -1;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemequalityComparer"></param>
        /// <param name = "memoryType">The type of memory for the enumerator used to iterate the collection</param>
        protected SequencedBase(SCG.IEqualityComparer<T> itemequalityComparer, MemoryType memoryType) : base(itemequalityComparer, memoryType) { }

        #region Util

        //TODO: make random for release
        const int HASHFACTOR = 31;

        /// <summary>
        /// Compute the unsequenced hash code of a collection
        /// </summary>
        /// <param name="items">The collection to compute hash code for</param>
        /// <param name="itemequalityComparer">The item equalitySCG.Comparer</param>
        /// <returns>The hash code</returns>
        public static int ComputeHashCode(ISequenced<T> items, SCG.IEqualityComparer<T> itemequalityComparer)
        {
            //NOTE: It must be possible to devise a much stronger combined hashcode, 
            //but unfortunately, it has to be universal. OR we could use a (strong)
            //family and initialise its parameter randomly at load time of this class!
            //(We would not want to have yet a flag to check for invalidation?!)
            //NBNBNB: the current hashcode has the very bad property that items with hashcode 0
            // is ignored.
            int iIndexedHashCode = 0;

            foreach (T item in items)
                iIndexedHashCode = iIndexedHashCode * HASHFACTOR + itemequalityComparer.GetHashCode(item);

            return iIndexedHashCode;
        }


        /// <summary>
        /// Examine if tit and tat are equal as sequenced collections
        /// using the specified item equalityComparer (assumed compatible with the two collections).
        /// </summary>
        /// <param name="collection1">The first collection</param>
        /// <param name="collection2">The second collection</param>
        /// <param name="itemequalityComparer">The item equalityComparer to use for comparison</param>
        /// <returns>True if equal</returns>
        public static bool StaticEquals(ISequenced<T> collection1, ISequenced<T> collection2, SCG.IEqualityComparer<T> itemequalityComparer)
        {
            if (ReferenceEquals(collection1, collection2))
                return true;

            if (collection1.Count != collection2.Count)
                return false;

            //This way we might run through both enumerations twice, but
            //probably not (if the hash codes are good)
            if (collection1.GetSequencedHashCode() != collection2.GetSequencedHashCode())
                return false;

            using (SCG.IEnumerator<T> dat = collection2.GetEnumerator(), dit = collection1.GetEnumerator())
            {
                while (dit.MoveNext())
                {
                    dat.MoveNext();
                    if (!itemequalityComparer.Equals(dit.Current, dat.Current))
                        return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Get the sequenced collection hash code of this collection: from the cached 
        /// value if present and up to date, else (re)compute.
        /// </summary>
        /// <returns>The hash code</returns>
        public virtual int GetSequencedHashCode()
        {
            if (iSequencedHashCodeStamp == stamp)
                return iSequencedHashCode;

            iSequencedHashCode = ComputeHashCode((ISequenced<T>)this, itemequalityComparer);
            iSequencedHashCodeStamp = stamp;
            return iSequencedHashCode;
        }


        /// <summary>
        /// Check if the contents of that is equal to the contents of this
        /// in the sequenced sense. Using the item equalityComparer of this collection.
        /// </summary>
        /// <param name="otherCollection">The collection to compare to.</param>
        /// <returns>True if  equal</returns>
        public virtual bool SequencedEquals(ISequenced<T> otherCollection)
        {
            return StaticEquals((ISequenced<T>)this, otherCollection, itemequalityComparer);
        }


        #endregion

        /// <summary>
        /// <code>Forwards</code> if same, else <code>Backwards</code>
        /// </summary>
        /// <value>The enumeration direction relative to the original collection.</value>
        public override EnumerationDirection Direction { get { return EnumerationDirection.Forwards; } }

        /// <summary>
        /// Check if there exists an item  that satisfies a
        /// specific predicate in this collection and return the index of the first one.
        /// </summary>
        /// <param name="predicate">A delegate defining the predicate</param>
        /// <returns>the index, if found, a negative value else</returns>
        public int FindIndex(Func<T, bool> predicate)
        {
            int index = 0;
            foreach (T item in this)
            {
                if (predicate(item))
                    return index;
                index++;
            }
            return -1;
        }

        /// <summary>
        /// Check if there exists an item  that satisfies a
        /// specific predicate in this collection and return the index of the last one.
        /// </summary>
        /// <param name="predicate">A delegate defining the predicate</param>
        /// <returns>the index, if found, a negative value else</returns>
        public int FindLastIndex(Func<T, bool> predicate)
        {
            int index = Count - 1;
            foreach (T item in Backwards())
            {
                if (predicate(item))
                    return index;
                index--;
            }
            return -1;
        }

    }

    /// <summary>
    /// Base class (abstract) for ICollection implementations.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class CollectionBase<T> : CollectionValueBase<T>
    {
        #region Fields

        /// <summary>
        /// The underlying field of the ReadOnly property
        /// </summary>
        protected bool isReadOnlyBase = false;

        /// <summary>
        /// The current stamp value
        /// </summary>
        protected int stamp { get; set; }

        /// <summary>
        /// The number of items in the collection
        /// </summary>
        protected int size;

        /// <summary>
        /// The item equalityComparer of the collection
        /// </summary>
        protected readonly SCG.IEqualityComparer<T> itemequalityComparer;

        int iUnSequencedHashCode, iUnSequencedHashCodeStamp = -1;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemequalityComparer"></param>
        /// <param name = "memoryType">The type of memory for the enumerator used to iterate the collection</param>
        protected CollectionBase(SCG.IEqualityComparer<T> itemequalityComparer, MemoryType memoryType)
        {
            if (itemequalityComparer == null)
                throw new NullReferenceException("Item EqualityComparer cannot be null.");
            this.itemequalityComparer = itemequalityComparer;

            MemoryType = memoryType;
        }

        #region Util

        /// <summary>
        /// Utility method for range checking.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"> if the start or count is negative or
        ///  if the range does not fit within collection size.</exception>
        /// <param name="start">start of range</param>
        /// <param name="count">size of range</param>
        protected void checkRange(int start, int count)
        {
            if (start < 0 || count < 0 || start + count > size)
                throw new ArgumentOutOfRangeException();
        }


        /// <summary>
        /// Compute the unsequenced hash code of a collection
        /// </summary>
        /// <param name="items">The collection to compute hash code for</param>
        /// <param name="itemequalityComparer">The item equalitySCG.Comparer</param>
        /// <returns>The hash code</returns>
        public static int ComputeHashCode(ICollectionValue<T> items, SCG.IEqualityComparer<T> itemequalityComparer)
        {
            int h = 0;

            //But still heuristic: 
            //Note: the three odd factors should really be random, 
            //but there will be a problem with serialization/deserialization!
            //Two products is too few
            foreach (T item in items)
            {
                uint h1 = (uint)itemequalityComparer.GetHashCode(item);

                h += (int)((h1 * 1529784657 + 1) ^ (h1 * 2912831877) ^ (h1 * 1118771817 + 2));
            }

            return h;
            /*
                  The pairs (-1657792980, -1570288808) and (1862883298, -272461342) gives the same
                  unsequenced hashcode with this hashfunction. The pair was found with code like

                  HashDictionary<int, int[]> set = new HashDictionary<int, int[]>();
                  Random rnd = new C5Random(12345);
                  while (true)
                  {
                      int[] a = new int[2];
                      a[0] = rnd.Next(); a[1] = rnd.Next();
                      int h = unsequencedhashcode(a);
                      int[] b = a;
                      if (set.FindOrAdd(h, ref b))
                      {
                          Logger.Log(string.Format("Code {5}, Pair ({1},{2}) number {0} matched other pair ({3},{4})", set.Count, a[0], a[1], b[0], b[1], h));
                      }
                  }
                  */

        }

        static Type isortedtype = typeof(ISorted<T>);

        /// <summary>
        /// Examine if collection1 and collection2 are equal as unsequenced collections
        /// using the specified item equalityComparer (assumed compatible with the two collections).
        /// </summary>
        /// <param name="collection1">The first collection</param>
        /// <param name="collection2">The second collection</param>
        /// <param name="itemequalityComparer">The item equalityComparer to use for comparison</param>
        /// <returns>True if equal</returns>
        public static bool StaticEquals(ICollection<T> collection1, ICollection<T> collection2, SCG.IEqualityComparer<T> itemequalityComparer)
        {
            if (ReferenceEquals(collection1, collection2))
                return true;

            // bug20070227:
            if (collection1 == null || collection2 == null)
                return false;

            if (collection1.Count != collection2.Count)
                return false;

            //This way we might run through both enumerations twice, but
            //probably not (if the hash codes are good)
            //TODO: check equal equalityComparers, at least here!
            if (collection1.GetUnsequencedHashCode() != collection2.GetUnsequencedHashCode())
                return false;

            //TODO: move this to the sorted implementation classes? 
            //Really depends on speed of InstanceOfType: we could save a cast
            {
                ISorted<T> stit, stat;
                if ((stit = collection1 as ISorted<T>) != null && (stat = collection2 as ISorted<T>) != null && stit.Comparer == stat.Comparer)
                {
                    using (SCG.IEnumerator<T> dat = collection2.GetEnumerator(), dit = collection1.GetEnumerator())
                    {
                        while (dit.MoveNext())
                        {
                            dat.MoveNext();
                            if (!itemequalityComparer.Equals(dit.Current, dat.Current))
                                return false;
                        }
                        return true;
                    }
                }
            }

            if (!collection1.AllowsDuplicates && (collection2.AllowsDuplicates || collection2.ContainsSpeed >= collection1.ContainsSpeed))
            {
                foreach (T x in collection1) if (!collection2.Contains(x)) return false;
            }
            else if (!collection2.AllowsDuplicates)
            {
                foreach (T x in collection2) if (!collection1.Contains(x)) return false;
            }
            // Now tit.AllowsDuplicates && tat.AllowsDuplicates
            else if (collection1.DuplicatesByCounting && collection2.DuplicatesByCounting)
            {
                foreach (T item in collection2) if (collection1.ContainsCount(item) != collection2.ContainsCount(item)) return false;
            }
            else
            {
                // To avoid an O(n^2) algorithm, we make an aux hashtable to hold the count of items
                // bug20101103: HashDictionary<T, int> dict = new HashDictionary<T, int>();
                HashDictionary<T, int> dict = new HashDictionary<T, int>(itemequalityComparer);
                foreach (T item in collection2)
                {
                    int count = 1;
                    if (dict.FindOrAdd(item, ref count))
                        dict[item] = count + 1;
                }
                foreach (T item in collection1)
                {
                    var i = item;
                    int count;
                    if (dict.Find(ref i, out count) && count > 0)
                        dict[item] = count - 1;
                    else
                        return false;
                }
                return true;
            }

            return true;
        }


        /// <summary>
        /// Get the unsequenced collection hash code of this collection: from the cached 
        /// value if present and up to date, else (re)compute.
        /// </summary>
        /// <returns>The hash code</returns>
        public virtual int GetUnsequencedHashCode()
        {
            if (iUnSequencedHashCodeStamp == stamp)
                return iUnSequencedHashCode;

            iUnSequencedHashCode = ComputeHashCode(this, itemequalityComparer);
            iUnSequencedHashCodeStamp = stamp;
            return iUnSequencedHashCode;
        }


        /// <summary>
        /// Check if the contents of otherCollection is equal to the contents of this
        /// in the unsequenced sense.  Uses the item equality comparer of this collection
        /// </summary>
        /// <param name="otherCollection">The collection to compare to.</param>
        /// <returns>True if  equal</returns>
        public virtual bool UnsequencedEquals(ICollection<T> otherCollection)
        {
            return otherCollection != null && StaticEquals((ICollection<T>)this, otherCollection, itemequalityComparer);
        }


        /// <summary>
        /// Check if the collection has been modified since a specified time, expressed as a stamp value.
        /// </summary>
        /// <exception cref="CollectionModifiedException"> if this collection has been updated 
        /// since a target time</exception>
        /// <param name="thestamp">The stamp identifying the target time</param>
        protected virtual void modifycheck(int thestamp)
        {
            if (stamp != thestamp)
                throw new CollectionModifiedException();
        }


        /// <summary>
        /// Check if it is valid to perform update operations, and if so increment stamp.
        /// </summary>
        /// <exception cref="ReadOnlyCollectionException">If collection is read-only</exception>
        protected virtual void updatecheck()
        {
            if (isReadOnlyBase)
                throw new ReadOnlyCollectionException();

            stamp++;
        }

        #endregion

        #region ICollection<T> members

        /// <summary>
        /// 
        /// </summary>
        /// <value>True if this collection is read only</value>
        public virtual bool IsReadOnly { get { return isReadOnlyBase; } }

        #endregion

        #region ICollectionValue<T> members
        /// <summary>
        /// 
        /// </summary>
        /// <value>The size of this collection</value>
        public override int Count { get { return size; } }

        /// <summary>
        /// The value is symbolic indicating the type of asymptotic complexity
        /// in terms of the size of this collection (worst-case or amortized as
        /// relevant).
        /// </summary>
        /// <value>A characterization of the speed of the 
        /// <code>Count</code> property in this collection.</value>
        public override Speed CountSpeed { get { return Speed.Constant; } }


        #endregion

        #region IExtensible<T> members

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public virtual SCG.IEqualityComparer<T> EqualityComparer { get { return itemequalityComparer; } }

        /// <summary>
        /// 
        /// </summary>
        /// <value>True if this collection is empty</value>
        public override bool IsEmpty { get { return size == 0; } }

        #endregion

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class DirectedCollectionBase<T> : CollectionBase<T>, IDirectedCollectionValue<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemequalityComparer"></param>
        /// <param name = "memoryType">The type of memory for the enumerator used to iterate the collection</param>
        protected DirectedCollectionBase(SCG.IEqualityComparer<T> itemequalityComparer, MemoryType memoryType) : base(itemequalityComparer, memoryType) { }
        /// <summary>
        /// <code>Forwards</code> if same, else <code>Backwards</code>
        /// </summary>
        /// <value>The enumeration direction relative to the original collection.</value>
        public virtual EnumerationDirection Direction { get { return EnumerationDirection.Forwards; } }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract IDirectedCollectionValue<T> Backwards();

        IDirectedEnumerable<T> IDirectedEnumerable<T>.Backwards() { return Backwards(); }

        /// <summary>
        /// Check if there exists an item  that satisfies a
        /// specific predicate in this collection and return the first one in enumeration order.
        /// </summary>
        /// <param name="predicate">A delegate 
        /// (<see cref="T:Func`2"/> with <code>R == bool</code>) defining the predicate</param>
        /// <param name="item"></param>
        /// <returns>True is such an item exists</returns>
        public virtual bool FindLast(Func<T, bool> predicate, out T item)
        {
            foreach (T jtem in Backwards())
                if (predicate(jtem))
                {
                    item = jtem;
                    return true;
                }
            item = default(T);
            return false;
        }
    }

    /// <summary>
    /// A generic dictionary class based on a hash set class <see cref="T:C5.HashSet`1"/>. 
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class HashDictionary<K, V> : DictionaryBase<K, V>, IDictionary<K, V>
    {
        /// <summary>
        /// Create a hash dictionary using a default equalityComparer for the keys.
        /// Initial capacity of internal table will be 16 entries and threshold for 
        /// expansion is 66% fill.
        /// </summary>
        public HashDictionary(MemoryType memoryType = MemoryType.Normal) : this(EqualityComparer<K>.Default, memoryType) { }

        /// <summary>
        /// Create a hash dictionary using a custom equalityComparer for the keys.
        /// Initial capacity of internal table will be 16 entries and threshold for 
        /// expansion is 66% fill.
        /// </summary>
        /// <param name="keyequalityComparer">The external key equalitySCG.Comparer</param>
        /// <param name="memoryType">The memory type of the enumerator used to iterate the collection</param>
        public HashDictionary(SCG.IEqualityComparer<K> keyequalityComparer, MemoryType memoryType = MemoryType.Normal)
            : base(keyequalityComparer, memoryType)
        {
            pairs = new HashSet<KeyValuePair<K, V>>(new KeyValuePairEqualityComparer<K, V>(keyequalityComparer), memoryType);
        }

        /// <summary>
        /// Create a hash dictionary using a custom equalityComparer and prescribing the 
        /// initial size of the dictionary and a non-default threshold for internal table expansion.
        /// </summary>
        /// <param name="capacity">The initial capacity. Will be rounded upwards to nearest
        /// power of 2, at least 16.</param>
        /// <param name="fill">The expansion threshold. Must be between 10% and 90%.</param>
        /// <param name="keyequalityComparer">The external key equalitySCG.Comparer</param>
        /// <param name="memoryType">The memory type of the enumerator used to iterate the collection</param>
        public HashDictionary(int capacity, double fill, SCG.IEqualityComparer<K> keyequalityComparer, MemoryType memoryType = MemoryType.Normal)
            : base(keyequalityComparer, memoryType)
        {
            pairs = new HashSet<KeyValuePair<K, V>>(capacity, fill, new KeyValuePairEqualityComparer<K, V>(keyequalityComparer), memoryType);
        }
    }

    /// <summary>
    /// A set collection class based on linear hashing
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class HashSet<T> : CollectionBase<T>, ICollection<T>
    {
        #region Feature
        /// <summary>
        /// Enum class to assist printing of compilation alternatives.
        /// </summary>
        [Flags]
        public enum Feature : short
        {
            /// <summary>
            /// Nothing
            /// </summary>
            Dummy = 0,
            /// <summary>
            /// Buckets are of reference type
            /// </summary>
            RefTypeBucket = 1,
            /// <summary>
            /// Primary buckets are of value type
            /// </summary>
            ValueTypeBucket = 2,
            /// <summary>
            /// Using linear probing to resolve index clashes
            /// </summary>
            LinearProbing = 4,
            /// <summary>
            /// Shrink table when very sparsely filled
            /// </summary>
            ShrinkTable = 8,
            /// <summary>
            /// Use chaining to resolve index clashes
            /// </summary>
            Chaining = 16,
            /// <summary>
            /// Use hash function on item hash code
            /// </summary>
            InterHashing = 32,
            /// <summary>
            /// Use a universal family of hash functions on item hash code
            /// </summary>
            RandomInterHashing = 64
        }


        private static Feature features = Feature.Dummy
                                          | Feature.RefTypeBucket
                                          | Feature.Chaining
                                          | Feature.RandomInterHashing;


        /// <summary>
        /// Show which implementation features was chosen at compilation time
        /// </summary>
        public static Feature Features { get { return features; } }

        #endregion

        #region Fields

        int indexmask, bits, bitsc, origbits, lastchosen; //bitsc==32-bits; indexmask==(1<<bits)-1;

        Bucket[] table;

        double fillfactor = 0.66;

        int resizethreshhold;

        private static readonly Random Random = new Random();

        private readonly HashEnumerator _hashEnumerator;
        uint _randomhashfactor;

        #endregion

        #region Events

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public override EventTypeEnum ListenableEvents { get { return EventTypeEnum.Basic; } }

        #endregion

        #region Bucket nested class(es)
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        class Bucket
        {
            internal T item;

            internal int hashval; //Cache!

            internal Bucket overflow;

            internal Bucket(T item, int hashval, Bucket overflow)
            {
                this.item = item;
                this.hashval = hashval;
                this.overflow = overflow;
            }
        }

        #endregion

        #region Basic Util

        bool equals(T i1, T i2) { return itemequalityComparer.Equals(i1, i2); }

        int gethashcode(T item) { return itemequalityComparer.GetHashCode(item); }


        int hv2i(int hashval)
        {
            return (int)(((uint)hashval * _randomhashfactor) >> bitsc);
        }


        void expand()
        {
            Logger.Log(string.Format(string.Format("Expand to {0} bits", bits + 1)));
            resize(bits + 1);
        }


        void shrink()
        {
            if (bits > 3)
            {
                Logger.Log(string.Format(string.Format("Shrink to {0} bits", bits - 1)));
                resize(bits - 1);
            }
        }


        void resize(int bits)
        {
            Logger.Log(string.Format(string.Format("Resize to {0} bits", bits)));
            this.bits = bits;
            bitsc = 32 - bits;
            indexmask = (1 << bits) - 1;

            Bucket[] newtable = new Bucket[indexmask + 1];

            for (int i = 0, s = table.Length; i < s; i++)
            {
                Bucket b = table[i];

                while (b != null)
                {
                    int j = hv2i(b.hashval);

                    newtable[j] = new Bucket(b.item, b.hashval, newtable[j]);
                    b = b.overflow;
                }

            }

            table = newtable;
            resizethreshhold = (int)(table.Length * fillfactor);
            Logger.Log(string.Format(string.Format("Resize to {0} bits done", bits)));
        }

        /// <summary>
        /// Search for an item equal (according to itemequalityComparer) to the supplied item.  
        /// </summary>
        /// <param name="item"></param>
        /// <param name="add">If true, add item to table if not found.</param>
        /// <param name="update">If true, update table entry if item found.</param>
        /// <param name="raise">If true raise events</param>
        /// <returns>True if found</returns>
        private bool searchoradd(ref T item, bool add, bool update, bool raise)
        {

            int hashval = gethashcode(item);
            int i = hv2i(hashval);
            Bucket b = table[i], bold = null;

            if (b != null)
            {
                while (b != null)
                {
                    T olditem = b.item;
                    if (equals(olditem, item))
                    {
                        if (update)
                        {
                            b.item = item;
                        }
                        if (raise && update)
                            raiseForUpdate(item, olditem);
                        // bug20071112:
                        item = olditem;
                        return true;
                    }

                    bold = b;
                    b = b.overflow;
                }

                if (!add) goto notfound;

                bold.overflow = new Bucket(item, hashval, null);
            }
            else
            {
                if (!add) goto notfound;

                table[i] = new Bucket(item, hashval, null);
            }
            size++;
            if (size > resizethreshhold)
                expand();
            notfound:
            if (raise && add)
                raiseForAdd(item);
            if (update)
                item = default(T);
            return false;
        }


        private bool remove(ref T item)
        {

            if (size == 0)
                return false;
            int hashval = gethashcode(item);
            int index = hv2i(hashval);
            Bucket b = table[index], bold;

            if (b == null)
                return false;

            if (equals(item, b.item))
            {
                //ref
                item = b.item;
                table[index] = b.overflow;
            }
            else
            {
                bold = b;
                b = b.overflow;
                while (b != null && !equals(item, b.item))
                {
                    bold = b;
                    b = b.overflow;
                }

                if (b == null)
                    return false;

                //ref
                item = b.item;
                bold.overflow = b.overflow;
            }
            size--;

            return true;
        }


        private void clear()
        {
            bits = origbits;
            bitsc = 32 - bits;
            indexmask = (1 << bits) - 1;
            size = 0;
            table = new Bucket[indexmask + 1];
            resizethreshhold = (int)(table.Length * fillfactor);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public HashSet()
            : this(MemoryType.Normal)
        {

        }

        /// <summary>
        /// Create a hash set with natural item equalityComparer and default fill threshold (66%)
        /// and initial table size (16).
        /// </summary>
        public HashSet(MemoryType memoryType = MemoryType.Normal)
            : this(EqualityComparer<T>.Default, memoryType) { }


        /// <summary>
        /// Create a hash set with external item equalityComparer and default fill threshold (66%)
        /// and initial table size (16).
        /// </summary>
        /// <param name="itemequalityComparer">The external item equalitySCG.Comparer</param>
        /// <param name="memoryType"></param>
        public HashSet(SCG.IEqualityComparer<T> itemequalityComparer, MemoryType memoryType = MemoryType.Normal)
            : this(16, itemequalityComparer, memoryType) { }


        /// <summary>
        /// Create a hash set with external item equalityComparer and default fill threshold (66%)
        /// </summary>
        /// <param name="capacity">Initial table size (rounded to power of 2, at least 16)</param>
        /// <param name="itemequalityComparer">The external item equalitySCG.Comparer</param>
        /// <param name="memoryType"></param>
        public HashSet(int capacity, SCG.IEqualityComparer<T> itemequalityComparer, MemoryType memoryType = MemoryType.Normal)
            : this(capacity, 0.66, itemequalityComparer, memoryType) { }


        /// <summary>
        /// Create a hash set with external item equalityComparer.
        /// </summary>
        /// <param name="capacity">Initial table size (rounded to power of 2, at least 16)</param>
        /// <param name="fill">Fill threshold (in range 10% to 90%)</param>
        /// <param name="itemequalityComparer">The external item equalitySCG.Comparer</param>
        /// <param name="memoryType"></param>
        public HashSet(int capacity, double fill, SCG.IEqualityComparer<T> itemequalityComparer, MemoryType memoryType = MemoryType.Normal)
            : base(itemequalityComparer, memoryType)
        {
            _randomhashfactor = (Debug.UseDeterministicHashing) ? 1529784659 : (2 * (uint)Random.Next() + 1) * 1529784659;

            if (fill < 0.1 || fill > 0.9)
                throw new ArgumentException("Fill outside valid range [0.1, 0.9]");
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be non-negative");
            //this.itemequalityComparer = itemequalityComparer;
            origbits = 4;
            while (capacity - 1 >> origbits > 0) origbits++;
            clear();
            _hashEnumerator = new HashEnumerator(memoryType);
        }



        #endregion

        #region IEditableCollection<T> Members

        /// <summary>
        /// The complexity of the Contains operation
        /// </summary>
        /// <value>Always returns Speed.Constant</value>
        public virtual Speed ContainsSpeed { get { return Speed.Constant; } }

        /// <summary>
        /// Check if an item is in the set 
        /// </summary>
        /// <param name="item">The item to look for</param>
        /// <returns>True if set contains item</returns>
        public virtual bool Contains(T item) { return searchoradd(ref item, false, false, false); }


        /// <summary>
        /// Check if an item (collection equal to a given one) is in the set and
        /// if so report the actual item object found.
        /// </summary>
        /// <param name="item">On entry, the item to look for.
        /// On exit the item found, if any</param>
        /// <returns>True if set contains item</returns>
        public virtual bool Find(ref T item) { return searchoradd(ref item, false, false, false); }


        /// <summary>
        /// Check if an item (collection equal to a given one) is in the set and
        /// if so replace the item object in the set with the supplied one.
        /// </summary>
        /// <param name="item">The item object to update with</param>
        /// <returns>True if item was found (and updated)</returns>
        public virtual bool Update(T item)
        { updatecheck(); return searchoradd(ref item, false, true, true); }

        /// <summary>
        /// Check if an item (collection equal to a given one) is in the set and
        /// if so replace the item object in the set with the supplied one.
        /// </summary>
        /// <param name="item">The item object to update with</param>
        /// <param name="olditem"></param>
        /// <returns>True if item was found (and updated)</returns>
        public virtual bool Update(T item, out T olditem)
        { updatecheck(); olditem = item; return searchoradd(ref olditem, false, true, true); }


        /// <summary>
        /// Check if an item (collection equal to a given one) is in the set.
        /// If found, report the actual item object in the set,
        /// else add the supplied one.
        /// </summary>
        /// <param name="item">On entry, the item to look for or add.
        /// On exit the actual object found, if any.</param>
        /// <returns>True if item was found</returns>
        public virtual bool FindOrAdd(ref T item)
        { updatecheck(); return searchoradd(ref item, true, false, true); }


        /// <summary>
        /// Check if an item (collection equal to a supplied one) is in the set and
        /// if so replace the item object in the set with the supplied one; else
        /// add the supplied one.
        /// </summary>
        /// <param name="item">The item to look for and update or add</param>
        /// <returns>True if item was updated</returns>
        public virtual bool UpdateOrAdd(T item)
        { updatecheck(); return searchoradd(ref item, true, true, true); }


        /// <summary>
        /// Check if an item (collection equal to a supplied one) is in the set and
        /// if so replace the item object in the set with the supplied one; else
        /// add the supplied one.
        /// </summary>
        /// <param name="item">The item to look for and update or add</param>
        /// <param name="olditem"></param>
        /// <returns>True if item was updated</returns>
        public virtual bool UpdateOrAdd(T item, out T olditem)
        { updatecheck(); olditem = item; return searchoradd(ref olditem, true, true, true); }


        /// <summary>
        /// Remove an item from the set
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>True if item was (found and) removed </returns>
        public virtual bool Remove(T item)
        {
            updatecheck();
            if (remove(ref item))
            {
                raiseForRemove(item);
                return true;
            }
            else
                return false;
        }


        /// <summary>
        /// Remove an item from the set, reporting the actual matching item object.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        /// <param name="removeditem">The removed value.</param>
        /// <returns>True if item was found.</returns>
        public virtual bool Remove(T item, out T removeditem)
        {
            updatecheck();
            removeditem = item;
            if (remove(ref removeditem))
            {
                raiseForRemove(removeditem);
                return true;
            }
            else
                return false;
        }


        /// <summary>
        /// Remove all items in a supplied collection from this set.
        /// </summary>
        /// <param name="items">The items to remove.</param>
        public virtual void RemoveAll(SCG.IEnumerable<T> items)
        {
            updatecheck();
            RaiseForRemoveAllHandler raiseHandler = new RaiseForRemoveAllHandler(this);
            bool raise = raiseHandler.MustFire;
            T jtem;
            foreach (var item in items)
            { jtem = item; if (remove(ref jtem) && raise) raiseHandler.Remove(jtem); }

            if (raise) raiseHandler.Raise();
        }

        /// <summary>
        /// Remove all items from the set, resetting internal table to initial size.
        /// </summary>
        public virtual void Clear()
        {
            updatecheck();
            int oldsize = size;
            clear();
            if (ActiveEvents != 0 && oldsize > 0)
            {
                raiseCollectionCleared(true, oldsize);
                raiseCollectionChanged();
            }
        }


        /// <summary>
        /// Remove all items *not* in a supplied collection from this set.
        /// </summary>
        /// <param name="items">The items to retain</param>
        public virtual void RetainAll(SCG.IEnumerable<T> items)
        {
            updatecheck();

            HashSet<T> aux = new HashSet<T>(EqualityComparer);

            //This only works for sets:
            foreach (var item in items)
                if (Contains(item))
                {
                    T jtem = item;

                    aux.searchoradd(ref jtem, true, false, false);
                }

            if (size == aux.size)
                return;

            CircularQueue<T> wasRemoved = null;
            if ((ActiveEvents & EventTypeEnum.Removed) != 0)
            {
                wasRemoved = new CircularQueue<T>();
                foreach (T item in this)
                    if (!aux.Contains(item))
                        wasRemoved.Enqueue(item);
            }

            table = aux.table;
            size = aux.size;

            indexmask = aux.indexmask;
            resizethreshhold = aux.resizethreshhold;
            bits = aux.bits;
            bitsc = aux.bitsc;

            _randomhashfactor = aux._randomhashfactor;

            if ((ActiveEvents & EventTypeEnum.Removed) != 0)
                raiseForRemoveAll(wasRemoved);
            else if ((ActiveEvents & EventTypeEnum.Changed) != 0)
                raiseCollectionChanged();
        }

        /// <summary>
        /// Check if all items in a supplied collection is in this set
        /// (ignoring multiplicities). 
        /// </summary>
        /// <param name="items">The items to look for.</param>
        /// <returns>True if all items are found.</returns>
        public virtual bool ContainsAll(SCG.IEnumerable<T> items)
        {
            foreach (var item in items)
                if (!Contains(item))
                    return false;
            return true;
        }


        /// <summary>
        /// Create an array containing all items in this set (in enumeration order).
        /// </summary>
        /// <returns>The array</returns>
        public override T[] ToArray()
        {
            T[] res = new T[size];
            int index = 0;

            for (int i = 0; i < table.Length; i++)
            {
                Bucket b = table[i];
                while (b != null)
                {
                    res[index++] = b.item;
                    b = b.overflow;
                }
            }

            System.Diagnostics.Debug.Assert(size == index);
            return res;
        }


        /// <summary>
        /// Count the number of times an item is in this set (either 0 or 1).
        /// </summary>
        /// <param name="item">The item to look for.</param>
        /// <returns>1 if item is in set, 0 else</returns>
        public virtual int ContainsCount(T item) { return Contains(item) ? 1 : 0; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual ICollectionValue<T> UniqueItems() { return this; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual ICollectionValue<KeyValuePair<T, int>> ItemMultiplicities()
        {
            return new MultiplicityOne<T>(this);
        }

        /// <summary>
        /// Remove all (at most 1) copies of item from this set.
        /// </summary>
        /// <param name="item">The item to remove</param>
        public virtual void RemoveAllCopies(T item) { Remove(item); }

        #endregion

        #region Enumerator

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class HashEnumerator : MemorySafeEnumerator<T>
        {
            private HashSet<T> _hashSet;
            private int _stamp;
            private int _index;
            Bucket b;


            public HashEnumerator(MemoryType memoryType)
                : base(memoryType)
            {

                _index = -1;
                Current = default(T);
            }

            internal void UpdateReference(HashSet<T> hashSet, int theStamp)
            {
                _hashSet = hashSet;
                _stamp = theStamp;
                Current = default(T);
                _index = -1;
            }


            public override void Dispose()
            {
                base.Dispose();

                //Do nothing
                _index = -1;
                b = null;
            }

            protected override MemorySafeEnumerator<T> Clone()
            {
                var enumerator = new HashEnumerator(MemoryType)
                {
                    Current = default(T),
                    _hashSet = _hashSet,
                };

                return enumerator;
            }

            public override bool MoveNext()
            {
                int len = _hashSet.table.Length;

                if (_stamp != _hashSet.stamp)
                    throw new CollectionModifiedException();
                //if (_index == len) return false;

                if (b == null || b.overflow == null)
                {
                    do
                    {
                        if (++_index < len) continue;
                        return false;
                    } while (_hashSet.table[_index] == null);

                    b = _hashSet.table[_index];
                    Current = b.item;

                    return true;
                }
                b = b.overflow;
                Current = b.item;
                return true;
            }

            public override void Reset()
            {
                throw new NotImplementedException();
            }
        }
        #endregion


        #region IEnumerable<T> Members


        /// <summary>
        /// Choose some item of this collection. 
        /// </summary>
        /// <exception cref="NoSuchItemException">if collection is empty.</exception>
        /// <returns></returns>
        public override T Choose()
        {
            int len = table.Length;
            if (size == 0)
                throw new NoSuchItemException();
            do { if (++lastchosen >= len) lastchosen = 0; } while (table[lastchosen] == null);

            return table[lastchosen].item;
        }

        /// <summary>
        /// Create an enumerator for this set.
        /// </summary>
        /// <returns>The enumerator</returns>
        public override SCG.IEnumerator<T> GetEnumerator()
        {
            var enumerator = (HashEnumerator)_hashEnumerator.GetEnumerator();

            enumerator.UpdateReference(this, stamp);

            return enumerator;
        }

        #endregion

        #region ISink<T> Members
        /// <summary>
        /// Report if this is a set collection.
        /// </summary>
        /// <value>Always false</value>
        public virtual bool AllowsDuplicates { get { return false; } }

        /// <summary>
        /// By convention this is true for any collection with set semantics.
        /// </summary>
        /// <value>True if only one representative of a group of equal items 
        /// is kept in the collection together with the total count.</value>
        public virtual bool DuplicatesByCounting { get { return true; } }

        /// <summary>
        /// Add an item to this set.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>True if item was added (i.e. not found)</returns>
        public virtual bool Add(T item)
        {
            updatecheck();
            return !searchoradd(ref item, true, false, true);
        }

        /// <summary>
        /// Add an item to this set.
        /// </summary>
        /// <param name="item">The item to add.</param>
        void SCG.ICollection<T>.Add(T item)
        {
            Add(item);
        }

        /// <summary>
        /// Add the elements from another collection with a more specialized item type 
        /// to this collection. Since this
        /// collection has set semantics, only items not already in the collection
        /// will be added.
        /// </summary>
        /// <param name="items">The items to add</param>
        public virtual void AddAll(SCG.IEnumerable<T> items)
        {
            updatecheck();
            bool wasChanged = false;
            bool raiseAdded = (ActiveEvents & EventTypeEnum.Added) != 0;
            CircularQueue<T> wasAdded = raiseAdded ? new CircularQueue<T>() : null;
            foreach (T item in items)
            {
                T jtem = item;

                if (!searchoradd(ref jtem, true, false, false))
                {
                    wasChanged = true;
                    if (raiseAdded)
                        wasAdded.Enqueue(item);
                }
            }
            //TODO: implement a RaiseForAddAll() method
            if (raiseAdded & wasChanged)
                foreach (T item in wasAdded)
                    raiseItemsAdded(item, 1);
            if (((ActiveEvents & EventTypeEnum.Changed) != 0 && wasChanged))
                raiseCollectionChanged();
        }


        #endregion

        #region Diagnostics

        /// <summary>
        /// Test internal structure of data (invariants)
        /// </summary>
        /// <returns>True if pass</returns>
        public virtual bool Check()
        {
            int count = 0;
            bool retval = true;

            if (bitsc != 32 - bits)
            {
                Logger.Log(string.Format("bitsc != 32 - bits ({0}, {1})", bitsc, bits));
                retval = false;
            }
            if (indexmask != (1 << bits) - 1)
            {
                Logger.Log(string.Format("indexmask != (1 << bits) - 1 ({0}, {1})", indexmask, bits));
                retval = false;
            }
            if (table.Length != indexmask + 1)
            {
                Logger.Log(string.Format("table.Length != indexmask + 1 ({0}, {1})", table.Length, indexmask));
                retval = false;
            }
            if (bitsc != 32 - bits)
            {
                Logger.Log(string.Format("resizethreshhold != (int)(table.Length * fillfactor) ({0}, {1}, {2})", resizethreshhold, table.Length, fillfactor));
                retval = false;
            }

            for (int i = 0, s = table.Length; i < s; i++)
            {
                int level = 0;
                Bucket b = table[i];
                while (b != null)
                {
                    if (i != hv2i(b.hashval))
                    {
                        Logger.Log(string.Format("Bad cell item={0}, hashval={1}, index={2}, level={3}", b.item, b.hashval, i, level));
                        retval = false;
                    }

                    count++;
                    level++;
                    b = b.overflow;
                }
            }

            if (count != size)
            {
                Logger.Log(string.Format("size({0}) != count({1})", size, count));
                retval = false;
            }

            return retval;
        }


        /// <summary>
        /// Produce statistics on distribution of bucket sizes. Current implementation is incomplete.
        /// </summary>
        /// <returns>Histogram data.</returns>
        public ISortedDictionary<int, int> BucketCostDistribution()
        {
            TreeDictionary<int, int> res = new TreeDictionary<int, int>();
            for (int i = 0, s = table.Length; i < s; i++)
            {
                int count = 0;
                Bucket b = table[i];

                while (b != null)
                {
                    count++;
                    b = b.overflow;
                }
                if (res.Contains(count))
                    res[count]++;
                else
                    res[count] = 1;
            }

            return res;
        }

        #endregion
    }



    /// <summary>
    /// Holds the real events for a collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class EventBlock<T>
    {
        internal EventTypeEnum events;

        event CollectionChangedHandler<T> collectionChanged;
        internal event CollectionChangedHandler<T> CollectionChanged
        {
            add
            {
                collectionChanged += value;
                events |= EventTypeEnum.Changed;
            }
            remove
            {
                collectionChanged -= value;
                if (collectionChanged == null)
                    events &= ~EventTypeEnum.Changed;
            }
        }
        internal void raiseCollectionChanged(object sender)
        { if (collectionChanged != null) collectionChanged(sender); }

        event CollectionClearedHandler<T> collectionCleared;
        internal event CollectionClearedHandler<T> CollectionCleared
        {
            add
            {
                collectionCleared += value;
                events |= EventTypeEnum.Cleared;
            }
            remove
            {
                collectionCleared -= value;
                if (collectionCleared == null)
                    events &= ~EventTypeEnum.Cleared;
            }
        }
        internal void raiseCollectionCleared(object sender, bool full, int count)
        { if (collectionCleared != null) collectionCleared(sender, new ClearedEventArgs(full, count)); }
        internal void raiseCollectionCleared(object sender, bool full, int count, int? start)
        { if (collectionCleared != null) collectionCleared(sender, new ClearedRangeEventArgs(full, count, start)); }

        event ItemsAddedHandler<T> itemsAdded;
        internal event ItemsAddedHandler<T> ItemsAdded
        {
            add
            {
                itemsAdded += value;
                events |= EventTypeEnum.Added;
            }
            remove
            {
                itemsAdded -= value;
                if (itemsAdded == null)
                    events &= ~EventTypeEnum.Added;
            }
        }
        internal void raiseItemsAdded(object sender, T item, int count)
        { if (itemsAdded != null) itemsAdded(sender, new ItemCountEventArgs<T>(item, count)); }

        event ItemsRemovedHandler<T> itemsRemoved;
        internal event ItemsRemovedHandler<T> ItemsRemoved
        {
            add
            {
                itemsRemoved += value;
                events |= EventTypeEnum.Removed;
            }
            remove
            {
                itemsRemoved -= value;
                if (itemsRemoved == null)
                    events &= ~EventTypeEnum.Removed;
            }
        }
        internal void raiseItemsRemoved(object sender, T item, int count)
        { if (itemsRemoved != null) itemsRemoved(sender, new ItemCountEventArgs<T>(item, count)); }

        event ItemInsertedHandler<T> itemInserted;
        internal event ItemInsertedHandler<T> ItemInserted
        {
            add
            {
                itemInserted += value;
                events |= EventTypeEnum.Inserted;
            }
            remove
            {
                itemInserted -= value;
                if (itemInserted == null)
                    events &= ~EventTypeEnum.Inserted;
            }
        }
        internal void raiseItemInserted(object sender, T item, int index)
        { if (itemInserted != null) itemInserted(sender, new ItemAtEventArgs<T>(item, index)); }

        event ItemRemovedAtHandler<T> itemRemovedAt;
        internal event ItemRemovedAtHandler<T> ItemRemovedAt
        {
            add
            {
                itemRemovedAt += value;
                events |= EventTypeEnum.RemovedAt;
            }
            remove
            {
                itemRemovedAt -= value;
                if (itemRemovedAt == null)
                    events &= ~EventTypeEnum.RemovedAt;
            }
        }
        internal void raiseItemRemovedAt(object sender, T item, int index)
        { if (itemRemovedAt != null) itemRemovedAt(sender, new ItemAtEventArgs<T>(item, index)); }
    }

    /// <summary>
    /// Tentative, to conserve memory in GuardedCollectionValueBase
    /// This should really be nested in Guarded collection value, only have a guardereal field
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class ProxyEventBlock<T>
    {
        ICollectionValue<T> proxy, real;

        internal ProxyEventBlock(ICollectionValue<T> proxy, ICollectionValue<T> real)
        { this.proxy = proxy; this.real = real; }

        event CollectionChangedHandler<T> collectionChanged;
        CollectionChangedHandler<T> collectionChangedProxy;
        internal event CollectionChangedHandler<T> CollectionChanged
        {
            add
            {
                if (collectionChanged == null)
                {
                    if (collectionChangedProxy == null)
                        collectionChangedProxy = delegate (object sender) { collectionChanged(proxy); };
                    real.CollectionChanged += collectionChangedProxy;
                }
                collectionChanged += value;
            }
            remove
            {
                collectionChanged -= value;
                if (collectionChanged == null)
                    real.CollectionChanged -= collectionChangedProxy;
            }
        }

        event CollectionClearedHandler<T> collectionCleared;
        CollectionClearedHandler<T> collectionClearedProxy;
        internal event CollectionClearedHandler<T> CollectionCleared
        {
            add
            {
                if (collectionCleared == null)
                {
                    if (collectionClearedProxy == null)
                        collectionClearedProxy = delegate (object sender, ClearedEventArgs e) { collectionCleared(proxy, e); };
                    real.CollectionCleared += collectionClearedProxy;
                }
                collectionCleared += value;
            }
            remove
            {
                collectionCleared -= value;
                if (collectionCleared == null)
                    real.CollectionCleared -= collectionClearedProxy;
            }
        }

        event ItemsAddedHandler<T> itemsAdded;
        ItemsAddedHandler<T> itemsAddedProxy;
        internal event ItemsAddedHandler<T> ItemsAdded
        {
            add
            {
                if (itemsAdded == null)
                {
                    if (itemsAddedProxy == null)
                        itemsAddedProxy = delegate (object sender, ItemCountEventArgs<T> e) { itemsAdded(proxy, e); };
                    real.ItemsAdded += itemsAddedProxy;
                }
                itemsAdded += value;
            }
            remove
            {
                itemsAdded -= value;
                if (itemsAdded == null)
                    real.ItemsAdded -= itemsAddedProxy;
            }
        }

        event ItemInsertedHandler<T> itemInserted;
        ItemInsertedHandler<T> itemInsertedProxy;
        internal event ItemInsertedHandler<T> ItemInserted
        {
            add
            {
                if (itemInserted == null)
                {
                    if (itemInsertedProxy == null)
                        itemInsertedProxy = delegate (object sender, ItemAtEventArgs<T> e) { itemInserted(proxy, e); };
                    real.ItemInserted += itemInsertedProxy;
                }
                itemInserted += value;
            }
            remove
            {
                itemInserted -= value;
                if (itemInserted == null)
                    real.ItemInserted -= itemInsertedProxy;
            }
        }

        event ItemsRemovedHandler<T> itemsRemoved;
        ItemsRemovedHandler<T> itemsRemovedProxy;
        internal event ItemsRemovedHandler<T> ItemsRemoved
        {
            add
            {
                if (itemsRemoved == null)
                {
                    if (itemsRemovedProxy == null)
                        itemsRemovedProxy = delegate (object sender, ItemCountEventArgs<T> e) { itemsRemoved(proxy, e); };
                    real.ItemsRemoved += itemsRemovedProxy;
                }
                itemsRemoved += value;
            }
            remove
            {
                itemsRemoved -= value;
                if (itemsRemoved == null)
                    real.ItemsRemoved -= itemsRemovedProxy;
            }
        }

        event ItemRemovedAtHandler<T> itemRemovedAt;
        ItemRemovedAtHandler<T> itemRemovedAtProxy;
        internal event ItemRemovedAtHandler<T> ItemRemovedAt
        {
            add
            {
                if (itemRemovedAt == null)
                {
                    if (itemRemovedAtProxy == null)
                        itemRemovedAtProxy = delegate (object sender, ItemAtEventArgs<T> e) { itemRemovedAt(proxy, e); };
                    real.ItemRemovedAt += itemRemovedAtProxy;
                }
                itemRemovedAt += value;
            }
            remove
            {
                itemRemovedAt -= value;
                if (itemRemovedAt == null)
                    real.ItemRemovedAt -= itemRemovedAtProxy;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class ItemCountEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly T Item;
        /// <summary>
        /// 
        /// </summary>
        public readonly int Count;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        /// <param name="item"></param>
        public ItemCountEventArgs(T item, int count) { Item = item; Count = count; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("(ItemCountEventArgs {0} '{1}')", Count, Item);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class ItemAtEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly T Item;
        /// <summary>
        /// 
        /// </summary>
        public readonly int Index;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        public ItemAtEventArgs(T item, int index) { Item = item; Index = index; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("(ItemAtEventArgs {0} '{1}')", Index, Item);
        }
    }

    /// <summary>
    /// 
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class ClearedEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly bool Full;
        /// <summary>
        /// 
        /// </summary>
        public readonly int Count;
        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="full">True if the operation cleared all of the collection</param>
        /// <param name="count">The number of items removed by the clear.</param>
        public ClearedEventArgs(bool full, int count) { Full = full; Count = count; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("(ClearedEventArgs {0} {1})", Count, Full);
        }
    }

    /// <summary>
    /// 
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class ClearedRangeEventArgs : ClearedEventArgs
    {
        //WE could let this be of type int? to  allow 
        /// <summary>
        /// 
        /// </summary>
        public readonly int? Start;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="full"></param>
        /// <param name="count"></param>
        /// <param name="start"></param>
        public ClearedRangeEventArgs(bool full, int count, int? start) : base(full, count) { Start = start; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("(ClearedRangeEventArgs {0} {1} {2})", Count, Full, Start);
        }
    }


    /// <summary>
    /// An entry in a dictionary from K to V.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public struct KeyValuePair<K, V> : IEquatable<KeyValuePair<K, V>>, IShowable
    {
        /// <summary>
        /// The key field of the entry
        /// </summary>
        public K Key;

        /// <summary>
        /// The value field of the entry
        /// </summary>
        public V Value;

        /// <summary>
        /// Create an entry with specified key and value
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        public KeyValuePair(K key, V value) { Key = key; Value = value; }


        /// <summary>
        /// Create an entry with a specified key. The value will be the default value of type <code>V</code>.
        /// </summary>
        /// <param name="key">The key</param>
        public KeyValuePair(K key) { Key = key; Value = default(V); }


        /// <summary>
        /// Pretty print an entry
        /// </summary>
        /// <returns>(key, value)</returns>
        public override string ToString() { return "(" + Key + ", " + Value + ")"; }


        /// <summary>
        /// Check equality of entries. 
        /// </summary>
        /// <param name="obj">The other object</param>
        /// <returns>True if obj is an entry of the same type and has the same key and value</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is KeyValuePair<K, V>))
                return false;
            KeyValuePair<K, V> other = (KeyValuePair<K, V>)obj;
            return Equals(other);
        }

        /// <summary>
        /// Get the hash code of the pair.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode() { return EqualityComparer<K>.Default.GetHashCode(Key) + 13984681 * EqualityComparer<V>.Default.GetHashCode(Value); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(KeyValuePair<K, V> other)
        {
            return EqualityComparer<K>.Default.Equals(Key, other.Key) && EqualityComparer<V>.Default.Equals(Value, other.Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pair1"></param>
        /// <param name="pair2"></param>
        /// <returns></returns>
        public static bool operator ==(KeyValuePair<K, V> pair1, KeyValuePair<K, V> pair2) { return pair1.Equals(pair2); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pair1"></param>
        /// <param name="pair2"></param>
        /// <returns></returns>
        public static bool operator !=(KeyValuePair<K, V> pair1, KeyValuePair<K, V> pair2) { return !pair1.Equals(pair2); }

        #region IShowable Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringbuilder"></param>
        /// <param name="formatProvider"></param>
        /// <param name="rest"></param>
        /// <returns></returns>
        public bool Show(System.Text.StringBuilder stringbuilder, ref int rest, IFormatProvider formatProvider)
        {
            if (rest < 0)
                return false;
            if (!Showing.Show(Key, stringbuilder, ref rest, formatProvider))
                return false;
            stringbuilder.Append(" => ");
            rest -= 4;
            if (!Showing.Show(Value, stringbuilder, ref rest, formatProvider))
                return false;
            return rest >= 0;
        }
        #endregion

        #region IFormattable Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return Showing.ShowString(this, format, formatProvider);
        }

        #endregion
    }

    /// <summary>
    /// Default comparer for dictionary entries in a sorted dictionary.
    /// Entry comparisons only look at keys and uses an externally defined comparer for that.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class KeyValuePairComparer<K, V> : SCG.IComparer<KeyValuePair<K, V>>
    {
        SCG.IComparer<K> comparer;


        /// <summary>
        /// Create an entry comparer for a item comparer of the keys
        /// </summary>
        /// <param name="comparer">Comparer of keys</param>
        public KeyValuePairComparer(SCG.IComparer<K> comparer)
        {
            if (comparer == null)
                throw new NullReferenceException();
            this.comparer = comparer;
        }


        /// <summary>
        /// Compare two entries
        /// </summary>
        /// <param name="entry1">First entry</param>
        /// <param name="entry2">Second entry</param>
        /// <returns>The result of comparing the keys</returns>
        public int Compare(KeyValuePair<K, V> entry1, KeyValuePair<K, V> entry2)
        {
            return comparer.Compare(entry1.Key, entry2.Key);
        }
    }

    /// <summary>
    /// Default equalityComparer for dictionary entries.
    /// Operations only look at keys and uses an externally defined equalityComparer for that.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public sealed class KeyValuePairEqualityComparer<K, V> : SCG.IEqualityComparer<KeyValuePair<K, V>>
    {
        SCG.IEqualityComparer<K> keyequalityComparer;


        /// <summary>
        /// Create an entry equalityComparer using the default equalityComparer for keys
        /// </summary>
        public KeyValuePairEqualityComparer() { keyequalityComparer = EqualityComparer<K>.Default; }


        /// <summary>
        /// Create an entry equalityComparer from a specified item equalityComparer for the keys
        /// </summary>
        /// <param name="keyequalityComparer">The key equalitySCG.Comparer</param>
        public KeyValuePairEqualityComparer(SCG.IEqualityComparer<K> keyequalityComparer)
        {
            if (keyequalityComparer == null)
                throw new NullReferenceException("Key equality comparer cannot be null");
            this.keyequalityComparer = keyequalityComparer;
        }


        /// <summary>
        /// Get the hash code of the entry
        /// </summary>
        /// <param name="entry">The entry</param>
        /// <returns>The hash code of the key</returns>
        public int GetHashCode(KeyValuePair<K, V> entry) { return keyequalityComparer.GetHashCode(entry.Key); }


        /// <summary>
        /// Test two entries for equality
        /// </summary>
        /// <param name="entry1">First entry</param>
        /// <param name="entry2">Second entry</param>
        /// <returns>True if keys are equal</returns>
        public bool Equals(KeyValuePair<K, V> entry1, KeyValuePair<K, V> entry2)
        {
            return keyequalityComparer.Equals(entry1.Key, entry2.Key);
        }
    }


    /// <summary>
    /// Utility class for building default generic equality comparers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public static class EqualityComparer<T>
    {
        private static SCG.IEqualityComparer<T> _default;

        readonly static Type SequencedCollectionEqualityComparer = typeof(SequencedCollectionEqualityComparer<,>);

        readonly static Type UnsequencedCollectionEqualityComparer = typeof(UnsequencedCollectionEqualityComparer<,>);

        /// <summary>
        /// A default generic equality comparer for type T. The procedure is as follows:
        /// <list>
        /// <item>If the actual generic argument T implements the generic interface
        /// <see cref="T:C5.ISequenced`1"/> for some value W of its generic parameter T,
        /// the equalityComparer will be <see cref="T:C5.SequencedCollectionEqualityComparer`2"/></item>
        /// <item>If the actual generic argument T implements 
        /// <see cref="T:C5.ICollection`1"/> for some value W of its generic parameter T,
        /// the equalityComparer will be <see cref="T:C5.UnsequencedCollectionEqualityComparer`2"/></item>
        /// <item>Otherwise the SCG.EqualityComparer&lt;T&gt;.Default is returned</item>
        /// </list>   
        /// </summary>
        /// <value>The comparer</value>
        public static SCG.IEqualityComparer<T> Default
        {
            get
            {
                if (_default != null)
                {
                    return _default;
                }

                var type = typeof(T);
                var interfaces = type.GetInterfaces();

                if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(ISequenced<>)))
                {
                    return CreateAndCache(SequencedCollectionEqualityComparer.MakeGenericType(new[] { type, type.GetGenericArguments()[0] }));
                }

                var isequenced = interfaces.FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition().Equals(typeof(ISequenced<>)));
                if (isequenced != null)
                {
                    return CreateAndCache(SequencedCollectionEqualityComparer.MakeGenericType(new[] { type, isequenced.GetGenericArguments()[0] }));
                }

                if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(ICollection<>)))
                {
                    return CreateAndCache(UnsequencedCollectionEqualityComparer.MakeGenericType(new[] { type, type.GetGenericArguments()[0] }));
                }

                var icollection = interfaces.FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition().Equals(typeof(ICollection<>)));
                if (icollection != null)
                {
                    return CreateAndCache(UnsequencedCollectionEqualityComparer.MakeGenericType(new[] { type, icollection.GetGenericArguments()[0] }));
                }

                return _default = SCG.EqualityComparer<T>.Default;
            }
        }

        private static SCG.IEqualityComparer<T> CreateAndCache(Type equalityComparertype)
        {
            return _default = (SCG.IEqualityComparer<T>)(equalityComparertype.GetProperty("Default", BindingFlags.Static | BindingFlags.Public).GetValue(null, null));
        }
    }


    /// <summary>
    /// An equalityComparer compatible with a given comparer. All hash codes are 0, 
    /// meaning that anything based on hash codes will be quite inefficient.
    /// <para><b>Note: this will give a new EqualityComparer each time created!</b></para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal class ComparerZeroHashCodeEqualityComparer<T> : SCG.IEqualityComparer<T>
    {
        SCG.IComparer<T> comparer;
        /// <summary>
        /// Create a trivial <see cref="T:System.Collections.Generic.IEqualityComparer`1"/> compatible with the 
        /// <see cref="T:System.Collections.Generic.IComparer`1"/> <code>comparer</code>
        /// </summary>
        /// <param name="comparer"></param>
        public ComparerZeroHashCodeEqualityComparer(SCG.IComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new NullReferenceException("Comparer cannot be null");
            }
            this.comparer = comparer;
        }
        /// <summary>
        /// A trivial, inefficient hash function. Compatible with any equality relation.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>0</returns>
        public int GetHashCode(T item) { return 0; }
        /// <summary>
        /// Equality of two items as defined by the comparer.
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns></returns>
        public bool Equals(T item1, T item2) { return comparer.Compare(item1, item2) == 0; }
    }

    /// <summary>
    /// Prototype for a sequenced equalityComparer for something (T) that implements ISequenced[W].
    /// This will use ISequenced[W] specific implementations of the equality comparer operations.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="W"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class SequencedCollectionEqualityComparer<T, W> : SCG.IEqualityComparer<T>
        where T : ISequenced<W>
    {
        static SequencedCollectionEqualityComparer<T, W> cached;
        SequencedCollectionEqualityComparer() { }
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public static SequencedCollectionEqualityComparer<T, W> Default
        {
            get { return cached ?? (cached = new SequencedCollectionEqualityComparer<T, W>()); }
        }
        /// <summary>
        /// Get the hash code with respect to this sequenced equalityComparer
        /// </summary>
        /// <param name="collection">The collection</param>
        /// <returns>The hash code</returns>
        public int GetHashCode(T collection) { return collection.GetSequencedHashCode(); }

        /// <summary>
        /// Check if two items are equal with respect to this sequenced equalityComparer
        /// </summary>
        /// <param name="collection1">first collection</param>
        /// <param name="collection2">second collection</param>
        /// <returns>True if equal</returns>
        public bool Equals(T collection1, T collection2) { return collection1 == null ? collection2 == null : collection1.SequencedEquals(collection2); }
    }

    /// <summary>
    /// Prototype for an unsequenced equalityComparer for something (T) that implements ICollection[W]
    /// This will use ICollection[W] specific implementations of the equalityComparer operations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="W"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class UnsequencedCollectionEqualityComparer<T, W> : SCG.IEqualityComparer<T>
        where T : ICollection<W>
    {
        static UnsequencedCollectionEqualityComparer<T, W> cached;
        UnsequencedCollectionEqualityComparer() { }
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public static UnsequencedCollectionEqualityComparer<T, W> Default { get { return cached ?? (cached = new UnsequencedCollectionEqualityComparer<T, W>()); } }
        /// <summary>
        /// Get the hash code with respect to this unsequenced equalityComparer
        /// </summary>
        /// <param name="collection">The collection</param>
        /// <returns>The hash code</returns>
        public int GetHashCode(T collection) { return collection.GetUnsequencedHashCode(); }


        /// <summary>
        /// Check if two collections are equal with respect to this unsequenced equalityComparer
        /// </summary>
        /// <param name="collection1">first collection</param>
        /// <param name="collection2">second collection</param>
        /// <returns>True if equal</returns>
        public bool Equals(T collection1, T collection2) { return collection1 == null ? collection2 == null : collection1.UnsequencedEquals(collection2); }
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal abstract class MemorySafeEnumerator<T> : SCG.IEnumerator<T>, SCG.IEnumerable<T>, IDisposable
    {
        private static int MainThreadId;

        //-1 means an iterator is not in use. 
        protected int IteratorState;

        protected MemoryType MemoryType { get; private set; }

        protected static bool IsMainThread
        {
            get { return System.Threading.Thread.CurrentThread.ManagedThreadId == MainThreadId; }
        }

        protected MemorySafeEnumerator(MemoryType memoryType)
        {
            MemoryType = memoryType;
            MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            IteratorState = -1;
        }

        protected abstract MemorySafeEnumerator<T> Clone();

        public abstract bool MoveNext();

        public abstract void Reset();

        public T Current { get; protected set; }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public virtual void Dispose()
        {
            IteratorState = -1;
        }

        public SCG.IEnumerator<T> GetEnumerator()
        {
            MemorySafeEnumerator<T> enumerator;

            switch (MemoryType)
            {
                case MemoryType.Normal:
                    enumerator = Clone();
                    break;
                case MemoryType.Safe:
                    if (IsMainThread)
                    {
                        enumerator = IteratorState != -1 ? Clone() : this;

                        IteratorState = 0;
                    }
                    else
                    {
                        enumerator = Clone();
                    }
                    break;
                case MemoryType.Strict:
                    if (!IsMainThread)
                    {
                        throw new ConcurrentEnumerationException("Multithread access detected! In Strict memory mode is not possible to iterate the collection from different threads");
                    }

                    if (IteratorState != -1)
                    {
                        throw new MultipleEnumerationException("Multiple Enumeration detected! In Strict memory mode is not possible to iterate the collection multiple times");
                    }

                    enumerator = this;
                    IteratorState = 0;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            return enumerator;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    abstract class MappedDirectedEnumerable<T, V> : EnumerableBase<V>, IDirectedEnumerable<V>
    {
        IDirectedEnumerable<T> directedenumerable;

        abstract public V Map(T item);

        public MappedDirectedEnumerable(IDirectedEnumerable<T> directedenumerable)
        {
            this.directedenumerable = directedenumerable;
        }

        public IDirectedEnumerable<V> Backwards()
        {
            MappedDirectedEnumerable<T, V> retval = (MappedDirectedEnumerable<T, V>)MemberwiseClone();
            retval.directedenumerable = directedenumerable.Backwards();
            return retval;
            //If we made this classs non-abstract we could do
            //return new MappedDirectedCollectionValue<T,V>(directedcollectionvalue.Backwards());;
        }


        public override SCG.IEnumerator<V> GetEnumerator()
        {
            foreach (T item in directedenumerable)
                yield return Map(item);
        }

        public EnumerationDirection Direction
        {
            get { return directedenumerable.Direction; }
        }
    }

    /// <summary>
    /// A base class for implementing an IEnumerable&lt;T&gt;
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class EnumerableBase<T> : SCG.IEnumerable<T>
    {
        /// <summary>
        /// Create an enumerator for this collection.
        /// </summary>
        /// <returns>The enumerator</returns>
        public abstract SCG.IEnumerator<T> GetEnumerator();

        /// <summary>
        /// Count the number of items in an enumerable by enumeration
        /// </summary>
        /// <param name="items">The enumerable to count</param>
        /// <returns>The size of the enumerable</returns>
        protected static int countItems(SCG.IEnumerable<T> items)
        {
            ICollectionValue<T> jtems = items as ICollectionValue<T>;

            if (jtems != null)
                return jtems.Count;

            int count = 0;

            using (SCG.IEnumerator<T> e = items.GetEnumerator())
                while (e.MoveNext()) count++;

            return count;
        }

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    abstract class MappedDirectedCollectionValue<T, V> : DirectedCollectionValueBase<V>, IDirectedCollectionValue<V>
    {
        IDirectedCollectionValue<T> directedcollectionvalue;

        abstract public V Map(T item);

        public MappedDirectedCollectionValue(IDirectedCollectionValue<T> directedcollectionvalue)
        {
            this.directedcollectionvalue = directedcollectionvalue;
        }

        public override V Choose() { return Map(directedcollectionvalue.Choose()); }

        public override bool IsEmpty { get { return directedcollectionvalue.IsEmpty; } }

        public override int Count { get { return directedcollectionvalue.Count; } }

        public override Speed CountSpeed { get { return directedcollectionvalue.CountSpeed; } }

        public override IDirectedCollectionValue<V> Backwards()
        {
            MappedDirectedCollectionValue<T, V> retval = (MappedDirectedCollectionValue<T, V>)MemberwiseClone();
            retval.directedcollectionvalue = directedcollectionvalue.Backwards();
            return retval;
            //If we made this classs non-abstract we could do
            //return new MappedDirectedCollectionValue<T,V>(directedcollectionvalue.Backwards());;
        }


        public override SCG.IEnumerator<V> GetEnumerator()
        {
            foreach (T item in directedcollectionvalue)
                yield return Map(item);
        }

        public override EnumerationDirection Direction
        {
            get { return directedcollectionvalue.Direction; }
        }

        IDirectedEnumerable<V> IDirectedEnumerable<V>.Backwards()
        {
            return Backwards();
        }


    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class DirectedCollectionValueBase<T> : CollectionValueBase<T>, IDirectedCollectionValue<T>
    {
        /// <summary>
        /// <code>Forwards</code> if same, else <code>Backwards</code>
        /// </summary>
        /// <value>The enumeration direction relative to the original collection.</value>
        public virtual EnumerationDirection Direction { get { return EnumerationDirection.Forwards; } }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract IDirectedCollectionValue<T> Backwards();

        IDirectedEnumerable<T> IDirectedEnumerable<T>.Backwards() { return this.Backwards(); }

        /// <summary>
        /// Check if there exists an item  that satisfies a
        /// specific predicate in this collection and return the first one in enumeration order.
        /// </summary>
        /// <param name="predicate">A delegate 
        /// (<see cref="T:Func`2"/> with <code>R == bool</code>) defining the predicate</param>
        /// <param name="item"></param>
        /// <returns>True is such an item exists</returns>
        public virtual bool FindLast(Func<T, bool> predicate, out T item)
        {
            foreach (T jtem in Backwards())
                if (predicate(jtem))
                {
                    item = jtem;
                    return true;
                }
            item = default(T);
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class CircularQueue<T> : SequencedBase<T>, IQueue<T>, IStack<T>
    {
        #region Fields
        /*
        Invariant: the itemes in the queue ar the elements from front upwards, 
        possibly wrapping around at the end of array, to back.

        if front<=back then size = back - front + 1;
        else size = array.Length + back - front + 1;

        */
        int front, back;
        /// <summary>
        /// The internal container array is doubled when necessary, but never shrinked.
        /// </summary>
        T[] array;
        bool forwards = true, original = true;
        #endregion

        #region Events

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public override EventTypeEnum ListenableEvents { get { return EventTypeEnum.Basic; } }

        #endregion

        #region Util
        void expand()
        {
            int newlength = 2 * array.Length;
            T[] newarray = new T[newlength];

            if (front <= back)
                Array.Copy(array, front, newarray, 0, size);
            else
            {
                int half = array.Length - front;
                Array.Copy(array, front, newarray, 0, half);
                Array.Copy(array, 0, newarray, half, size - half);
            }

            front = 0;
            back = size;
            array = newarray;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public CircularQueue(MemoryType memoryType = MemoryType.Normal) : this(8, memoryType) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="memoryType">The memory type strategy of the internal enumerator used to iterate over the collection</param>
        public CircularQueue(int capacity, MemoryType memoryType = MemoryType.Normal)
            : base(EqualityComparer<T>.Default, memoryType)
        {
            int newlength = 8;
            while (newlength < capacity) newlength *= 2;
            array = new T[newlength];
        }

        #endregion

        #region IQueue<T> Members
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public virtual bool AllowsDuplicates { get { return true; } }

        /// <summary>
        /// Get the i'th item in the queue. The front of the queue is at index 0.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public virtual T this[int i]
        {
            get
            {
                if (i < 0 || i >= size)
                    throw new IndexOutOfRangeException();
                i = i + front;
                //Bug fix by Steve Wallace 2006/02/10
                return array[i >= array.Length ? i - array.Length : i];
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public virtual void Enqueue(T item)
        {
            if (!original)
                throw new ReadOnlyCollectionException();
            stamp++;
            if (size == array.Length - 1) expand();
            size++;
            int oldback = back++;
            if (back == array.Length) back = 0;
            array[oldback] = item;
            if (ActiveEvents != 0)
                raiseForAdd(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual T Dequeue()
        {
            if (!original)
                throw new ReadOnlyCollectionException("Object is a non-updatable clone");
            stamp++;
            if (size == 0)
                throw new NoSuchItemException();
            size--;
            int oldfront = front++;
            if (front == array.Length) front = 0;
            T retval = array[oldfront];
            array[oldfront] = default(T);
            if (ActiveEvents != 0)
                raiseForRemove(retval);
            return retval;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void Push(T item) //== Enqueue
        {
            if (!original)
                throw new ReadOnlyCollectionException();
            stamp++;
            if (size == array.Length - 1) expand();
            size++;
            int oldback = back++;
            if (back == array.Length) back = 0;
            array[oldback] = item;
            if (ActiveEvents != 0)
                raiseForAdd(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
            if (!original)
                throw new ReadOnlyCollectionException("Object is a non-updatable clone");
            stamp++;
            if (size == 0)
                throw new NoSuchItemException();
            size--;
            back--;
            if (back == -1) back = array.Length - 1;
            T retval = array[back];
            array[back] = default(T);
            if (ActiveEvents != 0)
                raiseForRemove(retval);
            return retval;
        }
        #endregion

        #region ICollectionValue<T> Members

        //TODO: implement these with Array.Copy instead of relying on XxxBase:
        /*
            public void CopyTo(T[] a, int i)
            {
            }

            public T[] ToArray()
            {
            }*/

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override T Choose()
        {
            if (size == 0)
                throw new NoSuchItemException();
            return array[front];
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override SCG.IEnumerator<T> GetEnumerator()
        {
            int stamp = this.stamp;
            if (forwards)
            {
                int position = front;
                int end = front <= back ? back : array.Length;
                while (position < end)
                {
                    if (stamp != this.stamp)
                        throw new CollectionModifiedException();
                    yield return array[position++];
                }
                if (front > back)
                {
                    position = 0;
                    while (position < back)
                    {
                        if (stamp != this.stamp)
                            throw new CollectionModifiedException();
                        yield return array[position++];
                    }
                }
            }
            else
            {
                int position = back - 1;
                int end = front <= back ? front : 0;
                while (position >= end)
                {
                    if (stamp != this.stamp)
                        throw new CollectionModifiedException();
                    yield return array[position--];
                }
                if (front > back)
                {
                    position = array.Length - 1;
                    while (position >= front)
                    {
                        if (stamp != this.stamp)
                            throw new CollectionModifiedException();
                        yield return array[position--];
                    }
                }
            }
        }

        #endregion

        #region IDirectedCollectionValue<T> Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IDirectedCollectionValue<T> Backwards()
        {
            CircularQueue<T> retval = (CircularQueue<T>)MemberwiseClone();
            retval.original = false;
            retval.forwards = !forwards;
            return retval;
        }

        #endregion

        #region IDirectedEnumerable<T> Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IDirectedEnumerable<T> IDirectedEnumerable<T>.Backwards()
        {
            return Backwards();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual bool Check()
        {
            if (front < 0 || front >= array.Length || back < 0 || back >= array.Length ||
                (front <= back && size != back - front) || (front > back && size != array.Length + back - front))
            {
                Logger.Log(string.Format("Bad combination of (front,back,size,array.Length): ({0},{1},{2},{3})",
                    front, back, size, array.Length));
                return false;
            }
            return true;
        }
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    abstract class MappedCollectionValue<T, V> : CollectionValueBase<V>, ICollectionValue<V>
    {
        ICollectionValue<T> collectionvalue;

        abstract public V Map(T item);

        public MappedCollectionValue(ICollectionValue<T> collectionvalue)
        {
            this.collectionvalue = collectionvalue;
        }

        public override V Choose() { return Map(collectionvalue.Choose()); }

        public override bool IsEmpty { get { return collectionvalue.IsEmpty; } }

        public override int Count { get { return collectionvalue.Count; } }

        public override Speed CountSpeed { get { return collectionvalue.CountSpeed; } }

        public override SCG.IEnumerator<V> GetEnumerator()
        {
            foreach (T item in collectionvalue)
                yield return Map(item);
        }
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    class MultiplicityOne<K> : MappedCollectionValue<K, KeyValuePair<K, int>>
    {
        public MultiplicityOne(ICollectionValue<K> coll) : base(coll) { }
        public override KeyValuePair<K, int> Map(K k) { return new KeyValuePair<K, int>(k, 1); }
    }

    /// <summary>
    /// Class containing debugging symbols - to eliminate preprocessor directives
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal class Debug
    {
        /// <summary>
        /// Flag used to test hashing. Set to true when unit testing hash functions.
        /// </summary>
        internal static bool UseDeterministicHashing { get; set; }
    }

    // Static helper methods for Showing collections 

    /// <summary>
    /// 
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public static class Showing
    {
        /// <summary>
        /// Show  <code>Object obj</code> by appending it to <code>stringbuilder</code>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="stringbuilder"></param>
        /// <param name="rest"></param>
        /// <param name="formatProvider"></param>
        /// <returns>True if <code>obj</code> was shown completely.</returns>
        public static bool Show(Object obj, System.Text.StringBuilder stringbuilder, ref int rest, IFormatProvider formatProvider)
        {
            IShowable showable;
            if (rest <= 0)
                return false;
            else if ((showable = obj as IShowable) != null)
                return showable.Show(stringbuilder, ref rest, formatProvider);
            int oldLength = stringbuilder.Length;
            stringbuilder.AppendFormat(formatProvider, "{0}", obj);
            rest -= (stringbuilder.Length - oldLength);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="showable"></param>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public static String ShowString(IShowable showable, String format, IFormatProvider formatProvider)
        {
            int rest = maxLength(format);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            showable.Show(sb, ref rest, formatProvider);
            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        static int maxLength(String format)
        {
            //TODO: validate format string
            if (format == null)
                return 80;
            if (format.Length > 1 && format.StartsWith("L"))
            {
                return int.Parse(format.Substring(1));
            }
            else
                return int.MaxValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="stringbuilder"></param>
        /// <param name="rest"></param>
        /// <param name="formatProvider"></param>
        /// <returns>True if collection was shown completely</returns>
        public static bool ShowCollectionValue<T>(ICollectionValue<T> items, System.Text.StringBuilder stringbuilder, ref int rest, IFormatProvider formatProvider)
        {
            string startdelim = "{ ", enddelim = " }";
            bool showIndexes = false;
            bool showMultiplicities = false;
            //TODO: do not test here at run time, but select code at compile time
            //      perhaps by delivering the print type to this metod
            IList<T> list;
            ICollection<T> coll = items as ICollection<T>;
            if ((list = items as IList<T>) != null)
            {
                startdelim = "[ ";
                enddelim = " ]";
                //TODO: should have been (items as IIndexed<T>).IndexingSpeed
                showIndexes = list.IndexingSpeed == Speed.Constant;
            }
            else if (coll != null)
            {
                if (coll.AllowsDuplicates)
                {
                    startdelim = "{{ ";
                    enddelim = " }}";
                    if (coll.DuplicatesByCounting)
                        showMultiplicities = true;
                }
            }

            stringbuilder.Append(startdelim);
            rest -= 2 * startdelim.Length;
            bool first = true;
            bool complete = true;
            int index = 0;

            if (showMultiplicities)
            {
                foreach (KeyValuePair<T, int> p in coll.ItemMultiplicities())
                {
                    complete = false;
                    if (rest <= 0)
                        break;
                    if (first)
                        first = false;
                    else
                    {
                        stringbuilder.Append(", ");
                        rest -= 2;
                    }
                    if (complete = Showing.Show(p.Key, stringbuilder, ref rest, formatProvider))
                    {
                        string multiplicityString = string.Format("(*{0})", p.Value);
                        stringbuilder.Append(multiplicityString);
                        rest -= multiplicityString.Length;
                    }
                }
            }
            else
            {
                foreach (T x in items)
                {
                    complete = false;
                    if (rest <= 0)
                        break;
                    if (first)
                        first = false;
                    else
                    {
                        stringbuilder.Append(", ");
                        rest -= 2;
                    }
                    if (showIndexes)
                    {
                        string indexString = string.Format("{0}:", index++);
                        stringbuilder.Append(indexString);
                        rest -= indexString.Length;
                    }
                    complete = Showing.Show(x, stringbuilder, ref rest, formatProvider);
                }
            }
            if (!complete)
            {
                stringbuilder.Append("...");
                rest -= 3;
            }
            stringbuilder.Append(enddelim);
            return complete;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// 
        /// <param name="dictionary"></param>
        /// <param name="stringbuilder"></param>
        /// <param name="formatProvider"></param>
        /// <param name="rest"></param>
        /// <returns></returns>
        public static bool ShowDictionary<K, V>(IDictionary<K, V> dictionary, System.Text.StringBuilder stringbuilder, ref int rest, IFormatProvider formatProvider)
        {
            bool sorted = dictionary is ISortedDictionary<K, V>;
            stringbuilder.Append(sorted ? "[ " : "{ ");
            rest -= 4;				   // Account for "( " and " )"
            bool first = true;
            bool complete = true;

            foreach (KeyValuePair<K, V> p in dictionary)
            {
                complete = false;
                if (rest <= 0)
                    break;
                if (first)
                    first = false;
                else
                {
                    stringbuilder.Append(", ");
                    rest -= 2;
                }
                complete = Showing.Show(p, stringbuilder, ref rest, formatProvider);
            }
            if (!complete)
            {
                stringbuilder.Append("...");
                rest -= 3;
            }
            stringbuilder.Append(sorted ? " ]" : " }");
            return complete;
        }
    }

    /// <summary>
    /// Logging module
    /// </summary>
    public static class Logger
    {
        private static Action<string> _log;

        /// <summary>
        /// Gets or sets the log.
        /// </summary>
        /// <example>The following is an example of assigning a observer to the logging module:
        ///   <code>
        ///     Logger.Log = x => Console.WriteLine(x);
        ///   </code>
        /// </example>
        /// <remarks>
        /// If Log is not set it will return a dummy action
        /// <c>x => { return; })</c>
        /// eliminating the need for null-reference checks.
        /// </remarks>
        /// <value>
        /// The log.
        /// </value>
        public static Action<string> Log
        {
            get { return _log ?? (x => { return; }); }
            set { _log = value; }
        }
    }








    /*************************************************************************/
    /// <summary>
    /// A dictionary with keys of type K and values of type V. Equivalent to a
    /// finite partial map from K to V.
    /// </summary>
    public interface IDictionary<K, V> : ICollectionValue<KeyValuePair<K, V>>
    {
        /// <summary>
        /// The key equalityComparer.
        /// </summary>
        /// <value></value>
        System.Collections.Generic.IEqualityComparer<K> EqualityComparer { get; }

        /// <summary>
        /// Indexer for dictionary.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if no entry is found. </exception>
        /// <value>The value corresponding to the key</value>
        V this[K key] { get; set; }


        /// <summary>
        /// 
        /// </summary>
        /// <value>True if dictionary is read-only</value>
        bool IsReadOnly { get; }


        /// <summary>
        /// 
        /// </summary>
        /// <value>A collection containing all the keys of the dictionary</value>
        ICollectionValue<K> Keys { get; }


        /// <summary>
        /// 
        /// </summary>
        /// <value>A collection containing all the values of the dictionary</value>
        ICollectionValue<V> Values { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <value>A delegate of type <see cref="T:Func`2"/> defining the partial function from K to V give by the dictionary.</value>
        Func<K, V> Func { get; }


        //TODO: resolve inconsistency: Add thows exception if key already there, AddAll ignores keys already There?
        /// <summary>
        /// Add a new (key, value) pair (a mapping) to the dictionary.
        /// </summary>
        /// <exception cref="DuplicateNotAllowedException"> if there already is an entry with the same key. </exception>>
        /// <param name="key">Key to add</param>
        /// <param name="val">Value to add</param>
        void Add(K key, V val);

        /// <summary>
        /// Add the entries from a collection of <see cref="T:C5.KeyValuePair`2"/> pairs to this dictionary.
        /// </summary>
        /// <exception cref="DuplicateNotAllowedException"> 
        /// If the input contains duplicate keys or a key already present in this dictionary.</exception>
        /// <param name="entries"></param>
        void AddAll<U, W>(SCG.IEnumerable<KeyValuePair<U, W>> entries)
            where U : K
            where W : V
          ;

        /// <summary>
        /// The value is symbolic indicating the type of asymptotic complexity
        /// in terms of the size of this collection (worst-case or amortized as
        /// relevant). 
        /// <para>See <see cref="T:C5.Speed"/> for the set of symbols.</para>
        /// </summary>
        /// <value>A characterization of the speed of lookup operations
        /// (<code>Contains()</code> etc.) of the implementation of this dictionary.</value>
        Speed ContainsSpeed { get; }

        /// <summary>
        /// Check whether this collection contains all the values in another collection.
        /// If this collection has bag semantics (<code>AllowsDuplicates==true</code>)
        /// the check is made with respect to multiplicities, else multiplicities
        /// are not taken into account.
        /// </summary>
        /// <param name="items">The </param>
        /// <returns>True if all values in <code>items</code>is in this collection.</returns>
        bool ContainsAll<H>(SCG.IEnumerable<H> items) where H : K;

        /// <summary>
        /// Remove an entry with a given key from the dictionary
        /// </summary>
        /// <param name="key">The key of the entry to remove</param>
        /// <returns>True if an entry was found (and removed)</returns>
        bool Remove(K key);


        /// <summary>
        /// Remove an entry with a given key from the dictionary and report its value.
        /// </summary>
        /// <param name="key">The key of the entry to remove</param>
        /// <param name="val">On exit, the value of the removed entry</param>
        /// <returns>True if an entry was found (and removed)</returns>
        bool Remove(K key, out V val);


        /// <summary>
        /// Remove all entries from the dictionary
        /// </summary>
        void Clear();


        /// <summary>
        /// Check if there is an entry with a specified key
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <returns>True if key was found</returns>
        bool Contains(K key);

        /// <summary>
        /// Check if there is an entry with a specified key and report the corresponding
        /// value if found. This can be seen as a safe form of "val = this[key]".
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <param name="val">On exit, the value of the entry</param>
        /// <returns>True if key was found</returns>
        bool Find(ref K key, out V val);


        /// <summary>
        /// Look for a specific key in the dictionary and if found replace the value with a new one.
        /// This can be seen as a non-adding version of "this[key] = val".
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <param name="val">The new value</param>
        /// <returns>True if key was found</returns>
        bool Update(K key, V val);          //no-adding				    	


        /// <summary>
        /// Look for a specific key in the dictionary and if found replace the value with a new one.
        /// This can be seen as a non-adding version of "this[key] = val" reporting the old value.
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <param name="val">The new value</param>
        /// <param name="oldval">The old value if any</param>
        /// <returns>True if key was found</returns>
        bool Update(K key, V val, out V oldval);          //no-adding				    	

        /// <summary>
        /// Look for a specific key in the dictionary. If found, report the corresponding value,
        /// else add an entry with the key and the supplied value.
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <param name="val">On entry the value to add if the key is not found.
        /// On exit the value found if any.</param>
        /// <returns>True if key was found</returns>
        bool FindOrAdd(K key, ref V val);   //mixture


        /// <summary>
        /// Update value in dictionary corresponding to key if found, else add new entry.
        /// More general than "this[key] = val;" by reporting if key was found.
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <param name="val">The value to add or replace with.</param>
        /// <returns>True if key was found and value updated.</returns>
        bool UpdateOrAdd(K key, V val);


        /// <summary>
        /// Update value in dictionary corresponding to key if found, else add new entry.
        /// More general than "this[key] = val;" by reporting if key was found.
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <param name="val">The value to add or replace with.</param>
        /// <param name="oldval">The old value if any</param>
        /// <returns>True if key was found and value updated.</returns>
        bool UpdateOrAdd(K key, V val, out V oldval);


        /// <summary>
        /// Check the integrity of the internal data structures of this dictionary.
        /// Only available in DEBUG builds???
        /// </summary>
        /// <returns>True if check does not fail.</returns>
        bool Check();
    }

    /// <summary>
    /// A dictionary with sorted keys.
    /// </summary>
    public interface ISortedDictionary<K, V> : IDictionary<K, V>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        new ISorted<K> Keys { get; }

        /// <summary>
        /// Find the current least item of this sorted collection.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        /// <returns>The least item.</returns>
        KeyValuePair<K, V> FindMin();


        /// <summary>
        /// Remove the least item from this sorted collection.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        /// <returns>The removed item.</returns>
        KeyValuePair<K, V> DeleteMin();


        /// <summary>
        /// Find the current largest item of this sorted collection.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        /// <returns>The largest item.</returns>
        KeyValuePair<K, V> FindMax();


        /// <summary>
        /// Remove the largest item from this sorted collection.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        /// <returns>The removed item.</returns>
        KeyValuePair<K, V> DeleteMax();

        /// <summary>
        /// The key comparer used by this dictionary.
        /// </summary>
        /// <value></value>
        System.Collections.Generic.IComparer<K> Comparer { get; }

        /// <summary>
        /// Find the entry in the dictionary whose key is the
        /// predecessor of the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="res">The predecessor, if any</param>
        /// <returns>True if key has a predecessor</returns>
        bool TryPredecessor(K key, out KeyValuePair<K, V> res);

        /// <summary>
        /// Find the entry in the dictionary whose key is the
        /// successor of the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="res">The successor, if any</param>
        /// <returns>True if the key has a successor</returns>
        bool TrySuccessor(K key, out KeyValuePair<K, V> res);

        /// <summary>
        /// Find the entry in the dictionary whose key is the
        /// weak predecessor of the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="res">The predecessor, if any</param>
        /// <returns>True if key has a weak predecessor</returns>
        bool TryWeakPredecessor(K key, out KeyValuePair<K, V> res);

        /// <summary>
        /// Find the entry in the dictionary whose key is the
        /// weak successor of the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="res">The weak successor, if any</param>
        /// <returns>True if the key has a weak successor</returns>
        bool TryWeakSuccessor(K key, out KeyValuePair<K, V> res);

        /// <summary>
        /// Find the entry with the largest key less than a given key.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if there is no such entry. </exception>
        /// <param name="key">The key to compare to</param>
        /// <returns>The entry</returns>
        KeyValuePair<K, V> Predecessor(K key);


        /// <summary>
        /// Find the entry with the least key greater than a given key.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if there is no such entry. </exception>
        /// <param name="key">The key to compare to</param>
        /// <returns>The entry</returns>
        KeyValuePair<K, V> Successor(K key);


        /// <summary>
        /// Find the entry with the largest key less than or equal to a given key.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if there is no such entry. </exception>
        /// <param name="key">The key to compare to</param>
        /// <returns>The entry</returns>
        KeyValuePair<K, V> WeakPredecessor(K key);


        /// <summary>
        /// Find the entry with the least key greater than or equal to a given key.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if there is no such entry. </exception>
        /// <param name="key">The key to compare to</param>
        /// <returns>The entry</returns>
        KeyValuePair<K, V> WeakSuccessor(K key);

        /// <summary>
        /// Given a "cut" function from the items of the sorted collection to <code>int</code>
        /// whose only sign changes when going through items in increasing order
        /// can be 
        /// <list>
        /// <item>from positive to zero</item>
        /// <item>from positive to negative</item>
        /// <item>from zero to negative</item>
        /// </list>
        /// The "cut" function is supplied as the <code>CompareTo</code> method 
        /// of an object <code>c</code> implementing 
        /// <code>IComparable&lt;K&gt;</code>. 
        /// A typical example is the case where <code>K</code> is comparable and 
        /// <code>c</code> is itself of type <code>K</code>.
        /// <para>This method performs a search in the sorted collection for the ranges in which the
        /// "cut" function is negative, zero respectively positive. If <code>K</code> is comparable
        /// and <code>c</code> is of type <code>K</code>, this is a safe way (no exceptions thrown) 
        /// to find predecessor and successor of <code>c</code>.
        /// </para>
        /// <para> If the supplied cut function does not satisfy the sign-change condition, 
        /// the result of this call is undefined.
        /// </para>
        /// 
        /// </summary>
        /// <param name="cutFunction">The cut function <code>K</code> to <code>int</code>, given
        /// by the <code>CompareTo</code> method of an object implementing 
        /// <code>IComparable&lt;K&gt;</code>.</param>
        /// <param name="lowEntry">Returns the largest item in the collection, where the
        /// cut function is positive (if any).</param>
        /// <param name="lowIsValid">Returns true if the cut function is positive somewhere
        /// on this collection.</param>
        /// <param name="highEntry">Returns the least item in the collection, where the
        /// cut function is negative (if any).</param>
        /// <param name="highIsValid">Returns true if the cut function is negative somewhere
        /// on this collection.</param>
        /// <returns>True if the cut function is zero somewhere
        /// on this collection.</returns>
        bool Cut(IComparable<K> cutFunction, out KeyValuePair<K, V> lowEntry, out bool lowIsValid, out KeyValuePair<K, V> highEntry, out bool highIsValid);

        /// <summary>
        /// Query this sorted collection for items greater than or equal to a supplied value.
        /// <para>The returned collection is not a copy but a view into the collection.</para>
        /// <para>The view is fragile in the sense that changes to the underlying collection will 
        /// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        /// </summary>
        /// <param name="bot">The lower bound (inclusive).</param>
        /// <returns>The result directed collection.</returns>
        IDirectedEnumerable<KeyValuePair<K, V>> RangeFrom(K bot);


        /// <summary>
        /// Query this sorted collection for items between two supplied values.
        /// <para>The returned collection is not a copy but a view into the collection.</para>
        /// <para>The view is fragile in the sense that changes to the underlying collection will 
        /// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        /// </summary>
        /// <param name="lowerBound">The lower bound (inclusive).</param>
        /// <param name="upperBound">The upper bound (exclusive).</param>
        /// <returns>The result directed collection.</returns>
        IDirectedEnumerable<KeyValuePair<K, V>> RangeFromTo(K lowerBound, K upperBound);


        /// <summary>
        /// Query this sorted collection for items less than a supplied value.
        /// <para>The returned collection is not a copy but a view into the collection.</para>
        /// <para>The view is fragile in the sense that changes to the underlying collection will 
        /// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        /// </summary>
        /// <param name="top">The upper bound (exclusive).</param>
        /// <returns>The result directed collection.</returns>
        IDirectedEnumerable<KeyValuePair<K, V>> RangeTo(K top);


        /// <summary>
        /// Create a directed collection with the same items as this collection.
        /// <para>The returned collection is not a copy but a view into the collection.</para>
        /// <para>The view is fragile in the sense that changes to the underlying collection will 
        /// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        /// </summary>
        /// <returns>The result directed collection.</returns>
        IDirectedCollectionValue<KeyValuePair<K, V>> RangeAll();


        //TODO: remove now that we assume that we can check the sorting order?
        /// <summary>
        /// Add all the items from another collection with an enumeration order that 
        /// is increasing in the items.
        /// </summary>
        /// <exception cref="ArgumentException"> if the enumerated items turns out
        /// not to be in increasing order.</exception>
        /// <param name="items">The collection to add.</param>
        void AddSorted(SCG.IEnumerable<KeyValuePair<K, V>> items);


        /// <summary>
        /// Remove all items of this collection above or at a supplied threshold.
        /// </summary>
        /// <param name="low">The lower threshold (inclusive).</param>
        void RemoveRangeFrom(K low);


        /// <summary>
        /// Remove all items of this collection between two supplied thresholds.
        /// </summary>
        /// <param name="low">The lower threshold (inclusive).</param>
        /// <param name="hi">The upper threshold (exclusive).</param>
        void RemoveRangeFromTo(K low, K hi);


        /// <summary>
        /// Remove all items of this collection below a supplied threshold.
        /// </summary>
        /// <param name="hi">The upper threshold (exclusive).</param>
        void RemoveRangeTo(K hi);
    }

    /// <summary>
    /// A collection where items are maintained in sorted order together
    /// with their indexes in that order.
    /// </summary>
    public interface IIndexedSorted<T> : ISorted<T>, IIndexed<T>
    {
        /// <summary>
        /// Determine the number of items at or above a supplied threshold.
        /// </summary>
        /// <param name="bot">The lower bound (inclusive)</param>
        /// <returns>The number of matching items.</returns>
        int CountFrom(T bot);


        /// <summary>
        /// Determine the number of items between two supplied thresholds.
        /// </summary>
        /// <param name="bot">The lower bound (inclusive)</param>
        /// <param name="top">The upper bound (exclusive)</param>
        /// <returns>The number of matching items.</returns>
        int CountFromTo(T bot, T top);


        /// <summary>
        /// Determine the number of items below a supplied threshold.
        /// </summary>
        /// <param name="top">The upper bound (exclusive)</param>
        /// <returns>The number of matching items.</returns>
        int CountTo(T top);


        /// <summary>
        /// Query this sorted collection for items greater than or equal to a supplied value.
        /// </summary>
        /// <param name="bot">The lower bound (inclusive).</param>
        /// <returns>The result directed collection.</returns>
        new IDirectedCollectionValue<T> RangeFrom(T bot);


        /// <summary>
        /// Query this sorted collection for items between two supplied values.
        /// </summary>
        /// <param name="bot">The lower bound (inclusive).</param>
        /// <param name="top">The upper bound (exclusive).</param>
        /// <returns>The result directed collection.</returns>
        new IDirectedCollectionValue<T> RangeFromTo(T bot, T top);


        /// <summary>
        /// Query this sorted collection for items less than a supplied value.
        /// </summary>
        /// <param name="top">The upper bound (exclusive).</param>
        /// <returns>The result directed collection.</returns>
        new IDirectedCollectionValue<T> RangeTo(T top);


        /// <summary>
        /// Create a new indexed sorted collection consisting of the items of this
        /// indexed sorted collection satisfying a certain predicate.
        /// </summary>
        /// <param name="predicate">The filter delegate defining the predicate.</param>
        /// <returns>The new indexed sorted collection.</returns>
        IIndexedSorted<T> FindAll(Func<T, bool> predicate);


        /// <summary>
        /// Create a new indexed sorted collection consisting of the results of
        /// mapping all items of this list.
        /// <exception cref="ArgumentException"/> if the map is not increasing over 
        /// the items of this collection (with respect to the two given comparison 
        /// relations).
        /// </summary>
        /// <param name="mapper">The delegate definging the map.</param>
        /// <param name="comparer">The comparion relation to use for the result.</param>
        /// <returns>The new sorted collection.</returns>
        IIndexedSorted<V> Map<V>(Func<T, V> mapper, System.Collections.Generic.IComparer<V> comparer);
    }

    /// <summary>
    /// The type of a sorted collection with persistence
    /// </summary>
    public interface IPersistentSorted<T> : ISorted<T>, IDisposable
    {
        /// <summary>
        /// Make a (read-only) snap shot of this collection.
        /// </summary>
        /// <returns>The snap shot.</returns>
        ISorted<T> Snapshot();
    }

    /// <summary>
    /// A generic collection to which one may add items. This is just the intersection
    /// of the main stream generic collection interfaces and the priority queue interface,
    /// <see cref="T:C5.ICollection`1"/> and <see cref="T:C5.IPriorityQueue`1"/>.
    /// </summary>
    public interface IExtensible<T> : ICollectionValue<T>
    {
        /// <summary>
        /// If true any call of an updating operation will throw an
        /// <code>ReadOnlyCollectionException</code>
        /// </summary>
        /// <value>True if this collection is read-only.</value>
        bool IsReadOnly { get; }

        //TODO: wonder where the right position of this is
        /// <summary>
        /// 
        /// </summary>
        /// <value>False if this collection has set semantics, true if bag semantics.</value>
        bool AllowsDuplicates { get; }

        //TODO: wonder where the right position of this is. And the semantics.
        /// <summary>
        /// (Here should be a discussion of the role of equalityComparers. Any ). 
        /// </summary>
        /// <value>The equalityComparer used by this collection to check equality of items. 
        /// Or null (????) if collection does not check equality at all or uses a comparer.</value>
        System.Collections.Generic.IEqualityComparer<T> EqualityComparer { get; }

        //ItemEqualityTypeEnum ItemEqualityType {get ;}

        //TODO: find a good name

        /// <summary>
        /// By convention this is true for any collection with set semantics.
        /// </summary>
        /// <value>True if only one representative of a group of equal items 
        /// is kept in the collection together with the total count.</value>
        bool DuplicatesByCounting { get; }

        /// <summary>
        /// Add an item to this collection if possible. If this collection has set
        /// semantics, the item will be added if not already in the collection. If
        /// bag semantics, the item will always be added.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>True if item was added.</returns>
        bool Add(T item);

        /// <summary>
        /// Add the elements from another collection with a more specialized item type 
        /// to this collection. If this
        /// collection has set semantics, only items not already in the collection
        /// will be added.
        /// </summary>
        /// <param name="items">The items to add</param>
        void AddAll(SCG.IEnumerable<T> items);

        //void Clear(); // for priority queue
        //int Count why not?
        /// <summary>
        /// Check the integrity of the internal data structures of this collection.
        /// <i>This is only relevant for developers of the library</i>
        /// </summary>
        /// <returns>True if check was passed.</returns>
        bool Check();
    }

    /// <summary>
    /// The simplest interface of a main stream generic collection
    /// with lookup, insertion and removal operations. 
    /// </summary>
    public interface ICollection<T> : IExtensible<T>, System.Collections.Generic.ICollection<T>
    {
        //This is somewhat similar to the RandomAccess marker itf in java
        /// <summary>
        /// The value is symbolic indicating the type of asymptotic complexity
        /// in terms of the size of this collection (worst-case or amortized as
        /// relevant). 
        /// <para>See <see cref="T:C5.Speed"/> for the set of symbols.</para>
        /// </summary>
        /// <value>A characterization of the speed of lookup operations
        /// (<code>Contains()</code> etc.) of the implementation of this collection.</value>
        Speed ContainsSpeed { get; }

        /// <summary>
        /// </summary>
        /// <value>The number of items in this collection</value>
        new int Count { get; }

        /// <summary>
        /// If true any call of an updating operation will throw an
        /// <code>ReadOnlyCollectionException</code>
        /// </summary>
        /// <value>True if this collection is read-only.</value>
        new bool IsReadOnly { get; }

        /// <summary>
        /// Add an item to this collection if possible. If this collection has set
        /// semantics, the item will be added if not already in the collection. If
        /// bag semantics, the item will always be added.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>True if item was added.</returns>
        new bool Add(T item);

        /// <summary>
        /// Copy the items of this collection to a contiguous part of an array.
        /// </summary>
        /// <param name="array">The array to copy to</param>
        /// <param name="index">The index at which to copy the first item</param>
        new void CopyTo(T[] array, int index);

        /// <summary>
        /// The unordered collection hashcode is defined as the sum of 
        /// <code>h(hashcode(item))</code> over the items
        /// of the collection, where the function <code>h</code> is a function from 
        /// int to int of the form <code> t -> (a0*t+b0)^(a1*t+b1)^(a2*t+b2)</code>, where 
        /// the ax and bx are the same for all collection classes. 
        /// <para>The current implementation uses fixed values for the ax and bx, 
        /// specified as constants in the code.</para>
        /// </summary>
        /// <returns>The unordered hashcode of this collection.</returns>
        int GetUnsequencedHashCode();


        /// <summary>
        /// Compare the contents of this collection to another one without regards to
        /// the sequence order. The comparison will use this collection's itemequalityComparer
        /// to compare individual items.
        /// </summary>
        /// <param name="otherCollection">The collection to compare to.</param>
        /// <returns>True if this collection and that contains the same items.</returns>
        bool UnsequencedEquals(ICollection<T> otherCollection);


        /// <summary>
        /// Check if this collection contains (an item equivalent to according to the
        /// itemequalityComparer) a particular value.
        /// </summary>
        /// <param name="item">The value to check for.</param>
        /// <returns>True if the items is in this collection.</returns>
        new bool Contains(T item);


        /// <summary>
        /// Count the number of items of the collection equal to a particular value.
        /// Returns 0 if and only if the value is not in the collection.
        /// </summary>
        /// <param name="item">The value to count.</param>
        /// <returns>The number of copies found.</returns>
        int ContainsCount(T item);


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        ICollectionValue<T> UniqueItems();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        ICollectionValue<KeyValuePair<T, int>> ItemMultiplicities();

        /// <summary>
        /// Check whether this collection contains all the values in another collection.
        /// If this collection has bag semantics (<code>AllowsDuplicates==true</code>)
        /// the check is made with respect to multiplicities, else multiplicities
        /// are not taken into account.
        /// </summary>
        /// <param name="items">The </param>
        /// <returns>True if all values in <code>items</code>is in this collection.</returns>
        bool ContainsAll(SCG.IEnumerable<T> items);


        /// <summary>
        /// Check if this collection contains an item equivalent according to the
        /// itemequalityComparer to a particular value. If so, return in the ref argument (a
        /// binary copy of) the actual value found.
        /// </summary>
        /// <param name="item">The value to look for.</param>
        /// <returns>True if the items is in this collection.</returns>
        bool Find(ref T item);


        //This should probably just be bool Add(ref T item); !!!
        /// <summary>
        /// Check if this collection contains an item equivalent according to the
        /// itemequalityComparer to a particular value. If so, return in the ref argument (a
        /// binary copy of) the actual value found. Else, add the item to the collection.
        /// </summary>
        /// <param name="item">The value to look for.</param>
        /// <returns>True if the item was found (hence not added).</returns>
        bool FindOrAdd(ref T item);


        /// <summary>
        /// Check if this collection contains an item equivalent according to the
        /// itemequalityComparer to a particular value. If so, update the item in the collection 
        /// with a (binary copy of) the supplied value. If the collection has bag semantics,
        /// it depends on the value of DuplicatesByCounting if this updates all equivalent copies in
        /// the collection or just one.
        /// </summary>
        /// <param name="item">Value to update.</param>
        /// <returns>True if the item was found and hence updated.</returns>
        bool Update(T item);

        /// <summary>
        /// Check if this collection contains an item equivalent according to the
        /// itemequalityComparer to a particular value. If so, update the item in the collection 
        /// with a (binary copy of) the supplied value. If the collection has bag semantics,
        /// it depends on the value of DuplicatesByCounting if this updates all equivalent copies in
        /// the collection or just one.
        /// </summary>
        /// <param name="item">Value to update.</param>
        /// <param name="olditem">On output the olditem, if found.</param>
        /// <returns>True if the item was found and hence updated.</returns>
        bool Update(T item, out T olditem);


        /// <summary>
        /// Check if this collection contains an item equivalent according to the
        /// itemequalityComparer to a particular value. If so, update the item in the collection 
        /// to with a binary copy of the supplied value; else add the value to the collection. 
        /// </summary>
        /// <param name="item">Value to add or update.</param>
        /// <returns>True if the item was found and updated (hence not added).</returns>
        bool UpdateOrAdd(T item);


        /// <summary>
        /// Check if this collection contains an item equivalent according to the
        /// itemequalityComparer to a particular value. If so, update the item in the collection 
        /// to with a binary copy of the supplied value; else add the value to the collection. 
        /// </summary>
        /// <param name="item">Value to add or update.</param>
        /// <param name="olditem">On output the olditem, if found.</param>
        /// <returns>True if the item was found and updated (hence not added).</returns>
        bool UpdateOrAdd(T item, out T olditem);

        /// <summary>
        /// Remove a particular item from this collection. If the collection has bag
        /// semantics only one copy equivalent to the supplied item is removed. 
        /// </summary>
        /// <param name="item">The value to remove.</param>
        /// <returns>True if the item was found (and removed).</returns>
        new bool Remove(T item);


        /// <summary>
        /// Remove a particular item from this collection if found. If the collection
        /// has bag semantics only one copy equivalent to the supplied item is removed,
        /// which one is implementation dependent. 
        /// If an item was removed, report a binary copy of the actual item removed in 
        /// the argument.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        /// <param name="removeditem">The value removed if any.</param>
        /// <returns>True if the item was found (and removed).</returns>
        bool Remove(T item, out T removeditem);


        /// <summary>
        /// Remove all items equivalent to a given value.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        void RemoveAllCopies(T item);


        /// <summary>
        /// Remove all items in another collection from this one. If this collection
        /// has bag semantics, take multiplicities into account.
        /// </summary>
        /// <param name="items">The items to remove.</param>
        void RemoveAll(SCG.IEnumerable<T> items);

        //void RemoveAll(Func<T, bool> predicate);

        /// <summary>
        /// Remove all items from this collection.
        /// </summary>
        new void Clear();


        /// <summary>
        /// Remove all items not in some other collection from this one. If this collection
        /// has bag semantics, take multiplicities into account.
        /// </summary>
        /// <param name="items">The items to retain.</param>
        void RetainAll(SCG.IEnumerable<T> items);

        //void RetainAll(Func<T, bool> predicate);
        //IDictionary<T> UniqueItems()
    }

    /// <summary>
    /// A generic collection, that can be enumerated backwards.
    /// </summary>
    public interface IDirectedEnumerable<T> : SCG.IEnumerable<T> // TODO: Type parameter should be 'out T' when Silverlight supports is (version 5 and onwards)
    {
        /// <summary>
        /// Create a collection containing the same items as this collection, but
        /// whose enumerator will enumerate the items backwards. The new collection
        /// will become invalid if the original is modified. Method typically used as in
        /// <code>foreach (T x in coll.Backwards()) {...}</code>
        /// </summary>
        /// <returns>The backwards collection.</returns>
        IDirectedEnumerable<T> Backwards();

        /// <summary>
        /// <code>Forwards</code> if same, else <code>Backwards</code>
        /// </summary>
        /// <value>The enumeration direction relative to the original collection.</value>
        EnumerationDirection Direction { get; }
    }

    /// <summary>
    /// A sized generic collection, that can be enumerated backwards.
    /// </summary>
    public interface IDirectedCollectionValue<T> : ICollectionValue<T>, IDirectedEnumerable<T>
    {
        /// <summary>
        /// Create a collection containing the same items as this collection, but
        /// whose enumerator will enumerate the items backwards. The new collection
        /// will become invalid if the original is modified. Method typically used as in
        /// <code>foreach (T x in coll.Backwards()) {...}</code>
        /// </summary>
        /// <returns>The backwards collection.</returns>
        new IDirectedCollectionValue<T> Backwards();

        /// <summary>
        /// Check if there exists an item  that satisfies a
        /// specific predicate in this collection and return the first one in enumeration order.
        /// </summary>
        /// <param name="predicate">A delegate 
        /// (<see cref="T:Func`2"/> with <code>R == bool</code>) defining the predicate</param>
        /// <param name="item"></param>
        /// <returns>True is such an item exists</returns>
        bool FindLast(Func<T, bool> predicate, out T item);
    }

    /// <summary>
    /// A generic collection that may be enumerated and can answer
    /// efficiently how many items it contains. Like <code>IEnumerable&lt;T&gt;</code>,
    /// this interface does not prescribe any operations to initialize or update the 
    /// collection. The main usage for this interface is to be the return type of 
    /// query operations on generic collection.
    /// </summary>
    public interface ICollectionValue<T> : SCG.IEnumerable<T>, IShowable
    {
        /// <summary>
        /// A flag bitmap of the events subscribable to by this collection.
        /// </summary>
        /// <value></value>
        EventTypeEnum ListenableEvents { get; }

        /// <summary>
        /// A flag bitmap of the events currently subscribed to by this collection.
        /// </summary>
        /// <value></value>
        EventTypeEnum ActiveEvents { get; }

        /// <summary>
        /// The change event. Will be raised for every change operation on the collection.
        /// </summary>
        event CollectionChangedHandler<T> CollectionChanged;

        /// <summary>
        /// The change event. Will be raised for every clear operation on the collection.
        /// </summary>
        event CollectionClearedHandler<T> CollectionCleared;

        /// <summary>
        /// The item added  event. Will be raised for every individual addition to the collection.
        /// </summary>
        event ItemsAddedHandler<T> ItemsAdded;

        /// <summary>
        /// The item inserted  event. Will be raised for every individual insertion to the collection.
        /// </summary>
        event ItemInsertedHandler<T> ItemInserted;

        /// <summary>
        /// The item removed event. Will be raised for every individual removal from the collection.
        /// </summary>
        event ItemsRemovedHandler<T> ItemsRemoved;

        /// <summary>
        /// The item removed at event. Will be raised for every individual removal at from the collection.
        /// </summary>
        event ItemRemovedAtHandler<T> ItemRemovedAt;

        /// <summary>
        /// 
        /// </summary>
        /// <value>True if this collection is empty.</value>
        bool IsEmpty { get; }

        /// <summary>
        /// </summary>
        /// <value>The number of items in this collection</value>
        int Count { get; }

        /// <summary>
        /// The value is symbolic indicating the type of asymptotic complexity
        /// in terms of the size of this collection (worst-case or amortized as
        /// relevant).
        /// </summary>
        /// <value>A characterization of the speed of the 
        /// <code>Count</code> property in this collection.</value>
        Speed CountSpeed { get; }

        /// <summary>
        /// Copy the items of this collection to a contiguous part of an array.
        /// </summary>
        /// <param name="array">The array to copy to</param>
        /// <param name="index">The index at which to copy the first item</param>
        void CopyTo(T[] array, int index);

        /// <summary>
        /// Create an array with the items of this collection (in the same order as an
        /// enumerator would output them).
        /// </summary>
        /// <returns>The array</returns>
        T[] ToArray();

        /// <summary>
        /// Apply a delegate to all items of this collection.
        /// </summary>
        /// <param name="action">The delegate to apply</param>
        void Apply(Action<T> action);


        /// <summary>
        /// Check if there exists an item  that satisfies a
        /// specific predicate in this collection.
        /// </summary>
        /// <param name="predicate">A  delegate 
        /// (<see cref="T:Func`2"/> with <code>R == bool</code>) defining the predicate</param>
        /// <returns>True is such an item exists</returns>
        bool Exists(Func<T, bool> predicate);

        /// <summary>
        /// Check if there exists an item  that satisfies a
        /// specific predicate in this collection and return the first one in enumeration order.
        /// </summary>
        /// <param name="predicate">A delegate 
        /// (<see cref="T:Func`2"/> with <code>R == bool</code>) defining the predicate</param>
        /// <param name="item"></param>
        /// <returns>True is such an item exists</returns>
        bool Find(Func<T, bool> predicate, out T item);


        /// <summary>
        /// Check if all items in this collection satisfies a specific predicate.
        /// </summary>
        /// <param name="predicate">A delegate 
        /// (<see cref="T:Func`2"/> with <code>R == bool</code>) defining the predicate</param>
        /// <returns>True if all items satisfies the predicate</returns>
        bool All(Func<T, bool> predicate);

        /// <summary>
        /// Choose some item of this collection. 
        /// <para>Implementations must assure that the item 
        /// returned may be efficiently removed.</para>
        /// <para>Implementors may decide to implement this method in a way such that repeated
        /// calls do not necessarily give the same result, i.e. so that the result of the following 
        /// test is undetermined:
        /// <code>coll.Choose() == coll.Choose()</code></para>
        /// </summary>
        /// <exception cref="NoSuchItemException">if collection is empty.</exception>
        /// <returns></returns>
        T Choose();

        /// <summary>
        /// Create an enumerable, enumerating the items of this collection that satisfies 
        /// a certain condition.
        /// </summary>
        /// <param name="filter">The T->bool filter delegate defining the condition</param>
        /// <returns>The filtered enumerable</returns>
        SCG.IEnumerable<T> Filter(Func<T, bool> filter);
    }

    /// <summary>
    /// <i>(Describe usage of "L:300" format string.)</i>
    /// </summary>
    public interface IShowable : IFormattable
    {
        //TODO: wonder if we should use TextWriters instead of StringBuilders?
        /// <summary>
        /// Format <code>this</code> using at most approximately <code>rest</code> chars and 
        /// append the result, possibly truncated, to stringbuilder.
        /// Subtract the actual number of used chars from <code>rest</code>.
        /// </summary>
        /// <param name="stringbuilder"></param>
        /// <param name="rest"></param>
        /// <param name="formatProvider"></param>
        /// <returns>True if the appended formatted string was complete (not truncated).</returns>
        bool Show(System.Text.StringBuilder stringbuilder, ref int rest, IFormatProvider formatProvider);
    }

    /// <summary>
    /// A sorted collection, i.e. a collection where items are maintained and can be searched for in sorted order.
    /// Thus the sequence order is given as a sorting order.
    /// 
    /// <para>The sorting order is defined by a comparer, an object of type IComparer&lt;T&gt; 
    /// (<see cref="T:C5.IComparer`1"/>). Implementors of this interface will normally let the user 
    /// define the comparer as an argument to a constructor. 
    /// Usually there will also be constructors without a comparer argument, in which case the 
    /// comparer should be the defalt comparer for the item type, <see cref="P:C5.Comparer`1.Default"/>.</para>
    /// 
    /// <para>The comparer of the sorted collection is available as the <code>System.Collections.Generic.Comparer</code> property 
    /// (<see cref="P:C5.ISorted`1.Comparer"/>).</para>
    /// 
    /// <para>The methods are grouped according to
    /// <list>
    /// <item>Extrema: report or report and delete an extremal item. This is reminiscent of simplified priority queues.</item>
    /// <item>Nearest neighbor: report predecessor or successor in the collection of an item. Cut belongs to this group.</item>
    /// <item>Range: report a view of a range of elements or remove all elements in a range.</item>
    /// <item>AddSorted: add a collection of items known to be sorted in the same order (should be faster) (to be removed?)</item>
    /// </list>
    /// </para>
    /// 
    /// <para>Since this interface extends ISequenced&lt;T&gt;, sorted collections will also have an 
    /// item equalityComparer (<see cref="P:C5.IExtensible`1.EqualityComparer"/>). This equalityComparer will not be used in connection with 
    /// the inner workings of the sorted collection, but will be used if the sorted collection is used as 
    /// an item in a collection of unsequenced or sequenced collections, 
    /// (<see cref="T:C5.ICollection`1"/> and <see cref="T:C5.ISequenced`1"/>)</para>
    /// 
    /// <para>Note that code may check if two sorted collections has the same sorting order 
    /// by checking if the Comparer properties are equal. This is done a few places in this library
    /// for optimization purposes.</para>
    /// </summary>
    public interface ISorted<T> : ISequenced<T>
    {
        /// <summary>
        /// Find the current least item of this sorted collection.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        /// <returns>The least item.</returns>
        T FindMin();


        /// <summary>
        /// Remove the least item from this sorted collection.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        /// <returns>The removed item.</returns>
        T DeleteMin();


        /// <summary>
        /// Find the current largest item of this sorted collection.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        /// <returns>The largest item.</returns>
        T FindMax();


        /// <summary>
        /// Remove the largest item from this sorted collection.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        /// <returns>The removed item.</returns>
        T DeleteMax();

        /// <summary>
        /// The comparer object supplied at creation time for this sorted collection.
        /// </summary>
        /// <value>The comparer</value>
        System.Collections.Generic.IComparer<T> Comparer { get; }

        /// <summary>
        /// Find the strict predecessor of item in the sorted collection,
        /// that is, the greatest item in the collection smaller than the item.
        /// </summary>
        /// <param name="item">The item to find the predecessor for.</param>
        /// <param name="res">The predecessor, if any; otherwise the default value for T.</param>
        /// <returns>True if item has a predecessor; otherwise false.</returns>
        bool TryPredecessor(T item, out T res);


        /// <summary>
        /// Find the strict successor of item in the sorted collection,
        /// that is, the least item in the collection greater than the supplied value.
        /// </summary>
        /// <param name="item">The item to find the successor for.</param>
        /// <param name="res">The successor, if any; otherwise the default value for T.</param>
        /// <returns>True if item has a successor; otherwise false.</returns>
        bool TrySuccessor(T item, out T res);


        /// <summary>
        /// Find the weak predecessor of item in the sorted collection,
        /// that is, the greatest item in the collection smaller than or equal to the item.
        /// </summary>
        /// <param name="item">The item to find the weak predecessor for.</param>
        /// <param name="res">The weak predecessor, if any; otherwise the default value for T.</param>
        /// <returns>True if item has a weak predecessor; otherwise false.</returns>
        bool TryWeakPredecessor(T item, out T res);


        /// <summary>
        /// Find the weak successor of item in the sorted collection,
        /// that is, the least item in the collection greater than or equal to the supplied value.
        /// </summary>
        /// <param name="item">The item to find the weak successor for.</param>
        /// <param name="res">The weak successor, if any; otherwise the default value for T.</param>
        /// <returns>True if item has a weak successor; otherwise false.</returns>
        bool TryWeakSuccessor(T item, out T res);


        /// <summary>
        /// Find the strict predecessor in the sorted collection of a particular value,
        /// that is, the largest item in the collection less than the supplied value.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if no such element exists (the
        /// supplied  value is less than or equal to the minimum of this collection.)</exception>
        /// <param name="item">The item to find the predecessor for.</param>
        /// <returns>The predecessor.</returns>
        T Predecessor(T item);


        /// <summary>
        /// Find the strict successor in the sorted collection of a particular value,
        /// that is, the least item in the collection greater than the supplied value.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if no such element exists (the
        /// supplied  value is greater than or equal to the maximum of this collection.)</exception>
        /// <param name="item">The item to find the successor for.</param>
        /// <returns>The successor.</returns>
        T Successor(T item);


        /// <summary>
        /// Find the weak predecessor in the sorted collection of a particular value,
        /// that is, the largest item in the collection less than or equal to the supplied value.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if no such element exists (the
        /// supplied  value is less than the minimum of this collection.)</exception>
        /// <param name="item">The item to find the weak predecessor for.</param>
        /// <returns>The weak predecessor.</returns>
        T WeakPredecessor(T item);


        /// <summary>
        /// Find the weak successor in the sorted collection of a particular value,
        /// that is, the least item in the collection greater than or equal to the supplied value.
        /// </summary>
        /// <exception cref="NoSuchItemException"> if no such element exists (the
        /// supplied  value is greater than the maximum of this collection.)</exception>
        ///<param name="item">The item to find the weak successor for.</param>
        /// <returns>The weak successor.</returns>
        T WeakSuccessor(T item);


        /// <summary>
        /// Given a "cut" function from the items of the sorted collection to <code>int</code>
        /// whose only sign changes when going through items in increasing order
        /// can be 
        /// <list>
        /// <item>from positive to zero</item>
        /// <item>from positive to negative</item>
        /// <item>from zero to negative</item>
        /// </list>
        /// The "cut" function is supplied as the <code>CompareTo</code> method 
        /// of an object <code>c</code> implementing 
        /// <code>IComparable&lt;T&gt;</code>. 
        /// A typical example is the case where <code>T</code> is comparable and 
        /// <code>cutFunction</code> is itself of type <code>T</code>.
        /// <para>This method performs a search in the sorted collection for the ranges in which the
        /// "cut" function is negative, zero respectively positive. If <code>T</code> is comparable
        /// and <code>c</code> is of type <code>T</code>, this is a safe way (no exceptions thrown) 
        /// to find predecessor and successor of <code>c</code>.
        /// </para>
        /// <para> If the supplied cut function does not satisfy the sign-change condition, 
        /// the result of this call is undefined.
        /// </para>
        /// 
        /// </summary>
        /// <param name="cutFunction">The cut function <code>T</code> to <code>int</code>, given
        /// by the <code>CompareTo</code> method of an object implementing 
        /// <code>IComparable&lt;T&gt;</code>.</param>
        /// <param name="low">Returns the largest item in the collection, where the
        /// cut function is positive (if any).</param>
        /// <param name="lowIsValid">Returns true if the cut function is positive somewhere
        /// on this collection.</param>
        /// <param name="high">Returns the least item in the collection, where the
        /// cut function is negative (if any).</param>
        /// <param name="highIsValid">Returns true if the cut function is negative somewhere
        /// on this collection.</param>
        /// <returns>True if the cut function is zero somewhere
        /// on this collection.</returns>
        bool Cut(IComparable<T> cutFunction, out T low, out bool lowIsValid, out T high, out bool highIsValid);


        /// <summary>
        /// Query this sorted collection for items greater than or equal to a supplied value.
        /// <para>The returned collection is not a copy but a view into the collection.</para>
        /// <para>The view is fragile in the sense that changes to the underlying collection will 
        /// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        /// </summary>
        /// <param name="bot">The lower bound (inclusive).</param>
        /// <returns>The result directed collection.</returns>
        IDirectedEnumerable<T> RangeFrom(T bot);


        /// <summary>
        /// Query this sorted collection for items between two supplied values.
        /// <para>The returned collection is not a copy but a view into the collection.</para>
        /// <para>The view is fragile in the sense that changes to the underlying collection will 
        /// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        /// </summary>
        /// <param name="bot">The lower bound (inclusive).</param>
        /// <param name="top">The upper bound (exclusive).</param>
        /// <returns>The result directed collection.</returns>
        IDirectedEnumerable<T> RangeFromTo(T bot, T top);


        /// <summary>
        /// Query this sorted collection for items less than a supplied value.
        /// <para>The returned collection is not a copy but a view into the collection.</para>
        /// <para>The view is fragile in the sense that changes to the underlying collection will 
        /// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        /// </summary>
        /// <param name="top">The upper bound (exclusive).</param>
        /// <returns>The result directed collection.</returns>
        IDirectedEnumerable<T> RangeTo(T top);


        /// <summary>
        /// Create a directed collection with the same items as this collection.
        /// <para>The returned collection is not a copy but a view into the collection.</para>
        /// <para>The view is fragile in the sense that changes to the underlying collection will 
        /// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        /// </summary>
        /// <returns>The result directed collection.</returns>
        IDirectedCollectionValue<T> RangeAll();


        //TODO: remove now that we assume that we can check the sorting order?
        /// <summary>
        /// Add all the items from another collection with an enumeration order that 
        /// is increasing in the items.
        /// </summary>
        /// <exception cref="ArgumentException"> if the enumerated items turns out
        /// not to be in increasing order.</exception>
        /// <param name="items">The collection to add.</param>
        void AddSorted(SCG.IEnumerable<T> items);


        /// <summary>
        /// Remove all items of this collection above or at a supplied threshold.
        /// </summary>
        /// <param name="low">The lower threshold (inclusive).</param>
        void RemoveRangeFrom(T low);


        /// <summary>
        /// Remove all items of this collection between two supplied thresholds.
        /// </summary>
        /// <param name="low">The lower threshold (inclusive).</param>
        /// <param name="hi">The upper threshold (exclusive).</param>
        void RemoveRangeFromTo(T low, T hi);


        /// <summary>
        /// Remove all items of this collection below a supplied threshold.
        /// </summary>
        /// <param name="hi">The upper threshold (exclusive).</param>
        void RemoveRangeTo(T hi);
    }

    /// <summary>
    /// An editable collection maintaining a definite sequence order of the items.
    ///
    /// <i>Implementations of this interface must compute the hash code and 
    /// equality exactly as prescribed in the method definitions in order to
    /// be consistent with other collection classes implementing this interface.</i>
    /// <i>This interface is usually implemented by explicit interface implementation,
    /// not as ordinary virtual methods.</i>
    /// </summary>
    public interface ISequenced<T> : ICollection<T>, IDirectedCollectionValue<T>
    {
        /// <summary>
        /// The hashcode is defined as <code>h(...h(h(h(x1),x2),x3),...,xn)</code> for
        /// <code>h(a,b)=CONSTANT*a+b</code> and the x's the hash codes of the items of 
        /// this collection.
        /// </summary>
        /// <returns>The sequence order hashcode of this collection.</returns>
        int GetSequencedHashCode();


        /// <summary>
        /// Compare this sequenced collection to another one in sequence order.
        /// </summary>
        /// <param name="otherCollection">The sequenced collection to compare to.</param>
        /// <returns>True if this collection and that contains equal (according to
        /// this collection's itemequalityComparer) in the same sequence order.</returns>
        bool SequencedEquals(ISequenced<T> otherCollection);
    }

    /// <summary>
    /// A sequenced collection, where indices of items in the order are maintained
    /// </summary>
    public interface IIndexed<T> : ISequenced<T>, IReadOnlyList<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        Speed IndexingSpeed { get; }

        /// <summary>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <value>The directed collection of items in a specific index interval.</value>
        /// <param name="start">The low index of the interval (inclusive).</param>
        /// <param name="count">The size of the range.</param>
        IDirectedCollectionValue<T> this[int start, int count] { get; }


        /// <summary>
        /// Searches for an item in the list going forwards from the start. 
        /// </summary>
        /// <param name="item">Item to search for.</param>
        /// <returns>Index of item from start. A negative number if item not found, 
        /// namely the one's complement of the index at which the Add operation would put the item.</returns>
        int IndexOf(T item);


        /// <summary>
        /// Searches for an item in the list going backwards from the end.
        /// </summary>
        /// <param name="item">Item to search for.</param>
        /// <returns>Index of of item from the end. A negative number if item not found, 
        /// namely the two-complement of the index at which the Add operation would put the item.</returns>
        int LastIndexOf(T item);

        /// <summary>
        /// Check if there exists an item  that satisfies a
        /// specific predicate in this collection and return the index of the first one.
        /// </summary>
        /// <param name="predicate">A delegate 
        /// (<see cref="T:Func`2"/> with <code>R == bool</code>) defining the predicate</param>
        /// <returns>the index, if found, a negative value else</returns>
        int FindIndex(Func<T, bool> predicate);

        /// <summary>
        /// Check if there exists an item  that satisfies a
        /// specific predicate in this collection and return the index of the last one.
        /// </summary>
        /// <param name="predicate">A delegate 
        /// (<see cref="T:Func`2"/> with <code>R == bool</code>) defining the predicate</param>
        /// <returns>the index, if found, a negative value else</returns>
        int FindLastIndex(Func<T, bool> predicate);


        /// <summary>
        /// Remove the item at a specific position of the list.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"> if <code>index</code> is negative or
        /// &gt;= the size of the collection.</exception>
        /// <param name="index">The index of the item to remove.</param>
        /// <returns>The removed item.</returns>
        T RemoveAt(int index);


        /// <summary>
        /// Remove all items in an index interval.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"> if start or count 
        /// is negative or start+count &gt; the size of the collection.</exception>
        /// <param name="start">The index of the first item to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        void RemoveInterval(int start, int count);
    }

    /// <summary>
    /// Represents a read-only collection of elements that can be accessed by index. 
    /// Enables System.Collections.Generic.IReadOnlyList to be used in .NET 4.5 projects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadOnlyList<out T> : IReadOnlyCollection<T>, SCG.IEnumerable<T>, System.Collections.IEnumerable
    {
        /// <summary>
        /// Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        T this[int index] { get; }
    }

    /// <summary>
    /// Represents a strongly-typed, read-only collection of elements.
    /// Enables System.Collections.Generic.IReadOnlyCollection to be used in .NET 4.5 projects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadOnlyCollection<out T> : SCG.IEnumerable<T>, System.Collections.IEnumerable
    {
    }

    //TODO: decide if this should extend ICollection
    /// <summary>
    /// The interface describing the operations of a LIFO stack data structure.
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>
    public interface IStack<T> : IDirectedCollectionValue<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        bool AllowsDuplicates { get; }
        /// <summary>
        /// Get the <code>index</code>'th element of the stack.  The bottom of the stack has index 0.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        T this[int index] { get; }
        /// <summary>
        /// Push an item to the top of the stack.
        /// </summary>
        /// <param name="item">The item</param>
        void Push(T item);
        /// <summary>
        /// Pop the item at the top of the stack from the stack.
        /// </summary>
        /// <returns>The popped item.</returns>
        T Pop();
    }

    /// <summary>
    /// The interface describing the operations of a FIFO queue data structure.
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>
    public interface IQueue<T> : IDirectedCollectionValue<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        bool AllowsDuplicates { get; }
        /// <summary>
        /// Get the <code>index</code>'th element of the queue.  The front of the queue has index 0.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        T this[int index] { get; }
        /// <summary>
        /// Enqueue an item at the back of the queue. 
        /// </summary>
        /// <param name="item">The item</param>
        void Enqueue(T item);
        /// <summary>
        /// Dequeue an item from the front of the queue.
        /// </summary>
        /// <returns>The item</returns>
        T Dequeue();
    }

    /// <summary>
    /// This is an indexed collection, where the item order is chosen by 
    /// the user at insertion time.
    ///
    /// NBNBNB: we need a description of the view functionality here!
    /// </summary>
    public interface IList<T> : IIndexed<T>, IDisposable, System.Collections.Generic.IList<T>, System.Collections.IList
    {
        /// <summary>
        /// </summary>
        /// <exception cref="NoSuchItemException"> if this list is empty.</exception>
        /// <value>The first item in this list.</value>
        T First { get; }

        /// <summary>
        /// </summary>
        /// <exception cref="NoSuchItemException"> if this list is empty.</exception>
        /// <value>The last item in this list.</value>
        T Last { get; }

        /// <summary>
        /// Since <code>Add(T item)</code> always add at the end of the list,
        /// this describes if list has FIFO or LIFO semantics.
        /// </summary>
        /// <value>True if the <code>Remove()</code> operation removes from the
        /// start of the list, false if it removes from the end.</value>
        bool FIFO { get; set; }

        /// <summary>
        /// On this list, this indexer is read/write.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"> if index is negative or
        /// &gt;= the size of the collection.</exception>
        /// <value>The index'th item of this list.</value>
        /// <param name="index">The index of the item to fetch or store.</param>
        new T this[int index] { get; set; }

        #region Ambiguous calls when extending System.Collections.Generic.IList<T>

        #region System.Collections.Generic.ICollection<T>
        /// <summary>
        /// 
        /// </summary>
        new int Count { get; }

        /// <summary>
        /// 
        /// </summary>
        new bool IsReadOnly { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        new bool Add(T item);

        /// <summary>
        /// 
        /// </summary>
        new void Clear();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        new bool Contains(T item);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        new void CopyTo(T[] array, int index);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        new bool Remove(T item);

        #endregion

        #region System.Collections.Generic.IList<T> proper

        /// <summary>
        /// Searches for an item in the list going forwards from the start. 
        /// </summary>
        /// <param name="item">Item to search for.</param>
        /// <returns>Index of item from start. A negative number if item not found, 
        /// namely the one's complement of the index at which the Add operation would put the item.</returns>
        new int IndexOf(T item);

        /// <summary>
        /// Remove the item at a specific position of the list.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"> if <code>index</code> is negative or
        /// &gt;= the size of the collection.</exception>
        /// <param name="index">The index of the item to remove.</param>
        /// <returns>The removed item.</returns>
        new T RemoveAt(int index);

        #endregion

        #endregion

        /*/// <summary>
    /// Insert an item at a specific index location in this list. 
    /// </summary>
    /// <exception cref="IndexOutOfRangeException"> if <code>index</code> is negative or
    /// &gt; the size of the collection.</exception>
    /// <exception cref="DuplicateNotAllowedException"> if the list has
    /// <code>AllowsDuplicates==false</code> and the item is 
    /// already in the list.</exception>
    /// <param name="index">The index at which to insert.</param>
    /// <param name="item">The item to insert.</param>
    void Insert(int index, T item);*/

        /// <summary>
        /// Insert an item at the end of a compatible view, used as a pointer.
        /// <para>The <code>pointer</code> must be a view on the same list as
        /// <code>this</code> and the endpoint of <code>pointer</code> must be
        /// a valid insertion point of <code>this</code></para>
        /// </summary>
        /// <exception cref="IncompatibleViewException">If <code>pointer</code> 
        /// is not a view on the same list as <code>this</code></exception>
        /// <exception cref="IndexOutOfRangeException"><b>??????</b> if the endpoint of 
        ///  <code>pointer</code> is not inside <code>this</code></exception>
        /// <exception cref="DuplicateNotAllowedException"> if the list has
        /// <code>AllowsDuplicates==false</code> and the item is 
        /// already in the list.</exception>
        /// <param name="pointer"></param>
        /// <param name="item"></param>
        void Insert(IList<T> pointer, T item);

        /// <summary>
        /// Insert an item at the front of this list.
        /// <exception cref="DuplicateNotAllowedException"/> if the list has
        /// <code>AllowsDuplicates==false</code> and the item is 
        /// already in the list.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        void InsertFirst(T item);

        /// <summary>
        /// Insert an item at the back of this list.
        /// <exception cref="DuplicateNotAllowedException"/> if the list has
        /// <code>AllowsDuplicates==false</code> and the item is 
        /// already in the list.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        void InsertLast(T item);

        /// <summary>
        /// Insert into this list all items from an enumerable collection starting 
        /// at a particular index.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"> if <code>index</code> is negative or
        /// &gt; the size of the collection.</exception>
        /// <exception cref="DuplicateNotAllowedException"> if the list has 
        /// <code>AllowsDuplicates==false</code> and one of the items to insert is
        /// already in the list.</exception>
        /// <param name="index">Index to start inserting at</param>
        /// <param name="items">Items to insert</param>
        void InsertAll(int index, SCG.IEnumerable<T> items);

        /// <summary>
        /// Create a new list consisting of the items of this list satisfying a 
        /// certain predicate.
        /// </summary>
        /// <param name="filter">The filter delegate defining the predicate.</param>
        /// <returns>The new list.</returns>
        IList<T> FindAll(Func<T, bool> filter);

        /// <summary>
        /// Create a new list consisting of the results of mapping all items of this
        /// list. The new list will use the default equalityComparer for the item type V.
        /// </summary>
        /// <typeparam name="V">The type of items of the new list</typeparam>
        /// <param name="mapper">The delegate defining the map.</param>
        /// <returns>The new list.</returns>
        IList<V> Map<V>(Func<T, V> mapper);

        /// <summary>
        /// Create a new list consisting of the results of mapping all items of this
        /// list. The new list will use a specified equalityComparer for the item type.
        /// </summary>
        /// <typeparam name="V">The type of items of the new list</typeparam>
        /// <param name="mapper">The delegate defining the map.</param>
        /// <param name="equalityComparer">The equalityComparer to use for the new list</param>
        /// <returns>The new list.</returns>
        IList<V> Map<V>(Func<T, V> mapper, System.Collections.Generic.IEqualityComparer<V> equalityComparer);

        /// <summary>
        /// Remove one item from the list: from the front if <code>FIFO</code>
        /// is true, else from the back.
        /// <exception cref="NoSuchItemException"/> if this list is empty.
        /// </summary>
        /// <returns>The removed item.</returns>
        T Remove();

        /// <summary>
        /// Remove one item from the front of the list.
        /// <exception cref="NoSuchItemException"/> if this list is empty.
        /// </summary>
        /// <returns>The removed item.</returns>
        T RemoveFirst();

        /// <summary>
        /// Remove one item from the back of the list.
        /// <exception cref="NoSuchItemException"/> if this list is empty.
        /// </summary>
        /// <returns>The removed item.</returns>
        T RemoveLast();

        /// <summary>
        /// Create a list view on this list. 
        /// <exception cref="ArgumentOutOfRangeException"/> if the view would not fit into
        /// this list.
        /// </summary>
        /// <param name="start">The index in this list of the start of the view.</param>
        /// <param name="count">The size of the view.</param>
        /// <returns>The new list view.</returns>
        IList<T> View(int start, int count);

        /// <summary>
        /// Create a list view on this list containing the (first) occurrence of a particular item. 
        /// <exception cref="NoSuchItemException"/> if the item is not in this list.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>The new list view.</returns>
        IList<T> ViewOf(T item);

        /// <summary>
        /// Create a list view on this list containing the last occurrence of a particular item. 
        /// <exception cref="NoSuchItemException"/> if the item is not in this list.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>The new list view.</returns>
        IList<T> LastViewOf(T item);

        /// <summary>
        /// Null if this list is not a view.
        /// </summary>
        /// <value>Underlying list for view.</value>
        IList<T> Underlying { get; }

        /// <summary>
        /// </summary>
        /// <value>Offset for this list view or 0 for an underlying list.</value>
        int Offset { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        bool IsValid { get; }

        /// <summary>
        /// Slide this list view along the underlying list.
        /// </summary>
        /// <exception cref="NotAViewException"> if this list is not a view.</exception>
        /// <exception cref="ArgumentOutOfRangeException"> if the operation
        /// would bring either end of the view outside the underlying list.</exception>
        /// <param name="offset">The signed amount to slide: positive to slide
        /// towards the end.</param>
        IList<T> Slide(int offset);

        /// <summary>
        /// Slide this list view along the underlying list, changing its size.
        /// 
        /// </summary>
        /// <exception cref="NotAViewException"> if this list is not a view.</exception>
        /// <exception cref="ArgumentOutOfRangeException"> if the operation
        /// would bring either end of the view outside the underlying list.</exception>
        /// <param name="offset">The signed amount to slide: positive to slide
        /// towards the end.</param>
        /// <param name="size">The new size of the view.</param>
        IList<T> Slide(int offset, int size);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        bool TrySlide(int offset);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        bool TrySlide(int offset, int size);

        /// <summary>
        /// 
        /// <para>Returns null if <code>otherView</code> is strictly to the left of this view</para>
        /// </summary>
        /// <param name="otherView"></param>
        /// <exception cref="IncompatibleViewException">If otherView does not have the same underlying list as this</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <code>otherView</code> is strictly to the left of this view</exception>
        /// <returns></returns>
        IList<T> Span(IList<T> otherView);

        /// <summary>
        /// Reverse the list so the items are in the opposite sequence order.
        /// </summary>
        void Reverse();

        /// <summary>
        /// Check if this list is sorted according to the default sorting order
        /// for the item type T, as defined by the <see cref="T:C5.Comparer`1"/> class 
        /// </summary>
        /// <exception cref="NotComparableException">if T is not comparable</exception>
        /// <returns>True if the list is sorted, else false.</returns>
        bool IsSorted();

        /// <summary>
        /// Check if this list is sorted according to a specific sorting order.
        /// </summary>
        /// <param name="comparer">The comparer defining the sorting order.</param>
        /// <returns>True if the list is sorted, else false.</returns>
        bool IsSorted(System.Collections.Generic.IComparer<T> comparer);

        /// <summary>
        /// Sort the items of the list according to the default sorting order
        /// for the item type T, as defined by the <see cref="T:C5.Comparer`1"/> class 
        /// </summary>
        /// <exception cref="NotComparableException">if T is not comparable</exception>
        void Sort();

        /// <summary>
        /// Sort the items of the list according to a specified sorting order.
        /// <para>The sorting does not perform duplicate elimination or identify items
        /// according to the comparer or itemequalityComparer. I.e. the list as an 
        /// unsequenced collection with binary equality, will not change.
        /// </para>
        /// </summary>
        /// <param name="comparer">The comparer defining the sorting order.</param>
        void Sort(System.Collections.Generic.IComparer<T> comparer);


        /// <summary>
        /// Randomly shuffle the items of this list. 
        /// </summary>
        void Shuffle();


        /// <summary>
        /// Shuffle the items of this list according to a specific random source.
        /// </summary>
        /// <param name="rnd">The random source.</param>
        void Shuffle(Random rnd);
    }

    /// <summary>
    /// The base type of a priority queue handle
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPriorityQueueHandle<T>
    {
        //TODO: make abstract and prepare for double dispatch:
        //public virtual bool Delete(IPriorityQueue<T> q) { throw new InvalidFooException();}
        //bool Replace(T item);
    }


    /// <summary>
    /// A generic collection of items prioritized by a comparison (order) relation.
    /// Supports adding items and reporting or removing extremal elements. 
    /// <para>
    /// 
    /// </para>
    /// When adding an item, the user may choose to have a handle allocated for this item in the queue. 
    /// The resulting handle may be used for deleting the item even if not extremal, and for replacing the item.
    /// A priority queue typically only holds numeric priorities associated with some objects
    /// maintained separately in other collection objects.
    /// </summary>
    public interface IPriorityQueue<T> : IExtensible<T>
    {
        /// <summary>
        /// Find the current least item of this priority queue.
        /// </summary>
        /// <returns>The least item.</returns>
        T FindMin();


        /// <summary>
        /// Remove the least item from this  priority queue.
        /// </summary>
        /// <returns>The removed item.</returns>
        T DeleteMin();


        /// <summary>
        /// Find the current largest item of this priority queue.
        /// </summary>
        /// <returns>The largest item.</returns>
        T FindMax();


        /// <summary>
        /// Remove the largest item from this priority queue.
        /// </summary>
        /// <returns>The removed item.</returns>
        T DeleteMax();

        /// <summary>
        /// The comparer object supplied at creation time for this collection
        /// </summary>
        /// <value>The comparer</value>
        System.Collections.Generic.IComparer<T> Comparer { get; }
        /// <summary>
        /// Get or set the item corresponding to a handle. Throws exceptions on 
        /// invalid handles.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        T this[IPriorityQueueHandle<T> handle] { get; set; }

        /// <summary>
        /// Check if the entry corresponding to a handle is in the priority queue.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        bool Find(IPriorityQueueHandle<T> handle, out T item);

        /// <summary>
        /// Add an item to the priority queue, receiving a 
        /// handle for the item in the queue, 
        /// or reusing an existing unused handle.
        /// </summary>
        /// <param name="handle">On output: a handle for the added item. 
        /// On input: null for allocating a new handle, or a currently unused handle for reuse. 
        /// A handle for reuse must be compatible with this priority queue, 
        /// by being created by a priority queue of the same runtime type, but not 
        /// necessarily the same priority queue object.</param>
        /// <param name="item"></param>
        /// <returns></returns>
        bool Add(ref IPriorityQueueHandle<T> handle, T item);

        /// <summary>
        /// Delete an item with a handle from a priority queue
        /// </summary>
        /// <param name="handle">The handle for the item. The handle will be invalidated, but reusable.</param>
        /// <returns>The deleted item</returns>
        T Delete(IPriorityQueueHandle<T> handle);

        /// <summary>
        /// Replace an item with a handle in a priority queue with a new item. 
        /// Typically used for changing the priority of some queued object.
        /// </summary>
        /// <param name="handle">The handle for the old item</param>
        /// <param name="item">The new item</param>
        /// <returns>The old item</returns>
        T Replace(IPriorityQueueHandle<T> handle, T item);

        /// <summary>
        /// Find the current least item of this priority queue.
        /// </summary>
        /// <param name="handle">On return: the handle of the item.</param>
        /// <returns>The least item.</returns>
        T FindMin(out IPriorityQueueHandle<T> handle);

        /// <summary>
        /// Find the current largest item of this priority queue.
        /// </summary>
        /// <param name="handle">On return: the handle of the item.</param>
        /// <returns>The largest item.</returns>

        T FindMax(out IPriorityQueueHandle<T> handle);

        /// <summary>
        /// Remove the least item from this  priority queue.
        /// </summary>
        /// <param name="handle">On return: the handle of the removed item.</param>
        /// <returns>The removed item.</returns>

        T DeleteMin(out IPriorityQueueHandle<T> handle);

        /// <summary>
        /// Remove the largest item from this  priority queue.
        /// </summary>
        /// <param name="handle">On return: the handle of the removed item.</param>
        /// <returns>The removed item.</returns>
        T DeleteMax(out IPriorityQueueHandle<T> handle);
    }




    /// <summary>
    /// The symbolic characterization of the speed of lookups for a collection.
    /// The values may refer to worst-case, amortized and/or expected asymtotic 
    /// complexity wrt. the collection size.
    /// </summary>
    public enum Speed : short
    {
        /// <summary>
        /// Counting the collection with the <code>Count property</code> may not return
        /// (for a synthetic and potentially infinite collection).
        /// </summary>
        PotentiallyInfinite = 1,
        /// <summary>
        /// Lookup operations like <code>Contains(T item)</code> or the <code>Count</code>
        /// property may take time O(n),
        /// where n is the size of the collection.
        /// </summary>
        Linear = 2,
        /// <summary>
        /// Lookup operations like <code>Contains(T item)</code> or the <code>Count</code>
        /// property  takes time O(log n),
        /// where n is the size of the collection.
        /// </summary>
        Log = 3,
        /// <summary>
        /// Lookup operations like <code>Contains(T item)</code> or the <code>Count</code>
        /// property  takes time O(1),
        /// where n is the size of the collection.
        /// </summary>
        Constant = 4
    }


    /// <summary>
    /// Direction of enumeration order relative to original collection.
    /// </summary>
    public enum EnumerationDirection
    {
        /// <summary>
        /// Same direction
        /// </summary>
        Forwards,
        /// <summary>
        /// Opposite direction
        /// </summary>
        Backwards
    }

    /// <summary>
    /// It specifies the memory type strategy of the internal enumerator implemented to iterate over the collection.
    /// </summary>
    public enum MemoryType
    {
        /// <summary>
        /// Normal is the usual operator type. A new instance of an enumerator is always returned
        /// for multithread safety purposes.
        /// </summary>
        Normal,
        /// <summary>
        /// Safe returns the same enumerator instance and updates references or a new instance in case of multiple enumeration and multithread access  
        /// </summary>
        Safe,
        /// <summary>
        /// Strict always returns the same enumerator instance. An exception is raised if the collection is enumerated more than once or
        /// if the collection is accessed by multiple threads concurrently.
        /// </summary>
        Strict
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum EventTypeEnum
    {
        /// <summary>
        /// 
        /// </summary>
        None = 0x00000000,
        /// <summary>
        /// 
        /// </summary>
        Changed = 0x00000001,
        /// <summary>
        /// 
        /// </summary>
        Cleared = 0x00000002,
        /// <summary>
        /// 
        /// </summary>
        Added = 0x00000004,
        /// <summary>
        /// 
        /// </summary>
        Removed = 0x00000008,
        /// <summary>
        /// 
        /// </summary>
        Basic = 0x0000000f,
        /// <summary>
        /// 
        /// </summary>
        Inserted = 0x00000010,
        /// <summary>
        /// 
        /// </summary>
        RemovedAt = 0x00000020,
        /// <summary>
        /// 
        /// </summary>
        All = 0x0000003f
    }





    /// <summary>
    /// An exception to throw from library code when an internal inconsistency is encountered.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class InternalException : Exception
    {
        internal InternalException(string message) : base(message) { }
    }

    /// <summary>
    /// An exception thrown by an update operation on a Read-Only collection or dictionary.
    /// <para>This exception will be thrown unconditionally when an update operation 
    /// (method or set property) is called. No check is made to see if the update operation, 
    /// if allowed, would actually change the collection. </para>
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class ReadOnlyCollectionException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public ReadOnlyCollectionException() : base() { }
        /// <summary>
        /// Create the exception with an explanation of the reason.
        /// </summary>
        /// <param name="message"></param>
        public ReadOnlyCollectionException(string message) : base(message) { }
    }

    /// <summary>
    /// 
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class FixedSizeCollectionException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public FixedSizeCollectionException() : base() { }
        /// <summary>
        /// Create the exception with an explanation of the reason.
        /// </summary>
        /// <param name="message"></param>
        public FixedSizeCollectionException(string message) : base(message) { }
    }

    /// <summary>
    /// 
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class UnlistenableEventException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public UnlistenableEventException() : base() { }
        /// <summary>
        /// Create the exception with an explanation of the reason.
        /// </summary>
        /// <param name="message"></param>
        public UnlistenableEventException(string message) : base(message) { }
    }

    /// <summary>
    /// An exception thrown by the MemorySafeEnumerator if the collection is enumerated by multiple threads concurrently
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class ConcurrentEnumerationException : Exception
    {
        /// <summary>
        ///  Create a simple exception with no further explanation. 
        /// </summary>
        public ConcurrentEnumerationException()
        {

        }

        /// <summary>
        /// Create a simple exception with the an explanation contained in the error message.
        /// </summary>
        /// <param name="message"></param>
        public ConcurrentEnumerationException(string message) : base(message) { }
    }
    /// <summary>
    /// An exception thrown by the MemorySafeEnumerator if the collection is enumerated multiple times when the 
    /// memory mode is set to Strict 
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class MultipleEnumerationException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public MultipleEnumerationException()
        {

        }

        /// <summary>
        /// Create a simple exception with the an explanation contained in the error message.
        /// </summary>
        /// <param name="message"></param>
        public MultipleEnumerationException(string message) : base(message) { }
    }
    /// <summary>
    /// An exception thrown by enumerators, range views etc. when accessed after 
    /// the underlying collection has been modified.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class CollectionModifiedException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public CollectionModifiedException() : base() { }
        /// <summary>
        /// Create the exception with an explanation of the reason.
        /// </summary>
        /// <param name="message"></param>
        public CollectionModifiedException(string message) : base(message) { }
    }

    /// <summary>
    /// An exception thrown when trying to access a view (a list view on a <see cref="T:C5.IList`1"/> or 
    /// a snapshot on a <see cref="T:C5.IPersistentSorted`1"/>)
    /// that has been invalidated by some earlier operation.
    /// <para>
    /// The typical scenario is a view on a list that hash been invalidated by a call to 
    /// Sort, Reverse or Shuffle on some other, overlapping view or the whole list.
    /// </para>
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class ViewDisposedException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public ViewDisposedException() : base() { }
        /// <summary>
        /// Create the exception with an explanation of the reason.
        /// </summary>
        /// <param name="message"></param>
        public ViewDisposedException(string message) : base(message) { }
    }

    /// <summary>
    /// An exception thrown by a lookup or lookup with update operation that does not 
    /// find the lookup item and has no other means to communicate failure.
    /// <para>The typical scenario is a lookup by key in a dictionary with an indexer,
    /// see e.g. <see cref="P:C5.IDictionary`2.Item(`0)"/></para>
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class NoSuchItemException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public NoSuchItemException() : base() { }
        /// <summary>
        /// Create the exception with an explanation of the reason.
        /// </summary>
        /// <param name="message"></param>
        public NoSuchItemException(string message) : base(message) { }
    }

    /// <summary>
    /// An exception thrown by an operation on a list (<see cref="T:C5.IList`1"/>)
    /// that only makes sense for a view, not for an underlying list.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class NotAViewException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public NotAViewException() : base() { }
        /// <summary>
        /// Create the exception with an explanation of the reason.
        /// </summary>
        /// <param name="message"></param>
        public NotAViewException(string message) : base(message) { }
    }

    /// <summary>
    /// An exception thrown when an operation attempts to create a duplicate in a collection with set semantics 
    /// (<see cref="P:C5.IExtensible`1.AllowsDuplicates"/> is false) or attempts to create a duplicate key in a dictionary.
    /// <para>With collections this can only happen with Insert operations on lists, since the Add operations will
    /// not try to create duplictes and either ignore the failure or report it in a bool return value.
    /// </para>
    /// <para>With dictionaries this can happen with the <see cref="M:C5.IDictionary`2.Add(`0,`1)"/> metod.</para>
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class DuplicateNotAllowedException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public DuplicateNotAllowedException() : base() { }
        /// <summary>
        /// Create the exception with an explanation of the reason.
        /// </summary>
        /// <param name="message"></param>
        public DuplicateNotAllowedException(string message) : base(message) { }
    }

    /// <summary>
    /// 
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class InvalidPriorityQueueHandleException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public InvalidPriorityQueueHandleException() : base() { }
        /// <summary>
        /// Create the exception with an explanation of the reason.
        /// </summary>
        /// <param name="message"></param>
        public InvalidPriorityQueueHandleException(string message) : base(message) { }
    }

    /// <summary>
    /// An exception thrown by an operation that need to construct a natural
    /// comparer for a type.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class NotComparableException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public NotComparableException() : base() { }
        /// <summary>
        /// Create the exception with an explanation of the reason.
        /// </summary>
        /// <param name="message"></param>
        public NotComparableException(string message) : base(message) { }
    }

    /// <summary>
    /// An exception thrown by operations on a list that expects an argument
    /// that is a view on the same underlying list.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class IncompatibleViewException : Exception
    {
        /// <summary>
        /// Create a simple exception with no further explanation.
        /// </summary>
        public IncompatibleViewException() : base() { }
        /// <summary>
        /// Create the exception with an explanation of the reason.
        /// </summary>
        /// <param name="message"></param>
        public IncompatibleViewException(string message) : base(message) { }
    }




    /// <summary>
    /// The type of event raised after an operation on a collection has changed its contents.
    /// Normally, a multioperation like AddAll, 
    /// <see cref="M:C5.IExtensible`1.AddAll(System.Collections.Generic.IEnumerable{`0})"/> 
    /// will only fire one CollectionChanged event. Any operation that changes the collection
    /// must fire CollectionChanged as its last event.
    /// </summary>
    public delegate void CollectionChangedHandler<T>(object sender);

    /// <summary>
    /// The type of event raised after the Clear() operation on a collection.
    /// <para/>
    /// Note: The Clear() operation will not fire ItemsRemoved events. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    public delegate void CollectionClearedHandler<T>(object sender, ClearedEventArgs eventArgs);

    /// <summary>
    /// The type of event raised after an item has been added to a collection.
    /// The event will be raised at a point of time, where the collection object is 
    /// in an internally consistent state and before the corresponding CollectionChanged 
    /// event is raised.
    /// <para/>
    /// Note: an Update operation will fire an ItemsRemoved and an ItemsAdded event.
    /// <para/>
    /// Note: When an item is inserted into a list (<see cref="T:C5.IList`1"/>), both
    /// ItemInserted and ItemsAdded events will be fired.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs">An object with the item that was added</param>
    public delegate void ItemsAddedHandler<T>(object sender, ItemCountEventArgs<T> eventArgs);

    /// <summary>
    /// The type of event raised after an item has been removed from a collection.
    /// The event will be raised at a point of time, where the collection object is 
    /// in an internally consistent state and before the corresponding CollectionChanged 
    /// event is raised.
    /// <para/>
    /// Note: The Clear() operation will not fire ItemsRemoved events. 
    /// <para/>
    /// Note: an Update operation will fire an ItemsRemoved and an ItemsAdded event.
    /// <para/>
    /// Note: When an item is removed from a list by the RemoveAt operation, both an 
    /// ItemsRemoved and an ItemRemovedAt event will be fired.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs">An object with the item that was removed</param>
    public delegate void ItemsRemovedHandler<T>(object sender, ItemCountEventArgs<T> eventArgs);

    /// <summary>
    /// The type of event raised after an item has been inserted into a list by an Insert, 
    /// InsertFirst or InsertLast operation.
    /// The event will be raised at a point of time, where the collection object is 
    /// in an internally consistent state and before the corresponding CollectionChanged 
    /// event is raised.
    /// <para/>
    /// Note: an ItemsAdded event will also be fired.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    public delegate void ItemInsertedHandler<T>(object sender, ItemAtEventArgs<T> eventArgs);

    /// <summary>
    /// The type of event raised after an item has been removed from a list by a RemoveAt(int i)
    /// operation (or RemoveFirst(), RemoveLast(), Remove() operation).
    /// The event will be raised at a point of time, where the collection object is 
    /// in an internally consistent state and before the corresponding CollectionChanged 
    /// event is raised.
    /// <para/>
    /// Note: an ItemRemoved event will also be fired.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    public delegate void ItemRemovedAtHandler<T>(object sender, ItemAtEventArgs<T> eventArgs);
}
