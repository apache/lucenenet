using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Join;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

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
     
    public class TestJoinUtil : LuceneTestCase
    {
        [Test]
        public void TestSimple()
        {
            const string idField = "id";
            const string toField = "productId";

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
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

            IndexSearcher indexSearcher = new IndexSearcher(w.Reader);
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
            Test300spartans(Random().NextBoolean(), (ScoreMode) Random().Next(scoreModeLength));
        }

        protected virtual void Test300spartans(bool multipleValues, ScoreMode scoreMode)
        {
            const string idField = "id";
            const string toField = "productId";

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
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

            IndexSearcher indexSearcher = new IndexSearcher(w.Reader);
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
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
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

            IndexSearcher indexSearcher = new IndexSearcher(w.Reader);
            w.Dispose();

            // Search for product
            Query joinQuery = JoinUtil.CreateJoinQuery(idField, false, toField,
                new TermQuery(new Term("description", "random")), indexSearcher, ScoreMode.Avg);

            BooleanQuery bq = new BooleanQuery();
            bq.Add(joinQuery, Occur.SHOULD);
            bq.Add(new TermQuery(new Term("id", "3")), Occur.SHOULD);

            indexSearcher.Search(bq, new CollectorAnonymousInnerClassHelper(this));

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        private class CollectorAnonymousInnerClassHelper : Collector
        {
            private readonly TestJoinUtil OuterInstance;

            public CollectorAnonymousInnerClassHelper(TestJoinUtil outerInstance)
            {
                OuterInstance = outerInstance;
            }

            internal bool sawFive;

            public override void SetNextReader(AtomicReaderContext context)
            {
            }

            public override void Collect(int docID)
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

            public override void SetScorer(Scorer scorer)
            {
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }

        [Test]
        public void TestSimpleWithScoring()
        {
            const string idField = "id";
            const string toField = "movieId";

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
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

            IndexSearcher indexSearcher = new IndexSearcher(w.Reader);
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
        public void TestSingleValueRandomJoin()
        {
            int maxIndexIter = TestUtil.NextInt(Random(), 6, 12);
            int maxSearchIter = TestUtil.NextInt(Random(), 13, 26);
            ExecuteRandomJoin(false, maxIndexIter, maxSearchIter, TestUtil.NextInt(Random(), 87, 764));
        }

        [Test]
        public void TestMultiValueRandomJoin()
            // this test really takes more time, that is why the number of iterations are smaller.
        {
            int maxIndexIter = TestUtil.NextInt(Random(), 3, 6);
            int maxSearchIter = TestUtil.NextInt(Random(), 6, 12);
            ExecuteRandomJoin(true, maxIndexIter, maxSearchIter, TestUtil.NextInt(Random(), 11, 57));
        }
        
        private void ExecuteRandomJoin(bool multipleValuesPerDocument, int maxIndexIter, int maxSearchIter,
            int numberOfDocumentsToIndex)
        {
            for (int indexIter = 1; indexIter <= maxIndexIter; indexIter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("indexIter=" + indexIter);
                }
                Directory dir = NewDirectory();
                RandomIndexWriter w = new RandomIndexWriter(Random(), dir,
                    NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false))
                        .SetMergePolicy(NewLogMergePolicy()));
                bool scoreDocsInOrder = TestJoinUtil.Random().NextBoolean();
                IndexIterationContext context = CreateContext(numberOfDocumentsToIndex, w, multipleValuesPerDocument,
                    scoreDocsInOrder);

                IndexReader topLevelReader = w.Reader;
                w.Dispose();
                for (int searchIter = 1; searchIter <= maxSearchIter; searchIter++)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("searchIter=" + searchIter);
                    }
                    IndexSearcher indexSearcher = NewSearcher(topLevelReader);

                    int r = Random().Next(context.RandomUniqueValues.Length);
                    bool from = context.RandomFrom[r];
                    string randomValue = context.RandomUniqueValues[r];
                    FixedBitSet expectedResult = CreateExpectedResult(randomValue, from, indexSearcher.IndexReader,
                        context);

                    Query actualQuery = new TermQuery(new Term("value", randomValue));
                    if (VERBOSE)
                    {
                        Console.WriteLine("actualQuery=" + actualQuery);
                    }
                    
                    var scoreModeLength = Enum.GetNames(typeof(ScoreMode)).Length;
                    ScoreMode scoreMode = (ScoreMode) Random().Next(scoreModeLength);
                    if (VERBOSE)
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
                    if (VERBOSE)
                    {
                        Console.WriteLine("joinQuery=" + joinQuery);
                    }

                    // Need to know all documents that have matches. TopDocs doesn't give me that and then I'd be also testing TopDocsCollector...
                    FixedBitSet actualResult = new FixedBitSet(indexSearcher.IndexReader.MaxDoc);
                    TopScoreDocCollector topScoreDocCollector = TopScoreDocCollector.Create(10, false);
                    indexSearcher.Search(joinQuery,
                        new CollectorAnonymousInnerClassHelper2(this, scoreDocsInOrder, context, actualResult,
                            topScoreDocCollector));
                    // Asserting bit set...
                    if (VERBOSE)
                    {
                        Console.WriteLine("expected cardinality:" + expectedResult.Cardinality());
                        DocIdSetIterator iterator = expectedResult.GetIterator();
                        for (int doc = iterator.NextDoc();
                            doc != DocIdSetIterator.NO_MORE_DOCS;
                            doc = iterator.NextDoc())
                        {
                            Console.WriteLine(string.Format("Expected doc[{0}] with id value {1}", doc, indexSearcher.Doc(doc).Get("id")));
                        }
                        Console.WriteLine("actual cardinality:" + actualResult.Cardinality());
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
                    TopDocs actualTopDocs = topScoreDocCollector.TopDocs();
                    assertEquals(expectedTopDocs.TotalHits, actualTopDocs.TotalHits);
                    assertEquals(expectedTopDocs.ScoreDocs.Length, actualTopDocs.ScoreDocs.Length);
                    if (scoreMode == ScoreMode.None)
                    {
                        continue;
                    }

                    assertEquals(expectedTopDocs.MaxScore, actualTopDocs.MaxScore, 0.0f);
                    for (int i = 0; i < expectedTopDocs.ScoreDocs.Length; i++)
                    {
                        if (VERBOSE)
                        {
                            string.Format("Expected doc: {0} | Actual doc: {1}\n", expectedTopDocs.ScoreDocs[i].Doc, actualTopDocs.ScoreDocs[i].Doc);
                            string.Format("Expected score: {0} | Actual score: {1}\n", expectedTopDocs.ScoreDocs[i].Score, actualTopDocs.ScoreDocs[i].Score);
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

        private class CollectorAnonymousInnerClassHelper2 : Collector
        {
            private readonly TestJoinUtil OuterInstance;

            private bool ScoreDocsInOrder;
            private IndexIterationContext Context;
            private FixedBitSet ActualResult;
            private TopScoreDocCollector TopScoreDocCollector;

            public CollectorAnonymousInnerClassHelper2(TestJoinUtil outerInstance, bool scoreDocsInOrder,
                IndexIterationContext context, FixedBitSet actualResult,
                TopScoreDocCollector topScoreDocCollector)
            {
                OuterInstance = outerInstance;
                ScoreDocsInOrder = scoreDocsInOrder;
                Context = context;
                ActualResult = actualResult;
                TopScoreDocCollector = topScoreDocCollector;
            }


            private int _docBase;
            
            public override void Collect(int doc)
            {
                ActualResult.Set(doc + _docBase);
                TopScoreDocCollector.Collect(doc);
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                _docBase = context.DocBase;
                TopScoreDocCollector.SetNextReader(context);
            }
            
            public override void SetScorer(Scorer scorer)
            {
                TopScoreDocCollector.SetScorer(scorer);
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return ScoreDocsInOrder; }
            }
        }
        
        private IndexIterationContext CreateContext(int nDocs, RandomIndexWriter writer, bool multipleValuesPerDocument,
            bool scoreDocsInOrder)
        {
            return CreateContext(nDocs, writer, writer, multipleValuesPerDocument, scoreDocsInOrder);
        }
        
        private IndexIterationContext CreateContext(int nDocs, RandomIndexWriter fromWriter, RandomIndexWriter toWriter,
            bool multipleValuesPerDocument, bool scoreDocsInOrder)
        {
            IndexIterationContext context = new IndexIterationContext();
            int numRandomValues = nDocs/2;
            context.RandomUniqueValues = new string[numRandomValues];
            ISet<string> trackSet = new HashSet<string>();
            context.RandomFrom = new bool[numRandomValues];
            for (int i = 0; i < numRandomValues; i++)
            {
                string uniqueRandomValue;
                do
                {
                    uniqueRandomValue = TestUtil.RandomRealisticUnicodeString(Random());
                    //        uniqueRandomValue = TestUtil.randomSimpleString(random);
                } while ("".Equals(uniqueRandomValue) || trackSet.Contains(uniqueRandomValue));
                // Generate unique values and empty strings aren't allowed.
                trackSet.Add(uniqueRandomValue);
                context.RandomFrom[i] = Random().NextBoolean();
                context.RandomUniqueValues[i] = uniqueRandomValue;
            }

            RandomDoc[] docs = new RandomDoc[nDocs];
            for (int i = 0; i < nDocs; i++)
            {
                string id = Convert.ToString(i);
                int randomI = Random().Next(context.RandomUniqueValues.Length);
                string value = context.RandomUniqueValues[randomI];
                Document document = new Document();
                document.Add(NewTextField(Random(), "id", id, Field.Store.NO));
                document.Add(NewTextField(Random(), "value", value, Field.Store.NO));

                bool from = context.RandomFrom[randomI];
                int numberOfLinkValues = multipleValuesPerDocument ? 2 + Random().Next(10) : 1;
                docs[i] = new RandomDoc(id, numberOfLinkValues, value, from);
                for (int j = 0; j < numberOfLinkValues; j++)
                {
                    string linkValue = context.RandomUniqueValues[Random().Next(context.RandomUniqueValues.Length)];
                    docs[i].LinkValues.Add(linkValue);
                    if (from)
                    {
                        if (!context.FromDocuments.ContainsKey(linkValue))
                        {
                            context.FromDocuments[linkValue] = new List<RandomDoc>();
                        }
                        if (!context.RandomValueFromDocs.ContainsKey(value))
                        {
                            context.RandomValueFromDocs[value] = new List<RandomDoc>();
                        }

                        context.FromDocuments[linkValue].Add(docs[i]);
                        context.RandomValueFromDocs[value].Add(docs[i]);
                        document.Add(NewTextField(Random(), "from", linkValue, Field.Store.NO));
                    }
                    else
                    {
                        if (!context.ToDocuments.ContainsKey(linkValue))
                        {
                            context.ToDocuments[linkValue] = new List<RandomDoc>();
                        }
                        if (!context.RandomValueToDocs.ContainsKey(value))
                        {
                            context.RandomValueToDocs[value] = new List<RandomDoc>();
                        }

                        context.ToDocuments[linkValue].Add(docs[i]);
                        context.RandomValueToDocs[value].Add(docs[i]);
                        document.Add(NewTextField(Random(), "to", linkValue, Field.Store.NO));
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
                if (Random().Next(10) == 4)
                {
                    w.Commit();
                }
                if (VERBOSE)
                {
                    Console.WriteLine("Added document[" + docs[i].Id + "]: " + document);
                }
            }

            // Pre-compute all possible hits for all unique random values. On top of this also compute all possible score for
            // any ScoreMode.
            IndexSearcher fromSearcher = NewSearcher(fromWriter.Reader);
            IndexSearcher toSearcher = NewSearcher(toWriter.Reader);
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
                        new CollectorAnonymousInnerClassHelper3(this, context, fromField, joinValueToJoinScores));
                }
                else
                {
                    fromSearcher.Search(new TermQuery(new Term("value", uniqueRandomValue)),
                        new CollectorAnonymousInnerClassHelper4(this, context, fromField, joinValueToJoinScores));
                }

                IDictionary<int, JoinScore> docToJoinScore = new Dictionary<int, JoinScore>();
                if (multipleValuesPerDocument)
                {
                    if (scoreDocsInOrder)
                    {
                        AtomicReader slowCompositeReader = SlowCompositeReaderWrapper.Wrap(toSearcher.IndexReader);
                        Terms terms = slowCompositeReader.Terms(toField);
                        if (terms != null)
                        {
                            DocsEnum docsEnum = null;
                            TermsEnum termsEnum = null;
                            SortedSet<BytesRef> joinValues =
                                new SortedSet<BytesRef>(BytesRef.UTF8SortedAsUnicodeComparer);
                            joinValues.AddAll(joinValueToJoinScores.Keys);
                            foreach (BytesRef joinValue in joinValues)
                            {
                                termsEnum = terms.Iterator(termsEnum);
                                if (termsEnum.SeekExact(joinValue))
                                {
                                    docsEnum = termsEnum.Docs(slowCompositeReader.LiveDocs, docsEnum, DocsEnum.FLAG_NONE);
                                    JoinScore joinScore = joinValueToJoinScores[joinValue];

                                    for (int doc = docsEnum.NextDoc();
                                        doc != DocIdSetIterator.NO_MORE_DOCS;
                                        doc = docsEnum.NextDoc())
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
                            new CollectorAnonymousInnerClassHelper5(this, context, toField, joinValueToJoinScores,
                                docToJoinScore));
                    }
                }
                else
                {
                    toSearcher.Search(new MatchAllDocsQuery(),
                        new CollectorAnonymousInnerClassHelper6(this, context, toField, joinValueToJoinScores,
                            docToJoinScore));
                }
                queryVals[uniqueRandomValue] = docToJoinScore;
            }

            fromSearcher.IndexReader.Dispose();
            toSearcher.IndexReader.Dispose();

            return context;
        }

        private class CollectorAnonymousInnerClassHelper3 : Collector
        {
            private readonly TestJoinUtil OuterInstance;

            private IndexIterationContext Context;
            private string FromField;
            private IDictionary<BytesRef, JoinScore> JoinValueToJoinScores;

            public CollectorAnonymousInnerClassHelper3(TestJoinUtil outerInstance,
                IndexIterationContext context, string fromField,
                IDictionary<BytesRef, JoinScore> joinValueToJoinScores)
            {
                OuterInstance = outerInstance;
                Context = context;
                FromField = fromField;
                JoinValueToJoinScores = joinValueToJoinScores;
                joinValue = new BytesRef();
            }


            private Scorer scorer;
            private SortedSetDocValues docTermOrds;
            internal readonly BytesRef joinValue;
            
            public override void Collect(int doc)
            {
                docTermOrds.SetDocument(doc);
                long ord;
                while ((ord = docTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                {
                    docTermOrds.LookupOrd(ord, joinValue);
                    var joinScore = JoinValueToJoinScores.ContainsKey(joinValue) ? JoinValueToJoinScores[joinValue] : null;
                    if (joinScore == null)
                    {
                        JoinValueToJoinScores[BytesRef.DeepCopyOf(joinValue)] = joinScore = new JoinScore();
                    }
                    joinScore.AddScore(scorer.Score());
                }
            }
            
            public override void SetNextReader(AtomicReaderContext context)
            {
                docTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.AtomicReader, FromField);
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return false; }
            }
        }

        private class CollectorAnonymousInnerClassHelper4 : Collector
        {
            private readonly TestJoinUtil OuterInstance;

            private IndexIterationContext Context;
            private string FromField;
            private IDictionary<BytesRef, JoinScore> JoinValueToJoinScores;

            public CollectorAnonymousInnerClassHelper4(TestJoinUtil outerInstance,
                IndexIterationContext context, string fromField,
                IDictionary<BytesRef, JoinScore> joinValueToJoinScores)
            {
                OuterInstance = outerInstance;
                Context = context;
                FromField = fromField;
                JoinValueToJoinScores = joinValueToJoinScores;
                spare = new BytesRef();
            }


            private Scorer scorer;
            private BinaryDocValues terms;
            private Bits docsWithField;
            private readonly BytesRef spare;
            
            public override void Collect(int doc)
            {
                terms.Get(doc, spare);
                BytesRef joinValue = spare;
                if (joinValue.Length == 0 && !docsWithField.Get(doc))
                {
                    return;
                }

                var joinScore = JoinValueToJoinScores.ContainsKey(joinValue) ? JoinValueToJoinScores[joinValue] : null;
                if (joinScore == null)
                {
                    JoinValueToJoinScores[BytesRef.DeepCopyOf(joinValue)] = joinScore = new JoinScore();
                }
                joinScore.AddScore(scorer.Score());
            }
            
            public override void SetNextReader(AtomicReaderContext context)
            {
                terms = FieldCache.DEFAULT.GetTerms(context.AtomicReader, FromField, true);
                docsWithField = FieldCache.DEFAULT.GetDocsWithField(context.AtomicReader, FromField);
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return false; }
            }
        }

        private class CollectorAnonymousInnerClassHelper5 : Collector
        {
            private readonly TestJoinUtil OuterInstance;

            private string _toField;
            private readonly IDictionary<BytesRef, JoinScore> _joinValueToJoinScores;
            private readonly IDictionary<int, JoinScore> _docToJoinScore;

            private SortedSetDocValues docTermOrds;
            private readonly BytesRef scratch = new BytesRef();
            private int docBase;

            public CollectorAnonymousInnerClassHelper5(TestJoinUtil testJoinUtil, IndexIterationContext context, 
                string toField, IDictionary<BytesRef, JoinScore> joinValueToJoinScores, 
                IDictionary<int, JoinScore> docToJoinScore)
            {
                OuterInstance = testJoinUtil;
                _toField = toField;
                _joinValueToJoinScores = joinValueToJoinScores;
                _docToJoinScore = docToJoinScore;
            }

            public override void Collect(int doc)
            {
                docTermOrds.SetDocument(doc);
                long ord;
                while ((ord = docTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                {
                    docTermOrds.LookupOrd(ord, scratch);
                    JoinScore joinScore = _joinValueToJoinScores.ContainsKey(scratch) ? _joinValueToJoinScores[scratch] : null;
                    if (joinScore == null)
                    {
                        continue;
                    }
                    int basedDoc = docBase + doc;
                    // First encountered join value determines the score.
                    // Something to keep in mind for many-to-many relations.
                    if (!_docToJoinScore.ContainsKey(basedDoc))
                    {
                        _docToJoinScore[basedDoc] = joinScore;
                    }
                }
            }
            
            public override void SetNextReader(AtomicReaderContext context)
            {
                docBase = context.DocBase;
                docTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.AtomicReader, _toField);
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return false; }
            }

            public override void SetScorer(Scorer scorer)
            {
            }
        }

        private class CollectorAnonymousInnerClassHelper6 : Collector
        {
            private readonly TestJoinUtil OuterInstance;

            private IndexIterationContext Context;
            private string ToField;
            private IDictionary<BytesRef, JoinScore> JoinValueToJoinScores;
            private IDictionary<int, JoinScore> DocToJoinScore;

            private BinaryDocValues terms;
            private int docBase;
            private readonly BytesRef spare = new BytesRef();

            public CollectorAnonymousInnerClassHelper6(TestJoinUtil testJoinUtil, 
                IndexIterationContext context, string toField, 
                IDictionary<BytesRef, JoinScore> joinValueToJoinScores, 
                IDictionary<int, JoinScore> docToJoinScore)
            {
                OuterInstance = testJoinUtil;
                ToField = toField;
                JoinValueToJoinScores = joinValueToJoinScores;
                DocToJoinScore = docToJoinScore;
            }

            public override void Collect(int doc)
            {
                terms.Get(doc, spare);
                JoinScore joinScore = JoinValueToJoinScores.ContainsKey(spare) ? JoinValueToJoinScores[spare] : null;
                if (joinScore == null)
                {
                    return;
                }
                DocToJoinScore[docBase + doc] = joinScore;
            }
            
            public override void SetNextReader(AtomicReaderContext context)
            {
                terms = FieldCache.DEFAULT.GetTerms(context.AtomicReader, ToField, false);
                docBase = context.DocBase;
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return false; }
            }

            public override void SetScorer(Scorer scorer)
            {
            }
        }

        private TopDocs CreateExpectedTopDocs(string queryValue, bool from, ScoreMode scoreMode,
            IndexIterationContext context)
        {
            var hitsToJoinScores = @from
                ? context.FromHitsToJoinScore[queryValue]
                : context.ToHitsToJoinScore[queryValue];

            var hits = new List<KeyValuePair<int, JoinScore>>(hitsToJoinScores.EntrySet());
            hits.Sort(new ComparatorAnonymousInnerClassHelper(this, scoreMode));
            ScoreDoc[] scoreDocs = new ScoreDoc[Math.Min(10, hits.Count)];
            for (int i = 0; i < scoreDocs.Length; i++)
            {
                KeyValuePair<int, JoinScore> hit = hits[i];
                scoreDocs[i] = new ScoreDoc(hit.Key, hit.Value.Score(scoreMode));
            }
            return new TopDocs(hits.Count, scoreDocs, hits.Count == 0 ? float.NaN : hits[0].Value.Score(scoreMode));
        }

        private class ComparatorAnonymousInnerClassHelper : IComparer<KeyValuePair<int, JoinScore>>
        {
            private readonly TestJoinUtil OuterInstance;

            private ScoreMode ScoreMode;

            public ComparatorAnonymousInnerClassHelper(TestJoinUtil outerInstance, ScoreMode scoreMode)
            {
                OuterInstance = outerInstance;
                ScoreMode = scoreMode;
            }

            public virtual int Compare(KeyValuePair<int, JoinScore> hit1, KeyValuePair<int, JoinScore> hit2)
            {
                float score1 = hit1.Value.Score(ScoreMode);
                float score2 = hit2.Value.Score(ScoreMode);

                int cmp = score2.CompareTo(score1);
                if (cmp != 0)
                {
                    return cmp;
                }
                return hit1.Key - hit2.Key;
            }
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
            IList<RandomDoc> matchingDocs = randomValueDocs.ContainsKey(queryValue) ? randomValueDocs[queryValue] : null;
            if (matchingDocs == null)
            {
                return new FixedBitSet(topLevelReader.MaxDoc);
            }

            foreach (RandomDoc matchingDoc in matchingDocs)
            {
                foreach (string linkValue in matchingDoc.LinkValues)
                {
                    IList<RandomDoc> otherMatchingDocs = linkValueDocuments.ContainsKey(linkValue) ? linkValueDocuments[linkValue] : null;
                    if (otherMatchingDocs == null)
                    {
                        continue;
                    }

                    foreach (RandomDoc otherSideDoc in otherMatchingDocs)
                    {
                        DocsEnum docsEnum = MultiFields.GetTermDocsEnum(topLevelReader,
                            MultiFields.GetLiveDocs(topLevelReader), "id", new BytesRef(otherSideDoc.Id), 0);
                        Debug.Assert(docsEnum != null);
                        int doc = docsEnum.NextDoc();
                        expectedResult.Set(doc);
                    }
                }
            }
            return expectedResult;
        }

        private class IndexIterationContext
        {

            internal string[] RandomUniqueValues;
            internal bool[] RandomFrom;
            internal IDictionary<string, IList<RandomDoc>> FromDocuments = new Dictionary<string, IList<RandomDoc>>();
            internal IDictionary<string, IList<RandomDoc>> ToDocuments = new Dictionary<string, IList<RandomDoc>>();

            internal IDictionary<string, IList<RandomDoc>> RandomValueFromDocs =
                new Dictionary<string, IList<RandomDoc>>();

            internal IDictionary<string, IList<RandomDoc>> RandomValueToDocs =
                new Dictionary<string, IList<RandomDoc>>();

            internal IDictionary<string, IDictionary<int, JoinScore>> FromHitsToJoinScore =
                new Dictionary<string, IDictionary<int, JoinScore>>();

            internal IDictionary<string, IDictionary<int, JoinScore>> ToHitsToJoinScore =
                new Dictionary<string, IDictionary<int, JoinScore>>();
        }

        private class RandomDoc
        {
            internal readonly string Id;
            internal readonly IList<string> LinkValues;
            internal readonly string Value;
            internal readonly bool From;

            internal RandomDoc(string id, int numberOfLinkValues, string value, bool from)
            {
                Id = id;
                From = from;
                LinkValues = new List<string>(numberOfLinkValues);
                Value = value;
            }
        }

        private class JoinScore
        {
            internal float MaxScore;
            internal float Total;
            internal int Count;

            internal virtual void AddScore(float score)
            {
                Total += score;
                if (score > MaxScore)
                {
                    MaxScore = score;
                }
                Count++;
            }

            internal virtual float Score(ScoreMode mode)
            {
                switch (mode)
                {
                    case ScoreMode.None:
                        return 1.0f;
                    case ScoreMode.Total:
                        return Total;
                    case ScoreMode.Avg:
                        return Total/Count;
                    case ScoreMode.Max:
                        return MaxScore;
                }
                throw new ArgumentException("Unsupported ScoreMode: " + mode);
            }
        }
    }
}