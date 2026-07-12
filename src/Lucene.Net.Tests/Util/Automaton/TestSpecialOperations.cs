using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
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

    using Util = Lucene.Net.Util.Fst.Util;

    [TestFixture]
    public class TestSpecialOperations : LuceneTestCase
    {
        /// <summary>
        /// tests against the original brics implementation.
        /// </summary>
        [Test]
        public virtual void TestIsFinite()
        {
            int num = AtLeast(200);
            for (int i = 0; i < num; i++)
            {
                Automaton a = AutomatonTestUtil.RandomAutomaton(Random);
                Automaton b = (Automaton)a.Clone();
                Assert.AreEqual(AutomatonTestUtil.IsFiniteSlow(a), SpecialOperations.IsFinite(b));
            }
        }

        /// <summary>
        /// Pass false for testRecursive if the expected strings
        /// may be too long
        /// </summary>
        private static ISet<Int32sRef> GetFiniteStrings(Automaton a, int limit, bool testRecursive) // LUCENENET: made static
        {
            ISet<Int32sRef> result = SpecialOperations.GetFiniteStrings(a, limit);
            if (testRecursive)
            {
                Assert.AreEqual(AutomatonTestUtil.GetFiniteStringsRecursive(a, limit), result);
            }
            return result;
        }

        /// <summary>
        /// Basic test for getFiniteStrings
        /// </summary>
        [Test]
        public virtual void TestFiniteStringsBasic()
        {
            Automaton a = BasicOperations.Union(BasicAutomata.MakeString("dog"), BasicAutomata.MakeString("duck"));
            MinimizationOperations.Minimize(a);
            ISet<Int32sRef> strings = GetFiniteStrings(a, -1, true);
            Assert.AreEqual(2, strings.Count);
            Int32sRef dog = new Int32sRef();
            Util.ToInt32sRef(new BytesRef("dog"), dog);
            Assert.IsTrue(strings.Contains(dog));
            Int32sRef duck = new Int32sRef();
            Util.ToInt32sRef(new BytesRef("duck"), duck);
            Assert.IsTrue(strings.Contains(duck));
        }

        [Test]
        public void TestFiniteStringsEatsStack()
        {
            char[] chars = new char[50000];
            TestUtil.RandomFixedLengthUnicodeString(Random, chars, 0, chars.Length);
            string bigString1 = new string(chars);
            TestUtil.RandomFixedLengthUnicodeString(Random, chars, 0, chars.Length);
            string bigString2 = new string(chars);
            Automaton a = BasicOperations.Union(BasicAutomata.MakeString(bigString1), BasicAutomata.MakeString(bigString2));
            ISet<Int32sRef> strings = GetFiniteStrings(a, -1, false);
            Assert.AreEqual(2, strings.Count);
            Int32sRef scratch = new Int32sRef();
            Util.ToUTF32(bigString1.ToCharArray(), 0, bigString1.Length, scratch);
            Assert.IsTrue(strings.Contains(scratch));
            Util.ToUTF32(bigString2.ToCharArray(), 0, bigString2.Length, scratch);
            Assert.IsTrue(strings.Contains(scratch));
        }

        [Test]
        public void TestRandomFiniteStrings1()
        {
            int numStrings = AtLeast(500);
            if (Verbose)
            {
                Console.WriteLine($"TEST: numStrings={numStrings}");
            }

            ISet<Int32sRef> strings = new JCG.HashSet<Int32sRef>();
            IList<Automaton> automata = new List<Automaton>();
            for (int i = 0; i < numStrings; i++)
            {
                string s = TestUtil.RandomSimpleString(Random, 1, 200);
                automata.Add(BasicAutomata.MakeString(s));
                Int32sRef scratch = new Int32sRef();
                Util.ToUTF32(s.ToCharArray(), 0, s.Length, scratch);
                strings.Add(scratch);
                if (Verbose)
                {
                    Console.WriteLine($"  add string={s}");
                }
            }

            // TODO: we could sometimes use
            // DaciukMihovAutomatonBuilder here

            // TODO: what other random things can we do here...
            Automaton a = BasicOperations.Union(automata);
            if (Random.NextBoolean())
            {
                Automaton.Minimize(a);
                if (Verbose)
                {
                    Console.WriteLine($"TEST: a.minimize numStates={a.GetNumberOfStates()}");
                }
            }
            else if (Random.NextBoolean())
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: a.determinize");
                }
                a.Determinize();
            }
            else if (Random.NextBoolean())
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: a.reduce");
                }
                a.Reduce();
            }
            else if (Random.NextBoolean())
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: a.getNumberedStates");
                }
                a.GetNumberedStates();
            }

            ISet<Int32sRef> actual = GetFiniteStrings(a, -1, true);
            if (!strings.Equals(actual))
            {
                Console.WriteLine($"strings.size()={strings.Count} actual.size={actual.Count}");
                IList<Int32sRef> x = new List<Int32sRef>(strings);
                x.Sort();
                IList<Int32sRef> y = new List<Int32sRef>(actual);
                y.Sort();
                int end = Math.Min(x.Count, y.Count);
                for (int i = 0; i < end; i++)
                {
                    Console.WriteLine($"  i={i} string={ToString(x[i])} actual={ToString(y[i])}");
                }
                Assert.Fail("wrong strings found");
            }
        }

        // ascii only!
        private static string ToString(Int32sRef ints)
        {
            BytesRef br = new BytesRef(ints.Length);
            for (int i = 0; i < ints.Length; i++)
            {
                br.Bytes[i] = (byte) ints.Int32s[i];
            }
            br.Length = ints.Length;
            return br.Utf8ToString();
        }

        [Test]
        public void TestWithCycle()
        {
            try
            {
                SpecialOperations.GetFiniteStrings(new RegExp("abc.*", RegExpSyntax.NONE).ToAutomaton(), -1);
                Assert.Fail("did not hit exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
        }

        [Test]
        public void TestRandomFiniteStrings2()
        {
            // Just makes sure we can run on any random finite
            // automaton:
            int iters = AtLeast(100);
            for (int i = 0; i < iters; i++)
            {
                Automaton a = AutomatonTestUtil.RandomAutomaton(Random);
                try
                {
                    // Must pass a limit because the random automaton
                    // can accept MANY strings:
                    SpecialOperations.GetFiniteStrings(a, TestUtil.NextInt32(Random, 1, 1000));
                    // NOTE: cannot do this, because the method is not
                    // guaranteed to detect cycles when you have a limit
                    //assertTrue(SpecialOperations.isFinite(a));
                }
                catch (Exception iae) when (iae.IsIllegalArgumentException())
                {
                    Assert.IsFalse(SpecialOperations.IsFinite(a));
                }
            }
        }

        [Test]
        public void TestInvalidLimit()
        {
            Automaton a = AutomatonTestUtil.RandomAutomaton(Random);
            try
            {
                SpecialOperations.GetFiniteStrings(a, -7);
                Assert.Fail("did not hit exception");
            }
            catch (ArgumentOutOfRangeException /*iae*/) // LUCENENET-specific AOORE
            {
                // expected
            }
        }

        [Test]
        public void TestInvalidLimit2()
        {
            Automaton a = AutomatonTestUtil.RandomAutomaton(Random);
            try
            {
                SpecialOperations.GetFiniteStrings(a, 0);
                Assert.Fail("did not hit exception");
            }
            catch (ArgumentOutOfRangeException /*iae*/) // LUCENENET-specific AOORE
            {
                // expected
            }
        }

        [Test]
        public void TestSingletonNoLimit()
        {
            ISet<Int32sRef> result = SpecialOperations.GetFiniteStrings(BasicAutomata.MakeString("foobar"), -1);
            Assert.AreEqual(1, result.Count);
            Int32sRef scratch = new Int32sRef();
            Util.ToUTF32("foobar".ToCharArray(), 0, 6, scratch);
            Assert.IsTrue(result.Contains(scratch));
        }

        [Test]
        public void TestSingletonLimit1()
        {
            ISet<Int32sRef> result = SpecialOperations.GetFiniteStrings(BasicAutomata.MakeString("foobar"), 1);
            Assert.AreEqual(1, result.Count);
            Int32sRef scratch = new Int32sRef();
            Util.ToUTF32("foobar".ToCharArray(), 0, 6, scratch);
            Assert.IsTrue(result.Contains(scratch));
        }
    }
}
