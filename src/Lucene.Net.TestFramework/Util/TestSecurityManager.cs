#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete
using System.Threading;

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

    /// <summary>
    /// A <seealso cref="SecurityManager"/> that prevents tests calling <seealso cref="System#exit(int)"/>.
    /// Only the test runner itself is allowed to exit the JVM.
    /// All other security checks are handled by the default security policy.
    /// <p>
    /// Use this with {@code -Djava.security.manager=Lucene.Net.Util.TestSecurityManager}.
    /// </summary>
    public sealed class TestSecurityManager : SecurityManager
    {

      internal const string TEST_RUNNER_PACKAGE = "com.carrotsearch.ant.tasks.junit4.";

      /// <summary>
      /// Creates a new TestSecurityManager. this ctor is called on JVM startup,
      /// when {@code -Djava.security.manager=Lucene.Net.Util.TestSecurityManager}
      /// is passed to JVM.
      /// </summary>
      public TestSecurityManager() : base()
      {
      }

      /// <summary>
      /// {@inheritDoc}
      /// <p>this method inspects the stack trace and checks who is calling
      /// <seealso cref="System#exit(int)"/> and similar methods </summary>
      /// <exception cref="SecurityException"> if the caller of this method is not the test runner itself. </exception>
      public override void CheckExit(int status)
      {
        AccessController.doPrivileged(new PrivilegedActionAnonymousClass(this, status));

        // we passed the stack check, delegate to super, so default policy can still deny permission:
        base.CheckExit(status);
      }

      private sealed class PrivilegedActionAnonymousClass : PrivilegedAction<Void>
      {
          private readonly TestSecurityManager OuterInstance;

          private int Status;

          public PrivilegedActionAnonymousClass(TestSecurityManager outerInstance, int status)
          {
              this.OuterInstance = outerInstance;
              this.Status = status;
          }

          public override void Run()
          {
            const string systemClassName = typeof(System).Name, runtimeClassName = typeof(Runtime).Name;
            string exitMethodHit = null;
            foreach (StackTraceElement se in Thread.CurrentThread.StackTrace)
            {
              const string className = se.ClassName, methodName = se.MethodName;
              if (("exit".Equals(methodName) || "halt".Equals(methodName)) && (systemClassName.Equals(className) || runtimeClassName.Equals(className)))
              {
                exitMethodHit = className + '#' + methodName + '(' + Status + ')';
                continue;
              }

              if (exitMethodHit != null)
              {
                if (className.StartsWith(TEST_RUNNER_PACKAGE))
                {
                  // this exit point is allowed, we return normally from closure:
                  return null; //void
                }
                else
                {
                  // anything else in stack trace is not allowed, break and throw SecurityException below:
                  break;
                }
              }
            }

            if (exitMethodHit is null)
            {
              // should never happen, only if JVM hides stack trace - replace by generic:
              exitMethodHit = "JVM exit method";
            }
            throw new SecurityException(exitMethodHit + " calls are not allowed because they terminate the test runner's JVM.");
          }
      }

    }

}
#endif