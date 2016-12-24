using System.Collections;
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
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// A simple float function with a single argument
    /// </summary>
     // LUCENENET TODO: Rename SimpleSingleFunction ?
    public abstract class SimpleFloatFunction : SingleFunction
    {
        protected SimpleFloatFunction(ValueSource source)
            : base(source)
        {
        }

        protected abstract float Func(int doc, FunctionValues vals);

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FunctionValues vals = source.GetValues(context, readerContext);
            return new FloatDocValuesAnonymousInnerClassHelper(this, this, vals);
        }

        private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
        {
            private readonly SimpleFloatFunction outerInstance;
            private readonly FunctionValues vals;

            public FloatDocValuesAnonymousInnerClassHelper(SimpleFloatFunction outerInstance, SimpleFloatFunction @this, FunctionValues vals)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.vals = vals;
            }

            public override float FloatVal(int doc)
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