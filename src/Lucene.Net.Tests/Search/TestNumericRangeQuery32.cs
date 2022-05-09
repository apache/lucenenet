using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Int32Field = Int32Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using NumericUtils = Lucene.Net.Util.NumericUtils;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SingleField = SingleField;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestNumericUtils = Lucene.Net.Util.TestNumericUtils; // NaN arrays
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestNumericRangeQuery32 : LuceneTestCase
    {
        // distance of entries
        private static int distance;

        // shift the starting of the values to the left, to also have negative values:
        private static readonly int startOffset = -1 << 15;

        // number of docs to generate for testing
        private static int noDocs;

        private static Directory directory = null;
        private static IndexReader reader = null;
        private static IndexSearcher searcher = null;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            noDocs = AtLeast(4096);
            distance = (1 << 30) / noDocs;
            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(TestUtil.NextInt32(Random, 100, 1000)).SetMergePolicy(NewLogMergePolicy()));

            FieldType storedInt = new FieldType(Int32Field.TYPE_NOT_STORED);
            storedInt.IsStored = true;
            storedInt.Freeze();

            FieldType storedInt8 = new FieldType(storedInt);
            storedInt8.NumericPrecisionStep = 8;

            FieldType storedInt4 = new FieldType(storedInt);
            storedInt4.NumericPrecisionStep = 4;

            FieldType storedInt2 = new FieldType(storedInt);
            storedInt2.NumericPrecisionStep = 2;

            FieldType storedIntNone = new FieldType(storedInt);
            storedIntNone.NumericPrecisionStep = int.MaxValue;

            FieldType unstoredInt = Int32Field.TYPE_NOT_STORED;

            FieldType unstoredInt8 = new FieldType(unstoredInt);
            unstoredInt8.NumericPrecisionStep = 8;

            FieldType unstoredInt4 = new FieldType(unstoredInt);
            unstoredInt4.NumericPrecisionStep = 4;

            FieldType unstoredInt2 = new FieldType(unstoredInt);
            unstoredInt2.NumericPrecisionStep = 2;

            Int32Field field8 = new Int32Field("field8", 0, storedInt8), field4 = new Int32Field("field4", 0, storedInt4), field2 = new Int32Field("field2", 0, storedInt2), fieldNoTrie = new Int32Field("field" + int.MaxValue, 0, storedIntNone), ascfield8 = new Int32Field("ascfield8", 0, unstoredInt8), ascfield4 = new Int32Field("ascfield4", 0, unstoredInt4), ascfield2 = new Int32Field("ascfield2", 0, unstoredInt2);

            Document doc = new Document();
            // add fields, that have a distance to test general functionality
            doc.Add(field8);
            doc.Add(field4);
            doc.Add(field2);
            doc.Add(fieldNoTrie);
            // add ascending fields with a distance of 1, beginning at -noDocs/2 to test the correct splitting of range and inclusive/exclusive
            doc.Add(ascfield8);
            doc.Add(ascfield4);
            doc.Add(ascfield2);

            // Add a series of noDocs docs with increasing int values
            for (int l = 0; l < noDocs; l++)
            {
                int val = distance * l + startOffset;
                field8.SetInt32Value(val);
                field4.SetInt32Value(val);
                field2.SetInt32Value(val);
                fieldNoTrie.SetInt32Value(val);

                val = l - (noDocs / 2);
                ascfield8.SetInt32Value(val);
                ascfield4.SetInt32Value(val);
                ascfield2.SetInt32Value(val);
                writer.AddDocument(doc);
            }

            reader = writer.GetReader();
            searcher = NewSearcher(reader);
            writer.Dispose();
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            searcher = null;
            reader.Dispose();
            reader = null;
            directory.Dispose();
            directory = null;
            base.AfterClass();
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            // set the theoretical maximum term count for 8bit (see docs for the number)
            // super.tearDown will restore the default
            BooleanQuery.MaxClauseCount = 3 * 255 * 2 + 255;
        }

        /// <summary>
        /// test for both constant score and boolean query, the other tests only use the constant score mode </summary>
        private void TestRange(int precisionStep)
        {
            string field = "field" + precisionStep;
            int count = 3000;
            int lower = (distance * 3 / 2) + startOffset, upper = lower + count * distance + (distance / 3);
            NumericRangeQuery<int> q = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, upper, true, true);
            NumericRangeFilter<int> f = NumericRangeFilter.NewInt32Range(field, precisionStep, lower, upper, true, true);
            for (sbyte i = 0; i < 3; i++)
            {
                TopDocs topDocs;
                string type;
                switch (i)
                {
                    case 0:
                        type = " (constant score filter rewrite)";
                        q.MultiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;
                        topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
                        break;

                    case 1:
                        type = " (constant score boolean rewrite)";
                        q.MultiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE;
                        topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
                        break;

                    case 2:
                        type = " (filter)";
                        topDocs = searcher.Search(new MatchAllDocsQuery(), f, noDocs, Sort.INDEXORDER);
                        break;

                    default:
                        return;
                }
                ScoreDoc[] sd = topDocs.ScoreDocs;
                Assert.IsNotNull(sd);
                Assert.AreEqual(count, sd.Length, "Score doc count" + type);
                Document doc = searcher.Doc(sd[0].Doc);
                Assert.AreEqual(2 * distance + startOffset, doc.GetField(field).GetInt32Value().Value, "First doc" + type);
                doc = searcher.Doc(sd[sd.Length - 1].Doc);
                Assert.AreEqual((1 + count) * distance + startOffset, doc.GetField(field).GetInt32Value().Value, "Last doc" + type);
            }
        }

        [Test]
        public virtual void TestRange_8bit()
        {
            TestRange(8);
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
            AtomicReaderContext context = (AtomicReaderContext)SlowCompositeReaderWrapper.Wrap(reader).Context;
            NumericRangeFilter<int> f = NumericRangeFilter.NewInt32Range("field8", 8, 1000, -1000, true, true);
            Assert.IsNull(f.GetDocIdSet(context, (context.AtomicReader).LiveDocs), "A inverse range should return the null instance");
            f = NumericRangeFilter.NewInt32Range("field8", 8, int.MaxValue, null, false, false);
            Assert.IsNull(f.GetDocIdSet(context, (context.AtomicReader).LiveDocs), "A exclusive range starting with Integer.MAX_VALUE should return the null instance");
            f = NumericRangeFilter.NewInt32Range("field8", 8, null, int.MinValue, false, false);
            Assert.IsNull(f.GetDocIdSet(context, (context.AtomicReader).LiveDocs), "A exclusive range ending with Integer.MIN_VALUE should return the null instance");
        }

        [Test]
        public virtual void TestOneMatchQuery()
        {
            NumericRangeQuery<int> q = NumericRangeQuery.NewInt32Range("ascfield8", 8, 1000, 1000, true, true);
            TopDocs topDocs = searcher.Search(q, noDocs);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            Assert.IsNotNull(sd);
            Assert.AreEqual(1, sd.Length, "Score doc count");
        }

        private void TestLeftOpenRange(int precisionStep)
        {
            string field = "field" + precisionStep;
            int count = 3000;
            int upper = (count - 1) * distance + (distance / 3) + startOffset;
            NumericRangeQuery<int> q = NumericRangeQuery.NewInt32Range(field, precisionStep, null, upper, true, true);
            TopDocs topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            Assert.IsNotNull(sd);
            Assert.AreEqual(count, sd.Length, "Score doc count");
            Document doc = searcher.Doc(sd[0].Doc);
            Assert.AreEqual(startOffset, doc.GetField(field).GetInt32Value().Value, "First doc");
            doc = searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.AreEqual((count - 1) * distance + startOffset, doc.GetField(field).GetInt32Value().Value, "Last doc");

            q = NumericRangeQuery.NewInt32Range(field, precisionStep, null, upper, false, true);
            topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
            sd = topDocs.ScoreDocs;
            Assert.IsNotNull(sd);
            Assert.AreEqual(count, sd.Length, "Score doc count");
            doc = searcher.Doc(sd[0].Doc);
            Assert.AreEqual(startOffset, doc.GetField(field).GetInt32Value().Value, "First doc");
            doc = searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.AreEqual((count - 1) * distance + startOffset, doc.GetField(field).GetInt32Value().Value, "Last doc");
        }

        [Test]
        public virtual void TestLeftOpenRange_8bit()
        {
            TestLeftOpenRange(8);
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
            int lower = (count - 1) * distance + (distance / 3) + startOffset;
            NumericRangeQuery<int> q = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, null, true, true);
            TopDocs topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            Assert.IsNotNull(sd);
            Assert.AreEqual(noDocs - count, sd.Length, "Score doc count");
            Document doc = searcher.Doc(sd[0].Doc);
            Assert.AreEqual(count * distance + startOffset, doc.GetField(field).GetInt32Value().Value, "First doc");
            doc = searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.AreEqual((noDocs - 1) * distance + startOffset, doc.GetField(field).GetInt32Value().Value, "Last doc");

            q = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, null, true, false);
            topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
            sd = topDocs.ScoreDocs;
            Assert.IsNotNull(sd);
            Assert.AreEqual(noDocs - count, sd.Length, "Score doc count");
            doc = searcher.Doc(sd[0].Doc);
            Assert.AreEqual(count * distance + startOffset, doc.GetField(field).GetInt32Value().Value, "First doc");
            doc = searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.AreEqual((noDocs - 1) * distance + startOffset, doc.GetField(field).GetInt32Value().Value, "Last doc");
        }

        [Test]
        public virtual void TestRightOpenRange_8bit()
        {
            TestRightOpenRange(8);
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
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            doc.Add(new SingleField("float", float.NegativeInfinity, Field.Store.NO));
            doc.Add(new Int32Field("int", int.MinValue, Field.Store.NO));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new SingleField("float", float.PositiveInfinity, Field.Store.NO));
            doc.Add(new Int32Field("int", int.MaxValue, Field.Store.NO));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new SingleField("float", 0.0f, Field.Store.NO));
            doc.Add(new Int32Field("int", 0, Field.Store.NO));
            writer.AddDocument(doc);

            foreach (float f in TestNumericUtils.FLOAT_NANs)
            {
                doc = new Document();
                doc.Add(new SingleField("float", f, Field.Store.NO));
                writer.AddDocument(doc);
            }

            writer.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            IndexSearcher s = NewSearcher(r);

            Query q = NumericRangeQuery.NewInt32Range("int", null, null, true, true);
            TopDocs topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewInt32Range("int", null, null, false, false);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewInt32Range("int", int.MinValue, int.MaxValue, true, true);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewInt32Range("int", int.MinValue, int.MaxValue, false, false);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(1, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewSingleRange("float", null, null, true, true);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewSingleRange("float", null, null, false, false);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewSingleRange("float", float.NegativeInfinity, float.PositiveInfinity, true, true);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(3, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewSingleRange("float", float.NegativeInfinity, float.PositiveInfinity, false, false);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(1, topDocs.ScoreDocs.Length, "Score doc count");

            q = NumericRangeQuery.NewSingleRange("float", float.NaN, float.NaN, true, true);
            topDocs = s.Search(q, 10);
            Assert.AreEqual(TestNumericUtils.FLOAT_NANs.Length, topDocs.ScoreDocs.Length, "Score doc count");

            r.Dispose();
            dir.Dispose();
        }

        private void TestRandomTrieAndClassicRangeQuery(int precisionStep)
        {
            string field = "field" + precisionStep;
            int totalTermCountT = 0, totalTermCountC = 0, termCountT, termCountC;
            int num = TestUtil.NextInt32(Random, 10, 20);
            for (int i = 0; i < num; i++)
            {
                int lower = (int)(Random.NextDouble() * noDocs * distance) + startOffset;
                int upper = (int)(Random.NextDouble() * noDocs * distance) + startOffset;
                if (lower > upper)
                {
                    int a = lower;
                    lower = upper;
                    upper = a;
                }
                BytesRef lowerBytes = new BytesRef(NumericUtils.BUF_SIZE_INT32), upperBytes = new BytesRef(NumericUtils.BUF_SIZE_INT32);
                NumericUtils.Int32ToPrefixCodedBytes(lower, 0, lowerBytes);
                NumericUtils.Int32ToPrefixCodedBytes(upper, 0, upperBytes);

                // test inclusive range
                NumericRangeQuery<int> tq = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, upper, true, true);
                TermRangeQuery cq = new TermRangeQuery(field, lowerBytes, upperBytes, true, true);
                TopDocs tTopDocs = searcher.Search(tq, 1);
                TopDocs cTopDocs = searcher.Search(cq, 1);
                Assert.AreEqual(cTopDocs.TotalHits, tTopDocs.TotalHits, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
                // test exclusive range
                tq = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, upper, false, false);
                cq = new TermRangeQuery(field, lowerBytes, upperBytes, false, false);
                tTopDocs = searcher.Search(tq, 1);
                cTopDocs = searcher.Search(cq, 1);
                Assert.AreEqual(cTopDocs.TotalHits, tTopDocs.TotalHits, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
                // test left exclusive range
                tq = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, upper, false, true);
                cq = new TermRangeQuery(field, lowerBytes, upperBytes, false, true);
                tTopDocs = searcher.Search(tq, 1);
                cTopDocs = searcher.Search(cq, 1);
                Assert.AreEqual(cTopDocs.TotalHits, tTopDocs.TotalHits, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
                // test right exclusive range
                tq = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, upper, true, false);
                cq = new TermRangeQuery(field, lowerBytes, upperBytes, true, false);
                tTopDocs = searcher.Search(tq, 1);
                cTopDocs = searcher.Search(cq, 1);
                Assert.AreEqual(cTopDocs.TotalHits, tTopDocs.TotalHits, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
            }

            CheckTermCounts(precisionStep, totalTermCountT, totalTermCountC);
            if (Verbose && precisionStep != int.MaxValue)
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
            int lower = (distance * 3 / 2) + startOffset, upper = lower + count * distance + (distance / 3);
            // test empty enum
            if (Debugging.AssertsEnabled) Debugging.Assert(lower < upper);
            Assert.IsTrue(0 < CountTerms(NumericRangeQuery.NewInt32Range("field4", 4, lower, upper, true, true)));
            Assert.AreEqual(0, CountTerms(NumericRangeQuery.NewInt32Range("field4", 4, upper, lower, true, true)));
            // test empty enum outside of bounds
            lower = distance * noDocs + startOffset;
            upper = 2 * lower;
            if (Debugging.AssertsEnabled) Debugging.Assert(lower < upper);
            Assert.AreEqual(0, CountTerms(NumericRangeQuery.NewInt32Range("field4", 4, lower, upper, true, true)));
        }

        private int CountTerms(MultiTermQuery q)
        {
            Terms terms = MultiFields.GetTerms(reader, q.Field);
            if (terms is null)
            {
                return 0;
            }
            TermsEnum termEnum = q.GetTermsEnum(terms);
            Assert.IsNotNull(termEnum);
            int count = 0;
            BytesRef cur, last = null;
            while (termEnum.MoveNext())
            {
                cur = termEnum.Term;
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
            int num = TestUtil.NextInt32(Random, 10, 20);
            for (int i = 0; i < num; i++)
            {
                int lower = (int)(Random.NextDouble() * noDocs - noDocs / 2);
                int upper = (int)(Random.NextDouble() * noDocs - noDocs / 2);
                if (lower > upper)
                {
                    int a = lower;
                    lower = upper;
                    upper = a;
                }
                // test inclusive range
                Query tq = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, upper, true, true);
                TopDocs tTopDocs = searcher.Search(tq, 1);
                Assert.AreEqual(upper - lower + 1, tTopDocs.TotalHits, "Returned count of range query must be equal to inclusive range length");
                // test exclusive range
                tq = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, upper, false, false);
                tTopDocs = searcher.Search(tq, 1);
                Assert.AreEqual(Math.Max(upper - lower - 1, 0), tTopDocs.TotalHits, "Returned count of range query must be equal to exclusive range length");
                // test left exclusive range
                tq = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, upper, false, true);
                tTopDocs = searcher.Search(tq, 1);
                Assert.AreEqual(upper - lower, tTopDocs.TotalHits, "Returned count of range query must be equal to half exclusive range length");
                // test right exclusive range
                tq = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, upper, true, false);
                tTopDocs = searcher.Search(tq, 1);
                Assert.AreEqual(upper - lower, tTopDocs.TotalHits, "Returned count of range query must be equal to half exclusive range length");
            }
        }

        [Test]
        public virtual void TestRangeSplit_8bit()
        {
            TestRangeSplit(8);
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
        /// we fake a float test using int2float conversion of NumericUtils </summary>
        private void TestFloatRange(int precisionStep)
        {
            string field = "ascfield" + precisionStep;
            const int lower = -1000, upper = +2000;

            Query tq = NumericRangeQuery.NewSingleRange(field, precisionStep, NumericUtils.SortableInt32ToSingle(lower), NumericUtils.SortableInt32ToSingle(upper), true, true);
            TopDocs tTopDocs = searcher.Search(tq, 1);
            Assert.AreEqual(upper - lower + 1, tTopDocs.TotalHits, "Returned count of range query must be equal to inclusive range length");

            Filter tf = NumericRangeFilter.NewSingleRange(field, precisionStep, NumericUtils.SortableInt32ToSingle(lower), NumericUtils.SortableInt32ToSingle(upper), true, true);
            tTopDocs = searcher.Search(new MatchAllDocsQuery(), tf, 1);
            Assert.AreEqual(upper - lower + 1, tTopDocs.TotalHits, "Returned count of range filter must be equal to inclusive range length");
        }

        [Test]
        public virtual void TestFloatRange_8bit()
        {
            TestFloatRange(8);
        }

        [Test]
        public virtual void TestFloatRange_4bit()
        {
            TestFloatRange(4);
        }

        [Test]
        public virtual void TestFloatRange_2bit()
        {
            TestFloatRange(2);
        }

        private void TestSorting(int precisionStep)
        {
            string field = "field" + precisionStep;
            // 10 random tests, the index order is ascending,
            // so using a reverse sort field should retun descending documents
            int num = TestUtil.NextInt32(Random, 10, 20);
            for (int i = 0; i < num; i++)
            {
                int lower = (int)(Random.NextDouble() * noDocs * distance) + startOffset;
                int upper = (int)(Random.NextDouble() * noDocs * distance) + startOffset;
                if (lower > upper)
                {
                    int a = lower;
                    lower = upper;
                    upper = a;
                }
                Query tq = NumericRangeQuery.NewInt32Range(field, precisionStep, lower, upper, true, true);
                TopDocs topDocs = searcher.Search(tq, null, noDocs, new Sort(new SortField(field, SortFieldType.INT32, true)));
                if (topDocs.TotalHits == 0)
                {
                    continue;
                }
                ScoreDoc[] sd = topDocs.ScoreDocs;
                Assert.IsNotNull(sd);
                int last = searcher.Doc(sd[0].Doc).GetField(field).GetInt32Value().Value;
                for (int j = 1; j < sd.Length; j++)
                {
                    int act = searcher.Doc(sd[j].Doc).GetField(field).GetInt32Value().Value;
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
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewInt32Range("test1", 4, 10, 20, true, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewInt32Range("test2", 4, 10, 20, false, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewInt32Range("test3", 4, 10, 20, true, false));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewInt32Range("test4", 4, 10, 20, false, false));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewInt32Range("test5", 4, 10, null, true, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewInt32Range("test6", 4, null, 20, true, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewInt32Range("test7", 4, null, null, true, true));
            QueryUtils.CheckEqual(NumericRangeQuery.NewInt32Range("test8", 4, 10, 20, true, true), NumericRangeQuery.NewInt32Range("test8", 4, 10, 20, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewInt32Range("test9", 4, 10, 20, true, true), NumericRangeQuery.NewInt32Range("test9", 8, 10, 20, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewInt32Range("test10a", 4, 10, 20, true, true), NumericRangeQuery.NewInt32Range("test10b", 4, 10, 20, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewInt32Range("test11", 4, 10, 20, true, true), NumericRangeQuery.NewInt32Range("test11", 4, 20, 10, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewInt32Range("test12", 4, 10, 20, true, true), NumericRangeQuery.NewInt32Range("test12", 4, 10, 20, false, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewInt32Range("test13", 4, 10, 20, true, true), NumericRangeQuery.NewSingleRange("test13", 4, 10f, 20f, true, true));
            // the following produces a hash collision, because Long and Integer have the same hashcode, so only test equality:
            Query q1 = NumericRangeQuery.NewInt32Range("test14", 4, 10, 20, true, true);
            Query q2 = NumericRangeQuery.NewInt64Range("test14", 4, 10L, 20L, true, true);
            Assert.IsFalse(q1.Equals(q2));
            Assert.IsFalse(q2.Equals(q1));
        }
    }
}