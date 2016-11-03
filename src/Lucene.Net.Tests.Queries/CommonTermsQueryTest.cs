using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries
{
    public class CommonTermsQueryTest : LuceneTestCase
    {
        [Test]
        public void TestBasics()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, analyzer, Similarity, TimeZone);
            var docs = new string[]
            {
                @"this is the end of the world right", @"is this it or maybe not",
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

            IndexReader r = w.Reader;
            IndexSearcher s = NewSearcher(r);
            {
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD, Random().NextBoolean() ? 2F : 0.5F);
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
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD, Random().NextBoolean() ? 2F : 0.5F);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 2);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
                assertEquals(@"2", r.Document(search.ScoreDocs[1].Doc).Get(@"id"));
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.MUST, Random().NextBoolean() ? 2F : 0.5F);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 1);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.MUST, Random().NextBoolean() ? 2F : 0.5F);
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
            CommonTermsQuery query = new CommonTermsQuery(RandomOccur(Random()), RandomOccur(Random()), Random().NextFloat(), Random().NextBoolean());
            int terms = AtLeast(2);
            for (int i = 0; i < terms; i++)
            {
                query.Add(new Term(TestUtil.RandomRealisticUnicodeString(Random()), TestUtil.RandomRealisticUnicodeString(Random())));
            }

            QueryUtils.CheckHashEquals(query);
            QueryUtils.CheckUnequal(new CommonTermsQuery(RandomOccur(Random()), RandomOccur(Random()), Random().NextFloat(), Random().NextBoolean()), query);
            {
                long seed = Random().NextLong();
                Random r = new Random((int)seed);
                CommonTermsQuery left = new CommonTermsQuery(RandomOccur(r), RandomOccur(r), r.NextFloat(), r.NextBoolean());
                int leftTerms = AtLeast(r, 2);
                for (int i = 0; i < leftTerms; i++)
                {
                    left.Add(new Term(TestUtil.RandomRealisticUnicodeString(r), TestUtil.RandomRealisticUnicodeString(r)));
                }

                left.HighFreqMinimumNumberShouldMatch = r.nextInt(4);
                left.LowFreqMinimumNumberShouldMatch = r.nextInt(4);
                r = new Random((int)seed);
                CommonTermsQuery right = new CommonTermsQuery(RandomOccur(r), RandomOccur(r), r.NextFloat(), r.NextBoolean());
                int rightTerms = AtLeast(r, 2);
                for (int i = 0; i < rightTerms; i++)
                {
                    right.Add(new Term(TestUtil.RandomRealisticUnicodeString(r), TestUtil.RandomRealisticUnicodeString(r)));
                }

                right.HighFreqMinimumNumberShouldMatch = r.nextInt(4);
                right.LowFreqMinimumNumberShouldMatch = r.nextInt(4);
                QueryUtils.CheckEqual(left, right);
            }
        }

        private static BooleanClause.Occur RandomOccur(Random random)
        {
            return random.NextBoolean() ? BooleanClause.Occur.MUST : BooleanClause.Occur.SHOULD;
        }

        [Test]
        public void TestNullTerm()
        {
            Random random = Random();
            CommonTermsQuery query = new CommonTermsQuery(RandomOccur(random), RandomOccur(random), Random().NextFloat());
            try
            {
                query.Add(null);
                Fail(@"null values are not supported");
            }
            catch (ArgumentException)
            {
            }
        }

        [Test]
        public void TestMinShouldMatch()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, analyzer, Similarity, TimeZone);
            string[] docs = new string[]
            {
                @"this is the end of the world right", @"is this it or maybe not",
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

            IndexReader r = w.Reader;
            IndexSearcher s = NewSearcher(r);
            {
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD, Random().NextBoolean() ? 2F : 0.5F);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                query.LowFreqMinimumNumberShouldMatch = 0.5F;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 1);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD, Random().NextBoolean() ? 2F : 0.5F);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                query.LowFreqMinimumNumberShouldMatch = 2F;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 1);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD, Random().NextBoolean() ? 2F : 0.5F);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                query.LowFreqMinimumNumberShouldMatch = 0.49F;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 3);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
                assertEquals(@"2", r.Document(search.ScoreDocs[1].Doc).Get(@"id"));
                assertEquals(@"3", r.Document(search.ScoreDocs[2].Doc).Get(@"id"));
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD, Random().NextBoolean() ? 2F : 0.5F);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                query.LowFreqMinimumNumberShouldMatch = 1F;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 3);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
                assertEquals(@"2", r.Document(search.ScoreDocs[1].Doc).Get(@"id"));
                assertEquals(@"3", r.Document(search.ScoreDocs[2].Doc).Get(@"id"));
                assertTrue(search.ScoreDocs[1].Score > search.ScoreDocs[2].Score);
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD, Random().NextBoolean() ? 2F : 0.5F);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "end"));
                query.Add(new Term("field", "world"));
                query.Add(new Term("field", "universe"));
                query.Add(new Term("field", "right"));
                query.LowFreqMinimumNumberShouldMatch = 1F;
                query.HighFreqMinimumNumberShouldMatch = 4F;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 3);
                assertEquals(search.ScoreDocs[1].Score, search.ScoreDocs[2].Score, 0F);
                assertEquals(@"0", r.Document(search.ScoreDocs[0].Doc).Get(@"id"));
                assertEquals(new HashSet<string>(Arrays.AsList(@"2", @"3")), new HashSet<string>(Arrays.AsList(r.Document(search.ScoreDocs[1].Doc).Get(@"id"), r.Document(search.ScoreDocs[2].Doc).Get(@"id"))));
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD, Random().NextBoolean() ? 2F : 0.5F);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "the"));
                query.LowFreqMinimumNumberShouldMatch = 1F;
                query.HighFreqMinimumNumberShouldMatch = 2F;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 4);
            }

            {
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.MUST, BooleanClause.Occur.SHOULD, Random().NextBoolean() ? 2F : 0.5F);
                query.Add(new Term("field", "is"));
                query.Add(new Term("field", "this"));
                query.Add(new Term("field", "the"));
                query.LowFreqMinimumNumberShouldMatch = 1F;
                query.HighFreqMinimumNumberShouldMatch = 2F;
                TopDocs search = s.Search(query, 10);
                assertEquals(search.TotalHits, 2);
                assertEquals(new HashSet<string>(Arrays.AsList(@"0", @"2")), new HashSet<string>(Arrays.AsList(r.Document(search.ScoreDocs[0].Doc).Get(@"id"), r.Document(search.ScoreDocs[1].Doc).Get(@"id"))));
            }

            r.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestIllegalOccur()
        {
            Random random = Random();
            Assert.Throws<ArgumentException>(() =>
                {
                    new CommonTermsQuery(BooleanClause.Occur.MUST_NOT, RandomOccur(random), Random().NextFloat());
                },
                "MUST_NOT is not supported");

            Assert.Throws<ArgumentException>(() =>
                {
                    new CommonTermsQuery(RandomOccur(random), BooleanClause.Occur.MUST_NOT, Random().NextFloat());
                },
                "MUST_NOT is not supported");
        }

        [Test]
        public void TestExtend()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, analyzer, Similarity, TimeZone);
            var docs = new string[]
            {
                @"this is the end of the world right", @"is this it or maybe not",
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

            IndexReader r = w.Reader;
            IndexSearcher s = NewSearcher(r);
            {
                CommonTermsQuery query = new CommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD, Random().NextBoolean() ? 2F : 0.5F);
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
                CommonTermsQuery query = new ExtendedCommonTermsQuery(BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD, Random().NextBoolean() ? 2F : 0.5F);
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

        /*
         * LUCENENET TODO requires a better comparator implementation for PriorityQueue
        [Test]
        public void TestRandomIndex()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, analyzer);
            CreateRandomIndex(AtLeast(50), w, Random().NextLong());
            DirectoryReader reader = w.Reader;
            AtomicReader wrapper = SlowCompositeReaderWrapper.Wrap(reader);
            string field = @"body";
            Terms terms = wrapper.Terms(field);
            var lowFreqQueue = new AnonymousPriorityQueue(this, 5);
            Util.PriorityQueue<TermAndFreq> highFreqQueue = new AnonymousPriorityQueue1(this, 5);
            try
            {
                TermsEnum iterator = terms.Iterator(null);
                while (iterator.Next() != null)
                {
                    if (highFreqQueue.Size() < 5)
                    {
                        highFreqQueue.Add(new TermAndFreq(BytesRef.DeepCopyOf(iterator.Term()), iterator.DocFreq()));
                        lowFreqQueue.Add(new TermAndFreq(BytesRef.DeepCopyOf(iterator.Term()), iterator.DocFreq()));
                    }
                    else
                    {
                        if (highFreqQueue.Top().freq < iterator.DocFreq())
                        {
                            highFreqQueue.Top().freq = iterator.DocFreq();
                            highFreqQueue.Top().term = BytesRef.DeepCopyOf(iterator.Term());
                            highFreqQueue.UpdateTop();
                        }

                        if (lowFreqQueue.Top().freq > iterator.DocFreq())
                        {
                            lowFreqQueue.Top().freq = iterator.DocFreq();
                            lowFreqQueue.Top().term = BytesRef.DeepCopyOf(iterator.Term());
                            lowFreqQueue.UpdateTop();
                        }
                    }
                }

                int lowFreq = lowFreqQueue.Top().freq;
                int highFreq = highFreqQueue.Top().freq;
                AssumeTrue(@"unlucky index", highFreq - 1 > lowFreq);
                List<TermAndFreq> highTerms = QueueToList(highFreqQueue);
                List<TermAndFreq> lowTerms = QueueToList(lowFreqQueue);
                IndexSearcher searcher = NewSearcher(reader);
                BooleanClause.Occur lowFreqOccur = RandomOccur(Random());
                BooleanQuery verifyQuery = new BooleanQuery();
                CommonTermsQuery cq = new CommonTermsQuery(RandomOccur(Random()), lowFreqOccur, highFreq - 1, Random().NextBoolean());
                foreach (TermAndFreq termAndFreq in lowTerms)
                {
                    cq.Add(new Term(field, termAndFreq.term));
                    verifyQuery.Add(new BooleanClause(new TermQuery(new Term(field, termAndFreq.term)), lowFreqOccur));
                }

                foreach (TermAndFreq termAndFreq in highTerms)
                {
                    cq.Add(new Term(field, termAndFreq.term));
                }

                TopDocs cqSearch = searcher.Search(cq, reader.MaxDoc);
                TopDocs verifySearch = searcher.Search(verifyQuery, reader.MaxDoc);
                assertEquals(verifySearch.TotalHits, cqSearch.TotalHits);
                var hits = new HashSet<int>();
                foreach (ScoreDoc doc in verifySearch.ScoreDocs)
                {
                    hits.Add(doc.Doc);
                }

                foreach (ScoreDoc doc in cqSearch.ScoreDocs)
                {
                    assertTrue(hits.Remove(doc.Doc));
                }

                assertTrue(hits.IsEmpty());
                w.ForceMerge(1);
                DirectoryReader reader2 = w.Reader;
                QueryUtils.Check(Random(), cq, NewSearcher(reader2));
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

        private sealed class AnonymousPriorityQueue : Support.PriorityQueue<TermAndFreq>
        {
            public AnonymousPriorityQueue(CommonTermsQueryTest parent)
            {
                this.parent = parent;
            }

            private readonly CommonTermsQueryTest parent;
            protected override bool LessThan(TermAndFreq a, TermAndFreq b)
            {
                return a.freq > b.freq;
            }
        }

        private sealed class AnonymousPriorityQueue1 : Support.PriorityQueue<TermAndFreq>
        {
            public AnonymousPriorityQueue1(CommonTermsQueryTest parent)
            {
                this.parent = parent;
            }

            private readonly CommonTermsQueryTest parent;
            protected override bool LessThan(TermAndFreq a, TermAndFreq b)
            {
                return a.freq < b.freq;
            }
        }*/

        private static List<TermAndFreq> QueueToList(Util.PriorityQueue<TermAndFreq> queue)
        {
            var terms = new List<TermAndFreq>();
            while (queue.Size() > 0)
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

        public static void CreateRandomIndex(int numdocs, RandomIndexWriter writer, long seed)
        {
            Random random = new Random((int)seed);
            // primary source for our data is from linefiledocs, its realistic.
            LineFileDocs lineFileDocs = new LineFileDocs(random, false); // no docvalues in 4x
            for (int i = 0; i < numdocs; i++)
            {
                writer.AddDocument(lineFileDocs.NextDoc());
            }

            lineFileDocs.Dispose();
        }

        private sealed class ExtendedCommonTermsQuery : CommonTermsQuery
        {
            public ExtendedCommonTermsQuery(BooleanClause.Occur highFreqOccur, BooleanClause.Occur lowFreqOccur, float maxTermFrequency)
                : base(highFreqOccur, lowFreqOccur, maxTermFrequency)
            {
            }

            protected override Query NewTermQuery(Term term, TermContext context)
            {
                Query query = base.NewTermQuery(term, context);
                if (term.Text().Equals(@"universe"))
                {
                    query.Boost = 100F;
                }

                return query;
            }
        }
    }
}
