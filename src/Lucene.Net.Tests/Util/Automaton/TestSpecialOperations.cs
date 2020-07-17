using NUnit.Framework;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

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
        /// Basic test for getFiniteStrings
        /// </summary>
        [Test]
        public virtual void TestFiniteStrings()
        {
            Automaton a = BasicOperations.Union(BasicAutomata.MakeString("dog"), BasicAutomata.MakeString("duck"));
            MinimizationOperations.Minimize(a);
            ISet<Int32sRef> strings = SpecialOperations.GetFiniteStrings(a, -1);
            Assert.AreEqual(2, strings.Count);
            Int32sRef dog = new Int32sRef();
            Util.ToInt32sRef(new BytesRef("dog"), dog);
            Assert.IsTrue(strings.Contains(dog));
            Int32sRef duck = new Int32sRef();
            Util.ToInt32sRef(new BytesRef("duck"), duck);
            Assert.IsTrue(strings.Contains(duck));
        }
    }
}