// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// Factory for <see cref="LimitTokenCountFilter"/>. 
    /// <code>
    /// &lt;fieldType name="text_lngthcnt" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.LimitTokenCountFilterFactory" maxTokenCount="10" consumeAllTokens="false" /&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// <para>
    /// The <see cref="consumeAllTokens"/> property is optional and defaults to <c>false</c>.  
    /// See <see cref="LimitTokenCountFilter"/> for an explanation of it's use.
    /// </para>
    /// </summary>
    public class LimitTokenCountFilterFactory : TokenFilterFactory
    {
        public const string MAX_TOKEN_COUNT_KEY = "maxTokenCount";
        public const string CONSUME_ALL_TOKENS_KEY = "consumeAllTokens";
        private readonly int maxTokenCount;
        private readonly bool consumeAllTokens;

        /// <summary>
        /// Creates a new <see cref="LimitTokenCountFilterFactory"/> </summary>
        public LimitTokenCountFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            maxTokenCount = RequireInt32(args, MAX_TOKEN_COUNT_KEY);
            consumeAllTokens = GetBoolean(args, CONSUME_ALL_TOKENS_KEY, false);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new LimitTokenCountFilter(input, maxTokenCount, consumeAllTokens);
        }
    }
}