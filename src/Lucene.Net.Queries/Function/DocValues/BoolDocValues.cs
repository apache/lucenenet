// Lucene version compatibility level 4.8.1
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
    /// Abstract <see cref="FunctionValues"/> implementation which supports retrieving <see cref="bool"/> values.
    /// Implementations can control how the <see cref="bool"/> values are loaded through <see cref="BoolVal(int)"/>
    /// </summary>
    public abstract class BoolDocValues : FunctionValues
    {
        protected readonly ValueSource m_vs;

        protected BoolDocValues(ValueSource vs) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_vs = vs;
        }

        public override abstract bool BoolVal(int doc);

        public override byte ByteVal(int doc)
        {
            return BoolVal(doc) ? (byte)1 : (byte)0;
        }

        /// <summary>
        /// NOTE: This was shortVal() in Lucene
        /// </summary>
        public override short Int16Val(int doc)
        {
            return BoolVal(doc) ? (short)1 : (short)0;
        }

        /// <summary>
        /// NOTE: This was floatVal() in Lucene
        /// </summary>
        public override float SingleVal(int doc)
        {
            return BoolVal(doc) ? 1f : 0f;
        }

        /// <summary>
        /// NOTE: This was intVal() in Lucene
        /// </summary>
        public override int Int32Val(int doc)
        {
            return BoolVal(doc) ? 1 : 0;
        }

        /// <summary>
        /// NOTE: This was longVal() in Lucene
        /// </summary>
        public override long Int64Val(int doc)
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
            return Exists(doc) ? J2N.Numerics.Int32.GetInstance(Int32Val(doc)) : null; // LUCENENET TODO: Create Boolean reference type in J2N to return here (and format, etc)
        }

        public override string ToString(int doc)
        {
            return m_vs.GetDescription() + '=' + StrVal(doc);
        }

        public override ValueFiller GetValueFiller()
        {
            return new ValueFiller.AnonymousValueFiller<MutableValueBool>(new MutableValueBool(), fillValue: (doc, mutableValue) =>
            {
                mutableValue.Value = BoolVal(doc);
                mutableValue.Exists = Exists(doc);
            });
        }
    }
}