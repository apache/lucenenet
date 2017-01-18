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
using System;
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// Function that returns a constant double value for every document.
    /// </summary>
    public class DoubleConstValueSource : ConstNumberSource
    {
        internal readonly double constant;
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

            public override float FloatVal(int doc)
            {
                return outerInstance.fv;
            }

            public override int IntVal(int doc)
            {
                return (int)outerInstance.lv;
            }

            public override long LongVal(int doc)
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
            long bits = Lucene.Net.Support.Number.DoubleToRawLongBits(constant);
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

        public override int Int
        {
            get
            {
                return (int)lv;
            }
        }

        public override long Long
        {
            get
            {
                return lv;
            }
        }

        public override float Float
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

        public override bool Bool
        {
            get
            {
                return constant != 0;
            }
        }
    }
}