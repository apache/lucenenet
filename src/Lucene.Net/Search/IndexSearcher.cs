#nullable enable
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Lucene.Net.Search
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Document = Documents.Document;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using ReaderUtil = Lucene.Net.Index.ReaderUtil;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using StoredFieldVisitor = Lucene.Net.Index.StoredFieldVisitor;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using Terms = Lucene.Net.Index.Terms;

    /// <summary>
    /// Implements search over a single <see cref="Index.IndexReader"/>.
    ///
    /// <para/>Applications usually need only call the inherited
    /// <see cref="Search(Query,int)"/>
    /// or <see cref="Search(Query,Filter,int)"/> methods. For
    /// performance reasons, if your index is unchanging, you
    /// should share a single <see cref="IndexSearcher"/> instance across
    /// multiple searches instead of creating a new one
    /// per-search.  If your index has changed and you wish to
    /// see the changes reflected in searching, you should
    /// use <see cref="Index.DirectoryReader.OpenIfChanged(Index.DirectoryReader)"/>
    /// to obtain a new reader and
    /// then create a new <see cref="IndexSearcher"/> from that.  Also, for
    /// low-latency turnaround it's best to use a near-real-time
    /// reader (<see cref="Index.DirectoryReader.Open(Index.IndexWriter,bool)"/>).
    /// Once you have a new <see cref="Index.IndexReader"/>, it's relatively
    /// cheap to create a new <see cref="IndexSearcher"/> from it.
    ///
    /// <para/><a name="thread-safety"></a><p><b>NOTE</b>: 
    /// <see cref="IndexSearcher"/> instances are completely
    /// thread safe, meaning multiple threads can call any of its
    /// methods, concurrently.  If your application requires
    /// external synchronization, you should <b>not</b>
    /// synchronize on the <see cref="IndexSearcher"/> instance;
    /// use your own (non-Lucene) objects instead.</p>
    /// </summary>
    public class IndexSearcher
    {
        internal readonly IndexReader reader; // package private for testing!

        // NOTE: these members might change in incompatible ways
        // in the next release
        protected readonly IndexReaderContext m_readerContext;

        protected internal readonly IList<AtomicReaderContext> m_leafContexts;

        /// <summary>
        /// Used with executor - each slice holds a set of leafs executed within one thread </summary>
        protected readonly LeafSlice[]? m_leafSlices;

        // These are only used for multi-threaded search
        private readonly TaskScheduler? executor;

        // the default Similarity
        private static readonly Similarity defaultSimilarity = new DefaultSimilarity();

        /// <summary>
        /// Expert: returns a default <see cref="Similarities.Similarity"/> instance.
        /// In general, this method is only called to initialize searchers and writers.
        /// User code and query implementations should respect
        /// <see cref="IndexSearcher.Similarity"/>.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public static Similarity DefaultSimilarity => defaultSimilarity;

        /// <summary>
        /// The <see cref="Similarities.Similarity"/> implementation used by this searcher. </summary>
        private Similarity similarity = defaultSimilarity;

        /// <summary>
        /// Creates a searcher searching the provided index. </summary>
        /// <exception cref="ArgumentNullException"><paramref name="r"/> is <c>null</c>.</exception>
        public IndexSearcher(IndexReader r)
            : this(r, executor: null)
        {
        }

        /// <summary>
        /// Runs searches for each segment separately, using the
        /// provided <see cref="TaskScheduler"/>.  <see cref="IndexSearcher"/> will not
        /// shutdown/awaitTermination this <see cref="TaskScheduler"/> on
        /// dispose; you must do so, eventually, on your own.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="r"/> is <c>null</c>.</exception>
        public IndexSearcher(IndexReader r, TaskScheduler? executor)
            : this(r?.Context!, executor)
        {
        }

        /// <summary>
        /// Creates a searcher searching the provided top-level <see cref="IndexReaderContext"/>.
        /// <para/>
        /// Given a non-<c>null</c> <see cref="TaskScheduler"/> this method runs
        /// searches for each segment separately, using the provided <see cref="TaskScheduler"/>.
        /// <see cref="IndexSearcher"/> will not shutdown/awaitTermination this <see cref="TaskScheduler"/> on
        /// close; you must do so, eventually, on your own.
        /// <para/>
        /// @lucene.experimental 
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <seealso cref="IndexReaderContext"/>
        /// <seealso cref="IndexReader.Context"/>
        public IndexSearcher(IndexReaderContext context, TaskScheduler? executor)
            : this(context, executor, allocateLeafSlices: executor is not null)
        {
        }

        /// <summary>
        /// LUCENENET specific constructor that can be used by the subclasses to
        /// control whether the leaf slices are allocated in the base class or subclass.
        /// </summary>
        /// <remarks>
        /// If <paramref name="executor"/> is non-<c>null</c> and you choose to skip allocating the leaf slices
        /// (i.e. <paramref name="allocateLeafSlices"/> == <c>false</c>), you must
        /// set the <see cref="m_leafSlices"/> field in your subclass constructor.
        /// This is commonly done by calling <see cref="GetSlices(IList{AtomicReaderContext})"/>
        /// and using the result to set <see cref="m_leafSlices"/>. You may wish to do this if you
        /// have state to pass into your constructor and need to set it prior to the call to
        /// <see cref="GetSlices(IList{AtomicReaderContext})"/> so it is available for use
        /// as a member field or property inside a custom override of
        /// <see cref="GetSlices(IList{AtomicReaderContext})"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "Required for continuity with Lucene's design")]
        protected IndexSearcher(IndexReaderContext context, TaskScheduler? executor, bool allocateLeafSlices)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            if (Debugging.AssertsEnabled) Debugging.Assert(context.IsTopLevel, "IndexSearcher's ReaderContext must be topLevel for reader {0}", context.Reader);

            reader = context.Reader;
            this.executor = executor;
            this.m_readerContext = context;
            m_leafContexts = context.Leaves;

            if (allocateLeafSlices)
            {
                this.m_leafSlices = GetSlices(m_leafContexts);
            }
        }

        /// <summary>
        /// Creates a searcher searching the provided top-level <see cref="IndexReaderContext"/>.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <seealso cref="IndexReaderContext"/>
        /// <seealso cref="IndexReader.Context"/>
        public IndexSearcher(IndexReaderContext context)
            : this(context, null)
        {
        }

        /// <summary>
        /// Expert: Creates an array of leaf slices each holding a subset of the given leaves.
        /// Each <see cref="LeafSlice"/> is executed in a single thread. By default there
        /// will be one <see cref="LeafSlice"/> per leaf (<see cref="AtomicReaderContext"/>).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="leaves"/> is <c>null</c>.</exception>
        protected virtual LeafSlice[] GetSlices(IList<AtomicReaderContext> leaves)
        {
            // LUCENENET: Added guard clause
            if (leaves is null)
                throw new ArgumentNullException(nameof(leaves));

            LeafSlice[] slices = new LeafSlice[leaves.Count];
            for (int i = 0; i < slices.Length; i++)
            {
                slices[i] = new LeafSlice(leaves[i]);
            }
            return slices;
        }

        /// <summary>
        /// Return the <see cref="Index.IndexReader"/> this searches. </summary>
        public virtual IndexReader IndexReader => reader;

        /// <summary>
        /// Sugar for <code>.IndexReader.Document(docID)</code> </summary>
        /// <seealso cref="IndexReader.Document(int)"/>
        public virtual Document Doc(int docID)
        {
            return reader.Document(docID);
        }

        /// <summary>
        /// Sugar for <code>.IndexReader.Document(docID, fieldVisitor)</code> </summary>
        /// <seealso cref="IndexReader.Document(int, StoredFieldVisitor)"/>
        /// <exception cref="ArgumentNullException"><paramref name="fieldVisitor"/> is <c>null</c>.</exception>
        public virtual void Doc(int docID, StoredFieldVisitor fieldVisitor)
        {
            if (fieldVisitor is null)
                throw new ArgumentNullException(nameof(fieldVisitor));

            reader.Document(docID, fieldVisitor);
        }

        /// <summary>
        /// Sugar for <code>.IndexReader.Document(docID, fieldsToLoad)</code> </summary>
        /// <seealso cref="IndexReader.Document(int, ISet{string})"/>
        public virtual Document Doc(int docID, ISet<string>? fieldsToLoad)
        {
            return reader.Document(docID, fieldsToLoad);
        }

        /// @deprecated Use <see cref="Doc(int, ISet{string})"/> instead.
        [Obsolete("Use <seealso cref=#doc(int, java.util.Set)/> instead.")]
        public Document Document(int docID, ISet<string> fieldsToLoad)
        {
            return Doc(docID, fieldsToLoad);
        }

        /// <summary>
        /// Expert: Set the <see cref="Similarities.Similarity"/> implementation used by this IndexSearcher.
        /// </summary>
        public virtual Similarity Similarity
        {
            get => similarity;
            set => this.similarity = value;
        }

        /// <summary>
        /// @lucene.internal </summary>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        protected virtual Query WrapFilter(Query query, Filter? filter)
        {
            return (filter is null) ? query : new FilteredQuery(query, filter);
        }

        /// <summary>
        /// Finds the top <paramref name="n"/>
        /// hits for top <paramref name="query"/> where all results are after a previous
        /// result (top <paramref name="after"/>).
        /// <para/>
        /// By passing the bottom result from a previous page as <paramref name="after"/>,
        /// this method can be used for efficient 'deep-paging' across potentially
        /// large result sets.
        /// </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        public virtual TopDocs SearchAfter(ScoreDoc? after, Query query, int n)
        {
            return Search(CreateNormalizedWeight(query), after, n);
        }

        /// <summary>
        /// Finds the top <paramref name="n"/>
        /// hits for <paramref name="query"/>, applying <paramref name="filter"/> if non-null,
        /// where all results are after a previous result (<paramref name="after"/>).
        /// <para/>
        /// By passing the bottom result from a previous page as <paramref name="after"/>,
        /// this method can be used for efficient 'deep-paging' across potentially
        /// large result sets.
        /// </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        public virtual TopDocs SearchAfter(ScoreDoc? after, Query query, Filter? filter, int n)
        {
            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), after, n);
        }

        /// <summary>
        /// Finds the top <paramref name="n"/>
        /// hits for <paramref name="query"/>.
        /// </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        public virtual TopDocs Search(Query query, int n)
        {
            return Search(query, filter: null, n);
        }

        /// <summary>
        /// Finds the top <paramref name="n"/>
        /// hits for <paramref name="query"/>, applying <paramref name="filter"/> if non-<c>null</c>.
        /// </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        public virtual TopDocs Search(Query query, Filter? filter, int n)
        {
            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), after: null, n);
        }

        /// <summary>
        /// Lower-level search API.
        ///
        /// <para/><see cref="ICollector.Collect(int)"/> is called for every matching
        /// document.
        /// </summary>
        /// <param name="query"> To match documents </param>
        /// <param name="filter"> Ef non-<c>null</c>, used to permit documents to be collected. </param>
        /// <param name="results"> To receive hits </param>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> or
        ///         <paramref name="results"/> is <c>null</c>.</exception>
        public virtual void Search(Query query, Filter? filter, ICollector results)
        {
            Search(m_leafContexts, CreateNormalizedWeight(WrapFilter(query, filter)), results);
        }

        /// <summary>
        /// Lower-level search API.
        ///
        /// <para/><seealso cref="ICollector.Collect(int)"/> is called for every matching document.
        /// </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> or
        ///         <paramref name="results"/> is <c>null</c>.</exception>
        public virtual void Search(Query query, ICollector results)
        {
            Search(m_leafContexts, CreateNormalizedWeight(query), results);
        }

        /// <summary>
        /// Search implementation with arbitrary sorting.  Finds
        /// the top <paramref name="n"/> hits for <paramref name="query"/>, applying
        /// <paramref name="filter"/> if non-null, and sorting the hits by the criteria in
        /// <paramref name="sort"/>.
        ///
        /// <para/>NOTE: this does not compute scores by default; use
        /// <see cref="IndexSearcher.Search(Query,Filter,int,Sort,bool,bool)"/> to
        /// control scoring.
        /// </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> or
        ///         <paramref name="sort"/> is <c>null</c>.</exception>
        public virtual TopFieldDocs Search(Query query, Filter? filter, int n, Sort sort)
        {
            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), n, sort, false, false);
        }

        /// <summary>
        /// Search implementation with arbitrary sorting, plus
        /// control over whether hit scores and max score
        /// should be computed.  Finds
        /// the top <paramref name="n"/> hits for <paramref name="query"/>, applying
        /// <paramref name="filter"/> if non-null, and sorting the hits by the criteria in
        /// <paramref name="sort"/>.  If <paramref name="doDocScores"/> is <c>true</c>
        /// then the score of each hit will be computed and
        /// returned.  If <paramref name="doMaxScore"/> is
        /// <c>true</c> then the maximum score over all
        /// collected hits will be computed.
        /// </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> or
        ///         <paramref name="sort"/> is <c>null</c>.</exception>
        public virtual TopFieldDocs Search(Query query, Filter? filter, int n, Sort sort, bool doDocScores, bool doMaxScore)
        {
            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), n, sort, doDocScores, doMaxScore);
        }

        /// <summary>
        /// Finds the top <paramref name="n"/>
        /// hits for <paramref name="query"/>, applying <paramref name="filter"/> if non-null,
        /// where all results are after a previous result (<paramref name="after"/>).
        /// <para/>
        /// By passing the bottom result from a previous page as <paramref name="after"/>,
        /// this method can be used for efficient 'deep-paging' across potentially
        /// large result sets.
        /// </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <seealso cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> or
        ///         <paramref name="sort"/> is <c>null</c>.</exception>
        public virtual TopDocs SearchAfter(ScoreDoc? after, Query query, Filter? filter, int n, Sort sort)
        {
            FieldDoc? fieldDoc = GetScoreDocAsFieldDocIfNotNull(after);

            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), fieldDoc, n, sort, true, false, false);
        }

        private static FieldDoc? GetScoreDocAsFieldDocIfNotNull(ScoreDoc? after)
        {
            FieldDoc? fieldDoc = null;
            // LUCENENET: Simplified type check
            if (after is not null)
            {
                // TODO: if we fix type safety of TopFieldDocs we can
                // remove this
                fieldDoc = after as FieldDoc ?? throw new ArgumentException($"{nameof(after)} must be a {nameof(FieldDoc)}; got {after}");
            }

            return fieldDoc;
        }

        /// <summary>
        /// Search implementation with arbitrary sorting and no filter. </summary>
        /// <param name="query"> The query to search for </param>
        /// <param name="n"> Return only the top n results </param>
        /// <param name="sort"> The <see cref="Lucene.Net.Search.Sort"/> object </param>
        /// <returns> The top docs, sorted according to the supplied <see cref="Lucene.Net.Search.Sort"/> instance </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> or
        ///         <paramref name="sort"/> is <c>null</c>.</exception>
        public virtual TopFieldDocs Search(Query query, int n, Sort sort)
        {
            return Search(CreateNormalizedWeight(query), n, sort, false, false);
        }

        /// <summary>
        /// Finds the top <paramref name="n"/>
        /// hits for <paramref name="query"/> where all results are after a previous
        /// result (<paramref name="after"/>).
        /// <para/>
        /// By passing the bottom result from a previous page as <paramref name="after"/>,
        /// this method can be used for efficient 'deep-paging' across potentially
        /// large result sets.
        /// </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> or
        ///         <paramref name="sort"/> is <c>null</c>.</exception>
        public virtual TopDocs SearchAfter(ScoreDoc? after, Query query, int n, Sort sort)
        {
            var fieldDoc = GetScoreDocAsFieldDocIfNotNull(after);

            return Search(CreateNormalizedWeight(query), fieldDoc, n, sort, true, false, false);
        }

        /// <summary>
        /// Finds the top <paramref name="n"/>
        /// hits for <paramref name="query"/> where all results are after a previous
        /// result (<paramref name="after"/>), allowing control over
        /// whether hit scores and max score should be computed.
        /// <para/>
        /// By passing the bottom result from a previous page as <paramref name="after"/>,
        /// this method can be used for efficient 'deep-paging' across potentially
        /// large result sets.  If <paramref name="doDocScores"/> is <c>true</c>
        /// then the score of each hit will be computed and
        /// returned.  If <paramref name="doMaxScore"/> is
        /// <c>true</c> then the maximum score over all
        /// collected hits will be computed.
        /// </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> or
        ///         <paramref name="sort"/> is <c>null</c>.</exception>
        public virtual TopDocs SearchAfter(ScoreDoc? after, Query query, Filter? filter, int n, Sort sort, bool doDocScores, bool doMaxScore)
        {
            var fieldDoc = GetScoreDocAsFieldDocIfNotNull(after);

            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), fieldDoc, n, sort, true, doDocScores, doMaxScore);
        }

        /// <summary>
        /// Expert: Low-level search implementation.  Finds the top <paramref name="nDocs"/>
        /// hits for <c>query</c>, applying <c>filter</c> if non-null.
        ///
        /// <para/>Applications should usually call <see cref="IndexSearcher.Search(Query,int)"/> or
        /// <see cref="IndexSearcher.Search(Query,Filter,int)"/> instead. </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="weight"/> is <c>null</c>.</exception>
        protected virtual TopDocs Search(Weight weight, ScoreDoc? after, int nDocs)
        {
            int limit = reader.MaxDoc;
            if (limit == 0)
            {
                limit = 1;
            }
            if (after != null && after.Doc >= limit)
            {
                throw new ArgumentException("after.Doc exceeds the number of documents in the reader: after.Doc=" + after.Doc + " limit=" + limit);
            }
            nDocs = Math.Min(nDocs, limit);

            if (executor is null)
            {
                return Search(m_leafContexts, weight, after, nDocs);
            }
            else
            {
                // LUCENENET: Added guard clauses
                if (weight is null)
                    throw new ArgumentNullException(nameof(weight));
                if (m_leafSlices is null)
                    throw new InvalidOperationException($"When the constructor is passed a non-null {nameof(TaskScheduler)}, {nameof(m_leafSlices)} must also be set to a non-null value in the constructor.");

                HitQueue hq = new HitQueue(nDocs, prePopulate: false);
                ReentrantLock @lock = new ReentrantLock();
                ExecutionHelper<TopDocs> runner = new ExecutionHelper<TopDocs>(executor);

                for (int i = 0; i < m_leafSlices.Length; i++) // search each sub
                {
                    runner.Submit(new SearcherCallableNoSort(@lock, this, m_leafSlices[i], weight, after, nDocs, hq).Call);
                }

                int totalHits = 0;
                float maxScore = float.NegativeInfinity;
                foreach (TopDocs topDocs in runner)
                {
                    if (topDocs.TotalHits != 0)
                    {
                        totalHits += topDocs.TotalHits;
                        maxScore = Math.Max(maxScore, topDocs.MaxScore);
                    }
                }

                var scoreDocs = new ScoreDoc[hq.Count];
                for (int i = hq.Count - 1; i >= 0; i--) // put docs in array
                {
                    scoreDocs[i] = hq.Pop();
                }

                return new TopDocs(totalHits, scoreDocs, maxScore);
            }
        }

        /// <summary>
        /// Expert: Low-level search implementation.  Finds the top <code>n</code>
        /// hits for <c>query</c>.
        ///
        /// <para/>Applications should usually call <see cref="IndexSearcher.Search(Query,int)"/> or
        /// <see cref="IndexSearcher.Search(Query,Filter,int)"/> instead. </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="leaves"/> or
        ///         <paramref name="weight"/> is <c>null</c>.</exception>
        protected virtual TopDocs Search(IList<AtomicReaderContext> leaves, Weight weight, ScoreDoc? after, int nDocs)
        {
            // LUCENENET: Added guard clause
            if (weight is null)
                throw new ArgumentNullException(nameof(weight));

            // single thread
            int limit = reader.MaxDoc;
            if (limit == 0)
            {
                limit = 1;
            }
            nDocs = Math.Min(nDocs, limit);
            TopScoreDocCollector collector = TopScoreDocCollector.Create(nDocs, after, !weight.ScoresDocsOutOfOrder);
            Search(leaves, weight, collector);
            return collector.GetTopDocs();
        }

        /// <summary>
        /// Expert: Low-level search implementation with arbitrary
        /// sorting and control over whether hit scores and max
        /// score should be computed.  Finds
        /// the top <paramref name="nDocs"/> hits for <c>query</c> and sorting the hits
        /// by the criteria in <paramref name="sort"/>.
        ///
        /// <para/>Applications should usually call 
        /// <see cref="IndexSearcher.Search(Query,Filter,int,Sort)"/> instead.
        /// </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="weight"/> or
        ///         <paramref name="sort"/> is <c>null</c>.</exception>
        protected virtual TopFieldDocs Search(Weight weight, int nDocs, Sort sort, bool doDocScores, bool doMaxScore)
        {
            return Search(weight, after: null, nDocs, sort, true, doDocScores, doMaxScore);
        }

        /// <summary>
        /// Just like <see cref="Search(Weight, int, Sort, bool, bool)"/>, but you choose
        /// whether or not the fields in the returned <see cref="FieldDoc"/> instances should
        /// be set by specifying <paramref name="fillFields"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="weight"/> or
        ///         <paramref name="sort"/> is <c>null</c>.</exception>
        protected virtual TopFieldDocs Search(Weight weight, FieldDoc? after, int nDocs, Sort sort, bool fillFields, bool doDocScores, bool doMaxScore)
        {
            if (sort is null)
                throw new ArgumentNullException(nameof(sort), "Sort must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)

            int limit = reader.MaxDoc;
            if (limit == 0)
            {
                limit = 1;
            }
            nDocs = Math.Min(nDocs, limit);

            if (executor is null)
            {
                // use all leaves here!
                return Search(m_leafContexts, weight, after, nDocs, sort, fillFields, doDocScores, doMaxScore);
            }
            else
            {
                // LUCENENET: Added guard clauses
                if (weight is null)
                    throw new ArgumentNullException(nameof(weight));
                if (m_leafSlices is null)
                    throw new InvalidOperationException($"When the constructor is passed a non-null {nameof(TaskScheduler)}, {nameof(m_leafSlices)} must also be set to a non-null value in the constructor.");

                TopFieldCollector topCollector = TopFieldCollector.Create(sort, nDocs, after, fillFields, doDocScores, doMaxScore, false);

                ReentrantLock @lock = new ReentrantLock();
                ExecutionHelper<TopFieldDocs> runner = new ExecutionHelper<TopFieldDocs>(executor);

                for (int i = 0; i < m_leafSlices.Length; i++) // search each leaf slice
                {
                    runner.Submit(new SearcherCallableWithSort(@lock, this, m_leafSlices[i], weight, after, nDocs, topCollector, sort, doDocScores, doMaxScore).Call);
                }

                int totalHits = 0;
                float maxScore = float.NegativeInfinity;
                foreach (TopFieldDocs topFieldDocs in runner)
                {
                    if (topFieldDocs.TotalHits != 0)
                    {
                        totalHits += topFieldDocs.TotalHits;
                        maxScore = Math.Max(maxScore, topFieldDocs.MaxScore);
                    }
                }

                TopFieldDocs topDocs = (TopFieldDocs)topCollector.GetTopDocs();

                return new TopFieldDocs(totalHits, topDocs.ScoreDocs, topDocs.Fields, topDocs.MaxScore);
            }
        }

        /// <summary>
        /// Just like <see cref="Search(Weight, int, Sort, bool, bool)"/>, but you choose
        /// whether or not the fields in the returned <see cref="FieldDoc"/> instances should
        /// be set by specifying <paramref name="fillFields"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="leaves"/> or
        ///          <paramref name="weight"/> is <c>null</c>.</exception>
        protected virtual TopFieldDocs Search(IList<AtomicReaderContext> leaves, Weight weight, FieldDoc? after, int nDocs, Sort sort, bool fillFields, bool doDocScores, bool doMaxScore)
        {
            // LUCENENET: Added guard clause
            if (weight is null)
                throw new ArgumentNullException(nameof(weight));

            // single thread
            int limit = reader.MaxDoc;
            if (limit == 0)
            {
                limit = 1;
            }
            nDocs = Math.Min(nDocs, limit);

            TopFieldCollector collector = TopFieldCollector.Create(sort, nDocs, after, fillFields, doDocScores, doMaxScore, !weight.ScoresDocsOutOfOrder);
            Search(leaves, weight, collector);
            return (TopFieldDocs)collector.GetTopDocs();
        }

        /// <summary>
        /// Lower-level search API.
        ///
        /// <para/>
        /// <seealso cref="ICollector.Collect(int)"/> is called for every document. 
        ///
        /// <para/>
        /// NOTE: this method executes the searches on all given leaves exclusively.
        /// To search across all the searchers leaves use <see cref="m_leafContexts"/>.
        /// </summary>
        /// <param name="leaves">
        ///          The searchers leaves to execute the searches on </param>
        /// <param name="weight">
        ///          To match documents </param>
        /// <param name="collector">
        ///          To receive hits </param>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="leaves"/>, <paramref name="weight"/>,
        ///         or <paramref name="collector"/> is <c>null</c>.</exception>
        protected virtual void Search(IList<AtomicReaderContext> leaves, Weight weight, ICollector collector)
        {
            // LUCENENET: Added guard clauses
            if (leaves is null)
                throw new ArgumentNullException(nameof(leaves));
            if (weight is null)
                throw new ArgumentNullException(nameof(weight));
            if (collector is null)
                throw new ArgumentNullException(nameof(collector));

            // TODO: should we make this
            // threaded...?  the Collector could be sync'd?
            // always use single thread:
            foreach (AtomicReaderContext ctx in leaves) // search each subreader
            {
                try
                {
                    collector.SetNextReader(ctx);
                }
                catch (CollectionTerminatedException)
                {
                    // there is no doc of interest in this reader context
                    // continue with the following leaf
                    continue;
                }
                BulkScorer scorer = weight.GetBulkScorer(ctx, !collector.AcceptsDocsOutOfOrder, ctx.AtomicReader.LiveDocs);
                if (scorer != null)
                {
                    try
                    {
                        scorer.Score(collector);
                    }
                    catch (CollectionTerminatedException)
                    {
                        // collection was terminated prematurely
                        // continue with the following leaf
                    }
                }
            }
        }

        /// <summary>
        /// Expert: called to re-write queries into primitive queries. </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        public virtual Query Rewrite(Query query) // LUCENENET: renamed parameter from "original" to "query" so our exception message is consistent across the API
        {
            // LUCENENET: Added guard clause
            if (query is null)
                throw new ArgumentNullException(nameof(query));

            for (Query rewrittenQuery = query.Rewrite(reader); rewrittenQuery != query; rewrittenQuery = query.Rewrite(reader))
            {
                query = rewrittenQuery;
            }
            return query;
        }

        /// <summary>
        /// Returns an <see cref="Explanation"/> that describes how <paramref name="doc"/> scored against
        /// <paramref name="query"/>.
        ///
        /// <para/>This is intended to be used in developing <see cref="Similarities.Similarity"/> implementations,
        /// and, for good performance, should not be displayed with every hit.
        /// Computing an explanation is as expensive as executing the query over the
        /// entire index.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        public virtual Explanation Explain(Query query, int doc)
        {
            return Explain(CreateNormalizedWeight(query), doc);
        }

        /// <summary>
        /// Expert: low-level implementation method
        /// Returns an <see cref="Explanation"/> that describes how <paramref name="doc"/> scored against
        /// <paramref name="weight"/>.
        ///
        /// <para/>This is intended to be used in developing <see cref="Similarities.Similarity"/> implementations,
        /// and, for good performance, should not be displayed with every hit.
        /// Computing an explanation is as expensive as executing the query over the
        /// entire index.
        /// <para/>Applications should call <see cref="IndexSearcher.Explain(Query, int)"/>. </summary>
        /// <exception cref="BooleanQuery.TooManyClausesException"> If a query would exceed
        ///         <see cref="BooleanQuery.MaxClauseCount"/> clauses. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="weight"/> is <c>null</c>.</exception>
        protected virtual Explanation Explain(Weight weight, int doc)
        {
            // LUCENENET: Added guard clause
            if (weight is null)
                throw new ArgumentNullException(nameof(weight));

            int n = ReaderUtil.SubIndex(doc, m_leafContexts);
            AtomicReaderContext ctx = m_leafContexts[n];
            int deBasedDoc = doc - ctx.DocBase;

            return weight.Explain(ctx, deBasedDoc);
        }

        /// <summary>
        /// Creates a normalized weight for a top-level <see cref="Query"/>.
        /// The query is rewritten by this method and <see cref="Query.CreateWeight(IndexSearcher)"/> called,
        /// afterwards the <see cref="Weight"/> is normalized. The returned <see cref="Weight"/>
        /// can then directly be used to get a <see cref="Scorer"/>.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        public virtual Weight CreateNormalizedWeight(Query query)
        {
            query = Rewrite(query);
            Weight weight = query.CreateWeight(this);
            float v = weight.GetValueForNormalization();
            float norm = Similarity.QueryNorm(v);
            if (float.IsInfinity(norm) || float.IsNaN(norm))
            {
                norm = 1.0f;
            }
            weight.Normalize(norm, 1.0f);
            return weight;
        }

        /// <summary>
        /// Returns this searchers the top-level <see cref="IndexReaderContext"/>. </summary>
        /// <seealso cref="IndexReader.Context"/>
        /* sugar for #getReader().getTopReaderContext() */

        public virtual IndexReaderContext TopReaderContext => m_readerContext;

        /// <summary>
        /// A thread subclass for searching a single searchable
        /// </summary>
        private sealed class SearcherCallableNoSort // LUCENENET: no need for ICallable<V> interface
        {
            private readonly ReentrantLock @lock;
            private readonly IndexSearcher searcher;
            private readonly Weight weight;
            private readonly ScoreDoc? after;
            private readonly int nDocs;
            private readonly HitQueue hq;
            private readonly LeafSlice slice;

            public SearcherCallableNoSort(ReentrantLock @lock, IndexSearcher searcher, LeafSlice slice, Weight weight, ScoreDoc? after, int nDocs, HitQueue hq)
            {
                this.@lock = @lock;
                this.searcher = searcher;
                this.weight = weight;
                this.after = after;
                this.nDocs = nDocs;
                this.hq = hq;
                this.slice = slice;
            }

            public TopDocs Call()
            {
                TopDocs docs = searcher.Search(slice.Leaves, weight, after, nDocs);
                ScoreDoc[] scoreDocs = docs.ScoreDocs;
                //it would be so nice if we had a thread-safe insert
                @lock.Lock();
                try
                {
                    for (int j = 0; j < scoreDocs.Length; j++) // merge scoreDocs into hq
                    {
                        ScoreDoc scoreDoc = scoreDocs[j];
                        if (scoreDoc == hq.InsertWithOverflow(scoreDoc))
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    @lock.Unlock();
                }
                return docs;
            }
        }

        /// <summary>
        /// A thread subclass for searching a single searchable
        /// </summary>
        private sealed class SearcherCallableWithSort // LUCENENET: no need for ICallable<V> interface
        {
            private readonly ReentrantLock @lock;
            private readonly IndexSearcher searcher;
            private readonly Weight weight;
            private readonly int nDocs;
            private readonly TopFieldCollector hq;
            private readonly Sort sort;
            private readonly LeafSlice slice;
            private readonly FieldDoc? after;
            private readonly bool doDocScores;
            private readonly bool doMaxScore;

            public SearcherCallableWithSort(ReentrantLock @lock, IndexSearcher searcher, LeafSlice slice, Weight weight, FieldDoc? after, int nDocs, TopFieldCollector hq, Sort sort, bool doDocScores, bool doMaxScore)
            {
                this.@lock = @lock;
                this.searcher = searcher;
                this.weight = weight;
                this.nDocs = nDocs;
                this.hq = hq;
                this.sort = sort;
                this.slice = slice;
                this.after = after;
                this.doDocScores = doDocScores;
                this.doMaxScore = doMaxScore;
            }

            private readonly FakeScorer fakeScorer = new FakeScorer();

            public TopFieldDocs Call()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(slice.Leaves.Length == 1);
                TopFieldDocs docs = searcher.Search(slice.Leaves, weight, after, nDocs, sort, true, doDocScores || sort.NeedsScores, doMaxScore);
                @lock.Lock();
                try
                {
                    AtomicReaderContext ctx = slice.Leaves[0];
                    int @base = ctx.DocBase;
                    hq.SetNextReader(ctx);
                    hq.SetScorer(fakeScorer);
                    foreach (ScoreDoc scoreDoc in docs.ScoreDocs)
                    {
                        fakeScorer.doc = scoreDoc.Doc - @base;
                        fakeScorer.score = scoreDoc.Score;
                        hq.Collect(scoreDoc.Doc - @base);
                    }

                    // Carry over maxScore from sub:
                    // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                    if (doMaxScore && NumericUtils.SingleToSortableInt32(docs.MaxScore) > NumericUtils.SingleToSortableInt32(hq.maxScore))
                    {
                        hq.maxScore = docs.MaxScore;
                    }
                }
                finally
                {
                    @lock.Unlock();
                }
                return docs;
            }
        }

#nullable restore
        /// <summary>
        /// A helper class that wraps a <see cref="TaskSchedulerCompletionService{T}"/> and provides an
        /// iterable interface to the completed <see cref="Func{T}"/> delegates.
        /// </summary>
        /// <typeparam name="T">the type of the <see cref="Func{T}"/> return value</typeparam>
        private sealed class ExecutionHelper<T> : IEnumerator<T>, IEnumerable<T>
        {
            private readonly TaskSchedulerCompletionService<T> service;
            private int numTasks;
            private T current;

            internal ExecutionHelper(TaskScheduler executor)
            {
                this.service = new TaskSchedulerCompletionService<T>(executor);
            }

            public T Current => current;

            object IEnumerator.Current => current;

            public void Dispose()
            {
                // LUCENENET: Intentionally blank
            }

            public void Submit(Func<T> task)
            {
                this.service.Submit(task);
                ++numTasks;
            }

            public void Reset()
            {
                throw UnsupportedOperationException.Create();
            }

            public bool MoveNext()
            {
                if (numTasks > 0)
                {
                    try
                    {
                        var awaitable = service.Take();
                        awaitable.Wait();
                        current = awaitable.Result;

                        return true;
                    }
                    catch (Exception e) when (e.IsInterruptedException())
                    {
                        throw new Util.ThreadInterruptedException(e);
                    }
                    catch (Exception e)
                    {
                        throw RuntimeException.Create(e);
                    }
                    finally
                    {
                        --numTasks;
                    }
                }

                return false;
            }

            // LUCENENET NOTE: Remove() excluded because it is not applicable in .NET

            public IEnumerator<T> GetEnumerator()
            {
                // use the shortcut here - this is only used in a private context
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
        }
#nullable enable
        
        /// <summary>
        /// A class holding a subset of the <see cref="IndexSearcher"/>s leaf contexts to be
        /// executed within a single thread.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public class LeafSlice
        {
            internal AtomicReaderContext[] Leaves { get; private set; }

            /// <summary>
            /// Initializes a new instance of <see cref="LeafSlice"/> with
            /// the specified <paramref name="leaves"/>.
            /// </summary>
            /// <param name="leaves">The collection of leaves.</param>
            /// <exception cref="ArgumentNullException"><paramref name="leaves"/> is <c>null</c>.</exception>
            public LeafSlice(params AtomicReaderContext[] leaves)
            {
                this.Leaves = leaves ?? throw new ArgumentNullException(nameof(leaves)); // LUCENENET: Added guard clause
            }
        }

        public override string ToString()
        {
            return "IndexSearcher(" + reader + "; executor=" + executor + ")";
        }

        /// <summary>
        /// Returns <see cref="Search.TermStatistics"/> for a term.
        /// <para/>
        /// This can be overridden for example, to return a term's statistics
        /// across a distributed collection.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="term"/> or
        /// <paramref name="context"/> is <c>null</c>.</exception>
        public virtual TermStatistics TermStatistics(Term term, TermContext context)
        {
            // LUCENENET: Added guard clauses
            if (term is null)
                throw new ArgumentNullException(nameof(term));
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            return new TermStatistics(term.Bytes, context.DocFreq, context.TotalTermFreq);
        }

        /// <summary>
        /// Returns <see cref="Search.CollectionStatistics"/> for a field.
        /// <para/>
        /// This can be overridden for example, to return a field's statistics
        /// across a distributed collection.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public virtual CollectionStatistics CollectionStatistics(string field)
        {
            // LUCENENET: Added guard clause
            if (field is null)
                throw new ArgumentNullException(nameof(field));

            int docCount;
            long sumTotalTermFreq;
            long sumDocFreq;

            // LUCENENET specific - replaced debug assert check for field being null with above guard clause

            Terms? terms = MultiFields.GetTerms(reader, field);
            if (terms is null)
            {
                docCount = 0;
                sumTotalTermFreq = 0;
                sumDocFreq = 0;
            }
            else
            {
                docCount = terms.DocCount;
                sumTotalTermFreq = terms.SumTotalTermFreq;
                sumDocFreq = terms.SumDocFreq;
            }
            return new CollectionStatistics(field, reader.MaxDoc, docCount, sumTotalTermFreq, sumDocFreq);
        }
    }
}