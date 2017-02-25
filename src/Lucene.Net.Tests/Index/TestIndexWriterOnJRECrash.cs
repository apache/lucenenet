// LUCENENET NOTE: Clearly this test is not applicable to .NET, but just 
// adding the file to the project for completedness.

//using System;
//using System.Collections.Generic;
//using System.Threading;
//using Lucene.Net.Randomized;
//using Lucene.Net.Randomized.Generators;

//namespace Lucene.Net.Index
//{

//    /*
//    ///  Licensed to the Apache Software Foundation (ASF) under one or more
//    ///  contributor license agreements.  See the NOTICE file distributed with
//    ///  this work for additional information regarding copyright ownership.
//    ///  The ASF licenses this file to You under the Apache License, Version 2.0
//    ///  (the "License"); you may not use this file except in compliance with
//    ///  the License.  You may obtain a copy of the License at
//    /// 
//    ///      http://www.apache.org/licenses/LICENSE-2.0
//    /// 
//    ///  Unless required by applicable law or agreed to in writing, software
//    ///  distributed under the License is distributed on an "AS IS" BASIS,
//    ///  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    ///  See the License for the specific language governing permissions and
//    ///  limitations under the License.
//    /// 
//    */


//    using Codec = Lucene.Net.Codecs.Codec;
//    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
//    using Constants = Lucene.Net.Util.Constants;
//    using TestUtil = Lucene.Net.Util.TestUtil;

//    using NUnit.Framework;
//    using Lucene.Net.Support;
//    using System.IO;
//    /// <summary>
//    /// Runs TestNRTThreads in a separate process, crashes the JRE in the middle
//    /// of execution, then runs checkindex to make sure its not corrupt.
//    /// </summary>
//    [TestFixture]
//    public class TestIndexWriterOnJRECrash : TestNRTThreads
//    {
//        private DirectoryInfo TempDir;

//        [SetUp]
//        public override void SetUp()
//        {
//            base.SetUp();
//            TempDir = CreateTempDir("jrecrash");
//            TempDir.Delete();
//            TempDir.mkdir();
//        }

//        [Test]
//        public override void TestNRTThreads_Mem()
//        {
//            // if we are not the fork
//            if (System.getProperty("tests.crashmode") == null)
//            {
//                // try up to 10 times to create an index
//                for (int i = 0; i < 10; i++)
//                {
//                    ForkTest();
//                    // if we succeeded in finding an index, we are done.
//                    if (CheckIndexes(TempDir))
//                    {
//                        return;
//                    }
//                }
//            }
//            else
//            {
//                // TODO: the non-fork code could simply enable impersonation?
//                AssumeFalse("does not support PreFlex, see LUCENE-3992", Codec.Default.Name.Equals("Lucene3x"));
//                // we are the fork, setup a crashing thread
//                int crashTime = TestUtil.NextInt(Random(), 3000, 4000);
//                ThreadClass t = new ThreadAnonymousInnerClassHelper(this, crashTime);
//                t.Priority = ThreadPriority.Highest;
//                t.Start();
//                // run the test until we crash.
//                for (int i = 0; i < 1000; i++)
//                {
//                    base.TestNRTThreads_Mem();
//                }
//            }
//        }

//        private class ThreadAnonymousInnerClassHelper : ThreadClass
//        {
//            private readonly TestIndexWriterOnJRECrash OuterInstance;

//            private int CrashTime;

//            public ThreadAnonymousInnerClassHelper(TestIndexWriterOnJRECrash outerInstance, int crashTime)
//            {
//                this.OuterInstance = outerInstance;
//                this.CrashTime = crashTime;
//            }

//            public override void Run()
//            {
//                try
//                {
//                    Thread.Sleep(CrashTime);
//                }
//                catch (ThreadInterruptedException e)
//                {
//                }
//                OuterInstance.CrashJRE();
//            }
//        }

//        /// <summary>
//        /// fork ourselves in a new jvm. sets -Dtests.crashmode=true </summary>
//        public virtual void ForkTest()
//        {
//            IList<string> cmd = new List<string>();
//            cmd.Add(System.getProperty("java.home") + System.getProperty("file.separator") + "bin" + System.getProperty("file.separator") + "java");
//            cmd.Add("-Xmx512m");
//            cmd.Add("-Dtests.crashmode=true");
//            // passing NIGHTLY to this test makes it run for much longer, easier to catch it in the act...
//            cmd.Add("-Dtests.nightly=true");
//            cmd.Add("-DtempDir=" + TempDir.Path);
//            cmd.Add("-Dtests.seed=" + SeedUtils.formatSeed(Random().NextLong()));
//            cmd.Add("-ea");
//            cmd.Add("-cp");
//            cmd.Add(System.getProperty("java.class.path"));
//            cmd.Add("org.junit.runner.JUnitCore");
//            cmd.Add(this.GetType().Name);
//            ProcessBuilder pb = new ProcessBuilder(cmd);
//            pb.directory(TempDir);
//            pb.redirectErrorStream(true);
//            Process p = pb.Start();

//            // We pump everything to stderr.
//            PrintStream childOut = System.err;
//            Thread stdoutPumper = ThreadPumper.Start(p.InputStream, childOut);
//            Thread stderrPumper = ThreadPumper.Start(p.ErrorStream, childOut);
//            if (VERBOSE)
//            {
//                childOut.println(">>> Begin subprocess output");
//            }
//            p.waitFor();
//            stdoutPumper.Join();
//            stderrPumper.Join();
//            if (VERBOSE)
//            {
//                childOut.println("<<< End subprocess output");
//            }
//        }

//        /// <summary>
//        /// A pipe thread. It'd be nice to reuse guava's implementation for this... </summary>
//        internal class ThreadPumper
//        {
//            public static Thread Start(InputStream from, OutputStream to)
//            {
//                ThreadClass t = new ThreadAnonymousInnerClassHelper2(from, to);
//                t.Start();
//                return t;
//            }

//            private class ThreadAnonymousInnerClassHelper2 : ThreadClass
//            {
//                private InputStream From;
//                private OutputStream To;

//                public ThreadAnonymousInnerClassHelper2(InputStream from, OutputStream to)
//                {
//                    this.From = from;
//                    this.To = to;
//                }

//                public override void Run()
//                {
//                    try
//                    {
//                        sbyte[] buffer = new sbyte[1024];
//                        int len;
//                        while ((len = From.Read(buffer)) != -1)
//                        {
//                            if (VERBOSE)
//                            {
//                                To.Write(buffer, 0, len);
//                            }
//                        }
//                    }
//                    catch (IOException e)
//                    {
//                        Console.Error.WriteLine("Couldn't pipe from the forked process: " + e.ToString());
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// Recursively looks for indexes underneath <code>file</code>,
//        /// and runs checkindex on them. returns true if it found any indexes.
//        /// </summary>
//        public virtual bool CheckIndexes(DirectoryInfo file)
//        {
//            if (file.IsDirectory)
//            {
//                BaseDirectoryWrapper dir = NewFSDirectory(file);
//                dir.CheckIndexOnClose = false; // don't double-checkindex
//                if (DirectoryReader.IndexExists(dir))
//                {
//                    if (VERBOSE)
//                    {
//                        Console.Error.WriteLine("Checking index: " + file);
//                    }
//                    // LUCENE-4738: if we crashed while writing first
//                    // commit it's possible index will be corrupt (by
//                    // design we don't try to be smart about this case
//                    // since that too risky):
//                    if (SegmentInfos.GetLastCommitGeneration(dir) > 1)
//                    {
//                        TestUtil.CheckIndex(dir);
//                    }
//                    dir.Dispose();
//                    return true;
//                }
//                dir.Dispose();
//                foreach (FileInfo f in file.ListAll())
//                {
//                    if (CheckIndexes(f))
//                    {
//                        return true;
//                    }
//                }
//            }
//            return false;
//        }

//        /// <summary>
//        /// currently, this only works/tested on Sun and IBM.
//        /// </summary>
//        public virtual void CrashJRE()
//        {
//            string vendor = Constants.JAVA_VENDOR;
//            bool supportsUnsafeNpeDereference = vendor.StartsWith("Oracle") || vendor.StartsWith("Sun") || vendor.StartsWith("Apple");

//            try
//            {
//                if (supportsUnsafeNpeDereference)
//                {
//                    try
//                    {
//                        Type clazz = Type.GetType("sun.misc.Unsafe");
//                        Field field = clazz.GetDeclaredField("theUnsafe");
//                        field.Accessible = true;
//                        object o = field.Get(null);
//                        Method m = clazz.GetMethod("putAddress", typeof(long), typeof(long));
//                        m.invoke(o, 0L, 0L);
//                    }
//                    catch (Exception e)
//                    {
//                        Console.WriteLine("Couldn't kill the JVM via Unsafe.");
//                        Console.WriteLine(e.StackTrace);
//                    }
//                }

//                // Fallback attempt to Runtime.halt();
//                Runtime.Runtime.halt(-1);
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine("Couldn't kill the JVM.");
//                Console.WriteLine(e.StackTrace);
//            }

//            // We couldn't get the JVM to crash for some reason.
//            Assert.Fail();
//        }
//    }

//}