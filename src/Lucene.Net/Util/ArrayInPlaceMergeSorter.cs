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
    /// An <seealso cref="InPlaceMergeSorter"/> for object arrays.
    /// @lucene.internal
    /// </summary>
    internal sealed class ArrayInPlaceMergeSorter<T> : InPlaceMergeSorter
    {
        private readonly T[] arr;
        private readonly IComparer<T> comparer;

        /// <summary>
        /// Create a new <seealso cref="ArrayInPlaceMergeSorter"/>. </summary>
        public ArrayInPlaceMergeSorter(T[] arr, IComparer<T> comparer)
        {
            this.arr = arr;
            this.comparer = comparer;
        }

        protected override int Compare(int i, int j)
        {
            return comparer.Compare(arr[i], arr[j]);
        }

        protected override void Swap(int i, int j)
        {
            ArrayUtil.Swap(arr, i, j);
        }
    }
}