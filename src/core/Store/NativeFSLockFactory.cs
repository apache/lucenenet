namespace Lucene.Net.Store
{
    using System.Collections.Generic;
    using System.IO;

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
    /// <p>Implements <seealso cref="LockFactory"/> using native OS file
    /// locks.  Note that because this LockFactory relies on
    /// java.nio.* APIs for locking, any problems with those APIs
    /// will cause locking to fail.  Specifically, on certain NFS
    /// environments the java.nio.* locks will fail (the lock can
    /// incorrectly be double acquired) whereas {@link
    /// SimpleFSLockFactory} worked perfectly in those same
    /// environments.  For NFS based access to an index, it's
    /// recommended that you try <seealso cref="SimpleFSLockFactory"/>
    /// first and work around the one limitation that a lock file
    /// could be left when the JVM exits abnormally.</p>
    ///
    /// <p>The primary benefit of <seealso cref="NativeFSLockFactory"/> is
    /// that locks (not the lock file itsself) will be properly
    /// removed (by the OS) if the JVM has an abnormal exit.</p>
    ///
    /// <p>Note that, unlike <seealso cref="SimpleFSLockFactory"/>, the existence of
    /// leftover lock files in the filesystem is fine because the OS
    /// will free the locks held against these files even though the
    /// files still remain. Lucene will never actively remove the lock
    /// files, so although you see them, the index may not be locked.</p>
    ///
    /// <p>Special care needs to be taken if you change the locking
    /// implementation: First be certain that no writer is in fact
    /// writing to the index otherwise you can easily corrupt
    /// your index. Be sure to do the LockFactory change on all Lucene
    /// instances and clean up all leftover lock files before starting
    /// the new configuration for the first time. Different implementations
    /// can not work together!</p>
    ///
    /// <p>If you suspect that this or any other LockFactory is
    /// not working properly in your environment, you can easily
    /// test it by using <seealso cref="VerifyingLockFactory"/>, {@link
    /// LockVerifyServer} and <seealso cref="LockStressTest"/>.</p>
    /// </summary>
    /// <seealso cref= LockFactory </seealso>

    public class NativeFSLockFactory : FSLockFactory
    {
        /// <summary>
        /// Create a NativeFSLockFactory instance, with null (unset)
        /// lock directory. When you pass this factory to a <seealso cref="FSDirectory"/>
        /// subclass, the lock directory is automatically set to the
        /// directory itself. Be sure to create one instance for each directory
        /// your create!
        /// </summary>
        public NativeFSLockFactory()
            : this((DirectoryInfo)null)
        {
        }

        /// <summary>
        /// Create a NativeFSLockFactory instance, storing lock
        /// files into the specified lockDirName:
        /// </summary>
        /// <param name="lockDirName"> where lock files are created. </param>
        public NativeFSLockFactory(string lockDirName)
            : this(new DirectoryInfo(lockDirName))
        {
        }

        /// <summary>
        /// Create a NativeFSLockFactory instance, storing lock
        /// files into the specified lockDir:
        /// </summary>
        /// <param name="lockDir"> where lock files are created. </param>
        public NativeFSLockFactory(DirectoryInfo lockDir)
        {
            LockDir = lockDir;
        }

        public override Lock MakeLock(string lockName)
        {
            lock (this)
            {
                if (LockPrefix_Renamed != null)
                {
                    lockName = LockPrefix_Renamed + "-" + lockName;
                }
                return new NativeFSLock(LockDir_Renamed, lockName);
            }
        }

        public override void ClearLock(string lockName)
        {
            MakeLock(lockName).Release();
        }
    }

    internal class NativeFSLock : Lock
    {
        private System.IO.FileStream f;
        private FileStream Channel;
        private bool @lock;
        private DirectoryInfo Path;
        private DirectoryInfo LockDir;

        /*
       * The javadocs for FileChannel state that you should have
       * a single instance of a FileChannel (per JVM) for all
       * locking against a given file.  To ensure this, we have
       * a single (static) HashSet that contains the file paths
       * of all currently locked locks.  This protects against
       * possible cases where different Directory instances in
       * one JVM (each with their own NativeFSLockFactory
       * instance) have set the same lock dir and lock prefix.
       */
        private static HashSet<string> LOCK_HELD = new HashSet<string>();

        public NativeFSLock(DirectoryInfo lockDir, string lockFileName)
        {
            this.LockDir = lockDir;
            Path = new DirectoryInfo(System.IO.Path.Combine(lockDir.FullName, lockFileName));
        }

        private bool LockExists()
        {
            lock (this)
            {
                return @lock != false;
            }
        }

        public override bool Obtain()
        {
            lock (this)
            {
                if (LockExists())
                {
                    // Our instance is already locked:
                    return false;
                }

                // Ensure that lockDir exists and is a directory.
                bool tmpBool;
                if (System.IO.File.Exists(LockDir.FullName))
                    tmpBool = true;
                else
                    tmpBool = System.IO.Directory.Exists(LockDir.FullName);

                if (!tmpBool)
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(LockDir.FullName);
                    }
                    catch
                    {
                        throw new System.IO.IOException("Cannot create directory: " + LockDir.FullName);
                    }
                }
                else if (!System.IO.Directory.Exists(LockDir.FullName))
                {
                    throw new System.IO.IOException("Found regular file where directory expected: " + LockDir.FullName);
                }

                string canonicalPath = Path.FullName;

                bool markedHeld = false;

                try
                {
                    // Make sure nobody else in-process has this lock held
                    // already, and, mark it held if not:

                    lock (LOCK_HELD)
                    {
                        if (LOCK_HELD.Contains(canonicalPath))
                        {
                            // Someone else in this JVM already has the lock:
                            return false;
                        }
                        else
                        {
                            // This "reserves" the fact that we are the one
                            // thread trying to obtain this lock, so we own
                            // the only instance of a channel against this
                            // file:
                            LOCK_HELD.Add(canonicalPath);
                            markedHeld = true;
                        }
                    }

                    try
                    {
                        f = new System.IO.FileStream(Path.FullName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                    }
                    catch (System.IO.IOException e)
                    {
                        // On Windows, we can get intermittent "Access
                        // Denied" here.  So, we treat this as failure to
                        // acquire the lock, but, store the reason in case
                        // there is in fact a real error case.
                        FailureReason = e;
                        f = null;
                    }
                    // lucene.net: UnauthorizedAccessException does not derive from IOException like in java
                    catch (System.UnauthorizedAccessException e)
                    {
                        // On Windows, we can get intermittent "Access
                        // Denied" here.  So, we treat this as failure to
                        // acquire the lock, but, store the reason in case
                        // there is in fact a real error case.
                        FailureReason = e;
                        f = null;
                    }

                    if (f != null)
                    {
                        try
                        {
                            Channel = f;
                            @lock = false;
                            try
                            {
                                Channel.Lock(0, Channel.Length);
                                @lock = true;
                            }
                            catch (System.IO.IOException e)
                            {
                                // At least on OS X, we will sometimes get an
                                // intermittent "Permission Denied" IOException,
                                // which seems to simply mean "you failed to get
                                // the lock".  But other IOExceptions could be
                                // "permanent" (eg, locking is not supported via
                                // the filesystem).  So, we record the failure
                                // reason here; the timeout obtain (usually the
                                // one calling us) will use this as "root cause"
                                // if it fails to get the lock.
                                FailureReason = e;
                            }
                            // lucene.net: UnauthorizedAccessException does not derive from IOException like in java
                            catch (System.UnauthorizedAccessException e)
                            {
                                // At least on OS X, we will sometimes get an
                                // intermittent "Permission Denied" IOException,
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
                                if (@lock == false)
                                {
                                    try
                                    {
                                        Channel.Close();
                                    }
                                    finally
                                    {
                                        Channel = null;
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (Channel == null)
                            {
                                try
                                {
                                    f.Close();
                                }
                                finally
                                {
                                    f = null;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (markedHeld && !LockExists())
                    {
                        lock (LOCK_HELD)
                        {
                            if (LOCK_HELD.Contains(canonicalPath))
                            {
                                LOCK_HELD.Remove(canonicalPath);
                            }
                        }
                    }
                }
                return LockExists();
            }

            /*

              Channel = FileChannel.open(Path.toPath(), StandardOpenOption.CREATE, StandardOpenOption.WRITE);
              bool success = false;
              try
              {
                @lock = Channel.TryLock();
                success = @lock != null;
              }
              catch (System.IO.IOException | OverlappingFileLockException e)
              {
                // At least on OS X, we will sometimes get an
                // intermittent "Permission Denied" System.IO.IOException,
                // which seems to simply mean "you failed to get
                // the lock".  But other System.IO.IOExceptions could be
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
                  try
                  {
                    IOUtils.CloseWhileHandlingException(Channel);
                  }
                  finally
                  {
                    Channel = null;
                  }
                }
              }
              return @lock != null;
            }*/
        }

        public override void Release()
        {
            lock (this)
            {
                if (LockExists())
                {
                    try
                    {
                        Channel.Unlock(0, Channel.Length);
                    }
                    finally
                    {
                        @lock = false;
                        try
                        {
                            Channel.Close();
                        }
                        finally
                        {
                            Channel = null;
                            try
                            {
                                f.Close();
                            }
                            finally
                            {
                                f = null;
                                lock (LOCK_HELD)
                                {
                                    LOCK_HELD.Remove(Path.FullName);
                                }
                            }
                        }
                    }
                    bool tmpBool;
                    if (File.Exists(Path.FullName))
                    {
                        File.Delete(Path.FullName);
                        tmpBool = true;
                    }
                    else if (System.IO.Directory.Exists(Path.FullName))
                    {
                        System.IO.Directory.Delete(Path.FullName);
                        tmpBool = true;
                    }
                    else
                    {
                        tmpBool = false;
                    }
                    if (!tmpBool)
                    {
                        throw new LockReleaseFailedException("failed to delete " + Path);
                    }
                }
            }
        }

        public override bool Locked
        {
            get
            {
                lock (this)
                {
                    // The test for is isLocked is not directly possible with native file locks:

                    // First a shortcut, if a lock reference in this instance is available
                    if (LockExists())
                    {
                        return true;
                    }

                    // Look if lock file is present; if not, there can definitely be no lock!
                    bool tmpBool;
                    if (System.IO.File.Exists(Path.FullName))
                        tmpBool = true;
                    else
                        tmpBool = System.IO.Directory.Exists(Path.FullName);
                    if (!tmpBool)
                        return false;

                    // Try to obtain and release (if was locked) the lock
                    try
                    {
                        bool obtained = Obtain();
                        if (obtained)
                        {
                            Release();
                        }
                        return !obtained;
                    }
                    catch (System.IO.IOException ioe)
                    {
                        return false;
                    }
                }
            }
        }

        public override string ToString()
        {
            return "NativeFSLock@" + Path;
        }
    }
}