using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Lucene.Net.Tests.TestFramework.NUnitExtensions.Attributes
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

    [LuceneNetSpecific]
    [TestFixture]
    public class TestFixtureAttributeTests
    {
        [Test]
        public void TestCategory_MultipleValues_JoinsWithComma()
        {
            var attr = new LuceneTestCase.TestFixtureAttribute { Category = "A,B,C" };

            // Exercises the count > 1 branch of the getter (comma-join).
            Assert.AreEqual("A,B,C", attr.Category);

            // Also verify it's stored as a list of individual entries.
            var stored = attr.Properties[PropertyNames.Category];
            Assert.AreEqual(3, stored.Count);
            Assert.AreEqual("A", stored[0]);
            Assert.AreEqual("B", stored[1]);
            Assert.AreEqual("C", stored[2]);
        }

        [Test]
        public void TestCategory_SingleValue()
        {
            var attr = new LuceneTestCase.TestFixtureAttribute { Category = "Solo" };
            Assert.AreEqual("Solo", attr.Category);
        }

        [Test]
        public void TestCategory_WhenUnset_ReturnsNull()
        {
            var attr = new LuceneTestCase.TestFixtureAttribute();
            Assert.IsNull(attr.Category);
        }

        [Test]
        public void TestReason_RoundTripsThroughSkipReasonProperty()
        {
            var attr = new LuceneTestCase.TestFixtureAttribute { Reason = "because" };

            Assert.AreEqual("because", attr.Reason);
            Assert.AreEqual("because", attr.Properties.Get(PropertyNames.SkipReason));
            // Reason alone does not change RunState.
            Assert.AreEqual(RunState.Runnable, attr.RunState);
        }

        [Test]
        public void TestIgnoreReason_SetsSkipReasonAndFlipsRunStateToIgnored()
        {
            var attr = new LuceneTestCase.TestFixtureAttribute { IgnoreReason = "nope" };

            Assert.AreEqual("nope", attr.IgnoreReason);
            Assert.AreEqual("nope", attr.Reason);
            Assert.AreEqual("nope", attr.Ignore);
            Assert.AreEqual("nope", attr.Properties.Get(PropertyNames.SkipReason));
            Assert.AreEqual(RunState.Ignored, attr.RunState);
        }

        [Test]
        public void TestIgnore_DelegatesToIgnoreReason()
        {
            var attr = new LuceneTestCase.TestFixtureAttribute { Ignore = "skip" };

            // Ignore is a pass-through to IgnoreReason, so RunState flips and all three getters match.
            Assert.AreEqual("skip", attr.Ignore);
            Assert.AreEqual("skip", attr.IgnoreReason);
            Assert.AreEqual("skip", attr.Reason);
            Assert.AreEqual(RunState.Ignored, attr.RunState);
        }
    }
}
