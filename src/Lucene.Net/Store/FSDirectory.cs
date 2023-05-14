using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;// Used only for WRITE_LOCK_NAME in deprecated create=true case:
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

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
    ///     <item><description> <see cref="SimpleFSDirectory"/> is a straightforward
    ///         implementation using <see cref="FileStream"/>, which is ideal for writing
    ///         without using much RAM. However, it has poor concurrent performance
    ///         (multiple threads will bottleneck) as it
    ///         synchronizes when multiple threads read from the
    ///         same file.</description></item>
    ///
    ///     <item><description> <see cref="NIOFSDirectory"/>
    ///         uses <see cref="FileStream"/>'s positional seeking,
    ///         which makes it slightly less efficient than using <see cref="SimpleFSDirectory"/>
    ///         during reading, with similar write performance.</description></item>
    ///
    ///     <item><description> <see cref="MMapDirectory"/> uses memory-mapped IO when
    ///         reading. This is a good choice if you have plenty
    ///         of virtual memory relative to your index size, eg
    ///         if you are running on a 64 bit runtime, or you are
    ///         running on a 32 bit runtime but your index sizes are
    ///         small enough to fit into the virtual memory space.</description></item>
    /// </list>
    ///
    /// Unfortunately, because of system peculiarities, there is
    /// no single overall best implementation.  Therefore, we've
    /// added the <see cref="Open(string)"/> method  (or one of its overloads), to allow Lucene to choose
    /// the best <see cref="FSDirectory"/> implementation given your
    /// environment, and the known limitations of each
    /// implementation. For users who have no reason to prefer a
    /// specific implementation, it's best to simply use 
    /// <see cref="Open(string)"/>  (or one of its overloads). For all others, you should instantiate the
    /// desired implementation directly.
    ///
    /// <para/>The locking implementation is by default 
    /// <see cref="NativeFSLockFactory"/>, but can be changed by
    /// passing in a custom <see cref="LockFactory"/> instance.
    ///
    /// <para/>
    /// <font color="red"><b>NOTE:</b> Unlike in Java, it is not recommended to use
    /// <see cref="Thread.Interrupt()"/> in .NET
    /// in conjunction with an open <see cref="FSDirectory"/> because it is not guaranteed to exit atomically.
    /// Any <c>lock</c> statement or <see cref="Monitor.Enter(object)"/> call can throw a
    /// <see cref="ThreadInterruptedException"/>, which makes shutting down unpredictable.
    /// To exit parallel tasks safely, we recommend using <see cref="Task"/>s
    /// and "interrupt" them with <see cref="CancellationToken"/>s.</font>
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

        // LUCENENET specific: No such thing as "stale files" in .NET, since Flush(true) writes everything to disk before
        // our FileStream is disposed.
        //protected readonly ISet<string> m_staleFiles = new ConcurrentHashSet<string>(); // Files written, but not yet sync'ed
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
        protected FSDirectory(DirectoryInfo path, LockFactory lockFactory)
        {
            // new ctors use always NativeFSLockFactory as default:
            if (lockFactory is null)
            {
                lockFactory = new NativeFSLockFactory();
            }
            m_directory = new DirectoryInfo(path.GetCanonicalPath());

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
            if ((Constants.WINDOWS || Constants.SUN_OS || Constants.LINUX) && Constants.RUNTIME_IS_64BIT /*&&
                MMapDirectory.UNMAP_SUPPORTED*/) // LUCENENET specific - unmap hack not needed
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
            if (lockFactory is FSLockFactory lf)
            {
                var dir = lf.LockDir;
                // if the lock factory has no lockDir set, use the this directory as lockDir
                if (dir is null)
                {
                    lf.SetLockDir(m_directory);
                    lf.LockPrefix = null;
                }
                else if (dir.GetCanonicalPath().Equals(m_directory.GetCanonicalPath(), StringComparison.Ordinal))
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
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
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
            //if (result is null)
            //{
            //    throw new IOException("directory '" + dir + "' exists and is a directory, but cannot be listed: list() returned null");
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
            // LUCENENET specific: We need to explicitly throw when the file has already been deleted,
            // since FileInfo doesn't do that for us.
            // (An enhancement carried over from Lucene 8.2.0)
            if (!File.Exists(file.FullName))
            {
                throw new FileNotFoundException("Cannot delete " + file + " because it doesn't exist.");
            }
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
            // LUCENENET specific: No such thing as "stale files" in .NET, since Flush(true) writes everything to disk before
            // our FileStream is disposed.
            //m_staleFiles.Remove(name);
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
            // LUCENENET specific: No such thing as "stale files" in .NET, since Flush(true) writes everything to disk before
            // our FileStream is disposed.
            //m_staleFiles.Add(io.name);
        }

        public override void Sync(ICollection<string> names)
        {
            EnsureOpen();

            // LUCENENET specific: No such thing as "stale files" in .NET, since Flush(true) writes everything to disk before
            // our FileStream is disposed. Therefore, there is nothing else to do in this method.
            //ISet<string> toSync = new HashSet<string>(names);
            //toSync.IntersectWith(m_staleFiles);

            //// LUCENENET specific: Fsync breaks concurrency here.
            //// Part of a solution suggested by Vincent Van Den Berghe: http://apache.markmail.org/message/hafnuhq2ydhfjmi2
            ////foreach (var name in toSync)
            ////{
            ////    Fsync(name);
            ////}

            //// fsync the directory itsself, but only if there was any file fsynced before
            //// (otherwise it can happen that the directory does not yet exist)!
            //if (toSync.Count > 0)
            //{
            //    IOUtils.Fsync(m_directory.FullName, true);
            //}

            //m_staleFiles.ExceptWith(toSync);
        }

        public override string GetLockID()
        {
            EnsureOpen();
            string dirName; // name to be hashed
            try
            {
                dirName = m_directory.GetCanonicalPath();
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
            }

            int digest = 0;
            for (int charIDX = 0; charIDX < dirName.Length; charIDX++)
            {
                char ch = dirName[charIDX];
                digest = 31*digest + ch;
            }
            return "lucene-" + digest.ToString("x", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Closes the store to future operations. </summary>
        protected override void Dispose(bool disposing)
        {
            IsOpen = false; // LUCENENET: Since there is nothing else to do here, we can safely call this. If we have other stuff to dispose, change to if (!CompareAndSetIsOpen(expect: true, update: false)) return;
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
            get => chunkSize;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(ReadChunkSize), "chunkSize must be positive"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)

                this.chunkSize = value;
            }
        }

        /// <summary>
        /// Writes output with <see cref="FileStream.Write(byte[], int, int)"/>
        /// </summary>
        // LUCENENET specific: Since FileStream does its own buffering, this class was refactored
        // to do all checksum operations as well as writing to the FileStream. By doing this we elminate
        // the extra set of buffers that were only creating unnecessary memory allocations and copy operations.
        protected class FSIndexOutput : BufferedIndexOutput
        {
            private const int CHUNK_SIZE = DEFAULT_BUFFER_SIZE;

            private readonly FSDirectory parent;
            internal readonly string name;
#pragma warning disable CA2213 // Disposable fields should be disposed
            private readonly FileStream file;
#pragma warning restore CA2213 // Disposable fields should be disposed
            private volatile bool isOpen; // remember if the file is open, so that we don't try to close it more than once
            private readonly CRC32 crc = new CRC32();

            public FSIndexOutput(FSDirectory parent, string name)
                : base(CHUNK_SIZE, null)
            {
                this.parent = parent;
                this.name = name;
                file = new FileStream(
                    path: Path.Combine(parent.m_directory.FullName, name),
                    mode: FileMode.OpenOrCreate,
                    access: FileAccess.Write,
                    share: FileShare.ReadWrite,
                    bufferSize: CHUNK_SIZE);
                isOpen = true;
            }

            /// <inheritdoc/>
            public override void WriteByte(byte b)
            {
                // LUCENENET specific: Guard to ensure we aren't disposed.
                if (!isOpen)
                    throw AlreadyClosedException.Create(this.GetType().FullName, "This FSIndexOutput is disposed.");

                crc.Update(b);
                file.WriteByte(b);
            }

            /// <inheritdoc/>
            public override void WriteBytes(byte[] b, int offset, int length)
            {
                // LUCENENET specific: Guard to ensure we aren't disposed.
                if (!isOpen)
                    throw AlreadyClosedException.Create(this.GetType().FullName, "This FSIndexOutput is disposed.");

                crc.Update(b, offset, length);
                file.Write(b, offset, length);
            }

            /// <inheritdoc/>
            protected internal override void FlushBuffer(byte[] b, int offset, int size)
            {
                // LUCENENET specific: Guard to ensure we aren't disposed.
                if (!isOpen)
                    throw AlreadyClosedException.Create(this.GetType().FullName, "This FSIndexOutput is disposed.");

                crc.Update(b, offset, size);
                file.Write(b, offset, size);
            }

            /// <inheritdoc/>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public override void Flush()
            {
                // LUCENENET specific: Guard to ensure we aren't disposed.
                if (!isOpen)
                    throw AlreadyClosedException.Create(this.GetType().FullName, "This FSIndexOutput is disposed.");

                file.Flush();
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    parent.OnIndexOutputClosed(this);
                    // only close the file if it has not been closed yet
                    if (isOpen)
                    {
                        Exception priorE = null; // LUCENENET: No need to cast to IOExcpetion
                        try
                        {
                            file.Flush(flushToDisk: true);
                        }
                        catch (Exception ioe) when (ioe.IsIOException())
                        {
                            priorE = ioe;
                        }
                        finally
                        {
                            isOpen = false;
                            IOUtils.DisposeWhileHandlingException(priorE, file);
                        }
                    }
                }
                //base.Dispose(disposing); // LUCENENET: No need to call base class, we are not using the functionality of BufferedIndexOutput
            }

            /// <summary>
            /// Random-access methods </summary>
            [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
            public override void Seek(long pos)
            {
                // LUCENENET specific: Guard to ensure we aren't disposed.
                if (!isOpen)
                    throw AlreadyClosedException.Create(this.GetType().FullName, "This FSIndexOutput is disposed.");

                file.Seek(pos, SeekOrigin.Begin);
            }

            /// <inheritdoc/>
            public override long Length => file.Length;

            // LUCENENET NOTE: FileStream doesn't have a way to set length

            /// <inheritdoc/>
            public override long Checksum => crc.Value; // LUCENENET specific - need to override, since we are buffering locally

            /// <inheritdoc/>
            public override long Position => file.Position; // LUCENENET specific - need to override, since we are buffering locally, renamed from getFilePointer() to match FileStream
        }

        // LUCENENET specific: Fsync is pointless in .NET, since we are 
        // calling FileStream.Flush(true) before the stream is disposed
        // which means we never need it at the point in Java where it is called.
        //protected virtual void Fsync(string name)
        //{
        //    IOUtils.Fsync(Path.Combine(m_directory.FullName, name), false);
        //}
    }
}