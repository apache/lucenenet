using Lucene.Net.Support;
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
    /// A subclass exposes <see cref="ChunkCount"/> chunks; for each chunk it
    /// supplies a cached base pointer (<see cref="ChunkBase"/>) and the chunk's
    /// length (<see cref="ChunkLength"/>). This base never sees the underlying
    /// chunk implementation (a <c>MemoryMappedViewAccessor</c>, a pinned managed
    /// array in tests, etc.) - only those two operations.
    ///
    /// <para/>
    /// <b>Window.</b> An instance views <c>[baseOffset, baseOffset + length)</c>
    /// of the chunked region; this supports slices over a larger backing region.
    /// All positions exposed to callers are window-relative; chunk lookup
    /// translates through <c>baseOffset</c>.
    ///
    /// <para/>
    /// <b>Concurrency / teardown.</b> A chunk's base pointer is valid for the whole
    /// lifetime of the backing region, so the read paths cache it across reads with
    /// no per-read or per-crossing native call (this is what removes the #1151
    /// contention). Liveness against a concurrent close is provided by an
    /// <see cref="IMMapReclaimer"/> shared by every instance over a region (a root,
    /// its clones, and any slices): each instance registers once, and every pointer
    /// dereference on the read paths is bracketed by the reclaimer's
    /// <c>Enter</c>/<c>Exit</c>. Closing the region calls <c>Close</c> on the
    /// reclaimer, which defers the actual unmap until all in-flight readers have
    /// drained, and makes any later <c>Enter</c> throw <see cref="AlreadyClosedException"/>.
    /// Because the announce-then-check in <c>Enter</c> is symmetric to the
    /// publish-then-scan in <c>Close</c>, a reader is never left dereferencing a
    /// freed view - even when a different thread (a slicer disposing a slice this
    /// thread is reading) triggers the close. Only the region owner (the root)
    /// closes the reclaimer; disposing a clone or slice just invalidates that
    /// instance's cursor.
    /// </summary>
    // LUCENENET specific
    internal abstract unsafe class UnsafeChunkIndexInput : IndexInput
    {
        // Per-instance closed flag, independent of per-chunk state, so disposing
        // a clone does not affect the original or sibling clones.
        private int instanceClosed;

        // The mapping-wide reclaimer (shared by the root, its clones, and any
        // slices) and this instance's own reader token. Each instance must have its
        // OWN token: tokens carry a per-reader re-entrancy depth, and a clone reads
        // concurrently with its parent, so they cannot share one. Clone() copies
        // readerToken by reference, so ResetClonedCursor re-registers to replace it.
        private readonly IMMapReclaimer reclaimer;
        private IMMapReclaimer.IReaderToken readerToken;

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

        private const int NO_CHUNK = -1;

        /// <summary>
        /// Creates an instance viewing <c>[offset, offset + length)</c> of the
        /// chunked region exposed by the subclass.
        /// </summary>
        /// <param name="resourceDescription">A description of the resource, for diagnostics.</param>
        /// <param name="offset">Window start (window-relative positions are added to this for chunk lookup).</param>
        /// <param name="length">Window length in bytes.</param>
        /// <param name="chunkSizePower">log2 of the chunk size; chunks are <c>1 &lt;&lt; chunkSizePower</c> bytes (last may be shorter).</param>
        protected UnsafeChunkIndexInput(IMMapReclaimer reclaimer, string resourceDescription,
            long offset, long length, int chunkSizePower)
            : base(resourceDescription)
        {
            this.reclaimer = reclaimer;
            this.readerToken = reclaimer.Register(this);
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
        /// The cached base pointer of the chunk at <paramref name="index"/>
        /// (already adjusted for any page offset). The pointer is valid for the
        /// chunk's whole lifetime; callers only dereference it inside the
        /// reclaimer's <c>Enter</c>/<c>Exit</c> bracket so a concurrent close cannot
        /// unmap the chunk mid-dereference.
        /// </summary>
        protected abstract byte* ChunkBase(int index);

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
                reclaimer.Enter(readerToken);
                try
                {
                    byte b = *(readBase + pos);
                    position = pos + 1;
                    return b;
                }
                finally
                {
                    reclaimer.Exit(readerToken);
                }
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

            reclaimer.Enter(readerToken);
            try
            {
                byte b = *(readBase + pos);
                position = pos + 1;
                return b;
            }
            finally
            {
                reclaimer.Exit(readerToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override short ReadInt16()
        {
            long pos = position;
            if (pos + 2 <= currentEnd)
            {
                reclaimer.Enter(readerToken);
                try
                {
                    ushort raw = Unsafe.ReadUnaligned<ushort>(readBase + pos);
                    short v = (short)(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(raw) : raw);
                    position = pos + 2;
                    return v;
                }
                finally
                {
                    reclaimer.Exit(readerToken);
                }
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
                reclaimer.Enter(readerToken);
                try
                {
                    uint raw = Unsafe.ReadUnaligned<uint>(readBase + pos);
                    int v = (int)(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(raw) : raw);
                    position = pos + 4;
                    return v;
                }
                finally
                {
                    reclaimer.Exit(readerToken);
                }
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
                reclaimer.Enter(readerToken);
                try
                {
                    ulong raw = Unsafe.ReadUnaligned<ulong>(readBase + pos);
                    long v = (long)(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(raw) : raw);
                    position = pos + 8;
                    return v;
                }
                finally
                {
                    reclaimer.Exit(readerToken);
                }
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

                reclaimer.Enter(readerToken);
                try
                {
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
                }
                finally
                {
                    reclaimer.Exit(readerToken);
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

        // Point the cursor cache at the chunk containing window-relative
        // `windowPos`: set readBase / currentStart / currentEnd. Throws
        // AlreadyClosedException if this instance is closed. No read reference is
        // taken here - the chunk's base pointer is valid for the mapping's lifetime
        // and the reclaimer's Enter/Exit (around each dereference) is what keeps a
        // concurrent close from unmapping it mid-read.
        private void EnsureCurrentChunk(long windowPos)
        {
            if (Volatile.Read(ref instanceClosed) != 0)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
            }
            ReleaseCurrentChunk();

            long globalPos = baseOffset + windowPos;
            int chunkIdx = (int)(globalPos >> chunkSizePower);
            byte* chunkBase = ChunkBase(chunkIdx);

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
        }

        // Invalidate the cached chunk cursor so the next read reacquires via
        // EnsureCurrentChunk. There is no native reference to drop (the chunk's
        // pointer is owned by the mapping and reclaimed only when the mapping
        // closes), so this just clears the cached fields. Idempotent.
        private void ReleaseCurrentChunk()
        {
            int idx = Interlocked.Exchange(ref currentChunkIndex, NO_CHUNK);
            if (idx != NO_CHUNK)
            {
                readBase = null;
                currentStart = 0;
                currentEnd = 0;
            }
        }

        // --- Clone / Dispose ---------------------------------------------------

        /// <summary>
        /// Resets a freshly-cloned instance. Subclasses MUST call this from their
        /// <c>Clone()</c> override (after <c>base.Clone()</c>) so the clone does not
        /// share the parent's cached cursor or reader token: it gets its own token
        /// (its own re-entrancy depth in the reclaimer) and an empty cursor cache,
        /// so the clone and parent read independently and disposing one never
        /// affects the other.
        /// </summary>
        protected void ResetClonedCursor()
        {
            instanceClosed = 0;
            currentChunkIndex = NO_CHUNK;
            readBase = null;
            currentStart = 0;
            currentEnd = 0;
            // base.Clone() copied the parent's token by reference; replace it with a
            // fresh registration so this clone has its own depth slot.
            readerToken = reclaimer.Register(this);
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

            // Invalidate this instance's cursor cache. This only affects this input;
            // the shared mapping (and its reclaimer) is closed by the owning root's
            // DisposeChunkSource, NOT here, so disposing a clone or slice never tears
            // down the mapping that sibling readers still depend on.
            ReleaseCurrentChunk();

            DisposeChunkSource(disposing);
        }

        /// <summary>
        /// Called at the end of <see cref="Dispose(bool)"/> so a subclass can tear
        /// down the backing chunk source it owns. Only a root input owns the shared
        /// mapping (and so closes its reclaimer, deferring the unmap until readers
        /// drain); clones and slices leave this a no-op. The default is a no-op.
        /// </summary>
        protected virtual void DisposeChunkSource(bool disposing)
        {
        }
    }
}
