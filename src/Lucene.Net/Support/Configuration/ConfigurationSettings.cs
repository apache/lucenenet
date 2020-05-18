using Lucene.Net.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

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

    public abstract class ConfigurationSettings
    {
        private static IConfigurationRootFactory configurationFactory = new DefaultConfigurationRootFactory(false);

        /// <summary>
        /// Sets the <see cref="IConfigurationRootFactory"/> instance used to instantiate
        /// <see cref="ConfigurationSettings"/> subclasses.
        /// </summary>
        /// <param name="configurationFactory">The new <see cref="IConfigurationRootFactory"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="configurationFactory"/> parameter is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static void SetConfigurationFactory(IConfigurationRootFactory configurationFactory)
        {
            ConfigurationSettings.configurationFactory = configurationFactory ?? throw new ArgumentNullException(nameof(configurationFactory));
        }

        /// <summary>
        /// Gets the associated ConfigurationSettings factory.
        /// </summary>
        /// <returns>The ConfigurationSettings factory.</returns>
        [CLSCompliant(false)]
        public static IConfigurationRootFactory GetConfigurationFactory()
        {
            return configurationFactory;
        }

        public static void Reload()
        {
            configurationFactory.ReloadConfiguration();
        }
        /*
         ********
         * Set IConfigurationBuilder directly instead of going via a factory
         * 
        
        private static IConfigurationBuilder configurationBuilder { get; set; } = new LuceneConfigurationBuilder().Add(new LuceneConfigurationSource());
        /// <summary>
        /// Sets the <see cref="IConfigurationFactory"/> instance used to instantiate
        /// <see cref="ConfigurationSettings"/> subclasses.
        /// </summary>
        /// <param name="configurationFactory">The new <see cref="IConfigurationFactory"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="configurationFactory"/> parameter is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static void SetConfiguration(IConfigurationBuilder configurationBuilder)
        {
            ConfigurationSettings.configurationBuilder = configurationBuilder ?? throw new ArgumentNullException(nameof(configurationBuilder));
        }
        /// <summary>
        /// Gets the associated ConfigurationSettings factory.
        /// </summary>
        /// <returns>The ConfigurationSettings factory.</returns>
        [CLSCompliant(false)]
        public static IConfigurationRoot GetConfiguration()
        {
            return configurationBuilder.Build();
        }
        */
    }
}
