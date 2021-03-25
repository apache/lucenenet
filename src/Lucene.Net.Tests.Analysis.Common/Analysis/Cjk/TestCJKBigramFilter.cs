// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Standard;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Cjk
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

    public class TestCJKBigramFilter : BaseTokenStreamTestCase
    {
        internal static readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer t = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
            return new TokenStreamComponents(t, new CJKBigramFilter(t));
        });

        internal static readonly Analyzer unibiAnalyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer t = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
            return new TokenStreamComponents(t, new CJKBigramFilter(t, (CJKScript)0xff, true));
        });

        [Test]
        public virtual void TestHuge()
        {
            AssertAnalyzesTo(analyzer, "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた", new string[] { "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた" });
        }

        [Test]
        public virtual void TestHanOnly()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer t = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(t, new CJKBigramFilter(t, CJKScript.HAN));
            });
            AssertAnalyzesTo(a, "多くの学生が試験に落ちた。", new string[] { "多", "く", "の", "学生", "が", "試験", "に", "落", "ち", "た" }, new int[] { 0, 1, 2, 3, 5, 6, 8, 9, 10, 11 }, new int[] { 1, 2, 3, 5, 6, 8, 9, 10, 11, 12 }, new string[] { "<SINGLE>", "<HIRAGANA>", "<HIRAGANA>", "<DOUBLE>", "<HIRAGANA>", "<DOUBLE>", "<HIRAGANA>", "<SINGLE>", "<HIRAGANA>", "<HIRAGANA>", "<SINGLE>" }, new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
        }

        [Test]
        public virtual void TestAllScripts()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer t = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(t, new CJKBigramFilter(t, (CJKScript)0xff, false));
            });
            AssertAnalyzesTo(a, "多くの学生が試験に落ちた。", new string[] { "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた" });
        }

        [Test]
        public virtual void TestUnigramsAndBigramsAllScripts()
        {
            AssertAnalyzesTo(unibiAnalyzer, "多くの学生が試験に落ちた。", new string[] { "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た" }, new int[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11 }, new int[] { 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12 }, new string[] { "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>" }, new int[] { 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 }, new int[] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1 });
        }

        [Test]
        public virtual void TestUnigramsAndBigramsHanOnly()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer t = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(t, new CJKBigramFilter(t, CJKScript.HAN, true));
            });
            AssertAnalyzesTo(a, "多くの学生が試験に落ちた。", new string[] { "多", "く", "の", "学", "学生", "生", "が", "試", "試験", "験", "に", "落", "ち", "た" }, new int[] { 0, 1, 2, 3, 3, 4, 5, 6, 6, 7, 8, 9, 10, 11 }, new int[] { 1, 2, 3, 4, 5, 5, 6, 7, 8, 8, 9, 10, 11, 12 }, new string[] { "<SINGLE>", "<HIRAGANA>", "<HIRAGANA>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<HIRAGANA>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<HIRAGANA>", "<SINGLE>", "<HIRAGANA>", "<HIRAGANA>", "<SINGLE>" }, new int[] { 1, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 1, 1 }, new int[] { 1, 1, 1, 1, 2, 1, 1, 1, 2, 1, 1, 1, 1, 1 });
        }

        [Test]
        public virtual void TestUnigramsAndBigramsHuge()
        {
            AssertAnalyzesTo(unibiAnalyzer, "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた", new string[] { "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomUnibiStrings()
        {
            CheckRandomData(Random, unibiAnalyzer, 1000 * RandomMultiplier);
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomUnibiHugeStrings()
        {
            Random random = Random;
            CheckRandomData(random, unibiAnalyzer, 100 * RandomMultiplier, 8192);
        }
    }
}