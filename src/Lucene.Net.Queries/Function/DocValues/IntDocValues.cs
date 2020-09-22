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
    /// Abstract <see cref="FunctionValues"/> implementation which supports retrieving <see cref="int"/> values.
    /// Implementations can control how the <see cref="int"/> values are loaded through <see cref="Int32Val(int)"/>
    /// <para/>
    /// NOTE: This was IntDocValues in Lucene
    /// </summary>
    public abstract class Int32DocValues : FunctionValues
    {
        protected readonly ValueSource m_vs;

        public Int32DocValues(ValueSource vs)
        {
            this.m_vs = vs;
        }

        public override byte ByteVal(int doc)
        {
            return (byte)Int32Val(doc);
        }

        /// <summary>
        /// NOTE: This was shortVal() in Lucene
        /// </summary>
        public override short Int16Val(int doc)
        {
            return (short)Int32Val(doc);
        }

        /// <summary>
        /// NOTE: This was floatVal() in Lucene
        /// </summary>
        public override float SingleVal(int doc)
        {
            return (float)Int32Val(doc);
        }

        /// <summary>
        /// NOTE: This was intVal() in Lucene
        /// </summary>
        public override abstract int Int32Val(int doc);

        /// <summary>
        /// NOTE: This was longVal() in Lucene
        /// </summary>
        public override long Int64Val(int doc)
        {
            return (long)Int32Val(doc);
        }

        public override double DoubleVal(int doc)
        {
            return (double)Int32Val(doc);
        }

        public override string StrVal(int doc)
        {
            return Int32Val(doc).ToString(CultureInfo.InvariantCulture);
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? Int32Val(doc) : (int?)null;
        }

        public override string ToString(int doc)
        {
            return m_vs.GetDescription() + '=' + StrVal(doc);
        }

        public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
        {
            int lower, upper;

            // instead of using separate comparison functions, adjust the endpoints.

            if (lowerVal is null)
            {
                lower = int.MinValue;
            }
            else
            {
                lower = Convert.ToInt32(lowerVal, CultureInfo.InvariantCulture);
                if (!includeLower && lower < int.MaxValue)
                {
                    lower++;
                }
            }

            if (upperVal is null)
            {
                upper = int.MaxValue;
            }
            else
            {
                upper = Convert.ToInt32(upperVal, CultureInfo.InvariantCulture);
                if (!includeUpper && upper > int.MinValue)
                {
                    upper--;
                }
            }

            int ll = lower;
            int uu = upper;

            return new ValueSourceScorer.AnonymousValueSourceScorer(reader, this, matchesValue: (doc) =>
            {
                int val = Int32Val(doc);
                // only check for deleted if it's the default value
                // if (val==0 && reader.isDeleted(doc)) return false;
                return val >= ll && val <= uu;
            });
        }

        public override ValueFiller GetValueFiller()
        {
            return new ValueFiller.AnonymousValueFiller<MutableValueInt32>(new MutableValueInt32(), fillValue: (doc, mutableValue) =>
            {
                mutableValue.Value = Int32Val(doc);
                mutableValue.Exists = Exists(doc);
            });
        }
    }
}