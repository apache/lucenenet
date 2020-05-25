using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

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

    internal class TestConfigurationRootFactory : DefaultConfigurationRootFactory, IConfigurationRootFactory
    {
        private readonly ConcurrentDictionary<string, IConfigurationRoot> configurationCache = new ConcurrentDictionary<string, IConfigurationRoot>();

        /// <summary>
        /// Filename to be used for configuration settings
        /// </summary>
        public string JsonTestSettingsFileName { get; set; } = "lucene.TestSettings.json";

        /// <summary>
        /// Initialises a cache containing a LuceneDefaultConfigurationSource and a Json Source by default. 
        /// Uses the supplied JsonTestSettingsFileName
        /// </summary>
        /// <returns>A ConfigurationRoot object</returns>

        public override IConfigurationRoot CurrentConfiguration
        {
            get
            {
                return CreateConfiguration();
            }
        }

        /// <summary>
        /// Build and return the configuration
        /// </summary>
        private IConfigurationRoot CreateConfiguration()
        {
            EnsureInitialized();

            string testDirectory =
#if TESTFRAMEWORK_NUNIT
            NUnit.Framework.TestContext.CurrentContext.TestDirectory;
#else
                            AppDomain.CurrentDomain.BaseDirectory;
#endif

            return configurationCache.GetOrAdd(testDirectory, (key) => { return null; });
        }
        /// <summary>
        /// Initializes the dependencies of this factory.
        /// </summary>
        protected override void Initialize()
        {

            string testDirectory =
#if TESTFRAMEWORK_NUNIT
            NUnit.Framework.TestContext.CurrentContext.TestDirectory;
#else
                            AppDomain.CurrentDomain.BaseDirectory;
#endif
            configurationCache.GetOrAdd(testDirectory, (key) =>
            {
                return new ConfigurationBuilder()
                    .AddLuceneDefaultSettings(prefix: "lucene:") // Use a custom prefix to only load Lucene.NET settings
                    .AddJsonFilesFromRootDirectoryTo(currentPath: key, JsonTestSettingsFileName)
#if TESTFRAMEWORK_NUNIT
                                .AddNUnitTestRunSettings()
#endif
                                .Build();
            });
        }
    }

}
