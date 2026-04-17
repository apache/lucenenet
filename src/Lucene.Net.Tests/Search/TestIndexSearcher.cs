using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;

    [TestFixture]
    public class TestIndexSearcher : LuceneTestCase
    {
        private Directory dir;
        private IndexReader reader;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            for (int i = 0; i < 100; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("field", Convert.ToString(i), Field.Store.NO));
                doc.Add(NewStringField("field2", Convert.ToString(i % 2 == 0), Field.Store.NO));
                iw.AddDocument(doc);
            }
            reader = iw.GetReader();
            iw.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        // should not throw exception
        [Test]
        public virtual void TestHugeN()
        {
            // LUCENENET: this differs from Java
            TaskScheduler service = new LimitedConcurrencyLevelTaskScheduler(4);

            IndexSearcher[] searchers = new IndexSearcher[]
            {
                new IndexSearcher(reader),
                new IndexSearcher(reader, service)
            };
            Query[] queries = new Query[]
            {
                new MatchAllDocsQuery(),
                new TermQuery(new Term("field", "1"))
            };
            Sort[] sorts = new Sort[]
            {
                null,
                new Sort(new SortField("field2", SortFieldType.STRING))
            };
            Filter[] filters = new Filter[]
            {
                null,
                new QueryWrapperFilter(new TermQuery(new Term("field2", "true")))
            };
            ScoreDoc[] afters = new ScoreDoc[]
            {
                null,
                new FieldDoc(0, 0f, new object[] { new BytesRef("boo!") })
            };

            foreach (IndexSearcher searcher in searchers)
            {
                foreach (ScoreDoc after in afters)
                {
                    foreach (Query query in queries)
                    {
                        foreach (Sort sort in sorts)
                        {
                            foreach (Filter filter in filters)
                            {
                                searcher.Search(query, int.MaxValue);
                                searcher.SearchAfter(after, query, int.MaxValue);
                                searcher.Search(query, filter, int.MaxValue);
                                searcher.SearchAfter(after, query, filter, int.MaxValue);
                                if (sort != null)
                                {
                                    searcher.Search(query, int.MaxValue, sort);
                                    searcher.Search(query, filter, int.MaxValue, sort);
                                    searcher.Search(query, filter, int.MaxValue, sort, true, true);
                                    searcher.Search(query, filter, int.MaxValue, sort, true, false);
                                    searcher.Search(query, filter, int.MaxValue, sort, false, true);
                                    searcher.Search(query, filter, int.MaxValue, sort, false, false);
                                    searcher.SearchAfter(after, query, filter, int.MaxValue, sort);
                                    searcher.SearchAfter(after, query, filter, int.MaxValue, sort, true, true);
                                    searcher.SearchAfter(after, query, filter, int.MaxValue, sort, true, false);
                                    searcher.SearchAfter(after, query, filter, int.MaxValue, sort, false, true);
                                    searcher.SearchAfter(after, query, filter, int.MaxValue, sort, false, false);
                                }
                            }
                        }
                    }
                }
            }

            // LUCENENET: .NET doesn't have a way to shut down the TaskScheduler explicitly
            //TestUtil.ShutdownExecutorService(service);
        }

        [Test]
        public virtual void TestSearchAfterPassedMaxDoc()
        {
            // LUCENE-5128: ensure we get a meaningful message if searchAfter exceeds maxDoc
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            w.AddDocument(new Document());
            IndexReader r = w.GetReader();
            w.Dispose();

            IndexSearcher s = new IndexSearcher(r);
            try
            {
                s.SearchAfter(new ScoreDoc(r.MaxDoc, 0.54f), new MatchAllDocsQuery(), 10);
                Assert.Fail("should have hit IllegalArgumentException when searchAfter exceeds maxDoc");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // ok
            }
            finally
            {
                IOUtils.Dispose(r, dir);
            }
        }

        // LUCENENET specific - tests for the CancellationToken support
        // added to IndexSearcher methods. See #922.

        /// <summary>
        /// Builds a multi-segment index so cancellation at leaf boundaries is observable.
        /// </summary>
        private static IndexReader BuildMultiSegmentReader(Directory directory)
        {
            RandomIndexWriter iw = new RandomIndexWriter(Random, directory);
            for (int i = 0; i < 50; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("field", Convert.ToString(i), Field.Store.NO));
                iw.AddDocument(doc);
                // Commit every few docs to force multiple segments (leaves).
                if (i % 5 == 0)
                {
                    iw.Commit();
                }
            }
            IndexReader r = iw.GetReader();
            iw.Dispose();
            return r;
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestCancellation_SingleThreaded_PreCanceledToken_ThrowsOperationCanceledException()
        {
            // When the executor is null (single-threaded), an already-canceled token should
            // cause OperationCanceledException to be thrown on the first leaf.
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            IndexSearcher searcher = new IndexSearcher(reader);
            Query query = new MatchAllDocsQuery();

            Assert.Throws<OperationCanceledException>(
                () => searcher.Search(query, 10, cts.Token));

            Assert.Throws<OperationCanceledException>(
                () => searcher.Search(query, filter: null, 10, cts.Token));

            Assert.Throws<OperationCanceledException>(
                () => searcher.SearchAfter(after: null, query, 10, cts.Token));

            Assert.Throws<OperationCanceledException>(
                () => searcher.SearchAfter(after: null, query, filter: null, 10, cts.Token));

            Sort sort = new Sort(new SortField("field", SortFieldType.STRING));
            Assert.Throws<OperationCanceledException>(
                () => searcher.Search(query, 10, sort, cts.Token));

            Assert.Throws<OperationCanceledException>(
                () => searcher.Search(query, filter: null, 10, sort, cts.Token));

            Assert.Throws<OperationCanceledException>(
                () => searcher.Search(query, filter: null, 10, sort, doDocScores: true, doMaxScore: true, cts.Token));

            Assert.Throws<OperationCanceledException>(
                () => searcher.SearchAfter(after: null, query, filter: null, 10, sort, cts.Token));

            Assert.Throws<OperationCanceledException>(
                () => searcher.SearchAfter(after: null, query, filter: null, 10, sort, doDocScores: true, doMaxScore: true, cts.Token));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestCancellation_SingleThreaded_CollectorOverload_PreCanceledTokenThrows()
        {
            // The ICollector overloads also thread the CancellationToken through and
            // should throw on entry to the leaf iteration.
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            IndexSearcher searcher = new IndexSearcher(reader);
            Query query = new MatchAllDocsQuery();

            TotalHitCountCollector collector = new TotalHitCountCollector();

            Assert.Throws<OperationCanceledException>(
                () => searcher.Search(query, collector, cts.Token));

            Assert.Throws<OperationCanceledException>(
                () => searcher.Search(query, filter: null, collector, cts.Token));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestCancellation_SingleThreaded_CancelDuringSearch_StopsAtNextLeaf()
        {
            // Verify that cancellation requested during collection takes effect at the
            // next leaf boundary, and the partial work already performed does not prevent
            // the OperationCanceledException from being observed.
            using Directory directory = NewDirectory();
            IndexReader r = BuildMultiSegmentReader(directory);
            try
            {
                // Require at least 2 leaves for this assertion to be meaningful.
                Assume.That(r.Leaves.Count >= 2, "Test requires a multi-segment index");

                IndexSearcher searcher = new IndexSearcher(r);
                using CancellationTokenSource cts = new CancellationTokenSource();

                int leavesEntered = 0;
                ICollector collector = Collector.NewAnonymous(
                    setScorer: _ => { },
                    collect: _ => { },
                    setNextReader: _ =>
                    {
                        // Trigger cancellation on the first leaf; the next leaf should
                        // observe the cancellation at the loop's entry check.
                        Interlocked.Increment(ref leavesEntered);
                        cts.Cancel();
                    },
                    acceptsDocsOutOfOrder: () => true);

                Assert.Throws<OperationCanceledException>(
                    () => searcher.Search(new MatchAllDocsQuery(), collector, cts.Token));

                // Only the first leaf should have been entered; subsequent leaves must have
                // been skipped due to the cancellation check.
                Assert.AreEqual(1, leavesEntered);
            }
            finally
            {
                r.Dispose();
            }
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestCancellation_SingleThreaded_DefaultToken_SearchCompletesNormally()
        {
            // Sanity check: a default (non-cancellable) token must not affect a normal search.
            IndexSearcher searcher = new IndexSearcher(reader);
            TopDocs docs = searcher.Search(new MatchAllDocsQuery(), 10, CancellationToken.None);
            Assert.AreEqual(100, docs.TotalHits);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestCancellation_MultiThreaded_PreCanceledTokenThrows()
        {
            // When the executor is non-null, cancellation is propagated to the submitted
            // tasks. The awaiting call in ExecutionHelper.MoveNext catches any Wait
            // exception and re-throws it wrapped via RuntimeException.Create, so we
            // assert on the wrapped exception having a cancellation-derived inner.
            TaskScheduler service = new LimitedConcurrencyLevelTaskScheduler(4);

            using Directory directory = NewDirectory();
            IndexReader r = BuildMultiSegmentReader(directory);
            try
            {
                IndexSearcher searcher = new IndexSearcher(r, service);
                Query query = new MatchAllDocsQuery();

                using CancellationTokenSource cts = new CancellationTokenSource();
                cts.Cancel();

                Exception ex = Assert.Catch(() => searcher.Search(query, 10, cts.Token));
                AssertCancellationInChain(ex);

                ex = Assert.Catch(() => searcher.SearchAfter(after: null, query, 10, cts.Token));
                AssertCancellationInChain(ex);

                Sort sort = new Sort(new SortField("field", SortFieldType.STRING));
                ex = Assert.Catch(() => searcher.Search(query, 10, sort, cts.Token));
                AssertCancellationInChain(ex);

                ex = Assert.Catch(() => searcher.Search(query, filter: null, 10, sort, doDocScores: true, doMaxScore: true, cts.Token));
                AssertCancellationInChain(ex);

                ex = Assert.Catch(() => searcher.SearchAfter(after: null, query, filter: null, 10, sort, cts.Token));
                AssertCancellationInChain(ex);
            }
            finally
            {
                r.Dispose();
            }
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestCancellation_MultiThreaded_CancelDuringSearch_Throws()
        {
            // Verify that cancellation requested while tasks are in-flight causes the
            // multi-threaded search to observe the cancellation. We override the leaf-level
            // Search to cancel the token after entering the first leaf, which is deterministic
            // regardless of scheduling.
            using Directory directory = NewDirectory();
            IndexReader r = BuildMultiSegmentReader(directory);
            try
            {
                Assume.That(r.Leaves.Count >= 2, "Test requires a multi-segment index");

                using CancellationTokenSource cts = new CancellationTokenSource();
                TaskScheduler service = new LimitedConcurrencyLevelTaskScheduler(4);
                IndexSearcher searcher = new CancelAfterFirstLeafSearcher(r, service, cts);

                Exception ex = Assert.Catch(() => searcher.Search(new MatchAllDocsQuery(), 10, cts.Token));
                AssertCancellationInChain(ex);
            }
            finally
            {
                r.Dispose();
            }
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestCancellation_MultiThreaded_DefaultToken_SearchCompletesNormally()
        {
            // Sanity check: a default (non-cancellable) token must not affect a normal
            // multi-threaded search.
            TaskScheduler service = new LimitedConcurrencyLevelTaskScheduler(4);

            using Directory directory = NewDirectory();
            IndexReader r = BuildMultiSegmentReader(directory);
            try
            {
                IndexSearcher searcher = new IndexSearcher(r, service);
                TopDocs docs = searcher.Search(new MatchAllDocsQuery(), 10, CancellationToken.None);
                Assert.AreEqual(50, docs.TotalHits);
            }
            finally
            {
                r.Dispose();
            }
        }

        /// <summary>
        /// Walks the exception chain (exception + inner exceptions, including the inner
        /// exceptions of any <see cref="AggregateException"/> encountered) looking for an
        /// <see cref="OperationCanceledException"/>. The multi-threaded search path wraps
        /// cancellation exceptions via <c>RuntimeException.Create</c>, so the cancellation
        /// is not the top-level exception but should always be present in the chain.
        /// </summary>
        private static void AssertCancellationInChain(Exception ex)
        {
            Assert.IsNotNull(ex, "Expected an exception, but none was thrown.");
            Exception current = ex;
            while (current != null)
            {
                if (current is OperationCanceledException)
                {
                    return;
                }
                if (current is AggregateException agg)
                {
                    foreach (Exception inner in agg.InnerExceptions)
                    {
                        if (ContainsOperationCanceled(inner))
                        {
                            return;
                        }
                    }
                }
                current = current.InnerException;
            }
            Assert.Fail("Expected OperationCanceledException in the exception chain, but got: " + ex);
        }

        private static bool ContainsOperationCanceled(Exception ex)
        {
            Exception current = ex;
            while (current != null)
            {
                if (current is OperationCanceledException)
                {
                    return true;
                }
                if (current is AggregateException agg)
                {
                    foreach (Exception inner in agg.InnerExceptions)
                    {
                        if (ContainsOperationCanceled(inner))
                        {
                            return true;
                        }
                    }
                }
                current = current.InnerException;
            }
            return false;
        }

        /// <summary>
        /// An <see cref="IndexSearcher"/> subclass that cancels the given
        /// <see cref="CancellationTokenSource"/> after the first leaf context is entered
        /// in the leaf-level Search method. This makes cancellation during multi-threaded
        /// search deterministic — the first slice triggers cancellation, and subsequent
        /// slices (or leaves within the same slice) observe it.
        /// </summary>
        private sealed class CancelAfterFirstLeafSearcher : IndexSearcher
        {
            private readonly CancellationTokenSource cts;
            private int leafEntered; // 0 = not yet, 1 = already canceled

            public CancelAfterFirstLeafSearcher(IndexReader r, TaskScheduler executor, CancellationTokenSource cts)
                : base(r, executor)
            {
                this.cts = cts;
            }

            protected override void Search(IList<AtomicReaderContext> leaves, Weight weight, ICollector collector, CancellationToken cancellationToken = default)
            {
                // Cancel after the very first leaf has been entered across all slices.
                if (Interlocked.Exchange(ref leafEntered, 1) == 0)
                {
                    cts.Cancel();
                }
                base.Search(leaves, weight, collector, cancellationToken);
            }
        }
    }
}
