// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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

            TokenStream tokenStream = analyzer.GetTokenStream("field", text);
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
                IOUtils.DisposeWhileHandlingException(tokenStream);
            }

            tokenStream = analyzer.GetTokenStream("special", text);
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
                IOUtils.DisposeWhileHandlingException(tokenStream);
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestLUCENENET615()
        {
            var english = new EnglishAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);

            var whitespace = new WhitespaceAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);

            var pf = new PerFieldAnalyzerWrapper(english, new JCG.Dictionary<string, Analyzer>() { { "foo", whitespace } });

            var test1 = english.GetTokenStream(null, "test"); // Does not throw

            var test2 = pf.GetTokenStream("", "test"); // works

            Assert.DoesNotThrow(() => pf.GetTokenStream(null, "test"), "GetTokenStream should not throw NullReferenceException with a null key");
        }

        [Test]
        public virtual void TestCharFilters()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                return new TokenStreamComponents(new MockTokenizer(reader));
            }, initReader: (fieldName, reader) => new MockCharFilter(reader, 7));
            AssertAnalyzesTo(a, "ab", new string[] { "aab" }, new int[] { 0 }, new int[] { 2 });

            // now wrap in PFAW
            PerFieldAnalyzerWrapper p = new PerFieldAnalyzerWrapper(a, Collections.EmptyMap<string, Analyzer>());

            AssertAnalyzesTo(p, "ab", new string[] { "aab" }, new int[] { 0 }, new int[] { 2 });
        }
    }
}