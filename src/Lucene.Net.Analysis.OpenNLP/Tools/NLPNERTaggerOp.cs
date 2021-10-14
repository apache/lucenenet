// Lucene version compatibility level 8.2.0
using Lucene.Net.Support.Threading;
using opennlp.tools.namefind;
using opennlp.tools.util;

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
    /// Supply OpenNLP Named Entity Resolution tool
    /// Requires binary models from OpenNLP project on SourceForge.
    /// <para/>
    /// Usage: from <a href="http://opennlp.apache.org/docs/1.8.3/manual/opennlp.html#tools.namefind.recognition.api">
    /// the OpenNLP documentation</a>:
    /// <para/>
    /// "The NameFinderME class is not thread safe, it must only be called from one thread.
    /// To use multiple threads multiple NameFinderME instances sharing the same model instance
    /// can be created. The input text should be segmented into documents, sentences and tokens.
    /// To perform entity detection an application calls the find method for every sentence in
    /// the document. After every document clearAdaptiveData must be called to clear the adaptive
    /// data in the feature generators. Not calling clearAdaptiveData can lead to a sharp drop
    /// in the detection rate after a few documents."
    /// </summary>
    public class NLPNERTaggerOp
    {
        private readonly TokenNameFinder nameFinder;

        public NLPNERTaggerOp(TokenNameFinderModel model)
        {
            this.nameFinder = new NameFinderME(model);
        }

        public virtual Span[] GetNames(string[] words)
        {
            Span[] names = nameFinder.find(words);
            return names;
        }

        public virtual void Reset()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                nameFinder.clearAdaptiveData();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }
}
