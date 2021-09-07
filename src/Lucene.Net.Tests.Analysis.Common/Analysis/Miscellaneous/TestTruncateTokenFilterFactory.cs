// Lucene version compatibility level 4.8.1
using NUnit.Framework;
using System;
using System.IO;
using BaseTokenStreamFactoryTestCase = Lucene.Net.Analysis.Util.BaseTokenStreamFactoryTestCase;

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
    /// Simple tests to ensure the simple truncation filter factory is working.
    /// </summary>
    public class TestTruncateTokenFilterFactory : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Ensure the filter actually truncates text.
        /// </summary>
        [Test]
        public virtual void TestTruncating()
        {
            TextReader reader = new StringReader("abcdefg 1234567 ABCDEFG abcde abc 12345 123");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Truncate", TruncateTokenFilterFactory.PREFIX_LENGTH_KEY, "5").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "abcde", "12345", "ABCDE", "abcde", "abc", "12345", "123" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception
        /// </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("Truncate", TruncateTokenFilterFactory.PREFIX_LENGTH_KEY, "5", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameter(s):"));
            }
        }

        /// <summary>
        /// Test that negative prefix length result in exception
        /// </summary>
        [Test]
        public virtual void TestNonPositivePrefixLengthArgument()
        {
            try
            {
                TokenFilterFactory("Truncate", TruncateTokenFilterFactory.PREFIX_LENGTH_KEY, "-5");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains(TruncateTokenFilterFactory.PREFIX_LENGTH_KEY + " parameter must be a positive number: -5"));
            }
        }
    }
}