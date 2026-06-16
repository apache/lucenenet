using Lucene.Net.NUnit.TestUtilities;
using Lucene.Net.TestData.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
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
    /// Verifies that <c>LuceneDelegatingTestCommand</c> (used by the Nightly/Weekly/AwaitsFix/Slow
    /// attributes) correctly records an exception thrown by the test body into NUnit's result
    /// rather than letting it escape. This exercises the <c>RecordException</c> messaging tap on
    /// the path where the test actually runs.
    /// </summary>
    [TestFixture, LuceneNetSpecific]
    public class LuceneTestCommandMessagingTests
    {
        [Test]
        public void DelegatingCommandRecordsThrownException()
        {
            // The throwing test is marked [Slow], so it only reaches the throw when Slow tests run.
            Assume.That(LuceneTestCase.TestSlow, "Requires tests:slow to be enabled so the Slow test body runs.");

            var fixture = (AttributeFixtureBase)Reflect.Construct(typeof(SlowThrowingMethodFixture));
            ITestResult result = TestBuilder.RunTestFixture(fixture);

            Assert.AreEqual(1, fixture.ExecutedCount, "The Slow test should run, reaching the throw.");

            ITestResult child = result.Children.First();
            Assert.AreEqual(ResultState.Error, child.ResultState,
                "An unhandled exception should be recorded as an Error result, not escape the command.");
            Assert.IsTrue(child.Message.Contains(SlowThrowingMethodFixture.FailureMessage),
                "The recorded result message should contain the thrown exception message. Actual: " + child.Message);
        }
    }
}
