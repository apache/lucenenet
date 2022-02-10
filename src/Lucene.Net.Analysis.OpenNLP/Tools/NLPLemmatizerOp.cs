// Lucene version compatibility level 8.2.0
using opennlp.tools.lemmatizer;
using System.Diagnostics;
using System.IO;

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
    /// Supply OpenNLP Lemmatizer tools.
    /// <para/>
    /// Both a dictionary-based lemmatizer and a MaxEnt lemmatizer are supported.
    /// If both are configured, the dictionary-based lemmatizer is tried first,
    /// and then the MaxEnt lemmatizer is consulted for out-of-vocabulary tokens.
    /// <para/>
    /// The MaxEnt implementation requires binary models from OpenNLP project on SourceForge.
    /// </summary>
    public class NLPLemmatizerOp
    {
        private readonly DictionaryLemmatizer dictionaryLemmatizer;
        private readonly LemmatizerME lemmatizerME;

        public NLPLemmatizerOp(Stream dictionary, LemmatizerModel lemmatizerModel)
        {
            Debug.Assert(dictionary != null || lemmatizerModel != null, "At least one parameter must be non-null");
            dictionaryLemmatizer = dictionary is null ? null : new DictionaryLemmatizer(new ikvm.io.InputStreamWrapper(dictionary));
            lemmatizerME = lemmatizerModel is null ? null : new LemmatizerME(lemmatizerModel);
        }

        public virtual string[] Lemmatize(string[] words, string[] postags)
        {
            string[] lemmas; // LUCENENET: IDE0059: Remove unnecessary value assignment
            string[] maxEntLemmas = null;
            if (dictionaryLemmatizer != null)
            {
                lemmas = dictionaryLemmatizer.lemmatize(words, postags);
                for (int i = 0; i < lemmas.Length; ++i)
                {
                    if (lemmas[i].Equals("O"))
                    {   // this word is not in the dictionary
                        if (lemmatizerME != null)
                        {  // fall back to the MaxEnt lemmatizer if it's enabled
                            if (maxEntLemmas is null)
                            {
                                maxEntLemmas = lemmatizerME.lemmatize(words, postags);
                            }
                            if ("_".Equals(maxEntLemmas[i]))
                            {
                                lemmas[i] = words[i];    // put back the original word if no lemma is found
                            }
                            else
                            {
                                lemmas[i] = maxEntLemmas[i];
                            }
                        }
                        else
                        {                     // there is no MaxEnt lemmatizer
                            lemmas[i] = words[i];      // put back the original word if no lemma is found
                        }
                    }
                }
            }
            else
            {                           // there is only a MaxEnt lemmatizer
                maxEntLemmas = lemmatizerME.lemmatize(words, postags);
                for (int i = 0; i < maxEntLemmas.Length; ++i)
                {
                    if ("_".Equals(maxEntLemmas[i]))
                    {
                        maxEntLemmas[i] = words[i];  // put back the original word if no lemma is found
                    }
                }
                lemmas = maxEntLemmas;
            }
            return lemmas;
        }
    }
}
