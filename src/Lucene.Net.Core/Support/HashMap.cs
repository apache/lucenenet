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

namespace Lucene.Net.Support
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;

    /// <summary>
    /// A C# emulation of the <a href="http://download.oracle.com/javase/1,5.0/docs/api/java/util/HashMap.html">Java Hashmap</a>
    /// <para>
    /// A <see cref="Dictionary{TKey, TValue}" /> is a close equivalent to the Java
    /// HashMap.  One difference java implementation of the class is that
    /// the HashMap supports both null keys and values, where the C# Dictionary
    /// only supports null values not keys.  Also, <c>V Get(TKey)</c>
    /// method in Java returns null if the key doesn't exist, instead of throwing
    /// an exception.  This implementation doesn't throw an exception when a key 
    /// doesn't exist, it will return null.  This class is slower than using a 
    /// <see cref="Dictionary{TKey, TValue}"/>, because of extra checks that have to be
    /// done on each access, to check for null.
    /// </para>
    /// <para>
    /// <b>NOTE:</b> This class works best with nullable types.  default(T) is returned
    /// when a key doesn't exist in the collection (this being similar to how Java returns
    /// null).  Therefore, if the expected behavior of the java code is to execute code
    /// based on if the key exists, when the key is an integer type, it will return 0 instead of null.
    /// </para>
    /// <remarks>
    /// Consider also implementing IDictionary, IEnumerable, and ICollection
    /// like <see cref="Dictionary{TKey, TValue}" /> does, so HashMap can be
    /// used in substituted in place for the same interfaces it implements.
    /// </remarks>
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary</typeparam>
    // ReSharper disable CSharpWarnings::CS1574
    [Serializable]
    public class HashMap<TKey, TValue> : IDictionary<TKey, TValue>
    {
        internal IEqualityComparer<TKey> Comparer;
        internal IDictionary<TKey, TValue> Dictionary;

        // Indicates if a null key has been assigned, used for iteration
        private bool hasNullValue;
        // stores the value for the null key
        private TValue nullValue;
        // Indicates the type of key is a non-nullable value type
        private readonly bool isValueType;

        /// <summary>
        /// Initializes a new instance of the <see cref="HashMap{TKey, TValue}"/> class.
        /// </summary>
        public HashMap()
            : this(0)
        { }

        public HashMap(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HashMap{TKey, TValue}"/> class with an initial capacity
        /// </summary>
        /// <param name="initialCapacity">The number of elements that the new <see cref="HashMap{TKey, TValue}"/> can store.</param>
        public HashMap(int initialCapacity)
            : this(initialCapacity, EqualityComparer<TKey>.Default)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HashMap{TKey, TValue}"/> class with an initial capacity and an alternative
        /// comparer.
        /// </summary>
        /// <param name="initialCapacity">The number of elements that the new <see cref="HashMap{TKey, TValue}"/> can store.</param>
        /// <param name="comparer">The </param>
        public HashMap(int initialCapacity, IEqualityComparer<TKey> comparer)
            : this(new Dictionary<TKey, TValue>(initialCapacity, comparer), comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HashMap{TKey, TValue}"/> class that contains elements copied from a collection.
        /// </summary>
        /// <param name="collection">The collection of elements that will be copied into the instance.</param>
        [SuppressMessage("Microsoft.Usage","CA2214", Justification = "By Design")]
        public HashMap(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            : this(0)
        {
            foreach (var kvp in collection)
            {
                // ReSharper disable once DoNotCallOverridableMethodsInConstructor
                Add(kvp.Key, kvp.Value);
            }
        }

        internal HashMap(IDictionary<TKey, TValue> wrappedDictionary, IEqualityComparer<TKey> comparer)
        {
            this.Comparer = comparer ?? EqualityComparer<TKey>.Default;
            this.Dictionary = wrappedDictionary;
            this.hasNullValue = false;


            if (typeof(TKey).GetTypeInfo().IsValueType)
            {
                this.isValueType = Nullable.GetUnderlyingType(typeof(TKey)) == null;
            }
        }

        /// <summary>
        ///  Determines whether the <see cref="HashMap{TKey, TValue}"/> contains an element
        ///  with the specified value.
        /// </summary>
        /// <param name="value">The object that the <see cref="HashMap{TKey, TValue}"/> may contain.</param>
        /// <returns>True, if the <see cref="HashMap{TKey, TValue}"/> contains the value; otherwise, false.</returns>
        public bool ContainsValue(TValue value)
        {
            if (!this.isValueType && this.hasNullValue && this.nullValue.Equals(value))
                return true;

            return this.Dictionary.Values.Contains(value);
        }

        /*
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public TValue AddIfAbsent(TKey key, TValue value)
        {
            if (!ContainsKey(key))
            {
                Add(key, value);
                return default(TValue);
            }
            return this[key];
        } */

        #region Implementation of IEnumerable

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="System.Collections.Generic.IEnumerator{T}"/> that can be used to iterate through
        //./    the collection.
        /// </returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (!this.isValueType && this.hasNullValue)
            {
                yield return new KeyValuePair<TKey, TValue>(default(TKey), this.nullValue);
            }
            foreach (var kvp in this.Dictionary)
            {
                yield return kvp;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Implementation of ICollection<KeyValuePair<TKey,TValue>>

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Removes all items from the <see cref="HashMap{TKey, TValue}"/>.
        /// </summary>
        public void Clear()
        {
            this.hasNullValue = false;
            this.nullValue = default(TValue);
            this.Dictionary.Clear();
        }


        /// <summary>
        ///  Determines whether the <see cref="HashMap{TKey, TValue}"/> contains a specific
        ///     value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="HashMap{TKey, TValue}"/></param>
        /// <returns>  True, if item is found in the <see cref="HashMap{TKey, TValue}"/> otherwise,
        ///     false.</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (!this.isValueType && this.Comparer.Equals(item.Key, default(TKey)))
            {
                return this.hasNullValue && EqualityComparer<TValue>.Default.Equals(item.Value, this.nullValue);
            }

            return this.Dictionary.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="HashMap{TKey, TValue}"/> to an <see cref="System.Array"/>,
        /// starting at a particular <see cref="System.Array"/>y index.
        /// </summary>
        /// <param name="array">
        ///  The one-dimensional <see cref="System.Array"/> that is the destination of the elements copied
        ///  from <see cref="HashMap{TKey, TValue}" /> . The <see cref="System.Array"/> must have zero-based
        ///     indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Check.NotNull("array", array);

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex", "arrayIndex must be 0 or greater.");

            this.Dictionary.CopyTo(array, arrayIndex);
            if (!this.isValueType && this.hasNullValue)
            {
                array[array.Length - 1] = new KeyValuePair<TKey, TValue>(default(TKey), this.nullValue);
            }


        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="HashMap{TKey, TValue}"/>
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="HashMap{TKey, TValue}"/></param>
        /// <returns>
        /// True if item was successfully removed from the <see cref="HashMap{TKey, TValue}"/>
        /// otherwise, false.
        /// </returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!this.isValueType && this.Comparer.Equals(item.Key, default(TKey)))
            {
                if (!this.hasNullValue)
                    return false;

                this.hasNullValue = false;
                this.nullValue = default(TValue);
                return true;
            }

            return this.Dictionary.Remove(item);
        }

        /// <summary>
        ///  Gets the number of elements contained in the <see cref="HashMap{TKey, TValue}"/>
        /// </summary>
        public int Count
        {
            get { return this.Dictionary.Count + (this.hasNullValue ? 1 : 0); }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="HashMap{TKey, TValue}"/>
        /// is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region Implementation of IDictionary<TKey,TValue>

        /// <summary>
        /// Determines whether the <see cref="HashMap{TKey, TValue}"/> contains an element
        ///  with the specified key.
        /// </summary>
        /// <param name="key">
        ///  An identifier that is used to retrieve the value.  
        /// </param>
        /// <returns>
        /// True, if the <see cref="HashMap{TKey, TValue}"/> contains an element with
        /// the key; otherwise, false.
        /// </returns>
        public bool ContainsKey(TKey key)
        {
            if (!this.isValueType && this.Comparer.Equals(key, default(TKey)))
            {
                if (this.hasNullValue)
                {
                    return true;
                }
                return false;
            }

            return this.Dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="HashMap{TKey, TValue}"/>
        /// </summary>
        /// <param name="key">An identifier that is used to lookup the value.</param>
        /// <param name="value">The object associated with the key.</param>
        public virtual void Add(TKey key, TValue value)
        {
            if (!this.isValueType && this.Comparer.Equals(key, default(TKey)))
            {
                this.hasNullValue = true;
                this.nullValue = value;
            }
            else
            {
                this.Dictionary[key] = value;
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="HashMap{TKey, TValue}"/>
        /// </summary>
        /// <param name="key">An identifier that is used to specify which <see cref="KeyValuePair{TKey, TValue}"/> should be removed.</param>
        /// <returns>True, if the element is successfully removed; otherwise, false. </returns>
        public bool Remove(TKey key)
        {
            if (this.isValueType || !this.Comparer.Equals(key, default(TKey))) 
                return this.Dictionary.Remove(key);
            
            this.hasNullValue = false;
            this.nullValue = default(TValue);
            return true;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">An identifier that is used to retrieve the value.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the
        ///     key is found; otherwise, the default value for the type of the value parameter.
        ///     This parameter is passed uninitialized.
        ///  </param>
        /// <returns>True, if the <see cref="HashMap{TKey, TValue}"/> contains
        ///     an element with the specified key; otherwise, false.
        /// </returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (this.isValueType || !this.Comparer.Equals(key, default(TKey)))
                return this.Dictionary.TryGetValue(key, out value);

            if (this.hasNullValue)
            {
                value = this.nullValue;
                return true;
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key">An identifier that is used to retrieve the value.</param>
        /// <returns>The element specified by the key.</returns>
        public TValue this[TKey key]
        {
            get
            {
                if (!this.isValueType && this.Comparer.Equals(key, default(TKey)))
                {
                    if (!this.hasNullValue)
                    {
                        return default(TValue);
                    }
                    return this.nullValue;
                }
                return this.Dictionary.ContainsKey(key) ? this.Dictionary[key] : default(TValue);
            }
            set { Add(key, value); }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                if (!this.hasNullValue) return this.Dictionary.Keys;

                // Using a List<T> to generate an ICollection<TKey>
                // would incur a costly copy of the dict's KeyCollection
                // use out own wrapper instead
                return new NullKeyCollection(this.Dictionary);
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (!this.hasNullValue) return this.Dictionary.Values;

                // Using a List<T> to generate an ICollection<TValue>
                // would incur a costly copy of the dict's ValueCollection
                // use out own wrapper instead
                return new NullValueCollection(this.Dictionary, this.nullValue);
            }
        }

        #endregion

        #region NullValueCollection

        /// <summary>
        /// Wraps a dictionary and adds the value
        /// represented by the null key
        /// </summary>
        class NullValueCollection : ICollection<TValue>
        {
            private readonly TValue nullValue;
            private readonly IDictionary<TKey, TValue> internalDictionary;

            public NullValueCollection(IDictionary<TKey, TValue> dictionary, TValue nullValue)
            {
                this.internalDictionary = dictionary;
                this.nullValue = nullValue;
            }

            #region Implementation of IEnumerable

            public IEnumerator<TValue> GetEnumerator()
            {
                yield return nullValue;

                foreach (var val in internalDictionary.Values)
                {
                    yield return val;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            #region Implementation of ICollection<TValue>

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                throw new NotImplementedException("Implement as needed");
            }

            public int Count
            {
                get { return internalDictionary.Count + 1; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            #region Explicit Interface Methods

            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                throw new NotSupportedException();
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException("Collection is read only!");
            }
            #endregion

            #endregion
        }

        #endregion

        #region NullKeyCollection
        /// <summary>
        /// Wraps a dictionary's collection, adding in a
        /// null key.
        /// </summary>
        class NullKeyCollection : ICollection<TKey>
        {
            private readonly IDictionary<TKey, TValue> internalDictionary;

            public NullKeyCollection(IDictionary<TKey, TValue> dictionary)
            {
                this.internalDictionary = dictionary;
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                yield return default(TKey);
                foreach (var key in internalDictionary.Keys)
                {
                    yield return key;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                throw new NotImplementedException("Implement this as needed");
            }

            public int Count
            {
                get { return internalDictionary.Count + 1; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            #region Explicit Interface Definitions
            bool ICollection<TKey>.Contains(TKey item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TKey>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException();
            }
            #endregion
        }
        #endregion
    }
}