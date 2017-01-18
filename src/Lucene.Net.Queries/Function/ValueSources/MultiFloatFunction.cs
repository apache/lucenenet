using System.Collections;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Support;

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
    /// Abstract <seealso cref="ValueSource"/> implementation which wraps multiple ValueSources
    /// and applies an extendible float function to their values.
    /// 
    /// </summary>
    public abstract class MultiFloatFunction : ValueSource
    {
        protected internal readonly ValueSource[] sources;

        protected MultiFloatFunction(ValueSource[] sources)
        {
            this.sources = sources;
        }

        protected abstract string Name { get; }

        protected abstract float Func(int doc, FunctionValues[] valsArr);

        public override string GetDescription()
        {
            var sb = new StringBuilder();
            sb.Append(Name).Append('(');
            bool firstTime = true;
            foreach (var source in sources)
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
            var valsArr = new FunctionValues[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                valsArr[i] = sources[i].GetValues(context, readerContext);
            }

            return new FloatDocValuesAnonymousInnerClassHelper(this, this, valsArr);
        }

        private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
        {
            private readonly MultiFloatFunction outerInstance;

            private readonly FunctionValues[] valsArr;

            public FloatDocValuesAnonymousInnerClassHelper(MultiFloatFunction outerInstance, MultiFloatFunction @this, FunctionValues[] valsArr)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.valsArr = valsArr;
            }

            public override float FloatVal(int doc)
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
            foreach (ValueSource source in sources)
            {
                source.CreateWeight(context, searcher);
            }
        }

        public override int GetHashCode()
        {
            return Arrays.GetHashCode(sources) + Name.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (this.GetType() != o.GetType())
            {
                return false;
            }
            var other = o as MultiFloatFunction;
            if (other == null)
                return false;
            return Name.Equals(other.Name) && Arrays.Equals(this.sources, other.sources);
        }
    }
}