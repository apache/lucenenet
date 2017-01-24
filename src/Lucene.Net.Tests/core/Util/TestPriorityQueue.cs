using Lucene.Net.Attributes;
using NUnit.Framework;
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

    [TestFixture]
    public class TestPriorityQueue : LuceneTestCase
    {
        private static readonly int MAX_PQ_SIZE = ArrayUtil.MAX_ARRAY_LENGTH - 1;

        private class IntegerQueue : PriorityQueue<int?>
        {
            public IntegerQueue(int count)
                : base(count)
            {
            }

            public IntegerQueue(int count, bool prepopulate)
                : base(count, prepopulate)
            {
            }

            protected internal override bool LessThan(int? a, int? b)
            {
                return (a <= b);
            }
        }

        public void TestPQ()
        {
            TestPQ(AtLeast(10000), Random());
        }

        public static void TestPQ(int count, Random gen)
        {
            PriorityQueue<int?> pq = new IntegerQueue(count);
            int sum = 0, sum2 = 0;

            for (int i = 0; i < count; i++)
            {
                int next = gen.Next();
                sum += next;
                pq.Add(next);
            }

            //      Date end = new Date();

            //      System.out.print(((float)(end.getTime()-start.getTime()) / count) * 1000);
            //      System.out.println(" microseconds/put");

            //      start = new Date();

            int last = int.MinValue;
            for (int i = 0; i < count; i++)
            {
                var next = pq.Pop();
                assertTrue(next.Value >= last);
                last = next.Value;
                sum2 += last;
            }

            assertEquals(sum, sum2);
            //      end = new Date();

            //      System.out.print(((float)(end.getTime()-start.getTime()) / count) * 1000);
            //      System.out.println(" microseconds/pop");
        }

        [Test]
        public virtual void TestClear()
        {
            PriorityQueue<int?> pq = new IntegerQueue(3);
            pq.Add(2);
            pq.Add(3);
            pq.Add(1);
            Assert.AreEqual(3, pq.Count);
            pq.Clear();
            Assert.AreEqual(0, pq.Count);
        }

        [Test]
        public void TestFixedSize()
        {
            PriorityQueue<int?> pq = new IntegerQueue(3);
            pq.InsertWithOverflow(2);
            pq.InsertWithOverflow(3);
            pq.InsertWithOverflow(1);
            pq.InsertWithOverflow(5);
            pq.InsertWithOverflow(7);
            pq.InsertWithOverflow(1);
            assertEquals(3, pq.Count);
            assertEquals((int?)3, pq.Top);
        }

        [Test]
        public virtual void TestInsertWithOverflow()
        {
            // Tests that InsertWithOverflow discards the correct value,
            // and the resulting PQ preserves its structure

            int size = 4;
            PriorityQueue<int?> pq = new IntegerQueue(size);
            int? i1 = 2;
            int? i2 = 3;
            int? i3 = 1;
            int? i4 = 5;
            int? i5 = 7;
            int? i6 = 1;

            Assert.IsNull(pq.InsertWithOverflow(i1));
            Assert.IsNull(pq.InsertWithOverflow(i2));
            Assert.IsNull(pq.InsertWithOverflow(i3));
            Assert.IsNull(pq.InsertWithOverflow(i4));
            Assert.IsTrue(pq.InsertWithOverflow(i5) == i3); // i3 should have been dropped
            Assert.IsTrue(pq.InsertWithOverflow(i6) == i6); // i6 should not have been inserted
            Assert.AreEqual(size, pq.Count);
            Assert.AreEqual((int?)2, pq.Top);

            // LUCENENET SPECIFIC
            pq.Pop();
            Assert.AreEqual((int?)3, pq.Top);
            pq.Pop();
            Assert.AreEqual((int?)5, pq.Top);
            pq.Pop();
            Assert.AreEqual((int?)7, pq.Top);
        }


        #region LUCENENET SPECIFIC TESTS

        private class IntegerQueueWithSentinel : IntegerQueue
        {
            public IntegerQueueWithSentinel(int count, bool prepopulate)
                : base(count, prepopulate)
            {
            }

            protected override int? GetSentinelObject()
            {
                return int.MaxValue;
            }
        }

        private class MyType
        {
            public MyType(int field)
            {
                Field = field;
            }
            public int Field { get; set; }
        }

        private class MyQueue : PriorityQueue<MyType>
        {
            public MyQueue(int count)
                : base(count)
            {
            }

            protected internal override bool LessThan(MyType a, MyType b)
            {
                return (a.Field < b.Field);
            }
        }

        new private class Less : IComparer<int?>
        {
            public int Compare(int? a, int? b)
            {
                Assert.IsNotNull(a);
                Assert.IsNotNull(b);
                return (int) (a - b);
            }
        }

        new private class Greater : IComparer<int?>
        {
            public int Compare(int? a, int? b)
            {
                Assert.IsNotNull(a);
                Assert.IsNotNull(b);
                return (int) (a - b);
            }
        } 

        [Ignore("Increase heap size to run this test")]
        [Test, LuceneNetSpecific]
        public static void TestMaxSizeBounds()
        {
            // Minimum size is 0
            int maxSize = 0;
            PriorityQueue<int?> pq = new IntegerQueue(maxSize);

            // Maximum size is ArrayUtil.MAX_ARRAY_LENGTH - 1
            maxSize = MAX_PQ_SIZE;
            pq = new IntegerQueue(maxSize);

            // A valid maximum size
            maxSize = 12345;
            pq = new IntegerQueue(maxSize);
            
            // Cannot construct a negative size heap
            maxSize = -3;
            try
            {
                pq = new IntegerQueue(maxSize);
                // Should had thrown an exception
                Assert.Fail();
            }
            catch (ArgumentException)
            {
            }

            maxSize = MAX_PQ_SIZE;
            try
            {
                pq = new IntegerQueue(maxSize);
            }
            catch (ArgumentException)
            {
            }
        }

        [Test, LuceneNetSpecific]
        public static void TestPrepopulation()
        {
            int maxSize = 10;
            // Populates the internal array
            PriorityQueue<int?> pq = new IntegerQueueWithSentinel(maxSize, true);
            Assert.AreEqual(pq.Top, int.MaxValue);
            Assert.AreEqual(pq.Count, 10);

            // Does not populate it
            pq = new IntegerQueue(maxSize, false);
            Assert.AreEqual(pq.Top, default(int?));
            Assert.AreEqual(pq.Count, 0);
        }

        private static void AddAndTest<T>(PriorityQueue<T> pq, T element, T expectedTop, int expectedSize)
        {
            pq.Add(element);
            Assert.AreEqual(pq.Top, expectedTop);
            Assert.AreEqual(pq.Count, expectedSize);
        }

        [Test, LuceneNetSpecific]
        public static void TestAdd()
        {
            int maxSize = 10;
            PriorityQueue<int?> pq = new IntegerQueue(maxSize);
            
            // Add mixed elements
            AddAndTest(pq, 5, 5, 1);
            AddAndTest(pq, 1, 1, 2);
            AddAndTest(pq, 3, 1, 3);
            AddAndTest(pq, -1, -1, 4);
            AddAndTest(pq, -111111, -111111, 5);
            
            // Add a sorted list of elements
            pq = new IntegerQueue(maxSize);
            AddAndTest(pq, -111111, -111111, 1);
            AddAndTest(pq, -1, -111111, 2);
            AddAndTest(pq, 1, -111111, 3);
            AddAndTest(pq, 3, -111111, 4);
            AddAndTest(pq, 5, -111111, 5);

            // Add a reversed sorted list of elements
            pq = new IntegerQueue(maxSize);
            AddAndTest(pq, 5, 5, 1);
            AddAndTest(pq, 3, 3, 2);
            AddAndTest(pq, 1, 1, 3);
            AddAndTest(pq, -1, -1, 4);
            AddAndTest(pq, -111111, -111111, 5);
        }

        [Test, LuceneNetSpecific]
        public static void TestDuplicates()
        {
            // Tests that the queue doesn't absorb elements with duplicate keys
            int maxSize = 10;
            PriorityQueue<int?> pq = new IntegerQueue(maxSize);

            pq.Add(3);
            pq.Add(3);
            Assert.AreEqual(pq.Count, 2);

            pq.Add(3);
            Assert.AreEqual(pq.Count, 3);

            pq.Add(17);
            pq.Add(17);
            pq.Add(17);
            pq.Add(17);
            Assert.AreEqual(pq.Count, 7);
        }

        [Test, LuceneNetSpecific]
        public static void TestPop()
        {
            int maxSize = 10;
            PriorityQueue<int?> pq = new IntegerQueue(maxSize);

            // Add one element and pop it
            pq.Add(7);
            pq.Pop();
            Assert.AreEqual(pq.Count, 0);
            
            // Add a bunch of elements, pop them all
            pq.Add(1);
            pq.Add(20);
            pq.Add(1);
            pq.Add(15);
            pq.Add(4);
            pq.Add(12);
            pq.Add(1000);
            pq.Add(-3);
            pq.Pop();
            Assert.AreEqual(pq.Count, 7);
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            Assert.AreEqual(pq.Count, 0);

            // Interleaved adds and pops
            pq.Add(1);
            pq.Add(20);
            pq.Pop();
            Assert.AreEqual(pq.Count, 1);
            pq.Add(1);
            pq.Add(15);
            pq.Add(4);
            pq.Pop();
            pq.Pop();
            Assert.AreEqual(pq.Count, 2);
            pq.Add(12);
            pq.Add(1000);
            pq.Add(-3);
            pq.Pop();
            pq.Pop();
            Assert.AreEqual(pq.Count, 3);

            // Pop an empty PQ
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
        }

        [Test, LuceneNetSpecific]
        public static void TestUpdateTop()
        {
            // Mostly to reflect the usage of UpdateTop
            int maxSize = 10;
            PriorityQueue<MyType> pq = new MyQueue(maxSize);

            pq.Add(new MyType(1));
            pq.Add(new MyType(20));
            pq.Add(new MyType(1));
            pq.Add(new MyType(15));
            pq.Add(new MyType(4));
            pq.Add(new MyType(12));
            pq.Add(new MyType(1000));
            pq.Add(new MyType(-300));

            Assert.AreEqual(pq.Top.Field, -300);
            MyType topElement = pq.Top;
            topElement.Field = 25;  // Now this should no longer be at the top of the queue
            pq.UpdateTop();         // Hence we need to update the top queue
            Assert.AreEqual(pq.Top.Field, 1);

            // The less eficient way to do this is the following
            topElement = pq.Pop();
            topElement.Field = 678;
            pq.Add(topElement);
            Assert.AreEqual(pq.Top.Field, 1);
        }

        [Test, LuceneNetSpecific]
        public static void TestOverflow()
        {
            // Tests adding elements to full queues
            // Add's documentation claims throwing an IndexOutOfRangeException in this situation
            
            // Add an element to a prepopulated queue
            int maxSize = 10;
            PriorityQueue<int?> pq = new IntegerQueueWithSentinel(maxSize, true);

            try
            {
                pq.Add(3);
                Assert.Fail();
            }
            catch (IndexOutOfRangeException)
            {
            }

            // Populate manually
            maxSize = 5;
            pq = new IntegerQueue(maxSize);
            pq.Add(1);
            pq.Add(4);
            pq.Add(-1);
            pq.Add(0);
            pq.Add(10);

            try
            {
                pq.Add(666);
            }
            catch (IndexOutOfRangeException)
            {
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestInsertWithOverflowDoesNotOverflow()
        {
            // Tests that InsertWithOverflow does not cause overflow

            PriorityQueue<int?> pq = new IntegerQueue(3);
            pq.InsertWithOverflow(2);
            pq.InsertWithOverflow(3);
            pq.InsertWithOverflow(1);
            pq.InsertWithOverflow(5);
            pq.InsertWithOverflow(7);
            pq.InsertWithOverflow(1);
            Assert.AreEqual(3, pq.Count);
            Assert.AreEqual((int?)3, pq.Top);
        }

        private static void AddElements<T>(PriorityQueue<T> pq, T[] elements)
        {
            int size = (int)elements.size();

            for (int i = 0; i < size; i++)
            {
                pq.Add(elements[i]);
            }
        }

        private static void PopElements<T>(PriorityQueue<T> pq)
        {
            int size = pq.Count;

            for (int i = 0; i < size; i++)
            {
                pq.Pop();
            }
        }

        private static void PopAndTestElements<T>(PriorityQueue<T> pq, T[] elements)
        {
            int size = pq.Count;

            for (int i = 0; i < size; i++)
            {
                Assert.AreEqual(pq.Pop(), elements[i]);
            }
        }

        private static void PopAndTestElements<T>(PriorityQueue<T> pq)
        {
            int size = pq.Count;
            T last = pq.Pop();

            for (int i = 1; i < size; i++)
            {
                T next = pq.Pop();
                Assert.IsTrue(pq.LessThan(last, next));
                last = next;
            }
        }

        private static void TimedAddAndPop<T>(PriorityQueue<T> pq, T[] elements)
        {
            int size = (int)elements.size();
            DateTime start, end;
            TimeSpan total;

            start = DateTime.Now;

            AddElements(pq, elements);

            end = DateTime.Now;
            total = end - start;

            System.Console.WriteLine("Total adding time: {0} ticks or {1}ms", total.Ticks, total.Milliseconds);
            System.Console.WriteLine("Average time per add: {0} ticks", total.Ticks / size);

            start = DateTime.Now;

            PopElements(pq);

            end = DateTime.Now;
            total = end - start;

            System.Console.WriteLine("Total popping time: {0} ticks or {1}ms", total.Ticks, total.Milliseconds);
            System.Console.WriteLine("Average time per pop: {0} ticks", total.Ticks / size);
        }

        [Test, LuceneNetSpecific]
        public static void TestPersistence()
        {
            // Tests that a big number of elements are added and popped (in the correct order)
            // without losing any information

            int maxSize = AtLeast(100000);
            PriorityQueue<int?> pq = new IntegerQueue(maxSize);

            int?[] elements = new int?[maxSize];
            for (int i = 0; i < maxSize; i++)
            {
                elements[i] = Random().Next();
            }

            AddElements(pq, elements);

            ArrayUtil.IntroSort(elements, new Less());

            PopAndTestElements(pq, elements);
        }

        [Test, LuceneNetSpecific]
        public static void TestStress()
        {
            int atLeast = 1000000;
            int maxSize = AtLeast(atLeast);
            PriorityQueue<int?> pq = new IntegerQueue(maxSize);

            // Add a lot of elements
            for (int i = 0; i < maxSize; i++)
            {
                pq.Add(Random().Next());
            }

            // Pop some of them
            while (pq.Count > atLeast/2)
            {
                pq.Pop();
            }

            // Add some more
            while (pq.Count < (atLeast*3)/4)
            {
                pq.Add(Random().Next());
            }

            PopAndTestElements(pq);

            Assert.AreEqual(pq.Count, 0);

            // We fill it again
            for (int i = 0; 2 * i < maxSize; i++)
            {
                pq.Add(Random().Next());
            }

            Assert.AreEqual(pq.Count, (maxSize + 1) / 2);
            pq.Clear();
            Assert.AreEqual(pq.Count, 0);

            // One last time
            for (int i = 0; 2 * i < maxSize; i++)
            {
                pq.Add(Random().Next());
            }

            PopAndTestElements(pq);
            Assert.AreEqual(pq.Count, 0);
        }

        [Test, Explicit, LuceneNetSpecific]
        public static void Benchmarks()
        {
            AssumeTrue("Turn VERBOSE on or otherwise you won't see the results.", VERBOSE);
               
            int maxSize = AtLeast(100000);
            PriorityQueue<int?> pq = new IntegerQueue(maxSize);
            int?[] elements = new int?[maxSize];

            for (int i = 0; i < maxSize; i++)
            {
                elements[i] = Random().Next();
            }

            System.Console.WriteLine("Random list of elements...");

            TimedAddAndPop<int?>(pq, elements);
            pq.Clear();

            System.Console.WriteLine("\nSorted list of elements...");

            pq = new IntegerQueue(maxSize);
            ArrayUtil.IntroSort(elements, new Less());
            TimedAddAndPop<int?>(pq, elements);
            pq.Clear();

            System.Console.WriteLine("\nReverse sorted list of elements...");

            pq = new IntegerQueue(maxSize);
            ArrayUtil.IntroSort(elements, new Greater());
            TimedAddAndPop<int?>(pq, elements);
            pq.Clear();
        }

        #endregion
    }
}