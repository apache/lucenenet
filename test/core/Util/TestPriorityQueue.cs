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
        private class IntegerQueue : PriorityQueue<int?>
        {
            public IntegerQueue(int count)
                : base(count)
            {
            }

            public override bool LessThan(int? a, int? b)
            {
                return (a < b);
            }
        }

        [Test]
        public virtual void TestPQ()
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
                int? next = pq.Pop();
                Assert.IsTrue((int)next >= last);
                last = (int)next;
                sum2 += last;
            }

            Assert.AreEqual(sum, sum2);
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
            Assert.AreEqual(3, pq.Size());
            pq.Clear();
            Assert.AreEqual(0, pq.Size());
        }

        [Test]
        public virtual void TestFixedSize()
        {
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
        public virtual void TestInsertWithOverflow()
        {
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
        }
    }
}