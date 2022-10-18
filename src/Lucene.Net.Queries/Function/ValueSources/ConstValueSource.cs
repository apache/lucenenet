// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
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
    /// <see cref="ConstValueSource"/> returns a constant for all documents
    /// </summary>
    public class ConstValueSource : ConstNumberSource
    {
        private readonly float constant;
        private readonly double dv;

        public ConstValueSource(float constant)
        {
            this.constant = constant;
            this.dv = constant;
        }

        public override string GetDescription()
        {
            return "const(" + constant + ")";
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            return new SingleDocValuesAnonymousClass(this, this);
        }

        private sealed class SingleDocValuesAnonymousClass : SingleDocValues
        {
            private readonly ConstValueSource outerInstance;

            public SingleDocValuesAnonymousClass(ConstValueSource outerInstance, ConstValueSource @this)
                : base(@this)
            {
                this.outerInstance = outerInstance;
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return outerInstance.constant;
            }

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override int Int32Val(int doc)
            {
                return (int)outerInstance.constant;
            }

            /// <summary>
            /// NOTE: This was longVal() in Lucene
            /// </summary>
            public override long Int64Val(int doc)
            {
                return (long)outerInstance.constant;
            }
            public override double DoubleVal(int doc)
            {
                return outerInstance.dv;
            }
            public override string ToString(int doc)
            {
                return outerInstance.GetDescription();
            }
            public override object ObjectVal(int doc)
            {
                return J2N.Numerics.Single.GetInstance(outerInstance.constant); // LUCENENET: In Java, the conversion to instance of java.util.Float is implicit, but we need to do an explicit conversion
            }
            public override bool BoolVal(int doc)
            {
                return outerInstance.constant != 0.0f;
            }
        }

        public override int GetHashCode()
        {
            return J2N.BitConversion.SingleToInt32Bits(constant) * 31;
        }

        public override bool Equals(object o)
        {
            if (!(o is ConstValueSource other))
            {
                return false;
            }
            return this.constant == other.constant;
        }

        /// <summary>
        /// NOTE: This was getInt() in Lucene
        /// </summary>
        public override int Int32 => (int)constant;

        /// <summary>
        /// NOTE: This was getLong() in Lucene
        /// </summary>
        public override long Int64 => (long)constant;

        /// <summary>
        /// NOTE: This was getFloat() in Lucene
        /// </summary>
        public override float Single => constant;

        public override double Double => dv;

        // LUCENENET NOTE: getNumber() not supported

        public override bool Bool => constant != 0.0f;
    }
}