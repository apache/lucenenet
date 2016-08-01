using NUnit.Framework;

namespace Lucene.Net.Codecs.Lucene3x
{
    using System.Collections.Generic;

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
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    public class TestLucene3xTermVectorsFormat : BaseTermVectorsFormatTestCase
    {
        [SetUp]
        public override void SetUp()
        {
            OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
            base.SetUp();
        }

        protected override Codec Codec
        {
            get
            {
                Assert.IsTrue(OLD_FORMAT_IMPERSONATION_IS_ACTIVE, "This should have been set up in the test fixture");
                return new PreFlexRWCodec(OLD_FORMAT_IMPERSONATION_IS_ACTIVE);
            }
        }

        protected override IEnumerable<Options> ValidOptions()
        {
            return ValidOptions(Options.NONE, Options.POSITIONS_AND_OFFSETS);
        }
    }
}