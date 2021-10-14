// Lucene version compatibility level 8.2.0
using Lucene.Net.Support.Threading;
using opennlp.tools.postag;

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
    /// Supply OpenNLP Parts-Of-Speech Tagging tool.
    /// Requires binary models from OpenNLP project on SourceForge.
    /// </summary>
    public class NLPPOSTaggerOp
    {
        private readonly POSTagger tagger = null;

        public NLPPOSTaggerOp(POSModel model)
        {
            tagger = new POSTaggerME(model);
        }

        public virtual string[] GetPOSTags(string[] words)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                return tagger.tag(words);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }
}
