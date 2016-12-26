using System;
using System.Collections.Generic;

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
    /// A Directory is a flat list of files.  Files may be written once, when they
    /// are created.  Once a file is created it may only be opened for read, or
    /// deleted.  Random access is permitted both when reading and writing.
    ///
    /// Java's i/o APIs not used directly, but rather all i/o is
    /// through this API.  this permits things such as: <ul>
    /// <li> implementation of RAM-based indices;</li>
    /// <li> implementation indices stored in a database, via JDBC;</li>
    /// <li> implementation of an index as a single file;</li>
    /// </ul>
    ///
    /// Directory locking is implemented by an instance of {@link
    /// LockFactory}, and can be changed for each Directory
    /// instance using <seealso cref="setLockFactory"/>.
    ///
    /// </summary>
    public abstract class Directory : IDisposable // LUCENENET TODO: Subclass System.IO.FileSystemInfo ?
    {
        /// <summary>
        /// Returns an array of strings, one for each file in the directory.
        /// </summary>
        /// <exception cref="NoSuchDirectoryException"> if the directory is not prepared for any
        ///         write operations (such as <seealso cref="#createOutput(String, IOContext)"/>). </exception>
        /// <exception cref="System.IO.IOException"> in case of other IO errors </exception>
        public abstract string[] ListAll();

        /// <summary>
        /// Returns true iff a file with the given name exists.
        /// </summary>
        ///  @deprecated this method will be removed in 5.0
        [Obsolete("this method will be removed in 5.0")]
        public abstract bool FileExists(string name);

        /// <summary>
        /// Removes an existing file in the directory. </summary>
        public abstract void DeleteFile(string name);

        /// <summary>
        /// Returns the length of a file in the directory. this method follows the
        /// following contract:
        /// <ul>
        /// <li>Throws <seealso cref="FileNotFoundException"/> or <seealso cref="NoSuchFileException"/>
        /// if the file does not exist.</li>
        /// <li>Returns a value &gt;=0 if the file exists, which specifies its length.</li>
        /// </ul>
        /// </summary>
        /// <param name="name"> the name of the file for which to return the length. </param>
        /// <exception cref="System.IO.IOException"> if there was an IO error while retrieving the file's
        ///         length. </exception>
        public abstract long FileLength(string name);

        /// <summary>
        /// Creates a new, empty file in the directory with the given name.
        ///    Returns a stream writing this file.
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
        /// specified read buffer size.  The particular Directory
        /// implementation may ignore the buffer size.  Currently
        /// the only Directory implementations that respect this
        /// parameter are <seealso cref="FSDirectory"/> and {@link
        /// CompoundFileDirectory}.
        /// <p>Throws <seealso cref="FileNotFoundException"/> or <seealso cref="NoSuchFileException"/>
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
        /// Construct a <seealso cref="Lock"/>. </summary>
        /// <param name="name"> the name of the lock file </param>
        public abstract Lock MakeLock(string name);

        /// <summary>
        /// Attempt to clear (forcefully unlock and remove) the
        /// specified lock.  Only call this at a time when you are
        /// certain this lock is no longer in use. </summary>
        /// <param name="name"> name of the lock to be cleared. </param>
        public abstract void ClearLock(string name);

        /// <summary>
        /// Closes the store. </summary>
        public abstract void Dispose();

        /// <summary>
        /// Set the LockFactory that this Directory instance should
        /// use for its locking implementation.  Each * instance of
        /// LockFactory should only be used for one directory (ie,
        /// do not share a single instance across multiple
        /// Directories).
        /// </summary>
        /// <param name="lockFactory"> instance of <seealso cref="LockFactory"/>. </param>
        public abstract void SetLockFactory(LockFactory lockFactory);

        /// <summary>
        /// Get the LockFactory that this Directory instance is
        /// using for its locking implementation.  Note that this
        /// may be null for Directory implementations that provide
        /// their own locking implementation.
        /// </summary>
        public abstract LockFactory LockFactory { get; }

        /// <summary>
        /// Return a string identifier that uniquely differentiates
        /// this Directory instance from other Directory instances.
        /// this ID should be the same if two Directory instances
        /// (even in different JVMs and/or on different machines)
        /// are considered "the same index".  this is how locking
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
        /// Copies the file <i>src</i> to <seealso cref="Directory"/> <i>to</i> under the new
        /// file name <i>dest</i>.
        /// <p>
        /// If you want to copy the entire source directory to the destination one, you
        /// can do so like this:
        ///
        /// <pre class="prettyprint">
        /// Directory to; // the directory to copy to
        /// for (String file : dir.listAll()) {
        ///   dir.copy(to, file, newFile, IOContext.DEFAULT); // newFile can be either file, or a new name
        /// }
        /// </pre>
        /// <p>
        /// <b>NOTE:</b> this method does not check whether <i>dest</i> exist and will
        /// overwrite it if it does.
        /// </summary>
        public virtual void Copy(Directory to, string src, string dest, IOContext context)
        {
            IndexOutput os = null;
            IndexInput @is = null;
            System.IO.IOException priorException = null;
            try
            {
                os = to.CreateOutput(dest, context);
                @is = OpenInput(src, context);
                os.CopyBytes(@is, @is.Length());
            }
            catch (System.IO.IOException ioe)
            {
                priorException = ioe;
            }
            finally
            {
                bool success = false;
                try
                {
                    IOUtils.CloseWhileHandlingException(priorException, os, @is);
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
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates an <seealso cref="IndexInputSlicer"/> for the given file name.
        /// IndexInputSlicer allows other <seealso cref="Directory"/> implementations to
        /// efficiently open one or more sliced <seealso cref="IndexInput"/> instances from a
        /// single file handle. The underlying file handle is kept open until the
        /// <seealso cref="IndexInputSlicer"/> is closed.
        /// <p>Throws <seealso cref="FileNotFoundException"/> or <seealso cref="NoSuchFileException"/>
        /// if the file does not exist.
        /// </summary>
        /// <exception cref="System.IO.IOException">
        ///           if an <seealso cref="System.IO.IOException"/> occurs
        /// @lucene.internal
        /// @lucene.experimental </exception>
        public virtual IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            return new IndexInputSlicerAnonymousInnerClassHelper(this, name, context);
        }

        private class IndexInputSlicerAnonymousInnerClassHelper : IndexInputSlicer
        {
            private readonly Directory OuterInstance;

            private string Name;
            private IOContext Context;

            public IndexInputSlicerAnonymousInnerClassHelper(Directory outerInstance, string name, IOContext context)
                : base(outerInstance)
            {
                this.OuterInstance = outerInstance;
                this.Name = name;
                this.Context = context;
                @base = outerInstance.OpenInput(name, context);
            }

            private readonly IndexInput @base;

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                return new SlicedIndexInput("SlicedIndexInput(" + sliceDescription + " in " + @base + ")", @base, offset, length);
            }

            public override void Dispose(bool disposing)
            {
                @base.Dispose();
            }

            public override IndexInput OpenFullSlice()
            {
                return (IndexInput)@base.Clone();
            }
        }

        /// <exception cref="AlreadyClosedException"> if this Directory is closed </exception>
        protected internal virtual void EnsureOpen()
        {
        }

        /// <summary>
        /// Allows to create one or more sliced <seealso cref="IndexInput"/> instances from a single
        /// file handle. Some <seealso cref="Directory"/> implementations may be able to efficiently map slices of a file
        /// into memory when only certain parts of a file are required.
        /// @lucene.internal
        /// @lucene.experimental
        /// </summary>
        public abstract class IndexInputSlicer : IDisposable
        {
            private readonly Directory OuterInstance;

            public IndexInputSlicer(Directory outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            /// <summary>
            /// Returns an <seealso cref="IndexInput"/> slice starting at the given offset with the given length.
            /// </summary>
            public abstract IndexInput OpenSlice(string sliceDescription, long offset, long length);

            /// <summary>
            /// Returns an <seealso cref="IndexInput"/> slice starting at offset <i>0</i> with a
            /// length equal to the length of the underlying file </summary>
            /// @deprecated Only for reading CFS files from 3.x indexes.
            [Obsolete("Only for reading CFS files from 3.x indexes.")]
            public abstract IndexInput OpenFullSlice();

            // can we remove this somehow?

            public abstract void Dispose(bool disposing);

            public void Dispose()
            {
                Dispose(true);
            }
        }

        /// <summary>
        /// Implementation of an IndexInput that reads from a portion of
        ///  a file.
        /// </summary>
        private sealed class SlicedIndexInput : BufferedIndexInput
        {
            private IndexInput @base;
            private long FileOffset;
            private long Length_Renamed;

            internal SlicedIndexInput(string sliceDescription, IndexInput @base, long fileOffset, long length)
                : this(sliceDescription, @base, fileOffset, length, BufferedIndexInput.BUFFER_SIZE)
            {
            }

            internal SlicedIndexInput(string sliceDescription, IndexInput @base, long fileOffset, long length, int readBufferSize)
                : base("SlicedIndexInput(" + sliceDescription + " in " + @base + " slice=" + fileOffset + ":" + (fileOffset + length) + ")", readBufferSize)
            {
                this.@base = (IndexInput)@base.Clone();
                this.FileOffset = fileOffset;
                this.Length_Renamed = length;
            }

            public override object Clone()
            {
                SlicedIndexInput clone = (SlicedIndexInput)base.Clone();
                clone.@base = (IndexInput)@base.Clone();
                clone.FileOffset = FileOffset;
                clone.Length_Renamed = Length_Renamed;
                return clone;
            }

            /// <summary>
            /// Expert: implements buffer refill.  Reads bytes from the current
            ///  position in the input. </summary>
            /// <param name="b"> the array to read bytes into </param>
            /// <param name="offset"> the offset in the array to start storing bytes </param>
            /// <param name="len"> the number of bytes to read </param>
            protected override void ReadInternal(byte[] b, int offset, int len)
            {
                long start = FilePointer;
                if (start + len > Length_Renamed)
                {
                    throw new Exception("read past EOF: " + this);
                }
                @base.Seek(FileOffset + start);
                @base.ReadBytes(b, offset, len, false);
            }

            /// <summary>
            /// Expert: implements seek.  Sets current position in this file, where
            ///  the next <seealso cref="#readInternal(byte[],int,int)"/> will occur. </summary>
            /// <seealso> cref= #readInternal(byte[],int,int) </seealso>
            protected override void SeekInternal(long pos)
            {
            }

            public override void Dispose()
            {
                @base.Dispose();
            }

            public override long Length()
            {
                return Length_Renamed;
            }
        }
    }
}