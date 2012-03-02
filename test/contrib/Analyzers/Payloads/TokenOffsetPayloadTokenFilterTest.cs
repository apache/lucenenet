using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Payloads
{
    [TestFixture]
    public class TokenOffsetPayloadTokenFilterTest : BaseTokenStreamTestCase
    {
        [Test]
        public void Test()
        {
            String test = "The quick red fox jumped over the lazy brown dogs";

            TokenOffsetPayloadTokenFilter nptf = new TokenOffsetPayloadTokenFilter(new WhitespaceTokenizer(new StringReader(test)));
            int count = 0;
            PayloadAttribute payloadAtt = nptf.GetAttribute<PayloadAttribute>();
            OffsetAttribute offsetAtt = nptf.GetAttribute<OffsetAttribute>();

            while (nptf.IncrementToken())
            {
                Payload pay = payloadAtt.GetPayload();
                Assert.True(pay != null, "pay is null and it shouldn't be");
                byte[] data = pay.GetData();
                int start = PayloadHelper.DecodeInt(data, 0);
                Assert.True(start == offsetAtt.StartOffset(), start + " does not equal: " + offsetAtt.StartOffset());
                int end = PayloadHelper.DecodeInt(data, 4);
                Assert.True(end == offsetAtt.EndOffset(), end + " does not equal: " + offsetAtt.EndOffset());
                count++;
            }
            Assert.True(count == 10, count + " does not equal: " + 10);
        }
    }
}
