using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis
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

    [TestFixture]
    public class TestMockCharFilter : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void Test()
        {
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);

            AssertAnalyzesTo(analyzer, "aab", new string[] { "aab" }, new int[] { 0 }, new int[] { 3 });

            AssertAnalyzesTo(analyzer, "aabaa", new string[] { "aabaa" }, new int[] { 0 }, new int[] { 5 });

            AssertAnalyzesTo(analyzer, "aabcdefgaa", new string[] { "aabcdefgaa" }, new int[] { 0 }, new int[] { 10 });
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestMockCharFilter OuterInstance;

            public AnalyzerAnonymousInnerClassHelper(TestMockCharFilter outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, tokenizer);
            }

            protected internal override TextReader InitReader(string fieldName, TextReader reader)
            {
                return new MockCharFilter(reader, 7);
            }
        }
    }
}