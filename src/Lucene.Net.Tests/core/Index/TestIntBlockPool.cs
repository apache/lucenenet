using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;

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
    [TestFixture]
    public class TestIntBlockPool : LuceneTestCase
    {
        [Test]
        public virtual void TestSingleWriterReader()
        {
            Counter bytesUsed = Util.Counter.NewCounter();
            IntBlockPool pool = new IntBlockPool(new ByteTrackingAllocator(bytesUsed));

            for (int j = 0; j < 2; j++)
            {
                IntBlockPool.SliceWriter writer = new IntBlockPool.SliceWriter(pool);
                int start = writer.StartNewSlice();
                int num = AtLeast(100);
                for (int i = 0; i < num; i++)
                {
                    writer.WriteInt(i);
                }

                int upto = writer.CurrentOffset;
                IntBlockPool.SliceReader reader = new IntBlockPool.SliceReader(pool);
                reader.Reset(start, upto);
                for (int i = 0; i < num; i++)
                {
                    Assert.AreEqual(i, reader.ReadInt());
                }
                Assert.IsTrue(reader.EndOfSlice());
                if (Random().NextBoolean())
                {
                    pool.Reset(true, false);
                    Assert.AreEqual(0, bytesUsed.Get());
                }
                else
                {
                    pool.Reset(true, true);
                    Assert.AreEqual(IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT, bytesUsed.Get());
                }
            }
        }

        [Test]
        public virtual void TestMultipleWriterReader()
        {
            Counter bytesUsed = Util.Counter.NewCounter();
            IntBlockPool pool = new IntBlockPool(new ByteTrackingAllocator(bytesUsed));
            for (int j = 0; j < 2; j++)
            {
                IList<StartEndAndValues> holders = new List<StartEndAndValues>();
                int num = AtLeast(4);
                for (int i = 0; i < num; i++)
                {
                    holders.Add(new StartEndAndValues(Random().Next(1000)));
                }
                IntBlockPool.SliceWriter writer = new IntBlockPool.SliceWriter(pool);
                IntBlockPool.SliceReader reader = new IntBlockPool.SliceReader(pool);

                int numValuesToWrite = AtLeast(10000);
                for (int i = 0; i < numValuesToWrite; i++)
                {
                    StartEndAndValues values = holders[Random().Next(holders.Count)];
                    if (values.ValueCount == 0)
                    {
                        values.Start = writer.StartNewSlice();
                    }
                    else
                    {
                        writer.Reset(values.End);
                    }
                    writer.WriteInt(values.NextValue());
                    values.End = writer.CurrentOffset;
                    if (Random().Next(5) == 0)
                    {
                        // pick one and reader the ints
                        AssertReader(reader, holders[Random().Next(holders.Count)]);
                    }
                }

                while (holders.Count > 0)
                {
                    int randIndex = Random().Next(holders.Count);
                    StartEndAndValues values = holders[randIndex];
                    holders.RemoveAt(randIndex);
                    AssertReader(reader, values);
                }
                if (Random().NextBoolean())
                {
                    pool.Reset(true, false);
                    Assert.AreEqual(0, bytesUsed.Get());
                }
                else
                {
                    pool.Reset(true, true);
                    Assert.AreEqual(IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT, bytesUsed.Get());
                }
            }
        }

        private class ByteTrackingAllocator : IntBlockPool.Allocator
        {
            internal readonly Counter BytesUsed;

            public ByteTrackingAllocator(Counter bytesUsed)
                : this(IntBlockPool.INT_BLOCK_SIZE, bytesUsed)
            {
            }

            public ByteTrackingAllocator(int blockSize, Counter bytesUsed)
                : base(blockSize)
            {
                this.BytesUsed = bytesUsed;
            }

            public override int[] GetIntBlock()
            {
                BytesUsed.AddAndGet(BlockSize * RamUsageEstimator.NUM_BYTES_INT);
                return new int[BlockSize];
            }

            public override void RecycleIntBlocks(int[][] blocks, int start, int end)
            {
                BytesUsed.AddAndGet(-((end - start) * BlockSize * RamUsageEstimator.NUM_BYTES_INT));
            }
        }

        private void AssertReader(IntBlockPool.SliceReader reader, StartEndAndValues values)
        {
            reader.Reset(values.Start, values.End);
            for (int i = 0; i < values.ValueCount; i++)
            {
                Assert.AreEqual(values.ValueOffset + i, reader.ReadInt());
            }
            Assert.IsTrue(reader.EndOfSlice());
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