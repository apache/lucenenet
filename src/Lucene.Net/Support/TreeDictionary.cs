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

using Lucene.Net.Support.C5;
using System;
using System.Collections;
using SCG = System.Collections.Generic;

namespace Lucene.Net.Support
{
    /// <summary>
    /// A sorted generic dictionary based on a red-black tree set.
    /// <para/>
    /// A <see cref="TreeDictionary{TKey, TValue}"/> provides similar behavior to a <see cref="SCG.SortedDictionary{TKey, TValue}"/>,
    /// except that <see cref="TreeDictionary{TKey, TValue}"/> allows elements with null or duplicate keys to be created, where a
    /// <see cref="SCG.SortedDictionary{TKey, TValue}"/> does not.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary</typeparam>
    /// <remarks>
    /// <h2>Unordered Dictionaries</h2>
    /// <list type="bullet">
    ///     <item><description><see cref="SCG.Dictionary{TKey, TValue}"/> - use when order is not important and all keys are non-null.</description></item>
    ///     <item><description><see cref="HashMap{TKey, TValue}"/> - use when order is not important and support for a null key is required.</description></item>
    /// </list>
    /// <h2>Ordered Dictionaries</h2>
    /// <list type="bullet">
    ///     <item><description><see cref="LinkedHashMap{TKey, TValue}"/> - use when you need to preserve entry insertion order. Keys are nullable.</description></item>
    ///     <item><description><see cref="SCG.SortedDictionary{TKey, TValue}"/> - use when you need natural sort order. Keys must be unique.</description></item>
    ///     <item><description><see cref="TreeDictionary{K, V}"/> - use when you need natural sort order. Keys are nullable.</description></item>
    ///     <item><description><see cref="LurchTable{TKey, TValue}"/> - use when you need to sort by most recent access or most recent update. Works well for LRU caching.</description></item>
    /// </list>
    /// </remarks>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public sealed class TreeDictionary<TKey, TValue> : SCG.IDictionary<TKey, TValue>, IDictionary, SCG.IReadOnlyCollection<SCG.KeyValuePair<TKey, TValue>>, SCG.IReadOnlyDictionary<TKey, TValue>
//#if FEATURE_CLONEABLE
//        , ICloneable
//#endif
    {
#if FEATURE_SERIALIZABLE
        [NonSerialized]
#endif
        private KeyCollection keys;

#if FEATURE_SERIALIZABLE
        [NonSerialized]
#endif
        private ValueCollection values;

        private SCG.IComparer<TKey> comparer;
        private C5TreeDictionary<TKey, TValue> dictionary;
        private object _syncRoot;

        #region Constructors

        /// <summary>
        /// Create a red-black tree dictionary using the default <see cref="SCG.IComparer{T}"/> for the key type.
        /// <exception cref="ArgumentException"/> if the key type K is not comparable.
        /// </summary>
        public TreeDictionary()
            : this((SCG.IComparer<TKey>)null)
        { }

        /// <summary>
        /// Create a red-black tree dictionary using the specified <see cref="SCG.IComparer{T}"/> for the key type.
        /// </summary>
        /// <param name="comparer">The <see cref="SCG.IComparer{T}"/> implementation to use when comparing keys, 
        /// or <c>null</c> to use the default <see cref="SCG.Comparer{T}"/> for the type of the key.</param>
        public TreeDictionary(SCG.IComparer<TKey> comparer)
        {
            this.comparer = comparer ?? SCG.Comparer<TKey>.Default;
            this.dictionary = new C5TreeDictionary<TKey, TValue>(this.Comparer);
        }

        /// <summary>
        /// Create a red-black tree dictionary that contains the elements copied from the specified <see cref="IDictionary{K, V}"/>
        /// and uses the default <see cref="SCG.IComparer{T}"/> for the key type.
        /// 
        /// </summary>
        /// <exception cref="ArgumentException"/> if the key type TKey is not comparable.
        public TreeDictionary(SCG.IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        { }

        /// <summary>
        /// Create a red-black tree dictionary that contains the elements copied from the specified <see cref="IDictionary{K, V}"/>
        /// and uses the specified <see cref="SCG.IComparer{T}"/> for the key type.
        /// </summary>
        /// <param name="comparer">The <see cref="SCG.IComparer{T}"/> implementation to use when comparing keys, 
        /// or <c>null</c> to use the default <see cref="SCG.Comparer{T}"/> for the type of the key.</param>
        public TreeDictionary(SCG.IDictionary<TKey, TValue> dictionary, SCG.IComparer<TKey> comparer)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            this.comparer = comparer ?? SCG.Comparer<TKey>.Default;
            this.dictionary = new C5TreeDictionary<TKey, TValue>(dictionary, this.Comparer);
        }

        #endregion

        // NOTE: Can use this to create extension methods with C5 or Java compatibility APIs
        internal C5TreeDictionary<TKey, TValue> Dictionary
        {
            get => dictionary;
            set => dictionary = value;
        }

        #region Nested Type: KeyCollection

        public sealed class KeyCollection : SCG.ICollection<TKey>, ICollection, SCG.IReadOnlyCollection<TKey>
        {
            private readonly TreeDictionary<TKey, TValue> dictionary;

            
            public KeyCollection(TreeDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            }

            public SCG.IEnumerator<TKey> GetEnumerator()
            {
                foreach (var key in dictionary.Dictionary.Keys)
                    yield return key;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void CopyTo(TKey[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), $"Array index must be greater than or equal to 0. Value: '{index}'.");

                // will array, starting at arrayIndex, be able to hold elements? Note: not
                // checking arrayIndex >= array.Length (consistency with list of allowing
                // count of 0; subsequent check takes care of the rest)
                if (index > array.Length || Count > array.Length - index)
                    throw new ArgumentException("Array plus offset is too small");

                foreach (var item in this)
                    array[index++] = item;
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                if (array.Rank != 1)
                    throw new ArgumentException("Multidimentional array not supported.");

                if (array.GetLowerBound(0) != 0)
                    throw new ArgumentException("Non-zero array lower bound.");

                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), $"Array index must be greater than or equal to 0. Value: '{index}'.");

                // will array, starting at arrayIndex, be able to hold elements? Note: not
                // checking arrayIndex >= array.Length (consistency with list of allowing
                // count of 0; subsequent check takes care of the rest)
                if (index > array.Length || Count > array.Length - index)
                    throw new ArgumentException("Array plus offset is too small");

                TKey[] keys = array as TKey[];
                if (keys != null)
                {
                    CopyTo(keys, index);
                }
                else
                {
                    try
                    {
                        object[] objects = (object[])array;
                        foreach (var item in this)
                            objects[index++] = item;
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException($"Array type cannot be cast to {typeof(TKey)}.", nameof(array));
                    }
                }
            }

            public int Count => dictionary.Count;

            public bool IsReadOnly => true;

            void SCG.ICollection<TKey>.Add(TKey item) => throw new NotSupportedException("Collection is read-only");

            void SCG.ICollection<TKey>.Clear() => throw new NotSupportedException("Collection is read-only");

            bool SCG.ICollection<TKey>.Contains(TKey item) => dictionary.ContainsKey(item);

            bool SCG.ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException("Collection is read-only");

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)dictionary).SyncRoot;
        }

        #endregion

        #region Nested Type: ValueCollection

        public sealed class ValueCollection : SCG.ICollection<TValue>, ICollection, SCG.IReadOnlyCollection<TValue>
        {
            private readonly TreeDictionary<TKey, TValue> dictionary;

            public ValueCollection(TreeDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            }

            public SCG.IEnumerator<TValue> GetEnumerator()
            {
                foreach (var value in dictionary.Dictionary.Values)
                    yield return value;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void CopyTo(TValue[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), $"Array index must be greater than or equal to 0. Value: '{index}'.");

                // will array, starting at arrayIndex, be able to hold elements? Note: not
                // checking arrayIndex >= array.Length (consistency with list of allowing
                // count of 0; subsequent check takes care of the rest)
                if (index > array.Length || Count > array.Length - index)
                    throw new ArgumentException("Array plus offset is too small");

                foreach (var item in this)
                    array[index++] = item;
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                if (array.Rank != 1)
                    throw new ArgumentException("Multidimentional array not supported.");

                if (array.GetLowerBound(0) != 0)
                    throw new ArgumentException("Non-zero array lower bound.");


                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), $"Array index must be greater than or equal to 0. Value: '{index}'.");

                // will array, starting at arrayIndex, be able to hold elements? Note: not
                // checking arrayIndex >= array.Length (consistency with list of allowing
                // count of 0; subsequent check takes care of the rest)
                if (index > array.Length || Count > array.Length - index)
                    throw new ArgumentException("Array plus offset is too small");

                TValue[] values = array as TValue[];
                if (values != null)
                {
                    CopyTo(values, index);
                }
                else
                {
                    try
                    {
                        object[] objects = (object[])array;
                        foreach (var item in this)
                            objects[index++] = item;
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException($"Array type cannot be cast to {typeof(TKey)}.", nameof(array));
                    }
                }
            }

            public int Count => dictionary.Count;

            bool SCG.ICollection<TValue>.IsReadOnly => true;

            void SCG.ICollection<TValue>.Add(TValue item) => throw new NotSupportedException("Collection is read-only");

            void SCG.ICollection<TValue>.Clear() => throw new NotSupportedException("Collection is read-only");

            bool SCG.ICollection<TValue>.Contains(TValue item) => dictionary.ContainsValue(item);

            bool SCG.ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException("Collection is read-only");


            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)dictionary).SyncRoot;
        }

        #endregion

        #region Nested Type: Enumerator

        public struct Enumerator : SCG.IEnumerator<SCG.KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            //private TreeSet<KeyValuePair<TKey, TValue>>.Enumerator treeEnum;
            private SCG.IEnumerator<C5.KeyValuePair<TKey, TValue>> treeEnum;
            private int getEnumeratorRetType;  // What should Enumerator.Current return?
            private bool notStartedOrEnded;

            internal const int KeyValuePair = 1;
            internal const int DictEntry = 2;

            internal Enumerator(TreeDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
            {
                notStartedOrEnded = true;
                treeEnum = dictionary.Dictionary.GetEnumerator();
                this.getEnumeratorRetType = getEnumeratorRetType;
            }

            public bool MoveNext()
            {
                var result = treeEnum.MoveNext();
                notStartedOrEnded = !result;
                return result;
            }

            public void Dispose()
            {
                treeEnum.Dispose();
            }

            public SCG.KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    var current = treeEnum.Current;
                    return new SCG.KeyValuePair<TKey, TValue>(current.Key, current.Value);
                }
            }

            internal bool NotStartedOrEnded
            {
                get
                {
                    return notStartedOrEnded;
                    //return treeEnum.NotStartedOrEnded; // LUCENENET TODO: TreeSet.Enumerator.NotStartedOrEnded
                }
            }

            internal void Reset()
            {
                treeEnum.Reset();
            }


            void IEnumerator.Reset()
            {
                treeEnum.Reset();
            }

            object IEnumerator.Current
            {
                get
                {
                    CheckEnumerator();
                    if (getEnumeratorRetType == DictEntry)
                    {
                        return new DictionaryEntry(Current.Key, Current.Value);
                    }
                    else
                    {
                        return new KeyValuePair<TKey, TValue>(Current.Key, Current.Value);
                    }

                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    CheckEnumerator();
                    return Current.Key;
                }
            }

            object IDictionaryEnumerator.Value
            {
                get
                {
                    CheckEnumerator();
                    return Current.Value;
                }
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    CheckEnumerator();
                    return new DictionaryEntry(Current.Key, Current.Value);
                }
            }

            private void CheckEnumerator()
            {
                if (NotStartedOrEnded)
                    throw new InvalidOperationException("Enumeration pointer is either before the beginning or after the end of the collection.");
            }
        }

        #endregion

        #region SCG.SortedDictionary<TKey, TValue> Members

        /// <summary>
        /// Gets the <see cref="SCG.IComparer{T}"/> used to order the elements of the <see cref="TreeDictionary{TKey, TValue}"/>.
        /// </summary>
        public SCG.IComparer<TKey> Comparer => comparer;

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="TreeDictionary{TKey, TValue}"/>.
        /// </summary>
        public int Count => dictionary.Count;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>The value associated with the specified key. If the specified key is not found, a get operation throws a 
        /// <see cref="SCG.KeyNotFoundException"/>, and a set operation creates a new element with the specified key.</returns>
        public TValue this[TKey key]
        {
            get
            {
                if (dictionary.Find(ref key, out TValue value))
                    return value;
                else
                    throw new SCG.KeyNotFoundException($"No element with the key '{key}' exists.");
            }
            set
            {
                GuardReadOnly();
                dictionary[key] = value;
            }
        }

        /// <summary>
        /// Gets a collection containing the keys in the <see cref="TreeDictionary{TKey, TValue}"/>.
        /// </summary>
        public SCG.ICollection<TKey> Keys
        {
            get
            {
                if (keys == null) keys = new KeyCollection(this);
                return keys;
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the <see cref="TreeDictionary{TKey, TValue}"/>.
        /// </summary>
        public SCG.ICollection<TValue> Values
        {
            get
            {
                if (values == null) values = new ValueCollection(this);
                return values;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="SCG.ICollection{T}"/> is read-only.
        /// </summary>
        public bool IsReadOnly => dictionary.IsReadOnly;

        /// <summary>
        /// Adds an element with the specified key and value into the <see cref="TreeDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be <c>null</c> for reference types.</param>
        public void Add(TKey key, TValue value)
        {
            GuardReadOnly();
            C5.KeyValuePair<TKey, TValue> p = new C5.KeyValuePair<TKey, TValue>(key, value);

            if (!dictionary.Pairs.Add(p))
                throw new ArgumentException($"An element with the key '{key}' already exists.");
        }

        /// <summary>
        /// Removes all elements from the <see cref="TreeDictionary{TKey, TValue}"/>.
        /// </summary>
        public void Clear()
        {
            GuardReadOnly();
            dictionary.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="TreeDictionary{TKey, TValue}"/> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="TreeDictionary{TKey, TValue}"/>.</param>
        /// <returns><c>true</c> if the <see cref="TreeDictionary{TKey, TValue}"/> contains an element
        /// with the specified key; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(TKey key) => dictionary.Contains(key);

        /// <summary>
        /// Determines whether the <see cref="TreeDictionary{TKey, TValue}"/> contains an element with the specified value.
        /// </summary>
        /// <param name="value">The value to locate in the <see cref="TreeDictionary{TKey, TValue}"/>.
        /// The value can be null for reference types.</param>
        /// <returns><c>true</c> if the <see cref="TreeDictionary{TKey, TValue}"/> contains an element
        /// with the specified value; otherwise, <c>false</c>.</returns>
        public bool ContainsValue(TValue value) => ((CollectionValueBase<TValue>)dictionary.Values).Exists(v => v.Equals(value));

        /// <summary>
        /// Copies the elements of the <see cref="TreeDictionary{TKey, TValue}"/> to the specified
        /// <paramref name="array"/> of <see cref="SCG.KeyValuePair{TKey, TValue}"/> structures,
        /// starting at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="array">The one-dimensional array of <see cref="SCG.KeyValuePair{TKey, TValue}"/> 
        /// structures that is the destination of the elements copied from the current <see cref="TreeDictionary{TKey, TValue}"/>.
        /// The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(SCG.KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), $"Array index must be greater than or equal to 0. Value: '{index}'.");

            // will array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if (index > array.Length || Count > array.Length - index)
                throw new ArgumentException("Array plus offset is too small");

            foreach (var item in this)
                array[index++] = item;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="TreeDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <returns>An enumerator for the <see cref="TreeDictionary{TKey, TValue}"/>.</returns>
        /// 
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="TreeDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns><c>true</c> if the element is successfully removed; otherwise, <c>false</c>.
        /// This method also returns <c>false</c> if key is not found in the <see cref="TreeDictionary{TKey, TValue}"/>.</returns>
        public bool Remove(TKey key)
        {
            GuardReadOnly();
            return dictionary.Remove(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>,
        /// if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter.</param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value) => dictionary.Find(ref key, out value);

        #endregion

        #region System.Object Overloads

        /// <summary>
        /// Compares the specified object with this dictionary for equality. Returns <c>true</c> if the
        /// given object is also a map and the two maps represent the same mappings. More formally,
        /// two dictionaries <c>m1</c> and <c>m2</c> represent the same mappings if the values of <paramref name="obj"/>
        /// match the values of this dictionary (without regard to order, but with regard to any nested collections).
        /// </summary>
        /// <param name="obj">Object to be compared for equality with this dictionary.</param>
        /// <returns><c>true</c> if the specified object's values are equal to this dictionary.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is SCG.IDictionary<TKey, TValue>))
                return false;

            return Collections.Equals(this, obj as SCG.IDictionary<TKey, TValue>);
        }

        /// <summary>
        /// Returns the hash code value for this dictionary. The hash code of a dictionary is defined to be
        /// the sum of the hash codes of each entry in the dictionary. This ensures that <c>m1.Equals(m2)</c>
        /// implies that <c>m1.GetHashCode() == m2.GetHashCode()</c> for any two dictionaries <c>m1</c> and <c>m2</c>.
        /// </summary>
        /// <returns>The hash code value for this dictionary.</returns>
        public override int GetHashCode()
        {
            return Collections.GetHashCode(this);
        }

        /// <summary>
        /// Returns a string representation of this dictionary. The string representation consists
        /// of a list of key-value mappings in the order returned by the dictionary's iterator, enclosed
        /// in braces ("{}"). Adjacent mappings are separated by the characters ", " (comma and space).
        /// Each key-value mapping is rendered as the key followed by an equals sign ("=") followed by the associated value.
        /// </summary>
        /// <returns>A string representation of this dictionary.</returns>
        public override string ToString()
        {
            return Collections.ToString(this);
        }

        #endregion

        #region ICloneable

        internal object Clone()
        {
            return (TreeDictionary<TKey, TValue>)MemberwiseClone();
        }

//#if FEATURE_CLONEABLE
//        object ICloneable.Clone()
//        {
//            return Clone();
//        }
//#endif

        #endregion

        #region SCG.ICollection<SCG.KeyValuePair<TKey, TValue>> Members

        int SCG.ICollection<SCG.KeyValuePair<TKey, TValue>>.Count => Count;

        bool SCG.ICollection<SCG.KeyValuePair<TKey, TValue>>.IsReadOnly => IsReadOnly;

        void SCG.ICollection<SCG.KeyValuePair<TKey, TValue>>.Add(SCG.KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        void SCG.ICollection<SCG.KeyValuePair<TKey, TValue>>.Clear() => Clear();

        bool SCG.ICollection<SCG.KeyValuePair<TKey, TValue>>.Contains(SCG.KeyValuePair<TKey, TValue> item) => dictionary.Exists((thisItem) => dictionary.EqualityComparer.Equals(thisItem.Key, item.Key) &&  EqualityComparer<TValue>.Default.Equals(thisItem.Value, item.Value));

        void SCG.ICollection<SCG.KeyValuePair<TKey, TValue>>.CopyTo(SCG.KeyValuePair<TKey, TValue>[] array, int index) => CopyTo(array, index);

        bool SCG.ICollection<SCG.KeyValuePair<TKey, TValue>>.Remove(SCG.KeyValuePair<TKey, TValue> item) => Remove(item.Key);

        #endregion

        #region SCG.IDictionary<TKey, TValue> Members
        TValue SCG.IDictionary<TKey, TValue>.this[TKey key] { get => this[key]; set => this[key] = value; }

        SCG.ICollection<TKey> SCG.IDictionary<TKey, TValue>.Keys => Keys;

        SCG.ICollection<TValue> SCG.IDictionary<TKey, TValue>.Values => Values;

        void SCG.IDictionary<TKey, TValue>.Add(TKey key, TValue value) => Add(key, value);

        bool SCG.IDictionary<TKey, TValue>.ContainsKey(TKey key) => ContainsKey(key);

        bool SCG.IDictionary<TKey, TValue>.Remove(TKey key) => Remove(key);

        bool SCG.IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => TryGetValue(key, out value);

        #endregion

        #region SCG.IReadOnlyDictionary<TKey, TValue> Members

        TValue SCG.IReadOnlyDictionary<TKey, TValue>.this[TKey key] => this[key];

        SCG.IEnumerable<TKey> SCG.IReadOnlyDictionary<TKey, TValue>.Keys => this.Keys;

        SCG.IEnumerable<TValue> SCG.IReadOnlyDictionary<TKey, TValue>.Values => this.Values;

        bool SCG.IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key) => this.ContainsKey(key);

        bool SCG.IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => this.TryGetValue(key, out value);

        #endregion

        #region SCG.IEnumerable<SCG.KeyValuePair<TKey, TValue>> Members

        SCG.IEnumerator<SCG.KeyValuePair<TKey, TValue>> SCG.IEnumerable<SCG.KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        #endregion

        #region SCG.IReadOnlyCollection<SCG.KeyValuePair<TKey, TValue>> Members
        int SCG.IReadOnlyCollection<SCG.KeyValuePair<TKey, TValue>>.Count => Count;

        #endregion

        #region IDictionary Members

        bool IDictionary.IsFixedSize => false;

        bool IDictionary.IsReadOnly => IsReadOnly;

        ICollection IDictionary.Keys => (ICollection)Keys;

        ICollection IDictionary.Values => (ICollection)Values;

        object IDictionary.this[object key]
        {
            get
            {
                if (IsCompatibleKey(key))
                {
                    if (TryGetValue((TKey)key, out TValue value))
                    {
                        return value;
                    }
                }

                return null;
            }
            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (value == null && !(default(TValue) == null))
                    throw new ArgumentNullException(nameof(value));

                try
                {
                    TKey tempKey = (TKey)key;
                    try
                    {
                        this[tempKey] = (TValue)value;
                    }
                    catch (InvalidCastException)
                    {
                        throw new ArgumentException($"Value '{value}' cannot be cast to type '{typeof(TValue)}'.", nameof(value));
                    }
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException($"Key '{key}' cannot be cast to type '{typeof(TValue)}'.", nameof(key));
                }
            }
        }

        void IDictionary.Add(object key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (value == null && !(default(TValue) == null))
                throw new ArgumentNullException(nameof(value));

            try
            {
                TKey tempKey = (TKey)key;
                try
                {
                    Add(tempKey, (TValue)value);
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException($"Value '{value}' cannot be cast to type '{typeof(TValue)}'.", nameof(value));
                }
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException($"Key '{key}' cannot be cast to type '{typeof(TValue)}'.", nameof(key));
            }
        }

        void IDictionary.Clear() => Clear();

        bool IDictionary.Contains(object key)
        {
            if (IsCompatibleKey(key))
            {
                return ContainsKey((TKey)key);
            }
            return false;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator(this, Enumerator.DictEntry);
        }

        void IDictionary.Remove(object key)
        {
            if (IsCompatibleKey(key))
            {
                Remove((TKey)key);
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Rank != 1)
                throw new ArgumentException("Multidimentional array not supported.");

            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException("Non-zero array lower bound.");

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), $"Array index must be greater than or equal to 0. Value: '{index}'.");

            // will array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if (index > array.Length || Count > array.Length - index)
                throw new ArgumentException("Array plus offset is too small");

            SCG.KeyValuePair<TKey, TValue>[] pairs = array as SCG.KeyValuePair<TKey, TValue>[];
            if (pairs != null)
            {
                CopyTo(pairs, index);
            }
            else
            {
                try
                {
                    object[] objects = (object[])array;
                    foreach (var item in this)
                        objects[index++] = item;
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException($"Array type cannot be cast to {typeof(SCG.KeyValuePair<TKey, TValue>)}.", nameof(array));
                }
            }
        }

        #endregion

        #region ICollection

        int ICollection.Count => Count;

        /// <summary>
        /// Gets a value that indicates whether access to the <see cref="ICollection"/> is synchronized (thread safe).
        /// </summary>
        bool ICollection.IsSynchronized => false;

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>.
        /// </summary>
        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }

        #endregion

        #region Private Members

        private static bool IsCompatibleKey(object key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return (key is TKey);
        }

        private void GuardReadOnly()
        {
            if (dictionary.IsReadOnly)
                throw new NotSupportedException("Collection is read-only.");
        }

        #endregion
    }

    /// <summary>
    /// A subclass of <see cref="C5.TreeDictionary{K, V}"/> to expose protected
    /// fields as public properties.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal class C5TreeDictionary<TKey, TValue> : C5.TreeDictionary<TKey, TValue>
    {
        /// <summary>
        /// Create a red-black tree dictionary using an external comparer for keys.
        /// </summary>
        /// <param name="comparer">The external comparer</param>
        public C5TreeDictionary(SCG.IDictionary<TKey, TValue> dictionary, SCG.IComparer<TKey> comparer) 
            : base(comparer)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary.Count > 0)
            {
                foreach (var item in dictionary)
                    Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Create a red-black tree dictionary using an external comparer for keys.
        /// </summary>
        /// <param name="comparer">The external comparer</param>
        public C5TreeDictionary(SCG.IComparer<TKey> comparer)
            : base(comparer)
        { }

        public C5.ICollection<C5.KeyValuePair<TKey, TValue>> Pairs
        {
            get => pairs;
            set => pairs = value;
        }

        public C5.ISorted<C5.KeyValuePair<TKey, TValue>> SortedPairs
        {
            get => sortedpairs;
            set => sortedpairs = value;
        }
    }
}

namespace Lucene.Net.Support.C5Compatibility
{
    /// <summary>
    /// Extension methods that expose similar APIs to that of the C5 library.
    /// </summary>
    internal static class TreeDictionaryExtensions
    {
        /// <summary>
        /// Check the integrity of the internal data structures of this dictionary.
        /// </summary>
        /// <returns>True if check does not fail.</returns>
        public static bool Check<TKey, TValue>(this TreeDictionary<TKey, TValue> tree)
        {
            return tree.Dictionary.Pairs.Check();
        }

        //TODO: put in interface
        /// <summary>
        /// Make a snapshot of the current state of this dictionary
        /// </summary>
        /// <returns>The snapshot</returns>
        public static TreeDictionary<TKey, TValue> Snapshot<TKey, TValue>(this TreeDictionary<TKey, TValue> tree)
        {
            TreeDictionary<TKey, TValue> res = (TreeDictionary<TKey, TValue>)tree.Clone();

            res.Dictionary = (C5TreeDictionary<TKey, TValue>)tree.Dictionary.Snapshot();
            return res;
        }

        ///// <summary>
        ///// Find the current least item of this sorted collection.
        ///// </summary>
        ///// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        ///// <returns>The least item.</returns>
        //public static SCG.KeyValuePair<TKey, TValue> FindMin<TKey, TValue>(this TreeDictionary<TKey, TValue> tree)
        //{
        //    return tree.Dictionary.FindMin();
        //}


        ///// <summary>
        ///// Remove the least item from this sorted collection.
        ///// </summary>
        ///// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        ///// <returns>The removed item.</returns>
        //public static SCG.KeyValuePair<TKey, TValue> DeleteMin<TKey, TValue>(this TreeDictionary<TKey, TValue> tree);


        ///// <summary>
        ///// Find the current largest item of this sorted collection.
        ///// </summary>
        ///// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        ///// <returns>The largest item.</returns>
        //public static SCG.KeyValuePair<TKey, TValue> FindMax<TKey, TValue>(this TreeDictionary<TKey, TValue> tree);


        ///// <summary>
        ///// Remove the largest item from this sorted collection.
        ///// </summary>
        ///// <exception cref="NoSuchItemException"> if the collection is empty.</exception>
        ///// <returns>The removed item.</returns>
        //public static SCG.KeyValuePair<TKey, TValue> DeleteMax<TKey, TValue>(this TreeDictionary<TKey, TValue> tree);


        /// <summary>
        /// Find the entry in the dictionary whose key is the
        /// predecessor of the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="result">The predecessor, if any</param>
        /// <returns>True if key has a predecessor</returns>
        // Same as Java lowerEntry()
        public static bool TryPredecessor<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, TKey key, out SCG.KeyValuePair<TKey, TValue> result)
        {
            bool success = tree.Dictionary.TryPredecessor(key, out KeyValuePair<TKey, TValue> res);
            result = new SCG.KeyValuePair<TKey, TValue>(res.Key, res.Value);
            return success;
        }

        /// <summary>
        /// Find the entry in the dictionary whose key is the
        /// successor of the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="result">The successor, if any</param>
        /// <returns>True if the key has a successor</returns>
        // Same as Java higherEntry()
        public static bool TrySuccessor<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, TKey key, out SCG.KeyValuePair<TKey, TValue> result)
        {
            bool success = tree.Dictionary.TrySuccessor(key, out KeyValuePair<TKey, TValue> res);
            result = new SCG.KeyValuePair<TKey, TValue>(res.Key, res.Value);
            return success;
        }

        ///// <summary>
        ///// Find the entry in the dictionary whose key is the
        ///// weak predecessor of the specified key.
        ///// </summary>
        ///// <param name="key">The key</param>
        ///// <param name="result">The predecessor, if any</param>
        ///// <returns>True if key has a weak predecessor</returns>
        //public static bool TryWeakPredecessor<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, TKey key, out SCG.KeyValuePair<TKey, TValue> result) => dictionary.TryWeakPredecessor(key, out result);

        ///// <summary>
        ///// Find the entry in the dictionary whose key is the
        ///// weak successor of the specified key.
        ///// </summary>
        ///// <param name="key">The key</param>
        ///// <param name="result">The weak successor, if any</param>
        ///// <returns>True if the key has a weak successor</returns>
        //public static bool TryWeakSuccessor<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, TKey key, out SCG.KeyValuePair<TKey, TValue> result) => dictionary.TryWeakSuccessor(key, out result);

        ///// <summary>
        ///// Get the entry in the dictionary whose key is the
        ///// predecessor of the specified key.
        ///// </summary>
        ///// <exception cref="NoSuchItemException"></exception>
        ///// <param name="key">The key</param>
        ///// <returns>The entry</returns>
        //public static SCG.KeyValuePair<TKey, TValue> Predecessor<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, TKey key)
        //{
        //    return sortedpairs.Predecessor(new KeyValuePair<TKey, TValue>(key));
        //}

        ///// <summary>
        ///// Get the entry in the dictionary whose key is the
        ///// successor of the specified key.
        ///// </summary>
        ///// <exception cref="NoSuchItemException"></exception>
        ///// <param name="key">The key</param>
        ///// <returns>The entry</returns>
        //public static SCG.KeyValuePair<TKey, TValue> Successor<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, TKey key)
        //{
        //    return sortedpairs.Successor(new KeyValuePair<TKey, TValue>(key));
        //}

        ///// <summary>
        ///// Get the entry in the dictionary whose key is the
        ///// weak predecessor of the specified key.
        ///// </summary>
        ///// <exception cref="NoSuchItemException"></exception>
        ///// <param name="key">The key</param>
        ///// <returns>The entry</returns>
        //public static SCG.KeyValuePair<TKey, TValue> WeakPredecessor<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, TKey key)
        //{
        //    return sortedpairs.WeakPredecessor(new KeyValuePair<TKey, TValue>(key));
        //}

        ///// <summary>
        ///// Get the entry in the dictionary whose key is the
        ///// weak successor of the specified key.
        ///// </summary>
        ///// <exception cref="NoSuchItemException"></exception>
        ///// <param name="key">The key</param>
        ///// <returns>The entry</returns>
        //public static SCG.KeyValuePair<TKey, TValue> WeakSuccessor<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, TKey key)
        //{
        //    return sortedpairs.WeakSuccessor(new KeyValuePair<TKey, TValue>(key));
        //}

        ///// <summary>
        ///// Given a "cut" function from the items of the sorted collection to <code>int</code>
        ///// whose only sign changes when going through items in increasing order
        ///// can be 
        ///// <list>
        ///// <item><description>from positive to zero</description></item>
        ///// <item><description>from positive to negative</description></item>
        ///// <item><description>from zero to negative</description></item>
        ///// </list>
        ///// The "cut" function is supplied as the <code>CompareTo</code> method 
        ///// of an object <code>c</code> implementing 
        ///// <code>IComparable&lt;T&gt;</code>. 
        ///// A typical example is the case where <code>T</code> is comparable and 
        ///// <code>cutFunction</code> is itself of type <code>T</code>.
        ///// <para>This method performs a search in the sorted collection for the ranges in which the
        ///// "cut" function is negative, zero respectively positive. If <code>T</code> is comparable
        ///// and <code>c</code> is of type <code>T</code>, this is a safe way (no exceptions thrown) 
        ///// to find predecessor and successor of <code>c</code>.
        ///// </para>
        ///// <para> If the supplied cut function does not satisfy the sign-change condition, 
        ///// the result of this call is undefined.
        ///// </para>
        ///// 
        ///// </summary>
        ///// <param name="cutFunction">The cut function <code>T</code> to <code>int</code>, given
        ///// by the <code>CompareTo</code> method of an object implementing 
        ///// <code>IComparable&lt;T&gt;</code>.</param>
        ///// <param name="low">Returns the largest item in the collection, where the
        ///// cut function is positive (if any).</param>
        ///// <param name="lowIsValid">Returns true if the cut function is positive somewhere
        ///// on this collection.</param>
        ///// <param name="high">Returns the least item in the collection, where the
        ///// cut function is negative (if any).</param>
        ///// <param name="highIsValid">Returns true if the cut function is negative somewhere
        ///// on this collection.</param>
        ///// <returns>True if the cut function is zero somewhere
        ///// on this collection.</returns>
        //public static Cut<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, IComparable<T> cutFunction, out T low, out bool lowIsValid, out T high, out bool highIsValid);


        ///// <summary>
        ///// Query this sorted collection for items greater than or equal to a supplied value.
        ///// <para>The returned collection is not a copy but a view into the collection.</para>
        ///// <para>The view is fragile in the sense that changes to the underlying collection will 
        ///// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        ///// </summary>
        ///// <param name="bot">The lower bound (inclusive).</param>
        ///// <returns>The result directed collection.</returns>
        //public static IDirectedEnumerable<T> RangeFrom<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, T bot);


        ///// <summary>
        ///// Query this sorted collection for items between two supplied values.
        ///// <para>The returned collection is not a copy but a view into the collection.</para>
        ///// <para>The view is fragile in the sense that changes to the underlying collection will 
        ///// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        ///// </summary>
        ///// <param name="bot">The lower bound (inclusive).</param>
        ///// <param name="top">The upper bound (exclusive).</param>
        ///// <returns>The result directed collection.</returns>
        //public static IDirectedEnumerable<T> RangeFromTo<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, T bot, T top);


        ///// <summary>
        ///// Query this sorted collection for items less than a supplied value.
        ///// <para>The returned collection is not a copy but a view into the collection.</para>
        ///// <para>The view is fragile in the sense that changes to the underlying collection will 
        ///// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        ///// </summary>
        ///// <param name="top">The upper bound (exclusive).</param>
        ///// <returns>The result directed collection.</returns>
        //public static IDirectedEnumerable<T> RangeTo<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, T top);


        ///// <summary>
        ///// Create a directed collection with the same items as this collection.
        ///// <para>The returned collection is not a copy but a view into the collection.</para>
        ///// <para>The view is fragile in the sense that changes to the underlying collection will 
        ///// invalidate the view so that further operations on the view throws InvalidView exceptions.</para>
        ///// </summary>
        ///// <returns>The result directed collection.</returns>
        //public static IDirectedCollectionValue<T> RangeAll<TKey, TValue>(this TreeDictionary<TKey, TValue> tree);


        ////TODO: remove now that we assume that we can check the sorting order?
        ///// <summary>
        ///// Add all the items from another collection with an enumeration order that 
        ///// is increasing in the items.
        ///// </summary>
        ///// <exception cref="ArgumentException"> if the enumerated items turns out
        ///// not to be in increasing order.</exception>
        ///// <param name="items">The collection to add.</param>
        //public static void AddSorted<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, SCG.IEnumerable<T> items);


        ///// <summary>
        ///// Remove all items of this collection above or at a supplied threshold.
        ///// </summary>
        ///// <param name="low">The lower threshold (inclusive).</param>
        //public static void RemoveRangeFrom<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, T low);


        ///// <summary>
        ///// Remove all items of this collection between two supplied thresholds.
        ///// </summary>
        ///// <param name="low">The lower threshold (inclusive).</param>
        ///// <param name="hi">The upper threshold (exclusive).</param>
        //public static void RemoveRangeFromTo<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, T low, T hi);


        ///// <summary>
        ///// Remove all items of this collection below a supplied threshold.
        ///// </summary>
        ///// <param name="hi">The upper threshold (exclusive).</param>
        //public static void RemoveRangeTo<TKey, TValue>(this TreeDictionary<TKey, TValue> tree, T hi);
    }
}



namespace Lucene.Net.Support.C5
{
    /// <summary>
    /// A sorted generic dictionary based on a red-black tree set.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class TreeDictionary<K, V> : SortedDictionaryBase<K, V>, IDictionary<K, V>, ISortedDictionary<K, V>
    {

        #region Constructors

        /// <summary>
        /// Create a red-black tree dictionary using the natural comparer for keys.
        /// <exception cref="ArgumentException"/> if the key type K is not comparable.
        /// </summary>
        public TreeDictionary(MemoryType memoryType = MemoryType.Normal) : this(SCG.Comparer<K>.Default, C5.EqualityComparer<K>.Default, memoryType) { }

        /// <summary>
        /// Create a red-black tree dictionary using an external comparer for keys.
        /// </summary>
        /// <param name="comparer">The external comparer</param>
        /// <param name = "memoryType"></param>
        public TreeDictionary(SCG.IComparer<K> comparer, MemoryType memoryType = MemoryType.Normal) : this(comparer, new ComparerZeroHashCodeEqualityComparer<K>(comparer)) { }

        TreeDictionary(SCG.IComparer<K> comparer, SCG.IEqualityComparer<K> equalityComparer, MemoryType memoryType = MemoryType.Normal)
            : base(comparer, equalityComparer, memoryType)
        {
            pairs = sortedpairs = new TreeSet<KeyValuePair<K, V>>(new KeyValuePairComparer<K, V>(comparer));
            if (memoryType != MemoryType.Normal)
                throw new Exception("TreeDictionary doesn't support MemoryType Strict or Safe");

        }

        #endregion

        //TODO: put in interface
        /// <summary>
        /// Make a snapshot of the current state of this dictionary
        /// </summary>
        /// <returns>The snapshot</returns>
        public SCG.IEnumerable<KeyValuePair<K, V>> Snapshot()
        {
            TreeDictionary<K, V> res = (TreeDictionary<K, V>)MemberwiseClone();

            res.pairs = (TreeSet<KeyValuePair<K, V>>)((TreeSet<KeyValuePair<K, V>>)sortedpairs).Snapshot();
            return res;
        }
    }
}