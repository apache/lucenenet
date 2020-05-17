//using Lucene.Net.Configuration;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;

//namespace Lucene.Net.Configuration
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    public class TestConfigurationFactory : DefaultConfigurationFactory
//    {

//        [CLSCompliant(false)]
//        public IConfigurationBuilder builder { get; set; }

//        [CLSCompliant(false)]
//        public TestConfigurationFactory(string currentPath = "", string defaultJsonConfigurationFilename = "luceneTestSettings.json", string defaultXmlConfigurationFilename = "luceneTestSettings.xml", IConfigurationSource[] configurationSources = null) : base(false)
//        {
//            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

//            configurationBuilder.AddEnvironmentVariables();
//            //configurationBuilder.AddXmlFilesFromRootDirectoryTo(currentPath, defaultXmlConfigurationFilename);
//            //configurationBuilder.AddJsonFilesFromRootDirectoryTo(currentPath, defaultJsonConfigurationFilename);
//            //configurationBuilder.Add(new TestParameterConfigurationSource(NUnit.Framework.TestContext.Parameters));

//            //foreach (IConfigurationSource source in configurationSources)
//            //{
//            //    configurationBuilder.Add(source);
//            //}

//            this.builder = configurationBuilder;
//        }

//        /// <summary>
//        /// Initializes the dependencies of this factory.
//        /// </summary>
//        [CLSCompliant(false)]
//        protected override IConfiguration Initialize()
//        {
//            return builder.Build();
//        }
//    }

//}


using Lucene.Net.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

    internal class TestConfigurationFactory : DefaultConfigurationFactory
    {
        private readonly ConcurrentDictionary<string, IConfiguration> configurationCache = new ConcurrentDictionary<string, IConfiguration>();

        public string JsonTestSettingsFileName { get; set; } = "lucene.testsettings.json";

        public IConfigurationBuilder builder { get; }

        public TestConfigurationFactory() : base(false)
        {

            //configurationBuilder.AddEnvironmentVariables();
            //configurationBuilder.AddXmlFilesFromRootDirectoryTo(currentPath, defaultXmlConfigurationFilename);
            //configurationBuilder.AddJsonFilesFromRootDirectoryTo(currentPath, defaultJsonConfigurationFilename);
            //configurationBuilder.Add(new TestParameterConfigurationSource(NUnit.Framework.TestContext.Parameters));

            //this.builder = configurationBuilder;
        }

        public override IConfiguration CreateConfiguration()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

            string testDirectory =
#if TESTFRAMEWORK_NUNIT
                NUnit.Framework.TestContext.CurrentContext.TestDirectory;
#elif NETSTANDARD
                System.AppContext.BaseDirectory;
#else
                AppDomain.CurrentDomain.BaseDirectory;
#endif

            return configurationCache.GetOrAdd(testDirectory, (key) =>
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
            ///// <summary>
            ///// Initializes the dependencies of this factory.
            ///// </summary>
            //[CLSCompliant(false)]
            //protected override IConfiguration Initialize()
            //{
            //    return builder.Build();
            //}
        }

}
