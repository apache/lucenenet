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
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.NGram
{

    /*
     * Tests {@link NGramTokenizer} for correctness.
     */
    [TestFixture]
    public class TestNGramTokenizer : BaseTokenStreamTestCase
    {
        private StringReader input;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            input = new StringReader("abcde");
        }

        [Test]
        public void TestInvalidInput()
        {
            bool gotException = false;
            try
            {
                new NGramTokenizer(input, 2, 1);
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
                new NGramTokenizer(input, 0, 1);
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
            NGramTokenizer tokenizer = new NGramTokenizer(input, 1, 1);
            AssertTokenStreamContents(tokenizer, new String[] { "a", "b", "c", "d", "e" }, new int[] { 0, 1, 2, 3, 4 }, new int[] { 1, 2, 3, 4, 5 }, 5 /* abcde */);
        }

        [Test]
        public void TestBigrams()
        {
            NGramTokenizer tokenizer = new NGramTokenizer(input, 2, 2);
            AssertTokenStreamContents(tokenizer, new String[] { "ab", "bc", "cd", "de" }, new int[] { 0, 1, 2, 3 }, new int[] { 2, 3, 4, 5 }, 5 /* abcde */);
        }

        [Test]
        public void TestNgrams()
        {
            NGramTokenizer tokenizer = new NGramTokenizer(input, 1, 3);
            AssertTokenStreamContents(tokenizer,
              new String[] { "a", "b", "c", "d", "e", "ab", "bc", "cd", "de", "abc", "bcd", "cde" },
              new int[] { 0, 1, 2, 3, 4, 0, 1, 2, 3, 0, 1, 2 },
              new int[] { 1, 2, 3, 4, 5, 2, 3, 4, 5, 3, 4, 5 },
              5 /* abcde */
            );
        }

        [Test]
        public void TestOversizedNgrams()
        {
            NGramTokenizer tokenizer = new NGramTokenizer(input, 6, 7);
            AssertTokenStreamContents(tokenizer, new String[0], new int[0], new int[0], 5 /* abcde */);
        }

        [Test]
        public void TestReset()
        {
            NGramTokenizer tokenizer = new NGramTokenizer(input, 1, 1);
            AssertTokenStreamContents(tokenizer, new String[] { "a", "b", "c", "d", "e" }, new int[] { 0, 1, 2, 3, 4 }, new int[] { 1, 2, 3, 4, 5 }, 5 /* abcde */);
            tokenizer.Reset(new StringReader("abcde"));
            AssertTokenStreamContents(tokenizer, new String[] { "a", "b", "c", "d", "e" }, new int[] { 0, 1, 2, 3, 4 }, new int[] { 1, 2, 3, 4, 5 }, 5 /* abcde */);
        }
    }
}