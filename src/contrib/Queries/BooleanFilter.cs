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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
    public class BooleanFilter : Filter, IEnumerable<FilterClause>
    {
        private readonly IList<FilterClause> clauses = new List<FilterClause>();

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
                    if (disi == null) continue;
                    if (res == null)
                    {
                        res = new FixedBitSet(reader.MaxDoc);
                    }
                    res.Or(disi);
                }
            }
            if (hasShouldClauses && res == null)
                return DocIdSet.EMPTY_DOCIDSET;

            foreach (FilterClause fc in clauses)
            {
                if (fc.Occur == Occur.MUST_NOT)
                {
                    if (res == null)
                    {
                        //assert !hasShouldClauses;
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
                    if (disi == null)
                    {
                        return DocIdSet.EMPTY_DOCIDSET; // no documents can match
                    }
                    if (res == null)
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

            return res != null ? BitsFilteredDocIdSet.Wrap(res, acceptDocs) : DocIdSet.EMPTY_DOCIDSET;
        }

        private static DocIdSetIterator GetDISI(Filter filter, AtomicReaderContext context)
        {
            // we dont pass acceptDocs, we will filter at the end using an additional filter
            DocIdSet set = filter.GetDocIdSet(context, null);
            return (set == null || set == DocIdSet.EMPTY_DOCIDSET) ? null : set.Iterator();
        }

        /// <summary>
        /// Add a filter clause.
        /// </summary>
        /// <param name="filterClause">The clause to add.</param>
        public virtual void Add(FilterClause filterClause)
        {
            clauses.Add(filterClause);
        }

        public void Add(Filter filter, Occur occur)
        {
            Add(new FilterClause(filter, occur));
        }

        public IList<FilterClause> Clauses
        {
            get { return clauses; }
        }

        public IEnumerator<FilterClause> GetEnumerator()
        {
            return clauses.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if ((obj == null) || (obj.GetType() != this.GetType()))
            {
                return false;
            }

            BooleanFilter other = (BooleanFilter)obj;
            return clauses.Equals(other.clauses);
        }

        public override int GetHashCode()
        {
            return 657153718 ^ clauses.GetHashCode();
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder("BooleanFilter(");
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
    }
}
