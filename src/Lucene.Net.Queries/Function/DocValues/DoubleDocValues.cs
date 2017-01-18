using System;
using Lucene.Net.Index;
using Lucene.Net.Util.Mutable;

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
    /// Abstract <seealso cref="FunctionValues"/> implementation which supports retrieving double values.
    /// Implementations can control how the double values are loaded through <seealso cref="#DoubleVal(int)"/>}
    /// </summary>
    public abstract class DoubleDocValues : FunctionValues
    {
        protected internal readonly ValueSource vs;

        protected DoubleDocValues(ValueSource vs)
        {
            this.vs = vs;
        }

        public override sbyte ByteVal(int doc)
        {
            return (sbyte)DoubleVal(doc);
        }

        public override short ShortVal(int doc)
        {
            return (short)DoubleVal(doc);
        }

        public override float FloatVal(int doc)
        {
            return (float)DoubleVal(doc);
        }

        public override int IntVal(int doc)
        {
            return (int)DoubleVal(doc);
        }

        public override long LongVal(int doc)
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
            return Convert.ToString(DoubleVal(doc));
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? DoubleVal(doc) : (double?)null;
        }

        public override string ToString(int doc)
        {
            return vs.GetDescription() + '=' + StrVal(doc);
        }

        public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal,
            bool includeLower, bool includeUpper)
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
                return new ValueSourceScorerAnonymousInnerClassHelper(this, reader, this, l, u);
            }
            else if (includeLower && !includeUpper)
            {
                return new ValueSourceScorerAnonymousInnerClassHelper2(this, reader, this, l, u);
            }
            else if (!includeLower && includeUpper)
            {
                return new ValueSourceScorerAnonymousInnerClassHelper3(this, reader, this, l, u);
            }
            else
            {
                return new ValueSourceScorerAnonymousInnerClassHelper4(this, reader, this, l, u);
            }
        }

        private class ValueSourceScorerAnonymousInnerClassHelper : ValueSourceScorer
        {
            private readonly DoubleDocValues outerInstance;

            private double l;
            private double u;

            public ValueSourceScorerAnonymousInnerClassHelper(DoubleDocValues outerInstance, IndexReader reader, DoubleDocValues @this, double l, double u)
                : base(reader, @this)
            {
                this.outerInstance = outerInstance;
                this.l = l;
                this.u = u;
            }

            public override bool MatchesValue(int doc)
            {
                double docVal = outerInstance.DoubleVal(doc);
                return docVal >= l && docVal <= u;
            }
        }

        private class ValueSourceScorerAnonymousInnerClassHelper2 : ValueSourceScorer
        {
            private readonly DoubleDocValues outerInstance;

            private double l;
            private double u;

            public ValueSourceScorerAnonymousInnerClassHelper2(DoubleDocValues outerInstance, IndexReader reader, DoubleDocValues @this, double l, double u)
                : base(reader, @this)
            {
                this.outerInstance = outerInstance;
                this.l = l;
                this.u = u;
            }

            public override bool MatchesValue(int doc)
            {
                double docVal = outerInstance.DoubleVal(doc);
                return docVal >= l && docVal < u;
            }
        }

        private class ValueSourceScorerAnonymousInnerClassHelper3 : ValueSourceScorer
        {
            private readonly DoubleDocValues outerInstance;

            private double l;
            private double u;

            public ValueSourceScorerAnonymousInnerClassHelper3(DoubleDocValues outerInstance, IndexReader reader, DoubleDocValues @this, double l, double u)
                : base(reader, @this)
            {
                this.outerInstance = outerInstance;
                this.l = l;
                this.u = u;
            }

            public override bool MatchesValue(int doc)
            {
                double docVal = outerInstance.DoubleVal(doc);
                return docVal > l && docVal <= u;
            }
        }

        private class ValueSourceScorerAnonymousInnerClassHelper4 : ValueSourceScorer
        {
            private readonly DoubleDocValues outerInstance;

            private double l;
            private double u;

            public ValueSourceScorerAnonymousInnerClassHelper4(DoubleDocValues outerInstance, IndexReader reader,
                DoubleDocValues @this, double l, double u)
                : base(reader, @this)
            {
                this.outerInstance = outerInstance;
                this.l = l;
                this.u = u;
            }

            public override bool MatchesValue(int doc)
            {
                double docVal = outerInstance.DoubleVal(doc);
                return docVal > l && docVal < u;
            }
        }

        public override AbstractValueFiller ValueFiller
        {
            get
            {
                return new ValueFillerAnonymousInnerClassHelper(this);
            }
        }

        private class ValueFillerAnonymousInnerClassHelper : AbstractValueFiller
        {
            private readonly DoubleDocValues outerInstance;

            public ValueFillerAnonymousInnerClassHelper(DoubleDocValues outerInstance)
            {
                this.outerInstance = outerInstance;
                mval = new MutableValueDouble();
            }

            private readonly MutableValueDouble mval;

            public override MutableValue Value
            {
                get
                {
                    return mval;
                }
            }

            public override void FillValue(int doc)
            {
                mval.Value = outerInstance.DoubleVal(doc);
                mval.Exists = outerInstance.Exists(doc);
            }
        }
    }
}