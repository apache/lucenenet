using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;

namespace Lucene.Net.Test.Util.Automaton
{
    [TestFixture]
    public class TestCompiledAutomaton : LuceneTestCase
    {
        private CompiledAutomaton Build(params string[] strings)
        {
            var terms = strings.Select(s => new BytesRef(s)).ToList();
            terms.Sort();
            //Collections.sort(terms);
            var a = DaciukMihovAutomatonBuilder.Build(terms);
            return new CompiledAutomaton(a, true, false);
        }

        private void TestFloor(CompiledAutomaton c, string input, string expected)
        {
            var b = new BytesRef(input);
            var result = c.Floor(b, b);
            if (expected == null)
            {
                assertNull(result);
            }
            else
            {
                assertNotNull(result);
                assertEquals("actual=" + result.Utf8ToString() + " vs expected=" + expected + " (input=" + input + ")",
                             result, new BytesRef(expected));
            }
        }

        private void TestTerms(string[] terms)
        {
            var c = Build(terms);
            var termBytes = new BytesRef[terms.Length];
            for (var idx = 0; idx < terms.Length; idx++)
            {
                termBytes[idx] = new BytesRef(terms[idx]);
            }
            termBytes.ToList().Sort();
            //Arrays.sort(termBytes);

            if (VERBOSE)
            {
                Console.WriteLine("\nTEST: terms in unicode order");
                foreach (var t in termBytes)
                {
                    Console.WriteLine("  " + t.Utf8ToString());
                }
                //System.out.println(c.utf8.toDot());
            }

            for (var iter = 0; iter < 100 * RANDOM_MULTIPLIER; iter++)
            {
                var s = new Random().Next(10) == 1 ? terms[new Random().Next(terms.Length)] : RandomString();
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: floor(" + s + ")");
                }
                int loc = Arrays.BinarySearch(termBytes, new BytesRef(s));
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
                if (VERBOSE)
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
            var terms = new HashSet<string>();
            while (terms.Count != numTerms)
            {
                terms.Add(RandomString());
            }
            TestTerms(terms.ToArray());
        }

        private string RandomString()
        {
            // return _TestUtil.randomSimpleString(random);
            return _TestUtil.RandomRealisticUnicodeString(new Random());
        }

        [Test]
        public void TestBasic()
        {
            var c = Build("fob", "foo", "goo");
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
