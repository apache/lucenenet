// Lucene version compatibility level 9.2
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Fa
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

    /// <summary>Test the Persian Normalization Filter</summary>
    public class TestPersianStemFilter : BaseTokenStreamTestCase
    {
        private Analyzer a;

        public override void SetUp()
        {
            base.SetUp();
            a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer source = new MockTokenizer(reader);
                return new TokenStreamComponents(source, new PersianStemFilter(source));
            });
        }

        [Test]
        public virtual void TestAnSuffix()
        {
            CheckOneTerm(a, "دوستان", "دوست");
        }

        [Test]
        public virtual void TestHaSuffix()
        {
            CheckOneTerm(a, "كتابها", "كتاب");
        }

        [Test]
        public virtual void TestAtSuffix()
        {
            CheckOneTerm(a, "جامدات", "جامد");
        }

        [Test]
        public virtual void TestYeeSuffix()
        {
            CheckOneTerm(a, "عليرضايي", "عليرضا");
        }

        [Test]
        public virtual void TestYeSuffix()
        {
            CheckOneTerm(a, "شادماني", "شادمان");
        }

        [Test]
        public virtual void TestTarSuffix()
        {
            CheckOneTerm(a, "باحالتر", "باحال");
        }

        [Test]
        public virtual void TestTarinSuffix()
        {
            CheckOneTerm(a, "خوبترين", "خوب");
        }

        [Test]
        public virtual void TestShouldntStem()
        {
            CheckOneTerm(a, "كباب", "كباب");
        }

        [Test]
        public virtual void TestNonArabic()
        {
            CheckOneTerm(a, "English", "english");
        }


        [Test]
        public virtual void TestWithKeywordAttribute()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.Add("ساهدهات");
            MockTokenizer tokenStream = new MockTokenizer(new StringReader("ساهدهات"));

            PersianStemFilter filter = new PersianStemFilter(new SetKeywordMarkerFilter(tokenStream, set));
            AssertTokenStreamContents(filter, new string[] { "ساهدهات" });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new PersianStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
            a.Dispose();
        }

    }
}
