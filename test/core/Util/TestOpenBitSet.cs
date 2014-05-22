using System.Collections;

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


	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

	public class TestOpenBitSet : BaseDocIdSetTestCase<OpenBitSet>
	{

	  public override OpenBitSet CopyOf(BitArray bs, int length)
	  {
		OpenBitSet set = new OpenBitSet(length);
		for (int doc = bs.nextSetBit(0); doc != -1; doc = bs.nextSetBit(doc + 1))
		{
		  set.set(doc);
		}
		return set;
	  }

	  internal virtual void DoGet(BitArray a, OpenBitSet b)
	  {
		int max = a.Count;
		for (int i = 0; i < max; i++)
		{
		  if (a.Get(i) != b.get(i))
		  {
			Assert.Fail("mismatch: BitSet=[" + i + "]=" + a.Get(i));
		  }
		  if (a.Get(i) != b.get((long) i))
		  {
			Assert.Fail("mismatch: BitSet=[" + i + "]=" + a.Get(i));
		  }
		}
	  }

	  internal virtual void DoGetFast(BitArray a, OpenBitSet b, int max)
	  {
		for (int i = 0; i < max; i++)
		{
		  if (a.Get(i) != b.fastGet(i))
		  {
			Assert.Fail("mismatch: BitSet=[" + i + "]=" + a.Get(i));
		  }
		  if (a.Get(i) != b.fastGet((long) i))
		  {
			Assert.Fail("mismatch: BitSet=[" + i + "]=" + a.Get(i));
		  }
		}
	  }

	  internal virtual void DoNextSetBit(BitArray a, OpenBitSet b)
	  {
		int aa = -1, bb = -1;
		do
		{
		  aa = a.nextSetBit(aa + 1);
		  bb = b.nextSetBit(bb + 1);
		  Assert.AreEqual(aa,bb);
		} while (aa >= 0);
	  }

	  internal virtual void DoNextSetBitLong(BitArray a, OpenBitSet b)
	  {
		int aa = -1, bb = -1;
		do
		{
		  aa = a.nextSetBit(aa + 1);
		  bb = (int) b.nextSetBit((long)(bb + 1));
		  Assert.AreEqual(aa,bb);
		} while (aa >= 0);
	  }

	  internal virtual void DoPrevSetBit(BitArray a, OpenBitSet b)
	  {
		int aa = a.Count + random().Next(100);
		int bb = aa;
		do
		{
		  // aa = a.prevSetBit(aa-1);
		  aa--;
		  while ((aa >= 0) && (!a.Get(aa)))
		  {
			aa--;
		  }
		  bb = b.prevSetBit(bb - 1);
		  Assert.AreEqual(aa,bb);
		} while (aa >= 0);
	  }

	  internal virtual void DoPrevSetBitLong(BitArray a, OpenBitSet b)
	  {
		int aa = a.Count + random().Next(100);
		int bb = aa;
		do
		{
		  // aa = a.prevSetBit(aa-1);
		  aa--;
		  while ((aa >= 0) && (!a.Get(aa)))
		  {
			aa--;
		  }
		  bb = (int) b.prevSetBit((long)(bb - 1));
		  Assert.AreEqual(aa,bb);
		} while (aa >= 0);
	  }

	  // test interleaving different OpenBitSetIterator.next()/skipTo()
	  internal virtual void DoIterate(BitArray a, OpenBitSet b, int mode)
	  {
		if (mode == 1)
		{
			DoIterate1(a, b);
		}
		if (mode == 2)
		{
			DoIterate2(a, b);
		}
	  }

	  internal virtual void DoIterate1(BitArray a, OpenBitSet b)
	  {
		int aa = -1, bb = -1;
		OpenBitSetIterator iterator = new OpenBitSetIterator(b);
		do
		{
		  aa = a.nextSetBit(aa + 1);
		  bb = random().nextBoolean() ? iterator.nextDoc() : iterator.advance(bb + 1);
		  Assert.AreEqual(aa == -1 ? DocIdSetIterator.NO_MORE_DOCS : aa, bb);
		} while (aa >= 0);
	  }

	  internal virtual void DoIterate2(BitArray a, OpenBitSet b)
	  {
		int aa = -1, bb = -1;
		OpenBitSetIterator iterator = new OpenBitSetIterator(b);
		do
		{
		  aa = a.nextSetBit(aa + 1);
		  bb = random().nextBoolean() ? iterator.nextDoc() : iterator.advance(bb + 1);
		  Assert.AreEqual(aa == -1 ? DocIdSetIterator.NO_MORE_DOCS : aa, bb);
		} while (aa >= 0);
	  }

	  internal virtual void DoRandomSets(int maxSize, int iter, int mode)
	  {
		BitArray a0 = null;
		OpenBitSet b0 = null;

		for (int i = 0; i < iter; i++)
		{
		  int sz = random().Next(maxSize);
		  BitArray a = new BitArray(sz);
		  OpenBitSet b = new OpenBitSet(sz);

		  // test the various ways of setting bits
		  if (sz > 0)
		  {
			int nOper = random().Next(sz);
			for (int j = 0; j < nOper; j++)
			{
			  int idx;

			  idx = random().Next(sz);
			  a.Set(idx, true);
			  b.fastSet(idx);

			  idx = random().Next(sz);
			  a.Set(idx, true);
			  b.fastSet((long) idx);

			  idx = random().Next(sz);
			  a.Set(idx, false);
			  b.fastClear(idx);

			  idx = random().Next(sz);
			  a.Set(idx, false);
			  b.fastClear((long) idx);

			  idx = random().Next(sz);
			  a.Set(idx, !a.Get(idx));
			  b.fastFlip(idx);

			  bool val = b.flipAndGet(idx);
			  bool val2 = b.flipAndGet(idx);
			  Assert.IsTrue(val != val2);

			  idx = random().Next(sz);
			  a.Set(idx, !a.Get(idx));
			  b.fastFlip((long) idx);

			  val = b.flipAndGet((long) idx);
			  val2 = b.flipAndGet((long) idx);
			  Assert.IsTrue(val != val2);

			  val = b.getAndSet(idx);
			  Assert.IsTrue(val2 == val);
			  Assert.IsTrue(b.get(idx));

			  if (!val)
			  {
				  b.fastClear(idx);
			  }
			  Assert.IsTrue(b.get(idx) == val);
			}
		  }

		  // test that the various ways of accessing the bits are equivalent
		  DoGet(a,b);
		  DoGetFast(a, b, sz);

		  // test ranges, including possible extension
		  int fromIndex, toIndex;
		  fromIndex = random().Next(sz + 80);
		  toIndex = fromIndex + random().Next((sz >> 1) + 1);
		  BitArray aa = (BitArray)a.clone();
		  aa.flip(fromIndex,toIndex);
		  OpenBitSet bb = b.clone();
		  bb.flip(fromIndex,toIndex);

		  DoIterate(aa,bb, mode); // a problem here is from flip or doIterate

		  fromIndex = random().Next(sz + 80);
		  toIndex = fromIndex + random().Next((sz >> 1) + 1);
		  aa = (BitArray)a.clone();
		  aa.clear(fromIndex,toIndex);
		  bb = b.clone();
		  bb.clear(fromIndex,toIndex);

		  DoNextSetBit(aa,bb); // a problem here is from clear() or nextSetBit
		  DoNextSetBitLong(aa,bb);

		  DoPrevSetBit(aa,bb);
		  DoPrevSetBitLong(aa,bb);

		  fromIndex = random().Next(sz + 80);
		  toIndex = fromIndex + random().Next((sz >> 1) + 1);
		  aa = (BitArray)a.clone();
		  aa.Set(fromIndex,toIndex);
		  bb = b.clone();
		  bb.set(fromIndex,toIndex);

		  DoNextSetBit(aa,bb); // a problem here is from set() or nextSetBit
		  DoNextSetBitLong(aa,bb);

		  DoPrevSetBit(aa,bb);
		  DoPrevSetBitLong(aa,bb);

		  if (a0 != null)
		  {
			Assert.AreEqual(a.Equals(a0), b.Equals(b0));

			Assert.AreEqual(a.cardinality(), b.cardinality());

			BitArray a_and = (BitArray)a.clone();
			a_and = a_and.And(a0);
			BitArray a_or = (BitArray)a.clone();
			a_or = a_or.Or(a0);
			BitArray a_xor = (BitArray)a.clone();
			a_xor = a_xor.Xor(a0);
			BitArray a_andn = (BitArray)a.clone();
			a_andn.andNot(a0);

			OpenBitSet b_and = b.clone();
			Assert.AreEqual(b,b_and);
			b_and.and(b0);
			OpenBitSet b_or = b.clone();
			b_or.or(b0);
			OpenBitSet b_xor = b.clone();
			b_xor.xor(b0);
			OpenBitSet b_andn = b.clone();
			b_andn.andNot(b0);

			DoIterate(a_and,b_and, mode);
			DoIterate(a_or,b_or, mode);
			DoIterate(a_xor,b_xor, mode);
			DoIterate(a_andn,b_andn, mode);

			Assert.AreEqual(a_and.cardinality(), b_and.cardinality());
			Assert.AreEqual(a_or.cardinality(), b_or.cardinality());
			Assert.AreEqual(a_xor.cardinality(), b_xor.cardinality());
			Assert.AreEqual(a_andn.cardinality(), b_andn.cardinality());

			// test non-mutating popcounts
			Assert.AreEqual(b_and.cardinality(), OpenBitSet.intersectionCount(b,b0));
			Assert.AreEqual(b_or.cardinality(), OpenBitSet.unionCount(b,b0));
			Assert.AreEqual(b_xor.cardinality(), OpenBitSet.xorCount(b,b0));
			Assert.AreEqual(b_andn.cardinality(), OpenBitSet.andNotCount(b,b0));
		  }

		  a0 = a;
		  b0 = b;
		}
	  }

	  // large enough to flush obvious bugs, small enough to run in <.5 sec as part of a
	  // larger testsuite.
	  public virtual void TestSmall()
	  {
		DoRandomSets(atLeast(1200), atLeast(1000), 1);
		DoRandomSets(atLeast(1200), atLeast(1000), 2);
	  }

	  // uncomment to run a bigger test (~2 minutes).
	  /*
	  public void testBig() {
	    doRandomSets(2000,200000, 1);
	    doRandomSets(2000,200000, 2);
	  }
	  */

	  public virtual void TestEquals()
	  {
		OpenBitSet b1 = new OpenBitSet(1111);
		OpenBitSet b2 = new OpenBitSet(2222);
		Assert.IsTrue(b1.Equals(b2));
		Assert.IsTrue(b2.Equals(b1));
		b1.set(10);
		Assert.IsFalse(b1.Equals(b2));
		Assert.IsFalse(b2.Equals(b1));
		b2.set(10);
		Assert.IsTrue(b1.Equals(b2));
		Assert.IsTrue(b2.Equals(b1));
		b2.set(2221);
		Assert.IsFalse(b1.Equals(b2));
		Assert.IsFalse(b2.Equals(b1));
		b1.set(2221);
		Assert.IsTrue(b1.Equals(b2));
		Assert.IsTrue(b2.Equals(b1));

		// try different type of object
		Assert.IsFalse(b1.Equals(new object()));
	  }

	  public virtual void TestHashCodeEquals()
	  {
		OpenBitSet bs1 = new OpenBitSet(200);
		OpenBitSet bs2 = new OpenBitSet(64);
		bs1.set(3);
		bs2.set(3);
		Assert.AreEqual(bs1, bs2);
		Assert.AreEqual(bs1.GetHashCode(), bs2.GetHashCode());
	  }


	  private OpenBitSet MakeOpenBitSet(int[] a)
	  {
		OpenBitSet bs = new OpenBitSet();
		foreach (int e in a)
		{
		  bs.set(e);
		}
		return bs;
	  }

	  private BitArray MakeBitSet(int[] a)
	  {
		BitArray bs = new BitArray();
		foreach (int e in a)
		{
		  bs.Set(e, true);
		}
		return bs;
	  }

	  private void CheckPrevSetBitArray(int[] a)
	  {
		OpenBitSet obs = MakeOpenBitSet(a);
		BitArray bs = MakeBitSet(a);
		DoPrevSetBit(bs, obs);
	  }

	  public virtual void TestPrevSetBit()
	  {
		CheckPrevSetBitArray(new int[] {});
		CheckPrevSetBitArray(new int[] {0});
		CheckPrevSetBitArray(new int[] {0,2});
	  }

	  public virtual void TestEnsureCapacity()
	  {
		OpenBitSet bits = new OpenBitSet(1);
		int bit = random().Next(100) + 10;
		bits.ensureCapacity(bit); // make room for more bits
		bits.fastSet(bit - 1);
		Assert.IsTrue(bits.fastGet(bit - 1));
		bits.ensureCapacity(bit + 1);
		bits.fastSet(bit);
		Assert.IsTrue(bits.fastGet(bit));
		bits.ensureCapacity(3); // should not change numBits nor grow the array
		bits.fastSet(3);
		Assert.IsTrue(bits.fastGet(3));
		bits.fastSet(bit - 1);
		Assert.IsTrue(bits.fastGet(bit - 1));

		// test ensureCapacityWords
		int numWords = random().Next(10) + 2; // make sure we grow the array (at least 128 bits)
		bits.ensureCapacityWords(numWords);
		bit = TestUtil.Next(random(), 127, (numWords << 6) - 1); // pick a bit >= to 128, but still within range
		bits.fastSet(bit);
		Assert.IsTrue(bits.fastGet(bit));
		bits.fastClear(bit);
		Assert.IsFalse(bits.fastGet(bit));
		bits.fastFlip(bit);
		Assert.IsTrue(bits.fastGet(bit));
		bits.ensureCapacityWords(2); // should not change numBits nor grow the array
		bits.fastSet(3);
		Assert.IsTrue(bits.fastGet(3));
		bits.fastSet(bit - 1);
		Assert.IsTrue(bits.fastGet(bit - 1));
	  }

	}




}