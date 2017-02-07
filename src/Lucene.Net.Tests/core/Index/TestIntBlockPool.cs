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
    using Int32BlockPool = Lucene.Net.Util.Int32BlockPool;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// tests basic <seealso cref="Int32BlockPool"/> functionality
    /// </summary>
    [TestFixture]
    public class TestIntBlockPool : LuceneTestCase
    {
        [Test]
        public virtual void TestSingleWriterReader()
        {
            Counter bytesUsed = Util.Counter.NewCounter();
            Int32BlockPool pool = new Int32BlockPool(new ByteTrackingAllocator(bytesUsed));

            for (int j = 0; j < 2; j++)
            {
                Int32BlockPool.SliceWriter writer = new Int32BlockPool.SliceWriter(pool);
                int start = writer.StartNewSlice();
                int num = AtLeast(100);
                for (int i = 0; i < num; i++)
                {
                    writer.WriteInt32(i);
                }

                int upto = writer.CurrentOffset;
                Int32BlockPool.SliceReader reader = new Int32BlockPool.SliceReader(pool);
                reader.Reset(start, upto);
                for (int i = 0; i < num; i++)
                {
                    Assert.AreEqual(i, reader.ReadInt32());
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
                    Assert.AreEqual(Int32BlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT, bytesUsed.Get());
                }
            }
        }

        [Test]
        public virtual void TestMultipleWriterReader()
        {
            Counter bytesUsed = Util.Counter.NewCounter();
            Int32BlockPool pool = new Int32BlockPool(new ByteTrackingAllocator(bytesUsed));
            for (int j = 0; j < 2; j++)
            {
                IList<StartEndAndValues> holders = new List<StartEndAndValues>();
                int num = AtLeast(4);
                for (int i = 0; i < num; i++)
                {
                    holders.Add(new StartEndAndValues(Random().Next(1000)));
                }
                Int32BlockPool.SliceWriter writer = new Int32BlockPool.SliceWriter(pool);
                Int32BlockPool.SliceReader reader = new Int32BlockPool.SliceReader(pool);

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
                    writer.WriteInt32(values.NextValue());
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
                    Assert.AreEqual(Int32BlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT, bytesUsed.Get());
                }
            }
        }

        private class ByteTrackingAllocator : Int32BlockPool.Allocator
        {
            internal readonly Counter BytesUsed;

            public ByteTrackingAllocator(Counter bytesUsed)
                : this(Int32BlockPool.INT_BLOCK_SIZE, bytesUsed)
            {
            }

            public ByteTrackingAllocator(int blockSize, Counter bytesUsed)
                : base(blockSize)
            {
                this.BytesUsed = bytesUsed;
            }

            public override int[] GetInt32Block()
            {
                BytesUsed.AddAndGet(m_blockSize * RamUsageEstimator.NUM_BYTES_INT);
                return new int[m_blockSize];
            }

            public override void RecycleInt32Blocks(int[][] blocks, int start, int end)
            {
                BytesUsed.AddAndGet(-((end - start) * m_blockSize * RamUsageEstimator.NUM_BYTES_INT));
            }
        }

        private void AssertReader(Int32BlockPool.SliceReader reader, StartEndAndValues values)
        {
            reader.Reset(values.Start, values.End);
            for (int i = 0; i < values.ValueCount; i++)
            {
                Assert.AreEqual(values.ValueOffset + i, reader.ReadInt32());
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