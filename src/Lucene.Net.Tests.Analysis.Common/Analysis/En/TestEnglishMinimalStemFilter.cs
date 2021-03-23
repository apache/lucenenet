// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.En
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
    /// Simple tests for <seealso cref="EnglishMinimalStemFilter"/>
    /// </summary>
    public class TestEnglishMinimalStemFilter : BaseTokenStreamTestCase
    {
        private static readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            return new TokenStreamComponents(source, new EnglishMinimalStemFilter(source));
        });

        /// <summary>
        /// Test some examples from various papers about this technique </summary>
        [Test]
        public virtual void TestExamples()
        {
            CheckOneTerm(analyzer, "queries", "query");
            CheckOneTerm(analyzer, "phrases", "phrase");
            CheckOneTerm(analyzer, "corpus", "corpus");
            CheckOneTerm(analyzer, "stress", "stress");
            CheckOneTerm(analyzer, "kings", "king");
            CheckOneTerm(analyzer, "panels", "panel");
            CheckOneTerm(analyzer, "aerodynamics", "aerodynamic");
            CheckOneTerm(analyzer, "congress", "congress");
            CheckOneTerm(analyzer, "serious", "serious");
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, analyzer, 1000 * RandomMultiplier);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new EnglishMinimalStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}