// Lucene version compatibility level 4.8.1
using System;
using NUnit.Framework;
using Lucene.Net.Analysis.Util;
using System.IO;

namespace Lucene.Net.Analysis.Cjk
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
    /// Simple tests to ensure the CJK bigram factory is working.
    /// </summary>
    public class TestCJKBigramFilterFactory : BaseTokenStreamFactoryTestCase
    {
        [Test]
        public virtual void TestDefaults()
        {
            TextReader reader = new StringReader("多くの学生が試験に落ちた。");
            TokenStream stream = TokenizerFactory("standard").Create(reader);
            stream = TokenFilterFactory("CJKBigram").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた" });
        }

        [Test]
        public virtual void TestHanOnly()
        {
            TextReader reader = new StringReader("多くの学生が試験に落ちた。");
            TokenStream stream = TokenizerFactory("standard").Create(reader);
            stream = TokenFilterFactory("CJKBigram", "hiragana", "false").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "多", "く", "の", "学生", "が", "試験", "に", "落", "ち", "た" });
        }

        [Test]
        public virtual void TestHanOnlyUnigrams()
        {
            TextReader reader = new StringReader("多くの学生が試験に落ちた。");
            TokenStream stream = TokenizerFactory("standard").Create(reader);
            stream = TokenFilterFactory("CJKBigram", "hiragana", "false", "outputUnigrams", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "多", "く", "の", "学", "学生", "生", "が", "試", "試験", "験", "に", "落", "ち", "た" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("CJKBigram", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}