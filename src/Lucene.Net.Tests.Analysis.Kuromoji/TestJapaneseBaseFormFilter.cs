using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
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

    public class TestJapaneseBaseFormFilter : BaseTokenStreamTestCase
    {
        private Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new JapaneseTokenizer(reader, null, true, JapaneseTokenizer.DEFAULT_MODE);
            return new TokenStreamComponents(tokenizer, new JapaneseBaseFormFilter(tokenizer));
        });


        [Test]
        public void TestBasics()
        {
            AssertAnalyzesTo(analyzer, "それはまだ実験段階にあります",
            new String[] { "それ", "は", "まだ", "実験", "段階", "に", "ある", "ます" }
        );
        }

        [Test]
        public void TestKeyword()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("あり"), false);
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer source = new JapaneseTokenizer(reader, null, true, JapaneseTokenizer.DEFAULT_MODE);
                TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
                return new TokenStreamComponents(source, new JapaneseBaseFormFilter(sink));
            });

            AssertAnalyzesTo(a, "それはまだ実験段階にあります",
                new String[] { "それ", "は", "まだ", "実験", "段階", "に", "あり", "ます" }
            );
        }

        [Test]
        public void TestEnglish()
        {
            AssertAnalyzesTo(analyzer, "this atest",
                new String[] { "this", "atest" });
        }

        [Test]
        public void TestRandomStrings()
        {
            CheckRandomData(Random, analyzer, AtLeast(1000));
        }

        [Test]
        public void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new JapaneseBaseFormFilter(tokenizer));
            });

            CheckOneTerm(a, "", "");
        }
    }
}
