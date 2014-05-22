using System.Collections.Generic;

namespace Lucene.Net.Index
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

	using Counter = Lucene.Net.Util.Counter;
	using IntBlockPool = Lucene.Net.Util.IntBlockPool;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

	/// <summary>
	/// tests basic <seealso cref="IntBlockPool"/> functionality
	/// </summary>
	public class TestIntBlockPool : LuceneTestCase
	{

	  public virtual void TestSingleWriterReader()
	  {
		Counter bytesUsed = Counter.newCounter();
		IntBlockPool pool = new IntBlockPool(new ByteTrackingAllocator(bytesUsed));

		for (int j = 0; j < 2; j++)
		{
		  IntBlockPool.SliceWriter writer = new IntBlockPool.SliceWriter(pool);
		  int start = writer.startNewSlice();
		  int num = atLeast(100);
		  for (int i = 0; i < num; i++)
		  {
			writer.writeInt(i);
		  }

		  int upto = writer.CurrentOffset;
		  IntBlockPool.SliceReader reader = new IntBlockPool.SliceReader(pool);
		  reader.reset(start, upto);
		  for (int i = 0; i < num; i++)
		  {
			Assert.AreEqual(i, reader.readInt());
		  }
		  Assert.IsTrue(reader.endOfSlice());
		  if (random().nextBoolean())
		  {
			pool.reset(true, false);
			Assert.AreEqual(0, bytesUsed.get());
		  }
		  else
		  {
			pool.reset(true, true);
			Assert.AreEqual(IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT, bytesUsed.get());
		  }
		}
	  }

	  public virtual void TestMultipleWriterReader()
	  {
		Counter bytesUsed = Counter.newCounter();
		IntBlockPool pool = new IntBlockPool(new ByteTrackingAllocator(bytesUsed));
		for (int j = 0; j < 2; j++)
		{
		  IList<StartEndAndValues> holders = new List<StartEndAndValues>();
		  int num = atLeast(4);
		  for (int i = 0; i < num; i++)
		  {
			holders.Add(new StartEndAndValues(random().Next(1000)));
		  }
		  IntBlockPool.SliceWriter writer = new IntBlockPool.SliceWriter(pool);
		  IntBlockPool.SliceReader reader = new IntBlockPool.SliceReader(pool);

		  int numValuesToWrite = atLeast(10000);
		  for (int i = 0; i < numValuesToWrite; i++)
		  {
			StartEndAndValues values = holders[random().Next(holders.Count)];
			if (values.ValueCount == 0)
			{
			  values.Start = writer.startNewSlice();
			}
			else
			{
			  writer.reset(values.End);
			}
			writer.writeInt(values.NextValue());
			values.End = writer.CurrentOffset;
			if (random().Next(5) == 0)
			{
			  // pick one and reader the ints
			  AssertReader(reader, holders[random().Next(holders.Count)]);
			}
		  }

		  while (holders.Count > 0)
		  {
			StartEndAndValues values = holders.Remove(random().Next(holders.Count));
			AssertReader(reader, values);
		  }
		  if (random().nextBoolean())
		  {
			pool.reset(true, false);
			Assert.AreEqual(0, bytesUsed.get());
		  }
		  else
		  {
			pool.reset(true, true);
			Assert.AreEqual(IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT, bytesUsed.get());
		  }
		}
	  }

	  private class ByteTrackingAllocator : IntBlockPool.Allocator
	  {
		internal readonly Counter BytesUsed;

		public ByteTrackingAllocator(Counter bytesUsed) : this(IntBlockPool.INT_BLOCK_SIZE, bytesUsed)
		{
		}

		public ByteTrackingAllocator(int blockSize, Counter bytesUsed) : base(blockSize)
		{
		  this.BytesUsed = bytesUsed;
		}

		public override int[] IntBlock
		{
			get
			{
			  BytesUsed.addAndGet(blockSize * RamUsageEstimator.NUM_BYTES_INT);
			  return new int[blockSize];
			}
		}

		public override void RecycleIntBlocks(int[][] blocks, int start, int end)
		{
		  BytesUsed.addAndGet(-((end - start) * blockSize * RamUsageEstimator.NUM_BYTES_INT));
		}

	  }

	  private void AssertReader(IntBlockPool.SliceReader reader, StartEndAndValues values)
	  {
		reader.reset(values.Start, values.End);
		for (int i = 0; i < values.ValueCount; i++)
		{
		  Assert.AreEqual(values.ValueOffset + i, reader.readInt());
		}
		Assert.IsTrue(reader.endOfSlice());
	  }

	  private class StartEndAndValues
	  {
		internal int ValueOffset;
		internal int ValueCount;
		internal int Start;
		internal int End;

		public StartEndAndValues(int valueOffset)
		{
		  this.ValueOffset = valueOffset;
		}

		public virtual int NextValue()
		{
		  return ValueOffset + ValueCount++;
		}

	  }

	}

}