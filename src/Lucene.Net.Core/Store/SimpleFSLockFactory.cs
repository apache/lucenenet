using System;
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
    /// <p>Implements <seealso cref="LockFactory"/> using {@link
    /// File#createNewFile()}.</p>
    ///
    /// <p><b>NOTE:</b> the {@link File#createNewFile() javadocs
    /// for <code>File.createNewFile()</code>} contain a vague
    /// yet spooky warning about not using the API for file
    /// locking.  this warning was added due to <a target="_top"
    /// href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4676183">this
    /// bug</a>, and in fact the only known problem with using
    /// this API for locking is that the Lucene write lock may
    /// not be released when the JVM exits abnormally.</p>
    ///
    /// <p>When this happens, a <seealso cref="LockObtainFailedException"/>
    /// is hit when trying to create a writer, in which case you
    /// need to explicitly clear the lock file first.  You can
    /// either manually remove the file, or use the {@link
    /// Lucene.Net.Index.IndexWriter#unlock(Directory)}
    /// API.  But, first be certain that no writer is in fact
    /// writing to the index otherwise you can easily corrupt
    /// your index.</p>
    ///
    /// <p>Special care needs to be taken if you change the locking
    /// implementation: First be certain that no writer is in fact
    /// writing to the index otherwise you can easily corrupt
    /// your index. Be sure to do the LockFactory change all Lucene
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

    public class SimpleFSLockFactory : FSLockFactory
    {
        /// <summary>
        /// Create a SimpleFSLockFactory instance, with null (unset)
        /// lock directory. When you pass this factory to a <seealso cref="FSDirectory"/>
        /// subclass, the lock directory is automatically set to the
        /// directory itself. Be sure to create one instance for each directory
        /// your create!
        /// </summary>
        public SimpleFSLockFactory()
            : this((DirectoryInfo)null)
        {
        }

        /// <summary>
        /// Instantiate using the provided directory (as a File instance). </summary>
        /// <param name="lockDir"> where lock files should be created. </param>
        public SimpleFSLockFactory(DirectoryInfo lockDir)
        {
            LockDir = lockDir;
        }

        /// <summary>
        /// Instantiate using the provided directory name (String). </summary>
        /// <param name="lockDirName"> where lock files should be created. </param>
        public SimpleFSLockFactory(string lockDirName)
        {
            LockDir = new DirectoryInfo(lockDirName);
        }

        public override Lock MakeLock(string lockName)
        {
            if (LockPrefix_Renamed != null)
            {
                lockName = LockPrefix_Renamed + "-" + lockName;
            }
            return new SimpleFSLock(LockDir_Renamed, lockName);
        }

        public override void ClearLock(string lockName)
        {
            if (LockDir_Renamed.Exists)
            {
                if (LockPrefix_Renamed != null)
                {
                    lockName = LockPrefix_Renamed + "-" + lockName;
                }
                FileInfo lockFile = new FileInfo(Path.Combine(LockDir_Renamed.FullName, lockName));
                try
                {
                    lockFile.Delete();
                }
                catch (Exception e)
                {
                    if (lockFile.Exists) // Delete failed and lockFile exists
                        throw new System.IO.IOException("Cannot delete " + lockFile);
                }
            }
        }
    }

    internal class SimpleFSLock : Lock
    {
        internal FileInfo LockFile;
        internal DirectoryInfo LockDir;

        public SimpleFSLock(DirectoryInfo lockDir, string lockFileName)
        {
            this.LockDir = lockDir;
            LockFile = new FileInfo(Path.Combine(lockDir.FullName, lockFileName));
        }

        public override bool Obtain()
        {
            // Ensure that lockDir exists and is a directory:
            if (!LockDir.Exists)
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
            else
            {
                try
                {
                    System.IO.Directory.Exists(LockDir.FullName);
                }
                catch
                {
                    throw new System.IO.IOException("Found regular file where directory expected: " + LockDir.FullName);
                }
            }

            if (LockFile.Exists)
            {
                return false;
            }
            else
            {
                System.IO.FileStream createdFile = LockFile.Create();
                createdFile.Dispose();
                return true;
            }
        }

        public override void Release()
        {//LUCENE TO-DO
            try
            {
                LockFile.Delete();
            }
            catch (Exception e)
            {
                if (LockFile.Exists) // Delete failed and lockFile exists
                    throw new LockReleaseFailedException("failed to delete " + LockFile);
            }
        }

        public override bool Locked
        {
            get
            {
                return LockFile.Exists;
            }
        }

        public override string ToString()
        {
            return "SimpleFSLock@" + LockFile;
        }
    }
}