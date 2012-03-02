using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Miscellaneous
{
    [TestFixture]
    public class TestEmptyTokenStream : LuceneTestCase
    {
        [Test]
        public void Test()
        {
            TokenStream ts = new EmptyTokenStream();
            Assert.False(ts.IncrementToken());
            ts.Reset();
            Assert.False(ts.IncrementToken());
        }
    }
}
