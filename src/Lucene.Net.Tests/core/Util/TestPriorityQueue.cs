using NUnit.Framework;
using System;

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

            public override bool LessThan(int? a, int? b)
            {
                return (a < b);
            }
        }

        private class IntegerQueueWithSentinel : IntegerQueue
        {
            public IntegerQueueWithSentinel(int count, bool prepopulate)
                : base(count, prepopulate)
            {
            }

            protected override int? SentinelObject
            {
                get
                {
                    return int.MaxValue;
                }
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

            public override bool LessThan(MyType a, MyType b)
            {
                return (a.Field < b.Field);
            }
        }

        [Ignore] // Increase heap size to run this test
        [Test]
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

        [Test]
        public static void TestPrepopulation()
        {
            int maxSize = 10;
            // Populates the internal array
            PriorityQueue<int?> pq = new IntegerQueueWithSentinel(maxSize, true);
            Assert.AreEqual(pq.Top(), int.MaxValue);
            Assert.AreEqual(pq.Size(), 10);

            // Does not populate it
            pq = new IntegerQueue(maxSize, false);
            Assert.AreEqual(pq.Top(), default(int?));
            Assert.AreEqual(pq.Size(), 0);
        }

        [Test]
        public static void TestAdd()
        {
            int maxSize = 10;
            PriorityQueue<int?> pq = new IntegerQueue(maxSize);
            
            // Add mixed elements
            pq.Add(5);
            Assert.AreEqual(pq.Top(), 5);
            Assert.AreEqual(pq.Size(), 1);
            pq.Add(1);
            Assert.AreEqual(pq.Top(), 1);
            Assert.AreEqual(pq.Size(), 2);
            pq.Add(3);
            Assert.AreEqual(pq.Top(), 1);
            Assert.AreEqual(pq.Size(), 3);
            pq.Add(-1);
            Assert.AreEqual(pq.Top(), -1);
            Assert.AreEqual(pq.Size(), 4);
            pq.Add(-111111);
            Assert.AreEqual(pq.Top(), -111111);
            Assert.AreEqual(pq.Size(), 5);

            // Add a sorted list of elements
            pq = new IntegerQueue(maxSize);
            pq.Add(-111111);
            Assert.AreEqual(pq.Top(), -111111);
            Assert.AreEqual(pq.Size(), 1);
            pq.Add(-1);
            Assert.AreEqual(pq.Top(), -111111);
            Assert.AreEqual(pq.Size(), 2);
            pq.Add(1);
            Assert.AreEqual(pq.Top(), -111111);
            Assert.AreEqual(pq.Size(), 3);
            pq.Add(3);
            Assert.AreEqual(pq.Top(), -111111);
            Assert.AreEqual(pq.Size(), 4);
            pq.Add(5);
            Assert.AreEqual(pq.Top(), -111111);
            Assert.AreEqual(pq.Size(), 5);

            // Add a reversed sorted list of elements
            pq = new IntegerQueue(maxSize);
            pq.Add(5);
            Assert.AreEqual(pq.Top(), 5);
            Assert.AreEqual(pq.Size(), 1);
            pq.Add(3);
            Assert.AreEqual(pq.Top(), 3);
            Assert.AreEqual(pq.Size(), 2);
            pq.Add(1);
            Assert.AreEqual(pq.Top(), 1);
            Assert.AreEqual(pq.Size(), 3);
            pq.Add(-1);
            Assert.AreEqual(pq.Top(), -1);
            Assert.AreEqual(pq.Size(), 4);
            pq.Add(-111111);
            Assert.AreEqual(pq.Top(), -111111);
            Assert.AreEqual(pq.Size(), 5);
        }

        [Test]
        public static void TestDuplicates()
        {
            // Tests that the queue doesn't absorb elements with duplicate keys
            int maxSize = 10;
            PriorityQueue<int?> pq = new IntegerQueue(maxSize);

            pq.Add(3);
            pq.Add(3);
            Assert.AreEqual(pq.Size(), 2);

            pq.Add(3);
            Assert.AreEqual(pq.Size(), 3);

            pq.Add(17);
            pq.Add(17);
            pq.Add(17);
            pq.Add(17);
            Assert.AreEqual(pq.Size(), 7);
        }

        [Test]
        public static void TestPop()
        {
            int maxSize = 10;
            PriorityQueue<int?> pq = new IntegerQueue(maxSize);

            // Add one element and pop it
            pq.Add(7);
            pq.Pop();
            Assert.AreEqual(pq.Size(), 0);
            
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
            Assert.AreEqual(pq.Size(), 7);
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            Assert.AreEqual(pq.Size(), 0);

            // Interleaved adds and pops
            pq.Add(1);
            pq.Add(20);
            pq.Pop();
            Assert.AreEqual(pq.Size(), 1);
            pq.Add(1);
            pq.Add(15);
            pq.Add(4);
            pq.Pop();
            pq.Pop();
            Assert.AreEqual(pq.Size(), 2);
            pq.Add(12);
            pq.Add(1000);
            pq.Add(-3);
            pq.Pop();
            pq.Pop();
            Assert.AreEqual(pq.Size(), 3);

            // Pop an empty PQ
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
            pq.Pop();
        }

        [Test]
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

            Assert.AreEqual(pq.Top().Field, -300);
            MyType topElement = pq.Top();
            topElement.Field = 25;  // Now this should no longer be at the top of the queue
            pq.UpdateTop();         // Hence we need to update the top queue
            Assert.AreEqual(pq.Top().Field, 1);

            // The less eficient way to do this is the following
            topElement = pq.Pop();
            topElement.Field = 678;
            pq.Add(topElement);
            Assert.AreEqual(pq.Top().Field, 1);
        }

        [Test]
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

        [Test]
        public virtual void TestClear()
        {
            PriorityQueue<int?> pq = new IntegerQueue(3);
            pq.Add(2);
            pq.Add(3);
            pq.Add(1);
            Assert.AreEqual(3, pq.Size());
            pq.Clear();
            Assert.AreEqual(0, pq.Size());
        }

        [Test]
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
            Assert.AreEqual(3, pq.Size());
            Assert.AreEqual((int?)3, pq.Top());
        }

        [Test]
        public virtual void TestInsertWithOverflowDiscardsRight()
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
            Assert.AreEqual(size, pq.Size());
            Assert.AreEqual((int?)2, pq.Top());

            pq.Pop();
            Assert.AreEqual((int?)3, pq.Top());
            pq.Pop();
            Assert.AreEqual((int?)5, pq.Top());
            pq.Pop();
            Assert.AreEqual((int?)7, pq.Top());
        }

        [Test]
        public static void TestStress()
        {
            int maxSize = AtLeast(100000);

            PriorityQueue<int?> pq = new IntegerQueue(maxSize);
            int sum = 0, sum2 = 0;

            DateTime start, end;
            TimeSpan total;
            start = DateTime.Now;

            // Add a lot of elements
            for (int i = 0; i < maxSize; i++)
            {
                int next = Random().Next();
                sum += next;
                pq.Add(next);
            }

            end = DateTime.Now;
            total = end - start;
            // Note that this measurement considers the random number generation
            System.Console.WriteLine("Total adding time: {0} ticks or {1}ms", total.Ticks, total.Milliseconds);
            System.Console.WriteLine("Time per add: {0} ticks", total.Ticks / maxSize);

            // Pop them and check that the elements are taken in sorted order
            start = DateTime.Now;
            int last = int.MinValue;
            for (int i = 0; i < maxSize; i++)
            {
                int? next = pq.Pop();
                Assert.IsTrue((int)next >= last);
                last = (int)next;
                sum2 += last;
            }

            end = DateTime.Now;
            total = end - start;
            // Note that this measurement considers the random number generation
            System.Console.WriteLine("Total poping time: {0} ticks or {1}ms", total.Ticks, total.Milliseconds);
            System.Console.WriteLine("Time per pop: {0} ticks", total.Ticks / maxSize);

            // Loose checking that we didn't lose data in the process
            Assert.AreEqual(sum, sum2);
        }
    }
}