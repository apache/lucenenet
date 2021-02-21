using J2N.Numerics;
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
    /// <see cref="Sorter"/> implementation based on the merge-sort algorithm that merges
    /// in place (no extra memory will be allocated). Small arrays are sorted with
    /// insertion sort.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public abstract class InPlaceMergeSorter : Sorter
    {
        /// <summary>
        /// Create a new <see cref="InPlaceMergeSorter"/> </summary>
        protected InPlaceMergeSorter() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Sort the slice which starts at <paramref name="from"/> (inclusive) and ends at
        /// <paramref name="to"/> (exclusive).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed void Sort(int from, int to)
        {
            CheckRange(from, to);
            MergeSort(from, to);
        }

        internal virtual void MergeSort(int from, int to)
        {
            if (to - from < THRESHOLD)
            {
                InsertionSort(from, to);
            }
            else
            {
                int mid = (from + to).TripleShift(1);
                MergeSort(from, mid);
                MergeSort(mid, to);
                MergeInPlace(from, mid, to);
            }
        }
    }
}