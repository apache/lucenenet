using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

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
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    [TestFixture]
    public class TestQueryWrapperFilter : LuceneTestCase
    {
        [Test]
        public virtual void TestBasic()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewTextField("field", "value", Field.Store.NO));
            writer.AddDocument(doc);
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            TermQuery termQuery = new TermQuery(new Term("field", "value"));

            // should not throw exception with primitive query
            QueryWrapperFilter qwf = new QueryWrapperFilter(termQuery);

            IndexSearcher searcher = NewSearcher(reader);
            TopDocs hits = searcher.Search(new MatchAllDocsQuery(), qwf, 10);
            Assert.AreEqual(1, hits.TotalHits);
            hits = searcher.Search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10);
            Assert.AreEqual(1, hits.TotalHits);

            // should not throw exception with complex primitive query
            BooleanQuery booleanQuery = new BooleanQuery();
            booleanQuery.Add(termQuery, Occur.MUST);
            booleanQuery.Add(new TermQuery(new Term("field", "missing")), Occur.MUST_NOT);
            qwf = new QueryWrapperFilter(termQuery);

            hits = searcher.Search(new MatchAllDocsQuery(), qwf, 10);
            Assert.AreEqual(1, hits.TotalHits);
            hits = searcher.Search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10);
            Assert.AreEqual(1, hits.TotalHits);

            // should not throw exception with non primitive Query (doesn't implement
            // Query#createWeight)
            qwf = new QueryWrapperFilter(new FuzzyQuery(new Term("field", "valu")));

            hits = searcher.Search(new MatchAllDocsQuery(), qwf, 10);
            Assert.AreEqual(1, hits.TotalHits);
            hits = searcher.Search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10);
            Assert.AreEqual(1, hits.TotalHits);

            // test a query with no hits
            termQuery = new TermQuery(new Term("field", "not_exist"));
            qwf = new QueryWrapperFilter(termQuery);
            hits = searcher.Search(new MatchAllDocsQuery(), qwf, 10);
            Assert.AreEqual(0, hits.TotalHits);
            hits = searcher.Search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10);
            Assert.AreEqual(0, hits.TotalHits);
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestRandom()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            w.IndexWriter.Config.SetMaxBufferedDocs(17);
            int numDocs = AtLeast(100);
            ISet<string> aDocs = new JCG.HashSet<string>();
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                string v;
                if (Random.Next(5) == 4)
                {
                    v = "a";
                    aDocs.Add("" + i);
                }
                else
                {
                    v = "b";
                }
                Field f = NewStringField("field", v, Field.Store.NO);
                doc.Add(f);
                doc.Add(NewStringField("id", "" + i, Field.Store.YES));
                w.AddDocument(doc);
            }

            int numDelDocs = AtLeast(10);
            for (int i = 0; i < numDelDocs; i++)
            {
                string delID = "" + Random.Next(numDocs);
                w.DeleteDocuments(new Term("id", delID));
                aDocs.Remove(delID);
            }

            IndexReader r = w.GetReader();
            w.Dispose();
            TopDocs hits = NewSearcher(r).Search(new MatchAllDocsQuery(), new QueryWrapperFilter(new TermQuery(new Term("field", "a"))), numDocs);
            Assert.AreEqual(aDocs.Count, hits.TotalHits);
            foreach (ScoreDoc sd in hits.ScoreDocs)
            {
                Assert.IsTrue(aDocs.Contains(r.Document(sd.Doc).Get("id")));
            }
            r.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestThousandDocuments()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            for (int i = 0; i < 1000; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("field", English.Int32ToEnglish(i), Field.Store.NO));
                writer.AddDocument(doc);
            }

            IndexReader reader = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);

            for (int i = 0; i < 1000; i++)
            {
                TermQuery termQuery = new TermQuery(new Term("field", English.Int32ToEnglish(i)));
                QueryWrapperFilter qwf = new QueryWrapperFilter(termQuery);
                TopDocs td = searcher.Search(new MatchAllDocsQuery(), qwf, 10);
                Assert.AreEqual(1, td.TotalHits);
            }

            reader.Dispose();
            dir.Dispose();
        }
    }
}