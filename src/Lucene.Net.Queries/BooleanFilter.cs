// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    /// A container <see cref="Filter"/> that allows Boolean composition of <see cref="Filter"/>s.
    /// <see cref="Filter"/>s are allocated into one of three logical constructs;
    /// SHOULD, MUST NOT, MUST
    /// The results <see cref="Filter"/> BitSet is constructed as follows:
    /// SHOULD Filters are OR'd together
    /// The resulting <see cref="Filter"/> is NOT'd with the NOT <see cref="Filter"/>s
    /// The resulting <see cref="Filter"/> is AND'd with the MUST <see cref="Filter"/>s
    /// </summary>
    public class BooleanFilter : Filter, IEnumerable<FilterClause>
    {
        private readonly IList<FilterClause> clauses = new JCG.List<FilterClause>();

        /// <summary>
        /// Returns the a <see cref="DocIdSetIterator"/> representing the Boolean composition
        /// of the filters that have been added.
        /// </summary>
        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            FixedBitSet res = null;
            AtomicReader reader = context.AtomicReader;

            bool hasShouldClauses = false;
            foreach (FilterClause fc in clauses)
            {
                if (fc.Occur == Occur.SHOULD)
                {
                    hasShouldClauses = true;
                    DocIdSetIterator disi = GetDISI(fc.Filter, context);
                    if (disi is null)
                    {
                        continue;
                    }
                    if (res is null)
                    {
                        res = new FixedBitSet(reader.MaxDoc);
                    }
                    res.Or(disi);
                }
            }
            if (hasShouldClauses && res is null)
            {
                return null;
            }

            foreach (FilterClause fc in clauses)
            {
                if (fc.Occur == Occur.MUST_NOT)
                {
                    if (res is null)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(!hasShouldClauses);
                        res = new FixedBitSet(reader.MaxDoc);
                        res.Set(0, reader.MaxDoc); // NOTE: may set bits on deleted docs
                    }

                    DocIdSetIterator disi = GetDISI(fc.Filter, context);
                    if (disi != null)
                    {
                        res.AndNot(disi);
                    }
                }
            }

            foreach (FilterClause fc in clauses)
            {
                if (fc.Occur == Occur.MUST)
                {
                    DocIdSetIterator disi = GetDISI(fc.Filter, context);
                    if (disi is null)
                    {
                        return null; // no documents can match
                    }
                    if (res is null)
                    {
                        res = new FixedBitSet(reader.MaxDoc);
                        res.Or(disi);
                    }
                    else
                    {
                        res.And(disi);
                    }
                }
            }

            return BitsFilteredDocIdSet.Wrap(res, acceptDocs);
        }

        private static DocIdSetIterator GetDISI(Filter filter, AtomicReaderContext context)
        {
            // we dont pass acceptDocs, we will filter at the end using an additional filter
            DocIdSet set = filter.GetDocIdSet(context, null);
            return set?.GetIterator();
        }

        /// <summary>
        /// Adds a new <see cref="FilterClause"/> to the Boolean <see cref="Filter"/> container </summary>
        /// <param name="filterClause"> A <see cref="FilterClause"/> object containing a <see cref="Filter"/> and an <see cref="Occur"/> parameter </param>
        public virtual void Add(FilterClause filterClause)
        {
            clauses.Add(filterClause);
        }

        public void Add(Filter filter, Occur occur)
        {
            Add(new FilterClause(filter, occur));
        }

        /// <summary>
        /// Gets the list of clauses
        /// </summary>
        public virtual IList<FilterClause> Clauses => clauses;

        /// <summary>
        /// Returns an iterator on the clauses in this query. It implements the <see cref="IEnumerable{T}"/> interface to
        /// make it possible to do:
        /// <code>for (FilterClause clause : booleanFilter) {}</code>
        /// </summary>
        public IEnumerator<FilterClause> GetEnumerator()
        {
            return Clauses.GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if ((obj is null) || (obj.GetType() != this.GetType()))
            {
                return false;
            }

            var other = (BooleanFilter)obj;
            return clauses.Equals(other.clauses);
        }

        public override int GetHashCode()
        {
            return 657153718 ^ clauses.GetHashCode();
        }

        /// <summary>
        /// Prints a user-readable version of this <see cref="Filter"/>. </summary>
        public override string ToString()
        {
            var buffer = new StringBuilder("BooleanFilter(");
            int minLen = buffer.Length;
            foreach (FilterClause c in clauses)
            {
                if (buffer.Length > minLen)
                {
                    buffer.Append(' ');
                }
                buffer.Append(c);
            }
            return buffer.Append(')').ToString();
        }

        // LUCENENET specific
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}