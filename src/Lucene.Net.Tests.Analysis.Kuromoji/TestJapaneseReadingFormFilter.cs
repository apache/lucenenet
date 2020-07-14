using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Analysis.Core;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Ja
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
    /// Tests for <see cref="TestJapaneseReadingFormFilter"/>
    /// </summary>
    public class TestJapaneseReadingFormFilter : BaseTokenStreamTestCase
    {
        private Analyzer katakanaAnalyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new JapaneseTokenizer(reader, null, true, JapaneseTokenizerMode.SEARCH);
            return new TokenStreamComponents(tokenizer, new JapaneseReadingFormFilter(tokenizer, false));
        });

        private Analyzer romajiAnalyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new JapaneseTokenizer(reader, null, true, JapaneseTokenizerMode.SEARCH);
            return new TokenStreamComponents(tokenizer, new JapaneseReadingFormFilter(tokenizer, true));
        });


        [Test]
        public void TestKatakanaReadings()
        {
            AssertAnalyzesTo(katakanaAnalyzer, "今夜はロバート先生と話した",
                new String[] { "コンヤ", "ハ", "ロバート", "センセイ", "ト", "ハナシ", "タ" }
            );
        }

        [Test]
        public void TestKatakanaReadingsHalfWidth()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new JapaneseTokenizer(reader, null, true, JapaneseTokenizerMode.SEARCH);
                TokenStream stream = new CJKWidthFilter(tokenizer);
                return new TokenStreamComponents(tokenizer, new JapaneseReadingFormFilter(stream, false));
            });

            AssertAnalyzesTo(a, "今夜はﾛﾊﾞｰﾄ先生と話した",
                new String[] { "コンヤ", "ハ", "ロバート", "センセイ", "ト", "ハナシ", "タ" }
            );
        }

        [Test]
        public void TestRomajiReadings()
        {
            AssertAnalyzesTo(romajiAnalyzer, "今夜はロバート先生と話した",
                new String[] { "kon'ya", "ha", "robato", "sensei", "to", "hanashi", "ta" }
            );
        }

        [Test]
        public void TestRomajiReadingsHalfWidth()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new JapaneseTokenizer(reader, null, true, JapaneseTokenizerMode.SEARCH);
                TokenStream stream = new CJKWidthFilter(tokenizer);
                return new TokenStreamComponents(tokenizer, new JapaneseReadingFormFilter(stream, true));
            });

            AssertAnalyzesTo(a, "今夜はﾛﾊﾞｰﾄ先生と話した",
                new String[] { "kon'ya", "ha", "robato", "sensei", "to", "hanashi", "ta" }
            );
        }

        [Test]
        public void TestRandomData()
        {
            Random random = Random;
            CheckRandomData(random, katakanaAnalyzer, 1000 * RandomMultiplier);
            CheckRandomData(random, romajiAnalyzer, 1000 * RandomMultiplier);
        }

        [Test]
        public void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new JapaneseReadingFormFilter(tokenizer));
            });

            CheckOneTerm(a, "", "");
        }
    }
}
