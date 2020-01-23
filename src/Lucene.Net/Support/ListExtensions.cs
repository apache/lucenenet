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

    public static class ListExtensions
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> values)
        {
            var lt = list as List<T>;

            if (lt != null)
                lt.AddRange(values);
            else
            {
                foreach (var item in values)
                {
                    list.Add(item);
                }
            }
        }

        /// <summary>
        /// Performs a binary search for the specified element in the specified
        /// sorted list. The list needs to be already sorted in natural sorting
        /// order. Searching in an unsorted array has an undefined result. It's also
        /// undefined which element is found if there are multiple occurrences of the
        /// same element.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="list">The sorted list to search.</param>
        /// <param name="item">The element to find.</param>
        /// <returns>The non-negative index of the element, or a negative index which
        /// is the <c>-index - 1</c> where the element would be inserted.</returns>
        /// <exception cref="InvalidCastException">
        /// If an element in the List or the search element does not
        /// implement <see cref="IComparable{T}"/>, or cannot be compared to each other.</exception>
        public static int BinarySearch<T>(this IList<T> list, T item) where T : IComparable<T>
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            if (list.Count == 0)
            {
                return -1;
            }

            int low = 0, mid = list.Count, high = mid - 1, result = -1;
            while (low <= high)
            {
                mid = (low + high) >> 1;
                if ((result = -list[mid].CompareTo(item)) > 0)
                    low = mid + 1;
                else if (result == 0)
                    return mid;
                else
                    high = mid - 1;
            }
            return -mid - (result < 0 ? 1 : 2);
        }

        /// <summary>
        /// Performs a binary search for the specified element in the specified
        /// sorted list using the specified comparator. The list needs to be already
        /// sorted according to the <paramref name="comparer"/> passed. Searching in an unsorted array
        /// has an undefined result. It's also undefined which element is found if
        /// there are multiple occurrences of the same element.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="list">The sorted <see cref="IList{T}"/> to search.</param>
        /// <param name="item">The element to find.</param>
        /// <param name="comparer">The comparer. If the comparer is <c>null</c> then the
        /// search uses the objects' natural ordering.</param>
        /// <returns>the non-negative index of the element, or a negative index which
        /// is the <c>-index - 1</c> where the element would be inserted.</returns>
        /// <exception cref="InvalidCastException">
        /// when an element in the list and the searched element cannot
        /// be compared to each other using the comparer.</exception>
        public static int BinarySearch<T>(this IList<T> list, T item, IComparer<T> comparer) where T : IComparable<T>
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (comparer == null)
                return BinarySearch(list, item);

            int low = 0, mid = list.Count, high = mid - 1, result = -1;
            while (low <= high)
            {
                mid = (low + high) >> 1;
                if ((result = -comparer.Compare(list[mid], item)) > 0)
                    low = mid + 1;
                else if (result == 0)
                    return mid;
                else
                    high = mid - 1;
            }
            return -mid - (result < 0 ? 1 : 2);
        }

        public static IList<T> SubList<T>(this IList<T> list, int fromIndex, int toIndex)
        {
            // .NET Port: This is to mimic Java's List.subList method, which has a different usage
            // than .NETs' List.GetRange. subList's parameters are indices, GetRange's parameters are a
            // starting index and a count. So we would need to do some light index math to translate this into
            // GetRange. This will be a safer extension method to use when translating java code
            // as there will be no question as to how to change it into GetRange. Also, subList returns
            // a list instance that, when modified, modifies the original list. So we're duplicating
            // that behavior as well.

            return new SubList<T>(list, fromIndex, toIndex);
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
            if (list is List<T>)
            {
                ((List<T>)list).Sort();
            }
            else
            {
                Util.CollectionUtil.TimSort(list);
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
            if (list is List<T>)
            {
                ((List<T>)list).Sort(comparer);
            }
            else
            {
                Util.CollectionUtil.TimSort(list, comparer);
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
            Util.CollectionUtil.TimSort(list);
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
            Util.CollectionUtil.TimSort(list, comparer);
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
            Util.CollectionUtil.IntroSort(list);
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
            Util.CollectionUtil.IntroSort(list, comparer);
        }

        #region Nested Type: FunctorComparer<T>

        private sealed class FunctorComparer<T> : IComparer<T>
        {
            private Comparison<T> comparison;

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

    #region SubList<T>

    internal sealed class SubList<T> : IList<T>
    {
        private readonly IList<T> list;
        private readonly int fromIndex;
        private int toIndex;

        /// <summary>
        /// Creates a ranged view of the given <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The original list to view.</param>
        /// <param name="fromIndex">The inclusive starting index.</param>
        /// <param name="toIndex">The exclusive ending index.</param>
        public SubList(IList<T> list, int fromIndex, int toIndex)
        {
            if (fromIndex < 0)
                throw new ArgumentOutOfRangeException("fromIndex");

            if (toIndex > list.Count)
                throw new ArgumentOutOfRangeException("toIndex");

            if (toIndex < fromIndex)
                throw new ArgumentOutOfRangeException("toIndex");

            if (list == null)
                throw new ArgumentNullException("list");

            this.list = list;
            this.fromIndex = fromIndex;
            this.toIndex = toIndex;
        }

        public int IndexOf(T item)
        {
            for (int i = fromIndex, fakeIndex = 0; i < toIndex; i++, fakeIndex++)
            {
                var current = list[i];

                if (current == null && item == null)
                    return fakeIndex;

                if (current.Equals(item))
                {
                    return fakeIndex;
                }
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            // TODO: is this the right behavior?
            list.RemoveAt(fromIndex + index);
            toIndex--;
        }

        public T this[int index]
        {
            get
            {
                return list[fromIndex + index];
            }
            set
            {
                list[fromIndex + index] = value;
            }
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            // TODO: is this the correct behavior?

            for (int i = toIndex - 1; i >= fromIndex; i--)
            {
                list.RemoveAt(i);
            }

            toIndex = fromIndex; // can't move further
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int count = array.Length - arrayIndex;

            for (int i = fromIndex, arrayi = arrayIndex; i <= Math.Min(toIndex - 1, fromIndex + count - 1); i++, arrayi++)
            {
                array[arrayi] = list[i];
            }
        }

        public int Count
        {
            get { return Math.Max(toIndex - fromIndex, 0); }
        }

        public bool IsReadOnly
        {
            get { return list.IsReadOnly; }
        }

        public bool Remove(T item)
        {
            var index = this.IndexOf(item); // get fake index

            if (index < 0)
                return false;

            list.RemoveAt(fromIndex + index);
            toIndex--;

            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return YieldItems().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<T> YieldItems()
        {
            for (int i = fromIndex; i <= Math.Min(toIndex - 1, list.Count - 1); i++)
            {
                yield return list[i];
            }
        }
    }

    #endregion SubList<T>
}