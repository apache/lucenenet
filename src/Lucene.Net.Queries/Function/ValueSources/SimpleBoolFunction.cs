// Lucene version compatibility level 4.8.1
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
    /// <see cref="BoolFunction"/> implementation which applies an extendible <see cref="bool"/>
    /// function to the values of a single wrapped <see cref="ValueSource"/>.
    /// 
    /// Functions this can be used for include whether a field has a value or not,
    /// or inverting the <see cref="bool"/> value of the wrapped <see cref="ValueSource"/>.
    /// </summary>
    public abstract class SimpleBoolFunction : BoolFunction
    {
        protected readonly ValueSource m_source;

        protected SimpleBoolFunction(ValueSource source) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_source = source;
        }

        protected abstract string Name { get; }

        protected abstract bool Func(int doc, FunctionValues vals);

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FunctionValues vals = m_source.GetValues(context, readerContext);
            return new BoolDocValuesAnonymousClass(this, this, vals);
        }

        private sealed class BoolDocValuesAnonymousClass : BoolDocValues
        {
            private readonly SimpleBoolFunction outerInstance;

            private readonly FunctionValues vals;

            public BoolDocValuesAnonymousClass(SimpleBoolFunction outerInstance, SimpleBoolFunction @this, FunctionValues vals)
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
            return Name + '(' + m_source.GetDescription() + ')';
        }

        public override int GetHashCode()
        {
            return m_source.GetHashCode() + Name.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (!(o is SimpleBoolFunction other))
                return false;
            return this.m_source.Equals(other.m_source);
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            m_source.CreateWeight(context, searcher);
        }
    }
}