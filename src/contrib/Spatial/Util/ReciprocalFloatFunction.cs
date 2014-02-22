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
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;

namespace Lucene.Net.Spatial.Util
{
    /// <summary>
    /// <code>ReciprocalFloatFunction</code> implements a reciprocal function f(x) = a/(mx+b), based on
    /// the float value of a field or function as exported by {@link org.apache.lucene.queries.function.ValueSource}.
    /// 
    /// When a and b are equal, and x>=0, this function has a maximum value of 1 that drops as x increases.
    /// Increasing the value of a and b together results in a movement of the entire function to a flatter part of the curve.
    /// <p>These properties make this an idea function for boosting more recent documents.
    /// <p>Example:<code>  recip(ms(NOW,mydatefield),3.16e-11,1,1)</code>
    /// <p>A multiplier of 3.16e-11 changes the units from milliseconds to years (since there are about 3.16e10 milliseconds
    ///  per year).  Thus, a very recent date will yield a value close to 1/(0+1) or 1,
    /// a date a year in the past will get a multiplier of about 1/(1+1) or 1/2,
    /// and date two years old will yield 1/(2+1) or 1/3.
    /// </summary>
    public class ReciprocalFloatFunction : ValueSource
    {
        protected readonly ValueSource source;
        protected readonly float m;
        protected readonly float a;
        protected readonly float b;

        /// <summary>
        /// f(source) = a/(m*float(source)+b)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="m"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public ReciprocalFloatFunction(ValueSource source, float m, float a, float b)
        {
            this.source = source;
            this.m = m;
            this.a = a;
            this.b = b;
        }

        public class FloatDocValues : FunctionValues
        {
            private readonly ReciprocalFloatFunction _enclosingInstance;
            private readonly FunctionValues vals;

            public FloatDocValues(ReciprocalFloatFunction enclosingInstance, FunctionValues vals)
            {
                _enclosingInstance = enclosingInstance;
                this.vals = vals;
            }

            public override float FloatVal(int doc)
            {
                return _enclosingInstance.a / (_enclosingInstance.m * vals.FloatVal(doc) + _enclosingInstance.b);
            }

            public override string ToString(int doc)
            {
                return _enclosingInstance.a + "/("
                       + _enclosingInstance.m + "*float(" + vals.ToString(doc) + ')'
                       + '+' + _enclosingInstance.b + ')';
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            var vals = source.GetValues(context, readerContext);
            return new FloatDocValues(this, vals);
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            source.CreateWeight(context, searcher);
        }



        public override string Description
        {
            get
            {
                return string.Format("{0}/({1}*float({2})" + "+{3}{4}", a, m, source.Description, b, ')');
            }
        }

        public override bool Equals(object o)
        {
            if (typeof(ReciprocalFloatFunction) != o.GetType()) return false;
            var other = (ReciprocalFloatFunction)o;
            return this.m == other.m
                   && this.a == other.a
                   && this.b == other.b
                   && this.source.Equals(other.source);
        }

        public override int GetHashCode()
        {
            int h = (int) BitConverter.DoubleToInt64Bits(a) + (int) BitConverter.DoubleToInt64Bits(m);
            h ^= (h << 13) | (int)((uint)h >> 20);
            return h + ((int) BitConverter.DoubleToInt64Bits(b)) + source.GetHashCode();
        }
    }
}
