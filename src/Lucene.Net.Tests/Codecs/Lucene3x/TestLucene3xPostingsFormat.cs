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

    using BasePostingsFormatTestCase = Lucene.Net.Index.BasePostingsFormatTestCase;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    /// <summary>
    /// Tests Lucene3x postings format
    /// </summary>
    public class TestLucene3xPostingsFormat : BasePostingsFormatTestCase
    {
        private readonly Codec codec = new PreFlexRWCodec();

        /// <summary>
        /// we will manually instantiate preflex-rw here
        /// </summary>
        public override void SetUp()
        {
            base.SetUp();
            LuceneTestCase.OldFormatImpersonationIsActive = true;
        }


        protected override Codec GetCodec()
        {
            return codec;
        }
    }
}