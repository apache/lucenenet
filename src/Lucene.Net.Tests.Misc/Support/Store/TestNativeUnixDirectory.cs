using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.IO;
#if FEATURE_SUPPORTEDOSPLATFORMATTRIBUTE
using System.Runtime.Versioning;
#endif
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Store
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
    /// Cross-platform tests for the platform guard on <see cref="NativeUnixDirectory"/> and
    /// <see cref="NativePosixUtil"/>. This fixture runs on every platform; on Microsoft Windows it
    /// verifies that the types refuse to operate (they require Linux/macOS direct I/O).
    /// <para/>
    /// LUCENENET specific: the original Lucene types had no test coverage (GH-1342).
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestNativeUnixDirectoryPlatformGuard : LuceneTestCase
    {
        [Test]
        public virtual void TestDirectoryThrowsOnWindows()
        {
            DirectoryInfo path = CreateTempDir("nativeUnixGuard");
#pragma warning disable CA1416 // Validate platform compatibility - intentionally exercising the guard on all platforms
            using Directory @delegate = new RAMDirectory();
            if (Constants.WINDOWS)
            {
                Assert.Throws<PlatformNotSupportedException>(() => new NativeUnixDirectory(path, @delegate));
            }
            else
            {
                // On Unix the constructor must succeed (no direct files are opened until a merge-context
                // CreateOutput/OpenInput); functional behavior is covered by TestRoundTrip below.
                using Directory dir = new NativeUnixDirectory(path, @delegate);
                Assert.IsNotNull(dir);
            }
#pragma warning restore CA1416 // Validate platform compatibility
        }

        [Test]
        public virtual void TestOpenDirectThrowsOnWindows()
        {
            if (Constants.WINDOWS)
            {
#pragma warning disable CA1416 // Validate platform compatibility - intentionally exercising the guard
                Assert.Throws<PlatformNotSupportedException>(() => NativePosixUtil.OpenDirect("anything", read: true));
#pragma warning restore CA1416 // Validate platform compatibility
            }
            else
            {
                AssumeTrue("OpenDirect's open path is exercised by the Unix functional tests.", false);
            }
        }
    }

    /// <summary>
    /// Functional tests for <see cref="NativeUnixDirectory"/>'s direct-I/O read/write path. These
    /// only run on Unix-like platforms and additionally require a filesystem that supports direct
    /// I/O (<c>O_DIRECT</c>); they are skipped on Microsoft Windows.
    /// <para/>
    /// Unlike <c>TestWindowsDirectory</c>, this does not subclass <see cref="BaseDirectoryTestCase"/>:
    /// <see cref="NativeUnixDirectory"/> only uses its direct-I/O input/output in a MERGE
    /// <see cref="IOContext"/> for files larger than <c>minBytesDirect</c> (otherwise it delegates),
    /// and the experimental direct-I/O input does not throw on read-past-EOF the way the generic
    /// suite expects, so the randomized generic suite would be non-deterministic. These targeted
    /// tests instead exercise the direct path in its designed usage (sequential write, full read,
    /// clone, and concurrent clones).
    /// <para/>
    /// LUCENENET specific (GH-1342): the original Lucene type had no test coverage.
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
#if FEATURE_SUPPORTEDOSPLATFORMATTRIBUTE
    [UnsupportedOSPlatform("windows")]
#endif
    public class TestNativeUnixDirectory : LuceneTestCase
    {
        // A MERGE context with estimatedMergeBytes >= minBytesDirect(0) forces the direct path.
        private static readonly IOContext MERGE_CONTEXT = new IOContext(new MergeInfo(1, 1024 * 1024, false, 1));

        public override void SetUp()
        {
            base.SetUp();
            AssumeTrue("NativeUnixDirectory requires Linux or macOS direct I/O.", !Constants.WINDOWS);
        }

        /// <summary>
        /// Creates a <see cref="NativeUnixDirectory"/> (plus its delegate) configured so that the
        /// direct-I/O path is always taken for MERGE-context I/O. The caller must dispose both the
        /// returned directory and <paramref name="delegate"/>.
        /// </summary>
        private NativeUnixDirectory NewDirectDirectory(DirectoryInfo path, out Directory @delegate)
        {
            @delegate = new SimpleFSDirectory(path);
            // mergeBufferSize must be a multiple of 512 (and, per the upstream check, have bit 9 clear).
            return new NativeUnixDirectory(path, mergeBufferSize: 1024, minBytesDirect: 0, @delegate);
        }

        /// <summary>
        /// Writes a file through the direct-I/O merge path and reads it back (in bulk), verifying the
        /// bytes round-trip across many buffer dumps/refills, plus a clone read.
        /// </summary>
        [Test]
        public virtual void TestRoundTrip()
        {
            DirectoryInfo path = CreateTempDir("nativeUnixRoundTrip");
            using var dir = NewDirectDirectory(path, out Directory @delegate);
            using (@delegate)
            {
                int len = TestUtil.NextInt32(Random, 2000, 20000);
                byte[] expected = new byte[len];
                Random.NextBytes(expected);

                using (IndexOutput output = dir.CreateOutput("test", MERGE_CONTEXT))
                {
                    output.WriteBytes(expected, expected.Length);
                }

                using IndexInput input = dir.OpenInput("test", MERGE_CONTEXT);
                Assert.AreEqual(len, input.Length);
                byte[] actual = new byte[len];
                input.ReadBytes(actual, 0, actual.Length);
                Assert.AreEqual(expected, actual);

                // random positioned reads
                for (int i = 0; i < 50; i++)
                {
                    int pos = Random.Next(len);
                    int count = Math.Min(TestUtil.NextInt32(Random, 1, 500), len - pos);
                    input.Seek(pos);
                    byte[] buf = new byte[count];
                    input.ReadBytes(buf, 0, count);
                    for (int j = 0; j < count; j++)
                    {
                        Assert.AreEqual(expected[pos + j], buf[j], "mismatch at " + (pos + j));
                    }
                }

                // a clone reads independently from the same shared descriptor
                input.Seek(0);
                using IndexInput clone = (IndexInput)input.Clone();
                int clonePos = Random.Next(len);
                clone.Seek(clonePos);
                Assert.AreEqual(expected[clonePos], clone.ReadByte());
            }
        }

        /// <summary>
        /// Disposing an input (and a clone) more than once must be a no-op; clones must not close the
        /// shared descriptor. Mirrors the equivalent WindowsDirectory test.
        /// </summary>
        [Test]
        public virtual void TestCloneDisposeDoesNotCloseSharedDescriptor()
        {
            DirectoryInfo path = CreateTempDir("nativeUnixDoubleDispose");
            using var dir = NewDirectDirectory(path, out Directory @delegate);
            using (@delegate)
            {
                byte[] data = new byte[2048];
                Random.NextBytes(data);
                IndexOutput output = dir.CreateOutput("data", MERGE_CONTEXT);
                output.WriteBytes(data, data.Length);
                Assert.DoesNotThrow(() => output.Dispose());
                Assert.DoesNotThrow(() => output.Dispose()); // double dispose is a no-op

                IndexInput input = dir.OpenInput("data", MERGE_CONTEXT);
                IndexInput clone = (IndexInput)input.Clone();

                // disposing the clone must not close the shared descriptor: the original still reads
                Assert.DoesNotThrow(() => clone.Dispose());
                Assert.DoesNotThrow(() => clone.Dispose());
                input.Seek(0);
                byte[] check = new byte[data.Length];
                input.ReadBytes(check, 0, check.Length);
                Assert.AreEqual(data, check);

                Assert.DoesNotThrow(() => input.Dispose());
                Assert.DoesNotThrow(() => input.Dispose()); // double dispose is a no-op
            }
        }

        /// <summary>
        /// Many threads reading random positions through independent clones of the same input (and
        /// thus the same shared descriptor) must each get correct data. Positioned <c>pread</c> is
        /// atomic and each clone has its own buffer, so this must be safe under concurrency. Mirrors
        /// the equivalent WindowsDirectory test.
        /// </summary>
        [Test]
        public virtual void TestConcurrentCloneReads()
        {
            DirectoryInfo path = CreateTempDir("nativeUnixConcurrent");
            using var dir = NewDirectDirectory(path, out Directory @delegate);
            using (@delegate)
            {
                int len = TestUtil.NextInt32(Random, 10000, 50000);
                byte[] expected = new byte[len];
                Random.NextBytes(expected);
                using (IndexOutput output = dir.CreateOutput("data", MERGE_CONTEXT))
                {
                    output.WriteBytes(expected, expected.Length);
                }

                using IndexInput input = dir.OpenInput("data", MERGE_CONTEXT);

                int numThreads = Math.Max(2, Environment.ProcessorCount);
                var threads = new Thread[numThreads];
                Exception failure = null;
                using (var start = new ManualResetEventSlim(false))
                {
                    for (int t = 0; t < numThreads; t++)
                    {
                        int seed = t;
                        threads[t] = new Thread(() =>
                        {
                            try
                            {
                                var rnd = new J2N.Randomizer(seed + 1);
                                using IndexInput clone = (IndexInput)input.Clone();
                                start.Wait();
                                for (int i = 0; i < 1000; i++)
                                {
                                    int pos = rnd.Next(len);
                                    int count = Math.Min(rnd.Next(1, 256), len - pos);
                                    clone.Seek(pos);
                                    byte[] buf = new byte[count];
                                    clone.ReadBytes(buf, 0, count);
                                    for (int j = 0; j < count; j++)
                                    {
                                        if (buf[j] != expected[pos + j])
                                        {
                                            throw new Exception($"mismatch at {pos + j}: got {buf[j]}, expected {expected[pos + j]}");
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Interlocked.CompareExchange(ref failure, e, null);
                            }
                        });
                        threads[t].Start();
                    }

                    start.Set(); // release all threads at once to maximize contention
                    foreach (var thread in threads)
                    {
                        thread.Join();
                    }
                }

                Assert.IsNull(failure, "concurrent clone reads failed: " + failure);
            }
        }
    }

    /// <summary>
    /// Runs the full <see cref="BaseDirectoryTestCase"/> suite against <see cref="NativeUnixDirectory"/>
    /// with <c>minBytesDirect == 0</c>, so the direct-I/O path is taken for all MERGE-context I/O.
    /// Unix-gated and requires an <c>O_DIRECT</c>-capable filesystem.
    /// <para/>
    /// LUCENENET specific (GH-1342).
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
#if FEATURE_SUPPORTEDOSPLATFORMATTRIBUTE
    [UnsupportedOSPlatform("windows")]
#endif
    public class TestNativeUnixDirectoryBase : BaseDirectoryTestCase
    {
        private readonly List<Directory> delegates = new List<Directory>();

        public override void SetUp()
        {
            base.SetUp();
            AssumeTrue("NativeUnixDirectory requires Linux or macOS direct I/O.", !Constants.WINDOWS);
        }

        public override void TearDown()
        {
            foreach (var d in delegates)
            {
                try { d.Dispose(); } catch { /* ignore: best-effort cleanup of the delegate */ }
            }
            delegates.Clear();
            base.TearDown();
        }

        protected override Directory GetDirectory(DirectoryInfo path)
        {
            var del = new SimpleFSDirectory(path);
            delegates.Add(del);
            // minBytesDirect = 0 so the direct-I/O path is used for all MERGE-context I/O.
            return new NativeUnixDirectory(path, mergeBufferSize: 1024, minBytesDirect: 0, del);
        }

        /// <summary>
        /// Not applicable to <see cref="NativeUnixDirectory"/>. It is a composite/delegating directory:
        /// files written in a non-MERGE context go through the delegate and are tracked in the
        /// delegate's <c>staleFiles</c>, not this directory's. Since <see cref="FSDirectory.Sync"/> only
        /// fsyncs files in its own <c>staleFiles</c> (<c>toSync.IntersectWith(m_staleFiles)</c>), it does
        /// not throw for a file written through the delegate, so this backdoor-the-filesystem test does
        /// not hold here. The base test itself notes (TODO) that it does not handle composite/two-FSDir
        /// directories.
        /// </summary>
        public override void TestFsyncDoesntCreateNewFiles()
        {
            AssumeTrue("TestFsyncDoesntCreateNewFiles does not apply to the composite/delegating NativeUnixDirectory; see override remarks.", false);
        }
    }
}
