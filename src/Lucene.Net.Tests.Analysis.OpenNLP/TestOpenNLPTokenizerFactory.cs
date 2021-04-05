// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.OpenNlp
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
    /// Tests the Tokenizer as well- the Tokenizer needs the OpenNLP model files,
    /// which this can load from src/test-files/opennlp/solr/conf
    /// </summary>
    public class TestOpenNLPTokenizerFactory : BaseTokenStreamTestCase
    {
        private const String SENTENCES = "Sentence number 1 has 6 words. Sentence number 2, 5 words.";
        private static String[] SENTENCES_punc = { "Sentence", "number", "1", "has", "6", "words", ".", "Sentence", "number", "2", ",", "5", "words", "." };
        private static int[] SENTENCES_startOffsets = { 0, 9, 16, 18, 22, 24, 29, 31, 40, 47, 48, 50, 52, 57 };
        private static int[] SENTENCES_endOffsets = { 8, 15, 17, 21, 23, 29, 30, 39, 46, 48, 49, 51, 57, 58 };

        private const String SENTENCE1 = "Sentence number 1 has 6 words.";
        private static String[] SENTENCE1_punc = { "Sentence", "number", "1", "has", "6", "words", "." };

        [Test]
        public void TestTokenizer()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var tokenizerFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "sentenceModel", "en-test-sent.bin" }, { "tokenizerModel", "en-test-tokenizer.bin" } });
                var tokenizer = tokenizerFactory.Create(reader);
                return new TokenStreamComponents(tokenizer);
            });
            //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //    .withTokenizer("opennlp", "sentenceModel", "en-test-sent.bin", "tokenizerModel", "en-test-tokenizer.bin")
            //    .build();
            AssertAnalyzesTo(analyzer, SENTENCES, SENTENCES_punc, SENTENCES_startOffsets, SENTENCES_endOffsets);
            AssertAnalyzesTo(analyzer, SENTENCE1, SENTENCE1_punc);
        }

        [Test]
        public void TestTokenizerNoSentenceDetector()
        {
            var expected = Assert.Throws<ArgumentException>(() =>
            {
                Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
                {
                    var tokenizerFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", "en-test-tokenizer.bin" } });
                    var tokenizer = tokenizerFactory.Create(reader);
                    return new TokenStreamComponents(tokenizer);
                });
                analyzer.GetTokenStream("", "");
            });

            //        IllegalArgumentException expected = expectThrows(IllegalArgumentException.class, () -> {
            //          CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //              .withTokenizer("opennlp", "tokenizerModel", "en-test-tokenizer.bin")
            //              .build();
            //});
            assertTrue(expected.Message.Contains("Configuration Error: missing parameter 'sentenceModel'"));
        }

        [Test]
        public void TestTokenizerNoTokenizer()
        {
            //Analyzer analyzer2 = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            //{
            //    var tokenizerFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "sentenceModel", "en-test-sent.bin" } });
            //    tokenizerFactory.Inform(new ClasspathResourceLoader(GetType()));
            //    var tokenizer = tokenizerFactory.Create(reader);
            //    return new TokenStreamComponents(tokenizer);
            //});
            //analyzer2.GetTokenStream("", "");

            var expected = Assert.Throws<ArgumentException>(() =>
            {
                Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
                {
                    var tokenizerFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "sentenceModel", "en-test-sent.bin" } });
                    var tokenizer = tokenizerFactory.Create(reader);
                    return new TokenStreamComponents(tokenizer);
                });
                analyzer.GetTokenStream("", "");
            });

            //        IllegalArgumentException expected = expectThrows(ArgumentException.class, () -> {
            //          CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //              .withTokenizer("opennlp", "sentenceModel", "en-test-sent.bin")
            //              .build();
            //});
            assertTrue(expected.Message.Contains("Configuration Error: missing parameter 'tokenizerModel'"));
        }

        // test analyzer caching the tokenizer
        [Test]
        public void TestClose()
        {
            IDictionary<String, String> args = new Dictionary<String, String>()
            {
                { "sentenceModel", "en-test-sent.bin" },
                { "tokenizerModel", "en-test-tokenizer.bin" }
            };
            OpenNLPTokenizerFactory factory = new OpenNLPTokenizerFactory(args);
            factory.Inform(new ClasspathResourceLoader(GetType()));

            Tokenizer ts = factory.Create(NewAttributeFactory(), new StringReader(SENTENCES));
            //ts.SetReader(new StringReader(SENTENCES));

            ts.Reset();
            ts.Dispose();
            ts.Reset();
            ts.SetReader(new StringReader(SENTENCES));
            AssertTokenStreamContents(ts, SENTENCES_punc);
            ts.Dispose();
            ts.Reset();
            ts.SetReader(new StringReader(SENTENCES));
            AssertTokenStreamContents(ts, SENTENCES_punc);
        }
    }
}
