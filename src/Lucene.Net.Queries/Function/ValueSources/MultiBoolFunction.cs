using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System.Collections;
using System.Collections.Generic;
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
    /// and applies an extendible <see cref="bool"/> function to their values.
    /// </summary>
    public abstract class MultiBoolFunction : BoolFunction
    {
        protected readonly IList<ValueSource> m_sources;

        public MultiBoolFunction(IList<ValueSource> sources)
        {
            this.m_sources = sources;
        }

        protected abstract string Name { get; }

        protected abstract bool Func(int doc, FunctionValues[] vals);

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var vals = new FunctionValues[m_sources.Count];
            int i = 0;
            foreach (ValueSource source in m_sources)
            {
                vals[i++] = source.GetValues(context, readerContext);
            }

            return new BoolDocValuesAnonymousInnerClassHelper(this, this, vals);
        }

        private class BoolDocValuesAnonymousInnerClassHelper : BoolDocValues
        {
            private readonly MultiBoolFunction outerInstance;

            private readonly FunctionValues[] vals;

            public BoolDocValuesAnonymousInnerClassHelper(MultiBoolFunction outerInstance, MultiBoolFunction @this, FunctionValues[] vals)
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
                var sb = new StringBuilder(outerInstance.Name);
                sb.Append('(');
                bool first = true;
                foreach (FunctionValues dv in vals)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }
                    sb.Append(dv.ToString(doc));
                }
                return sb.ToString();
            }
        }

        public override string GetDescription()
        {
            var sb = new StringBuilder(Name);
            sb.Append('(');
            bool first = true;
            foreach (ValueSource source in m_sources)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(',');
                }
                sb.Append(source.GetDescription());
            }
            return sb.ToString();
        }

        public override int GetHashCode()
        {
            // LUCENENET specific: ensure our passed in list is equatable by
            // wrapping it in an EquatableList if it is not one already
            return Equatable.Wrap(m_sources).GetHashCode() + Name.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (this.GetType() != o.GetType())
            {
                return false;
            }
            var other = o as MultiBoolFunction;
            if (other == null)
                return false;

            // LUCENENET specific: ensure our passed in list is equatable by
            // wrapping it in an EquatableList if it is not one already
            return Equatable.Wrap(this.m_sources).Equals(other.m_sources);
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            foreach (ValueSource source in m_sources)
            {
                source.CreateWeight(context, searcher);
            }
        }
    }
}