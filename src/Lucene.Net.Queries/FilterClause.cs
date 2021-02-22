// Lucene version compatibility level 4.8.1
using Lucene.Net.Search;

namespace Lucene.Net.Queries
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
    /// A <see cref="Search.Filter"/> that wrapped with an indication of how that filter
    /// is used when composed with another filter.
    /// (Follows the boolean logic in <see cref="BooleanClause"/> for composition 
    /// of queries.)
    /// </summary>
    public sealed class FilterClause
    {
        private readonly Occur occur;
        private readonly Filter filter;

        /// <summary>
        /// Create a new <see cref="FilterClause"/> </summary>
        /// <param name="filter"> A <see cref="Search.Filter"/> object containing a BitSet </param>
        /// <param name="occur"> A parameter implementation indicating SHOULD, MUST or MUST NOT </param>
        public FilterClause(Filter filter, Occur occur)
        {
            this.occur = occur;
            this.filter = filter;
        }

        /// <summary>
        /// Returns this <see cref="FilterClause"/>'s filter </summary>
        /// <returns> A <see cref="Search.Filter"/> object </returns>
        public Filter Filter => filter;

        /// <summary>
        /// Returns this <see cref="FilterClause"/>'s occur parameter </summary>
        /// <returns> An <see cref="Search.Occur"/> object </returns>
        public Occur Occur => occur;

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (!(o is FilterClause other))
            {
                return false;
            }
            return this.filter.Equals(other.filter) 
                && this.occur == other.occur;
        }

        public override int GetHashCode()
        {
            return filter.GetHashCode() ^ occur.GetHashCode();
        }

        public override string ToString()
        {
            return BooleanClause.ToString(occur) + filter.ToString();
        }
    }
}