using J2N.Runtime.InteropServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JCG = J2N.Collections.Generic;
#nullable enable

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

    /// <summary>
    /// Provides extension methods for performing dictionary operations using
    /// <see cref="ReadOnlySpan{T}"/> keys.
    /// </summary>
    internal static partial class DictionaryExtensions
    {
        #region ContainsKey

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="key">The key to locate.</param>
        /// <returns>
        /// <c>true</c> if the dictionary contains an element with the specified key;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The dictionary's comparer does not support alternate lookup using a character span.
        /// </exception>
        public static bool ContainsKey<TValue>(this JCG.Dictionary<string, TValue> dictionary, ReadOnlySpan<char> key)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the ordered dictionary contains the specified key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The ordered dictionary to search.</param>
        /// <param name="key">The key to locate.</param>
        /// <returns><c>true</c> if the dictionary contains the key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The comparer does not support alternate lookup.</exception>
        public static bool ContainsKey<TValue>(this JCG.OrderedDictionary<string, TValue> dictionary, ReadOnlySpan<char> key)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="key">The key to locate.</param>
        /// <returns><c>true</c> if the dictionary contains the key; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Known dictionary implementations use alternate lookup where supported.
        /// Other implementations are searched using a string created from
        /// <paramref name="key"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">
        /// A recognized dictionary's comparer does not support alternate lookup.
        /// </exception>
        public static bool ContainsKey<TValue>(this IDictionary<string, TValue> dictionary, ReadOnlySpan<char> key)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary is JCG.Dictionary<string, TValue> jcgDictionary)
                return jcgDictionary.GetSpanAlternateLookup<char>().ContainsKey(key);

            if (dictionary is JCG.OrderedDictionary<string, TValue> orderedDictionary)
                return orderedDictionary.GetSpanAlternateLookup<char>().ContainsKey(key);

            if (dictionary is JCG.SortedDictionary<string, TValue> sortedDictionary)
                return sortedDictionary.GetSpanAlternateLookup<char>().ContainsKey(key);

#if FEATURE_DICTIONARY_ALTERNATELOOKUP
            if (dictionary is Dictionary<string, TValue> bclDictionary)
                return bclDictionary.GetAlternateLookup<ReadOnlySpan<char>>().ContainsKey(key);

            if (dictionary is ConcurrentDictionary<string, TValue> concurrentDictionary)
                return concurrentDictionary.GetAlternateLookup<ReadOnlySpan<char>>().ContainsKey(key);
#endif

            return dictionary.ContainsKey(key.ToString());
        }

        #endregion

        #region Put

        /// <summary>
        /// Associates the specified value with the specified key, returning
        /// the previous value associated with the key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to modify.</param>
        /// <param name="key">The key whose value is to be set.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <returns>
        /// The previous value associated with the key, or the default value
        /// for <typeparamref name="TValue"/> if the key was not present.
        /// </returns>
        /// <remarks>
        /// A default return value does not distinguish between an absent key
        /// and a key previously associated with the default value.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The dictionary's comparer does not support alternate lookup using a character span.
        /// </exception>
        public static TValue Put<TValue>(this JCG.Dictionary<string, TValue> dictionary, ReadOnlySpan<char> key, TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            ref TValue? slot = ref CollectionMarshal.GetValueRefOrAddDefault(
                dictionary.GetSpanAlternateLookup<char>(), key, out bool exists);

            TValue previousValue = exists ? slot! : default!;
            slot = value;
            return previousValue;
        }

        /// <summary>
        /// Associates the specified value with the specified key and returns its previous value.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The ordered dictionary to modify.</param>
        /// <param name="key">The key whose value is to be set.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <returns>The previous value, or the default value of <typeparamref name="TValue"/> if the key was absent.</returns>
        /// <remarks>A default return value does not distinguish an absent key from one previously associated with the default value.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The comparer does not support alternate lookup.</exception>
        public static TValue Put<TValue>(this JCG.OrderedDictionary<string, TValue> dictionary, ReadOnlySpan<char> key, TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            ref TValue? slot = ref CollectionMarshal.GetValueRefOrAddDefault(
                dictionary.GetSpanAlternateLookup<char>(), key, out bool exists);

            TValue previousValue = exists ? slot! : default!;
            slot = value;
            return previousValue;
        }

        /// <summary>
        /// Associates the specified value with the specified key and returns
        /// the previous value associated with that key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to modify.</param>
        /// <param name="key">The key whose value is to be set.</param>
        /// <param name="value">The value to associate with the key.</param>
        /// <returns>
        /// The previous value associated with the key, or the default value
        /// for <typeparamref name="TValue"/> if the key was absent.
        /// </returns>
        /// <remarks>
        /// A default return value does not distinguish an absent key from a key
        /// previously associated with the default value. For unrecognized
        /// implementations and <see cref="ConcurrentDictionary{TKey, TValue}"/>,
        /// retrieving the previous value and setting the new value are separate
        /// operations and are not atomic.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">
        /// A recognized dictionary's comparer does not support alternate lookup.
        /// </exception>
        public static TValue Put<TValue>(this IDictionary<string, TValue> dictionary, ReadOnlySpan<char> key, TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary is JCG.Dictionary<string, TValue> jcgDictionary)
            {
                ref TValue? slot = ref CollectionMarshal.GetValueRefOrAddDefault(
                    jcgDictionary.GetSpanAlternateLookup<char>(), key, out bool exists);

                TValue previousValue = exists ? slot! : default!;
                slot = value;
                return previousValue;
            }

            if (dictionary is JCG.OrderedDictionary<string, TValue> orderedDictionary)
            {
                ref TValue? slot = ref CollectionMarshal.GetValueRefOrAddDefault(
                    orderedDictionary.GetSpanAlternateLookup<char>(), key, out bool exists);

                TValue previousValue = exists ? slot! : default!;
                slot = value;
                return previousValue;
            }

            if (dictionary is JCG.SortedDictionary<string, TValue> sortedDictionary)
            {
                JCG.SortedDictionary<string, TValue>.SpanAlternateLookup<char> lookup =
                    sortedDictionary.GetSpanAlternateLookup<char>();

                TValue previousValue = lookup.TryGetValue(key, out TValue? existingValue)
                    ? existingValue
                    : default!;

                lookup[key] = value;
                return previousValue;
            }

#if FEATURE_DICTIONARY_ALTERNATELOOKUP
            if (dictionary is Dictionary<string, TValue> bclDictionary)
            {
                ref TValue? slot = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    bclDictionary.GetAlternateLookup<ReadOnlySpan<char>>(), key, out bool exists);

                TValue previousValue = exists ? slot! : default!;
                slot = value;
                return previousValue;
            }

            if (dictionary is ConcurrentDictionary<string, TValue> concurrentDictionary)
            {
                var lookup = concurrentDictionary.GetAlternateLookup<ReadOnlySpan<char>>();
                while (true)
                {
                    if (lookup.TryGetValue(key, out string? actualKey, out TValue? previousValue))
                    {
                        if (concurrentDictionary.TryUpdate(actualKey, value, previousValue))
                            return previousValue;
                    }
                    else if (lookup.TryAdd(key, value))
                    {
                        return default!;
                    }
                }
            }
#endif

            string lookupKey = key.ToString();
            TValue previous = dictionary.TryGetValue(lookupKey, out TValue? existing)
                ? existing
                : default!;

            dictionary[lookupKey] = value;
            return previous;
        }

        #endregion Put

        #region Remove

        /// <summary>
        /// Removes the element with the specified key from the dictionary.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to modify.</param>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns><c>true</c> if the element was successfully removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The dictionary's comparer does not support alternate lookup using a character span.
        /// </exception>
        public static bool Remove<TValue>(this JCG.Dictionary<string, TValue> dictionary, ReadOnlySpan<char> key)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().Remove(key);
        }

        /// <summary>
        /// Removes the element with the specified key from the dictionary and
        /// returns the actual stored key and its associated value.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to modify.</param>
        /// <param name="key">The key of the element to remove.</param>
        /// <param name="actualKey">
        /// When this method returns, the actual key stored in the dictionary,
        /// if an element was removed; otherwise, <c>null</c>.
        /// </param>
        /// <param name="value">
        /// When this method returns, the value associated with the removed key,
        /// if an element was removed; otherwise, the default value for
        /// <typeparamref name="TValue"/>.
        /// </param>
        /// <returns><c>true</c> if the element was successfully removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The dictionary's comparer does not support alternate lookup using a character span.
        /// </exception>
        public static bool Remove<TValue>(this JCG.Dictionary<string, TValue> dictionary, ReadOnlySpan<char> key, [MaybeNullWhen(false)] out string actualKey, [MaybeNullWhen(false)] out TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().Remove(key, out actualKey, out value);
        }

        /// <summary>
        /// Removes the element with the specified key from the ordered dictionary.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The ordered dictionary to modify.</param>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns><c>true</c> if the element was removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The comparer does not support alternate lookup.</exception>
        public static bool Remove<TValue>(this JCG.OrderedDictionary<string, TValue> dictionary, ReadOnlySpan<char> key)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().Remove(key);
        }

        /// <summary>
        /// Removes the element with the specified key and returns its actual stored key and value.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The ordered dictionary to modify.</param>
        /// <param name="key">The key of the element to remove.</param>
        /// <param name="actualKey">When this method returns, the actual stored key, if removed; otherwise, <c>null</c>.</param>
        /// <param name="value">When this method returns, the removed value, if removed.</param>
        /// <returns><c>true</c> if the element was removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The comparer does not support alternate lookup.</exception>
        public static bool Remove<TValue>(this JCG.OrderedDictionary<string, TValue> dictionary, ReadOnlySpan<char> key, [MaybeNullWhen(false)] out string actualKey, [MaybeNullWhen(false)] out TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().Remove(key, out actualKey, out value);
        }

        /// <summary>
        /// Removes the element with the specified key from the dictionary.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to modify.</param>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns><c>true</c> if the element was removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">
        /// A recognized dictionary's comparer does not support alternate lookup.
        /// </exception>
        public static bool Remove<TValue>(this IDictionary<string, TValue> dictionary, ReadOnlySpan<char> key)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary is JCG.Dictionary<string, TValue> jcgDictionary)
                return jcgDictionary.GetSpanAlternateLookup<char>().Remove(key);

            if (dictionary is JCG.OrderedDictionary<string, TValue> orderedDictionary)
                return orderedDictionary.GetSpanAlternateLookup<char>().Remove(key);

            if (dictionary is JCG.SortedDictionary<string, TValue> sortedDictionary)
                return sortedDictionary.GetSpanAlternateLookup<char>().Remove(key);

#if FEATURE_DICTIONARY_ALTERNATELOOKUP
            if (dictionary is Dictionary<string, TValue> bclDictionary)
                return bclDictionary.GetAlternateLookup<ReadOnlySpan<char>>().Remove(key);

            if (dictionary is ConcurrentDictionary<string, TValue> concurrentDictionary)
                return concurrentDictionary.GetAlternateLookup<ReadOnlySpan<char>>().TryRemove(key, out _);
#endif

            return dictionary.Remove(key.ToString());
        }

        /// <summary>
        /// Removes the element with the specified key and returns its actual stored key and value.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to modify.</param>
        /// <param name="key">The key of the element to remove.</param>
        /// <param name="actualKey">
        /// When this method returns, the actual stored key if an element was removed.
        /// For an unrecognized dictionary implementation, this is the string created
        /// from <paramref name="key"/>.
        /// </param>
        /// <param name="value">When this method returns, the removed value, if an element was removed.</param>
        /// <returns><c>true</c> if the element was removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">
        /// A recognized dictionary's comparer does not support alternate lookup.
        /// </exception>
        public static bool Remove<TValue>(this IDictionary<string, TValue> dictionary, ReadOnlySpan<char> key, [MaybeNullWhen(false)] out string actualKey, [MaybeNullWhen(false)] out TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary is JCG.Dictionary<string, TValue> jcgDictionary)
                return jcgDictionary.GetSpanAlternateLookup<char>().Remove(key, out actualKey, out value);

            if (dictionary is JCG.OrderedDictionary<string, TValue> orderedDictionary)
                return orderedDictionary.GetSpanAlternateLookup<char>().Remove(key, out actualKey, out value);

            if (dictionary is JCG.SortedDictionary<string, TValue> sortedDictionary)
                return sortedDictionary.GetSpanAlternateLookup<char>().Remove(key, out actualKey, out value);

#if FEATURE_DICTIONARY_ALTERNATELOOKUP
            if (dictionary is Dictionary<string, TValue> bclDictionary)
                return bclDictionary.GetAlternateLookup<ReadOnlySpan<char>>().Remove(key, out actualKey, out value);

            if (dictionary is ConcurrentDictionary<string, TValue> concurrentDictionary)
                return concurrentDictionary.GetAlternateLookup<ReadOnlySpan<char>>().TryRemove(key, out actualKey, out value);
#endif

            string lookupKey = key.ToString();
            if (dictionary.TryGetValue(lookupKey, out value)
                && dictionary.Remove(lookupKey))
            {
                actualKey = lookupKey;
                return true;
            }

            actualKey = null;
            value = default;
            return false;
        }

        #endregion Remove

        #region this[int index]

        /// <summary>
        /// Provides an indexer that gets or sets dictionary values using a
        /// <see cref="ReadOnlySpan{T}"/> key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to access.</param>
        extension<TValue>(JCG.Dictionary<string, TValue> dictionary)
        {
            /// <summary>
            /// Gets or sets the value associated with the specified key.
            /// </summary>
            /// <param name="key">The key whose value is to be retrieved or set.</param>
            /// <returns>The value associated with the specified key.</returns>
            /// <exception cref="ArgumentNullException">
            /// The dictionary is <c>null</c>.
            /// </exception>
            /// <exception cref="KeyNotFoundException">
            /// The key is not present in the dictionary when getting the value.
            /// </exception>
            /// <exception cref="NotSupportedException">
            /// The dictionary's comparer does not support alternate lookup using a character span.
            /// </exception>
            public TValue this[ReadOnlySpan<char> key]
            {
                get
                {
                    if (dictionary is null)
                        throw new ArgumentNullException(nameof(dictionary));

                    return dictionary.GetSpanAlternateLookup<char>()[key];
                }
                set
                {
                    if (dictionary is null)
                        throw new ArgumentNullException(nameof(dictionary));

                    dictionary.GetSpanAlternateLookup<char>()[key] = value;
                }
            }
        }

        /// <summary>
        /// Provides an indexer that gets or sets ordered dictionary values using a
        /// <see cref="ReadOnlySpan{T}"/> key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The ordered dictionary to access.</param>
        extension<TValue>(JCG.OrderedDictionary<string, TValue> dictionary)
        {
            /// <summary>
            /// Gets or sets the value associated with the specified key.
            /// </summary>
            /// <param name="key">The key whose value is to be retrieved or set.</param>
            /// <returns>The value associated with the specified key.</returns>
            /// <exception cref="ArgumentNullException">
            /// The dictionary is <c>null</c>.
            /// </exception>
            /// <exception cref="KeyNotFoundException">
            /// The key is not present in the dictionary when getting the value.
            /// </exception>
            /// <exception cref="NotSupportedException">
            /// The dictionary's comparer does not support alternate lookup using a character span.
            /// </exception>
            public TValue this[ReadOnlySpan<char> key]
            {
                get
                {
                    if (dictionary is null)
                        throw new ArgumentNullException(nameof(dictionary));

                    return dictionary.GetSpanAlternateLookup<char>()[key];
                }
                set
                {
                    if (dictionary is null)
                        throw new ArgumentNullException(nameof(dictionary));

                    dictionary.GetSpanAlternateLookup<char>()[key] = value;
                }
            }
        }

        /// <summary>
        /// Provides an indexer that gets or sets dictionary values using a
        /// <see cref="ReadOnlySpan{T}"/> key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to access.</param>
        /// <remarks>
        /// Known dictionary implementations use alternate lookup where supported.
        /// Other implementations use a string created from the span.
        /// </remarks>
        extension<TValue>(IDictionary<string, TValue> dictionary)
        {
            /// <summary>
            /// Gets or sets the value associated with the specified key.
            /// </summary>
            /// <param name="key">The key whose value is to be retrieved or set.</param>
            /// <returns>The value associated with the specified key.</returns>
            /// <exception cref="ArgumentNullException">
            /// The dictionary is <c>null</c>.
            /// </exception>
            /// <exception cref="KeyNotFoundException">
            /// The key is not present in the dictionary when getting the value.
            /// </exception>
            /// <exception cref="NotSupportedException">
            /// A recognized dictionary's comparer does not support alternate lookup.
            /// </exception>
            public TValue this[ReadOnlySpan<char> key]
            {
                get
                {
                    if (dictionary is null)
                        throw new ArgumentNullException(nameof(dictionary));

                    if (dictionary is JCG.Dictionary<string, TValue> jcgDictionary)
                        return jcgDictionary.GetSpanAlternateLookup<char>()[key];

                    if (dictionary is JCG.OrderedDictionary<string, TValue> orderedDictionary)
                        return orderedDictionary.GetSpanAlternateLookup<char>()[key];

                    if (dictionary is JCG.SortedDictionary<string, TValue> sortedDictionary)
                        return sortedDictionary.GetSpanAlternateLookup<char>()[key];

#if FEATURE_DICTIONARY_ALTERNATELOOKUP
                    if (dictionary is Dictionary<string, TValue> bclDictionary)
                        return bclDictionary.GetAlternateLookup<ReadOnlySpan<char>>()[key];

                    if (dictionary is ConcurrentDictionary<string, TValue> concurrentDictionary)
                        return concurrentDictionary.GetAlternateLookup<ReadOnlySpan<char>>()[key];
#endif

                    return dictionary[key.ToString()];
                }
                set
                {
                    if (dictionary is null)
                        throw new ArgumentNullException(nameof(dictionary));

                    if (dictionary is JCG.Dictionary<string, TValue> jcgDictionary)
                    {
                        jcgDictionary.GetSpanAlternateLookup<char>()[key] = value;
                        return;
                    }

                    if (dictionary is JCG.OrderedDictionary<string, TValue> orderedDictionary)
                    {
                        orderedDictionary.GetSpanAlternateLookup<char>()[key] = value;
                        return;
                    }

                    if (dictionary is JCG.SortedDictionary<string, TValue> sortedDictionary)
                    {
                        sortedDictionary.GetSpanAlternateLookup<char>()[key] = value;
                        return;
                    }

#if FEATURE_DICTIONARY_ALTERNATELOOKUP
                    if (dictionary is Dictionary<string, TValue> bclDictionary)
                    {
                        bclDictionary.GetAlternateLookup<ReadOnlySpan<char>>()[key] = value;
                        return;
                    }

                    if (dictionary is ConcurrentDictionary<string, TValue> concurrentDictionary)
                    {
                        concurrentDictionary.GetAlternateLookup<ReadOnlySpan<char>>()[key] = value;
                        return;
                    }
#endif

                    dictionary[key.ToString()] = value;
                }
            }
        }

        #endregion this[int index]

        #region TryAdd

        /// <summary>
        /// Adds an element with the specified key and value to the dictionary,
        /// if the key does not already exist.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to modify.</param>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>
        /// <c>true</c> if the element was added; otherwise, <c>false</c> if the key
        /// already exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The dictionary's comparer does not support alternate lookup using a character span.
        /// </exception>
        public static bool TryAdd<TValue>(this JCG.Dictionary<string, TValue> dictionary, ReadOnlySpan<char> key, TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().TryAdd(key, value);
        }

        /// <summary>
        /// Adds an element with the specified key and value if the key does not already exist.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The ordered dictionary to modify.</param>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns><c>true</c> if the element was added; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The comparer does not support alternate lookup.</exception>
        public static bool TryAdd<TValue>(this JCG.OrderedDictionary<string, TValue> dictionary, ReadOnlySpan<char> key, TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().TryAdd(key, value);
        }

        /// <summary>
        /// Adds an element with the specified key and value if the key does not already exist.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to modify.</param>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns><c>true</c> if the element was added; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// For unrecognized dictionary implementations, this operation uses
        /// <see cref="IDictionary{TKey, TValue}.ContainsKey(TKey)"/> followed by
        /// <see cref="IDictionary{TKey, TValue}.Add(TKey, TValue)"/> and is not atomic.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">
        /// A recognized dictionary's comparer does not support alternate lookup.
        /// </exception>
        public static bool TryAdd<TValue>(this IDictionary<string, TValue> dictionary, ReadOnlySpan<char> key, TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary is JCG.Dictionary<string, TValue> jcgDictionary)
                return jcgDictionary.GetSpanAlternateLookup<char>().TryAdd(key, value);

            if (dictionary is JCG.OrderedDictionary<string, TValue> orderedDictionary)
                return orderedDictionary.GetSpanAlternateLookup<char>().TryAdd(key, value);

            if (dictionary is JCG.SortedDictionary<string, TValue> sortedDictionary)
                return sortedDictionary.GetSpanAlternateLookup<char>().TryAdd(key, value);

#if FEATURE_DICTIONARY_ALTERNATELOOKUP
            if (dictionary is Dictionary<string, TValue> bclDictionary)
                return bclDictionary.GetAlternateLookup<ReadOnlySpan<char>>().TryAdd(key, value);

            if (dictionary is ConcurrentDictionary<string, TValue> concurrentDictionary)
                return concurrentDictionary.GetAlternateLookup<ReadOnlySpan<char>>().TryAdd(key, value);
#endif

            string lookupKey = key.ToString();
            if (dictionary.ContainsKey(lookupKey))
                return false;

            dictionary.Add(lookupKey, value);
            return true;
        }

        #endregion TryAdd

        #region TryGetValue

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key,
        /// if the key is found; otherwise, the default value for the type of the
        /// <typeparamref name="TValue"/> parameter.
        /// </param>
        /// <returns><c>true</c> if the dictionary contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The dictionary's comparer does not support alternate lookup using a character span.
        /// </exception>
        public static bool TryGetValue<TValue>(this JCG.Dictionary<string, TValue> dictionary, ReadOnlySpan<char> key, [MaybeNullWhen(false)] out TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets the value and stored key associated with the specified key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="key">The key to locate.</param>
        /// <param name="actualKey">
        /// When this method returns, the actual key stored in the dictionary,
        /// if the key is found; otherwise, <c>null</c>.
        /// </param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key,
        /// if the key is found; otherwise, the default value for the type of the
        /// <typeparamref name="TValue"/> parameter.
        /// </param>
        /// <returns><c>true</c> if the dictionary contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The dictionary's comparer does not support alternate lookup using a character span.
        /// </exception>
        public static bool TryGetValue<TValue>(this JCG.Dictionary<string, TValue> dictionary, ReadOnlySpan<char> key, [MaybeNullWhen(false)] out string actualKey, [MaybeNullWhen(false)] out TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().TryGetValue(key, out actualKey, out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The ordered dictionary to search.</param>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">When this method returns, the associated value, if found.</param>
        /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The comparer does not support alternate lookup.</exception>
        public static bool TryGetValue<TValue>(this JCG.OrderedDictionary<string, TValue> dictionary, ReadOnlySpan<char> key, [MaybeNullWhen(false)] out TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets the value and actual stored key associated with the specified key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The ordered dictionary to search.</param>
        /// <param name="key">The key to locate.</param>
        /// <param name="actualKey">When this method returns, the actual stored key, if found; otherwise, <c>null</c>.</param>
        /// <param name="value">When this method returns, the associated value, if found.</param>
        /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The comparer does not support alternate lookup.</exception>
        public static bool TryGetValue<TValue>(this JCG.OrderedDictionary<string, TValue> dictionary, ReadOnlySpan<char> key, [MaybeNullWhen(false)] out string actualKey, [MaybeNullWhen(false)] out TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetSpanAlternateLookup<char>().TryGetValue(key, out actualKey, out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">When this method returns, the associated value, if found.</param>
        /// <returns><c>true</c> if the dictionary contains the key; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Known dictionary implementations use alternate lookup where supported.
        /// Other implementations are searched using a string created from
        /// <paramref name="key"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">
        /// A recognized dictionary's comparer does not support alternate lookup.
        /// </exception>
        public static bool TryGetValue<TValue>(this IDictionary<string, TValue> dictionary, ReadOnlySpan<char> key, [MaybeNullWhen(false)] out TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary is JCG.Dictionary<string, TValue> jcgDictionary)
                return jcgDictionary.GetSpanAlternateLookup<char>().TryGetValue(key, out value);

            if (dictionary is JCG.OrderedDictionary<string, TValue> orderedDictionary)
                return orderedDictionary.GetSpanAlternateLookup<char>().TryGetValue(key, out value);

            if (dictionary is JCG.SortedDictionary<string, TValue> sortedDictionary)
                return sortedDictionary.GetSpanAlternateLookup<char>().TryGetValue(key, out value);

#if FEATURE_DICTIONARY_ALTERNATELOOKUP
            if (dictionary is Dictionary<string, TValue> bclDictionary)
                return bclDictionary.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(key, out value);

            if (dictionary is ConcurrentDictionary<string, TValue> concurrentDictionary)
                return concurrentDictionary.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(key, out value);
#endif

            return dictionary.TryGetValue(key.ToString(), out value);
        }

        /// <summary>
        /// Gets the value and actual stored key associated with the specified key.
        /// </summary>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="key">The key to locate.</param>
        /// <param name="actualKey">
        /// When this method returns, the stored key if the key is found.
        /// For an unrecognized dictionary implementation, this is the string
        /// created from <paramref name="key"/>.
        /// </param>
        /// <param name="value">When this method returns, the associated value, if found.</param>
        /// <returns><c>true</c> if the dictionary contains the key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">
        /// A recognized dictionary's comparer does not support alternate lookup.
        /// </exception>
        public static bool TryGetValue<TValue>(this IDictionary<string, TValue> dictionary, ReadOnlySpan<char> key, [MaybeNullWhen(false)] out string actualKey, [MaybeNullWhen(false)] out TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary is JCG.Dictionary<string, TValue> jcgDictionary)
                return jcgDictionary.GetSpanAlternateLookup<char>().TryGetValue(key, out actualKey, out value);

            if (dictionary is JCG.OrderedDictionary<string, TValue> orderedDictionary)
                return orderedDictionary.GetSpanAlternateLookup<char>().TryGetValue(key, out actualKey, out value);

            if (dictionary is JCG.SortedDictionary<string, TValue> sortedDictionary)
                return sortedDictionary.GetSpanAlternateLookup<char>().TryGetValue(key, out actualKey, out value);

#if FEATURE_DICTIONARY_ALTERNATELOOKUP
            if (dictionary is Dictionary<string, TValue> bclDictionary)
                return bclDictionary.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(key, out actualKey, out value);

            if (dictionary is ConcurrentDictionary<string, TValue> concurrentDictionary)
                return concurrentDictionary.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(key, out actualKey, out value);
#endif

            string lookupKey = key.ToString();
            bool found = dictionary.TryGetValue(lookupKey, out value);
            actualKey = found ? lookupKey : null;
            return found;
        }

        #endregion TryGetValue
    }
}
