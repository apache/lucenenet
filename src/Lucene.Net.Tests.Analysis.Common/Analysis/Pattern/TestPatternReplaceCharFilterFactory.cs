// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;
using Reader = System.IO.TextReader;

namespace Lucene.Net.Analysis.Pattern
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
    public class TestPatternReplaceCharFilterFactory : BaseTokenStreamFactoryTestCase
    {

        //           1111
        // 01234567890123
        // this is test.
        [Test]
        public virtual void TestNothingChange()
        {
            Reader reader = new StringReader("this is test.");
            reader = CharFilterFactory("PatternReplace", "pattern", "(aa)\\s+(bb)\\s+(cc)", "replacement", "$1$2$3").Create(reader);
            TokenStream ts = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "this", "is", "test." }, new int[] { 0, 5, 8 }, new int[] { 4, 7, 13 });
        }

        // 012345678
        // aa bb cc
        [Test]
        public virtual void TestReplaceByEmpty()
        {
            Reader reader = new StringReader("aa bb cc");
            reader = CharFilterFactory("PatternReplace", "pattern", "(aa)\\s+(bb)\\s+(cc)").Create(reader);
            TokenStream ts = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { });
        }

        // 012345678
        // aa bb cc
        // aa#bb#cc
        [Test]
        public virtual void Test1block1matchSameLength()
        {
            Reader reader = new StringReader("aa bb cc");
            reader = CharFilterFactory("PatternReplace", "pattern", "(aa)\\s+(bb)\\s+(cc)", "replacement", "$1#$2#$3").Create(reader);
            TokenStream ts = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(ts, new string[] { "aa#bb#cc" }, new int[] { 0 }, new int[] { 8 });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                CharFilterFactory("PatternReplace", "pattern", "something", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}