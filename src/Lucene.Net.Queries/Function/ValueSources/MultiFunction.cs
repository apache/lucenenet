// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    /// Abstract parent class for <see cref="ValueSource"/> implementations that wrap multiple
    /// <see cref="ValueSource"/>s and apply their own logic.
    /// </summary>
    public abstract class MultiFunction : ValueSource
    {
        protected readonly IList<ValueSource> m_sources;

        protected MultiFunction(IList<ValueSource> sources)
        {
            this.m_sources = sources;
        }

        protected abstract string Name { get; }

        public override string GetDescription()
        {
            return GetDescription(Name, m_sources);
        }

        public static string GetDescription(string name, IList<ValueSource> sources)
        {
            var sb = new StringBuilder();
            sb.Append(name).Append('(');
            bool firstTime = true;
            foreach (ValueSource source in sources)
            {
                if (firstTime)
                {
                    firstTime = false;
                }
                else
                {
                    sb.Append(',');
                }
                sb.Append((object)source);
            }
            sb.Append(')');
            return sb.ToString();
        }

        public static FunctionValues[] ValsArr(IList<ValueSource> sources, IDictionary fcontext, AtomicReaderContext readerContext)
        {
            var valsArr = new FunctionValues[sources.Count];
            int i = 0;
            foreach (var source in sources)
            {
                valsArr[i++] = source.GetValues(fcontext, readerContext);
            }
            return valsArr;
        }

        public class Values : FunctionValues
        {
            private readonly MultiFunction outerInstance;

            internal readonly FunctionValues[] valsArr;

            public Values(MultiFunction outerInstance, FunctionValues[] valsArr)
            {
                this.outerInstance = outerInstance;
                this.valsArr = valsArr;
            }

            public override string ToString(int doc)
            {
                return MultiFunction.ToString(outerInstance.Name, valsArr, doc);
            }
        }


        public static string ToString(string name, FunctionValues[] valsArr, int doc)
        {
            var sb = new StringBuilder();
            sb.Append(name).Append('(');
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

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            foreach (ValueSource source in m_sources)
            {
                source.CreateWeight(context, searcher);
            }
        }

        public override int GetHashCode()
        {
            // LUCENENET specific: use structural equality comparison
            return JCG.ListEqualityComparer<ValueSource>.Default.GetHashCode(m_sources) + Name.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (this.GetType() != o.GetType())
            {
                return false;
            }
            var other = (MultiFunction)o;

            // LUCENENET specific: use structural equality comparison
            return JCG.ListEqualityComparer<ValueSource>.Default.Equals(this.m_sources, other.m_sources);
        }
    }
}