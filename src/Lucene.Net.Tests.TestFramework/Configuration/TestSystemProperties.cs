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
            Assert.AreEqual(3, this.configurationBuilder.Sources.Count);
            Assert.Pass();
        }
    }
}
