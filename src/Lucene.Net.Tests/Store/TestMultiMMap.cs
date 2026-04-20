using J2N;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Document = Documents.Document;
    using Field = Field;
    using IndexInputSlicer = Lucene.Net.Store.Directory.IndexInputSlicer;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Tests MMapDirectory's MultiMMapIndexInput
    /// <p>
    /// Because Java's ByteBuffer uses an int to address the
    /// values, it's necessary to access a file >
    /// Integer.MAX_VALUE in size using multiple byte buffers.
    /// </summary>
    [TestFixture]
    public class TestMultiMMap : LuceneTestCase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            // LUCENENET NOTE:
            // Java seems to have issues releasing memory mapped resources when calling close()
            // http://stackoverflow.com/a/2973059/181087

            // However, according to MSDN, the Dispose() method of the MemoryMappedFile class will "release all resources".
            // https://msdn.microsoft.com/en-us/library/system.io.memorymappedfiles.memorymappedfile(v=vs.110).aspx
            // Therefore, I am assuming removing the below line is the correct choice for .NET.

            //AssumeTrue("test requires a jre that supports unmapping", MMapDirectory.UNMAP_SUPPORTED);
        }

        [Test]
        public virtual void TestCloneSafety()
        {
            MMapDirectory mmapDir = new MMapDirectory(CreateTempDir("testCloneSafety"));
            IndexOutput io = mmapDir.CreateOutput("bytes", NewIOContext(Random));
            io.WriteVInt32(5);
            io.Dispose();
            IndexInput one = mmapDir.OpenInput("bytes", IOContext.DEFAULT);
            IndexInput two = (IndexInput)one.Clone();
            IndexInput three = (IndexInput)two.Clone(); // clone of clone
            one.Dispose();
            try
            {
                one.ReadVInt32();
                Assert.Fail("Must throw ObjectDisposedException");
            }
            catch (Exception ignore) when (ignore.IsAlreadyClosedException())
            {
                // pass
            }
            try
            {
                two.ReadVInt32();
                Assert.Fail("Must throw ObjectDisposedException");
            }
            catch (Exception ignore) when (ignore.IsAlreadyClosedException())
            {
                // pass
            }
            try
            {
                three.ReadVInt32();
                Assert.Fail("Must throw ObjectDisposedException");
            }
            catch (Exception ignore) when (ignore.IsAlreadyClosedException())
            {
                // pass
            }
            two.Dispose();
            three.Dispose();
            // test double close of master:
            one.Dispose();
            mmapDir.Dispose();
        }

        [Test]
        public virtual void TestCloneClose()
        {
            MMapDirectory mmapDir = new MMapDirectory(CreateTempDir("testCloneClose"));
            IndexOutput io = mmapDir.CreateOutput("bytes", NewIOContext(Random));
            io.WriteVInt32(5);
            io.Dispose();
            IndexInput one = mmapDir.OpenInput("bytes", IOContext.DEFAULT);
            IndexInput two = (IndexInput)one.Clone();
            IndexInput three = (IndexInput)two.Clone(); // clone of clone
            two.Dispose();
            Assert.AreEqual(5, one.ReadVInt32());
            try
            {
                two.ReadVInt32();
                Assert.Fail("Must throw ObjectDisposedException");
            }
            catch (Exception ignore) when (ignore.IsAlreadyClosedException())
            {
                // pass
            }
            Assert.AreEqual(5, three.ReadVInt32());
            one.Dispose();
            three.Dispose();
            mmapDir.Dispose();
        }

        [Test]
        public virtual void TestCloneSliceSafety()
        {
            MMapDirectory mmapDir = new MMapDirectory(CreateTempDir("testCloneSliceSafety"));
            IndexOutput io = mmapDir.CreateOutput("bytes", NewIOContext(Random));
            io.WriteInt32(1);
            io.WriteInt32(2);
            io.Dispose();
            IndexInputSlicer slicer = mmapDir.CreateSlicer("bytes", NewIOContext(Random));
            IndexInput one = slicer.OpenSlice("first int", 0, 4);
            IndexInput two = slicer.OpenSlice("second int", 4, 4);
            IndexInput three = (IndexInput)one.Clone(); // clone of clone
            IndexInput four = (IndexInput)two.Clone(); // clone of clone
            slicer.Dispose();
            try
            {
                one.ReadInt32();
                Assert.Fail("Must throw ObjectDisposedException");
            }
            catch (Exception ignore) when (ignore.IsAlreadyClosedException())
            {
                // pass
            }
            try
            {
                two.ReadInt32();
                Assert.Fail("Must throw ObjectDisposedException");
            }
            catch (Exception ignore) when (ignore.IsAlreadyClosedException())
            {
                // pass
            }
            try
            {
                three.ReadInt32();
                Assert.Fail("Must throw ObjectDisposedException");
            }
            catch (Exception ignore) when (ignore.IsAlreadyClosedException())
            {
                // pass
            }
            try
            {
                four.ReadInt32();
                Assert.Fail("Must throw ObjectDisposedException");
            }
            catch (Exception ignore) when (ignore.IsAlreadyClosedException())
            {
                // pass
            }
            one.Dispose();
            two.Dispose();
            three.Dispose();
            four.Dispose();
            // test double-close of slicer:
            slicer.Dispose();
            mmapDir.Dispose();
        }

        [Test]
        public virtual void TestCloneSliceClose()
        {
            MMapDirectory mmapDir = new MMapDirectory(CreateTempDir("testCloneSliceClose"));
            IndexOutput io = mmapDir.CreateOutput("bytes", NewIOContext(Random));
            io.WriteInt32(1);
            io.WriteInt32(2);
            io.Dispose();
            IndexInputSlicer slicer = mmapDir.CreateSlicer("bytes", NewIOContext(Random));
            IndexInput one = slicer.OpenSlice("first int", 0, 4);
            IndexInput two = slicer.OpenSlice("second int", 4, 4);
            one.Dispose();
            try
            {
                one.ReadInt32();
                Assert.Fail("Must throw ObjectDisposedException");
            }
            catch (Exception ignore) when (ignore.IsAlreadyClosedException())
            {
                // pass
            }
            Assert.AreEqual(2, two.ReadInt32());
            // reopen a new slice "one":
            one = slicer.OpenSlice("first int", 0, 4);
            Assert.AreEqual(1, one.ReadInt32());
            one.Dispose();
            two.Dispose();
            slicer.Dispose();
            mmapDir.Dispose();
        }

        [Test]
        public virtual void TestSeekZero()
        {
            for (int i = 0; i < 31; i++)
            {
                MMapDirectory mmapDir = new MMapDirectory(CreateTempDir("testSeekZero"), null, 1 << i);
                IndexOutput io = mmapDir.CreateOutput("zeroBytes", NewIOContext(Random));
                io.Dispose();
                IndexInput ii = mmapDir.OpenInput("zeroBytes", NewIOContext(Random));
                ii.Seek(0L);
                ii.Dispose();
                mmapDir.Dispose();
            }
        }

        [Test]
        public virtual void TestSeekSliceZero()
        {
            for (int i = 0; i < 31; i++)
            {
                MMapDirectory mmapDir = new MMapDirectory(CreateTempDir("testSeekSliceZero"), null, 1 << i);
                IndexOutput io = mmapDir.CreateOutput("zeroBytes", NewIOContext(Random));
                io.Dispose();
                IndexInputSlicer slicer = mmapDir.CreateSlicer("zeroBytes", NewIOContext(Random));
                IndexInput ii = slicer.OpenSlice("zero-length slice", 0, 0);
                ii.Seek(0L);
                ii.Dispose();
                slicer.Dispose();
                mmapDir.Dispose();
            }
        }

        [Test]
        public virtual void TestSeekEnd()
        {
            for (int i = 0; i < 17; i++)
            {
                MMapDirectory mmapDir = new MMapDirectory(CreateTempDir("testSeekEnd"), null, 1 << i);
                IndexOutput io = mmapDir.CreateOutput("bytes", NewIOContext(Random));
                var bytes = new byte[1 << i];
                Random.NextBytes(bytes);
                io.WriteBytes(bytes, bytes.Length);
                io.Dispose();
                IndexInput ii = mmapDir.OpenInput("bytes", NewIOContext(Random));
                var actual = new byte[1 << i];
                ii.ReadBytes(actual, 0, actual.Length);
                Assert.AreEqual(new BytesRef(bytes), new BytesRef(actual));
                ii.Seek(1 << i);
                ii.Dispose();
                mmapDir.Dispose();
            }
        }

        [Test]
        public virtual void TestSeekSliceEnd()
        {
            for (int i = 0; i < 17; i++)
            {
                MMapDirectory mmapDir = new MMapDirectory(CreateTempDir("testSeekSliceEnd"), null, 1 << i);
                IndexOutput io = mmapDir.CreateOutput("bytes", NewIOContext(Random));
                var bytes = new byte[1 << i];
                Random.NextBytes(bytes);
                io.WriteBytes(bytes, bytes.Length);
                io.Dispose();
                IndexInputSlicer slicer = mmapDir.CreateSlicer("bytes", NewIOContext(Random));
                IndexInput ii = slicer.OpenSlice("full slice", 0, bytes.Length);
                var actual = new byte[1 << i];
                ii.ReadBytes(actual, 0, actual.Length);
                Assert.AreEqual(new BytesRef(bytes), new BytesRef(actual));
                ii.Seek(1 << i);
                ii.Dispose();
                slicer.Dispose();
                mmapDir.Dispose();
            }
        }

        [Test]
        [Slow]
        public virtual void TestSeeking()
        {
            for (int i = 0; i < 10; i++)
            {
                MMapDirectory mmapDir = new MMapDirectory(CreateTempDir("testSeeking"), null, 1 << i);
                IndexOutput io = mmapDir.CreateOutput("bytes", NewIOContext(Random));
                var bytes = new byte[1 << (i + 1)]; // make sure we switch buffers
                Random.NextBytes(bytes);
                io.WriteBytes(bytes, bytes.Length);
                io.Dispose();
                IndexInput ii = mmapDir.OpenInput("bytes", NewIOContext(Random));
                var actual = new byte[1 << (i + 1)]; // first read all bytes
                ii.ReadBytes(actual, 0, actual.Length);
                Assert.AreEqual(new BytesRef(bytes), new BytesRef(actual));
                for (int sliceStart = 0; sliceStart < bytes.Length; sliceStart++)
                {
                    for (int sliceLength = 0; sliceLength < bytes.Length - sliceStart; sliceLength++)
                    {
                        var slice = new byte[sliceLength];
                        ii.Seek(sliceStart);
                        ii.ReadBytes(slice, 0, slice.Length);
                        Assert.AreEqual(new BytesRef(bytes, sliceStart, sliceLength), new BytesRef(slice));
                    }
                }
                ii.Dispose();
                mmapDir.Dispose();
            }
        }

        // note instead of seeking to offset and reading length, this opens slices at the
        // the various offset+length and just does readBytes.
        [Test]
        [Slow]
        public virtual void TestSlicedSeeking()
        {
            for (int i = 0; i < 10; i++)
            {
                MMapDirectory mmapDir = new MMapDirectory(CreateTempDir("testSlicedSeeking"), null, 1 << i);
                IndexOutput io = mmapDir.CreateOutput("bytes", NewIOContext(Random));
                var bytes = new byte[1 << (i + 1)]; // make sure we switch buffers
                Random.NextBytes(bytes);
                io.WriteBytes(bytes, bytes.Length);
                io.Dispose();
                IndexInput ii = mmapDir.OpenInput("bytes", NewIOContext(Random));
                var actual = new byte[1 << (i + 1)]; // first read all bytes
                ii.ReadBytes(actual, 0, actual.Length);
                ii.Dispose();
                Assert.AreEqual(new BytesRef(bytes), new BytesRef(actual));
                IndexInputSlicer slicer = mmapDir.CreateSlicer("bytes", NewIOContext(Random));
                for (int sliceStart = 0; sliceStart < bytes.Length; sliceStart++)
                {
                    for (int sliceLength = 0; sliceLength < bytes.Length - sliceStart; sliceLength++)
                    {
                        var slice = new byte[sliceLength];
                        IndexInput input = slicer.OpenSlice("bytesSlice", sliceStart, slice.Length);
                        input.ReadBytes(slice, 0, slice.Length);
                        input.Dispose();
                        Assert.AreEqual(new BytesRef(bytes, sliceStart, sliceLength), new BytesRef(slice));
                    }
                }
                slicer.Dispose();
                mmapDir.Dispose();
            }
        }

        [Test]
        public virtual void TestRandomChunkSizes()
        {
            int num = AtLeast(10);
            for (int i = 0; i < num; i++)
            {
                AssertChunking(Random, TestUtil.NextInt32(Random, 20, 100));
            }
        }

        private void AssertChunking(Random random, int chunkSize)
        {
            DirectoryInfo path = CreateTempDir("mmap" + chunkSize);
            MMapDirectory mmapDir = new MMapDirectory(path, null, chunkSize);
            // LUCENENET specific - unmap hack not needed
            //// we will map a lot, try to turn on the unmap hack
            //if (MMapDirectory.UNMAP_SUPPORTED)
            //{
            //    mmapDir.UseUnmap = true;
            //}
            MockDirectoryWrapper dir = new MockDirectoryWrapper(random, mmapDir);
            RandomIndexWriter writer = new RandomIndexWriter(random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetMergePolicy(NewLogMergePolicy()));
            Document doc = new Document();
            Field docid = NewStringField("docid", "0", Field.Store.YES);
            Field junk = NewStringField("junk", "", Field.Store.YES);
            doc.Add(docid);
            doc.Add(junk);

            int numDocs = 100;
            for (int i = 0; i < numDocs; i++)
            {
                docid.SetStringValue("" + i);
                junk.SetStringValue(TestUtil.RandomUnicodeString(random));
                writer.AddDocument(doc);
            }
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            int numAsserts = AtLeast(100);
            for (int i = 0; i < numAsserts; i++)
            {
                int docID = random.Next(numDocs);
                Assert.AreEqual("" + docID, reader.Document(docID).Get("docid"));
            }
            reader.Dispose();
            dir.Dispose();
        }


        // LUCENENET: Regression test for GitHub #1090. A background thread
        // extends a file on disk while the foreground thread repeatedly
        // opens it with MMapDirectory.OpenInput. Before the fix, the
        // file's length could grow between the caller capturing fc.Length
        // and MemoryMappedFile.CreateFromFile performing its internal
        // stat, causing ArgumentOutOfRangeException (paramName="capacity")
        // with the message "The capacity may not be smaller than the
        // file size."
        // NonParallelizable: the retry-path assertion reads static counters on
        // MMapDirectory, so any other test exercising MMapDirectory in parallel
        // could skew the observed retry count.
        [Test, LuceneNetSpecific, Slow, NonParallelizable]
        public void TestOpenInputConcurrentFileExtension_Issue1090()
        {
            var dir = CreateTempDir("testOpenInputConcurrentFileExtension");
            const string name = "data.bin";
            string filePath = Path.Combine(dir.FullName, name);

            // Seed with a small initial payload.
            File.WriteAllBytes(filePath, new byte[64]);

            using var mmapDir = new MMapDirectory(dir);

            const long maxFileSize = 1L * 1024 * 1024; // 1 MiB cap
            var stop = new ManualResetEventSlim(false);
            Exception writerError = null;

            var writer = new Thread(() =>
            {
                var chunk = new byte[64];
                try
                {
                    while (!stop.IsSet)
                    {
                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                        fs.Seek(0, SeekOrigin.End);
                        if (fs.Length < maxFileSize)
                        {
                            fs.Write(chunk, 0, chunk.Length);
                        }
                        else
                        {
                            // Keep the file bounded: truncate back and grow again.
                            fs.SetLength(64);
                        }
                    }
                }
                catch (Exception e)
                {
                    writerError = e;
                }
            })
            { IsBackground = true, Name = "mmap-issue1090-extender" };
            writer.Start();

            // Snapshot counters so this test's assertion is not affected by
            // any earlier test's activity on MMapDirectory.
            long baselineRetries = Interlocked.Read(ref MMapDirectory.s_capacityRetryCount);

            try
            {
                var sw = Stopwatch.StartNew();
                int iterations = 0;
                // Keep stretching the window until either the race fires or we
                // hit a hard deadline. On most machines this takes < 1 second.
                const int maxSeconds = 15;
                while (sw.Elapsed < TimeSpan.FromSeconds(maxSeconds))
                {
                    using (var _ = mmapDir.OpenInput(name, NewIOContext(Random)))
                    {
                        // Just open and dispose; the race occurs during construction.
                    }
                    iterations++;

                    if (Interlocked.Read(ref MMapDirectory.s_capacityRetryCount) > baselineRetries)
                    {
                        break; // race reproduced and handled by the retry loop
                    }
                }

                long retries = Interlocked.Read(ref MMapDirectory.s_capacityRetryCount) - baselineRetries;
                int maxAttempts = Volatile.Read(ref MMapDirectory.s_maxCapacityAttemptsObserved);

                // Surface what was observed for diagnostics when run with -v normal.
                TestContext.Progress.WriteLine(
                    $"TestOpenInputConcurrentFileExtension: iterations={iterations}, retries={retries}, maxAttemptsObserved={maxAttempts}");

                // The real check: the race must have fired and our retry loop
                // must have swallowed it. Without the fix, the exception
                // escapes OpenInput and the test fails with ArgumentOutOfRangeException
                // (as seen in #1090). If the race never fires during this run
                // (timing-dependent), mark the test inconclusive rather than
                // silently passing — we haven't actually exercised the fix.
                if (retries == 0)
                {
                    NUnit.Framework.Assert.Inconclusive(
                        $"The concurrent-extension race was not reproduced within {maxSeconds}s " +
                        $"({iterations} OpenInput iterations). The fix was therefore not exercised on this run.");
                }
            }
            finally
            {
                stop.Set();
                writer.Join();
            }

            if (writerError != null)
            {
                throw new Exception("Writer thread failed", writerError);
            }
        }

        // Regression test for issue #1013: sporadic AccessViolationException
        // during concurrent search with SearcherManager on MMapDirectory.
        //
        // Strategy: spin many reader threads cloning + reading a shared
        // IndexInput while another thread disposes it mid-flight. The
        // invariant under test: concurrent Clone/read against a Dispose
        // must only ever surface AlreadyClosed-style exceptions — never an
        // AVE (which crashes the test host), never an NRE, never an IOE
        // from a half-torn-down mapping.
        //
        // Under the new MemoryMappedViewAccessorIndexInput design, clones
        // observe the shared View's closed flag and throw AlreadyClosed
        // promptly after the root is disposed. A successful pass here is
        // therefore a *positive* result — not Inconclusive — because the
        // expected behavior is that the invariant holds throughout.
        // [Nightly]: wall-clock stress loop (up to ~30s). Kept out of the
        // default run so CI isn't lengthened, but exercised in nightly runs
        // where catching regressions in the #1013 race path is worth the time.
        [Test, LuceneNetSpecific, Slow, Nightly, NonParallelizable]
        public void TestConcurrentCloneReadVsDispose_Issue1013()
        {
            var dirPath = CreateTempDir("testIssue1013");
            using var mmapDir = new MMapDirectory(dirPath);

            const string name = "bytes";
            const int fileSize = 1 << 20; // 1 MiB
            using (var io = mmapDir.CreateOutput(name, NewIOContext(Random)))
            {
                var buf = new byte[4096];
                new Random(42).NextBytes(buf);
                for (int written = 0; written < fileSize; written += buf.Length)
                {
                    io.WriteBytes(buf, 0, buf.Length);
                }
            }

            const int readerThreads = 8;
            const int maxSeconds = 30;

            var sw = Stopwatch.StartNew();
            int iteration = 0;
            int raceObserved = 0;
            var unexpectedExceptions = new ConcurrentBag<Exception>();

            while (sw.Elapsed < TimeSpan.FromSeconds(maxSeconds) && raceObserved == 0)
            {
                iteration++;

                var master = mmapDir.OpenInput(name, NewIOContext(Random));
                var start = new ManualResetEventSlim(false);
                var threads = new Thread[readerThreads];
                long totalReads = 0;

                for (int i = 0; i < readerThreads; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        start.Wait();
                        try
                        {
                            while (true)
                            {
                                IndexInput clone;
                                try
                                {
                                    clone = (IndexInput)master.Clone();
                                }
                                catch (Exception e) when (e.IsAlreadyClosedException())
                                {
                                    return;
                                }

                                try
                                {
                                    for (int p = 0; p < fileSize; p++)
                                    {
                                        clone.ReadByte();
                                        Interlocked.Increment(ref totalReads);
                                    }
                                }
                                catch (Exception e) when (e.IsAlreadyClosedException())
                                {
                                    return;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            unexpectedExceptions.Add(e);
                            Interlocked.Exchange(ref raceObserved, 1);
                        }
                    })
                    { IsBackground = true, Name = $"issue1013-reader-{i}" };
                    threads[i].Start();
                }

                start.Set();

                Thread.Sleep(Random.Next(1, 5));
                master.Dispose();

                // Join every reader. The View's drain barrier ensures Dispose
                // only returns after in-flight reads complete, and clones
                // then observe view.IsClosed via TryEnterRead and exit via
                // the expected-AlreadyClosed catch. So Join timing out would
                // itself be a defect — either the drain barrier is leaking
                // or a reader is stuck in a broken state. Record that and
                // fail rather than silently abandoning the thread.
                foreach (var t in threads)
                {
                    if (!t.Join(TimeSpan.FromSeconds(10)))
                    {
                        unexpectedExceptions.Add(new TimeoutException(
                            $"Reader thread {t.Name} did not exit within 10s after master.Dispose(); " +
                            "expected AlreadyClosed to propagate out of ReadInternal/TryEnterRead."));
                        Interlocked.Exchange(ref raceObserved, 1);
                        // Continue joining the rest so we don't leak live
                        // threads holding IndexInput clones into later
                        // iterations or subsequent tests.
                    }
                }

                if (iteration % 50 == 0)
                {
                    TestContext.Progress.WriteLine(
                        $"issue1013 repro: iteration={iteration}, elapsed={sw.Elapsed.TotalSeconds:0.0}s, reads={totalReads}");
                }
            }

            if (raceObserved != 0)
            {
                var example = unexpectedExceptions.FirstOrDefault();
                Assert.Fail(
                    $"Issue #1013 invariant violated on iteration {iteration}: " +
                    $"concurrent clone/read vs Dispose produced an unexpected exception type. " +
                    $"Example: {example?.GetType().FullName}: {example?.Message}\n{example}");
            }

            Assert.Pass(
                $"Issue #1013 invariant held across {iteration} iterations in " +
                $"{sw.Elapsed.TotalSeconds:0.0}s — no AVE / NRE / unexpected exception " +
                "under concurrent clone/read vs Dispose.");
        }

        // LUCENENET-specific: race-condition coverage for the new
        // MemoryMappedViewAccessorIndexInput. These tests complement the
        // single-threaded TestCloneClose / TestCloneSliceSafety /
        // TestCloneSliceClose tests by exercising concurrent Dispose vs.
        // read, Dispose vs. Clone, and slicer-cascade scenarios.

        // Concurrent Clone during Dispose: the root is disposed while many
        // threads repeatedly call Clone() + ReadByte(). Invariants:
        //   - No AVE / NRE / memory corruption.
        //   - Once master.Dispose has returned and the cloner thread has
        //     observed that (via TryEnterRead), subsequent reads on its
        //     current clone throw AlreadyClosed.
        //   - After join, calling Clone() + read on the disposed master
        //     from the main thread throws AlreadyClosed — pinning that a
        //     disposed root cannot silently hand out a working clone.
        // [Nightly]: wall-clock stress loop (~15s). See Issue1013 rationale.
        [Test, LuceneNetSpecific, Slow, Nightly, NonParallelizable]
        public void TestConcurrentCloneVsDispose_RaceScenario()
        {
            var dirPath = CreateTempDir("testCloneVsDispose");
            using var mmapDir = new MMapDirectory(dirPath);
            const string name = "bytes";
            const int fileSize = 64 * 1024;
            using (var io = mmapDir.CreateOutput(name, NewIOContext(Random)))
            {
                var buf = new byte[4096];
                new Random(7).NextBytes(buf);
                for (int w = 0; w < fileSize; w += buf.Length)
                    io.WriteBytes(buf, 0, buf.Length);
            }

            var unexpected = new ConcurrentBag<Exception>();
            int iterations = 0;
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(15))
            {
                iterations++;
                var master = mmapDir.OpenInput(name, NewIOContext(Random));
                var start = new ManualResetEventSlim(false);
                var cloners = new Thread[6];
                for (int i = 0; i < cloners.Length; i++)
                {
                    cloners[i] = new Thread(() =>
                    {
                        start.Wait();
                        try
                        {
                            while (true)
                            {
                                IndexInput c;
                                try { c = (IndexInput)master.Clone(); }
                                catch (Exception e) when (e.IsAlreadyClosedException()) { return; }
                                // Touch a byte on the clone — but don't read past dispose to keep the test focused on Clone itself.
                                try { c.ReadByte(); }
                                catch (Exception e) when (e.IsAlreadyClosedException()) { return; }
                            }
                        }
                        catch (Exception e) { unexpected.Add(e); }
                    }) { IsBackground = true };
                    cloners[i].Start();
                }
                start.Set();
                Thread.Sleep(Random.Next(0, 3));
                master.Dispose();
                foreach (var t in cloners)
                {
                    if (!t.Join(TimeSpan.FromSeconds(5)))
                    {
                        unexpected.Add(new TimeoutException(
                            "Cloner thread did not exit within 5s after master.Dispose()."));
                    }
                }

                // Positive contract check: after Dispose, Clone() on the
                // disposed root either throws AlreadyClosed or produces a
                // clone whose first read throws AlreadyClosed. The failure
                // mode we want to catch is a clone that silently hands back
                // bytes from a released mapping.
                try
                {
                    var postDisposeClone = (IndexInput)master.Clone();
                    try
                    {
                        postDisposeClone.ReadByte();
                        unexpected.Add(new InvalidOperationException(
                            "Clone() + ReadByte() on disposed master returned without throwing AlreadyClosed."));
                    }
                    catch (Exception e) when (e.IsAlreadyClosedException())
                    {
                        // expected
                    }
                }
                catch (Exception e) when (e.IsAlreadyClosedException())
                {
                    // also acceptable: Clone() itself refused
                }
            }

            if (!unexpected.IsEmpty)
            {
                var ex = unexpected.First();
                Assert.Fail($"Concurrent Clone-vs-Dispose produced unexpected exception after {iterations} iterations: {ex.GetType().FullName}: {ex.Message}\n{ex}");
            }
        }

        // Concurrent read of the SAME instance during Dispose of that
        // instance. Drain-barrier must prevent the disposer from releasing
        // the pointer while a reader is mid-CopyBlockUnaligned.
        // [Nightly]: wall-clock stress loop (~15s). See Issue1013 rationale.
        [Test, LuceneNetSpecific, Slow, Nightly, NonParallelizable]
        public void TestConcurrentReadVsSelfDispose_RaceScenario()
        {
            var dirPath = CreateTempDir("testReadVsSelfDispose");
            using var mmapDir = new MMapDirectory(dirPath);
            const string name = "bytes";
            const int fileSize = 1 << 18; // 256 KiB — enough for several buffer refills
            using (var io = mmapDir.CreateOutput(name, NewIOContext(Random)))
            {
                var buf = new byte[4096];
                new Random(11).NextBytes(buf);
                for (int w = 0; w < fileSize; w += buf.Length)
                    io.WriteBytes(buf, 0, buf.Length);
            }

            var unexpected = new ConcurrentBag<Exception>();
            int iterations = 0;
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(15) && unexpected.IsEmpty)
            {
                iterations++;
                var input = mmapDir.OpenInput(name, NewIOContext(Random));
                var start = new ManualResetEventSlim(false);
                var readers = new Thread[4];
                for (int i = 0; i < readers.Length; i++)
                {
                    readers[i] = new Thread(() =>
                    {
                        // Each reader uses its OWN clone — contract says
                        // IndexInput isn't thread-safe. We want to stress
                        // the drain barrier on the shared View, not on the
                        // single IndexInput.
                        IndexInput clone;
                        try { clone = (IndexInput)input.Clone(); }
                        catch (Exception e) when (e.IsAlreadyClosedException()) { return; }

                        start.Wait();
                        try
                        {
                            while (true)
                            {
                                clone.Seek(0);
                                for (int p = 0; p < fileSize; p++)
                                {
                                    clone.ReadByte();
                                }
                            }
                        }
                        catch (Exception e) when (e.IsAlreadyClosedException()) { return; }
                        catch (Exception e) { unexpected.Add(e); }
                    }) { IsBackground = true };
                    readers[i].Start();
                }
                start.Set();
                Thread.Sleep(Random.Next(1, 5));
                input.Dispose();
                foreach (var t in readers) t.Join(TimeSpan.FromSeconds(5));
            }

            if (!unexpected.IsEmpty)
            {
                var ex = unexpected.First();
                Assert.Fail($"Concurrent read-vs-self-dispose produced unexpected exception after {iterations} iterations: {ex.GetType().FullName}: {ex.Message}\n{ex}");
            }
        }

        // Slicer Dispose while slices are being read concurrently. The
        // slicer cascades Dispose to all issued slices; every in-flight
        // reader must either finish its current CopyBlockUnaligned or
        // observe the closed state cleanly (no AVE).
        // [Nightly]: wall-clock stress loop (~15s). See Issue1013 rationale.
        [Test, LuceneNetSpecific, Slow, Nightly, NonParallelizable]
        public void TestConcurrentSliceReadVsSlicerDispose_RaceScenario()
        {
            var dirPath = CreateTempDir("testSliceVsSlicerDispose");
            using var mmapDir = new MMapDirectory(dirPath);
            const string name = "bytes";
            const int fileSize = 1 << 18;
            using (var io = mmapDir.CreateOutput(name, NewIOContext(Random)))
            {
                var buf = new byte[4096];
                new Random(13).NextBytes(buf);
                for (int w = 0; w < fileSize; w += buf.Length)
                    io.WriteBytes(buf, 0, buf.Length);
            }

            var unexpected = new ConcurrentBag<Exception>();
            int iterations = 0;
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(15) && unexpected.IsEmpty)
            {
                iterations++;
                var slicer = mmapDir.CreateSlicer(name, NewIOContext(Random));
                var start = new ManualResetEventSlim(false);
                var readers = new Thread[4];
                for (int i = 0; i < readers.Length; i++)
                {
                    int sliceIndex = i;
                    readers[i] = new Thread(() =>
                    {
                        IndexInput slice;
                        try
                        {
                            slice = slicer.OpenSlice("slice" + sliceIndex, 0, fileSize);
                        }
                        catch (Exception e) when (e.IsAlreadyClosedException()) { return; }

                        start.Wait();
                        try
                        {
                            while (true)
                            {
                                slice.Seek(0);
                                for (int p = 0; p < fileSize; p++)
                                    slice.ReadByte();
                            }
                        }
                        catch (Exception e) when (e.IsAlreadyClosedException()) { return; }
                        catch (Exception e) { unexpected.Add(e); }
                    }) { IsBackground = true };
                    readers[i].Start();
                }
                start.Set();
                Thread.Sleep(Random.Next(1, 5));
                slicer.Dispose();
                foreach (var t in readers) t.Join(TimeSpan.FromSeconds(5));
            }

            if (!unexpected.IsEmpty)
            {
                var ex = unexpected.First();
                Assert.Fail($"Concurrent slice-read-vs-slicer-dispose produced unexpected exception after {iterations} iterations: {ex.GetType().FullName}: {ex.Message}\n{ex}");
            }
        }

        // Empty (zero-byte) file edge case: OpenInput/Clone/Dispose must not
        // AVE or throw. Zero-length files take a different MapAndAcquire
        // path (no MMF, no AcquirePointer), so ReadInternal hits the
        // basePtr==null branch and must handle a zero-byte read cleanly
        // while rejecting a non-zero read with EOF.
        [Test, LuceneNetSpecific]
        public void TestZeroLengthFile_ReadsAndCloneAndDispose()
        {
            var dirPath = CreateTempDir("testZeroLengthFile");
            using var mmapDir = new MMapDirectory(dirPath);
            using (var io = mmapDir.CreateOutput("empty", NewIOContext(Random))) { }

            using var input = mmapDir.OpenInput("empty", NewIOContext(Random));
            Assert.AreEqual(0L, input.Length);
            using var clone = (IndexInput)input.Clone();
            Assert.AreEqual(0L, clone.Length);
            Assert.AreEqual(0L, input.Position);
            Assert.AreEqual(0L, clone.Position);

            // Exercise the ReadInternal basePtr==null, destination.Length==0
            // branch. This should succeed silently.
            input.ReadBytes(Array.Empty<byte>(), 0, 0);
            clone.ReadBytes(Array.Empty<byte>(), 0, 0);

            // Any non-zero read must throw EOF (we can't satisfy it from 0
            // bytes of mapped data).
            try
            {
                input.ReadByte();
                Assert.Fail("ReadByte on zero-length file must throw EOF");
            }
            catch (EndOfStreamException)
            {
                // expected
            }
        }

        // Read-after-Dispose on a single instance (not concurrent): the new
        // class must throw AlreadyClosedException rather than reading stale
        // bytes from the unmapped region. This is the single-threaded
        // correctness analogue of #1013.
        [Test, LuceneNetSpecific]
        public void TestReadAfterDispose_ThrowsAlreadyClosed()
        {
            var dirPath = CreateTempDir("testReadAfterDispose");
            using var mmapDir = new MMapDirectory(dirPath);
            using (var io = mmapDir.CreateOutput("bytes", NewIOContext(Random)))
            {
                io.WriteInt32(42);
            }

            var input = mmapDir.OpenInput("bytes", NewIOContext(Random));
            input.Dispose();
            try
            {
                input.ReadInt32();
                Assert.Fail("Must throw AlreadyClosedException after Dispose");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // pass
            }
        }

        // Clone-after-Dispose: pins the current contract that Clone() on a
        // disposed root does NOT throw (MemberwiseClone is just a managed
        // object copy), but the cloned IndexInput observes the shared
        // View's closed flag on its first read and throws AlreadyClosed.
        // Likewise, disposing a clone does not close the shared View, so
        // the root + sibling clones stay alive.
        [Test, LuceneNetSpecific]
        public void TestCloneAfterDispose_ReadsThrowAlreadyClosed()
        {
            var dirPath = CreateTempDir("testCloneAfterDispose");
            using var mmapDir = new MMapDirectory(dirPath);
            using (var io = mmapDir.CreateOutput("bytes", NewIOContext(Random)))
            {
                io.WriteInt32(1);
                io.WriteInt32(2);
            }

            var root = mmapDir.OpenInput("bytes", NewIOContext(Random));

            // Clone taken BEFORE the root is disposed: must fail on read
            // after the root is disposed, because the shared View is closed.
            var preDisposeClone = (IndexInput)root.Clone();

            root.Dispose();

            try
            {
                preDisposeClone.ReadInt32();
                Assert.Fail("Pre-dispose clone must throw AlreadyClosed after root Dispose");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }

            // Clone taken AFTER the root is disposed. MemberwiseClone still
            // succeeds (no synchronization in object Clone), but the first
            // read must observe view.IsClosed via TryEnterRead and throw.
            var postDisposeClone = (IndexInput)root.Clone();
            try
            {
                postDisposeClone.ReadInt32();
                Assert.Fail("Post-dispose clone must throw AlreadyClosed on first read");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }
        }

        // Sibling isolation: disposing one clone does NOT close the shared
        // View, so the root and other clones continue to read. Counterpart
        // to TestCloneAfterDispose — covers the case where a clone is
        // disposed first, rather than the root.
        [Test, LuceneNetSpecific]
        public void TestDisposingCloneDoesNotAffectRootOrSiblings()
        {
            var dirPath = CreateTempDir("testCloneSiblingIsolation");
            using var mmapDir = new MMapDirectory(dirPath);
            using (var io = mmapDir.CreateOutput("bytes", NewIOContext(Random)))
            {
                io.WriteInt32(1);
                io.WriteInt32(2);
            }

            using var root = mmapDir.OpenInput("bytes", NewIOContext(Random));
            var cloneA = (IndexInput)root.Clone();
            var cloneB = (IndexInput)root.Clone();

            cloneA.Dispose();

            // Root and sibling clone must still work.
            Assert.AreEqual(1, root.ReadInt32());
            Assert.AreEqual(1, cloneB.ReadInt32());

            // Disposed clone must throw on read.
            try
            {
                cloneA.ReadInt32();
                Assert.Fail("Disposed clone must throw AlreadyClosed");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }

            cloneB.Dispose();
        }

        // Multiple slices over the same file must each see their own region
        // of data, not stale bytes from a neighboring slice. Each OpenSlice
        // in the new design opens its own MemoryMappedFile+view over the
        // given (offset, length) window, so bounds are enforced per-slice
        // and reads are independent.
        [Test, LuceneNetSpecific]
        public void TestMultipleSlicesReadDistinctData()
        {
            var dirPath = CreateTempDir("testMultipleSlicesDistinct");
            using var mmapDir = new MMapDirectory(dirPath);
            const string name = "bytes";
            const int regionSize = 4096;
            const int regions = 4;

            // Write four contiguous 4KiB regions, each filled with a distinct
            // byte pattern (0x11, 0x22, 0x33, 0x44). Later we open a slice
            // over each region and check that reads return the right pattern.
            using (var io = mmapDir.CreateOutput(name, NewIOContext(Random)))
            {
                var buf = new byte[regionSize];
                for (int r = 0; r < regions; r++)
                {
                    byte fill = (byte)(0x11 * (r + 1));
                    for (int i = 0; i < buf.Length; i++) buf[i] = fill;
                    io.WriteBytes(buf, 0, buf.Length);
                }
            }

            using var slicer = mmapDir.CreateSlicer(name, NewIOContext(Random));

            var slices = new IndexInput[regions];
            try
            {
                for (int r = 0; r < regions; r++)
                {
                    slices[r] = slicer.OpenSlice("slice" + r, r * regionSize, regionSize);
                    Assert.AreEqual(regionSize, slices[r].Length, $"slice {r} length");
                }

                // Each slice sees only its own pattern.
                for (int r = 0; r < regions; r++)
                {
                    byte expected = (byte)(0x11 * (r + 1));
                    for (int i = 0; i < regionSize; i++)
                    {
                        byte b = slices[r].ReadByte();
                        if (b != expected)
                        {
                            Assert.Fail(
                                $"slice {r} at offset {i}: expected 0x{expected:X2}, got 0x{b:X2}");
                        }
                    }

                    // Reading past the slice's length must fail with EOF.
                    try
                    {
                        slices[r].ReadByte();
                        Assert.Fail($"slice {r}: read past end of slice must throw EOF");
                    }
                    catch (EndOfStreamException)
                    {
                        // expected
                    }
                }

                // Concurrent reads across sibling slices: each worker scans
                // its own slice fully and asserts the pattern. If the slices
                // were accidentally aliased to the same underlying view,
                // racing Seek() calls would cross-contaminate.
                var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
                var tasks = new System.Threading.Tasks.Task[regions];
                for (int r = 0; r < regions; r++)
                {
                    int idx = r;
                    byte expected = (byte)(0x11 * (idx + 1));
                    var clone = (IndexInput)slices[idx].Clone();
                    tasks[idx] = System.Threading.Tasks.Task.Run(() =>
                    {
                        for (int pass = 0; pass < 50; pass++)
                        {
                            clone.Seek(0);
                            for (int i = 0; i < regionSize; i++)
                            {
                                byte b = clone.ReadByte();
                                if (b != expected)
                                {
                                    errors.Add($"slice {idx} pass {pass} offset {i}: expected 0x{expected:X2}, got 0x{b:X2}");
                                    return;
                                }
                            }
                        }
                    });
                }
                System.Threading.Tasks.Task.WaitAll(tasks);
                if (!errors.IsEmpty)
                {
                    Assert.Fail("Cross-slice contamination detected:\n" + string.Join("\n", errors));
                }
            }
            finally
            {
                foreach (var s in slices) s?.Dispose();
            }
        }

        // Disposing a single slice must not affect its sibling slices from
        // the same slicer. In the new design each OpenSlice has its own
        // View, so slice.Dispose closes that slice's view only. Slicer
        // cascade Dispose is covered by TestCloneSliceSafety.
        [Test, LuceneNetSpecific]
        public void TestDisposingOneSliceDoesNotAffectSiblings()
        {
            var dirPath = CreateTempDir("testSliceSiblingIsolation");
            using var mmapDir = new MMapDirectory(dirPath);
            const string name = "bytes";
            using (var io = mmapDir.CreateOutput(name, NewIOContext(Random)))
            {
                for (int i = 0; i < 8; i++) io.WriteInt32(i);
            }

            using var slicer = mmapDir.CreateSlicer(name, NewIOContext(Random));
            var sliceA = slicer.OpenSlice("sliceA", 0, 16);
            var sliceB = slicer.OpenSlice("sliceB", 16, 16);

            sliceA.Dispose();

            // Sibling must continue to work and return its own data.
            Assert.AreEqual(4, sliceB.ReadInt32());
            Assert.AreEqual(5, sliceB.ReadInt32());

            // Disposed slice must throw on read.
            try
            {
                sliceA.ReadInt32();
                Assert.Fail("Disposed slice must throw AlreadyClosed");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }

            sliceB.Dispose();
        }

        // Concurrent-clone read correctness. N workers each take a Clone()
        // of the same root input, seek independently, and read the full
        // file; every worker must observe the exact bytes that were
        // written. Existing concurrent tests only check that reads don't
        // throw — this test pins that readers also don't observe torn or
        // stale bytes under contention over the shared View.basePtr.
        [Test, LuceneNetSpecific, Slow]
        public void TestConcurrentClonesReadIdenticalBytes()
        {
            var dirPath = CreateTempDir("testConcurrentClonesIntegrity");
            using var mmapDir = new MMapDirectory(dirPath);
            const string name = "bytes";
            // 2 MiB — large enough that readers overlap in time, small
            // enough that the test finishes in well under a second per
            // iteration.
            const int fileSize = 2 * 1024 * 1024;
            var expected = new byte[fileSize];
            new Random(0xC0FFEE).NextBytes(expected);

            using (var io = mmapDir.CreateOutput(name, NewIOContext(Random)))
            {
                io.WriteBytes(expected, 0, expected.Length);
            }

            using var root = mmapDir.OpenInput(name, NewIOContext(Random));

            const int numWorkers = 8;
            const int passesPerWorker = 20;
            var errors = new ConcurrentBag<string>();
            var clones = new IndexInput[numWorkers];
            for (int i = 0; i < numWorkers; i++)
            {
                clones[i] = (IndexInput)root.Clone();
            }

            var start = new ManualResetEventSlim(false);
            var threads = new Thread[numWorkers];
            for (int i = 0; i < numWorkers; i++)
            {
                int idx = i;
                threads[i] = new Thread(() =>
                {
                    var buf = new byte[fileSize];
                    start.Wait();
                    for (int pass = 0; pass < passesPerWorker && errors.IsEmpty; pass++)
                    {
                        clones[idx].Seek(0);
                        clones[idx].ReadBytes(buf, 0, buf.Length);
                        // Byte-equal check: any deviation means the read
                        // path observed torn or stale data.
                        for (int j = 0; j < buf.Length; j++)
                        {
                            if (buf[j] != expected[j])
                            {
                                errors.Add(
                                    $"worker {idx} pass {pass}: byte at {j} expected 0x{expected[j]:X2}, got 0x{buf[j]:X2}");
                                return;
                            }
                        }
                    }
                }) { IsBackground = true, Name = "concurrent-integrity-" + i };
                threads[i].Start();
            }
            start.Set();
            foreach (var t in threads) t.Join(TimeSpan.FromSeconds(30));

            if (!errors.IsEmpty)
            {
                Assert.Fail("Concurrent-clone read corruption detected:\n" + string.Join("\n", errors));
            }
        }

        // Open a 3.0 CFS index with MMapDirectory and read it end-to-end
        // through DirectoryReader. 3.0 (pre-3.1) CFS files are the only
        // remaining caller of IndexInputSlicer.OpenFullSlice, which in our
        // new slicer requires a disposed-state check before it can trust
        // descriptor.Length. If that path is broken, DirectoryReader.Open
        // will throw while reading segment headers.
        [Test, LuceneNetSpecific]
        public void TestRead3xCfsIndex_ViaMMap()
        {
            var indexDir = CreateTempDir("test3xCfsIndex");
            using (var zip = GetType().FindAndGetManifestResourceStream("index.30.cfs.zip"))
            {
                Assert.IsNotNull(zip, "expected index.30.cfs.zip to be embedded");
                TestUtil.Unzip(zip, indexDir);
            }

            using var mmapDir = new MMapDirectory(indexDir);
            using var reader = Index.DirectoryReader.Open(mmapDir);

            Assert.IsTrue(reader.MaxDoc > 0, "3.0 index should contain documents");

            // Touch each leaf to force real reads through the CFS +
            // OpenFullSlice path. This will throw AVE on a broken mmap
            // teardown or on a bad OpenFullSlice.
            int totalDocs = 0;
            foreach (var leaf in reader.Leaves)
            {
                var atomic = leaf.AtomicReader;
                for (int i = 0; i < atomic.MaxDoc; i++)
                {
                    if (atomic.LiveDocs != null && !atomic.LiveDocs.Get(i))
                        continue;
                    var doc = atomic.Document(i);
                    Assert.IsNotNull(doc, $"doc {i} in leaf {leaf} should not be null");
                    totalDocs++;
                }
            }
            Assert.IsTrue(totalDocs > 0, "at least one live 3.0 document should be readable");
        }

        // Directly exercise the OpenFullSlice entry point on a 3.0 .cfs
        // file. OpenFullSlice is [Obsolete("Only for reading CFS files
        // from 3.x indexes.")] — the test pins that it still produces an
        // IndexInput spanning the whole file and that its bytes match the
        // plain OpenInput read of the same file.
        [Test, LuceneNetSpecific]
        public void TestOpenFullSlice_On3xCfsFile_MatchesOpenInput()
        {
            var indexDir = CreateTempDir("test3xOpenFullSlice");
            using (var zip = GetType().FindAndGetManifestResourceStream("index.30.cfs.zip"))
            {
                Assert.IsNotNull(zip, "expected index.30.cfs.zip to be embedded");
                TestUtil.Unzip(zip, indexDir);
            }

            // Pick the first .cfs file in the unzipped 3.0 index.
            string cfsName = null;
            foreach (var f in indexDir.GetFiles("*.cfs"))
            {
                cfsName = f.Name;
                break;
            }
            Assert.IsNotNull(cfsName, "expected at least one .cfs file in the 3.0 index");

            using var mmapDir = new MMapDirectory(indexDir);

            byte[] viaOpenInput;
            using (var input = mmapDir.OpenInput(cfsName, NewIOContext(Random)))
            {
                viaOpenInput = new byte[input.Length];
                input.ReadBytes(viaOpenInput, 0, viaOpenInput.Length);
            }

            byte[] viaFullSlice;
            using (var slicer = mmapDir.CreateSlicer(cfsName, NewIOContext(Random)))
            {
#pragma warning disable 612, 618
                using var full = slicer.OpenFullSlice();
#pragma warning restore 612, 618
                Assert.AreEqual(viaOpenInput.Length, full.Length,
                    "OpenFullSlice length must match OpenInput length");
                viaFullSlice = new byte[full.Length];
                full.ReadBytes(viaFullSlice, 0, viaFullSlice.Length);
            }

            Assert.AreEqual(viaOpenInput, viaFullSlice,
                "OpenFullSlice bytes must match OpenInput bytes for the same CFS file");
        }

        // OpenFullSlice on a slicer that has been disposed must throw
        // AlreadyClosedException, not ObjectDisposedException leaking from
        // the underlying FileStream. Part of the review-item-6 fix.
        [Test, LuceneNetSpecific]
        public void TestOpenFullSlice_AfterDispose_ThrowsAlreadyClosed()
        {
            var dirPath = CreateTempDir("testFullSliceAfterDispose");
            using var mmapDir = new MMapDirectory(dirPath);
            using (var io = mmapDir.CreateOutput("bytes", NewIOContext(Random)))
            {
                io.WriteInt32(42);
            }

            var slicer = mmapDir.CreateSlicer("bytes", NewIOContext(Random));
            slicer.Dispose();

            try
            {
#pragma warning disable 612, 618
                slicer.OpenFullSlice();
#pragma warning restore 612, 618
                Assert.Fail("OpenFullSlice on disposed slicer must throw AlreadyClosed");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public void TestDisposeIndexInput()
        {
            string name = "foobar";
            var dir = CreateTempDir("testDisposeIndexInput");
            string fileName = Path.Combine(dir.FullName, name);

            // Create a zero byte file, and close it immediately
            File.WriteAllText(fileName, string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) /* No BOM */);

            MMapDirectory mmapDir = new MMapDirectory(dir);
            using (var _ = mmapDir.OpenInput(name, NewIOContext(Random)))
            {
            } // Dispose

            // Now it should be possible to delete the file. This is the condition we are testing for.
            File.Delete(fileName);
        }
    }
}
