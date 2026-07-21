using Lucene.Net.Attributes;
using Lucene.Net.NUnit.TestUtilities;
using Lucene.Net.TestData.Lifecycle;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    /// Verifies that the test framework's mandatory setup/teardown work cannot be disabled by a
    /// subclass that overrides <see cref="LuceneTestCase.OneTimeSetUp()"/>,
    /// <see cref="LuceneTestCase.OneTimeTearDown()"/>, <see cref="LuceneTestCase.SetUp()"/> or
    /// <see cref="LuceneTestCase.TearDown()"/> without calling the base method. The framework
    /// work runs in static framework-owned lifecycle methods
    /// (<see cref="LuceneTestCase.__OneTimeSetUp()"/>, <see cref="LuceneTestCase.__OneTimeTearDown()"/>
    /// and <see cref="LuceneTestCase.__TearDown()"/>) declared at the root of the inheritance
    /// hierarchy, which NUnit runs before all subclass setup methods and after all subclass
    /// teardown methods, respectively. See issue #1087.
    /// </summary>
    [TestFixture, LuceneNetSpecific]
    public class LuceneTestCaseLifecycleGuardTests
    {
        private const string ReproduceInfoMarker = "To reproduce this test result";

        [Test]
        public void FrameworkLifecycle_RunsWhenOverridesSkipBaseCalls()
        {
            LifecycleGuardNoBaseFixture.Reset();

            ITestResult result = TestBuilder.RunTestFixture(typeof(LifecycleGuardNoBaseFixture));

            Assert.AreEqual(ResultState.Success, result.ResultState);

            // The randomized environment (culture etc.) was applied before the fixture's
            // OneTimeSetUp, even though the override never calls base.OneTimeSetUp().
            Assert.IsTrue(LifecycleGuardNoBaseFixture.CultureAppliedInOneTimeSetUp,
                "The randomized culture must be applied before the fixture's OneTimeSetUp runs.");

            // The temporary directory was cleaned up, even though the override never calls
            // base.OneTimeTearDown().
            Assert.IsNotNull(LifecycleGuardNoBaseFixture.TempDir);
            Assert.IsFalse(Directory.Exists(LifecycleGuardNoBaseFixture.TempDir.FullName),
                "The temporary directory must be cleaned up after the suite completes.");

            // Resources registered with DisposeAfterTest/DisposeAfterSuite were disposed.
            Assert.IsTrue(LifecycleGuardNoBaseFixture.TestDisposable.Disposed,
                "A resource registered with DisposeAfterTest must be disposed.");
            Assert.IsTrue(LifecycleGuardNoBaseFixture.SuiteDisposable.Disposed,
                "A resource registered with DisposeAfterSuite must be disposed.");

            // The framework teardown work runs AFTER the user's teardown methods. (The disposal
            // order of the suite-scoped resource cannot be asserted here because the TestBuilder
            // harness shares a single RandomizedContext between the suite and its tests, so the
            // temp dir check below stands in for the suite-level ordering instead.)
            List<string> events = LifecycleGuardNoBaseFixture.Events;
            Assert.IsTrue(events.IndexOf("User.TearDown") < events.IndexOf("Disposed:TestDisposable"),
                "Test-scoped resources must be disposed after the fixture's TearDown. Events: " + string.Join(", ", events));
            Assert.IsTrue(LifecycleGuardNoBaseFixture.TempDirExistedInOneTimeTearDown,
                "The temporary directory must still exist during the fixture's OneTimeTearDown; framework cleanup runs after it.");
        }

        [Test]
        public void ReproduceInfo_AddedToFailedTestWhenTearDownSkipsBase()
        {
            ITestResult result = TestBuilder.RunTestFixture(typeof(LifecycleGuardNoBaseFailingFixture));

            ITestResult child = result.Children.Single();
            Assert.AreEqual(ResultState.Failure, child.ResultState);
            Assert.AreEqual(1, CountOccurrences(child.Message, ReproduceInfoMarker),
                "The failure reproduction info must be appended exactly once. Message: " + child.Message);
        }

        [Test]
        public void ReproduceInfo_AddedToFailedTestExactlyOnceWithBaseCalls()
        {
            ITestResult result = TestBuilder.RunTestFixture(typeof(LifecycleGuardBaseCallFailingFixture));

            ITestResult child = result.Children.Single();
            Assert.AreEqual(ResultState.Failure, child.ResultState);

            // Guards against the framework teardown running twice (e.g. once from an overridable
            // method's base call and once from the framework-owned lifecycle method).
            Assert.AreEqual(1, CountOccurrences(child.Message, ReproduceInfoMarker),
                "The failure reproduction info must be appended exactly once. Message: " + child.Message);
        }

        [Test]
        public void SuiteResources_DisposedWhenOneTimeSetUpThrows()
        {
            LifecycleGuardThrowingOneTimeSetUpFixture.Reset();

            ITestResult result = TestBuilder.RunTestFixture(typeof(LifecycleGuardThrowingOneTimeSetUpFixture));

            Assert.AreNotEqual(ResultState.Success, result.ResultState);
            Assert.IsTrue(LifecycleGuardThrowingOneTimeSetUpFixture.SuiteDisposable.Disposed,
                "Suite-scoped resources must be disposed even when OneTimeSetUp throws.");
        }

        [Test]
        public void FrameworkTearDown_RunsOnEveryRepeatAttempt()
        {
            LifecycleGuardRepeatFixture.Reset();

            ITestResult result = TestBuilder.RunTestFixture(typeof(LifecycleGuardRepeatFixture));

            Assert.AreEqual(ResultState.Success, result.ResultState);
            Assert.AreEqual(3, LifecycleGuardRepeatFixture.Disposables.Count,
                "The test must run once per repeat attempt.");
            for (int i = 0; i < LifecycleGuardRepeatFixture.Disposables.Count; i++)
            {
                Assert.IsTrue(LifecycleGuardRepeatFixture.Disposables[i].Disposed,
                    $"The resource registered on attempt {i} must be disposed by the framework teardown of that attempt.");
            }
        }

        private static int CountOccurrences(string text, string value)
            => text is null ? 0 : Regex.Matches(text, Regex.Escape(value)).Count;
    }
}
