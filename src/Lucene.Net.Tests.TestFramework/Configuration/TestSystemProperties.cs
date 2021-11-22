using J2N;
using Lucene.Net.Util;
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

    public class TestSystemProperties : ConfigurationSettingsTestCase
    {
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


        /// <summary>
        /// For subclasses to override. Overrides must call <c>base.TearDown()</c>.
        /// </summary>
        public override void TearDown()
        {
            ConfigurationSettings.CurrentConfiguration.Reload();
            base.TearDown();
        }

        private static readonly TestConfigurationFactory ConfigurationFactory = new TestConfigurationFactory
        {
            TestDirectory = testDirectory,
            JsonTestSettingsFileName = TestJsonFileName
        };

        protected override IConfiguration LoadConfiguration()
        {
            return ConfigurationFactory.GetConfiguration();
        }

        [Test]
        public virtual void TestRuntimeEnviromentSetting()
        {
            string testKey = "tests:setting";
            string testValue = "test.success";
            ConfigurationSettings.CurrentConfiguration[testKey] = testValue;
            Assert.AreEqual(testValue, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }

        [Test]
        public virtual void TestRuntimeEnviromentOverrideSetting()
        {
            Assert.AreEqual("fr", ConfigurationSettings.CurrentConfiguration["tests:locale"]);
            Assert.AreEqual("fr", SystemProperties.GetProperty("tests:locale"));
            ConfigurationSettings.CurrentConfiguration["tests:locale"] = "en";
            Assert.AreEqual("en", ConfigurationSettings.CurrentConfiguration["tests:locale"]);
            Assert.AreEqual("en", SystemProperties.GetProperty("tests:locale"));
        }

        [Ignore("Test SystemProperties is different from internal SystemProperties - seed is not set in base engine")]
        [Test]
        public virtual void TestHashCodeReadProperty()
        {

            Assert.AreEqual(0xf6a5c420, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), 0));

            Assert.AreEqual(16, StringHelper.GoodFastHashSeed);
            // Hashes computed using murmur3_32 from https://code.google.com/p/pyfasthash
            Assert.AreEqual(0xcd018ef6, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), StringHelper.GoodFastHashSeed));
        }

        [Test]
        public virtual void TestXMLConfiguration()
        {
            // TODO - not working with XML.
            Assert.AreEqual("0x00000010", ConfigurationSettings.CurrentConfiguration["tests:seed"]);
            Assert.AreEqual("0x00000010", SystemProperties.GetProperty("tests:seed"));
        }

        [Test]
        public virtual void TestCommandLineProperty()
        {
            TestContext.Progress.WriteLine("TestContext.Parameters ({0})", TestContext.Parameters.Count);
            foreach (var x in TestContext.Parameters.Names)
                TestContext.Progress.WriteLine(string.Format("{0}={1}", x, TestContext.Parameters[x]));
        }

        [Test]
        public virtual void TestCachedConfigProperty()
        {
            Assert.AreEqual("0x00000010", ConfigurationSettings.CurrentConfiguration["tests:seed"]);
            //Assert.AreEqual(0xf6a5c420, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), 0));
            //Assert.AreEqual(16, ConfigurationSettings.CurrentConfiguration["test.seed"));
            //// Hashes computed using murmurTR3_32 from https://code.google.com/p/pyfasthash
            //Assert.AreEqual(0xcd018ef6, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), StringHelper.GOOD_FAST_HASH_SEED));
        }
    }
}
