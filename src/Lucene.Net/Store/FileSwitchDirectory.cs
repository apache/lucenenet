using J2N.Collections.Generic.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using JCG = J2N.Collections.Generic;

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
    /// Expert: A <see cref="Directory"/> instance that switches files between
    /// two other <see cref="Directory"/> instances.
    ///
    /// <para/>Files with the specified extensions are placed in the
    /// primary directory; others are placed in the secondary
    /// directory.  The provided <see cref="T:ISet{string}"/> must not change once passed
    /// to this class, and must allow multiple threads to call
    /// contains at once.
    /// <para/>
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
        public virtual Directory PrimaryDir => primaryDir;

        /// <summary>
        /// Return the secondary directory </summary>
        public virtual Directory SecondaryDir => secondaryDir;

        protected override void Dispose(bool disposing)
        {
            if (disposing && doClose)
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
            ISet<string> files = new JCG.HashSet<string>();
            // LUCENE-3380: either or both of our dirs could be FSDirs,
            // but if one underlying delegate is an FSDir and mkdirs() has not
            // yet been called, because so far everything is written to the other,
            // in this case, we don't want to throw a NoSuchDirectoryException
            Exception exc = null; // LUCENENET: No need to cast to DirectoryNotFoundException
            try
            {
                foreach (string f in primaryDir.ListAll())
                {
                    files.Add(f);
                }
            }
            catch (Exception e) when (e.IsNoSuchDirectoryException())
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
            catch (Exception e) when (e.IsNoSuchDirectoryException())
            {
                // we got NoSuchDirectoryException from both dirs
                // rethrow the first.
                if (exc != null)
                {
                    ExceptionDispatchInfo.Capture(exc).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                }
                // we got NoSuchDirectoryException from the secondary,
                // and the primary is empty.
                if (files.Count == 0)
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
            }
            // we got NoSuchDirectoryException from the primary,
            // and the secondary is empty.

            if (exc != null && files.Count == 0)
            {
                ExceptionDispatchInfo.Capture(exc).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
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

        [Obsolete("this method will be removed in 5.0")]
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
            IList<string> primaryNames = new JCG.List<string>();
            IList<string> secondaryNames = new JCG.List<string>();

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