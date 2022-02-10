// Lucene version compatibility level < 7.1.0
using ICU4N.Text;
using Lucene.Net.Analysis.Core;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Icu
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
    /// Test the <see cref="ICUTransformFilter"/> with some basic examples.
    /// </summary>
    public class TestICUTransformFilter : BaseTokenStreamTestCase
    {
        [Test]
        public void TestBasicFunctionality()
        {
            CheckToken(Transliterator.GetInstance("Traditional-Simplified"),
                "簡化字", "简化字");
            CheckToken(Transliterator.GetInstance("Katakana-Hiragana"),
                "ヒラガナ", "ひらがな");
            CheckToken(Transliterator.GetInstance("Fullwidth-Halfwidth"),
                "アルアノリウ", "ｱﾙｱﾉﾘｳ");
            CheckToken(Transliterator.GetInstance("Any-Latin"),
                "Αλφαβητικός Κατάλογος", "Alphabētikós Katálogos");
            CheckToken(Transliterator.GetInstance("NFD; [:Nonspacing Mark:] Remove"),
                "Alphabētikós Katálogos", "Alphabetikos Katalogos");
            CheckToken(Transliterator.GetInstance("Han-Latin"),
                "中国", "zhōng guó");
        }
        [Test]
        public void TestCustomFunctionality()
        {
            string rules = "a > b; b > c;"; // convert a's to b's and b's to c's
            CheckToken(Transliterator.CreateFromRules("test", rules, Transliterator.Forward), "abacadaba", "bcbcbdbcb");
        }
        [Test]
        public void TestCustomFunctionality2()
        {
            string rules = "c { a > b; a > d;"; // convert a's to b's and b's to c's
            CheckToken(Transliterator.CreateFromRules("test", rules, Transliterator.Forward), "caa", "cbd");
        }
        [Test]
        public void TestOptimizer()
        {
            string rules = "a > b; b > c;"; // convert a's to b's and b's to c's
            Transliterator custom = Transliterator.CreateFromRules("test", rules, Transliterator.Forward);
            assertTrue(custom.Filter is null);
            new ICUTransformFilter(new KeywordTokenizer(new StringReader("")), custom);
            assertTrue(custom.Filter.Equals(new UnicodeSet("[ab]")));
        }
        [Test]
        public void TestOptimizer2()
        {
            CheckToken(Transliterator.GetInstance("Traditional-Simplified; CaseFold"),
                "ABCDE", "abcde");
        }
        [Test]
        public void TestOptimizerSurrogate()
        {
            string rules = "\\U00020087 > x;"; // convert CJK UNIFIED IDEOGRAPH-20087 to an x
            Transliterator custom = Transliterator.CreateFromRules("test", rules, Transliterator.Forward);
            assertTrue(custom.Filter is null);
            new ICUTransformFilter(new KeywordTokenizer(new StringReader("")), custom);
            assertTrue(custom.Filter.Equals(new UnicodeSet("[\\U00020087]")));
        }

        private void CheckToken(Transliterator transform, string input, string expected)
        {
            TokenStream ts = new ICUTransformFilter(new KeywordTokenizer((new StringReader(input))), transform);
            AssertTokenStreamContents(ts, new string[] { expected });
        }

        /** blast some random strings through the analyzer */
        [Test]
        public void TestRandomStrings()
        {
            Transliterator transform = Transliterator.GetInstance("Any-Latin");
            using Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new ICUTransformFilter(tokenizer, transform));
            });
            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }

        [Test]
        public void TestEmptyTerm()
        {
            using Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new ICUTransformFilter(tokenizer, Transliterator.GetInstance("Any-Latin")));
            });
            CheckOneTerm(a, "", "");
        }
    }
}