using System;
using System.IO;
using System.Reflection;

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
    /// Simple <seealso cref="ResourceLoader"/> that uses <seealso cref="ClassLoader#getResourceAsStream(String)"/>
    /// and <seealso cref="Class#forName(String,boolean,ClassLoader)"/> to open resources and
    /// classes, respectively.
    /// </summary>
    public sealed class ClasspathResourceLoader : IResourceLoader
    {
        // NOTE: This class was refactored significantly from its Java counterpart.
        // It is basically just a wrapper around the System.Assembly type.

        private readonly Assembly assembly;

        /// <summary>
        /// Creates an instance using the current Executing Assembly to load Resources and classes.
        /// Resource paths must be absolute.
        /// </summary>
        public ClasspathResourceLoader()
            : this(Assembly.GetExecutingAssembly())
        {
        }

        /// <summary>
        /// Creates an instance using the System.Assembly of the given class to load Resources and classes
        /// Resource paths must be absolute.
        /// </summary>
        public ClasspathResourceLoader(Type clazz)
            : this(clazz.Assembly)
        {
        }

        /// <summary>
        /// Creates an instance using the given System.Assembly to load Resources and classes
        /// Resource paths must be absolute.
        /// </summary>
        public ClasspathResourceLoader(Assembly assembly)
        {
            this.assembly = assembly;
        }

        public Stream OpenResource(string resource)
        {
            Stream stream = this.assembly.GetManifestResourceStream(resource);
            if (stream == null)
            {
                throw new IOException("Resource not found: " + resource);
            }
            return stream;
        }

        public Type FindClass(string cname)
        {
            try
            {
                return this.assembly.GetType(cname, true);
            }
            catch (Exception e)
            {
                throw new Exception("Cannot load class: " + cname, e);
            }
        }

        public T NewInstance<T>(string cname)
        {
            Type clazz = FindClass(cname);
            try
            {
                return (T)Activator.CreateInstance(clazz);
            }
            catch (Exception e)
            {
                throw new Exception("Cannot create instance: " + cname, e);
            }
        }
    }
}