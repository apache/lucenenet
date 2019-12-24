#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete
using Apache.NMS.Util;

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
    /// A <seealso cref="TestRule"/> that delegates to another <seealso cref="TestRule"/> via a delegate
    /// contained in a an <seealso cref="AtomicReference"/>.
    /// </summary>
    internal sealed class TestRuleDelegate<T> : TestRule where T : TestRule
    {
      private AtomicReference<T> @delegate;

      private TestRuleDelegate(AtomicReference<T> @delegate)
      {
        this.@delegate = @delegate;
      }

      public override Statement Apply(Statement s, Description d)
      {
        return @delegate.GetType().apply(s, d);
      }

      internal static TestRuleDelegate<T> of<T>(AtomicReference<T> @delegate) where T : TestRule
      {
        return new TestRuleDelegate<T>(@delegate);
      }
    }

}
#endif