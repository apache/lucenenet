// Lucene version compatibility level 4.8.1
using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Tests.Queries
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

    public class CommonTermsQueryTest : LuceneTestCase
    {
        [Test]
        public void TestBasics()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random);
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, analyzer);
            var docs = new string[]
            {
                @"this is the end of the world right",
                @"is this it or maybe not",
                @"this is the end of the universe as we know it",
                @"there is the famous restaurant at the end of the universe"
            };

            for (int i = 0; i < docs.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField(@"id", @"" + i, Field.Store.YES));
                doc.Add(NewTextField(@"field", docs[i], Field.Store.NO));
                w.AddDocument(doc);
            }

            IndexReader r = w.GetReader();
            IndexSearcher s = NewSearcher(r);
            {
                CommonTermsQuery query = new CommonTermsQuery(Occur.SHOULD, Occur.SHOULD, Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 3);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
                assertEquals(@"2", r.Document(search.ScoreDocs[1].Doc).Get(@"id"));
                assertEquals(@"3", r.Document(search.ScoreDocs[2].Doc).Get(@"id"));
            }

            { // only high freq
                CommonTermsQuery query = new CommonTermsQuery(Occur.SHOULD, Occur.SHOULD, Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 2);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
                assertEquals(@"2", r.Document(search.ScoreDocs[1].Doc).Get(@"id"));
            }

            { // low freq is mandatory
                CommonTermsQuery query = new CommonTermsQuery(Occur.SHOULD, Occur.MUST, Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));

                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 1);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
            }

            { // low freq is mandatory
                CommonTermsQuery query = new CommonTermsQuery(Occur.SHOULD, Occur.MUST, Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "restaurant"));
                query.Add(new Term("field", "universe"));

                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 1);
                assertEquals(@"3", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
            }

            r.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestEqualsHashCode()
        {
            CommonTermsQuery query = new CommonTermsQuery(RandomOccur(Random),
                RandomOccur(Random), Random.NextSingle(), Random.NextBoolean());
            int terms = AtLeast(2);
            for (int i = 0; i < terms; i++)
            {
                query.Add(new Term(TestUtil.RandomRealisticUnicodeString(Random),
                    TestUtil.RandomRealisticUnicodeString(Random)));
            }

            QueryUtils.CheckHashEquals(query);
            QueryUtils.CheckUnequal(new CommonTermsQuery(RandomOccur(Random),
                RandomOccur(Random), Random.NextSingle(), Random.NextBoolean()), query);
            {
                long seed = Random.NextInt64();
                Random r = new J2N.Randomizer(seed);
                CommonTermsQuery left = new CommonTermsQuery(RandomOccur(r),
                    RandomOccur(r), r.NextSingle(), r.NextBoolean());
                int leftTerms = AtLeast(r, 2);
                for (int i = 0; i < leftTerms; i++)
                {
                    left.Add(new Term(TestUtil.RandomRealisticUnicodeString(r),
                        TestUtil.RandomRealisticUnicodeString(r)));
                }

                left.HighFreqMinimumNumberShouldMatch = r.nextInt(4);
                left.LowFreqMinimumNumberShouldMatch = r.nextInt(4);
                r = new J2N.Randomizer(seed);
                CommonTermsQuery right = new CommonTermsQuery(RandomOccur(r),
                    RandomOccur(r), r.NextSingle(), r.NextBoolean());
                int rightTerms = AtLeast(r, 2);
                for (int i = 0; i < rightTerms; i++)
                {
                    right.Add(new Term(TestUtil.RandomRealisticUnicodeString(r),
                        TestUtil.RandomRealisticUnicodeString(r)));
                }

                right.HighFreqMinimumNumberShouldMatch = r.nextInt(4);
                right.LowFreqMinimumNumberShouldMatch = r.nextInt(4);
                QueryUtils.CheckEqual(left, right);
            }
        }

        private static Occur RandomOccur(Random random)
        {
            return random.NextBoolean() ? Occur.MUST : Occur.SHOULD;
        }

        [Test]
        public void TestNullTerm()
        {
            Random random = Random;
            CommonTermsQuery query = new CommonTermsQuery(RandomOccur(random),
                RandomOccur(random), Random.NextSingle());
            try
            {
                query.Add(null);
                Assert.Fail(@"null values are not supported");
            }
            catch (ArgumentNullException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            {
            }
        }

        [Test]
        public void TestMinShouldMatch()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random);
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, analyzer);
            string[] docs = new string[]
            {
                @"this is the end of the world right",
                @"is this it or maybe not",
                @"this is the end of the universe as we know it",
                @"there is the famous restaurant at the end of the universe"
            };

            for (int i = 0; i < docs.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField(@"id", @"" + i, Field.Store.YES));
                doc.Add(NewTextField(@"field", docs[i], Field.Store.NO));
                w.AddDocument(doc);
            }

            IndexReader r = w.GetReader();
            IndexSearcher s = NewSearcher(r);
            {
                CommonTermsQuery query = new CommonTermsQuery(Occur.SHOULD, Occur.SHOULD,
                    Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                query.LowFreqMinimumNumberShouldMatch = 0.5f;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 1);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(Occur.SHOULD, Occur.SHOULD,
                    Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                query.LowFreqMinimumNumberShouldMatch = 2.0f;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 1);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(Occur.SHOULD, Occur.SHOULD,
                    Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                query.LowFreqMinimumNumberShouldMatch = 0.49f;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 3);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
                assertEquals(@"2", r.Document(search.ScoreDocs[1].Doc).Get(@"id"));
                assertEquals(@"3", r.Document(search.ScoreDocs[2].Doc).Get(@"id"));
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(Occur.SHOULD, Occur.SHOULD,
                    Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                query.LowFreqMinimumNumberShouldMatch = 1.0f;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 3);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
                assertEquals(@"2", r.Document(search.ScoreDocs[1].Doc).Get(@"id"));
                assertEquals(@"3", r.Document(search.ScoreDocs[2].Doc).Get(@"id"));
                assertTrue(search.ScoreDocs[1].Score > search.ScoreDocs[2].Score);
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(Occur.SHOULD, Occur.SHOULD,
                    Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                query.LowFreqMinimumNumberShouldMatch = 1.0f;
                query.HighFreqMinimumNumberShouldMatch = 4.0f;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 3);
                assertEquals(search.ScoreDocs[1].Score, search.ScoreDocs[2].Score, 0.0f);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
                // doc 2 and 3 only get a score from low freq terms
                assertEquals(
                    new JCG.HashSet<string> { @"2", @"3" },
                    new JCG.HashSet<string> {
                        r.Document(search.ScoreDocs[1].Doc).Get(@"id"),
                        r.Document(search.ScoreDocs[2].Doc).Get(@"id") },
                    aggressive: false);
            }

            {
                // only high freq terms around - check that min should match is applied
                CommonTermsQuery query = new CommonTermsQuery(Occur.SHOULD, Occur.SHOULD,
                    Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "the"));
                query.LowFreqMinimumNumberShouldMatch = 1.0f;
                query.HighFreqMinimumNumberShouldMatch = 2.0f;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 4);
            }

            {
                // only high freq terms around - check that min should match is applied
                CommonTermsQuery query = new CommonTermsQuery(Occur.MUST, Occur.SHOULD,
                    Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "the"));
                query.LowFreqMinimumNumberShouldMatch = 1.0f;
                query.HighFreqMinimumNumberShouldMatch = 2.0f;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 2);
                assertEquals(
                    new JCG.HashSet<string> { @"0", @"2" },
                    new JCG.HashSet<string> {
                        r.Document(search.ScoreDocs[0].Doc).Get(@"id"),
                        r.Document(search.ScoreDocs[1].Doc).Get(@"id") },
                    aggressive: false);
            }

            r.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestIllegalOccur()
        {
            Random random = Random;
            try
            {
                new CommonTermsQuery(Occur.MUST_NOT, RandomOccur(random), Random.NextSingle());
                Assert.Fail(@"MUST_NOT is not supproted");
            }
            catch (Exception ex) when (ex.IsIllegalArgumentException())
            {
            }

            try
            {
                new CommonTermsQuery(RandomOccur(random), Occur.MUST_NOT, Random.NextSingle());
                Assert.Fail(@"MUST_NOT is not supproted");
            }
            catch (Exception ex) when (ex.IsIllegalArgumentException())
            {
            }
        }

        [Test]
        public void TestExtend()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random);
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, analyzer);
            var docs = new string[]
            {
                @"this is the end of the world right",
                @"is this it or maybe not",
                @"this is the end of the universe as we know it",
                @"there is the famous restaurant at the end of the universe"
            };
            for (int i = 0; i < docs.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField(@"id", @"" + i, Field.Store.YES));
                doc.Add(NewTextField(@"field", docs[i], Field.Store.NO));
                w.AddDocument(doc);
            }

            IndexReader r = w.GetReader();
            IndexSearcher s = NewSearcher(r);
            {
                CommonTermsQuery query = new CommonTermsQuery(Occur.SHOULD, Occur.SHOULD,
                    Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 3);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
                assertEquals(@"2", r.Document(search.ScoreDocs[1].Doc).Get(@"id"));
                assertEquals(@"3", r.Document(search.ScoreDocs[2].Doc).Get(@"id"));
            }

            {
                // this one boosts the termQuery("field" "universe") by 10x
                CommonTermsQuery query = new ExtendedCommonTermsQuery(Occur.SHOULD, Occur.SHOULD,
                    Random.NextBoolean() ? 2.0f : 0.5f);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 3);
                assertEquals(@"2", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
                assertEquals(@"3", r.Document(search.ScoreDocs[1].Doc).Get(@"id"));
                assertEquals(@"0", r.Document(search.ScoreDocs[2].Doc).Get(@"id"));
            }

            r.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestRandomIndex()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random);
            analyzer.MaxTokenLength = TestUtil.NextInt32(Random, 1, IndexWriter.MAX_TERM_LENGTH);
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, analyzer);
            CreateRandomIndex(AtLeast(50), w, Random.NextInt64());
            DirectoryReader reader = w.GetReader();
            AtomicReader wrapper = SlowCompositeReaderWrapper.Wrap(reader);
            string field = @"body";
            Terms terms = wrapper.GetTerms(field);
            var lowFreqQueue = new PriorityQueueAnonymousClass(5);
            var highFreqQueue = new PriorityQueueAnonymousClass1(5);
            try
            {
                TermsEnum iterator = terms.GetEnumerator();
                while (iterator.MoveNext())
                {
                    if (highFreqQueue.Count < 5)
                    {
                        highFreqQueue.Add(new TermAndFreq(
                            BytesRef.DeepCopyOf(iterator.Term), iterator.DocFreq));
                        lowFreqQueue.Add(new TermAndFreq(
                            BytesRef.DeepCopyOf(iterator.Term), iterator.DocFreq));
                    }
                    else
                    {
                        if (highFreqQueue.Top.freq < iterator.DocFreq)
                        {
                            highFreqQueue.Top.freq = iterator.DocFreq;
                            highFreqQueue.Top.term = BytesRef.DeepCopyOf(iterator.Term);
                            highFreqQueue.UpdateTop();
                        }

                        if (lowFreqQueue.Top.freq > iterator.DocFreq)
                        {
                            lowFreqQueue.Top.freq = iterator.DocFreq;
                            lowFreqQueue.Top.term = BytesRef.DeepCopyOf(iterator.Term);
                            lowFreqQueue.UpdateTop();
                        }
                    }
                }

                int lowFreq = lowFreqQueue.Top.freq;
                int highFreq = highFreqQueue.Top.freq;
                AssumeTrue(@"unlucky index", highFreq - 1 > lowFreq);
                IList<TermAndFreq> highTerms = QueueToList(highFreqQueue);
                IList<TermAndFreq> lowTerms = QueueToList(lowFreqQueue);

                IndexSearcher searcher = NewSearcher(reader);
                Occur lowFreqOccur = RandomOccur(Random);
                BooleanQuery verifyQuery = new BooleanQuery();
                CommonTermsQuery cq = new CommonTermsQuery(RandomOccur(Random),
                    lowFreqOccur, highFreq - 1, Random.NextBoolean());
                foreach (TermAndFreq termAndFreq in lowTerms)
                {
                    cq.Add(new Term(field, termAndFreq.term));
                    verifyQuery.Add(new BooleanClause(new TermQuery(new Term(field,
                        termAndFreq.term)), lowFreqOccur));
                }
                foreach (TermAndFreq termAndFreq in highTerms)
                {
                    cq.Add(new Term(field, termAndFreq.term));
                }

                TopDocs cqSearch = searcher.Search(cq, reader.MaxDoc);

                TopDocs verifySearch = searcher.Search(verifyQuery, reader.MaxDoc);
                assertEquals(verifySearch.TotalHits, cqSearch.TotalHits);
                var hits = new JCG.HashSet<int>();
                foreach (ScoreDoc doc in verifySearch.ScoreDocs)
                {
                    hits.Add(doc.Doc);
                }

                foreach (ScoreDoc doc in cqSearch.ScoreDocs)
                {
                    assertTrue(hits.Remove(doc.Doc));
                }

                assertTrue(hits.Count == 0);

                /*
                 *  need to force merge here since QueryUtils adds checks based
                 *  on leave readers which have different statistics than the top
                 *  level reader if we have more than one segment. This could 
                 *  result in a different query / results.
                 */
                w.ForceMerge(1);
                DirectoryReader reader2 = w.GetReader();
                QueryUtils.Check(Random, cq, NewSearcher(reader2));
                reader2.Dispose();
            }
            finally
            {
                reader.Dispose();
                wrapper.Dispose();
                w.Dispose();
                dir.Dispose();
            }
        }

        private sealed class PriorityQueueAnonymousClass : Util.PriorityQueue<TermAndFreq>
        {
            public PriorityQueueAnonymousClass(int maxSize)
                : base(maxSize)
            {
            }

            protected internal override bool LessThan(TermAndFreq a, TermAndFreq b)
            {
                return a.freq > b.freq;
            }
        }

        private sealed class PriorityQueueAnonymousClass1 : Util.PriorityQueue<TermAndFreq>
        {
            public PriorityQueueAnonymousClass1(int maxSize)
                : base(maxSize)
            {
            }

            protected internal override bool LessThan(TermAndFreq a, TermAndFreq b)
            {
                return a.freq < b.freq;
            }
        }

        private static IList<TermAndFreq> QueueToList(Util.PriorityQueue<TermAndFreq> queue)
        {
            var terms = new JCG.List<TermAndFreq>();
            while (queue.Count > 0)
            {
                terms.Add(queue.Pop());
            }

            return terms;
        }

        private class TermAndFreq : IComparable<TermAndFreq>
        {
            public BytesRef term;
            public int freq;

            public TermAndFreq(BytesRef term, int freq)
            {
                this.term = term;
                this.freq = freq;
            }

            public int CompareTo(TermAndFreq other)
            {
                return term.CompareTo(other.term) + freq.CompareTo(other.freq);
            }
        }

        /// <summary>
        /// populates a writer with random stuff. this must be fully reproducable with
        /// the seed!
        /// </summary>
        public static void CreateRandomIndex(int numdocs, RandomIndexWriter writer, long seed)
        {
            Random random = new J2N.Randomizer(seed);
            // primary source for our data is from linefiledocs, its realistic.
            LineFileDocs lineFileDocs = new LineFileDocs(random, false); // no docvalues in 4x

            // TODO: we should add other fields that use things like docs&freqs but omit
            // positions,
            // because linefiledocs doesn't cover all the possibilities.
            for (int i = 0; i < numdocs; i++)
            {
                writer.AddDocument(lineFileDocs.NextDoc());
            }

            lineFileDocs.Dispose();
        }

        private sealed class ExtendedCommonTermsQuery : CommonTermsQuery
        {
            public ExtendedCommonTermsQuery(Occur highFreqOccur, Occur lowFreqOccur, float maxTermFrequency)
                : base(highFreqOccur, lowFreqOccur, maxTermFrequency)
            {
            }

            protected override Query NewTermQuery(Term term, TermContext context)
            {
                Query query = base.NewTermQuery(term, context);
                if (term.Text.Equals(@"universe", StringComparison.Ordinal))
                {
                    query.Boost = 100f;
                }

                return query;
            }
        }
    }
}
