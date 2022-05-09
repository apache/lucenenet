using Lucene.Net.Documents;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Expressions
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

    [SuppressCodecs("Lucene3x")]
    public class TestExpressionRescorer : LuceneTestCase
    {
        internal IndexSearcher searcher;

        internal DirectoryReader reader;

        internal Directory dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            var iw = new RandomIndexWriter(Random, dir);
            var doc = new Document
            {
                NewStringField("id", "1", Field.Store.YES),
                NewTextField("body", "some contents and more contents", Field.Store.NO),
                new NumericDocValuesField("popularity", 5)
            };
            iw.AddDocument(doc);

            doc = new Document
            {
                NewStringField("id", "2", Field.Store.YES),
                NewTextField("body", "another document with different contents", Field.Store
                    .NO),
                new NumericDocValuesField("popularity", 20)
            };
            iw.AddDocument(doc);

            doc = new Document
            {
                NewStringField("id", "3", Field.Store.YES),
                NewTextField("body", "crappy contents", Field.Store.NO),
                new NumericDocValuesField("popularity", 2)
            };
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

            Expression e = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
            SimpleBindings bindings = new SimpleBindings();
            bindings.Add(new SortField("popularity", SortFieldType.INT32));
            bindings.Add(new SortField("_score", SortFieldType.SCORE));
            Rescorer rescorer = e.GetRescorer(bindings);

            hits = rescorer.Rescore(searcher, hits, 10);
            Assert.AreEqual(3, hits.TotalHits);
            Assert.AreEqual("2", r.Document(hits.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("1", r.Document(hits.ScoreDocs[1].Doc).Get("id"));
            Assert.AreEqual("3", r.Document(hits.ScoreDocs[2].Doc).Get("id"));

            string expl = rescorer.Explain(searcher, 
                                           searcher.Explain(query, hits.ScoreDocs[0].Doc), 
                                           hits.ScoreDocs[0].Doc).ToString();

            // Confirm the explanation breaks out the individual
            // variables:
            Assert.IsTrue(expl.Contains("= variable \"popularity\""));

            // Confirm the explanation includes first pass details:
            Assert.IsTrue(expl.Contains("= first pass score"));
            Assert.IsTrue(expl.Contains("body:contents in"));
        }
    }
}
