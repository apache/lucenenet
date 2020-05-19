using Lucene.Net.Configuration;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic.CompilerServices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;

namespace Lucene.Net.Cli.Configuration
{


    [TestFixture]
    class TestSystemProperties : LuceneTestCase
    {

        //[OneTimeSetUp]
        //public override void BeforeClass()
        //{
        //    ConfigurationFactory = new TestConfigurationFactory();
        //    base.BeforeClass();
        //}
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
            Lucene.Net.Util.SystemProperties.SetProperty("tests:locale", "en");
            Assert.AreEqual("en", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:locale"]);
        }

        [Test]
        public virtual void TestSetandUnset()
        {
            Assert.AreEqual("fr", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:locale"]);
            Lucene.Net.Util.SystemProperties.SetProperty("tests:locale", "en");
            Assert.AreEqual("en", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:locale"]);
            ConfigurationSettings.CurrentConfiguration.Reload();
            Assert.AreEqual("fr", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["tests:locale"]);
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
        [Ignore("not working")]
        [Test]
        public virtual void TestXMLConfiguration()
        {

            Assert.AreEqual("Title from  MyXMLFile", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["Position:Title"]);
            Assert.AreEqual("0x00000010", Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration["xmlseed"]);
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
