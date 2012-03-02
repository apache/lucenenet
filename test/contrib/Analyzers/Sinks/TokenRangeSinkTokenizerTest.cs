using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Sinks;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Sinks
{
    [TestFixture]
    public class TokenRangeSinkTokenizerTest : BaseTokenStreamTestCase
    {
        [Test]
        public void Test()
        {
            TokenRangeSinkFilter sinkFilter = new TokenRangeSinkFilter(2, 4);
            String test = "The quick red fox jumped over the lazy brown dogs";
            TeeSinkTokenFilter tee = new TeeSinkTokenFilter(new WhitespaceTokenizer(new StringReader(test)));
            TeeSinkTokenFilter.SinkTokenStream rangeToks = tee.NewSinkTokenStream(sinkFilter);

            int count = 0;
            tee.Reset();
            while (tee.IncrementToken())
            {
                count++;
            }

            int sinkCount = 0;
            rangeToks.Reset();
            while (rangeToks.IncrementToken())
            {
                sinkCount++;
            }

            Assert.True(count == 10, count + " does not equal: " + 10);
            Assert.True(sinkCount == 2, "rangeToks Size: " + sinkCount + " is not: " + 2);
        }
    }
}