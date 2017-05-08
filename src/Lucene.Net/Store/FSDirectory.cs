using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;// Used only for WRITE_LOCK_NAME in deprecated create=true case:
using Lucene.Net.Support;

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

    using Constants = Lucene.Net.Util.Constants;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Base class for <see cref="Directory"/> implementations that store index
    /// files in the file system.
    /// <para/>
    /// There are currently three core
    /// subclasses:
    ///
    /// <list type="bullet">
    ///
    ///     <item> <see cref="SimpleFSDirectory"/> is a straightforward
    ///         implementation using <see cref="System.IO.FileStream"/>.
    ///         However, it has poor concurrent performance
    ///         (multiple threads will bottleneck) as it
    ///         synchronizes when multiple threads read from the
    ///         same file.</item>
    ///
    ///     <item> <see cref="NIOFSDirectory"/> uses java.nio's
    ///         FileChannel's positional io when reading to avoid
    ///         synchronization when reading from the same file.
    ///         Unfortunately, due to a Windows-only <a
    ///         href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6265734">Sun
    ///         JRE bug</a> this is a poor choice for Windows, but
    ///         on all other platforms this is the preferred
    ///         choice. Applications using <see cref="System.Threading.Thread.Interrupt()"/> or
    ///         <see cref="System.Threading.Tasks.Task{TResult}"/> should use
    ///         <see cref="SimpleFSDirectory"/> instead. See <see cref="NIOFSDirectory"/> java doc
    ///         for details.</item>
    ///
    ///     <item> <see cref="MMapDirectory"/> uses memory-mapped IO when
    ///         reading. This is a good choice if you have plenty
    ///         of virtual memory relative to your index size, eg
    ///         if you are running on a 64 bit runtime, or you are
    ///         running on a 32 bit runtime but your index sizes are
    ///         small enough to fit into the virtual memory space.
    ///         <para/>
    ///         Applications using <see cref="System.Threading.Thread.Interrupt()"/> or
    ///         <see cref="System.Threading.Tasks.Task"/> should use
    ///         <see cref="SimpleFSDirectory"/> instead. See <see cref="MMapDirectory"/>
    ///         doc for details.</item>
    /// </list>
    ///
    /// Unfortunately, because of system peculiarities, there is
    /// no single overall best implementation.  Therefore, we've
    /// added the <see cref="Open(string)"/> method  (or one of its overloads), to allow Lucene to choose
    /// the best <see cref="FSDirectory"/> implementation given your
    /// environment, and the known limitations of each
    /// implementation.  For users who have no reason to prefer a
    /// specific implementation, it's best to simply use 
    /// <see cref="Open(string)"/>  (or one of its overloads).  For all others, you should instantiate the
    /// desired implementation directly.
    ///
    /// <para/>The locking implementation is by default 
    /// <see cref="NativeFSLockFactory"/>, but can be changed by
    /// passing in a custom <see cref="LockFactory"/> instance.
    /// </summary>
    /// <seealso cref="Directory"/>
    public abstract class FSDirectory : BaseDirectory
    {
        /// <summary>
        /// Default read chunk size: 8192 bytes (this is the size up to which the runtime
        /// does not allocate additional arrays while reading/writing) </summary>
        [Obsolete("this constant is no longer used since Lucene 4.5.")]
        public const int DEFAULT_READ_CHUNK_SIZE = 8192;

        protected readonly DirectoryInfo m_directory; // The underlying filesystem directory
        protected readonly ISet<string> m_staleFiles = new ConcurrentHashSet<string>(); // Files written, but not yet sync'ed
#pragma warning disable 612, 618
        private int chunkSize = DEFAULT_READ_CHUNK_SIZE;
#pragma warning restore 612, 618

        protected FSDirectory(DirectoryInfo dir)
            : this(dir, null)
        {
        }

        /// <summary>
        /// Create a new <see cref="FSDirectory"/> for the named location (ctor for subclasses). </summary>
        /// <param name="path"> the path of the directory </param>
        /// <param name="lockFactory"> the lock factory to use, or null for the default
        /// (<seealso cref="NativeFSLockFactory"/>); </param>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        protected internal FSDirectory(DirectoryInfo path, LockFactory lockFactory)
        {
            // new ctors use always NativeFSLockFactory as default:
            if (lockFactory == null)
            {
                lockFactory = new NativeFSLockFactory();
            }
            m_directory = path; // Lucene.NET doesn't need to call GetCanonicalPath since we already have DirectoryInfo handy

            if (File.Exists(path.FullName))
            {
                throw new DirectoryNotFoundException("file '" + path.FullName + "' exists but is not a directory");
            }

            SetLockFactory(lockFactory);
        }

        /// <summary>
        /// Creates an <see cref="FSDirectory"/> instance, trying to pick the
        /// best implementation given the current environment.
        /// The directory returned uses the <see cref="NativeFSLockFactory"/>.
        ///
        /// <para/>Currently this returns <see cref="MMapDirectory"/> for most Solaris
        /// and Windows 64-bit runtimes, <see cref="NIOFSDirectory"/> for other
        /// non-Windows runtimes, and <see cref="SimpleFSDirectory"/> for other
        /// runtimes on Windows. It is highly recommended that you consult the
        /// implementation's documentation for your platform before
        /// using this method.
        ///
        /// <para/><b>NOTE</b>: this method may suddenly change which
        /// implementation is returned from release to release, in
        /// the event that higher performance defaults become
        /// possible; if the precise implementation is important to
        /// your application, please instantiate it directly,
        /// instead. For optimal performance you should consider using
        /// <see cref="MMapDirectory"/> on 64 bit runtimes.
        ///
        /// <para/>See <see cref="FSDirectory"/>.
        /// </summary>
        public static FSDirectory Open(DirectoryInfo path)
        {
            return Open(path, null);
        }

        /// <summary>
        /// Just like <see cref="Open(DirectoryInfo)"/>, but
        /// allows you to specify the directory as a <see cref="string"/>.
        /// </summary>
        /// <param name="path">The path (to a directory) to open</param>
        /// <returns>An open <see cref="FSDirectory"/></returns>
        public static FSDirectory Open(string path) // LUCENENET specific overload for ease of use with .NET
        {
            return Open(new DirectoryInfo(path), null);
        }

        /// <summary>
        /// Just like <see cref="Open(DirectoryInfo)"/>, but allows you to
        /// also specify a custom <see cref="LockFactory"/>.
        /// </summary>
        public static FSDirectory Open(DirectoryInfo path, LockFactory lockFactory)
        {
            if ((Constants.WINDOWS || Constants.SUN_OS || Constants.LINUX) && Constants.RUNTIME_IS_64BIT &&
                MMapDirectory.UNMAP_SUPPORTED)
            {
                return new MMapDirectory(path, lockFactory);
            }
            else if (Constants.WINDOWS)
            {
                return new SimpleFSDirectory(path, lockFactory);
            }
            else
            {
                return new NIOFSDirectory(path, lockFactory);
            }
        }

        /// <summary>
        /// Just like <see cref="Open(DirectoryInfo, LockFactory)"/>, but
        /// allows you to specify the directory as a <see cref="string"/>.
        /// </summary>
        /// <param name="path">The path (to a directory) to open</param>
        /// <param name="lockFactory"></param>
        /// <returns>An open <see cref="FSDirectory"/></returns>
        public static FSDirectory Open(string path, LockFactory lockFactory) // LUCENENET specific overload for ease of use with .NET
        {
            return Open(new DirectoryInfo(path), lockFactory);
        }

        public override void SetLockFactory(LockFactory lockFactory)
        {
            base.SetLockFactory(lockFactory);

            // for filesystem based LockFactory, delete the lockPrefix, if the locks are placed
            // in index dir. If no index dir is given, set ourselves
            if (lockFactory is FSLockFactory)
            {
                FSLockFactory lf = (FSLockFactory)lockFactory;
                DirectoryInfo dir = lf.LockDir;
                // if the lock factory has no lockDir set, use the this directory as lockDir
                if (dir == null)
                {
                    lf.SetLockDir(m_directory);
                    lf.LockPrefix = null;
                }
                else if (dir.FullName.Equals(m_directory.FullName, StringComparison.Ordinal))
                {
                    lf.LockPrefix = null;
                }
            }
        }

        /// <summary>
        /// Lists all files (not subdirectories) in the
        /// directory.  This method never returns <c>null</c> (throws
        /// <seealso cref="IOException"/> instead).
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"> if the directory
        /// does not exist, or does exist but is not a
        /// directory or is invalid (for example, it is on an unmapped drive). </exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        public static string[] ListAll(DirectoryInfo dir)
        {
            if (!System.IO.Directory.Exists(dir.FullName))
            {
                throw new DirectoryNotFoundException("directory '" + dir + "' does not exist");
            }
            else if (File.Exists(dir.FullName))
            {
                throw new DirectoryNotFoundException("file '" + dir + "' exists but is not a directory");
            }

            // Exclude subdirs
            FileInfo[] files = dir.EnumerateFiles().ToArray();
            string[] result = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                result[i] = files[i].Name;
            }
            // LUCENENET NOTE: this can never happen in .NET
            //if (result == null)
            //{
            //    throw new System.IO.IOException("directory '" + dir + "' exists and is a directory, but cannot be listed: list() returned null");
            //}

            return result;
        }

        /// <summary>
        /// Lists all files (not subdirectories) in the
        /// directory. </summary>
        /// <seealso cref="ListAll(DirectoryInfo)"/>
        public override string[] ListAll()
        {
            EnsureOpen();
            return ListAll(m_directory);
        }

        /// <summary>
        /// Returns true iff a file with the given name exists. </summary>
        [Obsolete("this method will be removed in 5.0")]
        public override bool FileExists(string name)
        {
            EnsureOpen();
            return File.Exists(Path.Combine(m_directory.FullName, name));
        }

        /// <summary>
        /// Returns the length in bytes of a file in the directory. </summary>
        public override long FileLength(string name)
        {
            EnsureOpen();
            FileInfo file = new FileInfo(Path.Combine(m_directory.FullName, name));
            long len = file.Length;
            if (len == 0 && !file.Exists)
            {
                throw new FileNotFoundException(name);
            }
            else
            {
                return len;
            }
        }

        /// <summary>
        /// Removes an existing file in the directory. </summary>
        public override void DeleteFile(string name)
        {
            EnsureOpen();
            FileInfo file = new FileInfo(Path.Combine(m_directory.FullName, name));
            try
            {
                file.Delete();
                if (File.Exists(file.FullName))
                {
                    throw new IOException("Cannot delete " + file);
                }
            }
            catch (Exception e)
            {
                throw new IOException("Cannot delete " + file, e);
            }
            m_staleFiles.Remove(name);
        }

        /// <summary>
        /// Creates an <see cref="IndexOutput"/> for the file with the given name. </summary>
        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();

            EnsureCanWrite(name);
            return new FSIndexOutput(this, name);
        }

        protected virtual void EnsureCanWrite(string name)
        {
            if (!m_directory.Exists)
            {
                try
                {
                    m_directory.Create();
                }
                catch
                {
                    throw new IOException("Cannot create directory: " + m_directory);
                }
            }

            FileInfo file = new FileInfo(Path.Combine(m_directory.FullName, name));
            if (file.Exists) // delete existing, if any
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    throw new IOException("Cannot overwrite: " + file);
                }
            }
        }

        protected virtual void OnIndexOutputClosed(FSIndexOutput io)
        {
            m_staleFiles.Add(io.name);
        }

        public override void Sync(ICollection<string> names)
        {
            EnsureOpen();
            ISet<string> toSync = new HashSet<string>(names);
            toSync.IntersectWith(m_staleFiles);

            foreach (var name in toSync)
            {
                Fsync(name);
            }

            // fsync the directory itsself, but only if there was any file fsynced before
            // (otherwise it can happen that the directory does not yet exist)!
            if (toSync.Count > 0)
            {
                IOUtils.Fsync(m_directory.FullName, true);
            }

            m_staleFiles.ExceptWith(toSync);
        }

        public override string GetLockID()
        {
            EnsureOpen();
            string dirName; // name to be hashed
            try
            {
                dirName = m_directory.FullName;
            }
            catch (IOException e)
            {
                throw new Exception(e.ToString(), e);
            }

            int digest = 0;
            for (int charIDX = 0; charIDX < dirName.Length; charIDX++)
            {
                char ch = dirName[charIDX];
                digest = 31*digest + ch;
            }
            return "lucene-" + digest.ToString("x");
        }

        /// <summary>
        /// Closes the store to future operations. </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsOpen = false;
            }
        }

        /// <summary> the underlying filesystem directory </summary>
        public virtual DirectoryInfo Directory
        {
            get
            {
                EnsureOpen();
                return m_directory;
            }
        }

        /// <summary>
        /// For debug output. </summary>
        public override string ToString()
        {
            return this.GetType().Name + "@" + m_directory + " lockFactory=" + LockFactory;
        }

        /// <summary>
        /// this setting has no effect anymore. </summary>
        [Obsolete("this is no longer used since Lucene 4.5.")]
        public int ReadChunkSize
        {
            set
            {
                if (value <= 0)
                {
                    throw new System.ArgumentException("chunkSize must be positive");
                }
                this.chunkSize = value;
            }
            get { return chunkSize; }
        }

        /// <summary>
        /// Writes output with <see cref="FileStream.Write(byte[], int, int)"/>
        /// </summary>
        protected class FSIndexOutput : BufferedIndexOutput
        {
            // LUCENENET specific: chunk size not needed
            ///// <summary>
            ///// The maximum chunk size is 8192 bytes, because <seealso cref="RandomAccessFile"/> mallocs
            ///// a native buffer outside of stack if the write buffer size is larger.
            ///// </summary>
            //private const int CHUNK_SIZE = 8192;

            private readonly FSDirectory parent;
            internal readonly string name;
            private readonly FileStream file;
            private volatile bool isOpen; // remember if the file is open, so that we don't try to close it more than once

            public FSIndexOutput(FSDirectory parent, string name)
                : base(/*CHUNK_SIZE*/)
            {
                this.parent = parent;
                this.name = name;
                file = new FileStream(Path.Combine(parent.m_directory.FullName, name), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                isOpen = true;
            }

            protected internal override void FlushBuffer(byte[] b, int offset, int size)
            {
                //Debug.Assert(isOpen);
                //while (size > 0)
                //{
                //    int toWrite = Math.Min(CHUNK_SIZE, size);
                //    file.Write(b, offset, toWrite);
                //    offset += toWrite;
                //    size -= toWrite;
                //}

                // LUCENENET specific: FileStream is already optimized to write natively
                // if over the buffer size that is passed through its constructor. So,
                // all we need to do is Write().
                file.Write(b, offset, size);

                //Debug.Assert(size == 0);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                { 
                    parent.OnIndexOutputClosed(this);
                    // only close the file if it has not been closed yet
                    if (isOpen)
                    {
                        IOException priorE = null;
                        try
                        {
                            base.Dispose(disposing);
                        }
                        catch (IOException ioe)
                        {
                            priorE = ioe;
                        }
                        finally
                        {
                            isOpen = false;
                            IOUtils.CloseWhileHandlingException(priorE, file);
                        }
                    }
                }
            }

            /// <summary>
            /// Random-access methods </summary>
            [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
            public override void Seek(long pos)
            {
                base.Seek(pos);
                file.Seek(pos, SeekOrigin.Begin);
            }

            public override long Length
            {
                get { return file.Length; }
            }

            // LUCENENET NOTE: FileStream doesn't have a way to set length
        }

        protected virtual void Fsync(string name)
        {
            IOUtils.Fsync(Path.Combine(m_directory.FullName, name), false);            
        }
    }
}