// Lucene version compatibility level 4.8.1
using J2N.Numerics;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
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
    /// Abstract <see cref="ValueSource"/> implementation which wraps two <see cref="ValueSource"/>s
    /// and applies an extendible <see cref="float"/> function to their values.
    /// <para/>
    /// NOTE: This was DualFloatFunction in Lucene
    /// </summary>
    public abstract class DualSingleFunction : ValueSource
    {
        protected readonly ValueSource m_a;
        protected readonly ValueSource m_b;

        /// <param name="a">  the base. </param>
        /// <param name="b">  the exponent. </param>
        protected DualSingleFunction(ValueSource a, ValueSource b) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_a = a;
            this.m_b = b;
        }

        protected abstract string Name { get; }
        protected abstract float Func(int doc, FunctionValues aVals, FunctionValues bVals);

        public override string GetDescription()
        {
            return Name + "(" + m_a.GetDescription() + "," + m_b.GetDescription() + ")";
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FunctionValues aVals = m_a.GetValues(context, readerContext);
            FunctionValues bVals = m_b.GetValues(context, readerContext);
            return new SingleDocValuesAnonymousClass(this, this, aVals, bVals);
        }

        private sealed class SingleDocValuesAnonymousClass : SingleDocValues
        {
            private readonly DualSingleFunction outerInstance;

            private readonly FunctionValues aVals;
            private readonly FunctionValues bVals;

            public SingleDocValuesAnonymousClass(DualSingleFunction outerInstance, DualSingleFunction @this, FunctionValues aVals, FunctionValues bVals)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.aVals = aVals;
                this.bVals = bVals;
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return outerInstance.Func(doc, aVals, bVals);
            }

            public override string ToString(int doc)
            {
                return outerInstance.Name + '(' + aVals.ToString(doc) + ',' + bVals.ToString(doc) + ')';
            }
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            m_a.CreateWeight(context, searcher);
            m_b.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            int h = m_a.GetHashCode();
            h ^= (h << 13) | (h.TripleShift(20));
            h += m_b.GetHashCode();
            h ^= (h << 23) | (h.TripleShift(10));
            h += Name.GetHashCode();
            return h;
        }

        public override bool Equals(object o)
        {
            if (!(o is DualSingleFunction other))
            {
                return false;
            }
            return this.m_a.Equals(other.m_a) && this.m_b.Equals(other.m_b);
        }
    }
}