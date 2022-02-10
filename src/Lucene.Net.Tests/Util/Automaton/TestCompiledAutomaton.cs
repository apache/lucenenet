using J2N.Collections.Generic.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util.Automaton
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

    [TestFixture]
    public class TestCompiledAutomaton : LuceneTestCase
    {
        private CompiledAutomaton Build(params string[] strings)
        {
            IList<BytesRef> terms = new JCG.List<BytesRef>();
            foreach (string s in strings)
            {
                terms.Add(new BytesRef(s));
            }
            terms.Sort();
            Automaton a = DaciukMihovAutomatonBuilder.Build(terms);
            return new CompiledAutomaton(a, true, false);
        }

        private void TestFloor(CompiledAutomaton c, string input, string expected)
        {
            BytesRef b = new BytesRef(input);
            BytesRef result = c.Floor(b, b);
            if (expected is null)
            {
                Assert.IsNull(result);
            }
            else
            {
                Assert.IsNotNull(result);
                Assert.AreEqual(result, new BytesRef(expected), "actual=" + result.Utf8ToString() + " vs expected=" + expected + " (input=" + input + ")");
            }
        }

        private void TestTerms(string[] terms)
        {
            CompiledAutomaton c = Build(terms);
            BytesRef[] termBytes = new BytesRef[terms.Length];
            for (int idx = 0; idx < terms.Length; idx++)
            {
                termBytes[idx] = new BytesRef(terms[idx]);
            }
            Array.Sort(termBytes);

            if (Verbose)
            {
                Console.WriteLine("\nTEST: terms in unicode order");
                foreach (BytesRef t in termBytes)
                {
                    Console.WriteLine("  " + t.Utf8ToString());
                }
                //System.out.println(c.utf8.toDot());
            }

            for (int iter = 0; iter < 100 * RandomMultiplier; iter++)
            {
                string s = Random.Next(10) == 1 ? terms[Random.Next(terms.Length)] : RandomString();
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: floor(" + s + ")");
                }
                int loc = Array.BinarySearch(termBytes, new BytesRef(s));
                string expected;
                if (loc >= 0)
                {
                    expected = s;
                }
                else
                {
                    // term doesn't exist
                    loc = -(loc + 1);
                    if (loc == 0)
                    {
                        expected = null;
                    }
                    else
                    {
                        expected = termBytes[loc - 1].Utf8ToString();
                    }
                }
                if (Verbose)
                {
                    Console.WriteLine("  expected=" + expected);
                }
                TestFloor(c, s, expected);
            }
        }

        [Test]
        public void TestRandom()
        {
            int numTerms = AtLeast(400);
            if (Verbose)
            {
                Console.WriteLine("Testing with {0} terms", numTerms);
            }

            ISet<string> terms = new JCG.HashSet<string>();
            while (terms.Count < numTerms)
            {
                terms.Add(RandomString());
            }
            TestTerms(terms.ToArray());
        }

        private string RandomString()
        {
            // return TestUtil.randomSimpleString(random);
            return TestUtil.RandomRealisticUnicodeString(Random);
        }

        [Test]
        public virtual void TestBasic()
        {
            CompiledAutomaton c = Build("fob", "foo", "goo");
            TestFloor(c, "goo", "goo");
            TestFloor(c, "ga", "foo");
            TestFloor(c, "g", "foo");
            TestFloor(c, "foc", "fob");
            TestFloor(c, "foz", "foo");
            TestFloor(c, "f", null);
            TestFloor(c, "", null);
            TestFloor(c, "aa", null);
            TestFloor(c, "zzz", "goo");
        }
    }
}