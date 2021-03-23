// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.De
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

    /// <summary>
    /// Test the German stemmer. The stemming algorithm is known to work less 
    /// than perfect, as it doesn't use any word lists with exceptions. We 
    /// also check some of the cases where the algorithm is wrong.
    /// 
    /// </summary>
    public class TestGermanStemFilter : BaseTokenStreamTestCase
    {
        internal static readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer t = new KeywordTokenizer(reader);
            return new TokenStreamComponents(t, new GermanStemFilter(new LowerCaseFilter(TEST_VERSION_CURRENT, t)));
        });

        [Test]
        public virtual void TestStemming()
        {
            System.IO.Stream vocOut = this.GetType().getResourceAsStream("data.txt");
            VocabularyAssert.AssertVocabulary(analyzer, vocOut);
            vocOut.Dispose();
        }

        // LUCENE-3043: we use keywordtokenizer in this test,
        // so ensure the stemmer does not crash on zero-length strings.
        [Test]
        public virtual void TestEmpty()
        {
            AssertAnalyzesTo(analyzer, "", new string[] { "" });
        }

        [Test]
        public virtual void TestKeyword()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("sängerinnen"), false);
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
                return new TokenStreamComponents(source, new GermanStemFilter(sink));
            });
            CheckOneTerm(a, "sängerinnen", "sängerinnen");
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, analyzer, 1000 * RandomMultiplier);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new GermanStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}