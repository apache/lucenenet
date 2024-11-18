using Lucene.Net.Support;
using NUnit.Framework;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.TokenAttributes
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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestSimpleAttributeImpl : LuceneTestCase
    {
        // this checks using reflection API if the defaults are correct
        [Test]
        public virtual void TestAttributes()
        {
            TestUtil.AssertAttributeReflection(new PositionIncrementAttribute(), Collections.SingletonMap(nameof(IPositionIncrementAttribute) + "#positionIncrement", (object)1));
            TestUtil.AssertAttributeReflection(new PositionLengthAttribute(), Collections.SingletonMap(nameof(IPositionLengthAttribute) + "#positionLength", (object)1));
            TestUtil.AssertAttributeReflection(new FlagsAttribute(), Collections.SingletonMap(nameof(IFlagsAttribute) + "#flags", (object)0));
            TestUtil.AssertAttributeReflection(new TypeAttribute(), Collections.SingletonMap(nameof(ITypeAttribute) + "#type", (object)TypeAttribute.DEFAULT_TYPE));
            TestUtil.AssertAttributeReflection(new PayloadAttribute(), Collections.SingletonMap(nameof(IPayloadAttribute) + "#payload", (object)null));
            TestUtil.AssertAttributeReflection(new KeywordAttribute(), Collections.SingletonMap(nameof(IKeywordAttribute) + "#keyword", (object)false));
            TestUtil.AssertAttributeReflection(new OffsetAttribute(), new Dictionary<string, object>()
            {
                {nameof(IOffsetAttribute) + "#startOffset", 0 },
                {nameof(IOffsetAttribute) + "#endOffset", 0}
            });
        }
    }
}
