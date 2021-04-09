// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
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
    /// Simple tests to ensure the CJK tokenizer factory is working. </summary>
    /// @deprecated remove this test in 5.0 
    [Obsolete("remove this test in 5.0")]
    public class TestCJKTokenizerFactory : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Ensure the tokenizer actually tokenizes CJK text correctly
        /// </summary>
        [Test]
        public virtual void TestTokenizer()
        {
            TextReader reader = new StringReader("我是中国人");
            TokenStream stream = TokenizerFactory("CJK").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "我是", "是中", "中国", "国人" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenizerFactory("CJK", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}