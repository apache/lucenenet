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

namespace Lucene.Net.Util
{
    /// <summary>
    /// <see cref="Sorter"/> implementation based on the merge-sort algorithm that merge
    /// in place (no extra memory will be allocated). Small arrays are sorted with insertion sort.
    /// This class is meant for internal use only.
    /// </summary>
    public abstract class InPlaceMergeSorter : Sorter
    {

        /** Create a new {@link InPlaceMergeSorter} */
        public InPlaceMergeSorter() { }

       
        public sealed override void SortRange(int start, int count)
        {
            this.CheckSlice(start, count);
            this.MergeSort(start, count);
        }

      
        private void MergeSort(int start, int end)
        {
            if (end - start < THRESHOLD)
            {
                this.InsertionSort(start, end);
            }
            else
            {
                int mid = (start + end) >> 1;

                this.MergeSort(start, mid);
                this.MergeSort(mid, end);
                this.MergeInPlace(start, mid, end);
            }
        }

    }
}