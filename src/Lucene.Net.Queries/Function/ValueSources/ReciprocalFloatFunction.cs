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
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Support;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// <code>ReciprocalFloatFunction</code> implements a reciprocal function f(x) = a/(mx+b), based on
    /// the float value of a field or function as exported by <seealso cref="ValueSource"/>.
    /// <br>
    /// 
    /// When a and b are equal, and x>=0, this function has a maximum value of 1 that drops as x increases.
    /// Increasing the value of a and b together results in a movement of the entire function to a flatter part of the curve.
    /// <para>These properties make this an idea function for boosting more recent documents.
    /// </para>
    /// <para>Example:<code>  recip(ms(NOW,mydatefield),3.16e-11,1,1)</code>
    /// </para>
    /// <para>A multiplier of 3.16e-11 changes the units from milliseconds to years (since there are about 3.16e10 milliseconds
    /// per year).  Thus, a very recent date will yield a value close to 1/(0+1) or 1,
    /// a date a year in the past will get a multiplier of about 1/(1+1) or 1/2,
    /// and date two years old will yield 1/(2+1) or 1/3.
    /// 
    /// </para>
    /// </summary>
    /// <seealso cref= org.apache.lucene.queries.function.FunctionQuery
    /// 
    ///  </seealso>
    public class ReciprocalFloatFunction : ValueSource
    {
        protected internal readonly ValueSource source;
        protected internal readonly float m;
        protected internal readonly float a;
        protected internal readonly float b;

        /// <summary>
        ///  f(source) = a/(m*float(source)+b)
        /// </summary>
        public ReciprocalFloatFunction(ValueSource source, float m, float a, float b)
        {
            this.source = source;
            this.m = m;
            this.a = a;
            this.b = b;
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var vals = source.GetValues(context, readerContext);
            return new FloatDocValuesAnonymousInnerClassHelper(this, this, vals);
        }

        private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
        {
            private readonly ReciprocalFloatFunction outerInstance;
            private readonly FunctionValues vals;

            public FloatDocValuesAnonymousInnerClassHelper(ReciprocalFloatFunction outerInstance, ReciprocalFloatFunction @this, FunctionValues vals)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.vals = vals;
            }

            public override float FloatVal(int doc)
            {
                return outerInstance.a / (outerInstance.m * vals.FloatVal(doc) + outerInstance.b);
            }
            public override string ToString(int doc)
            {
                return Convert.ToString(outerInstance.a) + "/(" + outerInstance.m + "*float(" + vals.ToString(doc) + ')' + '+' + outerInstance.b + ')';
            }
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            source.CreateWeight(context, searcher);
        }

        public override string GetDescription()
        {
            return Convert.ToString(a) + "/(" + m + "*float(" + source.GetDescription() + ")" + "+" + b + ')';
        }

        public override int GetHashCode()
        {
            int h = Number.FloatToIntBits(a) + Number.FloatToIntBits(m);
            h ^= (h << 13) | ((int)((uint)h >> 20));
            return h + (Number.FloatToIntBits(b)) + source.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (typeof(ReciprocalFloatFunction) != o.GetType())
            {
                return false;
            }
            var other = o as ReciprocalFloatFunction;
            if (other == null)
                return false;
            return this.m == other.m && this.a == other.a && this.b == other.b && this.source.Equals(other.source);
        }
    }
}