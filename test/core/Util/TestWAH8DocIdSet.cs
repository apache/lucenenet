using System.Collections;
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


	public class TestWAH8DocIdSet : BaseDocIdSetTestCase<WAH8DocIdSet>
	{

	  public override WAH8DocIdSet CopyOf(BitArray bs, int length)
	  {
		int indexInterval = TestUtil.Next(random(), 8, 256);
		WAH8DocIdSet.Builder builder = (new WAH8DocIdSet.Builder()).setIndexInterval(indexInterval);
		for (int i = bs.nextSetBit(0); i != -1; i = bs.nextSetBit(i + 1))
		{
		  builder.add(i);
		}
		return builder.build();
	  }

	  public override void AssertEquals(int numBits, BitArray ds1, WAH8DocIdSet ds2)
	  {
		base.Assert.AreEqual(numBits, ds1, ds2);
		Assert.AreEqual(ds1.cardinality(), ds2.cardinality());
	  }

	  public virtual void TestUnion()
	  {
		int numBits = TestUtil.Next(random(), 100, 1 << 20);
		int numDocIdSets = TestUtil.Next(random(), 0, 4);
		IList<BitArray> fixedSets = new List<BitArray>(numDocIdSets);
		for (int i = 0; i < numDocIdSets; ++i)
		{
		  fixedSets.Add(randomSet(numBits, random().nextFloat() / 16));
		}
		IList<WAH8DocIdSet> compressedSets = new List<WAH8DocIdSet>(numDocIdSets);
		foreach (BitArray set in fixedSets)
		{
		  compressedSets.Add(CopyOf(set, numBits));
		}

		WAH8DocIdSet union = WAH8DocIdSet.union(compressedSets);
		BitArray expected = new BitArray(numBits);
		foreach (BitArray set in fixedSets)
		{
		  for (int doc = set.nextSetBit(0); doc != -1; doc = set.nextSetBit(doc + 1))
		  {
			expected.Set(doc, true);
		  }
		}
		AssertEquals(numBits, expected, union);
	  }

	  public virtual void TestIntersection()
	  {
		int numBits = TestUtil.Next(random(), 100, 1 << 20);
		int numDocIdSets = TestUtil.Next(random(), 1, 4);
		IList<BitArray> fixedSets = new List<BitArray>(numDocIdSets);
		for (int i = 0; i < numDocIdSets; ++i)
		{
		  fixedSets.Add(randomSet(numBits, random().nextFloat()));
		}
		IList<WAH8DocIdSet> compressedSets = new List<WAH8DocIdSet>(numDocIdSets);
		foreach (BitArray set in fixedSets)
		{
		  compressedSets.Add(CopyOf(set, numBits));
		}

		WAH8DocIdSet union = WAH8DocIdSet.intersect(compressedSets);
		BitArray expected = new BitArray(numBits);
		expected.Set(0, expected.Count);
		foreach (BitArray set in fixedSets)
		{
		  for (int previousDoc = -1, doc = set.nextSetBit(0); ; previousDoc = doc, doc = set.nextSetBit(doc + 1))
		  {
			if (doc == -1)
			{
			  expected.clear(previousDoc + 1, set.Count);
			  break;
			}
			else
			{
			  expected.clear(previousDoc + 1, doc);
			}
		  }
		}
		AssertEquals(numBits, expected, union);
	  }

	}

}