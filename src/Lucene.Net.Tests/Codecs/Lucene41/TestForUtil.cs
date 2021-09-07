using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
using Assert = Lucene.Net.TestFramework.Assert;
using RandomInts = RandomizedTesting.Generators.RandomNumbers;

namespace Lucene.Net.Codecs.Lucene41
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

    using Directory = Lucene.Net.Store.Directory;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;

    [TestFixture]
    public class TestForUtil : LuceneTestCase
    {
        [Test]
        public virtual void TestEncodeDecode()
        {
            int iterations = RandomInts.RandomInt32Between(Random, 1, 1000);
            float AcceptableOverheadRatio = (float)Random.NextDouble();
            int[] values = new int[(iterations - 1) * Lucene41PostingsFormat.BLOCK_SIZE + ForUtil.MAX_DATA_SIZE];
            for (int i = 0; i < iterations; ++i)
            {
                int bpv = Random.Next(32);
                if (bpv == 0)
                {
                    int value = RandomInts.RandomInt32Between(Random, 0, int.MaxValue);
                    for (int j = 0; j < Lucene41PostingsFormat.BLOCK_SIZE; ++j)
                    {
                        values[i * Lucene41PostingsFormat.BLOCK_SIZE + j] = value;
                    }
                }
                else
                {
                    for (int j = 0; j < Lucene41PostingsFormat.BLOCK_SIZE; ++j)
                    {
                        values[i * Lucene41PostingsFormat.BLOCK_SIZE + j] = RandomInts.RandomInt32Between(Random, 0, (int)PackedInt32s.MaxValue(bpv));
                    }
                }
            }

            Directory d = new RAMDirectory();
            long endPointer;

            {
                // encode
                IndexOutput @out = d.CreateOutput("test.bin", IOContext.DEFAULT);
                ForUtil forUtil = new ForUtil(AcceptableOverheadRatio, @out);

                for (int i = 0; i < iterations; ++i)
                {
                    forUtil.WriteBlock(Arrays.CopyOfRange(values, i * Lucene41PostingsFormat.BLOCK_SIZE, values.Length), new byte[Lucene41.ForUtil.MAX_ENCODED_SIZE], @out);
                }
                endPointer = @out.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                @out.Dispose();
            }

            {
                // decode
                IndexInput @in = d.OpenInput("test.bin", IOContext.READ_ONCE);
                ForUtil forUtil = new ForUtil(@in);
                for (int i = 0; i < iterations; ++i)
                {
                    if (Random.NextBoolean())
                    {
                        forUtil.SkipBlock(@in);
                        continue;
                    }
                    int[] restored = new int[Lucene41.ForUtil.MAX_DATA_SIZE];
                    forUtil.ReadBlock(@in, new byte[Lucene41.ForUtil.MAX_ENCODED_SIZE], restored);
                    Assert.AreEqual(Arrays.CopyOfRange(values, i * Lucene41PostingsFormat.BLOCK_SIZE, (i + 1) * Lucene41PostingsFormat.BLOCK_SIZE), Arrays.CopyOf(restored, Lucene41PostingsFormat.BLOCK_SIZE));
                }
                assertEquals(endPointer, @in.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                @in.Dispose();
            }
        }
    }
}