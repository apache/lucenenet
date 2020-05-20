using Microsoft.Extensions.Configuration;
using System.Threading;

namespace Lucene.Net.Configuration
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
    internal class DefaultConfigurationRootFactory : IConfigurationRootFactory
    {
        private bool initialized = false;
        protected object m_initializationLock = new object();
        private readonly IConfigurationBuilder builder;
        private IConfigurationRoot configuration;

        public DefaultConfigurationRootFactory(bool ignoreSecurityExceptionsOnRead = true)
        {
            this.builder = new ConfigurationBuilder();
            builder.Add(new LuceneDefaultConfigurationSource() { Prefix = "lucene:", IgnoreSecurityExceptionsOnRead = ignoreSecurityExceptionsOnRead });
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
