using Lucene.Net.Support;
using System;
using System.Collections.Generic;
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

    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// A <see cref="Directory"/> is a flat list of files.  Files may be written once, when they
    /// are created.  Once a file is created it may only be opened for read, or
    /// deleted.  Random access is permitted both when reading and writing.
    /// <para/>
    /// .NET's i/o APIs not used directly, but rather all i/o is
    /// through this API.  This permits things such as: 
    /// <list type="bullet">
    ///     <item><description> implementation of RAM-based indices;</description></item>
    ///     <item><description> implementation indices stored in a database;</description></item>
    ///     <item><description> implementation of an index as a single file;</description></item>
    /// </list>
    /// <para/>
    /// Directory locking is implemented by an instance of
    /// <see cref="Store.LockFactory"/>, and can be changed for each <see cref="Directory"/>
    /// instance using <see cref="SetLockFactory"/>.
    /// </summary>
    public abstract class Directory : IDisposable // LUCENENET TODO: Subclass System.IO.FileSystemInfo ?
    {
        /// <summary>
        /// Returns an array of strings, one for each file in the directory.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"> if the directory is not prepared for any
        ///         write operations (such as <see cref="CreateOutput(string, IOContext)"/>). </exception>
        /// <exception cref="IOException"> in case of other IO errors </exception>
        public abstract string[] ListAll();

        /// <summary>
        /// Returns <c>true</c> iff a file with the given name exists.
        /// </summary>
        [Obsolete("this method will be removed in 5.0")]
        public abstract bool FileExists(string name);

        /// <summary>
        /// Removes an existing file in the directory. </summary>
        public abstract void DeleteFile(string name);

        /// <summary>
        /// Returns the length of a file in the directory. this method follows the
        /// following contract:
        /// <list>
        ///     <item><description>Throws <see cref="FileNotFoundException"/>
        ///         if the file does not exist.</description></item>
        ///     <item><description>Returns a value &gt;=0 if the file exists, which specifies its length.</description></item>
        /// </list>
        /// </summary>
        /// <param name="name"> the name of the file for which to return the length. </param>
        /// <exception cref="IOException"> if there was an IO error while retrieving the file's
        ///         length. </exception>
        public abstract long FileLength(string name);

        /// <summary>
        /// Creates a new, empty file in the directory with the given name.
        /// Returns a stream writing this file.
        /// </summary>
        public abstract IndexOutput CreateOutput(string name, IOContext context);

        /// <summary>
        /// Ensure that any writes to these files are moved to
        /// stable storage.  Lucene uses this to properly commit
        /// changes to the index, to prevent a machine/OS crash
        /// from corrupting the index.<br/>
        /// <br/>
        /// NOTE: Clients may call this method for same files over
        /// and over again, so some impls might optimize for that.
        /// For other impls the operation can be a noop, for various
        /// reasons.
        /// </summary>
        public abstract void Sync(ICollection<string> names);

        /// <summary>
        /// Returns a stream reading an existing file, with the
        /// specified read buffer size.  The particular <see cref="Directory"/>
        /// implementation may ignore the buffer size.  Currently
        /// the only <see cref="Directory"/> implementations that respect this
        /// parameter are <see cref="FSDirectory"/> and 
        /// <see cref="CompoundFileDirectory"/>.
        /// <para/>Throws <see cref="FileNotFoundException"/>
        /// if the file does not exist.
        /// </summary>
        public abstract IndexInput OpenInput(string name, IOContext context);

        /// <summary>
        /// Returns a stream reading an existing file, computing checksum as it reads </summary>
        public virtual ChecksumIndexInput OpenChecksumInput(string name, IOContext context)
        {
            return new BufferedChecksumIndexInput(OpenInput(name, context));
        }

        /// <summary>
        /// Construct a <see cref="Lock"/>. </summary>
        /// <param name="name"> the name of the lock file </param>
        public abstract Lock MakeLock(string name);

        /// <summary>
        /// Attempt to clear (forcefully unlock and remove) the
        /// specified lock.  Only call this at a time when you are
        /// certain this lock is no longer in use. </summary>
        /// <param name="name"> name of the lock to be cleared. </param>
        public abstract void ClearLock(string name);

        /// <summary>
        /// Disposes the store. </summary>
        // LUCENENET specific - implementing proper dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the store. </summary>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Set the <see cref="Store.LockFactory"/> that this <see cref="Directory"/> instance should
        /// use for its locking implementation.  Each * instance of
        /// <see cref="Store.LockFactory"/> should only be used for one directory (ie,
        /// do not share a single instance across multiple
        /// Directories).
        /// </summary>
        /// <param name="lockFactory"> instance of <see cref="Store.LockFactory"/>. </param>
        public abstract void SetLockFactory(LockFactory lockFactory);

        /// <summary>
        /// Get the <see cref="Store.LockFactory"/> that this <see cref="Directory"/> instance is
        /// using for its locking implementation.  Note that this
        /// may be null for <see cref="Directory"/> implementations that provide
        /// their own locking implementation.
        /// </summary>
        public abstract LockFactory LockFactory { get; }

        /// <summary>
        /// Return a string identifier that uniquely differentiates
        /// this <see cref="Directory"/> instance from other <see cref="Directory"/> instances.
        /// This ID should be the same if two <see cref="Directory"/> instances
        /// (even in different AppDomains and/or on different machines)
        /// are considered "the same index".  This is how locking
        /// "scopes" to the right index.
        /// </summary>
        public virtual string GetLockID()
        {
            return this.ToString();
        }

        public override string ToString()
        {
            return this.GetType().Name + '@' + GetHashCode().ToString("x") + " lockFactory=" + LockFactory;
        }

        /// <summary>
        /// Copies the file <paramref name="src"/> to <seealso cref="Directory"/> <paramref name="to"/> under the new
        /// file name <paramref name="dest"/>.
        /// <para/>
        /// If you want to copy the entire source directory to the destination one, you
        /// can do so like this:
        ///
        /// <code>
        /// Directory to; // the directory to copy to
        /// foreach (string file in dir.ListAll()) {
        ///     dir.Copy(to, file, newFile, IOContext.DEFAULT); // newFile can be either file, or a new name
        /// }
        /// </code>
        /// <para/>
        /// <b>NOTE:</b> this method does not check whether <paramref name="dest"/> exist and will
        /// overwrite it if it does.
        /// </summary>
        public virtual void Copy(Directory to, string src, string dest, IOContext context)
        {
            IndexOutput os = null;
            IndexInput @is = null;
            Exception priorException = null; // LUCENENET: No need to cast to IOExcpetion
            try
            {
                os = to.CreateOutput(dest, context);
                @is = OpenInput(src, context);
                os.CopyBytes(@is, @is.Length);
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                priorException = ioe;
            }
            finally
            {
                bool success = false;
                try
                {
                    IOUtils.DisposeWhileHandlingException(priorException, os, @is);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        try
                        {
                            to.DeleteFile(dest);
                        }
                        catch (Exception t) when (t.IsThrowable())
                        {
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates an <see cref="IndexInputSlicer"/> for the given file name.
        /// <see cref="IndexInputSlicer"/> allows other <see cref="Directory"/> implementations to
        /// efficiently open one or more sliced <see cref="IndexInput"/> instances from a
        /// single file handle. The underlying file handle is kept open until the
        /// <see cref="IndexInputSlicer"/> is closed.
        /// <para/>Throws <see cref="FileNotFoundException"/>
        /// if the file does not exist.
        /// <para/>
        /// @lucene.internal
        /// @lucene.experimental
        /// </summary>
        /// <exception cref="IOException">
        ///           if an <seealso cref="IOException"/> occurs</exception>
        public virtual IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            return new IndexInputSlicerAnonymousClass(OpenInput(name, context));
        }

        private sealed class IndexInputSlicerAnonymousClass : IndexInputSlicer
        {
            private readonly IndexInput @base;
            private int disposed = 0; // LUCENENET specific - allow double-dispose

            public IndexInputSlicerAnonymousClass(IndexInput @base)
            {
                this.@base = @base;
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                return new SlicedIndexInput("SlicedIndexInput(" + sliceDescription + " in " + @base + ")", @base, offset, length);
            }

            protected override void Dispose(bool disposing)
            {
                if (0 != Interlocked.CompareExchange(ref this.disposed, 1, 0)) return; // LUCENENET specific - allow double-dispose

                if (disposing)
                {
                    @base.Dispose();
                }
            }

            [Obsolete("Only for reading CFS files from 3.x indexes.")]
            public override IndexInput OpenFullSlice()
            {
                return (IndexInput)@base.Clone();
            }
        }

        /// <exception cref="ObjectDisposedException"> if this Directory is closed </exception>
        protected internal virtual void EnsureOpen()
        {
        }

        /// <summary>
        /// Allows to create one or more sliced <see cref="IndexInput"/> instances from a single
        /// file handle. Some <see cref="Directory"/> implementations may be able to efficiently map slices of a file
        /// into memory when only certain parts of a file are required.
        /// <para/>
        /// @lucene.internal
        /// @lucene.experimental
        /// </summary>
        public abstract class IndexInputSlicer : IDisposable
        {
            /// <summary>
            /// Returns an <see cref="IndexInput"/> slice starting at the given offset with the given length.
            /// </summary>
            public abstract IndexInput OpenSlice(string sliceDescription, long offset, long length);

            /// <summary>
            /// Returns an <see cref="IndexInput"/> slice starting at offset <c>0</c> with a
            /// length equal to the length of the underlying file </summary>
            [Obsolete("Only for reading CFS files from 3.x indexes.")]
            public abstract IndexInput OpenFullSlice(); // can we remove this somehow?

            protected abstract void Dispose(bool disposing);

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Implementation of an <see cref="IndexInput"/> that reads from a portion of
        /// a file.
        /// </summary>
        private sealed class SlicedIndexInput : BufferedIndexInput
        {
            private IndexInput @base;
            private long fileOffset;
            private long length;
            private int disposed = 0; // LUCENENET specific - allow double-dispose

            internal SlicedIndexInput(string sliceDescription, IndexInput @base, long fileOffset, long length)
                : this(sliceDescription, @base, fileOffset, length, BufferedIndexInput.BUFFER_SIZE)
            {
            }

            internal SlicedIndexInput(string sliceDescription, IndexInput @base, long fileOffset, long length, int readBufferSize)
                : base("SlicedIndexInput(" + sliceDescription + " in " + @base + " slice=" + fileOffset + ":" + (fileOffset + length) + ")", readBufferSize)
            {
                this.@base = (IndexInput)@base.Clone();
                this.fileOffset = fileOffset;
                this.length = length;
            }

            public override object Clone()
            {
                SlicedIndexInput clone = (SlicedIndexInput)base.Clone();
                clone.@base = (IndexInput)@base.Clone();
                clone.fileOffset = fileOffset;
                clone.length = length;
                return clone;
            }

            /// <summary>
            /// Expert: implements buffer refill.  Reads bytes from the current
            /// position in the input. </summary>
            /// <param name="b"> the array to read bytes into </param>
            /// <param name="offset"> the offset in the array to start storing bytes </param>
            /// <param name="len"> the number of bytes to read </param>
            protected override void ReadInternal(byte[] b, int offset, int len)
            {
                long start = Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                if (start + len > length)
                {
                    throw EOFException.Create("read past EOF: " + this);
                }
                @base.Seek(fileOffset + start);
                @base.ReadBytes(b, offset, len, false);
            }

            /// <summary>
            /// Expert: implements seek.  Sets current position in this file, where
            /// the next <see cref="ReadInternal(byte[], int, int)"/> will occur. 
            /// </summary>
            /// <seealso cref="ReadInternal(byte[], int, int)"/>
            protected override void SeekInternal(long pos)
            {
            }

            /// <summary>
            /// Closes the stream to further operations.
            /// </summary>
            protected override void Dispose(bool disposing)
            {
                if (0 != Interlocked.CompareExchange(ref this.disposed, 1, 0)) return; // LUCENENET specific - allow double-dispose

                if (disposing)
                {
                    @base.Dispose();
                }
            }

            public override long Length => length;
        }

        // LUCENENET specific - formatter to defer building the string of directory contents in string.Format().
        // This struct is meant to wrap a directory parameter when passed as a string.Format() argument.
        internal struct ListAllFormatter // For assert/test/debug
        {
#pragma warning disable IDE0044 // Add readonly modifier
            private Directory directory;
#pragma warning restore IDE0044 // Add readonly modifier
            public ListAllFormatter(Directory directory)
            {
                this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            }

            public override string ToString()
            {
                return Arrays.ToString(directory.ListAll());
            }
        }
    }
}