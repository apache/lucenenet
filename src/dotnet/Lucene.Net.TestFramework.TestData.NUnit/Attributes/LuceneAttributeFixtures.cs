using Lucene.Net.Util;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.TestData.Attributes
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
    /// Base class for the attribute test-data fixtures. Tracks how many times the
    /// test body actually executed so the tests can assert whether a test ran or
    /// was skipped/ignored by one of the custom attributes.
    /// </summary>
    public abstract class AttributeFixtureBase : LuceneTestCase
    {
        public int ExecutedCount { get; protected set; }
    }

    // --- Method-level attribute fixtures ---

    public class NightlyMethodFixture : AttributeFixtureBase
    {
        [Test, Nightly]
        public void TestMethod()
        {
            ExecutedCount++;
            Assert.IsTrue(true);
        }
    }

    public class WeeklyMethodFixture : AttributeFixtureBase
    {
        [Test, Weekly]
        public void TestMethod()
        {
            ExecutedCount++;
            Assert.IsTrue(true);
        }
    }

    public class AwaitsFixMethodFixture : AttributeFixtureBase
    {
        [Test, AwaitsFix(BugUrl = "https://github.com/apache/lucenenet/issues/1001")]
        public void TestMethod()
        {
            ExecutedCount++;
            Assert.IsTrue(true);
        }
    }

    public class SlowMethodFixture : AttributeFixtureBase
    {
        [Test, Slow(Message = "This is a slow test fixture.")]
        public void TestMethod()
        {
            ExecutedCount++;
            Assert.IsTrue(true);
        }
    }

    // --- Fixture-level attribute fixtures (attribute applied to the class) ---

    [Nightly]
    public class NightlyClassFixture : AttributeFixtureBase
    {
        [Test]
        public void TestMethod()
        {
            ExecutedCount++;
            Assert.IsTrue(true);
        }
    }

    [Weekly]
    public class WeeklyClassFixture : AttributeFixtureBase
    {
        [Test]
        public void TestMethod()
        {
            ExecutedCount++;
            Assert.IsTrue(true);
        }
    }

    [AwaitsFix(BugUrl = "https://github.com/apache/lucenenet/issues/1001")]
    public class AwaitsFixClassFixture : AttributeFixtureBase
    {
        [Test]
        public void TestMethod()
        {
            ExecutedCount++;
            Assert.IsTrue(true);
        }
    }

    [Slow(Message = "This is a slow test fixture.")]
    public class SlowClassFixture : AttributeFixtureBase
    {
        [Test]
        public void TestMethod()
        {
            ExecutedCount++;
            Assert.IsTrue(true);
        }
    }

    /// <summary>
    /// A Slow test (which runs by default) whose body throws. Used to verify that
    /// <c>LuceneDelegatingTestCommand</c> records the exception into the NUnit result
    /// (the messaging tap) rather than letting it escape.
    /// </summary>
    public class SlowThrowingMethodFixture : AttributeFixtureBase
    {
        public const string FailureMessage = "Intentional failure from SlowThrowingMethodFixture.";

        [Test, Slow]
        public void TestMethod()
        {
            ExecutedCount++;
            throw new InvalidOperationException(FailureMessage);
        }
    }

    /// <summary>
    /// A fixture with multiple test methods, used to verify that the seeding builder
    /// attaches a RandomizedContext to the fixture and to every child test.
    /// </summary>
    public class MultiMethodFixture : LuceneTestCase
    {
        [Test]
        public void TestOne() => Assert.IsTrue(true);

        [Test]
        public void TestTwo() => Assert.IsTrue(true);

        [Test]
        public void TestThree() => Assert.IsTrue(true);
    }
}
