using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Facet;
using Lucene.Net.Search;

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
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;

    /// <summary>
    /// Collects hits for subsequent faceting, using sampling if needed. Once you've
    /// run a search and collect hits into this, instantiate one of the
    /// <seealso cref="Facets"/> subclasses to do the facet counting. Note that this collector
    /// does not collect the scores of matching docs (i.e.
    /// <seealso cref="FacetsCollector.MatchingDocs#scores"/>) is {@code null}.
    /// <para>
    /// If you require the original set of hits, you can call
    /// <seealso cref="#getOriginalMatchingDocs()"/>. Also, since the counts of the top-facets
    /// is based on the sampled set, you can amortize the counts by calling
    /// <seealso cref="#amortizeFacetCounts"/>.
    /// </para>
    /// </summary>
    public class RandomSamplingFacetsCollector : FacetsCollector
    {

        /// <summary>
        /// Faster alternative for java.util.Random, inspired by
        /// http://dmurphy747.wordpress.com/2011/03/23/xorshift-vs-random-
        /// performance-in-java/
        /// <para>
        /// Has a period of 2^64-1
        /// </para>
        /// </summary>
        private class XORShift64Random
        {

            internal long x;

            /// <summary>
            /// Creates a xorshift random generator using the provided seed </summary>
            public XORShift64Random(long seed)
            {
                x = seed == 0 ? 0xdeadbeef : seed;
            }

            /// <summary>
            /// Get the next random long value </summary>
            public virtual long RandomLong()
            {
                x ^= (x << 21);
                x ^= ((long)((ulong)x >> 35));
                x ^= (x << 4);
                return x;
            }

            /// <summary>
            /// Get the next random int, between 0 (inclusive) and n (exclusive) </summary>
            public virtual int NextInt(int n)
            {
                int res = (int)(RandomLong() % n);
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
        /// <seealso cref= #RandomSamplingFacetsCollector(int, long) </seealso>
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
        ///          The random seed. If {@code 0} then a seed will be chosen for you. </param>
        public RandomSamplingFacetsCollector(int sampleSize, long seed)
            : base(false)
        {
            this.sampleSize = sampleSize;
            this.random = new XORShift64Random(seed);
            this.sampledDocs = null;
        }

        /// <summary>
        /// Returns the sampled list of the matching documents. Note that a
        /// <seealso cref="FacetsCollector.MatchingDocs"/> instance is returned per segment, even
        /// if no hits from that segment are included in the sampled set.
        /// <para>
        /// Note: One or more of the MatchingDocs might be empty (not containing any
        /// hits) as result of sampling.
        /// </para>
        /// <para>
        /// Note: {@code MatchingDocs.totalHits} is copied from the original
        /// MatchingDocs, scores is set to {@code null}
        /// </para>
        /// </summary>
        public override IList<MatchingDocs> GetMatchingDocs
        {
            get
            {
                IList<MatchingDocs> matchingDocs = base.GetMatchingDocs;

                if (totalHits == NOT_CALCULATED)
                {
                    totalHits = 0;
                    foreach (MatchingDocs md in matchingDocs)
                    {
                        totalHits += md.totalHits;
                    }
                }

                if (totalHits <= sampleSize)
                {
                    return matchingDocs;
                }

                if (sampledDocs == null)
                {
                    samplingRate = (1.0 * sampleSize) / totalHits;
                    sampledDocs = CreateSampledDocs(matchingDocs);
                }
                return sampledDocs;
            }
        }

        /// <summary>
        /// Returns the original matching documents. </summary>
        public virtual IList<MatchingDocs> OriginalMatchingDocs
        {
            get
            {
                return base.GetMatchingDocs;
            }
        }

        /// <summary>
        /// Create a sampled copy of the matching documents list. </summary>
        private IList<MatchingDocs> CreateSampledDocs(IList<MatchingDocs> matchingDocsList)
        {
            IList<MatchingDocs> sampledDocsList = new List<MatchingDocs>(matchingDocsList.Count);
            foreach (MatchingDocs docs in matchingDocsList)
            {
                sampledDocsList.Add(CreateSample(docs));
            }
            return sampledDocsList;
        }

        /// <summary>
        /// Create a sampled of the given hits. </summary>
        private MatchingDocs CreateSample(MatchingDocs docs)
        {
            int maxdoc = docs.context.Reader.MaxDoc;

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
                    randomIndex = random.NextInt(binSize);
                }
                DocIdSetIterator it = docs.bits.GetIterator();
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
                        randomIndex = random.NextInt(binSize);
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

                return new MatchingDocs(docs.context, sampleDocs, docs.totalHits, null);
            }
            catch (IOException)
            {
                throw new Exception();
            }
        }

        /// <summary>
        /// Note: if you use a counting <seealso cref="Facets"/> implementation, you can amortize the
        /// sampled counts by calling this method. Uses the <seealso cref="FacetsConfig"/> and
        /// the <seealso cref="IndexSearcher"/> to determine the upper bound for each facet value.
        /// </summary>
        public virtual FacetResult AmortizeFacetCounts(FacetResult res, FacetsConfig config, IndexSearcher searcher)
        {
            if (res == null || totalHits <= sampleSize)
            {
                return res;
            }

            LabelAndValue[] fixedLabelValues = new LabelAndValue[res.labelValues.Length];
            IndexReader reader = searcher.IndexReader;
            DimConfig dimConfig = config.GetDimConfig(res.dim);

            // +2 to prepend dimension, append child label
            string[] childPath = new string[res.path.Length + 2];
            childPath[0] = res.dim;

            Array.Copy(res.path, 0, childPath, 1, res.path.Length); // reuse

            for (int i = 0; i < res.labelValues.Length; i++)
            {
                childPath[res.path.Length + 1] = res.labelValues[i].label;
                string fullPath = FacetsConfig.PathToString(childPath, childPath.Length);
                int max = reader.DocFreq(new Term(dimConfig.indexFieldName, fullPath));
                int correctedCount = (int)((double)res.labelValues[i].value / samplingRate);
                correctedCount = Math.Min(max, correctedCount);
                fixedLabelValues[i] = new LabelAndValue(res.labelValues[i].label, correctedCount);
            }

            // cap the total count on the total number of non-deleted documents in the reader
            int correctedTotalCount = (int)res.value;
            if (correctedTotalCount > 0)
            {
                correctedTotalCount = Math.Min(reader.NumDocs, (int)((double)res.value / samplingRate));
            }

            return new FacetResult(res.dim, res.path, correctedTotalCount, fixedLabelValues, res.childCount);
        }

        /// <summary>
        /// Returns the sampling rate that was used. </summary>
        public virtual double SamplingRate
        {
            get
            {
                return samplingRate;
            }
        }

    }

}