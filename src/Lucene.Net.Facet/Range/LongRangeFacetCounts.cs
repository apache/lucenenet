// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Facet.Range
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

    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Filter = Lucene.Net.Search.Filter;
    using FunctionValues = Lucene.Net.Queries.Function.FunctionValues;
    using IBits = Lucene.Net.Util.IBits;
    using Int64FieldSource = Lucene.Net.Queries.Function.ValueSources.Int64FieldSource;
    using MatchingDocs = FacetsCollector.MatchingDocs;
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;

    /// <summary>
    /// <see cref="Facets"/> implementation that computes counts for
    /// dynamic long ranges from a provided <see cref="ValueSource"/>,
    /// using <see cref="FunctionValues.Int64Val(int)"/> or <see cref="FunctionValues.Int64Val(int, long[])"/>.  Use
    /// this for dimensions that change in real-time (e.g. a
    /// relative time based dimension like "Past day", "Past 2
    /// days", etc.) or that change for each request (e.g. 
    /// distance from the user's location, "&lt; 1 km", "&lt; 2 km",
    /// etc.).
    /// <para/>
    /// NOTE: This was LongRangeFacetCounts in Lucene
    /// 
    /// @lucene.experimental 
    /// </summary>
    public class Int64RangeFacetCounts : RangeFacetCounts
    {
        /// <summary>
        /// Create <see cref="Int64RangeFacetCounts"/>, using
        /// <see cref="Int64FieldSource"/> from the specified field. 
        /// </summary>
        public Int64RangeFacetCounts(string field, FacetsCollector hits, params Int64Range[] ranges)
            : this(field, new Int64FieldSource(field), hits, ranges)
        {
        }

        /// <summary>
        /// Create <see cref="Int64RangeFacetCounts"/>, using the provided
        /// <see cref="ValueSource"/>. 
        /// </summary>
        public Int64RangeFacetCounts(string field, ValueSource valueSource, FacetsCollector hits, params Int64Range[] ranges)
            : this(field, valueSource, hits, null, ranges)
        {
        }

        /// <summary>
        /// Create <see cref="Int64RangeFacetCounts"/>, using the provided
        /// <see cref="ValueSource"/>, and using the provided Filter as
        /// a fastmatch: only documents passing the filter are
        /// checked for the matching ranges.  The filter must be
        /// random access (implement <see cref="DocIdSet.Bits"/>). 
        /// </summary>
        public Int64RangeFacetCounts(string field, ValueSource valueSource, 
            FacetsCollector hits, Filter fastMatchFilter, params Int64Range[] ranges)
            : base(field, ranges, fastMatchFilter)
        {
            Count(valueSource, hits.GetMatchingDocs());
        }

        private void Count(ValueSource valueSource, IList<MatchingDocs> matchingDocs)
        {

            Int64Range[] ranges = (Int64Range[])this.m_ranges;

            Int64RangeCounter counter = new Int64RangeCounter(ranges);

            int missingCount = 0;
            foreach (MatchingDocs hits in matchingDocs)
            {
                FunctionValues fv = valueSource.GetValues(Collections.EmptyMap<string, object>(), hits.Context);

                m_totCount += hits.TotalHits;
                IBits bits;
                if (m_fastMatchFilter != null)
                {
                    DocIdSet dis = m_fastMatchFilter.GetDocIdSet(hits.Context, null);
                    if (dis is null)
                    {
                        // No documents match
                        continue;
                    }
                    bits = dis.Bits;
                    if (bits is null)
                    {
                        throw new ArgumentException("fastMatchFilter does not implement DocIdSet.Bits");
                    }
                }
                else
                {
                    bits = null;
                }

                DocIdSetIterator docs = hits.Bits.GetIterator();
                int doc;
                while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (bits != null && bits.Get(doc) == false)
                    {
                        doc++;
                        continue;
                    }
                    // Skip missing docs:
                    if (fv.Exists(doc))
                    {
                        counter.Add(fv.Int64Val(doc));
                    }
                    else
                    {
                        missingCount++;
                    }
                }
            }

            int x = counter.FillCounts(m_counts);

            missingCount += x;

            //System.out.println("totCount " + totCount + " missingCount " + counter.missingCount);
            m_totCount -= missingCount;
        }
    }
}