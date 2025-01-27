// Source: https://github.com/nunit/nunit/blob/v3.14.0/src/NUnitFramework/testdata/RepeatingTestsFixtureBase.cs

using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;

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
        }

        [TearDown]
        public override void TearDown()
        {
            tearDownResults.Add(TestContext.CurrentContext.Result.Outcome.ToString());
            teardownCount++;
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
