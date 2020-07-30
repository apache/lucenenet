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

    /// <summary>
    /// The default implementation of <see cref="IConfigurationFactory"/> that is used when
    /// the end user doesn't supply one. This implementation simply reads settings from
    /// environment variables.
    /// </summary>
    internal sealed class DefaultConfigurationFactory : IConfigurationFactory
    {
        private IConfiguration configuration;

        /// <summary>
        /// Returns the default configuration instance, creating it first if necessary.
        /// </summary>
        /// <returns>The default <see cref="IConfiguration"/> instance.</returns>
        public IConfiguration GetConfiguration()
        {
            return LazyInitializer.EnsureInitialized(ref this.configuration,
                () => new ConfigurationRoot(new IConfigurationProvider[] {
                    new EnvironmentVariablesConfigurationProvider(prefix: "lucene:", ignoreSecurityExceptionsOnRead: true)
                }));
        }
    }
}
