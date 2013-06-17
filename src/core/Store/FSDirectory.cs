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
using System.Collections.Generic;

// Used only for WRITE_LOCK_NAME in deprecated create=true case:
using System.IO;
using Lucene.Net.Support;
using IndexFileNameFilter = Lucene.Net.Index.IndexFileNameFilter;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Constants = Lucene.Net.Util.Constants;
using System.Threading;

namespace Lucene.Net.Store
{

    /// <summary> <a name="subclasses"/>
    /// Base class for Directory implementations that store index
    /// files in the file system.  There are currently three core
    /// subclasses:
    /// 
    /// <list type="bullet">
    /// 
    /// <item> <see cref="SimpleFSDirectory" /> is a straightforward
    /// implementation using java.io.RandomAccessFile.
    /// However, it has poor concurrent performance
    /// (multiple threads will bottleneck) as it
    /// synchronizes when multiple threads read from the
    /// same file.</item>
    /// 
    /// <item> <see cref="NIOFSDirectory" /> uses java.nio's
    /// FileChannel's positional io when reading to avoid
    /// synchronization when reading from the same file.
    /// Unfortunately, due to a Windows-only <a
    /// href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6265734">Sun
    /// JRE bug</a> this is a poor choice for Windows, but
    /// on all other platforms this is the preferred
    /// choice. Applications using <see cref="System.Threading.Thread.Interrupt()" /> or
    /// <c>Future#cancel(boolean)</c> (on Java 1.5) should use
    /// <see cref="SimpleFSDirectory" /> instead. See <see cref="NIOFSDirectory" /> java doc
    /// for details.
    ///        
    ///        
    /// 
    /// <item> <see cref="MMapDirectory" /> uses memory-mapped IO when
    /// reading. This is a good choice if you have plenty
    /// of virtual memory relative to your index size, eg
    /// if you are running on a 64 bit JRE, or you are
    /// running on a 32 bit JRE but your index sizes are
    /// small enough to fit into the virtual memory space.
    /// Java has currently the limitation of not being able to
    /// unmap files from user code. The files are unmapped, when GC
    /// releases the byte buffers. Due to
    /// <a href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4724038">
    /// this bug</a> in Sun's JRE, MMapDirectory's <see cref="IndexInput.Close" />
    /// is unable to close the underlying OS file handle. Only when
    /// GC finally collects the underlying objects, which could be
    /// quite some time later, will the file handle be closed.
    /// This will consume additional transient disk usage: on Windows,
    /// attempts to delete or overwrite the files will result in an
    /// exception; on other platforms, which typically have a &quot;delete on
    /// last close&quot; semantics, while such operations will succeed, the bytes
    /// are still consuming space on disk.  For many applications this
    /// limitation is not a problem (e.g. if you have plenty of disk space,
    /// and you don't rely on overwriting files on Windows) but it's still
    /// an important limitation to be aware of. This class supplies a
    /// (possibly dangerous) workaround mentioned in the bug report,
    /// which may fail on non-Sun JVMs.</item>
    ///       
    /// Applications using <see cref="System.Threading.Thread.Interrupt()" /> or
    /// <c>Future#cancel(boolean)</c> (on Java 1.5) should use
    /// <see cref="SimpleFSDirectory" /> instead. See <see cref="MMapDirectory" />
    /// java doc for details.</item>
    /// </list>
    /// 
    /// Unfortunately, because of system peculiarities, there is
    /// no single overall best implementation.  Therefore, we've
    /// added the <see cref="Open(System.IO.DirectoryInfo)" /> method, to allow Lucene to choose
    /// the best FSDirectory implementation given your
    /// environment, and the known limitations of each
    /// implementation.  For users who have no reason to prefer a
    /// specific implementation, it's best to simply use <see cref="FSDirectory.Open(System.IO.DirectoryInfo)" />
    ///.  For all others, you should instantiate the
    /// desired implementation directly.
    /// 
    /// <p/>The locking implementation is by default <see cref="NativeFSLockFactory" />
    ///, but can be changed by
    /// passing in a custom <see cref="LockFactory" /> instance.
    /// </summary>
    public abstract class FSDirectory : Directory
    {
        public const int DEFAULT_READ_CHUNK_SIZE = Constants.JRE_IS_64BIT ? Int16.MaxValue : 100 * 1024 * 1024;

        protected readonly DirectoryInfo directory; // the underlying filesystem directory
        protected readonly ISet<string> staleFiles = new HashSet<string>(); // TODO: .NET port: should we make this concurrent somehow?
        private int chunkSize = DEFAULT_READ_CHUNK_SIZE;

        private static DirectoryInfo GetCanonicalPath(DirectoryInfo file)
        {
            // .NET port: there isn't a "canonical path" concept in .NET's version, so we're just following the logic
            // of creating the directory if it doesn't exist, and returning the input.
            if (!file.Exists)
                file.Create();

            return file;
        }

        /// <summary>Create a new FSDirectory for the named location (ctor for subclasses).</summary>
        /// <param name="path">the path of the directory
        /// </param>
        /// <param name="lockFactory">the lock factory to use, or null for the default
        /// (<see cref="NativeFSLockFactory" />);
        /// </param>
        /// <throws>  IOException </throws>
        protected internal FSDirectory(DirectoryInfo path, LockFactory lockFactory)
        {
            // new ctors use always NativeFSLockFactory as default:
            if (lockFactory == null)
            {
                lockFactory = new NativeFSLockFactory();
            }

            if (File.Exists(path.FullName))
                throw new NoSuchDirectoryException("file '" + path.FullName + "' exists but is not a directory");

            // due to differences in how Java & .NET refer to files, the checks are a bit different
            directory = GetCanonicalPath(path);

            this.LockFactory = lockFactory;
        }

        /// <summary>Creates an FSDirectory instance, trying to pick the
        /// best implementation given the current environment.
        /// The directory returned uses the <see cref="NativeFSLockFactory" />.
        /// 
        /// <p/>Currently this returns <see cref="SimpleFSDirectory" /> as
        /// NIOFSDirectory is currently not supported.
        /// 
        /// <p/><b>NOTE</b>: this method may suddenly change which
        /// implementation is returned from release to release, in
        /// the event that higher performance defaults become
        /// possible; if the precise implementation is important to
        /// your application, please instantiate it directly,
        /// instead. On 64 bit systems, it may also good to
        /// return <see cref="MMapDirectory" />, but this is disabled
        /// because of officially missing unmap support in Java.
        /// For optimal performance you should consider using
        /// this implementation on 64 bit JVMs.
        /// 
        /// <p/>See <a href="#subclasses">above</a> 
        /// </summary>
        public static FSDirectory Open(string path)
        {
            return Open(new DirectoryInfo(path), null);
        }

        /// <summary>Creates an FSDirectory instance, trying to pick the
        /// best implementation given the current environment.
        /// The directory returned uses the <see cref="NativeFSLockFactory" />.
        /// 
        /// <p/>Currently this returns <see cref="SimpleFSDirectory" /> as
        /// NIOFSDirectory is currently not supported.
        /// 
        /// <p/><b>NOTE</b>: this method may suddenly change which
        /// implementation is returned from release to release, in
        /// the event that higher performance defaults become
        /// possible; if the precise implementation is important to
        /// your application, please instantiate it directly,
        /// instead. On 64 bit systems, it may also good to
        /// return <see cref="MMapDirectory" />, but this is disabled
        /// because of officially missing unmap support in Java.
        /// For optimal performance you should consider using
        /// this implementation on 64 bit JVMs.
        /// 
        /// <p/>See <a href="#subclasses">above</a> 
        /// </summary>
        public static FSDirectory Open(DirectoryInfo path)
        {
            return Open(path, null);
        }

        /// <summary>Just like <see cref="Open(System.IO.DirectoryInfo)" />, but allows you to
        /// also specify a custom <see cref="LockFactory" />. 
        /// </summary>
        public static FSDirectory Open(DirectoryInfo path, LockFactory lockFactory)
        {
            if ((Constants.WINDOWS || Constants.SUN_OS || Constants.LINUX)
                && Constants.JRE_IS_64BIT && MMapDirectory.UNMAP_SUPPORTED)
            {
                return new MMapDirectory(path, lockFactory);
            }
            else if (Constants.WINDOWS)
            {
                return new SimpleFSDirectory(path, lockFactory);
            }
            else
            {
                //NIOFSDirectory is not implemented in Lucene.Net
                //return new NIOFSDirectory(path, lockFactory);
                return new SimpleFSDirectory(path, lockFactory);
            }
        }

        public override LockFactory LockFactory
        {
            get
            {
                return base.LockFactory;
            }
            set
            {
                base.LockFactory = value;

                // for filesystem based LockFactory, delete the lockPrefix, if the locks are placed
                // in index dir. If no index dir is given, set ourselves
                if (value is FSLockFactory)
                {
                    FSLockFactory lf = (FSLockFactory)value;
                    DirectoryInfo dir = lf.LockDir;
                    // if the lock factory has no lockDir set, use the this directory as lockDir
                    if (dir == null)
                    {
                        lf.LockDir = directory;
                        lf.LockPrefix = null;
                    }
                    else if (dir.FullName.Equals(directory.FullName))
                    {
                        lf.LockPrefix = null;
                    }
                }
            }
        }

        /// <summary>Lists all files (not subdirectories) in the
        /// directory.  This method never returns null (throws
        /// <see cref="System.IO.IOException" /> instead).
        /// 
        /// </summary>
        /// <throws>  NoSuchDirectoryException if the directory </throws>
        /// <summary>   does not exist, or does exist but is not a
        /// directory.
        /// </summary>
        /// <throws>  IOException if list() returns null  </throws>
        public static String[] ListAll(DirectoryInfo dir)
        {
            if (!dir.Exists)
            {
                throw new NoSuchDirectoryException("directory '" + dir.FullName + "' does not exist");
            }
            else if (File.Exists(dir.FullName))
            {
                throw new NoSuchDirectoryException("File '" + dir.FullName + "' does not exist");
            }

            // Exclude subdirs, only the file names, not the paths
            FileInfo[] files = dir.GetFiles();
            String[] result = new String[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                result[i] = files[i].Name;
            }

            // no reason to return null, if the directory cannot be listed, an exception 
            // will be thrown on the above call to dir.GetFiles()
            // use of LINQ to create the return value array may be a bit more efficient

            return result;
        }

        /// <summary>Lists all files (not subdirectories) in the
        /// directory.
        /// </summary>
        /// <seealso cref="ListAll(System.IO.DirectoryInfo)">
        /// </seealso>
        public override String[] ListAll()
        {
            EnsureOpen();
            return ListAll(directory);
        }

        /// <summary>Returns true iff a file with the given name exists. </summary>
        public override bool FileExists(String name)
        {
            EnsureOpen();
            FileInfo file = new FileInfo(Path.Combine(directory.FullName, name));
            return file.Exists;
        }

        /// <summary>Returns the time the named file was last modified. </summary>
        public static long FileModified(DirectoryInfo directory, String name)
        {
            FileInfo file = new FileInfo(Path.Combine(directory.FullName, name));
            return (long)file.LastWriteTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds; //{{LUCENENET-353}}
        }

        /// <summary>Returns the length in bytes of a file in the directory. </summary>
        public override long FileLength(String name)
        {
            EnsureOpen();
            FileInfo file = new FileInfo(Path.Combine(directory.FullName, name));
            return file.Exists ? file.Length : 0;
        }

        /// <summary>Removes an existing file in the directory. </summary>
        public override void DeleteFile(String name)
        {
            EnsureOpen();
            FileInfo file = new FileInfo(Path.Combine(directory.FullName, name));
            try
            {
                file.Delete();
            }
            catch (Exception)
            {
                throw new System.IO.IOException("Cannot delete " + file);
            }
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();
            EnsureCanWrite(name);
            return new FSIndexOutput(this, name);
        }

        protected void EnsureCanWrite(string name)
        {
            if (!directory.Exists)
            {
                try
                {
                    directory.Create();
                }
                catch
                {
                    throw new IOException("Cannot create directory: " + directory);
                }
            }

            FileInfo file = new FileInfo(Path.Combine(directory.FullName, name));
            if (file.Exists)
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    // delete existing, if any
                    throw new IOException("Cannot overwrite: " + file);
                }
            }
        }

        protected void OnIndexOutputClosed(FSIndexOutput io)
        {
            staleFiles.Add(io.Name);
        }

        public override void Sync(ICollection<string> names)
        {
            EnsureOpen();
            ISet<string> toSync = new HashSet<string>(names);
            toSync.IntersectWith(staleFiles);

            foreach (string name in toSync)
            {
                Fsync(name);
            }

            staleFiles.ExceptWith(toSync);
        }

        public override string LockId
        {
            get
            {
                EnsureOpen();
                String dirName;                               // name to be hashed
                try
                {
                    dirName = directory.FullName;
                }
                catch (IOException e)
                {
                    throw new SystemException(e.ToString(), e);
                }

                int digest = 0;
                for (int charIDX = 0; charIDX < dirName.Length; charIDX++)
                {
                    char ch = dirName[charIDX];
                    digest = 31 * digest + ch;
                }
                return "lucene-" + digest.ToString("X");
            }
        }

        protected override void Dispose(bool disposing)
        {
            lock (this)
            {
                isOpen = false;
            }
        }

        // Java Lucene implements GetFile() which returns a FileInfo.
        // For Lucene.Net, GetDirectory() is more appropriate

        public virtual DirectoryInfo Directory
        {
            get
            {
                EnsureOpen();
                return directory;
            }
        }

        /// <summary>For debug output. </summary>
        public override String ToString()
        {
            return this.GetType().FullName + "@" + directory + " lockFactory=" + LockFactory;
        }

        /// <summary> The maximum number of bytes to read at once from the
        /// underlying file during <see cref="IndexInput.ReadBytes(byte[],int,int)" />.
        /// </summary>
        /// <seealso cref="ReadChunkSize">
        /// </seealso>
        public int ReadChunkSize
        {
            get
            {
                // LUCENE-1566
                return chunkSize;
            }
            set
            {
                // LUCENE-1566
                if (value <= 0)
                {
                    throw new System.ArgumentException("chunkSize must be positive");
                }
                if (!Constants.JRE_IS_64BIT)
                {
                    this.chunkSize = value;
                }
            }
        }

        protected abstract class FSIndexInput : BufferedIndexInput
        {
            protected readonly FileStream file;
            bool isClone = false;
            protected readonly int chunkSize;
            protected readonly long off;
            protected readonly long end;

            protected FSIndexInput(string resourceDesc, FileInfo path, IOContext context, int chunkSize)
                : base(resourceDesc, context)
            {
                this.file = path.OpenRead();
                this.chunkSize = chunkSize;
                this.off = 0L;
                this.end = file.Length;
            }

            protected FSIndexInput(string resourceDesc, FileStream file, long off, long length, int bufferSize, int chunkSize)
                : base(resourceDesc, bufferSize)
            {
                this.file = file;
                this.chunkSize = chunkSize;
                this.off = off;
                this.end = off + length;
                this.isClone = true; // well, we are sorta?
            }

            protected override void Dispose(bool disposing)
            {
                // only close the file if this is not a clone
                if (!isClone)
                {
                    file.Dispose();
                }
            }

            public override object Clone()
            {
                FSIndexInput clone = (FSIndexInput)base.Clone();
                clone.isClone = true;
                return clone;
            }

            public override long Length
            {
                get { return end - off; }
            }

            internal bool IsFDValid
            {
                get
                {
                    return true; // .NET port: not sure what to do here
                }
            }
        }

        protected class FSIndexOutput : BufferedIndexOutput
        {
            private readonly FSDirectory parent;
            private readonly String name;
            private readonly FileStream file;
            private volatile bool isOpen; // remember if the file is open, so that we don't try to close it more than once

            public FSIndexOutput(FSDirectory parent, string name)
            {
                this.parent = parent;
                this.name = name;
                file = new FileStream(Path.Combine(parent.directory.FullName, name), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                isOpen = true;
            }

            public string Name
            {
                get { return name; }
            }

            public override void FlushBuffer(byte[] b, int offset, int size)
            {
                //assert isOpen;
                file.Write(b, offset, size);
            }

            protected override void Dispose(bool disposing)
            {
                parent.OnIndexOutputClosed(this);
                // only close the file if it has not been closed yet
                if (isOpen)
                {
                    bool success = false;
                    try
                    {
                        base.Dispose(true);
                        success = true;
                    }
                    finally
                    {
                        isOpen = false;
                        if (!success)
                        {
                            try
                            {
                                file.Dispose();
                            }
                            catch
                            {
                                // Suppress so we don't mask original exception
                            }
                        }
                        else
                        {
                            file.Dispose();
                        }
                    }
                }
            }

            public override void Seek(long pos)
            {
                base.Seek(pos);
                file.Seek(pos, SeekOrigin.Begin);
            }

            public override long Length
            {
                get { return file.Length; }
            }

            public override void SetLength(long length)
            {
                base.SetLength(length);
                file.SetLength(length);
            }
        }

        protected void Fsync(String name)
        {
            FileInfo fullFile = new FileInfo(Path.Combine(directory.FullName, name));
            bool success = false;
            int retryCount = 0;
            IOException exc = null;
            while (!success && retryCount < 5)
            {
                retryCount++;
                FileStream file = null;
                try
                {
                    try
                    {
                        file = new FileStream(fullFile.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        //file.getFD().sync();
                        // TODO: .NET Port: what do we do here?
                        success = true;
                    }
                    finally
                    {
                        if (file != null)
                            file.Dispose();
                    }
                }
                catch (IOException ioe)
                {
                    if (exc == null)
                        exc = ioe;
                    try
                    {
                        // Pause 5 msec
                        Thread.Sleep(5);
                    }
                    catch (ThreadInterruptedException)
                    {
                        throw;
                    }
                }
            }
            if (!success)
                // Throw original exception
                throw exc;
        }
    }
}