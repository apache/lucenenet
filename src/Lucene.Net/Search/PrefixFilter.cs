using System.Text;

namespace Lucene.Net.Search
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

    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// A <see cref="Filter"/> that restricts search results to values that have a matching prefix in a given
    /// field.
    /// </summary>
    public class PrefixFilter : MultiTermQueryWrapperFilter<PrefixQuery>
    {
        public PrefixFilter(Term prefix)
            : base(new PrefixQuery(prefix))
        {
        }

        public virtual Term Prefix => m_query.Prefix;

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("PrefixFilter(");
            buffer.Append(Prefix.ToString());
            buffer.Append(')');
            return buffer.ToString();
        }
    }
}