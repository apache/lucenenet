// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.Util;
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
    /// Factory for <see cref="OpenNLPChunkerFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_opennlp_chunked" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.OpenNLPTokenizerFactory" sentenceModel="filename" tokenizerModel="filename"/&gt;
    ///     &lt;filter class="solr.OpenNLPPOSFilterFactory" posTaggerModel="filename"/&gt;
    ///     &lt;filter class="solr.OpenNLPChunkerFilterFactory" chunkerModel="filename"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    /// <since>7.3.0</since>
    public class OpenNLPChunkerFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public const string CHUNKER_MODEL = "chunkerModel";

        private readonly string chunkerModelFile;

        public OpenNLPChunkerFilterFactory(IDictionary<string, string> args)
                  : base(args)
        {
            chunkerModelFile = Get(args, CHUNKER_MODEL);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            try
            {
                NLPChunkerOp chunkerOp = null;

                if (chunkerModelFile != null)
                {
                    chunkerOp = OpenNLPOpsFactory.GetChunker(chunkerModelFile);
                }
                return new OpenNLPChunkerFilter(input, chunkerOp);
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw new ArgumentException(e.ToString(), e);
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            try
            {
                // load and register read-only models in cache with file/resource names
                if (chunkerModelFile != null)
                {
                    OpenNLPOpsFactory.GetChunkerModel(chunkerModelFile, loader);
                }
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw new ArgumentException(e.ToString(), e);
            }
        }
    }
}
