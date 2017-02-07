using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using System;
using System.Collections;

namespace Lucene.Net.Queries.Function.ValueSources
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
    /// Function that returns a constant double value for every document.
    /// </summary>
    public class DoubleConstValueSource : ConstNumberSource
    {
        private readonly double constant;
        private readonly float fv;
        private readonly long lv;

        public DoubleConstValueSource(double constant)
        {
            this.constant = constant;
            this.fv = (float)constant;
            this.lv = (long)constant;
        }

        public override string GetDescription()
        {
            return "const(" + constant + ")";
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            return new DoubleDocValuesAnonymousInnerClassHelper(this, this);
        }

        private class DoubleDocValuesAnonymousInnerClassHelper : DoubleDocValues
        {
            private readonly DoubleConstValueSource outerInstance;

            public DoubleDocValuesAnonymousInnerClassHelper(DoubleConstValueSource outerInstance, DoubleConstValueSource @this)
                : base(@this)
            {
                this.outerInstance = outerInstance;
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return outerInstance.fv;
            }

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override int Int32Val(int doc)
            {
                return (int)outerInstance.lv;
            }

            /// <summary>
            /// NOTE: This was longVal() in Lucene
            /// </summary>
            public override long Int64Val(int doc)
            {
                return outerInstance.lv;
            }

            public override double DoubleVal(int doc)
            {
                return outerInstance.constant;
            }

            public override string StrVal(int doc)
            {
                return Convert.ToString(outerInstance.constant);
            }

            public override object ObjectVal(int doc)
            {
                return outerInstance.constant;
            }

            public override string ToString(int doc)
            {
                return outerInstance.GetDescription();
            }
        }

        public override int GetHashCode()
        {
            long bits = Lucene.Net.Support.Number.DoubleToRawInt64Bits(constant);
            return (int)(bits ^ ((long)((ulong)bits >> 32)));
        }

        public override bool Equals(object o)
        {
            var other = o as DoubleConstValueSource;
            if (other == null)
            {
                return false;
            }
            return this.constant == other.constant;
        }

        /// <summary>
        /// NOTE: This was getInt() in Lucene
        /// </summary>
        public override int Int32
        {
            get
            {
                return (int)lv;
            }
        }

        /// <summary>
        /// NOTE: This was getLong() in Lucene
        /// </summary>
        public override long Int64
        {
            get
            {
                return lv;
            }
        }

        /// <summary>
        /// NOTE: This was getFloat() in Lucene
        /// </summary>
        public override float Single
        {
            get
            {
                return fv;
            }
        }

        public override double Double
        {
            get
            {
                return constant;
            }
        }

        // LUCENENET NOTE: getNumber() not supported

        public override bool Bool
        {
            get
            {
                return constant != 0;
            }
        }
    }
}