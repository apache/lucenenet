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
using System.Linq;

namespace Lucene.Net.Store
{

    /// <summary> Expert: A Directory instance that switches files between
    /// two other Directory instances.
    /// <p/>Files with the specified extensions are placed in the
    /// primary directory; others are placed in the secondary
    /// directory.  The provided Set must not change once passed
    /// to this class, and must allow multiple threads to call
    /// contains at once.<p/>
    /// 
    /// <p/><b>NOTE</b>: this API is new and experimental and is
    /// subject to suddenly change in the next release.
    /// </summary>

    public class FileSwitchDirectory : Directory
    {
        private Directory secondaryDir;
        private Directory primaryDir;
        private ISet<string> primaryExtensions;
        private bool doClose;
        private bool isDisposed;

        public FileSwitchDirectory(ISet<string> primaryExtensions,
                                    Directory primaryDir,
                                    Directory secondaryDir,
                                    bool doClose)
        {
            this.primaryExtensions = primaryExtensions;
            this.primaryDir = primaryDir;
            this.secondaryDir = secondaryDir;
            this.doClose = doClose;
            this.interalLockFactory = primaryDir.LockFactory;
        }

        /// <summary>Return the primary directory </summary>
        public virtual Directory PrimaryDir
        {
            get { return primaryDir; }
        }

        /// <summary>Return the secondary directory </summary>
        public virtual Directory SecondaryDir
        {
            get { return secondaryDir; }
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (doClose)
            {
                try
                {
                    if (secondaryDir != null)
                    {
                        secondaryDir.Dispose();
                    }
                }
                finally
                {
                    if (primaryDir != null)
                    {
                        primaryDir.Dispose();
                    }
                }
                doClose = false;
            }

            secondaryDir = null;
            primaryDir = null;
            isDisposed = true;
        }

        public override String[] ListAll()
        {
            ISet<String> files = new HashSet<String>();
            // LUCENE-3380: either or both of our dirs could be FSDirs,
            // but if one underlying delegate is an FSDir and mkdirs() has not
            // yet been called, because so far everything is written to the other,
            // in this case, we don't want to throw a NoSuchDirectoryException
            NoSuchDirectoryException exc = null;
            try
            {
                foreach (String f in primaryDir.ListAll())
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
                foreach (String f in secondaryDir.ListAll())
                {
                    files.Add(f);
                }
            }
            catch (NoSuchDirectoryException e)
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
                    throw e;
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

        /// <summary>Utility method to return a file's extension. </summary>
        public static String GetExtension(String name)
        {
            int i = name.LastIndexOf('.');
            if (i == -1)
            {
                return "";
            }
            return name.Substring(i + 1, (name.Length) - (i + 1));
        }

        private Directory GetDirectory(String name)
        {
            String ext = GetExtension(name);
            if (primaryExtensions.Contains(ext))
            {
                return primaryDir;
            }
            else
            {
                return secondaryDir;
            }
        }

        public override bool FileExists(String name)
        {
            return GetDirectory(name).FileExists(name);
        }

        public override void DeleteFile(String name)
        {
            GetDirectory(name).DeleteFile(name);
        }

        public override long FileLength(String name)
        {
            return GetDirectory(name).FileLength(name);
        }

        public override IndexOutput CreateOutput(String name, IOContext context)
        {
            return GetDirectory(name).CreateOutput(name, context);
        }

        public override void Sync(ICollection<string> names)
        {
            IList<String> primaryNames = new List<String>();
            IList<String> secondaryNames = new List<String>();

            foreach (String name in names)
                if (primaryExtensions.Contains(GetExtension(name)))
                    primaryNames.Add(name);
                else
                    secondaryNames.Add(name);

            primaryDir.Sync(primaryNames);
            secondaryDir.Sync(secondaryNames);
        }

        public override IndexInput OpenInput(String name, IOContext context)
        {
            return GetDirectory(name).OpenInput(name, context);
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            return GetDirectory(name).CreateSlicer(name, context);
        }
    }
}