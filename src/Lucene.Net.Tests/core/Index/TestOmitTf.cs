using System;
using System.Text;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using NUnit.Framework;

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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BooleanQuery = Lucene.Net.Search.BooleanQuery;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CollectionStatistics = Lucene.Net.Search.CollectionStatistics;
    using Collector = Lucene.Net.Search.Collector;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Explanation = Lucene.Net.Search.Explanation;
    using Field = Field;
    using FieldType = FieldType;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Occur = Lucene.Net.Search.Occur;
    using PhraseQuery = Lucene.Net.Search.PhraseQuery;
    using Scorer = Lucene.Net.Search.Scorer;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TermStatistics = Lucene.Net.Search.TermStatistics;
    using TextField = TextField;
    using TFIDFSimilarity = Lucene.Net.Search.Similarities.TFIDFSimilarity;

    [TestFixture]
    public class TestOmitTf : LuceneTestCase
    {
        public class SimpleSimilarity : TFIDFSimilarity
        {
            public override float DecodeNormValue(long norm)
            {
                return norm;
            }

            public override long EncodeNormValue(float f)
            {
                return (long)f;
            }

            public override float QueryNorm(float sumOfSquaredWeights)
            {
                return 1.0f;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return 1.0f;
            }

            public override float LengthNorm(FieldInvertState state)
            {
                return state.Boost;
            }

            public override float Tf(float freq)
            {
                return freq;
            }

            public override float SloppyFreq(int distance)
            {
                return 2.0f;
            }

            public override float Idf(long docFreq, long numDocs)
            {
                return 1.0f;
            }

            public override Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
            {
                return new Explanation(1.0f, "Inexplicable");
            }

            public override float ScorePayload(int doc, int start, int end, BytesRef payload)
            {
                return 1.0f;
            }
        }

        private static readonly FieldType OmitType = new FieldType(TextField.TYPE_NOT_STORED);
        private static readonly FieldType NormalType = new FieldType(TextField.TYPE_NOT_STORED);

        static TestOmitTf()
        {
            OmitType.IndexOptions = IndexOptions.DOCS_ONLY;
        }

        // Tests whether the DocumentWriter correctly enable the
        // omitTermFreqAndPositions bit in the FieldInfo
        [Test]
        public virtual void TestOmitTermFreqAndPositions()
        {
            Directory ram = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            Document d = new Document();

            // this field will have Tf
            Field f1 = NewField("f1", "this field has term freqs", NormalType);
            d.Add(f1);

            // this field will NOT have Tf
            Field f2 = NewField("f2", "this field has NO Tf in all docs", OmitType);
            d.Add(f2);

            writer.AddDocument(d);
            writer.ForceMerge(1);
            // now we add another document which has term freq for field f2 and not for f1 and verify if the SegmentMerger
            // keep things constant
            d = new Document();

            // Reverse
            f1 = NewField("f1", "this field has term freqs", OmitType);
            d.Add(f1);

            f2 = NewField("f2", "this field has NO Tf in all docs", NormalType);
            d.Add(f2);

            writer.AddDocument(d);

            // force merge
            writer.ForceMerge(1);
            // flush
            writer.Dispose();

            SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
            FieldInfos fi = reader.FieldInfos;
            Assert.AreEqual(IndexOptions.DOCS_ONLY, fi.FieldInfo("f1").IndexOptions, "OmitTermFreqAndPositions field bit should be set.");
            Assert.AreEqual(IndexOptions.DOCS_ONLY, fi.FieldInfo("f2").IndexOptions, "OmitTermFreqAndPositions field bit should be set.");

            reader.Dispose();
            ram.Dispose();
        }

        // Tests whether merging of docs that have different
        // omitTermFreqAndPositions for the same field works
        [Test]
        public virtual void TestMixedMerge()
        {
            Directory ram = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(3).SetMergePolicy(NewLogMergePolicy(2)));
            Document d = new Document();

            // this field will have Tf
            Field f1 = NewField("f1", "this field has term freqs", NormalType);
            d.Add(f1);

            // this field will NOT have Tf
            Field f2 = NewField("f2", "this field has NO Tf in all docs", OmitType);
            d.Add(f2);

            for (int i = 0; i < 30; i++)
            {
                writer.AddDocument(d);
            }

            // now we add another document which has term freq for field f2 and not for f1 and verify if the SegmentMerger
            // keep things constant
            d = new Document();

            // Reverese
            f1 = NewField("f1", "this field has term freqs", OmitType);
            d.Add(f1);

            f2 = NewField("f2", "this field has NO Tf in all docs", NormalType);
            d.Add(f2);

            for (int i = 0; i < 30; i++)
            {
                writer.AddDocument(d);
            }

            // force merge
            writer.ForceMerge(1);
            // flush
            writer.Dispose();

            SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
            FieldInfos fi = reader.FieldInfos;
            Assert.AreEqual(IndexOptions.DOCS_ONLY, fi.FieldInfo("f1").IndexOptions, "OmitTermFreqAndPositions field bit should be set.");
            Assert.AreEqual(IndexOptions.DOCS_ONLY, fi.FieldInfo("f2").IndexOptions, "OmitTermFreqAndPositions field bit should be set.");

            reader.Dispose();
            ram.Dispose();
        }

        // Make sure first adding docs that do not omitTermFreqAndPositions for
        // field X, then adding docs that do omitTermFreqAndPositions for that same
        // field,
        [Test]
        public virtual void TestMixedRAM()
        {
            Directory ram = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(10).SetMergePolicy(NewLogMergePolicy(2)));
            Document d = new Document();

            // this field will have Tf
            Field f1 = NewField("f1", "this field has term freqs", NormalType);
            d.Add(f1);

            // this field will NOT have Tf
            Field f2 = NewField("f2", "this field has NO Tf in all docs", OmitType);
            d.Add(f2);

            for (int i = 0; i < 5; i++)
            {
                writer.AddDocument(d);
            }

            for (int i = 0; i < 20; i++)
            {
                writer.AddDocument(d);
            }

            // force merge
            writer.ForceMerge(1);

            // flush
            writer.Dispose();

            SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
            FieldInfos fi = reader.FieldInfos;
            Assert.AreEqual(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, fi.FieldInfo("f1").IndexOptions, "OmitTermFreqAndPositions field bit should not be set.");
            Assert.AreEqual(IndexOptions.DOCS_ONLY, fi.FieldInfo("f2").IndexOptions, "OmitTermFreqAndPositions field bit should be set.");

            reader.Dispose();
            ram.Dispose();
        }

        private void AssertNoPrx(Directory dir)
        {
            string[] files = dir.ListAll();
            for (int i = 0; i < files.Length; i++)
            {
                Assert.IsFalse(files[i].EndsWith(".prx"));
                Assert.IsFalse(files[i].EndsWith(".pos"));
            }
        }

        // Verifies no *.prx exists when all fields omit term freq:
        [Test]
        public virtual void TestNoPrxFile()
        {
            Directory ram = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(3).SetMergePolicy(NewLogMergePolicy()));
            LogMergePolicy lmp = (LogMergePolicy)writer.Config.MergePolicy;
            lmp.MergeFactor = 2;
            lmp.NoCFSRatio = 0.0;
            Document d = new Document();

            Field f1 = NewField("f1", "this field has term freqs", OmitType);
            d.Add(f1);

            for (int i = 0; i < 30; i++)
            {
                writer.AddDocument(d);
            }

            writer.Commit();

            AssertNoPrx(ram);

            // now add some documents with positions, and check
            // there is no prox after full merge
            d = new Document();
            f1 = NewTextField("f1", "this field has positions", Field.Store.NO);
            d.Add(f1);

            for (int i = 0; i < 30; i++)
            {
                writer.AddDocument(d);
            }

            // force merge
            writer.ForceMerge(1);
            // flush
            writer.Dispose();

            AssertNoPrx(ram);
            ram.Dispose();
        }

        // Test scores with one field with Term Freqs and one without, otherwise with equal content
        [Test]
        public virtual void TestBasic()
        {
            Directory dir = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(2).SetSimilarity(new SimpleSimilarity()).SetMergePolicy(NewLogMergePolicy(2)));

            StringBuilder sb = new StringBuilder(265);
            string term = "term";
            for (int i = 0; i < 30; i++)
            {
                Document doc = new Document();
                sb.Append(term).Append(" ");
                string content = sb.ToString();
                Field noTf = NewField("noTf", content + (i % 2 == 0 ? "" : " notf"), OmitType);
                doc.Add(noTf);

                Field tf = NewField("tf", content + (i % 2 == 0 ? " tf" : ""), NormalType);
                doc.Add(tf);

                writer.AddDocument(doc);
                //System.out.println(d);
            }

            writer.ForceMerge(1);
            // flush
            writer.Dispose();

            /*
             * Verify the index
             */
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            searcher.Similarity = new SimpleSimilarity();

            Term a = new Term("noTf", term);
            Term b = new Term("tf", term);
            Term c = new Term("noTf", "notf");
            Term d = new Term("tf", "tf");
            TermQuery q1 = new TermQuery(a);
            TermQuery q2 = new TermQuery(b);
            TermQuery q3 = new TermQuery(c);
            TermQuery q4 = new TermQuery(d);

            PhraseQuery pq = new PhraseQuery();
            pq.Add(a);
            pq.Add(c);
            try
            {
                searcher.Search(pq, 10);
                Assert.Fail("did not hit expected exception");
            }
            catch (Exception e)
            {
                Exception cause = e;
                // If the searcher uses an executor service, the IAE is wrapped into other exceptions
                while (cause.InnerException != null)
                {
                    cause = cause.InnerException;
                }
                if (!(cause is InvalidOperationException))
                {
                    throw new InvalidOperationException("Expected an IAE", e);
                } // else OK because positions are not indexed
            }

            searcher.Search(q1, new CountingHitCollectorAnonymousInnerClassHelper(this));
            //System.out.println(CountingHitCollector.getCount());

            searcher.Search(q2, new CountingHitCollectorAnonymousInnerClassHelper2(this));
            //System.out.println(CountingHitCollector.getCount());

            searcher.Search(q3, new CountingHitCollectorAnonymousInnerClassHelper3(this));
            //System.out.println(CountingHitCollector.getCount());

            searcher.Search(q4, new CountingHitCollectorAnonymousInnerClassHelper4(this));
            //System.out.println(CountingHitCollector.getCount());

            BooleanQuery bq = new BooleanQuery();
            bq.Add(q1, Occur.MUST);
            bq.Add(q4, Occur.MUST);

            searcher.Search(bq, new CountingHitCollectorAnonymousInnerClassHelper5(this));
            Assert.AreEqual(15, CountingHitCollector.Count);

            reader.Dispose();
            dir.Dispose();
        }

        private class CountingHitCollectorAnonymousInnerClassHelper : CountingHitCollector
        {
            private readonly TestOmitTf OuterInstance;

            public CountingHitCollectorAnonymousInnerClassHelper(TestOmitTf outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            private Scorer scorer;

            public override sealed void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override sealed void Collect(int doc)
            {
                //System.out.println("Q1: Doc=" + doc + " score=" + score);
                float score = scorer.Score();
                Assert.IsTrue(score == 1.0f, "got score=" + score);
                base.Collect(doc);
            }
        }

        private class CountingHitCollectorAnonymousInnerClassHelper2 : CountingHitCollector
        {
            private readonly TestOmitTf OuterInstance;

            public CountingHitCollectorAnonymousInnerClassHelper2(TestOmitTf outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            private Scorer scorer;

            public override sealed void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override sealed void Collect(int doc)
            {
                //System.out.println("Q2: Doc=" + doc + " score=" + score);
                float score = scorer.Score();
                Assert.AreEqual(1.0f + doc, score, 0.00001f);
                base.Collect(doc);
            }
        }

        private class CountingHitCollectorAnonymousInnerClassHelper3 : CountingHitCollector
        {
            private readonly TestOmitTf OuterInstance;

            public CountingHitCollectorAnonymousInnerClassHelper3(TestOmitTf outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            private Scorer scorer;

            public override sealed void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override sealed void Collect(int doc)
            {
                //System.out.println("Q1: Doc=" + doc + " score=" + score);
                float score = scorer.Score();
                Assert.IsTrue(score == 1.0f);
                Assert.IsFalse(doc % 2 == 0);
                base.Collect(doc);
            }
        }

        private class CountingHitCollectorAnonymousInnerClassHelper4 : CountingHitCollector
        {
            private readonly TestOmitTf OuterInstance;

            public CountingHitCollectorAnonymousInnerClassHelper4(TestOmitTf outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            private Scorer scorer;

            public override sealed void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override sealed void Collect(int doc)
            {
                float score = scorer.Score();
                //System.out.println("Q1: Doc=" + doc + " score=" + score);
                Assert.IsTrue(score == 1.0f);
                Assert.IsTrue(doc % 2 == 0);
                base.Collect(doc);
            }
        }

        private class CountingHitCollectorAnonymousInnerClassHelper5 : CountingHitCollector
        {
            private readonly TestOmitTf OuterInstance;

            public CountingHitCollectorAnonymousInnerClassHelper5(TestOmitTf outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override sealed void Collect(int doc)
            {
                //System.out.println("BQ: Doc=" + doc + " score=" + score);
                base.Collect(doc);
            }
        }

        public class CountingHitCollector : Collector
        {
            internal static int Count_Renamed = 0;
            internal static int Sum_Renamed = 0;
            internal int DocBase = -1;

            internal CountingHitCollector()
            {
                Count_Renamed = 0;
                Sum_Renamed = 0;
            }

            public override void SetScorer(Scorer scorer)
            {
            }

            public override void Collect(int doc)
            {
                Count_Renamed++;
                Sum_Renamed += doc + DocBase; // use it to avoid any possibility of being merged away
            }

            public static int Count
            {
                get
                {
                    return Count_Renamed;
                }
            }

            public static int Sum
            {
                get
                {
                    return Sum_Renamed;
                }
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                DocBase = context.DocBase;
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return true;
            }
        }

        /// <summary>
        /// test that when freqs are omitted, that totalTermFreq and sumTotalTermFreq are -1 </summary>
        [Test]
        public virtual void TestStats()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.IndexOptions = IndexOptions.DOCS_ONLY;
            ft.Freeze();
            Field f = NewField("foo", "bar", ft);
            doc.Add(f);
            iw.AddDocument(doc);
            IndexReader ir = iw.Reader;
            iw.Dispose();
            Assert.AreEqual(-1, ir.TotalTermFreq(new Term("foo", new BytesRef("bar"))));
            Assert.AreEqual(-1, ir.GetSumTotalTermFreq("foo"));
            ir.Dispose();
            dir.Dispose();
        }
    }
}