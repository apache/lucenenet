using Lucene.Net.Analysis;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Search.Highlight
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

    public class OffsetLimitTokenFilterTest : BaseTokenStreamTestCase
    {
        [Test]
        public void TestFilter()
        {
            // we disable MockTokenizer checks because we will forcefully limit the 
            // tokenstream and call end() before incrementToken() returns false.
            MockTokenizer stream = new MockTokenizer(new StringReader(
                "short toolong evenmuchlongertext a ab toolong foo"),
                MockTokenizer.WHITESPACE, false);
            stream.EnableChecks = (false);
            OffsetLimitTokenFilter filter = new OffsetLimitTokenFilter(stream, 10);
            AssertTokenStreamContents(filter, new String[] { "short", "toolong" });

            stream = new MockTokenizer(new StringReader(
                "short toolong evenmuchlongertext a ab toolong foo"),
            MockTokenizer.WHITESPACE, false);
            stream.EnableChecks = (false);
            filter = new OffsetLimitTokenFilter(stream, 12);
            AssertTokenStreamContents(filter, new String[] { "short", "toolong" });

            stream = new MockTokenizer(new StringReader(
                "short toolong evenmuchlongertext a ab toolong foo"),
                MockTokenizer.WHITESPACE, false);
            stream.EnableChecks = (false);
            filter = new OffsetLimitTokenFilter(stream, 30);
            AssertTokenStreamContents(filter, new String[] {"short", "toolong",
                "evenmuchlongertext"});

            CheckOneTerm(new AnalyzerAnonymousClass(), "llenges", "llenges");
        }

        private sealed class AnalyzerAnonymousClass : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                tokenizer.EnableChecks = (false);
                return new TokenStreamComponents(tokenizer, new OffsetLimitTokenFilter(tokenizer, 10));
            }
        }
    }
}
