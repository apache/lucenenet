using Lucene.Net.Attributes;
using NUnit.Framework;

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

    /// <summary>
    /// Not completely thorough, but tries to test determinism correctness
    /// somewhat randomly.
    /// </summary>
    [TestFixture]
    public class TestDeterminism : LuceneTestCase
    {
        /// <summary>
        /// test a bunch of random regular expressions </summary>
        [Test]
        public virtual void TestRegexps()
        {
            int num = AtLeast(500);
            for (int i = 0; i < num; i++)
            {
                AssertAutomaton((new RegExp(AutomatonTestUtil.RandomRegexp(Random()), RegExp.NONE)).ToAutomaton());
            }
        }

        /// <summary>
        /// test against a simple, unoptimized det </summary>
        [Test]
        public virtual void TestAgainstSimple()
        {
            int num = AtLeast(200);
            for (int i = 0; i < num; i++)
            {
                Automaton a = AutomatonTestUtil.RandomAutomaton(Random());
                Automaton b = (Automaton)a.Clone();
                AutomatonTestUtil.DeterminizeSimple(a);
                b.Deterministic = false; // force det
                b.Determinize();
                // TODO: more verifications possible?
                Assert.IsTrue(BasicOperations.SameLanguage(a, b));
            }
        }

        private static void AssertAutomaton(Automaton a)
        {
            Automaton clone = (Automaton)a.Clone();
            // complement(complement(a)) = a
            Automaton equivalent = BasicOperations.Complement(BasicOperations.Complement(a));
            Assert.IsTrue(BasicOperations.SameLanguage(a, equivalent));

            // a union a = a
            equivalent = BasicOperations.Union(a, clone);
            Assert.IsTrue(BasicOperations.SameLanguage(a, equivalent));

            // a intersect a = a
            equivalent = BasicOperations.Intersection(a, clone);
            Assert.IsTrue(BasicOperations.SameLanguage(a, equivalent));

            // a minus a = empty
            Automaton empty = BasicOperations.Minus(a, clone);
            Assert.IsTrue(BasicOperations.IsEmpty(empty));

            // as long as don't accept the empty string
            // then optional(a) - empty = a
            if (!BasicOperations.Run(a, ""))
            {
                //System.out.println("test " + a);
                Automaton optional = BasicOperations.Optional(a);
                //System.out.println("optional " + optional);
                equivalent = BasicOperations.Minus(optional, BasicAutomata.MakeEmptyString());
                //System.out.println("equiv " + equivalent);
                Assert.IsTrue(BasicOperations.SameLanguage(a, equivalent));
            }
        }
    }
}