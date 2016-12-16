using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
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

    public class TestPerFieldAnalyzerWrapper : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestPerField()
        {
            string text = "Qwerty";

            IDictionary<string, Analyzer> analyzerPerField = new Dictionary<string, Analyzer>();
            analyzerPerField["special"] = new SimpleAnalyzer(TEST_VERSION_CURRENT);

            PerFieldAnalyzerWrapper analyzer = new PerFieldAnalyzerWrapper(new WhitespaceAnalyzer(TEST_VERSION_CURRENT), analyzerPerField);

            TokenStream tokenStream = analyzer.TokenStream("field", text);
            try
            {
                ICharTermAttribute termAtt = tokenStream.GetAttribute<ICharTermAttribute>();
                tokenStream.Reset();

                assertTrue(tokenStream.IncrementToken());
                assertEquals("WhitespaceAnalyzer does not lowercase", "Qwerty", termAtt.ToString());
                assertFalse(tokenStream.IncrementToken());
                tokenStream.End();
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(tokenStream);
            }

            tokenStream = analyzer.TokenStream("special", text);
            try
            {
                ICharTermAttribute termAtt = tokenStream.GetAttribute<ICharTermAttribute>();
                tokenStream.Reset();

                assertTrue(tokenStream.IncrementToken());
                assertEquals("SimpleAnalyzer lowercases", "qwerty", termAtt.ToString());
                assertFalse(tokenStream.IncrementToken());
                tokenStream.End();
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(tokenStream);
            }
        }

        [Test]
        public virtual void TestCharFilters()
        {
            Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
            AssertAnalyzesTo(a, "ab", new string[] { "aab" }, new int[] { 0 }, new int[] { 2 });

            // now wrap in PFAW
            PerFieldAnalyzerWrapper p = new PerFieldAnalyzerWrapper(a, new Dictionary<string, Analyzer>());

            AssertAnalyzesTo(p, "ab", new string[] { "aab" }, new int[] { 0 }, new int[] { 2 });
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestPerFieldAnalyzerWrapper outerInstance;

            public AnalyzerAnonymousInnerClassHelper(TestPerFieldAnalyzerWrapper outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new MockTokenizer(reader));
            }

            protected override TextReader InitReader(string fieldName, TextReader reader)
            {
                return new MockCharFilter(reader, 7);
            }
        }
    }
}