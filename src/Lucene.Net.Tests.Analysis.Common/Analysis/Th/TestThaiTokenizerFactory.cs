using Lucene.Net.Analysis.Util;
using NUnit.Framework;
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
    public class TestThaiTokenizerFactory : BaseTokenStreamFactoryTestCase
    {

        public override void SetUp()
        {
            base.SetUp();
#if NETSTANDARD
            fail("LUCENENET TODO: AccessViolationException being thrown from icu-dotnet");
#endif
        }

        /// <summary>
        /// Ensure the filter actually decomposes text.
        /// </summary>
        [Test]
        public virtual void TestWordBreak()
        {
            AssumeTrue("JRE does not support Thai dictionary-based BreakIterator", ThaiTokenizer.DBBI_AVAILABLE);
            Tokenizer tokenizer = TokenizerFactory("Thai").Create(new StringReader("การที่ได้ต้องแสดงว่างานดี"));
            AssertTokenStreamContents(tokenizer, new string[] { "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            AssumeTrue("JRE does not support Thai dictionary-based BreakIterator", ThaiTokenizer.DBBI_AVAILABLE);
            try
            {
                TokenizerFactory("Thai", "bogusArg", "bogusValue");
                fail();
            }
            catch (System.ArgumentException expected)
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}