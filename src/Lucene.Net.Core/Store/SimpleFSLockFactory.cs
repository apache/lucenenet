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
            LockDir = lockDir;
        }

        /// <summary>
        /// Instantiate using the provided directory name (<see cref="string"/>). </summary>
        /// <param name="lockDirName"> where lock files should be created. </param>
        public SimpleFSLockFactory(string lockDirName)
        {
            LockDir = new DirectoryInfo(lockDirName);
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
#pragma warning disable 168
                catch (Exception e)
#pragma warning restore 168
                {
                    if (lockFile.Exists) // Delete failed and lockFile exists
                        throw new IOException("Cannot delete " + lockFile);
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
            if (!lockDir.Exists)
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
            else
            {
                // LUCENENET TODO: This doesn't look right. I think it should be File.Exists and if true throw the exception...
                try
                {
                    System.IO.Directory.Exists(lockDir.FullName);
                }
                catch
                {
                    throw new System.IO.IOException("Found regular file where directory expected: " + lockDir.FullName);
                }
            }

            if (lockFile.Exists) // LUCENENET TODO: This should probably be File.Exists(lockFile.FullName)...
            {
                return false;
            }
            else
            {
                // Create the file, and close it immediately
                FileStream createdFile = lockFile.Create();
                createdFile.Dispose();
                return true;
            }
        }

        public override void Release()
        {//LUCENE TO-DO : change the logic to be more like Lucene...don't attempt delete unless the file exists
            try
            {
                lockFile.Delete();
            }
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {
                if (lockFile.Exists) // Delete failed and lockFile exists
                    throw new LockReleaseFailedException("failed to delete " + lockFile);
            }
        }

        public override bool IsLocked
        {
            get
            {
                return lockFile.Exists;
            }
        }

        public override string ToString()
        {
            return "SimpleFSLock@" + lockFile;
        }
    }
}