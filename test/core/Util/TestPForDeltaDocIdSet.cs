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


	public class TestPForDeltaDocIdSet : BaseDocIdSetTestCase<PForDeltaDocIdSet>
	{

	  public override PForDeltaDocIdSet CopyOf(BitArray bs, int length)
	  {
		PForDeltaDocIdSet.Builder builder = (new PForDeltaDocIdSet.Builder()).setIndexInterval(TestUtil.Next(random(), 1, 20));
		for (int doc = bs.nextSetBit(0); doc != -1; doc = bs.nextSetBit(doc + 1))
		{
		  builder.add(doc);
		}
		return builder.build();
	  }

	  public override void AssertEquals(int numBits, BitArray ds1, PForDeltaDocIdSet ds2)
	  {
		base.Assert.AreEqual(numBits, ds1, ds2);
		Assert.AreEqual(ds1.cardinality(), ds2.cardinality());
	  }

	}

}