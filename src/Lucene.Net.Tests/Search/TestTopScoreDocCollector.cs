using NUnit.Framework;
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
    using Document = Documents.Document;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;

    [TestFixture]
    public class TestTopScoreDocCollector : LuceneTestCase
    {
        [Test]
        public virtual void TestOutOfOrderCollection()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            for (int i = 0; i < 10; i++)
            {
                writer.AddDocument(new Document());
            }

            bool[] inOrder = new bool[] { false, true };
            string[] actualTSDCClass = new string[] { "OutOfOrderTopScoreDocCollector", "InOrderTopScoreDocCollector" };

            BooleanQuery bq = new BooleanQuery();
            // Add a Query with SHOULD, since bw.Scorer() returns BooleanScorer2
            // which delegates to BS if there are no mandatory clauses.
            bq.Add(new MatchAllDocsQuery(), Occur.SHOULD);
            // Set minNrShouldMatch to 1 so that BQ will not optimize rewrite to return
            // the clause instead of BQ.
            bq.MinimumNumberShouldMatch = 1;
            IndexReader reader = writer.GetReader();
            IndexSearcher searcher = NewSearcher(reader);
            for (int i = 0; i < inOrder.Length; i++)
            {
                TopDocsCollector<ScoreDoc> tdc = TopScoreDocCollector.Create(3, inOrder[i]);
                Assert.AreEqual("Lucene.Net.Search.TopScoreDocCollector+" + actualTSDCClass[i], tdc.GetType().FullName);

                searcher.Search(new MatchAllDocsQuery(), tdc);

                ScoreDoc[] sd = tdc.GetTopDocs().ScoreDocs;
                Assert.AreEqual(3, sd.Length);
                for (int j = 0; j < sd.Length; j++)
                {
                    Assert.AreEqual(j, sd[j].Doc, "expected doc Id " + j + " found " + sd[j].Doc);
                }
            }
            writer.Dispose();
            reader.Dispose();
            dir.Dispose();
        }
    }
}