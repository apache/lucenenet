using Lucene.Net.Documents;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

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
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NumericDocValuesField = NumericDocValuesField;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestSortRescorer : LuceneTestCase
    {
        internal IndexSearcher searcher;
        internal DirectoryReader reader;
        internal Directory dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            doc.Add(NewTextField("body", "some contents and more contents", Field.Store.NO));
            doc.Add(new NumericDocValuesField("popularity", 5));
            iw.AddDocument(doc);

            doc = new Document();
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            doc.Add(NewTextField("body", "another document with different contents", Field.Store.NO));
            doc.Add(new NumericDocValuesField("popularity", 20));
            iw.AddDocument(doc);

            doc = new Document();
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            doc.Add(NewTextField("body", "crappy contents", Field.Store.NO));
            doc.Add(new NumericDocValuesField("popularity", 2));
            iw.AddDocument(doc);

            reader = iw.GetReader();
            searcher = new IndexSearcher(reader);
            iw.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestBasic()
        {
            // create a sort field and sort by it (reverse order)
            Query query = new TermQuery(new Term("body", "contents"));
            IndexReader r = searcher.IndexReader;

            // Just first pass query
            TopDocs hits = searcher.Search(query, 10);
            Assert.AreEqual(3, hits.TotalHits);
            Assert.AreEqual("3", r.Document(hits.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("1", r.Document(hits.ScoreDocs[1].Doc).Get("id"));
            Assert.AreEqual("2", r.Document(hits.ScoreDocs[2].Doc).Get("id"));

            // Now, rescore:
            Sort sort = new Sort(new SortField("popularity", SortFieldType.INT32, true));
            Rescorer rescorer = new SortRescorer(sort);
            hits = rescorer.Rescore(searcher, hits, 10);
            Assert.AreEqual(3, hits.TotalHits);
            Assert.AreEqual("2", r.Document(hits.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("1", r.Document(hits.ScoreDocs[1].Doc).Get("id"));
            Assert.AreEqual("3", r.Document(hits.ScoreDocs[2].Doc).Get("id"));

            string expl = rescorer.Explain(searcher, searcher.Explain(query, hits.ScoreDocs[0].Doc), hits.ScoreDocs[0].Doc).ToString();

            // Confirm the explanation breaks out the individual
            // sort fields:
            Assert.IsTrue(expl.Contains("= sort field <int: \"popularity\">! value=20"));

            // Confirm the explanation includes first pass details:
            Assert.IsTrue(expl.Contains("= first pass score"));
            Assert.IsTrue(expl.Contains("body:contents in"));
        }

        [Test]
        public virtual void TestRandom()
        {
            Directory dir = NewDirectory();
            int numDocs = AtLeast(1000);
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            int[] idToNum = new int[numDocs];
            int maxValue = TestUtil.NextInt32(Random, 10, 1000000);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", "" + i, Field.Store.YES));
                int numTokens = TestUtil.NextInt32(Random, 1, 10);
                StringBuilder b = new StringBuilder();
                for (int j = 0; j < numTokens; j++)
                {
                    b.Append("a ");
                }
                doc.Add(NewTextField("field", b.ToString(), Field.Store.NO));
                idToNum[i] = Random.Next(maxValue);
                doc.Add(new NumericDocValuesField("num", idToNum[i]));
                w.AddDocument(doc);
            }
            IndexReader r = w.GetReader();
            w.Dispose();

            IndexSearcher s = NewSearcher(r);
            int numHits = TestUtil.NextInt32(Random, 1, numDocs);
            bool reverse = Random.NextBoolean();

            TopDocs hits = s.Search(new TermQuery(new Term("field", "a")), numHits);

            Rescorer rescorer = new SortRescorer(new Sort(new SortField("num", SortFieldType.INT32, reverse)));
            TopDocs hits2 = rescorer.Rescore(s, hits, numHits);

            int[] expected = new int[numHits];
            for (int i = 0; i < numHits; i++)
            {
                expected[i] = hits.ScoreDocs[i].Doc;
            }

            int reverseInt = reverse ? -1 : 1;

            Array.Sort(expected, Comparer<int>.Create((a, b) =>
            {
                try
                {
                    int av = idToNum[Convert.ToInt32(r.Document(a).Get("id"))];
                    int bv = idToNum[Convert.ToInt32(r.Document(b).Get("id"))];
                    if (av < bv)
                    {
                        return -reverseInt;
                    }
                    else if (bv < av)
                    {
                        return reverseInt;
                    }
                    else
                    {
                        // Tie break by docID, ascending
                        return a - b;
                    }
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    throw RuntimeException.Create(ioe);
                }
            }));

            bool fail = false;
            for (int i = 0; i < numHits; i++)
            {
                fail |= (int)expected[i] != hits2.ScoreDocs[i].Doc;
            }
            Assert.IsFalse(fail);

            r.Dispose();
            dir.Dispose();
        }               
    }
}