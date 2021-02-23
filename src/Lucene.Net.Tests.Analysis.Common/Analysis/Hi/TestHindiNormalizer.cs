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
    /// Test HindiNormalizer
    /// </summary>
    public class TestHindiNormalizer : BaseTokenStreamTestCase
    {
        /// <summary>
        /// Test some basic normalization, with an example from the paper.
        /// </summary>
        [Test]
        public virtual void TestBasics()
        {
            Check("अँगरेज़ी", "अंगरेजि");
            Check("अँगरेजी", "अंगरेजि");
            Check("अँग्रेज़ी", "अंगरेजि");
            Check("अँग्रेजी", "अंगरेजि");
            Check("अंगरेज़ी", "अंगरेजि");
            Check("अंगरेजी", "अंगरेजि");
            Check("अंग्रेज़ी", "अंगरेजि");
            Check("अंग्रेजी", "अंगरेजि");
        }

        [Test]
        public virtual void TestDecompositions()
        {
            // removing nukta dot
            Check("क़िताब", "किताब");
            Check("फ़र्ज़", "फरज");
            Check("क़र्ज़", "करज");
            // some other composed nukta forms
            Check("ऱऴख़ग़ड़ढ़य़", "रळखगडढय");
            // removal of format (ZWJ/ZWNJ)
            Check("शार्‍मा", "शारमा");
            Check("शार्‌मा", "शारमा");
            // removal of chandra
            Check("ॅॆॉॊऍऎऑऒ\u0972", "ेेोोएएओओअ");
            // vowel shortening
            Check("आईऊॠॡऐऔीूॄॣैौ", "अइउऋऌएओिुृॢेो");
        }
        
        private void Check(string input, string output)
        {
            Tokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            TokenFilter tf = new HindiNormalizationFilter(tokenizer);
            AssertTokenStreamContents(tf, new string[] { output });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new HindiNormalizationFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}