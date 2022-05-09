// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Util.Mutable;
using System;
using System.Globalization;

namespace Lucene.Net.Queries.Function.DocValues
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
    /// Abstract <see cref="FunctionValues"/> implementation which supports retrieving <see cref="long"/> values.
    /// Implementations can control how the <see cref="long"/> values are loaded through <see cref="Int64Val(int)"/>
    /// <para/>
    /// NOTE: This was LongDocValues in Lucene
    /// </summary>
    public abstract class Int64DocValues : FunctionValues
    {
        protected readonly ValueSource m_vs;

        protected Int64DocValues(ValueSource vs) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_vs = vs;
        }

        public override byte ByteVal(int doc)
        {
            return (byte)Int64Val(doc);
        }

        /// <summary>
        /// NOTE: This was shortVal() in Lucene
        /// </summary>
        public override short Int16Val(int doc)
        {
            return (short)Int64Val(doc);
        }

        /// <summary>
        /// NOTE: This was floatVal() in Lucene
        /// </summary>
        public override float SingleVal(int doc)
        {
            return (float)Int64Val(doc);
        }

        /// <summary>
        /// NOTE: This was intVal() in Lucene
        /// </summary>
        public override int Int32Val(int doc)
        {
            return (int)Int64Val(doc);
        }

        public override abstract long Int64Val(int doc);

        public override double DoubleVal(int doc)
        {
            return (double)Int64Val(doc);
        }

        public override bool BoolVal(int doc)
        {
            return Int64Val(doc) != 0;
        }

        public override string StrVal(int doc)
        {
            return Int64Val(doc).ToString(CultureInfo.InvariantCulture);
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? J2N.Numerics.Int64.GetInstance(Int64Val(doc)) : null; // LUCENENET: In Java, the conversion to instance of java.util.Long is implicit, but we need to do an explicit conversion
        }

        public override string ToString(int doc)
        {
            return m_vs.GetDescription() + '=' + StrVal(doc);
        }

        /// <summary>
        /// NOTE: This was externalToLong() in Lucene
        /// </summary>
        protected virtual long ExternalToInt64(string extVal)
        {
            return Convert.ToInt64(extVal, CultureInfo.InvariantCulture);
        }

        public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
        {
            long lower, upper;

            // instead of using separate comparison functions, adjust the endpoints.

            if (lowerVal is null)
            {
                lower = long.MinValue;
            }
            else
            {
                lower = ExternalToInt64(lowerVal);
                if (!includeLower && lower < long.MaxValue)
                {
                    lower++;
                }
            }

            if (upperVal is null)
            {
                upper = long.MaxValue;
            }
            else
            {
                upper = ExternalToInt64(upperVal);
                if (!includeUpper && upper > long.MinValue)
                {
                    upper--;
                }
            }

            long ll = lower;
            long uu = upper;

            return new ValueSourceScorer.AnonymousValueSourceScorer(reader, this, matchesValue: (doc) =>
            {
                long val = Int64Val(doc);
                // only check for deleted if it's the default value
                // if (val==0 && reader.isDeleted(doc)) return false;
                return val >= ll && val <= uu;
            });
        }

        public override ValueFiller GetValueFiller()
        {
            return new ValueFiller.AnonymousValueFiller<MutableValueInt64>(new MutableValueInt64(), fillValue: (doc, mutableValue) =>
            {
                mutableValue.Value = Int64Val(doc);
                mutableValue.Exists = Exists(doc);
            });
        }
    }
}