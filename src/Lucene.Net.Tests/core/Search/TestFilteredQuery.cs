using Lucene.Net.Documents;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections;

namespace Lucene.Net.Search
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using System.Reflection;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdBitSet = Lucene.Net.Util.DocIdBitSet;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using Document = Documents.Document;
    using Field = Field;
    using FilterStrategy = Lucene.Net.Search.FilteredQuery.FilterStrategy;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IOUtils = Lucene.Net.Util.IOUtils;
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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Occur = Lucene.Net.Search.BooleanClause.Occur;
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
        private IndexSearcher Searcher;
        private IndexReader Reader;
        private Directory Directory;
        private Query Query;
        private Filter Filter;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));

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

            Reader = writer.Reader;
            writer.Dispose();

            Searcher = NewSearcher(Reader);

            Query = new TermQuery(new Term("field", "three"));
            Filter = NewStaticFilterB();
        }

        // must be static for serialization tests
        private static Filter NewStaticFilterB()
        {
            return new FilterAnonymousInnerClassHelper();
        }

        private class FilterAnonymousInnerClassHelper : Filter
        {
            public FilterAnonymousInnerClassHelper()
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                if (acceptDocs == null)
                {
                    acceptDocs = new Bits_MatchAllBits(5);
                }
                BitArray bitset = new BitArray(5);
                if (acceptDocs.Get(1))
                {
                    bitset.SafeSet(1, true);
                }
                if (acceptDocs.Get(3))
                {
                    bitset.SafeSet(3, true);
                }
                return new DocIdBitSet(bitset);
            }
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Directory.Dispose();
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
            Query filteredquery = new FilteredQuery(Query, Filter, RandomFilterStrategy(Random(), useRandomAccess));
            ScoreDoc[] hits = Searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual(1, hits[0].Doc);
            QueryUtils.Check(Random(), filteredquery, Searcher, Similarity);

            hits = Searcher.Search(filteredquery, null, 1000, new Sort(new SortField("sorter", SortField.Type_e.STRING))).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual(1, hits[0].Doc);

            filteredquery = new FilteredQuery(new TermQuery(new Term("field", "one")), Filter, RandomFilterStrategy(Random(), useRandomAccess));
            hits = Searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random(), filteredquery, Searcher, Similarity);

            filteredquery = new FilteredQuery(new MatchAllDocsQuery(), Filter, RandomFilterStrategy(Random(), useRandomAccess));
            hits = Searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random(), filteredquery, Searcher, Similarity);

            filteredquery = new FilteredQuery(new TermQuery(new Term("field", "x")), Filter, RandomFilterStrategy(Random(), useRandomAccess));
            hits = Searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual(3, hits[0].Doc);
            QueryUtils.Check(Random(), filteredquery, Searcher, Similarity);

            filteredquery = new FilteredQuery(new TermQuery(new Term("field", "y")), Filter, RandomFilterStrategy(Random(), useRandomAccess));
            hits = Searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);
            QueryUtils.Check(Random(), filteredquery, Searcher, Similarity);

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
            filteredquery = new FilteredQuery(tq, f, RandomFilterStrategy(Random(), useRandomAccess));
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
            return new FilterAnonymousInnerClassHelper2();
        }

        private class FilterAnonymousInnerClassHelper2 : Filter
        {
            public FilterAnonymousInnerClassHelper2()
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                Assert.IsNull(acceptDocs, "acceptDocs should be null, as we have an index without deletions");
                BitArray bitset = new BitArray(5, true);
                return new DocIdBitSet(bitset);
            }
        }

        /// <summary>
        /// Tests whether the scores of the two queries are the same.
        /// </summary>
        public virtual void AssertScoreEquals(Query q1, Query q2)
        {
            ScoreDoc[] hits1 = Searcher.Search(q1, null, 1000).ScoreDocs;
            ScoreDoc[] hits2 = Searcher.Search(q2, null, 1000).ScoreDocs;

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

            Query filteredquery = new FilteredQuery(rq, Filter, RandomFilterStrategy(Random(), useRandomAccess));
            ScoreDoc[] hits = Searcher.Search(filteredquery, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random(), filteredquery, Searcher, Similarity);
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
            Query query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(0), RandomFilterStrategy(Random(), useRandomAccess));
            bq.Add(query, BooleanClause.Occur.MUST);
            query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(1), RandomFilterStrategy(Random(), useRandomAccess));
            bq.Add(query, BooleanClause.Occur.MUST);
            ScoreDoc[] hits = Searcher.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);
            QueryUtils.Check(Random(), query, Searcher, Similarity);
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
            Query query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(0), RandomFilterStrategy(Random(), useRandomAccess));
            bq.Add(query, BooleanClause.Occur.SHOULD);
            query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(1), RandomFilterStrategy(Random(), useRandomAccess));
            bq.Add(query, BooleanClause.Occur.SHOULD);
            ScoreDoc[] hits = Searcher.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random(), query, Searcher, Similarity);
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
            Query query = new FilteredQuery(bq, new SingleDocTestFilter(0), RandomFilterStrategy(Random(), useRandomAccess));
            bq.Add(new TermQuery(new Term("field", "one")), BooleanClause.Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "two")), BooleanClause.Occur.SHOULD);
            ScoreDoc[] hits = Searcher.Search(query, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            QueryUtils.Check(Random(), query, Searcher, Similarity);
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
            Query query = new FilteredQuery(new FilteredQuery(new MatchAllDocsQuery(), new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "three")))), RandomFilterStrategy(Random(), useRandomAccess)), new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "four")))), RandomFilterStrategy(Random(), useRandomAccess));
            ScoreDoc[] hits = Searcher.Search(query, 10).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random(), query, Searcher, Similarity);

            // one more:
            query = new FilteredQuery(query, new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "five")))), RandomFilterStrategy(Random(), useRandomAccess));
            hits = Searcher.Search(query, 10).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            QueryUtils.Check(Random(), query, Searcher, Similarity);
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
            catch (System.ArgumentException iae)
            {
                // pass
            }
            try
            {
                new FilteredQuery(new TermQuery(new Term("field", "one")), null);
                Assert.Fail("Should throw IllegalArgumentException");
            }
            catch (System.ArgumentException iae)
            {
                // pass
            }
            try
            {
                new FilteredQuery(null, new PrefixFilter(new Term("field", "o")));
                Assert.Fail("Should throw IllegalArgumentException");
            }
            catch (System.ArgumentException iae)
            {
                // pass
            }
        }

        private FilterStrategy RandomFilterStrategy()
        {
            return RandomFilterStrategy(Random(), true);
        }

        private void AssertRewrite(FilteredQuery fq, Type clazz)
        {
            // assign crazy boost to FQ
            float boost = (float)Random().NextDouble() * 100.0f;
            fq.Boost = boost;

            // assign crazy boost to inner
            float innerBoost = (float)Random().NextDouble() * 100.0f;
            fq.Query.Boost = innerBoost;

            // check the class and boosts of rewritten query
            Query rewritten = Searcher.Rewrite(fq);
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
                return new RandomAccessFilterStrategyAnonymousInnerClassHelper();
            }
            return TestUtil.RandomFilterStrategy(random);
        }

        private class RandomAccessFilterStrategyAnonymousInnerClassHelper : FilteredQuery.RandomAccessFilterStrategy
        {
            public RandomAccessFilterStrategyAnonymousInnerClassHelper()
            {
            }

            protected internal override bool UseRandomAccess(Bits bits, int firstFilterDoc)
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
            RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            int numDocs = AtLeast(50);
            int totalDocsWithZero = 0;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                int num = Random().Next(5);
                if (num == 0)
                {
                    totalDocsWithZero++;
                }
                doc.Add(NewTextField("field", "" + num, Field.Store.YES));
                writer.AddDocument(doc);
            }
            IndexReader reader = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);
            Query query = new FilteredQuery(new TermQuery(new Term("field", "0")), new FilterAnonymousInnerClassHelper3(this, reader), FilteredQuery.QUERY_FIRST_FILTER_STRATEGY);

            TopDocs search = searcher.Search(query, 10);
            Assert.AreEqual(totalDocsWithZero, search.TotalHits);
            IOUtils.Close(reader, writer, directory);
        }

        private class FilterAnonymousInnerClassHelper3 : Filter
        {
            private readonly TestFilteredQuery OuterInstance;

            private IndexReader Reader;

            public FilterAnonymousInnerClassHelper3(TestFilteredQuery outerInstance, IndexReader reader)
            {
                this.OuterInstance = outerInstance;
                this.Reader = reader;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                bool nullBitset = Random().Next(10) == 5;
                AtomicReader reader = context.AtomicReader;
                DocsEnum termDocsEnum = reader.TermDocsEnum(new Term("field", "0"));
                if (termDocsEnum == null)
                {
                    return null; // no docs -- return null
                }
                BitArray bitSet = new BitArray(reader.MaxDoc);
                int d;
                while ((d = termDocsEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                {
                    bitSet.SafeSet(d, true);
                }
                return new DocIdSetAnonymousInnerClassHelper(this, nullBitset, reader, bitSet);
            }

            private class DocIdSetAnonymousInnerClassHelper : DocIdSet
            {
                private readonly FilterAnonymousInnerClassHelper3 OuterInstance;

                private bool NullBitset;
                private AtomicReader Reader;
                private BitArray BitSet;

                public DocIdSetAnonymousInnerClassHelper(FilterAnonymousInnerClassHelper3 outerInstance, bool nullBitset, AtomicReader reader, BitArray bitSet)
                {
                    this.OuterInstance = outerInstance;
                    this.NullBitset = nullBitset;
                    this.Reader = reader;
                    this.BitSet = bitSet;
                }

                public override Bits GetBits()
                {
                    if (NullBitset)
                    {
                        return null;
                    }
                    return new BitsAnonymousInnerClassHelper(this);
                }

                private class BitsAnonymousInnerClassHelper : Bits
                {
                    private readonly DocIdSetAnonymousInnerClassHelper OuterInstance;

                    public BitsAnonymousInnerClassHelper(DocIdSetAnonymousInnerClassHelper outerInstance)
                    {
                        this.OuterInstance = outerInstance;
                    }

                    public bool Get(int index)
                    {
                        Assert.IsTrue(OuterInstance.BitSet.SafeGet(index), "filter was called for a non-matching doc");
                        return OuterInstance.BitSet.SafeGet(index);
                    }

                    public int Length()
                    {
                        return OuterInstance.BitSet.Length;
                    }
                }

                public override DocIdSetIterator GetIterator()
                {
                    Assert.IsTrue(NullBitset, "iterator should not be called if bitset is present");
                    return Reader.TermDocsEnum(new Term("field", "0"));
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
            RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            int numDocs = AtLeast(50);
            int totalDocsWithZero = 0;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                int num = Random().Next(10);
                if (num == 0)
                {
                    totalDocsWithZero++;
                }
                doc.Add(NewTextField("field", "" + num, Field.Store.YES));
                writer.AddDocument(doc);
            }
            IndexReader reader = writer.Reader;
            writer.Dispose();
            bool queryFirst = Random().NextBoolean();
            IndexSearcher searcher = NewSearcher(reader);
            Query query = new FilteredQuery(new TermQuery(new Term("field", "0")), new FilterAnonymousInnerClassHelper4(this, queryFirst), queryFirst ? FilteredQuery.LEAP_FROG_QUERY_FIRST_STRATEGY : Random()
                  .NextBoolean() ? FilteredQuery.RANDOM_ACCESS_FILTER_STRATEGY : FilteredQuery.LEAP_FROG_FILTER_FIRST_STRATEGY); // if filterFirst, we can use random here since bits are null

            TopDocs search = searcher.Search(query, 10);
            Assert.AreEqual(totalDocsWithZero, search.TotalHits);
            IOUtils.Close(reader, writer, directory);
        }

        private class FilterAnonymousInnerClassHelper4 : Filter
        {
            private readonly TestFilteredQuery OuterInstance;

            private bool QueryFirst;

            public FilterAnonymousInnerClassHelper4(TestFilteredQuery outerInstance, bool queryFirst)
            {
                this.OuterInstance = outerInstance;
                this.QueryFirst = queryFirst;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                return new DocIdSetAnonymousInnerClassHelper2(this, context);
            }

            private class DocIdSetAnonymousInnerClassHelper2 : DocIdSet
            {
                private readonly FilterAnonymousInnerClassHelper4 OuterInstance;

                private AtomicReaderContext Context;

                public DocIdSetAnonymousInnerClassHelper2(FilterAnonymousInnerClassHelper4 outerInstance, AtomicReaderContext context)
                {
                    this.OuterInstance = outerInstance;
                    this.Context = context;
                }

                public override Bits GetBits()
                {
                    return null;
                }

                public override DocIdSetIterator GetIterator()
                {
                    DocsEnum termDocsEnum = ((AtomicReader)Context.Reader).TermDocsEnum(new Term("field", "0"));
                    if (termDocsEnum == null)
                    {
                        return null;
                    }
                    return new DocIdSetIteratorAnonymousInnerClassHelper(this, termDocsEnum);
                }

                private class DocIdSetIteratorAnonymousInnerClassHelper : DocIdSetIterator
                {
                    private readonly DocIdSetAnonymousInnerClassHelper2 OuterInstance;

                    private DocsEnum TermDocsEnum;

                    public DocIdSetIteratorAnonymousInnerClassHelper(DocIdSetAnonymousInnerClassHelper2 outerInstance, DocsEnum termDocsEnum)
                    {
                        this.OuterInstance = outerInstance;
                        this.TermDocsEnum = termDocsEnum;
                    }

                    internal bool nextCalled;
                    internal bool advanceCalled;

                    public override int NextDoc()
                    {
                        Assert.IsTrue(nextCalled || advanceCalled ^ !OuterInstance.OuterInstance.QueryFirst, "queryFirst: " + OuterInstance.OuterInstance.QueryFirst + " advanced: " + advanceCalled + " next: " + nextCalled);
                        nextCalled = true;
                        return TermDocsEnum.NextDoc();
                    }

                    public override int DocID()
                    {
                        return TermDocsEnum.DocID();
                    }

                    public override int Advance(int target)
                    {
                        Assert.IsTrue(advanceCalled || nextCalled ^ OuterInstance.OuterInstance.QueryFirst, "queryFirst: " + OuterInstance.OuterInstance.QueryFirst + " advanced: " + advanceCalled + " next: " + nextCalled);
                        advanceCalled = true;
                        return TermDocsEnum.Advance(target);
                    }

                    public override long Cost()
                    {
                        return TermDocsEnum.Cost();
                    }
                }
            }
        }
    }
}