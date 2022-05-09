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
    /// Factory for <see cref="OpenNLPLemmatizerFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_opennlp_lemma" class="solr.TextField" positionIncrementGap="100"
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.OpenNLPTokenizerFactory"
    ///                sentenceModel="filename"
    ///                tokenizerModel="filename"/&gt;
    ///     /&gt;
    ///     &lt;filter class="solr.OpenNLPLemmatizerFilterFactory"
    ///             dictionary="filename"
    ///             lemmatizerModel="filename"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class OpenNLPLemmatizerFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public const string DICTIONARY = "dictionary";
        public const string LEMMATIZER_MODEL = "lemmatizerModel";

        private readonly string dictionaryFile;
        private readonly string lemmatizerModelFile;

        public OpenNLPLemmatizerFilterFactory(IDictionary<string, string> args)
                  : base(args)
        {
            dictionaryFile = Get(args, DICTIONARY);
            lemmatizerModelFile = Get(args, LEMMATIZER_MODEL);

            if (dictionaryFile is null && lemmatizerModelFile is null)
            {
                throw new ArgumentException("Configuration Error: missing parameter: at least one of '"
                    + DICTIONARY + "' and '" + LEMMATIZER_MODEL + "' must be provided.");
            }

            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            try
            {
                NLPLemmatizerOp lemmatizerOp = OpenNLPOpsFactory.GetLemmatizer(dictionaryFile, lemmatizerModelFile);
                return new OpenNLPLemmatizerFilter(input, lemmatizerOp);
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            // register models in cache with file/resource names
            if (dictionaryFile != null)
            {
                OpenNLPOpsFactory.GetLemmatizerDictionary(dictionaryFile, loader);
            }
            if (lemmatizerModelFile != null)
            {
                OpenNLPOpsFactory.GetLemmatizerModel(lemmatizerModelFile, loader);
            }
        }
    }
}
