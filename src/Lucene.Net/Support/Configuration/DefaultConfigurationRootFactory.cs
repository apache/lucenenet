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
        private readonly IConfigurationBuilder builder;
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
    }
}
