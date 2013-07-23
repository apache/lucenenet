using System;
using Lucene.Net.Codecs;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestNamedSPILoader : LuceneTestCase
    {
        [Test]
        public virtual void TestLookup()
        {
            var codec = Codec.ForName("Lucene42");
            assertEquals("Lucene42", codec.Name);
        }

        // we want an exception if its not found.
        [Test]
        public virtual void TestBogusLookup()
        {
            Assert.Throws<ArgumentException>(() => Codec.ForName("dskfdskfsdfksdfdsf"));
        }

        [Test]
        public virtual void TestAvailableServices()
        {
            var codecs = Codec.AvailableCodecs;
            Assert.IsTrue(codecs.Contains("Lucene42"));
        }
    }
}
