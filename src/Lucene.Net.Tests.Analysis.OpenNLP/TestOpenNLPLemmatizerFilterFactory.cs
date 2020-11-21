// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

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

    public class TestOpenNLPLemmatizerFilterFactory : BaseTokenStreamTestCase
    {
        private const String SENTENCE = "They sent him running in the evening.";
        private static readonly String[] SENTENCE_dict_punc = { "they", "send", "he", "run", "in", "the", "evening", "." };
        private static readonly String[] SENTENCE_maxent_punc = { "they", "send", "he", "runn", "in", "the", "evening", "." };
        private static readonly String[] SENTENCE_posTags = { "NNP", "VBD", "PRP", "VBG", "IN", "DT", "NN", "." };
        private static readonly String SENTENCES = "They sent him running in the evening. He did not come back.";
        private static readonly String[] SENTENCES_dict_punc
            = { "they", "send", "he", "run", "in", "the", "evening", ".", "he", "do", "not", "come", "back", "." };
        private static readonly String[] SENTENCES_maxent_punc
            = { "they", "send", "he", "runn", "in", "the", "evening", ".", "he", "do", "not", "come", "back", "." };
        private static readonly String[] SENTENCES_posTags
            = { "NNP", "VBD", "PRP", "VBG", "IN", "DT", "NN", ".", "PRP", "VBD", "RB", "VB", "RB", "." };

        private static readonly String SENTENCE_both = "Konstantin Kalashnitsov constantly caliphed.";
        private static readonly String[] SENTENCE_both_punc
            = { "konstantin", "kalashnitsov", "constantly", "caliph", "." };
        private static readonly String[] SENTENCE_both_posTags
            = { "IN", "JJ", "NN", "VBN", "." };

        private const String SENTENCES_both = "Konstantin Kalashnitsov constantly caliphed. Coreena could care, completely.";
        private static readonly String[] SENTENCES_both_punc
            = { "konstantin", "kalashnitsov", "constantly", "caliph", ".", "coreena", "could", "care", ",", "completely", "." };
        private static readonly String[] SENTENCES_both_posTags
            = { "IN", "JJ", "NN", "VBN", ".", "NNP", "VBN", "NN", ",", "NN", "." };

        private static readonly String[] SENTENCES_dict_keep_orig_punc
            = { "They", "they", "sent", "send", "him", "he", "running", "run", "in", "the", "evening", ".", "He", "he", "did", "do", "not", "come", "back", "." };
        private static readonly String[] SENTENCES_max_ent_keep_orig_punc
            = { "They", "they", "sent", "send", "him", "he", "running", "runn", "in", "the", "evening", ".", "He", "he", "did", "do", "not", "come", "back", "." };
        private static readonly String[] SENTENCES_keep_orig_posTags
            = { "NNP", "NNP", "VBD", "VBD", "PRP", "PRP", "VBG", "VBG", "IN", "DT", "NN", ".", "PRP", "PRP", "VBD", "VBD", "RB", "VB", "RB", "." };

        private static readonly String[] SENTENCES_both_keep_orig_punc
            = { "Konstantin", "konstantin", "Kalashnitsov", "kalashnitsov", "constantly", "caliphed", "caliph", ".", "Coreena", "coreena", "could", "care", ",", "completely", "." };
        private static readonly String[] SENTENCES_both_keep_orig_posTags
            = { "IN", "IN", "JJ", "JJ", "NN", "VBN", "VBN", ".", "NNP", "NNP", "VBN", "NN", ",", "NN", "." };


        private const String tokenizerModelFile = "en-test-tokenizer.bin";
        private const String sentenceModelFile = "en-test-sent.bin";
        private const String posTaggerModelFile = "en-test-pos-maxent.bin";
        private const String lemmatizerModelFile = "en-test-lemmatizer.bin";
        private const String lemmatizerDictFile = "en-test-lemmas.dict";

        [Test]
        public void Test1SentenceDictionaryOnly()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var loader = new ClasspathResourceLoader(GetType());

                var opennlpFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                opennlpFactory.Inform(loader);
                var opennlp = opennlpFactory.Create(reader);

                var opennlpPOSFilterFactory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                opennlpPOSFilterFactory.Inform(loader);
                var opennlpPOSFilter = opennlpPOSFilterFactory.Create(opennlp);

                var opennlpLemmatizerFilterFactory = new OpenNLPLemmatizerFilterFactory(new Dictionary<string, string> { { "dictionary", lemmatizerDictFile } });
                opennlpLemmatizerFilterFactory.Inform(loader);
                var opennlpLemmatizerFilter = opennlpLemmatizerFilterFactory.Create(opennlpPOSFilter);

                return new TokenStreamComponents(opennlp, opennlpLemmatizerFilter);
            });
            //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //.withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //.addTokenFilter("opennlpPOS", "posTaggerModel", "en-test-pos-maxent.bin")
            //.addTokenFilter("opennlplemmatizer", "dictionary", "en-test-lemmas.dict")
            //.build();
            AssertAnalyzesTo(analyzer, SENTENCE, SENTENCE_dict_punc, null, null,
                SENTENCE_posTags, null, null, true);
        }

        [Test]
        public void Test2SentencesDictionaryOnly()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var loader = new ClasspathResourceLoader(GetType());

                var opennlpFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                opennlpFactory.Inform(loader);
                var opennlp = opennlpFactory.Create(reader);

                var opennlpPOSFilterFactory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                opennlpPOSFilterFactory.Inform(loader);
                var opennlpPOSFilter = opennlpPOSFilterFactory.Create(opennlp);

                var opennlpLemmatizerFilterFactory = new OpenNLPLemmatizerFilterFactory(new Dictionary<string, string> { { "dictionary", lemmatizerDictFile } });
                opennlpLemmatizerFilterFactory.Inform(loader);
                var opennlpLemmatizerFilter = opennlpLemmatizerFilterFactory.Create(opennlpPOSFilter);

                return new TokenStreamComponents(opennlp, opennlpLemmatizerFilter);
            });
            //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //.withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //.addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
            //.addTokenFilter("opennlplemmatizer", "dictionary", lemmatizerDictFile)
            //.build();
            AssertAnalyzesTo(analyzer, SENTENCES, SENTENCES_dict_punc, null, null,
                SENTENCES_posTags, null, null, true);
        }

        [Test]
        public void Test1SentenceMaxEntOnly()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var loader = new ClasspathResourceLoader(GetType());

                var opennlpFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                opennlpFactory.Inform(loader);
                var opennlp = opennlpFactory.Create(reader);

                var opennlpPOSFilterFactory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                opennlpPOSFilterFactory.Inform(loader);
                var opennlpPOSFilter = opennlpPOSFilterFactory.Create(opennlp);

                var opennlpLemmatizerFilterFactory = new OpenNLPLemmatizerFilterFactory(new Dictionary<string, string> { { "lemmatizerModel", lemmatizerModelFile } });
                opennlpLemmatizerFilterFactory.Inform(loader);
                var opennlpLemmatizerFilter = opennlpLemmatizerFilterFactory.Create(opennlpPOSFilter);

                return new TokenStreamComponents(opennlp, opennlpLemmatizerFilter);
            });
            //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //    .withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //    .addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
            //    .addTokenFilter("opennlplemmatizer", "lemmatizerModel", lemmatizerModelFile)
            //    .build();
            AssertAnalyzesTo(analyzer, SENTENCE, SENTENCE_maxent_punc, null, null,
                SENTENCE_posTags, null, null, true);
        }

        [Test]
        public void Test2SentencesMaxEntOnly()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var loader = new ClasspathResourceLoader(GetType());

                var opennlpFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                opennlpFactory.Inform(loader);
                var opennlp = opennlpFactory.Create(reader);

                var opennlpPOSFilterFactory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                opennlpPOSFilterFactory.Inform(loader);
                var opennlpPOSFilter = opennlpPOSFilterFactory.Create(opennlp);

                var opennlpLemmatizerFilterFactory = new OpenNLPLemmatizerFilterFactory(new Dictionary<string, string> { { "lemmatizerModel", lemmatizerModelFile } });
                opennlpLemmatizerFilterFactory.Inform(loader);
                var opennlpLemmatizerFilter = opennlpLemmatizerFilterFactory.Create(opennlpPOSFilter);

                return new TokenStreamComponents(opennlp, opennlpLemmatizerFilter);
            });

            //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //    .withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //    .addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
            //    .addTokenFilter("OpenNLPLemmatizer", "lemmatizerModel", lemmatizerModelFile)
            //    .build();
            AssertAnalyzesTo(analyzer, SENTENCES, SENTENCES_maxent_punc, null, null,
                SENTENCES_posTags, null, null, true);
        }

        [Test]
        public void Test1SentenceDictionaryAndMaxEnt()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var loader = new ClasspathResourceLoader(GetType());

                var opennlpFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                opennlpFactory.Inform(loader);
                var opennlp = opennlpFactory.Create(reader);

                var opennlpPOSFilterFactory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                opennlpPOSFilterFactory.Inform(loader);
                var opennlpPOSFilter = opennlpPOSFilterFactory.Create(opennlp);

                var opennlpLemmatizerFilterFactory = new OpenNLPLemmatizerFilterFactory(new Dictionary<string, string> { { "dictionary", lemmatizerDictFile }, { "lemmatizerModel", lemmatizerModelFile } });
                opennlpLemmatizerFilterFactory.Inform(loader);
                var opennlpLemmatizerFilter = opennlpLemmatizerFilterFactory.Create(opennlpPOSFilter);

                return new TokenStreamComponents(opennlp, opennlpLemmatizerFilter);
            });
            //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //    .withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //    .addTokenFilter("opennlpPOS", "posTaggerModel", "en-test-pos-maxent.bin")
            //    .addTokenFilter("opennlplemmatizer", "dictionary", "en-test-lemmas.dict", "lemmatizerModel", lemmatizerModelFile)
            //    .build();
            AssertAnalyzesTo(analyzer, SENTENCE_both, SENTENCE_both_punc, null, null,
                SENTENCE_both_posTags, null, null, true);
        }

        [Test]
        public void Test2SentencesDictionaryAndMaxEnt()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var loader = new ClasspathResourceLoader(GetType());

                var opennlpFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                opennlpFactory.Inform(loader);
                var opennlp = opennlpFactory.Create(reader);

                var opennlpPOSFilterFactory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                opennlpPOSFilterFactory.Inform(loader);
                var opennlpPOSFilter = opennlpPOSFilterFactory.Create(opennlp);

                var opennlpLemmatizerFilterFactory = new OpenNLPLemmatizerFilterFactory(new Dictionary<string, string> { { "dictionary", lemmatizerDictFile }, { "lemmatizerModel", lemmatizerModelFile } });
                opennlpLemmatizerFilterFactory.Inform(loader);
                var opennlpLemmatizerFilter = opennlpLemmatizerFilterFactory.Create(opennlpPOSFilter);

                return new TokenStreamComponents(opennlp, opennlpLemmatizerFilter);
            });

            //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //    .withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //    .addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
            //    .addTokenFilter("opennlplemmatizer", "dictionary", lemmatizerDictFile, "lemmatizerModel", lemmatizerModelFile)
            //    .build();
            AssertAnalyzesTo(analyzer, SENTENCES_both, SENTENCES_both_punc, null, null,
                SENTENCES_both_posTags, null, null, true);
        }

        [Test]
        public void TestKeywordAttributeAwarenessDictionaryOnly()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var loader = new ClasspathResourceLoader(GetType());

                var opennlpFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                opennlpFactory.Inform(loader);
                var opennlp = opennlpFactory.Create(reader);

                var opennlpPOSFilterFactory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                opennlpPOSFilterFactory.Inform(loader);
                var opennlpPOSFilter = opennlpPOSFilterFactory.Create(opennlp);

                var keywordRepeatFilterFactory = new KeywordRepeatFilterFactory(new Dictionary<string, string>());
                var keywordRepeatFilter = keywordRepeatFilterFactory.Create(opennlpPOSFilter);

                var opennlpLemmatizerFilterFactory = new OpenNLPLemmatizerFilterFactory(new Dictionary<string, string> { { "dictionary", lemmatizerDictFile } });
                opennlpLemmatizerFilterFactory.Inform(loader);
                var opennlpLemmatizerFilter = opennlpLemmatizerFilterFactory.Create(keywordRepeatFilter);

                var removeDuplicatesTokenFilterFactory = new RemoveDuplicatesTokenFilterFactory(new Dictionary<string, string>());
                var removeDuplicatesTokenFilter = removeDuplicatesTokenFilterFactory.Create(opennlpLemmatizerFilter);

                return new TokenStreamComponents(opennlp, removeDuplicatesTokenFilter);
            });

            //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //    .withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //    .addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
            //    .addTokenFilter(KeywordRepeatFilterFactory.class)
            //    .addTokenFilter("opennlplemmatizer", "dictionary", lemmatizerDictFile)
            //    .addTokenFilter(RemoveDuplicatesTokenFilterFactory.class)
            //    .build();
            AssertAnalyzesTo(analyzer, SENTENCES, SENTENCES_dict_keep_orig_punc, null, null,
                SENTENCES_keep_orig_posTags, null, null, true);
        }

        [Test]
        public void TestKeywordAttributeAwarenessMaxEntOnly()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var loader = new ClasspathResourceLoader(GetType());

                var opennlpFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                opennlpFactory.Inform(loader);
                var opennlp = opennlpFactory.Create(reader);

                var opennlpPOSFilterFactory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                opennlpPOSFilterFactory.Inform(loader);
                var opennlpPOSFilter = opennlpPOSFilterFactory.Create(opennlp);

                var keywordRepeatFilterFactory = new KeywordRepeatFilterFactory(new Dictionary<string, string>());
                var keywordRepeatFilter = keywordRepeatFilterFactory.Create(opennlpPOSFilter);

                var opennlpLemmatizerFilterFactory = new OpenNLPLemmatizerFilterFactory(new Dictionary<string, string> { { "lemmatizerModel", lemmatizerModelFile } });
                opennlpLemmatizerFilterFactory.Inform(loader);
                var opennlpLemmatizerFilter = opennlpLemmatizerFilterFactory.Create(keywordRepeatFilter);

                var removeDuplicatesTokenFilterFactory = new RemoveDuplicatesTokenFilterFactory(new Dictionary<string, string>());
                var removeDuplicatesTokenFilter = removeDuplicatesTokenFilterFactory.Create(opennlpLemmatizerFilter);

                return new TokenStreamComponents(opennlp, removeDuplicatesTokenFilter);
            });
            //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //    .withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //    .addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
            //    .addTokenFilter(KeywordRepeatFilterFactory.class)
            //    .addTokenFilter("opennlplemmatizer", "lemmatizerModel", lemmatizerModelFile)
            //    .addTokenFilter(RemoveDuplicatesTokenFilterFactory.class)
            //    .build();
            AssertAnalyzesTo(analyzer, SENTENCES, SENTENCES_max_ent_keep_orig_punc, null, null,
                SENTENCES_keep_orig_posTags, null, null, true);
        }

        [Test]
        public void TestKeywordAttributeAwarenessDictionaryAndMaxEnt()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldname, reader) =>
            {
                var loader = new ClasspathResourceLoader(GetType());

                var opennlpFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
                opennlpFactory.Inform(loader);
                var opennlp = opennlpFactory.Create(reader);

                var opennlpPOSFilterFactory = new OpenNLPPOSFilterFactory(new Dictionary<string, string> { { "posTaggerModel", posTaggerModelFile } });
                opennlpPOSFilterFactory.Inform(loader);
                var opennlpPOSFilter = opennlpPOSFilterFactory.Create(opennlp);

                var keywordRepeatFilterFactory = new KeywordRepeatFilterFactory(new Dictionary<string, string>());
                var keywordRepeatFilter = keywordRepeatFilterFactory.Create(opennlpPOSFilter);

                var opennlpLemmatizerFilterFactory = new OpenNLPLemmatizerFilterFactory(new Dictionary<string, string> { { "dictionary", lemmatizerDictFile }, { "lemmatizerModel", lemmatizerModelFile } });
                opennlpLemmatizerFilterFactory.Inform(loader);
                var opennlpLemmatizerFilter = opennlpLemmatizerFilterFactory.Create(keywordRepeatFilter);

                var removeDuplicatesTokenFilterFactory = new RemoveDuplicatesTokenFilterFactory(new Dictionary<string, string>());
                var removeDuplicatesTokenFilter = removeDuplicatesTokenFilterFactory.Create(opennlpLemmatizerFilter);

                return new TokenStreamComponents(opennlp, removeDuplicatesTokenFilter);
            });
            //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
            //    .withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
            //    .addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
            //    .addTokenFilter(KeywordRepeatFilterFactory.class)
            //    .addTokenFilter("opennlplemmatizer", "dictionary", lemmatizerDictFile, "lemmatizerModel", lemmatizerModelFile)
            //    .addTokenFilter(RemoveDuplicatesTokenFilterFactory.class)
            //    .build();
            AssertAnalyzesTo(analyzer, SENTENCES_both, SENTENCES_both_keep_orig_punc, null, null,
                SENTENCES_both_keep_orig_posTags, null, null, true);
        }
    }
}
