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
    /// Abstract <see cref="FunctionValues"/> implementation which supports retrieving <see cref="float"/> values.
    /// Implementations can control how the <see cref="float"/> values are loaded through <see cref="SingleVal(int)"/>
    /// <para/>
    /// NOTE: This was FloatDocValues in Lucene
    /// </summary>
    public abstract class SingleDocValues : FunctionValues
    {
        protected readonly ValueSource m_vs;

        public SingleDocValues(ValueSource vs)
        {
            this.m_vs = vs;
        }

        public override byte ByteVal(int doc)
        {
            return (byte)SingleVal(doc);
        }

        /// <summary>
        /// NOTE: This was shortVal() in Lucene
        /// </summary>
        public override short Int16Val(int doc)
        {
            return (short)SingleVal(doc);
        }

        /// <summary>
        /// NOTE: This was floatVal() in Lucene
        /// </summary>
        public override abstract float SingleVal(int doc);

        /// <summary>
        /// NOTE: This was intVal() in Lucene
        /// </summary>
        public override int Int32Val(int doc)
        {
            return (int)SingleVal(doc);
        }

        /// <summary>
        /// NOTE: This was longVal() in Lucene
        /// </summary>
        public override long Int64Val(int doc)
        {
            return (long)SingleVal(doc);
        }

        public override double DoubleVal(int doc)
        {
            return (double)SingleVal(doc);
        }

        public override string StrVal(int doc)
        {
            return Convert.ToString(SingleVal(doc));
        }

        public override object ObjectVal(int doc)
        {
            return Exists(doc) ? SingleVal(doc) : (float?)null;
        }

        public override string ToString(int doc)
        {
            return m_vs.GetDescription() + '=' + StrVal(doc);
        }

        public override ValueFiller GetValueFiller()
        {
            return new ValueFillerAnonymousInnerClassHelper(this);
        }

        private class ValueFillerAnonymousInnerClassHelper : ValueFiller
        {
            private readonly SingleDocValues outerInstance;

            public ValueFillerAnonymousInnerClassHelper(SingleDocValues outerInstance)
            {
                this.outerInstance = outerInstance;
                mval = new MutableValueSingle();
            }

            private readonly MutableValueSingle mval;

            public override MutableValue Value
            {
                get
                {
                    return mval;
                }
            }

            public override void FillValue(int doc)
            {
                mval.Value = outerInstance.SingleVal(doc);
                mval.Exists = outerInstance.Exists(doc);
            }
        }
    }
}