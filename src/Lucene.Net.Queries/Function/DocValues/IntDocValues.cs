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
    /// Abstract <seealso cref="FunctionValues"/> implementation which supports retrieving int values.
    /// Implementations can control how the int values are loaded through <seealso cref="#IntVal(int)"/>
    /// </summary>
    public abstract class IntDocValues : FunctionValues
    {
        protected internal readonly ValueSource vs;

        public IntDocValues(ValueSource vs)
        {
            this.vs = vs;
        }

        public override sbyte ByteVal(int doc)
        {
            return (sbyte)IntVal(doc);
        }

        public override short ShortVal(int doc)
        {
            return (short)IntVal(doc);
        }

        public override float FloatVal(int doc)
        {
            return (float)IntVal(doc);
        }

        public override abstract int IntVal(int doc);

        public override long LongVal(int doc)
        {
            return (long)IntVal(doc);
        }

        public override double DoubleVal(int doc)
        {
            return (double)IntVal(doc);
        }

        public override string StrVal(int doc)
        {
            return Convert.ToString(IntVal(doc));
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? IntVal(doc) : (int?)null;
        }

        public override string ToString(int doc)
        {
            return vs.GetDescription() + '=' + StrVal(doc);
        }

        public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
        {
            int lower, upper;

            // instead of using separate comparison functions, adjust the endpoints.

            if (lowerVal == null)
            {
                lower = int.MinValue;
            }
            else
            {
                lower = Convert.ToInt32(lowerVal);
                if (!includeLower && lower < int.MaxValue)
                {
                    lower++;
                }
            }

            if (upperVal == null)
            {
                upper = int.MaxValue;
            }
            else
            {
                upper = Convert.ToInt32(upperVal);
                if (!includeUpper && upper > int.MinValue)
                {
                    upper--;
                }
            }

            int ll = lower;
            int uu = upper;

            return new ValueSourceScorerAnonymousInnerClassHelper(this, reader, this, ll, uu);
        }

        private class ValueSourceScorerAnonymousInnerClassHelper : ValueSourceScorer
        {
            private readonly IntDocValues outerInstance;

            private int ll;
            private int uu;

            public ValueSourceScorerAnonymousInnerClassHelper(IntDocValues outerInstance, IndexReader reader, IntDocValues @this, int ll, int uu)
                : base(reader, @this)
            {
                this.outerInstance = outerInstance;
                this.ll = ll;
                this.uu = uu;
            }

            public override bool MatchesValue(int doc)
            {
                int val = outerInstance.IntVal(doc);
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
            private readonly IntDocValues outerInstance;

            public ValueFillerAnonymousInnerClassHelper(IntDocValues outerInstance)
            {
                this.outerInstance = outerInstance;
                mval = new MutableValueInt();
            }

            private readonly MutableValueInt mval;

            public override MutableValue Value
            {
                get
                {
                    return mval;
                }
            }

            public override void FillValue(int doc)
            {
                mval.Value = outerInstance.IntVal(doc);
                mval.Exists = outerInstance.Exists(doc);
            }
        }
    }

}