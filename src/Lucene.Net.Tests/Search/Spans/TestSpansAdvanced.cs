using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Search.Spans
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

    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenFilter = Lucene.Net.Analysis.MockTokenFilter;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    ///*****************************************************************************
    /// Tests the span query bug in Lucene. It demonstrates that SpanTermQuerys don't
    /// work correctly in a BooleanQuery.
    ///
    /// </summary>
    [TestFixture]
    public class TestSpansAdvanced : LuceneTestCase
    {
        // location to the index
        protected internal Directory mDirectory;

        protected internal IndexReader reader;
        protected internal IndexSearcher searcher;

        // field names in the index
        private const string FIELD_ID = "ID";

        protected internal const string FIELD_TEXT = "TEXT";

        /// <summary>
        /// Initializes the tests by adding 4 identical documents to the index.
        /// </summary>
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            // create test index
            mDirectory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, mDirectory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET)).SetMergePolicy(NewLogMergePolicy()).SetSimilarity(new DefaultSimilarity()));
            AddDocument(writer, "1", "I think it should work.");
            AddDocument(writer, "2", "I think it should work.");
            AddDocument(writer, "3", "I think it should work.");
            AddDocument(writer, "4", "I think it should work.");
            reader = writer.GetReader();
            writer.Dispose();
            searcher = NewSearcher(reader);
            searcher.Similarity = new DefaultSimilarity();
        }

        [TearDown]
        public override void TearDown()
        {
            if (reader != null)
            {
                reader.Dispose();
            }

            if (mDirectory != null)
            {
                mDirectory.Dispose();
                mDirectory = null;
            }
            base.TearDown();
        }

        /// <summary>
        /// Adds the document to the index.
        /// </summary>
        /// <param name="writer"> the Lucene index writer </param>
        /// <param name="id"> the unique id of the document </param>
        /// <param name="text"> the text of the document </param>
        protected internal virtual void AddDocument(RandomIndexWriter writer, string id, string text)
        {
            Document document = new Document();
            document.Add(NewStringField(FIELD_ID, id, Field.Store.YES));
            document.Add(NewTextField(FIELD_TEXT, text, Field.Store.YES));
            writer.AddDocument(document);
        }

        /// <summary>
        /// Tests two span queries.
        /// </summary>
        [Test]
        public virtual void TestBooleanQueryWithSpanQueries()
        {
            DoTestBooleanQueryWithSpanQueries(searcher, 0.3884282f);
        }

        /// <summary>
        /// Tests two span queries.
        /// </summary>
        protected internal virtual void DoTestBooleanQueryWithSpanQueries(IndexSearcher s, float expectedScore)
        {
            Query spanQuery = new SpanTermQuery(new Term(FIELD_TEXT, "work"));
            BooleanQuery query = new BooleanQuery();
            query.Add(spanQuery, Occur.MUST);
            query.Add(spanQuery, Occur.MUST);
            string[] expectedIds = new string[] { "1", "2", "3", "4" };
            float[] expectedScores = new float[] { expectedScore, expectedScore, expectedScore, expectedScore };
            AssertHits(s, query, "two span queries", expectedIds, expectedScores);
        }

        /// <summary>
        /// Checks to see if the hits are what we expected.
        /// 
        /// LUCENENET specific
        /// Is non-static because it depends on the non-static variable, <see cref="LuceneTestCase.Similarity"/>
        /// </summary>
        /// <param name="query"> the query to execute </param>
        /// <param name="description"> the description of the search </param>
        /// <param name="expectedIds"> the expected document ids of the hits </param>
        /// <param name="expectedScores"> the expected scores of the hits </param>
        protected internal void AssertHits(IndexSearcher s, Query query, string description, string[] expectedIds, float[] expectedScores)
        {
            QueryUtils.Check(Random, query, s);

            const float tolerance = 1e-5f;

            // Hits hits = searcher.Search(query);
            // hits normalizes and throws things off if one score is greater than 1.0
            TopDocs topdocs = s.Search(query, null, 10000);

            /*
            /// // display the hits System.out.println(hits.Length() +
            /// " hits for search: \"" + description + '\"'); for (int i = 0; i <
            /// hits.Length(); i++) { System.out.println("  " + FIELD_ID + ':' +
            /// hits.Doc(i).Get(FIELD_ID) + " (score:" + hits.Score(i) + ')'); }
            /// ****
            */

            // did we get the hits we expected
            Assert.AreEqual(expectedIds.Length, topdocs.TotalHits);
            for (int i = 0; i < topdocs.TotalHits; i++)
            {
                // System.out.println(i + " exp: " + expectedIds[i]);
                // System.out.println(i + " field: " + hits.Doc(i).Get(FIELD_ID));

                int id = topdocs.ScoreDocs[i].Doc;
                float score = topdocs.ScoreDocs[i].Score;
                Document doc = s.Doc(id);
                Assert.AreEqual(expectedIds[i], doc.Get(FIELD_ID));
                bool scoreEq = Math.Abs(expectedScores[i] - score) < tolerance;
                if (!scoreEq)
                {
                    Console.WriteLine(i + " warning, expected score: " + expectedScores[i] + ", actual " + score);
                    Console.WriteLine(s.Explain(query, id));
                }
                Assert.AreEqual(expectedScores[i], score, tolerance);
                Assert.AreEqual(s.Explain(query, id).Value, score, tolerance);
            }
        }
    }
}