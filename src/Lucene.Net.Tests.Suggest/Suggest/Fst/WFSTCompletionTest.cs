using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public class WFSTCompletionTest : LuceneTestCase
    {
        [Test]
        public void TestBasic()
        {
            Input[] keys = new Input[] {
                new Input("foo", 50),
                new Input("bar", 10),
                new Input("barbar", 12),
                new Input("barbara", 6)
            };

            Random random = new Random(Random().Next());
            WFSTCompletionLookup suggester = new WFSTCompletionLookup();
            suggester.Build(new InputArrayIterator(keys));

            // top N of 2, but only foo is available
            IList<Lookup.LookupResult> results = suggester.DoLookup(TestUtil.StringToCharSequence("f", random).ToString(), false, 2);
            assertEquals(1, results.size());
            assertEquals("foo", results[0].key.toString());
            assertEquals(50, results[0].value, 0.01F);

            // make sure we don't get a dup exact suggestion:
            results = suggester.DoLookup(TestUtil.StringToCharSequence("foo", random).ToString(), false, 2);
            assertEquals(1, results.size());
            assertEquals("foo", results[0].key.toString());
            assertEquals(50, results[0].value, 0.01F);

            // top N of 1 for 'bar': we return this even though barbar is higher
            results = suggester.DoLookup(TestUtil.StringToCharSequence("bar", random).ToString(), false, 1);
            assertEquals(1, results.size());
            assertEquals("bar", results[0].key.toString());
            assertEquals(10, results[0].value, 0.01F);

            // top N Of 2 for 'b'
            results = suggester.DoLookup(TestUtil.StringToCharSequence("b", random).ToString(), false, 2);
            assertEquals(2, results.size());
            assertEquals("barbar", results[0].key.toString());
            assertEquals(12, results[0].value, 0.01F);
            assertEquals("bar", results[1].key.toString());
            assertEquals(10, results[1].value, 0.01F);

            // top N of 3 for 'ba'
            results = suggester.DoLookup(TestUtil.StringToCharSequence("ba", random).ToString(), false, 3);
            assertEquals(3, results.size());
            assertEquals("barbar", results[0].key.toString());
            assertEquals(12, results[0].value, 0.01F);
            assertEquals("bar", results[1].key.toString());
            assertEquals(10, results[1].value, 0.01F);
            assertEquals("barbara", results[2].key.toString());
            assertEquals(6, results[2].value, 0.01F);
        }

        [Test]
        public void TestExactFirst()
        {

            WFSTCompletionLookup suggester = new WFSTCompletionLookup(true);

            suggester.Build(new InputArrayIterator(new Input[] {
                new Input("x y", 20),
                new Input("x", 2),
            }));

            for (int topN = 1; topN < 4; topN++)
            {
                IList<Lookup.LookupResult> results = suggester.DoLookup("x", false, topN);

                assertEquals(Math.Min(topN, 2), results.size());

                assertEquals("x", results[0].key);
                assertEquals(2, results[0].value);

                if (topN > 1)
                {
                    assertEquals("x y", results[1].key);
                    assertEquals(20, results[1].value);
                }
            }
        }

        [Test]
        public void TestNonExactFirst()
        {

            WFSTCompletionLookup suggester = new WFSTCompletionLookup(false);

            suggester.Build(new InputArrayIterator(new Input[] {
                new Input("x y", 20),
                new Input("x", 2),
            }));

            for (int topN = 1; topN < 4; topN++)
            {
                IList<Lookup.LookupResult> results = suggester.DoLookup("x", false, topN);

                assertEquals(Math.Min(topN, 2), results.size());

                assertEquals("x y", results[0].key);
                assertEquals(20, results[0].value);

                if (topN > 1)
                {
                    assertEquals("x", results[1].key);
                    assertEquals(2, results[1].value);
                }
            }
        }

        [Test]
        public void TestRandom()
        {
            int numWords = AtLeast(1000);

            IDictionary<string, long> slowCompletor = new SortedDictionary<string, long>(); //new TreeMap<>();
            ISet<string> allPrefixes = new SortedSet<string>(); //new TreeSet<>();

            Input[] keys = new Input[numWords];

            for (int i = 0; i < numWords; i++)
            {
                String s;
                while (true)
                {
                    // TODO: would be nice to fix this slowCompletor/comparer to
                    // use full range, but we might lose some coverage too...
                    s = TestUtil.RandomSimpleString(Random());
                    if (!slowCompletor.ContainsKey(s))
                    {
                        break;
                    }
                }

                for (int j = 1; j < s.Length; j++)
                {
                    allPrefixes.add(s.Substring(0, j));
                }
                // we can probably do Integer.MAX_VALUE here, but why worry.
                int weight = Random().nextInt(1 << 24);
                slowCompletor.Put(s, (long)weight);
                keys[i] = new Input(s, weight);
            }

            WFSTCompletionLookup suggester = new WFSTCompletionLookup(false);
            suggester.Build(new InputArrayIterator(keys));

            assertEquals(numWords, suggester.Count);
            Random random = new Random(Random().Next());
            foreach (String prefix in allPrefixes)
            {
                int topN = TestUtil.NextInt(random, 1, 10);
                IList<Lookup.LookupResult> r = suggester.DoLookup(TestUtil.StringToCharSequence(prefix, random).ToString(), false, topN);

                // 2. go thru whole treemap (slowCompletor) and check its actually the best suggestion
                List<Lookup.LookupResult> matches = new List<Lookup.LookupResult>();

                // TODO: could be faster... but its slowCompletor for a reason
                foreach (KeyValuePair<string, long> e in slowCompletor)
                {
                    if (e.Key.StartsWith(prefix))
                    {
                        matches.Add(new Lookup.LookupResult(e.Key, e.Value));
                    }
                }

                assertTrue(matches.size() > 0);
                matches.Sort(new TestRandomComparer());

                if (matches.size() > topN)
                {
                    //matches.SubList(topN, matches.size()).clear();
                    matches.RemoveRange(topN, matches.size() - topN);
                }

                assertEquals(matches.size(), r.size());

                for (int hit = 0; hit < r.size(); hit++)
                {
                    //System.out.println("  check hit " + hit);
                    assertEquals(matches[hit].key.toString(), r[hit].key.toString());
                    assertEquals(matches[hit].value, r[hit].value, 0f);
                }
            }
        }

        internal class TestRandomComparer : IComparer<Lookup.LookupResult>
        {
            public int Compare(Lookup.LookupResult left, Lookup.LookupResult right)
            {
                int cmp = ((float)right.value).CompareTo((float)left.value);
                if (cmp == 0)
                {
                    return left.CompareTo(right);
                }
                else
                {
                    return cmp;
                }
            }
        }

        [Test]
        public void Test0ByteKeys()
        {
            BytesRef key1 = new BytesRef(4);
            key1.Length = 4;
            BytesRef key2 = new BytesRef(3);
            key1.Length = 3;

            WFSTCompletionLookup suggester = new WFSTCompletionLookup(false);

            suggester.Build(new InputArrayIterator(new Input[] {
                new Input(key1, 50),
                new Input(key2, 50),
            }));
        }

        [Test]
        public void TestEmpty()
        {
            WFSTCompletionLookup suggester = new WFSTCompletionLookup(false);

            suggester.Build(new InputArrayIterator(new Input[0]));
            assertEquals(0, suggester.Count);
            IList<Lookup.LookupResult> result = suggester.DoLookup("a", false, 20);
            assertTrue(!result.Any());
        }
    }
}
