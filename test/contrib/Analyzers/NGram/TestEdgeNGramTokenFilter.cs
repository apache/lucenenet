/**
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
using System.Collections;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.NGram
{

    /**
     * Tests {@link EdgeNGramTokenFilter} for correctness.
     */
    [TestFixture]
    public class TestEdgeNGramTokenFilter : BaseTokenStreamTestCase
    {
        private TokenStream input;

        [SetUp]
        public void SetUp()
        {
            base.SetUp();
            input = new WhitespaceTokenizer(new StringReader("abcde"));
        }

        [Test]
        public void TestInvalidInput()
        {
            bool gotException = false;
            try
            {
                new EdgeNGramTokenFilter(input, EdgeNGramTokenFilter.Side.FRONT, 0, 0);
            }
            catch (System.ArgumentException e)
            {
                gotException = true;
            }
            Assert.IsTrue(gotException);
        }

        [Test]
        public void TestInvalidInput2()
        {
            bool gotException = false;
            try
            {
                new EdgeNGramTokenFilter(input, EdgeNGramTokenFilter.Side.FRONT, 2, 1);
            }
            catch (System.ArgumentException e)
            {
                gotException = true;
            }
            Assert.IsTrue(gotException);
        }

        [Test]
        public void TestInvalidInput3()
        {
            bool gotException = false;
            try
            {
                new EdgeNGramTokenFilter(input, EdgeNGramTokenFilter.Side.FRONT, -1, 2);
            }
            catch (System.ArgumentException e)
            {
                gotException = true;
            }
            Assert.IsTrue(gotException);
        }

        [Test]
        public void TestFrontUnigram()
        {
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(input, EdgeNGramTokenFilter.Side.FRONT, 1, 1);
            AssertTokenStreamContents(tokenizer, new String[] { "a" }, new int[] { 0 }, new int[] { 1 });
        }

        [Test]
        public void TestBackUnigram()
        {
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(input, EdgeNGramTokenFilter.Side.BACK, 1, 1);
            AssertTokenStreamContents(tokenizer, new String[] { "e" }, new int[] { 4 }, new int[] { 5 });
        }

        [Test]
        public void TestOversizedNgrams()
        {
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(input, EdgeNGramTokenFilter.Side.FRONT, 6, 6);
            AssertTokenStreamContents(tokenizer, new String[0], new int[0], new int[0]);
        }

        [Test]
        public void TestFrontRangeOfNgrams()
        {
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(input, EdgeNGramTokenFilter.Side.FRONT, 1, 3);
            AssertTokenStreamContents(tokenizer, new String[] { "a", "ab", "abc" }, new int[] { 0, 0, 0 }, new int[] { 1, 2, 3 });
        }

        [Test]
        public void TestBackRangeOfNgrams()
        {
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(input, EdgeNGramTokenFilter.Side.BACK, 1, 3);
            AssertTokenStreamContents(tokenizer, new String[] { "e", "de", "cde" }, new int[] { 4, 3, 2 }, new int[] { 5, 5, 5 });
        }

        [Test]
        public void TestSmallTokenInStream()
        {
            input = new WhitespaceTokenizer(new StringReader("abc de fgh"));
            EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(input, EdgeNGramTokenFilter.Side.FRONT, 3, 3);
            AssertTokenStreamContents(tokenizer, new String[] { "abc", "fgh" }, new int[] { 0, 7 }, new int[] { 3, 10 });
        }

        [Test]
        public void TestReset()
        {
            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(new StringReader("abcde"));
            EdgeNGramTokenFilter filter = new EdgeNGramTokenFilter(tokenizer, EdgeNGramTokenFilter.Side.FRONT, 1, 3);
            AssertTokenStreamContents(filter, new String[] { "a", "ab", "abc" }, new int[] { 0, 0, 0 }, new int[] { 1, 2, 3 });
            tokenizer.Reset(new StringReader("abcde"));
            AssertTokenStreamContents(filter, new String[] { "a", "ab", "abc" }, new int[] { 0, 0, 0 }, new int[] { 1, 2, 3 });
        }
    }
}