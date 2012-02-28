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
    public class DateRecognizerSinkTokenizerTest : BaseTokenStreamTestCase
    {
        [Test]
        public void Test()
        {
            DateRecognizerSinkFilter sinkFilter = new DateRecognizerSinkFilter(System.Globalization.CultureInfo.CurrentCulture);
            String test = "The quick red fox jumped over the lazy brown dogs on 7/11/2006  The dogs finally reacted on 7/12/2006";
            TeeSinkTokenFilter tee = new TeeSinkTokenFilter(new WhitespaceTokenizer(new StringReader(test)));
            TeeSinkTokenFilter.SinkTokenStream sink = tee.NewSinkTokenStream(sinkFilter);
            int count = 0;

            tee.Reset();
            while (tee.IncrementToken())
            {
                count++;
            }
            Assert.True(count == 18, count + " does not equal: " + 18);

            int sinkCount = 0;
            sink.Reset();
            while (sink.IncrementToken())
            {
                sinkCount++;
            }
            Assert.True(sinkCount == 2, "sink Size: " + sinkCount + " is not: " + 2);
        }
    }
}
