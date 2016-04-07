using System;
using System.Diagnostics;

namespace Lucene.Net.Store
{
    using Lucene.Net.Support;
    using System.IO;
    using System.IO.MemoryMappedFiles;

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
    /// File-based <seealso cref="Directory"/> implementation that uses
    ///  mmap for reading, and {@link
    ///  FSDirectory.FSIndexOutput} for writing.
    ///
    /// <p><b>NOTE</b>: memory mapping uses up a portion of the
    /// virtual memory address space in your process equal to the
    /// size of the file being mapped.  Before using this class,
    /// be sure your have plenty of virtual address space, e.g. by
    /// using a 64 bit JRE, or a 32 bit JRE with indexes that are
    /// guaranteed to fit within the address space.
    /// On 32 bit platforms also consult <seealso cref="#MMapDirectory(File, LockFactory, int)"/>
    /// if you have problems with mmap failing because of fragmented
    /// address space. If you get an OutOfMemoryException, it is recommended
    /// to reduce the chunk size, until it works.
    ///
    /// <p>Due to <a href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4724038">
    /// this bug</a> in Sun's JRE, MMapDirectory's <seealso cref="IndexInput#close"/>
    /// is unable to close the underlying OS file handle.  Only when GC
    /// finally collects the underlying objects, which could be quite
    /// some time later, will the file handle be closed.
    ///
    /// <p>this will consume additional transient disk usage: on Windows,
    /// attempts to delete or overwrite the files will result in an
    /// exception; on other platforms, which typically have a &quot;delete on
    /// last close&quot; semantics, while such operations will succeed, the bytes
    /// are still consuming space on disk.  For many applications this
    /// limitation is not a problem (e.g. if you have plenty of disk space,
    /// and you don't rely on overwriting files on Windows) but it's still
    /// an important limitation to be aware of.
    ///
    /// <p>this class supplies the workaround mentioned in the bug report
    /// (see <seealso cref="#setUseUnmap"/>), which may fail on
    /// non-Sun JVMs. It forcefully unmaps the buffer on close by using
    /// an undocumented internal cleanup functionality.
    /// <seealso cref="#UNMAP_SUPPORTED"/> is <code>true</code>, if the workaround
    /// can be enabled (with no guarantees).
    /// <p>
    /// <b>NOTE:</b> Accessing this class either directly or
    /// indirectly from a thread while it's interrupted can close the
    /// underlying channel immediately if at the same time the thread is
    /// blocked on IO. The channel will remain closed and subsequent access
    /// to <seealso cref="MMapDirectory"/> will throw a <seealso cref="ClosedChannelException"/>.
    /// </p>
    /// </summary>
    public class MMapDirectory : FSDirectory
    {
        private bool UseUnmapHack = UNMAP_SUPPORTED;

        /// <summary>
        /// Default max chunk size. </summary>
        /// <seealso cref= #MMapDirectory(File, LockFactory, int) </seealso>
        public static readonly int DEFAULT_MAX_BUFF = Constants.JRE_IS_64BIT ? (1 << 30) : (1 << 28);

        internal readonly int ChunkSizePower;

        /// <summary>
        /// Create a new MMapDirectory for the named location.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<seealso cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="System.IO.IOException"> if there is a low-level I/O error </exception>
        public MMapDirectory(DirectoryInfo path, LockFactory lockFactory)
            : this(path, lockFactory, DEFAULT_MAX_BUFF)
        {
        }

        /// <summary>
        /// Create a new MMapDirectory for the named location and <seealso cref="NativeFSLockFactory"/>.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <exception cref="System.IO.IOException"> if there is a low-level I/O error </exception>
        public MMapDirectory(DirectoryInfo path)
            : this(path, null)
        {
        }

        /// <summary>
        /// Create a new MMapDirectory for the named location, specifying the
        /// maximum chunk size used for memory mapping.
        /// </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<seealso cref="NativeFSLockFactory"/>); </param>
        /// <param name="maxChunkSize"> maximum chunk size (default is 1 GiBytes for
        /// 64 bit JVMs and 256 MiBytes for 32 bit JVMs) used for memory mapping.
        /// <p>
        /// Especially on 32 bit platform, the address space can be very fragmented,
        /// so large index files cannot be mapped. Using a lower chunk size makes
        /// the directory implementation a little bit slower (as the correct chunk
        /// may be resolved on lots of seeks) but the chance is higher that mmap
        /// does not fail. On 64 bit Java platforms, this parameter should always
        /// be {@code 1 << 30}, as the address space is big enough.
        /// <p>
        /// <b>Please note:</b> The chunk size is always rounded down to a power of 2. </param>
        /// <exception cref="System.IO.IOException"> if there is a low-level I/O error </exception>
        public MMapDirectory(DirectoryInfo path, LockFactory lockFactory, int maxChunkSize)
            : base(path, lockFactory)
        {
            if (maxChunkSize <= 0)
            {
                throw new System.ArgumentException("Maximum chunk size for mmap must be >0");
            }
            this.ChunkSizePower = 31 - Number.NumberOfLeadingZeros(maxChunkSize);
            Debug.Assert(this.ChunkSizePower >= 0 && this.ChunkSizePower <= 30);
        }

        /// <summary>
        /// <code>true</code>, if this platform supports unmapping mmapped files.
        /// </summary>
        public static readonly bool UNMAP_SUPPORTED;

        /*static MMapDirectory()
        {
            bool v;
            try
            {
                Type.GetType("sun.misc.Cleaner");
                Type.GetType("java.nio.DirectByteBuffer").GetMethod("cleaner");
                v = true;
            }
            catch (Exception)
            {
                v = false;
            }
            UNMAP_SUPPORTED = v;
        }*/

        /// <summary>
        /// this method enables the workaround for unmapping the buffers
        /// from address space after closing <seealso cref="IndexInput"/>, that is
        /// mentioned in the bug report. this hack may fail on non-Sun JVMs.
        /// It forcefully unmaps the buffer on close by using
        /// an undocumented internal cleanup functionality.
        /// <p><b>NOTE:</b> Enabling this is completely unsupported
        /// by Java and may lead to JVM crashes if <code>IndexInput</code>
        /// is closed while another thread is still accessing it (SIGSEGV). </summary>
        /// <exception cref="IllegalArgumentException"> if <seealso cref="#UNMAP_SUPPORTED"/>
        /// is <code>false</code> and the workaround cannot be enabled. </exception>
        public virtual bool UseUnmap
        {
            set
            {
                if (value && !UNMAP_SUPPORTED)
                {
                    throw new System.ArgumentException("Unmap hack not supported on this platform!");
                }
                this.UseUnmapHack = value;
            }
            get
            {
                return UseUnmapHack;
            }
        }

        /// <summary>
        /// Returns the current mmap chunk size. </summary>
        /// <seealso cref= #MMapDirectory(File, LockFactory, int) </seealso>
        public int MaxChunkSize
        {
            get
            {
                return 1 << ChunkSizePower;
            }
        }

        /// <summary>
        /// Creates an IndexInput for the file with the given name. </summary>
        public override IndexInput OpenInput(string name, IOContext context)
        {
            EnsureOpen();
            var file = new FileInfo(Path.Combine(Directory.FullName, name));

            var c = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            return new MMapIndexInput(this, "MMapIndexInput(path=\"" + file + "\")", c);
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            var full = (MMapIndexInput)OpenInput(name, context);

            return new IndexInputSlicerAnonymousInnerClassHelper(this, full);
        }

        private class IndexInputSlicerAnonymousInnerClassHelper : IndexInputSlicer
        {
            private readonly MMapDirectory OuterInstance;

            private MMapIndexInput Full;

            public IndexInputSlicerAnonymousInnerClassHelper(MMapDirectory outerInstance, MMapIndexInput full)
                : base(outerInstance)
            {
                this.OuterInstance = outerInstance;
                this.Full = full;
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                OuterInstance.EnsureOpen();
                return Full.Slice(sliceDescription, offset, length);
            }

            public override IndexInput OpenFullSlice()
            {
                OuterInstance.EnsureOpen();
                return (IndexInput)Full.Clone();
            }

            public override void Dispose(bool disposing)
            {
                Full.Dispose();
            }
        }

        public sealed class MMapIndexInput : ByteBufferIndexInput
        {
            internal readonly bool UseUnmapHack;
            internal MemoryMappedFile memoryMappedFile; // .NET port: this is equivalent to FileChannel.map
            internal MMapDirectory outerInstance;

            internal MMapIndexInput(MMapDirectory outerInstance, string resourceDescription, FileStream fc)
                : base(resourceDescription, null, fc.Length, outerInstance.ChunkSizePower, outerInstance.UseUnmap)
            {
                this.outerInstance = outerInstance;
                this.UseUnmapHack = outerInstance.UseUnmap;
                this.Buffers = outerInstance.Map(this, fc, 0, fc.Length);

                //Called here to let buffers get set up
                base.Seek(0L);
            }

            public override sealed void Dispose()
            {
                if (null != this.memoryMappedFile)
                {
                    this.memoryMappedFile.Dispose();
                    this.memoryMappedFile = null;
                }
                base.Dispose();
            }

            /// <summary>
            /// Try to unmap the buffer, this method silently fails if no support
            /// for that in the JVM. On Windows, this leads to the fact,
            /// that mmapped files cannot be modified or deleted.
            /// </summary>
            protected internal override void FreeBuffer(ByteBuffer buffer)
            {
                // .NET port: this should free the memory mapped view accessor
                var mmfbb = buffer as MemoryMappedFileByteBuffer;

                if (mmfbb != null)
                    mmfbb.Dispose();
                /*
              if (UseUnmapHack)
              {
                try
                {
                  AccessController.doPrivileged(new PrivilegedExceptionActionAnonymousInnerClassHelper(this, buffer));
                }
                catch (PrivilegedActionException e)
                {
                  System.IO.IOException ioe = new System.IO.IOException("unable to unmap the mapped buffer");
                  ioe.initCause(e.InnerException);
                  throw ioe;
                }
              }*/
            }

            /*
          private class PrivilegedExceptionActionAnonymousInnerClassHelper : PrivilegedExceptionAction<Void>
          {
              private readonly MMapIndexInput OuterInstance;

              private ByteBuffer Buffer;

              public PrivilegedExceptionActionAnonymousInnerClassHelper(MMapIndexInput outerInstance, ByteBuffer buffer)
              {
                  this.OuterInstance = outerInstance;
                  this.Buffer = buffer;
              }

              public override void Run()
              {
                Method getCleanerMethod = Buffer.GetType().GetMethod("cleaner");
                getCleanerMethod.Accessible = true;
                object cleaner = getCleanerMethod.invoke(Buffer);
                if (cleaner != null)
                {
                  cleaner.GetType().GetMethod("clean").invoke(cleaner);
                }
                //return null;
              }
          }*/
        }

        /// <summary>
        /// Maps a file into a set of buffers </summary>
        internal virtual ByteBuffer[] Map(MMapIndexInput input, FileStream fc, long offset, long length)
        {
            if (Number.URShift(length, ChunkSizePower) >= int.MaxValue)
                throw new ArgumentException("RandomAccessFile too big for chunk size: " + fc.ToString());

            long chunkSize = 1L << ChunkSizePower;

            // we always allocate one more buffer, the last one may be a 0 byte one
            int nrBuffers = (int)((long)((ulong)length >> ChunkSizePower)) + 1;

            ByteBuffer[] buffers = new ByteBuffer[nrBuffers];

            /*
             public static MemoryMappedFile CreateFromFile(FileStream fileStream, String mapName, Int64 capacity,
                                                        MemoryMappedFileAccess access, MemoryMappedFileSecurity memoryMappedFileSecurity,
                                                        HandleInheritability inheritability, bool leaveOpen)
             */

            if (input.memoryMappedFile == null)
            {
                //TODO: conniey
                //input.memoryMappedFile = MemoryMappedFile.CreateFromFile(fc, null, length == 0 ? 100 : length, MemoryMappedFileAccess.Read, null, HandleInheritability.Inheritable, false);
            }

            long bufferStart = 0L;
            for (int bufNr = 0; bufNr < nrBuffers; bufNr++)
            {
                int bufSize = (int)((length > (bufferStart + chunkSize)) ? chunkSize : (length - bufferStart));
                //LUCENE TO-DO
                buffers[bufNr] = new MemoryMappedFileByteBuffer(input.memoryMappedFile.CreateViewAccessor(offset + bufferStart, bufSize, MemoryMappedFileAccess.Read), -1, 0, bufSize, bufSize);
                //buffers[bufNr] = fc.Map(FileStream.MapMode.READ_ONLY, offset + bufferStart, bufSize);
                bufferStart += bufSize;
            }

            return buffers;
        }
    }
}