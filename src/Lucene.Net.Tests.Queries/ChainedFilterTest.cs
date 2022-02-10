// Lucene version compatibility level 4.8.1
using System;
using System.Globalization;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries
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

    public class ChainedFilterTest : LuceneTestCase
    {
        public const int Max = 500;

        private Directory directory;
        private IndexSearcher searcher;
        private IndexReader reader;
        private Query query;
        // private DateFilter dateFilter;   DateFilter was deprecated and removed
        private TermRangeFilter dateFilter;
        private QueryWrapperFilter bobFilter;
        private QueryWrapperFilter sueFilter;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            // we use the default Locale/TZ since LuceneTestCase randomizes it
            var cal = new GregorianCalendar().ToDateTime(2003, 1, 1, 0, 0, 0, 0); // 2003 January 01
            cal = TimeZoneInfo.ConvertTime(cal, TimeZoneInfo.Local);

            for (int i = 0; i < Max; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("key", "" + (i + 1), Field.Store.YES));
                doc.Add(NewStringField("owner", (i < Max / 2) ? "bob" : "sue", Field.Store.YES));
                doc.Add(NewStringField("date", cal.ToString(CultureInfo.InvariantCulture), Field.Store.YES));
                writer.AddDocument(doc);

                cal = cal.AddDays(1);
            }
            reader = writer.GetReader();
            writer.Dispose();

            searcher = NewSearcher(reader);

            // query for everything to make life easier
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term("owner", "bob")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term("owner", "sue")), Occur.SHOULD);
            query = bq;

            // date filter matches everything too
            //Date pastTheEnd = parseDate("2099 Jan 1");
            // dateFilter = DateFilter.Before("date", pastTheEnd);
            // just treat dates as strings and select the whole range for now...
            dateFilter = TermRangeFilter.NewStringRange("date", "", "ZZZZ", true, true);

            bobFilter = new QueryWrapperFilter(
                new TermQuery(new Term("owner", "bob")));
            sueFilter = new QueryWrapperFilter(
                new TermQuery(new Term("owner", "sue")));
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        private ChainedFilter GetChainedFilter(Filter[] chain, int[] logic)
        {
            if (logic is null)
            {
                return new ChainedFilter(chain);
            }
            return new ChainedFilter(chain, logic);
        }

        private ChainedFilter GetChainedFilter(Filter[] chain, int logic)
        {
            return new ChainedFilter(chain, logic);
        }

        
        [Test]
        public virtual void TestSingleFilter()
        {
            ChainedFilter chain = GetChainedFilter(new Filter[] { dateFilter }, null);

            int numHits = searcher.Search(query, chain, 1000).TotalHits;
            assertEquals(Max, numHits);

            chain = new ChainedFilter(new Filter[] { bobFilter });
            numHits = searcher.Search(query, chain, 1000).TotalHits;
            assertEquals(Max / 2, numHits);

            chain = GetChainedFilter(new Filter[] { bobFilter }, new[] { ChainedFilter.AND });
            TopDocs hits = searcher.Search(query, chain, 1000);
            numHits = hits.TotalHits;
            assertEquals(Max / 2, numHits);
            assertEquals("bob", searcher.Doc(hits.ScoreDocs[0].Doc).Get("owner"));

            chain = GetChainedFilter(new Filter[] { bobFilter }, new[] { ChainedFilter.ANDNOT });
            hits = searcher.Search(query, chain, 1000);
            numHits = hits.TotalHits;
            assertEquals(Max / 2, numHits);
            assertEquals("sue", searcher.Doc(hits.ScoreDocs[0].Doc).Get("owner"));
        }
        
        [Test]
        public virtual void TestOR()
        {
            ChainedFilter chain = GetChainedFilter(
                new Filter[] { sueFilter, bobFilter }, null);

            int numHits = searcher.Search(query, chain, 1000).TotalHits;
            assertEquals("OR matches all", Max, numHits);
        }
        
        [Test]
        public virtual void TestAND()
        {
            ChainedFilter chain = GetChainedFilter(
                new Filter[] { dateFilter, bobFilter }, ChainedFilter.AND);

            TopDocs hits = searcher.Search(query, chain, 1000);
            assertEquals("AND matches just bob", Max / 2, hits.TotalHits);
            assertEquals("bob", searcher.Doc(hits.ScoreDocs[0].Doc).Get("owner"));
        }
        
        [Test]
        public virtual void TestXOR()
        {
            ChainedFilter chain = GetChainedFilter(
                new Filter[] { dateFilter, bobFilter }, ChainedFilter.XOR);

            TopDocs hits = searcher.Search(query, chain, 1000);
            assertEquals("XOR matches sue", Max / 2, hits.TotalHits);
            assertEquals("sue", searcher.Doc(hits.ScoreDocs[0].Doc).Get("owner"));
        }

        [Test]
        public virtual void TestANDNOT()
        {
            ChainedFilter chain = GetChainedFilter(
                new Filter[] { dateFilter, sueFilter }, new int[] { ChainedFilter.AND, ChainedFilter.ANDNOT });

            TopDocs hits = searcher.Search(query, chain, 1000);
            assertEquals("ANDNOT matches just bob", Max / 2, hits.TotalHits);
            assertEquals("bob", searcher.Doc(hits.ScoreDocs[0].Doc).Get("owner"));

            chain = GetChainedFilter(new Filter[] { bobFilter, bobFilter }, new int[] { ChainedFilter.ANDNOT, ChainedFilter.ANDNOT });

            hits = searcher.Search(query, chain, 1000);
            assertEquals("ANDNOT bob ANDNOT bob matches all sues", Max / 2, hits.TotalHits);
            assertEquals("sue", searcher.Doc(hits.ScoreDocs[0].Doc).Get("owner"));
        }

        [Test]
        public virtual void TestWithCachingFilter()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);

            Query query = new TermQuery(new Term("none", "none"));

            QueryWrapperFilter queryFilter = new QueryWrapperFilter(query);
            CachingWrapperFilter cachingFilter = new CachingWrapperFilter(queryFilter);

            searcher.Search(query, cachingFilter, 1);

            CachingWrapperFilter cachingFilter2 = new CachingWrapperFilter(queryFilter);
            Filter[] chain = new Filter[2];
            chain[0] = cachingFilter;
            chain[1] = cachingFilter2;
            ChainedFilter cf = new ChainedFilter(chain);

            // throws java.lang.ClassCastException: org.apache.lucene.util.OpenBitSet cannot be cast to java.util.BitSet
            searcher.Search(new MatchAllDocsQuery(), cf, 1);
            reader.Dispose();
            dir.Dispose();
        }
    }
}
