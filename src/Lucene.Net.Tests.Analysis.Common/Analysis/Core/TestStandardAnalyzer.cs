// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Core
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

    public class TestStandardAnalyzer : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestHugeDoc()
        {
            StringBuilder sb = new StringBuilder();
            char[] whitespace = new char[4094];
            Arrays.Fill(whitespace, ' ');
            sb.Append(whitespace);
            sb.Append("testing 1234");
            string input = sb.ToString();
            StandardTokenizer tokenizer = new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
            BaseTokenStreamTestCase.AssertTokenStreamContents(tokenizer, new string[] { "testing", "1234" });
        }

        private static readonly Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
            return new TokenStreamComponents(tokenizer);
        });

        [Test]
        public virtual void TestArmenian()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "Վիքիպեդիայի 13 միլիոն հոդվածները (4,600` հայերեն վիքիպեդիայում) գրվել են կամավորների կողմից ու համարյա բոլոր հոդվածները կարող է խմբագրել ցանկաց մարդ ով կարող է բացել Վիքիպեդիայի կայքը։", new string[] { "Վիքիպեդիայի", "13", "միլիոն", "հոդվածները", "4,600", "հայերեն", "վիքիպեդիայում", "գրվել", "են", "կամավորների", "կողմից", "ու", "համարյա", "բոլոր", "հոդվածները", "կարող", "է", "խմբագրել", "ցանկաց", "մարդ", "ով", "կարող", "է", "բացել", "Վիքիպեդիայի", "կայքը" });
        }

        [Test]
        public virtual void TestAmharic()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "ዊኪፔድያ የባለ ብዙ ቋንቋ የተሟላ ትክክለኛና ነጻ መዝገበ ዕውቀት (ኢንሳይክሎፒዲያ) ነው። ማንኛውም", new string[] { "ዊኪፔድያ", "የባለ", "ብዙ", "ቋንቋ", "የተሟላ", "ትክክለኛና", "ነጻ", "መዝገበ", "ዕውቀት", "ኢንሳይክሎፒዲያ", "ነው", "ማንኛውም" });
        }

        [Test]
        public virtual void TestArabic()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "الفيلم الوثائقي الأول عن ويكيبيديا يسمى \"الحقيقة بالأرقام: قصة ويكيبيديا\" (بالإنجليزية: Truth in Numbers: The Wikipedia Story)، سيتم إطلاقه في 2008.", new string[] { "الفيلم", "الوثائقي", "الأول", "عن", "ويكيبيديا", "يسمى", "الحقيقة", "بالأرقام", "قصة", "ويكيبيديا", "بالإنجليزية", "Truth", "in", "Numbers", "The", "Wikipedia", "Story", "سيتم", "إطلاقه", "في", "2008" });
        }

        [Test]
        public virtual void TestAramaic()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "ܘܝܩܝܦܕܝܐ (ܐܢܓܠܝܐ: Wikipedia) ܗܘ ܐܝܢܣܩܠܘܦܕܝܐ ܚܐܪܬܐ ܕܐܢܛܪܢܛ ܒܠܫܢ̈ܐ ܣܓܝܐ̈ܐ܂ ܫܡܗ ܐܬܐ ܡܢ ܡ̈ܠܬܐ ܕ\"ܘܝܩܝ\" ܘ\"ܐܝܢܣܩܠܘܦܕܝܐ\"܀", new string[] { "ܘܝܩܝܦܕܝܐ", "ܐܢܓܠܝܐ", "Wikipedia", "ܗܘ", "ܐܝܢܣܩܠܘܦܕܝܐ", "ܚܐܪܬܐ", "ܕܐܢܛܪܢܛ", "ܒܠܫܢ̈ܐ", "ܣܓܝܐ̈ܐ", "ܫܡܗ", "ܐܬܐ", "ܡܢ", "ܡ̈ܠܬܐ", "ܕ", "ܘܝܩܝ", "ܘ", "ܐܝܢܣܩܠܘܦܕܝܐ" });
        }

        [Test]
        public virtual void TestBengali()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "এই বিশ্বকোষ পরিচালনা করে উইকিমিডিয়া ফাউন্ডেশন (একটি অলাভজনক সংস্থা)। উইকিপিডিয়ার শুরু ১৫ জানুয়ারি, ২০০১ সালে। এখন পর্যন্ত ২০০টিরও বেশী ভাষায় উইকিপিডিয়া রয়েছে।", new string[] { "এই", "বিশ্বকোষ", "পরিচালনা", "করে", "উইকিমিডিয়া", "ফাউন্ডেশন", "একটি", "অলাভজনক", "সংস্থা", "উইকিপিডিয়ার", "শুরু", "১৫", "জানুয়ারি", "২০০১", "সালে", "এখন", "পর্যন্ত", "২০০টিরও", "বেশী", "ভাষায়", "উইকিপিডিয়া", "রয়েছে" });
        }

        [Test]
        public virtual void TestFarsi()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "ویکی پدیای انگلیسی در تاریخ ۲۵ دی ۱۳۷۹ به صورت مکملی برای دانشنامهٔ تخصصی نوپدیا نوشته شد.", new string[] { "ویکی", "پدیای", "انگلیسی", "در", "تاریخ", "۲۵", "دی", "۱۳۷۹", "به", "صورت", "مکملی", "برای", "دانشنامهٔ", "تخصصی", "نوپدیا", "نوشته", "شد" });
        }

        [Test]
        public virtual void TestGreek()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "Γράφεται σε συνεργασία από εθελοντές με το λογισμικό wiki, κάτι που σημαίνει ότι άρθρα μπορεί να προστεθούν ή να αλλάξουν από τον καθένα.", new string[] { "Γράφεται", "σε", "συνεργασία", "από", "εθελοντές", "με", "το", "λογισμικό", "wiki", "κάτι", "που", "σημαίνει", "ότι", "άρθρα", "μπορεί", "να", "προστεθούν", "ή", "να", "αλλάξουν", "από", "τον", "καθένα" });
        }

        [Test]
        public virtual void TestThai()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "การที่ได้ต้องแสดงว่างานดี. แล้วเธอจะไปไหน? ๑๒๓๔", new string[] { "การที่ได้ต้องแสดงว่างานดี", "แล้วเธอจะไปไหน", "๑๒๓๔" });
        }

        [Test]
        public virtual void TestLao()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "ສາທາລະນະລັດ ປະຊາທິປະໄຕ ປະຊາຊົນລາວ", new string[] { "ສາທາລະນະລັດ", "ປະຊາທິປະໄຕ", "ປະຊາຊົນລາວ" });
        }

        [Test]
        public virtual void TestTibetan()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "སྣོན་མཛོད་དང་ལས་འདིས་བོད་ཡིག་མི་ཉམས་གོང་འཕེལ་དུ་གཏོང་བར་ཧ་ཅང་དགེ་མཚན་མཆིས་སོ། །", new string[] { "སྣོན", "མཛོད", "དང", "ལས", "འདིས", "བོད", "ཡིག", "མི", "ཉམས", "གོང", "འཕེལ", "དུ", "གཏོང", "བར", "ཧ", "ཅང", "དགེ", "མཚན", "མཆིས", "སོ" });
        }

        /*
         * For chinese, tokenize as char (these can later form bigrams or whatever)
         */
        [Test]
        public virtual void TestChinese()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "我是中国人。 １２３４ Ｔｅｓｔｓ ", new string[] { "我", "是", "中", "国", "人", "１２３４", "Ｔｅｓｔｓ" });
        }

        [Test]
        public virtual void TestEmpty()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "", new string[] { });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, ".", new string[] { });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, " ", new string[] { });
        }

        /* test various jira issues this analyzer is related to */

        [Test]
        public virtual void TestLUCENE1545()
        {
            /*
             * Standard analyzer does not correctly tokenize combining character U+0364 COMBINING LATIN SMALL LETTRE E.
             * The word "moͤchte" is incorrectly tokenized into "mo" "chte", the combining character is lost.
             * Expected result is only on token "moͤchte".
             */
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "moͤchte", new string[] { "moͤchte" });
        }

        /* Tests from StandardAnalyzer, just to show behavior is similar */
        [Test]
        public virtual void TestAlphanumericSA()
        {
            // alphanumeric tokens
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "B2B", new string[] { "B2B" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "2B", new string[] { "2B" });
        }

        [Test]
        public virtual void TestDelimitersSA()
        {
            // other delimiters: "-", "/", ","
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "some-dashed-phrase", new string[] { "some", "dashed", "phrase" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "dogs,chase,cats", new string[] { "dogs", "chase", "cats" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "ac/dc", new string[] { "ac", "dc" });
        }

        [Test]
        public virtual void TestApostrophesSA()
        {
            // internal apostrophes: O'Reilly, you're, O'Reilly's
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "O'Reilly", new string[] { "O'Reilly" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "you're", new string[] { "you're" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "she's", new string[] { "she's" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "Jim's", new string[] { "Jim's" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "don't", new string[] { "don't" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "O'Reilly's", new string[] { "O'Reilly's" });
        }

        [Test]
        public virtual void TestNumericSA()
        {
            // floating point, serial, model numbers, ip addresses, etc.
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "21.35", new string[] { "21.35" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "R2D2 C3PO", new string[] { "R2D2", "C3PO" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "216.239.63.104", new string[] { "216.239.63.104" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "216.239.63.104", new string[] { "216.239.63.104" });
        }

        [Test]
        public virtual void TestTextWithNumbersSA()
        {
            // numbers
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "David has 5000 bones", new string[] { "David", "has", "5000", "bones" });
        }

        [Test]
        public virtual void TestVariousTextSA()
        {
            // various
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "C embedded developers wanted", new string[] { "C", "embedded", "developers", "wanted" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "foo bar FOO BAR", new string[] { "foo", "bar", "FOO", "BAR" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new string[] { "foo", "bar", "FOO", "BAR" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "\"QUOTED\" word", new string[] { "QUOTED", "word" });
        }

        [Test]
        public virtual void TestKoreanSA()
        {
            // Korean words
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "안녕하세요 한글입니다", new string[] { "안녕하세요", "한글입니다" });
        }

        [Test]
        public virtual void TestOffsets()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "David has 5000 bones", new string[] { "David", "has", "5000", "bones" }, new int[] { 0, 6, 10, 15 }, new int[] { 5, 9, 14, 20 });
        }

        [Test]
        public virtual void TestTypes()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "David has 5000 bones", new string[] { "David", "has", "5000", "bones" }, new string[] { "<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<ALPHANUM>" });
        }

        [Test]
        public virtual void TestUnicodeWordBreaks()
        {
            WordBreakTestUnicode_6_3_0 wordBreakTest = new WordBreakTestUnicode_6_3_0();
            wordBreakTest.Test(a);
        }

        [Test]
        public virtual void TestSupplementary()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "𩬅艱鍟䇹愯瀛", new string[] { "𩬅", "艱", "鍟", "䇹", "愯", "瀛" }, new string[] { "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<IDEOGRAPHIC>" });
        }

        [Test]
        public virtual void TestKorean()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "훈민정음", new string[] { "훈민정음" }, new string[] { "<HANGUL>" });
        }

        [Test]
        public virtual void TestJapanese()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "仮名遣い カタカナ", new string[] { "仮", "名", "遣", "い", "カタカナ" }, new string[] { "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<HIRAGANA>", "<KATAKANA>" });
        }

        [Test]
        public virtual void TestCombiningMarks()
        {
            CheckOneTerm(a, "ざ", "ざ"); // hiragana
            CheckOneTerm(a, "ザ", "ザ"); // katakana
            CheckOneTerm(a, "壹゙", "壹゙"); // ideographic
            CheckOneTerm(a, "아゙", "아゙"); // hangul
        }

        /// <summary>
        /// Multiple consecutive chars in \p{WB:MidLetter}, \p{WB:MidNumLet},
        /// and/or \p{MidNum} should trigger a token split.
        /// </summary>
        [Test]
        public virtual void TestMid()
        {
            // ':' is in \p{WB:MidLetter}, which should trigger a split unless there is a Letter char on both sides
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "A:B", new string[] { "A:B" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "A::B", new string[] { "A", "B" });

            // '.' is in \p{WB:MidNumLet}, which should trigger a split unless there is a Letter or Numeric char on both sides
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "1.2", new string[] { "1.2" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "A.B", new string[] { "A.B" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "1..2", new string[] { "1", "2" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "A..B", new string[] { "A", "B" });

            // ',' is in \p{WB:MidNum}, which should trigger a split unless there is a Numeric char on both sides
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "1,2", new string[] { "1,2" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "1,,2", new string[] { "1", "2" });

            // Mixed consecutive \p{WB:MidLetter} and \p{WB:MidNumLet} should trigger a split
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "A.:B", new string[] { "A", "B" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "A:.B", new string[] { "A", "B" });

            // Mixed consecutive \p{WB:MidNum} and \p{WB:MidNumLet} should trigger a split
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "1,.2", new string[] { "1", "2" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "1.,2", new string[] { "1", "2" });

            // '_' is in \p{WB:ExtendNumLet}

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "A:B_A:B", new string[] { "A:B_A:B" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "A:B_A::B", new string[] { "A:B_A", "B" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "1.2_1.2", new string[] { "1.2_1.2" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "A.B_A.B", new string[] { "A.B_A.B" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "1.2_1..2", new string[] { "1.2_1", "2" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "A.B_A..B", new string[] { "A.B_A", "B" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "1,2_1,2", new string[] { "1,2_1,2" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "1,2_1,,2", new string[] { "1,2_1", "2" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "C_A.:B", new string[] { "C_A", "B" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "C_A:.B", new string[] { "C_A", "B" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "3_1,.2", new string[] { "3_1", "2" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "3_1.,2", new string[] { "3_1", "2" });
        }


        /// @deprecated remove this and sophisticated backwards layer in 5.0 
        [Test]
        [Obsolete("remove this and sophisticated backwards layer in 5.0")]
        public virtual void TestCombiningMarksBackwards()
        {
            Analyzer a = new StandardAnalyzer(LuceneVersion.LUCENE_33);
            CheckOneTerm(a, "ざ", "さ"); // hiragana Bug
            CheckOneTerm(a, "ザ", "ザ"); // katakana Works
            CheckOneTerm(a, "壹゙", "壹"); // ideographic Bug
            CheckOneTerm(a, "아゙", "아゙"); // hangul Works
        }

        /// @deprecated uses older unicode (6.0). simple test to make sure its basically working 
        [Test]
        [Obsolete("uses older unicode (6.0). simple test to make sure its basically working")]
        public virtual void TestVersion36()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
#pragma warning disable 612, 618
                Tokenizer tokenizer = new StandardTokenizer(LuceneVersion.LUCENE_36, reader);
#pragma warning restore 612, 618
                return new TokenStreamComponents(tokenizer);
            });
            AssertAnalyzesTo(a, "this is just a t\u08E6st lucene@apache.org", new string[] { "this", "is", "just", "a", "t", "st", "lucene", "apache.org" }); // new combining mark in 6.1
        }

        /// @deprecated uses older unicode (6.1). simple test to make sure its basically working 
        [Test]
        [Obsolete("uses older unicode (6.1). simple test to make sure its basically working")]
        public virtual void TestVersion40()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
#pragma warning disable 612, 618
                Tokenizer tokenizer = new StandardTokenizer(LuceneVersion.LUCENE_40, reader);
#pragma warning restore 612, 618
                return new TokenStreamComponents(tokenizer);
            });
            // U+061C is a new combining mark in 6.3, found using "[[\p{WB:Format}\p{WB:Extend}]&[^\p{Age:6.2}]]"
            // on the online UnicodeSet utility: <http://unicode.org/cldr/utility/list-unicodeset.jsp>
            AssertAnalyzesTo(a, "this is just a t\u061Cst lucene@apache.org", new string[] { "this", "is", "just", "a", "t", "st", "lucene", "apache.org" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new StandardAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }

        /// <summary>
        /// blast some random large strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomHugeStrings()
        {
            Random random = Random;
            CheckRandomData(random, new StandardAnalyzer(TEST_VERSION_CURRENT), 100 * RandomMultiplier, 8192);
        }

        // Adds random graph after:
        [Test]
        public virtual void TestRandomHugeStringsGraphAfter()
        {
            Random random = Random;
            CheckRandomData(random,
                Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
                    TokenStream tokenStream = new MockGraphTokenFilter(Random, tokenizer);
                    return new TokenStreamComponents(tokenizer, tokenStream);
                })
                , 100 * RandomMultiplier, 8192);
        }
    }
}