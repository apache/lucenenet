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
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// Abstract <seealso cref="ValueSource"/> implementation which wraps two ValueSources
    /// and applies an extendible float function to their values.
    /// 
    /// </summary>
    public abstract class DualFloatFunction : ValueSource
    {
        protected internal readonly ValueSource a;
        protected internal readonly ValueSource b;

        /// <param name="a">  the base. </param>
        /// <param name="b">  the exponent. </param>
        protected DualFloatFunction(ValueSource a, ValueSource b)
        {
            this.a = a;
            this.b = b;
        }

        protected abstract string Name { get; }
        protected abstract float Func(int doc, FunctionValues aVals, FunctionValues bVals);

        public override string GetDescription()
        {
            return Name + "(" + a.GetDescription() + "," + b.GetDescription() + ")";
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FunctionValues aVals = a.GetValues(context, readerContext);
            FunctionValues bVals = b.GetValues(context, readerContext);
            return new FloatDocValuesAnonymousInnerClassHelper(this, this, aVals, bVals);
        }

        private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
        {
            private readonly DualFloatFunction outerInstance;

            private readonly FunctionValues aVals;
            private readonly FunctionValues bVals;

            public FloatDocValuesAnonymousInnerClassHelper(DualFloatFunction outerInstance, DualFloatFunction @this, FunctionValues aVals, FunctionValues bVals)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.aVals = aVals;
                this.bVals = bVals;
            }

            public override float FloatVal(int doc)
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
            a.CreateWeight(context, searcher);
            b.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            int h = a.GetHashCode();
            h ^= (h << 13) | ((int)((uint)h >> 20));
            h += b.GetHashCode();
            h ^= (h << 23) | ((int)((uint)h >> 10));
            h += Name.GetHashCode();
            return h;
        }

        public override bool Equals(object o)
        {
            var other = o as DualFloatFunction;
            if (other == null)
            {
                return false;
            }
            return this.a.Equals(other.a) && this.b.Equals(other.b);
        }
    }
}