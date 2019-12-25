using Lucene.Net.Support;
using System;
using System.Reflection;
using System.Resources;

namespace Lucene.Net.Util
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
    /// This implementation of <see cref="IResourceManagerFactory"/> uses a convention
    /// to retrieve resources. In Java NLS, the convention is to use the same name for the
    /// resource key propeties and for the resource file names. This presents a problem
    /// for .NET because the resource generator already creates an internal class with the
    /// same name as the <c>.resx</c> file.
    /// <para/>
    /// To work around this, we use the convention of appending the suffix "Bundle" to 
    /// the end of the type the resource key propeties are stored in. For example,
    /// if our constants are stored in a class named ErrorMessages, the type
    /// that will be looked up by this factory will be ErrorMessagesBundle (which is the
    /// name of the <c>.resx</c> file that should be added to your project).
    /// <para/>
    /// This implementation can be inherited to use a different convention or can be replaced
    /// to get the resources from an external source.
    /// </summary>
    public class BundleResourceManagerFactory : IResourceManagerFactory
    {
        /// <summary>
        /// Creates a <see cref="ResourceManager"/> instance using the specified <paramref name="resourceSource"/>.
        /// </summary>
        /// <param name="resourceSource">The type representing the resource to retrieve.</param>
        /// <returns>A new <see cref="ResourceManager"/> instance.</returns>
        public virtual ResourceManager Create(Type resourceSource)
        {
            return new ResourceManager(GetResourceName(resourceSource), resourceSource.GetTypeInfo().Assembly);
        }

        /// <summary>
        /// Releases the <see cref="ResourceManager"/> instance including any disposable dependencies.
        /// </summary>
        /// <param name="manager">The <see cref="ResourceManager"/> to release.</param>
        public virtual void Release(ResourceManager manager)
        {
            var disposable = manager as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Gets the fully-qualified name of the bundle as it would appear
        /// using <see cref="Assembly.GetManifestResourceNames()"/>, without the
        /// <c>.resources</c> extension. This is the name that is passed to the
        /// <c>baseName</c> parameter of
        /// <see cref="ResourceManager.ResourceManager(string, Assembly)"/>.
        /// </summary>
        /// <param name="clazz">The type of the NLS-derived class where the field strings are located that identify resources.</param>
        /// <returns>The resource name.</returns>
        protected virtual string GetResourceName(Type clazz)
        {
            string resource = clazz.GetTypeInfo().Assembly.FindResource(clazz, string.Concat(clazz.Name, ResourceSuffix, ".resources"));
            return !string.IsNullOrEmpty(resource)
                ? resource.Substring(0, resource.Length - 10)
                : null;
        }

        /// <summary>
        /// The suffix to append to the resource key class name to locate the embedded resource.
        /// </summary>
        protected virtual string ResourceSuffix
        {
            get { return "Bundle"; }
        }
    }
}
