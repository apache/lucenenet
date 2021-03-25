// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.CharFilters;
using Lucene.Net.Analysis.Compound.Hyphenation;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Compound
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

    public class TestCompoundWordTokenFilter : BaseTokenStreamTestCase
    {

        private static CharArraySet makeDictionary(params string[] dictionary)
        {
            return new CharArraySet(TEST_VERSION_CURRENT, dictionary, true);
        }

        [Test]
        public virtual void TestHyphenationCompoundWordsDA()
        {
            CharArraySet dict = makeDictionary("læse", "hest");

            //InputSource @is = new InputSource(this.GetType().getResource("da_UTF8.xml").toExternalForm());
            using var @is = this.GetType().getResourceAsStream("da_UTF8.xml");
            HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.GetHyphenationTree(@is);

            HyphenationCompoundWordTokenFilter tf = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("min veninde som er lidt af en læsehest"), MockTokenizer.WHITESPACE, false), hyphenator, dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, false);
            AssertTokenStreamContents(tf, new string[] { "min", "veninde", "som", "er", "lidt", "af", "en", "læsehest", "læse", "hest" }, new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 });
        }

        [Test]
        public virtual void TestHyphenationCompoundWordsDELongestMatch()
        {
            CharArraySet dict = makeDictionary("basketball", "basket", "ball", "kurv");

            //InputSource @is = new InputSource(this.GetType().getResource("da_UTF8.xml").toExternalForm());
            using var @is = this.GetType().getResourceAsStream("da_UTF8.xml");
            HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.GetHyphenationTree(@is);

            // the word basket will not be added due to the longest match option
            HyphenationCompoundWordTokenFilter tf = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("basketballkurv"), MockTokenizer.WHITESPACE, false), hyphenator, dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, 40, true);
            AssertTokenStreamContents(tf, new string[] { "basketballkurv", "basketball", "ball", "kurv" }, new int[] { 1, 0, 0, 0 });
        }

        /// <summary>
        /// With hyphenation-only, you can get a lot of nonsense tokens.
        /// This can be controlled with the min/max subword size.
        /// </summary>
        [Test]
        public virtual void TestHyphenationOnly()
        {
            //InputSource @is = new InputSource(this.GetType().getResource("da_UTF8.xml").toExternalForm());
            using var @is = this.GetType().getResourceAsStream("da_UTF8.xml");
            HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.GetHyphenationTree(@is);

            HyphenationCompoundWordTokenFilter tf = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("basketballkurv"), MockTokenizer.WHITESPACE, false), hyphenator, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, 2, 4);

            // min=2, max=4
            AssertTokenStreamContents(tf, new string[] { "basketballkurv", "ba", "sket", "bal", "ball", "kurv" });

            tf = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("basketballkurv"), MockTokenizer.WHITESPACE, false), hyphenator, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, 4, 6);

            // min=4, max=6
            AssertTokenStreamContents(tf, new string[] { "basketballkurv", "basket", "sket", "ball", "lkurv", "kurv" });

            tf = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("basketballkurv"), MockTokenizer.WHITESPACE, false), hyphenator, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, 4, 10);

            // min=4, max=10
            AssertTokenStreamContents(tf, new string[] { "basketballkurv", "basket", "basketbal", "basketball", "sket", "sketbal", "sketball", "ball", "ballkurv", "lkurv", "kurv" });
        }

        [Test]
        public virtual void TestDumbCompoundWordsSE()
        {
            CharArraySet dict = makeDictionary("Bil", "Dörr", "Motor", "Tak", "Borr", "Slag", "Hammar", "Pelar", "Glas", "Ögon", "Fodral", "Bas", "Fiol", "Makare", "Gesäll", "Sko", "Vind", "Rute", "Torkare", "Blad");

            DictionaryCompoundWordTokenFilter tf = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("Bildörr Bilmotor Biltak Slagborr Hammarborr Pelarborr Glasögonfodral Basfiolsfodral Basfiolsfodralmakaregesäll Skomakare Vindrutetorkare Vindrutetorkarblad abba"), MockTokenizer.WHITESPACE, false), dict);

            AssertTokenStreamContents(tf, new string[] { "Bildörr", "Bil", "dörr", "Bilmotor", "Bil", "motor", "Biltak", "Bil", "tak", "Slagborr", "Slag", "borr", "Hammarborr", "Hammar", "borr", "Pelarborr", "Pelar", "borr", "Glasögonfodral", "Glas", "ögon", "fodral", "Basfiolsfodral", "Bas", "fiol", "fodral", "Basfiolsfodralmakaregesäll", "Bas", "fiol", "fodral", "makare", "gesäll", "Skomakare", "Sko", "makare", "Vindrutetorkare", "Vind", "rute", "torkare", "Vindrutetorkarblad", "Vind", "rute", "blad", "abba" }, new int[] { 0, 0, 0, 8, 8, 8, 17, 17, 17, 24, 24, 24, 33, 33, 33, 44, 44, 44, 54, 54, 54, 54, 69, 69, 69, 69, 84, 84, 84, 84, 84, 84, 111, 111, 111, 121, 121, 121, 121, 137, 137, 137, 137, 156 }, new int[] { 7, 7, 7, 16, 16, 16, 23, 23, 23, 32, 32, 32, 43, 43, 43, 53, 53, 53, 68, 68, 68, 68, 83, 83, 83, 83, 110, 110, 110, 110, 110, 110, 120, 120, 120, 136, 136, 136, 136, 155, 155, 155, 155, 160 }, new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1 });
        }

        [Test]
        public virtual void TestDumbCompoundWordsSELongestMatch()
        {
            CharArraySet dict = makeDictionary("Bil", "Dörr", "Motor", "Tak", "Borr", "Slag", "Hammar", "Pelar", "Glas", "Ögon", "Fodral", "Bas", "Fiols", "Makare", "Gesäll", "Sko", "Vind", "Rute", "Torkare", "Blad", "Fiolsfodral");

            DictionaryCompoundWordTokenFilter tf = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("Basfiolsfodralmakaregesäll"), MockTokenizer.WHITESPACE, false), dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, true);

            AssertTokenStreamContents(tf, new string[] { "Basfiolsfodralmakaregesäll", "Bas", "fiolsfodral", "fodral", "makare", "gesäll" }, new int[] { 0, 0, 0, 0, 0, 0 }, new int[] { 26, 26, 26, 26, 26, 26 }, new int[] { 1, 0, 0, 0, 0, 0 });
        }

        [Test]
        public virtual void TestTokenEndingWithWordComponentOfMinimumLength()
        {
            CharArraySet dict = makeDictionary("ab", "cd", "ef");

            DictionaryCompoundWordTokenFilter tf = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("abcdef")
               ), dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, false);

            AssertTokenStreamContents(tf, new string[] { "abcdef", "ab", "cd", "ef" }, new int[] { 0, 0, 0, 0 }, new int[] { 6, 6, 6, 6 }, new int[] { 1, 0, 0, 0 });
        }

        [Test]
        public virtual void TestWordComponentWithLessThanMinimumLength()
        {
            CharArraySet dict = makeDictionary("abc", "d", "efg");

            DictionaryCompoundWordTokenFilter tf = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("abcdefg")
               ), dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, false);

            // since "d" is shorter than the minimum subword size, it should not be added to the token stream
            AssertTokenStreamContents(tf, new string[] { "abcdefg", "abc", "efg" }, new int[] { 0, 0, 0 }, new int[] { 7, 7, 7 }, new int[] { 1, 0, 0 });
        }

        [Test]
        public virtual void TestReset()
        {
            CharArraySet dict = makeDictionary("Rind", "Fleisch", "Draht", "Schere", "Gesetz", "Aufgabe", "Überwachung");

            Tokenizer wsTokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("Rindfleischüberwachungsgesetz"));
            DictionaryCompoundWordTokenFilter tf = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, wsTokenizer, dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, false);

            ICharTermAttribute termAtt = tf.GetAttribute<ICharTermAttribute>();
            tf.Reset();
            assertTrue(tf.IncrementToken());
            assertEquals("Rindfleischüberwachungsgesetz", termAtt.ToString());
            assertTrue(tf.IncrementToken());
            assertEquals("Rind", termAtt.ToString());
            tf.End();
            tf.Dispose();
            wsTokenizer.SetReader(new StringReader("Rindfleischüberwachungsgesetz"));
            tf.Reset();
            assertTrue(tf.IncrementToken());
            assertEquals("Rindfleischüberwachungsgesetz", termAtt.ToString());
        }

        [Test]
        public virtual void TestRetainMockAttribute()
        {
            CharArraySet dict = makeDictionary("abc", "d", "efg");
            Tokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("abcdefg"));
            TokenStream stream = new MockRetainAttributeFilter(tokenizer);
            stream = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, stream, dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, false);
            IMockRetainAttribute retAtt = stream.AddAttribute<IMockRetainAttribute>();
            stream.Reset();
            while (stream.IncrementToken())
            {
                assertTrue("Custom attribute value was lost", retAtt.Retain);
            }

        }

        public interface IMockRetainAttribute : IAttribute
        {
            bool Retain { set; get; }
        }

        public sealed class MockRetainAttribute : Attribute, IMockRetainAttribute
        {
            internal bool retain = false;
            public override void Clear()
            {
                retain = false;
            }
            public bool Retain
            {
                get => retain;
                set => this.retain = value;
            }
            public override void CopyTo(IAttribute target)
            {
                IMockRetainAttribute t = (IMockRetainAttribute)target;
                t.Retain = retain;
            }
        }

        private sealed class MockRetainAttributeFilter : TokenFilter
        {

            internal IMockRetainAttribute retainAtt;

            internal MockRetainAttributeFilter(TokenStream input)
                    : base(input)
            {
                retainAtt = AddAttribute<IMockRetainAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
                    retainAtt.Retain = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        // SOLR-2891
        // *CompoundWordTokenFilter blindly adds term length to offset, but this can take things out of bounds
        // wrt original text if a previous filter increases the length of the word (in this case ü -> ue)
        // so in this case we behave like WDF, and preserve any modified offsets
        [Test]
        public virtual void TestInvalidOffsets()
        {
            CharArraySet dict = makeDictionary("fall");
            NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
            builder.Add("ü", "ue");
            NormalizeCharMap normMap = builder.Build();

            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenFilter filter = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, tokenizer, dict);
                return new TokenStreamComponents(tokenizer, filter);
            }, initReader: (fieldName, reader) => new MappingCharFilter(normMap, reader));

            AssertAnalyzesTo(analyzer, "banküberfall", new string[] { "bankueberfall", "fall" }, new int[] { 0, 0 }, new int[] { 12, 12 });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CharArraySet dict = makeDictionary("a", "e", "i", "o", "u", "y", "bc", "def");
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, tokenizer, dict));
            });
            CheckRandomData(Random, a, 1000 * RandomMultiplier);

            //InputSource @is = new InputSource(this.GetType().getResource("da_UTF8.xml").toExternalForm());
            using var @is = this.GetType().getResourceAsStream("da_UTF8.xml");
            HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.GetHyphenationTree(@is);
            Analyzer b = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenFilter filter = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, tokenizer, hyphenator);
                return new TokenStreamComponents(tokenizer, filter);
            });
            CheckRandomData(Random, b, 1000 * RandomMultiplier);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            CharArraySet dict = makeDictionary("a", "e", "i", "o", "u", "y", "bc", "def");
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, tokenizer, dict));
            });
            CheckOneTerm(a, "", "");

            //InputSource @is = new InputSource(this.GetType().getResource("da_UTF8.xml").toExternalForm());
            using var @is = this.GetType().getResourceAsStream("da_UTF8.xml");
            HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.GetHyphenationTree(@is);
            Analyzer b = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                TokenFilter filter = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, tokenizer, hyphenator);
                return new TokenStreamComponents(tokenizer, filter);
            });
            CheckOneTerm(b, "", "");
        }
    }
}