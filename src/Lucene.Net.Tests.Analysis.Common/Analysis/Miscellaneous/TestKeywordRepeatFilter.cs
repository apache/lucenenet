// Lucene version compatibility level 4.8.1
using NUnit.Framework;
using System.IO;

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

    using SnowballFilter = Lucene.Net.Analysis.Snowball.SnowballFilter;


    public class TestKeywordRepeatFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestBasic()
        {
            TokenStream ts = new RemoveDuplicatesTokenFilter(new SnowballFilter(new KeywordRepeatFilter(new MockTokenizer(new StringReader("the birds are flying"), MockTokenizer.WHITESPACE, false)), "English"));
            AssertTokenStreamContents(ts, new string[] { "the", "birds", "bird", "are", "flying", "fli" }, new int[] { 1, 1, 0, 1, 1, 0 });
        }


        [Test]
        public virtual void TestComposition()
        {
            TokenStream ts = new RemoveDuplicatesTokenFilter(new SnowballFilter(new KeywordRepeatFilter(new KeywordRepeatFilter(new MockTokenizer(new StringReader("the birds are flying"), MockTokenizer.WHITESPACE, false))), "English"));
            AssertTokenStreamContents(ts, new string[] { "the", "birds", "bird", "are", "flying", "fli" }, new int[] { 1, 1, 0, 1, 1, 0 });
        }
    }
}