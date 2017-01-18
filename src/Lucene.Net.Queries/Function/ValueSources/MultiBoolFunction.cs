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
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// Abstract <seealso cref="ValueSource"/> implementation which wraps multiple ValueSources
    /// and applies an extendible boolean function to their values.
    /// 
    /// </summary>
    public abstract class MultiBoolFunction : BoolFunction
    {
        protected readonly IList<ValueSource> sources;

        protected MultiBoolFunction(IList<ValueSource> sources)
        {
            this.sources = sources;
        }

        protected abstract string Name { get; }

        protected abstract bool Func(int doc, FunctionValues[] vals);

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var vals = new FunctionValues[sources.Count];
            int i = 0;
            foreach (ValueSource source in sources)
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
            foreach (ValueSource source in sources)
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
            return sources.GetHashCode() + Name.GetHashCode();
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
            return this.sources.Equals(other.sources);
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            foreach (ValueSource source in sources)
            {
                source.CreateWeight(context, searcher);
            }
        }
    }
}