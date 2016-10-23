using NUnit.Framework;

namespace Lucene.Net.Index
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

    using Codec = Lucene.Net.Codecs.Codec;
    using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;

    /// <summary>
    /// Tests with the default randomized codec. Not really redundant with
    /// other specific instantiations since we want to test some test-only impls
    /// like Asserting, as well as make it easy to write a codec and pass -Dtests.codec
    /// </summary>
    [TestFixture]
    public class TestStoredFieldsFormat : BaseStoredFieldsFormatTestCase
    {
        protected override Codec Codec
        {
            get
            {
                return Codec.Default;
            }
        }

        [Test]
        public override void TestWriteReadMerge()
        {
            AssumeFalse("impersonation isnt good enough", Codec is Lucene3xCodec);
            // this test tries to switch up between the codec and another codec.
            // for 3.x: we currently cannot take an index with existing 4.x segments
            // and merge into newly formed 3.x segments.
            base.TestWriteReadMerge();
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