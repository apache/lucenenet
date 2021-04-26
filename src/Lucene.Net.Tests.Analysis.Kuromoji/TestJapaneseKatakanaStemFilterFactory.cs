using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Ja
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
    /// Simple tests for <see cref="JapaneseKatakanaStemFilterFactory"/>
    /// </summary>
    public class TestJapaneseKatakanaStemFilterFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestKatakanaStemming()
        {
            JapaneseTokenizerFactory tokenizerFactory = new JapaneseTokenizerFactory(new Dictionary<String, String>());
            tokenizerFactory.Inform(new StringMockResourceLoader(""));
            TokenStream tokenStream = tokenizerFactory.Create(
                new StringReader("明後日パーティーに行く予定がある。図書館で資料をコピーしました。")
            );
            JapaneseKatakanaStemFilterFactory filterFactory = new JapaneseKatakanaStemFilterFactory(new Dictionary<String, String>()); ;
            AssertTokenStreamContents(filterFactory.Create(tokenStream),
                new String[]{ "明後日", "パーティ", "に", "行く", "予定", "が", "ある",   // パーティー should be stemmed
                      "図書館", "で", "資料", "を", "コピー", "し", "まし", "た"} // コピー should not be stemmed
            );
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new JapaneseKatakanaStemFilterFactory(new Dictionary<String, String>() {
                    { "bogusArg", "bogusValue" }
                });
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}
