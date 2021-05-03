using System;
using System.IO;
using System.Globalization;
using System.Resources;
#nullable enable

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
    /// Contract for a set of localized resources. Generally, this is an abstraction over one or
    /// more <see cref="ResourceManager"/> instances.
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// Returns the value of the string resource localized for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the resource is localized.</param>
        /// <returns>The value of the resource localized for the specified <paramref name="culture"/>, or <c>null</c>
        /// if <paramref name="name"/> cannot be found in a resource set.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> parameter is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The value of the specified resource is not a string.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of resources has been found, and there are
        /// no resources for a default culture. For information about how to handle this exception, see the
        /// "Handling MissingManifestResourceException and MissingSatelliteAssemblyException Exceptions" section
        /// in the <see cref="ResourceManager"/> class topic.</exception>
        /// <exception cref="MissingSatelliteAssemblyException">The default culture's resources reside in a satellite
        /// assembly that could not be found. For information about how to handle this exception, see the
        /// "Handling MissingManifestResourceException and MissingSatelliteAssemblyException Exceptions" section
        /// in the <see cref="ResourceManager"/> class topic.</exception>
        string? GetString(string name, CultureInfo? culture);

        /// <summary>
        /// Gets the value of the specified non-string resource localized for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <param name="culture">The culture for which the resource is localized. If the resource is not
        /// localized for this culture, the resource manager uses fallback rules to locate an appropriate resource.
        /// <para/>
        /// If this value is <c>null</c>, the <see cref="CultureInfo"/> object is obtained by using the
        /// <see cref="CultureInfo.CurrentUICulture"/> property.
        /// </param>
        /// <returns>The value of the resource, localized for the specified culture. If an appropriate resource
        /// set exists but <paramref name="name"/> cannot be found, the method returns <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> parameter is <c>null</c>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of resources has been found, and there are
        /// no resources for a default culture. For information about how to handle this exception, see the
        /// "Handling MissingManifestResourceException and MissingSatelliteAssemblyException Exceptions" section
        /// in the <see cref="ResourceManager"/> class topic.</exception>
        /// <exception cref="MissingSatelliteAssemblyException">The default culture's resources reside in a satellite
        /// assembly that could not be found. For information about how to handle this exception, see the
        /// "Handling MissingManifestResourceException and MissingSatelliteAssemblyException Exceptions" section
        /// in the <see cref="ResourceManager"/> class topic.</exception>
        object? GetObject(string name, CultureInfo? culture);


        /// <summary>
        /// Returns an unmanaged memory stream object from the specified resource, using the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of a resource.</param>
        /// <param name="culture">An object that specifies the culture to use for the resource lookup. If <paramref name="culture"/>
        /// is <c>null</c>, the culture for the current thread is used.</param>
        /// <returns>An unmanaged memory stream object that represents a resource.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> parameter is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The value of the specified resource is not a <see cref="MemoryStream"/> object.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of resources has been found, and there are
        /// no resources for a default culture. For information about how to handle this exception, see the
        /// "Handling MissingManifestResourceException and MissingSatelliteAssemblyException Exceptions" section
        /// in the <see cref="ResourceManager"/> class topic.</exception>
        /// <exception cref="MissingSatelliteAssemblyException">The default culture's resources reside in a satellite
        /// assembly that could not be found. For information about how to handle this exception, see the
        /// "Handling MissingManifestResourceException and MissingSatelliteAssemblyException Exceptions" section
        /// in the <see cref="ResourceManager"/> class topic.</exception>
        Stream? GetStream(string name, CultureInfo? culture);
    }
}
