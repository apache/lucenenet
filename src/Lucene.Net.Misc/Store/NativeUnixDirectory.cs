using Lucene.Net.Util;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
#if FEATURE_SUPPORTEDOSPLATFORMATTRIBUTE
using System.Runtime.Versioning;
#endif

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

    // TODO
    //   - newer Linux kernel versions (after 2.6.29) have
    //     improved MADV_SEQUENTIAL (and hopefully also
    //     FADV_SEQUENTIAL) interaction with the buffer
    //     cache; we should explore using that instead of direct
    //     IO when context is merge

    /// <summary>
    /// A <see cref="Directory"/> implementation for all Unixes that uses
    /// DIRECT I/O to bypass OS level IO caching during
    /// merging.  For all other cases (searching, writing) we delegate
    /// to the provided <see cref="Directory"/> instance.
    ///
    /// <para>LUCENENET specific: the original Lucene implementation reached the OS through a JNI
    /// shim (<c>NativePosixUtil.cpp</c>) plus Java NIO <c>FileChannel</c>s. This port replaces the
    /// native build step with direct P/Invoke into <c>libc</c> (see <see cref="NativePosixUtil"/>),
    /// so no native compilation is required, but it can only be used on Unix-like platforms
    /// (Linux and macOS); it is not supported on Microsoft Windows.</para>
    ///
    /// <para><b>WARNING</b>: this code is very new and quite easily
    /// could contain horrible bugs.  For example, here's one
    /// known issue: if you use seek in <see cref="IndexOutput"/>, and then
    /// write more than one buffer's worth of bytes, then the
    /// file will be wrong.  Lucene does not do this today (only writes
    /// small number of bytes after seek), but that may change.</para>
    ///
    /// <para>Direct I/O requires that reads and writes are aligned to the device block size
    /// (here <c>512</c> bytes) and that the underlying filesystem supports it; some filesystems
    /// (e.g. tmpfs) and overlay/virtual mounts do not.</para>
    ///
    /// @lucene.experimental
    /// </summary>
#if FEATURE_SUPPORTEDOSPLATFORMATTRIBUTE
    [UnsupportedOSPlatform("windows")]
#endif
    public class NativeUnixDirectory : FSDirectory
    {
        // TODO: this is OS dependent, but likely 512 is the LCD
        private const long ALIGN = 512;
        private const long ALIGN_NOT_MASK = ~(ALIGN - 1);

        /// <summary>
        /// Default buffer size before writing to disk (256 KB);
        /// larger means less IO load but more RAM and direct
        /// buffer storage space consumed during merging.
        /// </summary>
        public const int DEFAULT_MERGE_BUFFER_SIZE = 262144;

        /// <summary>
        /// Default min expected merge size before direct IO is
        /// used (10 MB):
        /// </summary>
        public const long DEFAULT_MIN_BYTES_DIRECT = 10 * 1024 * 1024;

        private readonly int mergeBufferSize;
        private readonly long minBytesDirect;
        private readonly Directory m_delegate;

        /// <summary>
        /// Create a new <see cref="NativeUnixDirectory"/> for the named location.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="mergeBufferSize"> Size of buffer to use for
        ///    merging.  See <see cref="DEFAULT_MERGE_BUFFER_SIZE"/>. </param>
        /// <param name="minBytesDirect"> Merges, or files to be opened for
        ///   reading, smaller than this will
        ///   not use direct IO.  See <see cref="DEFAULT_MIN_BYTES_DIRECT"/> </param>
        /// <param name="delegate"> fallback <see cref="Directory"/> for non-merges </param>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        /// <exception cref="PlatformNotSupportedException"> If running on Microsoft Windows </exception>
        public NativeUnixDirectory(DirectoryInfo path, int mergeBufferSize, long minBytesDirect, Directory @delegate)
            : base(path, @delegate.LockFactory)
        {
            EnsureUnix();
            if ((mergeBufferSize & ALIGN) != 0)
            {
                throw new ArgumentException("mergeBufferSize must be 0 mod " + ALIGN + " (got: " + mergeBufferSize + ")");
            }
            this.mergeBufferSize = mergeBufferSize;
            this.minBytesDirect = minBytesDirect;
            this.m_delegate = @delegate;
        }

        /// <summary>
        /// Create a new <see cref="NativeUnixDirectory"/> for the named location.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="delegate"> fallback <see cref="Directory"/> for non-merges </param>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        /// <exception cref="PlatformNotSupportedException"> If running on Microsoft Windows </exception>
        public NativeUnixDirectory(DirectoryInfo path, Directory @delegate)
            : this(path, DEFAULT_MERGE_BUFFER_SIZE, DEFAULT_MIN_BYTES_DIRECT, @delegate)
        {
        }

        /// <summary>
        /// Create a new <see cref="NativeUnixDirectory"/> for the named location.
        /// <para/>
        /// LUCENENET specific overload for convenience using string instead of <see cref="DirectoryInfo"/>.
        /// </summary>
        public NativeUnixDirectory(string path, int mergeBufferSize, long minBytesDirect, Directory @delegate)
            : this(new DirectoryInfo(path), mergeBufferSize, minBytesDirect, @delegate)
        {
        }

        /// <summary>
        /// Create a new <see cref="NativeUnixDirectory"/> for the named location.
        /// <para/>
        /// LUCENENET specific overload for convenience using string instead of <see cref="DirectoryInfo"/>.
        /// </summary>
        public NativeUnixDirectory(string path, Directory @delegate)
            : this(new DirectoryInfo(path), @delegate)
        {
        }

        private static void EnsureUnix()
        {
            if (Constants.WINDOWS)
            {
                throw new PlatformNotSupportedException(
                    $"{nameof(NativeUnixDirectory)} requires Linux or macOS direct I/O and is not supported on Microsoft Windows.");
            }
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            EnsureOpen();
            if (context.Context != IOContext.UsageContext.MERGE || context.MergeInfo.EstimatedMergeBytes < minBytesDirect || FileLength(name) < minBytesDirect)
            {
                return m_delegate.OpenInput(name, context);
            }
            else
            {
                return new NativeUnixIndexInput(Path.Combine(Directory.FullName, name), mergeBufferSize);
            }
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();
            if (context.Context != IOContext.UsageContext.MERGE || context.MergeInfo.EstimatedMergeBytes < minBytesDirect)
            {
                return m_delegate.CreateOutput(name, context);
            }
            else
            {
                EnsureCanWrite(name);
                return new NativeUnixIndexOutput(Path.Combine(Directory.FullName, name), mergeBufferSize);
            }
        }

        // ----- libc helpers for the operations the original code performed via FileChannel -----

        private static int Fd(SafeFileHandle handle) => (int)handle.DangerousGetHandle();

        private static void PWrite(SafeFileHandle fd, IntPtr buffer, int length, long pos)
        {
            long written = (long)NativeMethods.pwrite(Fd(fd), buffer, (nuint)length, pos);
            if (written < 0)
            {
                throw new IOException($"pwrite failed (errno {Marshal.GetLastWin32Error()})");
            }
        }

        private static void FTruncate(SafeFileHandle fd, long length)
        {
            if (NativeMethods.ftruncate(Fd(fd), length) < 0)
            {
                throw new IOException($"ftruncate failed (errno {Marshal.GetLastWin32Error()})");
            }
        }

        private static long FileSize(SafeFileHandle fd)
        {
            // The original used FileChannel.size(). All reads here are positioned (pread), so
            // moving the file offset with lseek(SEEK_END) to learn the size is harmless.
            long size = NativeMethods.lseek(Fd(fd), 0, NativeMethods.SEEK_END);
            if (size < 0)
            {
                throw new IOException($"lseek failed (errno {Marshal.GetLastWin32Error()})");
            }
            return size;
        }

        /// <summary>
        /// A small native, block-aligned buffer that mimics the subset of Java's direct
        /// <c>ByteBuffer</c> behavior used by the index input/output (position/limit, relative
        /// get/put, flip/clear/rewind). Direct I/O requires the buffer to be aligned to the
        /// device block size, which <see cref="Marshal.AllocHGlobal(int)"/> does not guarantee, so
        /// we over-allocate and align manually.
        /// </summary>
        private sealed unsafe class AlignedByteBuffer : IDisposable
        {
            private IntPtr basePtr;
            private readonly byte* alignedPtr;
            private readonly int capacity;
            private int position;
            private int limit;

            public AlignedByteBuffer(int capacity, int alignment)
            {
                this.capacity = capacity;
                basePtr = Marshal.AllocHGlobal(capacity + alignment - 1);
                long addr = basePtr.ToInt64();
                long aligned = (addr + alignment - 1) & ~((long)alignment - 1);
                alignedPtr = (byte*)aligned;
                Clear();
            }

            ~AlignedByteBuffer()
            {
                // Clones are never disposed by Lucene, so free native memory as a backstop.
                Free();
            }

            public int Capacity => capacity;
            public int Position { get => position; set => position = value; }
            public int Limit { get => limit; set => limit = value; }
            public IntPtr Pointer => (IntPtr)alignedPtr;

            public void Clear() { position = 0; limit = capacity; }
            public void Flip() { limit = position; position = 0; }
            public void Rewind() { position = 0; }

            public void Put(byte b) => alignedPtr[position++] = b;
            public byte Get() => alignedPtr[position++];

            public void Put(ReadOnlySpan<byte> src)
            {
                src.CopyTo(new Span<byte>(alignedPtr + position, src.Length));
                position += src.Length;
            }

            public void Get(Span<byte> dst)
            {
                new ReadOnlySpan<byte>(alignedPtr + position, dst.Length).CopyTo(dst);
                position += dst.Length;
            }

            private void Free()
            {
                if (basePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(basePtr);
                    basePtr = IntPtr.Zero;
                }
            }

            public void Dispose()
            {
                Free();
                GC.SuppressFinalize(this);
            }
        }

        private sealed class NativeUnixIndexOutput : IndexOutput
        {
            private readonly AlignedByteBuffer buffer;
            private readonly SafeFileHandle fd;
            private readonly int bufferSize;

            private int bufferPos;
            private long filePos;
            private long fileLength;
            private bool isOpen;

            public NativeUnixIndexOutput(string path, int bufferSize)
            {
                fd = NativePosixUtil.OpenDirect(path, read: false);
                buffer = new AlignedByteBuffer(bufferSize, (int)ALIGN);
                this.bufferSize = bufferSize;
                isOpen = true;
            }

            public override void WriteByte(byte b)
            {
                buffer.Put(b);
                if (++bufferPos == bufferSize)
                {
                    Dump();
                }
            }

            public override void WriteBytes(ReadOnlySpan<byte> source)
            {
                int offset = 0;
                int toWrite = source.Length;
                while (true)
                {
                    int left = bufferSize - bufferPos;
                    if (left <= toWrite)
                    {
                        buffer.Put(source.Slice(offset, left));
                        toWrite -= left;
                        offset += left;
                        bufferPos = bufferSize;
                        Dump();
                    }
                    else
                    {
                        buffer.Put(source.Slice(offset, toWrite));
                        bufferPos += toWrite;
                        break;
                    }
                }
            }

            public override void Flush()
            {
                // TODO -- I don't think this method is necessary?
            }

            private void Dump()
            {
                buffer.Flip();
                long limit = filePos + buffer.Limit;
                if (limit > fileLength)
                {
                    // this dump extends the file
                    fileLength = limit;
                }
                // else: we had seek'd back & wrote some changes

                // must always round to next block
                buffer.Limit = (int)((buffer.Limit + ALIGN - 1) & ALIGN_NOT_MASK);

                PWrite(fd, buffer.Pointer, buffer.Limit, filePos);
                filePos += bufferPos;
                bufferPos = 0;
                buffer.Clear();

                // TODO: the case where we'd seek'd back, wrote an
                // entire buffer, we must here read the next buffer;
                // likely Lucene won't trip on this since we only
                // write smallish amounts on seeking back
            }

            public override long Position => filePos + bufferPos;

            // TODO: seek is fragile at best; it can only properly
            // handle seek & then change bytes that fit entirely
            // within one buffer
            [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
            public override void Seek(long pos)
            {
                if (pos != Position)
                {
                    Dump();
                    long alignedPos = pos & ALIGN_NOT_MASK;
                    filePos = alignedPos;
                    int n = (int)NativePosixUtil.Pread(fd, filePos, buffer.Pointer, bufferSize);
                    if (n < bufferSize)
                    {
                        buffer.Limit = n;
                    }
                    int delta = (int)(pos - alignedPos);
                    buffer.Position = delta;
                    bufferPos = delta;
                }
            }

            public override long Length
            {
                get => fileLength + bufferPos;
                set { /* not supported: length is managed internally */ }
            }

            public override long Checksum => throw new NotSupportedException("this directory currently does not work at all!");

            protected override void Dispose(bool disposing)
            {
                if (isOpen)
                {
                    isOpen = false;
                    try
                    {
                        Dump();
                    }
                    finally
                    {
                        try
                        {
                            FTruncate(fd, fileLength);
                        }
                        finally
                        {
                            fd.Dispose();
                            buffer.Dispose();
                        }
                    }
                }
            }
        }

        private sealed class NativeUnixIndexInput : IndexInput
        {
            private readonly AlignedByteBuffer buffer;
            private readonly SafeFileHandle fd;        // owned descriptor; null for clones
            private readonly SafeFileHandle sharedFd;  // descriptor used for reads (own, or the original's)
            private readonly int bufferSize;

            private bool isOpen;
            private readonly bool isClone;
            private long filePos;
            private int bufferPos;

            public NativeUnixIndexInput(string path, int bufferSize)
                : base("NativeUnixIndexInput(path=\"" + path + "\")")
            {
                fd = NativePosixUtil.OpenDirect(path, read: true);
                sharedFd = fd;
                this.bufferSize = bufferSize;
                buffer = new AlignedByteBuffer(bufferSize, (int)ALIGN);
                isOpen = true;
                isClone = false;
                filePos = -bufferSize;
                bufferPos = bufferSize;
            }

            // for clone
            private NativeUnixIndexInput(NativeUnixIndexInput other)
                : base(other.ToString())
            {
                fd = null;
                sharedFd = other.sharedFd;
                bufferSize = other.bufferSize;
                buffer = new AlignedByteBuffer(bufferSize, (int)ALIGN);
                filePos = -bufferSize;
                bufferPos = bufferSize;
                isOpen = true;
                isClone = true;
                Seek(other.Position);
            }

            public override long Position => filePos + bufferPos;

            public override void Seek(long pos)
            {
                if (pos != Position)
                {
                    long alignedPos = pos & ALIGN_NOT_MASK;
                    filePos = alignedPos - bufferSize;

                    int delta = (int)(pos - alignedPos);
                    if (delta != 0)
                    {
                        Refill();
                        buffer.Position = delta;
                        bufferPos = delta;
                    }
                    else
                    {
                        // force refill on next read
                        bufferPos = bufferSize;
                    }
                }
            }

            public override long Length
            {
                get
                {
                    try
                    {
                        return FileSize(sharedFd);
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        throw RuntimeException.Create("IOException during Length: " + this, ioe);
                    }
                }
            }

            public override byte ReadByte()
            {
                // NOTE: we don't guard against EOF here... ie the
                // "final" buffer will typically be filled to less
                // than bufferSize
                if (bufferPos == bufferSize)
                {
                    Refill();
                }
                bufferPos++;
                return buffer.Get();
            }

            private void Refill()
            {
                buffer.Clear();
                filePos += bufferSize;
                bufferPos = 0;
                long n;
                try
                {
                    n = NativePosixUtil.Pread(sharedFd, filePos, buffer.Pointer, bufferSize);
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    throw new IOException(ioe.Message + ": " + this, ioe);
                }
                // Upstream used FileChannel.read(), which returns -1 at EOF. Here Pread() wraps libc
                // pread(), which returns 0 at EOF (a genuine error already threw above), so a refill that
                // reads nothing means we have stepped entirely past the end of the file.
                if (n <= 0)
                {
                    throw EOFException.Create("read past EOF: " + this);
                }
                buffer.Rewind();
            }

            public override void ReadBytes(Span<byte> destination)
            {
                int offset = 0;
                int toRead = destination.Length;
                while (true)
                {
                    int left = bufferSize - bufferPos;
                    if (left < toRead)
                    {
                        buffer.Get(destination.Slice(offset, left));
                        toRead -= left;
                        offset += left;
                        Refill();
                    }
                    else
                    {
                        buffer.Get(destination.Slice(offset, toRead));
                        bufferPos += toRead;
                        break;
                    }
                }
            }

            public override object Clone()
            {
                try
                {
                    return new NativeUnixIndexInput(this);
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    throw RuntimeException.Create("IOException during clone: " + this, ioe);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (isOpen && !isClone)
                {
                    isOpen = false;
                    fd.Dispose();
                }
                // each instance (including clones) owns its own native buffer
                buffer.Dispose();
            }
        }

        /// <summary>
        /// P/Invoke declarations for the <c>libc</c> functions that the original implementation
        /// reached through Java NIO <c>FileChannel</c>s (positioned write, truncate, size).
        /// </summary>
        private static class NativeMethods
        {
            private const string LIBC = "libc";

            internal const int SEEK_END = 2;

            [DllImport(LIBC, SetLastError = true)]
            internal static extern nint pwrite(int fd, IntPtr buf, nuint count, long offset);

            [DllImport(LIBC, SetLastError = true)]
            internal static extern int ftruncate(int fd, long length);

            [DllImport(LIBC, SetLastError = true)]
            internal static extern long lseek(int fd, long offset, int whence);
        }
    }
}
