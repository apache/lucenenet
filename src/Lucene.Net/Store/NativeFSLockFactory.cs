using Lucene.Net.Util;
using System;
using System.IO;
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
    public class NativeFSLockFactory : FSLockFactory
    {
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
        internal static readonly Dictionary<string, NativeFSLock> _locks = new Dictionary<string, NativeFSLock>();

        /// <summary>
        /// Given a lock name, return the full prefixed path of the actual lock file.
        /// </summary>
        /// <param name="lockName"></param>
        /// <returns></returns>
        private string GetPathOfLockFile(string lockName)
        {
            if (m_lockPrefix != null)
            {
                lockName = m_lockPrefix + "-" + lockName;
            }
            return Path.Combine(m_lockDir.FullName, lockName);
        }

        public override Lock MakeLock(string lockName)
        {
            var path = GetPathOfLockFile(lockName);
            NativeFSLock l;
            lock (_locks)
                if (!_locks.TryGetValue(path, out l))
                    _locks.Add(path, l = new NativeFSLock(this, m_lockDir, path));
            return l;
        }

        public override void ClearLock(string lockName)
        {
            var path = GetPathOfLockFile(lockName);
            NativeFSLock l;
            // this is the reason why we can't use ConcurrentDictionary: we need the removal and disposal of the lock to be atomic
            // otherwise it may clash with MakeLock making a lock and ClearLock disposing of it in another thread.
            lock (_locks)
                if (_locks.TryGetValue(path, out l))
                {
                    _locks.Remove(path);
                    l.Dispose();
                }
        }
    }

    internal class NativeFSLock : Lock
    {
#if FEATURE_FILESTREAM_LOCK
        private const int ERROR_LOCK_VIOLATION = 0x21;
#else
        private const int ERROR_SHARE_VIOLATION = 0x20;
#endif

        private readonly NativeFSLockFactory outerInstance;

        private FileStream channel;
        private readonly string path;
        private readonly DirectoryInfo lockDir;

        public NativeFSLock(NativeFSLockFactory outerInstance, DirectoryInfo lockDir, string path)
        {
            this.outerInstance = outerInstance;
            this.lockDir = lockDir;
            this.path = path;
        }

        /// <summary>
        /// Return true if the <see cref="IOException"/> is the result of a lock violation
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static bool IsLockOrShareViolation(IOException e)
        {
            var result = e.HResult & 0xFFFF;
#if FEATURE_FILESTREAM_LOCK
            return result == ERROR_LOCK_VIOLATION;
#else
            return result == ERROR_SHARE_VIOLATION;
#endif
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

#if FEATURE_FILESTREAM_LOCK
            return new FileStream(path, mode, FileAccess.Write, FileShare.ReadWrite);
#else
            return new FileStream(path, mode, FileAccess.Write, FileShare.None, 1, mode == FileMode.Open ? FileOptions.None : FileOptions.DeleteOnClose);
#endif
        }

        public override bool Obtain()
        {
            lock (this)
            {
                FailureReason = null;

                if (channel != null)
                {
                    // Our instance is already locked:
                    return false;
                }

#if FEATURE_FILESTREAM_LOCK
                FileStream stream = null;
                try
                {
                    stream = GetLockFileStream(FileMode.OpenOrCreate);
                }
                catch (IOException e)
                {
                    FailureReason = e;
                }
                // LUCENENET: UnauthorizedAccessException does not derive from IOException like in java
                catch (UnauthorizedAccessException e)
                {
                    // On Windows, we can get intermittent "Access
                    // Denied" here.  So, we treat this as failure to
                    // acquire the lock, but, store the reason in case
                    // there is in fact a real error case.
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
#else
                try
                {
                    channel = GetLockFileStream(FileMode.OpenOrCreate);
                }
                catch (IOException e) when (IsLockOrShareViolation(e))
                {
                    // no failure reason to be recorded, since this is the expected error if a lock exists
                }
                catch (IOException e)
                {
                    FailureReason = e;
                }
                // LUCENENET: UnauthorizedAccessException does not derive from IOException like in java
                catch (UnauthorizedAccessException e)
                {
                    // On Windows, we can get intermittent "Access
                    // Denied" here.  So, we treat this as failure to
                    // acquire the lock, but, store the reason in case
                    // there is in fact a real error case.
                    FailureReason = e;
                }
#endif
                return channel != null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
                {
                    if (channel != null)
                    {
                        try
                        {
                            NativeFSLockFactory._locks.Remove(path);
                        }
                        finally
                        {
                            IOUtils.DisposeWhileHandlingException(channel);
                            channel = null;
                        }
#if FEATURE_FILESTREAM_LOCK
                        // try to delete the file if we created it, but it's not an error if we can't.
                        try
                        {
                            File.Delete(path);
                        }
                        catch
                        {
                        }
#endif
                    }
                }
            }
        }

        public override bool IsLocked()
        {
            lock (this)
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
#if FEATURE_FILESTREAM_LOCK
                        // try to find out if the file is locked by writing a byte. Note that we need to flush the stream to find out.
                        stream.WriteByte(0);
                        stream.Flush();   // this *may* throw an IOException if the file is locked, but...
                                          // ... closing the stream is the real test
#endif
                    }
                    return false;
                }
                catch (IOException e) when (IsLockOrShareViolation(e))
                {
                    return true;
                }
                catch (FileNotFoundException)
                {
                    // if the file doesn't exists, there can be no lock active
                    return false;
                }
            }
        }

        public override string ToString()
        {
            return "NativeFSLock@" + path;
        }
    }
}
