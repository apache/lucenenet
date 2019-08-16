using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Search
{
    using NUnit.Framework;
    using System.IO;
    using AllDeletedFilterReader = Lucene.Net.Index.AllDeletedFilterReader;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    //using Assert = junit.framework.Assert;

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using MultiReader = Lucene.Net.Index.MultiReader;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
    using Similarities;

    /// <summary>
    /// Utility class for sanity-checking queries.
    /// </summary>
    public class QueryUtils
    {
        /// <summary>
        /// Check the types of things query objects should be able to do. </summary>
        public static void Check(Query q)
        {
            CheckHashEquals(q);
        }

        /// <summary>
        /// Check very basic <see cref="object.GetHashCode()"/> and <see cref="object.Equals(object)"/>. </summary>
        public static void CheckHashEquals(Query q)
        {
            Query q2 = (Query)q.Clone();
            CheckEqual(q, q2);

            Query q3 = (Query)q.Clone();
            q3.Boost = 7.21792348f;
            CheckUnequal(q, q3);

            // test that a class check is done so that no exception is thrown
            // in the implementation of equals()
            Query whacky = new QueryAnonymousInnerClassHelper();
            whacky.Boost = q.Boost;
            CheckUnequal(q, whacky);

            // null test
            Assert.IsFalse(q.Equals(null));
        }

        private class QueryAnonymousInnerClassHelper : Query
        {
            public QueryAnonymousInnerClassHelper()
            {
            }

            public override string ToString(string field)
            {
                return "My Whacky Query";
            }
        }

        public static void CheckEqual(Query q1, Query q2)
        {
            Assert.IsTrue(q1.Equals(q2));
            Assert.AreEqual(q1, q2);
            Assert.AreEqual(q1.GetHashCode(), q2.GetHashCode());
        }

        public static void CheckUnequal(Query q1, Query q2)
        {
            Assert.IsFalse(q1.Equals(q2), q1 + " equal to " + q2);
            Assert.IsFalse(q2.Equals(q1), q2 + " equal to " + q1);

            // possible this test can fail on a hash collision... if that
            // happens, please change test to use a different example.
            Assert.IsTrue(q1.GetHashCode() != q2.GetHashCode());
        }

        /// <summary>
        /// Deep check that explanations of a query 'score' correctly. </summary>
        public static void CheckExplanations(Query q, IndexSearcher s)
        {
            CheckHits.CheckExplanations(q, null, s, true);
        }

        /// <summary>
        /// Various query sanity checks on a searcher, some checks are only done for
        /// instance of <see cref="IndexSearcher"/>.
        /// </summary>
        /// <param name="similarity">
        /// LUCENENET specific
        /// Removes dependency on <see cref="LuceneTestCase.ClassEnv.Similarity"/>
        /// </param>
        /// <seealso cref="Check(Query)"/>
        /// <seealso cref="CheckFirstSkipTo(Query, IndexSearcher, Similarity)"/>
        /// <seealso cref="CheckSkipTo(Query, IndexSearcher, Similarity)"/>
        /// <seealso cref="CheckExplanations(Query, IndexSearcher)"/>
        /// <seealso cref="CheckEqual(Query, Query)"/>
        // LUCENENET TODO: API remove Similarity
        public static void Check(Random random, Query q1, IndexSearcher s, Similarity similarity)
        {
            Check(random, q1, s, true, similarity);
        }

        /// <param name = "similarity" >
        /// LUCENENET specific
        /// Removes dependency on <see cref="LuceneTestCase.ClassEnv.Similarity"/>
        /// </param>
        // LUCENENET TODO: API remove Similarity
        public static void Check(Random random, Query q1, IndexSearcher s, bool wrap, Similarity similarity)
        {
            try
            {
                Check(q1);
                if (s != null)
                {
                    CheckFirstSkipTo(q1, s, similarity);
                    CheckSkipTo(q1, s, similarity);
                    if (wrap)
                    {
                        Check(random, q1, WrapUnderlyingReader(random, s, -1, similarity), false, similarity);
                        Check(random, q1, WrapUnderlyingReader(random, s, 0, similarity), false, similarity);
                        Check(random, q1, WrapUnderlyingReader(random, s, +1, similarity), false, similarity);
                    }
                    CheckExplanations(q1, s);

                    Query q2 = (Query)q1.Clone();
                    CheckEqual(s.Rewrite(q1), s.Rewrite(q2));
                }
            }
            catch (IOException e)
            {
                throw new Exception(e.ToString(), e);
            }
        }

        public static void PurgeFieldCache(IndexReader r)
        {
            // this is just a hack, to get an atomic reader that contains all subreaders for insanity checks
            FieldCache.DEFAULT.PurgeByCacheKey(SlowCompositeReaderWrapper.Wrap(r).CoreCacheKey);
        }

        /// <summary>
        /// This is a <see cref="MultiReader"/> that can be used for randomly wrapping other readers
        /// without creating <see cref="FieldCache"/> insanity.
        /// The trick is to use an opaque/fake cache key.
        /// </summary>
        public class FCInvisibleMultiReader : MultiReader
        {
            internal readonly object cacheKey = new object();

            public FCInvisibleMultiReader(params IndexReader[] readers)
                : base(readers)
            {
            }

            public override object CoreCacheKey
            {
                get
                {
                    return cacheKey;
                }
            }

            public override object CombinedCoreAndDeletesKey
            {
                get
                {
                    return cacheKey;
                }
            }
        }

        /// <summary>
        /// Given an <see cref="IndexSearcher"/>, returns a new <see cref="IndexSearcher"/> whose <see cref="IndexReader"/>
        /// is a <see cref="MultiReader"/> containing the <see cref="IndexReader"/> of the original <see cref="IndexSearcher"/>,
        /// as well as several "empty" <see cref="IndexReader"/>s -- some of which will have
        /// deleted documents in them.  This new <see cref="IndexSearcher"/> should
        /// behave exactly the same as the original <see cref="IndexSearcher"/>. </summary>
        /// <param name="s"> The searcher to wrap. </param>
        /// <param name="edge"> If negative, s will be the first sub; if 0, s will be in the middle, if positive s will be the last sub. </param>
        /// <param name="similarity">
        /// LUCENENET specific
        /// Removes dependency on <see cref="LuceneTestCase.ClassEnv.Similarity"/>
        /// </param>
        // LUCENENET TODO: API remove Similarity
        public static IndexSearcher WrapUnderlyingReader(Random random, IndexSearcher s, int edge, Similarity similarity)
        {
            IndexReader r = s.IndexReader;

            // we can't put deleted docs before the nested reader, because
            // it will throw off the docIds
            IndexReader[] readers = new IndexReader[] { edge < 0 ? r : emptyReaders[0], emptyReaders[0], new FCInvisibleMultiReader(edge < 0 ? emptyReaders[4] : emptyReaders[0], emptyReaders[0], 0 == edge ? r : emptyReaders[0]), 0 < edge ? emptyReaders[0] : emptyReaders[7], emptyReaders[0], new FCInvisibleMultiReader(0 < edge ? emptyReaders[0] : emptyReaders[5], emptyReaders[0], 0 < edge ? r : emptyReaders[0]) };

            IndexSearcher @out = LuceneTestCase.NewSearcher(new FCInvisibleMultiReader(readers), similarity);
            @out.Similarity = s.Similarity;
            return @out;
        }

        internal static readonly IndexReader[] emptyReaders = null;// = new IndexReader[8];

        static QueryUtils()
        {
            emptyReaders = new IndexReader[8];
            try
            {
                emptyReaders[0] = new MultiReader();
                emptyReaders[4] = MakeEmptyIndex(new Random(0), 4);
                emptyReaders[5] = MakeEmptyIndex(new Random(0), 5);
                emptyReaders[7] = MakeEmptyIndex(new Random(0), 7);
            }
            catch (IOException ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        private static IndexReader MakeEmptyIndex(Random random, int numDocs)
        {
            Debug.Assert(numDocs > 0);
            Directory d = new MockDirectoryWrapper(random, new RAMDirectory());
            IndexWriter w = new IndexWriter(d, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(random)));
            for (int i = 0; i < numDocs; i++)
            {
                w.AddDocument(new Document());
            }
            w.ForceMerge(1);
            w.Commit();
            w.Dispose();
            DirectoryReader reader = DirectoryReader.Open(d);
            return new AllDeletedFilterReader(LuceneTestCase.GetOnlySegmentReader(reader));
        }

        /// <summary>
        /// Alternate scorer skipTo(),skipTo(),next(),next(),skipTo(),skipTo(), etc
        /// and ensure a hitcollector receives same docs and scores.
        /// </summary>
        /// <param name = "similarity" >
        /// LUCENENET specific
        /// Removes dependency on <see cref="LuceneTestCase.ClassEnv.Similarity"/>
        /// </param>
        // LUCENENET TODO: API remove Similarity
        public static void CheckSkipTo(Query q, IndexSearcher s, Similarity similarity)
        {
            //System.out.println("Checking "+q);
            IList<AtomicReaderContext> readerContextArray = s.TopReaderContext.Leaves;
            if (s.CreateNormalizedWeight(q).ScoresDocsOutOfOrder) // in this case order of skipTo() might differ from that of next().
            {
                return;
            }

            const int skip_op = 0;
            const int next_op = 1;
            int[][] orders = new int[][] { new int[] { next_op }, new int[] { skip_op }, new int[] { skip_op, next_op }, new int[] { next_op, skip_op }, new int[] { skip_op, skip_op, next_op, next_op }, new int[] { next_op, next_op, skip_op, skip_op }, new int[] { skip_op, skip_op, skip_op, next_op, next_op } };
            for (int k = 0; k < orders.Length; k++)
            {
                int[] order = orders[k];
                // System.out.print("Order:");for (int i = 0; i < order.Length; i++)
                // System.out.print(order[i]==skip_op ? " skip()":" next()");
                // System.out.println();
                int[] opidx = new int[] { 0 };
                int[] lastDoc = new int[] { -1 };

                // FUTURE: ensure scorer.Doc()==-1

                const float maxDiff = 1e-5f;
                AtomicReader[] lastReader = new AtomicReader[] { null };

                s.Search(q, new CollectorAnonymousInnerClassHelper(q, s, readerContextArray, skip_op, order, opidx, lastDoc, maxDiff, lastReader, similarity));

                if (lastReader[0] != null)
                {
                    // confirm that skipping beyond the last doc, on the
                    // previous reader, hits NO_MORE_DOCS
                    AtomicReader previousReader = lastReader[0];
                    IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader, false, similarity);
                    indexSearcher.Similarity = s.Similarity;
                    Weight w = indexSearcher.CreateNormalizedWeight(q);
                    AtomicReaderContext ctx = (AtomicReaderContext)previousReader.Context;
                    Scorer scorer = w.GetScorer(ctx, ((AtomicReader)ctx.Reader).LiveDocs);
                    if (scorer != null)
                    {
                        bool more = scorer.Advance(lastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
                        Assert.IsFalse(more, "query's last doc was " + lastDoc[0] + " but skipTo(" + (lastDoc[0] + 1) + ") got to " + scorer.DocID);
                    }
                }
            }
        }

        private class CollectorAnonymousInnerClassHelper : ICollector
        {
            private Query q;
            private IndexSearcher s;
            private IList<AtomicReaderContext> readerContextArray;
            private int skip_op;
            private int[] order;
            private int[] opidx;
            private int[] lastDoc;
            private float maxDiff;
            private AtomicReader[] lastReader;
            private readonly Similarity similarity;

            public CollectorAnonymousInnerClassHelper(Query q, IndexSearcher s, IList<AtomicReaderContext> readerContextArray, 
                int skip_op, int[] order, int[] opidx, int[] lastDoc, float maxDiff, AtomicReader[] lastReader,
                Similarity similarity)
            {
                this.q = q;
                this.s = s;
                this.readerContextArray = readerContextArray;
                this.skip_op = skip_op;
                this.order = order;
                this.opidx = opidx;
                this.lastDoc = lastDoc;
                this.maxDiff = maxDiff;
                this.lastReader = lastReader;
                this.similarity = similarity;
            }

            private Scorer sc;
            private Scorer scorer;
            private int leafPtr;

            public virtual void SetScorer(Scorer scorer)
            {
                this.sc = scorer;
            }

            public virtual void Collect(int doc)
            {
                float score = sc.GetScore();
                lastDoc[0] = doc;
                try
                {
                    if (scorer == null)
                    {
                        Weight w = s.CreateNormalizedWeight(q);
                        AtomicReaderContext context = readerContextArray[leafPtr];
                        scorer = w.GetScorer(context, (context.AtomicReader).LiveDocs);
                    }

                    int op = order[(opidx[0]++) % order.Length];
                    // System.out.println(op==skip_op ?
                    // "skip("+(sdoc[0]+1)+")":"next()");
                    bool more = op == skip_op ? scorer.Advance(scorer.DocID + 1) != DocIdSetIterator.NO_MORE_DOCS : scorer.NextDoc() != DocIdSetIterator.NO_MORE_DOCS;
                    int scorerDoc = scorer.DocID;
                    float scorerScore = scorer.GetScore();
                    float scorerScore2 = scorer.GetScore();
                    float scoreDiff = Math.Abs(score - scorerScore);
                    float scorerDiff = Math.Abs(scorerScore2 - scorerScore);
                    if (!more || doc != scorerDoc || scoreDiff > maxDiff || scorerDiff > maxDiff)
                    {
                        StringBuilder sbord = new StringBuilder();
                        for (int i = 0; i < order.Length; i++)
                        {
                            sbord.Append(order[i] == skip_op ? " skip()" : " next()");
                        }
                        throw new Exception("ERROR matching docs:" + "\n\t" + (doc != scorerDoc ? "--> " : "") + "doc=" + doc + ", scorerDoc=" + scorerDoc + "\n\t" + (!more ? "--> " : "") + "tscorer.more=" + more + "\n\t" + (scoreDiff > maxDiff ? "--> " : "") + "scorerScore=" + scorerScore + " scoreDiff=" + scoreDiff + " maxDiff=" + maxDiff + "\n\t" + (scorerDiff > maxDiff ? "--> " : "") + "scorerScore2=" + scorerScore2 + " scorerDiff=" + scorerDiff + "\n\thitCollector.Doc=" + doc + " score=" + score + "\n\t Scorer=" + scorer + "\n\t Query=" + q + "  " + q.GetType().Name + "\n\t Searcher=" + s + "\n\t Order=" + sbord + "\n\t Op=" + (op == skip_op ? " skip()" : " next()"));
                    }
                }
                catch (IOException e)
                {
                    throw new Exception(e.ToString(), e);
                }
            }

            public virtual void SetNextReader(AtomicReaderContext context)
            {
                // confirm that skipping beyond the last doc, on the
                // previous reader, hits NO_MORE_DOCS
                if (lastReader[0] != null)
                {
                    AtomicReader previousReader = lastReader[0];
                    IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader, similarity);
                    indexSearcher.Similarity = s.Similarity;
                    Weight w = indexSearcher.CreateNormalizedWeight(q);
                    AtomicReaderContext ctx = (AtomicReaderContext)indexSearcher.TopReaderContext;
                    Scorer scorer = w.GetScorer(ctx, ((AtomicReader)ctx.Reader).LiveDocs);
                    if (scorer != null)
                    {
                        bool more = scorer.Advance(lastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
                        Assert.IsFalse(more, "query's last doc was " + lastDoc[0] + " but skipTo(" + (lastDoc[0] + 1) + ") got to " + scorer.DocID);
                    }
                    leafPtr++;
                }
                lastReader[0] = (AtomicReader)context.Reader;
                Debug.Assert(readerContextArray[leafPtr].Reader == context.Reader);
                this.scorer = null;
                lastDoc[0] = -1;
            }

            public virtual bool AcceptsDocsOutOfOrder
            {
                get { return false; }
            }
        }

        /// <summary>
        /// Check that first skip on just created scorers always goes to the right doc.</summary>
        /// <param name = "similarity" >
        /// LUCENENET specific
        /// Removes dependency on <see cref="LuceneTestCase.ClassEnv.Similarity"/>
        /// </param>
        // LUCENENET TODO: API remove Similarity
        public static void CheckFirstSkipTo(Query q, IndexSearcher s, Similarity similarity)
        {
            //System.out.println("checkFirstSkipTo: "+q);
            const float maxDiff = 1e-3f;
            int[] lastDoc = new int[] { -1 };
            AtomicReader[] lastReader = new AtomicReader[] { null };
            IList<AtomicReaderContext> context = s.TopReaderContext.Leaves;
            s.Search(q, new CollectorAnonymousInnerClassHelper2(q, s, maxDiff, lastDoc, lastReader, context, similarity));

            if (lastReader[0] != null)
            {
                // confirm that skipping beyond the last doc, on the
                // previous reader, hits NO_MORE_DOCS
                AtomicReader previousReader = lastReader[0];
                IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader, similarity);
                indexSearcher.Similarity = s.Similarity;
                Weight w = indexSearcher.CreateNormalizedWeight(q);
                Scorer scorer = w.GetScorer((AtomicReaderContext)indexSearcher.TopReaderContext, previousReader.LiveDocs);
                if (scorer != null)
                {
                    bool more = scorer.Advance(lastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
                    Assert.IsFalse(more, "query's last doc was " + lastDoc[0] + " but skipTo(" + (lastDoc[0] + 1) + ") got to " + scorer.DocID);
                }
            }
        }

        private class CollectorAnonymousInnerClassHelper2 : ICollector
        {
            private Query q;
            private IndexSearcher s;
            private float maxDiff;
            private int[] lastDoc;
            private AtomicReader[] lastReader;
            private IList<AtomicReaderContext> context;
            private readonly Similarity similarity;

            public CollectorAnonymousInnerClassHelper2(Query q, IndexSearcher s, float maxDiff, int[] lastDoc, AtomicReader[] lastReader, IList<AtomicReaderContext> context, Similarity similarity)
            {
                this.q = q;
                this.s = s;
                this.maxDiff = maxDiff;
                this.lastDoc = lastDoc;
                this.lastReader = lastReader;
                this.context = context;
                this.similarity = similarity;
            }

            private Scorer scorer;
            private int leafPtr;
            private IBits liveDocs;

            public virtual void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public virtual void Collect(int doc)
            {
                float score = scorer.GetScore();
                try
                {
                    long startMS = Environment.TickCount;
                    for (int i = lastDoc[0] + 1; i <= doc; i++)
                    {
                        Weight w = s.CreateNormalizedWeight(q);
                        Scorer scorer_ = w.GetScorer(context[leafPtr], liveDocs);
                        Assert.IsTrue(scorer_.Advance(i) != DocIdSetIterator.NO_MORE_DOCS, "query collected " + doc + " but skipTo(" + i + ") says no more docs!");
                        Assert.AreEqual(doc, scorer_.DocID, "query collected " + doc + " but skipTo(" + i + ") got to " + scorer_.DocID);
                        float skipToScore = scorer_.GetScore();
                        Assert.AreEqual(skipToScore, scorer_.GetScore(), maxDiff, "unstable skipTo(" + i + ") score!");
                        Assert.AreEqual(score, skipToScore, maxDiff, "query assigned doc " + doc + " a score of <" + score + "> but skipTo(" + i + ") has <" + skipToScore + ">!");

                        // Hurry things along if they are going slow (eg
                        // if you got SimpleText codec this will kick in):
                        if (i < doc && Environment.TickCount - startMS > 5)
                        {
                            i = doc - 1;
                        }
                    }
                    lastDoc[0] = doc;
                }
                catch (IOException e)
                {
                    throw new Exception(e.ToString(), e);
                }
            }

            public virtual void SetNextReader(AtomicReaderContext context)
            {
                // confirm that skipping beyond the last doc, on the
                // previous reader, hits NO_MORE_DOCS
                if (lastReader[0] != null)
                {
                    AtomicReader previousReader = lastReader[0];
                    IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader, similarity);
                    indexSearcher.Similarity = s.Similarity;
                    Weight w = indexSearcher.CreateNormalizedWeight(q);
                    Scorer scorer = w.GetScorer((AtomicReaderContext)indexSearcher.TopReaderContext, previousReader.LiveDocs);
                    if (scorer != null)
                    {
                        bool more = scorer.Advance(lastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
                        Assert.IsFalse(more, "query's last doc was " + lastDoc[0] + " but skipTo(" + (lastDoc[0] + 1) + ") got to " + scorer.DocID);
                    }
                    leafPtr++;
                }

                lastReader[0] = (AtomicReader)context.Reader;
                lastDoc[0] = -1;
                liveDocs = ((AtomicReader)context.Reader).LiveDocs;
            }

            public virtual bool AcceptsDocsOutOfOrder
            {
                get { return false; }
            }
        }
    }
}