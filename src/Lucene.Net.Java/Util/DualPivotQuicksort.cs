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

    // ReSharper disable CSharpWarnings::CS1574
    /// <summary>
    /// A .NET implementation of Valdimir Yaroslavskiy's Dual-Pivot Quicksort
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The overloaded methods are to avoid the extra compare operations for 
    ///         known value types: <see cref="short" />,<see cref="int" />, <see cref="long"/>
    ///         <see cref="byte" />, and <see cref="char" />.  
    ///         <see cref="DualPivotQuicksort.Sort(IComparable{T})"/> can be used for all other types
    ///         that implement <see cref="IComparable{T}"/>.
    ///     </para>
    /// </remarks>
    // ReSharper disable FunctionRecursiveOnAllPaths
    public class DualPivotQuicksort
    {
        private const int DIST_SIZE = 13;



        private static void Swap<T>(IList<T> array, int left, int right)
        {
            var reference = array[left];
            array[left] = array[right];
            array[right] = reference;
        }

        public static IList<int> Sort(IList<int> list)
        {
            Check.NotNull("list", list);

            PerformSort(list, 0, list.Count);

            return list;
        }

        public static IList<int> Sort(IList<int> list, int start, int count)
        {
            Check.NotNull("list", list);
            Check.Range("list", list, start, count);

            PerformSort(list, start, count);

            return list;
        }

        private static void PerformSort(IList<int> array, int left, int right)
        {
            int length = right - left,
                x,
                pivot1,
                pivot2;

           

            int sixth = length/6,
                m1 = left + sixth,
                m2 = m1 + sixth,
                m3 = m2 + sixth,
                m4 = m3 + sixth,
                m5 = m4 + sixth;

            if (array[m1] > array[m2])
                Swap(array, m1, m2);

            if (array[m4] > array[m5])
                Swap(array, m4, m5);

            if (array[m1] > array[m3])
                Swap(array, m1, m3);

            if (array[m2] > array[m3])
                Swap(array, m2, m3);

            if (array[m1] > array[m4])
                Swap(array, m2, m4);

            if (array[m3] > array[m4])
                Swap(array, m3, m4);

            if (array[m2] > array[m5])
                Swap(array, m2, m5);

            if (array[m2] > array[m3])
                Swap(array, m2, m3);

            if (array[m4] > array[m5])
                Swap(array, m4, m5);

            pivot1 = array[m2];
            pivot2 = array[m4];

            var pivotsAreDifferent = pivot1 != pivot2;

            array[m2] = array[left];
            array[m4] = array[right];

            int less = left + 1,
                great = right - 1;

            if (pivotsAreDifferent)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x < pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x > pivot2)
                    {
                        while (array[great] > pivot2 && k < great)
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (x >= pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }
            else
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x == pivot1)
                        continue;

                    if (x < pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x > pivot2)
                    {
                        while (array[great] > pivot2 && k < great)
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (x >= pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            array[left] = array[less - 1];
            array[left - 1] = pivot1;

            array[right] = array[great + 1];
            array[great + 1] = pivot2;

            PerformSort(array, left, less - 2);
            PerformSort(array, great + 1, right);

            if (!pivotsAreDifferent)
                return;


            if (great - less > length - DIST_SIZE)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x == pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x == pivot2)
                    {
                        array[k] = array[great];
                        array[great--] = x;
                        x = array[k];


                        if (x != pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            PerformSort(array, less, great);
        }


        public static IList<char> Sort(IList<char> list)
        {
            Check.NotNull("list", list);

            PerformSort(list, 0, list.Count);

            return list;
        }

        public static IList<char> Sort(IList<char> list, int start, int count)
        {
            Check.NotNull("list", list);
            Check.Range("list", list, start, count);

            PerformSort(list, start, count);

            return list;
        }

        private static void PerformSort(IList<char> array, int left, int right)
        {
            int length = right - left;
            char x,
                pivot1,
                pivot2;

          

            int sixth = length/6,
                m1 = left + sixth,
                m2 = m1 + sixth,
                m3 = m2 + sixth,
                m4 = m3 + sixth,
                m5 = m4 + sixth;

            if (array[m1] > array[m2])
                Swap(array, m1, m2);

            if (array[m4] > array[m5])
                Swap(array, m4, m5);

            if (array[m1] > array[m3])
                Swap(array, m1, m3);

            if (array[m2] > array[m3])
                Swap(array, m2, m3);

            if (array[m1] > array[m4])
                Swap(array, m2, m4);

            if (array[m3] > array[m4])
                Swap(array, m3, m4);

            if (array[m2] > array[m5])
                Swap(array, m2, m5);

            if (array[m2] > array[m3])
                Swap(array, m2, m3);

            if (array[m4] > array[m5])
                Swap(array, m4, m5);

            pivot1 = array[m2];
            pivot2 = array[m4];

            bool pivotsAreDifferent = pivot1 != pivot2;

            array[m2] = array[left];
            array[m4] = array[right];

            int less = left + 1,
                great = right - 1;

            if (pivotsAreDifferent)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x < pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x > pivot2)
                    {
                        while (array[great] > pivot2 && k < great)
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (x >= pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }
            else
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x == pivot1)
                        continue;

                    if (x < pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x > pivot2)
                    {
                        while (array[great] > pivot2 && k < great)
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (x >= pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            array[left] = array[less - 1];
            array[left - 1] = pivot1;

            array[right] = array[great + 1];
            array[great + 1] = pivot2;

            PerformSort(array, left, less - 2);
            PerformSort(array, great + 1, right);

            if (!pivotsAreDifferent)
                return;


            if (great - less > length - DIST_SIZE)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x == pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x == pivot2)
                    {
                        array[k] = array[great];
                        array[great--] = x;
                        x = array[k];


                        if (x != pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            PerformSort(array, less, great);
        }

        public static IList<short> Sort(IList<short> list)
        {
            Check.NotNull("list", list);
            PerformSort(list, 0, list.Count);
            return list;
        }

        public static IList<short> Sort(IList<short> list, int start, int count)
        {
            Check.NotNull("list", list);
            Check.Range("list", list, start, count);
            PerformSort(list, start, count);
            return list;
        }

        private static void PerformSort(IList<short> array, int left, int right)
        {
            var length = right - left;
            short x,
                pivot1,
                pivot2;

           

            int sixth = length/6,
                m1 = left + sixth,
                m2 = m1 + sixth,
                m3 = m2 + sixth,
                m4 = m3 + sixth,
                m5 = m4 + sixth;

            if (array[m1] > array[m2])
                Swap(array, m1, m2);

            if (array[m4] > array[m5])
                Swap(array, m4, m5);

            if (array[m1] > array[m3])
                Swap(array, m1, m3);

            if (array[m2] > array[m3])
                Swap(array, m2, m3);

            if (array[m1] > array[m4])
                Swap(array, m2, m4);

            if (array[m3] > array[m4])
                Swap(array, m3, m4);

            if (array[m2] > array[m5])
                Swap(array, m2, m5);

            if (array[m2] > array[m3])
                Swap(array, m2, m3);

            if (array[m4] > array[m5])
                Swap(array, m4, m5);

            pivot1 = array[m2];
            pivot2 = array[m4];

            bool pivotsAreDifferent = pivot1 != pivot2;

            array[m2] = array[left];
            array[m4] = array[right];

            int less = left + 1,
                great = right - 1;

            if (pivotsAreDifferent)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x < pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x > pivot2)
                    {
                        while (array[great] > pivot2 && k < great)
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (x >= pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }
            else
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x == pivot1)
                        continue;

                    if (x < pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x > pivot2)
                    {
                        while (array[great] > pivot2 && k < great)
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (x >= pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            array[left] = array[less - 1];
            array[left - 1] = pivot1;

            array[right] = array[great + 1];
            array[great + 1] = pivot2;

            PerformSort(array, left, less - 2);
            PerformSort(array, great + 1, right);

            if (!pivotsAreDifferent)
                return;


            if (great - less > length - DIST_SIZE)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x == pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x == pivot2)
                    {
                        array[k] = array[great];
                        array[great--] = x;
                        x = array[k];


                        if (x != pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            PerformSort(array, less, great);
        }


        public static IList<byte> Sort(IList<byte> list, int start, int count)
        {
            Check.NotNull("list", list);
            Check.Range("list", list, start, count);

            PerformSort(list, start, count);

            return list;
        }

        internal static void PerformSort(IList<byte> array, int left, int right)
        {
            int length = right - left;
            byte x,
                pivot1,
                pivot2;

            

            int sixth = length/6,
                m1 = left + sixth,
                m2 = m1 + sixth,
                m3 = m2 + sixth,
                m4 = m3 + sixth,
                m5 = m4 + sixth;

            if (array[m1] > array[m2])
                Swap(array, m1, m2);

            if (array[m4] > array[m5])
                Swap(array, m4, m5);

            if (array[m1] > array[m3])
                Swap(array, m1, m3);

            if (array[m2] > array[m3])
                Swap(array, m2, m3);

            if (array[m1] > array[m4])
                Swap(array, m2, m4);

            if (array[m3] > array[m4])
                Swap(array, m3, m4);

            if (array[m2] > array[m5])
                Swap(array, m2, m5);

            if (array[m2] > array[m3])
                Swap(array, m2, m3);

            if (array[m4] > array[m5])
                Swap(array, m4, m5);

            pivot1 = array[m2];
            pivot2 = array[m4];

            bool pivotsAreDifferent = pivot1 != pivot2;

            array[m2] = array[left];
            array[m4] = array[right];

            int less = left + 1,
                great = right - 1;

            if (pivotsAreDifferent)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x < pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x > pivot2)
                    {
                        while (array[great] > pivot2 && k < great)
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (x >= pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }
            else
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x == pivot1)
                        continue;

                    if (x < pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x > pivot2)
                    {
                        while (array[great] > pivot2 && k < great)
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (x >= pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            array[left] = array[less - 1];
            array[left - 1] = pivot1;

            array[right] = array[great + 1];
            array[great + 1] = pivot2;

            PerformSort(array, left, less - 2);
            PerformSort(array, great + 1, right);

            if (!pivotsAreDifferent)
                return;


            if (great - less > length - DIST_SIZE)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x == pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x == pivot2)
                    {
                        array[k] = array[great];
                        array[great--] = x;
                        x = array[k];


                        if (x != pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            PerformSort(array, less, great);
        }

        public static long[] Sort(long[] array)
        {
            Check.NotNull("array", array);
            PerformSort(array, 0, array.Length);
            return array;
        }

        public static long[] Sort(long[] array, int start, int count)
        {
            Check.NotNull("array", array);
            Check.Range("array", array, start, count);
            PerformSort(array, start, count);
            return array;
        }


        internal static void PerformSort(long[] array, int left, int right)
        {
            int length = right - left;
            long x,
                pivot1,
                pivot2;

           

            int sixth = length/6,
                m1 = left + sixth,
                m2 = m1 + sixth,
                m3 = m2 + sixth,
                m4 = m3 + sixth,
                m5 = m4 + sixth;

            if (array[m1] > array[m2])
                Swap(array, m1, m2);

            if (array[m4] > array[m5])
                Swap(array, m4, m5);

            if (array[m1] > array[m3])
                Swap(array, m1, m3);

            if (array[m2] > array[m3])
                Swap(array, m2, m3);

            if (array[m1] > array[m4])
                Swap(array, m2, m4);

            if (array[m3] > array[m4])
                Swap(array, m3, m4);

            if (array[m2] > array[m5])
                Swap(array, m2, m5);

            if (array[m2] > array[m3])
                Swap(array, m2, m3);

            if (array[m4] > array[m5])
                Swap(array, m4, m5);

            pivot1 = array[m2];
            pivot2 = array[m4];

            bool pivotsAreDifferent = pivot1 != pivot2;

            array[m2] = array[left];
            array[m4] = array[right];

            int less = left + 1,
                great = right - 1;

            if (pivotsAreDifferent)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x < pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x > pivot2)
                    {
                        while (array[great] > pivot2 && k < great)
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (x >= pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }
            else
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x == pivot1)
                        continue;

                    if (x < pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x > pivot2)
                    {
                        while (array[great] > pivot2 && k < great)
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (x >= pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            array[left] = array[less - 1];
            array[left - 1] = pivot1;

            array[right] = array[great + 1];
            array[great + 1] = pivot2;

            PerformSort(array, left, less - 2);
            PerformSort(array, great + 1, right);

            if (!pivotsAreDifferent)
                return;


            if (great - less > length - DIST_SIZE)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x == pivot1)
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x == pivot2)
                    {
                        array[k] = array[great];
                        array[great--] = x;
                        x = array[k];


                        if (x != pivot1)
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            PerformSort(array, less, great);
        }

        private static bool LessThan<T>(T left, T right) where T : IComparable<T>
        {
            return left.CompareTo(right) < 0;
        }

        private static bool GreaterThan<T>(T left, T right) where T : IComparable<T>
        {
            return left.CompareTo(right) > 0;
        }

        private static bool GreaterThanOrEqualTo<T>(T left, T right) where T : IComparable<T>
        {
            var compare = left.CompareTo(right);
            return compare > 0 || compare == 0;
        }

        public static IList<T> Sort<T>(IList<T> list) where T: IComparable<T>
        {
            Check.NotNull("array", list);
            PerformSort(list, 0, list.Count);

            return list;
        }

        public static IList<T> Sort<T>(IList<T> list, int start, int count)  where T: IComparable<T> 
        {
            Check.NotNull("array", list);
            Check.Range("array", list, start, count);
            PerformSort(list, start, count);

            return list;
        }


        internal static void PerformSort<T>(IList<T> array, int left, int right) where T: IComparable<T> 
        {
            int length = right - left;
            T x, pivot2, pivot1;

            

            int sixth = length / 6,
                m1 = left + sixth,
                m2 = m1 + sixth,
                m3 = m2 + sixth,
                m4 = m3 + sixth,
                m5 = m4 + sixth;

            if (GreaterThan(array[m1] , array[m2]))
                Swap(array, m1, m2);

            if (GreaterThan(array[m4] , array[m5]))
                Swap(array, m4, m5);

            if (GreaterThan(array[m1] , array[m3]))
                Swap(array, m1, m3);

            if (GreaterThan(array[m2], array[m3]))
                Swap(array, m2, m3);

            if (GreaterThan(array[m1], array[m4]))
                Swap(array, m2, m4);

            if (GreaterThan(array[m3], array[m4]))
                Swap(array, m3, m4);

            if (GreaterThan(array[m2], array[m5]))
                Swap(array, m2, m5);

            if (GreaterThan(array[m2],array[m3]))
                Swap(array, m2, m3);

            if (GreaterThan(array[m4], array[m5]))
                Swap(array, m4, m5);

            pivot1 = array[m2];
            pivot2 = array[m4];

            bool pivotsAreDifferent = !pivot1.Equals(pivot2);

            array[m2] = array[left];
            array[m4] = array[right];

            int less = left + 1,
                great = right - 1;

            if (pivotsAreDifferent)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (LessThan(x , pivot1))
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (GreaterThan(x , pivot2))
                    {
                        while (GreaterThan(array[great] , pivot2) && LessThan(k , great))
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (GreaterThanOrEqualTo(x , pivot1))
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }
            else
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (LessThan(x, pivot1))
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (GreaterThan(x, pivot2))
                    {
                        while (GreaterThan(array[great], pivot2) && LessThan(k, great))
                        {
                            great--;
                        }
                        Swap(array, k, great--);

                        if (GreaterThanOrEqualTo(x, pivot1))
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            array[left] = array[less - 1];
            array[left - 1] = pivot1;

            array[right] = array[great + 1];
            array[great + 1] = pivot2;

            PerformSort(array, left, less - 2);
            PerformSort(array, great + 1, right);

            if (!pivotsAreDifferent)
                return;


            if (great - less > length - DIST_SIZE)
            {
                for (var k = less; k <= great; k++)
                {
                    x = array[k];

                    if (x.Equals(pivot1))
                    {
                        array[k] = array[less];
                        array[less++] = x;
                    }
                    else if (x.Equals(pivot2))
                    {
                        array[k] = array[great];
                        array[great--] = x;
                        x = array[k];


                        if (! x.Equals(pivot1))
                            continue;

                        array[k] = array[less];
                        array[less++] = x;
                    }
                }
            }

            PerformSort(array, less, great);
        }
    }
}
