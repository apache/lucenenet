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

	public class TestFixedBitSet : BaseDocIdSetTestCase<FixedBitSet>
	{

	  public override FixedBitSet CopyOf(BitArray bs, int length)
	  {
		FixedBitSet set = new FixedBitSet(length);
		for (int doc = bs.nextSetBit(0); doc != -1; doc = bs.nextSetBit(doc + 1))
		{
		  set.set(doc);
		}
		return set;
	  }

	  internal virtual void DoGet(BitArray a, FixedBitSet b)
	  {
		int max = b.length();
		for (int i = 0; i < max; i++)
		{
		  if (a.Get(i) != b.get(i))
		  {
			Assert.Fail("mismatch: BitSet=[" + i + "]=" + a.Get(i));
		  }
		}
	  }

	  internal virtual void DoNextSetBit(BitArray a, FixedBitSet b)
	  {
		int aa = -1, bb = -1;
		do
		{
		  aa = a.nextSetBit(aa + 1);
		  bb = bb < b.length() - 1 ? b.nextSetBit(bb + 1) : -1;
		  Assert.AreEqual(aa,bb);
		} while (aa >= 0);
	  }

	  internal virtual void DoPrevSetBit(BitArray a, FixedBitSet b)
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
		  if (b.length() == 0)
		  {
			bb = -1;
		  }
		  else if (bb > b.length() - 1)
		  {
			bb = b.prevSetBit(b.length() - 1);
		  }
		  else if (bb < 1)
		  {
			bb = -1;
		  }
		  else
		  {
			bb = bb >= 1 ? b.prevSetBit(bb - 1) : -1;
		  }
		  Assert.AreEqual(aa,bb);
		} while (aa >= 0);
	  }

	  // test interleaving different FixedBitSetIterator.next()/skipTo()
	  internal virtual void DoIterate(BitArray a, FixedBitSet b, int mode)
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

	  internal virtual void DoIterate1(BitArray a, FixedBitSet b)
	  {
		int aa = -1, bb = -1;
		DocIdSetIterator iterator = b.GetEnumerator();
		do
		{
		  aa = a.nextSetBit(aa + 1);
		  bb = (bb < b.length() && random().nextBoolean()) ? iterator.nextDoc() : iterator.advance(bb + 1);
		  Assert.AreEqual(aa == -1 ? DocIdSetIterator.NO_MORE_DOCS : aa, bb);
		} while (aa >= 0);
	  }

	  internal virtual void DoIterate2(BitArray a, FixedBitSet b)
	  {
		int aa = -1, bb = -1;
		DocIdSetIterator iterator = b.GetEnumerator();
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
		FixedBitSet b0 = null;

		for (int i = 0; i < iter; i++)
		{
		  int sz = TestUtil.Next(random(), 2, maxSize);
		  BitArray a = new BitArray(sz);
		  FixedBitSet b = new FixedBitSet(sz);

		  // test the various ways of setting bits
		  if (sz > 0)
		  {
			int nOper = random().Next(sz);
			for (int j = 0; j < nOper; j++)
			{
			  int idx;

			  idx = random().Next(sz);
			  a.Set(idx, true);
			  b.set(idx);

			  idx = random().Next(sz);
			  a.Set(idx, false);
			  b.clear(idx);

			  idx = random().Next(sz);
			  a.Set(idx, !a.Get(idx));
			  b.flip(idx, idx + 1);

			  idx = random().Next(sz);
			  a.Set(idx, !a.Get(idx));
			  b.flip(idx, idx + 1);

			  bool val2 = b.get(idx);
			  bool val = b.getAndSet(idx);
			  Assert.IsTrue(val2 == val);
			  Assert.IsTrue(b.get(idx));

			  if (!val)
			  {
				  b.clear(idx);
			  }
			  Assert.IsTrue(b.get(idx) == val);
			}
		  }

		  // test that the various ways of accessing the bits are equivalent
		  DoGet(a,b);

		  // test ranges, including possible extension
		  int fromIndex, toIndex;
		  fromIndex = random().Next(sz / 2);
		  toIndex = fromIndex + random().Next(sz - fromIndex);
		  BitArray aa = (BitArray)a.clone();
		  aa.flip(fromIndex,toIndex);
		  FixedBitSet bb = b.clone();
		  bb.flip(fromIndex,toIndex);

		  DoIterate(aa,bb, mode); // a problem here is from flip or doIterate

		  fromIndex = random().Next(sz / 2);
		  toIndex = fromIndex + random().Next(sz - fromIndex);
		  aa = (BitArray)a.clone();
		  aa.clear(fromIndex,toIndex);
		  bb = b.clone();
		  bb.clear(fromIndex,toIndex);

		  DoNextSetBit(aa,bb); // a problem here is from clear() or nextSetBit

		  DoPrevSetBit(aa,bb);

		  fromIndex = random().Next(sz / 2);
		  toIndex = fromIndex + random().Next(sz - fromIndex);
		  aa = (BitArray)a.clone();
		  aa.Set(fromIndex,toIndex);
		  bb = b.clone();
		  bb.set(fromIndex,toIndex);

		  DoNextSetBit(aa,bb); // a problem here is from set() or nextSetBit

		  DoPrevSetBit(aa,bb);

		  if (b0 != null && b0.length() <= b.length())
		  {
			Assert.AreEqual(a.cardinality(), b.cardinality());

			BitArray a_and = (BitArray)a.clone();
			a_and = a_and.And(a0);
			BitArray a_or = (BitArray)a.clone();
			a_or = a_or.Or(a0);
			BitArray a_xor = (BitArray)a.clone();
			a_xor = a_xor.Xor(a0);
			BitArray a_andn = (BitArray)a.clone();
			a_andn.andNot(a0);

			FixedBitSet b_and = b.clone();
			Assert.AreEqual(b,b_and);
			b_and.and(b0);
			FixedBitSet b_or = b.clone();
			b_or.or(b0);
			FixedBitSet b_xor = b.clone();
			b_xor.xor(b0);
			FixedBitSet b_andn = b.clone();
			b_andn.andNot(b0);

			Assert.AreEqual(a0.cardinality(), b0.cardinality());
			Assert.AreEqual(a_or.cardinality(), b_or.cardinality());

			DoIterate(a_and,b_and, mode);
			DoIterate(a_or,b_or, mode);
			DoIterate(a_andn,b_andn, mode);
			DoIterate(a_xor,b_xor, mode);

			Assert.AreEqual(a_and.cardinality(), b_and.cardinality());
			Assert.AreEqual(a_or.cardinality(), b_or.cardinality());
			Assert.AreEqual(a_xor.cardinality(), b_xor.cardinality());
			Assert.AreEqual(a_andn.cardinality(), b_andn.cardinality());
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
		// this test can't handle numBits==0:
		int numBits = random().Next(2000) + 1;
		FixedBitSet b1 = new FixedBitSet(numBits);
		FixedBitSet b2 = new FixedBitSet(numBits);
		Assert.IsTrue(b1.Equals(b2));
		Assert.IsTrue(b2.Equals(b1));
		for (int iter = 0;iter < 10 * RANDOM_MULTIPLIER;iter++)
		{
		  int idx = random().Next(numBits);
		  if (!b1.get(idx))
		  {
			b1.set(idx);
			Assert.IsFalse(b1.Equals(b2));
			Assert.IsFalse(b2.Equals(b1));
			b2.set(idx);
			Assert.IsTrue(b1.Equals(b2));
			Assert.IsTrue(b2.Equals(b1));
		  }
		}

		// try different type of object
		Assert.IsFalse(b1.Equals(new object()));
	  }

	  public virtual void TestHashCodeEquals()
	  {
		// this test can't handle numBits==0:
		int numBits = random().Next(2000) + 1;
		FixedBitSet b1 = new FixedBitSet(numBits);
		FixedBitSet b2 = new FixedBitSet(numBits);
		Assert.IsTrue(b1.Equals(b2));
		Assert.IsTrue(b2.Equals(b1));
		for (int iter = 0;iter < 10 * RANDOM_MULTIPLIER;iter++)
		{
		  int idx = random().Next(numBits);
		  if (!b1.get(idx))
		  {
			b1.set(idx);
			Assert.IsFalse(b1.Equals(b2));
			Assert.IsFalse(b1.GetHashCode() == b2.GetHashCode());
			b2.set(idx);
			Assert.AreEqual(b1, b2);
			Assert.AreEqual(b1.GetHashCode(), b2.GetHashCode());
		  }
		}
	  }

	  public virtual void TestSmallBitSets()
	  {
		// Make sure size 0-10 bit sets are OK:
		for (int numBits = 0;numBits < 10;numBits++)
		{
		  FixedBitSet b1 = new FixedBitSet(numBits);
		  FixedBitSet b2 = new FixedBitSet(numBits);
		  Assert.IsTrue(b1.Equals(b2));
		  Assert.AreEqual(b1.GetHashCode(), b2.GetHashCode());
		  Assert.AreEqual(0, b1.cardinality());
		  if (numBits > 0)
		  {
			b1.set(0, numBits);
			Assert.AreEqual(numBits, b1.cardinality());
			b1.flip(0, numBits);
			Assert.AreEqual(0, b1.cardinality());
		  }
		}
	  }

	  private FixedBitSet MakeFixedBitSet(int[] a, int numBits)
	  {
		FixedBitSet bs;
		if (random().nextBoolean())
		{
		  int bits2words = FixedBitSet.bits2words(numBits);
		  long[] words = new long[bits2words + random().Next(100)];
		  for (int i = bits2words; i < words.Length; i++)
		  {
			words[i] = random().nextLong();
		  }
		  bs = new FixedBitSet(words, numBits);

		}
		else
		{
		  bs = new FixedBitSet(numBits);
		}
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

	  private void CheckPrevSetBitArray(int[] a, int numBits)
	  {
		FixedBitSet obs = MakeFixedBitSet(a, numBits);
		BitArray bs = MakeBitSet(a);
		DoPrevSetBit(bs, obs);
	  }

	  public virtual void TestPrevSetBit()
	  {
		CheckPrevSetBitArray(new int[] {}, 0);
		CheckPrevSetBitArray(new int[] {0}, 1);
		CheckPrevSetBitArray(new int[] {0,2}, 3);
	  }


	  private void CheckNextSetBitArray(int[] a, int numBits)
	  {
		FixedBitSet obs = MakeFixedBitSet(a, numBits);
		BitArray bs = MakeBitSet(a);
		DoNextSetBit(bs, obs);
	  }

	  public virtual void TestNextBitSet()
	  {
		int[] setBits = new int[0 + random().Next(1000)];
		for (int i = 0; i < setBits.Length; i++)
		{
		  setBits[i] = random().Next(setBits.Length);
		}
		CheckNextSetBitArray(setBits, setBits.Length + random().Next(10));

		CheckNextSetBitArray(new int[0], setBits.Length + random().Next(10));
	  }

	  public virtual void TestEnsureCapacity()
	  {
		FixedBitSet bits = new FixedBitSet(5);
		bits.set(1);
		bits.set(4);

		FixedBitSet newBits = FixedBitSet.ensureCapacity(bits, 8); // grow within the word
		Assert.IsTrue(newBits.get(1));
		Assert.IsTrue(newBits.get(4));
		newBits.clear(1);
		// we align to 64-bits, so even though it shouldn't have, it re-allocated a long[1]
		Assert.IsTrue(bits.get(1));
		Assert.IsFalse(newBits.get(1));

		newBits.set(1);
		newBits = FixedBitSet.ensureCapacity(newBits, newBits.length() - 2); // reuse
		Assert.IsTrue(newBits.get(1));

		bits.set(1);
		newBits = FixedBitSet.ensureCapacity(bits, 72); // grow beyond one word
		Assert.IsTrue(newBits.get(1));
		Assert.IsTrue(newBits.get(4));
		newBits.clear(1);
		// we grew the long[], so it's not shared
		Assert.IsTrue(bits.get(1));
		Assert.IsFalse(newBits.get(1));
	  }

	}

}