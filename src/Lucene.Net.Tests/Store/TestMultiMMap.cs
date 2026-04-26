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
using System.Threading.Tasks;
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

    using BytesRef = Util.BytesRef;
    using Document = Document;
    using Field = Field;
    using IndexInputSlicer = Directory.IndexInputSlicer;
    using IndexReader = Index.IndexReader;
    using LuceneTestCase = Util.LuceneTestCase;
    using MockAnalyzer = Analysis.MockAnalyzer;
    using RandomIndexWriter = Index.RandomIndexWriter;
    using TestUtil = Util.TestUtil;

    /// <summary>
    /// Tests MMapDirectory's MultiMMapIndexInput
    /// <para/>
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

        // LUCENENET specific: exercises the shared MemoryMappedFile refactor
        // where OpenInput, CreateSlicer, its slices, and clones all piggyback
        // on a single MemoryMappedFile per file (per directory instance).
        // Verifies that (a) concurrent IndexInputs all see correct bytes,
        // (b) disposing in arbitrary order keeps siblings functional, and
        // (c) once the last referrer is disposed the OS handle is released
        // (on Windows a still-open mapping would prevent the file delete).
        [Test]
        public virtual void TestSharedMappingLifecycle()
        {
            var tempDir = CreateTempDir("testSharedMappingLifecycle");
            MMapDirectory mmapDir = new MMapDirectory(tempDir);
            const string name = "bytes";
            using (IndexOutput io = mmapDir.CreateOutput(name, NewIOContext(Random)))
            {
                // 4 ints at offsets 0, 4, 8, 12 — each slice reads a known value.
                io.WriteInt32(10);
                io.WriteInt32(20);
                io.WriteInt32(30);
                io.WriteInt32(40);
            }

            // Open several IndexInputs for the same file through both
            // OpenInput and CreateSlicer. All should share one mapping.
            IndexInput root = mmapDir.OpenInput(name, IOContext.DEFAULT);
            IndexInput rootClone = (IndexInput)root.Clone();

            IndexInputSlicer slicer = mmapDir.CreateSlicer(name, NewIOContext(Random));
            IndexInput sliceA = slicer.OpenSlice("a", 0, 4);
            IndexInput sliceB = slicer.OpenSlice("b", 8, 4);
            IndexInput sliceAClone = (IndexInput)sliceA.Clone();

            // Reads across all instances must be independent and correct.
            Assert.AreEqual(10, root.ReadInt32());
            Assert.AreEqual(10, rootClone.ReadInt32());
            Assert.AreEqual(10, sliceA.ReadInt32());
            Assert.AreEqual(30, sliceB.ReadInt32());
            Assert.AreEqual(10, sliceAClone.ReadInt32());

            // Dispose a clone first; the root and siblings must keep working.
            rootClone.Dispose();
            root.Seek(4);
            Assert.AreEqual(20, root.ReadInt32());
            sliceB.Seek(0);
            Assert.AreEqual(30, sliceB.ReadInt32());

            // Dispose a slice; its siblings from the same slicer must keep working.
            sliceAClone.Dispose();
            sliceA.Seek(0);
            Assert.AreEqual(10, sliceA.ReadInt32());

            // Dispose the remaining slice-side instances. The root IndexInput
            // still holds a refcount, so the underlying mapping must stay alive.
            sliceA.Dispose();
            sliceB.Dispose();
            slicer.Dispose();

            root.Seek(12);
            Assert.AreEqual(40, root.ReadInt32());

            // Final referrer — this should bring the refcount to zero, tear
            // down the MemoryMappedFile, and remove the dictionary entry.
            root.Dispose();

            // If any OS file handle is still open, this delete will fail on
            // Windows. On Unix it silently unlinks but the test still proves
            // the read-phase invariants above.
            mmapDir.DeleteFile(name);
            Assert.IsFalse(File.Exists(Path.Combine(tempDir.FullName, name)));

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
        // opens it with MMapDirectory.OpenInput. The original failure
        // mode was ArgumentOutOfRangeException(paramName="capacity")
        // from MemoryMappedFile.CreateFromFile, because we were passing
        // a caller-computed capacity (fc.Length) that could be smaller
        // than the actual file size by the time the framework did its
        // internal stat. The current design passes capacity: 0, which
        // tells the framework to size the mapping from the file's
        // current on-disk length atomically — there is no caller-side
        // capacity to disagree with the file. This test asserts that
        // OpenInput continues to succeed under concurrent file extension.
        [Test, LuceneNetSpecific, Slow]
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

            try
            {
                var sw = Stopwatch.StartNew();
                int iterations = 0;
                // Stress OpenInput while the background thread extends/truncates the
                // file. Each call must succeed cleanly. A short window is plenty:
                // before the capacity:0 fix this race fired in well under a second.
                const int maxSeconds = 5;
                while (sw.Elapsed < TimeSpan.FromSeconds(maxSeconds))
                {
                    using (var _ = mmapDir.OpenInput(name, NewIOContext(Random)))
                    {
                        // Just open and dispose; the race occurred during construction.
                    }
                    iterations++;
                }

                TestContext.Progress.WriteLine(
                    $"TestOpenInputConcurrentFileExtension: completed {iterations} OpenInput calls in {sw.Elapsed.TotalSeconds:F1}s");
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
            var random = Random;

            using (var io = mmapDir.CreateOutput(name, NewIOContext(random)))
            {
                var buf = new byte[4096];
                random.NextBytes(buf);
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

                var master = mmapDir.OpenInput(name, NewIOContext(random));
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

                Thread.Sleep(random.Next(1, 5));
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
            var random = Random;
            using (var io = mmapDir.CreateOutput(name, NewIOContext(random)))
            {
                var buf = new byte[4096];
                random.NextBytes(buf);
                for (int w = 0; w < fileSize; w += buf.Length)
                    io.WriteBytes(buf, 0, buf.Length);
            }

            var unexpected = new ConcurrentBag<Exception>();
            int iterations = 0;
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(15))
            {
                iterations++;
                var master = mmapDir.OpenInput(name, NewIOContext(random));
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
                Thread.Sleep(random.Next(0, 3));
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
            var random = Random;
            using (var io = mmapDir.CreateOutput(name, NewIOContext(random)))
            {
                var buf = new byte[4096];
                random.NextBytes(buf);
                for (int w = 0; w < fileSize; w += buf.Length)
                    io.WriteBytes(buf, 0, buf.Length);
            }

            var unexpected = new ConcurrentBag<Exception>();
            int iterations = 0;
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(15) && unexpected.IsEmpty)
            {
                iterations++;
                var input = mmapDir.OpenInput(name, NewIOContext(random));
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
                Thread.Sleep(random.Next(1, 5));
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
            var random = Random;
            using (var io = mmapDir.CreateOutput(name, NewIOContext(random)))
            {
                var buf = new byte[4096];
                random.NextBytes(buf);
                for (int w = 0; w < fileSize; w += buf.Length)
                    io.WriteBytes(buf, 0, buf.Length);
            }

            var unexpected = new ConcurrentBag<Exception>();
            int iterations = 0;
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(15) && unexpected.IsEmpty)
            {
                iterations++;
                var slicer = mmapDir.CreateSlicer(name, NewIOContext(random));
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
                Thread.Sleep(random.Next(1, 5));
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

            // Clone taken AFTER the root is disposed: Clone() itself must
            // throw AlreadyClosed (we check the parent's instanceClosed
            // flag at the top of Clone, matching upstream Java's behavior
            // of failing fast rather than deferring to the first read).
            try
            {
                root.Clone();
                Assert.Fail("Clone of disposed root must throw AlreadyClosed");
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
                var errors = new ConcurrentBag<string>();
                var tasks = new Task[regions];
                for (int r = 0; r < regions; r++)
                {
                    int idx = r;
                    byte expected = (byte)(0x11 * (idx + 1));
                    var clone = (IndexInput)slices[idx].Clone();
                    tasks[idx] = Task.Run(() =>
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
                Task.WaitAll(tasks);
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
            var random = Random;
            var expected = new byte[fileSize];
            random.NextBytes(expected);

            using (var io = mmapDir.CreateOutput(name, NewIOContext(random)))
            {
                io.WriteBytes(expected, 0, expected.Length);
            }

            using var root = mmapDir.OpenInput(name, NewIOContext(random));

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

        // LUCENENET specific: tests written to investigate the concern raised
        // in PR #1267 (review comment r3137038502) that the per-file shared
        // mapping cache, keyed only by file name with a fixed Length captured
        // at first-open time, could silently corrupt reads or return wrong
        // data when a file grows after its mapping is first cached.
        //
        // The Lucene directory contract is that a file is write-once: an
        // IndexOutput is opened, written, closed, and only then may any
        // IndexInput open it. Once read, the file is never extended by
        // Lucene — a new commit writes new segment files, it does not append
        // to existing ones. Readers capture the file length at open time and
        // never read past it (EOFException otherwise). These tests document
        // and pin that contract against the current design.

        /// <summary>
        /// After a reader opens an IndexInput, a concurrent (non-Lucene)
        /// writer extending the same file must NOT change what the reader
        /// observes: the IndexInput's Length is a snapshot, reads within
        /// [0, Length) return the bytes that were present at open time, and
        /// reads past Length throw EOFException. This is the "snapshot
        /// length at open time" invariant — the same behavior Java Lucene
        /// relies on.
        /// </summary>
        [Test, LuceneNetSpecific]
        public void TestGrowthAfterOpen_IsSnapshotAtOpenTime()
        {
            var dirPath = CreateTempDir("testGrowthAfterOpen");
            const string name = "data.bin";
            string filePath = Path.Combine(dirPath.FullName, name);

            // Seed with a known pattern of 64 bytes.
            var initial = new byte[64];
            for (int i = 0; i < initial.Length; i++) initial[i] = (byte)i;
            File.WriteAllBytes(filePath, initial);

            using var mmapDir = new MMapDirectory(dirPath);
            using var input = mmapDir.OpenInput(name, NewIOContext(Random));

            Assert.AreEqual(64L, input.Length, "IndexInput.Length must be the snapshot taken at open time.");

            // Extend the file externally by another 64 bytes of different data.
            using (var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                var extra = new byte[64];
                for (int i = 0; i < extra.Length; i++) extra[i] = (byte)(0xFF - i);
                fs.Write(extra, 0, extra.Length);
            }

            // Length must not change — the IndexInput is pinned to its
            // snapshot. This is the "do not track live file length"
            // contract: the mapping reflects the state at open time.
            Assert.AreEqual(64L, input.Length,
                "Growth of the underlying file must NOT be reflected in a previously-opened IndexInput.");

            // Reads within the captured window must return the original bytes.
            input.Seek(0);
            var buf = new byte[64];
            input.ReadBytes(buf, 0, buf.Length);
            for (int i = 0; i < buf.Length; i++)
            {
                Assert.AreEqual((byte)i, buf[i], $"byte[{i}] must be the original value");
            }

            // Reads past the captured Length must throw EOFException, not
            // return stale/new bytes. This is what protects Lucene readers
            // from reading partially-written data in a file that a writer
            // is still extending.
            input.Seek(60);
            Assert.Throws<EndOfStreamException>(() =>
            {
                var overflow = new byte[8];
                input.ReadBytes(overflow, 0, overflow.Length);
            }, "Reading past the snapshot Length must throw EOFException.");
        }

        /// <summary>
        /// If the file has grown on disk between two separate OpenInput
        /// calls (even for the same file name), the second caller must
        /// observe the CURRENT file length, not the length cached from
        /// the first open.
        /// <para/>
        /// This is the concern ChatGPT actually articulated in review
        /// comment r3137038502: a per-file cache keyed only by file name,
        /// with a fixed Length captured at first mapping time, cannot
        /// serve a later OpenInput that needs to see bytes the first
        /// mapping doesn't know about.
        /// <para/>
        /// Note: this is NOT a scenario that arises under normal Lucene
        /// operation — Lucene never extends a segment file once it has
        /// been closed and referenced by a commit. Lucene writes a new
        /// file for a new commit. This test documents the edge case for
        /// non-Lucene callers (and for potential future Lucene behaviors
        /// that might reuse file names) so that the contract is explicit.
        /// </summary>
        [Test, LuceneNetSpecific]
        public void TestSecondOpenAfterGrowth_ObservesCurrentLength()
        {
            var dirPath = CreateTempDir("testSecondOpenAfterGrowth");
            const string name = "data.bin";
            string filePath = Path.Combine(dirPath.FullName, name);

            File.WriteAllBytes(filePath, new byte[64]);

            using var mmapDir = new MMapDirectory(dirPath);

            // First open — mapping is created with length=64 and cached.
            using (var first = mmapDir.OpenInput(name, NewIOContext(Random)))
            {
                Assert.AreEqual(64L, first.Length);

                // Grow the file while the first IndexInput is still open.
                using (var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    fs.Write(new byte[64], 0, 64);
                }

                // Second open of the same file, with the first still live,
                // hits the cached SharedMapping. Under the current design
                // the cache entry's Length is still 64, so the second
                // IndexInput sees Length=64 rather than 128.
                //
                // This test asserts what we believe the CORRECT behavior
                // should be. If the current implementation returns 64, the
                // test fails and we have a real (if Lucene-irrelevant) gap
                // in the contract to discuss. If it returns 128, the cache
                // refreshes on length mismatch and no gap exists.
                using (var second = mmapDir.OpenInput(name, NewIOContext(Random)))
                {
                    Assert.AreEqual(128L, second.Length,
                        "A second OpenInput after the file grew must see the current file length, " +
                        "not the length cached from the first open. This is the ChatGPT-flagged " +
                        "concern from PR #1267 review r3137038502.");
                }
            }
        }

        /// <summary>
        /// After the last reference to a cached mapping is dropped, the
        /// cache entry is removed. A subsequent OpenInput then creates a
        /// fresh mapping that reflects the current file length. This test
        /// documents the "cache entry reaped, fresh mapping next time"
        /// path — the primary mechanism by which the Lucene.NET cache is
        /// correct under Lucene's actual write-once/read-many semantics.
        /// </summary>
        [Test, LuceneNetSpecific]
        public void TestReopenAfterGrowthWhenCacheDrained_ObservesCurrentLength()
        {
            var dirPath = CreateTempDir("testReopenAfterGrowth");
            const string name = "data.bin";
            string filePath = Path.Combine(dirPath.FullName, name);

            File.WriteAllBytes(filePath, new byte[64]);

            using var mmapDir = new MMapDirectory(dirPath);

            using (var first = mmapDir.OpenInput(name, NewIOContext(Random)))
            {
                Assert.AreEqual(64L, first.Length);
            } // Dispose drops the last ref, cache entry is reaped.

            // Grow the file after all references are gone.
            using (var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Write(new byte[64], 0, 64);
            }

            using (var second = mmapDir.OpenInput(name, NewIOContext(Random)))
            {
                Assert.AreEqual(128L, second.Length,
                    "After the cache entry was reaped, a new OpenInput must create a fresh " +
                    "mapping reflecting the current file length.");
            }
        }

        // LUCENENET specific: regression tests for the direct-pointer
        // MMapIndexInput. These exercise multi-byte reads that straddle
        // chunk boundaries, EOF behavior at exact file end, and seek/skip
        // interactions with the cached chunk pointer.

        private static byte[] WriteFile(MMapDirectory dir, string name, int length)
        {
            var bytes = new byte[length];
            // Deterministic, non-trivial pattern so off-by-one is detectable.
            for (int i = 0; i < length; i++)
            {
                bytes[i] = (byte)((i * 31 + 7) & 0xFF);
            }
            using (var io = dir.CreateOutput(name, NewIOContext(Random)))
            {
                io.WriteBytes(bytes, 0, bytes.Length);
            }
            return bytes;
        }

        [Test, LuceneNetSpecific]
        public void TestReadInt16AcrossChunkBoundary()
        {
            // chunkSize = 4 bytes; write 8 bytes; the int16 at position 3
            // starts in chunk 0 (offset 3) and ends in chunk 1 (offset 0).
            using var mmapDir = new MMapDirectory(CreateTempDir("readInt16AcrossChunk"), null, 1 << 2);
            var bytes = WriteFile(mmapDir, "f", 8);
            using var ii = mmapDir.OpenInput("f", NewIOContext(Random));
            for (int pos = 0; pos <= bytes.Length - 2; pos++)
            {
                ii.Seek(pos);
                short actual = ii.ReadInt16();
                short expected = (short)(((bytes[pos] & 0xFF) << 8) | (bytes[pos + 1] & 0xFF));
                Assert.AreEqual(expected, actual, "ReadInt16 mismatch at position " + pos);
            }
        }

        [Test, LuceneNetSpecific]
        public void TestReadInt32AcrossChunkBoundary()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("readInt32AcrossChunk"), null, 1 << 2);
            var bytes = WriteFile(mmapDir, "f", 16);
            using var ii = mmapDir.OpenInput("f", NewIOContext(Random));
            for (int pos = 0; pos <= bytes.Length - 4; pos++)
            {
                ii.Seek(pos);
                int actual = ii.ReadInt32();
                int expected = ((bytes[pos] & 0xFF) << 24) | ((bytes[pos + 1] & 0xFF) << 16)
                             | ((bytes[pos + 2] & 0xFF) << 8) | (bytes[pos + 3] & 0xFF);
                Assert.AreEqual(expected, actual, "ReadInt32 mismatch at position " + pos);
            }
        }

        [Test, LuceneNetSpecific]
        public void TestReadInt64AcrossChunkBoundary()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("readInt64AcrossChunk"), null, 1 << 2);
            var bytes = WriteFile(mmapDir, "f", 24);
            using var ii = mmapDir.OpenInput("f", NewIOContext(Random));
            for (int pos = 0; pos <= bytes.Length - 8; pos++)
            {
                ii.Seek(pos);
                long actual = ii.ReadInt64();
                long expected = 0;
                for (int b = 0; b < 8; b++)
                {
                    expected = (expected << 8) | (bytes[pos + b] & 0xFFL);
                }
                Assert.AreEqual(expected, actual, "ReadInt64 mismatch at position " + pos);
            }
        }

        [Test, LuceneNetSpecific]
        public void TestEofThrowsAtExactLength()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("eofExact"), null, 1 << 2);
            WriteFile(mmapDir, "f", 7);
            using var ii = mmapDir.OpenInput("f", NewIOContext(Random));

            ii.Seek(7);
            Assert.Throws<EndOfStreamException>(() => ii.ReadByte(), "ReadByte at Length must EOF");

            ii.Seek(6);
            Assert.Throws<EndOfStreamException>(() => ii.ReadInt16(), "ReadInt16 at Length-1 must EOF");

            ii.Seek(4);
            Assert.Throws<EndOfStreamException>(() => ii.ReadInt32(), "ReadInt32 at Length-3 must EOF");

            ii.Seek(0);
            Assert.Throws<EndOfStreamException>(() => ii.ReadInt64(), "ReadInt64 with Length<8 must EOF");
        }

        [Test, LuceneNetSpecific]
        public void TestBackwardSeekInvalidatesChunkCache()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("backSeek"), null, 1 << 2);
            var bytes = WriteFile(mmapDir, "f", 16);
            using var ii = mmapDir.OpenInput("f", NewIOContext(Random));
            // Read into chunk 3 to populate cache.
            ii.Seek(13);
            Assert.AreEqual(bytes[13], ii.ReadByte());
            // Backward seek to chunk 0 must invalidate the cache.
            ii.Seek(1);
            Assert.AreEqual(bytes[1], ii.ReadByte());
        }

        [Test, LuceneNetSpecific]
        public void TestSliceWithBaseOffsetAcrossChunkBoundary()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("sliceBaseOffset"), null, 1 << 2);
            var bytes = WriteFile(mmapDir, "f", 16);
            using var slicer = mmapDir.CreateSlicer("f", NewIOContext(Random));
            // Slice that starts mid-chunk and spans 3 chunk boundaries.
            using var slice = slicer.OpenSlice("s", 3, 11);
            Assert.AreEqual(11L, slice.Length);
            var actual = new byte[11];
            slice.ReadBytes(actual, 0, 11);
            for (int i = 0; i < 11; i++)
            {
                Assert.AreEqual(bytes[3 + i], actual[i], "slice byte mismatch at " + i);
            }
        }

        [Test, LuceneNetSpecific]
        public void TestSkipBytesAcrossChunks()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("skipBytes"), null, 1 << 2);
            var bytes = WriteFile(mmapDir, "f", 32);
            using var ii = mmapDir.OpenInput("f", NewIOContext(Random));
            ii.Seek(1);
            ii.SkipBytes(20);
            Assert.AreEqual(21L, ii.Position);
            Assert.AreEqual(bytes[21], ii.ReadByte());
            // Skip 0 is a no-op.
            long before = ii.Position;
            ii.SkipBytes(0);
            Assert.AreEqual(before, ii.Position);
        }

        [Test, LuceneNetSpecific]
        public void TestCloneIndependentChunkRent()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("cloneIndep"), null, 1 << 2);
            var bytes = WriteFile(mmapDir, "f", 32);
            using var parent = mmapDir.OpenInput("f", NewIOContext(Random));
            // Parent reads chunk 0.
            parent.Seek(2);
            Assert.AreEqual(bytes[2], parent.ReadByte());
            // Clone: independent position, independent chunk rent.
            var clone = (IndexInput)parent.Clone();
            try
            {
                clone.Seek(20);
                Assert.AreEqual(bytes[20], clone.ReadByte());
                // Parent position is unaffected by clone reads.
                Assert.AreEqual(3L, parent.Position);
                Assert.AreEqual(bytes[3], parent.ReadByte());
            }
            finally
            {
                clone.Dispose();
            }
            // Parent still works after clone disposed.
            Assert.AreEqual(bytes[4], parent.ReadByte());
        }

        [Test, LuceneNetSpecific]
        public void TestEmptySliceAndEmptyReadBytes()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("emptySlice"), null, 1 << 2);
            WriteFile(mmapDir, "f", 8);
            using var slicer = mmapDir.CreateSlicer("f", NewIOContext(Random));
            using var slice = slicer.OpenSlice("s", 4, 0);
            Assert.AreEqual(0L, slice.Length);
            // Empty span read on empty slice is a no-op.
            slice.ReadBytes(System.Array.Empty<byte>(), 0, 0);
            // Any non-empty read must EOF.
            Assert.Throws<EndOfStreamException>(() => slice.ReadByte());
        }

        [Test, LuceneNetSpecific]
        public void TestPositionTracksReads()
        {
            // Verifies Position is accurate after each read primitive.
            using var mmapDir = new MMapDirectory(CreateTempDir("positionTracks"), null, 1 << 2);
            var bytes = WriteFile(mmapDir, "f", 32);
            using var ii = mmapDir.OpenInput("f", NewIOContext(Random));
            Assert.AreEqual(0L, ii.Position);
            ii.ReadByte();   Assert.AreEqual(1L, ii.Position);
            ii.ReadInt16();  Assert.AreEqual(3L, ii.Position);
            ii.ReadInt32();  Assert.AreEqual(7L, ii.Position);
            ii.ReadInt64();  Assert.AreEqual(15L, ii.Position);
            ii.SkipBytes(5); Assert.AreEqual(20L, ii.Position);
            ii.Seek(2);      Assert.AreEqual(2L, ii.Position);
            var buf = new byte[4];
            ii.ReadBytes(buf, 0, 4);
            Assert.AreEqual(6L, ii.Position);
            for (int i = 0; i < 4; i++) Assert.AreEqual(bytes[2 + i], buf[i]);
        }

        [Test, LuceneNetSpecific]
        public void TestReadVInt32AcrossChunkBoundary()
        {
            // VInt parsing inherits from DataInput and walks via ReadByte;
            // verify the variable-length encoding survives chunk crossings
            // without corruption.
            using var mmapDir = new MMapDirectory(CreateTempDir("readVInt32"), null, 1 << 2);
            // Write known VInt values that span chunk boundaries.
            int[] values = { 0, 1, 127, 128, 16383, 16384, int.MaxValue / 2, int.MaxValue };
            using (var io = mmapDir.CreateOutput("f", NewIOContext(Random)))
            {
                foreach (int v in values) io.WriteVInt32(v);
            }
            using var ii = mmapDir.OpenInput("f", NewIOContext(Random));
            foreach (int expected in values)
            {
                Assert.AreEqual(expected, ii.ReadVInt32());
            }
        }

        [Test, LuceneNetSpecific]
        public void TestReadVInt64AcrossChunkBoundary()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("readVInt64"), null, 1 << 2);
            long[] values = { 0L, 1L, 127L, 128L, 16383L, 16384L, long.MaxValue / 2, long.MaxValue };
            using (var io = mmapDir.CreateOutput("f", NewIOContext(Random)))
            {
                foreach (long v in values) io.WriteVInt64(v);
            }
            using var ii = mmapDir.OpenInput("f", NewIOContext(Random));
            foreach (long expected in values)
            {
                Assert.AreEqual(expected, ii.ReadVInt64());
            }
        }

        [Test, LuceneNetSpecific]
        public void TestReadInt32AcrossChunkBoundary_ColdCache()
        {
            // The "cold cache" variant: open, immediately seek to a chunk
            // seam, ReadInt32. The fast path is only taken if the cached
            // chunk is already populated, which it isn't on the first call,
            // so this exercises the slow path explicitly.
            using var mmapDir = new MMapDirectory(CreateTempDir("readInt32Cold"), null, 1 << 2);
            var bytes = WriteFile(mmapDir, "f", 16);
            for (int pos = 1; pos <= bytes.Length - 4; pos++)
            {
                using var ii = mmapDir.OpenInput("f", NewIOContext(Random));
                ii.Seek(pos);
                int actual = ii.ReadInt32();
                int expected = ((bytes[pos] & 0xFF) << 24) | ((bytes[pos + 1] & 0xFF) << 16)
                             | ((bytes[pos + 2] & 0xFF) << 8) | (bytes[pos + 3] & 0xFF);
                Assert.AreEqual(expected, actual, "cold-cache ReadInt32 mismatch at position " + pos);
            }
        }

        [Test, LuceneNetSpecific]
        public void TestDoubleDisposeIsIdempotent()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("doubleDispose"), null, 1 << 2);
            WriteFile(mmapDir, "f", 16);
            var ii = mmapDir.OpenInput("f", NewIOContext(Random));
            ii.ReadByte(); // populate chunk rent
            ii.Dispose();
            ii.Dispose(); // must not throw
            try
            {
                ii.ReadByte();
                Assert.Fail("expected AlreadyClosedException after Dispose");
            }
            catch (Exception e) when (e.IsAlreadyClosedException() || e is ObjectDisposedException)
            {
                // pass
            }
        }

        [Test, LuceneNetSpecific]
        public void TestCrossThreadCloneDisposeWhileReading()
        {
            // Regression: clone is being read on thread A; thread B disposes
            // it. Reader thread must observe close cleanly (AlreadyClosed)
            // without AVE. This is the targeted same-design analog of the
            // slicer-vs-slice nightly stress test, but explicit and quick.
            using var mmapDir = new MMapDirectory(CreateTempDir("xthreadClone"), null, 1 << 4);
            // 4 KB file, 16-byte chunks → 256 chunks, lots of crossings.
            WriteFile(mmapDir, "f", 4096);
            using var parent = mmapDir.OpenInput("f", NewIOContext(Random));

            for (int trial = 0; trial < 20; trial++)
            {
                var clone = (IndexInput)parent.Clone();
                int observedAve = 0;
                int observedAcceptable = 0;

                var reader = new System.Threading.Thread(() =>
                {
                    try
                    {
                        clone.Seek(0);
                        long len = clone.Length;
                        long sum = 0;
                        for (long i = 0; i < len; i++) sum += clone.ReadByte();
                        if (sum == long.MinValue) System.Console.WriteLine("never");
                    }
                    catch (Exception e) when (e.IsAlreadyClosedException() || e is ObjectDisposedException || e is EndOfStreamException)
                    {
                        System.Threading.Interlocked.Increment(ref observedAcceptable);
                    }
                    catch (System.AccessViolationException)
                    {
                        System.Threading.Interlocked.Increment(ref observedAve);
                    }
                });
                reader.Start();
                System.Threading.Thread.Sleep(0); // let reader start
                clone.Dispose();
                reader.Join();
                Assert.AreEqual(0, observedAve, "AVE observed on trial " + trial);
            }
        }

        [Test, LuceneNetSpecific]
        public void TestSlicedReadInt32AcrossOffsets()
        {
            // Item 6: exhaustive sweep of slice (offset, length) with
            // ReadInt32 — exercises the fast-path multi-byte read code with
            // a non-zero baseOffset that may straddle a chunk boundary at
            // any offset, including offsets that don't align with any chunk.
            for (int chunkPower = 0; chunkPower < 5; chunkPower++)
            {
                using var mmapDir = new MMapDirectory(CreateTempDir("slicedReadInt32"), null, 1 << chunkPower);
                int fileLen = 1 << (chunkPower + 2);
                var bytes = WriteFile(mmapDir, "f", fileLen);
                using var slicer = mmapDir.CreateSlicer("f", NewIOContext(Random));
                for (int sliceStart = 0; sliceStart <= fileLen - 4; sliceStart++)
                {
                    int maxSliceLen = fileLen - sliceStart;
                    for (int sliceLen = 4; sliceLen <= maxSliceLen; sliceLen++)
                    {
                        using var slice = slicer.OpenSlice("s", sliceStart, sliceLen);
                        for (int innerPos = 0; innerPos <= sliceLen - 4; innerPos++)
                        {
                            slice.Seek(innerPos);
                            int actual = slice.ReadInt32();
                            int abs = sliceStart + innerPos;
                            int expected = ((bytes[abs] & 0xFF) << 24) | ((bytes[abs + 1] & 0xFF) << 16)
                                         | ((bytes[abs + 2] & 0xFF) << 8) | (bytes[abs + 3] & 0xFF);
                            Assert.AreEqual(expected, actual,
                                "chunkPower=" + chunkPower + " sliceStart=" + sliceStart +
                                " sliceLen=" + sliceLen + " innerPos=" + innerPos);
                        }
                    }
                }
            }
        }

        [Test, LuceneNetSpecific]
        public void TestSlicedReadInt64AcrossOffsets()
        {
            for (int chunkPower = 0; chunkPower < 4; chunkPower++)
            {
                using var mmapDir = new MMapDirectory(CreateTempDir("slicedReadInt64"), null, 1 << chunkPower);
                int fileLen = 1 << (chunkPower + 3);
                var bytes = WriteFile(mmapDir, "f", fileLen);
                using var slicer = mmapDir.CreateSlicer("f", NewIOContext(Random));
                for (int sliceStart = 0; sliceStart <= fileLen - 8; sliceStart++)
                {
                    int maxSliceLen = fileLen - sliceStart;
                    for (int sliceLen = 8; sliceLen <= maxSliceLen; sliceLen++)
                    {
                        using var slice = slicer.OpenSlice("s", sliceStart, sliceLen);
                        for (int innerPos = 0; innerPos <= sliceLen - 8; innerPos++)
                        {
                            slice.Seek(innerPos);
                            long actual = slice.ReadInt64();
                            int abs = sliceStart + innerPos;
                            long expected = 0L;
                            for (int b = 0; b < 8; b++)
                                expected = (expected << 8) | (bytes[abs + b] & 0xFFL);
                            Assert.AreEqual(expected, actual,
                                "chunkPower=" + chunkPower + " sliceStart=" + sliceStart +
                                " sliceLen=" + sliceLen + " innerPos=" + innerPos);
                        }
                    }
                }
            }
        }

        [Test, LuceneNetSpecific]
        public void TestPostXThreadDisposeReadPathsThrow()
        {
            // Item 8: after Dispose runs from a different thread (reader is
            // idle, not mid-deref), every read entry point must observe the
            // closed state and throw cleanly. Covers ReadByte (fast path
            // and slow path), ReadInt16/32/64 (fast and slow), ReadBytes,
            // SkipBytes, and Seek.
            using var mmapDir = new MMapDirectory(CreateTempDir("postXDispose"), null, 1 << 2);
            WriteFile(mmapDir, "f", 32);

            void DisposeFromOtherThread(IndexInput target)
            {
                var t = new System.Threading.Thread(() => target.Dispose());
                t.Start();
                t.Join();
            }

            // Each scenario opens a fresh input, optionally primes the
            // chunk-rent cache, disposes from another thread, and verifies
            // the named operation throws AlreadyClosed on the original
            // (reader) thread.
            void Scenario(string name, bool primeCache, Action<IndexInput> op)
            {
                var ii = mmapDir.OpenInput("f", NewIOContext(Random));
                if (primeCache) ii.ReadByte(); // populate currentChunk on the reader thread
                DisposeFromOtherThread(ii);
                try
                {
                    op(ii);
                    Assert.Fail(name + " did not throw after cross-thread Dispose");
                }
                catch (Exception e) when (e.IsAlreadyClosedException() || e is ObjectDisposedException)
                {
                    // pass
                }
                catch (Exception e)
                {
                    Assert.Fail(name + " threw unexpected " + e.GetType().Name + ": " + e.Message);
                }
            }

            // Cold cache (no rent held when Dispose ran).
            Scenario("ReadByte cold", false, ii => ii.ReadByte());
            Scenario("ReadInt16 cold", false, ii => ii.ReadInt16());
            Scenario("ReadInt32 cold", false, ii => ii.ReadInt32());
            Scenario("ReadInt64 cold", false, ii => ii.ReadInt64());
            Scenario("ReadBytes cold", false, ii => { var b = new byte[4]; ii.ReadBytes(b, 0, 4); });
            Scenario("Seek cold", false, ii => ii.Seek(8));
            Scenario("SkipBytes cold", false, ii => ii.SkipBytes(4));

            // Warm cache (reader had a rent at Dispose time; the reader
            // observes instanceClosed on its next op and must release the
            // rent on its own thread before throwing).
            Scenario("ReadByte warm", true, ii => { for (int i = 0; i < 100; i++) ii.ReadByte(); });
            Scenario("ReadInt16 warm", true, ii => ii.ReadInt16());
            Scenario("ReadInt32 warm", true, ii => ii.ReadInt32());
            Scenario("ReadInt64 warm", true, ii => ii.ReadInt64());
            Scenario("ReadBytes warm", true, ii => { var b = new byte[16]; ii.ReadBytes(b, 0, 16); });
            Scenario("Seek warm", true, ii => ii.Seek(8));
            Scenario("SkipBytes warm", true, ii => ii.SkipBytes(4));
        }

        [Test, LuceneNetSpecific]
        public void TestOpenSlice_OutOfBounds_Throws()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("sliceBounds"), null, 1 << 2);
            WriteFile(mmapDir, "f", 16);
            using var slicer = mmapDir.CreateSlicer("f", NewIOContext(Random));

            // Negative offset.
            Assert.Throws<ArgumentException>(() => slicer.OpenSlice("neg-off", -1, 4));
            // Negative length.
            Assert.Throws<ArgumentException>(() => slicer.OpenSlice("neg-len", 0, -1));
            // offset + length past end of file.
            Assert.Throws<ArgumentException>(() => slicer.OpenSlice("past-end", 8, 10));
            // offset alone past end of file.
            Assert.Throws<ArgumentException>(() => slicer.OpenSlice("off-past-end", 17, 0));

            // Edge: offset == length == 0 is fine on a non-empty file.
            using (var s = slicer.OpenSlice("empty", 0, 0)) { Assert.AreEqual(0L, s.Length); }
            // Edge: offset == fileLength, length == 0 is fine.
            using (var s = slicer.OpenSlice("end-empty", 16, 0)) { Assert.AreEqual(0L, s.Length); }
            // Edge: full file.
            using (var s = slicer.OpenSlice("full", 0, 16)) { Assert.AreEqual(16L, s.Length); }
        }

        [Test, LuceneNetSpecific]
        public void TestCloneAfterRootDispose_ThrowsAlreadyClosed()
        {
            using var mmapDir = new MMapDirectory(CreateTempDir("cloneAfterRootDispose"), null, 1 << 2);
            WriteFile(mmapDir, "f", 32);
            var root = mmapDir.OpenInput("f", NewIOContext(Random));
            root.Dispose();
            try
            {
                root.Clone();
                Assert.Fail("Clone of disposed root should throw AlreadyClosedException");
            }
            catch (Exception e) when (e.IsAlreadyClosedException() || e is ObjectDisposedException)
            {
                // pass
            }
        }

        [Test, LuceneNetSpecific]
        public void TestSliceNonZeroOffset_SeekToZero_ReadsSliceStart()
        {
            // Slice offset lands mid-chunk. After reading some bytes,
            // Seek(0) must reposition to the slice's first byte (which is
            // mid-chunk in the underlying file), not to file byte 0.
            using var mmapDir = new MMapDirectory(CreateTempDir("sliceSeekZero"), null, 1 << 2);
            var bytes = WriteFile(mmapDir, "f", 32);
            using var slicer = mmapDir.CreateSlicer("f", NewIOContext(Random));
            // offset=5 lands inside chunk 1 (chunk size = 4); slice spans
            // multiple chunks.
            using var slice = slicer.OpenSlice("s", 5, 20);

            // Drain a few bytes so currentChunk/readBase are populated.
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(bytes[5 + i], slice.ReadByte(), "pre-seek mismatch at " + i);
            }

            // Seek to slice-relative 0 -> file byte 5.
            slice.Seek(0);
            Assert.AreEqual(0L, slice.Position);
            Assert.AreEqual(bytes[5], slice.ReadByte(), "slice[0] after Seek(0) should equal file[offset]");

            // Read across the slice (covers multiple chunk crossings).
            slice.Seek(0);
            var actual = new byte[20];
            slice.ReadBytes(actual, 0, 20);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(bytes[5 + i], actual[i], "slice byte mismatch at " + i);
            }
        }
    }
}
