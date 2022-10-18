#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete
using System;
using System.Diagnostics;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Util
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

    /*using TestRule = org.junit.rules.TestRule;
    using Description = org.junit.runner.Description;
    using Statement = org.junit.runners.model.Statement;*/

    /// <summary>
    /// Require assertions for Lucene/Solr packages.
    /// </summary>
    public class TestRuleAssertionsRequired : TestRule
    {
      public override Statement Apply(Statement @base, Description description)
      {
        return new StatementAnonymousClass(this, @base, description);
      }

      private sealed class StatementAnonymousClass : Statement
      {
          private readonly TestRuleAssertionsRequired OuterInstance;

          private Statement @base;
          private Description Description;

          public StatementAnonymousClass(TestRuleAssertionsRequired outerInstance, Statement @base, Description description)
          {
              this.OuterInstance = outerInstance;
              this.@base = @base;
              this.Description = description;
          }

          public override void Evaluate()
          {
            try
            {
              if (Debugging.AssertsEnabled) Debugging.Assert(false);
              string msg = "Test class requires enabled assertions, enable globally (-ea)" + " or for Solr/Lucene subpackages only: " + Description.ClassName;
              Console.Error.WriteLine(msg);
              throw new Exception(msg);
            }
            catch (AssertionError e)
            {
              // Ok, enabled.
            }

            @base.evaluate();
          }
      }
    }

}
#endif