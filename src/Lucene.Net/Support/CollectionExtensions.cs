using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="ICollection{T}"/> to remove elements from.</param>
        /// <param name="removeList">An <see cref="IEnumerable{T}"/> containing the items to remove from <paramref name="source"/>.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAll<T>(this ICollection<T> source, IEnumerable<T> removeList)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (source.Count == 0) return;

            foreach (var elt in removeList)
            {
                source.Remove(elt);
            }
        }
    }
}
