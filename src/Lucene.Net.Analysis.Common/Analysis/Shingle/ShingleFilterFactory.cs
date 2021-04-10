// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Shingle
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
    /// Factory for <see cref="ShingleFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_shingle" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.ShingleFilterFactory" minShingleSize="2" maxShingleSize="2"
    ///             outputUnigrams="true" outputUnigramsIfNoShingles="false" tokenSeparator=" " fillerToken="_"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    public class ShingleFilterFactory : TokenFilterFactory
    {
        private readonly int minShingleSize;
        private readonly int maxShingleSize;
        private readonly bool outputUnigrams;
        private readonly bool outputUnigramsIfNoShingles;
        private readonly string tokenSeparator;
        private readonly string fillerToken;

        /// <summary>
        /// Creates a new <see cref="ShingleFilterFactory"/> </summary>
        public ShingleFilterFactory(IDictionary<string, string> args) 
            : base(args)
        {
            maxShingleSize = GetInt32(args, "maxShingleSize", ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE);
            if (maxShingleSize < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxShingleSize), "Invalid maxShingleSize (" + maxShingleSize + ") - must be at least 2"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            minShingleSize = GetInt32(args, "minShingleSize", ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE);
            if (minShingleSize < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(minShingleSize), "Invalid minShingleSize (" + minShingleSize + ") - must be at least 2"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (minShingleSize > maxShingleSize)
            {
                throw new ArgumentException("Invalid minShingleSize (" + minShingleSize + ") - must be no greater than maxShingleSize (" + maxShingleSize + ")");
            }
            outputUnigrams = GetBoolean(args, "outputUnigrams", true);
            outputUnigramsIfNoShingles = GetBoolean(args, "outputUnigramsIfNoShingles", false);
            tokenSeparator = Get(args, "tokenSeparator", ShingleFilter.DEFAULT_TOKEN_SEPARATOR);
            fillerToken = Get(args, "fillerToken", ShingleFilter.DEFAULT_FILLER_TOKEN);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            ShingleFilter r = new ShingleFilter(input, minShingleSize, maxShingleSize);
            r.SetOutputUnigrams(outputUnigrams);
            r.SetOutputUnigramsIfNoShingles(outputUnigramsIfNoShingles);
            r.SetTokenSeparator(tokenSeparator);
            r.SetFillerToken(fillerToken);
            return r;
        }
    }
}