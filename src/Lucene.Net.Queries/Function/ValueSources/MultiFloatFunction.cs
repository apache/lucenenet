// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Text;

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
    /// Abstract <see cref="ValueSource"/> implementation which wraps multiple <see cref="ValueSource"/>s
    /// and applies an extendible <see cref="float"/> function to their values.
    /// <para/>
    /// NOTE: This was MultiFloatFunction in Lucene
    /// </summary>
    public abstract class MultiSingleFunction : ValueSource
    {
        protected readonly ValueSource[] m_sources;

        protected MultiSingleFunction(ValueSource[] sources) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_sources = sources;
        }

        protected abstract string Name { get; }

        protected abstract float Func(int doc, FunctionValues[] valsArr);

        public override string GetDescription()
        {
            var sb = new StringBuilder();
            sb.Append(Name).Append('(');
            bool firstTime = true;
            foreach (var source in m_sources)
            {
                if (firstTime)
                {
                    firstTime = false;
                }
                else
                {
                    sb.Append(',');
                }
                sb.Append(source);
            }
            sb.Append(')');
            return sb.ToString();
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var valsArr = new FunctionValues[m_sources.Length];
            for (int i = 0; i < m_sources.Length; i++)
            {
                valsArr[i] = m_sources[i].GetValues(context, readerContext);
            }

            return new SingleDocValuesAnonymousClass(this, this, valsArr);
        }

        private sealed class SingleDocValuesAnonymousClass : SingleDocValues
        {
            private readonly MultiSingleFunction outerInstance;

            private readonly FunctionValues[] valsArr;

            public SingleDocValuesAnonymousClass(MultiSingleFunction outerInstance, MultiSingleFunction @this, FunctionValues[] valsArr)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.valsArr = valsArr;
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return outerInstance.Func(doc, valsArr);
            }
            public override string ToString(int doc)
            {
                var sb = new StringBuilder();
                sb.Append(outerInstance.Name).Append('(');
                bool firstTime = true;
                foreach (FunctionValues vals in valsArr)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }
                    sb.Append(vals.ToString(doc));
                }
                sb.Append(')');
                return sb.ToString();
            }
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            foreach (ValueSource source in m_sources)
            {
                source.CreateWeight(context, searcher);
            }
        }

        public override int GetHashCode()
        {
            return Arrays.GetHashCode(m_sources) + Name.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (this.GetType() != o.GetType())
            {
                return false;
            }
            if (!(o is MultiSingleFunction other))
                return false;
            return Name.Equals(other.Name, StringComparison.Ordinal) && Arrays.Equals(this.m_sources, other.m_sources);
        }
    }
}