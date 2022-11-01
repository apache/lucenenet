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
    /// A simple <see cref="float"/> function with a single argument
    /// <para/>
    /// NOTE: This was SimpleFloatFunction in Lucene
    /// </summary>
    public abstract class SimpleSingleFunction : SingularFunction
    {
        protected SimpleSingleFunction(ValueSource source) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : base(source)
        {
        }

        protected abstract float Func(int doc, FunctionValues vals);

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FunctionValues vals = m_source.GetValues(context, readerContext);
            return new SingleDocValuesAnonymousClass(this, this, vals);
        }

        private sealed class SingleDocValuesAnonymousClass : SingleDocValues
        {
            private readonly SimpleSingleFunction outerInstance;
            private readonly FunctionValues vals;

            public SingleDocValuesAnonymousClass(SimpleSingleFunction outerInstance, SimpleSingleFunction @this, FunctionValues vals)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.vals = vals;
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return outerInstance.Func(doc, vals);
            }
            public override string ToString(int doc)
            {
                return outerInstance.Name + '(' + vals.ToString(doc) + ')';
            }
        }
    }
}