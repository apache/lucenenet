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

using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

namespace Lucene.Net.Analyzers.Shingle
{
    /// <summary>
    /// A ShingleAnalyzerWrapper wraps a ShingleFilter around another Analyzer.
    /// 
    /// <p>A shingle is another name for a token based n-gram.</p>
    /// </summary>
    public class ShingleAnalyzerWrapper : Analyzer
    {
        protected Analyzer DefaultAnalyzer;
        protected int MaxShingleSize = 2;
        protected bool OutputUnigrams = true;

        /// <summary>
        /// Wraps StandardAnalyzer. 
        /// </summary>
        public ShingleAnalyzerWrapper()
        {
            DefaultAnalyzer = new StandardAnalyzer();
            SetOverridesTokenStreamMethod(typeof (ShingleAnalyzerWrapper));
        }

        public ShingleAnalyzerWrapper(int nGramSize)
            : this()
        {
            MaxShingleSize = nGramSize;
        }

        public ShingleAnalyzerWrapper(Analyzer defaultAnalyzer)
        {
            DefaultAnalyzer = defaultAnalyzer;
            SetOverridesTokenStreamMethod(typeof (ShingleAnalyzerWrapper));
        }

        public ShingleAnalyzerWrapper(Analyzer defaultAnalyzer, int maxShingleSize) : this(defaultAnalyzer)
        {
            MaxShingleSize = maxShingleSize;
        }

        /// <summary>
        /// The max shingle (ngram) size
        /// </summary>
        /// <returns></returns>
        public int GetMaxShingleSize()
        {
            return MaxShingleSize;
        }

        /// <summary>
        /// Set the maximum size of output shingles
        /// </summary>
        /// <param name="maxShingleSize">max shingle size</param>
        public void SetMaxShingleSize(int maxShingleSize)
        {
            MaxShingleSize = maxShingleSize;
        }

        public bool IsOutputUnigrams()
        {
            return OutputUnigrams;
        }

        /// <summary>
        /// Shall the filter pass the original tokens (the "unigrams") to the output
        /// stream?
        /// </summary>
        /// <param name="outputUnigrams">Whether or not the filter shall pass the original tokens to the output stream</param>
        public void SetOutputUnigrams(bool outputUnigrams)
        {
            OutputUnigrams = outputUnigrams;
        }

        public override TokenStream TokenStream(String fieldName, TextReader reader)
        {
            TokenStream wrapped;
            try
            {
                wrapped = DefaultAnalyzer.ReusableTokenStream(fieldName, reader);
            }
            catch (IOException)
            {
                wrapped = DefaultAnalyzer.TokenStream(fieldName, reader);
            }

            var filter = new ShingleFilter(wrapped);
            filter.SetMaxShingleSize(MaxShingleSize);
            filter.SetOutputUnigrams(OutputUnigrams);

            return filter;
        }

        public override TokenStream ReusableTokenStream(String fieldName, TextReader reader)
        {
            if (overridesTokenStreamMethod)
            {
                // LUCENE-1678: force fallback to tokenStream() if we
                // have been subclassed and that subclass overrides
                // tokenStream but not reusableTokenStream
                return TokenStream(fieldName, reader);
            }

            var streams = (SavedStreams) GetPreviousTokenStream();

            if (streams == null)
            {
                streams = new SavedStreams
                              {
                                  Wrapped = DefaultAnalyzer.ReusableTokenStream(fieldName, reader)
                              };
                streams.Shingle = new ShingleFilter(streams.Wrapped);
                SetPreviousTokenStream(streams);
            }
            else
            {
                var result = DefaultAnalyzer.ReusableTokenStream(fieldName, reader);
                if (result == streams.Wrapped)
                {
                    // the wrapped analyzer reused the stream 
                    streams.Shingle.Reset();
                }
                else
                {
                    // the wrapped analyzer did not, create a new shingle around the new one 
                    streams.Wrapped = result;
                    streams.Shingle = new ShingleFilter(streams.Wrapped);
                }
            }

            streams.Shingle.SetMaxShingleSize(MaxShingleSize);
            streams.Shingle.SetOutputUnigrams(OutputUnigrams);

            return streams.Shingle;
        }

        #region Nested type: SavedStreams

        private class SavedStreams
        {
            public ShingleFilter Shingle;
            public TokenStream Wrapped;
        } ;

        #endregion
    }
}