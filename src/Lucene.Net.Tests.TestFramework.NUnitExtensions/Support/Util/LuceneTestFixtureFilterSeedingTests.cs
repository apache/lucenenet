using Lucene.Net.Attributes;
using Lucene.Net.TestData.Attributes;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Guards the most fragile requirement of the test-discovery seeding "hack": that applying a
    /// pre-filter (as an IDE or a category/name filter does during discovery) must NOT change the
    /// per-test seed that a surviving test receives.
    /// <para/>
    /// <see cref="NUnitTestFixtureBuilder"/> achieves this by generating the seed for every method in
    /// a deterministic order and advancing the <c>Randomizer</c> for each one, regardless of whether
    /// the filter keeps the test. A filtered-out method therefore still consumes its slot in the seed
    /// sequence, so the tests that survive the filter keep exactly the seed they would have had if the
    /// whole fixture had run. If that ever regresses, a run reproduced from a CI seed string would
    /// silently use different per-test seeds whenever the developer narrowed the run to a single test,
    /// which is precisely the scenario this guards.
    /// </summary>
    [TestFixture, LuceneNetSpecific]
    public class LuceneTestFixtureFilterSeedingTests
    {
        // Mirrors NUnitTestFixtureBuilder.TEST_FIXTURE_SEED_OFFSET (private const). The
        // FixtureOwnSeedIsDrawnFromTheExpectedOffset test asserts this still matches, so if the
        // production offset ever changes this constant is the single place to update.
        private const int TEST_FIXTURE_SEED_OFFSET = 3;

        /// <summary>
        /// Builds <see cref="MultiMethodFixture"/> through the real <c>IFixtureBuilder2</c> path using
        /// the supplied filter and returns the resulting <see cref="TestFixture"/> (unwrapped from the
        /// custom SetUpFixture wrapper).
        /// </summary>
        private static TestFixture BuildFixture(IPreFilter filter)
        {
            var attribute = new LuceneTestCase.TestFixtureAttribute();
            ITypeInfo typeInfo = new TypeWrapper(typeof(MultiMethodFixture));

            // BuildFrom returns the SetUpFixture wrapper; the actual TestFixture is its single child.
            TestSuite setUpFixture = attribute.BuildFrom(typeInfo, filter).Single();
            return (TestFixture)setUpFixture.Tests.Single();
        }

        private static IReadOnlyDictionary<string, long> SeedsByTestName(TestFixture fixture)
        {
            return fixture.Tests
                .Cast<Test>()
                .ToDictionary(t => t.Name, t => t.GetRandomizedContext().TestSeed);
        }

        [Test]
        public void FixtureOwnSeedIsDrawnFromTheExpectedOffset()
        {
            // This both documents and verifies the draw model the other tests rely on: the fixture's
            // own per-test seed is the first draw from new Randomizer(RandomSeed + offset). If the
            // production offset changes, this fails clearly and TEST_FIXTURE_SEED_OFFSET must be updated.
            TestFixture fixture = BuildFixture(AlwaysMatchPreFilter.Instance);
            RandomizedContext fixtureContext = fixture.GetRandomizedContext();

            long expectedFixtureSeed = new J2N.Randomizer(fixtureContext.RandomSeed + TEST_FIXTURE_SEED_OFFSET).NextInt64();
            Assert.AreEqual(expectedFixtureSeed, fixtureContext.TestSeed,
                "The fixture's own per-test seed should be the first draw from the offset-adjusted initial seed. " +
                "If this fails, NUnitTestFixtureBuilder.TEST_FIXTURE_SEED_OFFSET changed; update the test constant.");
        }

        [Test]
        public void FilteringOutMethodsDoesNotChangeSurvivingTestSeeds()
        {
            // Unfiltered: all three methods kept.
            TestFixture unfiltered = BuildFixture(AlwaysMatchPreFilter.Instance);
            IReadOnlyDictionary<string, long> unfilteredSeeds = SeedsByTestName(unfiltered);

            Assert.AreEqual(3, unfilteredSeeds.Count, "Expected all three methods when unfiltered.");

            // Filtered: drop the FIRST method in sorted draw order (TestOne). This is the case that
            // actually exercises the invariant: the survivors (TestThree, TestTwo) are drawn AFTER the
            // dropped method, so if its draw were skipped they would shift down the Randomizer sequence
            // and receive different seeds. They must not.
            var filter = new MethodNameExclusionPreFilter(nameof(MultiMethodFixture.TestOne));
            TestFixture filtered = BuildFixture(filter);
            IReadOnlyDictionary<string, long> filteredSeeds = SeedsByTestName(filtered);

            Assert.AreEqual(2, filteredSeeds.Count, "Expected the filter to drop exactly one method.");
            Assert.IsFalse(filteredSeeds.ContainsKey(nameof(MultiMethodFixture.TestOne)),
                "The excluded method should not be present in the filtered fixture.");

            // The surviving tests must carry the exact same per-test seed they had unfiltered. The two
            // builds use different auto-generated assembly seeds, so we compare each surviving test's
            // seed against its own build re-derived from that build's initial RandomSeed, proving the
            // filtered method still consumed its draw and did not shift the survivors.
            string[] survivors =
            {
                nameof(MultiMethodFixture.TestTwo),
                nameof(MultiMethodFixture.TestThree),
            };
            AssertSeedsMatchDrawSequence(unfiltered, survivors);
            AssertSeedsMatchDrawSequence(filtered, survivors);
        }

        /// <summary>
        /// Re-derives the expected per-test seeds for <see cref="MultiMethodFixture"/> from the given
        /// fixture's own initial <c>RandomSeed</c> and asserts the kept tests match. The methods are
        /// drawn in <see cref="MethodInfoComparer"/> order (ordinal by name): TestOne, TestThree, TestTwo.
        /// The fixture's own seed is drawn first, then one draw per method in that order.
        /// </summary>
        private static void AssertSeedsMatchDrawSequence(TestFixture fixture, string[] expectedKeptMethods)
        {
            RandomizedContext context = fixture.GetRandomizedContext();
            var rng = new J2N.Randomizer(context.RandomSeed + TEST_FIXTURE_SEED_OFFSET);

            _ = rng.NextInt64(); // draw #1: the fixture's own seed

            // Draw order matches the sorted method order. We map each method name to the seed it would
            // receive in an unfiltered draw, then check only the survivors against the actual fixture.
            string[] sortedMethodNames =
            {
                nameof(MultiMethodFixture.TestOne),
                nameof(MultiMethodFixture.TestThree),
                nameof(MultiMethodFixture.TestTwo),
            };

            var expectedByName = new Dictionary<string, long>();
            foreach (string name in sortedMethodNames)
                expectedByName[name] = rng.NextInt64();

            IReadOnlyDictionary<string, long> actual = SeedsByTestName(fixture);
            foreach (string keptMethod in expectedKeptMethods)
            {
                Assert.IsTrue(actual.ContainsKey(keptMethod), $"Expected surviving method '{keptMethod}' to be present.");
                Assert.AreEqual(expectedByName[keptMethod], actual[keptMethod],
                    $"Surviving test '{keptMethod}' must keep the seed it would have had in an unfiltered draw, " +
                    "proving the filtered-out method still consumed its slot in the Randomizer sequence.");
            }
        }

        /// <summary>
        /// An <see cref="IPreFilter"/> that matches everything except the named methods, used to
        /// simulate an IDE or category/name filter narrowing a run during discovery.
        /// </summary>
        private sealed class MethodNameExclusionPreFilter : IPreFilter
        {
            private readonly HashSet<string> excludedMethodNames;

            public MethodNameExclusionPreFilter(params string[] excludedMethodNames)
            {
                this.excludedMethodNames = new HashSet<string>(excludedMethodNames, StringComparer.Ordinal);
            }

            public bool IsMatch(Type type) => true;

            public bool IsMatch(Type type, MethodInfo method) => !excludedMethodNames.Contains(method.Name);
        }
    }
}
