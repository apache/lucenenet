using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Sinks;
using Lucene.Net.Analysis.Tokenattributes;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Sinks
{
    [TestFixture]
    public class TokenTypeSinkTokenizerTest : BaseTokenStreamTestCase
    {
        [Test]
        public void Test()
        {
            TokenTypeSinkFilter sinkFilter = new TokenTypeSinkFilter("D");
            String test = "The quick red fox jumped over the lazy brown dogs";

            TeeSinkTokenFilter ttf = new TeeSinkTokenFilter(new WordTokenFilter(new WhitespaceTokenizer(new StringReader(test))));
            TeeSinkTokenFilter.SinkTokenStream sink = ttf.NewSinkTokenStream(sinkFilter);

            bool seenDogs = false;

            TermAttribute termAtt = ttf.AddAttribute<TermAttribute>();
            TypeAttribute typeAtt = ttf.AddAttribute<TypeAttribute>();
            ttf.Reset();
            while (ttf.IncrementToken())
            {
                if (termAtt.Term().Equals("dogs"))
                {
                    seenDogs = true;
                    Assert.True(typeAtt.Type().Equals("D") == true, typeAtt.Type() + " is not equal to " + "D");
                }
                else
                {
                    Assert.True(typeAtt.Type().Equals("word"), typeAtt.Type() + " is not null and it should be");
                }
            }
            Assert.True(seenDogs == true, seenDogs + " does not equal: " + true);

            int sinkCount = 0;
            sink.Reset();
            while (sink.IncrementToken())
            {
                sinkCount++;
            }

            Assert.True(sinkCount == 1, "sink Size: " + sinkCount + " is not: " + 1);
        }

        internal class WordTokenFilter : TokenFilter
        {
            private TermAttribute termAtt;
            private TypeAttribute typeAtt;

            internal WordTokenFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<TermAttribute>();
                typeAtt = AddAttribute<TypeAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                if (!input.IncrementToken()) return false;

                if (termAtt.Term().Equals("dogs"))
                {
                    typeAtt.SetType("D");
                }
                return true;
            }
        }
    }
}