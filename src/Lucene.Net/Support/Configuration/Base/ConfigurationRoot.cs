// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Lucene.Net.Configuration
{
    /// <summary>
    /// The root node for a configuration.
    /// </summary>
    internal class ConfigurationRoot : IConfiguration
#if FEATURE_ICONFIGURATIONROOT_PROVIDERS
        // LUCENENET: Since the IConfigurationRoot interface changed after version 1.1.2, we cannot implement it here
        // or upgrading the version of Microsoft.Extensions.Configuration will fail.
        , IConfigurationRoot
#endif
    {
        private readonly IList<IConfigurationProvider> _providers;
        private ConfigurationReloadToken _changeToken = new ConfigurationReloadToken();

        /// <summary>
        /// Initializes a Configuration root with a list of providers.
        /// </summary>
        /// <param name="providers">The <see cref="IConfigurationProvider"/>s for this configuration.</param>
        public ConfigurationRoot(IList<IConfigurationProvider> providers)
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            foreach (var p in providers)
            {
                p.Load();
                ChangeToken.OnChange(() => p.GetReloadToken(), () => RaiseChanged());
            }
        }

        /// <summary>
        /// The <see cref="IConfigurationProvider"/>s for this configuration.
        /// </summary>
        public IEnumerable<IConfigurationProvider> Providers => _providers;

        /// <summary>
        /// Gets or sets the value corresponding to a configuration key.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string this[string key]
        {
            get
            {
                foreach (var provider in _providers.Reverse())
                {
                    if (provider.TryGet(key, out string value))
                        return value;
                }

                return null;
            }

            set
            {
                if (_providers.Count == 0)
                {
                    throw new InvalidOperationException();
                }

                foreach (var provider in _providers)
                {
                    provider.Set(key, value);
                }
            }
        }

        /// <summary>
        /// Gets the immediate children sub-sections.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IConfigurationSection> GetChildren() => GetChildrenImplementation(null);

        internal IEnumerable<IConfigurationSection> GetChildrenImplementation(string path)
        {
            return _providers
                .Aggregate(Enumerable.Empty<string>(),
                    (seed, source) => source.GetChildKeys(seed, path))
                .Distinct()
                .Select(key => GetSection(path is null ? key : ConfigurationPath.Combine(path, key)));
        }

        /// <summary>
        /// Returns a <see cref="IChangeToken"/> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns></returns>
        public IChangeToken GetReloadToken() => _changeToken;

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="IConfigurationSection"/>.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
        ///     an empty <see cref="IConfigurationSection"/> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
            => new ConfigurationSection(this, key);

        /// <summary>
        /// Force the configuration values to be reloaded from the underlying sources.
        /// </summary>
        public void Reload()
        {
            foreach (var provider in _providers)
            {
                provider.Load();
            }
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            var previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }
    }
}
