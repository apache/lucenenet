using System;
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
    /// Abstract <seealso cref="FunctionValues"/> implementation which supports retrieving boolean values.
    /// Implementations can control how the boolean values are loaded through <seealso cref="#BoolVal(int)"/>}
    /// </summary>
    public abstract class BoolDocValues : FunctionValues
    {
        protected internal readonly ValueSource vs;

        protected BoolDocValues(ValueSource vs)
        {
            this.vs = vs;
        }

        public override abstract bool BoolVal(int doc);

        public override sbyte ByteVal(int doc)
        {
            return BoolVal(doc) ? (sbyte)1 : (sbyte)0;
        }

        public override short ShortVal(int doc)
        {
            return BoolVal(doc) ? (short)1 : (short)0;
        }

        public override float FloatVal(int doc)
        {
            return BoolVal(doc) ? 1f : 0f;
        }

        public override int IntVal(int doc)
        {
            return BoolVal(doc) ? 1 : 0;
        }

        public override long LongVal(int doc)
        {
            return BoolVal(doc) ? 1 : 0;
        }

        public override double DoubleVal(int doc)
        {
            return BoolVal(doc) ? 1d : 0d;
        }

        public override string StrVal(int doc)
        {
            return Convert.ToString(BoolVal(doc));
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? BoolVal(doc) : (bool?)null;
        }

        public override string ToString(int doc)
        {
            return vs.GetDescription() + '=' + StrVal(doc);
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
            private readonly BoolDocValues outerInstance;

            public ValueFillerAnonymousInnerClassHelper(BoolDocValues outerInstance)
            {
                this.outerInstance = outerInstance;
                mval = new MutableValueBool();
            }

            private readonly MutableValueBool mval;

            public override MutableValue Value
            {
                get
                {
                    return mval;
                }
            }

            public override void FillValue(int doc)
            {
                mval.Value = outerInstance.BoolVal(doc);
                mval.Exists = outerInstance.Exists(doc);
            }
        }
    }
}