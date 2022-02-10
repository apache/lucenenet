// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Facet.SortedSet
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using MultiDocValues = Lucene.Net.Index.MultiDocValues;
    using OrdRange = Lucene.Net.Facet.SortedSet.SortedSetDocValuesReaderState.OrdRange;
    using ReaderUtil = Lucene.Net.Index.ReaderUtil;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;

    /// <summary>
    /// Compute facets counts from previously
    /// indexed <see cref="SortedSetDocValuesFacetField"/>,
    /// without require a separate taxonomy index.  Faceting is
    /// a bit slower (~25%), and there is added cost on every
    /// <see cref="IndexReader"/> open to create a new 
    /// <see cref="SortedSetDocValuesReaderState"/>.  Furthermore, this does
    /// not support hierarchical facets; only flat (dimension +
    /// label) facets, but it uses quite a bit less RAM to do
    /// so.
    /// 
    /// <para><b>NOTE</b>: this class should be instantiated and
    /// then used from a single thread, because it holds a
    /// thread-private instance of <see cref="SortedSetDocValues"/>.
    /// 
    /// </para>
    /// <para><b>NOTE:</b>: tie-break is by unicode sort order
    /// 
    /// @lucene.experimental 
    /// </para>
    /// </summary>
    public class SortedSetDocValuesFacetCounts : Facets
    {
        internal readonly SortedSetDocValuesReaderState state;
        internal readonly SortedSetDocValues dv;
        internal readonly string field;
        internal readonly int[] counts;

        /// <summary>
        /// Sparse faceting: returns any dimension that had any
        /// hits, topCount labels per dimension. 
        /// </summary>
        public SortedSetDocValuesFacetCounts(SortedSetDocValuesReaderState state, FacetsCollector hits)
        {
            this.state = state;
            this.field = state.Field;
            dv = state.GetDocValues();
            counts = new int[state.Count];
            //System.out.println("field=" + field);
            Count(hits.GetMatchingDocs());
        }

        public override FacetResult GetTopChildren(int topN, string dim, params string[] path)
        {
            if (topN <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(topN), "topN must be > 0 (got: " + topN + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (path.Length > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(path), "path should be 0 length"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            OrdRange ordRange = state.GetOrdRange(dim);
            if (ordRange is null)
            {
                throw new ArgumentException("dimension \"" + dim + "\" was not indexed");
            }
            return GetDim(dim, ordRange, topN);
        }

        private FacetResult GetDim(string dim, OrdRange ordRange, int topN)
        {
            TopOrdAndInt32Queue q = null;

            int bottomCount = 0;

            int dimCount = 0;
            int childCount = 0;

            //System.out.println("getDim : " + ordRange.start + " - " + ordRange.end);
            for (int ord = ordRange.Start; ord <= ordRange.End; ord++)
            {
                //System.out.println("  ord=" + ord + " count=" + counts[ord]);
                if (counts[ord] > 0)
                {
                    dimCount += counts[ord];
                    childCount++;
                    if (counts[ord] > bottomCount)
                    {
                        if (q is null)
                        {
                            // Lazy init, so we don't create this for the
                            // sparse case unnecessarily
                            q = new TopOrdAndInt32Queue(topN);
                        }
                        // LUCENENET specific - use struct instead of reusing class instance for better performance
                        q.Insert(new OrdAndValue<int>(ord, counts[ord]));
                        if (q.Count == topN)
                        {
                            bottomCount = q.Top.Value;
                        }
                    }
                }
            }

            if (q is null)
            {
                return null;
            }

            var scratch = new BytesRef();

            LabelAndValue[] labelValues = new LabelAndValue[q.Count];
            for (int i = labelValues.Length - 1; i >= 0; i--)
            {
                var ordAndValue = q.Pop();
                dv.LookupOrd(ordAndValue.Ord, scratch);
                string[] parts = FacetsConfig.StringToPath(scratch.Utf8ToString());
                labelValues[i] = new LabelAndValue(parts[1], ordAndValue.Value);
            }

            return new FacetResult(dim, Arrays.Empty<string>(), dimCount, labelValues, childCount);
        }

        /// <summary>
        /// Does all the "real work" of tallying up the counts. </summary>
        private void Count(IList<FacetsCollector.MatchingDocs> matchingDocs)
        {
            //System.out.println("ssdv count");

            MultiDocValues.OrdinalMap ordinalMap;

            // TODO: is this right?  really, we need a way to
            // verify that this ordinalMap "matches" the leaves in
            // matchingDocs...
            if (dv is MultiDocValues.MultiSortedSetDocValues values && matchingDocs.Count > 1)
            {
                ordinalMap = values.Mapping;
            }
            else
            {
                ordinalMap = null;
            }

            IndexReader origReader = state.OrigReader;

            foreach (FacetsCollector.MatchingDocs hits in matchingDocs)
            {
                var reader = hits.Context.AtomicReader;
                //System.out.println("  reader=" + reader);
                // LUCENE-5090: make sure the provided reader context "matches"
                // the top-level reader passed to the
                // SortedSetDocValuesReaderState, else cryptic
                // AIOOBE can happen:
                if (!Equals(ReaderUtil.GetTopLevelContext(hits.Context).Reader, origReader))
                {
                    throw IllegalStateException.Create("the SortedSetDocValuesReaderState provided to this class does not match the reader being searched; you must create a new SortedSetDocValuesReaderState every time you open a new IndexReader");
                }

                SortedSetDocValues segValues = reader.GetSortedSetDocValues(field);
                if (segValues is null)
                {
                    continue;
                }

                DocIdSetIterator docs = hits.Bits.GetIterator();

                // TODO: yet another option is to count all segs
                // first, only in seg-ord space, and then do a
                // merge-sort-PQ in the end to only "resolve to
                // global" those seg ords that can compete, if we know
                // we just want top K?  ie, this is the same algo
                // that'd be used for merging facets across shards
                // (distributed faceting).  but this has much higher
                // temp ram req'ts (sum of number of ords across all
                // segs)
                if (ordinalMap != null)
                {
                    int segOrd = hits.Context.Ord;

                    int numSegOrds = (int)segValues.ValueCount;

                    if (hits.TotalHits < numSegOrds / 10)
                    {
                        //System.out.println("    remap as-we-go");
                        // Remap every ord to global ord as we iterate:
                        int doc;
                        while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            //System.out.println("    doc=" + doc);
                            segValues.SetDocument(doc);
                            int term = (int)segValues.NextOrd();
                            while (term != SortedSetDocValues.NO_MORE_ORDS)
                            {
                                //System.out.println("      segOrd=" + segOrd + " ord=" + term + " globalOrd=" + ordinalMap.getGlobalOrd(segOrd, term));
                                counts[(int)ordinalMap.GetGlobalOrd(segOrd, term)]++;
                                term = (int)segValues.NextOrd();
                            }
                        }
                    }
                    else
                    {
                        //System.out.println("    count in seg ord first");

                        // First count in seg-ord space:
                        int[] segCounts = new int[numSegOrds];
                        int doc;
                        while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            //System.out.println("    doc=" + doc);
                            segValues.SetDocument(doc);
                            int term = (int)segValues.NextOrd();
                            while (term != SortedSetDocValues.NO_MORE_ORDS)
                            {
                                //System.out.println("      ord=" + term);
                                segCounts[term]++;
                                term = (int)segValues.NextOrd();
                            }
                        }

                        // Then, migrate to global ords:
                        for (int ord = 0; ord < numSegOrds; ord++)
                        {
                            int count = segCounts[ord];
                            if (count != 0)
                            {
                                //System.out.println("    migrate segOrd=" + segOrd + " ord=" + ord + " globalOrd=" + ordinalMap.getGlobalOrd(segOrd, ord));
                                counts[(int)ordinalMap.GetGlobalOrd(segOrd, ord)] += count;
                            }
                        }
                    }
                }
                else
                {
                    // No ord mapping (e.g., single segment index):
                    // just aggregate directly into counts:
                    int doc;
                    while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        segValues.SetDocument(doc);
                        int term = (int)segValues.NextOrd();
                        while (term != SortedSetDocValues.NO_MORE_ORDS)
                        {
                            counts[term]++;
                            term = (int)segValues.NextOrd();
                        }
                    }
                }
            }
        }

        public override float GetSpecificValue(string dim, params string[] path)
        {
            if (path.Length != 1)
            {
                throw new ArgumentException("path must be Length=1");
            }
            int ord = (int)dv.LookupTerm(new BytesRef(FacetsConfig.PathToString(dim, path)));
            if (ord < 0)
            {
                return -1;
            }

            return counts[ord];
        }

        public override IList<FacetResult> GetAllDims(int topN)
        {
            JCG.List<FacetResult> results = new JCG.List<FacetResult>();
            foreach (KeyValuePair<string, OrdRange> ent in state.PrefixToOrdRange)
            {
                FacetResult fr = GetDim(ent.Key, ent.Value, topN);
                if (fr != null)
                {
                    results.Add(fr);
                }
            }

            // Sort by highest count:
            results.Sort(Comparer<FacetResult>.Create((a, b) =>
            {
                if ((int)a.Value > (int)b.Value)
                {
                    return -1;
                }
                else if ((int)b.Value > (int)a.Value)
                {
                    return 1;
                }
                else
                {
                    return a.Dim.CompareToOrdinal(b.Dim);
                }
            }));
            return results;
        }
    }
}