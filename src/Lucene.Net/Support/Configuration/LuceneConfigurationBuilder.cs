using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Lucene.Net.Configuration
{
    public class LuceneConfigurationBuilder : IConfigurationBuilder
    {
#if NETSTANDARD
        /// <summary>
        /// Returns the sources used to obtain configuration values.
        /// </summary>
        public IList<IConfigurationSource> Sources { get; } = new List<IConfigurationSource>();

        /// <summary>
        /// Gets a key/value collection that can be used to share data between the <see cref="IConfigurationBuilder"/>
        /// and the registered <see cref="IConfigurationProvider"/>s.
        /// </summary>
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
#elif NET45
        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        private IList<IConfigurationSource> _sources = new List<IConfigurationSource>();
        public IEnumerable<IConfigurationSource> Sources { get { return _sources; } }

        public Dictionary<string, object> Properties { get { return _properties; } }

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

#if NETSTANDARD
            Sources.Add(source);
#elif NET45
            _sources.Add(source);
#endif

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
    /// <summary>
    /// The root node for a configuration.
    /// </summary>
    public class ConfigurationRoot : IConfigurationRoot
    {
        private IList<IConfigurationProvider> _providers;

        /// <summary>
        /// Initializes a Configuration root with a list of providers.
        /// </summary>
        /// <param name="providers">The <see cref="IConfigurationProvider"/>s for this configuration.</param>
        public ConfigurationRoot(IList<IConfigurationProvider> providers)
        {
            if (providers == null)
            {
                throw new ArgumentNullException(nameof(providers));
            }

            _providers = providers;
            foreach (var p in providers)
            {
                p.Load();
                //ChangeToken.OnChange(() => p.GetReloadToken(), () => RaiseChanged());
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
                    string value;

                    if (provider.TryGet(key, out value))
                    {
                        return value;
                    }
                }

                return null;
            }

            set => throw new NotImplementedException();
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
                .Select(key => GetSection(path == null ? key : ConfigurationPath.Combine(path, key)));
        }

        /// <summary>
        /// Returns a <see cref="IChangeToken"/> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns></returns>
        public IChangeToken GetReloadToken() => null;

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
            //    var previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
            //    previousToken.OnReload();
        }
    }
    /// <summary>
    /// Represents a section of application configuration values.
    /// </summary>
    public class ConfigurationSection : IConfigurationSection
    {
        private readonly ConfigurationRoot _root;
        private readonly string _path;
        private string _key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="root">The configuration root.</param>
        /// <param name="path">The path to this section.</param>
        public ConfigurationSection(ConfigurationRoot root, string path)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            _root = root;
            _path = path;
        }

        /// <summary>
        /// Gets the full path to this section from the <see cref="IConfigurationRoot"/>.
        /// </summary>
        public string Path => _path;

        /// <summary>
        /// Gets the key this section occupies in its parent.
        /// </summary>
        public string Key
        {
            get
            {
                if (_key == null)
                {
                    // Key is calculated lazily as last portion of Path
                    _key = ConfigurationPath.GetSectionKey(_path);
                }
                return _key;
            }
        }

        /// <summary>
        /// Gets or sets the section value.
        /// </summary>
        public string Value
        {
            get
            {
                return _root[Path];
            }
            set
            {
                _root[Path] = value;
            }
        }

        /// <summary>
        /// Gets or sets the value corresponding to a configuration key.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string this[string key]
        {
            get
            {
                return _root[ConfigurationPath.Combine(Path, key)];
            }

            set
            {
                _root[ConfigurationPath.Combine(Path, key)] = value;
            }
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="IConfigurationSection"/>.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
        ///     an empty <see cref="IConfigurationSection"/> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key) => _root.GetSection(ConfigurationPath.Combine(Path, key));

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren() => _root.GetChildrenImplementation(Path);

        /// <summary>
        /// Returns a <see cref="IChangeToken"/> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns></returns>
        public IChangeToken GetReloadToken() => _root.GetReloadToken();

        IChangeToken IConfiguration.GetReloadToken()
        {
            throw new NotImplementedException();
        }
    }

}
