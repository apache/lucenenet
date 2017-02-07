using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Support;
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
    /// <see cref="ReciprocalSingleFunction"/> implements a reciprocal function <c>f(x) = a/(mx+b)</c>, based on
    /// the <see cref="float"/> value of a field or function as exported by <see cref="ValueSource"/>.
    /// <para/>
    /// When a and b are equal, and <c>x&gt;=0</c>, this function has a maximum value of 1 that drops as x increases.
    /// Increasing the value of a and b together results in a movement of the entire function to a flatter part of the curve.
    /// <para/>These properties make this an idea function for boosting more recent documents.
    /// <para/>Example:<c>  recip(ms(NOW,mydatefield),3.16e-11,1,1)</c>
    /// <para/>A multiplier of 3.16e-11 changes the units from milliseconds to years (since there are about 3.16e10 milliseconds
    /// per year).  Thus, a very recent date will yield a value close to 1/(0+1) or 1,
    /// a date a year in the past will get a multiplier of about 1/(1+1) or 1/2,
    /// and date two years old will yield 1/(2+1) or 1/3.
    /// <para/>
    /// NOTE: This was ReciprocalFloatFunction in Lucene
    /// </summary>
    /// <seealso cref="FunctionQuery"/>
    public class ReciprocalSingleFunction : ValueSource
    {
        protected readonly ValueSource m_source;
        protected readonly float m_m;
        protected readonly float m_a;
        protected readonly float m_b;

        /// <summary>
        ///  f(source) = a/(m*float(source)+b)
        /// </summary>
        public ReciprocalSingleFunction(ValueSource source, float m, float a, float b)
        {
            this.m_source = source;
            this.m_m = m;
            this.m_a = a;
            this.m_b = b;
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var vals = m_source.GetValues(context, readerContext);
            return new SingleDocValuesAnonymousInnerClassHelper(this, this, vals);
        }

        private class SingleDocValuesAnonymousInnerClassHelper : SingleDocValues
        {
            private readonly ReciprocalSingleFunction outerInstance;
            private readonly FunctionValues vals;

            public SingleDocValuesAnonymousInnerClassHelper(ReciprocalSingleFunction outerInstance, ReciprocalSingleFunction @this, FunctionValues vals)
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
                return outerInstance.m_a / (outerInstance.m_m * vals.SingleVal(doc) + outerInstance.m_b);
            }
            public override string ToString(int doc)
            {
                return Convert.ToString(outerInstance.m_a) + "/(" + outerInstance.m_m + "*float(" + vals.ToString(doc) + ')' + '+' + outerInstance.m_b + ')';
            }
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            m_source.CreateWeight(context, searcher);
        }

        public override string GetDescription()
        {
            return Convert.ToString(m_a) + "/(" + m_m + "*float(" + m_source.GetDescription() + ")" + "+" + m_b + ')';
        }

        public override int GetHashCode()
        {
            int h = Number.SingleToInt32Bits(m_a) + Number.SingleToInt32Bits(m_m);
            h ^= (h << 13) | ((int)((uint)h >> 20));
            return h + (Number.SingleToInt32Bits(m_b)) + m_source.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (typeof(ReciprocalSingleFunction) != o.GetType())
            {
                return false;
            }
            var other = o as ReciprocalSingleFunction;
            if (other == null)
                return false;
            return this.m_m == other.m_m && this.m_a == other.m_a && this.m_b == other.m_b && this.m_source.Equals(other.m_source);
        }
    }
}