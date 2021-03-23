// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
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
    /// </summary>
    public class TestArabicNormalizationFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestAlifMadda()
        {
            Check("آجن", "اجن");
        }

        [Test]
        public virtual void TestAlifHamzaAbove()
        {
            Check("أحمد", "احمد");
        }

        [Test]
        public virtual void TestAlifHamzaBelow()
        {
            Check("إعاذ", "اعاذ");
        }

        [Test]
        public virtual void TestAlifMaksura()
        {
            Check("بنى", "بني");
        }

        [Test]
        public virtual void TestTehMarbuta()
        {
            Check("فاطمة", "فاطمه");
        }

        [Test]
        public virtual void TestTatweel()
        {
            Check("روبرـــــت", "روبرت");
        }

        [Test]
        public virtual void TestFatha()
        {
            Check("مَبنا", "مبنا");
        }

        [Test]
        public virtual void TestKasra()
        {
            Check("علِي", "علي");
        }

        [Test]
        public virtual void TestDamma()
        {
            Check("بُوات", "بوات");
        }

        [Test]
        public virtual void TestFathatan()
        {
            Check("ولداً", "ولدا");
        }

        [Test]
        public virtual void TestKasratan()
        {
            Check("ولدٍ", "ولد");
        }

        [Test]
        public virtual void TestDammatan()
        {
            Check("ولدٌ", "ولد");
        }

        [Test]
        public virtual void TestSukun()
        {
            Check("نلْسون", "نلسون");
        }

        [Test]
        public virtual void TestShaddah()
        {
            Check("هتميّ", "هتمي");
        }

        private void Check(string input, string expected)
        {
#pragma warning disable 612, 618
            ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
#pragma warning restore 612, 618
            ArabicNormalizationFilter filter = new ArabicNormalizationFilter(tokenStream);
            AssertTokenStreamContents(filter, new string[] { expected });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new ArabicNormalizationFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}