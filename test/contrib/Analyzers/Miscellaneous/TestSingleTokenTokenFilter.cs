using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Miscellaneous
{
    [TestFixture]
    public class TestSingleTokenTokenFilter : LuceneTestCase
    {
        [Test]
        public void Test()
        {
            Token token = new Token();
            SingleTokenTokenStream ts = new SingleTokenTokenStream(token);
            AttributeImpl tokenAtt = (AttributeImpl)ts.AddAttribute<TermAttribute>();
            Assert.True(tokenAtt is Token);
            ts.Reset();

            Assert.True(ts.IncrementToken());
            Assert.AreEqual(token, tokenAtt);
            Assert.False(ts.IncrementToken());

            token = new Token("hallo", 10, 20, "someType");
            ts.SetToken(token);
            ts.Reset();

            Assert.True(ts.IncrementToken());
            Assert.AreEqual(token, tokenAtt);
            Assert.False(ts.IncrementToken());
        }
    }
}
