#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete
using System.Threading;

namespace Lucene.Net.Util
{

    /*using TestRule = org.junit.rules.TestRule;
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
    /// Saves the executing thread and method name of the test case.
    /// </summary>
    internal sealed class TestRuleThreadAndTestName : TestRule
    {
      /// <summary>
      /// The thread executing the current test case. </summary>
      /// <seealso> cref= LuceneTestCase#isTestThread() </seealso>
      public volatile Thread TestCaseThread;

      /// <summary>
      /// Test method name.
      /// </summary>
      public volatile string TestMethodName = "<unknown>";

      public override Statement Apply(Statement @base, Description description)
      {
        return new StatementAnonymousClass(this, @base, description);
      }

      private sealed class StatementAnonymousClass : Statement
      {
          private readonly TestRuleThreadAndTestName OuterInstance;

          private Statement @base;
          private Description Description;

          public StatementAnonymousClass(TestRuleThreadAndTestName outerInstance, Statement @base, Description description)
          {
              this.OuterInstance = outerInstance;
              this.@base = @base;
              this.Description = description;
          }

          public override void Evaluate()
          {
            try
            {
              Thread current = Thread.CurrentThread;
              OuterInstance.TestCaseThread = current;
              OuterInstance.TestMethodName = Description.MethodName;

              @base.Evaluate();
            }
            finally
            {
              OuterInstance.TestCaseThread = null;
              OuterInstance.TestMethodName = null;
            }
          }
      }
    }
}
#endif