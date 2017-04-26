/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

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
                    ISet<Int32sRef> termsSet = new HashSet<Int32sRef>();
                    Int32sRef[] terms = new Int32sRef[numWords];
                    while (termsSet.size() < numWords)
                    {
                        string term = FSTTester<object>.GetRandomString(random);
                        termsSet.Add(FSTTester<object>.ToIntsRef(term, inputMode));
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
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: now test UpToTwoPositiveIntOutputs");
                }
                UpToTwoPositiveInt64Outputs outputs = UpToTwoPositiveInt64Outputs.GetSingleton(true);
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
                    PositiveInt32Outputs _outputs = PositiveInt32Outputs.Singleton; 
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
                if (output1 is UpToTwoPositiveInt64Outputs.TwoInt64s && output2 is IEnumerable<long>)
                {
                    UpToTwoPositiveInt64Outputs.TwoInt64s twoLongs1 = output1 as UpToTwoPositiveInt64Outputs.TwoInt64s;
                    long[] list2 = (output2 as IEnumerable<long>).ToArray();
                    return (new long[] { twoLongs1.First, twoLongs1.Second }).SequenceEqual(list2);
                }
                else if (output2 is UpToTwoPositiveInt64Outputs.TwoInt64s && output1 is IEnumerable<long>)
                {
                    long[] list1 = (output1 as IEnumerable<long>).ToArray();
                    UpToTwoPositiveInt64Outputs.TwoInt64s twoLongs2 = output2 as UpToTwoPositiveInt64Outputs.TwoInt64s;
                    return (new long[] { twoLongs2.First, twoLongs2.Second }).SequenceEqual(list1);
                }

                return output1.Equals(output2);
            }
        }


        [Test]
        public void TestListOfOutputs()
        {
            PositiveInt32Outputs _outputs = PositiveInt32Outputs.Singleton;
            ListOfOutputs<long?> outputs = new ListOfOutputs<long?>(_outputs);
            Builder<object> builder = new Builder<object>(Lucene.Net.Util.Fst.FST.INPUT_TYPE.BYTE1, outputs);

            Int32sRef scratch = new Int32sRef();
            // Add the same input more than once and the outputs
            // are merged:
            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), 1L);
            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), 3L);
            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), 0L);
            builder.Add(Util.ToInt32sRef(new BytesRef("b"), scratch), 17L);
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
            PositiveInt32Outputs _outputs = PositiveInt32Outputs.Singleton;
            ListOfOutputs<long?> outputs = new ListOfOutputs<long?>(_outputs);
            Builder<object> builder = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);

            Int32sRef scratch = new Int32sRef();
            builder.Add(scratch, 0L);
            builder.Add(scratch, 1L);
            builder.Add(scratch, 17L);
            builder.Add(scratch, 1L);

            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), 1L);
            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), 3L);
            builder.Add(Util.ToInt32sRef(new BytesRef("a"), scratch), 0L);
            builder.Add(Util.ToInt32sRef(new BytesRef("b"), scratch), 0L);

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
