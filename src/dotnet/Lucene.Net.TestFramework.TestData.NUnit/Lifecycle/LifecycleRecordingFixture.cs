using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;

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
    /// A <see cref="LuceneTestCase"/> fixture that records the order in which its NUnit lifecycle
    /// methods fire, so a test can verify that <see cref="LuceneTestCase"/> integrates with NUnit's
    /// OneTimeSetUp/SetUp/TearDown/OneTimeTearDown lifecycle as expected.
    /// </summary>
    public class LifecycleRecordingFixture : LuceneTestCase
    {
        /// <summary>The ordered list of lifecycle events recorded for the most recent run.</summary>
        public static readonly List<string> Events = new List<string>();

        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            Events.Add(nameof(OneTimeSetUp));
        }

        [OneTimeTearDown]
        public override void OneTimeTearDown()
        {
            Events.Add(nameof(OneTimeTearDown));
            base.OneTimeTearDown();
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Events.Add(nameof(SetUp));
        }

        [TearDown]
        public override void TearDown()
        {
            Events.Add(nameof(TearDown));
            base.TearDown();
        }

        [Test]
        public void TestA() => Events.Add(nameof(TestA));

        [Test]
        public void TestB() => Events.Add(nameof(TestB));
    }
}
