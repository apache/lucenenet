// Lucene version compatibility level 4.8.1
using J2N.Globalization;
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
    /// Abstract <see cref="FunctionValues"/> implementation which supports retrieving <see cref="double"/> values.
    /// Implementations can control how the <see cref="double"/> values are loaded through <see cref="DoubleVal(int)"/>
    /// </summary>
    public abstract class DoubleDocValues : FunctionValues
    {
        protected readonly ValueSource m_vs;

        protected DoubleDocValues(ValueSource vs) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_vs = vs;
        }

        public override byte ByteVal(int doc)
        {
            return (byte)DoubleVal(doc);
        }

        /// <summary>
        /// NOTE: This was shortVal() in Lucene
        /// </summary>
        public override short Int16Val(int doc)
        {
            return (short)DoubleVal(doc);
        }

        /// <summary>
        /// NOTE: This was floatVal() in Lucene
        /// </summary>
        public override float SingleVal(int doc)
        {
            return (float)DoubleVal(doc);
        }

        /// <summary>
        /// NOTE: This was intVal() in Lucene
        /// </summary>
        public override int Int32Val(int doc)
        {
            return (int)DoubleVal(doc);
        }

        /// <summary>
        /// NOTE: This was longVal() in Lucene
        /// </summary>
        public override long Int64Val(int doc)
        {
            return (long)DoubleVal(doc);
        }

        public override bool BoolVal(int doc)
        {
            return DoubleVal(doc) != 0;
        }

        public override abstract double DoubleVal(int doc);

        public override string StrVal(int doc)
        {
            return J2N.Numerics.Double.ToString(DoubleVal(doc), NumberFormatInfo.InvariantInfo); // LUCENENET: Use J2N to mimic the Java string format using the "J" format
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? J2N.Numerics.Double.GetInstance(DoubleVal(doc)) : null; // LUCENENET: In Java, the conversion to instance of java.util.Double is implicit, but we need to do an explicit conversion
        }

        public override string ToString(int doc)
        {
            return m_vs.GetDescription() + '=' + StrVal(doc);
        }

        public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal,
            bool includeLower, bool includeUpper)
        {
            double lower, upper;

            if (lowerVal is null)
            {
                lower = double.NegativeInfinity;
            }
            else
            {
                lower = J2N.Numerics.Double.Parse(lowerVal, NumberStyle.Float, NumberFormatInfo.InvariantInfo);
            }

            if (upperVal is null)
            {
                upper = double.PositiveInfinity;
            }
            else
            {
                upper = J2N.Numerics.Double.Parse(upperVal, NumberStyle.Float, NumberFormatInfo.InvariantInfo);
            }

            double l = lower;
            double u = upper;
            if (includeLower && includeUpper)
            {
                return new ValueSourceScorer.AnonymousValueSourceScorer(reader, this, matchesValue: (doc) =>
                {
                    double docVal = DoubleVal(doc);
                    return docVal >= l && docVal <= u;
                });
            }
            else if (includeLower && !includeUpper)
            {
                return new ValueSourceScorer.AnonymousValueSourceScorer(reader, this, matchesValue: (doc) =>
                {
                    double docVal = DoubleVal(doc);
                    return docVal >= l && docVal < u;
                });
            }
            else if (!includeLower && includeUpper)
            {
                return new ValueSourceScorer.AnonymousValueSourceScorer(reader, this, matchesValue: (doc) =>
                {
                    double docVal = DoubleVal(doc);
                    return docVal > l && docVal <= u;
                });
            }
            else
            {
                return new ValueSourceScorer.AnonymousValueSourceScorer(reader, this, matchesValue: (doc) =>
                {
                    double docVal = DoubleVal(doc);
                    return docVal > l && docVal < u;
                });
            }
        }

        public override ValueFiller GetValueFiller()
        {
            return new ValueFiller.AnonymousValueFiller<MutableValueDouble>(new MutableValueDouble(), fillValue: (doc, mutableValue) =>
            {
                mutableValue.Value = DoubleVal(doc);
                mutableValue.Exists = Exists(doc);
            });
        }
    }
}