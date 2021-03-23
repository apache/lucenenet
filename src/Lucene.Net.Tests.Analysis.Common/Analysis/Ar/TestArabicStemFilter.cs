// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Ar
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
    /// Test the Arabic Normalization Filter
    /// 
    /// </summary>
    public class TestArabicStemFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestAlPrefix()
        {
            Check("الحسن", "حسن");
        }

        [Test]
        public virtual void TestWalPrefix()
        {
            Check("والحسن", "حسن");
        }

        [Test]
        public virtual void TestBalPrefix()
        {
            Check("بالحسن", "حسن");
        }

        [Test]
        public virtual void TestKalPrefix()
        {
            Check("كالحسن", "حسن");
        }

        [Test]
        public virtual void TestFalPrefix()
        {
            Check("فالحسن", "حسن");
        }

        [Test]
        public virtual void TestLlPrefix()
        {
            Check("للاخر", "اخر");
        }

        [Test]
        public virtual void TestWaPrefix()
        {
            Check("وحسن", "حسن");
        }

        [Test]
        public virtual void TestAhSuffix()
        {
            Check("زوجها", "زوج");
        }

        [Test]
        public virtual void TestAnSuffix()
        {
            Check("ساهدان", "ساهد");
        }

        [Test]
        public virtual void TestAtSuffix()
        {
            Check("ساهدات", "ساهد");
        }

        [Test]
        public virtual void TestWnSuffix()
        {
            Check("ساهدون", "ساهد");
        }

        [Test]
        public virtual void TestYnSuffix()
        {
            Check("ساهدين", "ساهد");
        }

        [Test]
        public virtual void TestYhSuffix()
        {
            Check("ساهديه", "ساهد");
        }

        [Test]
        public virtual void TestYpSuffix()
        {
            Check("ساهدية", "ساهد");
        }

        [Test]
        public virtual void TestHSuffix()
        {
            Check("ساهده", "ساهد");
        }

        [Test]
        public virtual void TestPSuffix()
        {
            Check("ساهدة", "ساهد");
        }

        [Test]
        public virtual void TestYSuffix()
        {
            Check("ساهدي", "ساهد");
        }

        [Test]
        public virtual void TestComboPrefSuf()
        {
            Check("وساهدون", "ساهد");
        }

        [Test]
        public virtual void TestComboSuf()
        {
            Check("ساهدهات", "ساهد");
        }

        [Test]
        public virtual void TestShouldntStem()
        {
            Check("الو", "الو");
        }

        [Test]
        public virtual void TestNonArabic()
        {
            Check("English", "English");
        }

        [Test]
        public virtual void TestWithKeywordAttribute()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("ساهدهات");
#pragma warning disable 612, 618
            ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(TEST_VERSION_CURRENT, new StringReader("ساهدهات"));
#pragma warning restore 612, 618

            ArabicStemFilter filter = new ArabicStemFilter(new SetKeywordMarkerFilter(tokenStream, set));
            AssertTokenStreamContents(filter, new string[] { "ساهدهات" });
        }

        private void Check(string input, string expected)
        {
#pragma warning disable 612, 618
            ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
#pragma warning restore 612, 618
            ArabicStemFilter filter = new ArabicStemFilter(tokenStream);
            AssertTokenStreamContents(filter, new string[] { expected });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new ArabicStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}