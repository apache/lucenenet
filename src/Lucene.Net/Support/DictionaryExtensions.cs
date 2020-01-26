using System;
using System.Collections.Generic;
using System.IO;

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
            foreach (var kvp in kvps)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        public static TValue Put<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict == null)
                return default(TValue);

            var oldValue = dict.ContainsKey(key) ? dict[key] : default(TValue);
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

        /// <summary>
        /// Loads properties from the specified <see cref="Stream"/>. The encoding is
        /// ISO8859-1. 
        /// </summary>
        /// <remarks>
        /// The Properties file is interpreted according to the
        /// following rules:
        /// <list type="bullet">
        ///     <item><description>
        ///         Empty lines are ignored.
        ///     </description></item>
        ///     <item><description>
        ///         Lines starting with either a "#" or a "!" are comment lines and are
        ///         ignored.
        ///     </description></item>
        ///     <item><description>
        ///         A backslash at the end of the line escapes the following newline
        ///         character ("\r", "\n", "\r\n"). If there's a whitespace after the
        ///         backslash it will just escape that whitespace instead of concatenating
        ///         the lines. This does not apply to comment lines.
        ///     </description></item>
        ///     <item><description>
        ///         A property line consists of the key, the space between the key and
        ///         the value, and the value. The key goes up to the first whitespace, "=" or
        ///         ":" that is not escaped. The space between the key and the value contains
        ///         either one whitespace, one "=" or one ":" and any number of additional
        ///         whitespaces before and after that character. The value starts with the
        ///         first character after the space between the key and the value.
        ///     </description></item>
        ///     <item><description>
        ///         Following escape sequences are recognized: "\ ", "\\", "\r", "\n",
        ///         "\!", "\#", "\t", "\b", "\f", and "&#92;uXXXX" (unicode character).
        ///     </description></item>
        /// </list>
        /// <para/>
        /// This method is to mimic and interoperate with the Properties class in Java, which
        /// is essentially a string dictionary that natively supports importing and exporting to this format.
        /// </remarks>
        /// <param name="dict">This dictionary.</param>
        /// <param name="input">The <see cref="Stream"/>.</param>
        /// <exception cref="IOException">If error occurs during reading from the <see cref="Stream"/>.</exception>
        public static void Load(this IDictionary<string, string> dict, Stream input)
        {
            J2N.PropertyExtensions.LoadProperties(dict, input);
        }

        /// <summary>
        /// Stores the mappings in this Properties to the specified
        /// <see cref="Stream"/>, putting the specified comment at the beginning. The
        /// output from this method is suitable for being read by the
        /// <see cref="Load(IDictionary{string, string}, Stream)"/> method.
        /// </summary>
        /// <param name="dict">This dictionary.</param>
        /// <param name="output">The output <see cref="Stream"/> to write to.</param>
        /// <param name="comments">The comments to put at the beginning.</param>
        /// <exception cref="IOException">If an error occurs during the write to the <see cref="Stream"/>.</exception>
        /// <exception cref="InvalidCastException">If the key or value of a mapping is not a <see cref="string"/>.</exception>
        public static void Store(this IDictionary<string, string> dict, Stream output, string comments)
        {
            J2N.PropertyExtensions.SaveProperties(dict, output, comments);
        }
    }
}