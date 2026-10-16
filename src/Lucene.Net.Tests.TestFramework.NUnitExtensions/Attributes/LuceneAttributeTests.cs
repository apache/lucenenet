using Lucene.Net.NUnit.TestUtilities;
using Lucene.Net.TestData.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Linq;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Attributes
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
    /// Tests for the custom NUnit-derived attributes <see cref="LuceneTestCase.NightlyAttribute"/>,
    /// <see cref="LuceneTestCase.WeeklyAttribute"/>, <see cref="LuceneTestCase.AwaitsFixAttribute"/>,
    /// and <see cref="LuceneTestCase.SlowAttribute"/>.
    /// <para/>
    /// These attributes run or skip a test based on the <c>tests:nightly</c>, <c>tests:weekly</c>,
    /// <c>tests:awaitsfix</c>, and <c>tests:slow</c> system properties, exposed as
    /// <see cref="LuceneTestCase.TestNightly"/>, <see cref="LuceneTestCase.TestWeekly"/>,
    /// <see cref="LuceneTestCase.TestAwaitsFix"/>, and <see cref="LuceneTestCase.TestSlow"/>. Those
    /// values are cached per-process and cannot be changed at runtime, so rather than assuming a
    /// particular setting these tests read the current value and assert the behavior that
    /// corresponds to it. This keeps them green regardless of how the suite is configured while
    /// still exercising whichever branch (run or skip) is active.
    /// </summary>
    [TestFixture, LuceneNetSpecific]
    public class LuceneAttributeTests
    {
        private static string CategoryOf(Type fixtureType)
        {
            TestSuite suite = TestBuilder.MakeFixture(fixtureType);
            var test = (Test)suite.Tests[0];
            System.Collections.IList categories = test.Properties["Category"];
            Assert.IsNotNull(categories, "Expected a Category property to be set by the attribute.");
            Assert.AreEqual(1, categories.Count);
            return categories[0]?.ToString();
        }

        // --- Category application (IApplyToTest), independent of the enabled/disabled flag ---

        [TestCase(typeof(NightlyMethodFixture), "Nightly")]
        [TestCase(typeof(WeeklyMethodFixture), "Weekly")]
        [TestCase(typeof(AwaitsFixMethodFixture), "AwaitsFix")]
        [TestCase(typeof(SlowMethodFixture), "Slow")]
        public void AttributeAddsCategory(Type fixtureType, string expectedCategory)
        {
            Assert.AreEqual(expectedCategory, CategoryOf(fixtureType));
        }

        // --- Method-level behavior (IWrapTestMethod) ---

        // When disabled, the per-method Wrap path sets RunState.Skipped for Nightly/Weekly, but
        // RunState.Ignored for AwaitsFix. When enabled, the test simply runs normally.
        [TestCase(typeof(NightlyMethodFixture), RunState.Skipped)]
        [TestCase(typeof(WeeklyMethodFixture), RunState.Skipped)]
        [TestCase(typeof(AwaitsFixMethodFixture), RunState.Ignored)]
        [TestCase(typeof(SlowMethodFixture), RunState.Skipped)]
        public void AttributeControlsTestMethodExecution(Type fixtureType, RunState disabledRunState)
        {
            bool enabled = IsEnabled(fixtureType);

            var fixture = (AttributeFixtureBase)Reflect.Construct(fixtureType);
            ITestResult result = TestBuilder.RunTestFixture(fixture);

            if (enabled)
            {
                Assert.AreEqual(ResultState.Success, result.ResultState);
                Assert.AreEqual(1, fixture.ExecutedCount, "The test body must run when the attribute is enabled.");
            }
            else
            {
                // The wrapped command returns the default (unrun) result, which surfaces as Inconclusive.
                Assert.AreEqual(ResultState.Inconclusive, result.ResultState);
                Assert.AreEqual(0, fixture.ExecutedCount, "The test body must not run when the attribute is disabled.");
                Assert.AreEqual(disabledRunState, result.Children.First().Test.RunState);
            }
        }

        // --- Fixture-level behavior (IApplyToContext) ---

        // When disabled, all four attributes set RunState.Skipped at the fixture level (AwaitsFix
        // only sets RunState.Ignored on the per-method path). When enabled, the test runs normally.
        [TestCase(typeof(NightlyClassFixture), "This is a nightly test.")]
        [TestCase(typeof(WeeklyClassFixture), "This is a weekly test.")]
        [TestCase(typeof(AwaitsFixClassFixture), "https://github.com/apache/lucenenet/issues/1001")]
        [TestCase(typeof(SlowClassFixture), "This is a slow test fixture.")]
        public void AttributeControlsWholeFixtureExecution(Type fixtureType, string disabledReason)
        {
            bool enabled = IsEnabled(fixtureType);

            var fixture = (AttributeFixtureBase)Reflect.Construct(fixtureType);
            ITestResult result = TestBuilder.RunTestFixture(fixture);

            Assert.IsTrue(result.HasChildren);
            ITestResult child = result.Children.First();

            if (enabled)
            {
                Assert.AreEqual(ResultState.Success, child.ResultState);
                Assert.AreEqual(1, fixture.ExecutedCount, "The test body must run when the fixture-level attribute is enabled.");
            }
            else
            {
                Assert.AreEqual(RunState.Skipped, child.Test.RunState);
                Assert.AreEqual(disabledReason, child.Test.Properties.Get(PropertyNames.SkipReason));
                Assert.AreEqual(0, fixture.ExecutedCount, "The test body must not run when the fixture-level attribute is disabled.");
            }
        }

        /// <summary>
        /// Returns whether the attribute applied to the given fixture is currently enabled,
        /// based on the corresponding <see cref="LuceneTestCase"/> flag.
        /// </summary>
        private static bool IsEnabled(Type fixtureType)
        {
            string name = fixtureType.Name;
            if (name.StartsWith("Nightly", StringComparison.Ordinal)) return LuceneTestCase.TestNightly;
            if (name.StartsWith("Weekly", StringComparison.Ordinal)) return LuceneTestCase.TestWeekly;
            if (name.StartsWith("AwaitsFix", StringComparison.Ordinal)) return LuceneTestCase.TestAwaitsFix;
            if (name.StartsWith("Slow", StringComparison.Ordinal)) return LuceneTestCase.TestSlow;
            throw new ArgumentException($"Unrecognized attribute fixture type: {fixtureType.FullName}");
        }
    }
}
