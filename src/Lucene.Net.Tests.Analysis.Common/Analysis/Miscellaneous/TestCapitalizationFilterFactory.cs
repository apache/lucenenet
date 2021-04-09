// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;
using Reader = System.IO.TextReader;

namespace Lucene.Net.Analysis.Miscellaneous
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

    public class TestCapitalizationFilterFactory : BaseTokenStreamFactoryTestCase
    {

        [Test]
        public virtual void TestCapitalization()
        {
            Reader reader = new StringReader("kiTTEN");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Kitten" });
        }

        [Test]
        public virtual void TestCapitalization2()
        {
            Reader reader = new StringReader("and");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "And" });
        }

        /// <summary>
        /// first is forced, but it's not a keep word, either </summary>
        [Test]
        public virtual void TestCapitalization3()
        {
            Reader reader = new StringReader("AnD");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "And" });
        }

        [Test]
        public virtual void TestCapitalization4()
        {
            Reader reader = new StringReader("AnD");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "false").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "And" });
        }

        [Test]
        public virtual void TestCapitalization5()
        {
            Reader reader = new StringReader("big");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Big" });
        }

        [Test]
        public virtual void TestCapitalization6()
        {
            Reader reader = new StringReader("BIG");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "BIG" });
        }

        [Test]
        public virtual void TestCapitalization7()
        {
            Reader reader = new StringReader("Hello thEre my Name is Ryan");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Hello there my name is ryan" });
        }

        [Test]
        public virtual void TestCapitalization8()
        {
            Reader reader = new StringReader("Hello thEre my Name is Ryan");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "forceFirstLetter", "true", 
                // LUCENENET specific - pass in the invariant culture to get the same behavior as Lucene,
                // otherwise the filter is culture-sensitive.
                "culture", "invariant").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Hello", "There", "My", "Name", "Is", "Ryan" });
        }

        [Test]
        public virtual void TestCapitalization9()
        {
            Reader reader = new StringReader("Hello thEre my Name is Ryan");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "minWordLength", "3", "forceFirstLetter", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Hello", "There", "my", "Name", "is", "Ryan" });
        }

        [Test]
        public virtual void TestCapitalization10()
        {
            Reader reader = new StringReader("McKinley");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "minWordLength", "3", "forceFirstLetter", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Mckinley" });
        }

        /// <summary>
        /// using "McK" as okPrefix </summary>
        [Test]
        public virtual void TestCapitalization11()
        {
            Reader reader = new StringReader("McKinley");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "minWordLength", "3", "okPrefix", "McK", "forceFirstLetter", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "McKinley" });
        }

        /// <summary>
        /// test with numbers </summary>
        [Test]
        public virtual void TestCapitalization12()
        {
            Reader reader = new StringReader("1st 2nd third");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "minWordLength", "3", "okPrefix", "McK", "forceFirstLetter", "false").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "1st", "2nd", "Third" });
        }

        [Test]
        public virtual void TestCapitalization13()
        {
            Reader reader = new StringReader("the The the");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
            stream = TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "minWordLength", "3", "okPrefix", "McK", "forceFirstLetter", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "The The the" });
        }

        [Test]
        public virtual void TestKeepIgnoreCase()
        {
            Reader reader = new StringReader("kiTTEN");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
            stream = TokenFilterFactory("Capitalization", "keep", "kitten", "keepIgnoreCase", "true", "onlyFirstWord", "true", "forceFirstLetter", "true").Create(stream);

            AssertTokenStreamContents(stream, new string[] { "KiTTEN" });
        }

        [Test]
        public virtual void TestKeepIgnoreCase2()
        {
            Reader reader = new StringReader("kiTTEN");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
            stream = TokenFilterFactory("Capitalization", "keep", "kitten", "keepIgnoreCase", "true", "onlyFirstWord", "true", "forceFirstLetter", "false").Create(stream);

            AssertTokenStreamContents(stream, new string[] { "kiTTEN" });
        }

        [Test]
        public virtual void TestKeepIgnoreCase3()
        {
            Reader reader = new StringReader("kiTTEN");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
            stream = TokenFilterFactory("Capitalization", "keepIgnoreCase", "true", "onlyFirstWord", "true", "forceFirstLetter", "false").Create(stream);

            AssertTokenStreamContents(stream, new string[] { "Kitten" });
        }

        /// <summary>
        /// Test CapitalizationFilterFactory's minWordLength option.
        /// 
        /// This is very weird when combined with ONLY_FIRST_WORD!!!
        /// </summary>
        [Test]
        public virtual void TestMinWordLength()
        {
            Reader reader = new StringReader("helo testing");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "onlyFirstWord", "true", "minWordLength", "5").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "helo", "Testing" });
        }

        /// <summary>
        /// Test CapitalizationFilterFactory's maxWordCount option with only words of 1
        /// in each token (it should do nothing)
        /// </summary>
        [Test]
        public virtual void TestMaxWordCount()
        {
            Reader reader = new StringReader("one two three four");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "maxWordCount", "2").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "One", "Two", "Three", "Four" });
        }

        /// <summary>
        /// Test CapitalizationFilterFactory's maxWordCount option when exceeded
        /// </summary>
        [Test]
        public virtual void TestMaxWordCount2()
        {
            Reader reader = new StringReader("one two three four");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
            stream = TokenFilterFactory("Capitalization", "maxWordCount", "2").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "one two three four" });
        }

        /// <summary>
        /// Test CapitalizationFilterFactory's maxTokenLength option when exceeded
        /// 
        /// This is weird, it is not really a max, but inclusive (look at 'is')
        /// </summary>
        [Test]
        public virtual void TestMaxTokenLength()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "maxTokenLength", "2").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this", "is", "A", "test" });
        }

        /// <summary>
        /// Test CapitalizationFilterFactory's forceFirstLetter option
        /// </summary>
        [Test]
        public virtual void TestForceFirstLetterWithKeep()
        {
            Reader reader = new StringReader("kitten");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Capitalization", "keep", "kitten", "forceFirstLetter", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Kitten" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("Capitalization", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }

        /// <summary>
        /// Test that invalid arguments result in exception
        /// </summary>
        [Test]
        public virtual void TestInvalidArguments()
        {
            foreach (string arg in new string[] { "minWordLength", "maxTokenLength", "maxWordCount" })
            {
                try
                {
                    Reader reader = new StringReader("foo foobar super-duper-trooper");
                    TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);

                    TokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", arg, "-3", "okPrefix", "McK", "forceFirstLetter", "true").Create(stream);
                    fail();
                }
                catch (ArgumentOutOfRangeException expected) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                {
                    assertTrue(expected.Message.Contains(arg + " must be greater than or equal to zero") || expected.Message.Contains(arg + " must be greater than zero"));
                }
            }
        }
    }
}