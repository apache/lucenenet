using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using SCG = System.Collections.Generic;

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
    /// File-based <see cref="Directory"/> implementation that uses
    /// <see cref="MemoryMappedFile"/> for reading, and
    /// <see cref="FSDirectory.FSIndexOutput"/> for writing.
    ///
    /// <para/><b>NOTE</b>: memory mapping uses up a portion of the
    /// virtual memory address space in your process equal to the
    /// size of the file being mapped.  Before using this class,
    /// be sure your have plenty of virtual address space, e.g. by
    /// using a 64-bit runtime, or a 32-bit runtime with indexes that are
    /// guaranteed to fit within the address space.
    /// On 32-bit platforms also consult <see cref="MMapDirectory(DirectoryInfo, LockFactory, int)"/>
    /// if you have problems with mmap failing because of fragmented
    /// address space. If you get an <see cref="OutOfMemoryException"/>, it is recommended
    /// to reduce the chunk size, until it works.
    /// <para/>
    /// <font color="red"><b>NOTE:</b> Unlike in Java, it is not recommended to use
    /// <see cref="System.Threading.Thread.Interrupt()"/> in .NET
    /// in conjunction with an open <see cref="FSDirectory"/> because it is not guaranteed to exit atomically.
    /// Any <c>lock</c> statement or <see cref="System.Threading.Monitor.Enter(object)"/> call can throw a
    /// <see cref="System.Threading.ThreadInterruptedException"/>, which makes shutting down unpredictable.
    /// To exit parallel tasks safely, we recommend using <see cref="System.Threading.Tasks.Task"/>s
    /// and "interrupt" them with <see cref="System.Threading.CancellationToken"/>s.</font>
    /// </summary>
    public class MMapDirectory : FSDirectory
    {
        // LUCENENET specific - unmap hack not needed

        /// <summary>
        /// Default max chunk size. </summary>
        /// <seealso cref="MMapDirectory(DirectoryInfo, LockFactory, int)"/>
        public static readonly int DEFAULT_MAX_BUFF = Constants.RUNTIME_IS_64BIT ? (1 << 30) : (1 << 28);

        private readonly int chunkSizePower;

        /// <summary>
        /// Create a new <see cref="MMapDirectory"/> for the named location.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<see cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public MMapDirectory(DirectoryInfo path, LockFactory? lockFactory)
            : this(path, lockFactory, DEFAULT_MAX_BUFF)
        {
        }

        /// <summary>
        /// Create a new <see cref="MMapDirectory"/> for the named location and <see cref="NativeFSLockFactory"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public MMapDirectory(DirectoryInfo path)
            : this(path, null)
        {
        }

        /// <summary>
        /// Create a new <see cref="MMapDirectory"/> for the named location, specifying the
        /// maximum chunk size used for memory mapping.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or <c>null</c> for the default
        /// (<see cref="NativeFSLockFactory"/>); </param>
        /// <param name="maxChunkSize"> maximum chunk size (default is 1 GiBytes for
        /// 64 bit runtimes and 256 MiBytes for 32 bit runtimes) used for memory mapping.
        /// <para/>
        /// Especially on 32 bit platform, the address space can be very fragmented,
        /// so large index files cannot be mapped. Using a lower chunk size makes
        /// the directory implementation a little bit slower (as the correct chunk
        /// may be resolved on lots of seeks) but the chance is higher that mmap
        /// does not fail. On 64 bit platforms, this parameter should always
        /// be <c>1 &lt;&lt; 30</c>, as the address space is big enough.
        /// <para/>
        /// <b>Please note:</b> The chunk size is always rounded down to a power of 2.
        /// </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public MMapDirectory(DirectoryInfo path, LockFactory? lockFactory, int maxChunkSize)
            : base(path, lockFactory)
        {
            if (maxChunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxChunkSize), "Maximum chunk size for mmap must be > 0"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.chunkSizePower = 31 - maxChunkSize.LeadingZeroCount();
            if (Debugging.AssertsEnabled) Debugging.Assert(this.chunkSizePower >= 0 && this.chunkSizePower <= 30);
        }

        /// <summary>
        /// Create a new <see cref="MMapDirectory"/> for the named location.
        /// <para/>
        /// LUCENENET specific overload for convenience using string instead of <see cref="DirectoryInfo"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<see cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public MMapDirectory(string path, LockFactory? lockFactory)
            : this(path, lockFactory, DEFAULT_MAX_BUFF)
        {
        }

        /// <summary>
        /// Create a new <see cref="MMapDirectory"/> for the named location and <see cref="NativeFSLockFactory"/>.
        /// <para/>
        /// LUCENENET specific overload for convenience using string instead of <see cref="DirectoryInfo"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public MMapDirectory(string path)
            : this(path, null)
        {
        }

        /// <summary>
        /// Create a new <see cref="MMapDirectory"/> for the named location, specifying the
        /// maximum chunk size used for memory mapping.
        /// <para/>
        /// LUCENENET specific overload for convenience using string instead of <see cref="DirectoryInfo"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or <c>null</c> for the default
        /// (<see cref="NativeFSLockFactory"/>); </param>
        /// <param name="maxChunkSize"> maximum chunk size (default is 1 GiBytes for
        /// 64 bit runtimes and 256 MiBytes for 32 bit runtimes) used for memory mapping.
        /// <para/>
        /// Especially on 32 bit platform, the address space can be very fragmented,
        /// so large index files cannot be mapped. Using a lower chunk size makes
        /// the directory implementation a little bit slower (as the correct chunk
        /// may be resolved on lots of seeks) but the chance is higher that mmap
        /// does not fail. On 64 bit platforms, this parameter should always
        /// be <c>1 &lt;&lt; 30</c>, as the address space is big enough.
        /// <para/>
        /// <b>Please note:</b> The chunk size is always rounded down to a power of 2.
        /// </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public MMapDirectory(string path, LockFactory? lockFactory, int maxChunkSize)
            : this(new DirectoryInfo(path), lockFactory, maxChunkSize)
        {
        }

        // LUCENENET specific - Some JREs had a bug that didn't allow them to unmap.
        // But according to MSDN, the MemoryMappedFile.Dispose() method will
        // indeed "release all resources". Therefore, unmap hack is not needed in .NET.

        /// <summary>
        /// Returns the current mmap chunk size.
        /// </summary>
        /// <seealso cref="MMapDirectory(DirectoryInfo, LockFactory, int)"/>
        public int MaxChunkSize => 1 << chunkSizePower;

        /// <summary>
        /// Creates an <see cref="IndexInput"/> for the file with the given name. </summary>
        public override IndexInput OpenInput(string name, IOContext context)
        {
            EnsureOpen();
            var file = Path.Combine(Directory.FullName, name); // LUCENENET specific: changed to use string file name instead of allocating a FileInfo (#832)
            // LUCENENET specific: a fresh SharedMapping per OpenInput call.
            // Matches upstream Java (openInput creates a new FileChannel +
            // fc.map()) and ensures Length reflects the file's current size.
            SharedMapping mapping = SharedMapping.Create(file, chunkSizePower);
            try
            {
                return new MMapIndexInput("MMapIndexInput(path=\"" + file + "\")", ownsMapping: true, mapping, 0, mapping.Length, chunkSizePower);
            }
            catch
            {
                mapping.Dispose();
                throw;
            }
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            var file = Path.Combine(Directory.FullName, name);
            SharedMapping mapping = SharedMapping.Create(file, chunkSizePower);
            try
            {
                return new IndexInputSlicerAnonymousClass(this, file, mapping);
            }
            catch
            {
                mapping.Dispose();
                throw;
            }
        }

        private sealed class IndexInputSlicerAnonymousClass : IndexInputSlicer
        {
            private readonly MMapDirectory outerInstance;
            private readonly string file;

            // The slicer owns the shared mapping; all slices issued from it
            // piggyback on the slicer's ownership. Disposing the slicer
            // tears the mapping down once issued slices have been disposed.
            private readonly SharedMapping mapping;

            private int disposed /* = 0 */; // LUCENENET specific - allow double-dispose

            // Track issued slices so that Dispose cascades. Lucene's
            // contract is that after slicer.Dispose, reads from any slice
            // (or clone of a slice) throw AlreadyClosedException.
            private readonly SCG.List<MMapIndexInput> issuedSlices = new SCG.List<MMapIndexInput>();
            private readonly object issuedSlicesLock = new object();

            public IndexInputSlicerAnonymousClass(MMapDirectory outerInstance, string file, SharedMapping mapping)
            {
                this.outerInstance = outerInstance;
                this.file = file;
                this.mapping = mapping;
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                outerInstance.EnsureOpen();
                if (offset < 0 || length < 0 || offset + length > mapping.Length)
                {
                    throw new ArgumentException(
                        "slice() " + sliceDescription + " out of bounds: offset=" + offset
                        + ",length=" + length + ",fileLength=" + mapping.Length + ": " + this);
                }
                // Slices reference the slicer's mapping; only the slicer
                // itself owns the mapping and will dispose it.
                var input = new MMapIndexInput(
                    "MMapIndexInput(" + sliceDescription + " in path=\"" + file + "\" slice=" + offset + ":" + (offset + length) + ")", ownsMapping: false, mapping,
                    offset, length,
                    outerInstance.chunkSizePower);
                lock (issuedSlicesLock)
                {
                    if (Volatile.Read(ref disposed) != 0)
                    {
                        // Slicer was disposed after EnsureOpen but before we
                        // got the lock. Tear down what we just allocated.
                        input.Dispose();
                        throw AlreadyClosedException.Create(nameof(IndexInputSlicer), "this IndexInputSlicer is closed");
                    }
                    issuedSlices.Add(input);
                }
                return input;
            }

            [Obsolete("Only for reading CFS files from 3.x indexes.")]
            public override IndexInput OpenFullSlice()
            {
                outerInstance.EnsureOpen();
                // The shared mapping's Length was captured at creation time,
                // so we don't need to touch any FileStream here.
                return OpenSlice("full-slice", 0, mapping.Length);
            }

            protected override void Dispose(bool disposing)
            {
                if (0 != Interlocked.CompareExchange(ref this.disposed, 1, 0)) return; // LUCENENET specific - allow double-dispose

                if (disposing)
                {
                    IDisposable[] toDispose;
                    lock (issuedSlicesLock)
                    {
                        toDispose = issuedSlices.OfType<IDisposable>().ToArray();
                        issuedSlices.Clear();
                    }

                    IOUtils.DisposeWhileHandlingException(toDispose);

                    // Slicer owns the mapping; tear it down.
                    mapping.Dispose();
                }
            }
        }

        // LUCENENET specific: nested IndexInput implementation. This replaces
        // the upstream ByteBufferIndexInput approach with an
        // array of MemoryMappedViewAccessor chunks whose raw pointers are
        // acquired once per chunk. See the class doc for rationale (#1013,
        // #1151).
        /// <summary>
        /// LUCENENET-specific <see cref="IndexInput"/> that reads from an array of
        /// <see cref="MemoryMappedViewAccessor"/> chunks via cached raw pointers
        /// obtained once per chunk from
        /// <see cref="System.Runtime.InteropServices.SafeBuffer.AcquirePointer(ref byte*)"/>.
        /// This diverges from the upstream Java implementation (which uses
        /// <c>ByteBufferIndexInput</c> + <c>ByteBufferGuard</c>) in order to address
        /// two Lucene.NET-specific issues together:
        ///
        /// <list type="bullet">
        ///   <item><description>
        ///     <b>#1013</b> — sporadic <c>AccessViolationException</c> in
        ///     concurrent search with <c>SearcherManager</c>. The <c>SafeHandle</c>
        ///     refcount incremented by <c>AcquirePointer</c> defers the underlying
        ///     <c>munmap</c>/<c>UnmapViewOfFile</c> until every outstanding pointer
        ///     ref is released, which structurally prevents a reader from
        ///     dereferencing a freed mapping while any clone is still alive.
        ///     A per-chunk closed-flag checked on each refill observes in-flight
        ///     Dispose so clones throw <see cref="AlreadyClosedException"/>
        ///     instead of reading stale bytes.
        ///   </description></item>
        ///   <item><description>
        ///     <b>#1151</b> — MMapDirectory 37–51× slower than
        ///     SimpleFSDirectory under parallel load. Every
        ///     <c>MemoryMappedViewAccessor.ReadByte</c>/<c>ReadArray</c> call does
        ///     its own <c>AcquirePointer</c>/<c>ReleasePointer</c> + range check;
        ///     that per-call overhead is the hot spot in LZ4 decompression and
        ///     other one-byte-at-a-time readers. Caching the pointer once per
        ///     chunk and using <c>Unsafe.CopyBlockUnaligned</c> eliminates that
        ///     overhead.
        ///   </description></item>
        /// </list>
        ///
        /// <para/>
        /// The file is mapped as an array of fixed-size <see cref="Chunk"/>s of
        /// <c>1 &lt;&lt; chunkSizePower</c> bytes (the last chunk may be smaller),
        /// mirroring the upstream Java <c>ByteBufferIndexInput</c> shape. Chunking
        /// avoids huge contiguous virtual-address reservations (helpful on 32-bit
        /// runtimes and on long-running 64-bit processes where address space can
        /// fragment), keeps each native mapping small enough to be reclaimed
        /// predictably, and aligns with Lucene's typical small-working-set access
        /// patterns.
        ///
        /// <para/>
        /// Reads go directly through cached chunk pointers. <c>ReadByte</c>,
        /// <c>ReadInt16</c>, <c>ReadInt32</c>, and <c>ReadInt64</c> are
        /// specialized fast paths that read straight from the cached chunk
        /// pointer with a single bounds check, avoiding any managed-buffer
        /// indirection. <c>ReadVInt32</c>, <c>ReadVInt64</c>, <c>ReadString</c>
        /// etc. inherit from <see cref="DataInput"/> and call our fast
        /// <c>ReadByte</c>, so they pick up the speed-up automatically. This
        /// mirrors upstream Java's <c>ByteBufferIndexInput</c>, which derives
        /// directly from <c>IndexInput</c> rather than via a buffered base.
        ///
        /// <para/>
        /// Concurrency: the per-chunk rent (<see cref="Chunk.TryAcquire"/>/
        /// <see cref="Chunk.Release"/>) is held for as long as this
        /// <see cref="IndexInput"/> caches a chunk's pointer (i.e., from one
        /// chunk crossing to the next, or until <see cref="Seek"/>/
        /// <see cref="Dispose(bool)"/>). <see cref="Chunk.Close"/> sets the
        /// closed flag immediately and lazily defers <c>UnmapViewOfFile</c>/
        /// <c>munmap</c> until every outstanding rent has been released. A
        /// reader that has already entered a chunk completes its reads
        /// safely on the still-mapped view; any subsequent chunk crossing
        /// or read after the parent has been disposed fails with
        /// <see cref="AlreadyClosedException"/>.
        /// </summary>
        internal sealed unsafe class MMapIndexInput : IndexInput
        {
            // Non-readonly so that Clone() can mark the cloned instance as a
            // non-root (see Clone()). A "root" instance is one returned from
            // OpenInput (it owns the shared mapping). Clones and slices from
            // IndexInputSlicer are non-root.
            private bool isRoot;

            // Per-instance closed flag: tracks whether THIS IndexInput
            // instance has been disposed. Independent of the shared mapping's
            // per-chunk closed flags, so that disposing a clone does not
            // affect the root or sibling clones.
            private int instanceClosed;

            // Shared mapping used by this IndexInput. The same SharedMapping
            // is shared by the root returned from OpenInput and its clones,
            // OR by a slicer + its issued slices + their clones. Only root
            // instances (ownsMapping == true) dispose the underlying mapping
            // on Dispose. Slices and clones do not own it.
            private readonly SharedMapping mapping;

            // The window into the shared mapping that this IndexInput sees.
            // For OpenInput this is [0, mapping.Length); for OpenSlice it is
            // the requested slice range. All offsets in the cached chunk
            // fields below are slice-relative; chunk lookup translates via
            // baseOffset.
            private readonly long baseOffset;
            private readonly long length;
            private readonly int chunkSizePower;

            // Slice-relative read cursor. 0 <= position <= length.
            private long position;

            // Cached current-chunk state. Valid iff currentChunk != null.
            // currentChunk holds a TryAcquire rent; we Release when switching
            // chunks, on Seek to a different chunk, or on Dispose.
            //
            // The fast path needs only TWO fields beyond `position` to compute
            // the load address:
            //   readBase: a precomputed pointer such that the file byte at
            //             slice-relative position `pos` is `*(readBase + pos)`.
            //             This collapses chunkBasePtr + baseOffset -
            //             chunkFileStart into a single value, removing two
            //             field loads from the per-byte hot path.
            //   currentEnd: slice-relative end of the cached chunk's
            //             intersection with [0, length); fast path is
            //             `pos < currentEnd`.
            // currentStart is kept only for backward-seek cache validation
            // in Seek(); it isn't read by ReadByte/ReadInt*.
            private Chunk? currentChunk;
            private byte* readBase;        // = chunkBasePtr + baseOffset - chunkFileStart
            private long currentStart;     // slice-relative start of the cached chunk's intersection with [0, length)
            private long currentEnd;       // slice-relative end of the cached chunk's intersection with [0, length)
            // ManagedThreadId of the thread that acquired currentChunk's rent.
            // Used to keep cross-thread Dispose (e.g., slicer.Dispose disposing
            // slices being read on other threads) from releasing a rent it
            // doesn't own — see Dispose for the lifecycle rationale.
            private int currentChunkOwnerThreadId;

            /// <summary>
            /// Creates an <see cref="IndexInput"/> viewing <c>[offset, offset+length)</c>
            /// of the given shared mapping. Pass <c>ownsMapping: true</c> only for
            /// the root returned from <see cref="MMapDirectory.OpenInput(string, IOContext)"/>;
            /// slices issued from a slicer and clones must pass <c>false</c>.
            /// </summary>
            internal MMapIndexInput(string resourceDescription,
                bool ownsMapping, SharedMapping mapping,
                long offset, long length, int chunkSizePower)
                : base(resourceDescription)
            {
                this.isRoot = ownsMapping;
                this.mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
                this.baseOffset = offset;
                this.length = length;
                this.chunkSizePower = chunkSizePower;
            }

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
                // If the seek lands inside the currently-rented chunk, we
                // can keep the rent and just move the cursor. Otherwise,
                // invalidate the cache so the next read takes the slow path
                // and acquires the new chunk.
                if (currentChunk != null && pos >= currentStart && pos < currentEnd)
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

            // Slow path: cache miss, EOF, or instance disposed. Acquires the
            // chunk that contains `position` (or throws), then retries the
            // single-byte read.
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
                return base.ReadInt16();
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
                return base.ReadInt32();
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
                return base.ReadInt64();
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                ReadBytes(new Span<byte>(b, offset, len));
            }

            public override void ReadBytes(Span<byte> destination)
            {
                if (Volatile.Read(ref instanceClosed) != 0)
                {
                    throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
                }

                int len = destination.Length;
                if (len == 0) return;

                long pos = position;
                if (pos + len > length)
                {
                    throw EOFException.Create("read past EOF: " + this);
                }

                int dstOff = 0;
                while (len > 0)
                {
                    if (pos >= currentEnd)
                    {
                        EnsureCurrentChunk(pos);
                    }
                    int inChunk = (int)Math.Min(currentEnd - pos, (long)len);
                    ref byte src = ref Unsafe.AsRef<byte>(readBase + pos);
                    ref byte dst = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(destination.Slice(dstOff));
                    Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)inChunk);
                    pos += inChunk;
                    dstOff += inChunk;
                    len -= inChunk;
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

            // Acquire the chunk containing `slicePos` (slice-relative). Releases
            // any currently-rented chunk first. Sets currentChunkBasePtr,
            // currentChunkFileStart, currentStart, and currentEnd. Throws
            // AlreadyClosedException if this instance or the chunk is closed.
            private void EnsureCurrentChunk(long slicePos)
            {
                if (Volatile.Read(ref instanceClosed) != 0)
                {
                    throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
                }
                ReleaseCurrentChunk();

                long globalPos = baseOffset + slicePos;
                Chunk[] chunks = mapping.Chunks;
                int chunkIdx = (int)(globalPos >> chunkSizePower);
                Chunk chunk = chunks[chunkIdx];
                if (!chunk.TryAcquire())
                {
                    throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
                }

                long chunkFileStart = (long)chunkIdx << chunkSizePower;
                long chunkFileEnd = chunkFileStart + chunk.length;
                // Slice-relative interval = chunk's file interval clipped to
                // [baseOffset, baseOffset + length] and translated.
                long sliceFileEnd = baseOffset + length;
                long start = Math.Max(chunkFileStart, baseOffset) - baseOffset;
                long end = Math.Min(chunkFileEnd, sliceFileEnd) - baseOffset;

                currentChunk = chunk;
                // Precompute readBase so ReadByte/ReadInt* fast-path is
                // a single load: *(readBase + pos). Algebraically, the
                // file address of slice byte `pos` is
                //   chunk.basePtr + (baseOffset + pos - chunkFileStart)
                // = (chunk.basePtr + baseOffset - chunkFileStart) + pos
                //                ^------- readBase ----^
                readBase = chunk.basePtr + (baseOffset - chunkFileStart);
                currentStart = start;
                currentEnd = end;
                currentChunkOwnerThreadId = Environment.CurrentManagedThreadId;
            }

            // Release the rent on the currently-cached chunk. Must only be
            // called from the thread that acquired the rent; cross-thread
            // Dispose paths must not call this. Idempotent if no rent is
            // currently held.
            private void ReleaseCurrentChunk()
            {
                Chunk? c = currentChunk;
                if (c != null)
                {
                    currentChunk = null;
                    readBase = null;
                    currentStart = 0;
                    currentEnd = 0;
                    currentChunkOwnerThreadId = 0;
                    c.Release();
                }
            }

            public override object Clone()
            {
                if (Volatile.Read(ref instanceClosed) != 0)
                {
                    throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
                }
                // Share the same SharedMapping with the parent. The clone
                // does NOT acquire its own refcount on the mapping; it
                // piggybacks on the parent's lifetime. The clone enters
                // its own chunk rent on first read.
                var clone = (MMapIndexInput)base.Clone();
                clone.isRoot = false;
                clone.instanceClosed = 0;
                // Critical: do NOT inherit the parent's chunk rent. Otherwise
                // disposing the clone would ExitRead a rent the parent still
                // depends on (and vice versa).
                clone.currentChunk = null;
                clone.readBase = null;
                clone.currentStart = 0;
                clone.currentEnd = 0;
                clone.currentChunkOwnerThreadId = 0;
                return clone;
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing)
                {
                    return;
                }
                // Mark this instance closed. Idempotent.
                if (Interlocked.CompareExchange(ref instanceClosed, 1, 0) != 0)
                {
                    return;
                }
                // Cross-thread Dispose safety: only release the chunk rent
                // if Dispose runs on the same thread that acquired it.
                // Otherwise (e.g. slicer.Dispose disposing slices being read
                // on other threads — #1013) the rent is owned by the reader
                // and we must not touch it. The reader's next Read* call
                // observes instanceClosed at the start of EnsureCurrentChunk
                // (and via ReadBytes' guard) and throws AlreadyClosedException
                // — its slow path ReleaseCurrentChunk runs on the reader's
                // own thread, releasing the rent safely. If the reader stops
                // reading entirely without disposing, the rent is released
                // when the MMapIndexInput is garbage-collected (the chunk
                // is reachable via SharedMapping → Chunks[i] until then).
                //
                // We DO clear currentEnd from any thread, so the reader's
                // fast path (`pos < currentEnd`) takes the slow path on its
                // next call and observes the instanceClosed flag. Use
                // Interlocked.Exchange so the 64-bit write is atomic on
                // 32-bit runtimes too (a torn write could otherwise produce
                // a value that accidentally satisfies pos < currentEnd, so
                // a disposed reader could return one extra byte before
                // throwing — the rent keeps the mapping alive, so this is
                // a correctness nit rather than a memory-safety issue).
                Interlocked.Exchange(ref currentEnd, 0L);
                if (currentChunkOwnerThreadId == Environment.CurrentManagedThreadId)
                {
                    ReleaseCurrentChunk();
                }
                // Only root instances own the shared mapping. Clones and
                // slices do not own it; disposing them only flips this
                // instance's closed flag.
                if (isRoot)
                {
                    mapping.Dispose();
                }
            }
        }

        /// <summary>
        /// LUCENENET specific: a single memory-mapped file plus its chunk
        /// array, owned by exactly one <see cref="MMapIndexInput"/> root or
        /// one <see cref="Directory.IndexInputSlicer"/>. Clones and slices
        /// reference it without owning it. The owner calls
        /// <see cref="SharedMapping.Dispose"/> exactly once; the per-chunk
        /// rent in <see cref="Chunk"/> ensures in-flight reads from
        /// non-owning clones/slices either complete safely against the
        /// still-mapped view or fail with <see cref="AlreadyClosedException"/>
        /// rather than dereferencing a freed mapping.
        /// </summary>
        internal sealed unsafe class SharedMapping : IDisposable
        {
            /// <summary>
            /// The memory-mapped file reference for this mapping.
            /// Note that this can be null in the edge case of a zero-length mapping.
            /// </summary>
            private readonly MemoryMappedFile? memoryMappedFile;
            private int disposed;

            private SharedMapping(MemoryMappedFile? mmf, Chunk[] chunks, long length)
            {
                this.memoryMappedFile = mmf;
                this.Chunks = chunks;
                this.Length = length;
            }

            internal static SharedMapping Create(string file, int chunkSizePower)
            {
                // We open our own FileStream so we control the FileShare
                // flags. The path-based CreateFromFile overload internally
                // opens with FileShare.Read, which on Windows blocks
                // attempts to delete or write to this file while we have
                // it mapped — breaking callers (e.g. FreeTextSuggester)
                // that build a temp index, dispose the directory, and
                // immediately recursively delete the directory. We need
                // FileShare.Delete in particular: on Windows, a delete
                // attempt against an open file fails unless the open
                // share-mode permits FILE_SHARE_DELETE.
                //
                // bufferSize: 1 because MemoryMappedFile uses only the
                // file handle and bypasses the FileStream buffer, so a
                // 4 KiB default buffer would just be allocated and
                // immediately discarded.
                FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 1, FileOptions.RandomAccess);
                MemoryMappedFile? mmf = null;
                Chunk[]? chunks = null;
                try
                {
                    long length = fs.Length;
                    if (length == 0)
                    {
                        // CreateViewAccessor rejects zero-length views and
                        // CreateFromFile rejects capacity 0 on an empty file,
                        // so handle this edge case ourselves. Dispose the
                        // FileStream eagerly since there's no MMF to own it.
                        fs.Dispose();
                        return new SharedMapping(mmf: null, chunks: Array.Empty<Chunk>(), length: 0);
                    }

                    // capacity: 0 -> the framework uses the file's
                    // current size on disk, atomically with mapping
                    // creation. This eliminates the #1090 race window
                    // we previously had to retry around.
                    // leaveOpen: false -> the MMF takes ownership of the
                    // FileStream and disposes it on its own Dispose, so
                    // we don't need to track it ourselves.
                    mmf = MemoryMappedFile.CreateFromFile(
                        fileStream: fs,
                        mapName: null,
                        capacity: 0,
                        access: MemoryMappedFileAccess.Read,
#if FEATURE_MEMORYMAPPEDFILESECURITY
                        memoryMappedFileSecurity: null,
#endif
                        inheritability: HandleInheritability.None,
                        leaveOpen: false);
                    chunks = MapChunks(mmf, 0, length, chunkSizePower);
                    return new SharedMapping(mmf, chunks, length);
                }
                catch
                {
                    DisposeChunks(chunks);
                    if (mmf != null)
                    {
                        mmf.Dispose(); // disposes fs (leaveOpen: false)
                    }
                    else
                    {
                        IOUtils.DisposeWhileHandlingException(fs);
                    }
                    throw;
                }
            }

            /// <summary>
            /// Dispose the underlying native resources. Idempotent.
            /// </summary>
            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;
                DisposeChunks(Chunks);
                IOUtils.DisposeWhileHandlingException(memoryMappedFile);
            }

            internal Chunk[] Chunks { get; }

            internal long Length { get; }

            private static Chunk[] MapChunks(MemoryMappedFile? mmf, long offset, long length, int chunkSizePower)
            {
                if (length == 0 || mmf == null)
                {
                    return Array.Empty<Chunk>();
                }

                long chunkSize = 1L << chunkSizePower;
                // LUCENENET specific: ceiling-divide, so nChunks covers exactly
                // the requested range with no trailing empty slot. Upstream
                // Java (MMapDirectory.map) instead allocates nrBuffers = floor + 1
                // and tolerates a final 0-byte ByteBuffer — a sentinel required
                // because ByteBufferIndexInput's read loop unconditionally
                // advances the buffer cursor past the end of each buffer, so
                // indexing `buffers[N]` must be valid after the last byte of a
                // file whose length is a whole multiple of chunkSize. We don't
                // need that here: ReadInternal bounds-checks `pos + len` before
                // indexing and only advances `chunkIdx` while `len > 0`. Also,
                // MemoryMappedFile.CreateViewAccessor rejects a zero-length view,
                // so a sentinel would require a special-case code path anyway.
                int nChunks = (int)((length + chunkSize - 1) >> chunkSizePower);
                var result = new Chunk[nChunks];

                try
                {
                    for (int i = 0; i < nChunks; i++)
                    {
                        long chunkOffset = offset + ((long)i << chunkSizePower);
                        long thisChunkLen = Math.Min(chunkSize, length - ((long)i << chunkSizePower));

                        var accessor = mmf.CreateViewAccessor(chunkOffset, thisChunkLen, MemoryMappedFileAccess.Read);
                        byte* ptr = null;
                        try
                        {
                            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                        }
                        catch
                        {
                            accessor.Dispose();
                            throw;
                        }
                        // The accessor may be mapped at an offset inside the OS page,
                        // in which case PointerOffset is the distance from the
                        // SafeBuffer's base to the first byte of the requested view.
                        result[i] = new Chunk(accessor, ptr + accessor.PointerOffset, thisChunkLen);
                    }
                    return result;
                }
                catch
                {
                    DisposeChunks(result);
                    throw;
                }
            }

            private static void DisposeChunks(Chunk[]? chunks)
            {
                if (chunks == null) return;
                foreach (var c in chunks)
                {
                    try
                    {
                        c.Close();
                    }
                    catch
                    {
                         /* never propagate from cleanup */
                    }
                }
            }
        }

        /// <summary>
        /// Owner of a single chunk's <see cref="MemoryMappedViewAccessor"/>.
        /// One <see cref="Chunk"/> per chunk-sized region of the file; they
        /// live inside a <see cref="SharedMapping"/>.
        ///
        /// <para/>
        /// Concurrency model: a reader holds a "rent" on the chunk via
        /// <see cref="TryAcquire"/>/<see cref="Release"/> for as long as it
        /// caches the chunk's pointer (i.e., from one chunk crossing to the
        /// next, or until the owning <see cref="IndexInput"/> is sought to
        /// a different chunk or disposed). <see cref="Close"/> flips the
        /// closed flag immediately, so any subsequent <see cref="TryAcquire"/>
        /// fails. The actual <c>ReleasePointer</c> + <c>accessor.Dispose</c>
        /// are deferred until the per-chunk in-flight count reaches zero,
        /// either at <see cref="Close"/> time (no readers) or whenever the
        /// last reader calls <see cref="Release"/>. This is the "lazy
        /// unmap" pattern: <see cref="Close"/> never blocks waiting for
        /// readers, and a reader's cached pointer stays valid for as long
        /// as it hasn't released. AVEs are structurally impossible because
        /// <c>UnmapViewOfFile</c>/<c>munmap</c> can't run while any rent is
        /// outstanding.
        /// </summary>
        internal sealed unsafe class Chunk
        {
            private MemoryMappedViewAccessor? accessor;
            internal readonly long length;
            // The chunk's base pointer (already adjusted for PointerOffset).
            // Stays valid until the last rent has been released AND Close
            // has run (whichever is later). Per-rent acquisition does NOT
            // require a fresh AcquirePointer because the mapping's own
            // AcquirePointer (taken once in MapChunks) keeps the SafeBuffer
            // alive; the per-chunk inFlight count below is what defers the
            // matching ReleasePointer.
            internal readonly byte* basePtr;

            // Bit 0: closed flag. Bits 1..31: in-flight rent count. Packed
            // into a single int so closed-then-zero-rent can be observed
            // atomically and the actual unmap done exactly once.
            // closed=1, inFlight=0 is the "ready to release native resources"
            // terminal state; whoever observes that transition does the
            // ReleasePointer + accessor.Dispose.
            private int state;

            private const int CLOSED_BIT = 1;
            private const int RENT_INC = 2;

            internal Chunk(MemoryMappedViewAccessor accessor, byte* basePtr, long length)
            {
                this.accessor = accessor;
                this.basePtr = basePtr;
                this.length = length;
            }

            /// <summary>
            /// Acquire a rent on this chunk. Returns false if the chunk is
            /// closed; otherwise the caller may dereference <see cref="basePtr"/>
            /// until it calls <see cref="Release"/>.
            /// </summary>
            public bool TryAcquire()
            {
                // CAS-loop: increment rent count iff closed bit is clear.
                while (true)
                {
                    int s = Volatile.Read(ref state);
                    if ((s & CLOSED_BIT) != 0) return false;
                    int next = s + RENT_INC;
                    if (Interlocked.CompareExchange(ref state, next, s) == s) return true;
                }
            }

            /// <summary>
            /// Release a previously-acquired rent. If this was the last
            /// outstanding rent and the chunk is closed, releases the native
            /// resources here.
            /// </summary>
            public void Release()
            {
                int s = Interlocked.Add(ref state, -RENT_INC);
                if (s == CLOSED_BIT)
                {
                    // closed && inFlight==0: we observed the terminal state.
                    ReleaseNative();
                }
            }

            /// <summary>
            /// Mark the chunk closed. New <see cref="TryAcquire"/> calls fail.
            /// If no rents are outstanding, releases native resources
            /// immediately; otherwise the last <see cref="Release"/> will.
            /// </summary>
            public void Close()
            {
                // CAS-loop: set the closed bit if not already set.
                while (true)
                {
                    int s = Volatile.Read(ref state);
                    if ((s & CLOSED_BIT) != 0) return; // already closed
                    int next = s | CLOSED_BIT;
                    if (Interlocked.CompareExchange(ref state, next, s) == s)
                    {
                        if (next == CLOSED_BIT)
                        {
                            // No rents outstanding; we own the unmap.
                            ReleaseNative();
                        }
                        return;
                    }
                }
            }

            // Releases the SafeBuffer's mapping-level reference and disposes
            // the accessor. Must be called exactly once, by whichever thread
            // observes the (closed=1, inFlight=0) transition.
            private void ReleaseNative()
            {
                MemoryMappedViewAccessor? acc = accessor;
                if (acc is null) return;
                accessor = null;
                try
                {
                    acc.SafeMemoryMappedViewHandle.ReleasePointer();
                }
                catch
                {
                    // Never propagate from cleanup.
                }
                acc.Dispose();
            }
        }
    }
}
