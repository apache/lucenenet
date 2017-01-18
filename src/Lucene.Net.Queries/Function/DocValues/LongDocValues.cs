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
    /// Abstract <seealso cref="FunctionValues"/> implementation which supports retrieving long values.
    /// Implementations can control how the long values are loaded through <seealso cref="#LongVal(int)"/>}
    /// </summary>
    public abstract class LongDocValues : FunctionValues
    {
        protected internal readonly ValueSource vs;

        protected LongDocValues(ValueSource vs)
        {
            this.vs = vs;
        }

        public override sbyte ByteVal(int doc)
        {
            return (sbyte)LongVal(doc);
        }

        public override short ShortVal(int doc)
        {
            return (short)LongVal(doc);
        }

        public override float FloatVal(int doc)
        {
            return (float)LongVal(doc);
        }

        public override int IntVal(int doc)
        {
            return (int)LongVal(doc);
        }

        public override abstract long LongVal(int doc);

        public override double DoubleVal(int doc)
        {
            return (double)LongVal(doc);
        }

        public override bool BoolVal(int doc)
        {
            return LongVal(doc) != 0;
        }

        public override string StrVal(int doc)
        {
            return Convert.ToString(LongVal(doc));
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? LongVal(doc) : (long?)null;
        }

        public override string ToString(int doc)
        {
            return vs.GetDescription() + '=' + StrVal(doc);
        }

        protected virtual long ExternalToLong(string extVal)
        {
            return Convert.ToInt64(extVal);
        }

        public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
        {
            long lower, upper;

            // instead of using separate comparison functions, adjust the endpoints.

            if (lowerVal == null)
            {
                lower = long.MinValue;
            }
            else
            {
                lower = ExternalToLong(lowerVal);
                if (!includeLower && lower < long.MaxValue)
                {
                    lower++;
                }
            }

            if (upperVal == null)
            {
                upper = long.MaxValue;
            }
            else
            {
                upper = ExternalToLong(upperVal);
                if (!includeUpper && upper > long.MinValue)
                {
                    upper--;
                }
            }

            long ll = lower;
            long uu = upper;

            return new ValueSourceScorerAnonymousInnerClassHelper(this, reader, this, ll, uu);
        }

        private class ValueSourceScorerAnonymousInnerClassHelper : ValueSourceScorer
        {
            private readonly LongDocValues outerInstance;

            private readonly long ll;
            private readonly long uu;

            public ValueSourceScorerAnonymousInnerClassHelper(LongDocValues outerInstance, IndexReader reader, LongDocValues @this, long ll, long uu)
                : base(reader, @this)
            {
                this.outerInstance = outerInstance;
                this.ll = ll;
                this.uu = uu;
            }

            public override bool MatchesValue(int doc)
            {
                long val = outerInstance.LongVal(doc);
                // only check for deleted if it's the default value
                // if (val==0 && reader.isDeleted(doc)) return false;
                return val >= ll && val <= uu;
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
            private readonly LongDocValues outerInstance;

            public ValueFillerAnonymousInnerClassHelper(LongDocValues outerInstance)
            {
                this.outerInstance = outerInstance;
                mval = new MutableValueLong();
            }

            private readonly MutableValueLong mval;

            public override MutableValue Value
            {
                get
                {
                    return mval;
                }
            }

            public override void FillValue(int doc)
            {
                mval.Value = outerInstance.LongVal(doc);
                mval.Exists = outerInstance.Exists(doc);
            }
        }
    }

}