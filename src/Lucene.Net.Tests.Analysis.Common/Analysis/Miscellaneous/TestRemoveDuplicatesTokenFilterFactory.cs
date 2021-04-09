// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.Text.RegularExpressions;

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

    /// <summary>
    /// Simple tests to ensure this factory is working </summary>
    public class TestRemoveDuplicatesTokenFilterFactory : BaseTokenStreamFactoryTestCase
    {

        public static Token Tok(int pos, string t, int start, int end)
        {
            Token tok = new Token(t, start, end);
            tok.PositionIncrement = pos;
            return tok;
        }

        public virtual void TestDups(string expected, params Token[] tokens)
        {
            TokenStream stream = new CannedTokenStream(tokens);
            stream = TokenFilterFactory("RemoveDuplicates").Create(stream);
            AssertTokenStreamContents(stream, Regex.Split(expected, "\\s").TrimEnd());
        }

        [Test]
        public virtual void TestSimpleDups()
        {
            TestDups("A B C D E", Tok(1, "A", 0, 4), Tok(1, "B", 5, 10), Tok(0, "B", 11, 15), Tok(1, "C", 16, 20), Tok(0, "D", 16, 20), Tok(1, "E", 21, 25));
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("RemoveDuplicates", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}