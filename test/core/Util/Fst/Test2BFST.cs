using System;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Test.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;
using NUnit.Framework;

namespace Lucene.Net.Test.Util.Fst
{
    [TestFixture]
    [Ignore("Requires tons of heap to run (10G works)")]
    [Timeout(360000000)] // @TimeoutSuite(millis = 100 * TimeUnits.HOUR)
    public class Test2BFST : LuceneTestCase
    {
        private static long LIMIT = 3L * 1024 * 1024 * 1024;

        [Test]
        public void Test()
        {
            var ints = new int[7];
            var input = new IntsRef(ints, 0, ints.Length);
            var seed = new Random().NextLong();

            Directory dir = new MMapDirectory(_TestUtil.GetTempDir("2BFST"));

            for (var doPackIter = 0; doPackIter < 2; doPackIter++)
            {
                var doPack = doPackIter == 1;

                // Build FST w/ NoOutputs and stop when nodeCount > 3B
                if (!doPack)
                {
                    Console.WriteLine("\nTEST: 3B nodes; doPack=false output=NO_OUTPUTS");
                    Outputs<object> outputs = NoOutputs.GetSingleton();
                    var NO_OUTPUT = outputs.GetNoOutput();
                    var b = new Builder<object>(FST.INPUT_TYPE.BYTE1, 0, 0, false, false, int.MaxValue, outputs,
                                                                  null, doPack, PackedInts.COMPACT, true, 15);

                    var count = 0;
                    var r = new Random((int)seed);
                    var ints2 = new int[200];
                    var input2 = new IntsRefs(ints2, 0, ints2.Length);
                    while (true)
                    {
                        //Console.WriteLine("add: " + input + " -> " + output);
                        for (var i = 10; i < ints2.Length; i++)
                        {
                            ints2[i] = r.Next(256);
                        }
                        b.Add(input2, NO_OUTPUT);
                        count++;
                        if (count % 100000 == 0)
                        {
                            Console.WriteLine(count + ": " + b.FstSizeInBytes() + " bytes; " + b.TotStateCount + " nodes");
                        }
                        if (b.TotStateCount > LIMIT)
                        {
                            break;
                        }
                        NextInput(r, ints2);
                    }

                    var fst = b.Finish();

                    for (var verify = 0; verify < 2; verify++)
                    {
                        Console.WriteLine("\nTEST: now verify [fst size=" + fst.SizeInBytes() + "; nodeCount=" + fst.NodeCount + "; arcCount=" + fst.ArcCount + "]");

                        Arrays.Fill(ints2, 0);
                        r = new Random((int)seed);

                        for (var i = 0; i < count; i++)
                        {
                            if (i % 1000000 == 0)
                            {
                                Console.WriteLine(i + "...: ");
                            }
                            for (int j = 10; j < ints2.Length; j++)
                            {
                                ints2[j] = r.Next(256);
                            }
                            assertEquals(NO_OUTPUT, Util.get(fst, input2));
                            NextInput(r, ints2);
                        }

                        Console.WriteLine("\nTEST: enum all input/outputs");
                        var fstEnum = new IntsRefFSTEnum<object>(fst);

                        Arrays.Fill(ints2, 0);
                        r = new Random((int)seed);
                        var upto = 0;
                        while (true)
                        {
                            var pair = fstEnum.Next();
                            if (pair == null)
                            {
                                break;
                            }
                            for (int j = 10; j < ints2.Length; j++)
                            {
                                ints2[j] = r.Next(256);
                            }
                            assertEquals(input2, pair.Input);
                            assertEquals(NO_OUTPUT, pair.Output);
                            upto++;
                            NextInput(r, ints2);
                        }
                        assertEquals(count, upto);

                        if (verify == 0)
                        {
                            Console.WriteLine("\nTEST: save/load FST and re-verify");
                            var output = dir.CreateOutput("fst", IOContext.DEFAULT);
                            fst.Save(output);
                            output.Dispose();
                            var input3 = dir.OpenInput("fst", IOContext.DEFAULT);
                            fst = new FST<object>(input3, outputs);
                            input3.Dispose();
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
                    Outputs<BytesRef> outputs = ByteSequenceOutputs.GetSingleton();
                    var b = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs,
                                                                      null, doPack, PackedInts.COMPACT, true, 15);

                    var outputBytes = new byte[20];
                    var output = new BytesRef(outputBytes);
                    Arrays.Fill(ints, 0);
                    var count = 0;
                    var r = new Random(seed);
                    while (true)
                    {
                        r.NextBytes(outputBytes);
                        //Console.WriteLine("add: " + input + " -> " + output);
                        b.Add(input, BytesRef.DeepCopyOf(output));
                        count++;
                        if (count % 1000000 == 0)
                        {
                            Console.WriteLine(count + "...: " + b.FstSizeInBytes() + " bytes");
                        }
                        if (b.FstSizeInBytes() > LIMIT)
                        {
                            break;
                        }
                        NextInput(r, ints);
                    }

                    FST<BytesRef> fst = b.Finish();
                    for (int verify = 0; verify < 2; verify++)
                    {

                        Console.WriteLine("\nTEST: now verify [fst size=" + fst.SizeInBytes() + "; nodeCount=" + fst.NodeCount + "; arcCount=" + fst.ArcCount + "]");

                        r = new Random(seed);
                        Arrays.Fill(ints, 0);

                        for (int i = 0; i < count; i++)
                        {
                            if (i % 1000000 == 0)
                            {
                                Console.WriteLine(i + "...: ");
                            }
                            r.NextBytes(outputBytes);
                            assertEquals(output, Util.get(fst, input));
                            NextInput(r, ints);
                        }

                        Console.WriteLine("\nTEST: enum all input/outputs");
                        var fstEnum = new IntsRefFSTEnum<BytesRef>(fst);

                        Arrays.Fill(ints, 0);
                        r = new Random((int)seed);
                        int upto = 0;
                        while (true)
                        {
                            var pair = fstEnum.Next();
                            if (pair == null)
                            {
                                break;
                            }
                            assertEquals(input, pair.Input);
                            r.NextBytes(outputBytes);
                            assertEquals(output, pair.Output);
                            upto++;
                            NextInput(r, ints);
                        }
                        assertEquals(count, upto);

                        if (verify == 0)
                        {
                            Console.WriteLine("\nTEST: save/load FST and re-verify");
                            var output2 = dir.CreateOutput("fst", IOContext.DEFAULT);
                            fst.Save(output2);
                            output2.Dispose();
                            var input4 = dir.OpenInput("fst", IOContext.DEFAULT);
                            fst = new FST<BytesRef>(input4, outputs);
                            input4.Dispose();
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
                    Outputs<long> outputs = PositiveIntOutputs.GetSingleton();
                    Builder<long> b = new Builder<long>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs,
                                                              null, doPack, PackedInts.COMPACT, true, 15);

                    long output = 1;

                    Arrays.Fill(ints, 0);
                    var count = 0;
                    var r = new Random(seed);
                    while (true)
                    {
                        //Console.WriteLine("add: " + input + " -> " + output);
                        b.Add(input, output);
                        output += 1 + r.Next(10);
                        count++;
                        if (count % 1000000 == 0)
                        {
                            Console.WriteLine(count + "...: " + b.FstSizeInBytes() + " bytes");
                        }
                        if (b.FstSizeInBytes() > LIMIT)
                        {
                            break;
                        }
                        NextInput(r, ints);
                    }

                    FST<long> fst = b.Finish();

                    for (int verify = 0; verify < 2; verify++)
                    {

                        Console.WriteLine("\nTEST: now verify [fst size=" + fst.SizeInBytes() + "; nodeCount=" + fst.NodeCount + "; arcCount=" + fst.ArcCount + "]");

                        Arrays.Fill(ints, 0);

                        output = 1;
                        r = new Random(seed);
                        for (int i = 0; i < count; i++)
                        {
                            if (i % 1000000 == 0)
                            {
                                Console.WriteLine(i + "...: ");
                            }

                            // forward lookup:
                            assertEquals(output, Util.get(fst, input).longValue());
                            // reverse lookup:
                            assertEquals(input, Util.getByOutput(fst, output));
                            output += 1 + r.Next(10);
                            NextInput(r, ints);
                        }

                        Console.WriteLine("\nTEST: enum all input/outputs");
                        IntsRefFSTEnum<long> fstEnum = new IntsRefFSTEnum<long>(fst);

                        Arrays.Fill(ints, 0);
                        r = new Random((int)seed);
                        int upto = 0;
                        output = 1;
                        while (true)
                        {
                            var pair = fstEnum.Next();
                            if (pair == null)
                            {
                                break;
                            }
                            assertEquals(input, pair.Input);
                            assertEquals(output, pair.Output);
                            output += 1 + r.Next(10);
                            upto++;
                            NextInput(r, ints);
                        }
                        assertEquals(count, upto);

                        if (verify == 0)
                        {
                            Console.WriteLine("\nTEST: save/load FST and re-verify");
                            var output3 = dir.CreateOutput("fst", IOContext.DEFAULT);
                            fst.Save(output3);
                            output3.Dispose();
                            var input5 = dir.OpenInput("fst", IOContext.DEFAULT);
                            fst = new FST<long>(input5, outputs);
                            input5.Dispose();
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
            var downTo = 6;
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
