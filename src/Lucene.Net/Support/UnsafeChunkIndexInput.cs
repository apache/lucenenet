using Lucene.Net.Diagnostics;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

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
    /// LUCENENET-specific base class that reads from a fixed-size <i>chunked</i>
    /// region of unmanaged memory via cached raw pointers, with one bounds check
    /// per read and no managed-buffer indirection. It encapsulates the native
    /// read engine that upstream Java keeps in <c>ByteBufferIndexInput</c> (which
    /// has no direct .NET equivalent), so that a concrete chunk source - e.g.
    /// <see cref="MMapDirectory"/>'s memory-mapped views - only has to supply the
    /// chunks; the cursor, the fast read paths, the chunk-crossing logic, and the
    /// concurrency/teardown coordination all live here and can be reviewed and
    /// unit-tested independently of any particular chunk source.
    ///
    /// <para/>
    /// There is no upstream counterpart to this class; it lives under
    /// <c>Support/</c> per the Lucene.NET convention for types with no Java
    /// equivalent.
    ///
    /// <para/>
    /// <b>Chunk model.</b> The readable region is partitioned into chunks of
    /// <c>1 &lt;&lt; chunkSizePower</c> bytes (the last chunk may be shorter).
    /// A subclass exposes <see cref="ChunkCount"/> chunks; for each chunk it can
    /// acquire a per-crossing <i>read reference</i> that yields a base pointer
    /// (<see cref="TryAcquireChunk"/>), release that reference
    /// (<see cref="ReleaseChunk"/>), and report the chunk's length
    /// (<see cref="ChunkLength"/>). This base never sees the underlying chunk
    /// implementation (a <c>MemoryMappedViewAccessor</c>, a pinned managed array
    /// in tests, etc.) - only those three operations.
    ///
    /// <para/>
    /// <b>Window.</b> An instance views <c>[baseOffset, baseOffset + length)</c>
    /// of the chunked region; this supports slices over a larger backing region.
    /// All positions exposed to callers are window-relative; chunk lookup
    /// translates through <c>baseOffset</c>.
    ///
    /// <para/>
    /// <b>Concurrency / teardown.</b> A read reference is held for as long as the
    /// instance caches a chunk's pointer (from one chunk crossing to the next, or
    /// until <see cref="Seek"/>/<see cref="Dispose(bool)"/>) - once per crossing,
    /// not per read, so it does not add per-read overhead. A subclass is expected
    /// to defer the actual release of a chunk's native memory until every
    /// outstanding read reference has been released (e.g. via the SafeHandle
    /// refcount behind a memory-mapped view), so a reader that has entered a chunk
    /// completes its reads safely. A cross-thread <see cref="Dispose(bool)"/>
    /// (e.g. a slicer disposing a slice another thread is reading) must NOT
    /// release the reader's reference directly - the reader may be between its
    /// bounds check and its pointer dereference - so this base hands the reference
    /// to a finalizable releaser, which releases it once this instance is
    /// unreachable (hence no thread is reading through it). A same-thread Dispose
    /// releases immediately.
    /// </summary>
    // LUCENENET specific
    internal abstract unsafe class UnsafeChunkIndexInput : IndexInput
    {
        // Per-instance closed flag, independent of per-chunk state, so disposing
        // a clone does not affect the original or sibling clones.
        private int instanceClosed;

        // The window into the chunked region this instance sees (slice range for a
        // slice, [0, regionLength) for a root). Cached-chunk offsets below are
        // window-relative; chunk lookup translates via baseOffset.
        private readonly long baseOffset;
        private readonly long length;
        private readonly int chunkSizePower;

        // Window-relative read cursor. 0 <= position <= length.
        private long position;

        // Cached current-chunk state, valid iff currentChunkIndex >= 0. The cached
        // chunk holds a per-crossing read reference (TryAcquireChunk); released on
        // chunk switch, Seek to a different chunk, or Dispose. The fast path needs
        // only readBase and currentEnd beyond `position`; readBase is precomputed
        // so the load address is a single `*(readBase + pos)`. currentStart is only
        // for Seek cache validation, not read by ReadByte/ReadInt*.
        private int currentChunkIndex = NO_CHUNK;
        private byte* readBase;        // = chunkBase + baseOffset - chunkFileStart
        private long currentStart;     // window-relative start of the cached chunk's intersection with [0, length)
        private long currentEnd;       // window-relative end of the cached chunk's intersection with [0, length)
        // ManagedThreadId of the thread that acquired the cached chunk's read
        // reference. A cross-thread Dispose must NOT release directly: the acquiring
        // thread may be mid-dereference of readBase, so freeing the view under it
        // would AVE. The disposer defers to a finalizer instead (see Dispose /
        // HandOffStrandedReadRef).
        private int currentChunkOwnerThreadId;

        private const int NO_CHUNK = -1;

        /// <summary>
        /// Creates an instance viewing <c>[offset, offset + length)</c> of the
        /// chunked region exposed by the subclass.
        /// </summary>
        /// <param name="resourceDescription">A description of the resource, for diagnostics.</param>
        /// <param name="offset">Window start (window-relative positions are added to this for chunk lookup).</param>
        /// <param name="length">Window length in bytes.</param>
        /// <param name="chunkSizePower">log2 of the chunk size; chunks are <c>1 &lt;&lt; chunkSizePower</c> bytes (last may be shorter).</param>
        protected UnsafeChunkIndexInput(string resourceDescription,
            long offset, long length, int chunkSizePower)
            : base(resourceDescription)
        {
            this.baseOffset = offset;
            this.length = length;
            this.chunkSizePower = chunkSizePower;
        }

        /// <summary>
        /// Throws <see cref="AlreadyClosedException"/> if this instance has been
        /// disposed. Subclasses call this from <c>Clone()</c> (a disposed input
        /// must not hand out a working clone); the base calls it on the read paths.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (Volatile.Read(ref instanceClosed) != 0)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
            }
        }

        // --- Subclass seam: the chunk source ----------------------------------

        /// <summary>
        /// The number of chunks in the backing region. The chunk containing a
        /// global (non-window-relative) position <c>p</c> is at index
        /// <c>p &gt;&gt; chunkSizePower</c>, which is always less than this count
        /// for any in-bounds position.
        /// </summary>
        protected abstract int ChunkCount { get; }

        /// <summary>
        /// Acquire a per-crossing read reference on the chunk at <paramref name="index"/>
        /// and return its base pointer (already adjusted for any page offset). The
        /// caller may dereference the returned pointer until it calls
        /// <see cref="ReleaseChunk"/> with the same index. Returns <c>false</c> if
        /// that chunk has been closed/torn down, which this base translates to an
        /// <see cref="AlreadyClosedException"/>. While a reference is held, the
        /// subclass must keep the chunk's memory valid (deferring any unmap/free
        /// until the reference is released).
        /// </summary>
        protected abstract bool TryAcquireChunk(int index, out byte* chunkBase);

        /// <summary>
        /// Release a read reference previously acquired on the chunk at
        /// <paramref name="index"/> via <see cref="TryAcquireChunk"/>. Must be
        /// callable from any thread.
        /// </summary>
        protected abstract void ReleaseChunk(int index);

        /// <summary>
        /// The length in bytes of the chunk at <paramref name="index"/> (the last
        /// chunk may be shorter than <c>1 &lt;&lt; chunkSizePower</c>).
        /// </summary>
        protected abstract long ChunkLength(int index);

        // --- IndexInput surface ------------------------------------------------

        public override long Length => length;

        public override long Position => position;

        public override void Seek(long pos)
        {
            if (pos < 0 || pos > length)
            {
                throw new IOException("Seek position is out of bounds: " + pos);
            }
            if (Volatile.Read(ref instanceClosed) != 0)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
            }
            // If the seek stays inside the cached chunk, keep the read reference and
            // just move the cursor; otherwise invalidate so the next read reacquires.
            if (currentChunkIndex != NO_CHUNK && pos >= currentStart && pos < currentEnd)
            {
                position = pos;
                return;
            }
            ReleaseCurrentChunk();
            position = pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte ReadByte()
        {
            long pos = position;
            if (pos < currentEnd)
            {
                byte b = *(readBase + pos);
                position = pos + 1;
                return b;
            }
            return ReadByteSlow();
        }

        // Slow path: cache miss, EOF, or disposed. Acquires the chunk containing
        // `position` (or throws), then retries the read.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private byte ReadByteSlow()
        {
            long pos = position;
            if (pos >= length)
            {
                throw EOFException.Create("read past EOF: " + this);
            }
            EnsureCurrentChunk(pos);
            byte b = *(readBase + pos);
            position = pos + 1;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override short ReadInt16()
        {
            long pos = position;
            if (pos + 2 <= currentEnd)
            {
                ushort raw = Unsafe.ReadUnaligned<ushort>(readBase + pos);
                short v = (short)(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(raw) : raw);
                position = pos + 2;
                return v;
            }
            // Slow path: bytes straddle a chunk boundary. Fill via ReadBytes (which
            // handles the crossing) and decode big-endian to match the fast path;
            // avoids the per-byte virtcall round-trip through base.ReadInt16.
            Span<byte> buf = stackalloc byte[2];
            ReadBytes(buf);
            return BinaryPrimitives.ReadInt16BigEndian(buf);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int ReadInt32()
        {
            long pos = position;
            if (pos + 4 <= currentEnd)
            {
                uint raw = Unsafe.ReadUnaligned<uint>(readBase + pos);
                int v = (int)(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(raw) : raw);
                position = pos + 4;
                return v;
            }
            // Slow path: see ReadInt16.
            Span<byte> buf = stackalloc byte[4];
            ReadBytes(buf);
            return BinaryPrimitives.ReadInt32BigEndian(buf);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long ReadInt64()
        {
            long pos = position;
            if (pos + 8 <= currentEnd)
            {
                ulong raw = Unsafe.ReadUnaligned<ulong>(readBase + pos);
                long v = (long)(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(raw) : raw);
                position = pos + 8;
                return v;
            }
            // Slow path: see ReadInt16.
            Span<byte> buf = stackalloc byte[8];
            ReadBytes(buf);
            return BinaryPrimitives.ReadInt64BigEndian(buf);
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            if (b is null)
            {
                throw new ArgumentNullException(nameof(b));
            }
            if ((uint)offset > (uint)b.Length || (uint)len > (uint)(b.Length - offset))
            {
                throw new ArgumentOutOfRangeException(nameof(offset),
                    $"offset/len out of range: offset={offset}, len={len}, b.Length={b.Length}");
            }
            if (len == 0) return;

            ReadBytesCore(ref b[offset], len);
        }

        public override void ReadBytes(Span<byte> destination)
        {
            int len = destination.Length;
            if (len == 0) return;

            ReadBytesCore(ref MemoryMarshal.GetReference(destination), len);
        }

        // Shared inner loop for both ReadBytes overloads. Takes a raw ref + length
        // so the byte[] path avoids a Span ctor + GetReference round-trip. The
        // byte[] overload must bounds-check itself (the Span ctor does it for free).
        private void ReadBytesCore(ref byte destination, int length)
        {
            if (Volatile.Read(ref instanceClosed) != 0)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
            }

            long pos = position;
            if (pos + length > this.length)
            {
                throw EOFException.Create("read past EOF: " + this);
            }

            int remaining = length;
            int dstOff = 0;

            while (remaining > 0)
            {
                if (pos >= currentEnd)
                {
                    EnsureCurrentChunk(pos);
                }

                long available = currentEnd - pos;
                int inChunk = (int)(available < remaining ? available : remaining);

                ref byte src = ref Unsafe.AsRef<byte>(readBase + pos);
                ref byte dst = ref Unsafe.Add(ref destination, dstOff);

                // Small-copy fast path (<= 8 bytes): CopyBlockUnaligned has notable
                // entry overhead for tiny copies, so a sized read/write is cheaper
                // for the common short read (e.g. the ReadInt16/Int32/Int64 slow path).
                if (inChunk <= 8)
                {
                    switch (inChunk)
                    {
                        case 8:
                            Unsafe.WriteUnaligned(ref dst, Unsafe.ReadUnaligned<ulong>(ref src));
                            break;
                        case 4:
                            Unsafe.WriteUnaligned(ref dst, Unsafe.ReadUnaligned<uint>(ref src));
                            break;
                        case 2:
                            Unsafe.WriteUnaligned(ref dst, Unsafe.ReadUnaligned<ushort>(ref src));
                            break;
                        case 1:
                            dst = src;
                            break;
                        default:
                            // 3, 5, 6, 7 - uncommon; fall through to byte loop.
                            for (int i = 0; i < inChunk; i++)
                            {
                                Unsafe.Add(ref dst, i) = Unsafe.Add(ref src, i);
                            }
                            break;
                    }
                }
                else
                {
                    Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)inChunk);
                }

                pos += inChunk;
                dstOff += inChunk;
                remaining -= inChunk;
            }
            position = pos;
        }

        public override void SkipBytes(long numBytes)
        {
            if (numBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numBytes), "numBytes must not be negative");
            }
            long newPos = position + numBytes;
            if (newPos < 0 || newPos > length)
            {
                throw EOFException.Create("skip past EOF: " + this);
            }
            Seek(newPos);
        }

        // --- Chunk crossing + teardown ----------------------------------------

        // Acquire a read reference on the chunk containing window-relative
        // `windowPos`, releasing any held reference first, and set readBase /
        // currentStart / currentEnd. Throws AlreadyClosedException if this instance
        // or the chunk is closed. The reference is taken once per crossing (not per
        // read), keeping the chunk valid without a per-read refcount cost.
        private void EnsureCurrentChunk(long windowPos)
        {
            if (Volatile.Read(ref instanceClosed) != 0)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
            }
            ReleaseCurrentChunk();

            long globalPos = baseOffset + windowPos;
            int chunkIdx = (int)(globalPos >> chunkSizePower);
            if (!TryAcquireChunk(chunkIdx, out byte* chunkBase))
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
            }

            long chunkFileStart = (long)chunkIdx << chunkSizePower;
            long chunkFileEnd = chunkFileStart + ChunkLength(chunkIdx);
            // Window-relative interval = chunk's global interval clipped to
            // [baseOffset, baseOffset + length] and translated.
            long sliceFileEnd = baseOffset + length;
            long start = Math.Max(chunkFileStart, baseOffset) - baseOffset;
            long end = Math.Min(chunkFileEnd, sliceFileEnd) - baseOffset;

            currentChunkIndex = chunkIdx;
            // Precompute readBase so the fast path is a single load *(readBase + pos).
            // Address of window byte `pos` is chunkBase + (baseOffset + pos - chunkFileStart)
            // = (chunkBase + baseOffset - chunkFileStart) + pos.
            readBase = chunkBase + (baseOffset - chunkFileStart);
            currentStart = start;
            currentEnd = end;
            currentChunkOwnerThreadId = Environment.CurrentManagedThreadId;
        }

        // Release the cached chunk's read reference. Only the acquiring thread may
        // call this (on crossing / Seek, or a SAME-THREAD Dispose); a cross-thread
        // Dispose defers via HandOffStrandedReadRef because the acquirer may be
        // mid-dereference. currentChunkIndex is swapped with Interlocked.Exchange so
        // this release and a concurrent handoff can't both claim the same reference:
        // whoever wins the swap owns the single matching ReleaseChunk. Idempotent.
        private void ReleaseCurrentChunk()
        {
            int idx = Interlocked.Exchange(ref currentChunkIndex, NO_CHUNK);
            if (idx != NO_CHUNK)
            {
                readBase = null;
                currentStart = 0;
                currentEnd = 0;
                currentChunkOwnerThreadId = 0;
                ReleaseChunk(idx);
            }
        }

        // Hand a cross-thread-stranded read reference to a finalizable releaser so it
        // is released once this instance is unreachable (no thread can be reading
        // through it, making the unmap/free AVE-safe). A finalizer on the instance
        // itself won't do: base IndexInput.Dispose() calls GC.SuppressFinalize(this).
        // The releaser is a separate, never-suppressed object kept alive by this
        // instance. Allocated only on the rare cross-thread-strand path.
        private StrandedReadRefReleaser? strandedReadRefReleaser;

        private void HandOffStrandedReadRef()
        {
            int idx = Interlocked.Exchange(ref currentChunkIndex, NO_CHUNK);
            if (idx != NO_CHUNK)
            {
                strandedReadRefReleaser = new StrandedReadRefReleaser(this, idx);
            }
        }

        // --- Clone / Dispose ---------------------------------------------------

        /// <summary>
        /// Resets the cursor cache on a freshly-cloned instance. Subclasses MUST
        /// call this from their <c>Clone()</c> override (after <c>base.Clone()</c>)
        /// so the clone does not inherit the parent's cached chunk read reference -
        /// each instance acquires and releases its own; sharing the cached chunk
        /// would make disposing the clone release a reference the parent still
        /// depends on (and vice versa).
        /// </summary>
        protected void ResetClonedCursor()
        {
            instanceClosed = 0;
            currentChunkIndex = NO_CHUNK;
            readBase = null;
            currentStart = 0;
            currentEnd = 0;
            currentChunkOwnerThreadId = 0;
            strandedReadRefReleaser = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            if (Interlocked.CompareExchange(ref instanceClosed, 1, 0) != 0)
            {
                return;
            }
            // Clear currentEnd so a racing reader's fast path (`pos < currentEnd`)
            // drops to the slow path and observes instanceClosed. Interlocked so the
            // 64-bit write can't tear on 32-bit runtimes.
            Interlocked.Exchange(ref currentEnd, 0L);
            // Same-thread Dispose can release directly (this thread is in Dispose,
            // provably not mid-dereference). A cross-thread Dispose (e.g. a slicer
            // disposing a slice another thread is reading, #1013) must hand off: the
            // reader may be between its `pos < currentEnd` check and `*(readBase + pos)`
            // load, so releasing here could free the memory under it (an AVE).
            if (currentChunkOwnerThreadId == Environment.CurrentManagedThreadId)
            {
                ReleaseCurrentChunk();
            }
            else
            {
                HandOffStrandedReadRef();
            }

            DisposeChunkSource(disposing);
        }

        /// <summary>
        /// Called at the end of <see cref="Dispose(bool)"/> (after this instance's
        /// own chunk read reference has been released or handed off) so a subclass
        /// can tear down the backing chunk source it owns. For example, only a
        /// root input owns the shared mapping; clones and slices override this to
        /// do nothing. The default is a no-op.
        /// </summary>
        protected virtual void DisposeChunkSource(bool disposing)
        {
        }

        // Finalizable holder for a cross-thread-stranded chunk read reference (see
        // HandOffStrandedReadRef). Its finalizer calls ReleaseChunk(index) once,
        // only after the owning instance is unreachable, so no thread can be reading
        // through it.
        private sealed class StrandedReadRefReleaser
        {
            private readonly UnsafeChunkIndexInput owner;
            private readonly int chunkIndex;

            internal StrandedReadRefReleaser(UnsafeChunkIndexInput owner, int chunkIndex)
            {
                this.owner = owner;
                this.chunkIndex = chunkIndex;
            }

            ~StrandedReadRefReleaser()
            {
                owner.ReleaseChunk(chunkIndex);
            }
        }
    }
}
