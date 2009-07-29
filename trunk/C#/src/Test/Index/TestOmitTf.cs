/**
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

using NUnit.Framework;

using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using _TestUtil = Lucene.Net.Util._TestUtil;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using BooleanQuery = Lucene.Net.Search.BooleanQuery;
using HitCollector = Lucene.Net.Search.HitCollector;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Searcher = Lucene.Net.Search.Searcher;
using Similarity = Lucene.Net.Search.Similarity;
using TermQuery = Lucene.Net.Search.TermQuery;
using Occur = Lucene.Net.Search.BooleanClause.Occur;
using Directory = Lucene.Net.Store.Directory;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;

namespace Lucene.Net.Index
{
    [TestFixture]
    public class TestOmitTf : LuceneTestCase
    {

        public class SimpleSimilarity : Similarity
        {
            override public float LengthNorm(string field, int numTerms) { return 1.0f; }
            override public float QueryNorm(float sumOfSquaredWeights) { return 1.0f; }
            override public float Tf(float freq) { return freq; }
            override public float SloppyFreq(int distance) { return 2.0f; }
            override public float Idf(System.Collections.ICollection terms, Searcher searcher) { return 1.0f; }
            override public float Idf(int docFreq, int numDocs) { return 1.0f; }
            override public float Coord(int overlap, int maxOverlap) { return 1.0f; }
        }

        // Tests whether the DocumentWriter correctly enable the
        // omitTf bit in the FieldInfo
        [Test]
        public void TestOmitTf_Renamed()
        {
            Directory ram = new MockRAMDirectory();
            Analyzer analyzer = new StandardAnalyzer();
            IndexWriter writer = new IndexWriter(ram, analyzer, true, IndexWriter.MaxFieldLength.LIMITED);
            Document d = new Document();

            // this field will have Tf
            Field f1 = new Field("f1", "This field has term freqs", Field.Store.NO, Field.Index.ANALYZED);
            d.Add(f1);

            // this field will NOT have Tf
            Field f2 = new Field("f2", "This field has NO Tf in all docs", Field.Store.NO, Field.Index.ANALYZED);
            f2.SetOmitTf(true);
            d.Add(f2);

            writer.AddDocument(d);
            writer.Optimize();
            // now we add another document which has term freq for field f2 and not for f1 and verify if the SegmentMerger
            // keep things constant
            d = new Document();

            // Reverese
            f1.SetOmitTf(true);
            d.Add(f1);

            f2.SetOmitTf(false);
            d.Add(f2);

            writer.AddDocument(d);
            // force merge
            writer.Optimize();
            // flush
            writer.Close();
            _TestUtil.CheckIndex(ram);

            // only one segment in the index, so we can cast to SegmentReader
            SegmentReader reader = (SegmentReader)IndexReader.Open(ram);
            FieldInfos fi = reader.FieldInfos();
            Assert.IsTrue(fi.FieldInfo("f1").omitTf_ForNUnitTest, "OmitTf field bit should be set.");
            Assert.IsTrue(fi.FieldInfo("f2").omitTf_ForNUnitTest, "OmitTf field bit should be set.");

            reader.Close();
            ram.Close();
        }

        // Tests whether merging of docs that have different
        // omitTf for the same field works
        [Test]
        public void TestMixedMerge()
        {
            Directory ram = new MockRAMDirectory();
            Analyzer analyzer = new StandardAnalyzer();
            IndexWriter writer = new IndexWriter(ram, analyzer, true, IndexWriter.MaxFieldLength.LIMITED);
            writer.SetMaxBufferedDocs(3);
            writer.SetMergeFactor(2);
            Document d = new Document();

            // this field will have Tf
            Field f1 = new Field("f1", "This field has term freqs", Field.Store.NO, Field.Index.ANALYZED);
            d.Add(f1);

            // this field will NOT have Tf
            Field f2 = new Field("f2", "This field has NO Tf in all docs", Field.Store.NO, Field.Index.ANALYZED);
            f2.SetOmitTf(true);
            d.Add(f2);

            for (int i = 0; i < 30; i++)
                writer.AddDocument(d);

            // now we add another document which has term freq for field f2 and not for f1 and verify if the SegmentMerger
            // keep things constant
            d = new Document();

            // Reverese
            f1.SetOmitTf(true);
            d.Add(f1);

            f2.SetOmitTf(false);
            d.Add(f2);

            for (int i = 0; i < 30; i++)
                writer.AddDocument(d);

            // force merge
            writer.Optimize();
            // flush
            writer.Close();

            _TestUtil.CheckIndex(ram);

            // only one segment in the index, so we can cast to SegmentReader
            SegmentReader reader = (SegmentReader)IndexReader.Open(ram);
            FieldInfos fi = reader.FieldInfos();
            Assert.IsTrue(fi.FieldInfo("f1").omitTf_ForNUnitTest, "OmitTf field bit should be set.");
            Assert.IsTrue(fi.FieldInfo("f2").omitTf_ForNUnitTest, "OmitTf field bit should be set.");

            reader.Close();
            ram.Close();
        }

        // Make sure first adding docs that do not omitTf for
        // field X, then adding docs that do omitTf for that same
        // field, 
        [Test]
        public void TestMixedRAM()
        {
            Directory ram = new MockRAMDirectory();
            Analyzer analyzer = new StandardAnalyzer();
            IndexWriter writer = new IndexWriter(ram, analyzer, true, IndexWriter.MaxFieldLength.LIMITED);
            writer.SetMaxBufferedDocs(10);
            writer.SetMergeFactor(2);
            Document d = new Document();

            // this field will have Tf
            Field f1 = new Field("f1", "This field has term freqs", Field.Store.NO, Field.Index.ANALYZED);
            d.Add(f1);

            // this field will NOT have Tf
            Field f2 = new Field("f2", "This field has NO Tf in all docs", Field.Store.NO, Field.Index.ANALYZED);
            d.Add(f2);

            for (int i = 0; i < 5; i++)
                writer.AddDocument(d);

            f2.SetOmitTf(true);

            for (int i = 0; i < 20; i++)
                writer.AddDocument(d);

            // force merge
            writer.Optimize();

            // flush
            writer.Close();

            _TestUtil.CheckIndex(ram);

            // only one segment in the index, so we can cast to SegmentReader
            SegmentReader reader = (SegmentReader)IndexReader.Open(ram);
            FieldInfos fi = reader.FieldInfos();
            Assert.IsTrue(!fi.FieldInfo("f1").omitTf_ForNUnitTest, "OmitTf field bit should not be set.");
            Assert.IsTrue(fi.FieldInfo("f2").omitTf_ForNUnitTest, "OmitTf field bit should be set.");

            reader.Close();
            ram.Close();
        }

        private void AssertNoPrx(Directory dir)
        {
            string[] files = dir.List();
            for (int i = 0; i < files.Length; i++)
                Assert.IsFalse(files[i].EndsWith(".prx"));
        }

        // Verifies no *.prx exists when all fields omit term freq:
        [Test]
        public void TestNoPrxFile()
        {
            Directory ram = new MockRAMDirectory();
            Analyzer analyzer = new StandardAnalyzer();
            IndexWriter writer = new IndexWriter(ram, analyzer, true, IndexWriter.MaxFieldLength.LIMITED);
            writer.SetMaxBufferedDocs(3);
            writer.SetMergeFactor(2);
            writer.SetUseCompoundFile(false);
            Document d = new Document();

            Field f1 = new Field("f1", "This field has term freqs", Field.Store.NO, Field.Index.ANALYZED);
            f1.SetOmitTf(true);
            d.Add(f1);

            for (int i = 0; i < 30; i++)
                writer.AddDocument(d);

            writer.Commit();

            AssertNoPrx(ram);

            // force merge
            writer.Optimize();
            // flush
            writer.Close();

            AssertNoPrx(ram);
            _TestUtil.CheckIndex(ram);
            ram.Close();
        }

        // Test scores with one field with Term Freqs and one without, otherwise with equal content 
        [Test]
        public void TestBasic()
        {
            Directory dir = new MockRAMDirectory();
            Analyzer analyzer = new StandardAnalyzer();
            IndexWriter writer = new IndexWriter(dir, analyzer, true, IndexWriter.MaxFieldLength.LIMITED);
            writer.SetMergeFactor(2);
            writer.SetMaxBufferedDocs(2);
            writer.SetSimilarity(new SimpleSimilarity());


            System.Text.StringBuilder sb = new System.Text.StringBuilder(265);
            string term = "term";
            for (int i = 0; i < 30; i++)
            {
                Document d = new Document();
                sb.Append(term).Append(" ");
                string content = sb.ToString();
                Field noTf = new Field("noTf", content + (i % 2 == 0 ? "" : " notf"), Field.Store.NO, Field.Index.ANALYZED);
                noTf.SetOmitTf(true);
                d.Add(noTf);

                Field tf = new Field("tf", content + (i % 2 == 0 ? " tf" : ""), Field.Store.NO, Field.Index.ANALYZED);
                d.Add(tf);

                writer.AddDocument(d);
                //System.out.println(d);
            }

            writer.Optimize();
            // flush
            writer.Close();
            _TestUtil.CheckIndex(dir);

            /*
             * Verify the index
             */
            Searcher searcher = new IndexSearcher(dir);
            searcher.SetSimilarity(new SimpleSimilarity());

            Term a = new Term("noTf", term);
            Term b = new Term("tf", term);
            Term c = new Term("noTf", "notf");
            Term d2 = new Term("tf", "tf");
            TermQuery q1 = new TermQuery(a);
            TermQuery q2 = new TermQuery(b);
            TermQuery q3 = new TermQuery(c);
            TermQuery q4 = new TermQuery(d2);


            searcher.Search(q1, new AnonymousCountingHitCollector1());
            searcher.Search(q2, new AnonymousCountingHitCollector2());
            searcher.Search(q3, new AnonymousCountingHitCollector3());
            searcher.Search(q4, new AnonymousCountingHitCollector4());

            BooleanQuery bq = new BooleanQuery();
            bq.Add(q1, Occur.MUST);
            bq.Add(q4, Occur.MUST);

            searcher.Search(bq, new AnonymousCountingHitCollector5());
            Assert.IsTrue(15 == CountingHitCollector.GetCount());

            searcher.Close();
            dir.Close();
        }

        public class CountingHitCollector : HitCollector
        {
            static int count = 0;
            static int sum = 0;
            internal CountingHitCollector() { count = 0; sum = 0; }
            override public void Collect(int doc, float score)
            {
                count++;
                sum += doc;  // use it to avoid any possibility of being optimized away
            }

            public static int GetCount() { return count; }
            public static int GetSum() { return sum; }
        }

        public class AnonymousCountingHitCollector1 : CountingHitCollector
        {
            override public void Collect(int doc, float score)
            {
                //System.out.println("Q1: Doc=" + doc + " score=" + score);
                Assert.IsTrue(score == 1.0f);
                base.Collect(doc, score);
            }
        }
        public class AnonymousCountingHitCollector2 : CountingHitCollector
        {
            override public void Collect(int doc, float score)
            {
                //System.out.println("Q1: Doc=" + doc + " score=" + score);
                Assert.IsTrue(score == 1.0f+doc);
                base.Collect(doc, score);
            }
        }
        public class AnonymousCountingHitCollector3 : CountingHitCollector
        {
            override public void Collect(int doc, float score)
            {
                //System.out.println("Q1: Doc=" + doc + " score=" + score);
                Assert.IsTrue(score == 1.0f);
                Assert.IsFalse(doc % 2 == 0);
                base.Collect(doc, score);
            }
        }
        public class AnonymousCountingHitCollector4 : CountingHitCollector
        {
            override public void Collect(int doc, float score)
            {
                //System.out.println("Q1: Doc=" + doc + " score=" + score);
                Assert.IsTrue(score == 1.0f);
                Assert.IsTrue(doc % 2 == 0);
                base.Collect(doc, score);
            }
        };
        public class AnonymousCountingHitCollector5 : CountingHitCollector
        {
            override public void Collect(int doc, float score)
            {
                //System.out.println("BQ: Doc=" + doc + " score=" + score);
                base.Collect(doc, score);
            }
        }
    }
}
