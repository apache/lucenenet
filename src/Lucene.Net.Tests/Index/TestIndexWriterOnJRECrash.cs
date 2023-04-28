// Lucene version compatibility level 4.8.1
using J2N.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Index
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
    /// Runs TestNRTThreads in a separate process, crashes the JRE in the middle
    /// of execution, then runs checkindex to make sure its not corrupt.
    /// </summary>
    [SuppressCodecs("SimpleText", "Memory", "Direct", "Lucene3x")]
    [TestFixture]
    public class TestIndexWriterOnJRECrash : TestNRTThreads
    {
        // LUCENENET: Setup unnecessary because we create a new temp directory
        // in each iteration of the test.

        [Test]
        [Slow]
        public override void TestNRTThreads_Mem()
        {
            //if we are not the fork
            if (SystemProperties.GetProperty("tests:crashmode") is null)
            {
                // try up to 10 times to create an index
                for (int i = 0; i < 10; i++)
                {
                    // LUCENENET: We create a new temp folder on each iteration to ensure
                    // there is only 1 index to check below it. Otherwise we could just
                    // get a cascade of empty checks because the first one contains an index that
                    // crashed upon the first commit. The subdirectories would otherwise be checked in
                    // lexicographical order rather than checking the one we create in the current iteration.
                    DirectoryInfo tempDir = CreateTempDir("netcrash");

                    FileInfo tempProcessToKillFile = CreateTempFile(prefix: "netcrash-processToKill", suffix: ".txt");
                    tempProcessToKillFile.Delete(); // We use the creation of this file as a signal to parse it.

                    // Note this is the vstest.console process we are tracking here.
                    Process p = ForkTest(tempDir.FullName, tempProcessToKillFile.FullName);

                    TextWriter childOut = BeginOutput(p, out ThreadJob stdOutPumper, out ThreadJob stdErrPumper);

                    // LUCENENET: Note that ForkTest() creates the vstest.console.exe process.
                    // This spawns testhost.exe, which runs our test. We wait until
                    // the process starts and logs its own Id so we know who to kill later.
                    int processIdToKill = WaitForProcessToKillLogFile(tempProcessToKillFile.FullName);

                    // Setup a time to crash the forked thread
                    int crashTime = TestUtil.NextInt32(Random, 4000, 5000); // LUCENENET: Adjusted these up by 1 second to give our tests some more time to spin up
                    ThreadJob t = new ThreadAnonymousClass(this, crashTime, processIdToKill);

                    t.Priority = ThreadPriority.Highest;
                    t.Start();
                    t.Join(); // Wait for our thread to kill the other process

                    // if we succeeded in finding an index, we are done.
                    if (CheckIndexes(tempDir))
                    {
                        EndOutput(p, childOut, stdOutPumper, stdErrPumper);
                        return;
                    }
                    EndOutput(p, childOut, stdOutPumper, stdErrPumper);
                }
            }
            else
            {
                // LUCENENET specific - suppressing the Lucene3x codec with[SuppressCodecs] at
                // the top of this file so we can always run the fork.

                // we are the fork, log our processId so the original test can kill us.
                int processIdToKill = Process.GetCurrentProcess().Id;
                string processIdToKillFile = SystemProperties.GetProperty("tests:tempProcessToKillFile");

                assertNotNull("No tests:tempProcessToKillFile value was passed to the fork. This is a required system property.", processIdToKillFile);

                // Writing this file will kick off the thread that crashes us.
                using (var writer = new StreamWriter(processIdToKillFile, append: false, Encoding.UTF8, bufferSize: 32))
                    writer.WriteLine(processIdToKill.ToString(CultureInfo.InvariantCulture));

                // run the test until we crash.
                for (int i = 0; i < 100; i++)
                {
                    base.TestNRTThreads_Mem();
                }

            }
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestIndexWriterOnJRECrash outerInstance;

            private readonly int crashTime;
            private readonly int processIdToKill;

            public ThreadAnonymousClass(TestIndexWriterOnJRECrash outerInstance, int crashTime, int processIdToKill)
            {
                this.outerInstance = outerInstance;
                this.crashTime = crashTime;
                this.processIdToKill = processIdToKill;
            }

            public override void Run()
            {
                try
                {
                    Thread.Sleep(crashTime);
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                }
                // Time to crash
                outerInstance.CrashDotNet(processIdToKill);
            }
        }

        public Process ForkTest(string tempDir, string tempProcessToKillFile)
        {
            //get the full location of the assembly with DaoTests in it
            string testAssemblyPath = Assembly.GetAssembly(typeof(TestIndexWriterOnJRECrash)).Location;

            //get the folder that's in
            string theDirectory = Path.GetDirectoryName(testAssemblyPath);
            // Set up the process to run the console app
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = string.Join(" ", new[] {
                    // LUCENENET NOTE: dotnet test doesn't need the --no-build flag since we are passing the DLL path in
                    "test", testAssemblyPath,
                    "--framework", GetTargetFramework(),
                    "--filter", nameof(TestIndexWriterOnJRECrash),
                    "--logger:\"console;verbosity=normal\"",
                    "--",
                    $"RunConfiguration.TargetPlatform={GetTargetPlatform()}",
                    // LUCENENET NOTE: Since in our CI environment we create a lucene.testSettings.config file
                    // for all tests, we need to pass some of these settings as test run parameters to override
                    // for this process. These are read as system properties on the inside of the application.
                    TestRunParameter("assert", "true"),
                    TestRunParameter("tests:seed", SeedUtils.FormatSeed(Random.NextInt64())),
                    TestRunParameter("tests:culture", Thread.CurrentThread.CurrentCulture.Name),
                    TestRunParameter("tests:crashmode", "true"),
                    // passing NIGHTLY to this test makes it run for much longer, easier to catch it in the act...
                    TestRunParameter("tests:nightly", "true"),
                    TestRunParameter("tempDir", tempDir),
                    // This file is for passing the process ID of the fork back to the original test so it can kill it.
                    TestRunParameter("tests:tempProcessToKillFile", tempProcessToKillFile),
                }),
                WorkingDirectory = theDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            Process p = Process.Start(startInfo);

            //// LUCENENET: For debugging, it is helpful to do this sometimes
            //var stdOut = p.StandardOutput.ReadToEnd();
            //var stdErr = p.StandardError.ReadToEnd();

            return p;
        }

        private static string TestRunParameter(string name, string value)
        {
            // See: https://github.com/microsoft/vstest/issues/862#issuecomment-621737720
            return $"TestRunParameters.Parameter(name=\\\"{Escape(name)}\\\", value=\\\"{Escape(value)}\\\")";
        }

        private static string Escape(string value)
            => value.Replace(Space, string.Concat(BackSlash, Space));

        private const string BackSlash = "\\";
        private const string Space = " ";

        private TextWriter BeginOutput(Process p, out ThreadJob stdOutPumper, out ThreadJob stdErrPumper)
        {
            // We pump everything to stderr.
            TextWriter childOut = Console.Error;
            stdOutPumper = ThreadPumper.Start(p.StandardOutput, childOut);
            stdErrPumper = ThreadPumper.Start(p.StandardError, childOut);
            if (Verbose) childOut.WriteLine(">>> Begin subprocess output");
            return childOut;
        }

        private void EndOutput(Process p, TextWriter childOut, ThreadJob stdOutPumper, ThreadJob stdErrPumper)
        {
            p.WaitForExit(10000);
            stdOutPumper.Join();
            stdErrPumper.Join();
            if (Verbose) childOut.WriteLine("<<< End subprocess output");
        }

        private string GetTargetFramework()
        {
            var targetFrameworkAttribute = GetType().Assembly.GetAttributes<System.Reflection.AssemblyMetadataAttribute>(inherit: false).Where(a => a.Key == "TargetFramework").FirstOrDefault();
            if (targetFrameworkAttribute is null)
                Assert.Fail("TargetFramework metadata not found in this assembly.");
            return targetFrameworkAttribute.Value;
        }

        private string GetTargetPlatform()
        {
            return Environment.Is64BitProcess ? "x64" : "x86";
        }

        /// <summary>
        /// A pipe thread. It'd be nice to reuse guava's implementation for this... </summary>
        internal static class ThreadPumper
        {
            public static ThreadJob Start(TextReader from, TextWriter to)
            {
                ThreadJob t = new ThreadPumperAnonymousClass(from, to);
                t.Start();
                return t;
            }

            private sealed class ThreadPumperAnonymousClass : ThreadJob
            {
                private TextReader from;
                private TextWriter to;

                public ThreadPumperAnonymousClass(TextReader from, TextWriter to)
                {
                    this.from = from;
                    this.to = to;
                }

                public override void Run()
                {
                    try
                    {
                        char[] buffer = new char[1024];
                        int len;
                        while ((len = from.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (Verbose)
                            {
                                to.Write(buffer, 0, len);
                            }
                        }
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        Console.Error.WriteLine("Couldn't pipe from the forked process: " + e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Recursively looks for indexes underneath <code>file</code>,
        /// and runs checkindex on them. returns true if it found any indexes.
        /// </summary>
        /// <remarks>
        /// LUCENENET: Since our base class will create an index, process it, then delete
        /// it until it crashes, we don't expect there to be more than one index
        /// when we get here. So, we return <c>true</c> on the first index found and
        /// checked. There are a couple of cases where we don't check the index:
        /// <list type="number">
        ///     <item><description>If the index has no _segments files.</description></item>
        ///     <item><description>If GetLastCommitGeneration() == 1.</description></item>
        /// </list>
        /// In both of these cases, the index is considered valid, but
        /// we skip the check because there is no way to check it for corruption.
        /// This is why our test will try up to 10 times to get an index to check.
        /// <para/>
        /// When an index is corrupt, we will get an exception from
        /// <see cref="TestUtil.CheckIndex(Store.Directory)"/> to fail the test.
        /// </remarks>
        public virtual bool CheckIndexes(FileSystemInfo file)
        {
            if (file is DirectoryInfo directoryInfo)
            {
                BaseDirectoryWrapper dir = NewFSDirectory(directoryInfo);
                dir.CheckIndexOnDispose = false; // don't double-checkindex
                if (DirectoryReader.IndexExists(dir))
                {
                    if (Verbose)
                    {
                        Console.Error.WriteLine("Checking index: " + file);
                    }
                    // LUCENE-4738: if we crashed while writing first
                    // commit it's possible index will be corrupt (by
                    // design we don't try to be smart about this case
                    // since that too risky):
                    if (SegmentInfos.GetLastCommitGeneration(dir) > 1)
                    {
                        TestUtil.CheckIndex(dir);
                    }
                    return true;
                }
                dir.Dispose();
                foreach (DirectoryInfo f in directoryInfo.EnumerateDirectories())
                {
                    if (CheckIndexes(f))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // LUCENENET: Wait for our test to spin up and log its PID so we can kill it.
        private int WaitForProcessToKillLogFile(string processToKillFile)
        {
            bool exists = false;
            Thread.Sleep(500);
            for (int i = 0; i < 150; i++)
            {
                if (File.Exists(processToKillFile))
                {
                    exists = true;
                    break;
                }
                Thread.Sleep(200);
            }
            // If the fork didn't log its process id, it is a failure.
            assertTrue("The test fork didn't log its process id, so we cannot kill it", exists);
            using var reader = new StreamReader(processToKillFile, Encoding.UTF8);
            // LUCENENET: Our file only has one line with the process Id in it
            return int.Parse(reader.ReadLine().Trim(), CultureInfo.InvariantCulture);
        }

        public virtual void CrashDotNet(int processIdToKill)
        {
            Process process = null;
            try
            {
                process = Process.GetProcessById(processIdToKill);
            }
            catch (ArgumentException)
            {
                // We get here if the process wasn't running for some reason.
                // We should fix the forked test to make it run longer if we get here.
                fail("The test completed before we could kill it.");
            }
#if FEATURE_PROCESS_KILL_ENTIREPROCESSTREE
            process.Kill(entireProcessTree: true);
#else
            process.Kill();
#endif
            process.WaitForExit(10000);
            // We couldn't get .NET to crash for some reason.
            assertTrue(process.HasExited);
        }
    }
}
