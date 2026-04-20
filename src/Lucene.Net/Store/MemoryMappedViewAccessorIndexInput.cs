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
    public sealed unsafe class MemoryMappedViewAccessorIndexInput : BufferedIndexInput
    {
        /// <summary>
        /// Shared, refcounted owner of a single chunk's
        /// <see cref="MemoryMappedViewAccessor"/> + cached base pointer.
        /// One <see cref="Chunk"/> per chunk-sized region of the file. The
        /// chunk array is shared by the root <see cref="IndexInput"/> returned
        /// from <c>OpenInput</c>/<c>OpenSlice</c> and every <see cref="Clone"/>
        /// derived from it, so that disposing the root closes all clones as
        /// Lucene's contract requires.
        /// </summary>
        internal sealed class Chunk
        {
            private MemoryMappedViewAccessor accessor;
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

        // Non-readonly so that Clone() can mark the cloned instance as a
        // non-root (see Clone()). The field is copied via MemberwiseClone
        // from the parent (true for the root, false for a clone-of-clone).
        private bool isRoot = true;

        // Per-instance closed flag: tracks whether THIS IndexInput
        // instance has been disposed. Independent of the per-chunk
        // closed flags, so that disposing a clone does not affect the root
        // or sibling clones.
        private int instanceClosed;

        // Shared chunk array + the MemoryMappedFile that owns them. All
        // clones share the same references; only the root disposes them.
        private readonly Chunk[] chunks;
        private readonly MemoryMappedFile memoryMappedFile;
        private readonly FileStream fc;

        private readonly long length;
        private readonly int chunkSizePower;
        private readonly long chunkSizeMask;

        /// <summary>
        /// Opens the entire file as a chunked memory-mapped view. Called from
        /// <see cref="MMapDirectory.OpenInput"/>.
        /// </summary>
        internal MemoryMappedViewAccessorIndexInput(string resourceDescription, FileStream fc, IOContext context, int chunkSizePower)
            : base(resourceDescription, context)
        {
            if (fc is null) throw new ArgumentNullException(nameof(fc));
            this.chunkSizePower = chunkSizePower;
            this.chunkSizeMask = (1L << chunkSizePower) - 1L;
            this.length = fc.Length;
            this.fc = fc;

            try
            {
                this.memoryMappedFile = CreateMemoryMappedFile(fc, this.length);
                this.chunks = MapChunks(this.memoryMappedFile, 0, this.length, chunkSizePower);
            }
            catch
            {
                // We took ownership of fc from the caller; on failure we
                // must dispose what we managed to allocate and the FileStream.
                DisposeChunks(this.chunks);
                this.memoryMappedFile?.Dispose();
                try { fc.Dispose(); } catch { /* never propagate from cleanup */ }
                throw;
            }
        }

        /// <summary>
        /// Opens a slice <c>[offset, offset+length)</c> of <paramref name="fc"/>
        /// as a chunked memory-mapped view. Each slice creates its own
        /// <see cref="MemoryMappedFile"/> + chunk array so slices never share
        /// view handles with one another.
        /// </summary>
        internal MemoryMappedViewAccessorIndexInput(string resourceDescription, FileStream fc,
            long offset, long length, int bufferSize, int chunkSizePower)
            : base(resourceDescription, bufferSize)
        {
            if (fc is null) throw new ArgumentNullException(nameof(fc));
            this.chunkSizePower = chunkSizePower;
            this.chunkSizeMask = (1L << chunkSizePower) - 1L;
            this.length = length;
            this.fc = fc;

            try
            {
                this.memoryMappedFile = CreateMemoryMappedFile(fc, offset + length);
                this.chunks = MapChunks(this.memoryMappedFile, offset, length, chunkSizePower);
            }
            catch
            {
                DisposeChunks(this.chunks);
                this.memoryMappedFile?.Dispose();
                try { fc.Dispose(); } catch { /* never propagate from cleanup */ }
                throw;
            }
        }

        public override long Length => length;

        private static MemoryMappedFile CreateMemoryMappedFile(FileStream fc, long requiredCapacity)
        {
            if (requiredCapacity == 0)
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
                        prior = Volatile.Read(ref MMapDirectory.s_maxCapacityAttemptsObserved);
                        if (attemptsTaken <= prior) break;
                    } while (Interlocked.CompareExchange(ref MMapDirectory.s_maxCapacityAttemptsObserved, attemptsTaken, prior) != prior);
                    return mmf;
                }
                catch (ArgumentOutOfRangeException e) when (e.ParamName == "capacity" && attempt < maxAttempts - 1)
                {
                    Interlocked.Increment(ref MMapDirectory.s_capacityRetryCount);
                    capacity = Math.Max(capacity, fc.Length);
                    attempt++;
                }
            }
            // LUCENENET specific END
        }

        private static Chunk[] MapChunks(MemoryMappedFile mmf, long offset, long length, int chunkSizePower)
        {
            if (length == 0)
            {
                return Array.Empty<Chunk>();
            }

            long chunkSize = 1L << chunkSizePower;
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

        private static void DisposeChunks(Chunk[] chunks)
        {
            if (chunks == null) return;
            foreach (var c in chunks)
            {
                try { c?.Close(); } catch { /* never propagate from cleanup */ }
            }
        }

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

            // Walk one chunk at a time; a single ReadInternal can span chunks.
            int chunkIdx = (int)(pos >> chunkSizePower);
            int chunkOff = (int)(pos & chunkSizeMask);
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
            // A clone may observe one of its chunks closed before its own
            // instanceClosed flag flips (the cascade from the root is not
            // atomic across all clones); surface that here too so a Seek
            // that won't be servable by a subsequent ReadInternal fails fast.
            if (chunks.Length > 0 && chunks[0].IsClosed)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
            }
        }

        public override object Clone()
        {
            // Share the same chunk array with the parent so that disposing
            // the parent (the root IndexInput returned from OpenInput/OpenSlice)
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
            // Only the root instance owns the shared chunks. Clones never
            // close shared chunks, so disposing a clone does not affect the
            // root or sibling clones. The root's Dispose cascades through
            // each chunk's closed flag which all clones observe.
            if (isRoot)
            {
                DisposeChunks(chunks);
                memoryMappedFile?.Dispose();
                try { fc?.Dispose(); } catch { /* never propagate from cleanup */ }
            }
        }
    }
}
