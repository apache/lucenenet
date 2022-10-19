#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Util
{

//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
    //import static Lucene.Net.Util.LuceneTestCase.*;


    /*using Description = org.junit.runner.Description;
    using Result = org.junit.runner.Result;
    using Failure = org.junit.runner.notification.Failure;
    using RunListener = org.junit.runner.notification.RunListener;

    using LifecycleScope = com.carrotsearch.randomizedtesting.LifecycleScope;
    using RandomizedContext = com.carrotsearch.randomizedtesting.RandomizedContext;*/

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
    /// A suite listener printing a "reproduce string". this ensures test result
    /// events are always captured properly even if exceptions happen at
    /// initialization or suite/ hooks level.
    /// </summary>
    public sealed class RunListenerPrintReproduceInfo : RunListener
    {
      /// <summary>
      /// A list of all test suite classes executed so far in this JVM (ehm, 
      /// under this class's classloader).
      /// </summary>
      private static IList<string> TestClassesRun = new JCG.List<string>();

      /// <summary>
      /// The currently executing scope.
      /// </summary>
      private LifecycleScope Scope;

      /// <summary>
      /// Current test failed. </summary>
      private bool TestFailed;

      /// <summary>
      /// Suite-level code (initialization, rule, hook) failed. </summary>
      private bool SuiteFailed;

      /// <summary>
      /// A marker to print full env. diagnostics after the suite. </summary>
      private bool PrintDiagnosticsAfterClass;


      public override void TestRunStarted(Description description)
      {
        SuiteFailed = false;
        TestFailed = false;
        Scope = LifecycleScope.SUITE;

        Type targetClass = RandomizedContext.current().TargetClass;
        TestClassesRun.Add(targetClass.Name);
      }

      public override void TestStarted(Description description)
      {
        this.TestFailed = false;
        this.Scope = LifecycleScope.TEST;
      }

      public override void TestFailure(Failure failure)
      {
        if (Scope == LifecycleScope.TEST)
        {
          TestFailed = true;
        }
        else
        {
          SuiteFailed = true;
        }
        PrintDiagnosticsAfterClass = true;
      }

      public override void TestFinished(Description description)
      {
        if (TestFailed)
        {
          ReportAdditionalFailureInfo(StripTestNameAugmentations(description.MethodName));
        }
        Scope = LifecycleScope.SUITE;
        TestFailed = false;
      }

      /// <summary>
      /// The <seealso cref="Description"/> object in JUnit does not expose the actual test method,
      /// instead it has the concept of a unique "name" of a test. To run the same method (tests)
      /// repeatedly, randomizedtesting must make those "names" unique: it appends the current iteration
      /// and seeds to the test method's name. We strip this information here.   
      /// </summary>
      private string StripTestNameAugmentations(string methodName)
      {
        if (methodName != null)
        {
          methodName = methodName.replaceAll("\\s*\\{.+?\\}", "");
        }
        return methodName;
      }

      public override void TestRunFinished(Result result)
      {
        if (PrintDiagnosticsAfterClass || LuceneTestCase.VERBOSE)
        {
          RunListenerPrintReproduceInfo.PrintDebuggingInformation();
        }

        if (SuiteFailed)
        {
          ReportAdditionalFailureInfo(null);
        }
      }

      /// <summary>
      /// print some useful debugging information about the environment </summary>
      private static void PrintDebuggingInformation()
      {
        if (classEnvRule != null)
        {
          Console.Error.WriteLine("NOTE: test params are: codec=" + classEnvRule.codec + ", sim=" + classEnvRule.similarity + ", locale=" + classEnvRule.locale + ", timezone=" + (classEnvRule.timeZone is null ? "(null)" : classEnvRule.timeZone.ID));
        }
        Console.Error.WriteLine("NOTE: " + System.getProperty("os.name") + " " + System.getProperty("os.version") + " " + System.getProperty("os.arch") + "/" + System.getProperty("java.vendor") + " " + System.getProperty("java.version") + " " + (Constants.JRE_IS_64BIT ? "(64-bit)" : "(32-bit)") + "/" + "cpus=" + Runtime.Runtime.availableProcessors() + "," + "threads=" + Thread.activeCount() + "," + "free=" + Runtime.Runtime.freeMemory() + "," + "total=" + Runtime.Runtime.totalMemory());
        Console.Error.WriteLine("NOTE: All tests run in this JVM: " + Arrays.ToString(TestClassesRun.ToArray()));
      }

      private void ReportAdditionalFailureInfo(string testName)
      {
        if (TEST_LINE_DOCS_FILE.EndsWith(JENKINS_LARGE_LINE_DOCS_FILE))
        {
          Console.Error.WriteLine("NOTE: download the large Jenkins line-docs file by running " + "'ant get-jenkins-line-docs' in the lucene directory.");
        }

        StringBuilder b = new StringBuilder();
        b.Append("NOTE: reproduce with: ant test ");

        // Test case, method, seed.
        AddVmOpt(b, "testcase", RandomizedContext.current().TargetClass.SimpleName);
        AddVmOpt(b, "tests.method", testName);
        AddVmOpt(b, "tests.seed", RandomizedContext.current().RunnerSeedAsString);

        // Test groups and multipliers.
        if (RANDOM_MULTIPLIER > 1)
        {
            AddVmOpt(b, "tests.multiplier", RANDOM_MULTIPLIER);
        }
        if (TEST_NIGHTLY)
        {
            AddVmOpt(b, SYSPROP_NIGHTLY, TEST_NIGHTLY);
        }
        if (TEST_WEEKLY)
        {
            AddVmOpt(b, SYSPROP_WEEKLY, TEST_WEEKLY);
        }
        if (TEST_SLOW)
        {
            AddVmOpt(b, SYSPROP_SLOW, TEST_SLOW);
        }
        if (TEST_AWAITSFIX)
        {
            AddVmOpt(b, SYSPROP_AWAITSFIX, TEST_AWAITSFIX);
        }

        // Codec, postings, directories.
        if (!TEST_CODEC.Equals("random"))
        {
            AddVmOpt(b, "tests.codec", TEST_CODEC);
        }
        if (!TEST_POSTINGSFORMAT.Equals("random"))
        {
            AddVmOpt(b, "tests.postingsformat", TEST_POSTINGSFORMAT);
        }
        if (!TEST_DOCVALUESFORMAT.Equals("random"))
        {
            AddVmOpt(b, "tests.docvaluesformat", TEST_DOCVALUESFORMAT);
        }
        if (!TEST_DIRECTORY.Equals("random"))
        {
            AddVmOpt(b, "tests.directory", TEST_DIRECTORY);
        }

        // Environment.
        if (!TEST_LINE_DOCS_FILE.Equals(DEFAULT_LINE_DOCS_FILE))
        {
            AddVmOpt(b, "tests.linedocsfile", TEST_LINE_DOCS_FILE);
        }
        if (classEnvRule != null)
        {
          AddVmOpt(b, "tests.locale", classEnvRule.locale);
          if (classEnvRule.timeZone != null)
          {
            AddVmOpt(b, "tests.timezone", classEnvRule.timeZone.ID);
          }
        }

        AddVmOpt(b, "tests.file.encoding", System.getProperty("file.encoding"));

        Console.Error.WriteLine(b.ToString());
      }

      /// <summary>
      /// Append a VM option (-Dkey=value) to a <seealso cref="StringBuilder"/>. Add quotes if 
      /// spaces or other funky characters are detected.
      /// </summary>
      internal static void AddVmOpt(StringBuilder b, string key, object value)
      {
        if (value is null)
        {
            return;
        }

        b.Append(" -D").Append(key).Append('=');
        string v = value.ToString();
        // Add simplistic quoting. this varies a lot from system to system and between
        // shells... ANT should have some code for doing it properly.
        if (Pattern.compile("[\\s=']").matcher(v).find())
        {
          v = '"' + v + '"';
        }
        b.Append(v);
      }
    }

}
#endif