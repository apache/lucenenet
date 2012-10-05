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
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.NGram
{

    /*
     * Tests {@link NGramTokenFilter} for correctness.
     */
    [TestFixture]
    public class TestNGramTokenFilter : BaseTokenStreamTestCase
    {
        private TokenStream input;

        [SetUp]
        public override void SetUp()
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
                new NGramTokenFilter(input, 2, 1);
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
                new NGramTokenFilter(input, 0, 1);
            }
            catch (System.ArgumentException e)
            {
                gotException = true;
            }
            Assert.IsTrue(gotException);
        }

        [Test]
        public void TestUnigrams()
        {
            NGramTokenFilter filter = new NGramTokenFilter(input, 1, 1);
            AssertTokenStreamContents(filter, new String[] { "a", "b", "c", "d", "e" }, new int[] { 0, 1, 2, 3, 4 }, new int[] { 1, 2, 3, 4, 5 });
        }

        [Test]
        public void TestBigrams()
        {
            NGramTokenFilter filter = new NGramTokenFilter(input, 2, 2);
            AssertTokenStreamContents(filter, new String[] { "ab", "bc", "cd", "de" }, new int[] { 0, 1, 2, 3 }, new int[] { 2, 3, 4, 5 });
        }

        [Test]
        public void TestNgrams()
        {
            NGramTokenFilter filter = new NGramTokenFilter(input, 1, 3);
            AssertTokenStreamContents(filter,
              new String[] { "a", "b", "c", "d", "e", "ab", "bc", "cd", "de", "abc", "bcd", "cde" },
              new int[] { 0, 1, 2, 3, 4, 0, 1, 2, 3, 0, 1, 2 },
              new int[] { 1, 2, 3, 4, 5, 2, 3, 4, 5, 3, 4, 5 }
            );
        }

        [Test]
        public void TestOversizedNgrams()
        {
            NGramTokenFilter filter = new NGramTokenFilter(input, 6, 7);
            AssertTokenStreamContents(filter, new String[0], new int[0], new int[0]);
        }

        [Test]
        public void TestSmallTokenInStream()
        {
            input = new WhitespaceTokenizer(new StringReader("abc de fgh"));
            NGramTokenFilter filter = new NGramTokenFilter(input, 3, 3);
            AssertTokenStreamContents(filter, new String[] { "abc", "fgh" }, new int[] { 0, 7 }, new int[] { 3, 10 });
        }

        [Test]
        public void TestReset()
        {
            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(new StringReader("abcde"));
            NGramTokenFilter filter = new NGramTokenFilter(tokenizer, 1, 1);
            AssertTokenStreamContents(filter, new String[] { "a", "b", "c", "d", "e" }, new int[] { 0, 1, 2, 3, 4 }, new int[] { 1, 2, 3, 4, 5 });
            tokenizer.Reset(new StringReader("abcde"));
            AssertTokenStreamContents(filter, new String[] { "a", "b", "c", "d", "e" }, new int[] { 0, 1, 2, 3, 4 }, new int[] { 1, 2, 3, 4, 5 });
        }
    }
}