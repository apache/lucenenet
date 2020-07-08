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

    [TestFixture]
    public class TestLucene3xStoredFieldsFormat : BaseStoredFieldsFormatTestCase
    {
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();
            OldFormatImpersonationIsActive = true; // explicitly instantiates ancient codec
        }

        protected override Codec GetCodec()
        {
            return new PreFlexRWCodec();
        }

        [Test]
        public override void TestWriteReadMerge()
        {
            AssumeFalse("impersonation isnt good enough", true);
            // this test tries to switch up between the codec and another codec.
            // for 3.x: we currently cannot take an index with existing 4.x segments
            // and merge into newly formed 3.x segments.
        }
    }
}