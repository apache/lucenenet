using Lucene.Net.Store;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Util.Fst
{
    public class TestFSTsMisc : LuceneTestCase
    {
        private MockDirectoryWrapper dir;

        public override void SetUp()
        {
            base.SetUp();
            dir = NewMockDirectory();
            dir.PreventDoubleWrite = false;
        }

        public override void TearDown()
        {
            // can be null if we force simpletext (funky, some kind of bug in test runner maybe)
            if (dir != null) dir.Dispose();
            base.TearDown();
        }

        [Test]
        public void TestRandomWords()
        {
            TestRandomWords(1000, LuceneTestCase.AtLeast(Random(), 2));
            //TestRandomWords(100, 1);
        }

        private void TestRandomWords(int maxNumWords, int numIter)
        {
            Random random = new Random(Random().Next());
            for (int iter = 0; iter < numIter; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter " + iter);
                }
                for (int inputMode = 0; inputMode < 2; inputMode++)
                {
                    int numWords = random.nextInt(maxNumWords + 1);
                    ISet<IntsRef> termsSet = new HashSet<IntsRef>();
                    IntsRef[] terms = new IntsRef[numWords];
                    while (termsSet.size() < numWords)
                    {
                        string term = FSTTester<object>.GetRandomString(random);
                        termsSet.Add(FSTTester<object>.ToIntsRef(term, inputMode));
                    }
                    DoTest(inputMode, termsSet.ToArray());
                }
            }
        }


        private void DoTest(int inputMode, IntsRef[] terms)
        {
            Array.Sort(terms);

            // Up to two positive ints, shared, generally but not
            // monotonically increasing
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: now test UpToTwoPositiveIntOutputs");
                }
                UpToTwoPositiveIntOutputs outputs = UpToTwoPositiveIntOutputs.GetSingleton(true);
                List<Lucene.Net.Util.Fst.FSTTester<object>.InputOutput<object>> pairs = new List<Lucene.Net.Util.Fst.FSTTester<object>.InputOutput<object>>(terms.Length);
                long lastOutput = 0;
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    // Sometimes go backwards
                    long value = lastOutput + TestUtil.NextInt(Random(), -100, 1000);
                    while (value < 0)
                    {
                        value = lastOutput + TestUtil.NextInt(Random(), -100, 1000);
                    }
                    object output;
                    if (Random().nextInt(5) == 3)
                    {
                        long value2 = lastOutput + TestUtil.NextInt(Random(), -100, 1000);
                        while (value2 < 0)
                        {
                            value2 = lastOutput + TestUtil.NextInt(Random(), -100, 1000);
                        }
                        List<long> values = new List<long>();
                        values.Add(value);
                        values.Add(value2);
                        output = values;
                    }
                    else
                    {
                        output = outputs.Get(value);
                    }
                    pairs.Add(new FSTTester<object>.InputOutput<object>(terms[idx], output));
                }
                new FSTTesterHelper<object>(Random(), dir, inputMode, pairs, outputs, false).DoTest(false);

                // ListOfOutputs(PositiveIntOutputs), generally but not
                // monotonically increasing
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: now test OneOrMoreOutputs");
                    }
                    PositiveIntOutputs _outputs = PositiveIntOutputs.Singleton; // LUCENENET TODO: This should probably not be a nullable type
                    ListOfOutputs<long?> outputs2 = new ListOfOutputs<long?>(_outputs);
                    List<FSTTester<object>.InputOutput<object>> pairs2 = new List<FSTTester<object>.InputOutput<object>>(terms.Length);
                    long lastOutput2 = 0;
                    for (int idx = 0; idx < terms.Length; idx++)
                    {

                        int outputCount = TestUtil.NextInt(Random(), 1, 7);
                        List<long?> values = new List<long?>();
                        for (int i = 0; i < outputCount; i++)
                        {
                            // Sometimes go backwards
                            long value = lastOutput2 + TestUtil.NextInt(Random(), -100, 1000);
                            while (value < 0)
                            {
                                value = lastOutput2 + TestUtil.NextInt(Random(), -100, 1000);
                            }
                            values.Add(value);
                            lastOutput2 = value;
                        }

                        object output;
                        if (values.size() == 1)
                        {
                            output = values[0];
                        }
                        else
                        {
                            output = values;
                        }

                        pairs2.Add(new FSTTester<object>.InputOutput<object>(terms[idx], output));
                    }
                    new FSTTester<object>(Random(), dir, inputMode, pairs2, outputs2, false).DoTest(false);
                }
            }
        }

        private class FSTTesterHelper<T> : FSTTester<T>
        {
            public FSTTesterHelper(Random random, Directory dir, int inputMode, List<InputOutput<T>> pairs, Outputs<T> outputs, bool doReverseLookup)
                : base(random, dir, inputMode, pairs, outputs, doReverseLookup)
            {
            }

            protected internal override bool OutputsEqual(T output1, T output2)
            {
                if (output1 is UpToTwoPositiveIntOutputs.TwoLongs && output2 is IEnumerable<long>)
                {
                    UpToTwoPositiveIntOutputs.TwoLongs twoLongs1 = output1 as UpToTwoPositiveIntOutputs.TwoLongs;
                    long[] list2 = (output2 as IEnumerable<long>).ToArray();
                    return (new long[] { twoLongs1.First, twoLongs1.Second }).SequenceEqual(list2);
                }
                else if (output2 is UpToTwoPositiveIntOutputs.TwoLongs && output1 is IEnumerable<long>)
                {
                    long[] list1 = (output1 as IEnumerable<long>).ToArray();
                    UpToTwoPositiveIntOutputs.TwoLongs twoLongs2 = output2 as UpToTwoPositiveIntOutputs.TwoLongs;
                    return (new long[] { twoLongs2.First, twoLongs2.Second }).SequenceEqual(list1);
                }

                return output1.Equals(output2);
            }
        }


        [Test]
        public void TestListOfOutputs()
        {
            PositiveIntOutputs _outputs = PositiveIntOutputs.Singleton;
            ListOfOutputs<long?> outputs = new ListOfOutputs<long?>(_outputs);
            Builder<object> builder = new Builder<object>(Lucene.Net.Util.Fst.FST.INPUT_TYPE.BYTE1, outputs);

            IntsRef scratch = new IntsRef();
            // Add the same input more than once and the outputs
            // are merged:
            builder.Add(Util.ToIntsRef(new BytesRef("a"), scratch), 1L);
            builder.Add(Util.ToIntsRef(new BytesRef("a"), scratch), 3L);
            builder.Add(Util.ToIntsRef(new BytesRef("a"), scratch), 0L);
            builder.Add(Util.ToIntsRef(new BytesRef("b"), scratch), 17L);
            FST<object> fst = builder.Finish();

            object output = Util.Get(fst, new BytesRef("a"));
            assertNotNull(output);
            IList<long?> outputList = outputs.AsList(output);
            assertEquals(3, outputList.size());
            assertEquals(1L, outputList[0]);
            assertEquals(3L, outputList[1]);
            assertEquals(0L, outputList[2]);

            output = Util.Get(fst, new BytesRef("b"));
            assertNotNull(output);
            outputList = outputs.AsList(output);
            assertEquals(1, outputList.size());
            assertEquals(17L, outputList[0]);
        }

        [Test]
        public void TestListOfOutputsEmptyString()
        {
            PositiveIntOutputs _outputs = PositiveIntOutputs.Singleton;
            ListOfOutputs<long?> outputs = new ListOfOutputs<long?>(_outputs);
            Builder<object> builder = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);

            IntsRef scratch = new IntsRef();
            builder.Add(scratch, 0L);
            builder.Add(scratch, 1L);
            builder.Add(scratch, 17L);
            builder.Add(scratch, 1L);

            builder.Add(Util.ToIntsRef(new BytesRef("a"), scratch), 1L);
            builder.Add(Util.ToIntsRef(new BytesRef("a"), scratch), 3L);
            builder.Add(Util.ToIntsRef(new BytesRef("a"), scratch), 0L);
            builder.Add(Util.ToIntsRef(new BytesRef("b"), scratch), 0L);

            FST<object> fst = builder.Finish();

            object output = Util.Get(fst, new BytesRef(""));
            assertNotNull(output);
            IList<long?> outputList = outputs.AsList(output);
            assertEquals(4, outputList.size());
            assertEquals(0L, outputList[0]);
            assertEquals(1L, outputList[1]);
            assertEquals(17L, outputList[2]);
            assertEquals(1L, outputList[3]);

            output = Util.Get(fst, new BytesRef("a"));
            assertNotNull(output);
            outputList = outputs.AsList(output);
            assertEquals(3, outputList.size());
            assertEquals(1L, outputList[0]);
            assertEquals(3L, outputList[1]);
            assertEquals(0L, outputList[2]);

            output = Util.Get(fst, new BytesRef("b"));
            assertNotNull(output);
            outputList = outputs.AsList(output);
            assertEquals(1, outputList.size());
            assertEquals(0L, outputList[0]);
        }
    }
}
