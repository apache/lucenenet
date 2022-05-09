using J2N.Collections.Generic.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using JCG = J2N.Collections.Generic;

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
    /// Extensions for <see cref="ICollection{T}"/>.
    /// </summary>
    internal static class CollectionExtensions
    {
        /// <summary>
        /// Removes the given collection of elements from the source <see cref="ICollection{T}"/>.
        /// <para/>
        /// Usage Note: This is the same operation as <see cref="ISet{T}.ExceptWith(IEnumerable{T})"/> or
        /// <see cref="List{T}.RemoveAll(Predicate{T})"/> with a predicate of <c>(value) => collection.Contains(value)</c>. It is
        /// recommended to use these alternatives when possible.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="ICollection{T}"/> to remove elements from.</param>
        /// <param name="collection">An <see cref="ICollection{T}"/> containing the items to remove from <paramref name="source"/>.</param>
        /// <returns><c>true</c> if the collection changed as a result of the call; otherwise, <c>false</c>.</returns>
        [DebuggerStepThrough]
        public static bool RemoveAll<T>(this ICollection<T> source, ICollection<T> collection)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (source.Count == 0) return false;

            if (source is ISet<T> set)
            {
                int originalCount = set.Count;
                set.ExceptWith(collection);
                return originalCount != set.Count;
            }
            else if (source is IList<T> list)
            {
                int removed = list.RemoveAll((value) => collection.Contains(value));
                return removed > 0;
            }

            // Slow path for unknown collection types
            bool modified = false;
            foreach (var e in collection)
            {
                modified |= source.Remove(e);
            }
            return modified;
        }

        /// <summary>
        /// Retains only the elements in this list that are contained in the specified collection (optional operation).
        /// In other words, removes from this list all of its elements that are not contained in the specified collection.
        /// <para/>
        /// Usage Note: This is the same operation as <see cref="ISet{T}.IntersectWith(IEnumerable{T})"/> or
        /// <see cref="List{T}.RemoveAll(Predicate{T})"/> with a predicate of <c>(value) => !collection.Contains(value)</c>. It is
        /// recommended to use these alternatives when possible.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="ICollection{T}"/> to remove elements from.</param>
        /// <param name="collection">An <see cref="ICollection{T}"/> containing the items to remove from <paramref name="source"/>.</param>
        /// <returns><c>true</c> if the collection changed as a result of the call; otherwise, <c>false</c>.</returns>
        [DebuggerStepThrough]
        public static bool RetainAll<T>(this ICollection<T> source, ICollection<T> collection)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (source.Count == 0) return false;

            if (source is ISet<T> set)
            {
                int originalCount = set.Count;
                set.IntersectWith(collection);
                return originalCount != set.Count;
            }
            else if (source is IList<T> list)
            {
                int removed = list.RemoveAll((value) => !collection.Contains(value));
                return removed > 0;
            }

            // Slow path for unknown collection types
            var toRemove = new JCG.HashSet<T>();
            foreach (var e in source)
            {
                if (!collection.Contains(e))
                    toRemove.Add(e);
            }
            if (toRemove.Count > 0)
                return source.RemoveAll(toRemove);
            return false;
        }
    }
}
