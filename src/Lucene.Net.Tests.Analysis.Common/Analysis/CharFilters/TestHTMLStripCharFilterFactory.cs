// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.CharFilters
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
    /// Simple tests to ensure this factory is working
    /// </summary>
    public class TestHTMLStripCharFilterFactory : BaseTokenStreamFactoryTestCase
    {

        [Test]
        public virtual void TestNothingChanged()
        {
            //                             11111111112
            //                   012345678901234567890
            const string text = "this is only a test.";
            TextReader cs = CharFilterFactory("HTMLStrip", "escapedTags", "a, Title").Create(new StringReader(text));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "this", "is", "only", "a", "test." }, new int[] { 0, 5, 8, 13, 15 }, new int[] { 4, 7, 12, 14, 20 });
        }

        [Test]
        public virtual void TestNoEscapedTags()
        {
            //                             11111111112222222222333333333344
            //                   012345678901234567890123456789012345678901
            const string text = "<u>this</u> is <b>only</b> a <I>test</I>.";
            TextReader cs = CharFilterFactory("HTMLStrip").Create(new StringReader(text));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "this", "is", "only", "a", "test." }, new int[] { 3, 12, 18, 27, 32 }, new int[] { 11, 14, 26, 28, 41 });
        }

        [Test]
        public virtual void TestEscapedTags()
        {
            //                             11111111112222222222333333333344
            //                   012345678901234567890123456789012345678901
            const string text = "<u>this</u> is <b>only</b> a <I>test</I>.";
            TextReader cs = CharFilterFactory("HTMLStrip", "escapedTags", "U i").Create(new StringReader(text));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "<u>this</u>", "is", "only", "a", "<I>test</I>." }, new int[] { 0, 12, 18, 27, 29 }, new int[] { 11, 14, 26, 28, 41 });
        }

        [Test]
        public virtual void TestSeparatorOnlyEscapedTags()
        {
            //                             11111111112222222222333333333344
            //                   012345678901234567890123456789012345678901
            const string text = "<u>this</u> is <b>only</b> a <I>test</I>.";
            TextReader cs = CharFilterFactory("HTMLStrip", "escapedTags", ",, , ").Create(new StringReader(text));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "this", "is", "only", "a", "test." }, new int[] { 3, 12, 18, 27, 32 }, new int[] { 11, 14, 26, 28, 41 });
        }

        [Test]
        public virtual void TestEmptyEscapedTags()
        {
            //                             11111111112222222222333333333344
            //                   012345678901234567890123456789012345678901
            const string text = "<u>this</u> is <b>only</b> a <I>test</I>.";
            TextReader cs = CharFilterFactory("HTMLStrip", "escapedTags", "").Create(new StringReader(text));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "this", "is", "only", "a", "test." }, new int[] { 3, 12, 18, 27, 32 }, new int[] { 11, 14, 26, 28, 41 });
        }

        [Test]
        public virtual void TestSingleEscapedTag()
        {
            //                             11111111112222222222333333333344
            //                   012345678901234567890123456789012345678901
            const string text = "<u>this</u> is <b>only</b> a <I>test</I>.";
            TextReader cs = CharFilterFactory("HTMLStrip", "escapedTags", ", B\r\n\t").Create(new StringReader(text));
            TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "this", "is", "<b>only</b>", "a", "test." }, new int[] { 3, 12, 15, 27, 32 }, new int[] { 11, 14, 26, 28, 41 });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                CharFilterFactory("HTMLStrip", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}