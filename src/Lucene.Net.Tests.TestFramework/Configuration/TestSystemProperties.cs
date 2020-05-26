using Lucene.Net.Util;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Linq;
using After = NUnit.Framework.TearDownAttribute;

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
    public class TestSystemProperties : ConfigurationSettingsTestCase
    {
        /// <summary>
        /// For subclasses to override. Overrides must call <c>base.TearDown()</c>.
        /// </summary>
        [After]
#pragma warning disable xUnit1013
        public virtual void TearDown()
#pragma warning restore xUnit1013
        {
            ConfigurationSettings.CurrentConfiguration.Reload();
            base.TearDown();
        }
        private static TestConfigurationRootFactory ConfigurationRootFactory = new TestConfigurationRootFactory();
        protected override IConfigurationRoot LoadConfiguration()
        {
            return ConfigurationRootFactory.CurrentConfiguration;
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

            Assert.AreEqual(16, StringHelper.GOOD_FAST_HASH_SEED);
            // Hashes computed using murmur3_32 from https://code.google.com/p/pyfasthash
            Assert.AreEqual(0xcd018ef6, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), StringHelper.GOOD_FAST_HASH_SEED));
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
