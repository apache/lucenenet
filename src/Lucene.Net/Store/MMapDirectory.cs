using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
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
            EnsureCanRead(name); // LUCENENET-specific: backported call site from Lucene 6.0.0
            var file = Path.Combine(Directory.FullName, name); // LUCENENET specific: changed to use string file name instead of allocating a FileInfo (#832)
            // LUCENENET specific: a fresh SharedMapping per OpenInput call, matching
            // upstream Java (new FileChannel + fc.map()), so Length reflects the
            // file's current size.
            SharedMapping mapping = SharedMapping.Create(file, chunkSizePower);
            // Ownership transfers to the root input (ownsMapping: true); the caller
            // disposes it, which disposes the mapping. No try/catch around the ctor:
            // it only sets fields and cannot throw, so the mapping cannot leak here.
            return new MMapIndexInput($"MMapIndexInput(path=\"{file}\")", ownsMapping: true, mapping, 0, mapping.Length, chunkSizePower);
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            EnsureCanRead(name); // LUCENENET-specific: backported call site from Lucene 6.0.0 (#1357). Unlike upstream, this no longer routes through OpenInput, so validate here too.
            var file = Path.Combine(Directory.FullName, name);
            SharedMapping mapping = SharedMapping.Create(file, chunkSizePower);
            // Ownership transfers to the slicer; the caller disposes it, which
            // disposes the mapping. No try/catch around the ctor: it only sets
            // fields and cannot throw, so the mapping cannot leak here.
            return new IndexInputSlicerAnonymousClass(this, file, mapping);
        }

        private sealed class IndexInputSlicerAnonymousClass : IndexInputSlicer
        {
            private readonly MMapDirectory outerInstance;
            private readonly string file;

            // The slicer owns the shared mapping; issued slices piggyback on it.
            private readonly SharedMapping mapping;

            private int disposed /* = 0 */; // LUCENENET specific - allow double-dispose

            // Track issued slices so Dispose cascades: Lucene's contract is that
            // after slicer.Dispose, reads from any slice (or clone) throw
            // AlreadyClosedException.
            private readonly SCG.List<MMapIndexInput> issuedSlices = new SCG.List<MMapIndexInput>();
            private readonly object issuedSlicesLock = new object();

            public IndexInputSlicerAnonymousClass(MMapDirectory outerInstance, string file, SharedMapping mapping)
            {
                this.outerInstance = outerInstance;
                this.file = file;
                this.mapping = mapping;
            }

            // Returns a slice the CALLER must dispose. The slice does not own the
            // mapping (ownsMapping: false) but is tracked in issuedSlices so
            // disposing the slicer cascades to any slices left open. A slice can
            // outlive a Dispose of outerInstance: we deliberately do not re-check
            // outerInstance on every read because reads against a disposed mapping
            // already fail fast with AlreadyClosedException via the per-chunk rent.
            // EnsureOpen here only guards the act of opening a new slice.
            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                outerInstance.EnsureOpen();
                if (offset < 0 || length < 0 || offset + length > mapping.Length)
                {
                    throw new ArgumentException(
                        "slice() " + sliceDescription + " out of bounds: offset=" + offset
                        + ",length=" + length + ",fileLength=" + mapping.Length + ": " + this);
                }
                // Slices reference the slicer's mapping; only the slicer owns and
                // disposes it.
                var input = new MMapIndexInput(
                    $"MMapIndexInput({sliceDescription} in path=\"{file}\" slice={offset}:{offset + length})", ownsMapping: false, mapping,
                    offset, length,
                    outerInstance.chunkSizePower);
                lock (issuedSlicesLock)
                {
                    if (Volatile.Read(ref disposed) != 0)
                    {
                        // Slicer disposed after EnsureOpen but before we got the
                        // lock; tear down what we just allocated.
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
                // A full slice is a slice over the whole mapping, sharing the
                // slicer's single SharedMapping rather than opening a second one.
                // Length was captured at creation time, so we touch no FileStream.
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

                    // Slicer owns the mapping.
                    mapping.Dispose();
                }
            }
        }

        // LUCENENET specific: the read engine lives in the reusable
        // Support/UnsafeChunkIndexInput base; this subclass only supplies the
        // chunk source - the Chunks of a SharedMapping (#1013, #1151).
        /// <summary>
        /// LUCENENET-specific <see cref="IndexInput"/> backed by an array of
        /// memory-mapped <see cref="Chunk"/>s (each a
        /// <see cref="MemoryMappedViewAccessor"/> whose raw pointer is acquired per
        /// chunk crossing). The read logic is inherited from
        /// <see cref="UnsafeChunkIndexInput"/>; this type wires that engine to a
        /// <see cref="SharedMapping"/>. The chunked, cached-pointer design
        /// addresses two Lucene.NET-specific issues together: <b>#1013</b>
        /// (sporadic <c>AccessViolationException</c> under concurrent search with
        /// <c>SearcherManager</c>, avoided by the per-chunk-crossing read reference
        /// + the SafeHandle refcount that defers <c>UnmapViewOfFile</c>/<c>munmap</c>
        /// until references drain), and <b>#1151</b> (MMapDirectory far slower than
        /// SimpleFSDirectory under parallel load, because
        /// <c>MemoryMappedViewAccessor</c>'s per-call <c>AcquirePointer</c>/range
        /// check contends; caching the pointer per chunk removes it).
        /// </summary>
        internal sealed unsafe class MMapIndexInput : UnsafeChunkIndexInput
        {
            // A "root" instance is one returned from OpenInput; it owns the shared
            // mapping. Clones and slicer-issued slices are non-root. Non-readonly
            // so Clone() can clear it on the clone.
            private bool isRoot;

            // Shared mapping for this IndexInput, shared by the root and its clones,
            // OR by a slicer + its slices + their clones. Only root instances
            // (ownsMapping == true) dispose it. This is the only state this subclass
            // adds; the read engine state lives in the base.
            private readonly SharedMapping mapping;

            // LUCENENET specific (PR #1267): for testing only. Exposes the shared
            // mapping so a test can assert that disposing a root input
            // deterministically disposes the mapping's backing FileStream.
            internal SharedMapping Mapping => mapping;

            /// <summary>
            /// Creates an <see cref="IndexInput"/> viewing <c>[offset, offset+length)</c>
            /// of the given shared mapping. Pass <c>ownsMapping: true</c> only for
            /// the root returned from <see cref="MMapDirectory.OpenInput(string, IOContext)"/>;
            /// slices issued from a slicer and clones must pass <c>false</c>.
            /// </summary>
            internal MMapIndexInput(string resourceDescription,
                bool ownsMapping, SharedMapping mapping,
                long offset, long length, int chunkSizePower)
                : base(resourceDescription, offset, length, chunkSizePower)
            {
                this.isRoot = ownsMapping;
                this.mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            }

            // --- UnsafeChunkIndexInput chunk source ---------------------------

            protected override int ChunkCount => mapping.Chunks.Length;

            protected override bool TryAcquireChunk(int index, out byte* chunkBase)
                => mapping.Chunks[index].TryAcquireRead(out chunkBase);

            protected override void ReleaseChunk(int index)
                => mapping.Chunks[index].ReleaseRead();

            protected override long ChunkLength(int index) => mapping.Chunks[index].Length;

            // --- Clone / dispose ----------------------------------------------

            public override object Clone()
            {
                // A disposed input must not hand out a working clone.
                ThrowIfDisposed();
                // The clone shares the parent's SharedMapping but does NOT own it;
                // it acquires its own per-chunk-crossing read reference on first
                // read. ResetClonedCursor clears the inherited cursor cache so the
                // clone never releases a reference the parent depends on.
                var clone = (MMapIndexInput)base.Clone();
                clone.isRoot = false;
                clone.ResetClonedCursor();
                return clone;
            }

            protected override void DisposeChunkSource(bool disposing)
            {
                // Only root instances own the mapping; for clones and slices the
                // base already released this instance's read reference. Disposing
                // the mapping closes its chunk accessors; the SafeHandle refcount
                // defers each chunk's unmap until outstanding read references drain.
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
        /// read reference in <see cref="Chunk"/> (a balanced
        /// <see cref="SafeBuffer.AcquirePointer"/>) ensures in-flight reads from
        /// non-owning clones/slices either complete safely against the
        /// still-mapped view or fail with <see cref="AlreadyClosedException"/>
        /// rather than dereferencing a freed mapping.
        /// </summary>
        internal sealed class SharedMapping : IDisposable
        {
            /// <summary>
            /// The memory-mapped file reference for this mapping.
            /// Note that this can be null in the edge case of a zero-length mapping.
            /// </summary>
            private readonly MemoryMappedFile? memoryMappedFile;

            /// <summary>
            /// The <see cref="FileStream"/> backing <see cref="memoryMappedFile"/>.
            /// We pass this stream to
            /// <see cref="MemoryMappedFile.CreateFromFile(FileStream, string?, long, MemoryMappedFileAccess, HandleInheritability, bool)"/>
            /// with <c>leaveOpen: true</c>, so the mapping borrows the file handle
            /// but never disposes the <see cref="FileStream"/> object. This mapping
            /// owns it and disposes it in <see cref="Dispose"/> so the stream (a
            /// finalizable object holding the file handle) is released
            /// deterministically rather than left to the finalizer. Null for the
            /// zero-length edge case (no mapping is created).
            /// </summary>
            private readonly FileStream? fileStream;
            private int disposed;

            // LUCENENET specific (PR #1267): for testing only. True once Dispose has
            // run and the owned FileStream (if any) has been disposed, so a test can
            // assert the mapping releases its FileStream deterministically rather
            // than leaking it to finalization.
            internal bool IsFileStreamDisposed =>
                Volatile.Read(ref disposed) != 0 &&
                (fileStream is null || !fileStream.CanRead);

            private SharedMapping(MemoryMappedFile? mmf, FileStream? fileStream, Chunk[] chunks, long length)
            {
                this.memoryMappedFile = mmf;
                this.fileStream = fileStream;
                this.Chunks = chunks;
                this.Length = length;
            }

            internal static SharedMapping Create(string file, int chunkSizePower)
            {
                // .NET Framework's MemoryMappedFile.CreateFromFile reads
                // fileStream.Length twice non-atomically (defaulting capacity from
                // it, then enforcing `Length <= capacity`), so a concurrent extender
                // that grows the file between those reads trips an
                // ArgumentOutOfRangeException ("capacity"). Modern .NET caches the
                // length into a single local and reuses it, so the race cannot fire
                // and this loop runs once. (#1090)
                // FW:     https://github.com/microsoft/referencesource/blob/ec9fa9ae770d522a5b5f0607898044b7478574a3/System.Core/System/IO/MemoryMappedFiles/MemoryMappedFile.cs#L192-L243
                // modern: https://github.com/dotnet/runtime/blob/550500a978b784658a04110d49b3335dcacf33e0/src/libraries/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedFile.cs#L237-L268
                //         https://github.com/dotnet/runtime/blob/550500a978b784658a04110d49b3335dcacf33e0/src/libraries/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedFile.Windows.cs#L14-L26
                //
                // The retry budget is generous because the retry is cheap and a tight
                // extender can keep winning the race; Yield between attempts so it can
                // reach a stable point between writes.
                const int maxAttempts = 32;
                for (int attempt = 0; ; attempt++)
                {
                    try
                    {
                        return CreateAttempt(file, chunkSizePower);
                    }
                    catch (ArgumentOutOfRangeException e)
                        when (e.ParamName == "capacity" && attempt < maxAttempts - 1)
                    {
                        // CreateAttempt already disposed its FileStream before
                        // throwing, so just yield and retry.
                        Thread.Yield();
                    }
                }
            }

            private static SharedMapping CreateAttempt(string file, int chunkSizePower)
            {
                // We open our own FileStream to control the FileShare flags. The
                // path-based CreateFromFile overload uses FileShare.Read, but on
                // Windows a delete against an open file fails unless the share-mode
                // permits FILE_SHARE_DELETE, so we need FileShare.Delete (matching
                // Java's FileChannel default read+write+delete). Without it, callers
                // that build a temp index then recursively delete it (e.g.
                // FreeTextSuggester) break.
                //
                // bufferSize: 1 because the MMF bypasses the FileStream buffer, so a
                // 4 KiB default buffer would be allocated and immediately discarded.
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
                        // CreateFromFile rejects capacity 0, so handle the empty
                        // file ourselves. Dispose fs through the swallowing overload
                        // since no MMF will own it and a throwing Dispose must not
                        // escape this success path.
                        IOUtils.DisposeWhileHandlingException(fs);
                        return new SharedMapping(mmf: null, fileStream: null, chunks: Array.Empty<Chunk>(), length: 0);
                    }

                    // capacity: 0 -> the framework sizes the mapping from the file's
                    // current length (the source of the .NET Framework race that
                    // Create's retry loop handles).
                    // leaveOpen: true -> the MMF borrows fs's handle but never
                    // disposes it; SharedMapping owns fs and closes the handle in
                    // Dispose. This keeps a single unambiguous owner of the handle.
                    mmf = MemoryMappedFile.CreateFromFile(
                        fileStream: fs,
                        mapName: null,
                        capacity: 0,
                        access: MemoryMappedFileAccess.Read,
#if FEATURE_MEMORYMAPPEDFILESECURITY
                        memoryMappedFileSecurity: null,
#endif
                        inheritability: HandleInheritability.None,
                        leaveOpen: true);
                    chunks = MapChunks(mmf, 0, length, chunkSizePower);
                    return new SharedMapping(mmf, fs, chunks, length);
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    // Cleanup must not mask e: DisposeChunks swallows internally and
                    // we dispose mmf/fs through the swallowing overload (not the
                    // priorException overload, which would re-throw), then bare-
                    // rethrow to preserve e's stack trace. With leaveOpen: true we
                    // always own fs; dispose mmf first so the mapping is torn down
                    // before the backing handle closes.
                    DisposeChunks(chunks);
                    IOUtils.DisposeWhileHandlingException(mmf, fs);
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
                // mmf first so the mapping is torn down before we close the handle
                // by disposing the FileStream we own. Both are null for the
                // zero-length edge case, which the overload tolerates.
                IOUtils.DisposeWhileHandlingException(memoryMappedFile, fileStream);
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
                // LUCENENET specific: ceiling-divide for an exact cover with no
                // trailing empty slot. Upstream Java (MMapDirectory.map) instead
                // allocates floor + 1 and keeps a final 0-byte sentinel ByteBuffer,
                // because its read loop unconditionally advances the buffer cursor
                // past each buffer's end. We don't need that: the base read engine
                // bounds-checks before indexing and only advances the chunk index
                // while bytes remain. (CreateViewAccessor rejects a zero-length view
                // anyway, so a sentinel would need a special case.)
                int nChunks = (int)((length + chunkSize - 1) >> chunkSizePower);
                var result = new Chunk[nChunks];

                try
                {
                    for (int i = 0; i < nChunks; i++)
                    {
                        long chunkOffset = offset + ((long)i << chunkSizePower);
                        long thisChunkLen = Math.Min(chunkSize, length - ((long)i << chunkSizePower));

                        MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(chunkOffset, thisChunkLen, MemoryMappedFileAccess.Read);
                        // We do NOT AcquirePointer here: that is done per chunk-
                        // crossing in Chunk.TryAcquireRead/ReleaseRead so the
                        // SafeHandle refcount is the drain barrier and a crossing
                        // after Close fails fast. A map-time AcquirePointer would
                        // pin the handle past Close and defeat that. PointerOffset
                        // (the view's offset within its OS page) is available
                        // without acquiring a pointer.
                        result[i] = new Chunk(accessor, accessor.PointerOffset, thisChunkLen);
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
        /// Concurrency model: the <see cref="MemoryMappedViewAccessor"/>'s
        /// <see cref="SafeBuffer"/> (a refcounted <see cref="SafeHandle"/>) IS
        /// the drain barrier - we use it directly instead of a hand-rolled
        /// in-flight count. A reader holds a per-chunk-crossing reference via
        /// <see cref="TryAcquireRead"/> (a balanced
        /// <see cref="SafeBuffer.AcquirePointer"/>) for as long as it caches the
        /// chunk's pointer (from one chunk crossing to the next, or until the
        /// owning <see cref="IndexInput"/> is sought to a different chunk or
        /// disposed), and drops it via <see cref="ReleaseRead"/>
        /// (<see cref="SafeBuffer.ReleasePointer"/>). <see cref="Close"/> just
        /// disposes the accessor: the runtime defers the actual
        /// <c>UnmapViewOfFile</c>/<c>munmap</c> until every outstanding
        /// AcquirePointer reference has been released, and fails any subsequent
        /// <see cref="TryAcquireRead"/> with <see cref="ObjectDisposedException"/>
        /// (which the caller maps to <see cref="AlreadyClosedException"/>). So a
        /// reader that has entered a chunk completes its reads safely on the
        /// still-mapped view, and any later chunk crossing after Close throws.
        /// AVEs are structurally impossible because the unmap cannot run while
        /// any read reference is outstanding - this is the BCL's own guarantee
        /// for <see cref="SafeHandle"/>, not a custom invariant. The AcquirePointer
        /// is taken only ONCE PER CHUNK CROSSING (not per read), so it does not
        /// reintroduce the per-read refcount contention of #1151.
        /// </summary>
        internal sealed unsafe class Chunk
        {
            private readonly MemoryMappedViewAccessor accessor;
            private readonly SafeMemoryMappedViewHandle safe;
            // The view's offset within its OS page (nonzero when it doesn't start
            // page-aligned).
            private readonly long pointerOffset;
            internal readonly long Length;

            // Set once Close has disposed the accessor; only makes Close idempotent.
            // The real teardown synchronization is the SafeHandle refcount.
            private int closed;

            internal Chunk(MemoryMappedViewAccessor accessor, long pointerOffset, long length)
            {
                this.accessor = accessor;
                this.safe = accessor.SafeMemoryMappedViewHandle;
                this.pointerOffset = pointerOffset;
                this.Length = length;
            }

            // LUCENENET specific (PR #1267): for testing only. True once Close has
            // disposed the accessor, so a test can assert disposing the owning input
            // tears the chunk down rather than leaking it.
            internal bool IsNativeReleased => Volatile.Read(ref closed) != 0;

            /// <summary>
            /// Acquire a per-chunk-crossing read reference and return the chunk's
            /// base pointer (already adjusted for the view's page offset). The
            /// caller may dereference the returned pointer until it calls
            /// <see cref="ReleaseRead"/>. Returns false if the chunk has been
            /// closed (its accessor disposed): the underlying
            /// <see cref="SafeBuffer.AcquirePointer"/> throws
            /// <see cref="ObjectDisposedException"/> in that case, which we
            /// translate to a clean "closed" result. While a reference is held,
            /// the runtime cannot unmap the view, so the pointer is safe to read.
            /// </summary>
            public bool TryAcquireRead(out byte* readBase)
            {
                byte* ptr = null;
                try
                {
                    safe.AcquirePointer(ref ptr);
                }
                catch (ObjectDisposedException)
                {
                    // Close disposed the handle; treat as closed.
                    readBase = null;
                    return false;
                }
                readBase = ptr + pointerOffset;
                return true;
            }

            /// <summary>
            /// Release a previously-acquired read reference. Once the last
            /// outstanding reference is released after <see cref="Close"/>, the
            /// runtime performs the actual unmap.
            /// </summary>
            public void ReleaseRead()
            {
                safe.ReleasePointer();
            }

            /// <summary>
            /// Dispose this chunk's accessor. The runtime defers the actual
            /// <c>UnmapViewOfFile</c>/<c>munmap</c> until every outstanding
            /// <see cref="TryAcquireRead"/> reference is released, so Close never
            /// unmaps a view out from under a live reader and never blocks
            /// waiting for one. Idempotent.
            /// </summary>
            public void Close()
            {
                if (Interlocked.CompareExchange(ref closed, 1, 0) != 0) return;
                // Disposing the accessor disposes the SafeHandle, which requests the
                // unmap but defers it until the AcquirePointer refcount reaches zero.
                // Swallowing overload so a throwing Dispose never propagates.
                IOUtils.DisposeWhileHandlingException(accessor);
            }
        }
    }
}
