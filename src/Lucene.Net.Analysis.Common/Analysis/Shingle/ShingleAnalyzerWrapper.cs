// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using System;

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
    /// A ShingleAnalyzerWrapper wraps a <see cref="ShingleFilter"/> around another <see cref="Analyzer"/>.
    /// <para>
    /// A shingle is another name for a token based n-gram.
    /// </para>
    /// </summary>
    public sealed class ShingleAnalyzerWrapper : AnalyzerWrapper
    {
        private readonly Analyzer @delegate;
        private readonly int maxShingleSize;
        private readonly int minShingleSize;
        private readonly string tokenSeparator;
        private readonly bool outputUnigrams;
        private readonly bool outputUnigramsIfNoShingles;
        private readonly string fillerToken;

        public ShingleAnalyzerWrapper(Analyzer defaultAnalyzer)
            : this(defaultAnalyzer, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE)
        {
        }

        public ShingleAnalyzerWrapper(Analyzer defaultAnalyzer, int maxShingleSize)
            : this(defaultAnalyzer, ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, maxShingleSize)
        {
        }

        public ShingleAnalyzerWrapper(Analyzer defaultAnalyzer, int minShingleSize, int maxShingleSize)
            : this(defaultAnalyzer, minShingleSize, maxShingleSize, ShingleFilter.DEFAULT_TOKEN_SEPARATOR, true, false, ShingleFilter.DEFAULT_FILLER_TOKEN)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ShingleAnalyzerWrapper"/>
        /// </summary>
        /// <param name="delegate"> <see cref="Analyzer"/> whose <see cref="TokenStream"/> is to be filtered </param>
        /// <param name="minShingleSize"> Min shingle (token ngram) size </param>
        /// <param name="maxShingleSize"> Max shingle size </param>
        /// <param name="tokenSeparator"> Used to separate input stream tokens in output shingles </param>
        /// <param name="outputUnigrams"> Whether or not the filter shall pass the original
        ///        tokens to the output stream </param>
        /// <param name="outputUnigramsIfNoShingles"> Overrides the behavior of outputUnigrams==false for those
        ///        times when no shingles are available (because there are fewer than
        ///        minShingleSize tokens in the input stream)?
        ///        Note that if outputUnigrams==true, then unigrams are always output,
        ///        regardless of whether any shingles are available. </param>
        /// <param name="fillerToken"> filler token to use when positionIncrement is more than 1 </param>
        public ShingleAnalyzerWrapper(Analyzer @delegate, int minShingleSize, int maxShingleSize, 
            string tokenSeparator, bool outputUnigrams, bool outputUnigramsIfNoShingles, string fillerToken) 
            : base(@delegate.Strategy)
        {
            this.@delegate = @delegate;

            if (maxShingleSize < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxShingleSize), "Max shingle size must be >= 2"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.maxShingleSize = maxShingleSize;

            if (minShingleSize < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(minShingleSize), "Min shingle size must be >= 2"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (minShingleSize > maxShingleSize)
            {
                throw new ArgumentException("Min shingle size must be <= max shingle size");
            }
            this.minShingleSize = minShingleSize;

            this.tokenSeparator = (tokenSeparator is null ? "" : tokenSeparator);
            this.outputUnigrams = outputUnigrams;
            this.outputUnigramsIfNoShingles = outputUnigramsIfNoShingles;
            this.fillerToken = fillerToken;
        }

        /// <summary>
        /// Wraps <see cref="StandardAnalyzer"/>. 
        /// </summary>
        public ShingleAnalyzerWrapper(LuceneVersion matchVersion)
              : this(matchVersion, ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE)
        {
        }

        /// <summary>
        /// Wraps <see cref="StandardAnalyzer"/>. 
        /// </summary>
        public ShingleAnalyzerWrapper(LuceneVersion matchVersion, int minShingleSize, int maxShingleSize)
              : this(new StandardAnalyzer(matchVersion), minShingleSize, maxShingleSize)
        {
        }

        /// <summary>
        /// The max shingle (token ngram) size
        /// </summary>
        /// <returns> The max shingle (token ngram) size </returns>
        public int MaxShingleSize => maxShingleSize;

        /// <summary>
        /// The min shingle (token ngram) size
        /// </summary>
        /// <returns> The min shingle (token ngram) size </returns>
        public int MinShingleSize => minShingleSize;

        public string TokenSeparator => tokenSeparator;

        public bool OutputUnigrams => outputUnigrams;

        public bool OutputUnigramsIfNoShingles => outputUnigramsIfNoShingles;

        public string FillerToken => fillerToken;

        protected override sealed Analyzer GetWrappedAnalyzer(string fieldName)
        {
            return @delegate;
        }

        protected override TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
        {
            ShingleFilter filter = new ShingleFilter(components.TokenStream, minShingleSize, maxShingleSize);
            filter.SetMinShingleSize(minShingleSize);
            filter.SetMaxShingleSize(maxShingleSize);
            filter.SetTokenSeparator(tokenSeparator);
            filter.SetOutputUnigrams(outputUnigrams);
            filter.SetOutputUnigramsIfNoShingles(outputUnigramsIfNoShingles);
            filter.SetFillerToken(fillerToken);
            return new TokenStreamComponents(components.Tokenizer, filter);
        }
    }
}