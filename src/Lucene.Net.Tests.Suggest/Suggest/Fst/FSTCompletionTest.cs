using J2N.Collections;
using J2N.Text;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Suggest.Fst
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

    public class FSTCompletionTest : LuceneTestCase
    {

        public static Input Tf(string t, int v)
        {
            return new Input(t, v);
        }

        private FSTCompletion completion;
        private FSTCompletion completionAlphabetical;

        public override void SetUp()
        {
            base.SetUp();

            FSTCompletionBuilder builder = new FSTCompletionBuilder();
            foreach (Input tf in EvalKeys())
            {
                builder.Add(tf.term, (int)tf.v);
            }
            completion = builder.Build();
            completionAlphabetical = new FSTCompletion(completion.FST, false, true);
        }

        private Input[] EvalKeys()
        {
            Input[] keys = new Input[] {
                Tf("one", 0),
                Tf("oneness", 1),
                Tf("onerous", 1),
                Tf("onesimus", 1),
                Tf("two", 1),
                Tf("twofold", 1),
                Tf("twonk", 1),
                Tf("thrive", 1),
                Tf("through", 1),
                Tf("threat", 1),
                Tf("three", 1),
                Tf("foundation", 1),
                Tf("fourblah", 1),
                Tf("fourteen", 1),
                Tf("four", 0),
                Tf("fourier", 0),
                Tf("fourty", 0),
                Tf("xo", 1),
            };
            return keys;
        }

        [Test]
        public void TestExactMatchHighPriority()
        {
            AssertMatchEquals(completion.DoLookup(StringToCharSequence("two").ToString(), 1),
                "two/1.0");
        }

        [Test]
        public void TestExactMatchLowPriority()
        {
            AssertMatchEquals(completion.DoLookup(StringToCharSequence("one").ToString(), 2),
                "one/0.0",
                "oneness/1.0");
        }

        [Test]
        public void TestExactMatchReordering()
        {
            // Check reordering of exact matches. 
            AssertMatchEquals(completion.DoLookup(StringToCharSequence("four").ToString(), 4),
                "four/0.0",
                "fourblah/1.0",
                "fourteen/1.0",
                "fourier/0.0");
        }

        [Test]
        public void TestRequestedCount()
        {
            // 'one' is promoted after collecting two higher ranking results.
            AssertMatchEquals(completion.DoLookup(StringToCharSequence("one").ToString(), 2),
                "one/0.0",
                "oneness/1.0");

            // 'four' is collected in a bucket and then again as an exact match. 
            AssertMatchEquals(completion.DoLookup(StringToCharSequence("four").ToString(), 2),
                "four/0.0",
                "fourblah/1.0");

            // Check reordering of exact matches. 
            AssertMatchEquals(completion.DoLookup(StringToCharSequence("four").ToString(), 4),
                "four/0.0",
                "fourblah/1.0",
                "fourteen/1.0",
                "fourier/0.0");

            // 'one' is at the top after collecting all alphabetical results.
            AssertMatchEquals(completionAlphabetical.DoLookup(StringToCharSequence("one").ToString(), 2),
                "one/0.0",
                "oneness/1.0");

            // 'one' is not promoted after collecting two higher ranking results.
            FSTCompletion noPromotion = new FSTCompletion(completion.FST, true, false);
            AssertMatchEquals(noPromotion.DoLookup(StringToCharSequence("one").ToString(), 2),
                "oneness/1.0",
                "onerous/1.0");

            // 'one' is at the top after collecting all alphabetical results. 
            AssertMatchEquals(completionAlphabetical.DoLookup(StringToCharSequence("one").ToString(), 2),
                "one/0.0",
                "oneness/1.0");
        }

        [Test]
        public void TestMiss()
        {
            AssertMatchEquals(completion.DoLookup(StringToCharSequence("xyz").ToString(), 1));
        }

        [Test]
        public void TestAlphabeticWithWeights()
        {
            assertEquals(0, completionAlphabetical.DoLookup(StringToCharSequence("xyz").ToString(), 1).size());
        }

        [Test]
        public void TestFullMatchList()
        {
            AssertMatchEquals(completion.DoLookup(StringToCharSequence("one").ToString(), int.MaxValue),
                "oneness/1.0",
                "onerous/1.0",
                "onesimus/1.0",
                "one/0.0");
        }

        [Test]
        public void TestThreeByte()
        {
            //string key = new string(new sbyte[] {
            //    (sbyte) 0xF0, (sbyte) 0xA4, (sbyte) 0xAD, (sbyte) 0xA2}, 0, 4, Encoding.UTF8);
            string key = Encoding.UTF8.GetString(new byte[] { 0xF0, 0xA4, 0xAD, 0xA2 });
            FSTCompletionBuilder builder = new FSTCompletionBuilder();
            builder.Add(new BytesRef(key), 0);

            FSTCompletion lookup = builder.Build();
            IList<FSTCompletion.Completion> result = lookup.DoLookup(StringToCharSequence(key).ToString(), 1);
            assertEquals(1, result.Count);
        }

        [Test]
        public void TestLargeInputConstantWeights()
        {
            FSTCompletionLookup lookup = new FSTCompletionLookup(10, true);

            Random r = Random;
            IList<Input> keys = new JCG.List<Input>();
            for (int i = 0; i < 5000; i++)
            {
                keys.Add(new Input(TestUtil.RandomSimpleString(r), -1));
            }

            lookup.Build(new InputArrayEnumerator(keys));

            // All the weights were constant, so all returned buckets must be constant, whatever they
            // are.
            long? previous = null;
            foreach (Input tf in keys)
            {
                long? current = lookup.Get(TestUtil.BytesToCharSequence(tf.term, Random).ToString());
                if (previous != null)
                {
                    assertEquals(previous, current);
                }
                previous = current;
            }
        }

        [Test]
        public void TestMultilingualInput()
        {
            IList<Input> input = LookupBenchmarkTest.ReadTop50KWiki();

            FSTCompletionLookup lookup = new FSTCompletionLookup();
            lookup.Build(new InputArrayEnumerator(input));
            assertEquals(input.size(), lookup.Count);
            foreach (Input tf in input)
            {
                assertNotNull("Not found: " + tf.term.toString(), lookup.Get(TestUtil.BytesToCharSequence(tf.term, Random).ToString()));
                assertEquals(tf.term.Utf8ToString(), lookup.DoLookup(TestUtil.BytesToCharSequence(tf.term, Random).ToString(), true, 1)[0].Key.toString());
            }

            IList<Lookup.LookupResult> result = lookup.DoLookup(StringToCharSequence("wit").ToString(), true, 5);
            assertEquals(5, result.size());
            assertTrue(result[0].Key.toString().Equals("wit", StringComparison.Ordinal));  // exact match.
            assertTrue(result[1].Key.toString().Equals("with", StringComparison.Ordinal)); // highest count.
        }

        [Test]
        public void TestEmptyInput()
        {
            completion = new FSTCompletionBuilder().Build();
            AssertMatchEquals(completion.DoLookup(StringToCharSequence("").ToString(), 10));
        }

        [Test]
        public void TestRandom()
        {
            JCG.List<Input> freqs = new JCG.List<Input>();
            Random rnd = Random;
            for (int i = 0; i < 2500 + rnd.nextInt(2500); i++)
            {
                int weight = rnd.nextInt(100);
                freqs.Add(new Input("" + rnd.Next(), weight));
            }

            FSTCompletionLookup lookup = new FSTCompletionLookup();
            lookup.Build(new InputArrayEnumerator(freqs.ToArray()));

            foreach (Input tf in freqs)
            {
                string term = tf.term.Utf8ToString();
                for (int i = 1; i < term.Length; i++)
                {
                    String prefix = term.Substring(0, i - 0);
                    foreach (Lookup.LookupResult lr in lookup.DoLookup(StringToCharSequence(prefix).ToString(), true, 10))
                    {
                        assertTrue(lr.Key.toString().StartsWith(prefix, StringComparison.Ordinal));
                    }
                }
            }
        }

        private ICharSequence StringToCharSequence(string prefix)
        {
            return TestUtil.StringToCharSequence(prefix, Random);
        }

        private void AssertMatchEquals(IList<FSTCompletion.Completion> res, params string[] expected)
        {
            string[] result = new string[res.Count];
            for (int i = 0; i < res.Count; i++)
            {
                result[i] = res[i].ToString();
            }

            if (!ArrayEqualityComparer<string>.OneDimensional.Equals(StripScore(expected), StripScore(result)))
            {
                int colLen = Math.Max(MaxLen(expected), MaxLen(result));

                StringBuilder b = new StringBuilder();
                string format = "{0," + colLen + "}  {1," + colLen + "}\n";
                b.Append(string.Format(CultureInfo.InvariantCulture, format, "Expected", "Result"));
                for (int i = 0; i < Math.Max(result.Length, expected.Length); i++)
                {
                    b.Append(string.Format(CultureInfo.InvariantCulture, format,
                        i < expected.Length ? expected[i] : "--",
                        i < result.Length ? result[i] : "--"));
                }

                Console.WriteLine(b.ToString());
                fail("Expected different output:\n" + b.ToString());
            }
        }

        private static readonly Regex Score = new Regex("\\/[0-9\\.]+", RegexOptions.Compiled);

        private string[] StripScore(string[] expected)
        {
            string[] result = new string[expected.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Score.Replace(expected[i], string.Empty);
            }
            return result;
        }

        private int MaxLen(string[] result)
        {
            int len = 0;
            foreach (string s in result)
                len = Math.Max(len, s.Length);
            return len;
        }
    }
}
