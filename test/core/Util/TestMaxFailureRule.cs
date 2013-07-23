using System;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestMaxFailureRule : WithNestedTests
    {
        public SystemPropertiesRestoreRule restoreSysProps = new SystemPropertiesRestoreRule();

        public TestMaxFailureRule() : base(true) { }

        public class Nested : WithNestedTests.AbstractNestedTest
        {
            public const int TOTAL_ITERS = 500;
            public const int DESIRED_FAILURES = TOTAL_ITERS / 10;
            private int numFails = 0;
            private int numIters = 0;

            [Repeat(TOTAL_ITERS)]
            [Test]
            public void testFailSometimes()
            {
                numIters++;
                bool fail = new Random().Next(5) == 0;
                if (fail) numFails++;
                // some seeds are really lucky ... so cheat.
                if (numFails < DESIRED_FAILURES &&
                    DESIRED_FAILURES <= TOTAL_ITERS - numIters)
                {
                    fail = true;
                }
                Assert.IsFalse(fail);
            }
        }

        private sealed class AnonymousRunListener : RunListener
        {
            internal char lastTest;

            public override void TestStarted(Description description)
            {
                lastTest = 'S'; // success.
            }

            public override void TestAssumptionFailure(MockRAMDirectory.Failure failure)
            {
                lastTest = 'A'; // assumption failure.
            }

            public override void TestFailure(MockRAMDirectory.Failure failure)
            {
                lastTest = 'F'; // failure
            }

            public override void TestFinished(Description description)
            {
                results.append(lastTest);
            }
        }

        [Test]
        public virtual void TestMaxFailures()
        {
            int maxFailures = LuceneTestCase.IgnoreAfterMaxFailures.maxFailures;
            int failuresSoFar = LuceneTestCase.IgnoreAfterMaxFailures.failuresSoFar;
            System.clearProperty(SysGlobals.SYSPROP_ITERATIONS());
            try
            {
                LuceneTestCase.IgnoreAfterMaxFailures.maxFailures = 2;
                LuceneTestCase.IgnoreAfterMaxFailures.failuresSoFar = 0;

                JUnitCore core = new JUnitCore();
                var results = new StringBuilder();
                core.AddListener(new AnonymousRunListener());

                Result result = core.Run(typeof(Nested)); // was Nested.class
                Assert.Equals(500, result.RunCount);
                Assert.Equals(0, result.IgnoreCount);
                Assert.Equals(2, result.FailureCount);

                // Make sure we had exactly two failures followed by assumption-failures
                // resulting from ignored tests.
                Assert.IsTrue(results.ToString(),
                    results.ToString().Matches("(S*F){2}A+"));

            }
            finally
            {
                LuceneTestCase.IgnoreAfterMaxFailures.maxFailures = maxFailures;
                LuceneTestCase.IgnoreAfterMaxFailures.failuresSoFar = failuresSoFar;
            }
        }
    }
}
