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
using Lucene.Net.Analysis.Standard;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Shingle
{
    /**
 * A ShingleAnalyzerWrapper wraps a {@link ShingleFilter} around another {@link Analyzer}.
 * <p>
 * A shingle is another name for a token based n-gram.
 * </p>
 */
    public class ShingleAnalyzerWrapper : Analyzer
    {

        protected Analyzer defaultAnalyzer;
        protected int maxShingleSize = 2;
        protected bool outputUnigrams = true;

        public ShingleAnalyzerWrapper(Analyzer defaultAnalyzer)
        {
            this.defaultAnalyzer = defaultAnalyzer;
            SetOverridesTokenStreamMethod<ShingleAnalyzerWrapper>();
        }

        public ShingleAnalyzerWrapper(Analyzer defaultAnalyzer, int maxShingleSize)
            : this(defaultAnalyzer)
        {

            this.maxShingleSize = maxShingleSize;
        }

        /**
         * Wraps {@link StandardAnalyzer}. 
         */
        public ShingleAnalyzerWrapper(Version matchVersion)
        {
            this.defaultAnalyzer = new StandardAnalyzer(matchVersion);
            SetOverridesTokenStreamMethod<ShingleAnalyzerWrapper>();
        }

        /**
         * Wraps {@link StandardAnalyzer}. 
         */
        public ShingleAnalyzerWrapper(Version matchVersion, int nGramSize)
            : this(matchVersion)
        {
            this.maxShingleSize = nGramSize;
        }

        /**
         * The max shingle (ngram) size
         * 
         * @return The max shingle (ngram) size
         */
        public int GetMaxShingleSize()
        {
            return maxShingleSize;
        }

        /**
         * Set the maximum size of output shingles
         * 
         * @param maxShingleSize max shingle size
         */
        public void SetMaxShingleSize(int maxShingleSize)
        {
            this.maxShingleSize = maxShingleSize;
        }

        public bool IsOutputUnigrams()
        {
            return outputUnigrams;
        }

        /**
         * Shall the filter pass the original tokens (the "unigrams") to the output
         * stream?
         * 
         * @param outputUnigrams Whether or not the filter shall pass the original
         *        tokens to the output stream
         */
        public void SetOutputUnigrams(bool outputUnigrams)
        {
            this.outputUnigrams = outputUnigrams;
        }

        public override TokenStream TokenStream(String fieldName, TextReader reader)
        {
            TokenStream wrapped;
            try
            {
                wrapped = defaultAnalyzer.ReusableTokenStream(fieldName, reader);
            }
            catch (IOException e)
            {
                wrapped = defaultAnalyzer.TokenStream(fieldName, reader);
            }
            ShingleFilter filter = new ShingleFilter(wrapped);
            filter.SetMaxShingleSize(maxShingleSize);
            filter.SetOutputUnigrams(outputUnigrams);
            return filter;
        }

        class SavedStreams
        {
            protected internal TokenStream wrapped;
            protected internal ShingleFilter shingle;
        };

        public override TokenStream ReusableTokenStream(String fieldName, TextReader reader)
        {
            if (overridesTokenStreamMethod)
            {
                // LUCENE-1678: force fallback to tokenStream() if we
                // have been subclassed and that subclass overrides
                // tokenStream but not reusableTokenStream
                return TokenStream(fieldName, reader);
            }

            SavedStreams streams = (SavedStreams)PreviousTokenStream;
            if (streams == null)
            {
                streams = new SavedStreams();
                streams.wrapped = defaultAnalyzer.ReusableTokenStream(fieldName, reader);
                streams.shingle = new ShingleFilter(streams.wrapped);
                PreviousTokenStream = streams;
            }
            else
            {
                TokenStream result = defaultAnalyzer.ReusableTokenStream(fieldName, reader);
                if (result == streams.wrapped)
                {
                    /* the wrapped analyzer reused the stream */
                    streams.shingle.Reset();
                }
                else
                {
                    /* the wrapped analyzer did not, create a new shingle around the new one */
                    streams.wrapped = result;
                    streams.shingle = new ShingleFilter(streams.wrapped);
                }
            }
            streams.shingle.SetMaxShingleSize(maxShingleSize);
            streams.shingle.SetOutputUnigrams(outputUnigrams);
            return streams.shingle;
        }
    }
}