using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util
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
    /// Extensions to <see cref="IList{T}"/>.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Adds the elements of the specified collection to the end of the <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="list">The list to add to.</param>
        /// <param name="collection">The collection whose elements should be added to the end of the <see cref="IList{T}"/>.
        /// The collection itself cannot be <c>null</c>, but it can contain elements that are <c>null</c>, if type
        /// <typeparamref name="T"/> is a reference type.</param>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> collection)
        {
            if (list is null)
                throw new ArgumentNullException(nameof(list));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (list is List<T> thisList)
            {
                thisList.AddRange(collection);
            }
            else if (list is JCG.List<T> jcgList)
            {
                jcgList.AddRange(collection);
            }
            else
            {
                foreach (var item in collection)
                {
                    list.Add(item);
                }
            }
        }

        /// <summary>
        /// If the underlying type is <see cref="List{T}"/>,
        /// calls <see cref="List{T}.Sort()"/>. If not, 
        /// uses <see cref="Util.CollectionUtil.TimSort{T}(IList{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">this <see cref="IList{T}"/></param>
        public static void Sort<T>(this IList<T> list)
        {
            if (list is List<T> listToSort)
            {
                listToSort.Sort();
            }
            else if (list is JCG.List<T> jcgListToSort)
            {
                jcgListToSort.Sort();
            }
            else
            {
                CollectionUtil.TimSort(list);
            }
        }

        /// <summary>
        /// If the underlying type is <see cref="List{T}"/>,
        /// calls <see cref="List{T}.Sort(IComparer{T})"/>. If not, 
        /// uses <see cref="Util.CollectionUtil.TimSort{T}(IList{T}, IComparer{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">this <see cref="IList{T}"/></param>
        /// <param name="comparer">the comparer to use for the sort</param>
        public static void Sort<T>(this IList<T> list, IComparer<T> comparer)
        {
            if (list is List<T> listToSort)
            {
                listToSort.Sort(comparer);
            }
            else if (list is JCG.List<T> jcgListToSort)
            {
                jcgListToSort.Sort(comparer);
            }
            else
            {
                CollectionUtil.TimSort(list, comparer);
            }
        }

        /// <summary>
        /// If the underlying type is <see cref="List{T}"/>,
        /// calls <see cref="List{T}.Sort(IComparer{T})"/>. If not, 
        /// uses <see cref="Util.CollectionUtil.TimSort{T}(IList{T}, IComparer{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">this <see cref="IList{T}"/></param>
        /// <param name="comparison">the comparison function to use for the sort</param>
        public static void Sort<T>(this IList<T> list, Comparison<T> comparison)
        {
            IComparer<T> comparer = new FunctorComparer<T>(comparison);
            Sort(list, comparer);
        }

        /// <summary>
        /// Sorts the given <see cref="IList{T}"/> using the <see cref="IComparer{T}"/>.
        /// This method uses the Tim sort
        /// algorithm, but falls back to binary sort for small lists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">this <see cref="IList{T}"/></param>
        public static void TimSort<T>(this IList<T> list)
        {
            CollectionUtil.TimSort(list);
        }

        /// <summary>
        /// Sorts the given <see cref="IList{T}"/> using the <see cref="IComparer{T}"/>.
        /// This method uses the Tim sort
        /// algorithm, but falls back to binary sort for small lists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">this <see cref="IList{T}"/></param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> to use for the sort.</param>
        public static void TimSort<T>(this IList<T> list, IComparer<T> comparer)
        {
            CollectionUtil.TimSort(list, comparer);
        }

        /// <summary>
        /// Sorts the given <see cref="IList{T}"/> using the <see cref="IComparer{T}"/>.
        /// This method uses the intro sort
        /// algorithm, but falls back to insertion sort for small lists. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">this <see cref="IList{T}"/></param>
        public static void IntroSort<T>(this IList<T> list)
        {
            CollectionUtil.IntroSort(list);
        }

        /// <summary>
        /// Sorts the given <see cref="IList{T}"/> using the <see cref="IComparer{T}"/>.
        /// This method uses the intro sort
        /// algorithm, but falls back to insertion sort for small lists. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">this <see cref="IList{T}"/></param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> to use for the sort.</param>
        public static void IntroSort<T>(this IList<T> list, IComparer<T> comparer)
        {
            CollectionUtil.IntroSort(list, comparer);
        }

        #region Nested Type: FunctorComparer<T>

        private sealed class FunctorComparer<T> : IComparer<T>
        {
            private readonly Comparison<T> comparison; // LUCENENET: marked readonly

            public FunctorComparer(Comparison<T> comparison)
            {
                this.comparison = comparison;
            }

            public int Compare(T x, T y)
            {
                return this.comparison(x, y);
            }
        }

        #endregion Nested Type: FunctorComparer<T>
    }
}
