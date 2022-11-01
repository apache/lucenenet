// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
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
    /// <see cref="TotalTermFreqValueSource"/> returns the total term freq 
    /// (sum of term freqs across all documents).
    /// Returns -1 if frequencies were omitted for the field, or if 
    /// the codec doesn't support this statistic.
    /// @lucene.internal
    /// </summary>
    public class TotalTermFreqValueSource : ValueSource
    {
        protected readonly string m_field;
        protected readonly string m_indexedField;
        protected readonly string m_val;
        protected readonly BytesRef m_indexedBytes;

        public TotalTermFreqValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
        {
            this.m_field = field;
            this.m_val = val;
            this.m_indexedField = indexedField;
            this.m_indexedBytes = indexedBytes;
        }

        public virtual string Name => "totaltermfreq";

        public override string GetDescription()
        {
            return Name + '(' + m_field + ',' + m_val + ')';
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            return (FunctionValues)context[this];
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            long totalTermFreq = 0;
            foreach (var readerContext in searcher.TopReaderContext.Leaves)
            {
                long val = readerContext.Reader.TotalTermFreq(new Term(m_indexedField, m_indexedBytes));
                if (val == -1)
                {
                    totalTermFreq = -1;
                    break;
                }
                else
                {
                    totalTermFreq += val;
                }
            }
            var ttf = totalTermFreq;
            context[this] = new Int64DocValuesAnonymousClass(this, ttf);
        }

        private sealed class Int64DocValuesAnonymousClass : Int64DocValues
        {
            private readonly long ttf;

            public Int64DocValuesAnonymousClass(TotalTermFreqValueSource @this, long ttf)
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
            return this.GetType().GetHashCode() + m_indexedField.GetHashCode() * 29 + m_indexedBytes.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (!(o is TotalTermFreqValueSource other))
                return false;
            return this.m_indexedField.Equals(other.m_indexedField, StringComparison.Ordinal) && this.m_indexedBytes.Equals(other.m_indexedBytes);
        }
    }
}