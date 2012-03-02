using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Payloads
{
    [TestFixture]
    public class NumericPayloadTokenFilterTest : BaseTokenStreamTestCase
    {
        [Test]
        public void Test()
        {
            String test = "The quick red fox jumped over the lazy brown dogs";

            NumericPayloadTokenFilter nptf = new NumericPayloadTokenFilter(new WordTokenFilter(new WhitespaceTokenizer(new StringReader(test))), 3, "D");
            bool seenDogs = false;
            TermAttribute termAtt = nptf.GetAttribute<TermAttribute>();
            TypeAttribute typeAtt = nptf.GetAttribute<TypeAttribute>();
            PayloadAttribute payloadAtt = nptf.GetAttribute<PayloadAttribute>();
            while (nptf.IncrementToken())
            {
                if (termAtt.Term().Equals("dogs"))
                {
                    seenDogs = true;
                    Assert.True(typeAtt.Type().Equals("D") == true, typeAtt.Type() + " is not equal to " + "D");
                    Assert.True(payloadAtt.GetPayload() != null, "payloadAtt.GetPayload() is null and it shouldn't be");
                    byte[] bytes = payloadAtt.GetPayload().GetData();//safe here to just use the bytes, otherwise we should use offset, length
                    Assert.True(bytes.Length == payloadAtt.GetPayload().Length(), bytes.Length + " does not equal: " + payloadAtt.GetPayload().Length());
                    Assert.True(payloadAtt.GetPayload().GetOffset() == 0, payloadAtt.GetPayload().GetOffset() + " does not equal: " + 0);
                    float pay = PayloadHelper.DecodeFloat(bytes);
                    Assert.True(pay == 3, pay + " does not equal: " + 3);
                }
                else
                {
                    Assert.True(typeAtt.Type().Equals("word"), typeAtt.Type() + " is not null and it should be");
                }
            }
            Assert.True(seenDogs == true, seenDogs + " does not equal: " + true);
        }

        internal sealed class WordTokenFilter : TokenFilter
        {
            private TermAttribute termAtt;
            private TypeAttribute typeAtt;

            internal WordTokenFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<TermAttribute>();
                typeAtt = AddAttribute<TypeAttribute>();
            }

            public override bool IncrementToken()
            {
                if (input.IncrementToken())
                {
                    if (termAtt.Term().Equals("dogs"))
                        typeAtt.SetType("D");
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
