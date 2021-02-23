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
    /// Factory for <see cref="LengthFilter"/>. 
    /// <code>
    /// &lt;fieldType name="text_lngth" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.LengthFilterFactory" min="0" max="1" /&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    public class LengthFilterFactory : TokenFilterFactory
    {
        private readonly int min;
        private readonly int max;
        private readonly bool enablePositionIncrements;
        public const string MIN_KEY = "min";
        public const string MAX_KEY = "max";

        /// <summary>
        /// Creates a new <see cref="LengthFilterFactory"/> </summary>
        public LengthFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            min = RequireInt32(args, MIN_KEY);
            max = RequireInt32(args, MAX_KEY);
            enablePositionIncrements = GetBoolean(args, "enablePositionIncrements", true);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
#pragma warning disable 612, 618
            var filter = new LengthFilter(m_luceneMatchVersion, enablePositionIncrements, input, min, max);
#pragma warning restore 612, 618
            return filter;
        }
    }
}