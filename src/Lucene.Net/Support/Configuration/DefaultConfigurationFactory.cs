using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Lucene.Net.Configuration
{
    public class DefaultConfigurationFactory : IConfigurationFactory
    {
        private readonly bool ignoreSecurityExceptionsOnRead;
        private bool initialized = false;
        protected object m_initializationLock = new object();
        private object initializationTarget; // Dummy variable required by LazyInitializer.EnsureInitialized
        protected IConfiguration configuration;

        public DefaultConfigurationFactory(bool ignoreSecurityExceptionsOnRead)
        {
            this.ignoreSecurityExceptionsOnRead = ignoreSecurityExceptionsOnRead;
        }

        public virtual IConfiguration CreateConfiguration()
        {
            EnsureInitialized();
            return configuration;
        }

        /// <summary>
        /// Ensures the <see cref="Initialize"/> method has been called since the
        /// last application start. This method is thread-safe.
        /// </summary>
        protected void EnsureInitialized()
        {
            LazyInitializer.EnsureInitialized(ref this.initializationTarget, ref this.initialized, ref this.m_initializationLock, () =>
            {
                Initialize();
                return null;
            });
        }

        /// <summary>
        /// Initializes the dependencies of this factory.
        /// </summary>
        protected virtual void Initialize()
        {
            configuration = new DefaultConfiguration(this.ignoreSecurityExceptionsOnRead);

        }
    }

    internal class DefaultConfiguration : IConfiguration
    {
        private readonly bool ignoreSecurityExceptionsOnRead;

        public DefaultConfiguration(bool ignoreSecurityExceptionsOnRead)
        {
            this.ignoreSecurityExceptionsOnRead = ignoreSecurityExceptionsOnRead;
        }

        public string this[string key]
        {
            get
            {
                if (ignoreSecurityExceptionsOnRead)
                {
                    try
                    {
                        return Environment.GetEnvironmentVariable(key);
                    }
                    catch (SecurityException)
                    {
                        return null;
                    }
                }
                else
                {
                    return Environment.GetEnvironmentVariable(key);
                }
            }
            set => Environment.SetEnvironmentVariable(key, value);
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            throw new NotImplementedException();
        }

        public IChangeToken GetReloadToken()
        {
            throw new NotImplementedException();
        }

        public IConfigurationSection GetSection(string key)
        {
            throw new NotImplementedException();
        }
    }

}
