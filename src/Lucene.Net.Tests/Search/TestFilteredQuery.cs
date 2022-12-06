using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using BitSet = J2N.Collections.BitSet;

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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdBitSet = Lucene.Net.Util.DocIdBitSet;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using Document = Documents.Document;
    using Field = Field;
    using FilterStrategy = Lucene.Net.Search.FilteredQuery.FilterStrategy;
    using IBits = Lucene.Net.Util.IBits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// FilteredQuery JUnit tests.
    ///
    /// <p>Created: Apr 21, 2004 1:21:46 PM
    ///
    ///
    /// @since   1.4
    /// </summary>
    [TestFixture]
    public class TestFilteredQuery : LuceneTestCase
    {
        private IndexSearcher searcher;
        private IndexReader reader;
        private Directory directory;
        private Query query;
        private Filter filter;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));

            Document doc = new Document();
            doc.Add(NewTextField("field", "one two three four five", Field.Store.YES));
            doc.Add(NewTextField("sorter", "b", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(NewTextField("field", "one two three four", Field.Store.YES));
            doc.Add(NewTextField("sorter", "d", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(NewTextField("field", "one two three y", Field.Store.YES));
            doc.Add(NewTextField("sorter", "a", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(NewTextField("field", "one two x", Field.Store.YES));
            doc.Add(NewTextField("sorter", "c", Field.Store.YES));
            writer.AddDocument(doc);

            // tests here require single segment (eg try seed
            // 8239472272678419952L), because SingleDocTestFilter(x)
            // blindly accepts that docID in any sub-segment
            writer.ForceMerge(1);

            reader = writer.GetReader();
            writer.Dispose();

            searcher = NewSearcher(reader);

            query = new TermQuery(new Term("field", "three"));
            filter = NewStaticFilterB();
        }

        // must be static for serialization tests
        private static Filter NewStaticFilterB()
        {
            return new FilterAnonymousClass();
        }

        private sealed class FilterAnonymousClass : Filter
        {
            public FilterAnonymousClass()
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                if (acceptDocs is null)
                {
                    acceptDocs = new Bits.MatchAllBits(5);
                }
                BitSet bitset = new BitSet(5);
                if (acceptDocs.Get(1))
                {
                    bitset.Set(1);
                }
                if (acceptDocs.Get(3))
                {
                    bitset.Set(3);
                }
                return new DocIdBitSet(bitset);
            }
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestFilteredQuery_Mem()
        {
            // force the filter to be executed as bits
            TFilteredQuery(true);
            // force the filter to be executed as iterator
            TFilteredQuery(false);
        }

        private void TFilteredQuery(bool useRandomAccess)
        {
            Query filteredquery = new FilteredQuery(query, filter, RandomFilterStrategy(Random, useRandomAccess));
            ScoreDoc[] hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual(1, hits[0].Doc);
            QueryUtils.Check(Random, filteredquery, searcher);

            hits = searcher.Search(filteredquery, null, 1000, new Sort(new SortField("sorter", SortFieldType.STRING))).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual(1, hits[0].Doc);

            filteredquery = new FilteredQuery(new TermQuery(new Term("field", "one")), filter, RandomFilterStrategy(Random, useRandomAccess));
            hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random, filteredquery, searcher);

            filteredquery = new FilteredQuery(new MatchAllDocsQuery(), filter, RandomFilterStrategy(Random, useRandomAccess));
            hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random, filteredquery, searcher);

            filteredquery = new FilteredQuery(new TermQuery(new Term("field", "x")), filter, RandomFilterStrategy(Random, useRandomAccess));
            hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual(3, hits[0].Doc);
            QueryUtils.Check(Random, filteredquery, searcher);

            filteredquery = new FilteredQuery(new TermQuery(new Term("field", "y")), filter, RandomFilterStrategy(Random, useRandomAccess));
            hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);
            QueryUtils.Check(Random, filteredquery, searcher);

            // test boost
            Filter f = NewStaticFilterA();

            float boost = 2.5f;
            BooleanQuery bq1 = new BooleanQuery();
            TermQuery tq = new TermQuery(new Term("field", "one"));
            tq.Boost = boost;
            bq1.Add(tq, Occur.MUST);
            bq1.Add(new TermQuery(new Term("field", "five")), Occur.MUST);

            BooleanQuery bq2 = new BooleanQuery();
            tq = new TermQuery(new Term("field", "one"));
            filteredquery = new FilteredQuery(tq, f, RandomFilterStrategy(Random, useRandomAccess));
            filteredquery.Boost = boost;
            bq2.Add(filteredquery, Occur.MUST);
            bq2.Add(new TermQuery(new Term("field", "five")), Occur.MUST);
            AssertScoreEquals(bq1, bq2);

            Assert.AreEqual(boost, filteredquery.Boost, 0);
            Assert.AreEqual(1.0f, tq.Boost, 0); // the boost value of the underlying query shouldn't have changed
        }

        // must be static for serialization tests
        private static Filter NewStaticFilterA()
        {
            return new FilterAnonymousClass2();
        }

        private sealed class FilterAnonymousClass2 : Filter
        {
            public FilterAnonymousClass2()
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                Assert.IsNull(acceptDocs, "acceptDocs should be null, as we have an index without deletions");
                BitSet bitset = new BitSet(5);
                bitset.Set(0, 5);
                return new DocIdBitSet(bitset);
            }
        }

        /// <summary>
        /// Tests whether the scores of the two queries are the same.
        /// </summary>
        public virtual void AssertScoreEquals(Query q1, Query q2)
        {
            ScoreDoc[] hits1 = searcher.Search(q1, null, 1000).ScoreDocs;
            ScoreDoc[] hits2 = searcher.Search(q2, null, 1000).ScoreDocs;

            Assert.AreEqual(hits1.Length, hits2.Length);

            for (int i = 0; i < hits1.Length; i++)
            {
                Assert.AreEqual(hits1[i].Score, hits2[i].Score, 0.000001f);
            }
        }

        /// <summary>
        /// this tests FilteredQuery's rewrite correctness
        /// </summary>
        [Test]
        public virtual void TestRangeQuery()
        {
            // force the filter to be executed as bits
            TRangeQuery(true);
            TRangeQuery(false);
        }

        private void TRangeQuery(bool useRandomAccess)
        {
            TermRangeQuery rq = TermRangeQuery.NewStringRange("sorter", "b", "d", true, true);

            Query filteredquery = new FilteredQuery(rq, filter, RandomFilterStrategy(Random, useRandomAccess));
            ScoreDoc[] hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random, filteredquery, searcher);
        }

        [Test]
        public virtual void TestBooleanMUST()
        {
            // force the filter to be executed as bits
            TBooleanMUST(true);
            // force the filter to be executed as iterator
            TBooleanMUST(false);
        }

        private void TBooleanMUST(bool useRandomAccess)
        {
            BooleanQuery bq = new BooleanQuery();
            Query query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(0), RandomFilterStrategy(Random, useRandomAccess));
            bq.Add(query, Occur.MUST);
            query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(1), RandomFilterStrategy(Random, useRandomAccess));
            bq.Add(query, Occur.MUST);
            ScoreDoc[] hits = searcher.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);
            QueryUtils.Check(Random, query, searcher);
        }

        [Test]
        public virtual void TestBooleanSHOULD()
        {
            // force the filter to be executed as bits
            TBooleanSHOULD(true);
            // force the filter to be executed as iterator
            TBooleanSHOULD(false);
        }

        private void TBooleanSHOULD(bool useRandomAccess)
        {
            BooleanQuery bq = new BooleanQuery();
            Query query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(0), RandomFilterStrategy(Random, useRandomAccess));
            bq.Add(query, Occur.SHOULD);
            query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(1), RandomFilterStrategy(Random, useRandomAccess));
            bq.Add(query, Occur.SHOULD);
            ScoreDoc[] hits = searcher.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random, query, searcher);
        }

        // Make sure BooleanQuery, which does out-of-order
        // scoring, inside FilteredQuery, works
        [Test]
        public virtual void TestBoolean2()
        {
            // force the filter to be executed as bits
            TBoolean2(true);
            // force the filter to be executed as iterator
            TBoolean2(false);
        }

        private void TBoolean2(bool useRandomAccess)
        {
            BooleanQuery bq = new BooleanQuery();
            Query query = new FilteredQuery(bq, new SingleDocTestFilter(0), RandomFilterStrategy(Random, useRandomAccess));
            bq.Add(new TermQuery(new Term("field", "one")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "two")), Occur.SHOULD);
            ScoreDoc[] hits = searcher.Search(query, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            QueryUtils.Check(Random, query, searcher);
        }

        [Test]
        public virtual void TestChainedFilters()
        {
            // force the filter to be executed as bits
            TChainedFilters(true);
            // force the filter to be executed as iterator
            TChainedFilters(false);
        }

        private void TChainedFilters(bool useRandomAccess)
        {
            Query query = new FilteredQuery(new FilteredQuery(new MatchAllDocsQuery(), new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "three")))), RandomFilterStrategy(Random, useRandomAccess)), new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "four")))), RandomFilterStrategy(Random, useRandomAccess));
            ScoreDoc[] hits = searcher.Search(query, 10).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random, query, searcher);

            // one more:
            query = new FilteredQuery(query, new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "five")))), RandomFilterStrategy(Random, useRandomAccess));
            hits = searcher.Search(query, 10).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            QueryUtils.Check(Random, query, searcher);
        }

        [Test]
        public virtual void TestEqualsHashcode()
        {
            // some tests before, if the used queries and filters work:
            Assert.AreEqual(new PrefixFilter(new Term("field", "o")), new PrefixFilter(new Term("field", "o")));
            Assert.IsFalse((new PrefixFilter(new Term("field", "a"))).Equals(new PrefixFilter(new Term("field", "o"))));
            QueryUtils.CheckHashEquals(new TermQuery(new Term("field", "one")));
            QueryUtils.CheckUnequal(new TermQuery(new Term("field", "one")), new TermQuery(new Term("field", "two"))
           );
            // now test FilteredQuery equals/hashcode:
            QueryUtils.CheckHashEquals(new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o"))));
            QueryUtils.CheckUnequal(new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o"))), new FilteredQuery(new TermQuery(new Term("field", "two")), new PrefixFilter(new Term("field", "o")))
           );
            QueryUtils.CheckUnequal(new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "a"))), new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o")))
           );
        }

        [Test]
        public virtual void TestInvalidArguments()
        {
            try
            {
                new FilteredQuery(null, null);
                Assert.Fail("Should throw IllegalArgumentException");
            }
            catch (ArgumentNullException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            {
                // pass
            }
            try
            {
                new FilteredQuery(new TermQuery(new Term("field", "one")), null);
                Assert.Fail("Should throw IllegalArgumentException");
            }
            catch (ArgumentNullException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            {
                // pass
            }
            try
            {
                new FilteredQuery(null, new PrefixFilter(new Term("field", "o")));
                Assert.Fail("Should throw IllegalArgumentException");
            }
            catch (ArgumentNullException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            {
                // pass
            }
        }

        private FilterStrategy RandomFilterStrategy()
        {
            return RandomFilterStrategy(Random, true);
        }

        private void AssertRewrite(FilteredQuery fq, Type clazz)
        {
            // assign crazy boost to FQ
            float boost = (float)Random.NextDouble() * 100.0f;
            fq.Boost = boost;

            // assign crazy boost to inner
            float innerBoost = (float)Random.NextDouble() * 100.0f;
            fq.Query.Boost = innerBoost;

            // check the class and boosts of rewritten query
            Query rewritten = searcher.Rewrite(fq);
            Assert.IsTrue(clazz.IsInstanceOfType(rewritten), "is not instance of " + clazz.Name);
            if (rewritten is FilteredQuery)
            {
                Assert.AreEqual(boost, rewritten.Boost, 1E-5f);
                Assert.AreEqual(innerBoost, ((FilteredQuery)rewritten).Query.Boost, 1E-5f);
                Assert.AreEqual(fq.Strategy, ((FilteredQuery)rewritten).Strategy);
            }
            else
            {
                Assert.AreEqual(boost * innerBoost, rewritten.Boost, 1E-5f);
            }

            // check that the original query was not modified
            Assert.AreEqual(boost, fq.Boost, 1E-5f);
            Assert.AreEqual(innerBoost, fq.Query.Boost, 1E-5f);
        }

        [Test]
        public virtual void TestRewrite()
        {
            AssertRewrite(new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o")), RandomFilterStrategy()), typeof(FilteredQuery));
            AssertRewrite(new FilteredQuery(new PrefixQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o")), RandomFilterStrategy()), typeof(FilteredQuery));
        }

        [Test]
        public virtual void TestGetFilterStrategy()
        {
            FilterStrategy randomFilterStrategy = RandomFilterStrategy();
            FilteredQuery filteredQuery = new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o")), randomFilterStrategy);
            Assert.AreSame(randomFilterStrategy, filteredQuery.Strategy);
        }

        private static FilteredQuery.FilterStrategy RandomFilterStrategy(Random random, bool useRandomAccess)
        {
            if (useRandomAccess)
            {
                return new RandomAccessFilterStrategyAnonymousClass();
            }
            return TestUtil.RandomFilterStrategy(random);
        }

        private sealed class RandomAccessFilterStrategyAnonymousClass : FilteredQuery.RandomAccessFilterStrategy
        {
            public RandomAccessFilterStrategyAnonymousClass()
            {
            }

            protected override bool UseRandomAccess(IBits bits, int firstFilterDoc)
            {
                return true;
            }
        }

        /*
         * Test if the QueryFirst strategy calls the bits only if the document has
         * been matched by the query and not otherwise
         */

        [Test]
        public virtual void TestQueryFirstFilterStrategy()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            int numDocs = AtLeast(50);
            int totalDocsWithZero = 0;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                int num = Random.Next(5);
                if (num == 0)
                {
                    totalDocsWithZero++;
                }
                doc.Add(NewTextField("field", "" + num, Field.Store.YES));
                writer.AddDocument(doc);
            }
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);
            Query query = new FilteredQuery(new TermQuery(new Term("field", "0")), new FilterAnonymousClass3(this, reader), FilteredQuery.QUERY_FIRST_FILTER_STRATEGY);

            TopDocs search = searcher.Search(query, 10);
            Assert.AreEqual(totalDocsWithZero, search.TotalHits);
            IOUtils.Dispose(reader, writer, directory);
        }

        private sealed class FilterAnonymousClass3 : Filter
        {
            private readonly TestFilteredQuery outerInstance;

            private IndexReader reader;

            public FilterAnonymousClass3(TestFilteredQuery outerInstance, IndexReader reader)
            {
                this.outerInstance = outerInstance;
                this.reader = reader;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                bool nullBitset = Random.Next(10) == 5;
                AtomicReader reader = context.AtomicReader;
                DocsEnum termDocsEnum = reader.GetTermDocsEnum(new Term("field", "0"));
                if (termDocsEnum is null)
                {
                    return null; // no docs -- return null
                }
                BitSet bitSet = new BitSet(reader.MaxDoc);
                int d;
                while ((d = termDocsEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                {
                    bitSet.Set(d);
                }
                return new DocIdSetAnonymousClass(this, nullBitset, reader, bitSet);
            }

            private sealed class DocIdSetAnonymousClass : DocIdSet
            {
                private readonly FilterAnonymousClass3 outerInstance;

                private readonly bool nullBitset;
                private readonly AtomicReader reader;
                private readonly BitSet bitSet;

                public DocIdSetAnonymousClass(FilterAnonymousClass3 outerInstance, bool nullBitset, AtomicReader reader, BitSet bitSet)
                {
                    this.outerInstance = outerInstance;
                    this.nullBitset = nullBitset;
                    this.reader = reader;
                    this.bitSet = bitSet;
                }

                public override IBits Bits
                {
                    get
                    {
                        if (nullBitset)
                        {
                            return null;
                        }
                        return new BitsAnonymousClass(this);
                    }
                }

                private sealed class BitsAnonymousClass : IBits
                {
                    private readonly DocIdSetAnonymousClass outerInstance;

                    public BitsAnonymousClass(DocIdSetAnonymousClass outerInstance)
                    {
                        this.outerInstance = outerInstance;
                    }

                    public bool Get(int index)
                    {
                        Assert.IsTrue(outerInstance.bitSet.Get(index), "filter was called for a non-matching doc");
                        return outerInstance.bitSet.Get(index);
                    }

                    public int Length => outerInstance.bitSet.Length;
                }

                public override DocIdSetIterator GetIterator()
                {
                    Assert.IsTrue(nullBitset, "iterator should not be called if bitset is present");
                    return reader.GetTermDocsEnum(new Term("field", "0"));
                }
            }
        }

        /*
         * Test if the leapfrog strategy works correctly in terms
         * of advancing / next the right thing first
         */

        [Test]
        public virtual void TestLeapFrogStrategy()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            int numDocs = AtLeast(50);
            int totalDocsWithZero = 0;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                int num = Random.Next(10);
                if (num == 0)
                {
                    totalDocsWithZero++;
                }
                doc.Add(NewTextField("field", "" + num, Field.Store.YES));
                writer.AddDocument(doc);
            }
            IndexReader reader = writer.GetReader();
            writer.Dispose();
            bool queryFirst = Random.NextBoolean();
            IndexSearcher searcher = NewSearcher(reader);
            Query query = new FilteredQuery(new TermQuery(new Term("field", "0")), new FilterAnonymousClass4(this, queryFirst), queryFirst ? FilteredQuery.LEAP_FROG_QUERY_FIRST_STRATEGY : Random
                  .NextBoolean() ? FilteredQuery.RANDOM_ACCESS_FILTER_STRATEGY : FilteredQuery.LEAP_FROG_FILTER_FIRST_STRATEGY); // if filterFirst, we can use random here since bits are null

            TopDocs search = searcher.Search(query, 10);
            Assert.AreEqual(totalDocsWithZero, search.TotalHits);
            IOUtils.Dispose(reader, writer, directory);
        }

        private sealed class FilterAnonymousClass4 : Filter
        {
            private readonly TestFilteredQuery outerInstance;

            private readonly bool queryFirst;

            public FilterAnonymousClass4(TestFilteredQuery outerInstance, bool queryFirst)
            {
                this.outerInstance = outerInstance;
                this.queryFirst = queryFirst;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return new DocIdSetAnonymousClass2(this, context);
            }

            private sealed class DocIdSetAnonymousClass2 : DocIdSet
            {
                private readonly FilterAnonymousClass4 outerInstance;

                private readonly AtomicReaderContext context;

                public DocIdSetAnonymousClass2(FilterAnonymousClass4 outerInstance, AtomicReaderContext context)
                {
                    this.outerInstance = outerInstance;
                    this.context = context;
                }

                public override IBits Bits => null;

                public override DocIdSetIterator GetIterator()
                {
                    DocsEnum termDocsEnum = ((AtomicReader)context.Reader).GetTermDocsEnum(new Term("field", "0"));
                    if (termDocsEnum is null)
                    {
                        return null;
                    }
                    return new DocIdSetIteratorAnonymousClass(this, termDocsEnum);
                }

                private sealed class DocIdSetIteratorAnonymousClass : DocIdSetIterator
                {
                    private readonly DocIdSetAnonymousClass2 outerInstance;

                    private readonly DocsEnum termDocsEnum;

                    public DocIdSetIteratorAnonymousClass(DocIdSetAnonymousClass2 outerInstance, DocsEnum termDocsEnum)
                    {
                        this.outerInstance = outerInstance;
                        this.termDocsEnum = termDocsEnum;
                    }

                    internal bool nextCalled;
                    internal bool advanceCalled;

                    public override int NextDoc()
                    {
                        Assert.IsTrue(nextCalled || advanceCalled ^ !outerInstance.outerInstance.queryFirst, "queryFirst: " + outerInstance.outerInstance.queryFirst + " advanced: " + advanceCalled + " next: " + nextCalled);
                        nextCalled = true;
                        return termDocsEnum.NextDoc();
                    }

                    public override int DocID => termDocsEnum.DocID;

                    public override int Advance(int target)
                    {
                        Assert.IsTrue(advanceCalled || nextCalled ^ outerInstance.outerInstance.queryFirst, "queryFirst: " + outerInstance.outerInstance.queryFirst + " advanced: " + advanceCalled + " next: " + nextCalled);
                        advanceCalled = true;
                        return termDocsEnum.Advance(target);
                    }

                    public override long GetCost()
                    {
                        return termDocsEnum.GetCost();
                    }
                }
            }
        }
    }
}