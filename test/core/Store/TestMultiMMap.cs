using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Lucene.Net.Store
{
    [TestFixture]
    class TestMultiMMap
    {
        [Test]
        public void TestDoesntExist()
        {
            Assert.Ignore("Need to port tests, but we don't really support MMapDirectories anyway");
        }
    }
}
