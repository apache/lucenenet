// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Attributes;
using Lucene.Net.Tartarus.Snowball.Ext;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Snowball
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */
#pragma warning disable 612, 618
    public class TestSnowball : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestEnglish()
        {
            Analyzer a = new SnowballAnalyzer(TEST_VERSION_CURRENT, "English");
            AssertAnalyzesTo(a, "he abhorred accents", new string[] { "he", "abhor", "accent" });
        }

        [Test]
        public virtual void TestStopwords()
        {
            Analyzer a = new SnowballAnalyzer(TEST_VERSION_CURRENT, "English", StandardAnalyzer.STOP_WORDS_SET);
            AssertAnalyzesTo(a, "the quick brown fox jumped", new string[] { "quick", "brown", "fox", "jump" });
        }

        /// <summary>
        /// Test english lowercasing. Test both cases (pre-3.1 and post-3.1) to ensure
        /// we lowercase I correct for non-Turkish languages in either case.
        /// </summary>
        [Test]
        public virtual void TestEnglishLowerCase()
        {
            Analyzer a = new SnowballAnalyzer(TEST_VERSION_CURRENT, "English");
            AssertAnalyzesTo(a, "cryogenic", new string[] { "cryogen" });
            AssertAnalyzesTo(a, "CRYOGENIC", new string[] { "cryogen" });

            Analyzer b = new SnowballAnalyzer(LuceneVersion.LUCENE_30, "English");
            AssertAnalyzesTo(b, "cryogenic", new string[] { "cryogen" });
            AssertAnalyzesTo(b, "CRYOGENIC", new string[] { "cryogen" });
        }

        /// <summary>
        /// Test turkish lowercasing
        /// </summary>
        [Test]
        public virtual void TestTurkish()
        {
            Analyzer a = new SnowballAnalyzer(TEST_VERSION_CURRENT, "Turkish");

            AssertAnalyzesTo(a, "ağacı", new string[] { "ağaç" });
            AssertAnalyzesTo(a, "AĞACI", new string[] { "ağaç" });
        }

        /// <summary>
        /// Test turkish lowercasing (old buggy behavior) </summary>
        /// @deprecated (3.1) Remove this when support for 3.0 indexes is no longer required (5.0) 
        [Test]
        [Obsolete("(3.1) Remove this when support for 3.0 indexes is no longer required (5.0)")]
        public virtual void TestTurkishBWComp()
        {
            Analyzer a = new SnowballAnalyzer(LuceneVersion.LUCENE_30, "Turkish");
            // AĞACI in turkish lowercases to ağacı, but with lowercase filter ağaci.
            // this fails due to wrong casing, because the stemmer
            // will only remove -ı, not -i
            AssertAnalyzesTo(a, "ağacı", new string[] { "ağaç" });
            AssertAnalyzesTo(a, "AĞACI", new string[] { "ağaci" });
        }

        // LUCENENET-544 - This would throw IndexOutOfRangeException in Lucene.Net 3.0.3
        [Test, LuceneNetSpecific]
        public virtual void TestLUCENENET_544()
        {
            TurkishStemmer ts = new TurkishStemmer();
            ts.SetCurrent("faydaland");
            ts.Stem();
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new SnowballAnalyzer(TEST_VERSION_CURRENT, "English");
            AssertAnalyzesTo(a, "he abhorred accents", new string[] { "he", "abhor", "accent" });
            AssertAnalyzesTo(a, "she abhorred him", new string[] { "she", "abhor", "him" });
        }

        [Test]
        public virtual void TestFilterTokens()
        {
            SnowballFilter filter = new SnowballFilter(new TestTokenStream(), "English");
            ICharTermAttribute termAtt = filter.GetAttribute<ICharTermAttribute>();
            IOffsetAttribute offsetAtt = filter.GetAttribute<IOffsetAttribute>();
            ITypeAttribute typeAtt = filter.GetAttribute<ITypeAttribute>();
            IPayloadAttribute payloadAtt = filter.GetAttribute<IPayloadAttribute>();
            IPositionIncrementAttribute posIncAtt = filter.GetAttribute<IPositionIncrementAttribute>();
            IFlagsAttribute flagsAtt = filter.GetAttribute<IFlagsAttribute>();

            filter.IncrementToken();

            assertEquals("accent", termAtt.ToString());
            assertEquals(2, offsetAtt.StartOffset);
            assertEquals(7, offsetAtt.EndOffset);
            assertEquals("wrd", typeAtt.Type);
            assertEquals(3, posIncAtt.PositionIncrement);
            assertEquals(77, flagsAtt.Flags);
            assertEquals(new BytesRef(new byte[] { 0, 1, 2, 3 }), payloadAtt.Payload);
        }

        private sealed class TestTokenStream : TokenStream
        {
            internal readonly ICharTermAttribute termAtt;
            internal readonly IOffsetAttribute offsetAtt;
            internal readonly ITypeAttribute typeAtt;
            internal readonly IPayloadAttribute payloadAtt;
            internal readonly IPositionIncrementAttribute posIncAtt;
            internal readonly IFlagsAttribute flagsAtt;

            internal TestTokenStream() : base()
            {
                this.termAtt = AddAttribute<ICharTermAttribute>();
                this.offsetAtt = AddAttribute<IOffsetAttribute>();
                this.typeAtt = AddAttribute<ITypeAttribute>();
                this.payloadAtt = AddAttribute<IPayloadAttribute>();
                this.posIncAtt = AddAttribute<IPositionIncrementAttribute>();
                this.flagsAtt = AddAttribute<IFlagsAttribute>();
            }

            public override bool IncrementToken()
            {
                ClearAttributes();
                termAtt.SetEmpty().Append("accents");
                offsetAtt.SetOffset(2, 7);
                typeAtt.Type = "wrd";
                posIncAtt.PositionIncrement = 3;
                payloadAtt.Payload = new BytesRef(new byte[] { 0, 1, 2, 3 });
                flagsAtt.Flags = 77;
                return true;
            }
        }

        /// <summary>
        /// for testing purposes ONLY </summary>
        public static string[] SNOWBALL_LANGS = new string[] { "Armenian", "Basque", "Catalan", "Danish", "Dutch", "English", "Finnish", "French", "German2", "German", "Hungarian", "Irish", "Italian", "Kp", "Lovins", "Norwegian", "Porter", "Portuguese", "Romanian", "Russian", "Spanish", "Swedish", "Turkish" };

        [Test]
        public virtual void TestEmptyTerm()
        {
            foreach (String lang in SNOWBALL_LANGS)
            {
                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new KeywordTokenizer(reader);
                    return new TokenStreamComponents(tokenizer, new SnowballFilter(tokenizer, lang));
                });
                CheckOneTerm(a, "", "");
            }
        }

        [Test]
        [Slow]
        public virtual void TestRandomStrings()
        {
            foreach (string lang in SNOWBALL_LANGS)
            {
                CheckRandomStrings(lang);
            }
        }

        public virtual void CheckRandomStrings(string snowballLanguage)
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer t = new MockTokenizer(reader);
                return new TokenStreamComponents(t, new SnowballFilter(t, snowballLanguage));
            });
            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }
    }
#pragma warning restore 612, 618
}