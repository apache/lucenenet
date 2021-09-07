using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Threading;
#nullable enable

namespace Lucene.Net.QueryParsers.Flexible.Core.Messages
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
    /// The default <see cref="IResourceProvider"/> implementation for the <see cref="QueryParserMessages"/> class.
    /// This class can be set in the <see cref="QueryParserMessages.SetResourceProvider(IResourceProvider)"/> method
    /// and supplied with one or more <see cref="ResourceManager"/> instances that can override the default query parser
    /// messages (generally, they are exception messages).
    /// <para/>
    /// Alternatively, this class may be overridden to provide either a custom <see cref="FallbackResourceManager"/> or
    /// to alter the fallback logic in either <see cref="GetString(string, CultureInfo?)"/> or <see cref="GetObject(string, CultureInfo?)"/>.
    /// The performance of this class may be improved significantly by specifying the <see cref="ResourceManager"/> that a specific resource
    /// can be found in rather than attempting all of them.
    /// </summary>
    public class QueryParserResourceProvider : IResourceProvider
    {
        private ResourceManager? fallbackResourceManager;
        private readonly IList<ResourceManager> resourceManagers;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParserResourceProvider"/> class with default values.
        /// </summary>
        public QueryParserResourceProvider()
            : this((IList<ResourceManager>)Arrays.Empty<ResourceManager>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParserResourceProvider"/> class with the specified
        /// <paramref name="resourceManagers"/>. The <paramref name="resourceManagers"/> may override resources
        /// in the <see cref="FallbackResourceManager"/>, provided they have the same names.
        /// <para/>
        /// Note that not all of the resources are required to be provided and if the name doesn't exist it will
        /// fall back to the next <see cref="ResourceManager"/> that is provided and ultimately will try the
        /// <see cref="FallbackResourceManager"/> if the resource is not found.
        /// </summary>
        /// <param name="resourceManagers">One or more <see cref="ResourceManager"/> instances that provide
        /// localized resources. The <paramref name="resourceManagers"/> are used in the order they are specified, and the first one
        /// that provides a non-<c>null</c> value for a given resource name wins.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="resourceManagers"/> is <c>null</c>.</exception>
        public QueryParserResourceProvider(params ResourceManager[] resourceManagers)
            : this((IList<ResourceManager>)resourceManagers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParserResourceProvider"/> class with the specified
        /// <paramref name="resourceManagers"/>. The <paramref name="resourceManagers"/> may override resources
        /// in the <see cref="FallbackResourceManager"/>, provided they have the same names.
        /// <para/>
        /// Note that not all of the resources are required to be provided and if the name doesn't exist it will
        /// fall back to the next <see cref="ResourceManager"/> that is provided and ultimately will try the
        /// <see cref="FallbackResourceManager"/> if the resource is not found.
        /// </summary>
        /// <param name="resourceManagers">One or more <see cref="ResourceManager"/> instances that provide
        /// localized resources. The <paramref name="resourceManagers"/> are used in the order they are specified, and the first one
        /// that provides a non-<c>null</c> value for a given resource name wins.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="resourceManagers"/> is <c>null</c>.</exception>
        public QueryParserResourceProvider(IList<ResourceManager> resourceManagers)
        {
            this.resourceManagers = resourceManagers ?? throw new ArgumentNullException(nameof(resourceManagers));
        }

        /// <summary>
        /// Gets the cached <see cref="ResourceManager"/> instance used as the fallback by this class.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public virtual ResourceManager FallbackResourceManager
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref fallbackResourceManager,
                    () => new ResourceManager("Lucene.Net.QueryParsers.Flexible.Core.Messages.QueryParserMessages", typeof(QueryParserMessages).Assembly))!;
            }
        }

        /// <summary>Returns the value of the string resource localized for the specified <paramref name="culture"/>.
        /// <para/>
        /// The resource is searched for
        /// first in the <see cref="ResourceManager"/> instances passed in the <see cref="QueryParserResourceProvider(IList{ResourceManager})"/>
        /// or <see cref="QueryParserResourceProvider(ResourceManager[])"/> constructor, in the order they are specified. If not found, the
        /// <see cref="FallbackResourceManager"/> is used. This method may return <c>null</c> if the resource with the given name is not found.</summary>
        /// <inheritdoc/>
        public virtual string? GetString(string name, CultureInfo? culture)
        {
            if (resourceManagers.Count > 0)
            {
                foreach (var resourceManager in resourceManagers)
                {
                    // LUCENENET NOTE: MissingManifestResourceException or MissingSatelliteAssemblyException
                    // can be thrown here, but these indicate that there is a misconfigured setup (missing resource file,
                    // missing assembly, etc.) We intentionally let these errors propagate to ensure the developer who
                    // is creating resources is aware of these problems before the app is deployed (provided they test it).
                    // However, if the resource simply doesn't have an entry, we get a null return value instead and handle
                    // the fallback accordingly.
                    var result = resourceManager.GetString(name, culture);
                    if (result is null)
                        continue;
                    return result;
                }
            }

            return FallbackResourceManager.GetString(name, culture);
        }

        /// <summary>Gets the value of the specified non-string resource localized for the specified <paramref name="culture"/>.
        /// <para/>
        /// The resource is searched for
        /// first in the <see cref="ResourceManager"/> instances passed in the <see cref="QueryParserResourceProvider(IList{ResourceManager})"/>
        /// or <see cref="QueryParserResourceProvider(ResourceManager[])"/> constructor, in the order they are specified. If not found, the
        /// <see cref="FallbackResourceManager"/> is used. This method may return <c>null</c> if the resource with the given name is not found.</summary>
        /// <inheritdoc/>
        public virtual object? GetObject(string name, CultureInfo? culture)
        {
            if (resourceManagers.Count > 0)
            {
                foreach (var resourceManager in resourceManagers)
                {
                    // LUCENENET NOTE: MissingManifestResourceException or MissingSatelliteAssemblyException
                    // can be thrown here, but these indicate that there is a misconfigured setup (missing resource file,
                    // missing assembly, etc.) We intentionally let these errors propagate to ensure the developer who
                    // is creating resources is aware of these problems before the app is deployed (provided they test it).
                    // However, if the resource simply doesn't have an entry, we get a null return value instead and handle
                    // the fallback accordingly.
                    var result = resourceManager.GetObject(name, culture);
                    if (result is null)
                        continue;
                    return result;
                }
            }

            return FallbackResourceManager.GetObject(name, culture);
        }

        /// <summary>Returns an unmanaged memory stream object from the specified resource, using the specified <paramref name="culture"/>.
        /// <para/>
        /// The resource is searched for
        /// first in the <see cref="ResourceManager"/> instances passed in the <see cref="QueryParserResourceProvider(IList{ResourceManager})"/>
        /// or <see cref="QueryParserResourceProvider(ResourceManager[])"/> constructor, in the order they are specified. If not found, the
        /// <see cref="FallbackResourceManager"/> is used. This method may return <c>null</c> if the resource with the given name is not found.</summary>
        /// <inheritdoc/>
        public Stream? GetStream(string name, CultureInfo? culture)
        {
            if (resourceManagers.Count > 0)
            {
                foreach (var resourceManager in resourceManagers)
                {
                    // LUCENENET NOTE: MissingManifestResourceException or MissingSatelliteAssemblyException
                    // can be thrown here, but these indicate that there is a misconfigured setup (missing resource file,
                    // missing assembly, etc.) We intentionally let these errors propagate to ensure the developer who
                    // is creating resources is aware of these problems before the app is deployed (provided they test it).
                    // However, if the resource simply doesn't have an entry, we get a null return value instead and handle
                    // the fallback accordingly.
                    var result = resourceManager.GetStream(name, culture);
                    if (result is null)
                        continue;
                    return result;
                }
            }

            return FallbackResourceManager.GetStream(name, culture);
        }
    }
}
