using System;
using System.Collections.Generic;

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
    /// A <seealso cref="TimSorter"/> for object arrays.
    /// @lucene.internal
    /// </summary>
    internal sealed class ArrayTimSorter<T> : TimSorter
    {
        private readonly IComparer<T> comparator;
        private readonly T[] arr;
        private readonly T[] tmp;

        /// <summary>
        /// Create a new <seealso cref="ArrayTimSorter"/>. </summary>
        public ArrayTimSorter(T[] arr, IComparer<T> comparator, int maxTempSlots)
            : base(maxTempSlots)
        {
            this.arr = arr;
            this.comparator = comparator;
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

        protected override int Compare(int i, int j)
        {
            return comparator.Compare(arr[i], arr[j]);
        }

        protected override void Swap(int i, int j)
        {
            ArrayUtil.Swap(arr, i, j);
        }

        protected override void Copy(int src, int dest)
        {
            arr[dest] = arr[src];
        }

        protected override void Save(int start, int len)
        {
            Array.Copy(arr, start, tmp, 0, len);
        }

        protected override void Restore(int src, int dest)
        {
            arr[dest] = tmp[src];
        }

        protected override int CompareSaved(int i, int j)
        {
            return comparator.Compare(tmp[i], arr[j]);
        }
    }
}