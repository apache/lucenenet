// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Compound.Hyphenation;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Compound
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
    /// Factory for <see cref="HyphenationCompoundWordTokenFilter"/>.
    /// <para/>
    /// This factory accepts the following parameters:
    /// <list type="bullet">
    ///     <item><description><code>hyphenator</code> (mandatory): path to the FOP xml hyphenation pattern. 
    ///     See <a href="http://offo.sourceforge.net/hyphenation/">http://offo.sourceforge.net/hyphenation/</a>.</description></item>
    ///     <item><description><code>encoding</code> (optional): encoding of the xml hyphenation file. defaults to UTF-8.</description></item>
    ///     <item><description><code>dictionary</code> (optional): dictionary of words. defaults to no dictionary.</description></item>
    ///     <item><description><code>minWordSize</code> (optional): minimal word length that gets decomposed. defaults to 5.</description></item>
    ///     <item><description><code>minSubwordSize</code> (optional): minimum length of subwords. defaults to 2.</description></item>
    ///     <item><description><code>maxSubwordSize</code> (optional): maximum length of subwords. defaults to 15.</description></item>
    ///     <item><description><code>onlyLongestMatch</code> (optional): if true, adds only the longest matching subword 
    ///     to the stream. defaults to false.</description></item>
    /// </list>
    /// <para>
    /// <code>
    /// &lt;fieldType name="text_hyphncomp" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.HyphenationCompoundWordTokenFilterFactory" hyphenator="hyphenator.xml" encoding="UTF-8"
    ///         dictionary="dictionary.txt" minWordSize="5" minSubwordSize="2" maxSubwordSize="15" onlyLongestMatch="false"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </para>
    /// </summary>
    /// <seealso cref="HyphenationCompoundWordTokenFilter"/>
    public class HyphenationCompoundWordTokenFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        private CharArraySet dictionary;
        private HyphenationTree hyphenator;
        private readonly string dictFile;
        private readonly string hypFile;
        private readonly string encoding;
        private readonly int minWordSize;
        private readonly int minSubwordSize;
        private readonly int maxSubwordSize;
        private readonly bool onlyLongestMatch;

        /// <summary>
        /// Creates a new <see cref="HyphenationCompoundWordTokenFilterFactory"/> </summary>
        public HyphenationCompoundWordTokenFilterFactory(IDictionary<string, string> args) : base(args)
        {
            AssureMatchVersion();
            dictFile = Get(args, "dictionary");
            encoding = Get(args, "encoding");
            hypFile = Require(args, "hyphenator");
            minWordSize = GetInt32(args, "minWordSize", CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE);
            minSubwordSize = GetInt32(args, "minSubwordSize", CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE);
            maxSubwordSize = GetInt32(args, "maxSubwordSize", CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE);
            onlyLongestMatch = GetBoolean(args, "onlyLongestMatch", false);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            Stream stream = null;
            try
            {
                if (dictFile != null) // the dictionary can be empty.
                {
                    dictionary = GetWordSet(loader, dictFile, false);
                }
                // TODO: Broken, because we cannot resolve real system id
                // ResourceLoader should also supply method like ClassLoader to get resource URL
                stream = loader.OpenResource(hypFile);
                //InputSource @is = new InputSource(stream);
                //@is.Encoding = encoding; // if it's null let xml parser decide
                //@is.SystemId = hypFile;

                var xmlEncoding = string.IsNullOrEmpty(encoding) ? Encoding.UTF8 : Encoding.GetEncoding(encoding);

                hyphenator = HyphenationCompoundWordTokenFilter.GetHyphenationTree(stream, xmlEncoding);

            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(stream);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new HyphenationCompoundWordTokenFilter(m_luceneMatchVersion, input, hyphenator, dictionary, minWordSize, minSubwordSize, maxSubwordSize, onlyLongestMatch);
        }
    }
}