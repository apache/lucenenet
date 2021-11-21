using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index.Sorter
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
    public class TestEarlyTermination : LuceneTestCase
    {
        private int numDocs;
        private IList<string> terms;
        private Directory dir;
        private Sort sort;
        private RandomIndexWriter iw;
        private IndexReader reader;

        public override void SetUp()
        {
            base.SetUp();
            sort = new Sort(new SortField("ndv1", SortFieldType.INT64));
        }

        private Document RandomDocument()
        {
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("ndv1", Random.nextInt(10)));
            doc.Add(new NumericDocValuesField("ndv2", Random.nextInt(10)));
            doc.Add(new StringField("s", RandomPicks.RandomFrom(Random, terms), Field.Store.YES));
            return doc;
        }

        private void CreateRandomIndexes(int maxSegments)
        {
            dir = NewDirectory();
            numDocs = AtLeast(150);
            int numTerms = TestUtil.NextInt32(Random, 1, numDocs / 5);
            ISet<string> randomTerms = new JCG.HashSet<string>();
            while (randomTerms.size() < numTerms)
            {
                randomTerms.add(TestUtil.RandomSimpleString(Random));
            }
            terms = new JCG.List<string>(randomTerms);
            long seed = Random.NextInt64();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(new J2N.Randomizer(seed)));
            iwc.SetMergePolicy(TestSortingMergePolicy.NewSortingMergePolicy(sort));
            iw = new RandomIndexWriter(new J2N.Randomizer(seed), dir, iwc);
            for (int i = 0; i < numDocs; ++i)
            {
                Document doc = RandomDocument();
                iw.AddDocument(doc);
                if (i == numDocs / 2 || (i != numDocs - 1 && Random.nextInt(8) == 0))
                {
                    iw.Commit();
                }
                if (Random.nextInt(15) == 0)
                {
                    string term = RandomPicks.RandomFrom(Random, terms);
                    iw.DeleteDocuments(new Term("s", term));
                }
            }
            reader = iw.GetReader();
        }

        public override void TearDown()
        {
            reader.Dispose();
            iw.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public void TestEarlyTermination_()
        {
            CreateRandomIndexes(5);
            int numHits = TestUtil.NextInt32(Random, 1, numDocs / 10);
            Sort sort = new Sort(new SortField("ndv1", SortFieldType.INT64, false));
            bool fillFields = Random.nextBoolean();
            bool trackDocScores = Random.nextBoolean();
            bool trackMaxScore = Random.nextBoolean();
            bool inOrder = Random.nextBoolean();
            TopFieldCollector collector1 = Search.TopFieldCollector.Create(sort, numHits, fillFields, trackDocScores, trackMaxScore, inOrder);
            TopFieldCollector collector2 = Search.TopFieldCollector.Create(sort, numHits, fillFields, trackDocScores, trackMaxScore, inOrder);

            IndexSearcher searcher = NewSearcher(reader);
            int iters = AtLeast(5);
            for (int i = 0; i < iters; ++i)
            {
                TermQuery query = new TermQuery(new Term("s", RandomPicks.RandomFrom(Random, terms)));
                searcher.Search(query, collector1);
                searcher.Search(query, new EarlyTerminatingSortingCollector(collector2, sort, numHits));
            }
            assertTrue(collector1.TotalHits >= collector2.TotalHits);
            AssertTopDocsEquals(collector1.GetTopDocs().ScoreDocs, collector2.GetTopDocs().ScoreDocs);
        }

        [Test]
        public void TestEarlyTerminationDifferentSorter()
        {
            // test that the collector works correctly when the index was sorted by a
            // different sorter than the one specified in the ctor.
            CreateRandomIndexes(5);
            int numHits = TestUtil.NextInt32(Random, 1, numDocs / 10);
            Sort sort = new Sort(new SortField("ndv2", SortFieldType.INT64, false));
            bool fillFields = Random.nextBoolean();
            bool trackDocScores = Random.nextBoolean();
            bool trackMaxScore = Random.nextBoolean();
            bool inOrder = Random.nextBoolean();
            // LUCENENET specific:
            // we are changing this test to use Lucene.Net 4.9-like behavior rather than going through all of the effort to
            // fix a hard-to-find null reference exception problem.
            // https://github.com/apache/lucene-solr/commit/c59f13f9918faeeb4e69acd41731e674ce88f912
            //TopFieldCollector collector1 = TopFieldCollector.Create(sort, numHits, fillFields, trackDocScores, trackMaxScore, inOrder);
            //TopFieldCollector collector2 = TopFieldCollector.Create(sort, numHits, fillFields, trackDocScores, trackMaxScore, inOrder);

            IndexSearcher searcher = NewSearcher(reader);
            int iters = AtLeast(5);
            for (int i = 0; i < iters; ++i)
            {
                // LUCENENET specific:
                // we are changing this test to use Lucene.Net 4.9-like behavior rather than going through all of the effort to
                // fix a hard-to-find null reference exception problem.
                // https://github.com/apache/lucene-solr/commit/c59f13f9918faeeb4e69acd41731e674ce88f912
                TopFieldCollector collector1 = TopFieldCollector.Create(sort, numHits, fillFields, trackDocScores, trackMaxScore, inOrder);
                TopFieldCollector collector2 = TopFieldCollector.Create(sort, numHits, fillFields, trackDocScores, trackMaxScore, inOrder);

                TermQuery query = new TermQuery(new Term("s", RandomPicks.RandomFrom(Random, terms)));
                searcher.Search(query, collector1);
                Sort different = new Sort(new SortField("ndv2", SortFieldType.INT64));
                searcher.Search(query, new EarlyTerminatingSortingCollectorHelper(collector2, different, numHits));


                assertTrue(collector1.TotalHits >= collector2.TotalHits);
                AssertTopDocsEquals(collector1.GetTopDocs().ScoreDocs, collector2.GetTopDocs().ScoreDocs);
            }
        }

        internal class EarlyTerminatingSortingCollectorHelper : EarlyTerminatingSortingCollector
        {
            public EarlyTerminatingSortingCollectorHelper(ICollector @in, Sort sort, int numDocsToCollect)
                : base(@in, sort, numDocsToCollect)
            {
            }
            public override void SetNextReader(AtomicReaderContext context)
            {
                base.SetNextReader(context);
                assertFalse("segment should not be recognized as sorted as different sorter was used", m_segmentSorted);
            }
        }


        private static void AssertTopDocsEquals(ScoreDoc[] scoreDocs1, ScoreDoc[] scoreDocs2)
        {
            assertEquals(scoreDocs1.Length, scoreDocs2.Length);
            for (int i = 0; i < scoreDocs1.Length; ++i)
            {
                ScoreDoc scoreDoc1 = scoreDocs1[i];
                ScoreDoc scoreDoc2 = scoreDocs2[i];
                assertEquals(scoreDoc1.Doc, scoreDoc2.Doc);
                assertEquals(scoreDoc1.Score, scoreDoc2.Score, 0.001f);
            }
        }
    }
}
