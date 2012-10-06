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

using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Miscellaneous
{
    [TestFixture]
    public class TestPrefixAndSuffixAwareTokenFilter : BaseTokenStreamTestCase
    {
        [Test]
        public void TestTokenStreamContents()
        {
            var ts = new PrefixAndSuffixAwareTokenFilter(
                new SingleTokenTokenStream(CreateToken("^", 0, 0)),
                new WhitespaceTokenizer(new StringReader("hello world")),
                new SingleTokenTokenStream(CreateToken("$", 0, 0)));

            AssertTokenStreamContents(ts,
                                      new[] {"^", "hello", "world", "$"},
                                      new[] {0, 0, 6, 11},
                                      new[] {0, 5, 11, 11});
        }

        private static Token CreateToken(String term, int start, int offset)
        {
            var token = new Token(start, offset);
            token.SetTermBuffer(term);
            return token;
        }
    }
}