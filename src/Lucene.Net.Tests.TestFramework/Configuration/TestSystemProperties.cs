using Lucene.Net.Util;
using NUnit.Framework;

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
    class TestSystemProperties : LuceneTestCase
    {
        [Test]
        public virtual void EnvironmentTest2()
        {
            string testKey = "lucene:tests:setting";
            string testValue = "test.success";
            Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey] = testValue;
            Assert.AreEqual(Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey], testValue);
        }
        [Test]
        public virtual void SetTest()
        {
            Assert.AreEqual("fr", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:locale"]);
            Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:locale"] = "en";
            Assert.AreEqual("en", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:locale"]);
        }

        [Test]
        public virtual void TestDefaults()
        {
            Assert.AreEqual("perMethod", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:cleanthreads:sysprop"]);
        }

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
            Assert.AreEqual("0x00000010", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:seed"]);
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
            Assert.AreEqual("0x00000020", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:seed"]);
            //Assert.AreEqual(0xf6a5c420, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), 0));
            //Assert.AreEqual(16, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["test.seed"));
            //// Hashes computed using murmur3_32 from https://code.google.com/p/pyfasthash
            //Assert.AreEqual(0xcd018ef6, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), StringHelper.GOOD_FAST_HASH_SEED));
        }
    }
}
