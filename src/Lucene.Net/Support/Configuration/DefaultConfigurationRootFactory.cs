using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Lucene.Net.Configuration
{
    internal class DefaultConfigurationRootFactory : IConfigurationRootFactory
    {
        private readonly bool ignoreSecurityExceptionsOnRead;
        private bool initialized = false;
        protected object m_initializationLock = new object();
        private IConfigurationBuilder builder { get; }
        private IConfigurationRoot configuration;

        public DefaultConfigurationRootFactory(bool ignoreSecurityExceptionsOnRead)
        {
            this.builder = new ConfigurationBuilder();
            builder.Add(new LuceneDefaultConfigurationSource() { Prefix = "lucene:" });
            this.ignoreSecurityExceptionsOnRead = ignoreSecurityExceptionsOnRead;
        }

        public virtual IConfigurationRoot CreateConfiguration()
        {
            return EnsureInitialized();
        }

        /// <summary>
        /// Ensures the <see cref="Initialize"/> method has been called since the
        /// last application start. This method is thread-safe.
        /// </summary>
        protected IConfigurationRoot EnsureInitialized()
        {
            return LazyInitializer.EnsureInitialized(ref this.configuration, ref this.initialized, ref this.m_initializationLock, () =>
            {
                return Initialize();
            });
        }

        /// <summary>
        /// Initializes the dependencies of this factory.
        /// </summary>
        protected virtual IConfigurationRoot Initialize()
        {
            return builder.Build();
        }
        /// <summary>
        /// Reloads the dependencies of this factory.
        /// </summary>
        public void ReloadConfiguration()
        {
            CreateConfiguration().Reload();
        }
    }
    //internal class DefaultConfiguration : IConfiguration
    //{
    //    private readonly bool ignoreSecurityExceptionsOnRead;

    //    public DefaultConfiguration(bool ignoreSecurityExceptionsOnRead)
    //    {
    //        this.ignoreSecurityExceptionsOnRead = ignoreSecurityExceptionsOnRead;
    //    }

    //    public string this[string key]
    //    {
    //        get
    //        {
    //            if (ignoreSecurityExceptionsOnRead)
    //            {
    //                try
    //                {
    //                    return Environment.GetEnvironmentVariable(key);
    //                }
    //                catch (SecurityException)
    //                {
    //                    return null;
    //                }
    //            }
    //            else
    //            {
    //                return Environment.GetEnvironmentVariable(key);
    //            }
    //        }
    //        set => Environment.SetEnvironmentVariable(key, value);
    //    }

    //    public IEnumerable<IConfigurationSection> GetChildren()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public IChangeToken GetReloadToken()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public IConfigurationSection GetSection(string key)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

}
