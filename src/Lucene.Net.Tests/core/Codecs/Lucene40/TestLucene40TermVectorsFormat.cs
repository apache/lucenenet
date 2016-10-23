namespace Lucene.Net.Codecs.Lucene40
{
    using NUnit.Framework;

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

    using BaseTermVectorsFormatTestCase = Lucene.Net.Index.BaseTermVectorsFormatTestCase;

    public class TestLucene40TermVectorsFormat : BaseTermVectorsFormatTestCase
    {
        /// <summary>
        /// LUCENENET specific
        /// Is non-static because OLD_FORMAT_IMPERSONATION_IS_ACTIVE is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public void BeforeClass()
        {
            OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true; // explicitly instantiates ancient codec
        }

        protected override Codec Codec
        {
            get
            {
                Assert.True(OLD_FORMAT_IMPERSONATION_IS_ACTIVE, "Expecting this to be set already");
                return new Lucene40RWCodec(OLD_FORMAT_IMPERSONATION_IS_ACTIVE);
            }
        }


        #region BaseTermVectorsFormatTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        // only one doc with vectors
        public override void TestRareVectors()
        {
            base.TestRareVectors();
        }

        [Test]
        public override void TestHighFreqs()
        {
            base.TestHighFreqs();
        }

        [Test]
        public override void TestLotsOfFields()
        {
            base.TestLotsOfFields();
        }

        [Test, MaxTime(300000)]
        // different options for the same field
        public override void TestMixedOptions()
        {
            base.TestMixedOptions();
        }

        [Test]
        public override void TestRandom()
        {
            base.TestRandom();
        }

        [Test]
        public override void TestMerge()
        {
            base.TestMerge();
        }

        [Test]
        // run random tests from different threads to make sure the per-thread clones
        // don't share mutable data
        public override void TestClone()
        {
            base.TestClone();
        }

        #endregion

        #region BaseIndexFileFormatTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestMergeStability()
        {
            base.TestMergeStability();
        }

        #endregion
    }
}