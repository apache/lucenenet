// Lucene version compatibility level 4.8.1
#if FEATURE_BREAKITERATOR
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Th
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
    /// Simple tests to ensure the Thai word filter factory is working.
    /// </summary>

    [Obsolete]
    public class TestThaiWordFilterFactory : BaseTokenStreamFactoryTestCase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        /// <summary>
        /// Ensure the filter actually decomposes text.
        /// </summary>
        [Test]
        public virtual void TestWordBreak()
        {
            // LUCENENET specific - DBBI_AVAILABLE removed because ICU always has a dictionary-based BreakIterator
            //AssumeTrue("JRE does not support Thai dictionary-based BreakIterator", ThaiWordFilter.DBBI_AVAILABLE);
            TextReader reader = new StringReader("การที่ได้ต้องแสดงว่างานดี");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("ThaiWord").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            // LUCENENET specific - DBBI_AVAILABLE removed because ICU always has a dictionary-based BreakIterator
            //AssumeTrue("JRE does not support Thai dictionary-based BreakIterator", ThaiWordFilter.DBBI_AVAILABLE);
            try
            {
                TokenFilterFactory("ThaiWord", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}
#endif