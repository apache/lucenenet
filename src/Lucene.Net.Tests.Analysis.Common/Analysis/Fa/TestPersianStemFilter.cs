// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
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

    /// <summary>
    /// Test the Arabic Normalization Filter
    /// 
    /// </summary>

    public class TestPersianStemFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestAnSuffix()
        {
            Check("دوستان", "دوست");
        }

        [Test]
        public virtual void TestHaSuffix()
        {
            Check("کتابها", "کتاب");
        }

        [Test]
        public virtual void TestAtSuffix()
        {
            Check("جامدات", "جامد");
        }

        [Test]
        public virtual void TestYeeSuffix()
        {
            Check("عليرضايي", "عليرضا");
        }

        [Test]
        public virtual void TestYeSuffix()
        {
            Check("شادماني", "شادمان");
        }

        [Test]
        public virtual void TestTarSuffix()
        {
            Check("باحالتر", "باحال");
        }

        [Test]
        public virtual void TestTarinSuffix()
        {
            Check("خوبترين", "خوب");
        }

        [Test]
        public virtual void TestShouldntStem()
        {
            Check("کباب", "کباب");
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
            StandardTokenizer tokenStream = new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader("ساهدهات"));
#pragma warning restore 612, 618

            PersianStemFilter filter = new PersianStemFilter(new SetKeywordMarkerFilter(tokenStream, set));
            AssertTokenStreamContents(filter, new string[] { "ساهدهات" });
        }

        private void Check(string input, string expected)
        {
#pragma warning disable 612, 618
            StandardTokenizer tokenStream = new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
#pragma warning restore 612, 618
            PersianStemFilter filter = new PersianStemFilter(tokenStream);
            AssertTokenStreamContents(filter, new string[] { expected });
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
        }

    }
}
