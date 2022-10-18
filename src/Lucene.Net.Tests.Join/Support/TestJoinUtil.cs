// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Join;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Tests.Join
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

    [Obsolete("Production tests are in Lucene.Net.Search.Join. This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class TestJoinUtil : LuceneTestCase
    {
        [Test]
        public void TestSimple()
        {
            const string idField = "id";
            const string toField = "productId";

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                    .SetMergePolicy(NewLogMergePolicy()));

            // 0
            Document doc = new Document();
            doc.Add(new TextField("description", "random text", Field.Store.NO));
            doc.Add(new TextField("name", "name1", Field.Store.NO));
            doc.Add(new TextField(idField, "1", Field.Store.NO));
            w.AddDocument(doc);

            // 1
            doc = new Document();
            doc.Add(new TextField("price", "10.0", Field.Store.NO));
            doc.Add(new TextField(idField, "2", Field.Store.NO));
            doc.Add(new TextField(toField, "1", Field.Store.NO));
            w.AddDocument(doc);

            // 2
            doc = new Document();
            doc.Add(new TextField("price", "20.0", Field.Store.NO));
            doc.Add(new TextField(idField, "3", Field.Store.NO));
            doc.Add(new TextField(toField, "1", Field.Store.NO));
            w.AddDocument(doc);

            // 3
            doc = new Document();
            doc.Add(new TextField("description", "more random text", Field.Store.NO));
            doc.Add(new TextField("name", "name2", Field.Store.NO));
            doc.Add(new TextField(idField, "4", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();

            // 4
            doc = new Document();
            doc.Add(new TextField("price", "10.0", Field.Store.NO));
            doc.Add(new TextField(idField, "5", Field.Store.NO));
            doc.Add(new TextField(toField, "4", Field.Store.NO));
            w.AddDocument(doc);

            // 5
            doc = new Document();
            doc.Add(new TextField("price", "20.0", Field.Store.NO));
            doc.Add(new TextField(idField, "6", Field.Store.NO));
            doc.Add(new TextField(toField, "4", Field.Store.NO));
            w.AddDocument(doc);

            IndexSearcher indexSearcher = new IndexSearcher(w.GetReader());
            w.Dispose();

            // Search for product
            Query joinQuery = JoinUtil.CreateJoinQuery(idField, false, toField, new TermQuery(new Term("name", "name2")),
                indexSearcher, ScoreMode.None);

            TopDocs result = indexSearcher.Search(joinQuery, 10);
            assertEquals(2, result.TotalHits);
            assertEquals(4, result.ScoreDocs[0].Doc);
            assertEquals(5, result.ScoreDocs[1].Doc);

            joinQuery = JoinUtil.CreateJoinQuery(idField, false, toField, new TermQuery(new Term("name", "name1")),
                indexSearcher, ScoreMode.None);
            result = indexSearcher.Search(joinQuery, 10);
            assertEquals(2, result.TotalHits);
            assertEquals(1, result.ScoreDocs[0].Doc);
            assertEquals(2, result.ScoreDocs[1].Doc);

            // Search for offer
            joinQuery = JoinUtil.CreateJoinQuery(toField, false, idField, new TermQuery(new Term("id", "5")),
                indexSearcher, ScoreMode.None);
            result = indexSearcher.Search(joinQuery, 10);
            assertEquals(1, result.TotalHits);
            assertEquals(3, result.ScoreDocs[0].Doc);

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        // TermsWithScoreCollector.MV.Avg forgets to grow beyond TermsWithScoreCollector.INITIAL_ARRAY_SIZE
        [Test]
        public void TestOverflowTermsWithScoreCollector()
        {
            Test300spartans(true, ScoreMode.Avg);
        }

        [Test]
        public void TestOverflowTermsWithScoreCollectorRandom()
        {
            var scoreModeLength = Enum.GetNames(typeof(ScoreMode)).Length;
            Test300spartans(Random.NextBoolean(), (ScoreMode)Random.Next(scoreModeLength));
        }

        protected virtual void Test300spartans(bool multipleValues, ScoreMode scoreMode)
        {
            const string idField = "id";
            const string toField = "productId";

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                    .SetMergePolicy(NewLogMergePolicy()));

            // 0
            Document doc = new Document();
            doc.Add(new TextField("description", "random text", Field.Store.NO));
            doc.Add(new TextField("name", "name1", Field.Store.NO));
            doc.Add(new TextField(idField, "0", Field.Store.NO));
            w.AddDocument(doc);

            doc = new Document();
            doc.Add(new TextField("price", "10.0", Field.Store.NO));
            for (int i = 0; i < 300; i++)
            {
                doc.Add(new TextField(toField, "" + i, Field.Store.NO));
                if (!multipleValues)
                {
                    w.AddDocument(doc);
                    doc.RemoveFields(toField);
                }
            }
            w.AddDocument(doc);

            IndexSearcher indexSearcher = new IndexSearcher(w.GetReader());
            w.Dispose();

            // Search for product
            Query joinQuery = JoinUtil.CreateJoinQuery(toField, multipleValues, idField,
                new TermQuery(new Term("price", "10.0")), indexSearcher, scoreMode);

            TopDocs result = indexSearcher.Search(joinQuery, 10);
            assertEquals(1, result.TotalHits);
            assertEquals(0, result.ScoreDocs[0].Doc);


            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// LUCENE-5487: verify a join query inside a SHOULD BQ
        ///  will still use the join query's optimized BulkScorers 
        /// </summary>
        [Test]
        public void TestInsideBooleanQuery()
        {
            const string idField = "id";
            const string toField = "productId";

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                    .SetMergePolicy(NewLogMergePolicy()));

            // 0
            Document doc = new Document();
            doc.Add(new TextField("description", "random text", Field.Store.NO));
            doc.Add(new TextField("name", "name1", Field.Store.NO));
            doc.Add(new TextField(idField, "7", Field.Store.NO));
            w.AddDocument(doc);

            // 1
            doc = new Document();
            doc.Add(new TextField("price", "10.0", Field.Store.NO));
            doc.Add(new TextField(idField, "2", Field.Store.NO));
            doc.Add(new TextField(toField, "7", Field.Store.NO));
            w.AddDocument(doc);

            // 2
            doc = new Document();
            doc.Add(new TextField("price", "20.0", Field.Store.NO));
            doc.Add(new TextField(idField, "3", Field.Store.NO));
            doc.Add(new TextField(toField, "7", Field.Store.NO));
            w.AddDocument(doc);

            // 3
            doc = new Document();
            doc.Add(new TextField("description", "more random text", Field.Store.NO));
            doc.Add(new TextField("name", "name2", Field.Store.NO));
            doc.Add(new TextField(idField, "0", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();

            // 4
            doc = new Document();
            doc.Add(new TextField("price", "10.0", Field.Store.NO));
            doc.Add(new TextField(idField, "5", Field.Store.NO));
            doc.Add(new TextField(toField, "0", Field.Store.NO));
            w.AddDocument(doc);

            // 5
            doc = new Document();
            doc.Add(new TextField("price", "20.0", Field.Store.NO));
            doc.Add(new TextField(idField, "6", Field.Store.NO));
            doc.Add(new TextField(toField, "0", Field.Store.NO));
            w.AddDocument(doc);

            w.ForceMerge(1);

            IndexSearcher indexSearcher = new IndexSearcher(w.GetReader());
            w.Dispose();

            // Search for product
            Query joinQuery = JoinUtil.CreateJoinQuery(idField, false, toField,
                new TermQuery(new Term("description", "random")), indexSearcher, ScoreMode.Avg);

            BooleanQuery bq = new BooleanQuery();
            bq.Add(joinQuery, Occur.SHOULD);
            bq.Add(new TermQuery(new Term("id", "3")), Occur.SHOULD);

            indexSearcher.Search(bq, new CollectorAnonymousClass());

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        private sealed class CollectorAnonymousClass : ICollector
        {
            internal bool sawFive;

            public void SetNextReader(AtomicReaderContext context)
            {
            }

            public void Collect(int docID)
            {
                // Hairy / evil (depends on how BooleanScorer
                // stores temporarily collected docIDs by
                // appending to head of linked list):
                if (docID == 5)
                {
                    sawFive = true;
                }
                else if (docID == 1)
                {
                    assertFalse("optimized bulkScorer was not used for join query embedded in boolean query!", sawFive);
                }
            }

            public void SetScorer(Scorer scorer)
            {
            }

            public bool AcceptsDocsOutOfOrder => true;
        }

        [Test]
        public void TestSimpleWithScoring()
        {
            const string idField = "id";
            const string toField = "movieId";

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                    .SetMergePolicy(NewLogMergePolicy()));

            // 0
            Document doc = new Document();
            doc.Add(new TextField("description", "A random movie", Field.Store.NO));
            doc.Add(new TextField("name", "Movie 1", Field.Store.NO));
            doc.Add(new TextField(idField, "1", Field.Store.NO));
            w.AddDocument(doc);

            // 1
            doc = new Document();
            doc.Add(new TextField("subtitle", "The first subtitle of this movie", Field.Store.NO));
            doc.Add(new TextField(idField, "2", Field.Store.NO));
            doc.Add(new TextField(toField, "1", Field.Store.NO));
            w.AddDocument(doc);

            // 2
            doc = new Document();
            doc.Add(new TextField("subtitle", "random subtitle; random event movie", Field.Store.NO));
            doc.Add(new TextField(idField, "3", Field.Store.NO));
            doc.Add(new TextField(toField, "1", Field.Store.NO));
            w.AddDocument(doc);

            // 3
            doc = new Document();
            doc.Add(new TextField("description", "A second random movie", Field.Store.NO));
            doc.Add(new TextField("name", "Movie 2", Field.Store.NO));
            doc.Add(new TextField(idField, "4", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();

            // 4
            doc = new Document();
            doc.Add(new TextField("subtitle", "a very random event happened during christmas night", Field.Store.NO));
            doc.Add(new TextField(idField, "5", Field.Store.NO));
            doc.Add(new TextField(toField, "4", Field.Store.NO));
            w.AddDocument(doc);

            // 5
            doc = new Document();
            doc.Add(new TextField("subtitle", "movie end movie test 123 test 123 random", Field.Store.NO));
            doc.Add(new TextField(idField, "6", Field.Store.NO));
            doc.Add(new TextField(toField, "4", Field.Store.NO));
            w.AddDocument(doc);

            IndexSearcher indexSearcher = new IndexSearcher(w.GetReader());
            w.Dispose();

            // Search for movie via subtitle
            Query joinQuery = JoinUtil.CreateJoinQuery(toField, false, idField,
                new TermQuery(new Term("subtitle", "random")), indexSearcher, ScoreMode.Max);
            TopDocs result = indexSearcher.Search(joinQuery, 10);
            assertEquals(2, result.TotalHits);
            assertEquals(0, result.ScoreDocs[0].Doc);
            assertEquals(3, result.ScoreDocs[1].Doc);

            // Score mode max.
            joinQuery = JoinUtil.CreateJoinQuery(toField, false, idField, new TermQuery(new Term("subtitle", "movie")),
                indexSearcher, ScoreMode.Max);
            result = indexSearcher.Search(joinQuery, 10);
            assertEquals(2, result.TotalHits);
            assertEquals(3, result.ScoreDocs[0].Doc);
            assertEquals(0, result.ScoreDocs[1].Doc);

            // Score mode total
            joinQuery = JoinUtil.CreateJoinQuery(toField, false, idField, new TermQuery(new Term("subtitle", "movie")),
                indexSearcher, ScoreMode.Total);
            result = indexSearcher.Search(joinQuery, 10);
            assertEquals(2, result.TotalHits);
            assertEquals(0, result.ScoreDocs[0].Doc);
            assertEquals(3, result.ScoreDocs[1].Doc);

            //Score mode avg
            joinQuery = JoinUtil.CreateJoinQuery(toField, false, idField, new TermQuery(new Term("subtitle", "movie")),
                indexSearcher, ScoreMode.Avg);
            result = indexSearcher.Search(joinQuery, 10);
            assertEquals(2, result.TotalHits);
            assertEquals(3, result.ScoreDocs[0].Doc);
            assertEquals(0, result.ScoreDocs[1].Doc);

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        [Test]
        [Slow]
        public void TestSingleValueRandomJoin()
        {
            int maxIndexIter = TestUtil.NextInt32(Random, 6, 12);
            int maxSearchIter = TestUtil.NextInt32(Random, 13, 26);
            ExecuteRandomJoin(false, maxIndexIter, maxSearchIter, TestUtil.NextInt32(Random, 87, 764));
        }

        [Test]
        // [Slow] // LUCENENET specific - Not slow in .NET
        public void TestMultiValueRandomJoin()
        // this test really takes more time, that is why the number of iterations are smaller.
        {
            int maxIndexIter = TestUtil.NextInt32(Random, 3, 6);
            int maxSearchIter = TestUtil.NextInt32(Random, 6, 12);
            ExecuteRandomJoin(true, maxIndexIter, maxSearchIter, TestUtil.NextInt32(Random, 11, 57));
        }

        private void ExecuteRandomJoin(bool multipleValuesPerDocument, int maxIndexIter, int maxSearchIter,
            int numberOfDocumentsToIndex)
        {
            for (int indexIter = 1; indexIter <= maxIndexIter; indexIter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("indexIter=" + indexIter);
                }
                Directory dir = NewDirectory();
                RandomIndexWriter w = new RandomIndexWriter(Random, dir,
                    NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.KEYWORD, false))
                        .SetMergePolicy(NewLogMergePolicy()));
                bool scoreDocsInOrder = TestJoinUtil.Random.NextBoolean();
                IndexIterationContext context = CreateContext(numberOfDocumentsToIndex, w, multipleValuesPerDocument,
                    scoreDocsInOrder);

                IndexReader topLevelReader = w.GetReader();
                w.Dispose();
                for (int searchIter = 1; searchIter <= maxSearchIter; searchIter++)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("searchIter=" + searchIter);
                    }
                    IndexSearcher indexSearcher = NewSearcher(topLevelReader);

                    int r = Random.Next(context.RandomUniqueValues.Length);
                    bool from = context.RandomFrom[r];
                    string randomValue = context.RandomUniqueValues[r];
                    FixedBitSet expectedResult = CreateExpectedResult(randomValue, from, indexSearcher.IndexReader,
                        context);

                    Query actualQuery = new TermQuery(new Term("value", randomValue));
                    if (Verbose)
                    {
                        Console.WriteLine("actualQuery=" + actualQuery);
                    }

                    var scoreModeLength = Enum.GetNames(typeof(ScoreMode)).Length;
                    ScoreMode scoreMode = (ScoreMode)Random.Next(scoreModeLength);
                    if (Verbose)
                    {
                        Console.WriteLine("scoreMode=" + scoreMode);
                    }

                    Query joinQuery;
                    if (from)
                    {
                        joinQuery = JoinUtil.CreateJoinQuery("from", multipleValuesPerDocument, "to", actualQuery,
                            indexSearcher, scoreMode);
                    }
                    else
                    {
                        joinQuery = JoinUtil.CreateJoinQuery("to", multipleValuesPerDocument, "from", actualQuery,
                            indexSearcher, scoreMode);
                    }
                    if (Verbose)
                    {
                        Console.WriteLine("joinQuery=" + joinQuery);
                    }

                    // Need to know all documents that have matches. TopDocs doesn't give me that and then I'd be also testing TopDocsCollector...
                    FixedBitSet actualResult = new FixedBitSet(indexSearcher.IndexReader.MaxDoc);
                    TopScoreDocCollector topScoreDocCollector = TopScoreDocCollector.Create(10, false);
                    indexSearcher.Search(joinQuery,
                        new CollectorAnonymousClass2(scoreDocsInOrder, actualResult,
                            topScoreDocCollector));
                    // Asserting bit set...
                    if (Verbose)
                    {
                        Console.WriteLine("expected cardinality:" + expectedResult.Cardinality);
                        DocIdSetIterator iterator = expectedResult.GetIterator();
                        for (int doc = iterator.NextDoc();
                            doc != DocIdSetIterator.NO_MORE_DOCS;
                            doc = iterator.NextDoc())
                        {
                            Console.WriteLine(string.Format("Expected doc[{0}] with id value {1}", doc, indexSearcher.Doc(doc).Get("id")));
                        }
                        Console.WriteLine("actual cardinality:" + actualResult.Cardinality);
                        iterator = actualResult.GetIterator();
                        for (int doc = iterator.NextDoc();
                            doc != DocIdSetIterator.NO_MORE_DOCS;
                            doc = iterator.NextDoc())
                        {
                            Console.WriteLine(string.Format("Actual doc[{0}] with id value {1}", doc, indexSearcher.Doc(doc).Get("id")));
                        }
                    }
                    assertEquals(expectedResult, actualResult);

                    // Asserting TopDocs...
                    TopDocs expectedTopDocs = CreateExpectedTopDocs(randomValue, from, scoreMode, context);
                    TopDocs actualTopDocs = topScoreDocCollector.GetTopDocs();
                    assertEquals(expectedTopDocs.TotalHits, actualTopDocs.TotalHits);
                    assertEquals(expectedTopDocs.ScoreDocs.Length, actualTopDocs.ScoreDocs.Length);
                    if (scoreMode == ScoreMode.None)
                    {
                        continue;
                    }

                    assertEquals(expectedTopDocs.MaxScore, actualTopDocs.MaxScore, 0.0f);
                    for (int i = 0; i < expectedTopDocs.ScoreDocs.Length; i++)
                    {
                        if (Verbose)
                        {
                            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Expected doc: {0} | Actual doc: {1}\n", expectedTopDocs.ScoreDocs[i].Doc, actualTopDocs.ScoreDocs[i].Doc));
                            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Expected score: {0} | Actual score: {1}\n", expectedTopDocs.ScoreDocs[i].Score, actualTopDocs.ScoreDocs[i].Score));
                        }
                        assertEquals(expectedTopDocs.ScoreDocs[i].Doc, actualTopDocs.ScoreDocs[i].Doc);
                        assertEquals(expectedTopDocs.ScoreDocs[i].Score, actualTopDocs.ScoreDocs[i].Score, 0.0f);
                        Explanation explanation = indexSearcher.Explain(joinQuery, expectedTopDocs.ScoreDocs[i].Doc);
                        assertEquals(expectedTopDocs.ScoreDocs[i].Score, explanation.Value, 0.0f);
                    }
                }
                topLevelReader.Dispose();
                dir.Dispose();
            }
        }

        private sealed class CollectorAnonymousClass2 : ICollector
        {
            private bool scoreDocsInOrder;
            private FixedBitSet actualResult;
            private TopScoreDocCollector topScoreDocCollector;

            public CollectorAnonymousClass2(bool scoreDocsInOrder,
                FixedBitSet actualResult,
                TopScoreDocCollector topScoreDocCollector)
            {
                this.scoreDocsInOrder = scoreDocsInOrder;
                this.actualResult = actualResult;
                this.topScoreDocCollector = topScoreDocCollector;
            }


            private int _docBase;

            public void Collect(int doc)
            {
                actualResult.Set(doc + _docBase);
                topScoreDocCollector.Collect(doc);
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                _docBase = context.DocBase;
                topScoreDocCollector.SetNextReader(context);
            }

            public void SetScorer(Scorer scorer)
            {
                topScoreDocCollector.SetScorer(scorer);
            }

            public bool AcceptsDocsOutOfOrder => scoreDocsInOrder;
        }

        private IndexIterationContext CreateContext(int nDocs, RandomIndexWriter writer, bool multipleValuesPerDocument, bool scoreDocsInOrder)
        {
            return CreateContext(nDocs, writer, writer, multipleValuesPerDocument, scoreDocsInOrder);
        }

        private IndexIterationContext CreateContext(int nDocs, RandomIndexWriter fromWriter, RandomIndexWriter toWriter,
            bool multipleValuesPerDocument, bool scoreDocsInOrder)
        {
            IndexIterationContext context = new IndexIterationContext();
            int numRandomValues = nDocs / 2;
            context.RandomUniqueValues = new string[numRandomValues];
            ISet<string> trackSet = new JCG.HashSet<string>();
            context.RandomFrom = new bool[numRandomValues];
            for (int i = 0; i < numRandomValues; i++)
            {
                string uniqueRandomValue;
                do
                {
                    uniqueRandomValue = TestUtil.RandomRealisticUnicodeString(Random);
                    //        uniqueRandomValue = TestUtil.randomSimpleString(random);
                } while ("".Equals(uniqueRandomValue, StringComparison.Ordinal) || trackSet.Contains(uniqueRandomValue));
                // Generate unique values and empty strings aren't allowed.
                trackSet.Add(uniqueRandomValue);
                context.RandomFrom[i] = Random.NextBoolean();
                context.RandomUniqueValues[i] = uniqueRandomValue;
            }

            RandomDoc[] docs = new RandomDoc[nDocs];
            for (int i = 0; i < nDocs; i++)
            {
                string id = Convert.ToString(i, CultureInfo.InvariantCulture);
                int randomI = Random.Next(context.RandomUniqueValues.Length);
                string value = context.RandomUniqueValues[randomI];
                Document document = new Document();
                document.Add(NewTextField(Random, "id", id, Field.Store.NO));
                document.Add(NewTextField(Random, "value", value, Field.Store.NO));

                bool from = context.RandomFrom[randomI];
                int numberOfLinkValues = multipleValuesPerDocument ? 2 + Random.Next(10) : 1;
                docs[i] = new RandomDoc(id, numberOfLinkValues, value, from);
                for (int j = 0; j < numberOfLinkValues; j++)
                {
                    string linkValue = context.RandomUniqueValues[Random.Next(context.RandomUniqueValues.Length)];
                    docs[i].linkValues.Add(linkValue);
                    if (from)
                    {
                        if (!context.FromDocuments.TryGetValue(linkValue, out IList<RandomDoc> fromDocs))
                        {
                            context.FromDocuments[linkValue] = fromDocs = new List<RandomDoc>();
                        }
                        if (!context.RandomValueFromDocs.TryGetValue(value, out IList<RandomDoc> randomValueFromDocs))
                        {
                            context.RandomValueFromDocs[value] = randomValueFromDocs = new List<RandomDoc>();
                        }

                        fromDocs.Add(docs[i]);
                        randomValueFromDocs.Add(docs[i]);
                        document.Add(NewTextField(Random, "from", linkValue, Field.Store.NO));
                    }
                    else
                    {
                        if (!context.ToDocuments.TryGetValue(linkValue, out IList<RandomDoc> toDocuments))
                        {
                            context.ToDocuments[linkValue] = toDocuments = new List<RandomDoc>();
                        }
                        if (!context.RandomValueToDocs.TryGetValue(value, out IList<RandomDoc> randomValueToDocs))
                        {
                            context.RandomValueToDocs[value] = randomValueToDocs = new List<RandomDoc>();
                        }

                        toDocuments.Add(docs[i]);
                        randomValueToDocs.Add(docs[i]);
                        document.Add(NewTextField(Random, "to", linkValue, Field.Store.NO));
                    }
                }

                RandomIndexWriter w;
                if (from)
                {
                    w = fromWriter;
                }
                else
                {
                    w = toWriter;
                }

                w.AddDocument(document);
                if (Random.Next(10) == 4)
                {
                    w.Commit();
                }
                if (Verbose)
                {
                    Console.WriteLine("Added document[" + docs[i].id + "]: " + document);
                }
            }

            // Pre-compute all possible hits for all unique random values. On top of this also compute all possible score for
            // any ScoreMode.
            IndexSearcher fromSearcher = NewSearcher(fromWriter.GetReader());
            IndexSearcher toSearcher = NewSearcher(toWriter.GetReader());
            for (int i = 0; i < context.RandomUniqueValues.Length; i++)
            {
                string uniqueRandomValue = context.RandomUniqueValues[i];
                string fromField;
                string toField;
                IDictionary<string, IDictionary<int, JoinScore>> queryVals;
                if (context.RandomFrom[i])
                {
                    fromField = "from";
                    toField = "to";
                    queryVals = context.FromHitsToJoinScore;
                }
                else
                {
                    fromField = "to";
                    toField = "from";
                    queryVals = context.ToHitsToJoinScore;
                }
                IDictionary<BytesRef, JoinScore> joinValueToJoinScores = new Dictionary<BytesRef, JoinScore>();
                if (multipleValuesPerDocument)
                {
                    fromSearcher.Search(new TermQuery(new Term("value", uniqueRandomValue)),
                        new CollectorAnonymousClass3(fromField, joinValueToJoinScores));
                }
                else
                {
                    fromSearcher.Search(new TermQuery(new Term("value", uniqueRandomValue)),
                        new CollectorAnonymousClass4(fromField, joinValueToJoinScores));
                }

                IDictionary<int, JoinScore> docToJoinScore = new Dictionary<int, JoinScore>();
                if (multipleValuesPerDocument)
                {
                    if (scoreDocsInOrder)
                    {
                        AtomicReader slowCompositeReader = SlowCompositeReaderWrapper.Wrap(toSearcher.IndexReader);
                        Terms terms = slowCompositeReader.GetTerms(toField);
                        if (terms != null)
                        {
                            DocsEnum docsEnum = null;
                            TermsEnum termsEnum = null;
                            JCG.SortedSet<BytesRef> joinValues =
                                new JCG.SortedSet<BytesRef>(BytesRef.UTF8SortedAsUnicodeComparer);
                            joinValues.UnionWith(joinValueToJoinScores.Keys);
                            foreach (BytesRef joinValue in joinValues)
                            {
                                termsEnum = terms.GetEnumerator(termsEnum);
                                if (termsEnum.SeekExact(joinValue))
                                {
                                    docsEnum = termsEnum.Docs(slowCompositeReader.LiveDocs, docsEnum, DocsFlags.NONE);
                                    JoinScore joinScore = joinValueToJoinScores[joinValue];

                                    for (int doc = docsEnum.NextDoc(); doc != DocIdSetIterator.NO_MORE_DOCS; doc = docsEnum.NextDoc())
                                    {
                                        // First encountered join value determines the score.
                                        // Something to keep in mind for many-to-many relations.
                                        if (!docToJoinScore.ContainsKey(doc))
                                        {
                                            docToJoinScore[doc] = joinScore;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        toSearcher.Search(new MatchAllDocsQuery(),
                            new CollectorAnonymousClass5(toField, joinValueToJoinScores,
                                docToJoinScore));
                    }
                }
                else
                {
                    toSearcher.Search(new MatchAllDocsQuery(),
                        new CollectorAnonymousClass6(toField, joinValueToJoinScores,
                            docToJoinScore));
                }
                queryVals[uniqueRandomValue] = docToJoinScore;
            }

            fromSearcher.IndexReader.Dispose();
            toSearcher.IndexReader.Dispose();

            return context;
        }

        private sealed class CollectorAnonymousClass3 : ICollector
        {
            private readonly string fromField;
            private readonly IDictionary<BytesRef, JoinScore> joinValueToJoinScores;

            public CollectorAnonymousClass3(string fromField,
                IDictionary<BytesRef, JoinScore> joinValueToJoinScores)
            {
                this.fromField = fromField;
                this.joinValueToJoinScores = joinValueToJoinScores;
                joinValue = new BytesRef();
            }


            private Scorer scorer;
            private SortedSetDocValues docTermOrds;
            internal readonly BytesRef joinValue;

            public void Collect(int doc)
            {
                docTermOrds.SetDocument(doc);
                long ord;
                while ((ord = docTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                {
                    docTermOrds.LookupOrd(ord, joinValue);
                    if (!joinValueToJoinScores.TryGetValue(joinValue, out JoinScore joinScore) || joinScore is null)
                    {
                        joinValueToJoinScores[BytesRef.DeepCopyOf(joinValue)] = joinScore = new JoinScore();
                    }
                    joinScore.AddScore(scorer.GetScore());
                }
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                docTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.AtomicReader, fromField);
            }

            public void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public bool AcceptsDocsOutOfOrder => false;
        }

        private sealed class CollectorAnonymousClass4 : ICollector
        {
            private readonly string fromField;
            private readonly IDictionary<BytesRef, JoinScore> joinValueToJoinScores;

            public CollectorAnonymousClass4(string fromField,
                IDictionary<BytesRef, JoinScore> joinValueToJoinScores)
            {
                this.fromField = fromField;
                this.joinValueToJoinScores = joinValueToJoinScores;
                spare = new BytesRef();
            }


            private Scorer scorer;
            private BinaryDocValues terms;
            private IBits docsWithField;
            private readonly BytesRef spare;

            public void Collect(int doc)
            {
                terms.Get(doc, spare);
                BytesRef joinValue = spare;
                if (joinValue.Length == 0 && !docsWithField.Get(doc))
                {
                    return;
                }

                if (!joinValueToJoinScores.TryGetValue(joinValue, out JoinScore joinScore) || joinScore is null)
                {
                    joinValueToJoinScores[BytesRef.DeepCopyOf(joinValue)] = joinScore = new JoinScore();
                }
                joinScore.AddScore(scorer.GetScore());
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                terms = FieldCache.DEFAULT.GetTerms(context.AtomicReader, fromField, true);
                docsWithField = FieldCache.DEFAULT.GetDocsWithField(context.AtomicReader, fromField);
            }

            public void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public bool AcceptsDocsOutOfOrder => false;
        }

        private sealed class CollectorAnonymousClass5 : ICollector
        {
            private readonly string toField;
            private readonly IDictionary<BytesRef, JoinScore> joinValueToJoinScores;
            private readonly IDictionary<int, JoinScore> docToJoinScore;

            private SortedSetDocValues docTermOrds;
            private readonly BytesRef scratch = new BytesRef();
            private int docBase;

            public CollectorAnonymousClass5(
                string toField, IDictionary<BytesRef, JoinScore> joinValueToJoinScores,
                IDictionary<int, JoinScore> docToJoinScore)
            {
                this.toField = toField;
                this.joinValueToJoinScores = joinValueToJoinScores;
                this.docToJoinScore = docToJoinScore;
            }

            public void Collect(int doc)
            {
                docTermOrds.SetDocument(doc);
                long ord;
                while ((ord = docTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                {
                    docTermOrds.LookupOrd(ord, scratch);
                    if (!joinValueToJoinScores.TryGetValue(scratch, out JoinScore joinScore) || joinScore is null)
                    {
                        continue;
                    }
                    int basedDoc = docBase + doc;
                    // First encountered join value determines the score.
                    // Something to keep in mind for many-to-many relations.
                    if (!docToJoinScore.ContainsKey(basedDoc))
                    {
                        docToJoinScore[basedDoc] = joinScore;
                    }
                }
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                docBase = context.DocBase;
                docTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.AtomicReader, toField);
            }

            public bool AcceptsDocsOutOfOrder => false;

            public void SetScorer(Scorer scorer)
            {
            }
        }

        private sealed class CollectorAnonymousClass6 : ICollector
        {
            private readonly string toField;
            private readonly IDictionary<BytesRef, JoinScore> joinValueToJoinScores;
            private readonly IDictionary<int, JoinScore> docToJoinScore;

            private BinaryDocValues terms;
            private int docBase;
            private readonly BytesRef spare = new BytesRef();

            public CollectorAnonymousClass6(
                string toField,
                IDictionary<BytesRef, JoinScore> joinValueToJoinScores,
                IDictionary<int, JoinScore> docToJoinScore)
            {
                this.toField = toField;
                this.joinValueToJoinScores = joinValueToJoinScores;
                this.docToJoinScore = docToJoinScore;
            }

            public void Collect(int doc)
            {
                terms.Get(doc, spare);
                if (!joinValueToJoinScores.TryGetValue(spare, out JoinScore joinScore) || joinScore is null)
                {
                    return;
                }
                docToJoinScore[docBase + doc] = joinScore;
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                terms = FieldCache.DEFAULT.GetTerms(context.AtomicReader, toField, false);
                docBase = context.DocBase;
            }

            public bool AcceptsDocsOutOfOrder => false;

            public void SetScorer(Scorer scorer)
            {
            }
        }

        private TopDocs CreateExpectedTopDocs(string queryValue,
            bool from,
            ScoreMode scoreMode,
            IndexIterationContext context)
        {
            var hitsToJoinScores = @from
                ? context.FromHitsToJoinScore[queryValue]
                : context.ToHitsToJoinScore[queryValue];

            var hits = new List<KeyValuePair<int, JoinScore>>(hitsToJoinScores);
            hits.Sort(Comparer<KeyValuePair<int, JoinScore>>.Create((hit1, hit2) =>
          {
              float score1 = hit1.Value.Score(scoreMode);
              float score2 = hit2.Value.Score(scoreMode);

              int cmp = score2.CompareTo(score1);
              if (cmp != 0)
              {
                  return cmp;
              }
              return hit1.Key - hit2.Key;
          }));
            ScoreDoc[] scoreDocs = new ScoreDoc[Math.Min(10, hits.Count)];
            for (int i = 0; i < scoreDocs.Length; i++)
            {
                KeyValuePair<int, JoinScore> hit = hits[i];
                scoreDocs[i] = new ScoreDoc(hit.Key, hit.Value.Score(scoreMode));
            }
            return new TopDocs(hits.Count, scoreDocs, hits.Count == 0 ? float.NaN : hits[0].Value.Score(scoreMode));
        }

        private FixedBitSet CreateExpectedResult(string queryValue, bool from, IndexReader topLevelReader,
            IndexIterationContext context)
        {
            IDictionary<string, IList<RandomDoc>> randomValueDocs;
            IDictionary<string, IList<RandomDoc>> linkValueDocuments;
            if (from)
            {
                randomValueDocs = context.RandomValueFromDocs;
                linkValueDocuments = context.ToDocuments;
            }
            else
            {
                randomValueDocs = context.RandomValueToDocs;
                linkValueDocuments = context.FromDocuments;
            }

            FixedBitSet expectedResult = new FixedBitSet(topLevelReader.MaxDoc);
            if (!randomValueDocs.TryGetValue(queryValue, out IList<RandomDoc> matchingDocs) || matchingDocs is null)
            {
                return new FixedBitSet(topLevelReader.MaxDoc);
            }

            foreach (RandomDoc matchingDoc in matchingDocs)
            {
                foreach (string linkValue in matchingDoc.linkValues)
                {
                    if (!linkValueDocuments.TryGetValue(linkValue, out IList<RandomDoc> otherMatchingDocs) || otherMatchingDocs is null)
                    {
                        continue;
                    }

                    foreach (RandomDoc otherSideDoc in otherMatchingDocs)
                    {
                        DocsEnum docsEnum = MultiFields.GetTermDocsEnum(topLevelReader,
                            MultiFields.GetLiveDocs(topLevelReader), "id", new BytesRef(otherSideDoc.id), 0);
                        if (Debugging.AssertsEnabled) Debugging.Assert(docsEnum != null);
                        int doc = docsEnum.NextDoc();
                        expectedResult.Set(doc);
                    }
                }
            }
            return expectedResult;
        }

        private class IndexIterationContext
        {

            internal string[] RandomUniqueValues { get; set; }
            internal bool[] RandomFrom { get; set; }
            internal IDictionary<string, IList<RandomDoc>> FromDocuments { get; set; } = new Dictionary<string, IList<RandomDoc>>();
            internal IDictionary<string, IList<RandomDoc>> ToDocuments { get; set; } = new Dictionary<string, IList<RandomDoc>>();

            internal IDictionary<string, IList<RandomDoc>> RandomValueFromDocs { get; set; } =
                new Dictionary<string, IList<RandomDoc>>();

            internal IDictionary<string, IList<RandomDoc>> RandomValueToDocs { get; set; } =
                new Dictionary<string, IList<RandomDoc>>();

            internal IDictionary<string, IDictionary<int, JoinScore>> FromHitsToJoinScore { get; set; } =
                new Dictionary<string, IDictionary<int, JoinScore>>();

            internal IDictionary<string, IDictionary<int, JoinScore>> ToHitsToJoinScore { get; set; } =
                new Dictionary<string, IDictionary<int, JoinScore>>();
        }

        private class RandomDoc
        {
            internal readonly string id;
            internal readonly IList<string> linkValues;
            internal readonly string value;
            internal readonly bool @from;

            internal RandomDoc(string id, int numberOfLinkValues, string value, bool from)
            {
                this.id = id;
                this.@from = from;
                linkValues = new List<string>(numberOfLinkValues);
                this.value = value;
            }
        }

        private class JoinScore
        {
            private float maxScore;
            private float total;
            private int count;

            internal virtual void AddScore(float score)
            {
                total += score;
                if (score > maxScore)
                {
                    maxScore = score;
                }
                count++;
            }

            internal virtual float Score(ScoreMode mode)
            {
                switch (mode)
                {
                    case ScoreMode.None:
                        return 1.0f;
                    case ScoreMode.Total:
                        return total;
                    case ScoreMode.Avg:
                        return total / count;
                    case ScoreMode.Max:
                        return maxScore;
                }
                throw new ArgumentException("Unsupported ScoreMode: " + mode);
            }
        }
    }
}