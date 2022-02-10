// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Util;
using opennlp.tools.chunker;
using opennlp.tools.lemmatizer;
using opennlp.tools.namefind;
using opennlp.tools.postag;
using opennlp.tools.sentdetect;
using opennlp.tools.tokenize;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.OpenNlp.Tools
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
    /// Supply OpenNLP Named Entity Recognizer.
    /// Cache model file objects. Assumes model files are thread-safe.
    /// </summary>
    public static class OpenNLPOpsFactory // LUCENENET: Made static because all members are static
    {
        private static readonly ConcurrentDictionary<string, SentenceModel> sentenceModels = new ConcurrentDictionary<string, SentenceModel>();
        private static readonly ConcurrentDictionary<string, TokenizerModel> tokenizerModels = new ConcurrentDictionary<string, TokenizerModel>();
        private static readonly ConcurrentDictionary<string, POSModel> posTaggerModels = new ConcurrentDictionary<string, POSModel>();
        private static readonly ConcurrentDictionary<string, ChunkerModel> chunkerModels = new ConcurrentDictionary<string, ChunkerModel>();
        private static readonly ConcurrentDictionary<string, TokenNameFinderModel> nerModels = new ConcurrentDictionary<string, TokenNameFinderModel>();
        private static readonly ConcurrentDictionary<string, LemmatizerModel> lemmatizerModels = new ConcurrentDictionary<string, LemmatizerModel>();
        private static readonly ConcurrentDictionary<string, string> lemmaDictionaries = new ConcurrentDictionary<string, string>();

        public static NLPSentenceDetectorOp GetSentenceDetector(string modelName)
        {
            if (modelName != null)
            {
                sentenceModels.TryGetValue(modelName, out SentenceModel model);
                return new NLPSentenceDetectorOp(model);
            }
            else
            {
                return new NLPSentenceDetectorOp();
            }
        }

        public static SentenceModel GetSentenceModel(string modelName, IResourceLoader loader)
        {
            // LUCENENET: Two competing threads in the add operation is okay as per the original implementation
            return sentenceModels.GetOrAdd(modelName, (modelName) =>
            {
                using Stream resource = loader.OpenResource(modelName);
                return new SentenceModel(new ikvm.io.InputStreamWrapper(resource));
            });
        }

        public static NLPTokenizerOp GetTokenizer(string modelName)
        {
            if (modelName is null)
            {
                return new NLPTokenizerOp();
            }
            else
            {
                TokenizerModel model = tokenizerModels[modelName];
                return new NLPTokenizerOp(model);
            }
        }

        public static TokenizerModel GetTokenizerModel(string modelName, IResourceLoader loader)
        {
            // LUCENENET: Two competing threads in the add operation is okay as per the original implementation
            return tokenizerModels.GetOrAdd(modelName, (modelName) =>
            {
                using Stream resource = loader.OpenResource(modelName);
                return new TokenizerModel(new ikvm.io.InputStreamWrapper(resource));
            });
        }

        public static NLPPOSTaggerOp GetPOSTagger(string modelName)
        {
            posTaggerModels.TryGetValue(modelName, out POSModel model);
            return new NLPPOSTaggerOp(model);
        }

        public static POSModel GetPOSTaggerModel(string modelName, IResourceLoader loader)
        {
            // LUCENENET: Two competing threads in the add operation is okay as per the original implementation
            return posTaggerModels.GetOrAdd(modelName, (modelName) =>
            {
                using Stream resource = loader.OpenResource(modelName);
                return new POSModel(new ikvm.io.InputStreamWrapper(resource));
            });
        }

        public static NLPChunkerOp GetChunker(string modelName)
        {
            chunkerModels.TryGetValue(modelName, out ChunkerModel model);
            return new NLPChunkerOp(model);
        }

        public static ChunkerModel GetChunkerModel(string modelName, IResourceLoader loader)
        {
            // LUCENENET: Two competing threads in the add operation is okay as per the original implementation
            return chunkerModels.GetOrAdd(modelName, (modelName) =>
            {
                using Stream resource = loader.OpenResource(modelName);
                return new ChunkerModel(new ikvm.io.InputStreamWrapper(resource));
            });
        }

        public static NLPNERTaggerOp GetNERTagger(string modelName)
        {
            nerModels.TryGetValue(modelName, out TokenNameFinderModel model);
            return new NLPNERTaggerOp(model);
        }

        public static TokenNameFinderModel GetNERTaggerModel(string modelName, IResourceLoader loader)
        {
            // LUCENENET: Two competing threads in the add operation is okay as per the original implementation
            return nerModels.GetOrAdd(modelName, (modelName) =>
            {
                using Stream resource = loader.OpenResource(modelName);
                return new TokenNameFinderModel(new ikvm.io.InputStreamWrapper(resource));
            });
        }

        public static NLPLemmatizerOp GetLemmatizer(string dictionaryFile, string lemmatizerModelFile)
        {
            Debug.Assert(dictionaryFile != null || lemmatizerModelFile != null, "At least one parameter must be non-null");
            Stream dictionaryInputStream = null;
            if (dictionaryFile != null)
            {
                string dictionary = lemmaDictionaries[dictionaryFile];
                dictionaryInputStream = new MemoryStream(Encoding.UTF8.GetBytes(dictionary));
            }
            LemmatizerModel lemmatizerModel = lemmatizerModelFile is null ? null : lemmatizerModels[lemmatizerModelFile];
            return new NLPLemmatizerOp(dictionaryInputStream, lemmatizerModel);
        }

        public static string GetLemmatizerDictionary(string dictionaryFile, IResourceLoader loader)
        {
            // LUCENENET: Two competing threads in the add operation is okay as per the original implementation
            return lemmaDictionaries.GetOrAdd(dictionaryFile, (dictionaryFile) =>
            {
                using TextReader reader = new StreamReader(loader.OpenResource(dictionaryFile), Encoding.UTF8);
                StringBuilder builder = new StringBuilder();
                char[] chars = new char[8092];
                int numRead = 0;
                do
                {
                    numRead = reader.Read(chars, 0, chars.Length);
                    if (numRead > 0)
                    {
                        builder.Append(chars, 0, numRead);
                    }
                } while (numRead > 0);
                return builder.ToString();
            });
        }

        public static LemmatizerModel GetLemmatizerModel(string modelName, IResourceLoader loader)
        {
            // LUCENENET: Two competing threads in the add operation is okay as per the original implementation
            return lemmatizerModels.GetOrAdd(modelName, (modelName) =>
            {
                using Stream resource = loader.OpenResource(modelName);
                return new LemmatizerModel(new ikvm.io.InputStreamWrapper(resource));
            });
        }

        // keeps unit test from blowing out memory
        public static void ClearModels()
        {
            sentenceModels.Clear();
            tokenizerModels.Clear();
            posTaggerModels.Clear();
            chunkerModels.Clear();
            nerModels.Clear();
            lemmaDictionaries.Clear();
        }
    }
}
