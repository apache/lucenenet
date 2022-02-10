// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;

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
    /// Factory for <see cref="DictionaryCompoundWordTokenFilter"/>. 
    /// <code>
    /// &lt;fieldType name="text_dictcomp" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.DictionaryCompoundWordTokenFilterFactory" dictionary="dictionary.txt"
    ///         minWordSize="5" minSubwordSize="2" maxSubwordSize="15" onlyLongestMatch="true"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class DictionaryCompoundWordTokenFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        private CharArraySet dictionary;
        private readonly string dictFile;
        private readonly int minWordSize;
        private readonly int minSubwordSize;
        private readonly int maxSubwordSize;
        private readonly bool onlyLongestMatch;

        /// <summary>
        /// Creates a new <see cref="DictionaryCompoundWordTokenFilterFactory"/> </summary>
        public DictionaryCompoundWordTokenFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            AssureMatchVersion();
            dictFile = Require(args, "dictionary");
            minWordSize = GetInt32(args, "minWordSize", CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE);
            minSubwordSize = GetInt32(args, "minSubwordSize", CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE);
            maxSubwordSize = GetInt32(args, "maxSubwordSize", CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE);
            onlyLongestMatch = GetBoolean(args, "onlyLongestMatch", true);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            dictionary = base.GetWordSet(loader, dictFile, false);
        }

        public override TokenStream Create(TokenStream input)
        {
            // if the dictionary is null, it means it was empty
            return dictionary is null ? input : new DictionaryCompoundWordTokenFilter(m_luceneMatchVersion, input, dictionary, minWordSize, minSubwordSize, maxSubwordSize, onlyLongestMatch);
        }
    }
}