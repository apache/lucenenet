using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RecyclingByteBlockAllocator = Lucene.Net.Util.RecyclingByteBlockAllocator;

    [TestFixture]
    public class TestByteSlices : LuceneTestCase
    {
        [Test]
        public virtual void TestBasic()
        {
            // LUCENENET specific: NUnit will crash with an OOM if we do the full test
            // with verbosity enabled. So, making this a manual setting that can be
            // turned on if, and only if, needed for debugging. If the setting is turned
            // on, we are decresing the number of iterations by 1/3, which seems to
            // keep it from crashing.
            bool isVerbose = false;
            if (!isVerbose)
            {
                Console.WriteLine("Verbosity disabled to keep NUnit from running out of memory - enable manually");
            }

            ByteBlockPool pool = new ByteBlockPool(new RecyclingByteBlockAllocator(ByteBlockPool.BYTE_BLOCK_SIZE, Random.Next(100)));

            int NUM_STREAM = AtLeast(100);

            ByteSliceWriter writer = new ByteSliceWriter(pool);

            int[] starts = new int[NUM_STREAM];
            int[] uptos = new int[NUM_STREAM];
            int[] counters = new int[NUM_STREAM];

            ByteSliceReader reader = new ByteSliceReader();

            for (int ti = 0; ti < 100; ti++)
            {
                for (int stream = 0; stream < NUM_STREAM; stream++)
                {
                    starts[stream] = -1;
                    counters[stream] = 0;
                }

                // LUCENENET NOTE: Since upgrading to NUnit 3, this test
                // will crash if VERBOSE is true because of an OutOfMemoryException.
                // This not only keeps this test from finishing, it crashes NUnit
                // and no other tests will run.
                // So, we need to allocate a smaller size to ensure this
                // doesn't happen with verbosity enabled.
                int num = isVerbose ? AtLeast(2000) : AtLeast(3000);
                for (int iter = 0; iter < num; iter++)
                {
                    int stream;
                    if (Random.NextBoolean())
                    {
                        stream = Random.Next(3);
                    }
                    else
                    {
                        stream = Random.Next(NUM_STREAM);
                    }

                    if (isVerbose)
                    {
                        Console.WriteLine("write stream=" + stream);
                    }

                    if (starts[stream] == -1)
                    {
                        int spot = pool.NewSlice(ByteBlockPool.FIRST_LEVEL_SIZE);
                        starts[stream] = uptos[stream] = spot + pool.ByteOffset;
                        if (isVerbose)
                        {
                            Console.WriteLine("  init to " + starts[stream]);
                        }
                    }

                    writer.Init(uptos[stream]);
                    int numValue;
                    if (Random.Next(10) == 3)
                    {
                        numValue = Random.Next(100);
                    }
                    else if (Random.Next(5) == 3)
                    {
                        numValue = Random.Next(3);
                    }
                    else
                    {
                        numValue = Random.Next(20);
                    }

                    for (int j = 0; j < numValue; j++)
                    {
                        if (isVerbose)
                        {
                            Console.WriteLine("    write " + (counters[stream] + j));
                        }
                        // write some large (incl. negative) ints:
                        writer.WriteVInt32(Random.Next());
                        writer.WriteVInt32(counters[stream] + j);
                    }
                    counters[stream] += numValue;
                    uptos[stream] = writer.Address;
                    if (isVerbose)
                    {
                        Console.WriteLine("    addr now " + uptos[stream]);
                    }
                }

                for (int stream = 0; stream < NUM_STREAM; stream++)
                {
                    if (isVerbose)
                    {
                        Console.WriteLine("  stream=" + stream + " count=" + counters[stream]);
                    }

                    if (starts[stream] != -1 && starts[stream] != uptos[stream])
                    {
                        reader.Init(pool, starts[stream], uptos[stream]);
                        for (int j = 0; j < counters[stream]; j++)
                        {
                            reader.ReadVInt32();
                            Assert.AreEqual(j, reader.ReadVInt32());
                        }
                    }
                }

                pool.Reset();
            }
        }
    }
}