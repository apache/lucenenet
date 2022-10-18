// Lucene version compatibility level < 7.1.0
using ICU4N.Globalization;
using J2N.Threading;
using Lucene.Net.Analysis.Icu.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Lucene.Net.Analysis.Icu.Segmentation
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

    public class TestICUTokenizer : BaseTokenStreamTestCase
    {
        [Test]
        public void TestHugeDoc()
        {
            StringBuilder sb = new StringBuilder();
            char[] whitespace = new char[4094];
            Arrays.Fill(whitespace, ' ');
            sb.append(whitespace);
            sb.append("testing 1234");
            string input = sb.toString();
            ICUTokenizer tokenizer = new ICUTokenizer(new StringReader(input), new DefaultICUTokenizerConfig(false, true));
            AssertTokenStreamContents(tokenizer, new string[] { "testing", "1234" });
        }

        [Test]
        public void TestHugeTerm2()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 40960; i++)
            {
                sb.append('a');
            }
            string input = sb.toString();
            ICUTokenizer tokenizer = new ICUTokenizer(new StringReader(input), new DefaultICUTokenizerConfig(false, true));
            char[] token = new char[4096];
            Arrays.Fill(token, 'a');
            string expectedToken = new string(token);
            string[] expected = {
                expectedToken, expectedToken, expectedToken,
                expectedToken, expectedToken, expectedToken,
                expectedToken, expectedToken, expectedToken,
                expectedToken
            };
            AssertTokenStreamContents(tokenizer, expected);
        }

        private Analyzer a;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new ICUTokenizer(reader, new DefaultICUTokenizerConfig(false, true));
                TokenFilter filter = new ICUNormalizer2Filter(tokenizer);
                return new TokenStreamComponents(tokenizer, filter);
            });
        }

        [TearDown]
        public override void TearDown()
        {
            if (a != null) a.Dispose();
            base.TearDown();
        }

        [Test]
        public void TestArmenian()
        {
            AssertAnalyzesTo(a, "Վիքիպեդիայի 13 միլիոն հոդվածները (4,600` հայերեն վիքիպեդիայում) գրվել են կամավորների կողմից ու համարյա բոլոր հոդվածները կարող է խմբագրել ցանկաց մարդ ով կարող է բացել Վիքիպեդիայի կայքը։",
                new string[] { "վիքիպեդիայի", "13", "միլիոն", "հոդվածները", "4,600", "հայերեն", "վիքիպեդիայում", "գրվել", "են", "կամավորների", "կողմից",
                "ու", "համարյա", "բոլոր", "հոդվածները", "կարող", "է", "խմբագրել", "ցանկաց", "մարդ", "ով", "կարող", "է", "բացել", "վիքիպեդիայի", "կայքը" });
        }

        [Test]
        public void TestAmharic()
        {
            AssertAnalyzesTo(a, "ዊኪፔድያ የባለ ብዙ ቋንቋ የተሟላ ትክክለኛና ነጻ መዝገበ ዕውቀት (ኢንሳይክሎፒዲያ) ነው። ማንኛውም",
        new string[] { "ዊኪፔድያ", "የባለ", "ብዙ", "ቋንቋ", "የተሟላ", "ትክክለኛና", "ነጻ", "መዝገበ", "ዕውቀት", "ኢንሳይክሎፒዲያ", "ነው", "ማንኛውም" });
        }

        [Test]
        public void TestArabic()
        {
            AssertAnalyzesTo(a, "الفيلم الوثائقي الأول عن ويكيبيديا يسمى \"الحقيقة بالأرقام: قصة ويكيبيديا\" (بالإنجليزية: Truth in Numbers: The Wikipedia Story)، سيتم إطلاقه في 2008.",
                new string[] { "الفيلم", "الوثائقي", "الأول", "عن", "ويكيبيديا", "يسمى", "الحقيقة", "بالأرقام", "قصة", "ويكيبيديا",
                "بالإنجليزية", "truth", "in", "numbers", "the", "wikipedia", "story", "سيتم", "إطلاقه", "في", "2008" });
        }

        [Test]
        public void TestAramaic()
        {
            AssertAnalyzesTo(a, "ܘܝܩܝܦܕܝܐ (ܐܢܓܠܝܐ: Wikipedia) ܗܘ ܐܝܢܣܩܠܘܦܕܝܐ ܚܐܪܬܐ ܕܐܢܛܪܢܛ ܒܠܫܢ̈ܐ ܣܓܝܐ̈ܐ܂ ܫܡܗ ܐܬܐ ܡܢ ܡ̈ܠܬܐ ܕ\"ܘܝܩܝ\" ܘ\"ܐܝܢܣܩܠܘܦܕܝܐ\"܀",
                new string[] { "ܘܝܩܝܦܕܝܐ", "ܐܢܓܠܝܐ", "wikipedia", "ܗܘ", "ܐܝܢܣܩܠܘܦܕܝܐ", "ܚܐܪܬܐ", "ܕܐܢܛܪܢܛ", "ܒܠܫܢ̈ܐ", "ܣܓܝܐ̈ܐ", "ܫܡܗ",
                "ܐܬܐ", "ܡܢ", "ܡ̈ܠܬܐ", "ܕ", "ܘܝܩܝ", "ܘ", "ܐܝܢܣܩܠܘܦܕܝܐ"});
        }

        [Test]
        public void TestBengali()
        {
            AssertAnalyzesTo(a, "এই বিশ্বকোষ পরিচালনা করে উইকিমিডিয়া ফাউন্ডেশন (একটি অলাভজনক সংস্থা)। উইকিপিডিয়ার শুরু ১৫ জানুয়ারি, ২০০১ সালে। এখন পর্যন্ত ২০০টিরও বেশী ভাষায় উইকিপিডিয়া রয়েছে।",
                new string[] { "এই", "বিশ্বকোষ", "পরিচালনা", "করে", "উইকিমিডিয়া", "ফাউন্ডেশন", "একটি", "অলাভজনক", "সংস্থা", "উইকিপিডিয়ার",
                "শুরু", "১৫", "জানুয়ারি", "২০০১", "সালে", "এখন", "পর্যন্ত", "২০০টিরও", "বেশী", "ভাষায়", "উইকিপিডিয়া", "রয়েছে" });
        }

        [Test]
        public void TestFarsi()
        {
            AssertAnalyzesTo(a, "ویکی پدیای انگلیسی در تاریخ ۲۵ دی ۱۳۷۹ به صورت مکملی برای دانشنامهٔ تخصصی نوپدیا نوشته شد.",
                new string[] { "ویکی", "پدیای", "انگلیسی", "در", "تاریخ", "۲۵", "دی", "۱۳۷۹", "به", "صورت", "مکملی",
                "برای", "دانشنامهٔ", "تخصصی", "نوپدیا", "نوشته", "شد" });
        }

        [Test]
        public void TestGreek()
        {
            AssertAnalyzesTo(a, "Γράφεται σε συνεργασία από εθελοντές με το λογισμικό wiki, κάτι που σημαίνει ότι άρθρα μπορεί να προστεθούν ή να αλλάξουν από τον καθένα.",
                new string[] { "γράφεται", "σε", "συνεργασία", "από", "εθελοντέσ", "με", "το", "λογισμικό", "wiki", "κάτι", "που",
                "σημαίνει", "ότι", "άρθρα", "μπορεί", "να", "προστεθούν", "ή", "να", "αλλάξουν", "από", "τον", "καθένα" });
        }

        [Test]
        public void TestKhmer()
        {
            AssertAnalyzesTo(a, "ផ្ទះស្កឹមស្កៃបីបួនខ្នងនេះ", new String[] { "ផ្ទះ", "ស្កឹមស្កៃ", "បី", "បួន", "ខ្នង", "នេះ" });
        }

        [Test]
        public void TestLao()
        {
            AssertAnalyzesTo(a, "ກວ່າດອກ", new string[] { "ກວ່າ", "ດອກ" });
            AssertAnalyzesTo(a, "ພາສາລາວ", new string[] { "ພາສາ", "ລາວ" }, new string[] { "<ALPHANUM>", "<ALPHANUM>" });
        }

        [Test]
        public void TestMyanmar() 
        {
            AssertAnalyzesTo(a, "သက်ဝင်လှုပ်ရှားစေပြီး", new String[] { "သက်ဝင်", "လှုပ်ရှား", "စေ", "ပြီး" });
        }

        [Test]
        public void TestThai()
        {
            AssertAnalyzesTo(a, "การที่ได้ต้องแสดงว่างานดี. แล้วเธอจะไปไหน? ๑๒๓๔",
                new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี", "แล้ว", "เธอ", "จะ", "ไป", "ไหน", "๑๒๓๔" });
        }

        [Test]
        public void TestTibetan()
        {
            AssertAnalyzesTo(a, "སྣོན་མཛོད་དང་ལས་འདིས་བོད་ཡིག་མི་ཉམས་གོང་འཕེལ་དུ་གཏོང་བར་ཧ་ཅང་དགེ་མཚན་མཆིས་སོ། །",
                new string[] { "སྣོན", "མཛོད", "དང", "ལས", "འདིས", "བོད", "ཡིག", "མི", "ཉམས", "གོང", "འཕེལ", "དུ", "གཏོང", "བར", "ཧ", "ཅང", "དགེ", "མཚན", "མཆིས", "སོ" });
        }

        /*
         * For chinese, tokenize as char (these can later form bigrams or whatever)
         */
        [Test]
        public void TestChinese()
        {
            AssertAnalyzesTo(a, "我是中国人。 １２３４ Ｔｅｓｔｓ ",
                new string[] { "我", "是", "中", "国", "人", "1234", "tests" });
        }

        [Test]
        public void TestHebrew()
        {
            AssertAnalyzesTo(a, "דנקנר תקף את הדו\"ח",
                new string[] { "דנקנר", "תקף", "את", "הדו\"ח" });
            AssertAnalyzesTo(a, "חברת בת של מודי'ס",
                new string[] { "חברת", "בת", "של", "מודי'ס" });
        }

        [Test]
        public void TestEmpty()
        {
            AssertAnalyzesTo(a, "", new string[] { });
            AssertAnalyzesTo(a, ".", new string[] { });
            AssertAnalyzesTo(a, " ", new string[] { });
        }

        /* test various jira issues this analyzer is related to */
        [Test]
        public void TestLUCENE1545()
        {
            /*
             * Standard analyzer does not correctly tokenize combining character U+0364 COMBINING LATIN SMALL LETTRE E.
             * The word "moͤchte" is incorrectly tokenized into "mo" "chte", the combining character is lost.
             * Expected result is only on token "moͤchte".
             */
            AssertAnalyzesTo(a, "moͤchte", new string[] { "moͤchte" });
        }

        /* Tests from StandardAnalyzer, just to show behavior is similar */
        [Test]
        public void TestAlphanumericSA()
        {
            // alphanumeric tokens
            AssertAnalyzesTo(a, "B2B", new string[] { "b2b" });
            AssertAnalyzesTo(a, "2B", new string[] { "2b" });
        }

        [Test]
        public void TestDelimitersSA()
        {
            // other delimiters: "-", "/", ","
            AssertAnalyzesTo(a, "some-dashed-phrase", new string[] { "some", "dashed", "phrase" });
            AssertAnalyzesTo(a, "dogs,chase,cats", new string[] { "dogs", "chase", "cats" });
            AssertAnalyzesTo(a, "ac/dc", new string[] { "ac", "dc" });
        }

        [Test]
        public void TestApostrophesSA()
        {
            // internal apostrophes: O'Reilly, you're, O'Reilly's
            AssertAnalyzesTo(a, "O'Reilly", new string[] { "o'reilly" });
            AssertAnalyzesTo(a, "you're", new string[] { "you're" });
            AssertAnalyzesTo(a, "she's", new string[] { "she's" });
            AssertAnalyzesTo(a, "Jim's", new string[] { "jim's" });
            AssertAnalyzesTo(a, "don't", new string[] { "don't" });
            AssertAnalyzesTo(a, "O'Reilly's", new string[] { "o'reilly's" });
        }

        [Test]
        public void TestNumericSA()
        {
            // floating point, serial, model numbers, ip addresses, etc.
            // every other segment must have at least one digit
            AssertAnalyzesTo(a, "21.35", new string[] { "21.35" });
            AssertAnalyzesTo(a, "R2D2 C3PO", new string[] { "r2d2", "c3po" });
            AssertAnalyzesTo(a, "216.239.63.104", new string[] { "216.239.63.104" });
            AssertAnalyzesTo(a, "216.239.63.104", new string[] { "216.239.63.104" });
        }

        [Test]
        public void TestTextWithNumbersSA()
        {
            // numbers
            AssertAnalyzesTo(a, "David has 5000 bones", new string[] { "david", "has", "5000", "bones" });
        }

        [Test]
        public void TestVariousTextSA()
        {
            // various
            AssertAnalyzesTo(a, "C embedded developers wanted", new string[] { "c", "embedded", "developers", "wanted" });
            AssertAnalyzesTo(a, "foo bar FOO BAR", new string[] { "foo", "bar", "foo", "bar" });
            AssertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new string[] { "foo", "bar", "foo", "bar" });
            AssertAnalyzesTo(a, "\"QUOTED\" word", new string[] { "quoted", "word" });
        }

        [Test]
        public void TestKoreanSA()
        {
            // Korean words
            AssertAnalyzesTo(a, "안녕하세요 한글입니다", new string[] { "안녕하세요", "한글입니다" });
        }

        [Test]
        public void TestReusableTokenStream()
        {
            AssertAnalyzesTo(a, "སྣོན་མཛོད་དང་ལས་འདིས་བོད་ཡིག་མི་ཉམས་གོང་འཕེལ་དུ་གཏོང་བར་ཧ་ཅང་དགེ་མཚན་མཆིས་སོ། །",
                new string[] { "སྣོན", "མཛོད", "དང", "ལས", "འདིས", "བོད", "ཡིག", "མི", "ཉམས", "གོང",
                      "འཕེལ", "དུ", "གཏོང", "བར", "ཧ", "ཅང", "དགེ", "མཚན", "མཆིས", "སོ" });
        }

        [Test]
        public void TestOffsets()
        {
            AssertAnalyzesTo(a, "David has 5000 bones",
                new string[] { "david", "has", "5000", "bones" },
                new int[] { 0, 6, 10, 15 },
                new int[] { 5, 9, 14, 20 });
        }

        [Test]
        public void TestTypes()
        {
            AssertAnalyzesTo(a, "David has 5000 bones",
                new string[] { "david", "has", "5000", "bones" },
                new string[] { "<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<ALPHANUM>" });
        }

        [Test]
        public void TestKorean()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "훈민정음",
                new string[] { "훈민정음" },
                new string[] { "<HANGUL>" });
        }

        [Test]
        public void TestJapanese()
        {
            BaseTokenStreamTestCase.AssertAnalyzesTo(a, "仮名遣い カタカナ",
                new string[] { "仮", "名", "遣", "い", "カタカナ" },
                new string[] { "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<IDEOGRAPHIC>", "<HIRAGANA>", "<KATAKANA>" });
        }

        /** blast some random strings through the analyzer */
        [Test]
        public void TestRandomStrings()
        {
            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }

        /** blast some random large strings through the analyzer */
        [Test]
        public void TestRandomHugeStrings()
        {
            Random random = Random;
            CheckRandomData(random, a, 100 * RandomMultiplier, 8192);
        }

        [Test]
        public void TestTokenAttributes()
        {
            using TokenStream ts = a.GetTokenStream("dummy", "This is a test");
            IScriptAttribute scriptAtt = ts.AddAttribute<IScriptAttribute>();
            ts.Reset();
            while (ts.IncrementToken())
            {
                assertEquals(UScript.Latin, scriptAtt.Code);
                assertEquals(UScript.GetName(UScript.Latin), scriptAtt.GetName());
                assertEquals(UScript.GetShortName(UScript.Latin), scriptAtt.GetShortName());
                assertTrue(ts.ReflectAsString(false).Contains("script=Latin"));
            }
            ts.End();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly CountdownEvent startingGun;

            public ThreadAnonymousClass(CountdownEvent startingGun)
            {
                this.startingGun = startingGun;
            }

            public override void Run()
            {
                try
                {
                    startingGun.Wait();
                    long tokenCount = 0;
                    string contents = "英 เบียร์ ビール ເບຍ abc";
                    for (int i = 0; i < 1000; i++)
                    {
                        //try
                        //{

                        Tokenizer tokenizer = new ICUTokenizer(new StringReader(contents));
                        tokenizer.Reset();
                        while (tokenizer.IncrementToken())
                        {
                            tokenCount++;
                        }
                        tokenizer.End();

                        if (Verbose)
                        {
                            SystemConsole.Out.WriteLine(tokenCount);
                        }
                    }
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        /** test for bugs like http://bugs.icu-project.org/trac/ticket/10767 */
        [Test]
        public void TestICUConcurrency()
        {
            int numThreads = 8;
            CountdownEvent startingGun = new CountdownEvent(1);
            ThreadAnonymousClass[] threads = new ThreadAnonymousClass[numThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ThreadAnonymousClass(startingGun);

                threads[i].Start();
            }
            startingGun.Signal();
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }
        }
    }
}