using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System;
using System.IO;
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
    /// A straightforward implementation of <see cref="FSDirectory"/>
    /// using <see cref="FileStream"/>.
    /// <para/>
    /// <see cref="FSDirectory"/> is ideal for use cases where efficient
    /// writing is required without utilizing too much RAM. However, reading
    /// is less efficient than when using <see cref="MMapDirectory"/>.
    /// This class has poor concurrent read performance (multiple threads will
    /// bottleneck) as it synchronizes when multiple threads
    /// read from the same file. It's usually better to use
    /// <see cref="MMapDirectory"/> for reading.
    /// <para/>
    /// <font color="red"><b>NOTE:</b> Unlike in Java, it is not recommended to use
    /// <see cref="System.Threading.Thread.Interrupt()"/> in .NET
    /// in conjunction with an open <see cref="FSDirectory"/> because it is not guaranteed to exit atomically.
    /// Any <c>lock</c> statement or <see cref="System.Threading.Monitor.Enter(object)"/> call can throw a
    /// <see cref="System.Threading.ThreadInterruptedException"/>, which makes shutting down unpredictable.
    /// To exit parallel tasks safely, we recommend using <see cref="System.Threading.Tasks.Task"/>s
    /// and "interrupt" them with <see cref="System.Threading.CancellationToken"/>s.</font>
    /// </summary>
    public class SimpleFSDirectory : FSDirectory
    {
        /// <summary>
        /// Create a new <see cref="SimpleFSDirectory"/> for the named location.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<see cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public SimpleFSDirectory(DirectoryInfo path, LockFactory lockFactory)
            : base(path, lockFactory)
        {
        }

        /// <summary>
        /// Create a new <see cref="SimpleFSDirectory"/> for the named location and <see cref="NativeFSLockFactory"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public SimpleFSDirectory(DirectoryInfo path)
            : base(path, null)
        {
        }

        /// <summary>
        /// Create a new <see cref="SimpleFSDirectory"/> for the named location.
        /// <para/>
        /// LUCENENET specific overload for convenience using string instead of <see cref="DirectoryInfo"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<see cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public SimpleFSDirectory(string path, LockFactory lockFactory)
            : this(new DirectoryInfo(path), lockFactory)
        {
        }

        /// <summary>
        /// Create a new <see cref="SimpleFSDirectory"/> for the named location and <see cref="NativeFSLockFactory"/>.
        /// <para/>
        /// LUCENENET specific overload for convenience using string instead of <see cref="DirectoryInfo"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public SimpleFSDirectory(string path)
            : this(path, null)
        {
        }

        /// <summary>
        /// Creates an <see cref="IndexInput"/> for the file with the given name. </summary>
        public override IndexInput OpenInput(string name, IOContext context)
        {
            EnsureOpen();
            var path = new FileInfo(Path.Combine(Directory.FullName, name));
            var raf = new FileStream(path.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new SimpleFSIndexInput("SimpleFSIndexInput(path=\"" + path.FullName + "\")", raf, context);
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            var file = new FileInfo(Path.Combine(Directory.FullName, name));
            var descriptor = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new IndexInputSlicerAnonymousClass(context, file, descriptor);
        }

        private sealed class IndexInputSlicerAnonymousClass : IndexInputSlicer
        {
            private readonly IOContext context;
            private readonly FileInfo file;
            private readonly FileStream descriptor;
            private int disposed = 0; // LUCENENET specific - allow double-dispose

            public IndexInputSlicerAnonymousClass(IOContext context, FileInfo file, FileStream descriptor)
            {
                this.context = context;
                this.file = file;
                this.descriptor = descriptor;
            }

            protected override void Dispose(bool disposing)
            {
                if (0 != Interlocked.CompareExchange(ref this.disposed, 1, 0)) return; // LUCENENET specific - allow double-dispose

                if (disposing)
                {
                    descriptor.Dispose();
                }
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                return new SimpleFSIndexInput("SimpleFSIndexInput(" + sliceDescription + " in path=\"" + file.FullName + "\" slice=" + offset + ":" + (offset + length) + ")", descriptor, offset, length, BufferedIndexInput.GetBufferSize(context));
            }

            [Obsolete("Only for reading CFS files from 3.x indexes.")]
            public override IndexInput OpenFullSlice()
            {
                try
                {
                    return OpenSlice("full-slice", 0, descriptor.Length);
                }
                catch (Exception ex) when (ex.IsIOException())
                {
                    throw RuntimeException.Create(ex);
                }
            }
        }

        /// <summary>
        /// Reads bytes with <see cref="FileStream.Seek(long, SeekOrigin)"/> followed by
        /// <see cref="FileStream.Read(byte[], int, int)"/>.
        /// </summary>
        protected internal class SimpleFSIndexInput : BufferedIndexInput
        {
            private int disposed = 0; // LUCENENET specific - allow double-dispose

            // LUCENENET specific: chunk size not needed
            ///// <summary>
            ///// The maximum chunk size is 8192 bytes, because <seealso cref="RandomAccessFile"/> mallocs
            ///// a native buffer outside of stack if the read buffer size is larger.
            ///// </summary>
            //private const int CHUNK_SIZE = 8192;

            /// <summary>
            /// the file channel we will read from </summary>
            protected internal readonly FileStream m_file;

            /// <summary>
            /// is this instance a clone and hence does not own the file to close it </summary>
            public bool IsClone { get; set; }

            /// <summary>
            /// start offset: non-zero in the slice case </summary>
            protected internal readonly long m_off;

            /// <summary>
            /// end offset (start+length) </summary>
            protected internal readonly long m_end;

            public SimpleFSIndexInput(string resourceDesc, FileStream file, IOContext context)
                : base(resourceDesc, context)
            {
                this.m_file = file;
                this.m_off = 0L;
                this.m_end = file.Length;
                this.IsClone = false;
            }

            public SimpleFSIndexInput(string resourceDesc, FileStream file, long off, long length, int bufferSize)
                : base(resourceDesc, bufferSize)
            {
                this.m_file = file;
                this.m_off = off;
                this.m_end = off + length;
                this.IsClone = true;
            }

            protected override void Dispose(bool disposing)
            {
                if (0 != Interlocked.CompareExchange(ref this.disposed, 1, 0)) return; // LUCENENET specific - allow double-dispose

                if (disposing && !IsClone)
                {
                    m_file.Dispose();
                }
            }

            public override object Clone()
            {
                SimpleFSIndexInput clone = (SimpleFSIndexInput)base.Clone();
                clone.IsClone = true;
                return clone;
            }

            public override sealed long Length => m_end - m_off;

            /// <summary>
            /// <see cref="IndexInput"/> methods </summary>
            protected override void ReadInternal(byte[] b, int offset, int len)
            {
                UninterruptableMonitor.Enter(m_file);
                try
                {
                    long position = m_off + Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    m_file.Seek(position, SeekOrigin.Begin);
                    int total = 0;

                    if (position + len > m_end)
                    {
                        throw EOFException.Create("read past EOF: " + this);
                    }

                    try
                    {
                        //while (total < len)
                        //{
                        //    int toRead = Math.Min(CHUNK_SIZE, len - total);
                        //    int i = m_file.Read(b, offset + total, toRead);
                        //    if (i < 0) // be defensive here, even though we checked before hand, something could have changed
                        //    {
                        //        throw EOFException.Create("read past EOF: " + this + " off: " + offset + " len: " + len + " total: " + total + " chunkLen: " + toRead + " end: " + m_end);
                        //    }
                        //    if (Debugging.AssertsEnabled) Debugging.Assert(i > 0, "RandomAccessFile.read with non zero-length toRead must always read at least one byte");
                        //    total += i;
                        //}

                        // LUCENENET specific: FileStream is already optimized to read natively
                        // using the buffer size that is passed through its constructor. So,
                        // all we need to do is Read().
                        total = m_file.Read(b, offset, len);

                        if (Debugging.AssertsEnabled) Debugging.Assert(total == len);
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        throw new IOException(ioe.Message + ": " + this, ioe);
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(m_file);
                }
            }

            protected override void SeekInternal(long position)
            {
            }

            public virtual bool IsFDValid => m_file != null;
        }
    }
}