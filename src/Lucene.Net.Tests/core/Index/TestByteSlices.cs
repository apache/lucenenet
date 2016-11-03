using System;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;

    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RecyclingByteBlockAllocator = Lucene.Net.Util.RecyclingByteBlockAllocator;

    /// <summary>
    /// Licensed under the Apache License, Version 2.0 (the "License");
    /// you may not use this file except in compliance with the License.
    /// You may obtain a copy of the License at
    ///
    ///     http://www.apache.org/licenses/LICENSE-2.0
    ///
    /// Unless required by applicable law or agreed to in writing, software
    /// distributed under the License is distributed on an "AS IS" BASIS,
    /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specific language governing permissions and
    /// limitations under the License.
    /// </summary>
    [TestFixture]
    public class TestByteSlices : LuceneTestCase
    {
        [Test, MaxTime(300000)]
        public virtual void TestBasic()
        {
            ByteBlockPool pool = new ByteBlockPool(new RecyclingByteBlockAllocator(ByteBlockPool.BYTE_BLOCK_SIZE, Random().Next(100)));

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

                int num = AtLeast(3000);
                for (int iter = 0; iter < num; iter++)
                {
                    int stream;
                    if (Random().NextBoolean())
                    {
                        stream = Random().Next(3);
                    }
                    else
                    {
                        stream = Random().Next(NUM_STREAM);
                    }

                    if (VERBOSE)
                    {
                        Console.WriteLine("write stream=" + stream);
                    }

                    if (starts[stream] == -1)
                    {
                        int spot = pool.NewSlice(ByteBlockPool.FIRST_LEVEL_SIZE);
                        starts[stream] = uptos[stream] = spot + pool.ByteOffset;
                        if (VERBOSE)
                        {
                            Console.WriteLine("  init to " + starts[stream]);
                        }
                    }

                    writer.Init(uptos[stream]);
                    int numValue;
                    if (Random().Next(10) == 3)
                    {
                        numValue = Random().Next(100);
                    }
                    else if (Random().Next(5) == 3)
                    {
                        numValue = Random().Next(3);
                    }
                    else
                    {
                        numValue = Random().Next(20);
                    }

                    for (int j = 0; j < numValue; j++)
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("    write " + (counters[stream] + j));
                        }
                        // write some large (incl. negative) ints:
                        writer.WriteVInt(Random().Next());
                        writer.WriteVInt(counters[stream] + j);
                    }
                    counters[stream] += numValue;
                    uptos[stream] = writer.Address;
                    if (VERBOSE)
                    {
                        Console.WriteLine("    addr now " + uptos[stream]);
                    }
                }

                for (int stream = 0; stream < NUM_STREAM; stream++)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  stream=" + stream + " count=" + counters[stream]);
                    }

                    if (starts[stream] != -1 && starts[stream] != uptos[stream])
                    {
                        reader.Init(pool, starts[stream], uptos[stream]);
                        for (int j = 0; j < counters[stream]; j++)
                        {
                            reader.ReadVInt();
                            Assert.AreEqual(j, reader.ReadVInt());
                        }
                    }
                }

                pool.Reset();
            }
        }
    }
}