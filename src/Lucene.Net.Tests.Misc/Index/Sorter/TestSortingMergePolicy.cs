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
    public class TestSortingMergePolicy : LuceneTestCase
    {
        private IList<string> terms;
        private Directory dir1, dir2;
        private Sort sort;
        private IndexReader reader;
        private IndexReader sortedReader;


        public override void SetUp()
        {
            base.SetUp();
            sort = new Sort(new SortField("ndv", SortFieldType.INT64));
            CreateRandomIndexes();
        }

        private Document randomDocument()
        {
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("ndv", Random.NextInt64()));
            doc.Add(new StringField("s", RandomPicks.RandomFrom(Random, terms), Field.Store.YES));
            return doc;
        }

        internal static MergePolicy NewSortingMergePolicy(Sort sort)
        {
            // create a MP with a low merge factor so that many merges happen
            MergePolicy mp;
            if (Random.nextBoolean())
            {
                TieredMergePolicy tmp = NewTieredMergePolicy(Random);
                int numSegs = TestUtil.NextInt32(Random, 3, 5);
                tmp.SegmentsPerTier = (numSegs);
                tmp.MaxMergeAtOnce = (TestUtil.NextInt32(Random, 2, numSegs));
                mp = tmp;
            }
            else
            {
                LogMergePolicy lmp = NewLogMergePolicy(Random);
                lmp.MergeFactor = TestUtil.NextInt32(Random, 3, 5);
                mp = lmp;
            }
            // wrap it with a sorting mp
            return new SortingMergePolicy(mp, sort);
        }

        private void CreateRandomIndexes()
        {
            dir1 = NewDirectory();
            dir2 = NewDirectory();
            int numDocs = AtLeast(150);
            int numTerms = TestUtil.NextInt32(Random, 1, numDocs / 5);
            ISet<string> randomTerms = new JCG.HashSet<string>();
            while (randomTerms.size() < numTerms)
            {
                randomTerms.add(TestUtil.RandomSimpleString(Random));
            }
            terms = new JCG.List<string>(randomTerms);
            long seed = Random.NextInt64();
            IndexWriterConfig iwc1 = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(new J2N.Randomizer(seed)));
            IndexWriterConfig iwc2 = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(new J2N.Randomizer(seed)));
            iwc2.SetMergePolicy(NewSortingMergePolicy(sort));
            RandomIndexWriter iw1 = new RandomIndexWriter(new J2N.Randomizer(seed), dir1, iwc1);
            RandomIndexWriter iw2 = new RandomIndexWriter(new J2N.Randomizer(seed), dir2, iwc2);
            for (int i = 0; i < numDocs; ++i)
            {
                if (Random.nextInt(5) == 0 && i != numDocs - 1)
                {
                    string term = RandomPicks.RandomFrom(Random, terms);
                    iw1.DeleteDocuments(new Term("s", term));
                    iw2.DeleteDocuments(new Term("s", term));
                }
                Document doc = randomDocument();
                iw1.AddDocument(doc);
                iw2.AddDocument(doc);
                if (Random.nextInt(8) == 0)
                {
                    iw1.Commit();
                    iw2.Commit();
                }
            }
            // Make sure we have something to merge
            iw1.Commit();
            iw2.Commit();
            Document doc2 = randomDocument();
            // NOTE: don't use RIW.addDocument directly, since it sometimes commits
            // which may trigger a merge, at which case forceMerge may not do anything.
            // With field updates this is a problem, since the updates can go into the
            // single segment in the index, and threefore the index won't be sorted.
            // This hurts the assumption of the test later on, that the index is sorted
            // by SortingMP.
            iw1.IndexWriter.AddDocument(doc2);
            iw2.IndexWriter.AddDocument(doc2);

            if (DefaultCodecSupportsFieldUpdates)
            {
                // update NDV of docs belonging to one term (covers many documents)
                long value = Random.NextInt64();
                string term = RandomPicks.RandomFrom(Random, terms);
                iw1.IndexWriter.UpdateNumericDocValue(new Term("s", term), "ndv", value);
                iw2.IndexWriter.UpdateNumericDocValue(new Term("s", term), "ndv", value);
            }

            iw1.ForceMerge(1);
            iw2.ForceMerge(1);
            iw1.Dispose();
            iw2.Dispose();
            reader = DirectoryReader.Open(dir1);
            sortedReader = DirectoryReader.Open(dir2);
        }

        public override void TearDown()
        {
            reader.Dispose();
            sortedReader.Dispose();
            dir1.Dispose();
            dir2.Dispose();
            base.TearDown();
        }

        private static void AssertSorted(AtomicReader reader)
        {
            NumericDocValues ndv = reader.GetNumericDocValues("ndv");
            for (int i = 1; i < reader.MaxDoc; ++i)
            {
                assertTrue("ndv(" + (i - 1) + ")=" + ndv.Get(i - 1) + ",ndv(" + i + ")=" + ndv.Get(i), ndv.Get(i - 1) <= ndv.Get(i));
            }
        }

        [Test]
        public void TestSortingMP()
        {
            AtomicReader sortedReader1 = SortingAtomicReader.Wrap(SlowCompositeReaderWrapper.Wrap(reader), sort);
            AtomicReader sortedReader2 = SlowCompositeReaderWrapper.Wrap(sortedReader);

            AssertSorted(sortedReader1);
            AssertSorted(sortedReader2);

            AssertReaderEquals("", sortedReader1, sortedReader2);
        }

        [Test]
        public void TestBadSort()
        {
            try
            {
                new SortingMergePolicy(NewMergePolicy(), Sort.RELEVANCE);
                fail("Didn't get expected exception");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                assertEquals("Cannot sort an index with a Sort that refers to the relevance score", e.Message);
            }
        }
    }
}
