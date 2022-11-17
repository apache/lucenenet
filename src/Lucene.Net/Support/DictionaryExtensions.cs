using System;
using System.Collections.Generic;

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
    /// Extensions to <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    internal static class DictionaryExtensions
    {
        /// <summary>
        /// Copies all of the mappings from the specified <paramref name="collection"/> to this dictionary.
        /// These mappings will replace any mappings that this dictionary had for any of the keys currently
        /// in the specified dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of key.</typeparam>
        /// <typeparam name="TValue">The type of value.</typeparam>
        /// <param name="dictionary">This dictionary.</param>
        /// <param name="collection">The collection to merge.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="dictionary"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                dictionary[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Associates the specified value with the specified key in this dictionary.
        /// If the dictionary previously contained a mapping for the key, the old value is replaced.
        /// <para/>
        /// <b>Usage Note:</b> Unless the return value is required, it is more efficient to use
        /// the setter of the dictionary indexer than this method.
        /// <para/>
        /// This method will only work right if <typeparamref name="TValue"/> is a nullable type, since
        /// it may not be possible to distinguish value types with actual values from their default value.
        /// Java collections only accept reference types, so this is a direct port from Java, not accounting
        /// for value types.
        /// </summary>
        /// <typeparam name="TKey">The type of key.</typeparam>
        /// <typeparam name="TValue">The type of value.</typeparam>
        /// <param name="dictionary">This dictionary.</param>
        /// <param name="key">The key with which the specified <paramref name="value"/> is associated.</param>
        /// <param name="value">The value to be associated with the specified <paramref name="key"/>.</param>
        /// <returns>The previous value associated with key, or <c>null</c> if there was no mapping for key.
        /// (A <c>null</c> return can also indicate that the map previously associated <c>null</c> with key.)</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The underlying dictionary implementation doesn't accept <c>null</c> for <paramref name="key"/>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The underlying dictionary implementation doesn't accept <c>null</c> for <paramref name="value"/>.
        /// </exception>
        public static TValue Put<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (!dictionary.TryGetValue(key, out TValue oldValue))
                oldValue = default;
            dictionary[key] = value;
            return oldValue;
        }

        /// <summary>
        /// Returns a concurrent wrapper for the current <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <param name="dictionary">The collection to make concurrent (thread-safe).</param>
        /// <returns>An object that acts as a read-only wrapper around the current <see cref="ISet{T}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <remarks>
        /// To synchronize any modifications to the <see cref="ISet{T}"/> object, expose it only through this wrapper.
        /// <para/>
        /// The set returned uses simple locking and may not be the most performant solution, but it provides a quick
        /// way to make any set thread-safe.
        /// <para/>
        /// This method is an O(1) operation.
        /// </remarks>
        internal static IDictionary<TKey, TValue> AsConcurrent<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return new ConcurrentDictionaryWrapper<TKey, TValue>(dictionary);
        }
    }
}