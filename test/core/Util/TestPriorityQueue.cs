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

	public class TestPriorityQueue : LuceneTestCase
	{

		private class IntegerQueue : PriorityQueue<int?>
		{
			public IntegerQueue(int count) : base(count)
			{
			}

			protected internal override bool LessThan(int? a, int? b)
			{
				return (a < b);
			}
		}

		public virtual void TestPQ()
		{
			TestPQ(atLeast(10000), random());
		}

		public static void TestPQ(int count, Random gen)
		{
			PriorityQueue<int?> pq = new IntegerQueue(count);
			int sum = 0, sum2 = 0;

			for (int i = 0; i < count; i++)
			{
				int next = gen.Next();
				sum += next;
				pq.add(next);
			}

			//      Date end = new Date();

			//      System.out.print(((float)(end.getTime()-start.getTime()) / count) * 1000);
			//      System.out.println(" microseconds/put");

			//      start = new Date();

			int last = int.MinValue;
			for (int i = 0; i < count; i++)
			{
				int? next = pq.pop();
				Assert.IsTrue((int)next >= last);
				last = (int)next;
				sum2 += last;
			}

			Assert.AreEqual(sum, sum2);
			//      end = new Date();

			//      System.out.print(((float)(end.getTime()-start.getTime()) / count) * 1000);
			//      System.out.println(" microseconds/pop");
		}

		public virtual void TestClear()
		{
			PriorityQueue<int?> pq = new IntegerQueue(3);
			pq.add(2);
			pq.add(3);
			pq.add(1);
			Assert.AreEqual(3, pq.size());
			pq.clear();
			Assert.AreEqual(0, pq.size());
		}

		public virtual void TestFixedSize()
		{
			PriorityQueue<int?> pq = new IntegerQueue(3);
			pq.insertWithOverflow(2);
			pq.insertWithOverflow(3);
			pq.insertWithOverflow(1);
			pq.insertWithOverflow(5);
			pq.insertWithOverflow(7);
			pq.insertWithOverflow(1);
			Assert.AreEqual(3, pq.size());
			Assert.AreEqual((int?) 3, pq.top());
		}

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

		  assertNull(pq.insertWithOverflow(i1));
		  assertNull(pq.insertWithOverflow(i2));
		  assertNull(pq.insertWithOverflow(i3));
		  assertNull(pq.insertWithOverflow(i4));
		  Assert.IsTrue(pq.insertWithOverflow(i5) == i3); // i3 should have been dropped
		  Assert.IsTrue(pq.insertWithOverflow(i6) == i6); // i6 should not have been inserted
		  Assert.AreEqual(size, pq.size());
		  Assert.AreEqual((int?) 2, pq.top());
		}

	}

}