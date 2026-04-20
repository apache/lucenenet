using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Threading;

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
    /// LUCENENET-specific <see cref="IndexInput"/> that reads from a
    /// <see cref="MemoryMappedViewAccessor"/> via a cached raw pointer obtained
    /// once per instance from
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
    ///     A shared closed-flag checked on each refill observes in-flight
    ///     Dispose so clones throw <see cref="AlreadyClosedException"/>
    ///     instead of reading stale bytes.
    ///   </description></item>
    ///   <item><description>
    ///     <b>#1151</b> — MMapDirectory 37–51× slower than
    ///     SimpleFSDirectory under parallel load. Every
    ///     <c>MemoryMappedViewAccessor.ReadByte</c>/<c>ReadArray</c> call does
    ///     its own <c>AcquirePointer</c>/<c>ReleasePointer</c> + range check;
    ///     that per-call overhead is the hot spot in LZ4 decompression and
    ///     other one-byte-at-a-time readers. Caching the pointer once and
    ///     using <c>Unsafe.CopyBlockUnaligned</c> eliminates that overhead.
    ///   </description></item>
    /// </list>
    ///
    /// <para/>
    /// The class inherits from <see cref="BufferedIndexInput"/> so that
    /// <c>ReadByte</c>, <c>ReadInt16</c>, <c>ReadVInt32</c> etc. are all
    /// served from the small managed buffer after a single bulk refill from
    /// the mapped region.
    /// </summary>
    public sealed unsafe class MemoryMappedViewAccessorIndexInput : BufferedIndexInput
    {
        /// <summary>
        /// Shared, refcounted owner of the <see cref="MemoryMappedFile"/> +
        /// <see cref="MemoryMappedViewAccessor"/> + cached base pointer. A
        /// single instance is shared by the root <see cref="IndexInput"/>
        /// returned from <c>OpenInput</c>/<c>OpenSlice</c> and every
        /// <see cref="Clone"/> derived from it, so that disposing the root
        /// closes all clones as Lucene's contract requires.
        /// </summary>
        private sealed class View
        {
            private MemoryMappedFile memoryMappedFile;
            private MemoryMappedViewAccessor accessor;
            private FileStream fc;
            internal readonly long length;
            internal readonly byte* basePtr;
            private int closed;

            // Reader count / closed flag: a minimal drain barrier so that
            // Dispose waits for in-flight reads to finish before calling
            // ReleasePointer + disposing the accessor. Without this, a reader
            // that has already read Closed==false can still be dereferencing
            // BasePtr when the parent's Dispose unmaps the view.
            private int inFlight;

            public bool IsClosed
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Volatile.Read(ref closed) != 0;
            }

            public View(FileStream fc, long offset, long length)
            {
                this.fc = fc;
                this.length = length;

                if (length == 0)
                {
                    return;
                }

                // LUCENENET specific BEGIN: retry on capacity race (#1090).
                // MemoryMappedFile.CreateFromFile performs an internal stat
                // and throws ArgumentOutOfRangeException("capacity") if the
                // on-disk file size exceeds the requested capacity. When
                // another process/thread is appending to this file, the file
                // can grow between when we capture fc.Length and when
                // CreateFromFile reads the size.
                long capacity = Math.Max(offset + length, fc.Length);
                const int maxAttempts = 5;
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        this.memoryMappedFile = MemoryMappedFile.CreateFromFile(
                            fileStream: fc,
                            mapName: null,
                            capacity: capacity,
                            access: MemoryMappedFileAccess.Read,
#if FEATURE_MEMORYMAPPEDFILESECURITY
                            memoryMappedFileSecurity: null,
#endif
                            inheritability: HandleInheritability.Inheritable,
                            leaveOpen: true); // We dispose the FileStream explicitly.
                        break;
                    }
                    catch (ArgumentOutOfRangeException e) when (e.ParamName == "capacity" && attempt < maxAttempts - 1)
                    {
                        Interlocked.Increment(ref MMapDirectory.s_capacityRetryCount);
                        capacity = Math.Max(capacity, fc.Length);
                        attempt++;
                    }
                }
                int attemptsTaken = attempt + 1;
                int prior;
                do
                {
                    prior = Volatile.Read(ref MMapDirectory.s_maxCapacityAttemptsObserved);
                    if (attemptsTaken <= prior) break;
                } while (Interlocked.CompareExchange(ref MMapDirectory.s_maxCapacityAttemptsObserved, attemptsTaken, prior) != prior);
                // LUCENENET specific END

                this.accessor = memoryMappedFile.CreateViewAccessor(offset, length, MemoryMappedFileAccess.Read);

                byte* ptr = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                // The accessor may be mapped at an offset inside the OS page,
                // in which case PointerOffset is the distance from the
                // SafeBuffer's base to the first byte of the requested view.
                this.basePtr = ptr + accessor.PointerOffset;
            }

            /// <summary>
            /// Enter a read. Returns false if the view is closed; the caller
            /// must throw AlreadyClosed. Otherwise returns true and the
            /// caller must call <see cref="ExitRead"/> in a finally block.
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
            /// Mark the view closed and release all resources, waiting for
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

                try
                {
                    if (accessor != null)
                    {
                        try
                        {
                            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                        catch
                        {
                            // Guard against double-release in odd failure
                            // paths. Never propagate from dispose.
                        }
                        accessor.Dispose();
                        accessor = null;
                    }
                }
                finally
                {
                    try
                    {
                        memoryMappedFile?.Dispose();
                        memoryMappedFile = null;
                    }
                    finally
                    {
                        fc?.Dispose();
                        fc = null;
                    }
                }
            }
        }

        // Non-readonly so that Clone() can mark the cloned instance as a
        // non-root (see Clone()). The field is copied via MemberwiseClone
        // from the parent (true for the root, false for a clone-of-clone).
        private bool isRoot = true;

        // Per-instance closed flag: tracks whether THIS IndexInput
        // instance has been disposed. Independent of the shared View's
        // closed flag, so that disposing a clone does not affect the root
        // or sibling clones.
        private int instanceClosed;

        private readonly View view;

        /// <summary>
        /// Opens the entire file as a new memory-mapped view. Called from
        /// <see cref="MMapDirectory.OpenInput"/>.
        /// </summary>
        internal MemoryMappedViewAccessorIndexInput(string resourceDescription, FileStream fc, IOContext context)
            : base(resourceDescription, context)
        {
            if (fc is null) throw new ArgumentNullException(nameof(fc));
            this.view = new View(fc, 0, fc.Length);
        }

        /// <summary>
        /// Opens a slice of <paramref name="fc"/> as an independent memory-mapped
        /// view. Each slice creates its own <see cref="MemoryMappedFile"/>/<see cref="MemoryMappedViewAccessor"/>
        /// so slices never share view handles with one another.
        /// </summary>
        internal MemoryMappedViewAccessorIndexInput(string resourceDescription, FileStream fc,
            long offset, long length, int bufferSize)
            : base(resourceDescription, bufferSize)
        {
            if (fc is null) throw new ArgumentNullException(nameof(fc));
            this.view = new View(fc, offset, length);
        }

        public override long Length => view.length;

        protected override void ReadInternal(Span<byte> destination)
        {
            if (Volatile.Read(ref instanceClosed) != 0)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
            }
            if (!view.TryEnterRead())
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
            }
            try
            {
                if (view.basePtr == null)
                {
                    if (destination.Length == 0)
                    {
                        return;
                    }
                    throw EOFException.Create("read past EOF: " + this);
                }

                long pos = Position;
                int len = destination.Length;
                if (pos + len > view.length)
                {
                    throw EOFException.Create("read past EOF: " + this);
                }

                ref byte src = ref Unsafe.AsRef<byte>(view.basePtr + pos);
                ref byte dst = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(destination);
                Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)len);
            }
            finally
            {
                view.ExitRead();
            }
        }

        protected override void SeekInternal(long pos)
        {
            if (pos < 0 || pos > view.length)
            {
                throw new IOException("Seek position is out of bounds: " + pos);
            }
            if (Volatile.Read(ref instanceClosed) != 0 || view.IsClosed)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
            }
        }

        public override object Clone()
        {
            // Share the same View with the parent so that disposing the
            // parent (the root IndexInput returned from OpenInput/OpenSlice)
            // closes this clone too, matching Lucene's contract.
            var clone = (MemoryMappedViewAccessorIndexInput)base.Clone();
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
            // Only the root instance owns the shared View. Clones never
            // close the shared View, so disposing a clone does not affect
            // the root or sibling clones. The root's Dispose cascades
            // through the View's closed flag which all clones observe.
            if (isRoot)
            {
                view.Close();
            }
        }
    }
}
