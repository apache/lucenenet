namespace Lucene.Net.Codecs.Lucene40
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

	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
    using NUnit.Framework;

	/// <summary>
	/// <code>TestBitVector</code> tests the <code>BitVector</code>, obviously.
	/// </summary>
	public class TestBitVector : LuceneTestCase
	{

		/// <summary>
		/// Test the default constructor on BitVectors of various sizes.
		/// </summary>
		public virtual void TestConstructSize()
		{
			DoTestConstructOfSize(8);
			DoTestConstructOfSize(20);
			DoTestConstructOfSize(100);
			DoTestConstructOfSize(1000);
		}

		private void DoTestConstructOfSize(int n)
		{
			BitVector bv = new BitVector(n);
			Assert.AreEqual(n,bv.Size());
		}

		/// <summary>
		/// Test the get() and set() methods on BitVectors of various sizes.
		/// </summary>
		public virtual void TestGetSet()
		{
			DoTestGetSetVectorOfSize(8);
			DoTestGetSetVectorOfSize(20);
			DoTestGetSetVectorOfSize(100);
			DoTestGetSetVectorOfSize(1000);
		}

		private void DoTestGetSetVectorOfSize(int n)
		{
			BitVector bv = new BitVector(n);
			for (int i = 0;i < bv.Size();i++)
			{
				// ensure a set bit can be git'
				Assert.IsFalse(bv.get(i));
				bv.Set(i);
				Assert.IsTrue(bv.get(i));
			}
		}

		/// <summary>
		/// Test the clear() method on BitVectors of various sizes.
		/// </summary>
		public virtual void TestClear()
		{
			DoTestClearVectorOfSize(8);
			DoTestClearVectorOfSize(20);
			DoTestClearVectorOfSize(100);
			DoTestClearVectorOfSize(1000);
		}

		private void DoTestClearVectorOfSize(int n)
		{
			BitVector bv = new BitVector(n);
			for (int i = 0;i < bv.Size();i++)
			{
				// ensure a set bit is cleared
				Assert.IsFalse(bv.get(i));
				bv.set(i);
				Assert.IsTrue(bv.get(i));
				bv.clear(i);
				Assert.IsFalse(bv.get(i));
			}
		}

		/// <summary>
		/// Test the count() method on BitVectors of various sizes.
		/// </summary>
		public virtual void TestCount()
		{
			DoTestCountVectorOfSize(8);
			DoTestCountVectorOfSize(20);
			DoTestCountVectorOfSize(100);
			DoTestCountVectorOfSize(1000);
		}

		private void DoTestCountVectorOfSize(int n)
		{
			BitVector bv = new BitVector(n);
			// test count when incrementally setting bits
			for (int i = 0;i < bv.Size();i++)
			{
				Assert.IsFalse(bv.get(i));
				Assert.AreEqual(i,bv.count());
				bv.set(i);
				Assert.IsTrue(bv.get(i));
				Assert.AreEqual(i + 1,bv.count());
			}

			bv = new BitVector(n);
			// test count when setting then clearing bits
			for (int i = 0;i < bv.Size();i++)
			{
				Assert.IsFalse(bv.get(i));
				Assert.AreEqual(0,bv.count());
				bv.set(i);
				Assert.IsTrue(bv.get(i));
				Assert.AreEqual(1,bv.count());
				bv.clear(i);
				Assert.IsFalse(bv.get(i));
				Assert.AreEqual(0,bv.count());
			}
		}

		/// <summary>
		/// Test writing and construction to/from Directory.
		/// </summary>
		public virtual void TestWriteRead()
		{
			DoTestWriteRead(8);
			DoTestWriteRead(20);
			DoTestWriteRead(100);
			DoTestWriteRead(1000);
		}

		private void DoTestWriteRead(int n)
		{
			MockDirectoryWrapper d = new MockDirectoryWrapper(Random(), new RAMDirectory());
			d.PreventDoubleWrite = false;
			BitVector bv = new BitVector(n);
			// test count when incrementally setting bits
			for (int i = 0;i < bv.Size();i++)
			{
				Assert.IsFalse(bv.get(i));
				Assert.AreEqual(i,bv.count());
				bv.set(i);
				Assert.IsTrue(bv.get(i));
				Assert.AreEqual(i + 1,bv.count());
				bv.write(d, "TESTBV", newIOContext(Random()));
				BitVector compare = new BitVector(d, "TESTBV", newIOContext(Random()));
				// compare bit vectors with bits set incrementally
				Assert.IsTrue(DoCompare(bv,compare));
			}
		}

		/// <summary>
		/// Test r/w when size/count cause switching between bit-set and d-gaps file formats.  
		/// </summary>
		public virtual void TestDgaps()
		{
		  DoTestDgaps(1,0,1);
		  DoTestDgaps(10,0,1);
		  DoTestDgaps(100,0,1);
		  DoTestDgaps(1000,4,7);
		  DoTestDgaps(10000,40,43);
		  DoTestDgaps(100000,415,418);
		  DoTestDgaps(1000000,3123,3126);
		  // now exercise skipping of fully populated byte in the bitset (they are omitted if bitset is sparse)
		  MockDirectoryWrapper d = new MockDirectoryWrapper(Random(), new RAMDirectory());
		  d.PreventDoubleWrite = false;
		  BitVector bv = new BitVector(10000);
		  bv.set(0);
		  for (int i = 8; i < 16; i++)
		  {
			bv.set(i);
		  } // make sure we have once byte full of set bits
		  for (int i = 32; i < 40; i++)
		  {
			bv.set(i);
		  } // get a second byte full of set bits
		  // add some more bits here 
		  for (int i = 40; i < 10000; i++)
		  {
			if (Random().Next(1000) == 0)
			{
			  bv.set(i);
			}
		  }
		  bv.write(d, "TESTBV", newIOContext(Random()));
		  BitVector compare = new BitVector(d, "TESTBV", newIOContext(Random()));
		  Assert.IsTrue(DoCompare(bv,compare));
		}

		private void DoTestDgaps(int size, int count1, int count2)
		{
		  MockDirectoryWrapper d = new MockDirectoryWrapper(Random(), new RAMDirectory());
		  d.PreventDoubleWrite = false;
		  BitVector bv = new BitVector(size);
		  bv.invertAll();
		  for (int i = 0; i < count1; i++)
		  {
			bv.clear(i);
			Assert.AreEqual(i + 1,size - bv.count());
		  }
		  bv.write(d, "TESTBV", newIOContext(Random()));
		  // gradually increase number of set bits
		  for (int i = count1; i < count2; i++)
		  {
			BitVector bv2 = new BitVector(d, "TESTBV", newIOContext(Random()));
			Assert.IsTrue(DoCompare(bv,bv2));
			bv = bv2;
			bv.clear(i);
			Assert.AreEqual(i + 1, size - bv.count());
			bv.write(d, "TESTBV", newIOContext(Random()));
		  }
		  // now start decreasing number of set bits
		  for (int i = count2 - 1; i >= count1; i--)
		  {
			BitVector bv2 = new BitVector(d, "TESTBV", newIOContext(Random()));
			Assert.IsTrue(DoCompare(bv,bv2));
			bv = bv2;
			bv.set(i);
			Assert.AreEqual(i,size - bv.count());
			bv.write(d, "TESTBV", newIOContext(Random()));
		  }
		}

		public virtual void TestSparseWrite()
		{
		  Directory d = NewDirectory();
		  const int numBits = 10240;
		  BitVector bv = new BitVector(numBits);
		  bv.invertAll();
		  int numToClear = Random().Next(5);
		  for (int i = 0;i < numToClear;i++)
		  {
			bv.clear(Random().Next(numBits));
		  }
		  bv.write(d, "test", newIOContext(Random()));
		  long size = d.fileLength("test");
		  Assert.IsTrue("size=" + size, size < 100);
		  d.close();
		}

		public virtual void TestClearedBitNearEnd()
		{
		  Directory d = NewDirectory();
		  int numBits = TestUtil.Next(Random(), 7, 1000);
		  BitVector bv = new BitVector(numBits);
		  bv.invertAll();
		  bv.clear(numBits - TestUtil.Next(Random(), 1, 7));
		  bv.write(d, "test", newIOContext(Random()));
		  Assert.AreEqual(numBits - 1, bv.count());
		  d.close();
		}

		public virtual void TestMostlySet()
		{
		  Directory d = NewDirectory();
		  int numBits = TestUtil.Next(Random(), 30, 1000);
		  for (int numClear = 0;numClear < 20;numClear++)
		  {
			BitVector bv = new BitVector(numBits);
			bv.invertAll();
			int count = 0;
			while (count < numClear)
			{
			  int bit = Random().Next(numBits);
			  // Don't use getAndClear, so that count is recomputed
			  if (bv.get(bit))
			  {
				bv.clear(bit);
				count++;
				Assert.AreEqual(numBits - count, bv.count());
			  }
			}
		  }

		  d.close();
		}

		/// <summary>
		/// Compare two BitVectors.
		/// this should really be an equals method on the BitVector itself. </summary>
		/// <param name="bv"> One bit vector </param>
		/// <param name="compare"> The second to compare </param>
		private bool DoCompare(BitVector bv, BitVector compare)
		{
			bool equal = true;
			for (int i = 0;i < bv.Size();i++)
			{
				// bits must be equal
				if (bv.get(i) != compare.get(i))
				{
					equal = false;
					break;
				}
			}
			Assert.AreEqual(bv.count(), compare.count());
			return equal;
		}
	}

}