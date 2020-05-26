using Lucene.Net.Util;
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

    [CLSCompliant(false)]
    public abstract class ConfigurationSettingsTestCase : LuceneTestCase
    {
        internal IProperties SystemProperties { get; private set; }

        [CLSCompliant(false)]
        public IConfigurationSettings ConfigurationSettings { get; private set; }

        protected abstract IConfigurationRoot LoadConfiguration();

        public override void BeforeClass()
        {
            base.BeforeClass();
            var configurationRoot = LoadConfiguration();
            // Set up mocks for ConfigurationSettings and SystemProperties
            ConfigurationSettings = new ConfigurationSettingsImpl(configurationRoot);
            SystemProperties = new Properties(() => configurationRoot);
        }

        [CLSCompliant(false)]
        public interface IConfigurationSettings
        {
            [CLSCompliant(false)]
            IConfigurationRoot CurrentConfiguration { get; }
        }

        public class ConfigurationSettingsImpl : IConfigurationSettings
        {
            private readonly IConfigurationRoot configurationRoot;

            [CLSCompliant(false)]
            public ConfigurationSettingsImpl(IConfigurationRoot configurationRoot)
            {
                this.configurationRoot = configurationRoot ?? throw new ArgumentNullException(nameof(configurationRoot));
            }

            [CLSCompliant(false)]
            public IConfigurationRoot CurrentConfiguration => configurationRoot;
        }
    }
}
