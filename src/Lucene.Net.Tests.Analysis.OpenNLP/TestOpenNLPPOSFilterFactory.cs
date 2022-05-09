// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Payloads;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    /// Needs the OpenNLP Tokenizer because it creates full streams of punctuation.
    /// The POS model is based on this tokenization.
    /// 
    /// <para/>Tagging models are created from tiny test data in opennlp/tools/test-model-data/ and are not very accurate.
    /// </summary>
    public class TestOpenNLPPOSFilterFactory : BaseTokenStreamTestCase
    {
        private const String SENTENCES = "Sentence number 1 has 6 words. Sentence number 2, 5 words.";
        private static readonly String[] SENTENCES_punc
            = { "Sentence", "number", "1", "has", "6", "words", ".", "Sentence", "number", "2", ",", "5", "words", "." };
        private static readonly int[] SENTENCES_startOffsets = { 0, 9, 16, 18, 22, 24, 29, 31, 40, 47, 48, 50, 52, 57 };
        private static readonly int[] SENTENCES_endOffsets = { 8, 15, 17, 21, 23, 29, 30, 39, 46, 48, 49, 51, 57, 58 };
        private static readonly String[] SENTENCES_posTags
            = { "NN", "NN", "CD", "VBZ", "CD", "NNS", ".", "NN", "NN", "CD", ",", "CD", "NNS", "." };

        private const String NO_BREAK = "No period";
        private static readonly String[] NO_BREAK_terms = { "No", "period" };
        private static readonly int[] NO_BREAK_startOffsets = { 0, 3 };
        private static readonly int[] NO_BREAK_endOffsets = { 2, 9 };

        private const String sentenceModelFile = "en-test-sent.bin";
        private const String tokenizerModelFile = "en-test-tokenizer.bin";
        private const String posTaggerModelFile = "en-test-pos-maxent.bin";


        private static byte[][] ToPayloads(params string[] strings)
        {
            return strings.Select(s => s is null ? null : Encoding.UTF8.GetBytes(s)).ToArray();
        }

        //    private static byte[][] ToPayloads(params String[] strings)
        //    {
        //        return Arrays.stream(strings).map(s->s is null ? null : s.getBytes(StandardCharsets.UTF_8)).toArray(byte[][]::new);
        //    }

        [Test]
        public void TestBasic()
        {
            var loader = new ClasspathResourceLoader(GetType());
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) => {
                var tokenizerFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                tokenizerFactory.Inform(loader);
                var tokenizer = tokenizerFactory.Create(reader);

                var filter1Factory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                filter1Factory.Inform(loader);
                var filter1 = filter1Factory.Create(tokenizer);

                return new TokenStreamComponents(tokenizer, filter1);
            });
        //    CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(GetType()))
        //.withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
        //.addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
        //.build();
            AssertAnalyzesTo(analyzer, SENTENCES, SENTENCES_punc, SENTENCES_startOffsets, SENTENCES_endOffsets);
        }

        [Test]
        public void TestPOS()
        {
            var loader = new ClasspathResourceLoader(GetType());
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var tokenizerFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                tokenizerFactory.Inform(loader);
                var tokenizer = tokenizerFactory.Create(reader);

                var filter1Factory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                filter1Factory.Inform(loader);
                var filter1 = filter1Factory.Create(tokenizer);

                return new TokenStreamComponents(tokenizer, filter1);
            });
            //    CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(GetType()))
            //    .withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //    .addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
            //    .build();
            AssertAnalyzesTo(analyzer, SENTENCES, SENTENCES_punc, SENTENCES_startOffsets, SENTENCES_endOffsets,
                SENTENCES_posTags, null, null, true);

            analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var tokenizerFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                tokenizerFactory.Inform(loader);
                var tokenizer = tokenizerFactory.Create(reader);

                var filter1Factory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                filter1Factory.Inform(loader);
                var filter1 = filter1Factory.Create(tokenizer);

                var filter2Factory = new TypeAsPayloadTokenFilterFactory(new Dictionary<string, string>());
                var filter2 = filter2Factory.Create(filter1);

                return new TokenStreamComponents(tokenizer, filter2);
            });
            //analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(GetType()))
            //    .withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //    .addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
            //    .addTokenFilter(TypeAsPayloadTokenFilterFactory.class)
            //.build();
            AssertAnalyzesTo(analyzer, SENTENCES, SENTENCES_punc, SENTENCES_startOffsets, SENTENCES_endOffsets,
                null, null, null, true, ToPayloads(SENTENCES_posTags));
        }

        [Test]
        public void TestNoBreak()
        {
            var analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var loader = new ClasspathResourceLoader(GetType());

                var tokenizerFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                tokenizerFactory.Inform(loader);
                var tokenizer = tokenizerFactory.Create(reader);
                
                var tokenFilterFactory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                tokenFilterFactory.Inform(loader);
                var tokenFilter = tokenFilterFactory.Create(tokenizer);
                
                return new TokenStreamComponents(tokenizer, tokenFilter);
            });

            //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(GetType()))
            //    .withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //    .addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
            //    .build();
            AssertAnalyzesTo(analyzer, NO_BREAK, NO_BREAK_terms, NO_BREAK_startOffsets, NO_BREAK_endOffsets,
                null, null, null, true);
        }
    }
}
