/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

namespace Lucene.Net.Util
{
	
	/// <version>  $Id$
	/// </version>
	[TestFixture]
	public class TestOpenBitSet:LuceneTestCase
	{
		internal System.Random rand;
		
		internal virtual void  DoGet(System.Collections.BitArray a, OpenBitSet b)
		{
			int max = a.Count;
			for (int i = 0; i < max; i++)
			{
				if (a.Get(i) != b.Get(i))
				{
					Assert.Fail("mismatch: BitSet=[" + i + "]=" + a.Get(i));
				}
			}
		}
		
		internal virtual void  DoNextSetBit(System.Collections.BitArray a, OpenBitSet b)
		{
			int aa = - 1, bb = - 1;
			do 
			{
				aa = SupportClass.BitSetSupport.NextSetBit(a, aa + 1);
				bb = b.NextSetBit(bb + 1);
				Assert.AreEqual(aa, bb);
			}
			while (aa >= 0);
		}
		
		// test interleaving different OpenBitSetIterator.next()/skipTo()
		internal virtual void  DoIterate(System.Collections.BitArray a, OpenBitSet b, int mode)
		{
			if (mode == 1)
				DoIterate1(a, b);
			if (mode == 2)
				DoIterate2(a, b);
		}
		
		internal virtual void  DoIterate1(System.Collections.BitArray a, OpenBitSet b)
		{
			int aa = - 1, bb = - 1;
			OpenBitSetIterator iterator = new OpenBitSetIterator(b);
			do 
			{
				aa = SupportClass.BitSetSupport.NextSetBit(a, aa + 1);
				bb = rand.NextDouble() > 0.5 ? iterator.NextDoc() : iterator.Advance(bb + 1);
				Assert.AreEqual(aa == - 1?DocIdSetIterator.NO_MORE_DOCS:aa, bb);
			}
			while (aa >= 0);
		}
		
		internal virtual void  DoIterate2(System.Collections.BitArray a, OpenBitSet b)
		{
			int aa = - 1, bb = - 1;
			OpenBitSetIterator iterator = new OpenBitSetIterator(b);
			do 
			{
				aa = SupportClass.BitSetSupport.NextSetBit(a, aa + 1);
				bb = rand.NextDouble() > 0.5 ? iterator.NextDoc() : iterator.Advance(bb + 1);
				Assert.AreEqual(aa == - 1?DocIdSetIterator.NO_MORE_DOCS:aa, bb);
			}
			while (aa >= 0);
		}
		
		internal virtual void  DoRandomSets(int maxSize, int iter, int mode)
		{
			System.Collections.BitArray a0 = null;
			OpenBitSet b0 = null;
			
			for (int i = 0; i < iter; i++)
			{
				int sz = rand.Next(maxSize);
				System.Collections.BitArray a = new System.Collections.BitArray((sz % 64 == 0?sz / 64:sz / 64 + 1) * 64);
				OpenBitSet b = new OpenBitSet(sz);
				
				// test the various ways of setting bits
				if (sz > 0)
				{
					int nOper = rand.Next(sz);
					for (int j = 0; j < nOper; j++)
					{
						int idx;
						
						idx = rand.Next(sz);
						a.Set(idx, true);
						b.FastSet(idx);
						idx = rand.Next(sz);
						a.Set(idx, false);
						b.FastClear(idx);
						idx = rand.Next(sz);
						a.Set(idx, !a.Get(idx));
						b.FastFlip(idx);
						
						bool val = b.FlipAndGet(idx);
						bool val2 = b.FlipAndGet(idx);
						Assert.IsTrue(val != val2);
						
						val = b.GetAndSet(idx);
						Assert.IsTrue(val2 == val);
						Assert.IsTrue(b.Get(idx));
						
						if (!val)
							b.FastClear(idx);
						Assert.IsTrue(b.Get(idx) == val);
					}
				}
				
				// test that the various ways of accessing the bits are equivalent
				DoGet(a, b);
				
				// test ranges, including possible extension
				int fromIndex, toIndex;
				fromIndex = rand.Next(sz + 80);
				toIndex = fromIndex + rand.Next((sz >> 1) + 1);
				System.Collections.BitArray aa = (System.Collections.BitArray) a.Clone(); for (int j = fromIndex; j < toIndex; i++) aa.Set(j, !aa.Get(j));
				OpenBitSet bb = (OpenBitSet) b.Clone(); bb.Flip(fromIndex, toIndex);
				
				DoIterate(aa, bb, mode); // a problem here is from flip or doIterate
				
				fromIndex = rand.Next(sz + 80);
				toIndex = fromIndex + rand.Next((sz >> 1) + 1);
				aa = (System.Collections.BitArray) a.Clone(); for (int j = fromIndex; j < toIndex; j++) aa.Set(j, false);
				bb = (OpenBitSet) b.Clone(); bb.Clear(fromIndex, toIndex);
				
				DoNextSetBit(aa, bb); // a problem here is from clear() or nextSetBit
				
				fromIndex = rand.Next(sz + 80);
				toIndex = fromIndex + rand.Next((sz >> 1) + 1);
				aa = (System.Collections.BitArray) a.Clone(); for (int j = fromIndex; j < toIndex; j++) aa.Set(j, true);
				bb = (OpenBitSet) b.Clone(); bb.Set(fromIndex, toIndex);
				
				DoNextSetBit(aa, bb); // a problem here is from set() or nextSetBit     
				
				
				if (a0 != null)
				{
					Assert.AreEqual(a.Equals(a0), b.Equals(b0));
					
					Assert.AreEqual(SupportClass.BitSetSupport.Cardinality(a), b.Cardinality());
					
					System.Collections.BitArray a_and = (System.Collections.BitArray) a.Clone();
					a_and.And(a0);
					System.Collections.BitArray a_or = (System.Collections.BitArray) a.Clone();
					a_or.Or(a0);
					System.Collections.BitArray a_xor = (System.Collections.BitArray) a.Clone();
					a_xor.Xor(a0);
					System.Collections.BitArray a_andn = (System.Collections.BitArray) a.Clone(); a_andn.And(a0.Not());
					
					OpenBitSet b_and = (OpenBitSet) b.Clone(); Assert.AreEqual(b, b_and); b_and.And(b0);
					OpenBitSet b_or = (OpenBitSet) b.Clone(); b_or.Or(b0);
					OpenBitSet b_xor = (OpenBitSet) b.Clone(); b_xor.Xor(b0);
					OpenBitSet b_andn = (OpenBitSet) b.Clone(); b_andn.AndNot(b0);
					
					DoIterate(a_and, b_and, mode);
					DoIterate(a_or, b_or, mode);
					DoIterate(a_xor, b_xor, mode);
					DoIterate(a_andn, b_andn, mode);
					
					Assert.AreEqual(SupportClass.BitSetSupport.Cardinality(a_and), b_and.Cardinality());
					Assert.AreEqual(SupportClass.BitSetSupport.Cardinality(a_or), b_or.Cardinality());
					Assert.AreEqual(SupportClass.BitSetSupport.Cardinality(a_xor), b_xor.Cardinality());
					Assert.AreEqual(SupportClass.BitSetSupport.Cardinality(a_andn), b_andn.Cardinality());
					
					// test non-mutating popcounts
					Assert.AreEqual(b_and.Cardinality(), OpenBitSet.IntersectionCount(b, b0));
					Assert.AreEqual(b_or.Cardinality(), OpenBitSet.UnionCount(b, b0));
					Assert.AreEqual(b_xor.Cardinality(), OpenBitSet.XorCount(b, b0));
					Assert.AreEqual(b_andn.Cardinality(), OpenBitSet.AndNotCount(b, b0));
				}
				
				a0 = a;
				b0 = b;
			}
		}
		
		// large enough to flush obvious bugs, small enough to run in <.5 sec as part of a
		// larger testsuite.
		[Test]
		public virtual void  TestSmall()
		{
			rand = NewRandom();
			DoRandomSets(1200, 1000, 1);
			DoRandomSets(1200, 1000, 2);
		}
		
		[Test]
		public virtual void  TestBig()
		{
			// uncomment to run a bigger test (~2 minutes).
			// rand = newRandom();
			// doRandomSets(2000,200000, 1);
			// doRandomSets(2000,200000, 2);
		}
		
		[Test]
		public virtual void  TestEquals()
		{
			rand = NewRandom();
			OpenBitSet b1 = new OpenBitSet(1111);
			OpenBitSet b2 = new OpenBitSet(2222);
			Assert.IsTrue(b1.Equals(b2));
			Assert.IsTrue(b2.Equals(b1));
			b1.Set(10);
			Assert.IsFalse(b1.Equals(b2));
			Assert.IsFalse(b2.Equals(b1));
			b2.Set(10);
			Assert.IsTrue(b1.Equals(b2));
			Assert.IsTrue(b2.Equals(b1));
			b2.Set(2221);
			Assert.IsFalse(b1.Equals(b2));
			Assert.IsFalse(b2.Equals(b1));
			b1.Set(2221);
			Assert.IsTrue(b1.Equals(b2));
			Assert.IsTrue(b2.Equals(b1));
			
			// try different type of object
			Assert.IsFalse(b1.Equals(new System.Object()));
		}
		
		[Test]
		public virtual void  TestBitUtils()
		{
			rand = NewRandom();
			long num = 100000;
			Assert.AreEqual(5, BitUtil.Ntz(num));
			Assert.AreEqual(5, BitUtil.Ntz2(num));
			Assert.AreEqual(5, BitUtil.Ntz3(num));
			
			num = 10;
			Assert.AreEqual(1, BitUtil.Ntz(num));
			Assert.AreEqual(1, BitUtil.Ntz2(num));
			Assert.AreEqual(1, BitUtil.Ntz3(num));
			
			for (int i = 0; i < 64; i++)
			{
				num = 1L << i;
				Assert.AreEqual(i, BitUtil.Ntz(num));
				Assert.AreEqual(i, BitUtil.Ntz2(num));
				Assert.AreEqual(i, BitUtil.Ntz3(num));
			}
		}
	}
}