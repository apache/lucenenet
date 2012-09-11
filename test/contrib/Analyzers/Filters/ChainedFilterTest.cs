/**
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Analysis;
using Lucene.Net.Util;

using NUnit.Framework;

namespace Lucene.Net.Analysis
{
    public class ChainedFilterTest : Lucene.Net.TestCase
    {
        public static int MAX = 500;

        private RAMDirectory directory;
        private IndexSearcher searcher;
        private Query query;
        // private DateFilter dateFilter;   DateFilter was deprecated and removed
        private TermRangeFilter dateFilter;
        private QueryWrapperFilter bobFilter;
        private QueryWrapperFilter sueFilter;

        [SetUp]
        public void SetUp()
        {
            directory = new RAMDirectory();
            IndexWriter writer =
               new IndexWriter(directory, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);

            DateTime cal = new DateTime(1041397200000L * TimeSpan.TicksPerMillisecond); // 2003 January 01

            for (int i = 0; i < MAX; i++)
            {
                Document doc = new Document();
                doc.Add(new Field("key", "" + (i + 1), Field.Store.YES, Field.Index.NOT_ANALYZED));
                doc.Add(new Field("owner", (i < MAX / 2) ? "bob" : "sue", Field.Store.YES, Field.Index.NOT_ANALYZED));
                doc.Add(new Field("date", (cal.Ticks / TimeSpan.TicksPerMillisecond).ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                writer.AddDocument(doc);

                cal.AddMilliseconds(1);
            }

            writer.Close();

            searcher = new IndexSearcher(directory, true);

            // query for everything to make life easier
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term("owner", "bob")), BooleanClause.Occur.SHOULD);
            bq.Add(new TermQuery(new Term("owner", "sue")), BooleanClause.Occur.SHOULD);
            query = bq;

            // date filter matches everything too
            //Date pastTheEnd = parseDate("2099 Jan 1");
            // dateFilter = DateFilter.Before("date", pastTheEnd);
            // just treat dates as strings and select the whole range for now...
            dateFilter = new TermRangeFilter("date", "", "ZZZZ", true, true);

            bobFilter = new QueryWrapperFilter(
                new TermQuery(new Term("owner", "bob")));
            sueFilter = new QueryWrapperFilter(
                new TermQuery(new Term("owner", "sue")));
        }

        private ChainedFilter GetChainedFilter(Filter[] chain, ChainedFilter.Logic[] logic)
        {
            if (logic == null)
            {
                return new ChainedFilter(chain);
            }
            else
            {
                return new ChainedFilter(chain, logic);
            }
        }

        private ChainedFilter GetChainedFilter(Filter[] chain, ChainedFilter.Logic logic)
        {
            return new ChainedFilter(chain, logic);
        }


        [Test]
        public void TestSingleFilter()
        {
            ChainedFilter chain = GetChainedFilter(new Filter[] { dateFilter }, null);

            int numHits = searcher.Search(query, chain, 1000).TotalHits;
            Assert.AreEqual(MAX, numHits);

            chain = new ChainedFilter(new Filter[] { bobFilter });
            numHits = searcher.Search(query, chain, 1000).TotalHits;
            Assert.AreEqual(MAX / 2, numHits);

            chain = GetChainedFilter(new Filter[] { bobFilter }, new ChainedFilter.Logic[] { ChainedFilter.Logic.AND });
            TopDocs hits = searcher.Search(query, chain, 1000);
            numHits = hits.TotalHits;
            Assert.AreEqual(MAX / 2, numHits);
            Assert.AreEqual("bob", searcher.Doc(hits.ScoreDocs[0].doc).Get("owner"));

            chain = GetChainedFilter(new Filter[] { bobFilter }, new ChainedFilter.Logic[] { ChainedFilter.Logic.ANDNOT });
            hits = searcher.Search(query, chain, 1000);
            numHits = hits.TotalHits;
            Assert.AreEqual(MAX / 2, numHits);
            Assert.AreEqual("sue", searcher.Doc(hits.ScoreDocs[0].doc).Get("owner"));
        }

        [Test]
        public void TestOR()
        {
            ChainedFilter chain = GetChainedFilter(
              new Filter[] { sueFilter, bobFilter }, null);

            int numHits = searcher.Search(query, chain, 1000).TotalHits;
            Assert.AreEqual(MAX, numHits, "OR matches all");
        }

        [Test]
        public void TestAND()
        {
            ChainedFilter chain = GetChainedFilter(
              new Filter[] { dateFilter, bobFilter }, ChainedFilter.Logic.AND);

            TopDocs hits = searcher.Search(query, chain, 1000);
            Assert.AreEqual(MAX / 2, hits.TotalHits, "AND matches just bob");
            Assert.AreEqual("bob", searcher.Doc(hits.ScoreDocs[0].doc).Get("owner"));
        }

        [Test]
        public void TestXOR()
        {
            ChainedFilter chain = GetChainedFilter(
              new Filter[] { dateFilter, bobFilter }, ChainedFilter.Logic.XOR);

            TopDocs hits = searcher.Search(query, chain, 1000);
            Assert.AreEqual(MAX / 2, hits.TotalHits, "XOR matches sue");
            Assert.AreEqual("sue", searcher.Doc(hits.ScoreDocs[0].doc).Get("owner"));
        }

        [Test]
        public void TestANDNOT()
        {
            ChainedFilter chain = GetChainedFilter(
              new Filter[] { dateFilter, sueFilter },
                new ChainedFilter.Logic[] { ChainedFilter.Logic.AND, ChainedFilter.Logic.ANDNOT });

            TopDocs hits = searcher.Search(query, chain, 1000);
            Assert.AreEqual(MAX / 2, hits.TotalHits, "ANDNOT matches just bob");
            Assert.AreEqual("bob", searcher.Doc(hits.ScoreDocs[0].doc).Get("owner"));

            chain = GetChainedFilter(
                new Filter[] { bobFilter, bobFilter },
                  new ChainedFilter.Logic[] { ChainedFilter.Logic.ANDNOT, ChainedFilter.Logic.ANDNOT });

            hits = searcher.Search(query, chain, 1000);
            Assert.AreEqual(MAX / 2, hits.TotalHits, "ANDNOT bob ANDNOT bob matches all sues");
            Assert.AreEqual("sue", searcher.Doc(hits.ScoreDocs[0].doc).Get("owner"));
        }

        /*
        private Date parseDate(String s) throws ParseException {
          return new SimpleDateFormat("yyyy MMM dd", Locale.US).parse(s);
        }
        */

        [Test]
        public void TestWithCachingFilter()
        {
            Directory dir = new RAMDirectory();
            Analyzer analyzer = new WhitespaceAnalyzer();

            IndexWriter writer = new IndexWriter(dir, analyzer, true, IndexWriter.MaxFieldLength.LIMITED);
            writer.Close();

            Searcher searcher = new IndexSearcher(dir, true);

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
        }

    }
}