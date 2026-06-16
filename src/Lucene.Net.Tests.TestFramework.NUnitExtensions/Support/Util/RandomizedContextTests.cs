using Lucene.Net.Attributes;
using Lucene.Net.NUnit.TestUtilities;
using Lucene.Net.TestData.Attributes;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
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
    /// Tests for <see cref="RandomizedContext"/>, which holds the per-test random seed and
    /// disposable-resource registrations. These guard the contract the test framework relies on
    /// when integrating with NUnit's <see cref="Test"/> and <see cref="TestExecutionContext"/>.
    /// </summary>
    [TestFixture, LuceneNetSpecific]
    public class RandomizedContextTests
    {
        private static readonly Assembly TestAssembly = typeof(RandomizedContextTests).Assembly;

        // A class-level Test (IsTestClass() == true), suitable for constructing a context directly.
        private static Test MakeClassLevelTest()
            => TestBuilder.MakeFixture(typeof(SlowClassFixture));

        // A test-method-level Test (IsTest() == true).
        private static Test MakeTestLevelTest()
            => TestBuilder.MakeTestCase(typeof(SlowClassFixture), nameof(SlowClassFixture.TestMethod));

        [Test]
        public void ConstructorExposesSeeds()
        {
            var context = new RandomizedContext(MakeClassLevelTest(), TestAssembly, randomSeed: 1234L, testSeed: 5678L);

            Assert.AreEqual(1234L, context.RandomSeed);
            Assert.AreEqual(5678L, context.TestSeed);
            Assert.AreSame(TestAssembly, context.CurrentTestAssembly);
            Assert.IsNotNull(context.CurrentTest);
        }

        [Test]
        public void RandomGeneratorUsesTestSeed()
        {
            var context = new RandomizedContext(MakeClassLevelTest(), TestAssembly, randomSeed: 1L, testSeed: 42L);
            Assert.AreEqual(42L, ((J2N.Randomizer)context.RandomGenerator).Seed);

            // Two generators created from the same test seed must produce the same sequence.
            var a = new RandomizedContext(MakeClassLevelTest(), TestAssembly, 1L, 42L).RandomGenerator;
            var b = new RandomizedContext(MakeClassLevelTest(), TestAssembly, 99L, 42L).RandomGenerator;
            Assert.AreEqual(a.NextInt64(), b.NextInt64(), "Same test seed must yield the same random sequence regardless of the initial random seed.");
        }

        [Test]
        public void ResetSeedUpdatesTestSeedAndGenerator()
        {
            var context = new RandomizedContext(MakeClassLevelTest(), TestAssembly, randomSeed: 1L, testSeed: 42L);
            Assert.AreEqual(42L, context.TestSeed);

            context.ResetSeed(100L);

            Assert.AreEqual(100L, context.TestSeed);
            Assert.AreEqual(100L, ((J2N.Randomizer)context.RandomGenerator).Seed,
                "The RandomGenerator must pick up the new test seed after ResetSeed.");
        }

        [Test]
        public void RandomSeedAsStringIsStableAndChangesWithSeed()
        {
            var context = new RandomizedContext(MakeClassLevelTest(), TestAssembly, randomSeed: 0x1234L, testSeed: 0x5678L);
            string first = context.RandomSeedAsString;
            Assert.IsNotNull(first);
            Assert.AreEqual(first, context.RandomSeedAsString, "RandomSeedAsString must be stable for a given seed.");

            context.ResetSeed(0x9999L);
            Assert.AreNotEqual(first, context.RandomSeedAsString, "RandomSeedAsString must reflect a changed test seed.");
        }

        [Test]
        public void DisposeAtEndTestScopeDisposesResources()
        {
            var context = new RandomizedContext(MakeTestLevelTest(), TestAssembly, randomSeed: 1L, testSeed: 2L);
            var resource = new TrackingDisposable();

            var returned = context.DisposeAtEnd(resource, LifecycleScope.TEST);
            Assert.AreSame(resource, returned, "DisposeAtEnd should return the resource for chaining.");
            Assert.IsFalse(resource.IsDisposed, "Resource must not be disposed until DisposeResources runs.");

            context.DisposeResources();
            Assert.IsTrue(resource.IsDisposed, "Resource must be disposed when DisposeResources runs.");
        }

        [Test]
        public void DisposeResourcesAggregatesThrownExceptions()
        {
            var context = new RandomizedContext(MakeTestLevelTest(), TestAssembly, randomSeed: 1L, testSeed: 2L);
            context.DisposeAtEnd(new ThrowingDisposable("first"), LifecycleScope.TEST);
            context.DisposeAtEnd(new ThrowingDisposable("second"), LifecycleScope.TEST);

            // The first failure is thrown; the second is attached as a suppressed exception.
            var ex = Assert.Throws<InvalidOperationException>(() => context.DisposeResources());
            Assert.IsNotNull(ex);
        }

        private sealed class TrackingDisposable : IDisposable
        {
            public bool IsDisposed { get; private set; }
            public void Dispose() => IsDisposed = true;
        }

        private sealed class ThrowingDisposable : IDisposable
        {
            private readonly string message;
            public ThrowingDisposable(string message) => this.message = message;
            public void Dispose() => throw new InvalidOperationException(message);
        }
    }
}
