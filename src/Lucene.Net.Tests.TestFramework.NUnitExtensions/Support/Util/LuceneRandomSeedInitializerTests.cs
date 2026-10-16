using Lucene.Net.Attributes;
using Lucene.Net.NUnit.TestUtilities;
using Lucene.Net.TestData.Attributes;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
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
    /// Tests for <see cref="LuceneRandomSeedInitializer"/>, which performs the seed wiring that
    /// the custom <see cref="LuceneTestCase.TestFixtureAttribute"/> (an <c>IFixtureBuilder2</c>)
    /// relies on during NUnit's fixture-building phase. This is the most fragile NUnit integration
    /// point, so these tests guard that a <see cref="RandomizedContext"/> is attached to the fixture
    /// and to every child test, and that they share a single initial random seed.
    /// </summary>
    [TestFixture, LuceneNetSpecific]
    public class LuceneRandomSeedInitializerTests
    {
        private static readonly Assembly TestAssembly = typeof(MultiMethodFixture).Assembly;

        [Test]
        public void InitializeTestFixtureAttachesContextToFixture()
        {
            TestSuite fixture = TestBuilder.MakeFixture(typeof(MultiMethodFixture));
            var context = new RandomizedContext(fixture, TestAssembly, randomSeed: 111L, testSeed: 222L);

            var initializer = new LuceneRandomSeedInitializer();
            RandomizedContext returned = initializer.InitializeTestFixture(fixture, context);

            Assert.AreSame(context, returned);
            Assert.AreSame(context, fixture.GetRandomizedContext(),
                "InitializeTestFixture must attach the context so GetRandomizedContext() can retrieve it.");
        }

        [Test]
        public void GenerateRandomSeedsAttachesContextToFixtureAndEveryChild()
        {
            TestSuite fixture = TestBuilder.MakeFixture(typeof(MultiMethodFixture));
            Assert.IsTrue(fixture.HasChildren);
            Assert.AreEqual(3, fixture.Tests.Count, "Expected the three test methods as children.");

            var initializer = new LuceneRandomSeedInitializer();
            // Seed the initializer (auto-generates an initial seed since no tests:seed is configured).
            initializer.InitializeTestFixture(fixture, TestAssembly);
            initializer.GenerateRandomSeeds(fixture);

            RandomizedContext fixtureContext = fixture.GetRandomizedContext();
            Assert.IsNotNull(fixtureContext, "The fixture must have a RandomizedContext after seeding.");

            var childContexts = new List<RandomizedContext>();
            foreach (ITest child in fixture.Tests)
            {
                var t = (Test)child;
                RandomizedContext childContext = t.GetRandomizedContext();
                Assert.IsNotNull(childContext, $"Child test '{t.Name}' must have a RandomizedContext after seeding.");
                childContexts.Add(childContext);
            }

            // All contexts derive from the same initial random seed...
            long initialSeed = fixtureContext.RandomSeed;
            Assert.IsTrue(childContexts.All(c => c.RandomSeed == initialSeed),
                "Every test must share the fixture's initial random seed so the run is repeatable from a single seed.");

            // ...but each child gets its own per-test seed.
            var distinctTestSeeds = childContexts.Select(c => c.TestSeed).Distinct().Count();
            Assert.AreEqual(childContexts.Count, distinctTestSeeds,
                "Each child test should receive its own distinct per-test seed.");
        }

        [Test]
        public void GenerateRandomSeedsIsReproducibleFromTheInitialSeed()
        {
            TestSuite fixture = TestBuilder.MakeFixture(typeof(MultiMethodFixture));

            var initializer = new LuceneRandomSeedInitializer();
            initializer.InitializeTestFixture(fixture, TestAssembly);
            initializer.GenerateRandomSeeds(fixture);

            long initialSeed = fixture.GetRandomizedContext().RandomSeed;

            // Reproduce the exact draw sequence the builder uses (seedOffset 0 for the test fixture):
            //   draw #1 -> the fixture context created by InitializeTestFixture (later overwritten)
            //   draw #2 -> the fixture context set by GenerateRandomSeeds
            //   draws #3.. -> each child test, in order
            var expected = new J2N.Randomizer(initialSeed);
            _ = expected.NextInt64(); // discard draw #1 (overwritten fixture context)

            long expectedFixtureSeed = expected.NextInt64();
            Assert.AreEqual(expectedFixtureSeed, fixture.GetRandomizedContext().TestSeed,
                "The fixture's per-test seed must be reproducible from the initial seed.");

            int i = 0;
            foreach (ITest child in fixture.Tests)
            {
                long expectedChildSeed = expected.NextInt64();
                Assert.AreEqual(expectedChildSeed, ((Test)child).GetRandomizedContext().TestSeed,
                    $"Child test #{i} per-test seed must be reproducible from the initial seed.");
                i++;
            }
        }
    }
}
