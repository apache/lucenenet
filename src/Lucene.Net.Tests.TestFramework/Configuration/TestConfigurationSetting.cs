using Lucene.Net.Util;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.IO;

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

    [TestFixture]
    internal class TestConfigurationSettings : ConfigurationSettingsTestCase
    {
        // This variable must be unique in all of the tests
        private const string EnvironmentVariablePrefix = "lucenetest:";

        internal class UnitTestConfigurationRootFactory : IConfigurationRootFactory
        {
            public bool IgnoreSecurityExceptionsOnRead { get; set; }
            /// <summary>
            /// PAth to be used for configuration settings
            /// </summary>
            public static string JsonTestSettingsFolderName { get; set; } = @"Configuration";
            /// <summary>
            /// Filename to be used for configuration settings
            /// </summary>
            public static string JsonTestSettingsFileName { get; set; } = @"lucene.testsettings.json";

            static string JsonTestPath =
#if TESTFRAMEWORK_NUNIT
            NUnit.Framework.TestContext.CurrentContext.TestDirectory;
#else
                            AppDomain.CurrentDomain.BaseDirectory;
#endif
            public UnitTestConfigurationRootFactory()
            {
            }

            protected IConfigurationRoot configuration = new ConfigurationBuilder().Add(new LuceneDefaultConfigurationSource() { Prefix = EnvironmentVariablePrefix, IgnoreSecurityExceptionsOnRead = false }
            ).AddJsonFile(Path.Combine(new string[] { JsonTestPath, JsonTestSettingsFolderName, JsonTestSettingsFileName })).Build();

            public virtual IConfigurationRoot CurrentConfiguration
            {
                get
                {
                    return configuration;
                }
            }

            public IConfigurationRoot CreateConfiguration()
            {
                return CurrentConfiguration;
            }
        }

        protected override IConfigurationRoot LoadConfiguration()
        {
            return new UnitTestConfigurationRootFactory().CurrentConfiguration;
        }

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();
            // set an Enviroment variable used in the test
            string testKey = EnvironmentVariablePrefix + "tests:setup";
            string testValue = "setup";
            Environment.SetEnvironmentVariable(testKey, testValue);
        }

        [Test]
        public virtual void ReadPreconfiguredEnvironmentTest()
        {
            string testKey = "tests:setup";
            string testValue = "setup";
            Assert.AreEqual(testValue, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }
        [Test]
        public virtual void SetRuntimeEnvironmentTest()
        {
            string testKey = "tests:setting";
            string testValue = "test.success";
            ConfigurationSettings.CurrentConfiguration[testKey] = testValue;
            Assert.AreEqual(testValue, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }

        [Test]
        public virtual void TestSetandUnset()
        {
            string testKey = "tests:locale";
            string testValue_fr = "fr";
            string testValue_en = "en";
            //ConfigurationSettings.CurrentConfiguration[testKey] = testValue_fr;
            Assert.AreEqual(testValue_fr, ConfigurationSettings.CurrentConfiguration["tests:locale"]);
            Assert.AreEqual(testValue_fr, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue_fr, SystemProperties.GetProperty(testKey));
            ConfigurationSettings.CurrentConfiguration[testKey] = testValue_en;
            Assert.AreEqual(testValue_en, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue_en, SystemProperties.GetProperty(testKey));
            ConfigurationSettings.CurrentConfiguration.Reload();
            Assert.AreEqual(testValue_fr, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue_fr, SystemProperties.GetProperty(testKey));
        }
    }
}
