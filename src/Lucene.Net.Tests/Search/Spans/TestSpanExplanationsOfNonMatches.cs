using NUnit.Framework;

namespace Lucene.Net.Search.Spans
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
    public class TestSpanExplanationsOfNonMatches : TestSpanExplanations
    {
        /// <summary>
        /// Overrides superclass to ignore matches and focus on non-matches
        /// </summary>
        /// <seealso> cref= CheckHits#checkNoMatchExplanations </seealso>
        public override void Qtest(Query q, int[] expDocNrs)
        {
            CheckHits.CheckNoMatchExplanations(q, FIELD, Searcher, expDocNrs);
        }


        #region TestSpanExplanations
        // LUCENENET NOTE: Tests in a base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestST1()
        {
            base.TestST1();
        }

        [Test]
        public override void TestST2()
        {
            base.TestST2();
        }

        [Test]
        public override void TestST4()
        {
            base.TestST4();
        }

        [Test]
        public override void TestST5()
        {
            base.TestST5();
        }

        /* some SpanFirstQueries */

        [Test]
        public override void TestSF1()
        {
            base.TestSF1();
        }

        [Test]
        public override void TestSF2()
        {
            base.TestSF2();
        }

        [Test]
        public override void TestSF4()
        {
            base.TestSF4();
        }

        [Test]
        public override void TestSF5()
        {
            base.TestSF5();
        }

        [Test]
        public override void TestSF6()
        {
            base.TestSF6();
        }

        /* some SpanOrQueries */

        [Test]
        public override void TestSO1()
        {
            base.TestSO1();
        }

        [Test]
        public override void TestSO2()
        {
            base.TestSO2();
        }

        [Test]
        public override void TestSO3()
        {
            base.TestSO3();
        }

        [Test]
        public override void TestSO4()
        {
            base.TestSO4();
        }

        /* some SpanNearQueries */

        [Test]
        public override void TestSNear1()
        {
            base.TestSNear1();
        }

        [Test]
        public override void TestSNear2()
        {
            base.TestSNear2();
        }

        [Test]
        public override void TestSNear3()
        {
            base.TestSNear3();
        }

        [Test]
        public override void TestSNear4()
        {
            base.TestSNear4();
        }

        [Test]
        public override void TestSNear5()
        {
            base.TestSNear5();
        }

        [Test]
        public override void TestSNear6()
        {
            base.TestSNear6();
        }

        [Test]
        public override void TestSNear7()
        {
            base.TestSNear7();
        }

        [Test]
        public override void TestSNear8()
        {
            base.TestSNear8();
        }

        [Test]
        public override void TestSNear9()
        {
            base.TestSNear9();
        }

        [Test]
        public override void TestSNear10()
        {
            base.TestSNear10();
        }

        [Test]
        public override void TestSNear11()
        {
            base.TestSNear11();
        }

        /* some SpanNotQueries */

        [Test]
        public override void TestSNot1()
        {
            base.TestSNot1();
        }

        [Test]
        public override void TestSNot2()
        {
            base.TestSNot2();
        }

        [Test]
        public override void TestSNot4()
        {
            base.TestSNot4();
        }

        [Test]
        public override void TestSNot5()
        {
            base.TestSNot5();
        }

        [Test]
        public override void TestSNot7()
        {
            base.TestSNot7();
        }

        [Test]
        public override void TestSNot10()
        {
            base.TestSNot10();
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