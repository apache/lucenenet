using Lucene.Net.Configuration;
using Lucene.Net.Util;
using NUnit.Framework;
using After = NUnit.Framework.TearDownAttribute;

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

    [TestFixture]
    class TestSystemProperties : LuceneTestCase
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


        [Test]
        public virtual void EnvironmentTest2()
        {
            string testKey = "lucene:tests:setting";
            string testValue = "test.success";
            Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey] = testValue;
            Assert.AreEqual(Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey], testValue);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }
        [Test]
        public virtual void SetTest()
        {
            Assert.AreEqual("fr", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:locale"]);
            Assert.AreEqual("fr", SystemProperties.GetProperty("tests:locale"));
            Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:locale"] = "en";
            Assert.AreEqual("en", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:locale"]);
            Assert.AreEqual("en", SystemProperties.GetProperty("tests:locale"));
        }

        [Test]
        public virtual void TestHashCodeReadProperty()
        {

            Assert.AreEqual("0x00000010", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:seed"]);
            Assert.AreEqual(0xf6a5c420, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), 0));

            Assert.AreEqual(16, StringHelper.GOOD_FAST_HASH_SEED);
            // Hashes computed using murmur3_32 from https://code.google.com/p/pyfasthash
            Assert.AreEqual(0xcd018ef6, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), StringHelper.GOOD_FAST_HASH_SEED));
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
            Assert.AreEqual("0x00000010", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:seed"]);
            //Assert.AreEqual(0xf6a5c420, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), 0));
            //Assert.AreEqual(16, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["test.seed"));
            //// Hashes computed using murmurTR3_32 from https://code.google.com/p/pyfasthash
            //Assert.AreEqual(0xcd018ef6, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), StringHelper.GOOD_FAST_HASH_SEED));
        }
    }
}
