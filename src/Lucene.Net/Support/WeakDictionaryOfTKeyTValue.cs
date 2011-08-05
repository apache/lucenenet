// -----------------------------------------------------------------------
// <copyright file="WeakDictionaryOfTKeyTValue.cs" company="Apache">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A dictionary of weak references.
    /// </summary>
    /// <remarks>
    ///    <note>
    ///        <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Support/WeakDictionaryOfTKeyTValue.cs">
    ///              src/Lucene.Net/Support/WeakDictionaryOfTKeyTValue.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Util/Support/WeakDictionaryOfTKeyTValueTest.cs">
    ///             test/Lucene.Net.Test/Support/WeakDictionaryOfTKeyTValueTest.cs
    ///             </a>
    ///         </para>
    ///     </note>
    ///     <para>
    ///         Heavily base on the implementation found here on
    ///         <a href="http://blogs.msdn.com/b/nicholg/archive/2006/06/04/617466.aspx"> Nick Guerrera's Blog</a>
    ///     </para>
    ///     <note>
    ///         This was implemented to be the C# equivalent of has a Java WeakHashMap.
    ///     </note>
    /// </remarks>
    /// <typeparam name="TKey">The <c>TKey</c> type.</typeparam>
    /// <typeparam name="TValue">The <c>TValue</c> type.</typeparam>
    public sealed class WeakDictionary<TKey, TValue> : BaseDictionary<TKey, TValue>
        where TKey : class
        where TValue : class
    {
        private Dictionary<object, WeakReference<TValue>> internalDictionary;
        private WeakKeyComparer<TKey> comparer;
        private int initialCapacity = 0;
        private DateTime lastRemoval = DateTime.Now;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakDictionary{TKey,TValue}"/> class.
        /// </summary>
        public WeakDictionary() 
            : this(0, null) 
        { 
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public WeakDictionary(int capacity) 
            : this(capacity, null) 
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        /// <param name="comparer">The comparer.</param>
        public WeakDictionary(IEqualityComparer<TKey> comparer)
            : this(0, comparer) 
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        /// <param name="comparer">The comparer.</param>
        public WeakDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.PeriodicRemoval = new TimeSpan(0, 0, 15);
            this.initialCapacity = capacity;
            this.comparer = new WeakKeyComparer<TKey>(comparer);
            this.internalDictionary = new Dictionary<object, WeakReference<TValue>>(capacity, this.comparer);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        public WeakDictionary(IDictionary<TKey, TValue> dictionary)
            : this(0, null)
        {
            foreach (var pair in dictionary)
                this.Add(pair.Key, pair.Value);
        }



        /// <summary>
        /// Gets the comparer.
        /// </summary>
        /// <value>The comparer.</value>
        public IEqualityComparer<TKey> Comparer
        {
            get { return this.comparer.InternalComparer; }
        }

        // WARNING: The count returned here may include entries for which
        // either the key or value objects have already been garbage
        // collected. Call RemoveCollectedEntries to weed out collected
        // entries and update the count accordingly.

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public override int Count
        {
            get 
            {
                this.EnsureRemovalOfCollectedEntries();
                return this.internalDictionary.Count; 
            }
        }

        /// <summary>
        /// Gets the initial capacity.
        /// </summary>
        /// <value>The initial capacity.</value>
        public int InitialCapacity
        {
            get { return this.initialCapacity; }
        }

        /// <summary>
        /// Gets or sets the periodic removal.
        /// </summary>
        /// <value>The periodic removal.</value>
        public TimeSpan PeriodicRemoval { get; set; }

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public override void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            this.EnsureRemovalOfCollectedEntries();

            WeakReference<TKey> weakKey = new WeakKeyReference<TKey>(key, this.comparer);
            WeakReference<TValue> weakValue = new WeakReference<TValue>(value);

            this.internalDictionary.Add(weakKey, weakValue);
        }


        /// <summary>
        /// Clears this instance.
        /// </summary>
        public override void Clear()
        {
            this.internalDictionary.Clear();
        }

        /// <summary>
        /// Determines whether the specified key contains key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///     <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
        /// </returns>
        public override bool ContainsKey(TKey key)
        {
            this.EnsureRemovalOfCollectedEntries();
            return this.internalDictionary.ContainsKey(key);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="IEnumerator{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> instance that 
        ///     can be used to iterate through the collection.
        /// </returns>
        public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            this.EnsureRemovalOfCollectedEntries();

            foreach (KeyValuePair<object, WeakReference<TValue>> pair in this.internalDictionary)
            {
                WeakReference<TKey> weakKey = (WeakReference<TKey>)pair.Key;
                WeakReference<TValue> weakValue = pair.Value;

                TKey key = weakKey.Target;
                TValue value = weakValue.Target;

                if (weakKey.IsAlive && weakValue.IsAlive)
                    yield return new KeyValuePair<TKey, TValue>(key, value);
            }
        }


        /// <summary>
        /// Removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///     <c>true</c> if the <see cref="KeyValuePair{TKey,TValue}"/> was successfully 
        ///     removed; otherwise, <c>false</c>.
        /// </returns>
        public override bool Remove(TKey key)
        {
            var result = this.internalDictionary.Remove(key);
            this.EnsureRemovalOfCollectedEntries();
            return result;
        }

        // Removes the left-over weak references for entries in the dictionary
        // whose key or value has already been reclaimed by the garbage
        // collector. This will reduce the dictionary's Count by the number
        // of dead key-value pairs that were eliminated.



        /// <summary>
        /// Removes the collected entries.
        /// </summary>
        public void RemoveCollectedEntries()
        {
            List<object> list = null;

            foreach (KeyValuePair<object, WeakReference<TValue>> pair in this.internalDictionary)
            {
                WeakReference<TKey> weakKey = (WeakReference<TKey>)pair.Key;
                WeakReference<TValue> weakValue = pair.Value;

                if (!weakKey.IsAlive || !weakValue.IsAlive)
                {
                    if (list == null)
                        list = new List<object>();

                    list.Add(weakKey);
                }
            }

            if (list != null)
            {
                foreach (object key in list)
                    this.internalDictionary.Remove(key);
            }
        }

        /// <summary>
        /// Tries the get value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     <c>true</c> if the <paramref name="key"/> was successfully 
        ///     found; otherwise, <c>false</c>.
        /// </returns>
        public override bool TryGetValue(TKey key, out TValue value)
        {
            this.EnsureRemovalOfCollectedEntries();

            WeakReference<TValue> weakValue;
            if (this.internalDictionary.TryGetValue(key, out weakValue))
            {
                value = weakValue.Target;
                return weakValue.IsAlive;
            }

            value = default(TValue);
            return false;
        }

       

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        protected override void SetValue(TKey key, TValue value)
        {
            WeakReference<TKey> weakKey = new WeakKeyReference<TKey>(key, this.comparer);
            this.internalDictionary[weakKey] = new WeakReference<TValue>(value);
        }

        /// <summary>
        /// Ensures the removal of collected entries.
        /// </summary>
        private void EnsureRemovalOfCollectedEntries()
        {
            if (this.PeriodicRemoval != null && this.lastRemoval.Add(this.PeriodicRemoval) < DateTime.Now)
            {
                this.RemoveCollectedEntries();
                this.lastRemoval = DateTime.Now;
            }
        }
    }
}
