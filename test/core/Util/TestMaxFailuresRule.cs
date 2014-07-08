using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Apache.NMS.Util;
using Lucene.Net.Support;

namespace Lucene.Net.Util
{

    using NUnit.Framework;
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

    //using WithNestedTests = Lucene.Net.Util.junitcompat.WithNestedTests;
    /*using Assert = org.junit.Assert;
    using BeforeClass = org.junit.BeforeClass;
    using Rule = org.junit.Rule;
    using Test = org.junit.Test;
    using Description = org.junit.runner.Description;
    using JUnitCore = org.junit.runner.JUnitCore;
    using Result = org.junit.runner.Result;
    using Failure = org.junit.runner.notification.Failure;
    using RunListener = org.junit.runner.notification.RunListener;

    using Repeat = com.carrotsearch.randomizedtesting.annotations.Repeat;
    using ThreadLeakAction = com.carrotsearch.randomizedtesting.annotations.ThreadLeakAction;
    using ThreadLeakLingering = com.carrotsearch.randomizedtesting.annotations.ThreadLeakLingering;
    using ThreadLeakScope = com.carrotsearch.randomizedtesting.annotations.ThreadLeakScope;
    using Scope = com.carrotsearch.randomizedtesting.annotations.ThreadLeakScope.Scope;
    using ThreadLeakZombies = com.carrotsearch.randomizedtesting.annotations.ThreadLeakZombies;
    using Consequence = com.carrotsearch.randomizedtesting.annotations.ThreadLeakZombies.Consequence;
    using SystemPropertiesInvariantRule = com.carrotsearch.randomizedtesting.rules.SystemPropertiesInvariantRule;
    using SystemPropertiesRestoreRule = com.carrotsearch.randomizedtesting.rules.SystemPropertiesRestoreRule;*/

    /// <seealso cref= TestRuleIgnoreAfterMaxFailures </seealso>
    /// <seealso cref= SystemPropertiesInvariantRule </seealso>
    [TestFixture]
    public class TestMaxFailuresRule : WithNestedTests
    {
        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Rule public com.carrotsearch.randomizedtesting.rules.SystemPropertiesRestoreRule restoreSysProps = new com.carrotsearch.randomizedtesting.rules.SystemPropertiesRestoreRule();
        public SystemPropertiesRestoreRule RestoreSysProps = new SystemPropertiesRestoreRule();

        public TestMaxFailuresRule()
            : base(true)
        {
        }

        public class Nested : WithNestedTests.AbstractNestedTest
        {
            public const int TOTAL_ITERS = 500;
            public static readonly int DESIRED_FAILURES = TOTAL_ITERS / 10;
            internal int NumFails = 0;
            internal int NumIters = 0;

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @Repeat(iterations = TOTAL_ITERS) public void testFailSometimes()
            [Test]
            public virtual void TestFailSometimes()
            {
                NumIters++;
                bool fail = Random().Next(5) == 0;
                if (fail)
                {
                    NumFails++;
                }
                // some seeds are really lucky ... so cheat.
                if (NumFails < DESIRED_FAILURES && DESIRED_FAILURES <= TOTAL_ITERS - NumIters)
                {
                    fail = true;
                }
                Assert.IsFalse(fail);
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void testMaxFailures()
        [Test]
        public virtual void TestMaxFailures()
        {
            LuceneTestCase.ReplaceMaxFailureRule(new TestRuleIgnoreAfterMaxFailures(2));
            JUnitCore core = new JUnitCore();
            StringBuilder results = new StringBuilder();
            core.addListener(new RunListenerAnonymousInnerClassHelper(this, results));

            Result result = core.run(typeof(Nested));
            Assert.AreEqual(500, result.RunCount);
            Assert.AreEqual(0, result.IgnoreCount);
            Assert.AreEqual(2, result.FailureCount);

            // Make sure we had exactly two failures followed by assumption-failures
            // resulting from ignored tests.
            Assert.IsTrue(results.ToString(), results.ToString().matches("(S*F){2}A+"));
        }

        private class RunListenerAnonymousInnerClassHelper : RunListener
        {
            private readonly TestMaxFailuresRule OuterInstance;

            private StringBuilder Results;

            public RunListenerAnonymousInnerClassHelper(TestMaxFailuresRule outerInstance, StringBuilder results)
            {
                this.OuterInstance = outerInstance;
                this.Results = results;
            }

            internal char lastTest;

            public override void TestStarted(Description description)
            {
                lastTest = 'S'; // success.
            }

            public override void TestAssumptionFailure(Failure failure)
            {
                lastTest = 'A'; // assumption failure.
            }

            public override void TestFailure(Failure failure)
            {
                lastTest = 'F'; // failure
            }

            public override void TestFinished(Description description)
            {
                Results.Append(lastTest);
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @ThreadLeakZombies(Consequence.IGNORE_REMAINING_TESTS) @ThreadLeakAction({ThreadLeakAction.Action.WARN}) @ThreadLeakScope(Scope.TEST) @ThreadLeakLingering(linger = 500) public static class Nested2 extends Lucene.Net.Util.junitcompat.WithNestedTests.AbstractNestedTest
        public class Nested2 : WithNestedTests.AbstractNestedTest
        {
            public const int TOTAL_ITERS = 10;
            public static CountDownLatch Die;
            public static Thread Zombie;
            public static int TestNum;

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @BeforeClass public static void setup()
            [SetUp]
            public static void Setup()
            {
                Debug.Assert(Zombie == null);
                Die = new CountDownLatch(1);
                TestNum = 0;
            }

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @Repeat(iterations = TOTAL_ITERS) public void testLeaveZombie()
            [Test]
            public virtual void TestLeaveZombie()
            {
                if (++TestNum == 2)
                {
                    Zombie = new ThreadAnonymousInnerClassHelper(this);
                    Zombie.Start();
                }
            }

            private class ThreadAnonymousInnerClassHelper : ThreadClass
            {
                private readonly Nested2 OuterInstance;

                public ThreadAnonymousInnerClassHelper(Nested2 outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                public override void Run()
                {
                    while (true)
                    {
                        try
                        {
                            Die.@await();
                            return;
                        } // ignore
                        catch (Exception e)
                        {
                        }
                    }
                }
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void testZombieThreadFailures() throws Exception
        [Test]
        public virtual void TestZombieThreadFailures()
        {
            LuceneTestCase.ReplaceMaxFailureRule(new TestRuleIgnoreAfterMaxFailures(1));
            JUnitCore core = new JUnitCore();
            StringBuilder results = new StringBuilder();
            core.addListener(new RunListenerAnonymousInnerClassHelper(this, results));

            Result result = core.run(typeof(Nested2));
            if (Nested2.Die != null)
            {
                Nested2.Die.countDown();
                Nested2.Zombie.Join();
            }

            base.PrevSysOut.println(results.ToString());
            Assert.AreEqual(Nested2.TOTAL_ITERS, result.RunCount);
            Assert.AreEqual(results.ToString(), "SFAAAAAAAA", results.ToString());
        }

        private class RunListenerAnonymousInnerClassHelper : RunListener
        {
            private readonly TestMaxFailuresRule OuterInstance;

            private StringBuilder Results;

            public RunListenerAnonymousInnerClassHelper(TestMaxFailuresRule outerInstance, StringBuilder results)
            {
                this.OuterInstance = outerInstance;
                this.Results = results;
            }

            internal char lastTest;

            public override void TestStarted(Description description)
            {
                lastTest = 'S'; // success.
            }

            public override void TestAssumptionFailure(Failure failure)
            {
                lastTest = 'A'; // assumption failure.
            }

            public override void TestFailure(Failure failure)
            {
                lastTest = 'F'; // failure
                Console.WriteLine(failure.Message);
            }

            public override void TestFinished(Description description)
            {
                Results.Append(lastTest);
            }
        }
    }

}