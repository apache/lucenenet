// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

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

    /// <summary>
    /// basic tests for <see cref="TestICUTokenizerFactory"/>
    /// </summary>
    public class TestICUTokenizerFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestMixedText()
        {
            TextReader reader = new StringReader("การที่ได้ต้องแสดงว่างานดี  This is a test ກວ່າດອກ");
            ICUTokenizerFactory factory = new ICUTokenizerFactory(new Dictionary<string, string>());
            factory.Inform(new ClasspathResourceLoader(GetType()));
            TokenStream stream = factory.Create(reader);
            AssertTokenStreamContents(stream,
                new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี",
                "This", "is", "a", "test", "ກວ່າ", "ດອກ"});
        }

        [Test]
        public void TestTokenizeLatinOnWhitespaceOnly()
        {
            // “ U+201C LEFT DOUBLE QUOTATION MARK; ” U+201D RIGHT DOUBLE QUOTATION MARK
            TextReader reader = new StringReader
                ("  Don't,break.at?/(punct)!  \u201Cnice\u201D\r\n\r\n85_At:all; `really\" +2=3$5,&813 !@#%$^)(*@#$   ");
            IDictionary<string, string> args = new Dictionary<string, string>();
            args[ICUTokenizerFactory.RULEFILES] = "Latn:Latin-break-only-on-whitespace.rbbi";
            ICUTokenizerFactory factory = new ICUTokenizerFactory(args);
            factory.Inform(new ClasspathResourceLoader(this.GetType()));
            TokenStream stream = factory.Create(reader);
            AssertTokenStreamContents(stream,
                new string[] { "Don't,break.at?/(punct)!", "\u201Cnice\u201D", "85_At:all;", "`really\"", "+2=3$5,&813", "!@#%$^)(*@#$" },
                new string[] { "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<OTHER>" });
        }

        [Test]
        public void TestTokenizeLatinDontBreakOnHyphens()
        {
            TextReader reader = new StringReader
                ("One-two punch.  Brang-, not brung-it.  This one--not that one--is the right one, -ish.");
            IDictionary<string, string> args = new Dictionary<string, string>();
            args[ICUTokenizerFactory.RULEFILES] = "Latn:Latin-dont-break-on-hyphens.rbbi";
            ICUTokenizerFactory factory = new ICUTokenizerFactory(args);
            factory.Inform(new ClasspathResourceLoader(GetType()));
            TokenStream stream = factory.Create(reader);
            AssertTokenStreamContents(stream,
                new string[] { "One-two", "punch",
                    "Brang", "not", "brung-it",
                    "This", "one", "not", "that", "one", "is", "the", "right", "one", "ish" });
        }

        /**
         * Specify more than one script/rule file pair.
         * Override default DefaultICUTokenizerConfig Thai script tokenization.
         * Use the same rule file for both scripts.
         */
        [Test]
        public void TestKeywordTokenizeCyrillicAndThai()
        {
            TextReader reader = new StringReader
                ("Some English.  Немного русский.  ข้อความภาษาไทยเล็ก ๆ น้อย ๆ  More English.");
            IDictionary<string, string> args = new Dictionary<string, string>();
            args[ICUTokenizerFactory.RULEFILES] = "Cyrl:KeywordTokenizer.rbbi,Thai:KeywordTokenizer.rbbi";
            ICUTokenizerFactory factory = new ICUTokenizerFactory(args);
            factory.Inform(new ClasspathResourceLoader(GetType()));
            TokenStream stream = factory.Create(reader);
            AssertTokenStreamContents(stream, new string[] { "Some", "English",
                "Немного русский.  ",
                "ข้อความภาษาไทยเล็ก ๆ น้อย ๆ  ",
                "More", "English" });
            }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new ICUTokenizerFactory(new Dictionary<string, string>() {
                    {"bogusArg", "bogusValue" }
                });
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}