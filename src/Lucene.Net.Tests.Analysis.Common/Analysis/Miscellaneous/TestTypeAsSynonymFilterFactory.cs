// Lucene version compatibility level 8.2.0
// LUCENENET NOTE: Ported because Lucene.Net.Analysis.OpenNLP requires this to be useful.
using Lucene.Net.Analysis.Util;
using NUnit.Framework;

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

    public class TestTypeAsSynonymFilterFactory : BaseTokenStreamFactoryTestCase
    {
        private static readonly Token[] TOKENS = { token("Visit", "<ALPHANUM>"), token("example.com", "<URL>") };

        [Test]
        public void TestBasic()
        {
            TokenStream stream = new CannedTokenStream(TOKENS);
            stream = TokenFilterFactory("TypeAsSynonym").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Visit", "<ALPHANUM>", "example.com", "<URL>" },
                null, null, new string[] { "<ALPHANUM>", "<ALPHANUM>", "<URL>", "<URL>" }, new int[] { 1, 0, 1, 0 });
        }

        [Test]
        public void TestPrefix()
        {
            TokenStream stream = new CannedTokenStream(TOKENS);
            stream = TokenFilterFactory("TypeAsSynonym", "prefix", "_type_").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Visit", "_type_<ALPHANUM>", "example.com", "_type_<URL>" },
                null, null, new string[] { "<ALPHANUM>", "<ALPHANUM>", "<URL>", "<URL>" }, new int[] { 1, 0, 1, 0 });
        }

        private static Token token(string term, string type)
        {
            Token token = new Token();
            token.Clear();
            token.Append(term);
            token.Type = type;
            return token;
        }
    }
}
