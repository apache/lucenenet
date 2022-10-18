// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
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
    /// Function that returns <see cref="TFIDFSimilarity.DecodeNormValue(long)"/>
    /// for every document.
    /// <para/>
    /// Note that the configured Similarity for the field must be
    /// a subclass of <see cref="TFIDFSimilarity"/>
    /// @lucene.internal 
    /// </summary>
    public class NormValueSource : ValueSource
    {
        protected readonly string m_field;

        public NormValueSource(string field)
        {
            this.m_field = field;
        }

        public virtual string Name => "norm";

        public override string GetDescription()
        {
            return Name + '(' + m_field + ')';
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            context["searcher"] = searcher;
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var searcher = (IndexSearcher)context["searcher"];
            TFIDFSimilarity similarity = IDFValueSource.AsTFIDF(searcher.Similarity, m_field);
            if (similarity is null)
            {
                throw UnsupportedOperationException.Create("requires a TFIDFSimilarity (such as DefaultSimilarity)");
            }

            NumericDocValues norms = readerContext.AtomicReader.GetNormValues(m_field);
            if (norms is null)
            {
                return new ConstDoubleDocValues(0.0, this);
            }

            return new SingleDocValuesAnonymousClass(this, similarity, norms);
        }

        private sealed class SingleDocValuesAnonymousClass : SingleDocValues
        {
            private readonly TFIDFSimilarity similarity;
            private readonly NumericDocValues norms;

            public SingleDocValuesAnonymousClass(NormValueSource @this, TFIDFSimilarity similarity, NumericDocValues norms)
                : base(@this)
            {
                this.similarity = similarity;
                this.norms = norms;
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return similarity.DecodeNormValue(norms.Get(doc));
            }
        }

        public override bool Equals(object o)
        {
            if (this.GetType() != o.GetType())
            {
                return false;
            }
            return this.m_field.Equals(((NormValueSource)o).m_field, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode() + m_field.GetHashCode();
        }
    }
}