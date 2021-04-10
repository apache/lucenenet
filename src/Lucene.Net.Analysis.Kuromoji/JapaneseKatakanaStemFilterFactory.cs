using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ja
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
    /// Factory for <see cref="JapaneseKatakanaStemFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_ja" class="solr.TextField"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.JapaneseTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.JapaneseKatakanaStemFilterFactory"
    ///             minimumLength="4"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class JapaneseKatakanaStemFilterFactory : TokenFilterFactory
    {
        private const string MINIMUM_LENGTH_PARAM = "minimumLength";
        private readonly int minimumLength;

        /// <summary>Creates a new <see cref="JapaneseKatakanaStemFilterFactory"/></summary>
        public JapaneseKatakanaStemFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            minimumLength = GetInt32(args, MINIMUM_LENGTH_PARAM, JapaneseKatakanaStemFilter.DEFAULT_MINIMUM_LENGTH);
            if (minimumLength < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumLength), "Illegal " + MINIMUM_LENGTH_PARAM + " " + minimumLength + " (must be 2 or greater)"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new JapaneseKatakanaStemFilter(input, minimumLength);
        }
    }
}
