#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete

using System;

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

    using FieldCache = Lucene.Net.Search.FieldCache;
    /*using TestRule = org.junit.rules.TestRule;
    using Description = org.junit.runner.Description;
    using Statement = org.junit.runners.model.Statement;*/

    /// <summary>
    /// this rule will fail the test if it has insane field caches.
    /// <p>
    /// calling assertSaneFieldCaches here isn't as useful as having test
    /// classes call it directly from the scope where the index readers
    /// are used, because they could be gc'ed just before this tearDown
    /// method is called.
    /// <p>
    /// But it's better then nothing.
    /// <p>
    /// If you are testing functionality that you know for a fact
    /// "violates" FieldCache sanity, then you should either explicitly
    /// call purgeFieldCache at the end of your test method, or refactor
    /// your Test class so that the inconsistent FieldCache usages are
    /// isolated in distinct test methods
    /// </summary>
    /// <seealso cref= FieldCacheSanityChecker </seealso>
    public class TestRuleFieldCacheSanity : TestRule
    {

      public override Statement Apply(Statement s, Description d)
      {
        return new StatementAnonymousClass(this, s, d);
      }

      private sealed class StatementAnonymousClass : Statement
      {
          private readonly TestRuleFieldCacheSanity OuterInstance;

          private Statement s;
          private Description d;

          public StatementAnonymousClass(TestRuleFieldCacheSanity outerInstance, Statement s, Description d)
          {
              this.OuterInstance = outerInstance;
              this.s = s;
              this.d = d;
          }

          public override void Evaluate()
          {
            s.evaluate();

            Exception problem = null;
            try
            {
              LuceneTestCase.AssertSaneFieldCaches(d.DisplayName);
            }
            catch (Exception t)
            {
              problem = t;
            }

            FieldCache.DEFAULT.purgeAllCaches();

            if (problem != null)
            {
              Rethrow.Rethrow(problem);
            }
          }
      }
    }

}
#endif