// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;

namespace Lucene.Net.Analysis.De
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
    /// Tests <seealso cref="GermanNormalizationFilter"/>
    /// </summary>
    public class TestGermanNormalizationFilter : BaseTokenStreamTestCase
    {
        private static readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            TokenStream stream = new GermanNormalizationFilter(tokenizer);
            return new TokenStreamComponents(tokenizer, stream);
        });

        /// <summary>
        /// Tests that a/o/u + e is equivalent to the umlaut form
        /// </summary>
        [Test]
        public virtual void TestBasicExamples()
        {
            CheckOneTerm(analyzer, "Schaltflächen", "Schaltflachen");
            CheckOneTerm(analyzer, "Schaltflaechen", "Schaltflachen");
        }

        /// <summary>
        /// Tests the specific heuristic that ue is not folded after a vowel or q.
        /// </summary>
        [Test]
        public virtual void TestUHeuristic()
        {
            CheckOneTerm(analyzer, "dauer", "dauer");
        }

        /// <summary>
        /// Tests german specific folding of sharp-s
        /// </summary>
        [Test]
        public virtual void TestSpecialFolding()
        {
            CheckOneTerm(analyzer, "weißbier", "weissbier");
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
                return new TokenStreamComponents(tokenizer, new GermanNormalizationFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}