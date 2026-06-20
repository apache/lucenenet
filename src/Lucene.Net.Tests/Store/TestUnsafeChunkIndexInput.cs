using Lucene.Net.Attributes;
using Lucene.Net.Support;
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
    ///   <item><description>force the fail-fast path (close the region, assert the next
    ///   read throws <see cref="AlreadyClosedException"/>) deterministically;</description></item>
    ///   <item><description>force the cross-thread close path with a reclaimer that
    ///   parks a reader exactly inside the <c>Enter</c>/<c>Exit</c> bracket, and
    ///   assert the close defers the unmap until that reader drains (running it now
    ///   would be an AccessViolation against real memory).</description></item>
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
        /// how a real <c>SharedMapping</c> is shared, including owning an
        /// <see cref="DrainReclaimer"/> that defers the chunk close until in-flight
        /// readers drain.
        /// </summary>
        internal sealed unsafe class ManagedChunkRegion : IDisposable
        {
            internal sealed unsafe class FakeChunk
            {
                private readonly byte[] data;
                private GCHandle handle;
                internal readonly byte* BasePtr;
                internal readonly long Length;

                private int closed;

                internal FakeChunk(byte[] data)
                {
                    this.data = data;
                    this.handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                    this.BasePtr = (byte*)handle.AddrOfPinnedObject();
                    this.Length = data.Length;
                }

                internal bool IsClosed => Volatile.Read(ref closed) != 0;

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
            private readonly DrainReclaimer reclaimer = new DrainReclaimer();
            private int disposed;

            internal DrainReclaimer Reclaimer => reclaimer;

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

            // Mirrors SharedMapping.Dispose: closes every chunk through the
            // reclaimer, which defers the close until in-flight readers drain. Real
            // teardown of the pin happens in FreeAllPins (test-controlled), so an
            // AVE is never possible here - managed memory stays valid.
            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;
                reclaimer.Close(() =>
                {
                    foreach (var c in Chunks) c.Close();
                });
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
                : base(region.Reclaimer, desc, offset, length, chunkSizePower)
            {
                this.isRoot = ownsRegion;
                this.region = region;
            }

            protected override int ChunkCount => region.Chunks.Length;

            protected override byte* ChunkBase(int index) => region.Chunks[index].BasePtr;

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
        public void TestSeekWithinChunkReadsCorrectlyWithoutRecrossing()
        {
            var data = MakeData(64);
            using var input = OpenRoot(data, chunkSizePower: 4, out var region); // 16-byte chunks
            input.Seek(0);
            input.ReadByte();
            // Seek within the same chunk, then across a boundary; both must read the
            // expected bytes. (The cached base pointer is reused within a chunk and
            // recomputed on a crossing; correctness is the observable invariant now
            // that there is no per-crossing native reference to count.)
            input.Seek(5);
            Assert.AreEqual(data[5], input.ReadByte());
            input.Seek(20);
            Assert.AreEqual(data[20], input.ReadByte());
            region.FreeAllPins();
        }

        // ------------------------------------------------------------------
        // Dispose / region lifecycle
        // ------------------------------------------------------------------

        [Test]
        public void TestDisposeClosesRegionDeterministically()
        {
            var data = MakeData(64);
            var input = OpenRoot(data, chunkSizePower: 4, out var region);
            input.Seek(0);
            input.ReadByte();

            input.Dispose(); // same-thread dispose: no in-flight reader, so the
                             // reclaimer reclaims inline and closes the region now.
            Assert.IsTrue(region.IsDisposed, "disposing the root disposes the region");
            Assert.IsTrue(region.Chunks[0].IsClosed,
                "the reclaimer must run the chunk close inline when no reader is active");
            region.FreeAllPins();
        }

        [Test]
        public void TestCloneDisposeLeavesRegionOpenForRoot()
        {
            var data = MakeData(64);
            using var root = OpenRoot(data, chunkSizePower: 4, out var region);
            root.Seek(0);
            root.ReadByte();
            var clone = (IndexInput)root.Clone();
            clone.Seek(0);
            clone.ReadByte();

            clone.Dispose(); // a non-owning clone must NOT close the shared region.
            Assert.IsFalse(region.IsDisposed,
                "disposing a clone must not dispose the shared region");
            Assert.IsFalse(region.Chunks[0].IsClosed,
                "disposing a clone must not close a chunk the root still reads");
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
        // Forced cross-thread close: the reclaimer defers the unmap, deterministically
        // ------------------------------------------------------------------

        [Test]
        public void TestCrossThreadCloseDefersUnmapUntilReaderDrains()
        {
            // Force the exact interleaving #1013 is about: a reader (thread A) is
            // INSIDE the reclaimer's Enter/Exit bracket (admitted but parked just
            // before its dereference) on a clone, while thread B closes the shared
            // region by disposing the owning root. The reclaimer MUST defer the
            // actual chunk close (the unmap, against real memory) until A drains -
            // running it now would free a view under a mid-dereference reader and
            // AVE. We assert deterministically that the chunk is NOT closed while A
            // is parked, and IS closed once A exits the bracket.
            //
            // True native unmap-vs-read AVE-safety under load is covered against
            // real mappings by the nightly stress tests in TestMultiMMap; here we
            // pin down the deferral handshake at the engine's logic level.
            var data = MakeData(64);
            using var root = OpenRoot(data, chunkSizePower: 4, out var region);
            var clone = (ManagedChunkIndexInput)root.Clone();

            var entered = new ManualResetEventSlim(false);
            var resume = new ManualResetEventSlim(false);
            // Park the clone's reader INSIDE its Enter/Exit bracket (admitted, before
            // the dereference returns) so a concurrent Close must wait for it.
            clone.SetOnEnterForTest(() =>
            {
                entered.Set();
                resume.Wait();
            });

            var reader = new Thread(state =>
            {
                var c = (IndexInput)state!;
                c.Seek(0);
                c.ReadByte(); // admitted by Enter, then parks inside the bracket
            }) { IsBackground = true };
            reader.Start(clone);

            // Wait until the reader is parked inside the bracket.
            Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)),
                "reader should reach the inside of the Enter/Exit bracket");

            // Close the region from THIS (different) thread while the reader is
            // mid-bracket: the reclaimer must NOT run the unmap yet.
            var disposer = new Thread(state => ((IDisposable)state!).Dispose())
            { IsBackground = true };
            disposer.Start(root);

            // Give the close a chance to (wrongly) reclaim; it must still be parked
            // because a reader is active. The chunk close is the observable unmap.
            Thread.Sleep(100);
            Assert.IsFalse(region.Chunks[0].IsClosed,
                "a cross-thread close must NOT unmap a chunk while a reader is " +
                "inside the bracket (against real memory this would AVE)");
            Assert.IsTrue(region.Reclaimer.IsClosed, "the region was closed (Close was called)");

            // Release the reader: its Exit drains the last reference and runs the
            // deferred unmap, so the chunk closes now.
            resume.Set();
            Assert.IsTrue(reader.Join(TimeSpan.FromSeconds(5)), "reader thread should finish");
            Assert.IsTrue(disposer.Join(TimeSpan.FromSeconds(5)), "disposer thread should finish");

            // After the reader drains, the deferred unmap has run.
            for (int i = 0; i < 2000 && !region.Chunks[0].IsClosed; i++) Thread.Sleep(1);
            Assert.IsTrue(region.Chunks[0].IsClosed,
                "once the reader exits the bracket, the deferred unmap must run");

            region.FreeAllPins();
        }
    }
}
