// Lucene version compatibility level 4.8.1
using Lucene.Net.Search;
using System;
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
    /// A function with a single (one) argument.
    /// <para/>
    /// NOTE: This was SingleFunction in Lucene, changed to avoid conusion with operations on the datatype <see cref="System.Single"/>.
    /// </summary>
    public abstract class SingularFunction : ValueSource
    {
        protected readonly ValueSource m_source;

        protected SingularFunction(ValueSource source) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_source = source;
        }

        protected abstract string Name { get; }

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
            if (!(o is SingularFunction other))
                return false;
            return Name.Equals(other.Name, StringComparison.Ordinal) 
                && m_source.Equals(other.m_source);
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            m_source.CreateWeight(context, searcher);
        }
    }
}