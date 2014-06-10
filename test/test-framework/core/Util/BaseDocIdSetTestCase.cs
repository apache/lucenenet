using System;
using System.Diagnostics;
using System.Collections;

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


	using DocIdSet = Lucene.Net.Search.DocIdSet;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

	/// <summary>
	/// Base test class for <seealso cref="DocIdSet"/>s. </summary>
	public abstract class BaseDocIdSetTestCase<T> : LuceneTestCase where T : Lucene.Net.Search.DocIdSet
	{

	  /// <summary>
	  /// Create a copy of the given <seealso cref="BitSet"/> which has <code>length</code> bits. </summary>
	  public abstract T CopyOf(BitArray bs, int length);

	  /// <summary>
	  /// Create a random set which has <code>numBitsSet</code> of its <code>numBits</code> bits set. </summary>
	  protected internal static BitArray RandomSet(int numBits, int numBitsSet)
	  {
		Debug.Assert(numBitsSet <= numBits);
		BitArray set = new BitArray(numBits);
		if (numBitsSet == numBits)
		{
		  set.Set(0, numBits);
		}
		else
		{
		  for (int i = 0; i < numBitsSet; ++i)
		  {
			while (true)
			{
			  int o = random().Next(numBits);
			  if (!set.Get(o))
			  {
				set.Set(o, true);
				break;
			  }
			}
		  }
		}
		return set;
	  }

	  /// <summary>
	  /// Same as <seealso cref="#randomSet(int, int)"/> but given a load factor. </summary>
	  protected internal static BitArray RandomSet(int numBits, float percentSet)
	  {
		return RandomSet(numBits, (int)(percentSet * numBits));
	  }

	  /// <summary>
	  /// Test length=0. </summary>
	  public virtual void TestNoBit()
	  {
		BitArray bs = new BitArray(1);
		T copy = CopyOf(bs, 0);
		AssertEquals(0, bs, copy);
	  }

	  /// <summary>
	  /// Test length=1. </summary>
	  public virtual void Test1Bit()
	  {
		BitArray bs = new BitArray(1);
		if (random().nextBoolean())
		{
		  bs.Set(0, true);
		}
		T copy = CopyOf(bs, 1);
		AssertEquals(1, bs, copy);
	  }

	  /// <summary>
	  /// Test length=2. </summary>
	  public virtual void Test2Bits()
	  {
		BitArray bs = new BitArray(2);
		if (random().nextBoolean())
		{
		  bs.Set(0, true);
		}
		if (random().nextBoolean())
		{
		  bs.Set(1, true);
		}
		T copy = CopyOf(bs, 2);
		AssertEquals(2, bs, copy);
	  }

	  /// <summary>
	  /// Compare the content of the set against a <seealso cref="BitSet"/>. </summary>
	  public virtual void TestAgainstBitSet()
	  {
		int numBits = TestUtil.NextInt(random(), 100, 1 << 20);
		// test various random sets with various load factors
		foreach (float percentSet in new float[] {0f, 0.0001f, random().nextFloat() / 2, 0.9f, 1f})
		{
		  BitArray set = RandomSet(numBits, percentSet);
		  T copy = CopyOf(set, numBits);
		  AssertEquals(numBits, set, copy);
		}
		// test one doc
		BitArray set = new BitArray(numBits);
		set.Set(0, true); // 0 first
		T copy = CopyOf(set, numBits);
		AssertEquals(numBits, set, copy);
		set.Set(0, false);
		set.Set(random().Next(numBits), true);
		copy = CopyOf(set, numBits); // then random index
		AssertEquals(numBits, set, copy);
		// test regular increments
		for (int inc = 2; inc < 1000; inc += TestUtil.NextInt(random(), 1, 100))
		{
		  set = new BitArray(numBits);
		  for (int d = random().Next(10); d < numBits; d += inc)
		  {
			set.Set(d, true);
		  }
		  copy = CopyOf(set, numBits);
		  AssertEquals(numBits, set, copy);
		}
	  }

	  /// <summary>
	  /// Assert that the content of the <seealso cref="DocIdSet"/> is the same as the content of the <seealso cref="BitSet"/>. </summary>
	  public virtual void AssertEquals(int numBits, BitArray ds1, T ds2)
	  {
		// nextDoc
		DocIdSetIterator it2 = ds2.GetEnumerator();
		if (it2 == null)
		{
		  Assert.AreEqual(-1, ds1.nextSetBit(0));
		}
		else
		{
		  Assert.AreEqual(-1, it2.docID());
		  for (int doc = ds1.nextSetBit(0); doc != -1; doc = ds1.nextSetBit(doc + 1))
		  {
			Assert.AreEqual(doc, it2.nextDoc());
			Assert.AreEqual(doc, it2.docID());
		  }
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, it2.nextDoc());
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, it2.docID());
		}

		// nextDoc / advance
		it2 = ds2.GetEnumerator();
		if (it2 == null)
		{
		  Assert.AreEqual(-1, ds1.nextSetBit(0));
		}
		else
		{
		  for (int doc = -1; doc != DocIdSetIterator.NO_MORE_DOCS;)
		  {
			if (random().nextBoolean())
			{
			  doc = ds1.nextSetBit(doc + 1);
			  if (doc == -1)
			  {
				doc = DocIdSetIterator.NO_MORE_DOCS;
			  }
			  Assert.AreEqual(doc, it2.nextDoc());
			  Assert.AreEqual(doc, it2.docID());
			}
			else
			{
			  int target = doc + 1 + random().Next(random().nextBoolean() ? 64 : Math.Max(numBits / 8, 1));
			  doc = ds1.nextSetBit(target);
			  if (doc == -1)
			  {
				doc = DocIdSetIterator.NO_MORE_DOCS;
			  }
			  Assert.AreEqual(doc, it2.advance(target));
			  Assert.AreEqual(doc, it2.docID());
			}
		  }
		}

		// bits()
		Bits bits = ds2.bits();
		if (bits != null)
		{
		  // test consistency between bits and iterator
		  it2 = ds2.GetEnumerator();
		  for (int previousDoc = -1, doc = it2.nextDoc(); ; previousDoc = doc, doc = it2.nextDoc())
		  {
			int max = doc == DocIdSetIterator.NO_MORE_DOCS ? bits.length() : doc;
			for (int i = previousDoc + 1; i < max; ++i)
			{
			  Assert.AreEqual(false, bits.get(i));
			}
			if (doc == DocIdSetIterator.NO_MORE_DOCS)
			{
			  break;
			}
			Assert.AreEqual(true, bits.get(doc));
		  }
		}
	  }

	}

}