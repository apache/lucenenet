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
    /// <seealso cref="BoolFunction"/> implementation which applies an extendible boolean
    /// function to the values of a single wrapped <seealso cref="ValueSource"/>.
    /// 
    /// Functions this can be used for include whether a field has a value or not,
    /// or inverting the boolean value of the wrapped ValueSource.
    /// </summary>
    public abstract class SimpleBoolFunction : BoolFunction
    {
        protected internal readonly ValueSource source;

        protected SimpleBoolFunction(ValueSource source)
        {
            this.source = source;
        }

        protected abstract string Name { get; }

        protected abstract bool Func(int doc, FunctionValues vals);

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FunctionValues vals = source.GetValues(context, readerContext);
            return new BoolDocValuesAnonymousInnerClassHelper(this, this, vals);
        }

        private class BoolDocValuesAnonymousInnerClassHelper : BoolDocValues
        {
            private readonly SimpleBoolFunction outerInstance;

            private readonly FunctionValues vals;

            public BoolDocValuesAnonymousInnerClassHelper(SimpleBoolFunction outerInstance, SimpleBoolFunction @this, FunctionValues vals)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.vals = vals;
            }

            public override bool BoolVal(int doc)
            {
                return outerInstance.Func(doc, vals);
            }
            public override string ToString(int doc)
            {
                return outerInstance.Name + '(' + vals.ToString(doc) + ')';
            }
        }

        public override string GetDescription()
        {
            return Name + '(' + source.GetDescription() + ')';
        }

        public override int GetHashCode()
        {
            return source.GetHashCode() + Name.GetHashCode();
        }

        public override bool Equals(object o)
        {
            var other = o as SimpleBoolFunction;
            if (other == null)
                return false;
            return this.source.Equals(other.source);
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            source.CreateWeight(context, searcher);
        }
    }
}