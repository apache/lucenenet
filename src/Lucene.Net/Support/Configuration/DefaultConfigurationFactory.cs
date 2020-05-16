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
        private IConfigurationBuilder builder { get; }
        private IConfiguration configuration;

        public DefaultConfigurationFactory(bool ignoreSecurityExceptionsOnRead)
        {
            this.builder = new LuceneConfigurationBuilder();
            builder.Add(new LuceneConfigurationSource() { Prefix = "lucene:" });
            this.ignoreSecurityExceptionsOnRead = ignoreSecurityExceptionsOnRead;
        }

        [CLSCompliant(false)]
        public virtual IConfiguration CreateConfiguration()
        {
            return EnsureInitialized();
        }

        /// <summary>
        /// Ensures the <see cref="Initialize"/> method has been called since the
        /// last application start. This method is thread-safe.
        /// </summary>
        [CLSCompliant(false)]
        protected IConfiguration EnsureInitialized()
        {
            return LazyInitializer.EnsureInitialized(ref this.configuration, ref this.initialized, ref this.m_initializationLock, () =>
            {
                this.configuration = Initialize();
                return this.configuration;
            });
        }

        /// <summary>
        /// Initializes the dependencies of this factory.
        /// </summary>
        [CLSCompliant(false)]
        protected virtual IConfiguration Initialize()
        {
            return builder.Build();
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
