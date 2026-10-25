using Lucene.Net.Attributes;
using Lucene.Net.NUnit.TestUtilities;
using Lucene.Net.TestData.Lifecycle;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util
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

    /// <summary>
    /// Verifies that <see cref="LuceneTestCase"/> integrates with NUnit's setup/teardown lifecycle:
    /// <c>OneTimeSetUp</c> runs once before any test, each test is wrapped by a <c>SetUp</c>/
    /// <c>TearDown</c> pair, and <c>OneTimeTearDown</c> runs once after all tests. This guards the
    /// lifecycle wiring directly, rather than only indirectly through the repeating-tests fixtures.
    /// </summary>
    [TestFixture, LuceneNetSpecific]
    public class LuceneTestCaseLifecycleTests
    {
        [Test]
        public void LifecycleMethodsFireInExpectedOrder()
        {
            LifecycleRecordingFixture.Events.Clear();

            ITestResult result = TestBuilder.RunTestFixture(typeof(LifecycleRecordingFixture));

            Assert.AreEqual(ResultState.Success, result.ResultState);

            List<string> events = LifecycleRecordingFixture.Events;

            // OneTimeSetUp fires exactly once, at the very beginning.
            Assert.AreEqual(1, events.Count(e => e == nameof(LifecycleRecordingFixture.OneTimeSetUp)));
            Assert.AreEqual(nameof(LifecycleRecordingFixture.OneTimeSetUp), events.First());

            // OneTimeTearDown fires exactly once, at the very end.
            Assert.AreEqual(1, events.Count(e => e == nameof(LifecycleRecordingFixture.OneTimeTearDown)));
            Assert.AreEqual(nameof(LifecycleRecordingFixture.OneTimeTearDown), events.Last());

            // Each of the two tests is wrapped by a SetUp before and a TearDown after.
            Assert.AreEqual(2, events.Count(e => e == "SetUp"));
            Assert.AreEqual(2, events.Count(e => e == "TearDown"));
            Assert.AreEqual(2, events.Count(e => e == nameof(LifecycleRecordingFixture.TestA) || e == nameof(LifecycleRecordingFixture.TestB)));

            // Verify the SetUp -> test -> TearDown ordering for each test method, regardless of
            // which order NUnit chooses to run TestA and TestB in.
            var testNames = new[] { nameof(LifecycleRecordingFixture.TestA), nameof(LifecycleRecordingFixture.TestB) };
            for (int i = 0; i < events.Count; i++)
            {
                if (testNames.Contains(events[i]))
                {
                    Assert.AreEqual("SetUp", events[i - 1], $"Expected SetUp immediately before {events[i]}.");
                    Assert.AreEqual("TearDown", events[i + 1], $"Expected TearDown immediately after {events[i]}.");
                }
            }
        }
    }
}
