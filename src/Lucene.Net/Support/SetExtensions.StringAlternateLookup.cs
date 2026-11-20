using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    /// Provides extension methods for performing set operations using
    /// <see cref="ReadOnlySpan{T}"/> values.
    /// </summary>
    internal static partial class SetExtensions
    {
        #region Add

        /// <summary>
        /// Adds the specified value to the set.
        /// </summary>
        /// <param name="set">The set to modify.</param>
        /// <param name="value">The value to add.</param>
        /// <returns><c>true</c> if the value was added; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The set's comparer does not support alternate lookup using a character span.</exception>
        public static bool Add(this JCG.HashSet<string> set, ReadOnlySpan<char> value)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            return set.GetSpanAlternateLookup<char>().Add(value);
        }

        /// <summary>
        /// Adds the specified value to the ordered set.
        /// </summary>
        /// <param name="set">The ordered set to modify.</param>
        /// <param name="value">The value to add.</param>
        /// <returns><c>true</c> if the value was added; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The set's comparer does not support alternate lookup using a character span.</exception>
        public static bool Add(this JCG.OrderedHashSet<string> set, ReadOnlySpan<char> value)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            return set.GetSpanAlternateLookup<char>().Add(value);
        }

        /// <summary>
        /// Adds the specified value to the set.
        /// </summary>
        /// <param name="set">The set to modify.</param>
        /// <param name="value">The value to add.</param>
        /// <returns><c>true</c> if the value was added; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Known set implementations use alternate lookup where supported.
        /// Other implementations are modified using a string created from
        /// <paramref name="value"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// A recognized set's comparer does not support alternate lookup using a character span.
        /// </exception>
        public static bool Add(this ISet<string> set, ReadOnlySpan<char> value)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            if (set is JCG.HashSet<string> hashSet)
                return hashSet.GetSpanAlternateLookup<char>().Add(value);

            if (set is JCG.OrderedHashSet<string> orderedHashSet)
                return orderedHashSet.GetSpanAlternateLookup<char>().Add(value);

            if (set is JCG.SortedSet<string> sortedSet)
                return sortedSet.GetSpanAlternateLookup<char>().Add(value);

#if FEATURE_HASHSET_ALTERNATELOOKUP
            if (set is HashSet<string> bclHashSet)
                return bclHashSet.GetAlternateLookup<ReadOnlySpan<char>>().Add(value);
#endif

            return set.Add(value.ToString());
        }

        #endregion Add

        #region Contains

        /// <summary>
        /// Determines whether the set contains the specified value.
        /// </summary>
        /// <param name="set">The set to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> if the set contains the value; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The set's comparer does not support alternate lookup using a character span.</exception>
        public static bool Contains(this JCG.HashSet<string> set, ReadOnlySpan<char> value)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            return set.GetSpanAlternateLookup<char>().Contains(value);
        }

        /// <summary>
        /// Determines whether the ordered set contains the specified value.
        /// </summary>
        /// <param name="set">The ordered set to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> if the set contains the value; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The set's comparer does not support alternate lookup using a character span.</exception>
        public static bool Contains(this JCG.OrderedHashSet<string> set, ReadOnlySpan<char> value)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            return set.GetSpanAlternateLookup<char>().Contains(value);
        }

        /// <summary>
        /// Determines whether the set contains the specified value.
        /// </summary>
        /// <param name="set">The set to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> if the set contains the value; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Known set implementations use alternate lookup where supported.
        /// Other implementations are searched using a string created from
        /// <paramref name="value"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// A recognized set's comparer does not support alternate lookup using a character span.
        /// </exception>
        public static bool Contains(this ISet<string> set, ReadOnlySpan<char> value)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            if (set is JCG.HashSet<string> hashSet)
                return hashSet.GetSpanAlternateLookup<char>().Contains(value);

            if (set is JCG.OrderedHashSet<string> orderedHashSet)
                return orderedHashSet.GetSpanAlternateLookup<char>().Contains(value);

            if (set is JCG.SortedSet<string> sortedSet)
                return sortedSet.GetSpanAlternateLookup<char>().Contains(value);

#if FEATURE_HASHSET_ALTERNATELOOKUP
            if (set is HashSet<string> bclHashSet)
                return bclHashSet.GetAlternateLookup<ReadOnlySpan<char>>().Contains(value);
#endif

            return set.Contains(value.ToString());
        }

        #endregion Contains

        #region Remove

        /// <summary>
        /// Removes the specified value from the set.
        /// </summary>
        /// <param name="set">The set to modify.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns><c>true</c> if the value was successfully removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The set's comparer does not support alternate lookup using a character span.</exception>
        public static bool Remove(this JCG.HashSet<string> set, ReadOnlySpan<char> value)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            return set.GetSpanAlternateLookup<char>().Remove(value);
        }

        /// <summary>
        /// Removes the specified value from the ordered set.
        /// </summary>
        /// <param name="set">The ordered set to modify.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns><c>true</c> if the value was successfully removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The set's comparer does not support alternate lookup using a character span.</exception>
        public static bool Remove(this JCG.OrderedHashSet<string> set, ReadOnlySpan<char> value)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            return set.GetSpanAlternateLookup<char>().Remove(value);
        }

        /// <summary>
        /// Removes the specified value from the set.
        /// </summary>
        /// <param name="set">The set to modify.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns><c>true</c> if the value was successfully removed; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Known set implementations use alternate lookup where supported.
        /// Other implementations are modified using a string created from
        /// <paramref name="value"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// A recognized set's comparer does not support alternate lookup using a character span.
        /// </exception>
        public static bool Remove(this ISet<string> set, ReadOnlySpan<char> value)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            if (set is JCG.HashSet<string> hashSet)
                return hashSet.GetSpanAlternateLookup<char>().Remove(value);

            if (set is JCG.OrderedHashSet<string> orderedHashSet)
                return orderedHashSet.GetSpanAlternateLookup<char>().Remove(value);

            if (set is JCG.SortedSet<string> sortedSet)
                return sortedSet.GetSpanAlternateLookup<char>().Remove(value);

#if FEATURE_HASHSET_ALTERNATELOOKUP
            if (set is HashSet<string> bclHashSet)
                return bclHashSet.GetAlternateLookup<ReadOnlySpan<char>>().Remove(value);
#endif

            return set.Remove(value.ToString());
        }

        #endregion Remove

        #region TryGetValue

        /// <summary>
        /// Gets the actual value stored in the set that is equal to the specified value.
        /// </summary>
        /// <param name="set">The set to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <param name="actualValue">When this method returns, the actual stored value, if found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if an equal value was found; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The set's comparer does not support alternate lookup using a character span.</exception>
        public static bool TryGetValue(this JCG.HashSet<string> set, ReadOnlySpan<char> value, [MaybeNullWhen(false)] out string actualValue)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            return set.GetSpanAlternateLookup<char>().TryGetValue(value, out actualValue);
        }

        /// <summary>
        /// Gets the actual value stored in the ordered set that is equal to the specified value.
        /// </summary>
        /// <param name="set">The ordered set to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <param name="actualValue">When this method returns, the actual stored value, if found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if an equal value was found; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The set's comparer does not support alternate lookup using a character span.</exception>
        public static bool TryGetValue(this JCG.OrderedHashSet<string> set, ReadOnlySpan<char> value, [MaybeNullWhen(false)] out string actualValue)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            return set.GetSpanAlternateLookup<char>().TryGetValue(value, out actualValue);
        }

        /// <summary>
        /// Gets a value from the set that is equal to the specified value.
        /// </summary>
        /// <param name="set">The set to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <param name="actualValue">
        /// When this method returns, the actual stored value if found.
        /// For an unrecognized set implementation, this is the string created
        /// from <paramref name="value"/> when the set contains an equal value.
        /// </param>
        /// <returns><c>true</c> if an equal value was found; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <see cref="ISet{T}"/> does not expose an operation for retrieving the
        /// stored instance of an equal value. Consequently, for unrecognized
        /// implementations, <paramref name="actualValue"/> is the lookup string,
        /// not necessarily the instance stored in the set.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// A recognized set's comparer does not support alternate lookup using a character span.
        /// </exception>
        public static bool TryGetValue(this ISet<string> set, ReadOnlySpan<char> value, [MaybeNullWhen(false)] out string actualValue)
        {
            if (set is null)
                throw new ArgumentNullException(nameof(set));

            if (set is JCG.HashSet<string> hashSet)
                return hashSet.GetSpanAlternateLookup<char>().TryGetValue(value, out actualValue);

            if (set is JCG.OrderedHashSet<string> orderedHashSet)
                return orderedHashSet.GetSpanAlternateLookup<char>().TryGetValue(value, out actualValue);

            if (set is JCG.SortedSet<string> sortedSet)
                return sortedSet.GetSpanAlternateLookup<char>().TryGetValue(value, out actualValue);

#if FEATURE_HASHSET_ALTERNATELOOKUP
            if (set is HashSet<string> bclHashSet)
                return bclHashSet.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(value, out actualValue);
#endif

            string lookupValue = value.ToString();
            bool found = set.Contains(lookupValue);
            actualValue = found ? lookupValue : null;
            return found;
        }

        #endregion TryGetValue
    }
}
