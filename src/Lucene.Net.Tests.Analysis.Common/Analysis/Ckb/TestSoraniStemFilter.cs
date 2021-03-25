// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Ckb
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
    /// Test the Sorani Stemmer.
    /// </summary>
    public class TestSoraniStemFilter : BaseTokenStreamTestCase
    {
        internal SoraniAnalyzer a = new SoraniAnalyzer(TEST_VERSION_CURRENT);

        [Test]
        public virtual void TestIndefiniteSingular()
        {
            CheckOneTerm(a, "پیاوێک", "پیاو"); // -ek
            CheckOneTerm(a, "دەرگایەک", "دەرگا"); // -yek
        }

        [Test]
        public virtual void TestDefiniteSingular()
        {
            CheckOneTerm(a, "پیاوەكە", "پیاو"); // -aka
            CheckOneTerm(a, "دەرگاكە", "دەرگا"); // -ka
        }

        [Test]
        public virtual void TestDemonstrativeSingular()
        {
            CheckOneTerm(a, "کتاویە", "کتاوی"); // -a
            CheckOneTerm(a, "دەرگایە", "دەرگا"); // -ya
        }

        [Test]
        public virtual void TestIndefinitePlural()
        {
            CheckOneTerm(a, "پیاوان", "پیاو"); // -An
            CheckOneTerm(a, "دەرگایان", "دەرگا"); // -yAn
        }

        [Test]
        public virtual void TestDefinitePlural()
        {
            CheckOneTerm(a, "پیاوەکان", "پیاو"); // -akAn
            CheckOneTerm(a, "دەرگاکان", "دەرگا"); // -kAn
        }

        [Test]
        public virtual void TestDemonstrativePlural()
        {
            CheckOneTerm(a, "پیاوانە", "پیاو"); // -Ana
            CheckOneTerm(a, "دەرگایانە", "دەرگا"); // -yAna
        }

        [Test]
        public virtual void TestEzafe()
        {
            CheckOneTerm(a, "هۆتیلی", "هۆتیل"); // singular
            CheckOneTerm(a, "هۆتیلێکی", "هۆتیل"); // indefinite
            CheckOneTerm(a, "هۆتیلانی", "هۆتیل"); // plural
        }

        [Test]
        public virtual void TestPostpositions()
        {
            CheckOneTerm(a, "دوورەوە", "دوور"); // -awa
            CheckOneTerm(a, "نیوەشەودا", "نیوەشەو"); // -dA
            CheckOneTerm(a, "سۆرانا", "سۆران"); // -A
        }

        [Test]
        public virtual void TestPossessives()
        {
            CheckOneTerm(a, "پارەمان", "پارە"); // -mAn
            CheckOneTerm(a, "پارەتان", "پارە"); // -tAn
            CheckOneTerm(a, "پارەیان", "پارە"); // -yAn
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new SoraniStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }

        /// <summary>
        /// test against a basic vocabulary file </summary>
        [Test]
        public virtual void TestVocabulary()
        {
            // top 8k words or so: freq > 1000
            VocabularyAssert.AssertVocabulary(a, GetDataFile("ckbtestdata.zip"), "testdata.txt");
        }
    }
}