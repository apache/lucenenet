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
    using DoubleFieldSource = Lucene.Net.Queries.Function.ValueSources.DoubleFieldSource;
    using Filter = Lucene.Net.Search.Filter;
    using FunctionValues = Lucene.Net.Queries.Function.FunctionValues;
    using IBits = Lucene.Net.Util.IBits;
    using MatchingDocs = FacetsCollector.MatchingDocs;
    using NumericUtils = Lucene.Net.Util.NumericUtils;
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;

    /// <summary>
    /// <see cref="Facets"/> implementation that computes counts for
    ///  dynamic double ranges from a provided <see cref="ValueSource"/>, 
    ///  using <see cref="FunctionValues.DoubleVal(int)"/> or <see cref="FunctionValues.DoubleVal(int, double[])"/>.  Use
    ///  this for dimensions that change in real-time (e.g. a
    ///  relative time based dimension like "Past day", "Past 2
    ///  days", etc.) or that change for each request (e.g.
    ///  distance from the user's location, "&lt; 1 km", "&lt; 2 km",
    ///  etc.).
    /// 
    ///  <para> If you had indexed your field using <see cref="Documents.SingleDocValuesField"/> 
    ///  then pass <see cref="Queries.Function.ValueSources.SingleFieldSource"/>
    ///  as the <see cref="ValueSource"/>; if you used 
    ///  <see cref="Documents.DoubleDocValuesField"/> then pass 
    ///  <see cref="DoubleFieldSource"/> (this is the default used when you
    ///  pass just a the field name).
    /// 
    /// @lucene.experimental 
    /// </para>
    /// </summary>
    public class DoubleRangeFacetCounts : RangeFacetCounts
    {
        /// <summary>
        /// Create <see cref="RangeFacetCounts"/>, using 
        /// <see cref="DoubleFieldSource"/> from the specified field. 
        /// </summary>
        public DoubleRangeFacetCounts(string field, FacetsCollector hits, params DoubleRange[] ranges)
            : this(field, new DoubleFieldSource(field), hits, ranges)
        {
        }

        /// <summary>
        /// Create <see cref="RangeFacetCounts"/>, using the provided
        /// <see cref="ValueSource"/>. 
        /// </summary>
        public DoubleRangeFacetCounts(string field, ValueSource valueSource, FacetsCollector hits, params DoubleRange[] ranges)
            : this(field, valueSource, hits, null, ranges)
        {
        }

        /// <summary>
        /// Create <see cref="RangeFacetCounts"/>, using the provided
        /// <see cref="ValueSource"/>, and using the provided Filter as
        /// a fastmatch: only documents passing the filter are
        /// checked for the matching ranges.  The filter must be
        /// random access (implement <see cref="DocIdSet.Bits"/>). 
        /// </summary>
        public DoubleRangeFacetCounts(string field, ValueSource valueSource, FacetsCollector hits, Filter fastMatchFilter, params DoubleRange[] ranges)
            : base(field, ranges, fastMatchFilter)
        {
            Count(valueSource, hits.GetMatchingDocs());
        }

        private void Count(ValueSource valueSource, IEnumerable<MatchingDocs> matchingDocs)
        {

            DoubleRange[] ranges = (DoubleRange[])this.m_ranges;

            Int64Range[] longRanges = new Int64Range[ranges.Length];
            for (int i = 0; i < ranges.Length; i++)
            {
                DoubleRange range = ranges[i];
                longRanges[i] = new Int64Range(range.Label, NumericUtils.DoubleToSortableInt64(range.minIncl), true, NumericUtils.DoubleToSortableInt64(range.maxIncl), true);
            }

            Int64RangeCounter counter = new Int64RangeCounter(longRanges);

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
                        counter.Add(NumericUtils.DoubleToSortableInt64(fv.DoubleVal(doc)));
                    }
                    else
                    {
                        missingCount++;
                    }
                }
            }

            missingCount += counter.FillCounts(m_counts);
            m_totCount -= missingCount;
        }
    }
}