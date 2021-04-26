// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using AttributeFactory = Lucene.Net.Util.AttributeSource.AttributeFactory;

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
    /// Factory for <see cref="OpenNLPTokenizer"/>.
    /// <code>
    /// &lt;fieldType name="text_opennlp" class="solr.TextField" positionIncrementGap="100"
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.OpenNLPTokenizerFactory" sentenceModel="filename" tokenizerModel="filename"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    /// <since>7.3.0</since>
    public class OpenNLPTokenizerFactory : TokenizerFactory, IResourceLoaderAware
    {
        public const string SENTENCE_MODEL = "sentenceModel";
        public const string TOKENIZER_MODEL = "tokenizerModel";

        private readonly string sentenceModelFile;
        private readonly string tokenizerModelFile;

        public OpenNLPTokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
            sentenceModelFile = Require(args, SENTENCE_MODEL);
            tokenizerModelFile = Require(args, TOKENIZER_MODEL);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override Tokenizer Create(AttributeFactory factory, TextReader reader)
        {
            try
            {
                NLPSentenceDetectorOp sentenceOp = OpenNLPOpsFactory.GetSentenceDetector(sentenceModelFile);
                NLPTokenizerOp tokenizerOp = OpenNLPOpsFactory.GetTokenizer(tokenizerModelFile);
                return new OpenNLPTokenizer(factory, reader, sentenceOp, tokenizerOp);
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            // register models in cache with file/resource names
            if (sentenceModelFile != null)
            {
                OpenNLPOpsFactory.GetSentenceModel(sentenceModelFile, loader);
            }
            if (tokenizerModelFile != null)
            {
                OpenNLPOpsFactory.GetTokenizerModel(tokenizerModelFile, loader);
            }
        }
    }
}
