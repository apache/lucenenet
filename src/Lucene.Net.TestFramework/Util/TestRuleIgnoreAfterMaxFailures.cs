#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete

namespace Lucene.Net.Util
{

    /*using Assert = org.junit.Assert;
    using AssumptionViolatedException = org.junit.@internal.AssumptionViolatedException;
    using TestRule = org.junit.rules.TestRule;
    using Description = org.junit.runner.Description;
    using Statement = org.junit.runners.model.Statement;

    using RandomizedTest = com.carrotsearch.randomizedtesting.RandomizedTest;
    using Repeat = com.carrotsearch.randomizedtesting.annotations.Repeat;*/

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
    /// this rule keeps a count of failed tests (suites) and will result in an
    /// <seealso cref="AssumptionViolatedException"/> after a given number of failures for all
    /// tests following this condition.
    /// 
    /// <p>
    /// Aborting quickly on failed tests can be useful when used in combination with
    /// test repeats (via the <seealso cref="Repeat"/> annotation or system property).
    /// </summary>
    public sealed class TestRuleIgnoreAfterMaxFailures : TestRule
    {
      /// <summary>
      /// Maximum failures. Package scope for tests.
      /// </summary>
      internal int MaxFailures;

      /// <param name="maxFailures">
      ///          The number of failures after which all tests are ignored. Must be
      ///          greater or equal 1. </param>
      public TestRuleIgnoreAfterMaxFailures(int maxFailures)
      {
        Assert.IsTrue("maxFailures must be >= 1: " + maxFailures, maxFailures >= 1);
        this.MaxFailures = maxFailures;
      }

      public override Statement Apply(Statement s, Description d)
      {
        return new StatementAnonymousClass(this, s);
      }

      private sealed class StatementAnonymousClass : Statement
      {
          private readonly TestRuleIgnoreAfterMaxFailures OuterInstance;

          private Statement s;

          public StatementAnonymousClass(TestRuleIgnoreAfterMaxFailures outerInstance, Statement s)
          {
              this.OuterInstance = outerInstance;
              this.s = s;
          }

          public override void Evaluate()
          {
            int failuresSoFar = FailureMarker.Failures;
            if (failuresSoFar >= OuterInstance.MaxFailures)
            {
              RandomizedTest.assumeTrue("Ignored, failures limit reached (" + failuresSoFar + " >= " + OuterInstance.MaxFailures + ").", false);
            }

            s.evaluate();
          }
      }
    }

}
#endif