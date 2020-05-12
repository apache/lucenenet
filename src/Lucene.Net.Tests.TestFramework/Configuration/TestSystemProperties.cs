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
        protected IConfigurationBuilder builder;

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            try
            {
#if NETSTANDARD2_1 || NETSTANDARD

                builder = new ConfigurationBuilder();

                string ANALYSIS_DATA_DIR = "";
                string fileName = "luceneTestSettings.json";

#if NETSTANDARD1_6
            string currentPath = System.AppContext.BaseDirectory;
#else
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
#endif

                builder.LoadJsonConfigurationFiles(currentPath, fileName);
                builder.AddEnvironmentVariables();

#elif NET451
                builder = new ConfigurationBuilder();
#else
                builder = new ConfigurationBuilder();
#endif
                // Setup the factories
                ConfigurationSettings.SetConfigurationFactory(
                    new MicrosoftExtensionsConfigurationFactory(false, builder));
            }
            catch (Exception ex)
            {
                // Write the stack trace so we have something to go on if an error occurs here.
                throw new Exception($"An exception occurred during BeforeClass:\n{ex.ToString()}", ex);
            }
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
            Assert.AreEqual(Lucene.Net.Util.SystemProperties.GetProperty("tests:locale"), "fr-FR");
            Lucene.Net.Util.SystemProperties.SetProperty("tests:locale", "en_EN");
            Assert.AreEqual(Lucene.Net.Util.SystemProperties.GetProperty("tests:locale"), "en_EN");
            Assert.Pass();
        }
        [Test]
        public virtual void TestTimezone()
        {
            Assert.AreEqual(Lucene.Net.Util.SystemProperties.GetProperty("user.timezone"), "SE Asia Standard Time");
            Assert.AreEqual(Lucene.Net.Util.SystemProperties.GetProperty("user:timezone"), "SE Asia Standard Time");
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
            Assert.AreEqual(3, builder.Sources.Count);
            Assert.Pass();
        }
    }
}
