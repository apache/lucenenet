using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using NUnit.Framework;

    /// <summary>
    /// Copyright 2005 The Apache Software Foundation
    ///
    /// Licensed under the Apache License, Version 2.0 (the "License");
    /// you may not use this file except in compliance with the License.
    /// You may obtain a copy of the License at
    ///
    ///     http://www.apache.org/licenses/LICENSE-2.0
    ///
    /// Unless required by applicable law or agreed to in writing, software
    /// distributed under the License is distributed on an "AS IS" BASIS,
    /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specific language governing permissions and
    /// limitations under the License.
    /// </summary>

    using DateTools = DateTools;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Unit test for sorting code. </summary>
    [TestFixture]
    public class TestCustomSearcherSort : LuceneTestCase
    {
        private Directory Index = null;
        private IndexReader Reader;
        private Query Query = null;

        // reduced from 20000 to 2000 to speed up test...
        private int INDEX_SIZE;

        /// <summary>
        /// Create index and query for test cases.
        /// </summary>
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            INDEX_SIZE = AtLeast(2000);
            Index = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Index, Similarity, TimeZone);
            RandomGen random = new RandomGen(this, Random());
            for (int i = 0; i < INDEX_SIZE; ++i) // don't decrease; if to low the
            {
                // problem doesn't show up
                Document doc = new Document();
                if ((i % 5) != 0) // some documents must not have an entry in the first
                {
                    // sort field
                    doc.Add(NewStringField("publicationDate_", random.LuceneDate, Field.Store.YES));
                }
                if ((i % 7) == 0) // some documents to match the query (see below)
                {
                    doc.Add(NewTextField("content", "test", Field.Store.YES));
                }
                // every document has a defined 'mandant' field
                doc.Add(NewStringField("mandant", Convert.ToString(i % 3), Field.Store.YES));
                writer.AddDocument(doc);
            }
            Reader = writer.Reader;
            writer.Dispose();
            Query = new TermQuery(new Term("content", "test"));
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Index.Dispose();
            base.TearDown();
        }

        /// <summary>
        /// Run the test using two CustomSearcher instances.
        /// </summary>
        [Test]
        public virtual void TestFieldSortCustomSearcher()
        {
            // log("Run testFieldSortCustomSearcher");
            // define the sort criteria
            Sort custSort = new Sort(new SortField("publicationDate_", SortField.Type_e.STRING), SortField.FIELD_SCORE);
            IndexSearcher searcher = new CustomSearcher(this, Reader, 2);
            // search and check hits
            MatchHits(searcher, custSort);
        }

        /// <summary>
        /// Run the test using one CustomSearcher wrapped by a MultiSearcher.
        /// </summary>
        [Test]
        public virtual void TestFieldSortSingleSearcher()
        {
            // log("Run testFieldSortSingleSearcher");
            // define the sort criteria
            Sort custSort = new Sort(new SortField("publicationDate_", SortField.Type_e.STRING), SortField.FIELD_SCORE);
            IndexSearcher searcher = new CustomSearcher(this, Reader, 2);
            // search and check hits
            MatchHits(searcher, custSort);
        }

        // make sure the documents returned by the search match the expected list
        private void MatchHits(IndexSearcher searcher, Sort sort)
        {
            // make a query without sorting first
            ScoreDoc[] hitsByRank = searcher.Search(Query, null, int.MaxValue).ScoreDocs;
            CheckHits(hitsByRank, "Sort by rank: "); // check for duplicates
            IDictionary<int?, int?> resultMap = new SortedDictionary<int?, int?>();
            // store hits in TreeMap - TreeMap does not allow duplicates; existing
            // entries are silently overwritten
            for (int hitid = 0; hitid < hitsByRank.Length; ++hitid)
            {
                resultMap[Convert.ToInt32(hitsByRank[hitid].Doc)] = Convert.ToInt32(hitid); // Value: Hits-Objekt Index -  Key: Lucene
                // Document ID
            }

            // now make a query using the sort criteria
            ScoreDoc[] resultSort = searcher.Search(Query, null, int.MaxValue, sort).ScoreDocs;
            CheckHits(resultSort, "Sort by custom criteria: "); // check for duplicates

            // besides the sorting both sets of hits must be identical
            for (int hitid = 0; hitid < resultSort.Length; ++hitid)
            {
                int? idHitDate = Convert.ToInt32(resultSort[hitid].Doc); // document ID
                // from sorted
                // search
                if (!resultMap.ContainsKey(idHitDate))
                {
                    Log("ID " + idHitDate + " not found. Possibliy a duplicate.");
                }
                Assert.IsTrue(resultMap.ContainsKey(idHitDate)); // same ID must be in the
                // Map from the rank-sorted
                // search
                // every hit must appear once in both result sets --> remove it from the
                // Map.
                // At the end the Map must be empty!
                resultMap.Remove(idHitDate);
            }
            if (resultMap.Count == 0)
            {
                // log("All hits matched");
            }
            else
            {
                Log("Couldn't match " + resultMap.Count + " hits.");
            }
            Assert.AreEqual(resultMap.Count, 0);
        }

        /// <summary>
        /// Check the hits for duplicates.
        /// </summary>
        private void CheckHits(ScoreDoc[] hits, string prefix)
        {
            if (hits != null)
            {
                IDictionary<int?, int?> idMap = new SortedDictionary<int?, int?>();
                for (int docnum = 0; docnum < hits.Length; ++docnum)
                {
                    int? luceneId = null;

                    luceneId = Convert.ToInt32(hits[docnum].Doc);
                    if (idMap.ContainsKey(luceneId))
                    {
                        StringBuilder message = new StringBuilder(prefix);
                        message.Append("Duplicate key for hit index = ");
                        message.Append(docnum);
                        message.Append(", previous index = ");
                        message.Append((idMap[luceneId]).ToString());
                        message.Append(", Lucene ID = ");
                        message.Append(luceneId);
                        Log(message.ToString());
                    }
                    else
                    {
                        idMap[luceneId] = Convert.ToInt32(docnum);
                    }
                }
            }
        }

        // Simply write to console - choosen to be independant of log4j etc
        private void Log(string message)
        {
            if (VERBOSE)
            {
                Console.WriteLine(message);
            }
        }

        public class CustomSearcher : IndexSearcher
        {
            private readonly TestCustomSearcherSort OuterInstance;

            internal int Switcher;

            public CustomSearcher(TestCustomSearcherSort outerInstance, IndexReader r, int switcher)
                : base(r)
            {
                this.OuterInstance = outerInstance;
                this.Switcher = switcher;
            }

            public override TopFieldDocs Search(Query query, Filter filter, int nDocs, Sort sort)
            {
                BooleanQuery bq = new BooleanQuery();
                bq.Add(query, BooleanClause.Occur.MUST);
                bq.Add(new TermQuery(new Term("mandant", Convert.ToString(Switcher))), BooleanClause.Occur.MUST);
                return base.Search(bq, filter, nDocs, sort);
            }

            public override TopDocs Search(Query query, Filter filter, int nDocs)
            {
                BooleanQuery bq = new BooleanQuery();
                bq.Add(query, BooleanClause.Occur.MUST);
                bq.Add(new TermQuery(new Term("mandant", Convert.ToString(Switcher))), BooleanClause.Occur.MUST);
                return base.Search(bq, filter, nDocs);
            }
        }

        private class RandomGen
        {
            private readonly TestCustomSearcherSort OuterInstance;

            internal RandomGen(TestCustomSearcherSort outerInstance, Random random)
            {
                this.OuterInstance = outerInstance;
                this.Random = random;
                @base = new DateTime(1980, 1, 1);
            }

            internal Random Random;

            // we use the default Locale/TZ since LuceneTestCase randomizes it
            internal DateTime @base;

            // Just to generate some different Lucene Date strings
            internal virtual string LuceneDate
            {
                get
                {
                    return DateTools.TimeToString((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + Random.Next() - int.MinValue, DateTools.Resolution.DAY);
                }
            }
        }
    }
}