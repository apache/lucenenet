using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Reverse;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Classification
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

    /**
     * Testcase for <see cref="SimpleNaiveBayesClassifier"/>
     */
    // TODO : eventually remove this if / when fallback methods exist for all un-supportable codec methods (see LUCENE-4872)
    [SuppressCodecs("Lucene3x")]
    public class SimpleNaiveBayesClassifierTest : ClassificationTestBase<BytesRef>
    {
        [Test]
        public void TestBasicUsage()
        {
            CheckCorrectClassification(new SimpleNaiveBayesClassifier(), TECHNOLOGY_INPUT, TECHNOLOGY_RESULT, new MockAnalyzer(Random), textFieldName, categoryFieldName);
            CheckCorrectClassification(new SimpleNaiveBayesClassifier(), POLITICS_INPUT, POLITICS_RESULT, new MockAnalyzer(Random), textFieldName, categoryFieldName);
        }

        [Test]
        public void TestBasicUsageWithQuery()
        {
            CheckCorrectClassification(new SimpleNaiveBayesClassifier(), TECHNOLOGY_INPUT, TECHNOLOGY_RESULT, new MockAnalyzer(Random), textFieldName, categoryFieldName, new TermQuery(new Term(textFieldName, "it")));
        }

        [Test]
        public void TestNGramUsage()
        {
            CheckCorrectClassification(new SimpleNaiveBayesClassifier(), TECHNOLOGY_INPUT, TECHNOLOGY_RESULT, new NGramAnalyzer(), textFieldName, categoryFieldName);
        }

        private class NGramAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new ReverseStringFilter(TEST_VERSION_CURRENT, new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, new ReverseStringFilter(TEST_VERSION_CURRENT, tokenizer), 10, 20)));
            }
        }

        [Test]
        public void TestPerformance()
        {
            CheckPerformance(new SimpleNaiveBayesClassifier(), new MockAnalyzer(Random), categoryFieldName);
        }
    }
}