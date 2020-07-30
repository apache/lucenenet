using J2N;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

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

    internal class TestConfigurationSettings : ConfigurationSettingsTestCase
    {
        // This variable must be unique in all of the tests
        private const string EnvironmentVariablePrefix = "lucenetest:";

        // Using a different file extension ensures we don't accidentally load the
        // settings from the current test framework setup, and only get the mock values.
        private const string TestJsonFileName = "lucene.testsettings.mock.json";
        private const string TestParentJsonFileName = "parent.lucene.testsettings.mock.json";
        private readonly static string parentDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        private readonly static string testDirectory = Path.Combine(parentDirectory, "SubDirectory");
        private readonly static string currentJsonFilePath = Path.Combine(testDirectory, TestJsonFileName);
        private readonly static string parentJsonFilePath = Path.Combine(parentDirectory, TestJsonFileName);

        public override void BeforeClass()
        {
            

            // Create directories if they do not exist
            Directory.CreateDirectory(testDirectory);

            // Output the current test file to the file system
            using (var input = this.GetType().FindAndGetManifestResourceStream(TestJsonFileName))
            using (var output = new FileStream(currentJsonFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
            {
                input.CopyTo(output);
            }

            // Output the parent test file to the file system
            using (var input = this.GetType().FindAndGetManifestResourceStream(TestParentJsonFileName))
            using (var output = new FileStream(parentJsonFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
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
            DirectoryInfo dir = null;

            try
            {
                dir = new DirectoryInfo(testDirectory);
            }
            catch { }


            try
            {
                foreach (var file in dir.GetFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch { }
                }
            }
            catch { }

            try
            {
                dir.Delete();
            }
            catch { }

            try
            {
                File.Delete(parentJsonFilePath);
            }
            catch { }

            try
            {
                Directory.Delete(parentDirectory);
            }
            catch { }

            base.AfterClass();
        }

        private static readonly IConfigurationFactory ConfigurationFactory = new TestConfigurationFactory
        {
            TestDirectory = testDirectory,
            JsonTestSettingsFileName = TestJsonFileName,
            EnvironmentVariablePrefix = EnvironmentVariablePrefix
        };

        protected override IConfiguration LoadConfiguration()
        {
            return ConfigurationFactory.GetConfiguration();
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

        [Test(Description = "Tests whether the a default value in a JSON file in the parent directory exists.")]
        public virtual void TestDefaultedValue()
        {
            string testKey = "tests:defaulted";
            string testValue = "the-default";
            Assert.AreEqual(testValue, ConfigurationSettings.CurrentConfiguration[testKey]);
        }

        [Test(Description = "Tests whether the a default value in a JSON file in the parent directory can be overridden by a subdirectory.")]
        public virtual void TestOverriddenValue()
        {
            string testKey = "tests:overridden";
            string testValue = "the-override";
            Assert.AreEqual(testValue, ConfigurationSettings.CurrentConfiguration[testKey]);
        }
    }
}
