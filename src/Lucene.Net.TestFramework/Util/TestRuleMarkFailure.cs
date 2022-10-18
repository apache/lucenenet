#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete
using System;
using System.Collections.Generic;

namespace Lucene.Net.Util
{


    /*using AssumptionViolatedException = org.junit.@internal.AssumptionViolatedException;
    using TestRule = org.junit.rules.TestRule;
    using Description = org.junit.runner.Description;
    using Statement = org.junit.runners.model.Statement;*/

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
    /// A rule for marking failed tests and suites.
    /// </summary>
    public sealed class TestRuleMarkFailure : TestRule
    {
      private readonly TestRuleMarkFailure[] Chained;
      private volatile bool Failures;

      public TestRuleMarkFailure(params TestRuleMarkFailure[] chained)
      {
        this.Chained = chained;
      }

      public override Statement Apply(Statement s, Description d)
      {
        return new StatementAnonymousClass(this, s);
      }

      private sealed class StatementAnonymousClass : Statement
      {
          private readonly TestRuleMarkFailure OuterInstance;

          private Statement s;

          public StatementAnonymousClass(TestRuleMarkFailure outerInstance, Statement s)
          {
              this.OuterInstance = outerInstance;
              this.s = s;
          }

          public override void Evaluate()
          {
            // Clear status at start.
            OuterInstance.Failures = false;

            try
            {
              s.evaluate();
            }
            catch (Exception t)
            {
              if (!IsAssumption(t))
              {
                outerInstance.MarkFailed();
              }
              throw t;
            }
          }
      }

      /// <summary>
      /// Is a given exception (or a MultipleFailureException) an 
      /// <seealso cref="AssumptionViolatedException"/>?
      /// </summary>
      public static bool IsAssumption(Exception t)
      {
        foreach (Exception t2 in ExpandFromMultiple(t))
        {
          if (!(t2 is AssumptionViolatedException))
          {
            return false;
          }
        }
        return true;
      }

      /// <summary>
      /// Expand from multi-exception wrappers.
      /// </summary>
      private static IList<Exception> ExpandFromMultiple(Exception t)
      {
        return ExpandFromMultiple(t, new JCG.List<Exception>());
      }

      /// <summary>
      /// Internal recursive routine. </summary>
      private static IList<Exception> ExpandFromMultiple(Exception t, IList<Exception> list)
      {
        if (t is org.junit.runners.model.MultipleFailureException)
        {
          foreach (Exception sub in ((org.junit.runners.model.MultipleFailureException) t).Failures)
          {
            ExpandFromMultiple(sub, list);
          }
        }
        else
        {
          list.Add(t);
        }

        return list;
      }

      /// <summary>
      /// Taints this object and any chained as having failures.
      /// </summary>
      public void MarkFailed()
      {
        Failures = true;
        foreach (TestRuleMarkFailure next in Chained)
        {
          next.MarkFailed();
        }
      }

      /// <summary>
      /// Check if this object had any marked failures.
      /// </summary>
      public bool HadFailures()
      {
        return Failures;
      }

      /// <summary>
      /// Check if this object was successful (the opposite of <seealso cref="#hadFailures()"/>). 
      /// </summary>
      public bool WasSuccessful()
      {
        return !HadFailures();
      }
    }

}
#endif