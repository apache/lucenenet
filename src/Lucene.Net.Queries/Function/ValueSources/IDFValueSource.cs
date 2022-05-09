// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections;

namespace Lucene.Net.Queries.Function.ValueSources
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
    /// Function that returns <see cref="TFIDFSimilarity.Idf(long, long)"/>
    /// for every document.
    /// <para/>
    /// Note that the configured Similarity for the field must be
    /// a subclass of <see cref="TFIDFSimilarity"/>
    /// @lucene.internal 
    /// </summary>
    [ExceptionToClassNameConvention]
    public class IDFValueSource : DocFreqValueSource
    {
        public IDFValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
            : base(field, val, indexedField, indexedBytes)
        {
        }

        public override string Name => "idf";

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var searcher = (IndexSearcher)context["searcher"];
            TFIDFSimilarity sim = AsTFIDF(searcher.Similarity, m_field);
            if (sim is null)
            {
                throw UnsupportedOperationException.Create("requires a TFIDFSimilarity (such as DefaultSimilarity)");
            }
            int docfreq = searcher.IndexReader.DocFreq(new Term(m_indexedField, m_indexedBytes));
            float idf = sim.Idf(docfreq, searcher.IndexReader.MaxDoc);
            return new ConstDoubleDocValues(idf, this);
        }

        // tries extra hard to cast the sim to TFIDFSimilarity
        internal static TFIDFSimilarity AsTFIDF(Similarity sim, string field)
        {
            while (sim is PerFieldSimilarityWrapper perFieldSimilarityWrapper)
            {
                sim = perFieldSimilarityWrapper.Get(field);
            }
            if (sim is TFIDFSimilarity similarity)
            {
                return similarity;
            }
            else
            {
                return null;
            }
        }
    }
}