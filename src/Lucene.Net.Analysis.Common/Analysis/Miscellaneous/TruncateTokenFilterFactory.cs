// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Globalization;

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
    /// Factory for <see cref="TruncateTokenFilter"/>. The following type is recommended for "<i>diacritics-insensitive search</i>" for Turkish.
    /// <code>
    /// &lt;fieldType name="text_tr_ascii_f5" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.ApostropheFilterFactory"/&gt;
    ///     &lt;filter class="solr.TurkishLowerCaseFilterFactory"/&gt;
    ///     &lt;filter class="solr.ASCIIFoldingFilterFactory" preserveOriginal="true"/&gt;
    ///     &lt;filter class="solr.KeywordRepeatFilterFactory"/&gt;
    ///     &lt;filter class="solr.TruncateTokenFilterFactory" prefixLength="5"/&gt;
    ///     &lt;filter class="solr.RemoveDuplicatesTokenFilterFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    public class TruncateTokenFilterFactory : TokenFilterFactory
    {
        public const string PREFIX_LENGTH_KEY = "prefixLength";
        private readonly sbyte prefixLength;

        /// <summary>
        /// Creates a new <see cref="TruncateTokenFilterFactory"/> </summary>
        public TruncateTokenFilterFactory(IDictionary<string, string> args) 
            : base(args)
        {
            var prefixLengthString = Get(args, PREFIX_LENGTH_KEY, "5");
            prefixLength = sbyte.Parse(prefixLengthString, NumberStyles.Integer, CultureInfo.InvariantCulture);
            if (prefixLength < 1)
            {
                // LUCENENET specific - showing the original string provided by the user so we don't need to worry about culture-specific number conversion issues
                throw new ArgumentException(PREFIX_LENGTH_KEY + " parameter must be a positive number: " + prefixLengthString);
            }
            if (args.Count > 0)
            {
                throw new ArgumentException("Unknown parameter(s): " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new TruncateTokenFilter(input, prefixLength);
        }
    }
}