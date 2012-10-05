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
    [NUnit.Framework.TestFixture]
    public class TestArabicStemFilter : BaseTokenStreamTestCase
    {
        [Test]
        public void TestAlPrefix()
        {
            Check("الحسن", "حسن");
        }

        [Test]
        public void TestWalPrefix()
        {
            Check("والحسن", "حسن");
        }

        [Test]
        public void TestBalPrefix()
        {
            Check("بالحسن", "حسن");
        }

        [Test]
        public void TestKalPrefix()
        {
            Check("كالحسن", "حسن");
        }

        [Test]
        public void TestFalPrefix()
        {
            Check("فالحسن", "حسن");
        }

        [Test]
        public void TestLlPrefix()
        {
            Check("للاخر", "اخر");
        }

        [Test]
        public void TestWaPrefix()
        {
            Check("وحسن", "حسن");
        }

        [Test]
        public void TestAhSuffix()
        {
            Check("زوجها", "زوج");
        }

        [Test]
        public void TestAnSuffix()
        {
            Check("ساهدان", "ساهد");
        }

        [Test]
        public void TestAtSuffix()
        {
            Check("ساهدات", "ساهد");
        }

        [Test]
        public void TestWnSuffix()
        {
            Check("ساهدون", "ساهد");
        }

        [Test]
        public void TestYnSuffix()
        {
            Check("ساهدين", "ساهد");
        }

        [Test]
        public void TestYhSuffix()
        {
            Check("ساهديه", "ساهد");
        }

        [Test]
        public void TestYpSuffix()
        {
            Check("ساهدية", "ساهد");
        }

        [Test]
        public void TestHSuffix()
        {
            Check("ساهده", "ساهد");
        }

        [Test]
        public void TestPSuffix()
        {
            Check("ساهدة", "ساهد");
        }

        [Test]
        public void TestYSuffix()
        {
            Check("ساهدي", "ساهد");
        }

        [Test]
        public void TestComboPrefSuf()
        {
            Check("وساهدون", "ساهد");
        }

        [Test]
        public void TestComboSuf()
        {
            Check("ساهدهات", "ساهد");
        }

        [Test]
        public void TestShouldntStem()
        {
            Check("الو", "الو");
        }

        [Test]
        public void TestNonArabic()
        {
            Check("English", "English");
        }

        private void Check(string input, string expected)
        {
            ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(new StringReader(input));
            ArabicStemFilter filter = new ArabicStemFilter(tokenStream);
            AssertTokenStreamContents(filter, new String[] { expected });
        }

    }
}