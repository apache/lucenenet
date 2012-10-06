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

using System;
using System.IO;
using Lucene.Net.Analysis.AR;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.AR
{


    /*
     * Test the Arabic Normalization Filter
     *
     */
    [TestFixture]
    public class TestArabicNormalizationFilter : BaseTokenStreamTestCase
    {

        [Test]
        public void TestAlifMadda()
        {
            Check("آجن", "اجن");
        }

        [Test]
        public void TestAlifHamzaAbove()
        {
            Check("أحمد", "احمد");
        }

        [Test]
        public void TestAlifHamzaBelow()
        {
            Check("إعاذ", "اعاذ");
        }

        [Test]
        public void TestAlifMaksura()
        {
            Check("بنى", "بني");
        }

        [Test]
        public void TestTehMarbuta()
        {
            Check("فاطمة", "فاطمه");
        }

        [Test]
        public void TestTatweel()
        {
            Check("روبرـــــت", "روبرت");
        }

        [Test]
        public void TestFatha()
        {
            Check("مَبنا", "مبنا");
        }

        [Test]
        public void TestKasra()
        {
            Check("علِي", "علي");
        }

        [Test]
        public void TestDamma()
        {
            Check("بُوات", "بوات");
        }

        [Test]
        public void TestFathatan()
        {
            Check("ولداً", "ولدا");
        }

        [Test]
        public void TestKasratan()
        {
            Check("ولدٍ", "ولد");
        }

        [Test]
        public void TestDammatan()
        {
            Check("ولدٌ", "ولد");
        }

        [Test]
        public void TestSukun()
        {
            Check("نلْسون", "نلسون");
        }

        [Test]
        public void TestShaddah()
        {
            Check("هتميّ", "هتمي");
        }

        private void Check(string input, string expected)
        {
            ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(new StringReader(input));
            ArabicNormalizationFilter filter = new ArabicNormalizationFilter(tokenStream);
            AssertTokenStreamContents(filter, new String[] { expected });
        }

    }
}