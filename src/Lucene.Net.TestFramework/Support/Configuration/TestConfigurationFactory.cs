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

    internal sealed class TestConfigurationFactory : IConfigurationFactory
    {
        private readonly ConcurrentDictionary<string, IConfigurationRoot> configurationCache = new ConcurrentDictionary<string, IConfigurationRoot>();

        /// <summary>
        /// Filename to be used for configuration settings.
        /// </summary>
        public string JsonTestSettingsFileName { get; set; } = "lucene.testsettings.json";

        /// <summary>
        /// Prefix to be used for environment variable settings.
        /// </summary>
        public string EnvironmentVariablePrefix { get; set; } = "lucene:";

        /// <summary>
        /// Test directory for mocking.
        /// </summary>
        internal string TestDirectory { get; set; }

        /// <summary>
        /// Initializes a cache containing a <see cref="LuceneDefaultConfigurationSource"/> and a JSON source by default. 
        /// Uses the supplied <see cref="JsonTestSettingsFileName"/>.
        /// </summary>
        /// <returns>An <see cref="IConfiguration"/> instance.</returns>
        public IConfiguration GetConfiguration()
        {
            string testDirectory = TestDirectory ?? // For mocking
#if TESTFRAMEWORK_NUNIT
                NUnit.Framework.TestContext.CurrentContext.TestDirectory;
#else
                AppDomain.CurrentDomain.BaseDirectory;
#endif

            return configurationCache.GetOrAdd(testDirectory, (key) =>
            {
                return new ConfigurationBuilder()
                    .AddEnvironmentVariables(EnvironmentVariablePrefix) // Use a custom prefix to only load Lucene.NET settings
                    .AddJsonFilesFromRootDirectoryTo(currentPath: key, JsonTestSettingsFileName)
#if TESTFRAMEWORK_NUNIT
                    .AddNUnitTestRunSettings()
#endif
                    .Build();
            });
        }
    }
}
