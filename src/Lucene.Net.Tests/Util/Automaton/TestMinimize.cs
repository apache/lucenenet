using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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
    /// this test builds some randomish NFA/DFA and minimizes them.
    /// </summary>
    [TestFixture]
    public class TestMinimize : LuceneTestCase
    {
        /// <summary>
        /// the minimal and non-minimal are compared to ensure they are the same. </summary>
        [Test]
        public virtual void Test()
        {
            int num = AtLeast(200);
            for (int i = 0; i < num; i++)
            {
                Automaton a = AutomatonTestUtil.RandomAutomaton(Random);
                Automaton b = (Automaton)a.Clone();
                MinimizationOperations.Minimize(b);
                Assert.IsTrue(BasicOperations.SameLanguage(a, b));
            }
        }

        /// <summary>
        /// compare minimized against minimized with a slower, simple impl.
        /// we check not only that they are the same, but that #states/#transitions
        /// are the same.
        /// </summary>
        [Test]
        public virtual void TestAgainstBrzozowski()
        {
            int num = AtLeast(200);
            for (int i = 0; i < num; i++)
            {
                Automaton a = AutomatonTestUtil.RandomAutomaton(Random);
                AutomatonTestUtil.MinimizeSimple(a);
                Automaton b = (Automaton)a.Clone();
                MinimizationOperations.Minimize(b);
                Assert.IsTrue(BasicOperations.SameLanguage(a, b));
                Assert.AreEqual(a.GetNumberOfStates(), b.GetNumberOfStates());
                Assert.AreEqual(a.GetNumberOfTransitions(), b.GetNumberOfTransitions());
            }
        }

        // LUCENENET TODO:
        //
        // The initialstate after MinimizeSimple is always 0
        // in Lucene.NET. However, it doesn't seem to be
        // causing any issues with other tests failing.

        // HERE IS THE RESULT OF THE FOLLOWING TEST IN JAVA

        //Before MinimizeSimple: initial state: 0
        //state 0 [accept]:
        //  S -> 1
        //  * -> 2
        //  \\U00005dbb -> 2
        //  \\U000a9c44 -> 3
        //state 1 [reject]:
        //  \\U00000000-\\U0010ffff -> 4
        //state 2 [accept]:
        //state 3 [reject]:
        //  \\U000001d9 -> 5
        //state 4 [reject]:
        //  ] -> 6
        //  \\U0000e38e -> 7
        //state 5 [reject]:
        //  \\U00000000-\\U0010ffff -> 8
        //state 6 [reject]:
        //  \\U0000e38e -> 7
        //state 7 [reject]:
        //  \\U00021180 -> 9
        //state 8 [reject]:
        //  ] -> 10
        //state 9 [reject]:
        //  + -> 11
        //state 10 [accept]:
        //  \\U000761ca -> 2
        //state 11 [reject]:
        //  \\U0000f34c -> 12
        //state 12 [reject]:
        //  ] -> 13
        //state 13 [reject]:
        //  ] -> 14
        //state 14 [accept]:
        //  ] -> 14

        //After MinimizeSimple: initial state: 12
        //state 0 [reject]:
        //  \\U00000000-\\U0010ffff -> 5
        //state 1 [reject]:
        //  \\U000001d9 -> 6
        //state 2 [accept]:
        //state 3 [reject]:
        //  \\U0000e38e -> 4
        //state 4 [reject]:
        //  \\U00021180 -> 10
        //state 5 [reject]:
        //  ] -> 3
        //  \\U0000e38e -> 4
        //state 6 [reject]:
        //  \\U00000000-\\U0010ffff -> 11
        //state 7 [reject]:
        //  ] -> 9
        //state 8 [reject]:
        //  ] -> 7
        //state 9 [accept]:
        //  ] -> 9
        //state 10 [reject]:
        //  + -> 13
        //state 11 [reject]:
        //  ] -> 14
        //state 12 [accept]:
        //  S -> 0
        //  \\U000a9c44 -> 1
        //  * -> 2
        //  \\U00005dbb -> 2
        //state 13 [reject]:
        //  \\U0000f34c -> 8
        //state 14 [accept]:
        //  \\U000761ca -> 2

        //After Clone: initial state: 0
        //state 0 [accept]:
        //  S -> 1
        //  \\U000a9c44 -> 2
        //  * -> 3
        //  \\U00005dbb -> 3
        //state 1 [reject]:
        //  \\U00000000-\\U0010ffff -> 4
        //state 2 [reject]:
        //  \\U000001d9 -> 5
        //state 3 [accept]:
        //state 4 [reject]:
        //  ] -> 6
        //  \\U0000e38e -> 7
        //state 5 [reject]:
        //  \\U00000000-\\U0010ffff -> 8
        //state 6 [reject]:
        //  \\U0000e38e -> 7
        //state 7 [reject]:
        //  \\U00021180 -> 9
        //state 8 [reject]:
        //  ] -> 10
        //state 9 [reject]:
        //  + -> 11
        //state 10 [accept]:
        //  \\U000761ca -> 3
        //state 11 [reject]:
        //  \\U0000f34c -> 12
        //state 12 [reject]:
        //  ] -> 13
        //state 13 [reject]:
        //  ] -> 14
        //state 14 [accept]:
        //  ] -> 14

        //After Minimize: initial state: 11
        //state 0 [reject]:
        //  \\U0000e38e -> 2
        //state 1 [reject]:
        //  ] -> 0
        //  \\U0000e38e -> 2
        //state 2 [reject]:
        //  \\U00021180 -> 3
        //state 3 [reject]:
        //  + -> 4
        //state 4 [reject]:
        //  \\U0000f34c -> 8
        //state 5 [reject]:
        //  \\U000001d9 -> 7
        //state 6 [accept]:
        //  \\U000761ca -> 14
        //state 7 [reject]:
        //  \\U00000000-\\U0010ffff -> 9
        //state 8 [reject]:
        //  ] -> 13
        //state 9 [reject]:
        //  ] -> 6
        //state 10 [reject]:
        //  \\U00000000-\\U0010ffff -> 1
        //state 11 [accept]:
        //  \\U000a9c44 -> 5
        //  S -> 10
        //  * -> 14
        //  \\U00005dbb -> 14
        //state 12 [accept]:
        //  ] -> 12
        //state 13 [reject]:
        //  ] -> 12
        //state 14 [accept]:


        //[Test]
        //public virtual void TestAgainstBrzozowskiFixed()
        //{
        //    //int num = AtLeast(200);
        //    //for (int i = 0; i < num; i++)
        //    //{

        //    //string regExp1 = AutomatonTestUtil.RandomRegexp(Random());
        //    //string regExp2 = AutomatonTestUtil.RandomRegexp(Random());
        //    //System.Console.WriteLine("regExp1: " + regExp1);
        //    //System.Console.WriteLine("regExp2: " + regExp2);

        //    string regExp1 = "S.]?𡆀(+]+)]";
        //    string regExp2 = "*|򩱄Ǚ.()]񶇊?|嶻?";

        //    Automaton a1 = new RegExp(regExp1, RegExpSyntax.NONE).ToAutomaton();
        //    Automaton a2 = new RegExp(regExp2, RegExpSyntax.NONE).ToAutomaton();
        //    Automaton a = BasicOperations.Union(a1, a2);
        //    System.Console.WriteLine("Before MinimizeSimple: " + a.ToString());

        //    AutomatonTestUtil.MinimizeSimple(a);
        //    System.Console.WriteLine("After MinimizeSimple: " + a.ToString());
        //    Automaton b = (Automaton)a.Clone();
        //    System.Console.WriteLine("After Clone: " + b.ToString());
        //    MinimizationOperations.Minimize(b);
        //    System.Console.WriteLine("After Minimize: " + b.ToString());
        //    Assert.IsTrue(BasicOperations.SameLanguage(a, b));
        //    Assert.AreEqual(a.GetNumberOfStates(), b.GetNumberOfStates());
        //    Assert.AreEqual(a.GetNumberOfTransitions(), b.GetNumberOfTransitions());
        //    //}
        //}


        /// <summary>
        /// n^2 space usage in Hopcroft minimization? </summary>
        [Test]
        public virtual void TestMinimizeHuge()
        {
            (new RegExp("+-*(A|.....|BC)*]", RegExpSyntax.NONE)).ToAutomaton();
        }
    }
}