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
    class TestConfigurationSettings : LuceneTestCase
    {

        internal class UnitTestConfigurationRootFactory : IConfigurationRootFactory
        {
            public bool IgnoreSecurityExceptionsOnRead { get; set; }
            /// <summary>
            /// Filename to be used for configuration settings
            /// </summary>
            public static string JsonTestSettingsFileName { get; set; } = "Configuration\\lucene.testsettings.json";

            static string JsonTestPath =
#if TESTFRAMEWORK_NUNIT
            NUnit.Framework.TestContext.CurrentContext.TestDirectory;
#else
                            AppDomain.CurrentDomain.BaseDirectory;
#endif
            public UnitTestConfigurationRootFactory()
            {
            }

            protected IConfigurationRoot configuration = new ConfigurationBuilder().Add(new LuceneDefaultConfigurationSource() { Prefix = "lucene:", IgnoreSecurityExceptionsOnRead = false }
            ).AddJsonFile(Path.Combine(JsonTestPath, JsonTestSettingsFileName)).Build();

            public virtual IConfigurationRoot CurrentConfiguration
            {
                get
                {
                    return configuration;
                }
            }
        }

        public static IConfigurationRootFactory TestConfigurationFactory;

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            string testKey = "lucene:tests:setup";
            string testValue = "setup";
            Environment.SetEnvironmentVariable(testKey, testValue);
            ConfigurationSettings.SetConfigurationRootFactory(new UnitTestConfigurationRootFactory());
            base.BeforeClass();
        }
        //private JsonConfigurationProvider LoadProvider(string json)
        //{
        //    var p = new JsonConfigurationProvider(new JsonConfigurationSource { Optional = true });
        //    p.Load(TestStreamHelpers.StringToStream(json));
        //    return p;
        //}

        [Test]
        public void LoadKeyValuePairsFromValidJson()
        {
            var json = @"
{
    'firstname': 'test',
    'test.last.name': 'last.name',
        'residential.address': {
            'street.name': 'Something street',
            'zipcode': '12345'
        }
}";
            //var jsonConfigSrc = LoadProvider(json);

            //Assert.AreEqual("test", jsonConfigSrc.Get("firstname"));
            //Assert.AreEqual("last.name", jsonConfigSrc.Get("test.last.name"));
            //Assert.AreEqual("Something street", jsonConfigSrc.Get("residential.address:STREET.name"));
            //Assert.AreEqual("12345", jsonConfigSrc.Get("residential.address:zipcode"));
        }

        [Test]
        public virtual void ReadEnvironmentTest()
        {
            string testKey = "tests:setup";
            string testValue = "setup";
            Assert.AreEqual(testValue, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }
        [Test]
        public virtual void SetEnvironmentTest()
        {
            string testKey = "lucene:tests:setting";
            string testValue = "test.success";
            Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey] = testValue;
            Assert.AreEqual(testValue, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }

        [Test]
        public virtual void TestSetandUnset()
        {
            string testKey = "tests:locale";
            string testValue_fr = "fr";
            string testValue_en = "en";
            //Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey] = testValue_fr;
            Assert.AreEqual(testValue_fr, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:locale"]);
            Assert.AreEqual(testValue_fr, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue_fr, SystemProperties.GetProperty(testKey));
            Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey] = testValue_en;
            Assert.AreEqual(testValue_en, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue_en, SystemProperties.GetProperty(testKey));
            ConfigurationSettings.CurrentConfiguration.Reload();
            Assert.AreEqual(testValue_fr, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue_fr, SystemProperties.GetProperty(testKey));
        }

    }
}
