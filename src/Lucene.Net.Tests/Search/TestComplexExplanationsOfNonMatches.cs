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
    public class TestComplexExplanationsOfNonMatches : TestComplexExplanations
    {
        /// <summary>
        /// Overrides superclass to ignore matches and focus on non-matches
        /// </summary>
        /// <seealso cref= CheckHits#checkNoMatchExplanations </seealso>
        public override void Qtest(Query q, int[] expDocNrs)
        {
            CheckHits.CheckNoMatchExplanations(q, FIELD, Searcher, expDocNrs);
        }


        #region TestComplexExplanations
        // LUCENENET NOTE: Tests in a base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void Test1()
        {
            base.Test1();
        }

        [Test]
        public override void Test2()
        {
            base.Test2();
        }

        // :TODO: we really need more crazy complex cases.

        // //////////////////////////////////////////////////////////////////

        // The rest of these aren't that complex, but they are <i>somewhat</i>
        // complex, and they expose weakness in dealing with queries that match
        // with scores of 0 wrapped in other queries

        [Test]
        public override void TestT3()
        {
            base.TestT3();
        }

        [Test]
        public override void TestMA3()
        {
            base.TestMA3();
        }

        [Test]
        public override void TestFQ5()
        {
            base.TestFQ5();
        }

        [Test]
        public override void TestCSQ4()
        {
            base.TestCSQ4();
        }

        [Test]
        public override void TestDMQ10()
        {
            base.TestDMQ10();
        }

        [Test]
        public override void TestMPQ7()
        {
            base.TestMPQ7();
        }

        [Test]
        public override void TestBQ12()
        {
            base.TestBQ12();
        }

        [Test]
        public override void TestBQ13()
        {
            base.TestBQ13();
        }

        [Test]
        public override void TestBQ18()
        {
            base.TestBQ18();
        }

        [Test]
        public override void TestBQ21()
        {
            base.TestBQ21();
        }

        [Test]
        public override void TestBQ22()
        {
            base.TestBQ22();
        }

        [Test]
        public override void TestST3()
        {
            base.TestST3();
        }

        [Test]
        public override void TestST6()
        {
            base.TestST6();
        }

        [Test]
        public override void TestSF3()
        {
            base.TestSF3();
        }

        [Test]
        public override void TestSF7()
        {
            base.TestSF7();
        }

        [Test]
        public override void TestSNot3()
        {
            base.TestSNot3();
        }

        [Test]
        public override void TestSNot6()
        {
            base.TestSNot6();
        }

        [Test]
        public override void TestSNot8()
        {
            base.TestSNot8();
        }

        [Test]
        public override void TestSNot9()
        {
            base.TestSNot9();
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