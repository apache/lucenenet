using Lucene.Net.Configuration;
using Microsoft.Extensions.Configuration;
using System;
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

    public class MicrosoftExtensionsConfigurationFactory : DefaultConfigurationFactory
    {
        [CLSCompliant(false)]
        public IConfigurationBuilder builder { get; }

        [CLSCompliant(false)]
        public MicrosoftExtensionsConfigurationFactory(IConfigurationBuilder builder) : base(false)
        {
            this.builder = builder;
        }

        [CLSCompliant(false)]
        public MicrosoftExtensionsConfigurationFactory(string[] args = null, string defaultJsonConfigurationFilename = "luceneTestSettings.json", string defaultXmlConfigurationFilename = "luceneTestSettings.xml") : base(false)
        {
#if NETSTANDARD
            string currentPath = System.AppContext.BaseDirectory;
#else
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
#endif

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

#if NETSTANDARD

            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddJsonFilesFromRootDirectoryTo(currentPath, defaultJsonConfigurationFilename);
            configurationBuilder.AddXmlFilesFromRootDirectoryTo(currentPath, defaultXmlConfigurationFilename);
            if (args != null)
                configurationBuilder.AddCommandLine(args);
#elif NET45
                // NET45 specific setup for builder
#else
                // Not sure if there is a default case that isnt covered?
#endif
            this.builder = configurationBuilder;
        }

        /// <summary>
        /// Initializes the dependencies of this factory.
        /// </summary>
        [CLSCompliant(false)]
        protected override IConfiguration Initialize()
        {
            return builder.Build();
        }
    }

}
