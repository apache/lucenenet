using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
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
    /// Tests for <see cref="WindowsDirectory"/>.
    /// <para/>
    /// This runs the full <see cref="BaseDirectoryTestCase"/> suite against
    /// <see cref="WindowsDirectory"/>. Because <see cref="WindowsDirectory"/> uses the
    /// Win32 file APIs via P/Invoke, these tests are only run on Microsoft Windows; on
    /// other platforms the entire fixture is skipped (see <see cref="SetUp"/>).
    /// <para/>
    /// LUCENENET specific: the original Lucene <c>WindowsDirectory</c> had no test coverage (GH-1342).
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
#if FEATURE_SUPPORTEDOSPLATFORMATTRIBUTE
    [SupportedOSPlatform("windows")]
#endif
    public class TestWindowsDirectory : BaseDirectoryTestCase
    {
        public override void SetUp()
        {
            base.SetUp();
            AssumeTrue("WindowsDirectory is only supported on Microsoft Windows.", Constants.WINDOWS);
        }

        protected override Directory GetDirectory(DirectoryInfo path)
        {
            return new WindowsDirectory(path);
        }

        /// <summary>
        /// Exercises the positioned reads that back <see cref="WindowsDirectory"/>: random seeks
        /// plus an independent clone reading from a different position, all sharing the same
        /// underlying Win32 file handle.
        /// </summary>
        [Test]
        public virtual void TestRandomAccessAndClones()
        {
            using Directory dir = GetDirectory(CreateTempDir("testWindowsRandomAccess"));

            int len = TestUtil.NextInt32(Random, 100, 100000);
            byte[] expected = new byte[len];
            Random.NextBytes(expected);

            using (IndexOutput output = dir.CreateOutput("data", NewIOContext(Random)))
            {
                output.WriteBytes(expected, expected.Length);
            }

            using IndexInput input = dir.OpenInput("data", NewIOContext(Random));
            Assert.AreEqual(len, input.Length);

            // sequential read of the whole file
            byte[] actual = new byte[len];
            input.ReadBytes(actual, 0, actual.Length);
            Assert.AreEqual(expected, actual);

            // random positioned reads
            for (int i = 0; i < 100; i++)
            {
                int pos = Random.Next(len);
                int count = Math.Min(TestUtil.NextInt32(Random, 1, 1000), len - pos);
                input.Seek(pos);
                byte[] buf = new byte[count];
                input.ReadBytes(buf, 0, count);
                for (int j = 0; j < count; j++)
                {
                    Assert.AreEqual(expected[pos + j], buf[j], "mismatch at " + (pos + j));
                }
            }

            // a clone shares the same handle but must read independently of the original
            input.Seek(0);
            using (IndexInput clone = (IndexInput)input.Clone())
            {
                int clonePos = Random.Next(len);
                clone.Seek(clonePos);
                Assert.AreEqual(expected[clonePos], clone.ReadByte());

                // the original's position is unaffected by the clone's reads
                Assert.AreEqual(0, input.Position);
                Assert.AreEqual(expected[0], input.ReadByte());
            }
        }

        /// <summary>
        /// Reading past the end of the file must throw an EOF exception, not silently return
        /// garbage or hang.
        /// </summary>
        [Test]
        public virtual void TestReadPastEOF()
        {
            using Directory dir = GetDirectory(CreateTempDir("testWindowsEOF"));

            const int len = 100;
            using (IndexOutput output = dir.CreateOutput("eof", NewIOContext(Random)))
            {
                output.WriteBytes(new byte[len], len);
            }

            using IndexInput input = dir.OpenInput("eof", NewIOContext(Random));
            Assert.AreEqual(len, input.Length);

            // read right up to EOF: fine
            input.Seek(len);
            try
            {
                input.ReadByte();
                fail("did not hit expected EOF exception");
            }
            catch (Exception e) when (e.IsEOFException())
            {
                // expected
            }

            // a read that straddles EOF must also throw
            input.Seek(len - 4);
            try
            {
                byte[] buf = new byte[8];
                input.ReadBytes(buf, 0, buf.Length);
                fail("did not hit expected EOF exception");
            }
            catch (Exception e) when (e.IsEOFException())
            {
                // expected
            }
        }

        /// <summary>
        /// Disposing an input (and a clone) more than once must be a no-op, never a double
        /// <c>CloseHandle</c>. Clones must not close the shared handle.
        /// </summary>
        [Test]
        public virtual void TestCloneDisposeDoesNotCloseSharedHandle()
        {
            using Directory dir = GetDirectory(CreateTempDir("testWindowsDoubleDispose"));
            using (IndexOutput output = dir.CreateOutput("data", NewIOContext(Random)))
            {
                output.WriteInt64(42);
            }

            IndexInput input = dir.OpenInput("data", NewIOContext(Random));
            IndexInput clone = (IndexInput)input.Clone();

            // disposing the clone must not close the shared handle: original still reads
            Assert.DoesNotThrow(() => clone.Dispose());
            Assert.DoesNotThrow(() => clone.Dispose());
            input.Seek(0);
            Assert.AreEqual(42L, input.ReadInt64());

            // original double-dispose is a no-op
            Assert.DoesNotThrow(() => input.Dispose());
            Assert.DoesNotThrow(() => input.Dispose());
        }

        /// <summary>
        /// Many threads reading random positions through independent clones of the same input
        /// (and thus the same shared Win32 handle) must each get correct data. This proves the
        /// positioned reads are safe under concurrency, not just single-threaded.
        /// </summary>
        [Test]
        public virtual void TestConcurrentCloneReads()
        {
            using Directory dir = GetDirectory(CreateTempDir("testWindowsConcurrent"));

            int len = TestUtil.NextInt32(Random, 10000, 50000);
            byte[] expected = new byte[len];
            Random.NextBytes(expected);
            using (IndexOutput output = dir.CreateOutput("data", NewIOContext(Random)))
            {
                output.WriteBytes(expected, expected.Length);
            }

            using IndexInput input = dir.OpenInput("data", NewIOContext(Random));

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
                            for (int i = 0; i < 2000; i++)
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

    /// <summary>
    /// Verifies the cross-platform guard on <see cref="WindowsDirectory"/>. Unlike
    /// <see cref="TestWindowsDirectory"/>, this fixture runs on every platform.
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestWindowsDirectoryPlatformGuard : LuceneTestCase
    {
        [Test]
        public virtual void TestThrowsOnNonWindows()
        {
            DirectoryInfo path = CreateTempDir("testWindowsGuard");
#pragma warning disable CA1416 // Validate platform compatibility - intentionally exercising the guard on all platforms
            if (Constants.WINDOWS)
            {
                using Directory dir = new WindowsDirectory(path);
                Assert.IsNotNull(dir);
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => new WindowsDirectory(path));
            }
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }
}
