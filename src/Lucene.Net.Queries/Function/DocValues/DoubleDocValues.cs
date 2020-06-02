using Lucene.Net.Index;
using Lucene.Net.Util.Mutable;
using System;

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

        public DoubleDocValues(ValueSource vs)
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

        public override string StrVal(int doc) // LUCENENET TODO: API - Add overload to include CultureInfo ?
        {
            return Convert.ToString(DoubleVal(doc));
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? DoubleVal(doc) : (double?)null;
        }

        public override string ToString(int doc)
        {
            return m_vs.GetDescription() + '=' + StrVal(doc);
        }

        public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal,
            bool includeLower, bool includeUpper) // LUCENENET TODO: API - Add overload to include CultureInfo ?
        {
            double lower, upper;

            if (lowerVal == null)
            {
                lower = double.NegativeInfinity;
            }
            else
            {
                lower = Convert.ToDouble(lowerVal);
            }

            if (upperVal == null)
            {
                upper = double.PositiveInfinity;
            }
            else
            {
                upper = Convert.ToDouble(upperVal);
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