namespace Lucene.Net.Codecs.Lucene45
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

    using BaseCompressingDocValuesFormatTestCase = Lucene.Net.Index.BaseCompressingDocValuesFormatTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Tests Lucene45DocValuesFormat
    /// </summary>
    [TestFixture]
    public class TestLucene45DocValuesFormat : BaseCompressingDocValuesFormatTestCase
    {
        private readonly Codec codec = TestUtil.AlwaysDocValuesFormat(new Lucene45DocValuesFormat());

        protected override Codec GetCodec()
        {
            return codec;
        }
    }
}
