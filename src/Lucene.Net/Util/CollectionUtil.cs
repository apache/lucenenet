using J2N.Collections.Generic.Extensions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
    /// Methods for manipulating (sorting) collections.
    /// Sort methods work directly on the supplied lists and don't copy to/from arrays
    /// before/after. For medium size collections as used in the Lucene indexer that is
    /// much more efficient.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public static class CollectionUtil // LUCENENET specific - made static
    {
        private sealed class ListIntroSorter<T> : IntroSorter
        {
            internal T pivot;
            internal IList<T> list;
            internal readonly IComparer<T> comp;

            internal ListIntroSorter(IList<T> list, IComparer<T> comp)
                : base()
            {
                // LUCENENET NOTE: All ILists in .NET are random access (only IEnumerable is forward-only)
                //if (!(list is RandomAccess))
                //{
                //  throw new ArgumentException("CollectionUtil can only sort random access lists in-place.");
                //}
                
                this.list = list;
                this.comp = comp;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void SetPivot(int i)
            {
                pivot = (i < list.Count) ? list[i] : default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void Swap(int i, int j)
            {
                list.Swap(i, j);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override int Compare(int i, int j)
            {
                return comp.Compare(list[i], list[j]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override int ComparePivot(int j)
            {
                return comp.Compare(pivot, list[j]);
            }
        }

        private sealed class ListTimSorter<T> : TimSorter
        {
            internal IList<T> list;
            internal readonly IComparer<T> comp;
            internal readonly T[] tmp;

            internal ListTimSorter(IList<T> list, IComparer<T> comp, int maxTempSlots)
                : base(maxTempSlots)
            {
                // LUCENENET NOTE: All ILists in .NET are random access (only IEnumerable is forward-only)
                //if (!(list is RandomAccess))
                //{
                //  throw new ArgumentException("CollectionUtil can only sort random access lists in-place.");
                //}
                this.list = list;
                this.comp = comp;
                if (maxTempSlots > 0)
                {
                    this.tmp = new T[maxTempSlots];
                }
                else
                {
                    this.tmp = null;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void Swap(int i, int j)
            {
                list.Swap(i, j);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void Copy(int src, int dest)
            {
                list[dest] = list[src];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void Save(int i, int len)
            {
                for (int j = 0; j < len; ++j)
                {
                    tmp[j] = list[i + j];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void Restore(int i, int j)
            {
                list[j] = tmp[i];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override int Compare(int i, int j)
            {
                return comp.Compare(list[i], list[j]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override int CompareSaved(int i, int j)
            {
                return comp.Compare(tmp[i], list[j]);
            }
        }

        /// <summary>
        /// Sorts the given <see cref="IList{T}"/> using the <see cref="IComparer{T}"/>.
        /// This method uses the intro sort
        /// algorithm, but falls back to insertion sort for small lists. 
        /// </summary>
        /// <param name="list">This <see cref="IList{T}"/></param>
        /// <param name="comp">The <see cref="IComparer{T}"/> to use for the sort.</param>
        public static void IntroSort<T>(IList<T> list, IComparer<T> comp)
        {
            int size = list.Count;
            if (size <= 1)
            {
                return;
            }
            (new ListIntroSorter<T>(list, comp)).Sort(0, size);
        }

        /// <summary>
        /// Sorts the given random access <see cref="IList{T}"/> in natural order.
        /// This method uses the intro sort
        /// algorithm, but falls back to insertion sort for small lists. 
        /// </summary>
        /// <param name="list">This <see cref="IList{T}"/></param>
        public static void IntroSort<T>(IList<T> list)
            //where T : IComparable<T> // LUCENENET specific: removing constraint because in .NET, it is not needed
        {
            int size = list.Count;
            if (size <= 1)
            {
                return;
            }
            IntroSort(list, ArrayUtil.GetNaturalComparer<T>());
        }

        // Tim sorts:

        /// <summary>
        /// Sorts the given <see cref="IList{T}"/> using the <see cref="IComparer{T}"/>.
        /// This method uses the Tim sort
        /// algorithm, but falls back to binary sort for small lists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">this <see cref="IList{T}"/></param>
        /// <param name="comp">The <see cref="IComparer{T}"/> to use for the sort.</param>
        public static void TimSort<T>(IList<T> list, IComparer<T> comp)
        {
            int size = list.Count;
            if (size <= 1)
            {
                return;
            }
            (new ListTimSorter<T>(list, comp, list.Count / 64)).Sort(0, size);
        }

        /// <summary>
        /// Sorts the given <see cref="IList{T}"/> in natural order.
        /// This method uses the Tim sort
        /// algorithm, but falls back to binary sort for small lists. </summary>
        /// <param name="list">This <see cref="IList{T}"/></param>
        public static void TimSort<T>(IList<T> list)
            //where T : IComparable<T> // LUCENENET specific: removing constraint because in .NET, it is not needed
        {
            int size = list.Count;
            if (size <= 1)
            {
                return;
            }
            TimSort(list, ArrayUtil.GetNaturalComparer<T>());
        }
    }
}