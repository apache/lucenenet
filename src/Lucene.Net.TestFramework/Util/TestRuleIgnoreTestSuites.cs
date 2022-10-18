#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete
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
    /// this rule will cause the suite to be assumption-ignored if 
    /// the test class implements a given marker interface and a special
    /// property is not set.
    /// 
    /// <p>this is a workaround for problems with certain JUnit containers (IntelliJ)
    /// which automatically discover test suites and attempt to run nested classes
    /// that we use for testing the test framework itself.
    /// </summary>
    public sealed class TestRuleIgnoreTestSuites : TestRule
    {
      /// <summary>
      /// Marker interface for nested suites that should be ignored
      /// if executed in stand-alone mode.
      /// </summary>
      public interface NestedTestSuite
      {
      }

      /// <summary>
      /// A boolean system property indicating nested suites should be executed
      /// normally.
      /// </summary>
      public const string PROPERTY_RUN_NESTED = "tests.runnested";

      public override Statement Apply(Statement s, Description d)
      {
        return new StatementAnonymousClass(this, s, d);
      }

      private sealed class StatementAnonymousClass : Statement
      {
          private readonly TestRuleIgnoreTestSuites OuterInstance;

          private Statement s;
          private Description d;

          public StatementAnonymousClass(TestRuleIgnoreTestSuites outerInstance, Statement s, Description d)
          {
              this.OuterInstance = outerInstance;
              this.s = s;
              this.d = d;
          }

          public override void Evaluate()
          {
            if (d.TestClass.IsSubclassOf(typeof(NestedTestSuite)))
            {
              LuceneTestCase.AssumeTrue("Nested suite class ignored (started as stand-alone).", RunningNested);
            }
            s.evaluate();
          }
      }

      /// <summary>
      /// Check if a suite class is running as a nested test.
      /// </summary>
      public static bool RunningNested
      {
          get
          {
            return bool.getBoolean(PROPERTY_RUN_NESTED);
          }
      }
    }

}
#endif