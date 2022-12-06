// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
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
    /// <see cref="SumTotalTermFreqValueSource"/> returns the number of tokens.
    /// (sum of term freqs across all documents, across all terms).
    /// Returns -1 if frequencies were omitted for the field, or if 
    /// the codec doesn't support this statistic.
    /// @lucene.internal
    /// </summary>
    public class SumTotalTermFreqValueSource : ValueSource
    {
        protected readonly string m_indexedField;

        public SumTotalTermFreqValueSource(string indexedField)
        {
            this.m_indexedField = indexedField;
        }

        public virtual string Name => "sumtotaltermfreq";

        public override string GetDescription()
        {
            return Name + '(' + m_indexedField + ')';
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            return (FunctionValues)context[this];
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            long sumTotalTermFreq = 0;
            foreach (AtomicReaderContext readerContext in searcher.TopReaderContext.Leaves)
            {
                Fields fields = readerContext.AtomicReader.Fields;
                if (fields is null)
                {
                    continue;
                }
                Terms terms = fields.GetTerms(m_indexedField);
                if (terms is null)
                {
                    continue;
                }
                long v = terms.SumTotalTermFreq;
                if (v == -1)
                {
                    sumTotalTermFreq = -1;
                    break;
                }
                else
                {
                    sumTotalTermFreq += v;
                }
            }
            long ttf = sumTotalTermFreq;
            context[this] = new Int64DocValuesAnonymousClass(this, ttf);
        }

        private sealed class Int64DocValuesAnonymousClass : Int64DocValues
        {
            private readonly long ttf;

            public Int64DocValuesAnonymousClass(SumTotalTermFreqValueSource @this, long ttf)
                : base(@this)
            {
                this.ttf = ttf;
            }

            /// <summary>
            /// NOTE: This was longVal() in Lucene
            /// </summary>
            public override long Int64Val(int doc)
            {
                return ttf;
            }
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode() + m_indexedField.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (!(o is SumTotalTermFreqValueSource other))
                return false;
            return this.m_indexedField.Equals(other.m_indexedField, StringComparison.Ordinal);
        }
    }
}