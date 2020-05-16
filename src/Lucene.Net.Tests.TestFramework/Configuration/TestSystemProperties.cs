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

namespace Lucene.Net.Configuration
{


    [TestFixture]
    class TestSystemProperties : LuceneTestCase
    {

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            ConfigurationFactory = new TestConfigurationFactory(TestBaseDirectory, TestSettingsFileNameJson, TestSettingsFileNameXml);
            base.BeforeClass();
        }
        [Test]
        public virtual void EnvironmentTest2()
        {
            Assert.AreEqual(Lucene.Net.Util.SystemProperties.GetProperty("windir"), "C:\\WINDOWS");
            Assert.Pass();

        }
        [Test]
        public virtual void SetTest()
        {
            Assert.AreEqual("fr-FR", Lucene.Net.Util.SystemProperties.GetProperty("tests:locale"));
            Lucene.Net.Util.SystemProperties.SetProperty("tests:locale", "en_EN");
            Assert.AreEqual("en_EN", Lucene.Net.Util.SystemProperties.GetProperty("tests:locale"));
            Assert.Pass();
        }
        [Test]
        public virtual void TestTimezone()
        {
            Assert.AreEqual("SE Asia Standard Time", Lucene.Net.Util.SystemProperties.GetProperty("user.timezone"));
            Assert.AreEqual("SE Asia Standard Time", Lucene.Net.Util.SystemProperties.GetProperty("user:timezone"));
            Assert.Pass();
        }

        [Test]
        public virtual void DirectoryScanTest()
        {
            Assert.AreEqual("Lucene.Net.Tests.TestFramework", Lucene.Net.Util.SystemProperties.GetProperty("tests:project"));
            Assert.AreEqual("Lucene.Net.TestFramework", Lucene.Net.Util.SystemProperties.GetProperty("tests:parent"));
            Assert.Pass();
        }

        [Test]
        public virtual void DirectoryCrawlTest()
        {
            Assert.AreEqual(6, ((TestConfigurationFactory)ConfigurationFactory).builder.Sources.Count);
            Assert.Pass();
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
            
            Assert.AreEqual("Title from  MyXMLFile", Lucene.Net.Util.SystemProperties.GetProperty("Position:Title"));
            Assert.AreEqual("0x00000010", Lucene.Net.Util.SystemProperties.GetProperty("xmlseed"));
        }
        [Test]
        public virtual void TestRunsettingsConfiguration()
        {
            Assert.AreEqual("localhost-runsettings", Lucene.Net.Util.SystemProperties.GetProperty("cli"));
        }

    }
}
