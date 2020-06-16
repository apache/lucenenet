// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Configuration
{
    /// <summary>
    /// Used to build key/value based configuration settings for use in an application.
    /// </summary>
    internal class ConfigurationBuilder : IConfigurationBuilder
    {
        private readonly IList<IConfigurationSource> _sources = new List<IConfigurationSource>();
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        /// <summary>
        /// Returns the sources used to obtain configuration values.
        /// </summary>
#if FEATURE_ICONFIGURATIONBUILDER_SOURCES_AS_ILIST
        public IList<IConfigurationSource> Sources => _sources;
#else
        
        public IEnumerable<IConfigurationSource> Sources => _sources;
#endif

        /// <summary>
        /// Gets a key/value collection that can be used to share data between the <see cref="IConfigurationBuilder"/>
        /// and the registered <see cref="IConfigurationProvider"/>s.
        /// </summary>
#if FEATURE_ICONFIGURATIONBUILDER_PROPERTIES_AS_IDICTIONARY
        public IDictionary<string, object> Properties => _properties;
#else
        
        public Dictionary<string, object> Properties => _properties;
#endif
        /// <summary>
        /// Adds a new configuration source.
        /// </summary>
        /// <param name="source">The configuration source to add.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/>.</returns>
        public IConfigurationBuilder Add(IConfigurationSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _sources.Add(source);

            return this;
        }

        /// <summary>
        /// Builds an <see cref="IConfiguration"/> with keys and values from the set of providers registered in
        /// <see cref="Sources"/>.
        /// </summary>
        /// <returns>An <see cref="IConfigurationRoot"/> with keys and values from the registered providers.</returns>
        public IConfigurationRoot Build()
        {
            var providers = new List<IConfigurationProvider>();
            foreach (var source in Sources)
            {
                var provider = source.Build(this);
                providers.Add(provider);
            }
            return new ConfigurationRoot(providers);
        }
    }
}
