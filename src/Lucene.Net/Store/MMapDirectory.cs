using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Concurrent;
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
    /// using a 64 bit runtime, or a 32 bit runtime with indexes that are
    /// guaranteed to fit within the address space.
    /// On 32 bit platforms also consult <see cref="MMapDirectory(DirectoryInfo, LockFactory, int)"/>
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

        // LUCENENET specific BEGIN: test-only counters for the capacity-retry
        // path in Map() — see #1090. Internal (exposed via InternalsVisibleTo
        // to the test assemblies) so regression tests can assert that the
        // race was actually exercised during a run, and to gather data on how
        // many retries are typically needed. Not intended for production use.
        internal static long s_capacityRetryCount;
        internal static int s_maxCapacityAttemptsObserved;
        // LUCENENET specific END

        // LUCENENET specific: per-file shared mapping cache. Each entry holds
        // a single MemoryMappedFile + chunk array shared across OpenInput,
        // CreateSlicer's slices, and clones for the same file. Refcounted so
        // that the mapping is torn down when the last referencing IndexInput
        // (or slicer) is disposed. Lazy<T> guards against two threads racing
        // to create a mapping for the same key.
        private readonly ConcurrentDictionary<string, Lazy<SharedMapping>> _mappings
            = new ConcurrentDictionary<string, Lazy<SharedMapping>>(StringComparer.Ordinal);

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
        // indeed "release all resources". Therefore unmap hack is not needed in .NET.

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
            // LUCENENET specific: the new MMapIndexInput path (nested class
            // below) replaces the upstream ByteBufferIndexInput-based approach.
            // See #1013 + #1151 and the class doc comment for rationale.
            var (mapping, lazy) = AcquireMapping(file);
            try
            {
                return new MMapIndexInput("MMapIndexInput(path=\"" + file + "\")",
                    this, file, lazy, mapping, 0, mapping.Length, context, chunkSizePower);
            }
            catch
            {
                ReleaseMapping(file, lazy);
                throw;
            }
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            var file = Path.Combine(Directory.FullName, name);
            var (mapping, lazy) = AcquireMapping(file);
            try
            {
                return new IndexInputSlicerAnonymousClass(this, context, file, lazy, mapping);
            }
            catch
            {
                ReleaseMapping(file, lazy);
                throw;
            }
        }

        // LUCENENET specific: acquire or create the shared mapping for the
        // given file path. Increments the mapping's refcount and returns both
        // the SharedMapping and the Lazy<SharedMapping> handle used for
        // Release. The Lazy<T> wrapper ensures that if two threads race on
        // the same key, only one MemoryMappedFile is created.
        internal (SharedMapping mapping, Lazy<SharedMapping> lazy) AcquireMapping(string file)
        {
            while (true)
            {
                // Was the Lazy already in the dict? If so, we need to
                // TryAcquire (an existing mapping can be mid-teardown and
                // should reject new acquirers). If we're the thread that
                // just added the Lazy, SharedMapping.Create returns refCount=1
                // and we own that ref — no further acquire needed.
                bool createdHere = false;
                var lazy = _mappings.GetOrAdd(file, f =>
                {
                    createdHere = true;
                    return new Lazy<SharedMapping>(
                        () => SharedMapping.Create(f, chunkSizePower),
                        LazyThreadSafetyMode.ExecutionAndPublication);
                });
                SharedMapping mapping;
                try
                {
                    mapping = lazy.Value;
                }
                catch
                {
                    // Creation failed — remove the poisoned Lazy so a retry
                    // on a different thread can try again, then rethrow.
                    _mappings.TryRemove(new SCG.KeyValuePair<string, Lazy<SharedMapping>>(file, lazy));
                    throw;
                }

                if (createdHere)
                {
                    // We own the initial refCount=1 that SharedMapping.Create
                    // returned.
                    return (mapping, lazy);
                }

                // Existing mapping. TryAcquire returns false if the mapping
                // was already released (refcount hit zero between GetOrAdd
                // and our Increment). In that case a concurrent Release is
                // about to remove the entry; loop and create a new one.
                if (mapping.TryAcquire())
                {
                    return (mapping, lazy);
                }
            }
        }

        // LUCENENET specific: release one ref on the given mapping. Removes
        // the dictionary entry and disposes underlying resources when the
        // refcount reaches zero.
        internal void ReleaseMapping(string file, Lazy<SharedMapping> lazy)
        {
            SharedMapping mapping;
            try
            {
                mapping = lazy.Value;
            }
            catch
            {
                return;
            }
            if (mapping.Release())
            {
                _mappings.TryRemove(new SCG.KeyValuePair<string, Lazy<SharedMapping>>(file, lazy));
                mapping.DisposeResources();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Forcibly tear down any mappings still in the dictionary.
                // Well-behaved callers should have disposed all IndexInputs
                // and slicers before disposing the directory, but Lucene
                // tests (and some callers) rely on directory Dispose
                // cleaning up dangling resources.
                var pairs = _mappings.ToArray();
                _mappings.Clear();
                foreach (var pair in pairs)
                {
                    try
                    {
                        if (pair.Value.IsValueCreated)
                        {
                            pair.Value.Value.DisposeResources();
                        }
                    }
                    catch
                    {
                        // never propagate from cleanup
                    }
                }
            }
            base.Dispose(disposing);
        }

        private sealed class IndexInputSlicerAnonymousClass : IndexInputSlicer
        {
            private readonly MMapDirectory outerInstance;
            private readonly IOContext context;
            private readonly string file;
            // The slicer holds one refcount on the shared mapping; all slices
            // issued from it piggyback on that ref (they do not increment).
            private readonly Lazy<SharedMapping> mappingLazy;
            private readonly SharedMapping mapping;
            private int disposed /* = 0 */; // LUCENENET specific - allow double-dispose
            // Track issued slices so that Dispose cascades. Lucene's
            // contract is that after slicer.Dispose, reads from any slice
            // (or clone of a slice) throw AlreadyClosedException.
            private readonly SCG.List<MMapIndexInput> issuedSlices
                = new SCG.List<MMapIndexInput>();
            private readonly object issuedSlicesLock = new object();

            public IndexInputSlicerAnonymousClass(MMapDirectory outerInstance, IOContext context, string file,
                Lazy<SharedMapping> mappingLazy, SharedMapping mapping)
            {
                this.outerInstance = outerInstance;
                this.context = context;
                this.file = file;
                this.mappingLazy = mappingLazy;
                this.mapping = mapping;
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                outerInstance.EnsureOpen();
                // Slices share the slicer's mapping ref; the MMapIndexInput
                // instance does NOT own a refcount on the shared mapping.
                var input = new MMapIndexInput(
                    "MMapIndexInput(" + sliceDescription + " in path=\"" + file + "\" slice=" + offset + ":" + (offset + length) + ")",
                    outerInstance, file, mappingLazy: null, mapping,
                    offset, length,
                    BufferedIndexInput.GetBufferSize(context), outerInstance.chunkSizePower);
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

                    // Release the slicer's refcount on the shared mapping.
                    outerInstance.ReleaseMapping(file, mappingLazy);
                }
            }
        }

        // LUCENENET specific: nested IndexInput implementation. This replaces
        // the upstream ByteBufferIndexInput + ByteBufferGuard approach with an
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
        /// The class inherits from <see cref="BufferedIndexInput"/> so that
        /// <c>ReadByte</c>, <c>ReadInt16</c>, <c>ReadVInt32</c> etc. are all
        /// served from the small managed buffer after a single bulk refill from
        /// the mapped region.
        /// </summary>
        internal sealed unsafe class MMapIndexInput : BufferedIndexInput
        {
            // Non-readonly so that Clone() can mark the cloned instance as a
            // non-root (see Clone()). A "root" instance is one returned from
            // OpenInput (it owns a refcount on the shared mapping). Clones
            // and slices from IndexInputSlicer are non-root.
            private bool isRoot = true;

            // Per-instance closed flag: tracks whether THIS IndexInput
            // instance has been disposed. Independent of the shared mapping's
            // per-chunk closed flags, so that disposing a clone does not
            // affect the root or sibling clones.
            private int instanceClosed;

            // Shared mapping used by this IndexInput. The same SharedMapping
            // is shared by OpenInput's root, every OpenSlice, and every
            // Clone. Only instances where mappingLazy != null own a refcount
            // and are responsible for releasing the mapping in Dispose.
            private readonly SharedMapping mapping;
            private readonly MMapDirectory directory;
            private readonly string file;
            private readonly Lazy<SharedMapping>? mappingLazy;

            // The window into the shared mapping that this IndexInput sees.
            // For OpenInput this is [0, mapping.Length); for OpenSlice it is
            // the requested slice range.
            private readonly long baseOffset;
            private readonly long length;
            private readonly int chunkSizePower;
            private readonly long chunkSizeMask;

            /// <summary>
            /// Creates an <see cref="IndexInput"/> viewing <c>[offset, offset+length)</c>
            /// of the given shared mapping. Pass a non-null <paramref name="mappingLazy"/>
            /// only for instances that own a refcount on the mapping (i.e. the
            /// root returned from <see cref="MMapDirectory.OpenInput(string, IOContext)"/>);
            /// slices issued from a slicer and clones piggyback on their
            /// parent's ref and must pass <c>null</c>.
            /// </summary>
            internal MMapIndexInput(string resourceDescription, MMapDirectory directory, string file,
                Lazy<SharedMapping> mappingLazy, SharedMapping mapping,
                long offset, long length, IOContext context, int chunkSizePower)
                : base(resourceDescription, context)
            {
                this.directory = directory;
                this.file = file;
                this.mappingLazy = mappingLazy;
                this.mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
                this.baseOffset = offset;
                this.length = length;
                this.chunkSizePower = chunkSizePower;
                this.chunkSizeMask = (1L << chunkSizePower) - 1L;
            }

            /// <summary>
            /// Slice-bufferSize overload used by <see cref="Directory.IndexInputSlicer"/>.
            /// </summary>
            internal MMapIndexInput(string resourceDescription, MMapDirectory directory, string file,
                Lazy<SharedMapping>? mappingLazy, SharedMapping mapping,
                long offset, long length, int bufferSize, int chunkSizePower)
                : base(resourceDescription, bufferSize)
            {
                this.directory = directory;
                this.file = file;
                this.mappingLazy = mappingLazy;
                this.mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
                this.baseOffset = offset;
                this.length = length;
                this.chunkSizePower = chunkSizePower;
                this.chunkSizeMask = (1L << chunkSizePower) - 1L;
            }

            public override long Length => length;

            protected override void ReadInternal(Span<byte> destination)
            {
                if (Volatile.Read(ref instanceClosed) != 0)
                {
                    throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
                }

                int len = destination.Length;
                if (len == 0) return;

                long pos = Position;
                if (pos + len > length)
                {
                    throw EOFException.Create("read past EOF: " + this);
                }

                // Translate to absolute file position, then walk the shared
                // chunks one at a time. A single ReadInternal can span chunks.
                long globalPos = baseOffset + pos;
                Chunk[] chunks = mapping.Chunks;
                int chunkIdx = (int)(globalPos >> chunkSizePower);
                int chunkOff = (int)(globalPos & chunkSizeMask);
                int dstOff = 0;

                while (len > 0)
                {
                    Chunk chunk = chunks[chunkIdx];
                    int avail = (int)(chunk.length - chunkOff);
                    int toCopy = avail < len ? avail : len;

                    if (!chunk.TryEnterRead())
                    {
                        throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
                    }
                    try
                    {
                        ref byte src = ref Unsafe.AsRef<byte>(chunk.basePtr + chunkOff);
                        ref byte dst = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(destination.Slice(dstOff));
                        Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)toCopy);
                    }
                    finally
                    {
                        chunk.ExitRead();
                    }

                    dstOff += toCopy;
                    len -= toCopy;
                    chunkIdx++;
                    chunkOff = 0;
                }
            }

            protected override void SeekInternal(long pos)
            {
                if (pos < 0 || pos > length)
                {
                    throw new IOException("Seek position is out of bounds: " + pos);
                }
                if (Volatile.Read(ref instanceClosed) != 0)
                {
                    throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
                }
                // Fail fast if the shared mapping has already been torn down
                // by a concurrent Dispose of the last ref-holder.
                Chunk[] chunks = mapping.Chunks;
                if (chunks.Length > 0 && chunks[0].IsClosed)
                {
                    throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
                }
            }

            public override object Clone()
            {
                // Share the same SharedMapping with the parent. The clone
                // does NOT acquire its own refcount; it piggybacks on the
                // parent's lifetime. Matching Lucene's contract, disposing
                // the root (the IndexInput returned from OpenInput) tears
                // down the underlying mapping for all clones.
                var clone = (MMapIndexInput)base.Clone();
                clone.isRoot = false;
                clone.instanceClosed = 0;
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
                // Only root instances hold a refcount on the shared mapping.
                // Clones and slices share their parent's ref, so disposing
                // them only flips this instance's closed flag.
                if (isRoot && mappingLazy != null)
                {
                    directory.ReleaseMapping(file, mappingLazy);
                }
            }
        }

        /// <summary>
        /// LUCENENET specific: a single memory-mapped file plus its chunk
        /// array, shared across <see cref="MMapIndexInput"/> instances that
        /// reference the same file (including slices and clones).
        /// Refcounted so that the underlying native resources are torn down
        /// when the last referencing IndexInput or slicer is disposed.
        /// </summary>
        internal sealed unsafe class SharedMapping
        {
            /// <summary>
            /// The memory-mapped file reference for this mapping.
            /// Note that this can be null in the edge case of a zero-length mapping.
            /// </summary>
            private readonly MemoryMappedFile? memoryMappedFile;
            private readonly FileStream fileStream;

            // 1 for the initial creator (held by AcquireMapping until the
            // caller transfers it to the returned IndexInput/Slicer).
            private int refCount = 1;

            private SharedMapping(MemoryMappedFile? mmf, FileStream fs, Chunk[] chunks, long length)
            {
                this.memoryMappedFile = mmf;
                this.fileStream = fs;
                this.Chunks = chunks;
                this.Length = length;
            }

            internal static SharedMapping Create(string file, int chunkSizePower)
            {
                // MemoryMappedFile uses only the file handle and bypasses
                // the FileStream buffer, so bufferSize: 1 avoids allocating
                // a 4 KiB buffer that would immediately be discarded.
                var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                    bufferSize: 1, FileOptions.RandomAccess | FileOptions.Asynchronous);
                MemoryMappedFile? mmf = null;
                Chunk[]? chunks = null;
                try
                {
                    long length = fs.Length;
                    mmf = CreateMemoryMappedFile(fs, length);
                    chunks = MapChunks(mmf, 0, length, chunkSizePower);
                    return new SharedMapping(mmf, fs, chunks, length);
                }
                catch
                {
                    DisposeChunks(chunks);
                    mmf?.Dispose();
                    IOUtils.DisposeWhileHandlingException(fs);
                    throw;
                }
            }

            /// <summary>
            /// Attempt to increment the refcount. Returns false if the
            /// mapping has already been released (refcount was zero).
            /// </summary>
            internal bool TryAcquire()
            {
                while (true)
                {
                    int current = Volatile.Read(ref refCount);
                    if (current <= 0) return false;
                    if (Interlocked.CompareExchange(ref refCount, current + 1, current) == current)
                    {
                        return true;
                    }
                }
            }

            /// <summary>
            /// Decrement the refcount. Returns true when this call brought
            /// it to zero, meaning the caller owns disposal.
            /// </summary>
            internal bool Release()
            {
                return Interlocked.Decrement(ref refCount) == 0;
            }

            /// <summary>
            /// Dispose the underlying native resources. Must be called at
            /// most once, only by the caller whose Release returned true
            /// (or by the directory's own forced cleanup on Dispose).
            /// </summary>
            internal void DisposeResources()
            {
                DisposeChunks(Chunks);
                IOUtils.DisposeWhileHandlingException(memoryMappedFile, fileStream);
            }

            internal Chunk[] Chunks { get; }

            internal long Length { get; }

            private static MemoryMappedFile? CreateMemoryMappedFile(FileStream fc, long requiredCapacity)
            {
                if (requiredCapacity <= 0)
                {
                    return null;
                }

                // LUCENENET specific BEGIN: retry on capacity race (#1090).
                // MemoryMappedFile.CreateFromFile performs an internal stat
                // and throws ArgumentOutOfRangeException("capacity") if the
                // on-disk file size exceeds the requested capacity. When
                // another process/thread is appending to this file, the file
                // can grow between when we capture fc.Length and when
                // CreateFromFile reads the size.
                long capacity = Math.Max(requiredCapacity, fc.Length);
                const int maxAttempts = 5;
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        var mmf = MemoryMappedFile.CreateFromFile(
                            fileStream: fc,
                            mapName: null,
                            capacity: capacity,
                            access: MemoryMappedFileAccess.Read,
#if FEATURE_MEMORYMAPPEDFILESECURITY
                            memoryMappedFileSecurity: null,
#endif
                            inheritability: HandleInheritability.Inheritable,
                            leaveOpen: true); // We dispose the FileStream explicitly.
                        int attemptsTaken = attempt + 1;
                        int prior;
                        do
                        {
                            prior = Volatile.Read(ref s_maxCapacityAttemptsObserved);
                            if (attemptsTaken <= prior) break;
                        } while (Interlocked.CompareExchange(ref s_maxCapacityAttemptsObserved, attemptsTaken, prior) != prior);
                        return mmf;
                    }
                    catch (ArgumentOutOfRangeException e) when (e.ParamName == "capacity" && attempt < maxAttempts - 1)
                    {
                        Interlocked.Increment(ref s_capacityRetryCount);
                        capacity = Math.Max(capacity, fc.Length);
                        attempt++;
                    }
                }
                // LUCENENET specific END
            }

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
        /// Shared, refcounted owner of a single chunk's
        /// <see cref="MemoryMappedViewAccessor"/> + cached base pointer.
        /// One <see cref="Chunk"/> per chunk-sized region of the file; they
        /// live inside a <see cref="SharedMapping"/>.
        /// </summary>
        internal sealed unsafe class Chunk
        {
            private MemoryMappedViewAccessor? accessor;
            internal readonly long length;
            internal readonly byte* basePtr;
            private int closed;

            // Reader count / closed flag: a minimal drain barrier so that
            // Close waits for in-flight reads to finish before calling
            // ReleasePointer + disposing the accessor. Without this, a reader
            // that has already read closed==false can still be dereferencing
            // basePtr when the parent's Dispose unmaps the view.
            private int inFlight;

            public bool IsClosed
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Volatile.Read(ref closed) != 0;
            }

            internal Chunk(MemoryMappedViewAccessor accessor, byte* basePtr, long length)
            {
                this.accessor = accessor;
                this.basePtr = basePtr;
                this.length = length;
            }

            /// <summary>
            /// Enter a read. Returns false if the chunk is closed; the caller
            /// must throw <see cref="AlreadyClosedException"/>. Otherwise
            /// returns true and the caller must call <see cref="ExitRead"/> in
            /// a finally block.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryEnterRead()
            {
                Interlocked.Increment(ref inFlight);
                if (Volatile.Read(ref closed) != 0)
                {
                    Interlocked.Decrement(ref inFlight);
                    return false;
                }
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ExitRead()
            {
                Interlocked.Decrement(ref inFlight);
            }

            /// <summary>
            /// Mark the chunk closed and release its accessor, waiting for
            /// any in-flight reader to exit its copy first.
            /// </summary>
            public void Close()
            {
                if (Interlocked.CompareExchange(ref closed, 1, 0) != 0)
                {
                    return;
                }

                // Drain in-flight readers. Reads are bounded by the small
                // BufferedIndexInput buffer size (1 KiB / 4 KiB for merges),
                // so this spin is very short in practice.
                var spin = new SpinWait();
                while (Volatile.Read(ref inFlight) != 0)
                {
                    spin.SpinOnce();
                }

                if (accessor != null)
                {
                    try
                    {
                        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                    }
                    catch
                    {
                        // Guard against double-release in odd failure paths.
                        // Never propagate from dispose.
                    }
                    accessor.Dispose();
                    accessor = null;
                }
            }
        }
    }
}
