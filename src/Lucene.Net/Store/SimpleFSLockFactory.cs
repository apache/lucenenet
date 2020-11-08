using System;
using System.IO;
using System.Text;

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
    /// <para>Implements <see cref="LockFactory"/> using 
    /// <see cref="File.WriteAllText(string, string, Encoding)"/> 
    /// (writes the file with UTF8 encoding and no byte order mark).</para>
    ///
    /// <para>Special care needs to be taken if you change the locking
    /// implementation: First be certain that no writer is in fact
    /// writing to the index otherwise you can easily corrupt
    /// your index. Be sure to do the <see cref="LockFactory"/> change to all Lucene
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
    public class SimpleFSLockFactory : FSLockFactory
    {
        /// <summary>
        /// Create a <see cref="SimpleFSLockFactory"/> instance, with <c>null</c> (unset)
        /// lock directory. When you pass this factory to a <see cref="FSDirectory"/>
        /// subclass, the lock directory is automatically set to the
        /// directory itself. Be sure to create one instance for each directory
        /// your create!
        /// </summary>
        public SimpleFSLockFactory()
            : this((DirectoryInfo)null)
        {
        }

        /// <summary>
        /// Instantiate using the provided directory (as a <see cref="DirectoryInfo"/> instance). </summary>
        /// <param name="lockDir"> where lock files should be created. </param>
        public SimpleFSLockFactory(DirectoryInfo lockDir)
        {
            SetLockDir(lockDir);
        }

        /// <summary>
        /// Instantiate using the provided directory name (<see cref="string"/>). </summary>
        /// <param name="lockDirName"> where lock files should be created. </param>
        public SimpleFSLockFactory(string lockDirName)
        {
            SetLockDir(new DirectoryInfo(lockDirName));
        }

        public override Lock MakeLock(string lockName)
        {
            if (m_lockPrefix != null)
            {
                lockName = m_lockPrefix + "-" + lockName;
            }
            return new SimpleFSLock(m_lockDir, lockName);
        }

        public override void ClearLock(string lockName)
        {
            if (m_lockDir.Exists)
            {
                if (m_lockPrefix != null)
                {
                    lockName = m_lockPrefix + "-" + lockName;
                }
                FileInfo lockFile = new FileInfo(Path.Combine(m_lockDir.FullName, lockName));
                try
                {
                    lockFile.Delete();
                }
                catch (Exception e)
                {
                    if (lockFile.Exists) // Delete failed and lockFile exists
                        throw new IOException("Cannot delete " + lockFile, e); // LUCENENET specific: wrapped inner exception
                }
            }
        }
    }

    internal class SimpleFSLock : Lock
    {
        internal FileInfo lockFile;
        internal DirectoryInfo lockDir;

        public SimpleFSLock(DirectoryInfo lockDir, string lockFileName)
        {
            this.lockDir = lockDir;
            lockFile = new FileInfo(Path.Combine(lockDir.FullName, lockFileName));
        }

        public override bool Obtain()
        {
            // Ensure that lockDir exists and is a directory:
            if (!System.IO.Directory.Exists(lockDir.FullName))
            {
                try
                {
                    lockDir.Create();
                }
                catch (Exception e)
                {
                    throw new IOException("Cannot create directory: " + lockDir.FullName, e);
                }
            }
            else if (File.Exists(lockDir.FullName)) // LUCENENET: File.Exists will be true if it is a file and not a directory
            {
                throw new IOException("Found regular file where directory expected: " + lockDir.FullName);
            }

            // LUCENENET: Since WriteAllText doesn't care if the file exists or not,
            // we need to make that check first. We create a new IOException "failure reason"
            // in this case to simulate what happens in Java
            if (File.Exists(lockFile.FullName))
            {
                FailureReason = new IOException(string.Format("lockFile '{0}' alredy exists.", lockFile.FullName));
                return false;
            }

            try
            {
                // Create the file, and close it immediately
                File.WriteAllText(lockFile.FullName, string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) /* No BOM */);
                return true;
            }
            catch (Exception e) // LUCENENET: Some of the exceptions that can happen are not IOException, so we catch everything
            {
                // On Windows, on concurrent createNewFile, the 2nd process gets "access denied".
                // In that case, the lock was not aquired successfully, so return false.
                // We record the failure reason here; the obtain with timeout (usually the
                // one calling us) will use this as "root cause" if it fails to get the lock.
                FailureReason = e;
                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (File.Exists(lockFile.FullName))
                {
                    File.Delete(lockFile.FullName);

                    // If lockFile still exists, delete failed
                    if (File.Exists(lockFile.FullName))
                    {
                        throw new LockReleaseFailedException("failed to delete " + lockFile);
                    }
                }
            }
        }

        public override bool IsLocked()
        {
            return File.Exists(lockFile.FullName);
        }

        public override string ToString()
        {
            return "SimpleFSLock@" + lockFile;
        }
    }
}