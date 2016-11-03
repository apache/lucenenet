using System;
using System.Diagnostics;
using System.IO;

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
    /// A straightforward implementation of <seealso cref="FSDirectory"/>
    ///  using java.io.RandomAccessFile.  However, this class has
    ///  poor concurrent performance (multiple threads will
    ///  bottleneck) as it synchronizes when multiple threads
    ///  read from the same file.  It's usually better to use
    ///  <seealso cref="NIOFSDirectory"/> or <seealso cref="MMapDirectory"/> instead.
    /// </summary>
    public class SimpleFSDirectory : FSDirectory
    {
        /// <summary>
        /// Create a new SimpleFSDirectory for the named location.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<seealso cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="System.IO.IOException"> if there is a low-level I/O error </exception>
        public SimpleFSDirectory(DirectoryInfo path, LockFactory lockFactory)
            : base(path, lockFactory)
        {
        }

        /// <summary>
        /// Create a new SimpleFSDirectory for the named location and <seealso cref="NativeFSLockFactory"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <exception cref="System.IO.IOException"> if there is a low-level I/O error </exception>
        public SimpleFSDirectory(DirectoryInfo path)
            : base(path, null)
        {
        }

        /// <summary>
        /// Creates an IndexInput for the file with the given name. </summary>
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
            return new IndexInputSlicerAnonymousInnerClassHelper(this, context, file, descriptor);
        }

        private class IndexInputSlicerAnonymousInnerClassHelper : IndexInputSlicer
        {
            private readonly IOContext Context;
            private readonly FileInfo File;
            private readonly FileStream Descriptor;

            public IndexInputSlicerAnonymousInnerClassHelper(SimpleFSDirectory outerInstance, IOContext context, FileInfo file, FileStream descriptor)
                : base(outerInstance)
            {
                this.Context = context;
                this.File = file;
                this.Descriptor = descriptor;
            }

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Descriptor.Dispose();
                }
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                return new SimpleFSIndexInput("SimpleFSIndexInput(" + sliceDescription + " in path=\"" + File.FullName + "\" slice=" + offset + ":" + (offset + length) + ")", Descriptor, offset, length, BufferedIndexInput.BufferSize(Context));
            }

            [Obsolete]
            public override IndexInput OpenFullSlice()
            {
                try
                {
                    return OpenSlice("full-slice", 0, Descriptor.Length);
                }
                catch (System.IO.IOException ex)
                {
                    throw new Exception(ex.ToString(), ex);
                }
            }
        }

        /// <summary>
        /// Reads bytes with <seealso cref="RandomAccessFile#seek(long)"/> followed by
        /// <seealso cref="RandomAccessFile#read(byte[], int, int)"/>.
        /// </summary>
        public class SimpleFSIndexInput : BufferedIndexInput
        {
            /// <summary>
            /// The maximum chunk size is 8192 bytes, because <seealso cref="RandomAccessFile"/> mallocs
            /// a native buffer outside of stack if the read buffer size is larger.
            /// </summary>
            internal const int CHUNK_SIZE = 8192;

            /// <summary>
            /// the file channel we will read from </summary>
            protected internal readonly FileStream File;

            /// <summary>
            /// is this instance a clone and hence does not own the file to close it </summary>
            public bool IsClone = false;

            /// <summary>
            /// start offset: non-zero in the slice case </summary>
            protected internal readonly long Off;

            /// <summary>
            /// end offset (start+length) </summary>
            protected internal readonly long End;

            public SimpleFSIndexInput(string resourceDesc, FileStream file, IOContext context)
                : base(resourceDesc, context)
            {
                this.File = file;
                this.Off = 0L;
                this.End = file.Length;
            }

            public SimpleFSIndexInput(string resourceDesc, FileStream file, long off, long length, int bufferSize)
                : base(resourceDesc, bufferSize)
            {
                this.File = file;
                this.Off = off;
                this.End = off + length;
                this.IsClone = true;
            }

            public override void Dispose()
            {
                if (!IsClone)
                {
                    File.Dispose();
                }
            }

            public override object Clone()
            {
                SimpleFSIndexInput clone = (SimpleFSIndexInput)base.Clone();
                clone.IsClone = true;
                return clone;
            }

            public override sealed long Length()
            {
                return End - Off;
            }

            /// <summary>
            /// IndexInput methods </summary>
            protected internal override void ReadInternal(byte[] b, int offset, int len)
            {
                lock (File)
                {
                    long position = Off + FilePointer;
                    File.Seek(position, SeekOrigin.Begin);
                    int total = 0;

                    if (position + len > End)
                    {
                        throw new EndOfStreamException("read past EOF: " + this);
                    }

                    try
                    {
                        while (total < len)
                        {
                            int toRead = Math.Min(CHUNK_SIZE, len - total);
                            int i = File.Read(b, offset + total, toRead);
                            if (i < 0) // be defensive here, even though we checked before hand, something could have changed
                            {
                                throw new EndOfStreamException("read past EOF: " + this + " off: " + offset + " len: " + len + " total: " + total + " chunkLen: " + toRead + " end: " + End);
                            }
                            Debug.Assert(i > 0, "RandomAccessFile.read with non zero-length toRead must always read at least one byte");
                            total += i;
                        }
                        Debug.Assert(total == len);
                    }
                    catch (System.IO.IOException ioe)
                    {
                        throw new System.IO.IOException(ioe.Message + ": " + this, ioe);
                    }
                }
            }

            protected override void SeekInternal(long position)
            {
            }

            public virtual bool FDValid
            {
                get
                {
                    return File != null;// File.FD.valid();
                }
            }
        }
    }
}