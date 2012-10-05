/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Lucene.Net.Analysis;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Snowball
{
    [TestFixture]
    public class TestSnowball : BaseTokenStreamTestCase
    {
        [Test]
        public void TestEnglish()
        {
            Analyzer a = new SnowballAnalyzer(Version.LUCENE_CURRENT, "English");
            AssertAnalyzesTo(a, "he abhorred accents",
                new String[] { "he", "abhor", "accent" });
        }

        [Test]
        public void TestStopwords()
        {
            Analyzer a = new SnowballAnalyzer(Version.LUCENE_CURRENT, "English",
                StandardAnalyzer.STOP_WORDS_SET);
            AssertAnalyzesTo(a, "the quick brown fox jumped",
                new String[] { "quick", "brown", "fox", "jump" });
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer a = new SnowballAnalyzer(Version.LUCENE_CURRENT, "English");
            AssertAnalyzesToReuse(a, "he abhorred accents",
                new String[] { "he", "abhor", "accent" });
            AssertAnalyzesToReuse(a, "she abhorred him",
                new String[] { "she", "abhor", "him" });
        }

        /*
         * subclass that acts just like whitespace analyzer for testing
         */
        private class SnowballSubclassAnalyzer : SnowballAnalyzer
        {
            public SnowballSubclassAnalyzer(String name)
                : base(Version.LUCENE_CURRENT, name)
            {

            }

            public override TokenStream TokenStream(String fieldName, TextReader reader)
            {
                return new WhitespaceTokenizer(reader);
            }
        }

        [Test]
        public void TestLucene1678BwComp()
        {
            Analyzer a = new SnowballSubclassAnalyzer("English");
            AssertAnalyzesToReuse(a, "he abhorred accents",
                new String[] { "he", "abhorred", "accents" });
        }

        [Test]
        public void TestFilterTokens()
        {
            SnowballFilter filter = new SnowballFilter(new TestTokenStream(), "English");
            ITermAttribute termAtt = filter.GetAttribute<ITermAttribute>();
            IOffsetAttribute offsetAtt = filter.GetAttribute<IOffsetAttribute>();
            ITypeAttribute typeAtt = filter.GetAttribute<ITypeAttribute>();
            IPayloadAttribute payloadAtt = filter.GetAttribute<IPayloadAttribute>();
            IPositionIncrementAttribute posIncAtt = filter.GetAttribute<IPositionIncrementAttribute>();
            IFlagsAttribute flagsAtt = filter.GetAttribute<IFlagsAttribute>();

            filter.IncrementToken();

            Assert.AreEqual("accent", termAtt.Term);
            Assert.AreEqual(2, offsetAtt.StartOffset);
            Assert.AreEqual(7, offsetAtt.EndOffset);
            Assert.AreEqual("wrd", typeAtt.Type);
            Assert.AreEqual(3, posIncAtt.PositionIncrement);
            Assert.AreEqual(77, flagsAtt.Flags);
            Assert.AreEqual(new Payload(new byte[] { 0, 1, 2, 3 }), payloadAtt.Payload);
        }

        [Test(Description = "LUCENENET-54")]
        public void TestJiraLuceneNet54()
        {
            var analyzer = new SnowballAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT, "Finnish");
            var input = new StringReader("terve");
            var tokenStream = analyzer.TokenStream("fieldName", input);
            var termAttr = tokenStream.AddAttribute<ITermAttribute>();
            Assert.That(tokenStream.IncrementToken(), Is.True);
            Assert.That(termAttr.Term, Is.EqualTo("terv"));
        }

        private sealed class TestTokenStream : TokenStream
        {
            private ITermAttribute termAtt;
            private IOffsetAttribute offsetAtt;
            private ITypeAttribute typeAtt;
            private IPayloadAttribute payloadAtt;
            private IPositionIncrementAttribute posIncAtt;
            private IFlagsAttribute flagsAtt;

            internal TestTokenStream()
            {
                termAtt = AddAttribute<ITermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
                typeAtt = AddAttribute<ITypeAttribute>();
                payloadAtt = AddAttribute<IPayloadAttribute>();
                posIncAtt = AddAttribute<IPositionIncrementAttribute>();
                flagsAtt = AddAttribute<IFlagsAttribute>();
            }

            public override bool IncrementToken()
            {
                ClearAttributes();
                termAtt.SetTermBuffer("accents");
                offsetAtt.SetOffset(2, 7);
                typeAtt.Type = "wrd";
                posIncAtt.PositionIncrement = 3;
                payloadAtt.Payload = new Payload(new byte[] { 0, 1, 2, 3 });
                flagsAtt.Flags = 77;
                return true;
            }

            protected override void Dispose(bool disposing)
            {
                // do nothing
            }
        }
    }
}