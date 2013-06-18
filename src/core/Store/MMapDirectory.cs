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
using System.IO.MemoryMappedFiles;
using Lucene.Net.Support;
using Constants = Lucene.Net.Util.Constants;

namespace Lucene.Net.Store
{

    /// <summary>File-based <see cref="Directory" /> implementation that uses
    /// mmap for reading, and <see cref="SimpleFSDirectory.SimpleFSIndexOutput" />
    /// for writing.
    /// 
    /// <p/><b>NOTE</b>: memory mapping uses up a portion of the
    /// virtual memory address space in your process equal to the
    /// size of the file being mapped.  Before using this class,
    /// be sure your have plenty of virtual address space, e.g. by
    /// using a 64 bit JRE, or a 32 bit JRE with indexes that are
    /// guaranteed to fit within the address space.
    /// On 32 bit platforms also consult <see cref="MaxChunkSize" />
    /// if you have problems with mmap failing because of fragmented
    /// address space. If you get an OutOfMemoryException, it is recommened
    /// to reduce the chunk size, until it works.
    /// 
    /// <p/>Due to <a href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4724038">
    /// this bug</a> in Sun's JRE, MMapDirectory's <see cref="IndexInput.Close" />
    /// is unable to close the underlying OS file handle.  Only when GC
    /// finally collects the underlying objects, which could be quite
    /// some time later, will the file handle be closed.
    /// 
    /// <p/>This will consume additional transient disk usage: on Windows,
    /// attempts to delete or overwrite the files will result in an
    /// exception; on other platforms, which typically have a &quot;delete on
    /// last close&quot; semantics, while such operations will succeed, the bytes
    /// are still consuming space on disk.  For many applications this
    /// limitation is not a problem (e.g. if you have plenty of disk space,
    /// and you don't rely on overwriting files on Windows) but it's still
    /// an important limitation to be aware of.
    /// 
    /// <p/>This class supplies the workaround mentioned in the bug report
    /// (disabled by default, see <see cref="UseUnmap" />), which may fail on
    /// non-Sun JVMs. It forcefully unmaps the buffer on close by using
    /// an undocumented internal cleanup functionality.
    /// <see cref="UNMAP_SUPPORTED" /> is <c>true</c>, if the workaround
    /// can be enabled (with no guarantees).
    /// </summary>
    public class MMapDirectory : FSDirectory
    {
        private bool useUnmapHack = UNMAP_SUPPORTED;

        public static readonly int DEFAULT_MAX_BUFF = Constants.JRE_IS_64BIT ? (1 << 30) : (1 << 28);

        internal readonly int chunkSizePower;

        /// <summary>Create a new MMapDirectory for the named location.
        /// 
        /// </summary>
        /// <param name="path">the path of the directory
        /// </param>
        /// <param name="lockFactory">the lock factory to use, or null for the default.
        /// </param>
        /// <throws>  IOException </throws>
        public MMapDirectory(System.IO.DirectoryInfo path, LockFactory lockFactory)
            : this(path, lockFactory, DEFAULT_MAX_BUFF)
        {
        }

        /// <summary>Create a new MMapDirectory for the named location and the default lock factory.
        /// 
        /// </summary>
        /// <param name="path">the path of the directory
        /// </param>
        /// <throws>  IOException </throws>
        public MMapDirectory(System.IO.DirectoryInfo path)
            : this(path, null)
        {
        }

        public MMapDirectory(System.IO.DirectoryInfo path, LockFactory lockFactory, int maxChunkSize)
            : base(path, lockFactory)
        {
            if (maxChunkSize <= 0)
            {
                throw new ArgumentException("Maximum chunk size for mmap must be >0");
            }
            this.chunkSizePower = 31 - Number.NumberOfLeadingZeros(maxChunkSize);
            //assert this.chunkSizePower >= 0 && this.chunkSizePower <= 30;
        }

        /// <summary> <c>true</c>, if this platform supports unmapping mmaped files.</summary>
        public static bool UNMAP_SUPPORTED;

        static MMapDirectory()
        {
            bool v;
            try
            {
                // {{Aroush-2.9
                /*
                System.Type.GetType("sun.misc.Cleaner"); // {{Aroush-2.9}} port issue?
                System.Type.GetType("java.nio.DirectByteBuffer").GetMethod("cleaner", (NO_PARAM_TYPES == null)?new System.Type[0]:(System.Type[]) NO_PARAM_TYPES);
                */
                //System.Diagnostics.Debug.Fail("Port issue:", "sun.misc.Cleaner.clean()"); // {{Aroush-2.9}}
                throw new NotImplementedException("Port issue: sun.misc.Cleaner.clean()");
                // Aroush-2.9}}
                //v = true;
            }
            catch
            {
                v = false;
            }

            UNMAP_SUPPORTED = v;
        }

        /// <summary> Enables or disables the workaround for unmapping the buffers
        /// from address space after closing <see cref="IndexInput" />, that is
        /// mentioned in the bug report. This hack may fail on non-Sun JVMs.
        /// It forcefully unmaps the buffer on close by using
        /// an undocumented internal cleanup functionality.
        /// <p/><b>NOTE:</b> Enabling this is completely unsupported
        /// by Java and may lead to JVM crashs if <c>IndexInput</c>
        /// is closed while another thread is still accessing it (SIGSEGV).
        /// </summary>
        /// <throws>  IllegalArgumentException if <see cref="UNMAP_SUPPORTED" /> </throws>
        /// <summary> is <c>false</c> and the workaround cannot be enabled.
        /// </summary>
        public virtual bool UseUnmap
        {
            get { return useUnmapHack; }
            set
            {
                if (value && !UNMAP_SUPPORTED)
                    throw new ArgumentException("Unmap hack not supported on this platform!");
                this.useUnmapHack = value;
            }
        }

        /// <summary> Gets or sets the maximum chunk size (default is <see cref="int.MaxValue" /> for
        /// 64 bit JVMs and 256 MiBytes for 32 bit JVMs) used for memory mapping.
        /// Especially on 32 bit platform, the address space can be very fragmented,
        /// so large index files cannot be mapped.
        /// Using a lower chunk size makes the directory implementation a little
        /// bit slower (as the correct chunk must be resolved on each seek)
        /// but the chance is higher that mmap does not fail. On 64 bit
        /// Java platforms, this parameter should always be <see cref="int.MaxValue" />,
        /// as the adress space is big enough.
        /// </summary>
        public virtual int MaxChunkSize
        {
            get { return 1 << chunkSizePower; }
        }

        /// <summary>Creates an IndexInput for the file with the given name. </summary>
        public override IndexInput OpenInput(String name, IOContext context)
        {
            EnsureOpen();
            String path = System.IO.Path.Combine(Directory.FullName, name);
            System.IO.FileStream raf = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            try
            {
                return new MMapIndexInput(this, "MMapIndexInput(path=\"" + path + "\")", raf, path);
            }
            finally
            {
                raf.Dispose();
            }
        }

        private sealed class AnonymousClassCreateSlicer : IndexInputSlicer
        {
            private readonly MMapIndexInput full;
            private readonly MMapDirectory parent;

            public AnonymousClassCreateSlicer(MMapDirectory parent, MMapIndexInput full)
            {
                this.parent = parent;
                this.full = full;
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                parent.EnsureOpen();
                return full.Slice(sliceDescription, offset, length);
            }

            public override IndexInput OpenFullSlice()
            {
                parent.EnsureOpen();
                return (IndexInput)full.Clone();
            }

            public override void Dispose(bool disposing)
            {
                full.Dispose();
            }
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            MMapIndexInput full = (MMapIndexInput)OpenInput(name, context);
            return new AnonymousClassCreateSlicer(this, full);
        }

        private class MMapIndexInput : ByteBufferIndexInput
        {
            private readonly bool useUnmapHack;

            internal MemoryMappedFile memoryMappedFile; // .NET port: this is equivalent to FileChannel.map
            
            internal MMapIndexInput(MMapDirectory parent, string resourceDescription, System.IO.FileStream raf, string path)
                : base(resourceDescription, null, raf.Length, parent.chunkSizePower, parent.UseUnmap)
            {
                this.useUnmapHack = parent.UseUnmap;
                
                // .NET port: need to reference "this" for buffer creation, so passing null to base ctor above
                this.Buffers = parent.Map(this, raf, 0, raf.Length, path);
            }

            protected override void FreeBuffer(ByteBuffer b)
            {
                // .NET port: this should free the memory mapped view accessor
                var mmfbb = b as MemoryMappedFileByteBuffer;

                if (mmfbb != null)
                    mmfbb.Dispose();
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                {
                    if (memoryMappedFile != null)
                        memoryMappedFile.Dispose();
                }
            }
        }

        internal ByteBuffer[] Map(MMapIndexInput input, System.IO.FileStream raf, long offset, long length, string path)
        {
            if (Number.URShift(length, chunkSizePower) >= int.MaxValue)
                throw new ArgumentException("RandomAccessFile too big for chunk size: " + raf.ToString());

            long chunkSize = 1L << chunkSizePower;

            // we always allocate one more buffer, the last one may be a 0 byte one
            int nrBuffers = (int)Number.URShift(length, chunkSizePower) + 1;

            ByteBuffer[] buffers = new ByteBuffer[nrBuffers];

            if (input.memoryMappedFile == null)
                input.memoryMappedFile = MemoryMappedFile.CreateFromFile(path, System.IO.FileMode.Open);

            long bufferStart = 0L;
            
            for (int bufNr = 0; bufNr < nrBuffers; bufNr++)
            {
                int bufSize = (int)((length > (bufferStart + chunkSize))
                    ? chunkSize
                        : (length - bufferStart)
                    );
                //buffers[bufNr] = rafc.map(MapMode.READ_ONLY, offset + bufferStart, bufSize);
                buffers[bufNr] = new MemoryMappedFileByteBuffer(input.memoryMappedFile.CreateViewAccessor(offset + bufferStart, bufSize), -1, 0, bufSize, bufSize);
                bufferStart += bufSize;
            }

            return buffers;
        }
    }
}