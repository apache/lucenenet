// -----------------------------------------------------------------------
// <copyright file="BaseDictionaryOfTKeyTValue.cs" company="Apache">
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


   
#if !SILVERLIGHT
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(PREFIX + "DictionaryDebugView`2" + SUFFIX)]
#endif

    /// <summary>
    /// The abstract type that helps to construct specialized dictionaries.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    ///     <note>
    ///        <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Support/BaseDictionaryOfTKeyTValue.cs">
    ///              src/Lucene.Net/Support/BaseDictionaryOfTKeyTValue.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Util/Support/WeakDictionaryOfTKeyTValueTest.cs">
    ///             test/Lucene.Net.Test/Support/WeakDictionaryOfTKeyTValueTest.cs
    ///             </a>
    ///         </para>
    ///     </note>
    /// </remarks>
    public abstract class BaseDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private KeyCollection keys;
        private ValueCollection values;

       /// <summary>
        /// Gets the number of key pair values stored in the dictionary.
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public ICollection<TKey> Keys
        {
            get { return this.keys ?? (this.keys = new KeyCollection(this)); }
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public ICollection<TValue> Values
        {
            get { return this.values ?? (this.values = new ValueCollection(this)); }
        }



        /// <summary>
        /// Gets or sets the <typeparamref name="TValue"/> with the specified key.
        /// </summary>
        /// <param name="key">The <typeparamref name="TKey"/> key mapped to a value.</param>
        /// <returns>
        ///     The <typeparamref name="TValue"/> instance that is mapped to the <paramref name="key"/>  
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///     Thrown when the specified <paramref name="key"/> can not be found.
        /// </exception>
        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!this.TryGetValue(key, out value))
                    throw new KeyNotFoundException();

                return value;
            }

            set
            {
                this.SetValue(key, value);
            }
        }


        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        public abstract void Add(TKey key, TValue value);



        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }


        /// <summary>
        /// Removes all the key pair values from the dictionary and resets the count.
        /// </summary>
        public abstract void Clear();


        /// <summary>
        /// Determines whether [contains] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///     <c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue value;
            return this.TryGetValue(item.Key, out value) && 
                EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }


        /// <summary>
        /// Determines whether the <see cref="BaseDictionary{TKey,TValue}"/> contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the dictionary.</param>
        /// <remarks>
        ///     <para>
        ///         This method approaches an O(1) operation.
        ///     </para>
        /// </remarks>
        /// <returns><c>true</c> if the <paramref name="key"/> was found, otherwise <c>false</c> </returns>
        /// <exception cref="System.ArgumentNullException">Throw when the <paramref name="key"/> is null.</exception>
        public abstract bool ContainsKey(TKey key);

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Copy(this, array, arrayIndex);
        }


        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Dictionary{TKey,TValue}" />.
        /// </summary>
        /// <returns>A <see cref="IEnumerator{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> structure for the Dictionary.</returns>
        public abstract IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();


        /// <summary>
        /// Removes the specified key from <see cref="BaseDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        ///     <c>true</c> if the element was successfully found and removed, otherwise <c>false</c>
        /// </returns>
        public abstract bool Remove(TKey key);

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if element was found and successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!this.Contains(item))
                return false;

            return this.Remove(item.Key);
        }


        /// <summary>
        /// Attempts to the get value of the associated key.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method combines the functionality of the ContainsKey method and the Item property.
        ///     </para>
        ///     <para>
        ///         If the key is not found, then the value parameter gets the appropriate default value 
        ///         for the value type TValue; for example, 0 (zero) for integer types, false for Boolean 
        ///         types, and null for reference types.
        ///     </para>
        ///     <para>
        ///         Use the TryGetValue method if your code frequently attempts to access keys that are not 
        ///         in the dictionary. Using this method is more efficient than catching the 
        ///         KeyNotFoundException thrown by the Item property.
        ///     </para>
        ///     <para>
        ///         This method approaches an O(1) operation.
        ///     </para>
        /// </remarks>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     <c>true</c>if the <see cref="BaseDictionary{TKey,TValue}"/> contains an element with the 
        ///     specified key; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TryGetValue(TKey key, out TValue value);

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        protected abstract void SetValue(TKey key, TValue value);



        private static void Copy<T>(ICollection<T> source, T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");

            if ((array.Length - arrayIndex) < source.Count)
                throw new ArgumentException("Destination array is not large enough. Check array.Length and arrayIndex.");

            foreach (T item in source)
                array[arrayIndex++] = item;
        }

       

        /// <summary>
        /// The base class for providing the <typeparamref name="TKey"/> and <typeparamref name="TValue"/>
        /// collections.
        /// </summary>
        /// <typeparam name="T">The <typeparamref name="T"/> type.</typeparam>
        private abstract class Collection<T> : ICollection<T>
        {
            protected readonly IDictionary<TKey, TValue> Dictionary;

            /// <summary>
            /// Initializes a new instance of the <see cref="BaseDictionary&lt;TKey, TValue&gt;.Collection&lt;T&gt;"/> class.
            /// </summary>
            /// <param name="dictionary">The dictionary.</param>
            protected Collection(IDictionary<TKey, TValue> dictionary)
            {
                this.Dictionary = dictionary;
            }

          

            /// <summary>
            /// Gets the count.
            /// </summary>
            /// <value>The count.</value>
            public int Count
            {
                get { return this.Dictionary.Count; }
            }

            /// <summary>
            /// Gets a value indicating whether this instance is read only.
            /// </summary>
            /// <value>
            ///     <c>true</c> if this instance is read only; otherwise, <c>false</c>.
            /// </value>
            public bool IsReadOnly
            {
                get { return true; }
            }

            /// <summary>
            /// Adds the specified item.
            /// </summary>
            /// <param name="item">The item.</param>
            public void Add(T item)
            {
                throw new NotSupportedException("Collection is read-only.");
            }

            /// <summary>
            /// Clears this instance.
            /// </summary>
            public void Clear()
            {
                throw new NotSupportedException("Collection is read-only.");
            }


            /// <summary>
            /// Determines whether [contains] [the specified item].
            /// </summary>
            /// <param name="item">The item.</param>
            /// <returns>
            ///     <c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.
            /// </returns>
            public virtual bool Contains(T item)
            {
                foreach (T element in this)
                    if (EqualityComparer<T>.Default.Equals(element, item))
                        return true;
                return false;
            }
        

            /// <summary>
            /// Copies to.
            /// </summary>
            /// <param name="array">The array.</param>
            /// <param name="arrayIndex">Index of the array.</param>
            public void CopyTo(T[] array, int arrayIndex)
            {
                Copy(this, array, arrayIndex);
            }

           

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>An <see cref="IEnumerator{T}"/> of that can be used to iterate through the collection.</returns>
            public IEnumerator<T> GetEnumerator()
            {
                foreach (KeyValuePair<TKey, TValue> pair in this.Dictionary)
                    yield return this.GetItem(pair);
            }


            /// <summary>
            /// Removes the specified item.
            /// </summary>
            /// <param name="item">The item.</param>
            /// <returns>
            ///     <c>true</c> if the item was successfully removed; otherwise, <c>false</c>.
            /// </returns>
            /// <exception cref="NotSupportedException">
            ///     Throw when this method is called. This collection is read only.
            /// </exception>
            public bool Remove(T item)
            {
                throw new NotSupportedException("Collection is read-only.");
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }


            /// <summary>
            /// Gets the item.
            /// </summary>
            /// <param name="pair">The pair.</param>
            /// <returns>an instance of <typeparamref name="T"/>.</returns>
            protected abstract T GetItem(KeyValuePair<TKey, TValue> pair);
        }

#if !SILVERLIGHT
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(PREFIX + "DictionaryKeyCollectionDebugView`2" + SUFFIX)]
#endif
        /// <summary>
        /// The collection of dictionary keys.
        /// </summary>
        private class KeyCollection : Collection<TKey>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BaseDictionary&lt;TKey, TValue&gt;.KeyCollection"/> class.
            /// </summary>
            /// <param name="dictionary">The dictionary.</param>
            public KeyCollection(IDictionary<TKey, TValue> dictionary)
                : base(dictionary) 
            { 
            }


            /// <summary>
            /// Determines whether [contains] [the specified item].
            /// </summary>
            /// <param name="item">The item.</param>
            /// <returns>
            ///     <c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.
            /// </returns>
            public override bool Contains(TKey item)
            {
                return this.Dictionary.ContainsKey(item);
            }

            /// <summary>
            /// Gets the item.
            /// </summary>
            /// <param name="pair">The pair.</param>
            /// <returns>
            ///     An instance of <typeparamref name="TKey"/>.
            /// </returns>
            protected override TKey GetItem(KeyValuePair<TKey, TValue> pair)
            {
                return pair.Key;
            }
        }

#if !SILVERLIGHT
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(PREFIX + "DictionaryValueCollectionDebugView`2" + SUFFIX)]
#endif
        /// <summary>
        /// The collection of values from the dictionary.
        /// </summary>
        private class ValueCollection : Collection<TValue>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BaseDictionary&lt;TKey, TValue&gt;.ValueCollection"/> class.
            /// </summary>
            /// <param name="dictionary">The dictionary.</param>
            public ValueCollection(IDictionary<TKey, TValue> dictionary)
                : base(dictionary) 
            {  
            }

            /// <summary>
            /// Gets the item.
            /// </summary>
            /// <param name="pair">The pair.</param>
            /// <returns>
            ///    An instance of <typeparamref name="TValue"/>.
            /// </returns>
            protected override TValue GetItem(KeyValuePair<TKey, TValue> pair)
            {
                return pair.Value;
            }
        }       
    } 
}
