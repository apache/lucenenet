using System;
using NUnit.Framework;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Core;

namespace Lucene.Net.Analysis.Th
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
    /// Test case for ThaiAnalyzer, modified from TestFrenchAnalyzer
    /// 
    /// </summary>
    public class TestThaiAnalyzer : BaseTokenStreamTestCase
    {

        public override void SetUp()
        {
            base.SetUp();
            AssumeTrue("JRE does not support Thai dictionary-based BreakIterator", ThaiTokenizer.DBBI_AVAILABLE);
        }
        /*
         * testcase for offsets
         */
        [Test]
        public virtual void TestOffsets()
        {
            AssertAnalyzesTo(new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET), "การที่ได้ต้องแสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" }, new int[] { 0, 3, 6, 9, 13, 17, 20, 23 }, new int[] { 3, 6, 9, 13, 17, 20, 23, 25 });
        }

        [Test]
        public virtual void TestStopWords()
        {
            AssertAnalyzesTo(new ThaiAnalyzer(TEST_VERSION_CURRENT), "การที่ได้ต้องแสดงว่างานดี", new string[] { "แสดง", "งาน", "ดี" }, new int[] { 13, 20, 23 }, new int[] { 17, 23, 25 }, new int[] { 5, 2, 1 });
        }

        [Test]
        public virtual void TestBackwardsStopWords()
        {
            AssertAnalyzesTo(
#pragma warning disable 612, 618
                new ThaiAnalyzer(LuceneVersion.LUCENE_35),
#pragma warning restore 612, 618
                "การที่ได้ต้องแสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" }, new int[] { 0, 3, 6, 9, 13, 17, 20, 23 }, new int[] { 3, 6, 9, 13, 17, 20, 23, 25 });
        }

        /// <summary>
        /// Thai numeric tokens were typed as <ALPHANUM> instead of <NUM>. </summary>
        /// @deprecated (3.1) testing backwards behavior 
        [Test]
        [Obsolete("(3.1) testing backwards behavior")]
        public virtual void TestBuggyTokenType30()
        {
            AssertAnalyzesTo(new ThaiAnalyzer(LuceneVersion.LUCENE_30), "การที่ได้ต้องแสดงว่างานดี ๑๒๓", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี", "๑๒๓" }, new string[] { "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>" });
        }

        /// @deprecated (3.1) testing backwards behavior 
        [Test]
        [Obsolete("(3.1) testing backwards behavior")]
        public virtual void TestAnalyzer30()
        {
            ThaiAnalyzer analyzer = new ThaiAnalyzer(LuceneVersion.LUCENE_30);

            AssertAnalyzesTo(analyzer, "", new string[] { });

            AssertAnalyzesTo(analyzer, "การที่ได้ต้องแสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" });

            AssertAnalyzesTo(analyzer, "บริษัทชื่อ XY&Z - คุยกับ xyz@demo.com", new string[] { "บริษัท", "ชื่อ", "xy&z", "คุย", "กับ", "xyz@demo.com" });

            // English stop words
            AssertAnalyzesTo(analyzer, "ประโยคว่า The quick brown fox jumped over the lazy dogs", new string[] { "ประโยค", "ว่า", "quick", "brown", "fox", "jumped", "over", "lazy", "dogs" });
        }

        /*
         * Test that position increments are adjusted correctly for stopwords.
         */
        // note this test uses stopfilter's stopset
        [Test]
        public virtual void TestPositionIncrements()
        {
            ThaiAnalyzer analyzer = new ThaiAnalyzer(TEST_VERSION_CURRENT, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
            AssertAnalyzesTo(analyzer, "การที่ได้ต้อง the แสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" }, new int[] { 0, 3, 6, 9, 18, 22, 25, 28 }, new int[] { 3, 6, 9, 13, 22, 25, 28, 30 }, new int[] { 1, 1, 1, 1, 2, 1, 1, 1 });

            // case that a stopword is adjacent to thai text, with no whitespace
            AssertAnalyzesTo(analyzer, "การที่ได้ต้องthe แสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" }, new int[] { 0, 3, 6, 9, 17, 21, 24, 27 }, new int[] { 3, 6, 9, 13, 21, 24, 27, 29 }, new int[] { 1, 1, 1, 1, 2, 1, 1, 1 });
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            ThaiAnalyzer analyzer = new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET);
            AssertAnalyzesTo(analyzer, "", new string[] { });

            AssertAnalyzesTo(analyzer, "การที่ได้ต้องแสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" });

            AssertAnalyzesTo(analyzer, "บริษัทชื่อ XY&Z - คุยกับ xyz@demo.com", new string[] { "บริษัท", "ชื่อ", "xy", "z", "คุย", "กับ", "xyz", "demo.com" });
        }

        /// @deprecated (3.1) for version back compat 
        [Test]
        [Obsolete("(3.1) for version back compat")]
        public virtual void TestReusableTokenStream30()
        {
            ThaiAnalyzer analyzer = new ThaiAnalyzer(LuceneVersion.LUCENE_30);
            AssertAnalyzesTo(analyzer, "", new string[] { });

            AssertAnalyzesTo(analyzer, "การที่ได้ต้องแสดงว่างานดี", new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" });

            AssertAnalyzesTo(analyzer, "บริษัทชื่อ XY&Z - คุยกับ xyz@demo.com", new string[] { "บริษัท", "ชื่อ", "xy&z", "คุย", "กับ", "xyz@demo.com" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random(), new ThaiAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
        }

        /// <summary>
        /// blast some random large strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomHugeStrings()
        {
            Random random = Random();
            CheckRandomData(random, new ThaiAnalyzer(TEST_VERSION_CURRENT), 100 * RANDOM_MULTIPLIER, 8192);
        }

        // LUCENE-3044
        [Test]
        public virtual void TestAttributeReuse()
        {
#pragma warning disable 612, 618
            ThaiAnalyzer analyzer = new ThaiAnalyzer(LuceneVersion.LUCENE_30);
#pragma warning restore 612, 618
            // just consume
            TokenStream ts = analyzer.TokenStream("dummy", "ภาษาไทย");
            AssertTokenStreamContents(ts, new string[] { "ภาษา", "ไทย" });
            // this consumer adds flagsAtt, which this analyzer does not use. 
            ts = analyzer.TokenStream("dummy", "ภาษาไทย");
            ts.AddAttribute<IFlagsAttribute>();
            AssertTokenStreamContents(ts, new string[] { "ภาษา", "ไทย" });
        }

        [Test]
        public virtual void TestTwoSentences()
        {
            AssertAnalyzesTo(new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET), "This is a test. การที่ได้ต้องแสดงว่างานดี", new string[] { "this", "is", "a", "test", "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" }, new int[] { 0, 5, 8, 10, 16, 19, 22, 25, 29, 33, 36, 39 }, new int[] { 4, 7, 9, 14, 19, 22, 25, 29, 33, 36, 39, 41 });
        }

        /// <summary>
        /// LUCENENET: Tests scenario outlined in <see cref="ThaiWordBreaker"/>
        /// </summary>
        [Test]
        public virtual void TestNumeralBreakages()
        {
            ThaiAnalyzer analyzer = new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET);
            AssertAnalyzesTo(analyzer, "๑๒๓456", new string[] { "๑๒๓", "456" });
        }
    }
}