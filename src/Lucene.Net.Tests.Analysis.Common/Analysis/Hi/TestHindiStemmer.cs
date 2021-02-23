// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Hi
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
    /// Test HindiStemmer
    /// </summary>
    public class TestHindiStemmer : BaseTokenStreamTestCase
    {
        /// <summary>
        /// Test masc noun inflections
        /// </summary>
        [Test]
        public virtual void TestMasculineNouns()
        {
            Check("लडका", "लडक");
            Check("लडके", "लडक");
            Check("लडकों", "लडक");

            Check("गुरु", "गुर");
            Check("गुरुओं", "गुर");

            Check("दोस्त", "दोस्त");
            Check("दोस्तों", "दोस्त");
        }

        /// <summary>
        /// Test feminine noun inflections
        /// </summary>
        [Test]
        public virtual void TestFeminineNouns()
        {
            Check("लडकी", "लडक");
            Check("लडकियों", "लडक");

            Check("किताब", "किताब");
            Check("किताबें", "किताब");
            Check("किताबों", "किताब");

            Check("आध्यापीका", "आध्यापीक");
            Check("आध्यापीकाएं", "आध्यापीक");
            Check("आध्यापीकाओं", "आध्यापीक");
        }

        /// <summary>
        /// Test some verb forms
        /// </summary>
        [Test]
        public virtual void TestVerbs()
        {
            Check("खाना", "खा");
            Check("खाता", "खा");
            Check("खाती", "खा");
            Check("खा", "खा");
        }

        /// <summary>
        /// From the paper: since the suffix list for verbs includes AI, awA and anI,
        /// additional suffixes had to be added to the list for noun/adjectives
        /// ending with these endings.
        /// </summary>
        [Test]
        public virtual void TestExceptions()
        {
            Check("कठिनाइयां", "कठिन");
            Check("कठिन", "कठिन");
        }

        private void Check(string input, string output)
        {
            Tokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            TokenFilter tf = new HindiStemFilter(tokenizer);
            AssertTokenStreamContents(tf, new string[] { output });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new HindiStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}