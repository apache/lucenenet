// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Synonym
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
    /// Factory for <see cref="SynonymFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_synonym" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.SynonymFilterFactory" synonyms="synonyms.txt" 
    ///             format="solr" ignoreCase="false" expand="true" 
    ///             tokenizerFactory="solr.WhitespaceTokenizerFactory"
    ///             [optional tokenizer factory parameters]/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// 
    /// <para>
    /// An optional param name prefix of "tokenizerFactory." may be used for any 
    /// init params that the <see cref="SynonymFilterFactory"/> needs to pass to the specified 
    /// <see cref="TokenizerFactory"/>.  If the <see cref="TokenizerFactory"/> expects an init parameters with 
    /// the same name as an init param used by the <see cref="SynonymFilterFactory"/>, the prefix 
    /// is mandatory.
    /// </para>
    /// <para>
    /// The optional <c>format</c> parameter controls how the synonyms will be parsed:
    /// It supports the short names of <c>solr</c> for <see cref="SolrSynonymParser"/> 
    /// and <c>wordnet</c> for and <see cref="WordnetSynonymParser"/>, or your own 
    /// <see cref="SynonymMap.Parser"/> class name. The default is <c>solr</c>.
    /// A custom <see cref="SynonymMap.Parser"/> is expected to have a constructor taking:
    /// <list type="bullet">
    ///     <item><description><c><see cref="bool"/> dedup</c> - true if duplicates should be ignored, false otherwise</description></item>
    ///     <item><description><c><see cref="bool"/> expand</c> - true if conflation groups should be expanded, false if they are one-directional</description></item>
    ///     <item><description><c><see cref="Analyzer"/> analyzer</c> - an analyzer used for each raw synonym</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class SynonymFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        private readonly TokenFilterFactory delegator;

        public SynonymFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            AssureMatchVersion();
#pragma warning disable 612, 618
            if (m_luceneMatchVersion.OnOrAfter(Lucene.Net.Util.LuceneVersion.LUCENE_34))
            {
                delegator = new FSTSynonymFilterFactory(new Dictionary<string, string>(OriginalArgs));
            }
#pragma warning restore 612, 618
            else
            {
                // check if you use the new optional arg "format". this makes no sense for the old one, 
                // as its wired to solr's synonyms format only.
                if (args.TryGetValue("format", out string value) && !value.Equals("solr", StringComparison.Ordinal))
                {
                    throw new ArgumentException("You must specify luceneMatchVersion >= 3.4 to use alternate synonyms formats");
                }
#pragma warning disable 612, 618
                delegator = new SlowSynonymFilterFactory(new Dictionary<string, string>(OriginalArgs));
#pragma warning restore 612, 618
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return delegator.Create(input);
        }

        public virtual void Inform(IResourceLoader loader)
        {
            ((IResourceLoaderAware)delegator).Inform(loader);
        }

        /// <summary>
        /// Access to the delegator <see cref="TokenFilterFactory"/> for test verification
        /// </summary>
        /// @deprecated Method exists only for testing 4x, will be removed in 5.0
        /// @lucene.internal 
        [Obsolete("Method exists only for testing 4x, will be removed in 5.0")]
        internal virtual TokenFilterFactory Delegator => delegator;
    }
}