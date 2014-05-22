namespace Lucene.Net.Store
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


	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestBufferedChecksum : LuceneTestCase
	{

	  public virtual void TestSimple()
	  {
		Checksum c = new BufferedChecksum(new CRC32());
		c.update(1);
		c.update(2);
		c.update(3);
		Assert.AreEqual(1438416925L, c.Value);
	  }

	  public virtual void TestRandom()
	  {
		Checksum c1 = new CRC32();
		Checksum c2 = new BufferedChecksum(new CRC32());
		int iterations = atLeast(10000);
		for (int i = 0; i < iterations; i++)
		{
		  switch (random().Next(4))
		  {
			case 0:
			  // update(byte[], int, int)
			  int length = random().Next(1024);
			  sbyte[] bytes = new sbyte[length];
			  random().nextBytes(bytes);
			  c1.update(bytes, 0, bytes.Length);
			  c2.update(bytes, 0, bytes.Length);
			  break;
			case 1:
			  // update(int)
			  int b = random().Next(256);
			  c1.update(b);
			  c2.update(b);
			  break;
			case 2:
			  // reset()
			  c1.reset();
			  c2.reset();
			  break;
			case 3:
			  // getValue()
			  Assert.AreEqual(c1.Value, c2.Value);
			  break;
		  }
		}
		Assert.AreEqual(c1.Value, c2.Value);
	  }
	}

}