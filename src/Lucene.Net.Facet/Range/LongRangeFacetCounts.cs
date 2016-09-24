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
    using Filter = Lucene.Net.Search.Filter;
    using FunctionValues = Lucene.Net.Queries.Function.FunctionValues;
    using LongFieldSource = Lucene.Net.Queries.Function.ValueSources.LongFieldSource;
    using MatchingDocs = FacetsCollector.MatchingDocs;
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;

    /// <summary>
    /// <seealso cref="Facets"/> implementation that computes counts for
    ///  dynamic long ranges from a provided <seealso cref="ValueSource"/>,
    ///  using <seealso cref="FunctionValues#longVal"/>.  Use
    ///  this for dimensions that change in real-time (e.g. a
    ///  relative time based dimension like "Past day", "Past 2
    ///  days", etc.) or that change for each request (e.g. 
    ///  distance from the user's location, "< 1 km", "< 2 km",
    ///  etc.).
    /// 
    ///  @lucene.experimental 
    /// </summary>
    public class LongRangeFacetCounts : RangeFacetCounts
    {

        /// <summary>
        /// Create {@code LongRangeFacetCounts}, using {@link
        ///  LongFieldSource} from the specified field. 
        /// </summary>
        public LongRangeFacetCounts(string field, FacetsCollector hits, params LongRange[] ranges)
            : this(field, new LongFieldSource(field), hits, ranges)
        {
        }

        /// <summary>
        /// Create {@code RangeFacetCounts}, using the provided
        ///  <seealso cref="ValueSource"/>. 
        /// </summary>
        public LongRangeFacetCounts(string field, ValueSource valueSource, FacetsCollector hits, params LongRange[] ranges)
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
        public LongRangeFacetCounts(string field, ValueSource valueSource, 
            FacetsCollector hits, Filter fastMatchFilter, params LongRange[] ranges)
            : base(field, ranges, fastMatchFilter)
        {
            Count(valueSource, hits.GetMatchingDocs);
        }

        private void Count(ValueSource valueSource, IList<MatchingDocs> matchingDocs)
        {

            LongRange[] ranges = (LongRange[])this.Ranges;

            LongRangeCounter counter = new LongRangeCounter(ranges);

            int missingCount = 0;
            foreach (MatchingDocs hits in matchingDocs)
            {
                FunctionValues fv = valueSource.GetValues(new Dictionary<string, object>(), hits.context);

                TotCount += hits.totalHits;
                Bits bits;
                if (FastMatchFilter != null)
                {
                    DocIdSet dis = FastMatchFilter.GetDocIdSet(hits.context, null);
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

                DocIdSetIterator docs = hits.bits.GetIterator();
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
                        counter.add(fv.LongVal(doc));
                    }
                    else
                    {
                        missingCount++;
                    }
                }
            }

            int x = counter.fillCounts(Counts);

            missingCount += x;

            //System.out.println("totCount " + totCount + " missingCount " + counter.missingCount);
            TotCount -= missingCount;
        }
    }
}