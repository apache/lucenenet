using System;
using System.Diagnostics;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Lucene.Net.Index;
    using NUnit.Framework;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using DoubleField = DoubleField;
    using Field = Field;
    using FieldType = FieldType;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LongField = LongField;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using NumericUtils = Lucene.Net.Util.NumericUtils;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestNumericUtils = Lucene.Net.Util.TestNumericUtils; // NaN arrays
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestNumericRangeQuery64 : LuceneTestCase
    {
        // distance of entries
        private static long Distance;

        // shift the starting of the values to the left, to also have negative values:
        private static readonly long StartOffset = -1L << 31;

        // number of docs to generate for testing
        private static int NoDocs;

        private static Directory Directory = null;
        private static IndexReader Reader = null;
        private static IndexSearcher Searcher = null;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        [TestFixtureSetUp]
        public void BeforeClass()
        {
            NoDocs = AtLeast(4096);
            Distance = (1L << 60) / NoDocs;
            Directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 100, 1000)).SetMergePolicy(NewLogMergePolicy()));

            FieldType storedLong = new FieldType(LongField.TYPE_NOT_STORED);
            storedLong.Stored = true;
            storedLong.Freeze();

            FieldType storedLong8 = new FieldType(storedLong);
            storedLong8.NumericPrecisionStep = 8;

            FieldType storedLong4 = new FieldType(storedLong);
            storedLong4.NumericPrecisionStep = 4;

            FieldType storedLong6 = new FieldType(storedLong);
            storedLong6.NumericPrecisionStep = 6;

            FieldType storedLong2 = new FieldType(storedLong);
            storedLong2.NumericPrecisionStep = 2;

            FieldType storedLongNone = new FieldType(storedLong);
            storedLongNone.NumericPrecisionStep = int.MaxValue;

            FieldType unstoredLong = LongField.TYPE_NOT_STORED;

            FieldType unstoredLong8 = new FieldType(unstoredLong);
            unstoredLong8.NumericPrecisionStep = 8;

            FieldType unstoredLong6 = new FieldType(unstoredLong);
            unstoredLong6.NumericPrecisionStep = 6;

            FieldType unstoredLong4 = new FieldType(unstoredLong);
            unstoredLong4.NumericPrecisionStep = 4;

            FieldType unstoredLong2 = new FieldType(unstoredLong);
            unstoredLong2.NumericPrecisionStep = 2;

            LongField field8 = new LongField("field8", 0L, storedLong8), field6 = new LongField("field6", 0L, storedLong6), field4 = new LongField("field4", 0L, storedLong4), field2 = new LongField("field2", 0L, storedLong2), fieldNoTrie = new LongField("field" + int.MaxValue, 0L, storedLongNone), ascfield8 = new LongField("ascfield8", 0L, unstoredLong8), ascfield6 = new LongField("ascfield6", 0L, unstoredLong6), ascfield4 = new LongField("ascfield4", 0L, unstoredLong4), ascfield2 = new LongField("ascfield2", 0L, unstoredLong2);

            Document doc = new Document();
            // add fields, that have a distance to test general functionality
            doc.Add(field8);
            doc.Add(field6);
            doc.Add(field4);
            doc.Add(field2);
            doc.Add(fieldNoTrie);
            // add ascending fields with a distance of 1, beginning at -noDocs/2 to test the correct splitting of range and inclusive/exclusive
            doc.Add(ascfield8);
            doc.Add(ascfield6);
            doc.Add(ascfield4);
            doc.Add(ascfield2);

            // Add a series of noDocs docs with increasing long values, by updating the fields
            for (int l = 0; l < NoDocs; l++)
            {
                long val = Distance * l + StartOffset;
                field8.LongValue = val;
                field6.LongValue = val;
                field4.LongValue = val;
                field2.LongValue = val;
                fieldNoTrie.LongValue = val;

                val = l - (NoDocs / 2);
                ascfield8.LongValue = val;
                ascfield6.LongValue = val;
                ascfield4.LongValue = val;
                ascfield2.LongValue = val;
                writer.AddDocument(doc);
            }
            Reader = writer.Reader;
            Searcher = NewSearcher(Reader);
            writer.Dispose();
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            Searcher = null;
            Reader.Dispose();
            Reader = null;
            Directory.Dispose();
            Directory = null;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            // set the theoretical maximum term count for 8bit (see docs for the number)
            // super.tearDown will restore the default
            BooleanQuery.MaxClauseCount = 7 * 255 * 2 + 255;
        }

        /// <summary>
        /// test for constant score + boolean query + filter, the other tests only use the constant score mode </summary>
        private void TestRange(int precisionStep)
        {
            string field = "field" + precisionStep;
            int count = 3000;
            long lower = (Distance * 3 / 2) + StartOffset, upper = lower + count * Distance + (Distance / 3);
            NumericRangeQuery<long> q = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, true, true);
            NumericRangeFilter<long> f = NumericRangeFilter.NewLongRange(field, precisionStep, lower, upper, true, true);
            for (sbyte i = 0; i < 3; i++)
            {
                TopDocs topDocs;
                string type;
                switch (i)
                {
                    case 0:
                        type = " (constant score filter rewrite)";
                        q.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
                        topDocs = Searcher.Search(q, null, NoDocs, Sort.INDEXORDER);
                        break;

                    case 1:
                        type = " (constant score boolean rewrite)";
                        q.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE);
                        topDocs = Searcher.Search(q, null, NoDocs, Sort.INDEXORDER);
                        break;

                    case 2:
                        type = " (filter)";
                        topDocs = Searcher.Search(new MatchAllDocsQuery(), f, NoDocs, Sort.INDEXORDER);
                        break;

                    default:
                        return;
                }
                ScoreDoc[] sd = topDocs.ScoreDocs;
                Assert.IsNotNull(sd);
                Assert.AreEqual(count, sd.Length, "Score doc count" + type);
                Document doc = Searcher.Doc(sd[0].Doc);
                Assert.AreEqual(2 * Distance + StartOffset, (long)doc.GetField(field).NumericValue, "First doc" + type);
                doc = Searcher.Doc(sd[sd.Length - 1].Doc);
                Assert.AreEqual((1 + count) * Distance + StartOffset, (long)doc.GetField(field).NumericValue, "Last doc" + type);
            }
        }

        [Test]
        public virtual void TestRange_8bit()
        {
            TestRange(8);
        }

        [Test]
        public virtual void TestRange_6bit()
        {
            TestRange(6);
        }

        [Test]
        public virtual void TestRange_4bit()
        {
            TestRange(4);
        }

        [Test]
        public virtual void TestRange_2bit()
        {
            TestRange(2);
        }

        [Test]
        public virtual void TestInverseRange()
        {
            AtomicReaderContext context = (AtomicReaderContext)SlowCompositeReaderWrapper.Wrap(Searcher.IndexReader).Context;
            NumericRangeFilter<long> f = NumericRangeFilter.NewLongRange("field8", 8, 1000L, -1000L, true, true);
            Assert.IsNull(f.GetDocIdSet(context, (context.AtomicReader).LiveDocs), "A inverse range should return the null instance");
            f = NumericRangeFilter.NewLongRange("field8", 8, long.MaxValue, null, false, false);
            Assert.IsNull(f.GetDocIdSet(context, (context.AtomicReader).LiveDocs), "A exclusive range starting with Long.MAX_VALUE should return the null instance");
            f = NumericRangeFilter.NewLongRange("field8", 8, null, long.MinValue, false, false);
            Assert.IsNull(f.GetDocIdSet(context, (context.AtomicReader).LiveDocs), "A exclusive range ending with Long.MIN_VALUE should return the null instance");
        }

        [Test]
        public virtual void TestOneMatchQuery()
        {
            NumericRangeQuery<long> q = NumericRangeQuery.NewLongRange("ascfield8", 8, 1000L, 1000L, true, true);
            TopDocs topDocs = Searcher.Search(q, NoDocs);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            Assert.IsNotNull(sd);
            Assert.AreEqual(1, sd.Length, "Score doc count");
        }

        private void TestLeftOpenRange(int precisionStep)
        {
            string field = "field" + precisionStep;
            int count = 3000;
            long upper = (count - 1) * Distance + (Distance / 3) + StartOffset;
            NumericRangeQuery<long> q = NumericRangeQuery.NewLongRange(field, precisionStep, null, upper, true, true);
            TopDocs topDocs = Searcher.Search(q, null, NoDocs, Sort.INDEXORDER);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            Assert.IsNotNull(sd);
            Assert.AreEqual(count, sd.Length, "Score doc count");
            Document doc = Searcher.Doc(sd[0].Doc);
            Assert.AreEqual(StartOffset, (long)doc.GetField(field).NumericValue, "First doc");
            doc = Searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.AreEqual((count - 1) * Distance + StartOffset, (long)doc.GetField(field).NumericValue, "Last doc");

            q = NumericRangeQuery.NewLongRange(field, precisionStep, null, upper, false, true);
            topDocs = Searcher.Search(q, null, NoDocs, Sort.INDEXORDER);
            sd = topDocs.ScoreDocs;
            Assert.IsNotNull(sd);
            Assert.AreEqual(count, sd.Length, "Score doc count");
            doc = Searcher.Doc(sd[0].Doc);
            Assert.AreEqual(StartOffset, (long)doc.GetField(field).NumericValue, "First doc");
            doc = Searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.AreEqual((count - 1) * Distance + StartOffset, (long)doc.GetField(field).NumericValue, "Last doc");
        }

        [Test]
        public virtual void TestLeftOpenRange_8bit()
        {
            TestLeftOpenRange(8);
        }

        [Test]
        public virtual void TestLeftOpenRange_6bit()
        {
            TestLeftOpenRange(6);
        }

        [Test]
        public virtual void TestLeftOpenRange_4bit()
        {
            TestLeftOpenRange(4);
        }

        [Test]
        public virtual void TestLeftOpenRange_2bit()
        {
            TestLeftOpenRange(2);
        }

        private void TestRightOpenRange(int precisionStep)
        {
            string field = "field" + precisionStep;
            int count = 3000;
            long lower = (count - 1) * Distance + (Distance / 3) + StartOffset;
            NumericRangeQuery<long> q = NumericRangeQuery.NewLongRange(field, precisionStep, lower, null, true, true);
            TopDocs topDocs = Searcher.Search(q, null, NoDocs, Sort.INDEXORDER);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            Assert.IsNotNull(sd);
            Assert.AreEqual(NoDocs - count, sd.Length, "Score doc count");
            Document doc = Searcher.Doc(sd[0].Doc);
            Assert.AreEqual(count * Distance + StartOffset, (long)doc.GetField(field).NumericValue, "First doc");
            doc = Searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.AreEqual((NoDocs - 1) * Distance + StartOffset, (long)doc.GetField(field).NumericValue, "Last doc");

            q = NumericRangeQuery.NewLongRange(field, precisionStep, lower, null, true, false);
            topDocs = Searcher.Search(q, null, NoDocs, Sort.INDEXORDER);
            sd = topDocs.ScoreDocs;
            Assert.IsNotNull(sd);
            Assert.AreEqual(NoDocs - count, sd.Length, "Score doc count");
            doc = Searcher.Doc(sd[0].Doc);
            Assert.AreEqual(count * Distance + StartOffset, (long)doc.GetField(field).NumericValue, "First doc");
            doc = Searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.AreEqual((NoDocs - 1) * Distance + StartOffset, (long)doc.GetField(field).NumericValue, "Last doc");
        }

        [Test]
        public virtual void TestRightOpenRange_8bit()
        {
            TestRightOpenRange(8);
        }

        [Test]
        public virtual void TestRightOpenRange_6bit()
        {
            TestRightOpenRange(6);
        }

        [Test]
        public virtual void TestRightOpenRange_4bit()
        {
            TestRightOpenRange(4);
        }

        [Test]
        public virtual void TestRightOpenRange_2bit()
        {
            TestRightOpenRange(2);
        }

        [Test]
        public virtual void TestInfiniteValues()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            doc.Add(new DoubleField("double", double.NegativeInfinity, Field.Store.NO));
            doc.Add(new LongField("long", long.MinValue, Field.Store.NO));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new DoubleField("double", double.PositiveInfinity, Field.Store.NO));
            doc.Add(new LongField("long", long.MaxValue, Field.Store.NO));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new DoubleField("double", 0.0, Field.Store.NO));
            doc.Add(new LongField("long", 0L, Field.Store.NO));
            writer.AddDocument(doc);

            foreach (double d in TestNumericUtils.DOUBLE_NANs)
            {
                doc = new Document();
                doc.Add(new DoubleField("double", d, Field.Store.NO));
                writer.AddDocument(doc);
            }

            writer.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            IndexSearcher s = NewSearcher(r);

            Query q = NumericRangeQuery.NewLongRange("long", null, null, true, true);
            TopDocs topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewLongRange("long", null, null, false, false);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewLongRange("long", long.MinValue, long.MaxValue, true, true);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewLongRange("long", long.MinValue, long.MaxValue, false, false);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(1, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewDoubleRange("double", null, null, true, true);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewDoubleRange("double", null, null, false, false);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewDoubleRange("double", double.NegativeInfinity, double.PositiveInfinity, true, true);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewDoubleRange("double", double.NegativeInfinity, double.PositiveInfinity, false, false);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(1, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewDoubleRange("double", double.NaN, double.NaN, true, true);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(TestNumericUtils.DOUBLE_NANs.Length, topDocs.ScoreDocs.Length, "Score doc count");

            r.Dispose();
            dir.Dispose();
        }

        private void TestRandomTrieAndClassicRangeQuery(int precisionStep)
        {
            string field = "field" + precisionStep;
            int totalTermCountT = 0, totalTermCountC = 0, termCountT, termCountC;
            int num = TestUtil.NextInt(Random(), 10, 20);
            for (int i = 0; i < num; i++)
            {
                long lower = (long)(Random().NextDouble() * NoDocs * Distance) + StartOffset;
                long upper = (long)(Random().NextDouble() * NoDocs * Distance) + StartOffset;
                if (lower > upper)
                {
                    long a = lower;
                    lower = upper;
                    upper = a;
                }
                BytesRef lowerBytes = new BytesRef(NumericUtils.BUF_SIZE_LONG), upperBytes = new BytesRef(NumericUtils.BUF_SIZE_LONG);
                NumericUtils.LongToPrefixCodedBytes(lower, 0, lowerBytes);
                NumericUtils.LongToPrefixCodedBytes(upper, 0, upperBytes);

                // test inclusive range
                NumericRangeQuery<long> tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, true, true);
                TermRangeQuery cq = new TermRangeQuery(field, lowerBytes, upperBytes, true, true);
                TopDocs tTopDocs = Searcher.Search(tq, 1);
                TopDocs cTopDocs = Searcher.Search(cq, 1);
                Assert.AreEqual(cTopDocs.TotalHits, tTopDocs.TotalHits, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
                // test exclusive range
                tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, false, false);
                cq = new TermRangeQuery(field, lowerBytes, upperBytes, false, false);
                tTopDocs = Searcher.Search(tq, 1);
                cTopDocs = Searcher.Search(cq, 1);
                Assert.AreEqual(cTopDocs.TotalHits, tTopDocs.TotalHits, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
                // test left exclusive range
                tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, false, true);
                cq = new TermRangeQuery(field, lowerBytes, upperBytes, false, true);
                tTopDocs = Searcher.Search(tq, 1);
                cTopDocs = Searcher.Search(cq, 1);
                Assert.AreEqual(cTopDocs.TotalHits, tTopDocs.TotalHits, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
                // test right exclusive range
                tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, true, false);
                cq = new TermRangeQuery(field, lowerBytes, upperBytes, true, false);
                tTopDocs = Searcher.Search(tq, 1);
                cTopDocs = Searcher.Search(cq, 1);
                Assert.AreEqual(cTopDocs.TotalHits, tTopDocs.TotalHits, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
            }

            CheckTermCounts(precisionStep, totalTermCountT, totalTermCountC);
            if (VERBOSE && precisionStep != int.MaxValue)
            {
                Console.WriteLine("Average number of terms during random search on '" + field + "':");
                Console.WriteLine(" Numeric query: " + (((double)totalTermCountT) / (num * 4)));
                Console.WriteLine(" Classical query: " + (((double)totalTermCountC) / (num * 4)));
            }
        }

        [Test]
        public virtual void TestEmptyEnums()
        {
            int count = 3000;
            long lower = (Distance * 3 / 2) + StartOffset, upper = lower + count * Distance + (Distance / 3);
            // test empty enum
            Debug.Assert(lower < upper);
            Assert.IsTrue(0 < CountTerms(NumericRangeQuery.NewLongRange("field4", 4, lower, upper, true, true)));
            Assert.AreEqual(0, CountTerms(NumericRangeQuery.NewLongRange("field4", 4, upper, lower, true, true)));
            // test empty enum outside of bounds
            lower = Distance * NoDocs + StartOffset;
            upper = 2L * lower;
            Debug.Assert(lower < upper);
            Assert.AreEqual(0, CountTerms(NumericRangeQuery.NewLongRange("field4", 4, lower, upper, true, true)));
        }

        private int CountTerms(MultiTermQuery q)
        {
            Terms terms = MultiFields.GetTerms(Reader, q.Field);
            if (terms == null)
            {
                return 0;
            }
            TermsEnum termEnum = q.GetTermsEnum(terms);
            Assert.IsNotNull(termEnum);
            int count = 0;
            BytesRef cur, last = null;
            while ((cur = termEnum.Next()) != null)
            {
                count++;
                if (last != null)
                {
                    Assert.IsTrue(last.CompareTo(cur) < 0);
                }
                last = BytesRef.DeepCopyOf(cur);
            }
            // LUCENE-3314: the results after next() already returned null are undefined,
            // Assert.IsNull(termEnum.Next());
            return count;
        }

        private void CheckTermCounts(int precisionStep, int termCountT, int termCountC)
        {
            if (precisionStep == int.MaxValue)
            {
                Assert.AreEqual(termCountC, termCountT, "Number of terms should be equal for unlimited precStep");
            }
            else
            {
                Assert.IsTrue(termCountT <= termCountC, "Number of terms for NRQ should be <= compared to classical TRQ");
            }
        }

        [Test]
        public virtual void TestRandomTrieAndClassicRangeQuery_8bit()
        {
            TestRandomTrieAndClassicRangeQuery(8);
        }

        [Test]
        public virtual void TestRandomTrieAndClassicRangeQuery_6bit()
        {
            TestRandomTrieAndClassicRangeQuery(6);
        }

        [Test]
        public virtual void TestRandomTrieAndClassicRangeQuery_4bit()
        {
            TestRandomTrieAndClassicRangeQuery(4);
        }

        [Test]
        public virtual void TestRandomTrieAndClassicRangeQuery_2bit()
        {
            TestRandomTrieAndClassicRangeQuery(2);
        }

        [Test]
        public virtual void TestRandomTrieAndClassicRangeQuery_NoTrie()
        {
            TestRandomTrieAndClassicRangeQuery(int.MaxValue);
        }

        private void TestRangeSplit(int precisionStep)
        {
            string field = "ascfield" + precisionStep;
            // 10 random tests
            int num = TestUtil.NextInt(Random(), 10, 20);
            for (int i = 0; i < num; i++)
            {
                long lower = (long)(Random().NextDouble() * NoDocs - NoDocs / 2);
                long upper = (long)(Random().NextDouble() * NoDocs - NoDocs / 2);
                if (lower > upper)
                {
                    long a = lower;
                    lower = upper;
                    upper = a;
                }
                // test inclusive range
                Query tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, true, true);
                TopDocs tTopDocs = Searcher.Search(tq, 1);
                Assert.AreEqual(upper - lower + 1, tTopDocs.TotalHits, "Returned count of range query must be equal to inclusive range length");
                // test exclusive range
                tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, false, false);
                tTopDocs = Searcher.Search(tq, 1);
                Assert.AreEqual(Math.Max(upper - lower - 1, 0), tTopDocs.TotalHits, "Returned count of range query must be equal to exclusive range length");
                // test left exclusive range
                tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, false, true);
                tTopDocs = Searcher.Search(tq, 1);
                Assert.AreEqual(upper - lower, tTopDocs.TotalHits, "Returned count of range query must be equal to half exclusive range length");
                // test right exclusive range
                tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, true, false);
                tTopDocs = Searcher.Search(tq, 1);
                Assert.AreEqual(upper - lower, tTopDocs.TotalHits, "Returned count of range query must be equal to half exclusive range length");
            }
        }

        [Test]
        public virtual void TestRangeSplit_8bit()
        {
            TestRangeSplit(8);
        }

        [Test]
        public virtual void TestRangeSplit_6bit()
        {
            TestRangeSplit(6);
        }

        [Test]
        public virtual void TestRangeSplit_4bit()
        {
            TestRangeSplit(4);
        }

        [Test]
        public virtual void TestRangeSplit_2bit()
        {
            TestRangeSplit(2);
        }

        /// <summary>
        /// we fake a double test using long2double conversion of NumericUtils </summary>
        private void TestDoubleRange(int precisionStep)
        {
            string field = "ascfield" + precisionStep;
            const long lower = -1000L, upper = +2000L;

            Query tq = NumericRangeQuery.NewDoubleRange(field, precisionStep, NumericUtils.SortableLongToDouble(lower), NumericUtils.SortableLongToDouble(upper), true, true);
            TopDocs tTopDocs = Searcher.Search(tq, 1);
            Assert.AreEqual(upper - lower + 1, tTopDocs.TotalHits, "Returned count of range query must be equal to inclusive range length");

            Filter tf = NumericRangeFilter.NewDoubleRange(field, precisionStep, NumericUtils.SortableLongToDouble(lower), NumericUtils.SortableLongToDouble(upper), true, true);
            tTopDocs = Searcher.Search(new MatchAllDocsQuery(), tf, 1);
            Assert.AreEqual(upper - lower + 1, tTopDocs.TotalHits, "Returned count of range filter must be equal to inclusive range length");
        }

        [Test]
        public virtual void TestDoubleRange_8bit()
        {
            TestDoubleRange(8);
        }

        [Test]
        public virtual void TestDoubleRange_6bit()
        {
            TestDoubleRange(6);
        }

        [Test]
        public virtual void TestDoubleRange_4bit()
        {
            TestDoubleRange(4);
        }

        [Test]
        public virtual void TestDoubleRange_2bit()
        {
            TestDoubleRange(2);
        }

        private void TestSorting(int precisionStep)
        {
            string field = "field" + precisionStep;
            // 10 random tests, the index order is ascending,
            // so using a reverse sort field should retun descending documents
            int num = TestUtil.NextInt(Random(), 10, 20);
            for (int i = 0; i < num; i++)
            {
                long lower = (long)(Random().NextDouble() * NoDocs * Distance) + StartOffset;
                long upper = (long)(Random().NextDouble() * NoDocs * Distance) + StartOffset;
                if (lower > upper)
                {
                    long a = lower;
                    lower = upper;
                    upper = a;
                }
                Query tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, true, true);
                TopDocs topDocs = Searcher.Search(tq, null, NoDocs, new Sort(new SortField(field, SortField.Type_e.LONG, true)));
                if (topDocs.TotalHits == 0)
                {
                    continue;
                }
                ScoreDoc[] sd = topDocs.ScoreDocs;
                Assert.IsNotNull(sd);
                long last = (long)Searcher.Doc(sd[0].Doc).GetField(field).NumericValue;
                for (int j = 1; j < sd.Length; j++)
                {
                    long act = (long)Searcher.Doc(sd[j].Doc).GetField(field).NumericValue;
                    Assert.IsTrue(last > act, "Docs should be sorted backwards");
                    last = act;
                }
            }
        }

        [Test]
        public virtual void TestSorting_8bit()
        {
            TestSorting(8);
        }

        [Test]
        public virtual void TestSorting_6bit()
        {
            TestSorting(6);
        }

        [Test]
        public virtual void TestSorting_4bit()
        {
            TestSorting(4);
        }

        [Test]
        public virtual void TestSorting_2bit()
        {
            TestSorting(2);
        }

        [Test]
        public virtual void TestEqualsAndHash()
        {
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test1", 4, 10L, 20L, true, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test2", 4, 10L, 20L, false, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test3", 4, 10L, 20L, true, false));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test4", 4, 10L, 20L, false, false));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test5", 4, 10L, null, true, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test6", 4, null, 20L, true, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test7", 4, null, null, true, true));
            QueryUtils.CheckEqual(NumericRangeQuery.NewLongRange("test8", 4, 10L, 20L, true, true), NumericRangeQuery.NewLongRange("test8", 4, 10L, 20L, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewLongRange("test9", 4, 10L, 20L, true, true), NumericRangeQuery.NewLongRange("test9", 8, 10L, 20L, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewLongRange("test10a", 4, 10L, 20L, true, true), NumericRangeQuery.NewLongRange("test10b", 4, 10L, 20L, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewLongRange("test11", 4, 10L, 20L, true, true), NumericRangeQuery.NewLongRange("test11", 4, 20L, 10L, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewLongRange("test12", 4, 10L, 20L, true, true), NumericRangeQuery.NewLongRange("test12", 4, 10L, 20L, false, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewLongRange("test13", 4, 10L, 20L, true, true), NumericRangeQuery.NewFloatRange("test13", 4, 10f, 20f, true, true));
            // difference to int range is tested in TestNumericRangeQuery32
        }
    }
}