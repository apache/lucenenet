using Lucene.Net.Support;
using System;
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
    /// A <see cref="TimSorter"/> for object arrays.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    internal sealed class ArrayTimSorter<T> : TimSorter
    {
        private readonly IComparer<T> comparer;
        private readonly T[] arr;
        private readonly T[] tmp;

        /// <summary>
        /// Create a new <see cref="ArrayTimSorter{T}"/>. </summary>
        public ArrayTimSorter(T[] arr, IComparer<T> comparer, int maxTempSlots)
            : base(maxTempSlots)
        {
            this.arr = arr;
            this.comparer = comparer;
            if (maxTempSlots > 0)
            {
                T[] tmp = new T[maxTempSlots];
                this.tmp = tmp;
            }
            else
            {
                this.tmp = null;
            }
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
        protected override void Copy(int src, int dest)
        {
            arr[dest] = arr[src];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Save(int start, int len)
        {
            Arrays.Copy(arr, start, tmp, 0, len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Restore(int src, int dest)
        {
            arr[dest] = tmp[src];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int CompareSaved(int i, int j)
        {
            return comparer.Compare(tmp[i], arr[j]);
        }
    }
}