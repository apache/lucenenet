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
    public class TypeAsPayloadTokenFilterTest : BaseTokenStreamTestCase
    {
        [Test]
        public void test()
        {
            String test = "The quick red fox jumped over the lazy brown dogs";

            TypeAsPayloadTokenFilter nptf = new TypeAsPayloadTokenFilter(new WordTokenFilter(new WhitespaceTokenizer(new StringReader(test))));
            int count = 0;
            TermAttribute termAtt = nptf.GetAttribute<TermAttribute>();
            TypeAttribute typeAtt = nptf.GetAttribute<TypeAttribute>();
            PayloadAttribute payloadAtt = nptf.GetAttribute<PayloadAttribute>();

            while (nptf.IncrementToken())
            {
                Assert.True(typeAtt.Type().Equals(char.ToUpper(termAtt.TermBuffer()[0]).ToString()), typeAtt.Type() + " is not null and it should be");
                Assert.True(payloadAtt.GetPayload() != null, "nextToken.getPayload() is null and it shouldn't be");
                String type = Encoding.UTF8.GetString(payloadAtt.GetPayload().GetData()); ;
                Assert.True(type != null, "type is null and it shouldn't be");
                Assert.True(type.Equals(typeAtt.Type()) == true, type + " is not equal to " + typeAtt.Type());
                count++;
            }

            Assert.True(count == 10, count + " does not equal: " + 10);
        }

        private sealed class WordTokenFilter : TokenFilter
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
                    typeAtt.SetType(char.ToUpper(termAtt.TermBuffer()[0]).ToString());
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