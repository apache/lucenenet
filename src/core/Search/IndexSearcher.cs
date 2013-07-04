/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Index;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
using Directory = Lucene.Net.Store.Directory;
using Document = Lucene.Net.Documents.Document;
using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;

namespace Lucene.Net.Search
{

    /// <summary>Implements search over a single IndexReader.
    /// 
    /// <p/>Applications usually need only call the inherited <see cref="Searcher.Search(Query,int)" />
    /// or <see cref="Searcher.Search(Query,Filter,int)" /> methods. For performance reasons it is 
    /// recommended to open only one IndexSearcher and use it for all of your searches.
    /// 
    /// <a name="thread-safety"></a><p/><b>NOTE</b>:
    /// <see cref="IndexSearcher" /> instances are completely
    /// thread safe, meaning multiple threads can call any of its
    /// methods, concurrently.  If your application requires
    /// external synchronization, you should <b>not</b>
    /// synchronize on the <c>IndexSearcher</c> instance;
    /// use your own (non-Lucene) objects instead.<p/>
    /// </summary>
    [Serializable]
    public class IndexSearcher
    {
        internal readonly IndexReader reader;

        // NOTE: these members might change in incompatible ways
        // in the next release
        protected readonly IndexReaderContext readerContext;
        protected readonly IList<AtomicReaderContext> leafContexts;
        /** used with executor - each slice holds a set of leafs executed within one thread */
        protected readonly LeafSlice[] leafSlices;

        // These are only used for multi-threaded search
        private readonly TaskScheduler executor;

        // the default Similarity
        private static readonly Similarity defaultSimilarity = new DefaultSimilarity();

        public static Similarity DefaultSimilarity
        {
            get { return defaultSimilarity; }
        }

        private Similarity similarity = defaultSimilarity;

        /// <summary>Creates a searcher searching the provided index
        /// <para>
        /// Note that the underlying IndexReader is not closed, if
        /// IndexSearcher was constructed with IndexSearcher(IndexReader r).
        /// If the IndexReader was supplied implicitly by specifying a directory, then
        /// the IndexReader gets closed.
        /// </para>
        /// </summary>
        public IndexSearcher(IndexReader r)
            : this(r, null)
        {
        }

        public IndexSearcher(IndexReader r, TaskScheduler executor)
            : this(r.Context, executor)
        {
        }

        public IndexSearcher(IndexReaderContext context, TaskScheduler executor)
        {
            //assert context.isTopLevel: "IndexSearcher's ReaderContext must be topLevel for reader" + context.reader();
            reader = context.Reader;
            this.executor = executor;
            this.readerContext = context;
            leafContexts = context.Leaves;
            this.leafSlices = executor == null ? null : Slices(leafContexts);
        }

        public IndexSearcher(IndexReaderContext context)
            : this(context, null)
        {
        }

        protected virtual LeafSlice[] Slices(IList<AtomicReaderContext> leaves)
        {
            LeafSlice[] slices = new LeafSlice[leaves.Count];
            for (int i = 0; i < slices.Length; i++)
            {
                slices[i] = new LeafSlice(leaves[i]);
            }
            return slices;
        }

        /// <summary>Return the <see cref="Index.IndexReader" /> this searches. </summary>
        public virtual IndexReader IndexReader
        {
            get { return reader; }
        }

        public virtual Document Doc(int docID)
        {
            return reader.Document(docID);
        }

        public virtual void Doc(int docID, StoredFieldVisitor fieldVisitor)
        {
            reader.Document(docID, fieldVisitor);
        }

        public virtual Document Doc(int docID, ISet<String> fieldsToLoad)
        {
            return reader.Document(docID, fieldsToLoad);
        }

        [Obsolete]
        public Document Document(int docID, ISet<String> fieldsToLoad)
        {
            return Doc(docID, fieldsToLoad);
        }

        public virtual Similarity Similarity
        {
            get { return similarity; }
            set { similarity = value; }
        }

        protected virtual Query WrapFilter(Query query, Filter filter)
        {
            return (filter == null) ? query : new FilteredQuery(query, filter);
        }

        public TopDocs SearchAfter(ScoreDoc after, Query query, int n)
        {
            return Search(CreateNormalizedWeight(query), after, n);
        }

        public TopDocs SearchAfter(ScoreDoc after, Query query, Filter filter, int n)
        {
            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), after, n);
        }

        public TopDocs Search(Query query, int n)
        {
            return Search(query, null, n);
        }

        public TopDocs Search(Query query, Filter filter, int n)
        {
            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), null, n);
        }

        public void Search(Query query, Filter filter, Collector results)
        {
            Search(leafContexts, CreateNormalizedWeight(WrapFilter(query, filter)), results);
        }

        public void Search(Query query, Collector results)
        {
            Search(leafContexts, CreateNormalizedWeight(query), results);
        }

        public TopFieldDocs Search(Query query, Filter filter, int n, Sort sort)
        {
            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), n, sort, false, false);
        }

        public TopFieldDocs Search(Query query, Filter filter, int n, Sort sort, bool doDocScores, bool doMaxScore)
        {
            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), n, sort, doDocScores, doMaxScore);
        }

        public TopDocs SearchAfter(ScoreDoc after, Query query, Filter filter, int n, Sort sort)
        {
            if (after != null && !(after is FieldDoc))
            {
                // TODO: if we fix type safety of TopFieldDocs we can
                // remove this
                throw new ArgumentException("after must be a FieldDoc; got " + after);
            }
            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), (FieldDoc)after, n, sort, true, false, false);
        }

        public TopFieldDocs Search(Query query, int n, Sort sort)
        {
            return Search(CreateNormalizedWeight(query), n, sort, false, false);
        }

        public TopDocs SearchAfter(ScoreDoc after, Query query, int n, Sort sort)
        {
            if (after != null && !(after is FieldDoc))
            {
                // TODO: if we fix type safety of TopFieldDocs we can
                // remove this
                throw new ArgumentException("after must be a FieldDoc; got " + after);
            }
            return Search(CreateNormalizedWeight(query), (FieldDoc)after, n, sort, true, false, false);
        }

        public TopDocs SearchAfter(ScoreDoc after, Query query, Filter filter, int n, Sort sort, bool doDocScores, bool doMaxScore)
        {
            if (after != null && !(after is FieldDoc))
            {
                // TODO: if we fix type safety of TopFieldDocs we can
                // remove this
                throw new ArgumentException("after must be a FieldDoc; got " + after);
            }
            return Search(CreateNormalizedWeight(WrapFilter(query, filter)), (FieldDoc)after, n, sort, true,
                          doDocScores, doMaxScore);
        }

        protected TopDocs Search(Weight weight, ScoreDoc after, int nDocs)
        {
            int limit = reader.MaxDoc;
            if (limit == 0)
            {
                limit = 1;
            }
            nDocs = Math.Min(nDocs, limit);

            if (executor == null)
            {
                return Search(leafContexts, weight, after, nDocs);
            }
            else
            {
                HitQueue hq = new HitQueue(nDocs, false);
                ReentrantLock lock_renamed = new ReentrantLock();
                ExecutionHelper<TopDocs> runner = new ExecutionHelper<TopDocs>(executor);

                for (int i = 0; i < leafSlices.Length; i++)
                {
                    // search each sub
                    runner.Submit(new SearcherCallableNoSort(lock_renamed, this, leafSlices[i], weight, after, nDocs, hq));
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

                ScoreDoc[] scoreDocs = new ScoreDoc[hq.Size];
                for (int i = hq.Size - 1; i >= 0; i--) // put docs in array
                    scoreDocs[i] = hq.Pop();

                return new TopDocs(totalHits, scoreDocs, maxScore);
            }
        }

        protected TopDocs Search(IList<AtomicReaderContext> leaves, Weight weight, ScoreDoc after, int nDocs)
        {
            // single thread
            int limit = reader.MaxDoc;
            if (limit == 0)
            {
                limit = 1;
            }
            nDocs = Math.Min(nDocs, limit);
            TopScoreDocCollector collector = TopScoreDocCollector.Create(nDocs, after, !weight.ScoresDocsOutOfOrder);
            Search(leaves, weight, collector);
            return collector.TopDocs();
        }

        protected TopFieldDocs Search(Weight weight,
                                int nDocs, Sort sort,
                                bool doDocScores, bool doMaxScore)
        {
            return Search(weight, null, nDocs, sort, true, doDocScores, doMaxScore);
        }

        protected TopFieldDocs Search(Weight weight, FieldDoc after, int nDocs,
                                Sort sort, bool fillFields,
                                bool doDocScores, bool doMaxScore)
        {

            if (sort == null) throw new NullReferenceException("Sort must not be null");

            int limit = reader.MaxDoc;
            if (limit == 0)
            {
                limit = 1;
            }
            nDocs = Math.Min(nDocs, limit);

            if (executor == null)
            {
                // use all leaves here!
                return Search(leafContexts, weight, after, nDocs, sort, fillFields, doDocScores, doMaxScore);
            }
            else
            {
                TopFieldCollector topCollector = TopFieldCollector.Create(sort, nDocs,
                                                                                after,
                                                                                fillFields,
                                                                                doDocScores,
                                                                                doMaxScore,
                                                                                false);

                ReentrantLock lock_renamed = new ReentrantLock();
                ExecutionHelper<TopFieldDocs> runner = new ExecutionHelper<TopFieldDocs>(executor);
                for (int i = 0; i < leafSlices.Length; i++)
                { 
                    // search each leaf slice
                    runner.Submit(new SearcherCallableWithSort(lock_renamed, this, leafSlices[i], weight, after, nDocs, topCollector, sort, doDocScores, doMaxScore));
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

                TopFieldDocs topDocs = (TopFieldDocs)topCollector.TopDocs();

                return new TopFieldDocs(totalHits, topDocs.ScoreDocs, topDocs.fields, topDocs.MaxScore);
            }
        }

        protected TopFieldDocs Search(IList<AtomicReaderContext> leaves, Weight weight, FieldDoc after, int nDocs,
                               Sort sort, bool fillFields, bool doDocScores, bool doMaxScore)
        {
            // single thread
            int limit = reader.MaxDoc;
            if (limit == 0)
            {
                limit = 1;
            }
            nDocs = Math.Min(nDocs, limit);

            TopFieldCollector collector = TopFieldCollector.Create(sort, nDocs, after,
                                                                   fillFields, doDocScores,
                                                                   doMaxScore, !weight.ScoresDocsOutOfOrder);
            Search(leaves, weight, collector);
            return (TopFieldDocs)collector.TopDocs();
        }

        protected void Search(IList<AtomicReaderContext> leaves, Weight weight, Collector collector)
        {

            // TODO: should we make this
            // threaded...?  the Collector could be sync'd?
            // always use single thread:
            foreach (AtomicReaderContext ctx in leaves)
            { // search each subreader
                try
                {
                    collector.SetNextReader(ctx);
                }
                catch (CollectionTerminatedException e)
                {
                    // there is no doc of interest in this reader context
                    // continue with the following leaf
                    continue;
                }
                Scorer scorer = weight.Scorer(ctx, !collector.AcceptsDocsOutOfOrder, true, ctx.Reader.LiveDocs);
                if (scorer != null)
                {
                    try
                    {
                        scorer.Score(collector);
                    }
                    catch (CollectionTerminatedException e)
                    {
                        // collection was terminated prematurely
                        // continue with the following leaf
                    }
                }
            }
        }

        public Query Rewrite(Query original)
        {
            Query query = original;
            for (Query rewrittenQuery = query.Rewrite(reader); rewrittenQuery != query;
                 rewrittenQuery = query.Rewrite(reader))
            {
                query = rewrittenQuery;
            }
            return query;
        }

        public Explanation Explain(Query query, int doc)
        {
            return Explain(CreateNormalizedWeight(query), doc);
        }

        protected Explanation Explain(Weight weight, int doc)
        {
            int n = ReaderUtil.SubIndex(doc, leafContexts);
            AtomicReaderContext ctx = leafContexts[n];
            int deBasedDoc = doc - ctx.docBase;

            return weight.Explain(ctx, deBasedDoc);
        }

        public Weight CreateNormalizedWeight(Query query)
        {
            query = Rewrite(query);
            Weight weight = query.CreateWeight(this);
            float v = weight.ValueForNormalization;
            float norm = Similarity.QueryNorm(v);
            if (float.IsInfinity(norm) || float.IsNaN(norm))
            {
                norm = 1.0f;
            }
            weight.Normalize(norm, 1.0f);
            return weight;
        }

        public IndexReaderContext TopReaderContext
        {
            get
            {
                return readerContext;
            }
        }

        private sealed class SearcherCallableNoSort : ICallable<TopDocs>
        {
            private readonly ReentrantLock lock_renamed;
            private readonly IndexSearcher searcher;
            private readonly Weight weight;
            private readonly ScoreDoc after;
            private readonly int nDocs;
            private readonly HitQueue hq;
            private readonly LeafSlice slice;

            public SearcherCallableNoSort(ReentrantLock lock_renamed, IndexSearcher searcher, LeafSlice slice, Weight weight,
                ScoreDoc after, int nDocs, HitQueue hq)
            {
                this.lock_renamed = lock_renamed;
                this.searcher = searcher;
                this.weight = weight;
                this.after = after;
                this.nDocs = nDocs;
                this.hq = hq;
                this.slice = slice;
            }

            public TopDocs Call()
            {
                TopDocs docs = searcher.Search(slice.leaves, weight, after, nDocs);
                ScoreDoc[] scoreDocs = docs.ScoreDocs;
                //it would be so nice if we had a thread-safe insert 
                lock_renamed.Lock();
                try
                {
                    for (int j = 0; j < scoreDocs.Length; j++)
                    { // merge scoreDocs into hq
                        ScoreDoc scoreDoc = scoreDocs[j];
                        if (scoreDoc == hq.InsertWithOverflow(scoreDoc))
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    lock_renamed.Unlock();
                }
                return docs;
            }
        }

        private sealed class SearcherCallableWithSort : ICallable<TopFieldDocs>
        {
            private readonly ReentrantLock lock_renamed;
            private readonly IndexSearcher searcher;
            private readonly Weight weight;
            private readonly int nDocs;
            private readonly TopFieldCollector hq;
            private readonly Sort sort;
            private readonly LeafSlice slice;
            private readonly FieldDoc after;
            private readonly bool doDocScores;
            private readonly bool doMaxScore;

            public SearcherCallableWithSort(ReentrantLock lock_renamed, IndexSearcher searcher, LeafSlice slice, Weight weight,
                                    FieldDoc after, int nDocs, TopFieldCollector hq, Sort sort,
                                    bool doDocScores, bool doMaxScore)
            {
                this.lock_renamed = lock_renamed;
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

            private sealed class FakeScorer : Scorer
            {
                internal float score;
                internal int doc;

                public FakeScorer()
                    : base(null)
                {
                }

                public override int Advance(int target)
                {
                    throw new NotSupportedException("FakeScorer doesn't support advance(int)");
                }

                public override int DocID
                {
                    get { return doc; }
                }

                public override int Freq
                {
                    get { throw new NotSupportedException("FakeScorer doesn't support freq()"); }
                }

                public override int NextDoc()
                {
                    throw new NotSupportedException("FakeScorer doesn't support nextDoc()");
                }

                public override float Score()
                {
                    return score;
                }

                public override long Cost
                {
                    get { return 1; }
                }
            }

            private readonly FakeScorer fakeScorer = new FakeScorer();

            public TopFieldDocs Call()
            {
                //assert slice.leaves.length == 1;
                TopFieldDocs docs = searcher.Search(slice.leaves,
                    weight, after, nDocs, sort, true, doDocScores || sort.NeedsScores(), doMaxScore);
                lock_renamed.Lock();
                try
                {
                    AtomicReaderContext ctx = slice.leaves[0];
                    int docbase = ctx.docBase;
                    hq.SetNextReader(ctx);
                    hq.SetScorer(fakeScorer);
                    foreach (ScoreDoc scoreDoc in docs.ScoreDocs)
                    {
                        fakeScorer.doc = scoreDoc.Doc - docbase;
                        fakeScorer.score = scoreDoc.Score;
                        hq.Collect(scoreDoc.Doc - docbase);
                    }

                    // Carry over maxScore from sub:
                    if (doMaxScore && docs.MaxScore > hq.maxScore)
                    {
                        hq.maxScore = docs.MaxScore;
                    }
                }
                finally
                {
                    lock_renamed.Unlock();
                }
                return docs;
            }
        }

        private sealed class ExecutionHelper<T> : IEnumerator<T>, IEnumerable<T>
        {
            private readonly ICompletionService<T> service;
            private int numTasks;
            private T current;

            public ExecutionHelper(TaskScheduler executor)
            {
                service = new TaskSchedulerCompletionService<T>(executor);
            }

            public T Current
            {
                get { return current; }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return current; }
            }

            public bool MoveNext()
            {
                if (numTasks > 0)
                {
                    try
                    {
                        current = service.Take().Result;
                    }
                    finally
                    {
                        --numTasks;
                    }
                }

                return false;
            }

            public void Submit(ICallable<T> task)
            {
                this.service.Submit(task);
                ++numTasks;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this;
            }
        }

        public class LeafSlice
        {
            internal readonly AtomicReaderContext[] leaves;

            public LeafSlice(params AtomicReaderContext[] leaves)
            {
                this.leaves = leaves;
            }
        }

        public override string ToString()
        {
            return "IndexSearcher(" + reader + "; executor=" + executor + ")";
        }

        public TermStatistics TermStatistics(Term term, TermContext context)
        {
            return new TermStatistics(term.Bytes, context.DocFreq, context.TotalTermFreq);
        }

        public CollectionStatistics CollectionStatistics(string field)
        {
            int docCount;
            long sumTotalTermFreq;
            long sumDocFreq;

            //assert field != null;

            Terms terms = MultiFields.GetTerms(reader, field);
            if (terms == null)
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