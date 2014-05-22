using System;

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


	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using DataInput = Lucene.Net.Store.DataInput;
	using DataOutput = Lucene.Net.Store.DataOutput;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using Ignore = org.junit.Ignore;

	public class TestPagedBytes : LuceneTestCase
	{

	  // Writes random byte/s to "normal" file in dir, then
	  // copies into PagedBytes and verifies with
	  // PagedBytes.Reader: 
	  public virtual void TestDataInputOutput()
	  {
		Random random = random();
		for (int iter = 0;iter < 5 * RANDOM_MULTIPLIER;iter++)
		{
		  BaseDirectoryWrapper dir = newFSDirectory(createTempDir("testOverflow"));
		  if (dir is MockDirectoryWrapper)
		  {
			((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		  }
		  int blockBits = TestUtil.Next(random, 1, 20);
		  int blockSize = 1 << blockBits;
		  PagedBytes p = new PagedBytes(blockBits);
		  IndexOutput @out = dir.createOutput("foo", IOContext.DEFAULT);
		  int numBytes = TestUtil.Next(random(), 2, 10000000);

		  sbyte[] answer = new sbyte[numBytes];
		  random().nextBytes(answer);
		  int written = 0;
		  while (written < numBytes)
		  {
			if (random().Next(10) == 7)
			{
			  @out.writeByte(answer[written++]);
			}
			else
			{
			  int chunk = Math.Min(random().Next(1000), numBytes - written);
			  @out.writeBytes(answer, written, chunk);
			  written += chunk;
			}
		  }

		  @out.close();
		  IndexInput input = dir.openInput("foo", IOContext.DEFAULT);
		  DataInput @in = input.clone();

		  p.copy(input, input.length());
		  PagedBytes.Reader reader = p.freeze(random.nextBoolean());

		  sbyte[] verify = new sbyte[numBytes];
		  int read = 0;
		  while (read < numBytes)
		  {
			if (random().Next(10) == 7)
			{
			  verify[read++] = @in.readByte();
			}
			else
			{
			  int chunk = Math.Min(random().Next(1000), numBytes - read);
			  @in.readBytes(verify, read, chunk);
			  read += chunk;
			}
		  }
		  Assert.IsTrue(Array.Equals(answer, verify));

		  BytesRef slice = new BytesRef();
		  for (int iter2 = 0;iter2 < 100;iter2++)
		  {
			int pos = random.Next(numBytes - 1);
			int len = random.Next(Math.Min(blockSize+1, numBytes - pos));
			reader.fillSlice(slice, pos, len);
			for (int byteUpto = 0;byteUpto < len;byteUpto++)
			{
			  Assert.AreEqual(answer[pos + byteUpto], slice.bytes[slice.offset + byteUpto]);
			}
		  }
		  input.close();
		  dir.close();
		}
	  }

	  // Writes random byte/s into PagedBytes via
	  // .getDataOutput(), then verifies with
	  // PagedBytes.getDataInput(): 
	  public virtual void TestDataInputOutput2()
	  {
		Random random = random();
		for (int iter = 0;iter < 5 * RANDOM_MULTIPLIER;iter++)
		{
		  int blockBits = TestUtil.Next(random, 1, 20);
		  int blockSize = 1 << blockBits;
		  PagedBytes p = new PagedBytes(blockBits);
		  DataOutput @out = p.DataOutput;
		  int numBytes = random().Next(10000000);

		  sbyte[] answer = new sbyte[numBytes];
		  random().nextBytes(answer);
		  int written = 0;
		  while (written < numBytes)
		  {
			if (random().Next(10) == 7)
			{
			  @out.writeByte(answer[written++]);
			}
			else
			{
			  int chunk = Math.Min(random().Next(1000), numBytes - written);
			  @out.writeBytes(answer, written, chunk);
			  written += chunk;
			}
		  }

		  PagedBytes.Reader reader = p.freeze(random.nextBoolean());

		  DataInput @in = p.DataInput;

		  sbyte[] verify = new sbyte[numBytes];
		  int read = 0;
		  while (read < numBytes)
		  {
			if (random().Next(10) == 7)
			{
			  verify[read++] = @in.readByte();
			}
			else
			{
			  int chunk = Math.Min(random().Next(1000), numBytes - read);
			  @in.readBytes(verify, read, chunk);
			  read += chunk;
			}
		  }
		  Assert.IsTrue(Array.Equals(answer, verify));

		  BytesRef slice = new BytesRef();
		  for (int iter2 = 0;iter2 < 100;iter2++)
		  {
			int pos = random.Next(numBytes - 1);
			int len = random.Next(Math.Min(blockSize+1, numBytes - pos));
			reader.fillSlice(slice, pos, len);
			for (int byteUpto = 0;byteUpto < len;byteUpto++)
			{
			  Assert.AreEqual(answer[pos + byteUpto], slice.bytes[slice.offset + byteUpto]);
			}
		  }
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore public void testOverflow() throws java.io.IOException
	  public virtual void TestOverflow() // memory hole
	  {
		BaseDirectoryWrapper dir = newFSDirectory(createTempDir("testOverflow"));
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}
		int blockBits = TestUtil.Next(random(), 14, 28);
		int blockSize = 1 << blockBits;
		sbyte[] arr = new sbyte[TestUtil.Next(random(), blockSize / 2, blockSize * 2)];
		for (int i = 0; i < arr.Length; ++i)
		{
		  arr[i] = (sbyte) i;
		}
		long numBytes = (1L << 31) + TestUtil.Next(random(), 1, blockSize * 3);
		PagedBytes p = new PagedBytes(blockBits);
		IndexOutput @out = dir.createOutput("foo", IOContext.DEFAULT);
		for (long i = 0; i < numBytes;)
		{
		  Assert.AreEqual(i, @out.FilePointer);
		  int len = (int) Math.Min(arr.Length, numBytes - i);
		  @out.writeBytes(arr, len);
		  i += len;
		}
		Assert.AreEqual(numBytes, @out.FilePointer);
		@out.close();
		IndexInput @in = dir.openInput("foo", IOContext.DEFAULT);
		p.copy(@in, numBytes);
		PagedBytes.Reader reader = p.freeze(random().nextBoolean());

		foreach (long offset in new long[] {0L, int.MaxValue, numBytes - 1, TestUtil.nextLong(random(), 1, numBytes - 2)})
		{
		  BytesRef b = new BytesRef();
		  reader.fillSlice(b, offset, 1);
		  Assert.AreEqual(arr[(int)(offset % arr.Length)], b.bytes[b.offset]);
		}
		@in.close();
		dir.close();
	  }

	}

}