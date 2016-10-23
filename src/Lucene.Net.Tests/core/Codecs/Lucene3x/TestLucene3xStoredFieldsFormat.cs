using NUnit.Framework;

namespace Lucene.Net.Codecs.Lucene3x
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

    using BaseStoredFieldsFormatTestCase = Lucene.Net.Index.BaseStoredFieldsFormatTestCase;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    public class TestLucene3xStoredFieldsFormat : BaseStoredFieldsFormatTestCase
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
                Assert.IsTrue(OLD_FORMAT_IMPERSONATION_IS_ACTIVE, "This should have been set up in the test fixture");
                return new PreFlexRWCodec(OLD_FORMAT_IMPERSONATION_IS_ACTIVE);
            }
        }

        [Test]
        public override void TestWriteReadMerge()
        {
            AssumeFalse("impersonation isnt good enough", true);
            // this test tries to switch up between the codec and another codec.
            // for 3.x: we currently cannot take an index with existing 4.x segments
            // and merge into newly formed 3.x segments.
        }



        #region BaseStoredFieldsFormatTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestRandomStoredFields()
        {
            base.TestRandomStoredFields();
        }

        [Test]
        // LUCENE-1727: make sure doc fields are stored in order
        public override void TestStoredFieldsOrder()
        {
            base.TestStoredFieldsOrder();
        }

        [Test]
        // LUCENE-1219
        public override void TestBinaryFieldOffsetLength()
        {
            base.TestBinaryFieldOffsetLength();
        }

        [Test]
        public override void TestNumericField()
        {
            base.TestNumericField();
        }

        [Test]
        public override void TestIndexedBit()
        {
            base.TestIndexedBit();
        }

        [Test]
        public override void TestReadSkip()
        {
            base.TestReadSkip();
        }

        [Test]
        public override void TestEmptyDocs()
        {
            base.TestEmptyDocs();
        }

        [Test]
        public override void TestConcurrentReads()
        {
            base.TestConcurrentReads();
        }

        [Test, MaxTime(120000)]
        public override void TestBigDocuments()
        {
            base.TestBigDocuments();
        }

        [Test]
        public override void TestBulkMergeWithDeletes()
        {
            base.TestBulkMergeWithDeletes();
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