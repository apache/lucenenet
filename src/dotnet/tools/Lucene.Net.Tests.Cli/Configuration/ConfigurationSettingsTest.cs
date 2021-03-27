using J2N;
using Lucene.Net.Attributes;
using Lucene.Net.Configuration;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Cli.Configuration
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

    internal class ConfigurationSettingsTest : ConfigurationSettingsTestCase
    {
        private const string TestJsonFileName = "appsettings.json";
        private readonly static string TempFileDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        private readonly static string TestJsonFilePath = Path.Combine(TempFileDirectory, TestJsonFileName);


        // This variable must be unique in all of the tests
        private const string EnvironmentVariablePrefix = "lucene-cli:";

        protected override IConfiguration LoadConfiguration()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: EnvironmentVariablePrefix) // Use a custom prefix to only load Lucene.NET settings 
                .AddJsonFile(TestJsonFilePath)
                .Build();

            return configuration;
        }

        public override void BeforeClass()
        {
            // Create directories if they do not exist
            Directory.CreateDirectory(TempFileDirectory);

            // Output the test file to the file system
            using (var input = this.GetType().FindAndGetManifestResourceStream(TestJsonFileName))
            using (var output = new FileStream(TestJsonFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
            {
                input.CopyTo(output);
            }

            // set an Enviroment variable used in the test
            string testKey = EnvironmentVariablePrefix + "tests:setup";
            string testValue = "setup";
            Environment.SetEnvironmentVariable(testKey, testValue);
            base.BeforeClass();
        }

        public override void AfterClass()
        {
            try
            {
                File.Delete(TestJsonFilePath);
            }
            catch { }

            try
            {
                Directory.Delete(TempFileDirectory);
            }
            catch { }

            base.AfterClass();
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void SetRuntimeEnvironmentTest()
        {
            string testKey = "tests:setting";
            string testValue = "test.success";
            ConfigurationSettings.CurrentConfiguration[testKey] = testValue;
            Assert.AreEqual(testValue, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }

        [Test]
        [LuceneNetSpecific]
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
