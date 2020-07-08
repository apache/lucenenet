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

    using Attribute = Lucene.Net.Util.Attribute;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestSimpleAttributeImpl : LuceneTestCase
    {
        // this checks using reflection API if the defaults are correct
        [Test]
        public virtual void TestAttributes()
        {
            TestUtil.AssertAttributeReflection(new PositionIncrementAttribute(), Collections.SingletonMap(typeof(IPositionIncrementAttribute).Name + "#positionIncrement", (object)1));
            TestUtil.AssertAttributeReflection(new PositionLengthAttribute(), Collections.SingletonMap(typeof(IPositionLengthAttribute).Name + "#positionLength", (object)1));
            TestUtil.AssertAttributeReflection(new FlagsAttribute(), Collections.SingletonMap(typeof(IFlagsAttribute).Name + "#flags", (object)0));
            TestUtil.AssertAttributeReflection(new TypeAttribute(), Collections.SingletonMap(typeof(ITypeAttribute).Name + "#type", (object)TypeAttribute.DEFAULT_TYPE));
            TestUtil.AssertAttributeReflection(new PayloadAttribute(), Collections.SingletonMap(typeof(IPayloadAttribute).Name + "#payload", (object)null));
            TestUtil.AssertAttributeReflection(new KeywordAttribute(), Collections.SingletonMap(typeof(IKeywordAttribute).Name + "#keyword", (object)false));
            TestUtil.AssertAttributeReflection(new OffsetAttribute(), new Dictionary<string, object>()
            {
                {typeof(IOffsetAttribute).Name + "#startOffset", 0 },
                {typeof(IOffsetAttribute).Name + "#endOffset", 0}
            });
        }

        public static Attribute AssertCloneIsEqual(Attribute att)
        {
            Attribute clone = (Attribute)att.Clone();
            Assert.AreEqual(att, clone, "Clone must be equal");
            Assert.AreEqual(att.GetHashCode(), clone.GetHashCode(), "Clone's hashcode must be equal");
            return clone;
        }

        public static Attribute AssertCopyIsEqual(Attribute att)
        {
            Attribute copy = (Attribute)System.Activator.CreateInstance(att.GetType());
            att.CopyTo(copy);
            Assert.AreEqual(att, copy, "Copied instance must be equal");
            Assert.AreEqual(att.GetHashCode(), copy.GetHashCode(), "Copied instance's hashcode must be equal");
            return copy;
        }
    }
}