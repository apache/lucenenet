// Lucene version compatibility level 4.8.1
using J2N.Numerics;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using System.Collections;
using System.Globalization;

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
    /// <see cref="LinearSingleFunction"/> implements a linear function over
    /// another <see cref="ValueSource"/>.
    /// <para/>
    /// Normally Used as an argument to a <see cref="FunctionQuery"/>
    /// <para/>
    /// NOTE: This was LinearFloatFunction in Lucene
    /// </summary>
    public class LinearSingleFunction : ValueSource
    {
        protected readonly ValueSource m_source;
        protected readonly float m_slope;
        protected readonly float m_intercept;

        public LinearSingleFunction(ValueSource source, float slope, float intercept)
        {
            this.m_source = source;
            this.m_slope = slope;
            this.m_intercept = intercept;
        }

        public override string GetDescription()
        {
            return m_slope + "*float(" + m_source.GetDescription() + ")+" + m_intercept;
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FunctionValues vals = m_source.GetValues(context, readerContext);
            return new SingleDocValuesAnonymousClass(this, this, vals);
        }

        private sealed class SingleDocValuesAnonymousClass : SingleDocValues
        {
            private readonly LinearSingleFunction outerInstance;
            private readonly FunctionValues vals;

            public SingleDocValuesAnonymousClass(LinearSingleFunction outerInstance, LinearSingleFunction @this, FunctionValues vals)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.vals = vals;
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return vals.SingleVal(doc) * outerInstance.m_slope + outerInstance.m_intercept;
            }

            public override string ToString(int doc)
            {
                // LUCENENET specific - changed formatting to ensure the same culture is used for each value.
                return string.Format("{0}*float({1})+{2}",
                    outerInstance.m_slope,
                    vals.ToString(doc),
                    outerInstance.m_intercept);
            }
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            m_source.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            int h = J2N.BitConversion.SingleToInt32Bits(m_slope);
            h = (h.TripleShift(2)) | (h << 30);
            h += J2N.BitConversion.SingleToInt32Bits(m_intercept);
            h ^= (h << 14) | (h.TripleShift(19));
            return h + m_source.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (!(o is LinearSingleFunction other))
                return false;
            return this.m_slope == other.m_slope && this.m_intercept == other.m_intercept && this.m_source.Equals(other.m_source);
        }
    }
}