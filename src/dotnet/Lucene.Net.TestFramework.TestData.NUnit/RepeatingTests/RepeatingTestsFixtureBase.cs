// Source: https://github.com/nunit/nunit/blob/v3.14.0/src/NUnitFramework/testdata/RepeatingTestsFixtureBase.cs

using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.TestData.RepeatingTests
{
    #region Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License.

    // Copyright (c) 2021 Charlie Poole, Rob Prouse
    // 
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    // 
    // The above copyright notice and this permission notice shall be included in
    // all copies or substantial portions of the Software.
    // 
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    // THE SOFTWARE.

    #endregion

    [TestFixture]
    public class RepeatingTestsFixtureBase : LuceneTestCase
    {
        private int fixtureSetupCount;
        private int fixtureTeardownCount;
        private int setupCount;
        private int teardownCount;
        private readonly List<string> tearDownResults = new List<string>();
        private long randomSeed;
        private long testSeed;

        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            fixtureSetupCount++;
        }

        [OneTimeTearDown]
        public override void OneTimeTearDown()
        {
            fixtureTeardownCount++;
            base.OneTimeTearDown();
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            setupCount++;
            var randomizedContext = RandomizedContext.CurrentContext;
            Assert.IsNotNull(randomizedContext);
            Assert.AreNotEqual(testSeed, randomizedContext.TestSeed, "RandomizedContext.TestSeed must change for each iteration.");
            randomSeed = randomizedContext.RandomSeed;
            testSeed = randomizedContext.TestSeed;
            Assert.AreEqual(testSeed, ((J2N.Randomizer)randomizedContext.RandomGenerator).Seed, "RandomizedContext.RandomGenerator must have the same seed as RandomizedContext.TestSeed.");
        }

        [TearDown]
        public override void TearDown()
        {
            tearDownResults.Add(TestContext.CurrentContext.Result.Outcome.ToString());
            teardownCount++;
            var randomizedContext = RandomizedContext.CurrentContext;
            Assert.IsNotNull(randomizedContext);
            Assert.AreEqual(randomSeed, randomizedContext.RandomSeed, "RandomizedContext.RandomSeed must be the same between StartUp() and TearDown().");
            Assert.AreEqual(testSeed, randomizedContext.TestSeed, "RandomizedContext.TestSeed must be the same between StartUp() and TearDown().");
            Assert.AreEqual(testSeed, ((J2N.Randomizer)randomizedContext.RandomGenerator).Seed, "RandomizedContext.RandomGenerator must have the same seed as RandomizedContext.TestSeed.");
            base.TearDown();
        }

        public int FixtureSetupCount => fixtureSetupCount;

        public int FixtureTeardownCount => fixtureTeardownCount;

        public int SetupCount => setupCount;

        public int TeardownCount => teardownCount;

        public List<string> TearDownResults => tearDownResults;

        public int Count { get; protected set; }
    }
}
