using Lucene.Net.Attributes;
using Lucene.Net.NUnit.TestUtilities;
using Lucene.Net.TestData.Lifecycle;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using System.IO;
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
    /// Pins the NUnit lifecycle ordering guarantees that <see cref="LuceneTestCase"/>'s design
    /// depends on (issue #1087): setup methods run base-level-first and teardown methods run
    /// derived-level-first (so the framework's static lifecycle methods at the root of the
    /// hierarchy always bracket all user code); an overridden virtual lifecycle method runs once,
    /// at the override's declaring level; and same-named static lifecycle methods (the Java
    /// Lucene convention) run at every level. Only cross-level ordering is asserted; the relative
    /// order of multiple lifecycle methods declared at the SAME level is unspecified by NUnit
    /// and deliberately not asserted here.
    /// </summary>
    [TestFixture, LuceneNetSpecific]
    public class LuceneTestCaseLifecycleOrderingTests
    {
        [Test]
        public void LifecycleMethods_RunBaseLevelFirstForSetUpAndDerivedLevelFirstForTearDown()
        {
            LifecycleOrderingLevel1Fixture.Reset();

            ITestResult result = TestBuilder.RunTestFixture(typeof(LifecycleOrderingLevel2Fixture));

            Assert.AreEqual(ResultState.Success, result.ResultState);
            List<string> events = LifecycleOrderingLevel1Fixture.Events;

            // The framework's suite setup runs before the first user method at any level, and its
            // temp file cleanup runs after the last user teardown at any level.
            Assert.IsTrue(LifecycleOrderingLevel1Fixture.CultureAppliedInFirstUserMethod,
                "The randomized culture must be applied before the base-most user one-time setup runs.");
            Assert.IsTrue(LifecycleOrderingLevel1Fixture.TempDirExistedInLastUserTearDown,
                "The temporary directory must still exist during the base-most user one-time teardown.");
            Assert.IsFalse(Directory.Exists(LifecycleOrderingLevel1Fixture.TempDir.FullName),
                "The temporary directory must be cleaned up after the suite completes.");

            // One-time setup: base level before derived level, and the override chain (declared
            // at level 2) runs at level 2, AFTER level 1's uniquely-named method. This is the
            // "relocation" behavior that motivated moving the framework work to static methods.
            AssertBefore(events, "L1.OneTimeSetUp", "L2.OneTimeSetUp");
            AssertBefore(events, "L1.OneTimeSetUp", "Chain.OneTimeSetUp.L1");
            AssertBefore(events, "Chain.OneTimeSetUp.L1", "Chain.OneTimeSetUp.L2");
            AssertBefore(events, "L2.OneTimeSetUp", "Test");
            AssertBefore(events, "Chain.OneTimeSetUp.L2", "Test");

            // Per-test setup: base level before all of the derived level's methods.
            AssertBefore(events, "L1.SetUp", "L2.SetUp");
            AssertBefore(events, "L1.SetUp", "Chain.SetUp.L2");
            AssertBefore(events, "L2.SetUp", "Test");
            AssertBefore(events, "Chain.SetUp.L2", "Test");

            // Per-test teardown: all of the derived level's methods before the base level's.
            AssertBefore(events, "Test", "L2.TearDown");
            AssertBefore(events, "Test", "Chain.TearDown.L2");
            AssertBefore(events, "L2.TearDown", "L1.TearDown");
            AssertBefore(events, "Chain.TearDown.L2", "L1.TearDown");

            // One-time teardown: derived level first (including the override chain, which runs at
            // level 2 and unwinds most-derived-first), base level last.
            AssertBefore(events, "Chain.OneTimeTearDown.L2", "Chain.OneTimeTearDown.L1");
            AssertBefore(events, "L2.OneTimeTearDown", "L1.OneTimeTearDown");
            AssertBefore(events, "Chain.OneTimeTearDown.L1", "L1.OneTimeTearDown");
        }

        [Test]
        public void SameNamedStaticLifecycleMethods_RunAtEveryLevel()
        {
            StaticLifecycleLevel1Fixture.Reset();

            ITestResult result = TestBuilder.RunTestFixture(typeof(StaticLifecycleLevel2Fixture));

            Assert.AreEqual(ResultState.Success, result.ResultState);

            // Both the hidden base method and the derived method run, base-first for setup and
            // derived-first for teardown, matching JUnit's static hook semantics.
            Assert.AreEqual(
                new[] { "L1.BeforeClass", "L2.BeforeClass", "Test", "L2.AfterClass", "L1.AfterClass" },
                StaticLifecycleLevel1Fixture.Events);
        }

        private static void AssertBefore(List<string> events, string first, string second)
            => LifecycleAssert.AssertBefore(events, first, second);
    }
}
