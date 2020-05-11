using Lucene.Net.Util;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Configuration
{
    [TestFixture]
    class TestSystemProperties : LuceneTestCase
    {
        protected IConfiguration config;

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            try
            {
#if NETSTANDARD2_1 || NETSTANDARD

                config = new ConfigurationBuilder()
                  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                  .AddEnvironmentVariables()
                  .Build();

#elif NET451
            
            
                config = new ConfigurationBuilder()
                  .Build();
            
#else
                config = new ConfigurationBuilder()
                  .Build();

#endif
                // Setup the factories
                ConfigurationSettings.SetConfigurationFactory(
                    new MicrosoftExtensionsConfigurationFactory(false, config));
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
            config["tests:locale"] = "en_EN";
            Assert.AreEqual(Lucene.Net.Util.SystemProperties.GetProperty("tests:locale"), "en_EN");
            Assert.AreNotEqual(Lucene.Net.Util.SystemProperties.GetProperty("tests:locale"), "fr-FR");
            Assert.Pass();
        }
        [Test]
        public virtual void TestTimezone()
        {
            Assert.AreEqual(Lucene.Net.Util.SystemProperties.GetProperty("user.timezone"), "SE Asia Standard Time");
            Assert.AreEqual(Lucene.Net.Util.SystemProperties.GetProperty("user:timezone"), "SE Asia Standard Time");
            Assert.Pass();
        }
    }
}
