using System;
using System.Collections.Generic;

namespace Lucene.Net.Util
{

	using Before = org.junit.Before;
	using Test = org.junit.Test;

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

	/// <summary>
	/// Testcase for <seealso cref="RecyclingByteBlockAllocator"/>
	/// </summary>
	public class TestRecyclingByteBlockAllocator : LuceneTestCase
	{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Override @Before public void setUp() throws Exception
	  public override void SetUp()
	  {
		base.setUp();
	  }

	  private RecyclingByteBlockAllocator NewAllocator()
	  {
		return new RecyclingByteBlockAllocator(1 << (2 + random().Next(15)), random().Next(97), Counter.newCounter());
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAllocate()
	  public virtual void TestAllocate()
	  {
		RecyclingByteBlockAllocator allocator = NewAllocator();
		HashSet<sbyte[]> set = new HashSet<sbyte[]>();
		sbyte[] block = allocator.ByteBlock;
		set.Add(block);
		Assert.IsNotNull(block);
		int size = block.Length;

		int num = atLeast(97);
		for (int i = 0; i < num; i++)
		{
		  block = allocator.ByteBlock;
		  Assert.IsNotNull(block);
		  Assert.AreEqual(size, block.Length);
		  Assert.IsTrue("block is returned twice", set.Add(block));
		  Assert.AreEqual(size * (i + 2), allocator.bytesUsed()); // zero based + 1
		  Assert.AreEqual(0, allocator.numBufferedBlocks());
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAllocateAndRecycle()
	  public virtual void TestAllocateAndRecycle()
	  {
		RecyclingByteBlockAllocator allocator = NewAllocator();
		HashSet<sbyte[]> allocated = new HashSet<sbyte[]>();

		sbyte[] block = allocator.ByteBlock;
		allocated.Add(block);
		Assert.IsNotNull(block);
		int size = block.Length;

		int numIters = atLeast(97);
		for (int i = 0; i < numIters; i++)
		{
		  int num = 1 + random().Next(39);
		  for (int j = 0; j < num; j++)
		  {
			block = allocator.ByteBlock;
			Assert.IsNotNull(block);
			Assert.AreEqual(size, block.Length);
			Assert.IsTrue("block is returned twice", allocated.Add(block));
			Assert.AreEqual(size * (allocated.Count + allocator.numBufferedBlocks()), allocator.bytesUsed());
		  }
		  sbyte[][] array = allocated.toArray(new sbyte[0][]);
		  int begin = random().Next(array.Length);
		  int end = begin + random().Next(array.Length - begin);
		  IList<sbyte[]> selected = new List<sbyte[]>();
		  for (int j = begin; j < end; j++)
		  {
			selected.Add(array[j]);
		  }
		  allocator.recycleByteBlocks(array, begin, end);
		  for (int j = begin; j < end; j++)
		  {
			assertNull(array[j]);
			sbyte[] b = selected.Remove(0);
			Assert.IsTrue(allocated.Remove(b));
		  }
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAllocateAndFree()
	  public virtual void TestAllocateAndFree()
	  {
		RecyclingByteBlockAllocator allocator = NewAllocator();
		HashSet<sbyte[]> allocated = new HashSet<sbyte[]>();
		int freeButAllocated = 0;
		sbyte[] block = allocator.ByteBlock;
		allocated.Add(block);
		Assert.IsNotNull(block);
		int size = block.Length;

		int numIters = atLeast(97);
		for (int i = 0; i < numIters; i++)
		{
		  int num = 1 + random().Next(39);
		  for (int j = 0; j < num; j++)
		  {
			block = allocator.ByteBlock;
			freeButAllocated = Math.Max(0, freeButAllocated - 1);
			Assert.IsNotNull(block);
			Assert.AreEqual(size, block.Length);
			Assert.IsTrue("block is returned twice", allocated.Add(block));
			Assert.AreEqual(size * (allocated.Count + allocator.numBufferedBlocks()), allocator.bytesUsed());
		  }

		  sbyte[][] array = allocated.toArray(new sbyte[0][]);
		  int begin = random().Next(array.Length);
		  int end = begin + random().Next(array.Length - begin);
		  for (int j = begin; j < end; j++)
		  {
			sbyte[] b = array[j];
			Assert.IsTrue(allocated.Remove(b));
		  }
		  allocator.recycleByteBlocks(array, begin, end);
		  for (int j = begin; j < end; j++)
		  {
			assertNull(array[j]);
		  }
		  // randomly free blocks
		  int numFreeBlocks = allocator.numBufferedBlocks();
		  int freeBlocks = allocator.freeBlocks(random().Next(7 + allocator.maxBufferedBlocks()));
		  Assert.AreEqual(allocator.numBufferedBlocks(), numFreeBlocks - freeBlocks);
		}
	  }
	}
}