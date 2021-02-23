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

    public class TestPrefixAndSuffixAwareTokenFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void Test()
        {

            PrefixAndSuffixAwareTokenFilter ts = new PrefixAndSuffixAwareTokenFilter(new SingleTokenTokenStream(CreateToken("^", 0, 0)), new MockTokenizer(new StringReader("hello world"), MockTokenizer.WHITESPACE, false), new SingleTokenTokenStream(CreateToken("$", 0, 0)));

            AssertTokenStreamContents(ts, new string[] { "^", "hello", "world", "$" }, new int[] { 0, 0, 6, 11 }, new int[] { 0, 5, 11, 11 });
        }

        private static Token CreateToken(string term, int start, int offset)
        {
            return new Token(term, start, offset);
        }
    }
}