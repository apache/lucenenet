// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Fr;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Util
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
    /// Factory for <see cref="ElisionFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_elsn" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.LowerCaseFilterFactory"/&gt;
    ///     &lt;filter class="solr.ElisionFilterFactory" 
    ///       articles="stopwordarticles.txt" ignoreCase="true"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    public class ElisionFilterFactory : TokenFilterFactory, IResourceLoaderAware, IMultiTermAwareComponent
    {
        private readonly string articlesFile;
        private readonly bool ignoreCase;
        private CharArraySet articles;

        /// <summary>
        /// Creates a new <see cref="ElisionFilterFactory"/> </summary>
        public ElisionFilterFactory(IDictionary<string, string> args) : base(args)
        {
            articlesFile = Get(args, "articles");
            ignoreCase = GetBoolean(args, "ignoreCase", false);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (articlesFile is null)
            {
                articles = FrenchAnalyzer.DEFAULT_ARTICLES;
            }
            else
            {
                articles = GetWordSet(loader, articlesFile, ignoreCase);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new ElisionFilter(input, articles);
        }

        public virtual AbstractAnalysisFactory GetMultiTermComponent()
        {
            return this;
        }
    }
}