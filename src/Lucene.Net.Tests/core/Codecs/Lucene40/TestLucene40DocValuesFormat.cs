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

    using BaseDocValuesFormatTestCase = Lucene.Net.Index.BaseDocValuesFormatTestCase;

    /// <summary>
    /// Tests Lucene40DocValuesFormat
    /// </summary>
    public class TestLucene40DocValuesFormat : BaseDocValuesFormatTestCase
    {
        /// <summary>
        /// LUCENENET specific
        /// Is non-static because OLD_FORMAT_IMPERSONATION_IS_ACTIVE is no longer static.
        /// </summary>
        [TestFixtureSetUp]
        public void BeforeClass()
        {
            OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true; // explicitly instantiates ancient codec
        }

        protected override Codec Codec
        {
            get
            {
                Assert.True(OLD_FORMAT_IMPERSONATION_IS_ACTIVE, "Expecting that this is true");
                return new Lucene40RWCodec(OLD_FORMAT_IMPERSONATION_IS_ACTIVE);
            }
        }

        // LUCENE-4583: this codec should throw IAE on huge binary values:
        protected internal override bool CodecAcceptsHugeBinaryValues(string field)
        {
            return false;
        }
    }
}