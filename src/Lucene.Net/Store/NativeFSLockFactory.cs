using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Collections.Generic;
using Lucene.Net.Support.Threading;

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
    /// <para>Implements <see cref="LockFactory"/> using native OS file
    /// locks.  For NFS based access to an index, it's
    /// recommended that you try <see cref="SimpleFSLockFactory"/>
    /// first and work around the one limitation that a lock file
    /// could be left when the runtime exits abnormally.</para>
    ///
    /// <para>The primary benefit of <see cref="NativeFSLockFactory"/> is
    /// that locks (not the lock file itsself) will be properly
    /// removed (by the OS) if the runtime has an abnormal exit.</para>
    ///
    /// <para>Note that, unlike <see cref="SimpleFSLockFactory"/>, the existence of
    /// leftover lock files in the filesystem is fine because the OS
    /// will free the locks held against these files even though the
    /// files still remain. Lucene will never actively remove the lock
    /// files, so although you see them, the index may not be locked.</para>
    ///
    /// <para>Special care needs to be taken if you change the locking
    /// implementation: First be certain that no writer is in fact
    /// writing to the index otherwise you can easily corrupt
    /// your index. Be sure to do the <see cref="LockFactory"/> change on all Lucene
    /// instances and clean up all leftover lock files before starting
    /// the new configuration for the first time. Different implementations
    /// can not work together!</para>
    ///
    /// <para>If you suspect that this or any other <see cref="LockFactory"/> is
    /// not working properly in your environment, you can easily
    /// test it by using <see cref="VerifyingLockFactory"/>,
    /// <see cref="LockVerifyServer"/> and <see cref="LockStressTest"/>.</para>
    /// </summary>
    /// <seealso cref="LockFactory"/>
    // LUCENENET specific - this class has been refactored significantly from its Java counterpart
    // to take advantage of .NET FileShare locking in the Windows and Linux environments.
    public class NativeFSLockFactory : FSLockFactory
    {
        internal enum FSLockingStrategy
        {
            FileStreamLockViolation,
            FileSharingViolation,
            Fallback
        }

        // LUCENENET: This controls the locking strategy used for the current operating system and framework
        internal static FSLockingStrategy LockingStrategy
        {
            get
            {
                if (IS_FILESTREAM_LOCKING_PLATFORM && HRESULT_FILE_LOCK_VIOLATION.HasValue)
                    return FSLockingStrategy.FileStreamLockViolation;
                else if (HRESULT_FILE_SHARE_VIOLATION.HasValue)
                    return FSLockingStrategy.FileSharingViolation;
                else
                    // Fallback implementation for unknown platforms that don't rely on HResult
                    return FSLockingStrategy.Fallback;
            }
        }


        // LUCNENENET NOTE: Lookup the HResult value we are interested in for the current OS
        // by provoking the exception during initialization and caching its HResult value for later.
        // We optimize for Windows because those HResult values are known and documented, but for
        // other platforms, this is the only way we can reliably determine the HResult values
        // we are interested in.
        //
        // Reference: https://stackoverflow.com/q/46380483
        private static readonly bool IS_FILESTREAM_LOCKING_PLATFORM = LoadIsFileStreamLockingPlatform();

        private const int WIN_HRESULT_FILE_LOCK_VIOLATION = unchecked((int)0x80070021);
        private const int WIN_HRESULT_FILE_SHARE_VIOLATION = unchecked((int)0x80070020);

        internal static readonly int? HRESULT_FILE_LOCK_VIOLATION = LoadFileLockViolationHResult();
        internal static readonly int? HRESULT_FILE_SHARE_VIOLATION = LoadFileShareViolationHResult();

        private static bool LoadIsFileStreamLockingPlatform()
        {
#if FEATURE_FILESTREAM_LOCK
            return Constants.WINDOWS; // LUCENENET: See: https://github.com/dotnet/corefx/issues/5964
#else
            return false;
#endif
        }

        private static int? LoadFileLockViolationHResult()
        {
            if (Constants.WINDOWS)
                return WIN_HRESULT_FILE_LOCK_VIOLATION;

            // Skip provoking the exception unless we know we will use the value
            if (IS_FILESTREAM_LOCKING_PLATFORM)
            {
                return FileSupport.GetFileIOExceptionHResult(provokeException: (fileName) =>
                {
                    using var lockStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
#pragma warning disable CA1416 // Validate platform compatibility
                    lockStream.Lock(0, 1); // Create an exclusive lock
#pragma warning restore CA1416 // Validate platform compatibility
                    using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                    // try to find out if the file is locked by writing a byte. Note that we need to flush the stream to find out.
                    stream.WriteByte(0);
                    stream.Flush();   // this *may* throw an IOException if the file is locked, but...
                                      // ... closing the stream is the real test
                });
            }

            return null;
        }

        private static int? LoadFileShareViolationHResult()
        {
            if (Constants.WINDOWS)
                return WIN_HRESULT_FILE_SHARE_VIOLATION;

            return FileSupport.GetFileIOExceptionHResult(provokeException: (fileName) =>
            {
                using var lockStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 1, FileOptions.None);
                // Try to get an exclusive lock on the file - this should throw an IOException with the current platform's HResult value for FileShare violation
                using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.None, 1, FileOptions.None);
            });
        }

        /// <summary>
        /// Create a <see cref="NativeFSLockFactory"/> instance, with <c>null</c> (unset)
        /// lock directory. When you pass this factory to a <see cref="FSDirectory"/>
        /// subclass, the lock directory is automatically set to the
        /// directory itself. Be sure to create one instance for each directory
        /// your create!
        /// </summary>
        public NativeFSLockFactory()
            : this((DirectoryInfo)null)
        {
        }

        /// <summary>
        /// Create a <see cref="NativeFSLockFactory"/> instance, storing lock
        /// files into the specified <paramref name="lockDirName"/>
        /// </summary>
        /// <param name="lockDirName"> where lock files are created. </param>
        public NativeFSLockFactory(string lockDirName)
            : this(new DirectoryInfo(lockDirName))
        {
        }

        /// <summary>
        /// Create a <see cref="NativeFSLockFactory"/> instance, storing lock
        /// files into the specified <paramref name="lockDir"/>
        /// </summary>
        /// <param name="lockDir"> where lock files are created. </param>
        public NativeFSLockFactory(DirectoryInfo lockDir)
        {
            SetLockDir(lockDir);
        }

        // LUCENENET: NativeFSLocks in Java are infact singletons; this is how we mimick that to track instances and make sure
        // IW.Unlock and IW.IsLocked works correctly
        internal static readonly Dictionary<string, Lock> _locks = new Dictionary<string, Lock>();

        /// <summary>
        /// Given a lock name, return the full prefixed path of the actual lock file.
        /// </summary>
        /// <param name="lockName"></param>
        /// <returns></returns>
        private string GetCanonicalPathOfLockFile(string lockName)
        {
            if (m_lockPrefix != null)
            {
                lockName = m_lockPrefix + "-" + lockName;
            }
            return new FileInfo(Path.Combine(m_lockDir.FullName, lockName)).GetCanonicalPath();
        }

        public override Lock MakeLock(string lockName)
        {
            var path = GetCanonicalPathOfLockFile(lockName);
            Lock l;
            UninterruptableMonitor.Enter(_locks);
            try
            {
                if (!_locks.TryGetValue(path, out l))
                    _locks.Add(path, l = NewLock(path));
            }
            finally
            {
                UninterruptableMonitor.Exit(_locks);
            }
            return l;
        }

        // Internal for testing
        internal virtual Lock NewLock(string path)
        {
            switch (LockingStrategy)
            {
                case FSLockingStrategy.FileStreamLockViolation:
#pragma warning disable CA1416 // Validate platform compatibility
                    return new NativeFSLock(m_lockDir, path);
#pragma warning restore CA1416 // Validate platform compatibility
                case FSLockingStrategy.FileSharingViolation:
                    return new SharingNativeFSLock(m_lockDir, path);
                default:
                    // Fallback implementation for unknown platforms that don't rely on HResult
                    return new FallbackNativeFSLock(m_lockDir, path);
            }
        }

        public override void ClearLock(string lockName)
        {
            var path = GetCanonicalPathOfLockFile(lockName);
            // this is the reason why we can't use ConcurrentDictionary: we need the removal and disposal of the lock to be atomic
            // otherwise it may clash with MakeLock making a lock and ClearLock disposing of it in another thread.
            UninterruptableMonitor.Enter(_locks);
            try
            {
                if (_locks.TryGetValue(path, out Lock l))
                {
                    _locks.Remove(path);
                    l.Dispose();
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(_locks);
            }
        }
    }


    // LUCENENET NOTE: We use this implementation as a fallback for platforms that we don't
    // know the HResult numbers for lock and file sharing errors.
    //
    // Note that using NativeFSLock would be ideal for all platforms. However, there is a
    // small chance that provoking lock/share exceptions will fail. In that rare case, we
    // fallback to this substandard implementation.
    // 
    // Reference: https://stackoverflow.com/q/46380483
    internal class FallbackNativeFSLock : Lock
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private FileStream channel;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly string path;
        private readonly DirectoryInfo lockDir;

        public FallbackNativeFSLock(DirectoryInfo lockDir, string path)
        {
            this.lockDir = lockDir;
            this.path = path;
        }

        public override bool Obtain()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                FailureReason = null;

                if (channel != null)
                {
                    // Our instance is already locked:
                    return false;
                }

                if (!System.IO.Directory.Exists(lockDir.FullName))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(lockDir.FullName);
                    }
                    catch
                    {
                        throw new IOException("Cannot create directory: " + lockDir.FullName);
                    }
                }
                else if (File.Exists(lockDir.FullName))
                {
                    throw new IOException("Found regular file where directory expected: " + lockDir.FullName);
                }

                var success = false;
                try
                {
                    // LUCENENET: Allow read access for the RAMDirectory to be able to copy the lock file.
                    channel = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);

                    success = true;
                }
                catch (Exception e) when (e.IsIOException())
                {
                    // At least on OS X, we will sometimes get an
                    // intermittent "Permission Denied" Exception,
                    // which seems to simply mean "you failed to get
                    // the lock".  But other IOExceptions could be
                    // "permanent" (eg, locking is not supported via
                    // the filesystem).  So, we record the failure
                    // reason here; the timeout obtain (usually the
                    // one calling us) will use this as "root cause"
                    // if it fails to get the lock.
                    FailureReason = e;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.DisposeWhileHandlingException(channel);
                        channel = null;
                    }
                }

                return channel != null;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    // whether or not we have created a file, we need to remove
                    // the lock instance from the dictionary that tracks them.
                    try
                    {
                        UninterruptableMonitor.Enter(NativeFSLockFactory._locks);
                        try
                        {
                            NativeFSLockFactory._locks.Remove(path);
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(NativeFSLockFactory._locks);
                        }
                    }
                    finally
                    {
                        if (channel != null)
                        {
                            IOUtils.DisposeWhileHandlingException(channel);
                            channel = null;

                            bool tmpBool;
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                                tmpBool = true;
                            }
                            else if (System.IO.Directory.Exists(path))
                            {
                                System.IO.Directory.Delete(path);
                                tmpBool = true;
                            }
                            else
                            {
                                tmpBool = false;
                            }
                            if (!tmpBool)
                            {
                                throw new LockReleaseFailedException("failed to delete " + path);
                            }
                        }
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        public override bool IsLocked()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // The test for is isLocked is not directly possible with native file locks:

                // First a shortcut, if a lock reference in this instance is available
                if (channel != null)
                {
                    return true;
                }

                // Look if lock file is present; if not, there can definitely be no lock!
                bool tmpBool;
                if (System.IO.File.Exists(path))
                    tmpBool = true;
                else
                    tmpBool = System.IO.Directory.Exists(path);
                if (!tmpBool)
                    return false;

                // Try to obtain and release (if was locked) the lock
                try
                {
                    bool obtained = Obtain();
                    if (obtained)
                    {
                        Dispose();
                    }
                    return !obtained;
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    return false;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override string ToString()
        {
            return $"{nameof(FallbackNativeFSLock)}@{path}";
        }
    }

    // Locks the entire file. macOS requires this approach.
    internal class SharingNativeFSLock : Lock
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private FileStream channel;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly string path;
        private readonly DirectoryInfo lockDir;

        public SharingNativeFSLock(DirectoryInfo lockDir, string path)
        {
            this.lockDir = lockDir;
            this.path = path;
        }

        /// <summary>
        /// Return true if the <see cref="IOException"/> is the result of a share violation
        /// </summary>
        private static bool IsShareViolation(IOException e) // LUCENENET: CA1822: Mark members as static
        {
            return e.HResult == NativeFSLockFactory.HRESULT_FILE_SHARE_VIOLATION;
        }

        private FileStream GetLockFileStream(FileMode mode)
        {
            if (!System.IO.Directory.Exists(lockDir.FullName))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(lockDir.FullName);
                }
                catch (Exception e)
                {
                    // note that several processes might have been trying to create the same directory at the same time.
                    // if one succeeded, the directory will exist and the exception can be ignored. In all other cases we should report it.
                    if (!System.IO.Directory.Exists(lockDir.FullName))
                        throw new IOException("Cannot create directory: " + lockDir.FullName, e);
                }
            }
            else if (File.Exists(lockDir.FullName))
            {
                throw new IOException("Found regular file where directory expected: " + lockDir.FullName);
            }

            return new FileStream(
                path,
                mode,
                FileAccess.Write,
                // LUCENENET: Allow read access of OpenOrCreate for the RAMDirectory to be able to copy the lock file.
                // For the Open case, set to FileShare.None to force a file share exception in IsLocked().
                share: mode == FileMode.Open ? FileShare.None : FileShare.Read,
                bufferSize: 1,
                options: mode == FileMode.Open ? FileOptions.None : FileOptions.DeleteOnClose);
        }

        public override bool Obtain()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                FailureReason = null;

                if (channel != null)
                {
                    // Our instance is already locked:
                    return false;
                }
                try
                {
                    channel = GetLockFileStream(FileMode.OpenOrCreate);
                }
                catch (IOException e) when (IsShareViolation(e))
                {
                    // no failure reason to be recorded, since this is the expected error if a lock exists
                }
                catch (Exception e) when (e.IsIOException())
                {
                    // At least on OS X, we will sometimes get an
                    // intermittent "Permission Denied" Exception,
                    // which seems to simply mean "you failed to get
                    // the lock".  But other IOExceptions could be
                    // "permanent" (eg, locking is not supported via
                    // the filesystem).  So, we record the failure
                    // reason here; the timeout obtain (usually the
                    // one calling us) will use this as "root cause"
                    // if it fails to get the lock.
                    FailureReason = e;
                }
                return channel != null;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    // whether or not we have created a file, we need to remove
                    // the lock instance from the dictionary that tracks them.
                    try
                    {
                        UninterruptableMonitor.Enter(NativeFSLockFactory._locks);
                        try
                        {
                            NativeFSLockFactory._locks.Remove(path);
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(NativeFSLockFactory._locks);
                        }
                    }
                    finally
                    {
                        if (channel != null)
                        {
                            try
                            {
                                IOUtils.DisposeWhileHandlingException(channel);
                            }
                            finally
                            {
                                channel = null;
                            }
                        }
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        public override bool IsLocked()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // First a shortcut, if a lock reference in this instance is available
                if (channel != null)
                {
                    return true;
                }

                try
                {
                    using (var stream = GetLockFileStream(FileMode.Open))
                    {
                    }
                    return false;
                }
                catch (IOException e) when (IsShareViolation(e))
                {
                    return true;
                }
                catch (FileNotFoundException)
                {
                    // if the file doesn't exists, there can be no lock active
                    return false;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override string ToString()
        {
            return $"{nameof(SharingNativeFSLock)}@{path}";
        }
    }

    // Uses FileStream locking of file pages.
#if NET6_0
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    internal class NativeFSLock : Lock
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private FileStream channel;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly string path;
        private readonly DirectoryInfo lockDir;

        public NativeFSLock(DirectoryInfo lockDir, string path)
        {
            this.lockDir = lockDir;
            this.path = path;
        }

        /// <summary>
        /// Return true if the <see cref="IOException"/> is the result of a lock violation
        /// </summary>
        private static bool IsLockViolation(IOException e) // LUCENENET: CA1822: Mark members as static
        {
            return e.HResult == NativeFSLockFactory.HRESULT_FILE_LOCK_VIOLATION;
        }

        private FileStream GetLockFileStream(FileMode mode)
        {
            if (!System.IO.Directory.Exists(lockDir.FullName))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(lockDir.FullName);
                }
                catch (Exception e)
                {
                    // note that several processes might have been trying to create the same directory at the same time.
                    // if one succeeded, the directory will exist and the exception can be ignored. In all other cases we should report it.
                    if (!System.IO.Directory.Exists(lockDir.FullName))
                        throw new IOException("Cannot create directory: " + lockDir.FullName, e);
                }
            }
            else if (File.Exists(lockDir.FullName))
            {
                throw new IOException("Found regular file where directory expected: " + lockDir.FullName);
            }

            return new FileStream(path, mode, FileAccess.Write, FileShare.ReadWrite);
        }

        public override bool Obtain()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                FailureReason = null;

                if (channel != null)
                {
                    // Our instance is already locked:
                    return false;
                }

                FileStream stream = null;
                try
                {
                    stream = GetLockFileStream(FileMode.OpenOrCreate);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    // At least on OS X, we will sometimes get an
                    // intermittent "Permission Denied" Exception,
                    // which seems to simply mean "you failed to get
                    // the lock".  But other IOExceptions could be
                    // "permanent" (eg, locking is not supported via
                    // the filesystem).  So, we record the failure
                    // reason here; the timeout obtain (usually the
                    // one calling us) will use this as "root cause"
                    // if it fails to get the lock.
                    FailureReason = e;
                }

                if (stream != null)
                {
                    try
                    {
                        stream.Lock(0, 1);
                        // only assign the channel if the lock succeeds
                        channel = stream;
                    }
                    catch (Exception e)
                    {
                        FailureReason = e;
                        IOUtils.DisposeWhileHandlingException(stream);
                    }
                }
                return channel != null;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    // whether or not we have created a file, we need to remove
                    // the lock instance from the dictionary that tracks them.
                    try
                    {
                        UninterruptableMonitor.Enter(NativeFSLockFactory._locks);
                        try
                        {
                            NativeFSLockFactory._locks.Remove(path);
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(NativeFSLockFactory._locks);
                        }
                    }
                    finally
                    {
                        if (channel != null)
                        {
                            try
                            {
                                IOUtils.DisposeWhileHandlingException(channel);
                            }
                            finally
                            {
                                channel = null;
                            }
                            // try to delete the file if we created it, but it's not an error if we can't.
                            try
                            {
                                File.Delete(path);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        public override bool IsLocked()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // First a shortcut, if a lock reference in this instance is available
                if (channel != null)
                {
                    return true;
                }

                try
                {
                    using (var stream = GetLockFileStream(FileMode.Open))
                    {
                        // try to find out if the file is locked by writing a byte. Note that we need to flush the stream to find out.
                        stream.WriteByte(0);
                        stream.Flush();   // this *may* throw an IOException if the file is locked, but...
                                          // ... closing the stream is the real test
                    }
                    return false;
                }
                catch (IOException e) when (IsLockViolation(e))
                {
                    return true;
                }
                catch (FileNotFoundException)
                {
                    // if the file doesn't exists, there can be no lock active
                    return false;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override string ToString()
        {
            return $"{nameof(NativeFSLock)}@{path}";
        }
    }

#if !FEATURE_FILESTREAM_LOCK
    internal static class FileStreamExtensions
    {
        // Dummy lock method to ensure we can compile even if the feature is unavailable
        public static void Lock(this FileStream stream, long position, long length)
        {
        }
    }
#endif
}