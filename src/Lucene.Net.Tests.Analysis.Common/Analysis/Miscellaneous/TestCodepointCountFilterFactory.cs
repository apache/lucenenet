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

    public class TestCodepointCountFilterFactory : BaseTokenStreamFactoryTestCase
    {
        [Test]
        public virtual void TestPositionIncrements()
        {
            Reader reader = new StringReader("foo foobar super-duper-trooper");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("CodepointCount", "min", "4", "max", "10").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "foobar" }, new int[] { 2 });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("CodepointCount", "min", "4", "max", "5", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }

        /// <summary>
        /// Test that invalid arguments result in exception </summary>
        [Test]
        public virtual void TestInvalidArguments()
        {
            try
            {
                Reader reader = new StringReader("foo foobar super-duper-trooper");
                TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenFilterFactory("CodepointCount", CodepointCountFilterFactory.MIN_KEY, "5", CodepointCountFilterFactory.MAX_KEY, "4").Create(stream);
                fail();
            }
            catch (ArgumentOutOfRangeException expected) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                assertTrue(expected.Message.Contains("maximum length must not be greater than minimum length"));
            }
        }
    }
}