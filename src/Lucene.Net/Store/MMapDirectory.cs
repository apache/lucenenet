using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using SCG = System.Collections.Generic;

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

    using Constants = Lucene.Net.Util.Constants;

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

        /// <summary>
        /// Create a new <see cref="MMapDirectory"/> for the named location.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<see cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public MMapDirectory(DirectoryInfo path, LockFactory lockFactory)
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
        /// <b>LUCENENET note:</b> this parameter is retained for API compatibility
        /// but is effectively a no-op in the current implementation. Each file is
        /// mapped as a single <see cref="System.IO.MemoryMappedFiles.MemoryMappedViewAccessor"/>
        /// regardless of <paramref name="maxChunkSize"/>; see the class remarks for
        /// the rationale (issues #1013 and #1151). The value is still validated and
        /// retained so that <see cref="MaxChunkSize"/> reflects what the caller passed.
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
        public MMapDirectory(DirectoryInfo path, LockFactory lockFactory, int maxChunkSize)
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
        public MMapDirectory(string path, LockFactory lockFactory)
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
        /// <b>LUCENENET note:</b> this parameter is retained for API compatibility
        /// but is effectively a no-op in the current implementation. Each file is
        /// mapped as a single <see cref="System.IO.MemoryMappedFiles.MemoryMappedViewAccessor"/>
        /// regardless of <paramref name="maxChunkSize"/>; see the class remarks for
        /// the rationale (issues #1013 and #1151). The value is still validated and
        /// retained so that <see cref="MaxChunkSize"/> reflects what the caller passed.
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
        public MMapDirectory(string path, LockFactory lockFactory, int maxChunkSize)
            : this(new DirectoryInfo(path), lockFactory, maxChunkSize)
        {
        }

        // LUCENENET specific - Some JREs had a bug that didn't allow them to unmap.
        // But according to MSDN, the MemoryMappedFile.Dispose() method will
        // indeed "release all resources". Therefore unmap hack is not needed in .NET.

        /// <summary>
        /// Returns the current mmap chunk size.
        /// <para/>
        /// <b>LUCENENET note:</b> this value is retained for API compatibility
        /// but does not affect runtime behavior in the current implementation.
        /// Each file is mapped as a single
        /// <see cref="System.IO.MemoryMappedFiles.MemoryMappedViewAccessor"/>
        /// regardless of this setting; see the class remarks (issues #1013 and
        /// #1151) for the rationale.
        /// </summary>
        /// <seealso cref="MMapDirectory(DirectoryInfo, LockFactory, int)"/>
        public int MaxChunkSize => 1 << chunkSizePower;

        /// <summary>
        /// Creates an <see cref="IndexInput"/> for the file with the given name. </summary>
        public override IndexInput OpenInput(string name, IOContext context)
        {
            EnsureOpen();
            var file = Path.Combine(Directory.FullName, name); // LUCENENET specific: changed to use string file name instead of allocating a FileInfo (#832)
            var fc = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            // LUCENENET specific: the new MemoryMappedViewAccessorIndexInput
            // path replaces the upstream ByteBufferIndexInput-based approach.
            // See #1013 + #1151 and the class doc comment for rationale.
            return new MemoryMappedViewAccessorIndexInput("MMapIndexInput(path=\"" + file + "\")", fc, context);
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            var file = Path.Combine(Directory.FullName, name);
            var descriptor = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            // Cache the length eagerly so OpenFullSlice never has to touch
            // the FileStream — which would race with Dispose closing it.
            long descriptorLength = descriptor.Length;
            return new IndexInputSlicerAnonymousClass(this, context, file, descriptor, descriptorLength);
        }

        private sealed class IndexInputSlicerAnonymousClass : IndexInputSlicer
        {
            private readonly MMapDirectory outerInstance;
            private readonly IOContext context;
            private readonly string file;
            private readonly FileStream descriptor;
            private readonly long descriptorLength;
            private int disposed = 0; // LUCENENET specific - allow double-dispose
            // Track issued slices so that Dispose cascades. Lucene's
            // contract is that after slicer.Dispose, reads from any slice
            // (or clone of a slice) throw AlreadyClosedException.
            private readonly SCG.List<MemoryMappedViewAccessorIndexInput> issuedSlices
                = new SCG.List<MemoryMappedViewAccessorIndexInput>();
            private readonly object issuedSlicesLock = new object();

            public IndexInputSlicerAnonymousClass(MMapDirectory outerInstance, IOContext context, string file, FileStream descriptor, long descriptorLength)
            {
                this.outerInstance = outerInstance;
                this.context = context;
                this.file = file;
                this.descriptor = descriptor;
                this.descriptorLength = descriptorLength;
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                outerInstance.EnsureOpen();
                // Each slice gets its own FileStream + MemoryMappedFile +
                // MemoryMappedViewAccessor. Slices never share view handles,
                // which simplifies the concurrent-dispose story.
                var fc = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var input = new MemoryMappedViewAccessorIndexInput(
                    "MMapIndexInput(" + sliceDescription + " in path=\"" + file + "\" slice=" + offset + ":" + (offset + length) + ")",
                    fc, offset, length, BufferedIndexInput.GetBufferSize(context));
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
                // File length is captured eagerly in the slicer ctor, so we
                // never touch the descriptor FileStream here. OpenSlice does
                // its own disposed-check under the lock, which will throw
                // AlreadyClosed deterministically if we raced with Dispose.
                return OpenSlice("full-slice", 0, descriptorLength);
            }

            protected override void Dispose(bool disposing)
            {
                if (0 != Interlocked.CompareExchange(ref this.disposed, 1, 0)) return; // LUCENENET specific - allow double-dispose

                if (disposing)
                {
                    MemoryMappedViewAccessorIndexInput[] toDispose;
                    lock (issuedSlicesLock)
                    {
                        toDispose = issuedSlices.ToArray();
                        issuedSlices.Clear();
                    }
                    foreach (var slice in toDispose)
                    {
                        try { slice.Dispose(); } catch { /* continue disposing siblings */ }
                    }

                    // The descriptor FileStream is only used here to stat the
                    // file length for OpenFullSlice. It is not shared with
                    // the per-slice IndexInputs (those open their own).
                    descriptor.Dispose();
                }
            }
        }

        // LUCENENET specific: the upstream-shaped MMapIndexInput nested class
        // (which inherited from ByteBufferIndexInput) has been removed in
        // favor of MemoryMappedViewAccessorIndexInput. See that class's doc
        // comment and issues #1013 + #1151.
    }
}
