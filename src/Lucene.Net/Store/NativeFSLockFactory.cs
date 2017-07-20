using Lucene.Net.Util;
using System;
using System.Collections.Concurrent;
using System.IO;

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
        internal static readonly ConcurrentDictionary<string, Lazy<NativeFSLock>> _locks = new ConcurrentDictionary<string, Lazy<NativeFSLock>>();

        public override Lock MakeLock(string lockName)
        {
            var path = new DirectoryInfo(System.IO.Path.Combine(m_lockDir.FullName, lockName));
            return _locks.GetOrAdd(path.FullName, s => new Lazy<NativeFSLock>(() => new NativeFSLock(this, m_lockDir, s))).Value;
        }

        public override void ClearLock(string lockName)
        {
            using (var _ = MakeLock(lockName)) { }
        }
    }

    internal class NativeFSLock : Lock
    {
        private readonly NativeFSLockFactory outerInstance;

        private FileStream channel;
        private readonly DirectoryInfo path;
        private readonly DirectoryInfo lockDir;

        public NativeFSLock(NativeFSLockFactory outerInstance, DirectoryInfo lockDir, string lockFileName)
        {
            this.outerInstance = outerInstance;
            this.lockDir = lockDir;
            path = new DirectoryInfo(System.IO.Path.Combine(lockDir.FullName, lockFileName));
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
                    channel = new FileStream(path.FullName, FileMode.Create, FileAccess.Write, FileShare.None);

                    success = true;
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
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
                {
                    if (channel != null)
                    {
                        IOUtils.DisposeWhileHandlingException(channel);
                        channel = null;

                        bool tmpBool;
                        if (File.Exists(path.FullName))
                        {
                            File.Delete(path.FullName);
                            tmpBool = true;
                        }
                        else if (System.IO.Directory.Exists(path.FullName))
                        {
                            System.IO.Directory.Delete(path.FullName);
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
        }

        public override bool IsLocked()
        {
            lock (this)
            {
                // The test for is isLocked is not directly possible with native file locks:

                // First a shortcut, if a lock reference in this instance is available
                if (channel != null)
                {
                    return true;
                }

                // Look if lock file is present; if not, there can definitely be no lock!
                bool tmpBool;
                if (System.IO.File.Exists(path.FullName))
                    tmpBool = true;
                else
                    tmpBool = System.IO.Directory.Exists(path.FullName);
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
                catch (IOException)
                {
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