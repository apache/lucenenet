using System.Collections.Generic;

namespace Lucene.Net.Util
{


	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements. See the NOTICE file distributed with this
	 * work for additional information regarding copyright ownership. The ASF
	 * licenses this file to You under the Apache License, Version 2.0 (the
	 * "License"); you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 * 
	 * http://www.apache.org/licenses/LICENSE-2.0
	 * 
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
	 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
	 * License for the specific language governing permissions and limitations under
	 * the License.
	 */
	public class TestByteBlockPool : LuceneTestCase
	{

	  public virtual void TestReadAndWrite()
	  {
		Counter bytesUsed = Counter.newCounter();
		ByteBlockPool pool = new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(bytesUsed));
		pool.nextBuffer();
		bool reuseFirst = random().nextBoolean();
		for (int j = 0; j < 2; j++)
		{

		  IList<BytesRef> list = new List<BytesRef>();
		  int maxLength = atLeast(500);
		  int numValues = atLeast(100);
		  BytesRef @ref = new BytesRef();
		  for (int i = 0; i < numValues; i++)
		  {
			string value = TestUtil.randomRealisticUnicodeString(random(), maxLength);
			list.Add(new BytesRef(value));
			@ref.copyChars(value);
			pool.append(@ref);
		  }
		  // verify
		  long position = 0;
		  foreach (BytesRef expected in list)
		  {
			@ref.grow(expected.length);
			@ref.length = expected.length;
			pool.readBytes(position, @ref.bytes, @ref.offset, @ref.length);
			Assert.AreEqual(expected, @ref);
			position += @ref.length;
		  }
		  pool.reset(random().nextBoolean(), reuseFirst);
		  if (reuseFirst)
		  {
			Assert.AreEqual(ByteBlockPool.BYTE_BLOCK_SIZE, bytesUsed.get());
		  }
		  else
		  {
			Assert.AreEqual(0, bytesUsed.get());
			pool.nextBuffer(); // prepare for next iter
		  }
		}
	  }
	}

}