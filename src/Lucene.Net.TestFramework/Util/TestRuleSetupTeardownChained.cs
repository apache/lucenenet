#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete

namespace Lucene.Net.Util
{

    /*using Assert = org.junit.Assert;
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
    /// Make sure <seealso cref="LuceneTestCase#setUp()"/> and <seealso cref="LuceneTestCase#tearDown()"/> were invoked even if they
    /// have been overriden. We assume nobody will call these out of non-overriden
    /// methods (they have to be public by contract, unfortunately). The top-level
    /// methods just set a flag that is checked upon successful execution of each test
    /// case.
    /// </summary>
    internal class TestRuleSetupTeardownChained : TestRule
    {
      /// <seealso cref= TestRuleSetupTeardownChained   </seealso>
      public bool SetupCalled;

      /// <seealso cref= TestRuleSetupTeardownChained </seealso>
      public bool TeardownCalled;

      public override Statement Apply(Statement @base, Description description)
      {
        return new StatementAnonymousClass(this, @base);
      }

      private sealed class StatementAnonymousClass : Statement
      {
          private readonly TestRuleSetupTeardownChained OuterInstance;

          private Statement @base;

          public StatementAnonymousClass(TestRuleSetupTeardownChained outerInstance, Statement @base)
          {
              this.OuterInstance = outerInstance;
              this.@base = @base;
          }

          public override void Evaluate()
          {
            OuterInstance.SetupCalled = false;
            OuterInstance.TeardownCalled = false;
            @base.evaluate();

            // I assume we don't want to check teardown chaining if something happens in the
            // test because this would obscure the original exception?
            if (!OuterInstance.SetupCalled)
            {
              Assert.Fail("One of the overrides of setUp does not propagate the call.");
            }
            if (!OuterInstance.TeardownCalled)
            {
              Assert.Fail("One of the overrides of tearDown does not propagate the call.");
            }
          }
      }
    }
}
#endif