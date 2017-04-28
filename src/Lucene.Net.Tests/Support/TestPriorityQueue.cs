// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if FEATURE_SERIALIZABLE
using System.Runtime.Serialization.Formatters.Binary;
#endif

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

    public class TestPriorityQueue : Util.LuceneTestCase
    {
        //private class Integer : IComparable<Integer>
        //{
        //    private readonly int value;

        //    public Integer(int value)
        //    {
        //        this.value = value;
        //    }

        //    public int CompareTo(Integer other)
        //    {
        //        value.CompareTo()
        //    }

        //    public override bool Equals(object obj)
        //    {
        //        if (!(obj is int))
        //        {
        //            return false;
        //        }

        //        return value.Equals((int)obj);
        //    }

        //    public override int GetHashCode()
        //    {
        //        return value.GetHashCode();
        //    }

        //    public override string ToString()
        //    {
        //        return value.ToString();
        //    }
        //}


        /// <summary>
        /// @tests java.util.PriorityQueue#iterator()
        /// </summary>
        [Test, LuceneNetSpecific]
        public void Test_Iterator()
        {
            PriorityQueue<int> integerQueue = new PriorityQueue<int>(Comparer<int>.Default);
            int[] array = { 2, 45, 7, -12, 9 };
            for (int i = 0; i < array.Length; i++)
            {
                integerQueue.Offer(array[i]);
            }
            List<int> iterResult = new List<int>();
            using (IEnumerator<int> iter = integerQueue.GetEnumerator())
            {
                assertNotNull(iter);

                while (iter.MoveNext())
                {
                    iterResult.Add(iter.Current);
                }
            }
            var resultArray = iterResult.ToArray();
            Array.Sort(array);
            Array.Sort(resultArray);
            assertTrue(Arrays.Equals(array, resultArray));
        }

        [Test, LuceneNetSpecific]
        public void Test_Iterator_Empty()
        {
            PriorityQueue<int?> integerQueue = new PriorityQueue<int?>(Comparer<int?>.Default);
            IEnumerator<int?> iter;
            using (iter = integerQueue.GetEnumerator())
            {
                assertFalse(iter.MoveNext());
            }
            // LUCENENET: Remove not supported in .NET
            //using (iter = integerQueue.GetEnumerator())
            //{
            //    try
            //    {
            //        iter.remove();
            //        fail("should throw IllegalStateException");
            //    }
            //    catch (IllegalStateException e)
            //    {
            //        // expected
            //    }
            //}
        }

        [Test, LuceneNetSpecific]
        public void Test_Iterator_Outofbound()
        {
            PriorityQueue<int?> integerQueue = new PriorityQueue<int?>(Comparer<int?>.Default);
            integerQueue.Offer(0);
            IEnumerator<int?> iter;

            using (iter = integerQueue.GetEnumerator())
            {
                iter.MoveNext();
                assertFalse(iter.MoveNext());
            }
            // LUCENENET: Remove not supported in .NET
            //using (iter = integerQueue.GetEnumerator())
            //{
            //    iter.MoveNext();
            //    iter.remove();
            //    try
            //    {
            //        iter.next();
            //        fail("should throw NoSuchElementException");
            //    }
            //    catch (NoSuchElementException e)
            //    {
            //        // expected
            //    }
            //}
        }

        // Iterator Remove methods omitted...

        [Test, LuceneNetSpecific]
        public void Test_Size()
        {
            PriorityQueue<int?> integerQueue = new PriorityQueue<int?>(Comparer<int?>.Default);
            assertEquals(0, integerQueue.size());
            int[] array = { 2, 45, 7, -12, 9 };
            for (int i = 0; i < array.Length; i++)
            {
                integerQueue.Offer(array[i]);
            }
            assertEquals(array.Length, integerQueue.size());
        }

        [Test, LuceneNetSpecific]
        public void Test_Constructor()
        {
            PriorityQueue<object> queue = new PriorityQueue<object>();
            assertNotNull(queue);
            assertEquals(0, queue.size());
            assertNull(queue.Comparer);
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorI()
        {
            PriorityQueue<object> queue = new PriorityQueue<object>(100);
            assertNotNull(queue);
            assertEquals(0, queue.size());
            assertNull(queue.Comparer);
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorILComparer()
        {
            PriorityQueue<Object> queue = new PriorityQueue<Object>(100,
                    (IComparer<Object>)null);
            assertNotNull(queue);
            assertEquals(0, queue.size());
            assertNull(queue.Comparer);

            MockComparer<Object> comparator = new MockComparer<Object>();
            queue = new PriorityQueue<Object>(100, comparator);
            assertNotNull(queue);
            assertEquals(0, queue.size());
            assertEquals(comparator, queue.Comparer);
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorILComparer_illegalCapacity()
        {
            try
            {
                new PriorityQueue<Object>(0, new MockComparer<Object>());
                fail("should throw ArgumentException");
            }
            catch (ArgumentException e)
            {
                // expected
            }

            try
            {
                new PriorityQueue<Object>(-1, new MockComparer<Object>());
                fail("should throw ArgumentException");
            }
            catch (ArgumentException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorILComparer_cast()
        {
            MockComparerCast<int> objectComparator = new MockComparerCast<int>();
            PriorityQueue<int> integerQueue = new PriorityQueue<int>(100,
                    objectComparator);
            assertNotNull(integerQueue);
            assertEquals(0, integerQueue.size());
            assertEquals(objectComparator, integerQueue.Comparer);
            int[] array = { 2, 45, 7, -12, 9 };
            List<int> list = Arrays.AsList(array);
            integerQueue.AddAll(list);
            assertEquals(list.size(), integerQueue.size());
            // just test here no cast exception raises.
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorLCollection()
        {
            int[] array = { 2, 45, 7, -12, 9 };
            List<int> list = Arrays.AsList(array);
            PriorityQueue<int> integerQueue = new PriorityQueue<int>(list);
            assertEquals(array.Length, integerQueue.size());
            assertNull(integerQueue.Comparer);
            Array.Sort(array);
            for (int i = 0; i < array.Length; i++)
            {
                assertEquals(array[i], integerQueue.Poll());
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorLColleciton_null()
        {
            List<Object> list = new List<Object>();
            list.Add(new float?(11));
            list.Add(null);
            list.Add(new int?(10));
            try
            {
                new PriorityQueue<Object>(list);
                fail("should throw NullReferenceException");
            }
            catch (NullReferenceException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorLColleciton_non_comparable()
        {
            List<Object> list = new List<Object>();
            list.Add(new float?(11));
            list.Add(new int?(10));
            try
            {
                new PriorityQueue<Object>(list);
                fail("should throw InvalidCastException");
            }
            catch (InvalidCastException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorLColleciton_from_priorityqueue()
        {
            String[] array = { "AAAAA", "AA", "AAAA", "AAAAAAAA" };
            PriorityQueue<String> queue = new PriorityQueue<String>(4,
                    new MockComparerStringByLength());
            for (int i = 0; i < array.Length; i++)
            {
                queue.Offer(array[i]);
            }
            ICollection<String> c = queue;
            PriorityQueue<String> constructedQueue = new PriorityQueue<String>(c);
            assertEquals(queue.Comparer, constructedQueue.Comparer);
            while (queue.size() > 0)
            {
                assertEquals(queue.Poll(), constructedQueue.Poll());
            }
            assertEquals(0, constructedQueue.size());
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorLCollection_from_sortedset()
        {
            int[] array = { 3, 5, 79, -17, 5 };
            SortedSet<int> treeSet = new SortedSet<int>(new MockComparer<int>());
            for (int i = 0; i < array.Length; i++)
            {
                treeSet.add(array[i]);
            }
            ICollection<int> c = treeSet;
            PriorityQueue<int> queue = new PriorityQueue<int>(c);
            assertEquals(treeSet.Comparer, queue.Comparer);
            IEnumerator<int> iter = treeSet.GetEnumerator();
            while (iter.MoveNext())
            {
                assertEquals(iter.Current, queue.Poll());
            }
            assertEquals(0, queue.size());
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorLCollection_from_treeset()
        {
            int[] array = { 3, 5, 79, -17, 5 };
            TreeSet<int> treeSet = new TreeSet<int>(new MockComparer<int>());
            for (int i = 0; i < array.Length; i++)
            {
                treeSet.add(array[i]);
            }
            ICollection<int> c = treeSet;
            PriorityQueue<int> queue = new PriorityQueue<int>(c);
            assertEquals(treeSet.Comparer, queue.Comparer);
            IEnumerator<int> iter = treeSet.GetEnumerator();
            while (iter.MoveNext())
            {
                assertEquals(iter.Current, queue.Poll());
            }
            assertEquals(0, queue.size());
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorLPriorityQueue()
        {
            PriorityQueue<int> integerQueue = new PriorityQueue<int>();
            int[] array = { 2, 45, 7, -12, 9 };
            for (int i = 0; i < array.Length; i++)
            {
                integerQueue.Offer(array[i]);
            }
            // Can't cast int > object in .NET
            PriorityQueue<int> objectQueue = new PriorityQueue<int>(
                    integerQueue);
            assertEquals(integerQueue.size(), objectQueue.size());
            assertEquals(integerQueue.Comparer, objectQueue.Comparer);
            Array.Sort(array);
            for (int i = 0; i < array.Length; i++)
            {
                assertEquals(array[i], objectQueue.Poll());
            }
        }

        [Test, LuceneNetSpecific]
        public void test_ConstructorLPriorityQueue_null()
        {
            try
            {
                new PriorityQueue<int>((PriorityQueue<int>)null);
                fail("should throw ArgumentNullException");
            }
            catch (ArgumentNullException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorLSortedSet()
        {
            int[] array = { 3, 5, 79, -17, 5 };
            SortedSet<int> treeSet = new SortedSet<int>();
            for (int i = 0; i < array.Length; i++)
            {
                treeSet.add(array[i]);
            }
            PriorityQueue<int> queue = new PriorityQueue<int>(treeSet);
            var iter = treeSet.GetEnumerator();
            while (iter.MoveNext())
            {
                assertEquals(iter.Current, queue.Poll());
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorLTreeSet()
        {
            int[] array = { 3, 5, 79, -17, 5 };
            TreeSet<int> treeSet = new TreeSet<int>();
            for (int i = 0; i < array.Length; i++)
            {
                treeSet.add(array[i]);
            }
            PriorityQueue<int> queue = new PriorityQueue<int>(treeSet);
            var iter = treeSet.GetEnumerator();
            while (iter.MoveNext())
            {
                assertEquals(iter.Current, queue.Poll());
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorLSortedSet_null()
        {
            try
            {
                new PriorityQueue<int>((SortedSet<int>) null);
                fail("should throw ArgumentNullException");
            }
            catch (ArgumentNullException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_ConstructorLTreeSet_null()
        {
            try
            {
                new PriorityQueue<int>((TreeSet<int>)null);
                fail("should throw ArgumentNullException");
            }
            catch (ArgumentNullException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_OfferLObject()
        {
            PriorityQueue<String> queue = new PriorityQueue<String>(10,
                    new MockComparerStringByLength());
            String[] array = { "AAAAA", "AA", "AAAA", "AAAAAAAA" };
            for (int i = 0; i < array.Length; i++)
            {
                queue.Offer(array[i]);
            }
            String[] sortedArray = { "AA", "AAAA", "AAAAA", "AAAAAAAA" };
            for (int i = 0; i < sortedArray.Length; i++)
            {
                assertEquals(sortedArray[i], queue.Poll());
            }
            assertEquals(0, queue.size());
            assertNull(queue.Poll());
        }

        [Test, LuceneNetSpecific]
        public void Test_OfferLObject_null()
        {
            PriorityQueue<Object> queue = new PriorityQueue<Object>();
            try
            {
                queue.Offer(null);
                fail("should throw ArgumentNullException");
            }
            catch (ArgumentNullException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_Offer_LObject_non_Comparable()
        {
            PriorityQueue<Object> queue = new PriorityQueue<Object>();
            queue.Offer(new int?(10));
            try
            {
                queue.Offer(new float?(1.3f));
                fail("should throw InvalidCastException");
            }
            catch (InvalidCastException e)
            {
                // expected
            }

            queue = new PriorityQueue<Object>();
            queue.Offer(new int?(10));
            try
            {
                queue.Offer(new Object());
                fail("should throw InvalidCastException");
            }
            catch (InvalidCastException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_Poll()
        {
            PriorityQueue<String> stringQueue = new PriorityQueue<String>();
            String[] array = { "MYTESTSTRING", "AAAAA", "BCDEF", "ksTRD", "AAAAA" };
            for (int i = 0; i < array.Length; i++)
            {
                stringQueue.Offer(array[i]);
            }
            Array.Sort(array);
            for (int i = 0; i < array.Length; i++)
            {
                assertEquals(array[i], stringQueue.Poll());
            }
            assertEquals(0, stringQueue.size());
            assertNull(stringQueue.Poll());
        }

        [Test, LuceneNetSpecific]
        public void Test_Poll_empty()
        {
            PriorityQueue<Object> queue = new PriorityQueue<Object>();
            assertEquals(0, queue.size());
            assertNull(queue.Poll());
        }

        [Test, LuceneNetSpecific]
        public void Test_Peek()
        {
            PriorityQueue<int> integerQueue = new PriorityQueue<int>();
            int[] array = { 2, 45, 7, -12, 9 };
            for (int i = 0; i < array.Length; i++)
            {
                integerQueue.Add(array[i]);
            }
            Array.Sort(array);
            assertEquals(new int?(array[0]), integerQueue.Peek());
            assertEquals(new int?(array[0]), integerQueue.Peek());
        }

        [Test, LuceneNetSpecific]
        public void Test_Peek_empty()
        {
            PriorityQueue<Object> queue = new PriorityQueue<Object>();
            assertEquals(0, queue.size());
            assertNull(queue.Peek());
            assertNull(queue.Peek());
        }

        [Test, LuceneNetSpecific]
        public void Test_Clear()
        {
            PriorityQueue<int> integerQueue = new PriorityQueue<int>();
            int[] array = { 2, 45, 7, -12, 9 };
            for (int i = 0; i < array.Length; i++)
            {
                integerQueue.Offer(array[i]);
            }
            integerQueue.Clear();
            assertTrue(!integerQueue.Any());
        }

        [Test, LuceneNetSpecific]
        public void Test_Add_LObject()
        {
            PriorityQueue<int> integerQueue = new PriorityQueue<int>();
            int[] array = { 2, 45, 7, -12, 9 };
            for (int i = 0; i < array.Length; i++)
            {
                integerQueue.Add(array[i]);
            }
            Array.Sort(array);
            assertEquals(array.Length, integerQueue.size());
            for (int i = 0; i < array.Length; i++)
            {
                assertEquals(array[i], integerQueue.Poll());
            }
            assertEquals(0, integerQueue.size());
        }

        [Test, LuceneNetSpecific]
        public void Test_Add_LObject_null()
        {
            PriorityQueue<Object> queue = new PriorityQueue<Object>();
            try
            {
                queue.Add(null);
                fail("should throw ArgumentNullException");
            }
            catch (ArgumentNullException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_Add_LObject_non_Comparable()
        {
            PriorityQueue<Object> queue = new PriorityQueue<Object>();
            queue.Add(new int?(10));
            try
            {
                queue.Add(new float?(1.3f));
                fail("should throw InvalidCastException");
            }
            catch (InvalidCastException e)
            {
                // expected
            }

            queue = new PriorityQueue<Object>();
            queue.Add(new int?(10));
            try
            {
                queue.Add(new Object());
                fail("should throw InvalidCastException");
            }
            catch (InvalidCastException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_Remove_LObject()
        {
            int[] array = { 2, 45, 7, -12, 9, 23, 17, 1118, 10, 16, 39 };
            List<int> list = Arrays.AsList(array);
            PriorityQueue<int> integerQueue = new PriorityQueue<int>(list);
            assertTrue(integerQueue.Remove(16));
            int[] newArray = { 2, 45, 7, -12, 9, 23, 17, 1118, 10, 39 };
            Array.Sort(newArray);
            for (int i = 0; i < newArray.Length; i++)
            {
                assertEquals(newArray[i], integerQueue.Poll());
            }
            assertEquals(0, integerQueue.size());
        }

        [Test, LuceneNetSpecific]
        public void Test_Remove_LObject_using_comparator()
        {
            PriorityQueue<String> queue = new PriorityQueue<String>(10,
                    new MockComparerStringByLength());
            String[] array = { "AAAAA", "AA", "AAAA", "AAAAAAAA" };
            for (int i = 0; i < array.Length; i++)
            {
                queue.Offer(array[i]);
            }
            assertFalse(queue.Contains("BB"));
            assertTrue(queue.Remove("AA"));
        }

        [Test, LuceneNetSpecific] // NOT Passing
        public void Test_Remove_LObject_not_exists()
        {
            object[] array = { 2, 45, 7, -12, 9, 23, 17, 1118, 10, 16, 39 };
            List<object> list = Arrays.AsList(array);
            PriorityQueue<object> integerQueue = new PriorityQueue<object>(list);
            assertFalse(integerQueue.Remove(111));
            assertFalse(integerQueue.Remove(null));
            assertFalse(integerQueue.Remove(""));
        }

        [Test, LuceneNetSpecific] // NOT Passing
        public void Test_Remove_LObject_null()
        {
            int?[] array = { 2, 45, 7, -12, 9, 23, 17, 1118, 10, 16, 39 };
            List<int?> list = Arrays.AsList(array);
            PriorityQueue<int?> integerQueue = new PriorityQueue<int?>(list);
            assertFalse(integerQueue.Remove(null));
        }

        [Test, LuceneNetSpecific] // NOT Passing
        public void Test_Remove_LObject_not_Compatible()
        {
            object[] array = { 2, 45, 7, -12, 9, 23, 17, 1118, 10, 16, 39 };
            List<object> list = Arrays.AsList(array);
            PriorityQueue<object> integerQueue = new PriorityQueue<object>(list);
            assertFalse(integerQueue.Remove(new float?(1.3F)));

            // although argument element type is not compatible with those in queue,
            // but comparator supports it.
            MockComparer<Object> comparator = new MockComparer<Object>();
            PriorityQueue<object> integerQueue1 = new PriorityQueue<object>(100,
                    comparator);
            integerQueue1.Offer(1);
            assertFalse(integerQueue1.Remove(new float?(1.3F)));

            PriorityQueue<Object> queue = new PriorityQueue<Object>();
            Object o = new Object();
            queue.Offer(o);
            assertTrue(queue.Remove(o));
        }

        [Test, LuceneNetSpecific]
        public void Test_Comparer()
        {
            PriorityQueue<Object> queue = new PriorityQueue<Object>();
            assertNull(queue.Comparer);

            MockComparer<Object> comparator = new MockComparer<Object>();
            queue = new PriorityQueue<Object>(100, comparator);
            assertEquals(comparator, queue.Comparer);
        }

#if FEATURE_SERIALIZABLE
        [Test, LuceneNetSpecific]
        public void Test_Serialization()
        {
            int[] array = { 2, 45, 7, -12, 9, 23, 17, 1118, 10, 16, 39 };
            List<int> list = Arrays.AsList(array);
            PriorityQueue<int> srcIntegerQueue = new PriorityQueue<int>(
                list);
            //PriorityQueue<int> destIntegerQueue = (PriorityQueue<int>)SerializationTester
            //        .getDeserilizedObject(srcIntegerQueue);
            PriorityQueue<int> destIntegerQueue = GetDeserializedObject(srcIntegerQueue);
            Array.Sort(array);
            for (int i = 0; i < array.Length; i++)
            {
                assertEquals(array[i], destIntegerQueue.Poll());
            }
            assertEquals(0, destIntegerQueue.size());
        }

        // LUCENENET: This type of casting is not allowed in .NET
        //[Test, LuceneNetSpecific]
        //public void Test_Serialization_casting()
        //{
        //    int[] array = { 2, 45, 7, -12, 9, 23, 17, 1118, 10, 16, 39 };
        //    List<int> list = Arrays.AsList(array);
        //    PriorityQueue<int> srcIntegerQueue = new PriorityQueue<int>(
        //        list);
        //    PriorityQueue<String> destStringQueue = (PriorityQueue<String>)GetDeserializedObject<object>(srcIntegerQueue);
        //    // will not incur class cast exception.
        //    Object o = destStringQueue.Peek();
        //    Array.Sort(array);
        //    int I = (int)o;
        //    assertEquals(array[0], I);
        //}
#endif

        private T GetDeserializedObject<T>(T objectToSerialize)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, objectToSerialize);
                ms.Seek(0, SeekOrigin.Begin);
                return (T)bf.Deserialize(ms);
            }
        }

        private class MockComparer<E> : IComparer<E>
        {
            public int Compare(E object1, E object2)
            {
                int hashcode1 = object1.GetHashCode();
                int hashcode2 = object2.GetHashCode();
                if (hashcode1 > hashcode2)
                {
                    return 1;
                }
                else if (hashcode1 == hashcode2)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
        }

        private class MockComparerStringByLength : IComparer<string>
        {
            public int Compare(string object1, string object2)
            {
                int length1 = object1.Length;
                int length2 = object2.Length;
                if (length1 > length2)
                {
                    return 1;
                }
                else if (length1 == length2)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
        }

        private class MockComparerCast<E> : IComparer<E>
        {
            public int Compare(E object1, E object2)
            {
                return 0;
            }
        }
    }
}
