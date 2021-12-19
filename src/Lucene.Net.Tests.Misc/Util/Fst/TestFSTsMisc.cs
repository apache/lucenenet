using J2N.Collections.Generic.Extensions;
using Lucene.Net.Store;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;
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
            TestRandomWords(1000, LuceneTestCase.AtLeast(Random, 2));
            //TestRandomWords(100, 1);
        }

        private void TestRandomWords(int maxNumWords, int numIter)
        {
            Random random = new J2N.Randomizer(Random.NextInt64());
            for (int iter = 0; iter < numIter; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter " + iter);
                }
                for (int inputMode = 0; inputMode < 2; inputMode++)
                {
                    int numWords = random.nextInt(maxNumWords + 1);
                    ISet<Int32sRef> termsSet = new JCG.HashSet<Int32sRef>();
                    //Int32sRef[] terms = new Int32sRef[numWords]; // LUCENENET: Not used
                    while (termsSet.size() < numWords)
                    {
                        string term = FSTTester<object>.GetRandomString(random);
                        termsSet.Add(FSTTester<object>.ToInt32sRef(term, inputMode));
                    }
                    DoTest(inputMode, termsSet.ToArray());
                }
            }
        }


        private void DoTest(int inputMode, Int32sRef[] terms)
        {
            Array.Sort(terms);

            // Up to two positive ints, shared, generally but not
            // monotonically increasing
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: now test UpToTwoPositiveIntOutputs");
                }
                UpToTwoPositiveInt64Outputs outputs = UpToTwoPositiveInt64Outputs.GetSingleton(true);
                IList<InputOutput<object>> pairs = new JCG.List<InputOutput<object>>(terms.Length);
                long lastOutput = 0;
                for (int idx = 0; idx < terms.Length; idx++)
                {
                    // Sometimes go backwards
                    long value = lastOutput + TestUtil.NextInt32(Random, -100, 1000);
                    while (value < 0)
                    {
                        value = lastOutput + TestUtil.NextInt32(Random, -100, 1000);
                    }
                    object output;
                    if (Random.nextInt(5) == 3)
                    {
                        long value2 = lastOutput + TestUtil.NextInt32(Random, -100, 1000);
                        while (value2 < 0)
                        {
                            value2 = lastOutput + TestUtil.NextInt32(Random, -100, 1000);
                        }
                        IList<Int64> values = new JCG.List<Int64>();
                        values.Add(value);
                        values.Add(value2);
                        output = values;
                    }
                    else
                    {
                        output = outputs.Get(value);
                    }
                    pairs.Add(new InputOutput<object>(terms[idx], output));
                }
                new FSTTesterHelper<object>(Random, dir, inputMode, pairs, outputs, false).DoTest(false);

                // ListOfOutputs(PositiveIntOutputs), generally but not
                // monotonically increasing
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: now test OneOrMoreOutputs");
                    }
                    PositiveInt32Outputs _outputs = PositiveInt32Outputs.Singleton; 
                    ListOfOutputs<Int64> outputs2 = new ListOfOutputs<Int64>(_outputs);
                    IList<InputOutput<object>> pairs2 = new JCG.List<InputOutput<object>>(terms.Length);
                    long lastOutput2 = 0;
                    for (int idx = 0; idx < terms.Length; idx++)
                    {

                        int outputCount = TestUtil.NextInt32(Random, 1, 7);
                        IList<Int64> values = new JCG.List<Int64>();
                        for (int i = 0; i < outputCount; i++)
                        {
                            // Sometimes go backwards
                            long value = lastOutput2 + TestUtil.NextInt32(Random, -100, 1000);
                            while (value < 0)
                            {
                                value = lastOutput2 + TestUtil.NextInt32(Random, -100, 1000);
                            }
                            values.Add(value);
                            lastOutput2 = value;
                        }

                        object output;
                        if (values.Count == 1)
                        {
                            output = values[0];
                        }
                        else
                        {
                            output = values;
                        }

                        pairs2.Add(new InputOutput<object>(terms[idx], output));
                    }
                    new FSTTester<object>(Random, dir, inputMode, pairs2, outputs2, false).DoTest(false);
                }
            }
        }

        private class FSTTesterHelper<T> : FSTTester<T> where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            public FSTTesterHelper(Random random, Directory dir, int inputMode, IList<InputOutput<T>> pairs, Outputs<T> outputs, bool doReverseLookup)
                : base(random, dir, inputMode, pairs, outputs, doReverseLookup)
            {
            }

            protected override bool OutputsEqual(T output1, T output2)
            {
                if (output1 is UpToTwoPositiveInt64Outputs.TwoInt64s twoLongs1 && output2 is JCG.List<Int64> output2List)
                {
                    return output2List.Equals(new Int64[] { twoLongs1.First, twoLongs1.Second });
                }
                else if (output2 is UpToTwoPositiveInt64Outputs.TwoInt64s twoLongs2 && output1 is JCG.List<Int64> output1List)
                {
                    return output1List.Equals(new Int64[] { twoLongs2.First, twoLongs2.Second });
                }

                return output1.Equals(output2);
            }
        }


        [Test]
        public void TestListOfOutputs()
        {
            PositiveInt32Outputs _outputs = PositiveInt32Outputs.Singleton;
            ListOfOutputs<Int64> outputs = new ListOfOutputs<Int64>(_outputs);
            Builder<object> builder = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);

            Int32sRef scratch = new Int32sRef();
            // Add the same input more than once and the outputs
            // are merged:
            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), (Int64)1L);
            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), (Int64)3L);
            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), (Int64)0L);
            builder.Add(Util.ToInt32sRef(new BytesRef("b"), scratch), (Int64)17L);
            FST<object> fst = builder.Finish();

            object output = Util.Get(fst, new BytesRef("a"));
            assertNotNull(output);
            IList<Int64> outputList = outputs.AsList(output);
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
            PositiveInt32Outputs _outputs = PositiveInt32Outputs.Singleton;
            ListOfOutputs<Int64> outputs = new ListOfOutputs<Int64>(_outputs);
            Builder<object> builder = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);

            Int32sRef scratch = new Int32sRef();
            builder.Add(scratch, (Int64)0L);
            builder.Add(scratch, (Int64)1L);
            builder.Add(scratch, (Int64)17L);
            builder.Add(scratch, (Int64)1L);

            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), (Int64)1L);
            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), (Int64)3L);
            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), (Int64)0L);
            builder.Add(Util.ToInt32sRef(new BytesRef("b"), scratch), (Int64)0L);

            FST<object> fst = builder.Finish();

            object output = Util.Get(fst, new BytesRef(""));
            assertNotNull(output);
            IList<Int64> outputList = outputs.AsList(output);
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
