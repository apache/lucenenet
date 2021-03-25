// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.En
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
    /// Test the PorterStemFilter with Martin Porter's test data.
    /// </summary>
    public class TestPorterStemFilter_ : BaseTokenStreamTestCase
    {
        internal static readonly Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer t = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
            return new TokenStreamComponents(t, new PorterStemFilter(t));
        });

        /// <summary>
        /// Run the stemmer against all strings in voc.txt
        /// The output should be the same as the string in output.txt
        /// </summary>
        [Test]
        public virtual void TestPorterStemFilter()
        {
            VocabularyAssert.AssertVocabulary(a, GetDataFile("porterTestData.zip"), "voc.txt", "output.txt");
        }

        [Test]
        public virtual void TestWithKeywordAttribute()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("yourselves");
            Tokenizer tokenizer = new MockTokenizer(new StringReader("yourselves yours"), MockTokenizer.WHITESPACE, false);
            TokenStream filter = new PorterStemFilter(new SetKeywordMarkerFilter(tokenizer, set));
            AssertTokenStreamContents(filter, new string[] { "yourselves", "your" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new PorterStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}