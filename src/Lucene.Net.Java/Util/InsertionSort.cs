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

namespace Java.Util
{
    using System;
    using System.Collections.Generic;

    public static class InsertionSort
    {

      

        private static void Swap<T>(IList<T> list,int left , int right )
        {
            var reference = list[left];
            list[left] = list[right];
            list[right] = reference;
        }




        public static IList<T> Sort<T>(IList<T> list) where T : IComparable<T>
        {
            Check.NotNull("list", list);

            return PerformSort(list, 0, list.Count);
        }

        public static IList<T> Sort<T>(IList<T> list, int start, int count)where T: IComparable<T>
        {
            Check.NotNull("list", list);
            Check.Range("list", list, start, count);

            return PerformSort(list, start, count);
        }

        internal static IList<T> PerformSort<T>(IList<T> list, int start, int count) where T: IComparable<T>
        {
            for (var i = start + 1; i < count; i++)
            {
               
                for (var j = i; j > start && list[j - 1].CompareTo(list[j]) >= 0; j--)
                {
                    // swap
                    Swap(list, j - 1, j);
                }
            }

            return list;
        }
    }
}
