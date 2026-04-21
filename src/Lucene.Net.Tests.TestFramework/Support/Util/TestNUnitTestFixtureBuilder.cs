using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Tests.TestFramework.Support.Util
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
    public class TestNUnitTestFixtureBuilder
    {
        /// <summary>
        /// Tests that the <see cref="NUnitTestFixtureBuilder.GetArgDisplayNames(ITestFixtureData)"/> method
        /// successfully uses its private reflection to get the ArgDisplayNames property from NUnit's internals.
        /// <para />
        /// If this ever breaks as part of an NUnit upgrade, then it means that the internal/private implementation
        /// changed, and we'll have to adapt.
        /// </summary>
        [Test]
        public void TestGetArgDisplayNames_Success()
        {
            var fixtureData = new TestFixtureData("a", 1, "b");
            fixtureData.SetArgDisplayNames("ValueA", "ValueOne", "ValueB");
            var displayNames = NUnitTestFixtureBuilder.GetArgDisplayNames(fixtureData);
            Assert.AreEqual(displayNames.Length, 3);
            Assert.AreEqual(displayNames[0], "ValueA");
            Assert.AreEqual(displayNames[1], "ValueOne");
            Assert.AreEqual(displayNames[2], "ValueB");
        }

        [Test]
        public void TestGetArgDisplayNames_NotATestParametersObject()
        {
            var data = new NonTestParametersFixtureData();
            var displayNames = NUnitTestFixtureBuilder.GetArgDisplayNames(data);
            Assert.IsNull(displayNames);
        }

        [Test]
        public void TestGetArgDisplayNames_NoneAvailable()
        {
            var data = new TestFixtureData("a", 1, "b");
            // deliberately not setting ArgDisplayNames here
            var displayNames = NUnitTestFixtureBuilder.GetArgDisplayNames(data);
            Assert.IsNull(displayNames);
        }

        [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
        [SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized")]
        private class NonTestParametersFixtureData : ITestFixtureData
        {
            public string TestName { get; }
            public RunState RunState { get; }
            public object[] Arguments { get; }
            public IPropertyBag Properties { get; }
            public Type[] TypeArgs { get; }
        }
    }
}
