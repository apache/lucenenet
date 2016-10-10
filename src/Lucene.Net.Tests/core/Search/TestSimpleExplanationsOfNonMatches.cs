using NUnit.Framework;

namespace Lucene.Net.Search
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
    /// subclass of TestSimpleExplanations that verifies non matches.
    /// </summary>
    [TestFixture]
    public class TestSimpleExplanationsOfNonMatches : TestSimpleExplanations
    {
        /// <summary>
        /// Overrides superclass to ignore matches and focus on non-matches
        /// </summary>
        /// <seealso cref= CheckHits#checkNoMatchExplanations </seealso>
        public override void Qtest(Query q, int[] expDocNrs)
        {
            CheckHits.CheckNoMatchExplanations(q, FIELD, Searcher, expDocNrs);
        }


        #region TestSimpleExplanations
        // LUCENENET NOTE: Tests in a base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestT1()
        {
            base.TestT1();
        }

        [Test]
        public override void TestT2()
        {
            base.TestT2();
        }

        /* MatchAllDocs */

        [Test]
        public override void TestMA1()
        {
            base.TestMA1();
        }

        [Test]
        public override void TestMA2()
        {
            base.TestMA2();
        }

        /* some simple phrase tests */

        [Test]
        public override void TestP1()
        {
            base.TestP1();
        }

        [Test]
        public override void TestP2()
        {
            base.TestP2();
        }

        [Test]
        public override void TestP3()
        {
            base.TestP3();
        }

        [Test]
        public override void TestP4()
        {
            base.TestP4();
        }

        [Test]
        public override void TestP5()
        {
            base.TestP5();
        }

        [Test]
        public override void TestP6()
        {
            base.TestP6();
        }

        [Test]
        public override void TestP7()
        {
            base.TestP7();
        }

        /* some simple filtered query tests */

        [Test]
        public override void TestFQ1()
        {
            base.TestFQ1();
        }

        [Test]
        public override void TestFQ2()
        {
            base.TestFQ2();
        }

        [Test]
        public override void TestFQ3()
        {
            base.TestFQ3();
        }

        [Test]
        public override void TestFQ4()
        {
            base.TestFQ4();
        }

        [Test]
        public override void TestFQ6()
        {
            base.TestFQ6();
        }

        /* ConstantScoreQueries */

        [Test]
        public override void TestCSQ1()
        {
            base.TestCSQ1();
        }

        [Test]
        public override void TestCSQ2()
        {
            base.TestCSQ2();
        }

        [Test]
        public override void TestCSQ3()
        {
            base.TestCSQ3();
        }

        /* DisjunctionMaxQuery */

        [Test]
        public override void TestDMQ1()
        {
            base.TestDMQ1();
        }

        [Test]
        public override void TestDMQ2()
        {
            base.TestDMQ2();
        }

        [Test]
        public override void TestDMQ3()
        {
            base.TestDMQ3();
        }

        [Test]
        public override void TestDMQ4()
        {
            base.TestDMQ4();
        }

        [Test]
        public override void TestDMQ5()
        {
            base.TestDMQ5();
        }

        [Test]
        public override void TestDMQ6()
        {
            base.TestDMQ6();
        }

        [Test]
        public override void TestDMQ7()
        {
            base.TestDMQ7();
        }

        [Test]
        public override void TestDMQ8()
        {
            base.TestDMQ8();
        }

        [Test]
        public override void TestDMQ9()
        {
            base.TestDMQ9();
        }

        /* MultiPhraseQuery */

        [Test]
        public override void TestMPQ1()
        {
            base.TestMPQ1();
        }

        [Test]
        public override void TestMPQ2()
        {
            base.TestMPQ2();
        }

        [Test]
        public override void TestMPQ3()
        {
            base.TestMPQ3();
        }

        [Test]
        public override void TestMPQ4()
        {
            base.TestMPQ4();
        }

        [Test]
        public override void TestMPQ5()
        {
            base.TestMPQ5();
        }

        [Test]
        public override void TestMPQ6()
        {
            base.TestMPQ6();
        }

        /* some simple tests of boolean queries containing term queries */

        [Test]
        public override void TestBQ1()
        {
            base.TestBQ1();
        }

        [Test]
        public override void TestBQ2()
        {
            base.TestBQ2();
        }

        [Test]
        public override void TestBQ3()
        {
            base.TestBQ3();
        }

        [Test]
        public override void TestBQ4()
        {
            base.TestBQ4();
        }

        [Test]
        public override void TestBQ5()
        {
            base.TestBQ5();
        }

        [Test]
        public override void TestBQ6()
        {
            base.TestBQ6();
        }

        [Test]
        public override void TestBQ7()
        {
            base.TestBQ7();
        }

        [Test]
        public override void TestBQ8()
        {
            base.TestBQ8();
        }

        [Test]
        public override void TestBQ9()
        {
            base.TestBQ9();
        }

        [Test]
        public override void TestBQ10()
        {
            base.TestBQ10();
        }

        [Test]
        public override void TestBQ11()
        {
            base.TestBQ11();
        }

        [Test]
        public override void TestBQ14()
        {
            base.TestBQ14();
        }

        [Test]
        public override void TestBQ15()
        {
            base.TestBQ15();
        }

        [Test]
        public override void TestBQ16()
        {
            base.TestBQ16();
        }

        [Test]
        public override void TestBQ17()
        {
            base.TestBQ17();
        }

        [Test]
        public override void TestBQ19()
        {
            base.TestBQ19();
        }

        [Test]
        public override void TestBQ20()
        {
            base.TestBQ20();
        }

        /* BQ of TQ: using alt so some fields have zero boost and some don't */

        [Test]
        public override void TestMultiFieldBQ1()
        {
            base.TestMultiFieldBQ1();
        }

        [Test]
        public override void TestMultiFieldBQ2()
        {
            base.TestMultiFieldBQ2();
        }

        [Test]
        public override void TestMultiFieldBQ3()
        {
            base.TestMultiFieldBQ3();
        }

        [Test]
        public override void TestMultiFieldBQ4()
        {
            base.TestMultiFieldBQ4();
        }

        [Test]
        public override void TestMultiFieldBQ5()
        {
            base.TestMultiFieldBQ5();
        }

        [Test]
        public override void TestMultiFieldBQ6()
        {
            base.TestMultiFieldBQ6();
        }

        [Test]
        public override void TestMultiFieldBQ7()
        {
            base.TestMultiFieldBQ7();
        }

        [Test]
        public override void TestMultiFieldBQ8()
        {
            base.TestMultiFieldBQ8();
        }

        [Test]
        public override void TestMultiFieldBQ9()
        {
            base.TestMultiFieldBQ9();
        }

        [Test]
        public override void TestMultiFieldBQ10()
        {
            base.TestMultiFieldBQ10();
        }

        /* BQ of PQ: using alt so some fields have zero boost and some don't */

        [Test]
        public override void TestMultiFieldBQofPQ1()
        {
            base.TestMultiFieldBQofPQ1();
        }

        [Test]
        public override void TestMultiFieldBQofPQ2()
        {
            base.TestMultiFieldBQofPQ2();
        }

        [Test]
        public override void TestMultiFieldBQofPQ3()
        {
            base.TestMultiFieldBQofPQ3();
        }

        [Test]
        public override void TestMultiFieldBQofPQ4()
        {
            base.TestMultiFieldBQofPQ4();
        }

        [Test]
        public override void TestMultiFieldBQofPQ5()
        {
            base.TestMultiFieldBQofPQ5();
        }

        [Test]
        public override void TestMultiFieldBQofPQ6()
        {
            base.TestMultiFieldBQofPQ6();
        }

        [Test]
        public override void TestMultiFieldBQofPQ7()
        {
            base.TestMultiFieldBQofPQ7();
        }

        #endregion

        #region TestExplanations
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.


        /// <summary>
        /// Placeholder: JUnit freaks if you don't have one test ... making
        /// class abstract doesn't help
        /// </summary>
        [Test]
        public override void TestNoop()
        {
            base.TestNoop();
        }

        #endregion
    }
}