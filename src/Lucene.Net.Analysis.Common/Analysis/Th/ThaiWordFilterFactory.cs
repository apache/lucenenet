// Lucene version compatibility level 4.8.1
#if FEATURE_BREAKITERATOR
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Th
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
    /// Factory for <see cref="ThaiWordFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_thai" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.ThaiWordFilterFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code> 
    /// </summary>
    /// @deprecated Use <see cref="ThaiTokenizerFactory"/> instead 
    [Obsolete("Use ThaiTokenizerFactory instead")]
    public class ThaiWordFilterFactory : TokenFilterFactory
    {
        /// <summary>
        /// Creates a new <see cref="ThaiWordFilterFactory"/> </summary>
        public ThaiWordFilterFactory(IDictionary<string, string> args) : base(args)
        {
            AssureMatchVersion();
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new ThaiWordFilter(m_luceneMatchVersion, input);
        }
    }
}
#endif