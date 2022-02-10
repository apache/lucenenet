// Lucene version compatibility level 4.8.1
using J2N;
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
    /// Simple <see cref="IResourceLoader"/> that uses <see cref="Assembly.GetManifestResourceStream(string)"/>
    /// and <see cref="Assembly.GetType(string)"/> to open resources and
    /// <see cref="Type"/>s, respectively.
    /// </summary>
    public sealed class ClasspathResourceLoader : IResourceLoader
    {
        // LUCENENET NOTE: This class was refactored significantly from its Java counterpart.

        private readonly Type clazz;

        /// <summary>
        /// Creates an instance using the System.Assembly of the given class to load Resources and classes
        /// Resource paths must be absolute.
        /// </summary>
        public ClasspathResourceLoader(Type clazz)
        {
            this.clazz = clazz;
        }

        public Stream OpenResource(string resource)
        {
            // LUCENENET NOTE: For some unknown reason, the shorthand version of this line
            // Stream stream = this.clazz.FindAndGetManifestResourceStream(resource);
            // causes TestMappingCharFilter.TestRandomMaps2 to run 2-3 times slower.
            // So, we are using the long-hand syntax in this one place.
            Stream stream = this.clazz.Assembly.FindAndGetManifestResourceStream(clazz, resource);
            if (stream is null)
            {
                throw new IOException("Resource not found: " + resource);
            }
            return stream;
        }

        public Type FindType(string cname)
        {
            try
            {
                // LUCENENET TODO: Apparently the second parameter of FindClass was used 
                // to determine what assembly a class is in (which makes this function pretty much
                // pointless). Need to evaluate whether it makes sense to pass a "relative" type here
                // to identify the correct assembly, since we can just pass a string to do the same.
                if (cname.Contains(","))
                {
                    // Assume we have an assembly qualified name
                    return Type.GetType(cname);
                }

                return this.clazz.Assembly.GetType(cname, true);
            }
            catch (Exception e) when (e.IsException())
            {
                throw RuntimeException.Create("Cannot load class: " + cname, e);
            }
        }

        public T NewInstance<T>(string cname)
        {
            Type clazz = FindType(cname);
            try
            {
                return (T)Activator.CreateInstance(clazz);
            }
            catch (Exception e) when (e.IsException())
            {
                throw RuntimeException.Create("Cannot create instance: " + cname, e);
            }
        }
    }
}