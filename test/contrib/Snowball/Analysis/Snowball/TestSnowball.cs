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
using FlagsAttribute = Lucene.Net.Analysis.Tokenattributes.FlagsAttribute;
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

        /**
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
            TermAttribute termAtt = filter.GetAttribute<TermAttribute>();
            OffsetAttribute offsetAtt = filter.GetAttribute<OffsetAttribute>();
            TypeAttribute typeAtt = filter.GetAttribute<TypeAttribute>();
            PayloadAttribute payloadAtt = filter.GetAttribute<PayloadAttribute>();
            PositionIncrementAttribute posIncAtt = filter.GetAttribute<PositionIncrementAttribute>();
            FlagsAttribute flagsAtt = filter.GetAttribute<FlagsAttribute>();

            filter.IncrementToken();

            Assert.AreEqual("accent", termAtt.Term());
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
            var termAttr = tokenStream.AddAttribute<TermAttribute>();
            Assert.That(tokenStream.IncrementToken(), Is.True);
            Assert.That(termAttr.Term(), Is.EqualTo("terv"));
        }

        private sealed class TestTokenStream : TokenStream
        {
            private TermAttribute termAtt;
            private OffsetAttribute offsetAtt;
            private TypeAttribute typeAtt;
            private PayloadAttribute payloadAtt;
            private PositionIncrementAttribute posIncAtt;
            private FlagsAttribute flagsAtt;

            internal TestTokenStream()
            {
                termAtt = AddAttribute<TermAttribute>();
                offsetAtt = AddAttribute<OffsetAttribute>();
                typeAtt = AddAttribute<TypeAttribute>();
                payloadAtt = AddAttribute<PayloadAttribute>();
                posIncAtt = AddAttribute<PositionIncrementAttribute>();
                flagsAtt = AddAttribute<FlagsAttribute>();
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