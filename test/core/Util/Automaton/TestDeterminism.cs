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
                AssertAutomaton((new RegExp(AutomatonTestUtil.randomRegexp(Random()), RegExp.NONE)).ToAutomaton());
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
                Automaton b = (Automaton) a.Clone();
                AutomatonTestUtil.DeterminizeSimple(a);
                b.Deterministic = false; // force det
                b.Determinize();
                // TODO: more verifications possible?
                Assert.IsTrue(BasicOperations.sameLanguage(a, b));
            }
        }

        private static void AssertAutomaton(Automaton a)
        {
            Automaton clone = a.Clone();
            // complement(complement(a)) = a
            Automaton equivalent = BasicOperations.complement(BasicOperations.complement(a));
            Assert.IsTrue(BasicOperations.sameLanguage(a, equivalent));

            // a union a = a
            equivalent = BasicOperations.union(a, clone);
            Assert.IsTrue(BasicOperations.sameLanguage(a, equivalent));

            // a intersect a = a
            equivalent = BasicOperations.intersection(a, clone);
            Assert.IsTrue(BasicOperations.sameLanguage(a, equivalent));

            // a minus a = empty
            Automaton empty = BasicOperations.minus(a, clone);
            Assert.IsTrue(BasicOperations.isEmpty(empty));

            // as long as don't accept the empty string
            // then optional(a) - empty = a
            if (!BasicOperations.run(a, ""))
            {
                //System.out.println("test " + a);
                Automaton optional = BasicOperations.optional(a);
                //System.out.println("optional " + optional);
                equivalent = BasicOperations.minus(optional, BasicAutomata.makeEmptyString());
                //System.out.println("equiv " + equivalent);
                Assert.IsTrue(BasicOperations.sameLanguage(a, equivalent));
            }
        }
    }

}