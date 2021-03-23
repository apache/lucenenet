// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
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

    public class TestUAX29URLEmailAnalyzer : BaseTokenStreamTestCase
    {

        private Analyzer a = new UAX29URLEmailAnalyzer(TEST_VERSION_CURRENT);

        [Test]
        public virtual void TestHugeDoc()
        {
            StringBuilder sb = new StringBuilder();
            char[] whitespace = new char[4094];
            Arrays.Fill(whitespace, ' ');
            sb.Append(whitespace);
            sb.Append("testing 1234");
            string input = sb.ToString();
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, input, new string[] { "testing", "1234" });
        }

        [Test]
        public virtual void TestArmenian()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "Վիքիպեդիայի 13 միլիոն հոդվածները (4,600` հայերեն վիքիպեդիայում) գրվել են կամավորների կողմից ու համարյա բոլոր հոդվածները կարող է խմբագրել ցանկաց մարդ ով կարող է բացել Վիքիպեդիայի կայքը։", new string[] { "վիքիպեդիայի", "13", "միլիոն", "հոդվածները", "4,600", "հայերեն", "վիքիպեդիայում", "գրվել", "են", "կամավորների", "կողմից", "ու", "համարյա", "բոլոր", "հոդվածները", "կարող", "է", "խմբագրել", "ցանկաց", "մարդ", "ով", "կարող", "է", "բացել", "վիքիպեդիայի", "կայքը" });
        }

        [Test]
        public virtual void TestAmharic()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "ዊኪፔድያ የባለ ብዙ ቋንቋ የተሟላ ትክክለኛና ነጻ መዝገበ ዕውቀት (ኢንሳይክሎፒዲያ) ነው። ማንኛውም", new string[] { "ዊኪፔድያ", "የባለ", "ብዙ", "ቋንቋ", "የተሟላ", "ትክክለኛና", "ነጻ", "መዝገበ", "ዕውቀት", "ኢንሳይክሎፒዲያ", "ነው", "ማንኛውም" });
        }

        [Test]
        public virtual void TestArabic()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "الفيلم الوثائقي الأول عن ويكيبيديا يسمى \"الحقيقة بالأرقام: قصة ويكيبيديا\" (بالإنجليزية: Truth in Numbers: The Wikipedia Story)، سيتم إطلاقه في 2008.", new string[] { "الفيلم", "الوثائقي", "الأول", "عن", "ويكيبيديا", "يسمى", "الحقيقة", "بالأرقام", "قصة", "ويكيبيديا", "بالإنجليزية", "truth", "numbers", "wikipedia", "story", "سيتم", "إطلاقه", "في", "2008" });
        }

        [Test]
        public virtual void TestAramaic()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "ܘܝܩܝܦܕܝܐ (ܐܢܓܠܝܐ: Wikipedia) ܗܘ ܐܝܢܣܩܠܘܦܕܝܐ ܚܐܪܬܐ ܕܐܢܛܪܢܛ ܒܠܫܢ̈ܐ ܣܓܝܐ̈ܐ܂ ܫܡܗ ܐܬܐ ܡܢ ܡ̈ܠܬܐ ܕ\"ܘܝܩܝ\" ܘ\"ܐܝܢܣܩܠܘܦܕܝܐ\"܀", new string[] { "ܘܝܩܝܦܕܝܐ", "ܐܢܓܠܝܐ", "wikipedia", "ܗܘ", "ܐܝܢܣܩܠܘܦܕܝܐ", "ܚܐܪܬܐ", "ܕܐܢܛܪܢܛ", "ܒܠܫܢ̈ܐ", "ܣܓܝܐ̈ܐ", "ܫܡܗ", "ܐܬܐ", "ܡܢ", "ܡ̈ܠܬܐ", "ܕ", "ܘܝܩܝ", "ܘ", "ܐܝܢܣܩܠܘܦܕܝܐ" });
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
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "Γράφεται σε συνεργασία από εθελοντές με το λογισμικό wiki, κάτι που σημαίνει ότι άρθρα μπορεί να προστεθούν ή να αλλάξουν από τον καθένα.", new string[] { "γράφεται", "σε", "συνεργασία", "από", "εθελοντές", "με", "το", "λογισμικό", "wiki", "κάτι", "που", "σημαίνει", "ότι", "άρθρα", "μπορεί", "να", "προστεθούν", "ή", "να", "αλλάξουν", "από", "τον", "καθένα" });
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
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "我是中国人。 １２３４ Ｔｅｓｔｓ ", new string[] { "我", "是", "中", "国", "人", "１２３４", "ｔｅｓｔｓ" });
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
             * Standard analyzer does not correctly tokenize combining character U+0364 COMBINING LATIN SMALL LETTER E.
             * The word "moͤchte" is incorrectly tokenized into "mo" "chte", the combining character is lost.
             * Expected result is only one token "moͤchte".
             */
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "moͤchte", new string[] { "moͤchte" });
        }

        /* Tests from StandardAnalyzer, just to show behavior is similar */
        [Test]
        public virtual void TestAlphanumericSA()
        {
            // alphanumeric tokens
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "B2B", new string[] { "b2b" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "2B", new string[] { "2b" });
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
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "O'Reilly", new string[] { "o'reilly" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "you're", new string[] { "you're" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "she's", new string[] { "she's" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "Jim's", new string[] { "jim's" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "don't", new string[] { "don't" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "O'Reilly's", new string[] { "o'reilly's" });
        }

        [Test]
        public virtual void TestNumericSA()
        {
            // floating point, serial, model numbers, ip addresses, etc.
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "21.35", new string[] { "21.35" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "R2D2 C3PO", new string[] { "r2d2", "c3po" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "216.239.63.104", new string[] { "216.239.63.104" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "216.239.63.104", new string[] { "216.239.63.104" });
        }

        [Test]
        public virtual void TestTextWithNumbersSA()
        {
            // numbers
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "David has 5000 bones", new string[] { "david", "has", "5000", "bones" });
        }

        [Test]
        public virtual void TestVariousTextSA()
        {
            // various
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "C embedded developers wanted", new string[] { "c", "embedded", "developers", "wanted" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "foo bar FOO BAR", new string[] { "foo", "bar", "foo", "bar" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new string[] { "foo", "bar", "foo", "bar" });
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "\"QUOTED\" word", new string[] { "quoted", "word" });
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
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "David has 5000 bones", new string[] { "david", "has", "5000", "bones" }, new int[] { 0, 6, 10, 15 }, new int[] { 5, 9, 14, 20 });
        }

        [Test]
        public virtual void TestTypes()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "david has 5000 bones", new string[] { "david", "has", "5000", "bones" }, new string[] { "<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<ALPHANUM>" });
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

        /// @deprecated remove this and sophisticated backwards layer in 5.0 
        [Test]
        [Obsolete("remove this and sophisticated backwards layer in 5.0")]
        public virtual void TestCombiningMarksBackwards()
        {
            Analyzer a = new UAX29URLEmailAnalyzer(LuceneVersion.LUCENE_33);
            CheckOneTerm(a, "ざ", "さ"); // hiragana Bug
            CheckOneTerm(a, "ザ", "ザ"); // katakana Works
            CheckOneTerm(a, "壹゙", "壹"); // ideographic Bug
            CheckOneTerm(a, "아゙", "아゙"); // hangul Works
        }

        [Test]
        public virtual void TestBasicEmails()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "one test@example.com two three [A@example.CO.UK] \"ArakaBanassaMassanaBakarA\" <info@Info.info>", new string[] { "one", "test@example.com", "two", "three", "a@example.co.uk", "arakabanassamassanabakara", "info@info.info" }, new string[] { "<ALPHANUM>", "<EMAIL>", "<ALPHANUM>", "<ALPHANUM>", "<EMAIL>", "<ALPHANUM>", "<EMAIL>" });
        }

        [Test]
        public virtual void TestMailtoSchemeEmails()
        {
            // See LUCENE-3880
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "MAILTO:Test@Example.ORG", new string[] { "mailto", "test@example.org" }, new string[] { "<ALPHANUM>", "<EMAIL>" });

            // TODO: Support full mailto: scheme URIs. See RFC 6068: http://tools.ietf.org/html/rfc6068
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "mailto:personA@example.com,personB@example.com?cc=personC@example.com" + "&subject=Subjectivity&body=Corpusivity%20or%20something%20like%20that", new string[] { "mailto", "persona@example.com", ",personb@example.com", "?cc=personc@example.com", "subject", "subjectivity", "body", "corpusivity", "20or", "20something", "20like", "20that" }, new string[] { "<ALPHANUM>", "<EMAIL>", "<EMAIL>", "<EMAIL>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>" }); // TODO: Hex decoding + re-tokenization -  TODO: split field keys/values
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              // TODO: recognize ',' address delimiter. Also, see examples of ';' delimiter use at: http://www.mailto.co.uk/
        }

        [Test]
        public virtual void TestBasicURLs()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "a <HTTPs://example.net/omg/isnt/that/NICE?no=its&n%30t#mntl-E>b-D ftp://www.example.com/ABC.txt file:///C:/path/to/a/FILE.txt C", new string[] { "https://example.net/omg/isnt/that/nice?no=its&n%30t#mntl-e", "b", "d", "ftp://www.example.com/abc.txt", "file:///c:/path/to/a/file.txt", "c" }, new string[] { "<URL>", "<ALPHANUM>", "<ALPHANUM>", "<URL>", "<URL>", "<ALPHANUM>" });
        }

        [Test]
        public virtual void TestNoSchemeURLs()
        {
            // ".ph" is a Top Level Domain
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "<index.ph>", new string[] { "index.ph" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "index.ph", new string[] { "index.ph" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "index.php", new string[] { "index.php" }, new string[] { "<ALPHANUM>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "index.phα", new string[] { "index.phα" }, new string[] { "<ALPHANUM>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "index-h.php", new string[] { "index", "h.php" }, new string[] { "<ALPHANUM>", "<ALPHANUM>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "index2.php", new string[] { "index2", "php" }, new string[] { "<ALPHANUM>", "<ALPHANUM>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "index2.ph９,", new string[] { "index2", "ph９" }, new string[] { "<ALPHANUM>", "<ALPHANUM>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com,example.ph,index.php,index2.php,example2.ph", new string[] { "example.com", "example.ph", "index.php", "index2", "php", "example2.ph" }, new string[] { "<URL>", "<URL>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com:8080 example.com/path/here example.com?query=something example.com#fragment", new string[] { "example.com:8080", "example.com/path/here", "example.com?query=something", "example.com#fragment" }, new string[] { "<URL>", "<URL>", "<URL>", "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com:8080/path/here?query=something#fragment", new string[] { "example.com:8080/path/here?query=something#fragment" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com:8080/path/here?query=something", new string[] { "example.com:8080/path/here?query=something" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com:8080/path/here#fragment", new string[] { "example.com:8080/path/here#fragment" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com:8080/path/here", new string[] { "example.com:8080/path/here" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com:8080?query=something#fragment", new string[] { "example.com:8080?query=something#fragment" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com:8080?query=something", new string[] { "example.com:8080?query=something" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com:8080#fragment", new string[] { "example.com:8080#fragment" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com/path/here?query=something#fragment", new string[] { "example.com/path/here?query=something#fragment" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com/path/here?query=something", new string[] { "example.com/path/here?query=something" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com/path/here#fragment", new string[] { "example.com/path/here#fragment" }, new string[] { "<URL>" });

            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "example.com?query=something#fragment", new string[] { "example.com?query=something#fragment" }, new string[] { "<URL>" });
        }


        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new UAX29URLEmailAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}