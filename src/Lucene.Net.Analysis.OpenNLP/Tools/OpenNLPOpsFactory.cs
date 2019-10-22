using Lucene.Net.Analysis.Util;
using opennlp.tools.chunker;
using opennlp.tools.lemmatizer;
using opennlp.tools.namefind;
using opennlp.tools.postag;
using opennlp.tools.sentdetect;
using opennlp.tools.tokenize;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private static readonly IDictionary<string, SentenceModel> sentenceModels = new ConcurrentDictionary<string, SentenceModel>();
        private static readonly ConcurrentDictionary<string, TokenizerModel> tokenizerModels = new ConcurrentDictionary<string, TokenizerModel>();
        private static readonly ConcurrentDictionary<string, POSModel> posTaggerModels = new ConcurrentDictionary<string, POSModel>();
        private static readonly ConcurrentDictionary<string, ChunkerModel> chunkerModels = new ConcurrentDictionary<string, ChunkerModel>();
        private static readonly IDictionary<string, TokenNameFinderModel> nerModels = new ConcurrentDictionary<string, TokenNameFinderModel>();
        private static readonly IDictionary<string, LemmatizerModel> lemmatizerModels = new ConcurrentDictionary<string, LemmatizerModel>();
        private static readonly IDictionary<string, string> lemmaDictionaries = new ConcurrentDictionary<string, string>();

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
            //SentenceModel model = sentenceModels.get(modelName);
            sentenceModels.TryGetValue(modelName, out SentenceModel model);
            if (model == null)
            {
                using (Stream resource = loader.OpenResource(modelName))
                {
                    model = new SentenceModel(new ikvm.io.InputStreamWrapper(resource));
                }
                sentenceModels[modelName] = model;
            }
            return model;
        }

        public static NLPTokenizerOp GetTokenizer(string modelName)
        {
            if (modelName == null)
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
            tokenizerModels.TryGetValue(modelName, out TokenizerModel model);
            if (model == null)
            {
                using (Stream resource = loader.OpenResource(modelName))
                {
                    model = new TokenizerModel(new ikvm.io.InputStreamWrapper(resource));
                }
                tokenizerModels[modelName] = model;
            }
            return model;
        }

        public static NLPPOSTaggerOp GetPOSTagger(string modelName)
        {
            posTaggerModels.TryGetValue(modelName, out POSModel model);
            return new NLPPOSTaggerOp(model);
        }

        public static POSModel GetPOSTaggerModel(string modelName, IResourceLoader loader)
        {
            posTaggerModels.TryGetValue(modelName, out POSModel model);
            if (model == null)
            {
                using (Stream resource = loader.OpenResource(modelName))
                {
                    model = new POSModel(new ikvm.io.InputStreamWrapper(resource));
                }
                posTaggerModels[modelName] = model;
            }
            return model;
        }

        public static NLPChunkerOp GetChunker(string modelName)
        {
            chunkerModels.TryGetValue(modelName, out ChunkerModel model);
            return new NLPChunkerOp(model);
        }

        public static ChunkerModel GetChunkerModel(string modelName, IResourceLoader loader)
        {
            chunkerModels.TryGetValue(modelName, out ChunkerModel model);
            if (model == null)
            {
                using (Stream resource = loader.OpenResource(modelName))
                {
                    model = new ChunkerModel(new ikvm.io.InputStreamWrapper(resource));
                }
                chunkerModels[modelName] = model;
            }
            return model;
        }

        public static NLPNERTaggerOp GetNERTagger(string modelName)
        {
            nerModels.TryGetValue(modelName, out TokenNameFinderModel model);
            return new NLPNERTaggerOp(model);
        }

        public static TokenNameFinderModel GetNERTaggerModel(string modelName, IResourceLoader loader)
        {
            nerModels.TryGetValue(modelName, out TokenNameFinderModel model);
            if (model == null)
            {
                using (Stream resource = loader.OpenResource(modelName))
                {
                    model = new TokenNameFinderModel(new ikvm.io.InputStreamWrapper(resource));
                }
                nerModels[modelName] = model;
            }
            return model;
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
            LemmatizerModel lemmatizerModel = lemmatizerModelFile == null ? null : lemmatizerModels[lemmatizerModelFile];
            return new NLPLemmatizerOp(dictionaryInputStream, lemmatizerModel);
        }

        public static string GetLemmatizerDictionary(string dictionaryFile, IResourceLoader loader)
        {
            lemmaDictionaries.TryGetValue(dictionaryFile, out string dictionary);
            if (dictionary == null)
            {
                using (TextReader reader = new StreamReader(loader.OpenResource(dictionaryFile), Encoding.UTF8))
                {
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
                    dictionary = builder.ToString();
                    lemmaDictionaries[dictionaryFile] = dictionary;
                }
            }
            return dictionary;
        }

        public static LemmatizerModel GetLemmatizerModel(string modelName, IResourceLoader loader)
        {
            lemmatizerModels.TryGetValue(modelName, out LemmatizerModel model);
            if (model == null)
            {
                using (Stream resource = loader.OpenResource(modelName))
                {
                    model = new LemmatizerModel(new ikvm.io.InputStreamWrapper(resource));
                }
                lemmatizerModels[modelName] = model;
            }
            return model;
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
