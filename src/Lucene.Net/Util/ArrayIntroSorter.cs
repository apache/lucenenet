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
    /// An <see cref="IntroSorter"/> for object arrays.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    internal sealed class ArrayIntroSorter<T> : IntroSorter
    {
        private readonly T[] arr;
        private readonly IComparer<T> comparer;
        private T pivot;

        /// <summary>
        /// Create a new <see cref="ArrayIntroSorter{T}"/>. </summary>
        public ArrayIntroSorter(T[] arr, IComparer<T> comparer)
        {
            this.arr = arr;
            this.comparer = comparer;
            pivot = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int Compare(int i, int j)
        {
            return comparer.Compare(arr[i], arr[j]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Swap(int i, int j)
        {
            ArrayUtil.Swap(arr, i, j);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void SetPivot(int i)
        {
            pivot = (i < arr.Length) ? arr[i] : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int ComparePivot(int i)
        {
            return comparer.Compare(pivot, arr[i]);
        }
    }
}