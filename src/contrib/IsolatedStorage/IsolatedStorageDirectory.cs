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
using System.IO;
using System.IO.IsolatedStorage;

namespace Lucene.Net.Store
{

    public class IsolatedStorageDirectory : Directory
    {
        private IsolatedStorageFile IsoDirectory = null;
        private string IsoDirectoryPath = null;

        /// <summary>
        /// Create a new instance of the isolated store for this user, domain, and assembly
        /// </summary>
        /// <param name="path">Path within the store for the Lucene indexs</param>
        public IsolatedStorageDirectory(string path) : this(path, null)
        {
        }

        /// <summary>
        /// Create a new instance of the isolated store for this user, domain, and assembly
        /// </summary>
        /// <param name="path">Path within the store for the Lucene indexs</param>
        /// <param name="lockFactory">Not Implimented</param>
        /// TODO: Impliment lockFactory
        public IsolatedStorageDirectory(string path, LockFactory lockFactory)
        {
            IsoDirectory = IsolatedStorageFile.GetStore(IsolatedStorageScope.User |
                IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly, null, null);

            if (!IsoDirectory.DirectoryExists(path))
            {
                throw new NoSuchDirectoryException(String.Format("{0} exists but is not a folder", path));
            }

            IsoDirectoryPath = path.TrimEnd('/');
        }

        /// <summary>
        /// Initializes the directory to create a new file with the given name.
        /// This method should be used in {@link #createOutput}. 
        /// </summary>
        protected internal void InitOutput(string name)
        {
            var isoFile = GetIsoStoragePath(name);

            if (IsoDirectory.FileExists(isoFile))
            {
                IsoDirectory.DeleteFile(isoFile);
            }
        }

        /// <summary>
        /// Lists all files (not subdirectories) in the directory.
        /// </summary>
        public String[] ListAll(string dir)
        {
            if (!IsoDirectory.DirectoryExists(dir))
            {
                throw new NoSuchDirectoryException(String.Format("Directory '{0}' does not exist", dir));
            }

            return IsoDirectory.GetFileNames();
        }

        /// <summary>
        /// Lists all files (not subdirectories) in the directory.
        /// </summary>
        public override String[] ListAll()
        {
            return ListAll(IsoDirectoryPath);
        }

        /// <summary>
        /// Returns true iff a file with the given name exists. 
        /// </summary>
        public override bool FileExists(string name)
        {
            return IsoDirectory.FileExists(GetIsoStoragePath(name));
        }

        /// <summary>
        /// Returns the time the named file was last modified. 
        /// </summary>
        public override long FileModified(string name)
        {
            var file = GetIsoStoragePath(name);

            if (IsoDirectory.FileExists(file))
            {
                // {{LUCENENET-353}}
                return (long) IsoDirectory.GetLastWriteTime(file)
                    .ToUniversalTime()
                    .Subtract(new DateTime(1970, 1, 1, 0, 0, 0))
                    .TotalMilliseconds;
            }

            throw new FileNotFoundException(name + "not found");
        }

        /// <summary>
        /// Set the modified time of an existing file to now.
        /// </summary>
        public override void TouchFile(string name)
        {
            var file = GetIsoStoragePath(name);

            var f = new IsolatedStorageFileStream(file, FileMode.Append);
            f.Close(); // TODO: Verify that this updates the modified time of the file
        }

        /// <summary>
        /// Returns the length in bytes of a file in the directory
        /// </summary>
        public override long FileLength(string name)
        {
            var file = GetIsoStoragePath(name);
            var isoFile = new IsolatedStorageFileStream(file, FileMode.Open);
            return isoFile.Length;
        }

        /// <summary>
        /// Removes an existing file in the directory. 
        /// </summary>
        public override void DeleteFile(string name)
        {
            var file = GetIsoStoragePath(name);
            IsoDirectory.DeleteFile(file);
        }

        /// <summary>
        /// Sync the file back to IsolatedStorage
        /// </summary>
        /// <param name="name"></param>
        public override void Sync(string name)
        {
            var fileName = GetIsoStoragePath(name);
            var success = false;
            var retryCount = 0;

            IOException exc = null;

            while (!success && retryCount < 5)
            {
                retryCount++;
                IsolatedStorageFileStream file = null;

                try
                {
                    try
                    {
                        file = new IsolatedStorageFileStream(fileName,
                            FileMode.OpenOrCreate,
                            FileAccess.Write,
                            FileShare.ReadWrite);

                        file.Flush();
                        success = true;
                    }
                    finally
                    {
                        if (file != null)
                            file.Close();
                    }
                }
                catch (IOException ioe)
                {
                    if (exc == null)
                        exc = ioe;
                    try
                    {
                        // Pause 5 msec
                        System.Threading.Thread.Sleep(new TimeSpan((Int64)10000 * 5));
                    }
                    catch (System.Threading.ThreadInterruptedException ie)
                    {
                        Support.ThreadClass.Current().Interrupt();
                        throw new System.Threading.ThreadInterruptedException(ie.ToString(), ie);
                    }
                }
            }

            if (!success)
                if (exc != null) throw exc;
        }

        /// <summary>
        /// Creates an IndexOutput for the file with the given name.
        /// <em>In 3.0 this method will become abstract.</em> 
        /// </summary>
        public override IndexOutput CreateOutput(string name)
        {
            InitOutput(name);
            return new IsolatedStorageOutputStream(GetIsoStoragePath(name), ref IsoDirectory);
        }

        /// <summary>
        /// Returns a stream reading an existing file. 
        /// </summary>
        public override IndexInput OpenInput(string name)
        {
            return new IsolatedStorageInputStream(GetIsoStoragePath(name), ref IsoDirectory);
        }

        /// <summary>
        /// Closes the store to future operations. 
        /// </summary>
        public override void Close()
        {
            IsoDirectory.Close();
        }

        /// <summary>
        /// For .Net Using
        /// </summary>
        public override void Dispose()
        {
            IsoDirectory.Close();
        }

        /// <summary>
        /// For debug output. 
        /// </summary>
        public override string ToString()
        {
            return GetType().FullName + "@" + IsoDirectoryPath + " lockFactory=" + GetLockFactory();
        }

        #region Obsolete

        /// <summary>
        /// Renames an existing file in the directory - overwriting the to file if exists
        /// </summary>
        /// <deprecated> 
        /// </deprecated>
        [Obsolete("Not Implimented")]
        public override void RenameFile(string from, string to)
        {
            throw new NotImplementedException("This method (IsolatedStorageDirectory.RenameFile) is Obsolete");
        }

        /// <summary>
        /// Lists all files (not subdirectories) in the directory.
        /// </summary>
        [Obsolete("Lucene.Net-2.9.1. This method overrides obsolete member Lucene.Net.Store.Directory.List()")]
        public override String[] List()
        {
            return IsoDirectory.GetFileNames();
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Prepends filenames with the correct path
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string GetIsoStoragePath(string file)
        {
            return IsoDirectoryPath + '/' + file;
        }

        #endregion
    }
}