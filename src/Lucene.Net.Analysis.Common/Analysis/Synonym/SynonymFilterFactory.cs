using System;
using System.Collections.Generic;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

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
    /// Factory for <seealso cref="SynonymFilter"/>.
    /// <pre class="prettyprint" >
    /// &lt;fieldType name="text_synonym" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.SynonymFilterFactory" synonyms="synonyms.txt" 
    ///             format="solr" ignoreCase="false" expand="true" 
    ///             tokenizerFactory="solr.WhitespaceTokenizerFactory"
    ///             [optional tokenizer factory parameters]/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</pre>
    /// 
    /// <para>
    /// An optional param name prefix of "tokenizerFactory." may be used for any 
    /// init params that the SynonymFilterFactory needs to pass to the specified 
    /// TokenizerFactory.  If the TokenizerFactory expects an init parameters with 
    /// the same name as an init param used by the SynonymFilterFactory, the prefix 
    /// is mandatory.
    /// </para>
    /// <para>
    /// The optional {@code format} parameter controls how the synonyms will be parsed:
    /// It supports the short names of {@code solr} for <seealso cref="SolrSynonymParser"/> 
    /// and {@code wordnet} for and <seealso cref="WordnetSynonymParser"/>, or your own 
    /// {@code SynonymMap.Parser} class name. The default is {@code solr}.
    /// A custom <seealso cref="SynonymMap.Parser"/> is expected to have a constructor taking:
    /// <ul>
    ///   <li><code>boolean dedup</code> - true if duplicates should be ignored, false otherwise</li>
    ///   <li><code>boolean expand</code> - true if conflation groups should be expanded, false if they are one-directional</li>
    ///   <li><code><seealso cref="Analyzer"/> analyzer</code> - an analyzer used for each raw synonym</li>
    /// </ul>
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
                if (args.ContainsKey("format") && !args["format"].Equals("solr"))
                {
                    throw new System.ArgumentException("You must specify luceneMatchVersion >= 3.4 to use alternate synonyms formats");
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
        /// Access to the delegator TokenFilterFactory for test verification
        /// </summary>
        /// @deprecated Method exists only for testing 4x, will be removed in 5.0
        /// @lucene.internal 
        [Obsolete("Method exists only for testing 4x, will be removed in 5.0")]
        internal virtual TokenFilterFactory Delegator
        {
            get
            {
                return delegator;
            }
        }
    }
}