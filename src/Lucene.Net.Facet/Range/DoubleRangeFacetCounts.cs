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

    using Bits = Lucene.Net.Util.Bits;
    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using DoubleFieldSource = Lucene.Net.Queries.Function.ValueSources.DoubleFieldSource;
    using Filter = Lucene.Net.Search.Filter;
    using FunctionValues = Lucene.Net.Queries.Function.FunctionValues;
    using MatchingDocs = FacetsCollector.MatchingDocs;
    using NumericUtils = Lucene.Net.Util.NumericUtils;
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;

    /// <summary>
    /// <seealso cref="Facets"/> implementation that computes counts for
    ///  dynamic double ranges from a provided {@link
    ///  ValueSource}, using <seealso cref="FunctionValues#doubleVal"/>.  Use
    ///  this for dimensions that change in real-time (e.g. a
    ///  relative time based dimension like "Past day", "Past 2
    ///  days", etc.) or that change for each request (e.g.
    ///  distance from the user's location, "< 1 km", "< 2 km",
    ///  etc.).
    /// 
    ///  <para> If you had indexed your field using {@link
    ///  FloatDocValuesField} then pass <seealso cref="FloatFieldSource"/>
    ///  as the <seealso cref="ValueSource"/>; if you used {@link
    ///  DoubleDocValuesField} then pass {@link
    ///  DoubleFieldSource} (this is the default used when you
    ///  pass just a the field name).
    /// 
    ///  @lucene.experimental 
    /// </para>
    /// </summary>
    public class DoubleRangeFacetCounts : RangeFacetCounts
    {
        /// <summary>
        /// Create {@code RangeFacetCounts}, using {@link
        ///  DoubleFieldSource} from the specified field. 
        /// </summary>
        public DoubleRangeFacetCounts(string field, FacetsCollector hits, params DoubleRange[] ranges)
            : this(field, new DoubleFieldSource(field), hits, ranges)
        {
        }

        /// <summary>
        /// Create {@code RangeFacetCounts}, using the provided
        ///  <seealso cref="ValueSource"/>. 
        /// </summary>
        public DoubleRangeFacetCounts(string field, ValueSource valueSource, FacetsCollector hits, params DoubleRange[] ranges)
            : this(field, valueSource, hits, null, ranges)
        {
        }

        /// <summary>
        /// Create {@code RangeFacetCounts}, using the provided
        ///  <seealso cref="ValueSource"/>, and using the provided Filter as
        ///  a fastmatch: only documents passing the filter are
        ///  checked for the matching ranges.  The filter must be
        ///  random access (implement <seealso cref="DocIdSet#bits"/>). 
        /// </summary>
        public DoubleRangeFacetCounts(string field, ValueSource valueSource, FacetsCollector hits, Filter fastMatchFilter, DoubleRange[] ranges)
            : base(field, ranges, fastMatchFilter)
        {
            Count(valueSource, hits.GetMatchingDocs());
        }

        private void Count(ValueSource valueSource, IEnumerable<MatchingDocs> matchingDocs)
        {

            DoubleRange[] ranges = (DoubleRange[])this.ranges;

            LongRange[] longRanges = new LongRange[ranges.Length];
            for (int i = 0; i < ranges.Length; i++)
            {
                DoubleRange range = ranges[i];
                longRanges[i] = new LongRange(range.Label, NumericUtils.DoubleToSortableLong(range.minIncl), true, NumericUtils.DoubleToSortableLong(range.maxIncl), true);
            }

            LongRangeCounter counter = new LongRangeCounter(longRanges);

            int missingCount = 0;
            foreach (MatchingDocs hits in matchingDocs)
            {
                FunctionValues fv = valueSource.GetValues(new Dictionary<string, object>(), hits.Context);

                totCount += hits.TotalHits;
                Bits bits;
                if (fastMatchFilter != null)
                {
                    DocIdSet dis = fastMatchFilter.GetDocIdSet(hits.Context, null);
                    if (dis == null)
                    {
                        // No documents match
                        continue;
                    }
                    bits = dis.GetBits();
                    if (bits == null)
                    {
                        throw new System.ArgumentException("fastMatchFilter does not implement DocIdSet.bits");
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
                        counter.Add(NumericUtils.DoubleToSortableLong(fv.DoubleVal(doc)));
                    }
                    else
                    {
                        missingCount++;
                    }
                }
            }

            missingCount += counter.FillCounts(counts);
            totCount -= missingCount;
        }
    }
}