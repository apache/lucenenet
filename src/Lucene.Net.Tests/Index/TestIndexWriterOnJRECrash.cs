/*LUCENENET NOTE: Clearly this test is not applicable to.NET, but just

adding the file to the project for completedness.*/

using System;
using System.Collections.Generic;
using System.Threading;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Index
{


    ///  Licensed to the Apache Software Foundation (ASF) under one or more
    ///  contributor license agreements.  See the NOTICE file distributed with
    ///  this work for additional information regarding copyright ownership.
    ///  The ASF licenses this file to You under the Apache License, Version 2.0
    ///  (the "License"); you may not use this file except in compliance with
    ///  the License.  You may obtain a copy of the License at
    /// 
    ///      http://www.apache.org/licenses/LICENSE-2.0
    /// 
    ///  Unless required by applicable law or agreed to in writing, software
    ///  distributed under the License is distributed on an "AS IS" BASIS,
    ///  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    ///  See the License for the specific language governing permissions and
    ///  limitations under the License.
    /// 



    using Codec = Lucene.Net.Codecs.Codec;
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using Constants = Lucene.Net.Util.Constants;
    using TestUtil = Lucene.Net.Util.TestUtil;

    using NUnit.Framework;
    using Lucene.Net.Support;
    using System.IO;
    using J2N.Threading;
    using Lucene.Net.Support.IO;
    using System.Diagnostics;
    using System.Reflection;
    using Lucene.Net.Util;

    /// <summary>
    /// Runs TestNRTThreads in a separate process, crashes the JRE in the middle
    /// of execution, then runs checkindex to make sure its not corrupt.
    /// </summary>
    [TestFixture]
    [Category("CrashTest")]
    public class TestIndexWriterOnJRECrash : TestNRTThreads
    {
        private DirectoryInfo TempDir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            var tempDir = Environment.GetEnvironmentVariable("tempDir");
            if (tempDir is null)
            {
                TempDir = CreateTempDir("netcrash");
                TempDir.Delete();
                TempDir.Create();
            }
            else
            {
                TempDir = new DirectoryInfo(tempDir);
            }
        }


        [Test, Property("Name", "TestNRTTThreadsMem")]
        public override void TestNRTThreads_Mem()
        {
            // if we are not the fork
            if (Environment.GetEnvironmentVariable("tempDir") is null)
            {
                // try up to 10 times to create an index
                for (int i = 0; i < 10; i++)
                {
                    ForkTest();
                    // if we succeeded in finding an index, we are done.
                    if (CheckIndexes(TempDir))
                    {
                        return;
                    }
                }
            }
            else
            {
                // TODO: the non-fork code could simply enable impersonation?
                AssumeFalse("does not support PreFlex, see LUCENE-3992", Codec.Default.Name.Equals("Lucene3x", StringComparison.Ordinal));
                // we are the fork, setup a crashing thread
                int crashTime = TestUtil.NextInt32(Random, 3000, 4000);
                ThreadJob t = new ThreadAnonymousClass(this, crashTime);
                t.Priority = ThreadPriority.Highest;
                t.Start();
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

            private int CrashTime;

            public ThreadAnonymousClass(TestIndexWriterOnJRECrash outerInstance, int crashTime)
            {
                this.outerInstance = outerInstance;
                this.CrashTime = crashTime;
            }

            public override void Run()
            {
                try
                {
                    Thread.Sleep(CrashTime);
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                }
                outerInstance.CrashJRE();
            }
        }

        public void ForkTest()
        {
            //get the full location of the assembly with DaoTests in it
            string fullPath = System.Reflection.Assembly.GetAssembly(typeof(TestIndexWriterOnJRECrash)).Location;

            //get the folder that's in
            string theDirectory = Path.GetDirectoryName(fullPath);
            // Set up the process to run the console app
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = String.Join(" ", new[] { "test", "--framework net7.0", "--filter", "FullyQualifiedName~TestIndexWriterOnJRECrash.cs", $"C:\\\\Users\\\\admin\\\\Projects\\\\Dotnet Projects local Repo\\\\lucenenet\\\\src\\\\Lucene.Net.Tests._I-J\\\\Lucene.Net.Tests._I-J.csproj\"" }),
                WorkingDirectory = "C:\\Users\\admin\\Projects\\Dotnet Projects local Repo\\lucenenet\\src\\Lucene.Net.Tests._I-J",
                EnvironmentVariables =  {
        { "lucene:tests:seed", RandomizedContext.CurrentContext.RandomSeedAsHex },
        { "lucene:tests:culture", Thread.CurrentThread.CurrentCulture.Name },
        { "tests:crashmode", "true" } },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };


            Process p = Process.Start(startInfo);

            // We pump everything to stderr.
            TextWriter childOut = Console.Error;
            ThreadJob stdoutPumper = ThreadPumper.Start(p.StandardOutput, childOut);
            ThreadJob stderrPumper = ThreadPumper.Start(p.StandardError, childOut);
            if (Verbose) childOut.WriteLine(">>> Begin subprocess output");
            p.WaitForExit(10000);
            stdoutPumper.Join();
            stderrPumper.Join();
            if (Verbose) childOut.WriteLine("<<< End subprocess output");
        }

        /// <summary>
        /// A pipe thread. It'd be nice to reuse guava's implementation for this... </summary>
        internal class ThreadPumper
        {
            public static ThreadJob Start(StreamReader from, TextWriter to)
            {
                ThreadJob t = new ThreadAnonymousClass2(from, to);
                t.Start();
                return t;
            }

            private sealed class ThreadAnonymousClass2 : ThreadJob
            {
                private StreamReader From;
                private TextWriter To;

                public ThreadAnonymousClass2(StreamReader from, TextWriter to)
                {
                    this.From = from;
                    this.To = to;
                }

                public override void Run()
                {
                    try
                    {
                        char[] buffer = new char[1024];
                        int len;
                        while ((len = From.Read(buffer)) != -1)
                        {
                            if (Verbose)
                            {
                                To.Write(buffer, 0, len);
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
        public virtual bool CheckIndexes(DirectoryInfo file)
        {
            if (file.Exists)
            {
                BaseDirectoryWrapper dir = NewFSDirectory(file);
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
                    dir.Dispose();
                    return true;
                }
                dir.Dispose();
                foreach (DirectoryInfo f in file.EnumerateDirectories())
                {
                    if (CheckIndexes(f))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// currently, this only works/tested on Sun and IBM.
        /// </summary>
        public virtual void CrashJRE()
        {
            string vendor = Constants.RUNTIME_VENDOR;
            bool supportsUnsafeNpeDereference = vendor.StartsWith("Sun_OS", StringComparison.Ordinal) || vendor.StartsWith("FreeBSD", StringComparison.Ordinal) || vendor.StartsWith("WINDOWS", StringComparison.Ordinal);

            try
            {
                if (supportsUnsafeNpeDereference)
                {
                    try
                    {
                        Type clazz = Type.GetType("sun.misc.Unsafe");
                        var field = clazz.GetField("theUnsafe");
                        field.SetValue(null, true);
                        object o = field.GetValue(null);
                        MethodInfo m = clazz.GetMethod("putAddress", new Type[] { typeof(long), typeof(long) });
                        m.Invoke(o, new object[] { 0L, 0L });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Couldn't kill the NetFramwork via Unsafe.");
                        Console.WriteLine(e.StackTrace);
                    }
                }

                // Fallback attempt to Runtime.halt();
                RuntimeTypeHandle.FromIntPtr(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't kill the NetFramwork.");
                Console.WriteLine(e.StackTrace);
            }

            // We couldn't get the JVM to crash for some reason.
            Assert.Fail();
        }
    }

}