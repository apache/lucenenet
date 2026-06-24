using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;

#nullable enable

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
    /// LUCENENET specific: white-box tests for <see cref="UnsafeChunkIndexInput"/>,
    /// the native read engine shared by <see cref="MMapDirectory"/>. These exercise
    /// the engine's logic against a managed-<see cref="byte"/>[]-backed chunk source
    /// (<see cref="ManagedChunkRegion"/>) instead of real memory-mapped views, which
    /// lets us:
    /// <list type="bullet">
    ///   <item><description>assert read correctness across chunk boundaries with
    ///   exact expected bytes (a bug is a wrong value, not a process crash);</description></item>
    ///   <item><description>force the fail-fast path (close a chunk, assert the next
    ///   read throws <see cref="AlreadyClosedException"/>) deterministically;</description></item>
    ///   <item><description>force the cross-thread "stranded read reference" path with
    ///   a barrier that pauses a reader exactly between its bounds check and its
    ///   pointer dereference, and assert the disposer does NOT release the reader's
    ///   reference (which would be an AccessViolation against real memory).</description></item>
    /// </list>
    /// A managed array is never freed under a live reader, so these tests cannot
    /// reproduce a true unmap-vs-read AccessViolationException - that native-safety
    /// property is covered by the real-mmap stress tests in
    /// <see cref="TestMultiMMap"/>. Here we test the engine's <i>logic</i>.
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestUnsafeChunkIndexInput : LuceneTestCase
    {
        // ------------------------------------------------------------------
        // Managed-byte[]-backed chunk source + input under test
        // ------------------------------------------------------------------

        /// <summary>
        /// A managed chunk source: the bytes of a logical region split into
        /// fixed-size chunks, each backed by a pinned <see cref="byte"/>[]. Shared
        /// by a root <see cref="ManagedChunkIndexInput"/> and its clones, mirroring
        /// how a real <c>SharedMapping</c> is shared. Tracks per-chunk acquire/
        /// release counts and a closed flag so tests can assert the engine's
        /// reference and fail-fast behavior, and exposes an optional barrier so a
        /// test can pause a reader inside <c>TryAcquire</c>.
        /// </summary>
        internal sealed unsafe class ManagedChunkRegion : IDisposable
        {
            internal sealed unsafe class FakeChunk
            {
                private readonly byte[] data;
                private GCHandle handle;
                internal readonly byte* BasePtr;
                internal readonly long Length;

                // Net outstanding references (acquires - releases). A leaked
                // reference shows up as a nonzero value after teardown.
                internal int OutstandingRefs;
                // Total acquires/releases ever, for assertions about call counts.
                internal int TotalAcquires;
                internal int TotalReleases;
                private int closed;

                // Optional test hook: if set, TryAcquire blocks on this until
                // signaled, AFTER recording the acquire intent - lets a test hold a
                // reader "mid-acquire" while another thread disposes.
                internal ManualResetEventSlim? PauseInAcquire;

                internal FakeChunk(byte[] data)
                {
                    this.data = data;
                    this.handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                    this.BasePtr = (byte*)handle.AddrOfPinnedObject();
                    this.Length = data.Length;
                }

                internal bool IsClosed => Volatile.Read(ref closed) != 0;

                internal bool TryAcquire(out byte* basePtr)
                {
                    if (Volatile.Read(ref closed) != 0)
                    {
                        basePtr = null;
                        return false;
                    }
                    Interlocked.Increment(ref TotalAcquires);
                    Interlocked.Increment(ref OutstandingRefs);
                    PauseInAcquire?.Wait();
                    basePtr = BasePtr;
                    return true;
                }

                internal void Release()
                {
                    Interlocked.Increment(ref TotalReleases);
                    Interlocked.Decrement(ref OutstandingRefs);
                }

                internal void Close()
                {
                    Volatile.Write(ref closed, 1);
                }

                internal void FreePin()
                {
                    if (handle.IsAllocated) handle.Free();
                }
            }

            internal readonly FakeChunk[] Chunks;
            internal readonly long Length;
            private int disposed;

            internal ManagedChunkRegion(byte[] data, int chunkSizePower)
            {
                Length = data.Length;
                long chunkSize = 1L << chunkSizePower;
                int nChunks = data.Length == 0 ? 0 : (int)((data.Length + chunkSize - 1) >> chunkSizePower);
                Chunks = new FakeChunk[nChunks];
                for (int i = 0; i < nChunks; i++)
                {
                    long off = (long)i << chunkSizePower;
                    int len = (int)Math.Min(chunkSize, data.Length - off);
                    var slice = new byte[len];
                    Array.Copy(data, off, slice, 0, len);
                    Chunks[i] = new FakeChunk(slice);
                }
            }

            internal bool IsDisposed => Volatile.Read(ref disposed) != 0;

            // Mirrors SharedMapping.Dispose: closes every chunk. Real teardown of
            // the pin happens in FreeAllPins (test-controlled), so an AVE is never
            // possible here - managed memory stays valid.
            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;
                foreach (var c in Chunks) c.Close();
            }

            internal void FreeAllPins()
            {
                foreach (var c in Chunks) c.FreePin();
            }
        }

        /// <summary>
        /// Test <see cref="UnsafeChunkIndexInput"/> subclass backed by a
        /// <see cref="ManagedChunkRegion"/>. Mirrors <c>MMapDirectory.MMapIndexInput</c>:
        /// a root owns the region (disposing it closes the chunks); clones share it.
        /// </summary>
        internal sealed unsafe class ManagedChunkIndexInput : UnsafeChunkIndexInput
        {
            private bool isRoot;
            private readonly ManagedChunkRegion region;

            internal ManagedChunkRegion Region => region;

            internal ManagedChunkIndexInput(string desc, bool ownsRegion, ManagedChunkRegion region,
                long offset, long length, int chunkSizePower)
                : base(desc, offset, length, chunkSizePower)
            {
                this.isRoot = ownsRegion;
                this.region = region;
            }

            protected override int ChunkCount => region.Chunks.Length;

            protected override bool TryAcquireChunk(int index, out byte* chunkBase)
                => region.Chunks[index].TryAcquire(out chunkBase);

            protected override void ReleaseChunk(int index)
                => region.Chunks[index].Release();

            protected override long ChunkLength(int index) => region.Chunks[index].Length;

            public override object Clone()
            {
                ThrowIfDisposed();
                var clone = (ManagedChunkIndexInput)base.Clone();
                clone.isRoot = false;
                clone.ResetClonedCursor();
                return clone;
            }

            protected override void DisposeChunkSource(bool disposing)
            {
                if (isRoot) region.Dispose();
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static byte[] MakeData(int len)
        {
            var data = new byte[len];
            for (int i = 0; i < len; i++) data[i] = (byte)(i * 31 + 7);
            return data;
        }

        private static ManagedChunkIndexInput OpenRoot(byte[] data, int chunkSizePower, out ManagedChunkRegion region)
        {
            region = new ManagedChunkRegion(data, chunkSizePower);
            return new ManagedChunkIndexInput("root", ownsRegion: true, region, 0, data.Length, chunkSizePower);
        }

        // ------------------------------------------------------------------
        // Read correctness across chunk boundaries
        // ------------------------------------------------------------------

        [Test]
        public void TestReadByteAcrossChunks()
        {
            // 8-byte chunks over a 40-byte region: 5 chunks, every read crosses.
            var data = MakeData(40);
            using var input = OpenRoot(data, chunkSizePower: 3, out var region);
            for (int i = 0; i < data.Length; i++)
            {
                Assert.AreEqual(data[i], input.ReadByte(), "byte at " + i);
            }
            region.FreeAllPins();
        }

        [Test]
        public void TestReadInt32StraddlingChunkBoundary()
        {
            // 4-byte chunks: every ReadInt32 except the aligned ones straddles a
            // boundary, exercising the stackalloc + ReadBytes slow path.
            var data = MakeData(64);
            using var input = OpenRoot(data, chunkSizePower: 2, out var region);
            for (int start = 0; start + 4 <= data.Length; start++)
            {
                input.Seek(start);
                int expected = (data[start] << 24) | (data[start + 1] << 16) | (data[start + 2] << 8) | data[start + 3];
                Assert.AreEqual(expected, input.ReadInt32(), "Int32 at offset " + start);
            }
            region.FreeAllPins();
        }

        [Test]
        public void TestReadBytesSpanningMultipleChunks()
        {
            var data = MakeData(100);
            using var input = OpenRoot(data, chunkSizePower: 4, out var region); // 16-byte chunks
            // Read the whole thing in one ReadBytes call (spans 7 chunks).
            var buf = new byte[data.Length];
            input.ReadBytes(buf, 0, buf.Length);
            Assert.AreEqual(data, buf);
            // And a sub-range that starts and ends mid-chunk.
            input.Seek(5);
            var buf2 = new byte[50];
            input.ReadBytes(buf2, 0, buf2.Length);
            for (int i = 0; i < 50; i++) Assert.AreEqual(data[5 + i], buf2[i], "byte " + i);
            region.FreeAllPins();
        }

        [Test]
        public void TestSeekKeepsReferenceWithinChunk()
        {
            var data = MakeData(64);
            using var input = OpenRoot(data, chunkSizePower: 4, out var region); // 16-byte chunks
            input.Seek(0);
            input.ReadByte(); // acquire chunk 0
            int acquiresAfterFirst = region.Chunks[0].TotalAcquires;
            // Seek within the same chunk - must NOT re-acquire.
            input.Seek(5);
            Assert.AreEqual(data[5], input.ReadByte());
            Assert.AreEqual(acquiresAfterFirst, region.Chunks[0].TotalAcquires,
                "seek within the cached chunk must not re-acquire a read reference");
            // Seek into chunk 1 - must release chunk 0 and acquire chunk 1.
            input.Seek(20);
            Assert.AreEqual(data[20], input.ReadByte());
            Assert.AreEqual(1, region.Chunks[1].TotalAcquires, "crossing to chunk 1 acquires it once");
            Assert.AreEqual(0, region.Chunks[0].OutstandingRefs, "chunk 0 reference released on crossing");
            region.FreeAllPins();
        }

        // ------------------------------------------------------------------
        // Reference lifecycle / leak (NightOwl888 #1267 concern, logic level)
        // ------------------------------------------------------------------

        [Test]
        public void TestDisposeReleasesReadReferenceDeterministically()
        {
            var data = MakeData(64);
            var input = OpenRoot(data, chunkSizePower: 4, out var region);
            input.Seek(0);
            input.ReadByte(); // acquire chunk 0
            Assert.AreEqual(1, region.Chunks[0].OutstandingRefs);

            input.Dispose(); // same-thread dispose: releases immediately, no GC
            Assert.AreEqual(0, region.Chunks[0].OutstandingRefs,
                "same-thread Dispose must release the read reference deterministically");
            Assert.IsTrue(region.IsDisposed, "disposing the root disposes the region");
            region.FreeAllPins();
        }

        [Test]
        public void TestCloneDisposeDoesNotReleaseParentReference()
        {
            var data = MakeData(64);
            using var root = OpenRoot(data, chunkSizePower: 4, out var region);
            root.Seek(0);
            root.ReadByte(); // root acquires chunk 0
            var clone = (IndexInput)root.Clone();
            clone.Seek(0);
            clone.ReadByte(); // clone acquires its OWN reference on chunk 0
            Assert.AreEqual(2, region.Chunks[0].OutstandingRefs, "root + clone each hold a reference");

            clone.Dispose();
            Assert.AreEqual(1, region.Chunks[0].OutstandingRefs,
                "disposing the clone releases only the clone's reference, not the root's");
            // Root still works.
            root.Seek(10);
            Assert.AreEqual(data[10], root.ReadByte());
            region.FreeAllPins();
        }

        // ------------------------------------------------------------------
        // Fail-fast: read after close throws AlreadyClosed (no crash)
        // ------------------------------------------------------------------

        [Test]
        public void TestCrossingIntoClosedChunkThrowsAlreadyClosed()
        {
            var data = MakeData(64);
            using var root = OpenRoot(data, chunkSizePower: 4, out var region); // 16-byte chunks
            var clone = (IndexInput)root.Clone();
            // Read chunk 0 on the clone, then dispose the ROOT (closes all chunks).
            clone.Seek(0);
            clone.ReadByte();
            root.Dispose(); // closes the shared region's chunks

            // The clone is mid-chunk-0; its cached reference is still valid for
            // chunk 0 reads it already entered... but crossing into chunk 1 must
            // observe the closed chunk and throw AlreadyClosed (NOT read freed mem,
            // NOT crash). Seek to chunk 1 to force a crossing.
            clone.Seek(20);
            try
            {
                clone.ReadByte();
                Assert.Fail("crossing into a closed chunk must throw AlreadyClosed");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }
            region.FreeAllPins();
        }

        [Test]
        public void TestReadAfterOwnDisposeThrowsAlreadyClosed()
        {
            var data = MakeData(32);
            var input = OpenRoot(data, chunkSizePower: 3, out var region);
            input.Seek(0);
            input.ReadByte();
            input.Dispose();
            try
            {
                input.ReadByte();
                Assert.Fail("reading a disposed input must throw AlreadyClosed");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }
            region.FreeAllPins();
        }

        // ------------------------------------------------------------------
        // Forced cross-thread strand: the deferral path, deterministically
        // ------------------------------------------------------------------

        [Test]
        public void TestCrossThreadDisposeDoesNotReleaseStrandedReference()
        {
            // Force the exact interleaving the StrandedReadRefReleaser exists for:
            // a reader (thread A) is INSIDE TryAcquireChunk for chunk 0 (paused on
            // a barrier), holding its acquire, while thread B disposes the same
            // clone. Because B is a different thread than the acquirer, the engine
            // must NOT release A's reference directly (which against real memory
            // could AVE a reader mid-dereference) - it hands it to the finalizable
            // releaser instead. We assert deterministically that the reference is
            // NOT released by B's Dispose.
            //
            // The complementary property - that the deferred reference is
            // eventually released once the input is unreachable - is GC/finalizer
            // timing-dependent, so it is NOT asserted here (it would be flaky);
            // it is covered against real mappings by
            // TestMultiMMap.TestCrossThreadCloneDisposeReleasesReadRefDeterministically
            // (which observes deterministic release via the owner's Dispose) and
            // the nightly concurrent race tests.
            var data = MakeData(64);
            using var root = OpenRoot(data, chunkSizePower: 4, out var region);

            var pause = new ManualResetEventSlim(false);
            region.Chunks[0].PauseInAcquire = pause;

            var clone = (IndexInput)root.Clone();
            var reader = new Thread(static state =>
            {
                var c = (IndexInput)state!;
                c.Seek(0);
                c.ReadByte(); // blocks inside TryAcquire on the chunk's PauseInAcquire
            }) { IsBackground = true };
            reader.Start(clone);

            // Spin until the acquire is recorded (reader is parked on the barrier
            // holding its reference).
            for (int i = 0; i < 2000 && Volatile.Read(ref region.Chunks[0].OutstandingRefs) == 0; i++)
                Thread.Sleep(1);
            Assert.AreEqual(1, region.Chunks[0].OutstandingRefs,
                "reader should be parked inside TryAcquire holding its reference");

            // Dispose the clone from THIS (different) thread while the reader holds
            // the reference: the engine must defer, not release.
            clone.Dispose();
            Assert.AreEqual(1, region.Chunks[0].OutstandingRefs,
                "a cross-thread Dispose must NOT release the reader's reference; " +
                "it must hand it to the finalizable releaser (releasing it here " +
                "could unmap a view under a mid-dereference reader against real memory)");

            // Let the reader finish (its ReadByte completes against still-valid
            // managed memory) and join before the test tears down.
            pause.Set();
            Assert.IsTrue(reader.Join(TimeSpan.FromSeconds(5)), "reader thread should finish");

            region.Chunks[0].PauseInAcquire = null;
            region.FreeAllPins();
        }
    }
}
