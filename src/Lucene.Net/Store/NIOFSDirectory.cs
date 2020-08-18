using J2N.IO;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.IO;
using System;
using System.IO;

namespace Lucene.Net.Store
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements. See the NOTICE file distributed with this
     * work for additional information regarding copyright ownership. The ASF
     * licenses this file to You under the Apache License, Version 2.0 (the
     * "License"); you may not use this file except in compliance with the License.
     * You may obtain a copy of the License at
     *
     * http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
     * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
     * License for the specific language governing permissions and limitations under
     * the License.
     */

    /// <summary>
    /// An <see cref="FSDirectory"/> implementation that uses <see cref="FileStream"/>'s
    /// positional read, which allows multiple threads to read from the same file
    /// without synchronizing.
    /// <para/>
    /// This class only uses <see cref="FileStream"/> when reading; writing is achieved with
    /// <see cref="FSDirectory.FSIndexOutput"/>.
    /// <para>
    /// <b>NOTE</b>: <see cref="NIOFSDirectory"/> is not recommended on Windows because of a bug in
    /// how FileChannel.read is implemented in Sun's JRE. Inside of the
    /// implementation the position is apparently synchronized. See <a
    /// href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6265734">here</a>
    /// for details.
    /// </para>
    /// <para>
    /// <font color="red"><b>NOTE:</b> Accessing this class either directly or
    /// indirectly from a thread while it's interrupted can close the
    /// underlying file descriptor immediately if at the same time the thread is
    /// blocked on IO. The file descriptor will remain closed and subsequent access
    /// to <see cref="NIOFSDirectory"/> will throw a <see cref="ObjectDisposedException"/>. If
    /// your application uses either <see cref="System.Threading.Thread.Interrupt()"/> or
    /// <see cref="System.Threading.Tasks.Task"/> you should use <see cref="SimpleFSDirectory"/> in
    /// favor of <see cref="NIOFSDirectory"/>.</font>
    /// </para>
    /// </summary>
    public class NIOFSDirectory : FSDirectory
    {
        /// <summary>
        /// Create a new <see cref="NIOFSDirectory"/> for the named location.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<see cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public NIOFSDirectory(DirectoryInfo path, LockFactory lockFactory)
            : base(path, lockFactory)
        {
        }

        /// <summary>
        /// Create a new <see cref="NIOFSDirectory"/> for the named location and <see cref="NativeFSLockFactory"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public NIOFSDirectory(DirectoryInfo path)
            : base(path, null)
        {
        }

        /// <summary>
        /// Create a new <see cref="NIOFSDirectory"/> for the named location.
        /// <para/>
        /// LUCENENET specific overload for convenience using string instead of <see cref="DirectoryInfo"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<see cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public NIOFSDirectory(string path, LockFactory lockFactory)
            : this(new DirectoryInfo(path), lockFactory)
        {
        }

        /// <summary>
        /// Create a new <see cref="NIOFSDirectory"/> for the named location and <see cref="NativeFSLockFactory"/>.
        /// <para/>
        /// LUCENENET specific overload for convenience using string instead of <see cref="DirectoryInfo"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public NIOFSDirectory(string path)
            : this(path, null)
        {
        }

        /// <summary>
        /// Creates an <see cref="IndexInput"/> for the file with the given name. </summary>
        public override IndexInput OpenInput(string name, IOContext context)
        {
            EnsureOpen();
            var path = new FileInfo(Path.Combine(Directory.FullName, name));
            var fc = new FileStream(path.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return new NIOFSIndexInput("NIOFSIndexInput(path=\"" + path + "\")", fc, context);
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            var path = new FileInfo(Path.Combine(Directory.FullName, name));
            var fc = new FileStream(path.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return new IndexInputSlicerAnonymousInnerClassHelper(context, path, fc);
        }

        private class IndexInputSlicerAnonymousInnerClassHelper : IndexInputSlicer
        {
            private readonly IOContext context;
            private readonly FileInfo path;
            private readonly FileStream descriptor;

            public IndexInputSlicerAnonymousInnerClassHelper(IOContext context, FileInfo path, FileStream descriptor)
            {
                this.context = context;
                this.path = path;
                this.descriptor = descriptor;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    descriptor.Dispose();
                }
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                return new NIOFSIndexInput("NIOFSIndexInput(" + sliceDescription + " in path=\"" + path + "\" slice=" + offset + ":" + (offset + length) + ")", descriptor, offset, length, 
                    BufferedIndexInput.GetBufferSize(context));
            }

            [Obsolete("Only for reading CFS files from 3.x indexes.")]
            public override IndexInput OpenFullSlice()
            {
                try
                {
                    return OpenSlice("full-slice", 0, descriptor.Length);
                }
                catch (IOException ex)
                {
                    throw new Exception(ex.ToString(), ex);
                }
            }
        }

        /// <summary>
        /// Reads bytes with the <see cref="StreamExtensions.Read(Stream, ByteBuffer, long)"/>
        /// extension method for <see cref="Stream"/>.
        /// </summary>
        protected class NIOFSIndexInput : BufferedIndexInput
        {
            /// <summary>
            /// The maximum chunk size for reads of 16384 bytes.
            /// </summary>
            private const int CHUNK_SIZE = 16384;

            /// <summary>
            /// the file channel we will read from </summary>
            protected readonly FileStream m_channel;

            /// <summary>
            /// is this instance a clone and hence does not own the file to close it </summary>
            internal bool isClone = false;

            /// <summary>
            /// start offset: non-zero in the slice case </summary>
            protected readonly long m_off;

            /// <summary>
            /// end offset (start+length) </summary>
            protected readonly long m_end;

            private ByteBuffer byteBuf; // wraps the buffer for NIO

            public NIOFSIndexInput(string resourceDesc, FileStream fc, IOContext context)
                : base(resourceDesc, context)
            {
                this.m_channel = fc;
                this.m_off = 0L;
                this.m_end = fc.Length;
            }

            public NIOFSIndexInput(string resourceDesc, FileStream fc, long off, long length, int bufferSize)
                : base(resourceDesc, bufferSize)
            {
                this.m_channel = fc;
                this.m_off = off;
                this.m_end = off + length;
                this.isClone = true;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !isClone)
                {
                    m_channel.Dispose();
                }
            }

            public override object Clone()
            {
                NIOFSIndexInput clone = (NIOFSIndexInput)base.Clone();
                clone.isClone = true;
                return clone;
            }

            public override sealed long Length => m_end - m_off;

            protected override void NewBuffer(byte[] newBuffer)
            {
                base.NewBuffer(newBuffer);
                byteBuf = ByteBuffer.Wrap(newBuffer);
            }

            protected override void ReadInternal(byte[] b, int offset, int len)
            {
                ByteBuffer bb;

                // Determine the ByteBuffer we should use
                if (b == m_buffer && 0 == offset)
                {
                    // Use our own pre-wrapped byteBuf:
                    if (Debugging.AssertsEnabled) Debugging.Assert(() => byteBuf != null);
                    byteBuf.Clear();
                    byteBuf.Limit = len;
                    bb = byteBuf;
                }
                else
                {
                    bb = ByteBuffer.Wrap(b, offset, len);
                }

                int readOffset = bb.Position;
                int readLength = bb.Limit - readOffset;
                long pos = GetFilePointer() + m_off;

                if (pos + len > m_end)
                {
                    throw new EndOfStreamException("read past EOF: " + this);
                }

                try
                {
                    while (readLength > 0)
                    {
                        int toRead = Math.Min(CHUNK_SIZE, readLength);
                        bb.Limit = readOffset + toRead;
                        if (Debugging.AssertsEnabled) Debugging.Assert(() => bb.Remaining == toRead);
                        int i = m_channel.Read(bb, pos);
                        if (i <= 0) // be defensive here, even though we checked before hand, something could have changed
                        {
                            throw new Exception("read past EOF: " + this + " off: " + offset + " len: " + len + " pos: " + pos + " chunkLen: " + readLength + " end: " + m_end);
                        }
                        pos += i;
                        readOffset += i;
                        readLength -= i;
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(() => readLength == 0);
                }
                catch (IOException ioe)
                {
                    throw new IOException(ioe.ToString() + ": " + this, ioe);
                }
            }

            protected override void SeekInternal(long pos)
            {
            }
        }
    }
}