#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete

using Lucene.Net.Support;
namespace Lucene.Net.Util
{

    /*using Failure = org.junit.runner.notification.Failure;
    using RunListener = org.junit.runner.notification.RunListener;*/

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
    /// A <seealso cref="RunListener"/> that detects suite/ test failures. We need it because failures
    /// due to thread leaks happen outside of any rule contexts.
    /// </summary>
    public class FailureMarker : RunListener
    {
      internal static readonly AtomicInteger failures = new AtomicInteger();

      public override void TestFailure(Failure failure)
      {
        failures.IncrementAndGet();
      }

      public static bool HadFailures()
      {
        return failures.Get() > 0;
      }

      internal static int Failures
      {
          get
          {
            return failures.Get();
          }
      }

      public static void ResetFailures()
      {
        failures.Set(0);
      }
    }

}
#endif