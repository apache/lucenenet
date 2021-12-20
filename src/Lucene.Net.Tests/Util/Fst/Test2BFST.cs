using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Util.Fst
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
    using MMapDirectory = Lucene.Net.Store.MMapDirectory;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;

    //ORIGINAL LINE: @TimeoutSuite(millis = 100 * TimeUnits.HOUR) public class Test2BFST extends Lucene.Net.Util.LuceneTestCase
    [Ignore("Requires tons of heap to run (420G works)")]
    [TestFixture]
    public class Test2BFST : LuceneTestCase
    {
        private const long LIMIT = 3L * 1024 * 1024 * 1024;

        [Test]
        public virtual void Test()
        {
            int[] ints = new int[7];
            Int32sRef input = new Int32sRef(ints, 0, ints.Length);
            long seed = Random.NextInt64();

            Directory dir = new MMapDirectory(CreateTempDir("2BFST"));

            for (int doPackIter = 0; doPackIter < 2; doPackIter++)
            {
                bool doPack = doPackIter == 1;

                // Build FST w/ NoOutputs and stop when nodeCount > 2.2B
                if (!doPack)
                {
                    Console.WriteLine("\nTEST: 3B nodes; doPack=false output=NO_OUTPUTS");
                    Outputs<object> outputs = NoOutputs.Singleton;
                    object NO_OUTPUT = outputs.NoOutput;
                    Builder<object> b = new Builder<object>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs, null, doPack, PackedInt32s.COMPACT, true, 15);

                    int count = 0;
                    Random r = new J2N.Randomizer(seed);
                    int[] ints2 = new int[200];
                    Int32sRef input2 = new Int32sRef(ints2, 0, ints2.Length);
                    while (true)
                    {
                        //System.out.println("add: " + input + " -> " + output);
                        for (int i = 10; i < ints2.Length; i++)
                        {
                            ints2[i] = r.Next(256);
                        }
                        b.Add(input2, NO_OUTPUT);
                        count++;
                        if (count % 100000 == 0)
                        {
                            Console.WriteLine(count + ": " + b.GetFstSizeInBytes() + " bytes; " + b.TotStateCount + " nodes");
                        }
                        if (b.TotStateCount > int.MaxValue + 100L * 1024 * 1024)
                        {
                            break;
                        }
                        NextInput(r, ints2);
                    }

                    FST<object> fst = b.Finish();

                    for (int verify = 0; verify < 2; verify++)
                    {
                        Console.WriteLine("\nTEST: now verify [fst size=" + fst.GetSizeInBytes() + "; nodeCount=" + fst.NodeCount + "; arcCount=" + fst.ArcCount + "]");

                        Arrays.Fill(ints2, 0);
                        r = new J2N.Randomizer(seed);

                        for (int i = 0; i < count; i++)
                        {
                            if (i % 1000000 == 0)
                            {
                                Console.WriteLine(i + "...: ");
                            }
                            for (int j = 10; j < ints2.Length; j++)
                            {
                                ints2[j] = r.Next(256);
                            }
                            Assert.AreEqual(NO_OUTPUT, Util.Get(fst, input2));
                            NextInput(r, ints2);
                        }

                        Console.WriteLine("\nTEST: enum all input/outputs");
                        Int32sRefFSTEnum<object> fstEnum = new Int32sRefFSTEnum<object>(fst);

                        Arrays.Fill(ints2, 0);
                        r = new J2N.Randomizer(seed);
                        int upto = 0;
                        while (fstEnum.MoveNext())
                        {
                            Int32sRefFSTEnum.InputOutput<object> pair = fstEnum.Current;
                            for (int j = 10; j < ints2.Length; j++)
                            {
                                ints2[j] = r.Next(256);
                            }
                            Assert.AreEqual(input2, pair.Input);
                            Assert.AreEqual(NO_OUTPUT, pair.Output);
                            upto++;
                            NextInput(r, ints2);
                        }
                        Assert.AreEqual(count, upto);

                        if (verify == 0)
                        {
                            Console.WriteLine("\nTEST: save/load FST and re-verify");
                            IndexOutput @out = dir.CreateOutput("fst", IOContext.DEFAULT);
                            fst.Save(@out);
                            @out.Dispose();
                            IndexInput @in = dir.OpenInput("fst", IOContext.DEFAULT);
                            fst = new FST<object>(@in, outputs);
                            @in.Dispose();
                        }
                        else
                        {
                            dir.DeleteFile("fst");
                        }
                    }
                }

                // Build FST w/ ByteSequenceOutputs and stop when FST
                // size = 3GB
                {
                    Console.WriteLine("\nTEST: 3 GB size; doPack=" + doPack + " outputs=bytes");
                    Outputs<BytesRef> outputs = ByteSequenceOutputs.Singleton;
                    Builder<BytesRef> b = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs, null, doPack, PackedInt32s.COMPACT, true, 15);

                    var outputBytes = new byte[20];
                    BytesRef output = new BytesRef(outputBytes);
                    Arrays.Fill(ints, 0);
                    int count = 0;
                    Random r = new J2N.Randomizer(seed);
                    while (true)
                    {
                        r.NextBytes(outputBytes);
                        //System.out.println("add: " + input + " -> " + output);
                        b.Add(input, BytesRef.DeepCopyOf(output));
                        count++;
                        if (count % 1000000 == 0)
                        {
                            Console.WriteLine(count + "...: " + b.GetFstSizeInBytes() + " bytes");
                        }
                        if (b.GetFstSizeInBytes() > LIMIT)
                        {
                            break;
                        }
                        NextInput(r, ints);
                    }

                    FST<BytesRef> fst = b.Finish();
                    for (int verify = 0; verify < 2; verify++)
                    {
                        Console.WriteLine("\nTEST: now verify [fst size=" + fst.GetSizeInBytes() + "; nodeCount=" + fst.NodeCount + "; arcCount=" + fst.ArcCount + "]");

                        r = new J2N.Randomizer(seed);
                        Arrays.Fill(ints, 0);

                        for (int i = 0; i < count; i++)
                        {
                            if (i % 1000000 == 0)
                            {
                                Console.WriteLine(i + "...: ");
                            }
                            r.NextBytes(outputBytes);
                            Assert.AreEqual(output, Util.Get(fst, input));
                            NextInput(r, ints);
                        }

                        Console.WriteLine("\nTEST: enum all input/outputs");
                        Int32sRefFSTEnum<BytesRef> fstEnum = new Int32sRefFSTEnum<BytesRef>(fst);

                        Arrays.Fill(ints, 0);
                        r = new J2N.Randomizer(seed);
                        int upto = 0;
                        while (fstEnum.MoveNext())
                        {
                            Int32sRefFSTEnum.InputOutput<BytesRef> pair = fstEnum.Current;
                            Assert.AreEqual(input, pair.Input);
                            r.NextBytes(outputBytes);
                            Assert.AreEqual(output, pair.Output);
                            upto++;
                            NextInput(r, ints);
                        }
                        Assert.AreEqual(count, upto);

                        if (verify == 0)
                        {
                            Console.WriteLine("\nTEST: save/load FST and re-verify");
                            IndexOutput @out = dir.CreateOutput("fst", IOContext.DEFAULT);
                            fst.Save(@out);
                            @out.Dispose();
                            IndexInput @in = dir.OpenInput("fst", IOContext.DEFAULT);
                            fst = new FST<BytesRef>(@in, outputs);
                            @in.Dispose();
                        }
                        else
                        {
                            dir.DeleteFile("fst");
                        }
                    }
                }

                // Build FST w/ PositiveIntOutputs and stop when FST
                // size = 3GB
                {
                    Console.WriteLine("\nTEST: 3 GB size; doPack=" + doPack + " outputs=long");
                    Outputs<Int64> outputs = PositiveInt32Outputs.Singleton;
                    Builder<Int64> b = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs, null, doPack, PackedInt32s.COMPACT, true, 15);

                    long output = 1;

                    Arrays.Fill(ints, 0);
                    int count = 0;
                    Random r = new J2N.Randomizer(seed);
                    while (true)
                    {
                        //System.out.println("add: " + input + " -> " + output);
                        b.Add(input, output);
                        output += 1 + r.Next(10);
                        count++;
                        if (count % 1000000 == 0)
                        {
                            Console.WriteLine(count + "...: " + b.GetFstSizeInBytes() + " bytes");
                        }
                        if (b.GetFstSizeInBytes() > LIMIT)
                        {
                            break;
                        }
                        NextInput(r, ints);
                    }

                    FST<Int64> fst = b.Finish();

                    for (int verify = 0; verify < 2; verify++)
                    {
                        Console.WriteLine("\nTEST: now verify [fst size=" + fst.GetSizeInBytes() + "; nodeCount=" + fst.NodeCount + "; arcCount=" + fst.ArcCount + "]");

                        Arrays.Fill(ints, 0);

                        output = 1;
                        r = new J2N.Randomizer(seed);
                        for (int i = 0; i < count; i++)
                        {
                            if (i % 1000000 == 0)
                            {
                                Console.WriteLine(i + "...: ");
                            }

                            // forward lookup:
                            Assert.AreEqual(output, (long)Util.Get(fst, input));
                            // reverse lookup:
                            Assert.AreEqual(input, Util.GetByOutput(fst, output));
                            output += 1 + r.Next(10);
                            NextInput(r, ints);
                        }

                        Console.WriteLine("\nTEST: enum all input/outputs");
                        Int32sRefFSTEnum<Int64> fstEnum = new Int32sRefFSTEnum<Int64>(fst);

                        Arrays.Fill(ints, 0);
                        r = new J2N.Randomizer(seed);
                        int upto = 0;
                        output = 1;
                        while (fstEnum.MoveNext())
                        {
                            Int32sRefFSTEnum.InputOutput<J2N.Numerics.Int64> pair = fstEnum.Current;
                            Assert.AreEqual(input, pair.Input);
                            Assert.AreEqual(output, pair.Output);
                            output += 1 + r.Next(10);
                            upto++;
                            NextInput(r, ints);
                        }
                        Assert.AreEqual(count, upto);

                        if (verify == 0)
                        {
                            Console.WriteLine("\nTEST: save/load FST and re-verify");
                            IndexOutput @out = dir.CreateOutput("fst", IOContext.DEFAULT);
                            fst.Save(@out);
                            @out.Dispose();
                            IndexInput @in = dir.OpenInput("fst", IOContext.DEFAULT);
                            fst = new FST<Int64>(@in, outputs);
                            @in.Dispose();
                        }
                        else
                        {
                            dir.DeleteFile("fst");
                        }
                    }
                }
            }
            dir.Dispose();
        }

        private void NextInput(Random r, int[] ints)
        {
            int downTo = 6;
            while (downTo >= 0)
            {
                // Must add random amounts (and not just 1) because
                // otherwise FST outsmarts us and remains tiny:
                ints[downTo] += 1 + r.Next(10);
                if (ints[downTo] < 256)
                {
                    break;
                }
                else
                {
                    ints[downTo] = 0;
                    downTo--;
                }
            }
        }
    }
}