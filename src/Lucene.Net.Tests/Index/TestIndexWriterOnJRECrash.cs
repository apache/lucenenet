// Lucene version compatibility level 4.8.1
using J2N.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
using Assert = Lucene.Net.TestFramework.Assert;

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
        // LUCENENET: Setup of directory unnecessary because we create a new temp directory
        // in each iteration of the test.

        [Test]
        [Slow]
        public override void TestNRTThreads_Mem()
        {
            //if we are not the fork
            if (!SystemProperties.GetPropertyAsBoolean("tests:crashmode", false))
            {
                // LUCENENET: Bail out early (Inconclusive) if the current platform can't launch the fork,
                // rather than hanging in WaitForProcessId waiting for a process that will never start.
                EnsureForkPlatformSupported();

                // try up to 10 times to create an index
                for (int i = 0; i < 10; i++)
                {
                    // LUCENENET: We create a new temp folder on each iteration to ensure
                    // there is only 1 index to check below it. Otherwise we could just
                    // get a cascade of empty checks because the first one contains an index that
                    // crashed upon the first commit. The subdirectories would otherwise be checked in
                    // lexicographical order rather than checking the one we create in the current iteration.
                    DirectoryInfo tempDir = CreateTempDir("netcrash");

                    // Set up a TCP listener to receive the process ID
                    TcpListener listener = SetupSocketListener();
                    Process p = null;
                    try
                    {
                        // Get the port that we picked at random.
                        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

                        // Note this is the vstest.console process we are tracking here.
                        p = ForkTest(tempDir.FullName, port);

                        // LUCENENET: Capture STDERR so we can report it if the fork fails to start or
                        // exits with a non-zero exit code.
                        StringBuilder stdErrCapture = new StringBuilder();
                        TextWriter childOut = BeginOutput(p, stdErrCapture, out ThreadJob stdOutPumper, out ThreadJob stdErrPumper);

                        // LUCENENET: Note that ForkTest() creates the vstest.console.exe process.
                        // This spawns testhost.exe, which runs our test. We wait until
                        // the process starts and transmits its own PID so we know who to kill later.
                        // If the fork exits before connecting (e.g. a build/launch error), this throws
                        // with the fork's exit code and STDERR rather than hanging indefinitely.
                        int processIdToKill = WaitForProcessId(listener, p, stdErrCapture);

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
                    finally
                    {
                        listener.Stop();
                        p?.Dispose();
                    }
                }
            }
            else
            {
                // LUCENENET specific - suppressing the Lucene3x codec with[SuppressCodecs] at
                // the top of this file so we can always run the fork.

                // we are the fork, log our processId so the original test can kill us.
                int processIdToKill = Process.GetCurrentProcess().Id;
                int port = SystemProperties.GetPropertyAsInt32("tests:crashtestport");

                assertTrue("No tests:crashtestport value was passed to the fork. This is a required system property.", port > 0);

                // Sending the process id will kick off the thread that crashes us.
                SendProcessId(processIdToKill, port);

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

        public Process ForkTest(string tempDir, int port)
        {
            //get the full location of the assembly with DaoTests in it
            string testAssemblyPath = Assembly.GetAssembly(typeof(TestIndexWriterOnJRECrash)).Location;

            //get the folder that's in
            string theDirectory = Path.GetDirectoryName(testAssemblyPath);
            // LUCENENET: Only constrain the target platform when running as x86. Since .NET 8, an x86
            // `dotnet test` fork will not run unless the 32-bit SDK is installed separately (the 64-bit
            // SDK is no longer sufficient), so we verify it up front and skip the test if it is missing
            // rather than hanging forever in WaitForProcessId. For x64/ARM64 we leave the platform off and
            // let vstest auto-detect a compatible host, which avoids a similar "Could not find 'dotnet'
            // host for the 'X64' architecture" hang on ARM64 hosts.
            var arguments = new List<string>
            {
                // LUCENENET NOTE: dotnet test doesn't need the --no-build flag since we are passing the DLL path in
                "test", testAssemblyPath,
                "--framework", GetTargetFramework(),
                "--filter", nameof(TestIndexWriterOnJRECrash),
                "--logger:\"console;verbosity=normal\"",
                "--",
            };

            string targetPlatform = GetTargetPlatform();
            if (targetPlatform != null)
            {
                arguments.Add($"RunConfiguration.TargetPlatform={targetPlatform}");
            }

            // LUCENENET NOTE: Since in our CI environment we create a lucene.testsettings.json file
            // for all tests, we need to pass some of these settings as test run parameters to override
            // for this process. These are read as system properties on the inside of the application.
            arguments.Add(TestRunParameter("assert", "true"));
            arguments.Add(TestRunParameter("tests:seed", SeedUtils.FormatSeed(Random.NextInt64())));
            arguments.Add(TestRunParameter("tests:culture", Thread.CurrentThread.CurrentCulture.Name));
            arguments.Add(TestRunParameter("tests:crashmode", "true"));
            // passing NIGHTLY to this test makes it run for much longer, easier to catch it in the act...
            arguments.Add(TestRunParameter("tests:nightly", "true"));
            arguments.Add(TestRunParameter("tempDir", tempDir));
            // This port is for passing the process ID of the fork back to the original test so it can kill it.
            arguments.Add(TestRunParameter("tests:crashtestport", port.ToString(CultureInfo.InvariantCulture)));

            // Set up the process to run the console app
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = string.Join(" ", arguments),
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

        #region LUCENENET-specific methods for ForkTest
        private static string TestRunParameter(string name, string value)
        {
            // See: https://github.com/microsoft/vstest/issues/862#issuecomment-621737720
            return $"TestRunParameters.Parameter(name=\\\"{Escape(name)}\\\", value=\\\"{Escape(value)}\\\")";
        }

        private static string Escape(string value)
            => value.Replace(Space, string.Concat(BackSlash, Space));

        private const string BackSlash = "\\";
        private const string Space = " ";

        private static TextWriter BeginOutput(Process p, StringBuilder stdErrCapture, out ThreadJob stdOutPumper, out ThreadJob stdErrPumper)
        {
            // We pump everything to stderr.
            TextWriter childOut = Console.Error;
            stdOutPumper = ThreadPumper.Start(p.StandardOutput, childOut, capture: null);
            // LUCENENET: Capture the fork's STDERR so it can be surfaced if the fork fails.
            stdErrPumper = ThreadPumper.Start(p.StandardError, childOut, capture: stdErrCapture);
            if (Verbose) childOut.WriteLine(">>> Begin subprocess output");
            return childOut;
        }

        private static void EndOutput(Process p, TextWriter childOut, ThreadJob stdOutPumper, ThreadJob stdErrPumper)
        {
            p.WaitForExit(10000);
            stdOutPumper.Join();
            stdErrPumper.Join();
            if (Verbose) childOut.WriteLine("<<< End subprocess output");
        }

        private string GetTargetFramework()
        {
            var targetFrameworkAttribute = GetType().Assembly.GetAttributes<AssemblyMetadataAttribute>(inherit: false).FirstOrDefault(a => a.Key == "TargetFramework");
            if (targetFrameworkAttribute is null)
                Assert.Fail("TargetFramework metadata not found in this assembly.");
            return targetFrameworkAttribute.Value;
        }

        private static string GetTargetPlatform()
        {
            // LUCENENET: Only x86 needs an explicit target platform. The forked vstest can otherwise
            // auto-detect a compatible dotnet host for the current architecture; forcing a platform on
            // x64/ARM64 risks the fork failing with "Could not find 'dotnet' host for the '<arch>'
            // architecture" (which left the parent blocked forever in WaitForProcessId, e.g. on ARM64
            // hosts), so we return null to leave RunConfiguration.TargetPlatform unset for those.
            //
            // For x86 we must verify the 32-bit SDK is actually installed: since .NET 8, an x86
            // `dotnet test` fork will not run unless the 32-bit SDK is installed separately. If it is
            // missing, EnsureForkPlatformSupported() makes the test inconclusive rather than letting it
            // hang waiting for a fork that can never start.
            if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
            {
                return "x86";
            }

            return null;
        }

        // LUCENENET: Verify the runtime/SDK needed to launch the fork on the current platform is present,
        // marking the test Inconclusive (rather than hanging) when it is not. Currently this only applies
        // to x86, where the 32-bit .NET SDK must be installed separately since .NET 8.
        private static void EnsureForkPlatformSupported()
        {
            if (RuntimeInformation.ProcessArchitecture != Architecture.X86)
            {
                return;
            }

            // The x86 SDK installs under "Program Files (x86)\dotnet". On 64-bit Windows this path exists
            // only when the 32-bit SDK has been installed in addition to the 64-bit one.
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string x86DotnetExe = string.IsNullOrEmpty(programFilesX86)
                ? null
                : Path.Combine(programFilesX86, "dotnet", "dotnet.exe");

            if (x86DotnetExe is null || !File.Exists(x86DotnetExe))
            {
                NUnit.Framework.Assert.Inconclusive(
                    "The 32-bit .NET SDK is required to run this test on x86 but was not found at '" +
                    (x86DotnetExe ?? "<unknown>") + "'. Since .NET 8, the 32-bit SDK must be installed " +
                    "separately from the 64-bit SDK to fork an x86 `dotnet test` process.");
            }
        }
        #endregion

        /// <summary>
        /// A pipe thread. It'd be nice to reuse guava's implementation for this... </summary>
        internal static class ThreadPumper
        {
            // LUCENENET: capture is an optional buffer that accumulates the piped text (e.g. STDERR) so it
            // can be reported when the fork fails, independent of Verbose.
            public static ThreadJob Start(TextReader from, TextWriter to, StringBuilder capture)
            {
                ThreadJob t = new ThreadPumperAnonymousClass(from, to, capture);
                t.Start();
                return t;
            }

            private sealed class ThreadPumperAnonymousClass : ThreadJob
            {
                private readonly TextReader from;
                private readonly TextWriter to;
                private readonly StringBuilder capture;

                public ThreadPumperAnonymousClass(TextReader from, TextWriter to, StringBuilder capture)
                {
                    this.from = from;
                    this.to = to;
                    this.capture = capture;
                }

                public override void Run()
                {
                    try
                    {
                        char[] buffer = new char[1024];
                        int len;
                        while ((len = from.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (capture != null)
                            {
                                lock (capture)
                                {
                                    capture.Append(buffer, 0, len);
                                }
                            }
                            if (Verbose)
                            {
                                to.Write(buffer, 0, len);
                            }
                        }
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        Console.Error.WriteLine("Couldn't pipe from the forked process: " + e.ToTypeMessageString()); // LUCENENET specific - use ToTypeMessageString to mimic Java behavior
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
                BaseDirectoryWrapper dir = null;
                Exception priorE = null;
                try
                {
                    dir = NewFSDirectory(directoryInfo);
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
                }
                catch (Exception e)
                {
                    priorE = e;
                    throw;
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(priorE, dir);
                }

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

        private TcpListener SetupSocketListener()
        {
            // Pick a random port that is available on the local machine.
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return listener;
        }

        // LUCENENET: Wait for our test to spin up and send its process ID so we can kill it.
        // Rather than blocking forever in AcceptTcpClient(), poll for either an incoming connection or the
        // fork exiting. If the fork exits before connecting (e.g. it failed to build or launch), hard-fail
        // with its exit code and captured STDERR so the test reports the cause instead of hanging.
        private int WaitForProcessId(TcpListener listener, Process fork, StringBuilder stdErrCapture)
        {
            IAsyncResult acceptResult = listener.BeginAcceptTcpClient(null, null);

            // The fork has to build the test project before it can run, so allow a generous window.
            const int TimeoutMs = 120_000;
            int waited = 0;
            const int PollMs = 100;
            while (!acceptResult.AsyncWaitHandle.WaitOne(PollMs))
            {
                waited += PollMs;
                if (fork.HasExited)
                {
                    FailForkStartup(fork, stdErrCapture, "The forked process exited before connecting back.");
                }
                if (waited >= TimeoutMs)
                {
                    FailForkStartup(fork, stdErrCapture,
                        $"Timed out after {TimeoutMs / 1000} seconds waiting for the forked process to connect back.");
                }
            }

            using var client = listener.EndAcceptTcpClient(acceptResult);
            using var stream = client.GetStream();
            // Directly read the process ID as a 32-bit integer
            using var reader = new BinaryReader(stream);
            return reader.ReadInt32();
        }

        // LUCENENET: Hard-fail with the fork's exit code and captured STDERR so failures are diagnosable
        // instead of presenting as a hang.
        private static void FailForkStartup(Process fork, StringBuilder stdErrCapture, string message)
        {
            string exitCode = fork.HasExited
                ? fork.ExitCode.ToString(CultureInfo.InvariantCulture)
                : "(still running)";
            string stdErr;
            lock (stdErrCapture)
            {
                stdErr = stdErrCapture.ToString();
            }
            if (stdErr.Length == 0)
            {
                stdErr = "(no STDERR captured)";
            }

            Assert.Fail($"{message} Fork exit code: {exitCode}.{Environment.NewLine}Fork STDERR:{Environment.NewLine}{stdErr}");
        }

        private void SendProcessId(int processId, int port)
        {
            using var client = new TcpClient("127.0.0.1", port);
            using var stream = client.GetStream();
            // Directly write the process ID as a 32-bit integer
            using var writer = new BinaryWriter(stream);
            writer.Write(processId);
        }

        public virtual void CrashDotNet(int processIdToKill)
        {
            Process process = null;
            try
            {
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
            finally
            {
                process?.Dispose();
            }
        }
    }
}
