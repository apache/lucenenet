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

    public static class DictionaryExtensions
    {
        public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> kvps)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));

            foreach (var kvp in kvps)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        public static TValue Put<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));

            if (!dict.TryGetValue(key, out TValue oldValue))
                oldValue = default;
            dict[key] = value;
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