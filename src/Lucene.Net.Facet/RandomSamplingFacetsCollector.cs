// Lucene version compatibility level 4.8.1
using J2N.Numerics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Facet
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

    using DimConfig = FacetsConfig.DimConfig;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Collects hits for subsequent faceting, using sampling if needed. Once you've
    /// run a search and collect hits into this, instantiate one of the
    /// <see cref="Facets"/> subclasses to do the facet counting. Note that this collector
    /// does not collect the scores of matching docs (i.e.
    /// <see cref="FacetsCollector.MatchingDocs.Scores"/>) is <c>null</c>.
    /// <para>
    /// If you require the original set of hits, you can call
    /// <see cref="GetOriginalMatchingDocs()"/>. Also, since the counts of the top-facets
    /// is based on the sampled set, you can amortize the counts by calling
    /// <see cref="AmortizeFacetCounts"/>.
    /// </para>
    /// </summary>
    public class RandomSamplingFacetsCollector : FacetsCollector
    {
        /// <summary>
        /// Faster alternative for java.util.Random, inspired by
        /// http://dmurphy747.wordpress.com/2011/03/23/xorshift-vs-random-performance-in-java/
        /// <para>
        /// Has a period of 2^64-1
        /// </para>
        /// </summary>
        private class XORShift64Random
        {
            internal long x;

            /// <summary>
            /// Creates a xorshift random generator using the provided seed
            /// </summary>
            public XORShift64Random(long seed)
            {
                x = seed == 0 ? 0xdeadbeef : seed;
            }

            /// <summary>
            /// Get the next random long value
            /// <para/>
            /// NOTE: This was randomLong() in Lucene
            /// </summary>
            public virtual long RandomInt64()
            {
                x ^= (x << 21);
                x ^= (x.TripleShift(35));
                x ^= (x << 4);
                return x;
            }

            /// <summary>
            /// Get the next random int, between 0 (inclusive) and <paramref name="n"/> (exclusive)
            /// <para/>
            /// NOTE: This was nextInt() in Lucene
            /// </summary>
            public virtual int NextInt32(int n)
            {
                int res = (int)(RandomInt64() % n);
                return (res < 0) ? -res : res;
            }
        }

        private const int NOT_CALCULATED = -1;

        private readonly int sampleSize;
        private readonly XORShift64Random random;

        private double samplingRate;
        private IList<MatchingDocs> sampledDocs;
        private int totalHits = NOT_CALCULATED;
        private int leftoverBin = NOT_CALCULATED;
        private int leftoverIndex = NOT_CALCULATED;

        /// <summary>
        /// Constructor with the given sample size and default seed.
        /// </summary>
        /// <seealso cref="RandomSamplingFacetsCollector(int, long)"/>
        public RandomSamplingFacetsCollector(int sampleSize)
            : this(sampleSize, 0)
        {
        }

        /// <summary>
        /// Constructor with the given sample size and seed.
        /// </summary>
        /// <param name="sampleSize">
        ///          The preferred sample size. If the number of hits is greater than
        ///          the size, sampling will be done using a sample ratio of sampling
        ///          size / totalN. For example: 1000 hits, sample size = 10 results in
        ///          samplingRatio of 0.01. If the number of hits is lower, no sampling
        ///          is done at all </param>
        /// <param name="seed">
        ///          The random seed. If <c>0</c> then a seed will be chosen for you. </param>
        public RandomSamplingFacetsCollector(int sampleSize, long seed)
            : base(false)
        {
            this.sampleSize = sampleSize;
            this.random = new XORShift64Random(seed);
            this.sampledDocs = null;
        }

        /// <summary>
        /// Returns the sampled list of the matching documents. Note that a
        /// <see cref="FacetsCollector.MatchingDocs"/> instance is returned per segment, even
        /// if no hits from that segment are included in the sampled set.
        /// <para>
        /// Note: One or more of the <see cref="FacetsCollector.MatchingDocs"/> might be empty (not containing any
        /// hits) as result of sampling.
        /// </para>
        /// <para>
        /// Note: <see cref="FacetsCollector.MatchingDocs.TotalHits"/> is copied from the original
        /// <see cref="FacetsCollector.MatchingDocs"/>, scores is set to <c>null</c>
        /// </para>
        /// </summary>
        public override IList<MatchingDocs> GetMatchingDocs()
        {
            IList<MatchingDocs> matchingDocs = base.GetMatchingDocs();

            if (totalHits == NOT_CALCULATED)
            {
                totalHits = 0;
                foreach (MatchingDocs md in matchingDocs)
                {
                    totalHits += md.TotalHits;
                }
            }

            if (totalHits <= sampleSize)
            {
                return matchingDocs;
            }

            if (sampledDocs is null)
            {
                samplingRate = (1.0 * sampleSize) / totalHits;
                sampledDocs = CreateSampledDocs(matchingDocs);
            }
            return sampledDocs;
        }

        /// <summary>
        /// Returns the original matching documents.
        /// </summary>
        public virtual IList<MatchingDocs> GetOriginalMatchingDocs()
        {
            return base.GetMatchingDocs();
        }

        /// <summary>
        /// Create a sampled copy of the matching documents list.
        /// </summary>
        private IList<MatchingDocs> CreateSampledDocs(ICollection<MatchingDocs> matchingDocsList)
        {
            IList<MatchingDocs> sampledDocsList = new JCG.List<MatchingDocs>(matchingDocsList.Count);
            foreach (MatchingDocs docs in matchingDocsList)
            {
                sampledDocsList.Add(CreateSample(docs));
            }
            return sampledDocsList;
        }

        /// <summary>
        /// Create a sampled of the given hits.
        /// </summary>
        private MatchingDocs CreateSample(MatchingDocs docs)
        {
            int maxdoc = docs.Context.Reader.MaxDoc;

            // TODO: we could try the WAH8DocIdSet here as well, as the results will be sparse
            FixedBitSet sampleDocs = new FixedBitSet(maxdoc);

            int binSize = (int)(1.0 / samplingRate);

            try
            {
                int counter = 0;
                int limit, randomIndex;
                if (leftoverBin != NOT_CALCULATED)
                {
                    limit = leftoverBin;
                    // either NOT_CALCULATED, which means we already sampled from that bin,
                    // or the next document to sample
                    randomIndex = leftoverIndex;
                }
                else
                {
                    limit = binSize;
                    randomIndex = random.NextInt32(binSize);
                }
                DocIdSetIterator it = docs.Bits.GetIterator();
                for (int doc = it.NextDoc(); doc != DocIdSetIterator.NO_MORE_DOCS; doc = it.NextDoc())
                {
                    if (counter == randomIndex)
                    {
                        sampleDocs.Set(doc);
                    }
                    counter++;
                    if (counter >= limit)
                    {
                        counter = 0;
                        limit = binSize;
                        randomIndex = random.NextInt32(binSize);
                    }
                }

                if (counter == 0)
                {
                    // we either exhausted the bin and the iterator at the same time, or
                    // this segment had no results. in the latter case we might want to
                    // carry leftover to the next segment as is, but that complicates the
                    // code and doesn't seem so important.
                    leftoverBin = leftoverIndex = NOT_CALCULATED;
                }
                else
                {
                    leftoverBin = limit - counter;
                    if (randomIndex > counter)
                    {
                        // the document to sample is in the next bin
                        leftoverIndex = randomIndex - counter;
                    }
                    else if (randomIndex < counter)
                    {
                        // we sampled a document from the bin, so just skip over remaining
                        // documents in the bin in the next segment.
                        leftoverIndex = NOT_CALCULATED;
                    }
                }

                return new MatchingDocs(docs.Context, sampleDocs, docs.TotalHits, null);
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
            }
        }

        /// <summary>
        /// Note: if you use a counting <see cref="Facets"/> implementation, you can amortize the
        /// sampled counts by calling this method. Uses the <see cref="FacetsConfig"/> and
        /// the <see cref="IndexSearcher"/> to determine the upper bound for each facet value.
        /// </summary>
        public virtual FacetResult AmortizeFacetCounts(FacetResult res, FacetsConfig config, IndexSearcher searcher)
        {
            if (res is null || totalHits <= sampleSize)
            {
                return res;
            }

            LabelAndValue[] fixedLabelValues = new LabelAndValue[res.LabelValues.Length];
            IndexReader reader = searcher.IndexReader;
            DimConfig dimConfig = config.GetDimConfig(res.Dim);

            // +2 to prepend dimension, append child label
            string[] childPath = new string[res.Path.Length + 2];
            childPath[0] = res.Dim;

            Arrays.Copy(res.Path, 0, childPath, 1, res.Path.Length); // reuse

            for (int i = 0; i < res.LabelValues.Length; i++)
            {
                childPath[res.Path.Length + 1] = res.LabelValues[i].Label;
                string fullPath = FacetsConfig.PathToString(childPath, childPath.Length);
                int max = reader.DocFreq(new Term(dimConfig.IndexFieldName, fullPath));
                int correctedCount = (int)((double)res.LabelValues[i].Value / samplingRate);
                correctedCount = Math.Min(max, correctedCount);
                fixedLabelValues[i] = new LabelAndValue(res.LabelValues[i].Label, correctedCount);
            }

            // cap the total count on the total number of non-deleted documents in the reader
            int correctedTotalCount = (int)res.Value;
            if (correctedTotalCount > 0)
            {
                correctedTotalCount = Math.Min(reader.NumDocs, (int)((double)res.Value / samplingRate));
            }

            return new FacetResult(res.Dim, res.Path, correctedTotalCount, fixedLabelValues, res.ChildCount);
        }

        /// <summary>
        /// Returns the sampling rate that was used.
        /// </summary>
        public virtual double SamplingRate => samplingRate;
    }
}