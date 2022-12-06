// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// Collects hits for subsequent faceting.  Once you've run
    /// a search and collect hits into this, instantiate one of
    /// the <see cref="ICollector"/> subclasses to do the facet
    /// counting.  Use the Search utility methods (such as <see cref="Search(IndexSearcher, Query, int, ICollector)"/>) to
    /// perform an "ordinary" search but also collect into a
    /// <see cref="Facets"/>. 
    /// </summary>
    public class FacetsCollector : ICollector
    {
        private AtomicReaderContext context;
        private Scorer scorer;
        private int totalHits;
        private float[] scores;
        private readonly bool keepScores;
        private readonly IList<MatchingDocs> matchingDocs = new JCG.List<MatchingDocs>();
        private Docs docs;

        /// <summary>
        /// Used during collection to record matching docs and then return a
        /// <see cref="DocIdSet"/> that contains them.
        /// </summary>
        protected abstract class Docs
        {

            /// <summary>
            /// Sole constructor.
            /// </summary>
            protected Docs() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            {
            }

            /// <summary>
            /// Record the given document.
            /// </summary>
            public abstract void AddDoc(int docId);

            /// <summary>
            /// Return the <see cref="DocIdSet"/> which contains all the recorded docs.
            /// </summary>
            public abstract DocIdSet DocIdSet { get; }
        }

        /// <summary>
        /// Holds the documents that were matched in the <see cref="AtomicReaderContext"/>.
        /// If scores were required, then <see cref="Scores"/> is not <c>null</c>.
        /// </summary>
        public sealed class MatchingDocs
        {
            /// <summary>
            /// Context for this segment. </summary>
            public AtomicReaderContext Context { get; private set; }

            /// <summary>
            /// Which documents were seen. </summary>
            public DocIdSet Bits { get; private set; }

            /// <summary>
            /// Non-sparse scores array. </summary>
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public float[] Scores { get; private set; }

            /// <summary>
            /// Total number of hits </summary>
            public int TotalHits { get; private set; }

            /// <summary>
            /// Sole constructor.
            /// </summary>
            public MatchingDocs(AtomicReaderContext context, DocIdSet bits, int totalHits, float[] scores)
            {
                this.Context = context;
                this.Bits = bits;
                this.Scores = scores;
                this.TotalHits = totalHits;
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public FacetsCollector()
            : this(false)
        {
        }

        /// <summary>
        /// Create this; if <paramref name="keepScores"/> is <c>true</c> then a
        /// <see cref="T:float[]"/> is allocated to hold score of all hits. 
        /// </summary>
        public FacetsCollector(bool keepScores)
        {
            this.keepScores = keepScores;
        }

        /// <summary>
        /// Creates a <see cref="Docs"/> to record hits. The default uses <see cref="FixedBitSet"/>
        /// to record hits and you can override to e.g. record the docs in your own
        /// <see cref="DocIdSet"/>.
        /// </summary>
        protected virtual Docs CreateDocs(int maxDoc)
        {
            return new DocsAnonymousClass(maxDoc);
        }

        private sealed class DocsAnonymousClass : Docs
        {
            public DocsAnonymousClass(int maxDoc)
            {
                bits = new FixedBitSet(maxDoc);
            }

            private readonly FixedBitSet bits;

            public override void AddDoc(int docId)
            {
                bits.Set(docId);
            }

            public override DocIdSet DocIdSet => bits;
        }

        /// <summary>
        /// True if scores were saved.
        /// </summary>
        public bool KeepScores => keepScores;

        /// <summary>
        /// Returns the documents matched by the query, one <see cref="MatchingDocs"/> per
        /// visited segment.
        /// </summary>
        public virtual IList<MatchingDocs> GetMatchingDocs()
        {
            if (docs != null)
            {
                matchingDocs.Add(new MatchingDocs(this.context, docs.DocIdSet, totalHits, scores));
                docs = null;
                scores = null;
                context = null;
            }

            return matchingDocs;
        }

        public bool AcceptsDocsOutOfOrder =>
            // If we are keeping scores then we require in-order
            // because we append each score to the float[] and
            // expect that they correlate in order to the hits:
            keepScores == false;

        public void Collect(int doc)
        {
            docs.AddDoc(doc);
            if (keepScores)
            {
                if (totalHits >= scores.Length)
                {
                    float[] newScores = new float[ArrayUtil.Oversize(totalHits + 1, 4)];
                    Arrays.Copy(scores, 0, newScores, 0, totalHits);
                    scores = newScores;
                }
                scores[totalHits] = scorer.GetScore();
            }
            totalHits++;
        }

        public void SetScorer(Scorer scorer)
        {
            this.scorer = scorer;
        }

        public void SetNextReader(AtomicReaderContext context)
        {
            if (docs != null)
            {
                matchingDocs.Add(new MatchingDocs(this.context, docs.DocIdSet, totalHits, scores));
            }
            docs = CreateDocs(context.Reader.MaxDoc);
            totalHits = 0;
            if (keepScores)
            {
                scores = new float[64]; // some initial size
            }
            this.context = context;
        }

        /// <summary>
        /// Utility method, to search and also collect all hits
        /// into the provided <see cref="ICollector"/>. 
        /// </summary>
        public static TopDocs Search(IndexSearcher searcher, Query q, int n, ICollector fc)
        {
            return DoSearch(searcher, null, q, null, n, null, false, false, fc);
        }

        /// <summary>
        /// Utility method, to search and also collect all hits
        /// into the provided <see cref="ICollector"/>. 
        /// </summary>
        public static TopDocs Search(IndexSearcher searcher, Query q, Filter filter, int n, ICollector fc)
        {
            return DoSearch(searcher, null, q, filter, n, null, false, false, fc);
        }

        /// <summary>
        /// Utility method, to search and also collect all hits
        /// into the provided <see cref="ICollector"/>. 
        /// </summary>
        public static TopFieldDocs Search(IndexSearcher searcher, Query q, Filter filter, int n, Sort sort, ICollector fc)
        {
            if (sort is null)
            {
                throw new ArgumentNullException(nameof(sort), "sort must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            return (TopFieldDocs)DoSearch(searcher, null, q, filter, n, sort, false, false, fc);
        }

        /// <summary>
        /// Utility method, to search and also collect all hits
        /// into the provided <see cref="ICollector"/>. 
        /// </summary>
        public static TopFieldDocs Search(IndexSearcher searcher, Query q, Filter filter, int n, Sort sort, bool doDocScores, bool doMaxScore, ICollector fc)
        {
            if (sort is null)
            {
                throw new ArgumentNullException(nameof(sort), "sort must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            return (TopFieldDocs)DoSearch(searcher, null, q, filter, n, sort, doDocScores, doMaxScore, fc);
        }

        /// <summary>
        /// Utility method, to search and also collect all hits
        /// into the provided <see cref="ICollector"/>. 
        /// </summary>
        public virtual TopDocs SearchAfter(IndexSearcher searcher, ScoreDoc after, Query q, int n, ICollector fc)
        {
            return DoSearch(searcher, after, q, null, n, null, false, false, fc);
        }

        /// <summary>
        /// Utility method, to search and also collect all hits
        /// into the provided <see cref="ICollector"/>. 
        /// </summary>
        public static TopDocs SearchAfter(IndexSearcher searcher, ScoreDoc after, Query q, Filter filter, int n, ICollector fc)
        {
            return DoSearch(searcher, after, q, filter, n, null, false, false, fc);
        }

        /// <summary>
        /// Utility method, to search and also collect all hits
        /// into the provided <see cref="ICollector"/>. 
        /// </summary>
        public static TopDocs SearchAfter(IndexSearcher searcher, ScoreDoc after, Query q, Filter filter, int n, Sort sort, ICollector fc)
        {
            if (sort is null)
            {
                throw new ArgumentNullException(nameof(sort), "sort must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            return DoSearch(searcher, after, q, filter, n, sort, false, false, fc);
        }

        /// <summary>
        /// Utility method, to search and also collect all hits
        /// into the provided <see cref="ICollector"/>. 
        /// </summary>
        public static TopDocs SearchAfter(IndexSearcher searcher, ScoreDoc after, Query q, Filter filter, int n, Sort sort, bool doDocScores, bool doMaxScore, ICollector fc)
        {
            if (sort is null)
            {
                throw new ArgumentNullException(nameof(sort), "sort must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            return DoSearch(searcher, after, q, filter, n, sort, doDocScores, doMaxScore, fc);
        }

        private static TopDocs DoSearch(IndexSearcher searcher, ScoreDoc after, Query q, Filter filter, int n, Sort sort, bool doDocScores, bool doMaxScore, ICollector fc)
        {

            if (filter != null)
            {
                q = new FilteredQuery(q, filter);
            }

            int limit = searcher.IndexReader.MaxDoc;
            if (limit == 0)
            {
                limit = 1;
            }
            n = Math.Min(n, limit);

            if (after != null && after.Doc >= limit)
            {
                throw new ArgumentException("after.doc exceeds the number of documents in the reader: after.doc=" + after.Doc + " limit=" + limit);
            }


            if (sort != null)
            {
                if (after != null && !(after is FieldDoc))
                {
                    // TODO: if we fix type safety of TopFieldDocs we can
                    // remove this
                    throw new ArgumentException("after must be a FieldDoc; got " + after);
                }
                const bool fillFields = true;
                var hitsCollector = TopFieldCollector.Create(sort, n, (FieldDoc)after, fillFields, doDocScores, doMaxScore, false);
                searcher.Search(q, MultiCollector.Wrap(hitsCollector, fc));
                return hitsCollector.GetTopDocs();
            }
            else
            {
                // TODO: can we pass the right boolean for
                // in-order instead of hardwired to false...?  we'd
                // need access to the protected IS.search methods
                // taking Weight... could use reflection...
                var hitsCollector = TopScoreDocCollector.Create(n, after, false);
                searcher.Search(q, MultiCollector.Wrap(hitsCollector, fc));
                return hitsCollector.GetTopDocs();
            }
        }
    }
}