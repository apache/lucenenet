using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Cli.Configuration
{


    [TestFixture]
    class TestDefaultSystemProperties : LuceneTestCase
    {
        //[OneTimeSetUp]
        //public override void BeforeClass()
        //{
        //    //ConfigurationFactory = new DefaultConfigurationFactory(false);
        //    //base.BeforeClass();
        //}
        [Test]
        public virtual void ReadEnvironmentTest()
        {
            string testKey = "lucene:tests:setting";
            string testValue = "test.success";
            Assert.AreEqual(testValue, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey]);
        }
        [Test]
        public virtual void SetEnvironmentTest()
        {
            string testKey  = "lucene:tests:setting";
            string testValue = "test.success";
            Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey] = testValue;
            Assert.AreEqual(testValue, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey]);
        }

    }
}