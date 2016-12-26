using System.Collections.Generic;
using System.Linq;

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
    /// Expert: A Directory instance that switches files between
    /// two other Directory instances.
    ///
    /// <p>Files with the specified extensions are placed in the
    /// primary directory; others are placed in the secondary
    /// directory.  The provided Set must not change once passed
    /// to this class, and must allow multiple threads to call
    /// contains at once.</p>
    ///
    /// @lucene.experimental
    /// </summary>
    public class FileSwitchDirectory : BaseDirectory
    {
        private readonly Directory secondaryDir;
        private readonly Directory primaryDir;
        private readonly ISet<string> primaryExtensions;
        private bool doClose;

        public FileSwitchDirectory(ISet<string> primaryExtensions, Directory primaryDir, Directory secondaryDir, bool doClose)
        {
            this.primaryExtensions = primaryExtensions;
            this.primaryDir = primaryDir;
            this.secondaryDir = secondaryDir;
            this.doClose = doClose;
            this.m_lockFactory = primaryDir.LockFactory;
        }

        /// <summary>
        /// Return the primary directory </summary>
        public virtual Directory PrimaryDir
        {
            get
            {
                return primaryDir;
            }
        }

        /// <summary>
        /// Return the secondary directory </summary>
        public virtual Directory SecondaryDir
        {
            get
            {
                return secondaryDir;
            }
        }

        public override void Dispose()
        {
            if (doClose)
            {
                try
                {
                    secondaryDir.Dispose();
                }
                finally
                {
                    primaryDir.Dispose();
                }
                doClose = false;
            }
        }

        public override string[] ListAll()
        {
            ISet<string> files = new HashSet<string>();
            // LUCENE-3380: either or both of our dirs could be FSDirs,
            // but if one underlying delegate is an FSDir and mkdirs() has not
            // yet been called, because so far everything is written to the other,
            // in this case, we don't want to throw a NoSuchDirectoryException
            NoSuchDirectoryException exc = null;
            try
            {
                foreach (string f in primaryDir.ListAll())
                {
                    files.Add(f);
                }
            }
            catch (NoSuchDirectoryException e)
            {
                exc = e;
            }

            try
            {
                foreach (string f in secondaryDir.ListAll())
                {
                    files.Add(f);
                }
            }
            catch (NoSuchDirectoryException)
            {
                // we got NoSuchDirectoryException from both dirs
                // rethrow the first.
                if (exc != null)
                {
                    throw exc;
                }
                // we got NoSuchDirectoryException from the secondary,
                // and the primary is empty.
                if (files.Count == 0)
                {
                    throw;
                }
            }
            // we got NoSuchDirectoryException from the primary,
            // and the secondary is empty.

            if (exc != null && files.Count == 0)
            {
                throw exc;
            }
            return files.ToArray();
        }

        /// <summary>
        /// Utility method to return a file's extension. </summary>
        public static string GetExtension(string name)
        {
            int i = name.LastIndexOf('.');
            if (i == -1)
            {
                return "";
            }
            return name.Substring(i + 1, name.Length - (i + 1));
        }

        private Directory GetDirectory(string name)
        {
            string ext = GetExtension(name);
            if (primaryExtensions.Contains(ext))
            {
                return primaryDir;
            }
            else
            {
                return secondaryDir;
            }
        }

        public override bool FileExists(string name)
        {
            return GetDirectory(name).FileExists(name);
        }

        public override void DeleteFile(string name)
        {
            GetDirectory(name).DeleteFile(name);
        }

        public override long FileLength(string name)
        {
            return GetDirectory(name).FileLength(name);
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            return GetDirectory(name).CreateOutput(name, context);
        }

        public override void Sync(ICollection<string> names)
        {
            IList<string> primaryNames = new List<string>();
            IList<string> secondaryNames = new List<string>();

            foreach (string name in names)
            {
                if (primaryExtensions.Contains(GetExtension(name)))
                {
                    primaryNames.Add(name);
                }
                else
                {
                    secondaryNames.Add(name);
                }
            }

            primaryDir.Sync(primaryNames);
            secondaryDir.Sync(secondaryNames);
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            return GetDirectory(name).OpenInput(name, context);
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            return GetDirectory(name).CreateSlicer(name, context);
        }
    }
}