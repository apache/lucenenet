using Lucene.Net.Util;
using NUnit.Framework;
using System;
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
    /// A disposable that records its disposal into a shared event list, so tests can verify both
    /// that the test framework disposed it and where the disposal happened relative to the
    /// fixture's own lifecycle events.
    /// </summary>
    public sealed class TrackingDisposable : IDisposable
    {
        private readonly string name;
        private readonly List<string> events;

        public TrackingDisposable(string name, List<string> events)
        {
            this.name = name;
            this.events = events;
        }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
            events.Add("Disposed:" + name);
        }
    }

    /// <summary>
    /// Overrides all four <see cref="LuceneTestCase"/> lifecycle methods and deliberately never
    /// calls the base methods. Used to verify that the test framework's mandatory setup/teardown
    /// work still runs (issue #1087): the randomized environment is applied before the fixture's
    /// <c>OneTimeSetUp</c>, temporary files are cleaned up, and resources registered with
    /// <see cref="LuceneTestCase.DisposeAfterTest"/>/<see cref="LuceneTestCase.DisposeAfterSuite"/>
    /// are disposed after the fixture's own teardown methods.
    /// </summary>
    public class LifecycleGuardNoBaseFixture : LuceneTestCase
    {
        public static readonly List<string> Events = new List<string>();
        public static bool CultureAppliedInOneTimeSetUp;
        public static bool TempDirExistedInOneTimeTearDown;
        public static DirectoryInfo TempDir;
        public static TrackingDisposable SuiteDisposable;
        public static TrackingDisposable TestDisposable;

        public static void Reset()
        {
            Events.Clear();
            CultureAppliedInOneTimeSetUp = false;
            TempDirExistedInOneTimeTearDown = false;
            TempDir = null;
            SuiteDisposable = null;
            TestDisposable = null;
        }

        public override void OneTimeSetUp()
        {
            // Deliberately does NOT call base.OneTimeSetUp().
            CultureAppliedInOneTimeSetUp = Thread.CurrentThread.CurrentCulture.Name == ClassEnvRule.locale?.Name;
            TempDir = CreateTempDir("lifecycle-guard-nobase");
            SuiteDisposable = DisposeAfterSuite(new TrackingDisposable(nameof(SuiteDisposable), Events));
            Events.Add("User.OneTimeSetUp");
        }

        public override void SetUp() => Events.Add("User.SetUp"); // Deliberately does NOT call base.SetUp().

        public override void TearDown() => Events.Add("User.TearDown"); // Deliberately does NOT call base.TearDown().

        public override void OneTimeTearDown()
        {
            // Deliberately does NOT call base.OneTimeTearDown(). The temporary directory must
            // still exist here, because the framework's cleanup runs after this method.
            TempDirExistedInOneTimeTearDown = Directory.Exists(TempDir.FullName);
            Events.Add("User.OneTimeTearDown");
        }

        [Test]
        public void TestA()
        {
            TestDisposable = DisposeAfterTest(new TrackingDisposable(nameof(TestDisposable), Events));
            Events.Add("Test");
        }
    }

    /// <summary>
    /// Has a failing test and a <c>TearDown</c> override that never calls <c>base.TearDown()</c>.
    /// Used to verify that the failure reproduction information is still appended to the test
    /// result by the test framework (issue #1087).
    /// </summary>
    public class LifecycleGuardNoBaseFailingFixture : LuceneTestCase
    {
        public override void TearDown()
        {
            // Deliberately does NOT call base.TearDown().
        }

        [Test]
        public void TestThatFails() => Assert.Fail("intentional failure");
    }

    /// <summary>
    /// Has a failing test and no lifecycle overrides, so the framework teardown runs at the
    /// traditional <c>base.TearDown()</c> position. Used to verify that the run-once guard
    /// prevents the framework work from also running a second time from the lifecycle backstop
    /// (the reproduction information must appear exactly once). See issue #1087.
    /// </summary>
    public class LifecycleGuardBaseCallFailingFixture : LuceneTestCase
    {
        [Test]
        public void TestThatFails() => Assert.Fail("intentional failure");
    }

    /// <summary>
    /// Throws from <c>OneTimeSetUp</c> after registering a suite-scoped disposable. Used to
    /// verify that the test framework's suite teardown (which disposes suite-scoped resources)
    /// still runs when a fixture's one-time setup fails. See issue #1087.
    /// </summary>
    public class LifecycleGuardThrowingOneTimeSetUpFixture : LuceneTestCase
    {
        public static readonly List<string> Events = new List<string>();
        public static TrackingDisposable SuiteDisposable;

        public static void Reset()
        {
            Events.Clear();
            SuiteDisposable = null;
        }

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            SuiteDisposable = DisposeAfterSuite(new TrackingDisposable(nameof(SuiteDisposable), Events));
            throw new InvalidOperationException("intentional failure");
        }

        [Test]
        public void TestA()
        {
        }
    }

    /// <summary>
    /// Uses <c>[Repeat]</c> with a <c>TearDown</c> override that never calls
    /// <c>base.TearDown()</c>, registering a test-scoped disposable on each attempt. Used to
    /// verify that the framework teardown runs on every repeat attempt (which reuse the same
    /// NUnit execution context), disposing the resources registered by each attempt. See
    /// issue #1087.
    /// </summary>
    public class LifecycleGuardRepeatFixture : LuceneTestCase
    {
        public static readonly List<string> Events = new List<string>();
        public static readonly List<TrackingDisposable> Disposables = new List<TrackingDisposable>();

        public static void Reset()
        {
            Events.Clear();
            Disposables.Clear();
        }

        public override void TearDown()
        {
            // Deliberately does NOT call base.TearDown().
        }

        [Test]
        [Repeat(3)]
        public void TestRegistersDisposablePerAttempt()
        {
            Disposables.Add(DisposeAfterTest(new TrackingDisposable("TestDisposable" + Disposables.Count, Events)));
        }
    }
}
