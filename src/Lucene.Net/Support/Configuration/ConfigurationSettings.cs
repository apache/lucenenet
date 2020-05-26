using Microsoft.Extensions.Configuration;
using System;

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
    /// Provides access to the application's configuration settings.
    /// </summary>
    public static class ConfigurationSettings
    {
        private static IConfigurationFactory configurationFactory = new DefaultConfigurationFactory();

        /// <summary>
        /// Sets the <see cref="IConfigurationFactory"/> instance used to instantiate
        /// <see cref="ConfigurationSettings"/> subclasses.
        /// </summary>
        /// <param name="configurationFactory">The new <see cref="IConfigurationFactory"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="configurationFactory"/> parameter is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static void SetConfigurationFactory(IConfigurationFactory configurationFactory)
        {
            ConfigurationSettings.configurationFactory = configurationFactory ?? throw new ArgumentNullException(nameof(configurationFactory));
        }

        /// <summary>
        /// Gets the associated <see cref="IConfigurationFactory"/> factory.
        /// </summary>
        /// <returns>The <see cref="IConfigurationFactory"/> factory.</returns>
        [CLSCompliant(false)]
        public static IConfigurationFactory GetConfigurationFactory()
        {
            return configurationFactory;
        }

        /// <summary>
        /// Returns the current configuration
        /// </summary>
        [CLSCompliant(false)]
        public static IConfiguration CurrentConfiguration => configurationFactory.GetConfiguration();
    }
}
