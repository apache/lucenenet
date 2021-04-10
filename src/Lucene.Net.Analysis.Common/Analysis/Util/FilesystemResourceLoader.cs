// Lucene version compatibility level 4.8.1
using System;
using System.IO;

namespace Lucene.Net.Analysis.Util
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
    /// Simple <see cref="IResourceLoader"/> that opens resource files
    /// from the local file system, optionally resolving against
    /// a base directory.
    /// 
    /// <para>This loader wraps a delegate <see cref="IResourceLoader"/>
    /// that is used to resolve all files, the current base directory
    /// does not contain. <see cref="NewInstance"/> is always resolved
    /// against the delegate, as an <see cref="T:System.Assembly"/> is needed.
    /// 
    /// </para>
    /// <para>You can chain several <see cref="FilesystemResourceLoader"/>s
    /// to allow lookup of files in more than one base directory.
    /// </para>
    /// </summary>
    public sealed class FilesystemResourceLoader : IResourceLoader
    {
        private readonly DirectoryInfo baseDirectory;
        private readonly IResourceLoader @delegate;

        /// <summary>
        /// Creates a resource loader that requires absolute filenames or relative to CWD
        /// to resolve resources. Files not found in file system and class lookups
        /// are delegated to context classloader.
        /// </summary>
        public FilesystemResourceLoader()
            : this((DirectoryInfo)null)
        {
        }

        /// <summary>
        /// Creates a resource loader that resolves resources against the given
        /// base directory (may be <c>null</c> to refer to CWD).
        /// Files not found in file system and class lookups are delegated to context
        /// classloader.
        /// </summary>
        public FilesystemResourceLoader(DirectoryInfo baseDirectory)
            : this(baseDirectory, new ClasspathResourceLoader(typeof(FilesystemResourceLoader)))
        {
        }

        /// <summary>
        /// Creates a resource loader that resolves resources against the given
        /// base directory (may be <c>null</c> to refer to CWD).
        /// Files not found in file system and class lookups are delegated
        /// to the given delegate <see cref="IResourceLoader"/>.
        /// </summary>
        public FilesystemResourceLoader(DirectoryInfo baseDirectory, IResourceLoader @delegate)
        {
            // LUCENENET NOTE: If you call DirectoryInfo.Create() it doesn't set the DirectoryInfo.Exists
            // flag to true, so we use the Directory object to check the path explicitly.
            if (!(baseDirectory is null) && !Directory.Exists(baseDirectory.FullName))
            {
                throw new ArgumentException("baseDirectory is not a directory or is null");
            }
            if (@delegate is null)
            {
                throw new ArgumentNullException(nameof(@delegate), "delegate IResourceLoader may not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }

            this.baseDirectory = baseDirectory;
            this.@delegate = @delegate;
        }

        public Stream OpenResource(string resource)
        {
            try
            {
                FileInfo file = null;

                // First try absolute.
                if (File.Exists(resource))
                {
                    file = new FileInfo(resource);
                }
                else
                {
                    // Try as a relative path
                    var fullPath = System.IO.Path.GetFullPath(resource);
                    if (File.Exists(fullPath))
                    {
                        file = new FileInfo(fullPath);
                    }
                    else if (baseDirectory != null)
                    {
                        // Try to combine with the base directory
                        string based = System.IO.Path.Combine(baseDirectory.FullName, resource);
                        if (File.Exists(based))
                        {
                            file = new FileInfo(based);
                        }
                    }
                }

                if (file != null)
                {
                    return file.OpenRead();
                }

                // Fallback on the inner resource loader (this could fail)
                return @delegate.OpenResource(resource);
            }
            catch (Exception e)
            {
                throw new IOException("The requested file could not be found", e);
            }
        }

        public T NewInstance<T>(string cname)
        {
            return @delegate.NewInstance<T>(cname);
        }

        public Type FindType(string cname)
        {
            return @delegate.FindType(cname);
        }
    }
}