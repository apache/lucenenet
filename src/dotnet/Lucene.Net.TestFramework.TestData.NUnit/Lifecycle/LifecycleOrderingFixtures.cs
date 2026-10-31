using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Lucene.Net.TestData.Lifecycle
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
    /// Middle level of a three-level hierarchy (<see cref="LuceneTestCase"/> at the root) that
    /// mixes the two lifecycle conventions NUnit supports: uniquely-named methods carrying the
    /// NUnit attributes directly (pinned to this level), and virtual override chains (which NUnit
    /// runs once, at the most derived override's level). Records the order its methods fire so
    /// tests can pin the NUnit ordering guarantees the test framework's design depends on
    /// (issue #1087): setup methods run base-level-first, teardown methods run
    /// derived-level-first, and the framework's own work brackets all of them.
    /// </summary>
    public abstract class LifecycleOrderingLevel1Fixture : LuceneTestCase
    {
        public static readonly List<string> Events = new List<string>();
        public static bool CultureAppliedInFirstUserMethod;
        public static bool TempDirExistedInLastUserTearDown;
        public static DirectoryInfo TempDir;

        public static void Reset()
        {
            Events.Clear();
            CultureAppliedInFirstUserMethod = false;
            TempDirExistedInLastUserTearDown = false;
            TempDir = null;
        }

        protected static void Record(string s) => Events.Add(s);

        [OneTimeSetUp]
        public void Level1OneTimeSetUp()
        {
            // This level's only one-time setup method, so it is the first USER method to run
            // (the override chain below lives at level 2). The framework's suite setup must
            // already have applied the randomized environment by now.
            CultureAppliedInFirstUserMethod = Thread.CurrentThread.CurrentCulture.Name == ClassEnvRule.locale?.Name;
            TempDir = CreateTempDir("lifecycle-ordering");
            Record("L1.OneTimeSetUp");
        }

        [OneTimeTearDown]
        public void Level1OneTimeTearDown()
        {
            // This level's only one-time teardown method, so it is the last USER method to run.
            // The framework's temp file cleanup must not have run yet.
            TempDirExistedInLastUserTearDown = Directory.Exists(TempDir.FullName);
            Record("L1.OneTimeTearDown");
        }

        [SetUp]
        public void Level1SetUp() => Record("L1.SetUp");

        [TearDown]
        public void Level1TearDown() => Record("L1.TearDown");

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            Record("Chain.OneTimeSetUp.L1");
        }

        public override void OneTimeTearDown()
        {
            Record("Chain.OneTimeTearDown.L1");
            base.OneTimeTearDown();
        }
    }

    /// <summary>
    /// Most derived level of the ordering hierarchy. Its overrides make the virtual chains run
    /// at THIS level (NUnit runs an overridden method at the override's declaring level, not the
    /// base's), which is the "relocation" behavior described in issue #1087.
    /// </summary>
    public class LifecycleOrderingLevel2Fixture : LifecycleOrderingLevel1Fixture
    {
        [OneTimeSetUp]
        public void Level2OneTimeSetUp() => Record("L2.OneTimeSetUp");

        [OneTimeTearDown]
        public void Level2OneTimeTearDown() => Record("L2.OneTimeTearDown");

        [SetUp]
        public void Level2SetUp() => Record("L2.SetUp");

        [TearDown]
        public void Level2TearDown() => Record("L2.TearDown");

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            Record("Chain.OneTimeSetUp.L2");
        }

        public override void OneTimeTearDown()
        {
            Record("Chain.OneTimeTearDown.L2");
            base.OneTimeTearDown();
        }

        public override void SetUp()
        {
            base.SetUp();
            Record("Chain.SetUp.L2");
        }

        public override void TearDown()
        {
            Record("Chain.TearDown.L2");
            base.TearDown();
        }

        [Test]
        public void TestA() => Record("Test");
    }

    /// <summary>
    /// Base of a hierarchy using the Java Lucene convention: same-named static lifecycle methods
    /// at every level (hiding, not overriding). NUnit must run BOTH, base-level-first for setup
    /// and derived-level-first for teardown, matching JUnit's static hook semantics. This is the
    /// pattern the issue #1087 follow-up will convert subclasses to.
    /// </summary>
    public abstract class StaticLifecycleLevel1Fixture : LuceneTestCase
    {
        public static readonly List<string> Events = new List<string>();

        public static void Reset() => Events.Clear();

        protected static void Record(string s) => Events.Add(s);

        [OneTimeSetUp]
        public static void BeforeClass() => Record("L1.BeforeClass");

        [OneTimeTearDown]
        public static void AfterClass() => Record("L1.AfterClass");
    }

    public class StaticLifecycleLevel2Fixture : StaticLifecycleLevel1Fixture
    {
        [OneTimeSetUp]
        public static new void BeforeClass() => Record("L2.BeforeClass");

        [OneTimeTearDown]
        public static new void AfterClass() => Record("L2.AfterClass");

        [Test]
        public void TestA() => Record("Test");
    }
}
