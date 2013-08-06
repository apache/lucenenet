/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;

namespace Lucene.Net.Store
{

    /// <summary>A straightforward implementation of <see cref="FSDirectory" />
    /// using java.io.RandomAccessFile.  However, this class has
    /// poor concurrent performance (multiple threads will
    /// bottleneck) as it synchronizes when multiple threads
    /// read from the same file.  It's usually better to use
    /// <see cref="NIOFSDirectory" /> or <see cref="MMapDirectory" /> instead. 
    /// </summary>
    public class SimpleFSDirectory : FSDirectory
    {
        /// <summary>Create a new SimpleFSDirectory for the named location.
        /// 
        /// </summary>
        /// <param name="path">the path of the directory
        /// </param>
        /// <param name="lockFactory">the lock factory to use, or null for the default.
        /// </param>
        /// <throws>  IOException </throws>
        public SimpleFSDirectory(DirectoryInfo path, LockFactory lockFactory)
            : base(path, lockFactory)
        {
        }

        /// <summary>Create a new SimpleFSDirectory for the named location and the default lock factory.
        /// 
        /// </summary>
        /// <param name="path">the path of the directory
        /// </param>
        /// <throws>  IOException </throws>
        public SimpleFSDirectory(DirectoryInfo path)
            : base(path, null)
        {
        }

        /// <summary>Creates an IndexInput for the file with the given name. </summary>
        public override IndexInput OpenInput(String name, IOContext context)
        {
            EnsureOpen();
            FileInfo path = new FileInfo(Path.Combine(directory.FullName, name));
            return new SimpleFSIndexInput("SimpleFSIndexInput(path=\"" + path.FullName + "\")", path, context, ReadChunkSize);
        }

        private sealed class AnonymousClassCreateSlicer : IndexInputSlicer
        {
            private FileStream descriptor;
            private readonly SimpleFSDirectory parent;
            private readonly FileInfo file;
            private readonly IOContext context;

            public AnonymousClassCreateSlicer(SimpleFSDirectory parent, FileInfo file, FileStream descriptor, IOContext context)
            {
                this.parent = parent;
                this.file = file;
                this.descriptor = descriptor;
                this.context = context;
            }

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    descriptor.Dispose();
                }

                descriptor = null;
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                return new SimpleFSIndexInput("SimpleFSIndexInput(" + sliceDescription + " in path=\"" + file.FullName + "\" slice=" + offset + ":" + (offset + length) + ")", descriptor, offset,
                    length, BufferedIndexInput.GetBufferSize(context), parent.ReadChunkSize);
            }

            public override IndexInput OpenFullSlice()
            {
                return OpenSlice("full-slice", 0, descriptor.Length);
            }
        }

        protected internal class SimpleFSIndexInput : FSIndexInput
        {
            public SimpleFSIndexInput(String resourceDesc, FileInfo path, IOContext context, int chunkSize)
                : base(resourceDesc, path, context, chunkSize)
            {
            }

            public SimpleFSIndexInput(String resourceDesc, FileStream file, long off, long length, int bufferSize, int chunkSize)
                : base(resourceDesc, file, off, length, bufferSize, chunkSize)
            {
            }

            public override void ReadInternal(byte[] b, int offset, int len)
            {
                lock (file)
                {
                    long position = off + FilePointer;
                    file.Seek(position, SeekOrigin.Begin);
                    int total = 0;

                    if (position + len > end)
                    {
                        throw new EndOfStreamException("read past EOF: " + this);
                    }

                    try
                    {
                        do
                        {
                            int readLength;
                            if (total + chunkSize > len)
                            {
                                readLength = len - total;
                            }
                            else
                            {
                                // LUCENE-1566 - work around JVM Bug by breaking very large reads into chunks
                                readLength = chunkSize;
                            }
                            int i = file.Read(b, offset + total, readLength);
                            total += i;
                        } while (total < len);
                    }
                    catch (OutOfMemoryException e)
                    {
                        // TODO: .NET port: needs different language here

                        // propagate OOM up and add a hint for 32bit VM Users hitting the bug
                        // with a large chunk size in the fast path.
                        OutOfMemoryException outOfMemoryError = new OutOfMemoryException(
                            "OutOfMemoryError likely caused by the Sun VM Bug described in "
                            + "https://issues.apache.org/jira/browse/LUCENE-1566; try calling FSDirectory.setReadChunkSize "
                            + "with a value smaller than the current chunk size (" + chunkSize + ")", e);
                        
                        throw outOfMemoryError;
                    }
                    catch (IOException ioe)
                    {
                        throw new IOException(ioe.Message + ": " + this, ioe);
                    }
                }
            }

            public override void SeekInternal(long pos)
            {
            }
        }
    }
}