using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Lucene.Net.Search;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
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
    using BinaryDocValuesField = BinaryDocValuesField;
    using Bits = Lucene.Net.Util.Bits;
    using BooleanClause = Lucene.Net.Search.BooleanClause;
    using BooleanQuery = Lucene.Net.Search.BooleanQuery;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using BytesRefHash = Lucene.Net.Util.BytesRefHash;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FloatDocValuesField = FloatDocValuesField;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using Lucene42DocValuesFormat = Lucene.Net.Codecs.Lucene42.Lucene42DocValuesFormat;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NumericDocValuesField = NumericDocValuesField;
    using Query = Lucene.Net.Search.Query;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
    using SortedDocValuesField = SortedDocValuesField;
    using SortedSetDocValuesField = SortedSetDocValuesField;
    using StoredField = StoredField;
    using StringField = StringField;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;
    using TopDocs = Lucene.Net.Search.TopDocs;

    /// <summary>
    /// Abstract class to do basic tests for a docvalues format.
    /// NOTE: this test focuses on the docvalues impl, nothing else.
    /// The [stretch] goal is for this test to be
    /// so thorough in testing a new DocValuesFormat that if this
    /// test passes, then all Lucene/Solr tests should also pass.  Ie,
    /// if there is some bug in a given DocValuesFormat that this
    /// test fails to catch then this test needs to be improved!
    /// </summary>
    public abstract class BaseDocValuesFormatTestCase : BaseIndexFileFormatTestCase
    {
        protected internal override void AddRandomFields(Document doc)
        {
            if (Usually())
            {
                doc.Add(new NumericDocValuesField("ndv", Random().Next(1 << 12)));
                doc.Add(new BinaryDocValuesField("bdv", new BytesRef(TestUtil.RandomSimpleString(Random()))));
                doc.Add(new SortedDocValuesField("sdv", new BytesRef(TestUtil.RandomSimpleString(Random(), 2))));
            }
            if (DefaultCodecSupportsSortedSet())
            {
                int numValues = Random().Next(5);
                for (int i = 0; i < numValues; ++i)
                {
                    doc.Add(new SortedSetDocValuesField("ssdv", new BytesRef(TestUtil.RandomSimpleString(Random(), 2))));
                }
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestOneNumber()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
            Document doc = new Document();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            doc.Add(NewTextField("fieldname", text, Field.Store.YES));
            doc.Add(new NumericDocValuesField("dv", 5));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = new IndexSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                Debug.Assert(ireader.Leaves.Count == 1);
                NumericDocValues dv = ((AtomicReader)((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader))).GetNumericDocValues("dv");
                Assert.AreEqual(5, dv.Get(hits.ScoreDocs[i].Doc));
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestOneFloat()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
            Document doc = new Document();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            doc.Add(NewTextField("fieldname", text, Field.Store.YES));
            doc.Add(new FloatDocValuesField("dv", 5.7f));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = new IndexSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                Debug.Assert(ireader.Leaves.Count == 1);
                NumericDocValues dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetNumericDocValues("dv");
                Assert.AreEqual(Number.FloatToIntBits(5.7f), dv.Get(hits.ScoreDocs[i].Doc));
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestTwoNumbers()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
            Document doc = new Document();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            doc.Add(NewTextField("fieldname", text, Field.Store.YES));
            doc.Add(new NumericDocValuesField("dv1", 5));
            doc.Add(new NumericDocValuesField("dv2", 17));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = new IndexSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                Debug.Assert(ireader.Leaves.Count == 1);
                NumericDocValues dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetNumericDocValues("dv1");
                Assert.AreEqual(5, dv.Get(hits.ScoreDocs[i].Doc));
                dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetNumericDocValues("dv2");
                Assert.AreEqual(17, dv.Get(hits.ScoreDocs[i].Doc));
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestTwoBinaryValues()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
            Document doc = new Document();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            doc.Add(NewTextField("fieldname", text, Field.Store.YES));
            doc.Add(new BinaryDocValuesField("dv1", new BytesRef(longTerm)));
            doc.Add(new BinaryDocValuesField("dv2", new BytesRef(text)));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = new IndexSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                Debug.Assert(ireader.Leaves.Count == 1);
                BinaryDocValues dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetBinaryDocValues("dv1");
                BytesRef scratch = new BytesRef();
                dv.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef(longTerm), scratch);
                dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetBinaryDocValues("dv2");
                dv.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef(text), scratch);
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestTwoFieldsMixed()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
            Document doc = new Document();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            doc.Add(NewTextField("fieldname", text, Field.Store.YES));
            doc.Add(new NumericDocValuesField("dv1", 5));
            doc.Add(new BinaryDocValuesField("dv2", new BytesRef("hello world")));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = new IndexSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            BytesRef scratch = new BytesRef();
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                Debug.Assert(ireader.Leaves.Count == 1);
                NumericDocValues dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetNumericDocValues("dv1");
                Assert.AreEqual(5, dv.Get(hits.ScoreDocs[i].Doc));
                BinaryDocValues dv2 = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetBinaryDocValues("dv2");
                dv2.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestThreeFieldsMixed()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
            Document doc = new Document();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            doc.Add(NewTextField("fieldname", text, Field.Store.YES));
            doc.Add(new SortedDocValuesField("dv1", new BytesRef("hello hello")));
            doc.Add(new NumericDocValuesField("dv2", 5));
            doc.Add(new BinaryDocValuesField("dv3", new BytesRef("hello world")));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = new IndexSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            BytesRef scratch = new BytesRef();
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                Debug.Assert(ireader.Leaves.Count == 1);
                SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv1");
                int ord = dv.GetOrd(0);
                dv.LookupOrd(ord, scratch);
                Assert.AreEqual(new BytesRef("hello hello"), scratch);
                NumericDocValues dv2 = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv2");
                Assert.AreEqual(5, dv2.Get(hits.ScoreDocs[i].Doc));
                BinaryDocValues dv3 = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv3");
                dv3.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestThreeFieldsMixed2()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
            Document doc = new Document();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            doc.Add(NewTextField("fieldname", text, Field.Store.YES));
            doc.Add(new BinaryDocValuesField("dv1", new BytesRef("hello world")));
            doc.Add(new SortedDocValuesField("dv2", new BytesRef("hello hello")));
            doc.Add(new NumericDocValuesField("dv3", 5));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = new IndexSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            BytesRef scratch = new BytesRef();
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                Debug.Assert(ireader.Leaves.Count == 1);
                SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv2");
                int ord = dv.GetOrd(0);
                dv.LookupOrd(ord, scratch);
                Assert.AreEqual(new BytesRef("hello hello"), scratch);
                NumericDocValues dv2 = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv3");
                Assert.AreEqual(5, dv2.Get(hits.ScoreDocs[i].Doc));
                BinaryDocValues dv3 = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv1");
                dv3.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestTwoDocumentsNumeric()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 1));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 2));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv");
            Assert.AreEqual(1, dv.Get(0));
            Assert.AreEqual(2, dv.Get(1));

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestTwoDocumentsMerged()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(NewField("id", "0", StringField.TYPE_STORED));
            doc.Add(new NumericDocValuesField("dv", -10));
            iwriter.AddDocument(doc);
            iwriter.Commit();
            doc = new Document();
            doc.Add(NewField("id", "1", StringField.TYPE_STORED));
            doc.Add(new NumericDocValuesField("dv", 99));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv");
            for (int i = 0; i < 2; i++)
            {
                Document doc2 = ((AtomicReader)ireader.Leaves[0].Reader).Document(i);
                long expected;
                if (doc2.Get("id").Equals("0"))
                {
                    expected = -10;
                }
                else
                {
                    expected = 99;
                }
                Assert.AreEqual(expected, dv.Get(i));
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestBigNumericRange()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", long.MinValue));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("dv", long.MaxValue));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv");
            Assert.AreEqual(long.MinValue, dv.Get(0));
            Assert.AreEqual(long.MaxValue, dv.Get(1));

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestBigNumericRange2()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", -8841491950446638677L));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 9062230939892376225L));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv");
            Assert.AreEqual(-8841491950446638677L, dv.Get(0));
            Assert.AreEqual(9062230939892376225L, dv.Get(1));

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            doc.Add(NewTextField("fieldname", text, Field.Store.YES));
            doc.Add(new BinaryDocValuesField("dv", new BytesRef("hello world")));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = new IndexSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            BytesRef scratch = new BytesRef();
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                Debug.Assert(ireader.Leaves.Count == 1);
                BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
                dv.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestBytesTwoDocumentsMerged()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(NewField("id", "0", StringField.TYPE_STORED));
            doc.Add(new BinaryDocValuesField("dv", new BytesRef("hello world 1")));
            iwriter.AddDocument(doc);
            iwriter.Commit();
            doc = new Document();
            doc.Add(NewField("id", "1", StringField.TYPE_STORED));
            doc.Add(new BinaryDocValuesField("dv", new BytesRef("hello 2")));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < 2; i++)
            {
                Document doc2 = ((AtomicReader)ireader.Leaves[0].Reader).Document(i);
                string expected;
                if (doc2.Get("id").Equals("0"))
                {
                    expected = "hello world 1";
                }
                else
                {
                    expected = "hello 2";
                }
                dv.Get(i, scratch);
                Assert.AreEqual(expected, scratch.Utf8ToString());
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            doc.Add(NewTextField("fieldname", text, Field.Store.YES));
            doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world")));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = new IndexSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            BytesRef scratch = new BytesRef();
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                Debug.Assert(ireader.Leaves.Count == 1);
                SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
                dv.LookupOrd(dv.GetOrd(hits.ScoreDocs[i].Doc), scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedBytesTwoDocuments()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.LookupOrd(dv.GetOrd(0), scratch);
            Assert.AreEqual("hello world 1", scratch.Utf8ToString());
            dv.LookupOrd(dv.GetOrd(1), scratch);
            Assert.AreEqual("hello world 2", scratch.Utf8ToString());

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedBytesThreeDocuments()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            Assert.AreEqual(2, dv.ValueCount);
            BytesRef scratch = new BytesRef();
            Assert.AreEqual(0, dv.GetOrd(0));
            dv.LookupOrd(0, scratch);
            Assert.AreEqual("hello world 1", scratch.Utf8ToString());
            Assert.AreEqual(1, dv.GetOrd(1));
            dv.LookupOrd(1, scratch);
            Assert.AreEqual("hello world 2", scratch.Utf8ToString());
            Assert.AreEqual(0, dv.GetOrd(2));

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedBytesTwoDocumentsMerged()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(NewField("id", "0", StringField.TYPE_STORED));
            doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
            iwriter.AddDocument(doc);
            iwriter.Commit();
            doc = new Document();
            doc.Add(NewField("id", "1", StringField.TYPE_STORED));
            doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            Assert.AreEqual(2, dv.ValueCount); // 2 ords
            BytesRef scratch = new BytesRef();
            dv.LookupOrd(0, scratch);
            Assert.AreEqual(new BytesRef("hello world 1"), scratch);
            dv.LookupOrd(1, scratch);
            Assert.AreEqual(new BytesRef("hello world 2"), scratch);
            for (int i = 0; i < 2; i++)
            {
                Document doc2 = ((AtomicReader)ireader.Leaves[0].Reader).Document(i);
                string expected;
                if (doc2.Get("id").Equals("0"))
                {
                    expected = "hello world 1";
                }
                else
                {
                    expected = "hello world 2";
                }
                dv.LookupOrd(dv.GetOrd(i), scratch);
                Assert.AreEqual(expected, scratch.Utf8ToString());
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedMergeAwayAllValues()
        {
            Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

            Document doc = new Document();
            doc.Add(new StringField("id", "0", Field.Store.NO));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new StringField("id", "1", Field.Store.NO));
            doc.Add(new SortedDocValuesField("field", new BytesRef("hello")));
            iwriter.AddDocument(doc);
            iwriter.Commit();
            iwriter.DeleteDocuments(new Term("id", "1"));
            iwriter.ForceMerge(1);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedDocValues dv = GetOnlySegmentReader(ireader).GetSortedDocValues("field");
            if (DefaultCodecSupportsDocsWithField())
            {
                Assert.AreEqual(-1, dv.GetOrd(0));
                Assert.AreEqual(0, dv.ValueCount);
            }
            else
            {
                Assert.AreEqual(0, dv.GetOrd(0));
                Assert.AreEqual(1, dv.ValueCount);
                BytesRef @ref = new BytesRef();
                dv.LookupOrd(0, @ref);
                Assert.AreEqual(new BytesRef(), @ref);
            }

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestBytesWithNewline()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new BinaryDocValuesField("dv", new BytesRef("hello\nworld\r1")));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual(new BytesRef("hello\nworld\r1"), scratch);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestMissingSortedBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
            iwriter.AddDocument(doc);
            // 2nd doc missing the DV field
            iwriter.AddDocument(new Document());
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.LookupOrd(dv.GetOrd(0), scratch);
            Assert.AreEqual(new BytesRef("hello world 2"), scratch);
            if (DefaultCodecSupportsDocsWithField())
            {
                Assert.AreEqual(-1, dv.GetOrd(1));
            }
            dv.Get(1, scratch);
            Assert.AreEqual(new BytesRef(""), scratch);
            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedTermsEnum()
        {
            Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

            Document doc = new Document();
            doc.Add(new SortedDocValuesField("field", new BytesRef("hello")));
            iwriter.AddDocument(doc);

            doc = new Document();
            doc.Add(new SortedDocValuesField("field", new BytesRef("world")));
            iwriter.AddDocument(doc);

            doc = new Document();
            doc.Add(new SortedDocValuesField("field", new BytesRef("beer")));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedDocValues dv = GetOnlySegmentReader(ireader).GetSortedDocValues("field");
            Assert.AreEqual(3, dv.ValueCount);

            TermsEnum termsEnum = dv.TermsEnum();

            // next()
            Assert.AreEqual("beer", termsEnum.Next().Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord());
            Assert.AreEqual("hello", termsEnum.Next().Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord());
            Assert.AreEqual("world", termsEnum.Next().Utf8ToString());
            Assert.AreEqual(2, termsEnum.Ord());

            // seekCeil()
            Assert.AreEqual(SeekStatus.NOT_FOUND, termsEnum.SeekCeil(new BytesRef("ha!")));
            Assert.AreEqual("hello", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord());
            Assert.AreEqual(SeekStatus.FOUND, termsEnum.SeekCeil(new BytesRef("beer")));
            Assert.AreEqual("beer", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord());
            Assert.AreEqual(SeekStatus.END, termsEnum.SeekCeil(new BytesRef("zzz")));

            // seekExact()
            Assert.IsTrue(termsEnum.SeekExact(new BytesRef("beer")));
            Assert.AreEqual("beer", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord());
            Assert.IsTrue(termsEnum.SeekExact(new BytesRef("hello")));
            Assert.AreEqual("hello", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord());
            Assert.IsTrue(termsEnum.SeekExact(new BytesRef("world")));
            Assert.AreEqual("world", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(2, termsEnum.Ord());
            Assert.IsFalse(termsEnum.SeekExact(new BytesRef("bogus")));

            // seek(ord)
            termsEnum.SeekExact(0);
            Assert.AreEqual("beer", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord());
            termsEnum.SeekExact(1);
            Assert.AreEqual("hello", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord());
            termsEnum.SeekExact(2);
            Assert.AreEqual("world", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(2, termsEnum.Ord());
            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestEmptySortedBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("")));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("")));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            BytesRef scratch = new BytesRef();
            Assert.AreEqual(0, dv.GetOrd(0));
            Assert.AreEqual(0, dv.GetOrd(1));
            dv.LookupOrd(dv.GetOrd(0), scratch);
            Assert.AreEqual("", scratch.Utf8ToString());

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestEmptyBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new BinaryDocValuesField("dv", new BytesRef("")));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new BinaryDocValuesField("dv", new BytesRef("")));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual("", scratch.Utf8ToString());
            dv.Get(1, scratch);
            Assert.AreEqual("", scratch.Utf8ToString());

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestVeryLargeButLegalBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            var bytes = new byte[32766];
            BytesRef b = new BytesRef(bytes);
            Random().NextBytes(bytes);
            doc.Add(new BinaryDocValuesField("dv", b));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual(new BytesRef(bytes), scratch);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestVeryLargeButLegalSortedBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            var bytes = new byte[32766];
            BytesRef b = new BytesRef(bytes);
            Random().NextBytes(bytes);
            doc.Add(new SortedDocValuesField("dv", b));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual(new BytesRef(bytes), scratch);
            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestCodecUsesOwnBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new BinaryDocValuesField("dv", new BytesRef("boo!")));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            var mybytes = new byte[20];
            BytesRef scratch = new BytesRef(mybytes);
            dv.Get(0, scratch);
            Assert.AreEqual("boo!", scratch.Utf8ToString());
            Assert.IsFalse(scratch.Bytes == mybytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestCodecUsesOwnSortedBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("boo!")));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            var mybytes = new byte[20];
            BytesRef scratch = new BytesRef(mybytes);
            dv.Get(0, scratch);
            Assert.AreEqual("boo!", scratch.Utf8ToString());
            Assert.IsFalse(scratch.Bytes == mybytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestCodecUsesOwnBytesEachTime()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new BinaryDocValuesField("dv", new BytesRef("foo!")));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new BinaryDocValuesField("dv", new BytesRef("bar!")));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual("foo!", scratch.Utf8ToString());

            BytesRef scratch2 = new BytesRef();
            dv.Get(1, scratch2);
            Assert.AreEqual("bar!", scratch2.Utf8ToString());
            // check scratch is still valid
            Assert.AreEqual("foo!", scratch.Utf8ToString());

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestCodecUsesOwnSortedBytesEachTime()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo!")));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("bar!")));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            Debug.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual("foo!", scratch.Utf8ToString());

            BytesRef scratch2 = new BytesRef();
            dv.Get(1, scratch2);
            Assert.AreEqual("bar!", scratch2.Utf8ToString());
            // check scratch is still valid
            Assert.AreEqual("foo!", scratch.Utf8ToString());

            ireader.Dispose();
            directory.Dispose();
        }

        /*
         * Simple test case to show how to use the API
         */
        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestDocValuesSimple()
        {
            Directory dir = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            IndexWriter writer = new IndexWriter(dir, conf);
            for (int i = 0; i < 5; i++)
            {
                Document doc = new Document();
                doc.Add(new NumericDocValuesField("docId", i));
                doc.Add(new TextField("docId", "" + i, Field.Store.NO));
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.ForceMerge(1, true);

            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir, 1);
            Assert.AreEqual(1, reader.Leaves.Count);

            IndexSearcher searcher = new IndexSearcher(reader);

            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("docId", "0")), BooleanClause.Occur.SHOULD);
            query.Add(new TermQuery(new Term("docId", "1")), BooleanClause.Occur.SHOULD);
            query.Add(new TermQuery(new Term("docId", "2")), BooleanClause.Occur.SHOULD);
            query.Add(new TermQuery(new Term("docId", "3")), BooleanClause.Occur.SHOULD);
            query.Add(new TermQuery(new Term("docId", "4")), BooleanClause.Occur.SHOULD);

            TopDocs search = searcher.Search(query, 10);
            Assert.AreEqual(5, search.TotalHits);
            ScoreDoc[] scoreDocs = search.ScoreDocs;
            NumericDocValues docValues = GetOnlySegmentReader(reader).GetNumericDocValues("docId");
            for (int i = 0; i < scoreDocs.Length; i++)
            {
                Assert.AreEqual(i, scoreDocs[i].Doc);
                Assert.AreEqual(i, docValues.Get(scoreDocs[i].Doc));
            }
            reader.Dispose();
            dir.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestRandomSortedBytes()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig cfg = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            if (!DefaultCodecSupportsDocsWithField())
            {
                // if the codec doesnt support missing, we expect missing to be mapped to byte[]
                // by the impersonator, but we have to give it a chance to merge them to this
                cfg.SetMergePolicy(NewLogMergePolicy());
            }
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, cfg);
            int numDocs = AtLeast(100);
            BytesRefHash hash = new BytesRefHash();
            IDictionary<string, string> docToString = new Dictionary<string, string>();
            int maxLength = TestUtil.NextInt(Random(), 1, 50);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("id", "" + i, Field.Store.YES));
                string @string = TestUtil.RandomRealisticUnicodeString(Random(), 1, maxLength);
                BytesRef br = new BytesRef(@string);
                doc.Add(new SortedDocValuesField("field", br));
                hash.Add(br);
                docToString["" + i] = @string;
                w.AddDocument(doc);
            }
            if (Rarely())
            {
                w.Commit();
            }
            int numDocsNoValue = AtLeast(10);
            for (int i = 0; i < numDocsNoValue; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("id", "noValue", Field.Store.YES));
                w.AddDocument(doc);
            }
            if (!DefaultCodecSupportsDocsWithField())
            {
                BytesRef bytesRef = new BytesRef();
                hash.Add(bytesRef); // add empty value for the gaps
            }
            if (Rarely())
            {
                w.Commit();
            }
            if (!DefaultCodecSupportsDocsWithField())
            {
                // if the codec doesnt support missing, we expect missing to be mapped to byte[]
                // by the impersonator, but we have to give it a chance to merge them to this
                w.ForceMerge(1);
            }
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                string id = "" + i + numDocs;
                doc.Add(NewTextField("id", id, Field.Store.YES));
                string @string = TestUtil.RandomRealisticUnicodeString(Random(), 1, maxLength);
                BytesRef br = new BytesRef(@string);
                hash.Add(br);
                docToString[id] = @string;
                doc.Add(new SortedDocValuesField("field", br));
                w.AddDocument(doc);
            }
            w.Commit();
            IndexReader reader = w.Reader;
            SortedDocValues docValues = MultiDocValues.GetSortedValues(reader, "field");
            int[] sort = hash.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
            BytesRef expected = new BytesRef();
            BytesRef actual = new BytesRef();
            Assert.AreEqual(hash.Size(), docValues.ValueCount);
            for (int i = 0; i < hash.Size(); i++)
            {
                hash.Get(sort[i], expected);
                docValues.LookupOrd(i, actual);
                Assert.AreEqual(expected.Utf8ToString(), actual.Utf8ToString());
                int ord = docValues.LookupTerm(expected);
                Assert.AreEqual(i, ord);
            }
            AtomicReader slowR = SlowCompositeReaderWrapper.Wrap(reader);
            ISet<KeyValuePair<string, string>> entrySet = docToString.EntrySet();

            foreach (KeyValuePair<string, string> entry in entrySet)
            {
                // pk lookup
                DocsEnum termDocsEnum = slowR.TermDocsEnum(new Term("id", entry.Key));
                int docId = termDocsEnum.NextDoc();
                expected = new BytesRef(entry.Value);
                docValues.Get(docId, actual);
                Assert.AreEqual(expected, actual);
            }

            reader.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        internal abstract class LongProducer
        {
            internal abstract long Next();
        }

        private void DoTestNumericsVsStoredFields(long minValue, long maxValue)
        {
            DoTestNumericsVsStoredFields(new LongProducerAnonymousInnerClassHelper(this, minValue, maxValue));
        }

        private class LongProducerAnonymousInnerClassHelper : LongProducer
        {
            private readonly BaseDocValuesFormatTestCase OuterInstance;

            private long MinValue;
            private long MaxValue;

            public LongProducerAnonymousInnerClassHelper(BaseDocValuesFormatTestCase outerInstance, long minValue, long maxValue)
            {
                this.OuterInstance = outerInstance;
                this.MinValue = minValue;
                this.MaxValue = maxValue;
            }

            internal override long Next()
            {
                return TestUtil.NextLong(Random(), MinValue, MaxValue);
            }
        }

        private void DoTestNumericsVsStoredFields(LongProducer longs)
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
            Document doc = new Document();
            Field idField = new StringField("id", "", Field.Store.NO);
            Field storedField = NewStringField("stored", "", Field.Store.YES);
            Field dvField = new NumericDocValuesField("dv", 0);
            doc.Add(idField);
            doc.Add(storedField);
            doc.Add(dvField);

            // index some docs
            int numDocs = AtLeast(300);
            // numDocs should be always > 256 so that in case of a codec that optimizes
            // for numbers of values <= 256, all storage layouts are tested
            Debug.Assert(numDocs > 256);
            for (int i = 0; i < numDocs; i++)
            {
                idField.StringValue = Convert.ToString(i);
                long value = longs.Next();
                storedField.StringValue = Convert.ToString(value);
                dvField.LongValue = value;
                writer.AddDocument(doc);
                if (Random().Next(31) == 0)
                {
                    writer.Commit();
                }
            }

            // delete some docs
            int numDeletions = Random().Next(numDocs / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                int id = Random().Next(numDocs);
                writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
            }

            // merge some segments and ensure that at least one of them has more than
            // 256 values
            writer.ForceMerge(numDocs / 256);

            writer.Dispose();

            // compare
            DirectoryReader ir = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                NumericDocValues docValues = r.GetNumericDocValues("dv");
                for (int i = 0; i < r.MaxDoc; i++)
                {
                    long storedValue = Convert.ToInt64(r.Document(i).Get("stored"));
                    Assert.AreEqual(storedValue, docValues.Get(i));
                }
            }
            ir.Dispose();
            dir.Dispose();
        }

        private void DoTestMissingVsFieldCache(long minValue, long maxValue)
        {
            DoTestMissingVsFieldCache(new LongProducerAnonymousInnerClassHelper2(this, minValue, maxValue));
        }

        private class LongProducerAnonymousInnerClassHelper2 : LongProducer
        {
            private readonly BaseDocValuesFormatTestCase OuterInstance;

            private long MinValue;
            private long MaxValue;

            public LongProducerAnonymousInnerClassHelper2(BaseDocValuesFormatTestCase outerInstance, long minValue, long maxValue)
            {
                this.OuterInstance = outerInstance;
                this.MinValue = minValue;
                this.MaxValue = maxValue;
            }

            internal override long Next()
            {
                return TestUtil.NextLong(Random(), MinValue, MaxValue);
            }
        }

        private void DoTestMissingVsFieldCache(LongProducer longs)
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField());
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
            Field idField = new StringField("id", "", Field.Store.NO);
            Field indexedField = NewStringField("indexed", "", Field.Store.NO);
            Field dvField = new NumericDocValuesField("dv", 0);

            // index some docs
            int numDocs = AtLeast(300);
            // numDocs should be always > 256 so that in case of a codec that optimizes
            // for numbers of values <= 256, all storage layouts are tested
            Debug.Assert(numDocs > 256);
            for (int i = 0; i < numDocs; i++)
            {
                idField.StringValue = Convert.ToString(i);
                long value = longs.Next();
                indexedField.StringValue = Convert.ToString(value);
                dvField.LongValue = value;
                Document doc = new Document();
                doc.Add(idField);
                // 1/4 of the time we neglect to add the fields
                if (Random().Next(4) > 0)
                {
                    doc.Add(indexedField);
                    doc.Add(dvField);
                }
                writer.AddDocument(doc);
                if (Random().Next(31) == 0)
                {
                    writer.Commit();
                }
            }

            // delete some docs
            int numDeletions = Random().Next(numDocs / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                int id = Random().Next(numDocs);
                writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
            }

            // merge some segments and ensure that at least one of them has more than
            // 256 values
            writer.ForceMerge(numDocs / 256);

            writer.Dispose();

            // compare
            DirectoryReader ir = DirectoryReader.Open(dir);
            foreach (var context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                Bits expected = FieldCache.DEFAULT.GetDocsWithField(r, "indexed");
                Bits actual = FieldCache.DEFAULT.GetDocsWithField(r, "dv");
                AssertEquals(expected, actual);
            }
            ir.Dispose();
            dir.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestBooleanNumericsVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestNumericsVsStoredFields(0, 1);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestByteNumericsVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestNumericsVsStoredFields(sbyte.MinValue, sbyte.MaxValue);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestByteMissingVsFieldCache()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestMissingVsFieldCache(sbyte.MinValue, sbyte.MaxValue);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestShortNumericsVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestNumericsVsStoredFields(short.MinValue, short.MaxValue);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestShortMissingVsFieldCache()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestMissingVsFieldCache(short.MinValue, short.MaxValue);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestIntNumericsVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestNumericsVsStoredFields(int.MinValue, int.MaxValue);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestIntMissingVsFieldCache()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestMissingVsFieldCache(int.MinValue, int.MaxValue);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestLongNumericsVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestNumericsVsStoredFields(long.MinValue, long.MaxValue);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestLongMissingVsFieldCache()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestMissingVsFieldCache(long.MinValue, long.MaxValue);
            }
        }

        private void DoTestBinaryVsStoredFields(int minLength, int maxLength)
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
            Document doc = new Document();
            Field idField = new StringField("id", "", Field.Store.NO);
            Field storedField = new StoredField("stored", new byte[0]);
            Field dvField = new BinaryDocValuesField("dv", new BytesRef());
            doc.Add(idField);
            doc.Add(storedField);
            doc.Add(dvField);

            // index some docs
            int numDocs = AtLeast(300);
            for (int i = 0; i < numDocs; i++)
            {
                idField.StringValue = Convert.ToString(i);
                int length;
                if (minLength == maxLength)
                {
                    length = minLength; // fixed length
                }
                else
                {
                    length = TestUtil.NextInt(Random(), minLength, maxLength);
                }
                var buffer = new byte[length];
                Random().NextBytes(buffer);
                storedField.BytesValue = new BytesRef(buffer);
                dvField.BytesValue = new BytesRef(buffer);
                writer.AddDocument(doc);
                if (Random().Next(31) == 0)
                {
                    writer.Commit();
                }
            }

            // delete some docs
            int numDeletions = Random().Next(numDocs / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                int id = Random().Next(numDocs);
                writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
            }
            writer.Dispose();

            // compare
            DirectoryReader ir = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                BinaryDocValues docValues = r.GetBinaryDocValues("dv");
                for (int i = 0; i < r.MaxDoc; i++)
                {
                    BytesRef binaryValue = r.Document(i).GetBinaryValue("stored");
                    BytesRef scratch = new BytesRef();
                    docValues.Get(i, scratch);
                    Assert.AreEqual(binaryValue, scratch);
                }
            }
            ir.Dispose();
            dir.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestBinaryFixedLengthVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt(Random(), 0, 10);
                DoTestBinaryVsStoredFields(fixedLength, fixedLength);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestBinaryVariableLengthVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestBinaryVsStoredFields(0, 10);
            }
        }

        private void DoTestSortedVsStoredFields(int minLength, int maxLength)
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
            Document doc = new Document();
            Field idField = new StringField("id", "", Field.Store.NO);
            Field storedField = new StoredField("stored", new byte[0]);
            Field dvField = new SortedDocValuesField("dv", new BytesRef());
            doc.Add(idField);
            doc.Add(storedField);
            doc.Add(dvField);

            // index some docs
            int numDocs = AtLeast(300);
            for (int i = 0; i < numDocs; i++)
            {
                idField.StringValue = Convert.ToString(i);
                int length;
                if (minLength == maxLength)
                {
                    length = minLength; // fixed length
                }
                else
                {
                    length = TestUtil.NextInt(Random(), minLength, maxLength);
                }
                var buffer = new byte[length];
                Random().NextBytes(buffer);
                storedField.BytesValue = new BytesRef(buffer);
                dvField.BytesValue = new BytesRef(buffer);
                writer.AddDocument(doc);
                if (Random().Next(31) == 0)
                {
                    writer.Commit();
                }
            }

            // delete some docs
            int numDeletions = Random().Next(numDocs / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                int id = Random().Next(numDocs);
                writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
            }
            writer.Dispose();

            // compare
            DirectoryReader ir = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                BinaryDocValues docValues = r.GetSortedDocValues("dv");
                for (int i = 0; i < r.MaxDoc; i++)
                {
                    BytesRef binaryValue = r.Document(i).GetBinaryValue("stored");
                    BytesRef scratch = new BytesRef();
                    docValues.Get(i, scratch);
                    Assert.AreEqual(binaryValue, scratch);
                }
            }
            ir.Dispose();
            dir.Dispose();
        }

        private void DoTestSortedVsFieldCache(int minLength, int maxLength)
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
            Document doc = new Document();
            Field idField = new StringField("id", "", Field.Store.NO);
            Field indexedField = new StringField("indexed", "", Field.Store.NO);
            Field dvField = new SortedDocValuesField("dv", new BytesRef());
            doc.Add(idField);
            doc.Add(indexedField);
            doc.Add(dvField);

            // index some docs
            int numDocs = AtLeast(300);
            for (int i = 0; i < numDocs; i++)
            {
                idField.StringValue = Convert.ToString(i);
                int length;
                if (minLength == maxLength)
                {
                    length = minLength; // fixed length
                }
                else
                {
                    length = TestUtil.NextInt(Random(), minLength, maxLength);
                }
                string value = TestUtil.RandomSimpleString(Random(), length);
                indexedField.StringValue = value;
                dvField.BytesValue = new BytesRef(value);
                writer.AddDocument(doc);
                if (Random().Next(31) == 0)
                {
                    writer.Commit();
                }
            }

            // delete some docs
            int numDeletions = Random().Next(numDocs / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                int id = Random().Next(numDocs);
                writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
            }
            writer.Dispose();

            // compare
            DirectoryReader ir = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                SortedDocValues expected = FieldCache.DEFAULT.GetTermsIndex(r, "indexed");
                SortedDocValues actual = r.GetSortedDocValues("dv");
                AssertEquals(r.MaxDoc, expected, actual);
            }
            ir.Dispose();
            dir.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedFixedLengthVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt(Random(), 1, 10);
                DoTestSortedVsStoredFields(fixedLength, fixedLength);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedFixedLengthVsFieldCache()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt(Random(), 1, 10);
                DoTestSortedVsFieldCache(fixedLength, fixedLength);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedVariableLengthVsFieldCache()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestSortedVsFieldCache(1, 10);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedVariableLengthVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestSortedVsStoredFields(1, 10);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetOneValue()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            iwriter.AddDocument(doc);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");

            dv.Document = 0;
            Assert.AreEqual(0, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            BytesRef bytes = new BytesRef();
            dv.LookupOrd(0, bytes);
            Assert.AreEqual(new BytesRef("hello"), bytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetTwoFields()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            doc.Add(new SortedSetDocValuesField("field2", new BytesRef("world")));
            iwriter.AddDocument(doc);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");

            dv.Document = 0;
            Assert.AreEqual(0, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            BytesRef bytes = new BytesRef();
            dv.LookupOrd(0, bytes);
            Assert.AreEqual(new BytesRef("hello"), bytes);

            dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field2");

            dv.Document = 0;
            Assert.AreEqual(0, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            dv.LookupOrd(0, bytes);
            Assert.AreEqual(new BytesRef("world"), bytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetTwoDocumentsMerged()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            iwriter.AddDocument(doc);
            iwriter.Commit();

            doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
            Assert.AreEqual(2, dv.ValueCount);

            dv.Document = 0;
            Assert.AreEqual(0, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            BytesRef bytes = new BytesRef();
            dv.LookupOrd(0, bytes);
            Assert.AreEqual(new BytesRef("hello"), bytes);

            dv.Document = 1;
            Assert.AreEqual(1, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            dv.LookupOrd(1, bytes);
            Assert.AreEqual(new BytesRef("world"), bytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetTwoValues()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
            iwriter.AddDocument(doc);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");

            dv.Document = 0;
            Assert.AreEqual(0, dv.NextOrd());
            Assert.AreEqual(1, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            BytesRef bytes = new BytesRef();
            dv.LookupOrd(0, bytes);
            Assert.AreEqual(new BytesRef("hello"), bytes);

            dv.LookupOrd(1, bytes);
            Assert.AreEqual(new BytesRef("world"), bytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetTwoValuesUnordered()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            iwriter.AddDocument(doc);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");

            dv.Document = 0;
            Assert.AreEqual(0, dv.NextOrd());
            Assert.AreEqual(1, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            BytesRef bytes = new BytesRef();
            dv.LookupOrd(0, bytes);
            Assert.AreEqual(new BytesRef("hello"), bytes);

            dv.LookupOrd(1, bytes);
            Assert.AreEqual(new BytesRef("world"), bytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetThreeValuesTwoDocs()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
            iwriter.AddDocument(doc);
            iwriter.Commit();

            doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("beer")));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
            Assert.AreEqual(3, dv.ValueCount);

            dv.Document = 0;
            Assert.AreEqual(1, dv.NextOrd());
            Assert.AreEqual(2, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            dv.Document = 1;
            Assert.AreEqual(0, dv.NextOrd());
            Assert.AreEqual(1, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            BytesRef bytes = new BytesRef();
            dv.LookupOrd(0, bytes);
            Assert.AreEqual(new BytesRef("beer"), bytes);

            dv.LookupOrd(1, bytes);
            Assert.AreEqual(new BytesRef("hello"), bytes);

            dv.LookupOrd(2, bytes);
            Assert.AreEqual(new BytesRef("world"), bytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetTwoDocumentsLastMissing()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            iwriter.AddDocument(doc);

            doc = new Document();
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
            Assert.AreEqual(1, dv.ValueCount);

            dv.Document = 0;
            Assert.AreEqual(0, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            BytesRef bytes = new BytesRef();
            dv.LookupOrd(0, bytes);
            Assert.AreEqual(new BytesRef("hello"), bytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetTwoDocumentsLastMissingMerge()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            iwriter.AddDocument(doc);
            iwriter.Commit();

            doc = new Document();
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
            Assert.AreEqual(1, dv.ValueCount);

            dv.Document = 0;
            Assert.AreEqual(0, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            BytesRef bytes = new BytesRef();
            dv.LookupOrd(0, bytes);
            Assert.AreEqual(new BytesRef("hello"), bytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetTwoDocumentsFirstMissing()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

            Document doc = new Document();
            iwriter.AddDocument(doc);

            doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            iwriter.AddDocument(doc);

            iwriter.ForceMerge(1);
            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
            Assert.AreEqual(1, dv.ValueCount);

            dv.Document = 1;
            Assert.AreEqual(0, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            BytesRef bytes = new BytesRef();
            dv.LookupOrd(0, bytes);
            Assert.AreEqual(new BytesRef("hello"), bytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetTwoDocumentsFirstMissingMerge()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

            Document doc = new Document();
            iwriter.AddDocument(doc);
            iwriter.Commit();

            doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
            Assert.AreEqual(1, dv.ValueCount);

            dv.Document = 1;
            Assert.AreEqual(0, dv.NextOrd());
            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

            BytesRef bytes = new BytesRef();
            dv.LookupOrd(0, bytes);
            Assert.AreEqual(new BytesRef("hello"), bytes);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetMergeAwayAllValues()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

            Document doc = new Document();
            doc.Add(new StringField("id", "0", Field.Store.NO));
            iwriter.AddDocument(doc);
            doc = new Document();
            doc.Add(new StringField("id", "1", Field.Store.NO));
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            iwriter.AddDocument(doc);
            iwriter.Commit();
            iwriter.DeleteDocuments(new Term("id", "1"));
            iwriter.ForceMerge(1);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
            Assert.AreEqual(0, dv.ValueCount);

            ireader.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetTermsEnum()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
            doc.Add(new SortedSetDocValuesField("field", new BytesRef("beer")));
            iwriter.AddDocument(doc);

            DirectoryReader ireader = iwriter.Reader;
            iwriter.Dispose();

            SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
            Assert.AreEqual(3, dv.ValueCount);

            TermsEnum termsEnum = dv.TermsEnum();

            // next()
            Assert.AreEqual("beer", termsEnum.Next().Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord());
            Assert.AreEqual("hello", termsEnum.Next().Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord());
            Assert.AreEqual("world", termsEnum.Next().Utf8ToString());
            Assert.AreEqual(2, termsEnum.Ord());

            // seekCeil()
            Assert.AreEqual(SeekStatus.NOT_FOUND, termsEnum.SeekCeil(new BytesRef("ha!")));
            Assert.AreEqual("hello", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord());
            Assert.AreEqual(SeekStatus.FOUND, termsEnum.SeekCeil(new BytesRef("beer")));
            Assert.AreEqual("beer", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord());
            Assert.AreEqual(SeekStatus.END, termsEnum.SeekCeil(new BytesRef("zzz")));

            // seekExact()
            Assert.IsTrue(termsEnum.SeekExact(new BytesRef("beer")));
            Assert.AreEqual("beer", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord());
            Assert.IsTrue(termsEnum.SeekExact(new BytesRef("hello")));
            Assert.AreEqual("hello", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord());
            Assert.IsTrue(termsEnum.SeekExact(new BytesRef("world")));
            Assert.AreEqual("world", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(2, termsEnum.Ord());
            Assert.IsFalse(termsEnum.SeekExact(new BytesRef("bogus")));

            // seek(ord)
            termsEnum.SeekExact(0);
            Assert.AreEqual("beer", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(0, termsEnum.Ord());
            termsEnum.SeekExact(1);
            Assert.AreEqual("hello", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(1, termsEnum.Ord());
            termsEnum.SeekExact(2);
            Assert.AreEqual("world", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(2, termsEnum.Ord());
            ireader.Dispose();
            directory.Dispose();
        }

        private void DoTestSortedSetVsStoredFields(int minLength, int maxLength, int maxValuesPerDoc)
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);

            // index some docs
            int numDocs = AtLeast(300);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                Field idField = new StringField("id", Convert.ToString(i), Field.Store.NO);
                doc.Add(idField);
                int length;
                if (minLength == maxLength)
                {
                    length = minLength; // fixed length
                }
                else
                {
                    length = TestUtil.NextInt(Random(), minLength, maxLength);
                }
                int numValues = TestUtil.NextInt(Random(), 0, maxValuesPerDoc);
                // create a random set of strings
                SortedSet<string> values = new SortedSet<string>();
                for (int v = 0; v < numValues; v++)
                {
                    values.Add(TestUtil.RandomSimpleString(Random(), length));
                }

                // add ordered to the stored field
                foreach (string v in values)
                {
                    doc.Add(new StoredField("stored", v));
                }

                // add in any order to the dv field
                IList<string> unordered = new List<string>(values);
                unordered = CollectionsHelper.Shuffle(unordered);
                foreach (string v in unordered)
                {
                    doc.Add(new SortedSetDocValuesField("dv", new BytesRef(v)));
                }

                writer.AddDocument(doc);
                if (Random().Next(31) == 0)
                {
                    writer.Commit();
                }
            }

            // delete some docs
            int numDeletions = Random().Next(numDocs / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                int id = Random().Next(numDocs);
                writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
            }
            writer.Dispose();

            // compare
            DirectoryReader ir = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                SortedSetDocValues docValues = r.GetSortedSetDocValues("dv");
                BytesRef scratch = new BytesRef();
                for (int i = 0; i < r.MaxDoc; i++)
                {
                    string[] stringValues = r.Document(i).GetValues("stored");
                    if (docValues != null)
                    {
                        docValues.Document = i;
                    }
                    for (int j = 0; j < stringValues.Length; j++)
                    {
                        Debug.Assert(docValues != null);
                        long ord = docValues.NextOrd();
                        Debug.Assert(ord != SortedSetDocValues.NO_MORE_ORDS);
                        docValues.LookupOrd(ord, scratch);
                        Assert.AreEqual(stringValues[j], scratch.Utf8ToString());
                    }
                    Debug.Assert(docValues == null || docValues.NextOrd() == SortedSetDocValues.NO_MORE_ORDS);
                }
            }
            ir.Dispose();
            dir.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetFixedLengthVsStoredFields()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt(Random(), 1, 10);
                DoTestSortedSetVsStoredFields(fixedLength, fixedLength, 16);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetVariableLengthVsStoredFields()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestSortedSetVsStoredFields(1, 10, 16);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetFixedLengthSingleValuedVsStoredFields()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt(Random(), 1, 10);
                DoTestSortedSetVsStoredFields(fixedLength, fixedLength, 1);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetVariableLengthSingleValuedVsStoredFields()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestSortedSetVsStoredFields(1, 10, 1);
            }
        }

        private void AssertEquals(Bits expected, Bits actual)
        {
            Assert.AreEqual(expected.Length(), actual.Length());
            for (int i = 0; i < expected.Length(); i++)
            {
                Assert.AreEqual(expected.Get(i), actual.Get(i));
            }
        }

        private void AssertEquals(int maxDoc, SortedDocValues expected, SortedDocValues actual)
        {
            AssertEquals(maxDoc, new SingletonSortedSetDocValues(expected), new SingletonSortedSetDocValues(actual));
        }

        private void AssertEquals(int maxDoc, SortedSetDocValues expected, SortedSetDocValues actual)
        {
            // can be null for the segment if no docs actually had any SortedDocValues
            // in this case FC.getDocTermsOrds returns EMPTY
            if (actual == null)
            {
                Assert.AreEqual(DocValues.EMPTY_SORTED_SET, expected);
                return;
            }
            Assert.AreEqual(expected.ValueCount, actual.ValueCount);
            // compare ord lists
            for (int i = 0; i < maxDoc; i++)
            {
                expected.Document = i;
                actual.Document = i;
                long expectedOrd;
                while ((expectedOrd = expected.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                {
                    Assert.AreEqual(expectedOrd, actual.NextOrd());
                }
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, actual.NextOrd());
            }

            // compare ord dictionary
            BytesRef expectedBytes = new BytesRef();
            BytesRef actualBytes = new BytesRef();
            for (long i = 0; i < expected.ValueCount; i++)
            {
                expected.LookupTerm(expectedBytes);
                actual.LookupTerm(actualBytes);
                Assert.AreEqual(expectedBytes, actualBytes);
            }

            // compare termsenum
            AssertEquals(expected.ValueCount, expected.TermsEnum(), actual.TermsEnum());
        }

        private void AssertEquals(long numOrds, TermsEnum expected, TermsEnum actual)
        {
            BytesRef @ref;

            // sequential next() through all terms
            while ((@ref = expected.Next()) != null)
            {
                Assert.AreEqual(@ref, actual.Next());
                Assert.AreEqual(expected.Ord(), actual.Ord());
                Assert.AreEqual(expected.Term(), actual.Term());
            }
            Assert.IsNull(actual.Next());

            // sequential seekExact(ord) through all terms
            for (long i = 0; i < numOrds; i++)
            {
                expected.SeekExact(i);
                actual.SeekExact(i);
                Assert.AreEqual(expected.Ord(), actual.Ord());
                Assert.AreEqual(expected.Term(), actual.Term());
            }

            // sequential seekExact(BytesRef) through all terms
            for (long i = 0; i < numOrds; i++)
            {
                expected.SeekExact(i);
                Assert.IsTrue(actual.SeekExact(expected.Term()));
                Assert.AreEqual(expected.Ord(), actual.Ord());
                Assert.AreEqual(expected.Term(), actual.Term());
            }

            // sequential seekCeil(BytesRef) through all terms
            for (long i = 0; i < numOrds; i++)
            {
                expected.SeekExact(i);
                Assert.AreEqual(SeekStatus.FOUND, actual.SeekCeil(expected.Term()));
                Assert.AreEqual(expected.Ord(), actual.Ord());
                Assert.AreEqual(expected.Term(), actual.Term());
            }

            // random seekExact(ord)
            for (long i = 0; i < numOrds; i++)
            {
                long randomOrd = TestUtil.NextLong(Random(), 0, numOrds - 1);
                expected.SeekExact(randomOrd);
                actual.SeekExact(randomOrd);
                Assert.AreEqual(expected.Ord(), actual.Ord());
                Assert.AreEqual(expected.Term(), actual.Term());
            }

            // random seekExact(BytesRef)
            for (long i = 0; i < numOrds; i++)
            {
                long randomOrd = TestUtil.NextLong(Random(), 0, numOrds - 1);
                expected.SeekExact(randomOrd);
                actual.SeekExact(expected.Term());
                Assert.AreEqual(expected.Ord(), actual.Ord());
                Assert.AreEqual(expected.Term(), actual.Term());
            }

            // random seekCeil(BytesRef)
            for (long i = 0; i < numOrds; i++)
            {
                BytesRef target = new BytesRef(TestUtil.RandomUnicodeString(Random()));
                SeekStatus expectedStatus = expected.SeekCeil(target);
                Assert.AreEqual(expectedStatus, actual.SeekCeil(target));
                if (expectedStatus != SeekStatus.END)
                {
                    Assert.AreEqual(expected.Ord(), actual.Ord());
                    Assert.AreEqual(expected.Term(), actual.Term());
                }
            }
        }

        private void DoTestSortedSetVsUninvertedField(int minLength, int maxLength)
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);

            // index some docs
            int numDocs = AtLeast(300);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                Field idField = new StringField("id", Convert.ToString(i), Field.Store.NO);
                doc.Add(idField);
                int length;
                if (minLength == maxLength)
                {
                    length = minLength; // fixed length
                }
                else
                {
                    length = TestUtil.NextInt(Random(), minLength, maxLength);
                }
                int numValues = Random().Next(17);
                // create a random list of strings
                IList<string> values = new List<string>();
                for (int v = 0; v < numValues; v++)
                {
                    values.Add(TestUtil.RandomSimpleString(Random(), length));
                }

                // add in any order to the indexed field
                IList<string> unordered = new List<string>(values);
                unordered = CollectionsHelper.Shuffle(unordered);
                foreach (string v in unordered)
                {
                    doc.Add(NewStringField("indexed", v, Field.Store.NO));
                }

                // add in any order to the dv field
                IList<string> unordered2 = new List<string>(values);
                unordered2 = CollectionsHelper.Shuffle(unordered2);
                foreach (string v in unordered2)
                {
                    doc.Add(new SortedSetDocValuesField("dv", new BytesRef(v)));
                }

                writer.AddDocument(doc);
                if (Random().Next(31) == 0)
                {
                    writer.Commit();
                }
            }

            // delete some docs
            int numDeletions = Random().Next(numDocs / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                int id = Random().Next(numDocs);
                writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
            }

            // compare per-segment
            DirectoryReader ir = writer.Reader;
            foreach (AtomicReaderContext context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                SortedSetDocValues expected = FieldCache.DEFAULT.GetDocTermOrds(r, "indexed");
                SortedSetDocValues actual = r.GetSortedSetDocValues("dv");
                AssertEquals(r.MaxDoc, expected, actual);
            }
            ir.Dispose();

            writer.ForceMerge(1);

            // now compare again after the merge
            ir = writer.Reader;
            AtomicReader ar = GetOnlySegmentReader(ir);
            SortedSetDocValues expected_ = FieldCache.DEFAULT.GetDocTermOrds(ar, "indexed");
            SortedSetDocValues actual_ = ar.GetSortedSetDocValues("dv");
            AssertEquals(ir.MaxDoc, expected_, actual_);
            ir.Dispose();

            writer.Dispose();
            dir.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetFixedLengthVsUninvertedField()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt(Random(), 1, 10);
                DoTestSortedSetVsUninvertedField(fixedLength, fixedLength);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSortedSetVariableLengthVsUninvertedField()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestSortedSetVsUninvertedField(1, 10);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestGCDCompression()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                long min = -(((long)Random().Next(1 << 30)) << 32);
                long mul = Random().Next() & 0xFFFFFFFFL;
                LongProducer longs = new LongProducerAnonymousInnerClassHelper3(this, min, mul);
                DoTestNumericsVsStoredFields(longs);
            }
        }

        private class LongProducerAnonymousInnerClassHelper3 : LongProducer
        {
            private readonly BaseDocValuesFormatTestCase OuterInstance;

            private long Min;
            private long Mul;

            public LongProducerAnonymousInnerClassHelper3(BaseDocValuesFormatTestCase outerInstance, long min, long mul)
            {
                this.OuterInstance = outerInstance;
                this.Min = min;
                this.Mul = mul;
            }

            internal override long Next()
            {
                return Min + Mul * Random().Next(1 << 20);
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestZeros()
        {
            DoTestNumericsVsStoredFields(0, 0);
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestZeroOrMin()
        {
            // try to make GCD compression fail if the format did not anticipate that
            // the GCD of 0 and MIN_VALUE is negative
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                LongProducer longs = new LongProducerAnonymousInnerClassHelper4(this);
                DoTestNumericsVsStoredFields(longs);
            }
        }

        private class LongProducerAnonymousInnerClassHelper4 : LongProducer
        {
            private readonly BaseDocValuesFormatTestCase OuterInstance;

            public LongProducerAnonymousInnerClassHelper4(BaseDocValuesFormatTestCase outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            internal override long Next()
            {
                return Random().NextBoolean() ? 0 : long.MinValue;
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestTwoNumbersOneMissing()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField());
            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new StringField("id", "0", Field.Store.YES));
            doc.Add(new NumericDocValuesField("dv1", 0));
            iw.AddDocument(doc);
            doc = new Document();
            doc.Add(new StringField("id", "1", Field.Store.YES));
            iw.AddDocument(doc);
            iw.ForceMerge(1);
            iw.Dispose();

            IndexReader ir = DirectoryReader.Open(directory);
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader ar = (AtomicReader)ir.Leaves[0].Reader;
            NumericDocValues dv = ar.GetNumericDocValues("dv1");
            Assert.AreEqual(0, dv.Get(0));
            Assert.AreEqual(0, dv.Get(1));
            Bits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
            ir.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestTwoNumbersOneMissingWithMerging()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField());
            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new StringField("id", "0", Field.Store.YES));
            doc.Add(new NumericDocValuesField("dv1", 0));
            iw.AddDocument(doc);
            iw.Commit();
            doc = new Document();
            doc.Add(new StringField("id", "1", Field.Store.YES));
            iw.AddDocument(doc);
            iw.ForceMerge(1);
            iw.Dispose();

            IndexReader ir = DirectoryReader.Open(directory);
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader ar = (AtomicReader)ir.Leaves[0].Reader;
            NumericDocValues dv = ar.GetNumericDocValues("dv1");
            Assert.AreEqual(0, dv.Get(0));
            Assert.AreEqual(0, dv.Get(1));
            Bits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
            ir.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestThreeNumbersOneMissingWithMerging()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField());
            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new StringField("id", "0", Field.Store.YES));
            doc.Add(new NumericDocValuesField("dv1", 0));
            iw.AddDocument(doc);
            doc = new Document();
            doc.Add(new StringField("id", "1", Field.Store.YES));
            iw.AddDocument(doc);
            iw.Commit();
            doc = new Document();
            doc.Add(new StringField("id", "2", Field.Store.YES));
            doc.Add(new NumericDocValuesField("dv1", 5));
            iw.AddDocument(doc);
            iw.ForceMerge(1);
            iw.Dispose();

            IndexReader ir = DirectoryReader.Open(directory);
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader ar = (AtomicReader)ir.Leaves[0].Reader;
            NumericDocValues dv = ar.GetNumericDocValues("dv1");
            Assert.AreEqual(0, dv.Get(0));
            Assert.AreEqual(0, dv.Get(1));
            Assert.AreEqual(5, dv.Get(2));
            Bits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
            Assert.IsTrue(docsWithField.Get(2));
            ir.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestTwoBytesOneMissing()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField());
            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new StringField("id", "0", Field.Store.YES));
            doc.Add(new BinaryDocValuesField("dv1", new BytesRef()));
            iw.AddDocument(doc);
            doc = new Document();
            doc.Add(new StringField("id", "1", Field.Store.YES));
            iw.AddDocument(doc);
            iw.ForceMerge(1);
            iw.Dispose();

            IndexReader ir = DirectoryReader.Open(directory);
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader ar = (AtomicReader)ir.Leaves[0].Reader;
            BinaryDocValues dv = ar.GetBinaryDocValues("dv1");
            BytesRef @ref = new BytesRef();
            dv.Get(0, @ref);
            Assert.AreEqual(new BytesRef(), @ref);
            dv.Get(1, @ref);
            Assert.AreEqual(new BytesRef(), @ref);
            Bits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
            ir.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestTwoBytesOneMissingWithMerging()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField());
            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new StringField("id", "0", Field.Store.YES));
            doc.Add(new BinaryDocValuesField("dv1", new BytesRef()));
            iw.AddDocument(doc);
            iw.Commit();
            doc = new Document();
            doc.Add(new StringField("id", "1", Field.Store.YES));
            iw.AddDocument(doc);
            iw.ForceMerge(1);
            iw.Dispose();

            IndexReader ir = DirectoryReader.Open(directory);
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader ar = (AtomicReader)ir.Leaves[0].Reader;
            BinaryDocValues dv = ar.GetBinaryDocValues("dv1");
            BytesRef @ref = new BytesRef();
            dv.Get(0, @ref);
            Assert.AreEqual(new BytesRef(), @ref);
            dv.Get(1, @ref);
            Assert.AreEqual(new BytesRef(), @ref);
            Bits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
            ir.Dispose();
            directory.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestThreeBytesOneMissingWithMerging()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField());
            Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
            Document doc = new Document();
            doc.Add(new StringField("id", "0", Field.Store.YES));
            doc.Add(new BinaryDocValuesField("dv1", new BytesRef()));
            iw.AddDocument(doc);
            doc = new Document();
            doc.Add(new StringField("id", "1", Field.Store.YES));
            iw.AddDocument(doc);
            iw.Commit();
            doc = new Document();
            doc.Add(new StringField("id", "2", Field.Store.YES));
            doc.Add(new BinaryDocValuesField("dv1", new BytesRef("boo")));
            iw.AddDocument(doc);
            iw.ForceMerge(1);
            iw.Dispose();

            IndexReader ir = DirectoryReader.Open(directory);
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader ar = (AtomicReader)ir.Leaves[0].Reader;
            BinaryDocValues dv = ar.GetBinaryDocValues("dv1");
            BytesRef @ref = new BytesRef();
            dv.Get(0, @ref);
            Assert.AreEqual(new BytesRef(), @ref);
            dv.Get(1, @ref);
            Assert.AreEqual(new BytesRef(), @ref);
            dv.Get(2, @ref);
            Assert.AreEqual(new BytesRef("boo"), @ref);
            Bits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
            Assert.IsTrue(docsWithField.Get(2));
            ir.Dispose();
            directory.Dispose();
        }

        // LUCENE-4853
        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestHugeBinaryValues()
        {
            Analyzer analyzer = new MockAnalyzer(Random());
            // FSDirectory because SimpleText will consume gobbs of
            // space when storing big binary values:
            Directory d = NewFSDirectory(CreateTempDir("hugeBinaryValues"));
            bool doFixed = Random().NextBoolean();
            int numDocs;
            int fixedLength = 0;
            if (doFixed)
            {
                // Sometimes make all values fixed length since some
                // codecs have different code paths for this:
                numDocs = TestUtil.NextInt(Random(), 10, 20);
                fixedLength = TestUtil.NextInt(Random(), 65537, 256 * 1024);
            }
            else
            {
                numDocs = TestUtil.NextInt(Random(), 100, 200);
            }
            IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            var docBytes = new List<byte[]>();
            long totalBytes = 0;
            for (int docID = 0; docID < numDocs; docID++)
            {
                // we don't use RandomIndexWriter because it might add
                // more docvalues than we expect !!!!

                // Must be > 64KB in size to ensure more than 2 pages in
                // PagedBytes would be needed:
                int numBytes;
                if (doFixed)
                {
                    numBytes = fixedLength;
                }
                else if (docID == 0 || Random().Next(5) == 3)
                {
                    numBytes = TestUtil.NextInt(Random(), 65537, 3 * 1024 * 1024);
                }
                else
                {
                    numBytes = TestUtil.NextInt(Random(), 1, 1024 * 1024);
                }
                totalBytes += numBytes;
                if (totalBytes > 5 * 1024 * 1024)
                {
                    break;
                }
                var bytes = new byte[numBytes];
                Random().NextBytes(bytes);
                docBytes.Add(bytes);
                Document doc = new Document();
                BytesRef b = new BytesRef(bytes);
                b.Length = bytes.Length;
                doc.Add(new BinaryDocValuesField("field", b));
                doc.Add(new StringField("id", "" + docID, Field.Store.YES));
                try
                {
                    w.AddDocument(doc);
                }
                catch (System.ArgumentException iae)
                {
                    if (iae.Message.IndexOf("is too large") == -1)
                    {
                        throw iae;
                    }
                    else
                    {
                        // OK: some codecs can't handle binary DV > 32K
                        Assert.IsFalse(CodecAcceptsHugeBinaryValues("field"));
                        w.Rollback();
                        d.Dispose();
                        return;
                    }
                }
            }

            DirectoryReader r;
            try
            {
                r = w.Reader;
            }
            catch (System.ArgumentException iae)
            {
                if (iae.Message.IndexOf("is too large") == -1)
                {
                    throw iae;
                }
                else
                {
                    Assert.IsFalse(CodecAcceptsHugeBinaryValues("field"));

                    // OK: some codecs can't handle binary DV > 32K
                    w.Rollback();
                    d.Dispose();
                    return;
                }
            }
            w.Dispose();

            AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);

            BinaryDocValues s = FieldCache.DEFAULT.GetTerms(ar, "field", false);
            for (int docID = 0; docID < docBytes.Count; docID++)
            {
                Document doc = ar.Document(docID);
                BytesRef bytes = new BytesRef();
                s.Get(docID, bytes);
                var expected = docBytes[Convert.ToInt32(doc.Get("id"))];
                Assert.AreEqual(expected.Length, bytes.Length);
                Assert.AreEqual(new BytesRef(expected), bytes);
            }

            Assert.IsTrue(CodecAcceptsHugeBinaryValues("field"));

            ar.Dispose();
            d.Dispose();
        }

        // TODO: get this out of here and into the deprecated codecs (4.0, 4.2)
        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestHugeBinaryValueLimit()
        {
            // We only test DVFormats that have a limit
            AssumeFalse("test requires codec with limits on max binary field length", CodecAcceptsHugeBinaryValues("field"));
            Analyzer analyzer = new MockAnalyzer(Random());
            // FSDirectory because SimpleText will consume gobbs of
            // space when storing big binary values:
            Directory d = NewFSDirectory(CreateTempDir("hugeBinaryValues"));
            bool doFixed = Random().NextBoolean();
            int numDocs;
            int fixedLength = 0;
            if (doFixed)
            {
                // Sometimes make all values fixed length since some
                // codecs have different code paths for this:
                numDocs = TestUtil.NextInt(Random(), 10, 20);
                fixedLength = Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH;
            }
            else
            {
                numDocs = TestUtil.NextInt(Random(), 100, 200);
            }
            IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            var docBytes = new List<byte[]>();
            long totalBytes = 0;
            for (int docID = 0; docID < numDocs; docID++)
            {
                // we don't use RandomIndexWriter because it might add
                // more docvalues than we expect !!!!

                // Must be > 64KB in size to ensure more than 2 pages in
                // PagedBytes would be needed:
                int numBytes;
                if (doFixed)
                {
                    numBytes = fixedLength;
                }
                else if (docID == 0 || Random().Next(5) == 3)
                {
                    numBytes = Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH;
                }
                else
                {
                    numBytes = TestUtil.NextInt(Random(), 1, Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH);
                }
                totalBytes += numBytes;
                if (totalBytes > 5 * 1024 * 1024)
                {
                    break;
                }
                var bytes = new byte[numBytes];
                Random().NextBytes(bytes);
                docBytes.Add(bytes);
                Document doc = new Document();
                BytesRef b = new BytesRef(bytes);
                b.Length = bytes.Length;
                doc.Add(new BinaryDocValuesField("field", b));
                doc.Add(new StringField("id", "" + docID, Field.Store.YES));
                w.AddDocument(doc);
            }

            DirectoryReader r = w.Reader;
            w.Dispose();

            AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);

            BinaryDocValues s = FieldCache.DEFAULT.GetTerms(ar, "field", false);
            for (int docID = 0; docID < docBytes.Count; docID++)
            {
                Document doc = ar.Document(docID);
                BytesRef bytes = new BytesRef();
                s.Get(docID, bytes);
                var expected = docBytes[Convert.ToInt32(doc.Get("id"))];
                Assert.AreEqual(expected.Length, bytes.Length);
                Assert.AreEqual(new BytesRef(expected), bytes);
            }

            ar.Dispose();
            d.Dispose();
        }

        /// <summary>
        /// Tests dv against stored fields with threads (binary/numeric/sorted, no missing)
        /// </summary>
        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestThreads()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
            Document doc = new Document();
            Field idField = new StringField("id", "", Field.Store.NO);
            Field storedBinField = new StoredField("storedBin", new byte[0]);
            Field dvBinField = new BinaryDocValuesField("dvBin", new BytesRef());
            Field dvSortedField = new SortedDocValuesField("dvSorted", new BytesRef());
            Field storedNumericField = new StoredField("storedNum", "");
            Field dvNumericField = new NumericDocValuesField("dvNum", 0);
            doc.Add(idField);
            doc.Add(storedBinField);
            doc.Add(dvBinField);
            doc.Add(dvSortedField);
            doc.Add(storedNumericField);
            doc.Add(dvNumericField);

            // index some docs
            int numDocs = AtLeast(300);
            for (int i = 0; i < numDocs; i++)
            {
                idField.StringValue = Convert.ToString(i);
                int length = TestUtil.NextInt(Random(), 0, 8);
                var buffer = new byte[length];
                Random().NextBytes(buffer);
                storedBinField.BytesValue = new BytesRef(buffer);
                dvBinField.BytesValue = new BytesRef(buffer);
                dvSortedField.BytesValue = new BytesRef(buffer);
                long numericValue = Random().NextLong();
                storedNumericField.StringValue = Convert.ToString(numericValue);
                dvNumericField.LongValue = numericValue;
                writer.AddDocument(doc);
                if (Random().Next(31) == 0)
                {
                    writer.Commit();
                }
            }

            // delete some docs
            int numDeletions = Random().Next(numDocs / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                int id = Random().Next(numDocs);
                writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
            }
            writer.Dispose();

            // compare
            DirectoryReader ir = DirectoryReader.Open(dir);
            int numThreads = TestUtil.NextInt(Random(), 2, 7);
            ThreadClass[] threads = new ThreadClass[numThreads];
            CountdownEvent startingGun = new CountdownEvent(1);

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ThreadAnonymousInnerClassHelper(this, ir, startingGun);
                threads[i].Start();
            }
            startingGun.Signal();
            foreach (ThreadClass t in threads)
            {
                t.Join();
            }
            ir.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly BaseDocValuesFormatTestCase OuterInstance;

            private DirectoryReader Ir;
            private CountdownEvent StartingGun;

            public ThreadAnonymousInnerClassHelper(BaseDocValuesFormatTestCase outerInstance, DirectoryReader ir, CountdownEvent startingGun)
            {
                this.OuterInstance = outerInstance;
                this.Ir = ir;
                this.StartingGun = startingGun;
            }

            public override void Run()
            {
                try
                {
                    StartingGun.Wait();
                    foreach (AtomicReaderContext context in Ir.Leaves)
                    {
                        AtomicReader r = context.AtomicReader;
                        BinaryDocValues binaries = r.GetBinaryDocValues("dvBin");
                        SortedDocValues sorted = r.GetSortedDocValues("dvSorted");
                        NumericDocValues numerics = r.GetNumericDocValues("dvNum");
                        for (int j = 0; j < r.MaxDoc; j++)
                        {
                            BytesRef binaryValue = r.Document(j).GetBinaryValue("storedBin");
                            BytesRef scratch = new BytesRef();
                            binaries.Get(j, scratch);
                            Assert.AreEqual(binaryValue, scratch);
                            sorted.Get(j, scratch);
                            Assert.AreEqual(binaryValue, scratch);
                            string expected = r.Document(j).Get("storedNum");
                            Assert.AreEqual(Convert.ToInt64(expected), numerics.Get(j));
                        }
                    }
                    TestUtil.CheckReader(Ir);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message, e);
                }
            }
        }

        /// <summary>
        /// Tests dv against stored fields with threads (all types + missing)
        /// </summary>
        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestThreads2()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField());
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
            Field idField = new StringField("id", "", Field.Store.NO);
            Field storedBinField = new StoredField("storedBin", new byte[0]);
            Field dvBinField = new BinaryDocValuesField("dvBin", new BytesRef());
            Field dvSortedField = new SortedDocValuesField("dvSorted", new BytesRef());
            Field storedNumericField = new StoredField("storedNum", "");
            Field dvNumericField = new NumericDocValuesField("dvNum", 0);

            // index some docs
            int numDocs = AtLeast(300);
            for (int i = 0; i < numDocs; i++)
            {
                idField.StringValue = Convert.ToString(i);
                int length = TestUtil.NextInt(Random(), 0, 8);
                var buffer = new byte[length];
                Random().NextBytes(buffer);
                storedBinField.BytesValue = new BytesRef(buffer);
                dvBinField.BytesValue = new BytesRef(buffer);
                dvSortedField.BytesValue = new BytesRef(buffer);
                long numericValue = Random().NextLong();
                storedNumericField.StringValue = Convert.ToString(numericValue);
                dvNumericField.LongValue = numericValue;
                Document doc = new Document();
                doc.Add(idField);
                if (Random().Next(4) > 0)
                {
                    doc.Add(storedBinField);
                    doc.Add(dvBinField);
                    doc.Add(dvSortedField);
                }
                if (Random().Next(4) > 0)
                {
                    doc.Add(storedNumericField);
                    doc.Add(dvNumericField);
                }
                int numSortedSetFields = Random().Next(3);
                SortedSet<string> values = new SortedSet<string>();
                for (int j = 0; j < numSortedSetFields; j++)
                {
                    values.Add(TestUtil.RandomSimpleString(Random()));
                }
                foreach (string v in values)
                {
                    doc.Add(new SortedSetDocValuesField("dvSortedSet", new BytesRef(v)));
                    doc.Add(new StoredField("storedSortedSet", v));
                }
                writer.AddDocument(doc);
                if (Random().Next(31) == 0)
                {
                    writer.Commit();
                }
            }

            // delete some docs
            int numDeletions = Random().Next(numDocs / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                int id = Random().Next(numDocs);
                writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
            }
            writer.Dispose();

            // compare
            DirectoryReader ir = DirectoryReader.Open(dir);
            int numThreads = TestUtil.NextInt(Random(), 2, 7);
            ThreadClass[] threads = new ThreadClass[numThreads];
            CountdownEvent startingGun = new CountdownEvent(1);

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ThreadAnonymousInnerClassHelper2(this, ir, startingGun);
                threads[i].Start();
            }
            startingGun.Signal();
            foreach (ThreadClass t in threads)
            {
                t.Join();
            }
            ir.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper2 : ThreadClass
        {
            private readonly BaseDocValuesFormatTestCase OuterInstance;

            private DirectoryReader Ir;
            private CountdownEvent StartingGun;

            public ThreadAnonymousInnerClassHelper2(BaseDocValuesFormatTestCase outerInstance, DirectoryReader ir, CountdownEvent startingGun)
            {
                this.OuterInstance = outerInstance;
                this.Ir = ir;
                this.StartingGun = startingGun;
            }

            public override void Run()
            {
                try
                {
                    StartingGun.Wait();
                    foreach (AtomicReaderContext context in Ir.Leaves)
                    {
                        AtomicReader r = context.AtomicReader;
                        BinaryDocValues binaries = r.GetBinaryDocValues("dvBin");
                        Bits binaryBits = r.GetDocsWithField("dvBin");
                        SortedDocValues sorted = r.GetSortedDocValues("dvSorted");
                        Bits sortedBits = r.GetDocsWithField("dvSorted");
                        NumericDocValues numerics = r.GetNumericDocValues("dvNum");
                        Bits numericBits = r.GetDocsWithField("dvNum");
                        SortedSetDocValues sortedSet = r.GetSortedSetDocValues("dvSortedSet");
                        Bits sortedSetBits = r.GetDocsWithField("dvSortedSet");
                        for (int j = 0; j < r.MaxDoc; j++)
                        {
                            BytesRef binaryValue = r.Document(j).GetBinaryValue("storedBin");
                            if (binaryValue != null)
                            {
                                if (binaries != null)
                                {
                                    BytesRef scratch = new BytesRef();
                                    binaries.Get(j, scratch);
                                    Assert.AreEqual(binaryValue, scratch);
                                    sorted.Get(j, scratch);
                                    Assert.AreEqual(binaryValue, scratch);
                                    Assert.IsTrue(binaryBits.Get(j));
                                    Assert.IsTrue(sortedBits.Get(j));
                                }
                            }
                            else if (binaries != null)
                            {
                                Assert.IsFalse(binaryBits.Get(j));
                                Assert.IsFalse(sortedBits.Get(j));
                                Assert.AreEqual(-1, sorted.GetOrd(j));
                            }

                            string number = r.Document(j).Get("storedNum");
                            if (number != null)
                            {
                                if (numerics != null)
                                {
                                    Assert.AreEqual(Convert.ToInt64(number), numerics.Get(j));
                                }
                            }
                            else if (numerics != null)
                            {
                                Assert.IsFalse(numericBits.Get(j));
                                Assert.AreEqual(0, numerics.Get(j));
                            }

                            string[] values = r.Document(j).GetValues("storedSortedSet");
                            if (values.Length > 0)
                            {
                                Assert.IsNotNull(sortedSet);
                                sortedSet.Document = j;
                                for (int k = 0; k < values.Length; k++)
                                {
                                    long ord = sortedSet.NextOrd();
                                    Assert.IsTrue(ord != SortedSetDocValues.NO_MORE_ORDS);
                                    BytesRef value = new BytesRef();
                                    sortedSet.LookupOrd(ord, value);
                                    Assert.AreEqual(values[k], value.Utf8ToString());
                                }
                                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd());
                                Assert.IsTrue(sortedSetBits.Get(j));
                            }
                            else if (sortedSet != null)
                            {
                                sortedSet.Document = j;
                                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd());
                                Assert.IsFalse(sortedSetBits.Get(j));
                            }
                        }
                    }
                    TestUtil.CheckReader(Ir);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message, e);
                }
            }
        }

        // LUCENE-5218
        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestEmptyBinaryValueOnPageSizes()
        {
            // Test larger and larger power-of-two sized values,
            // followed by empty string value:
            for (int i = 0; i < 20; i++)
            {
                if (i > 14 && CodecAcceptsHugeBinaryValues("field") == false)
                {
                    break;
                }
                Directory dir = NewDirectory();
                RandomIndexWriter w = new RandomIndexWriter(Random(), dir, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
                BytesRef bytes = new BytesRef();
                bytes.Bytes = new byte[1 << i];
                bytes.Length = 1 << i;
                for (int j = 0; j < 4; j++)
                {
                    Document doc_ = new Document();
                    doc_.Add(new BinaryDocValuesField("field", bytes));
                    w.AddDocument(doc_);
                }
                Document doc = new Document();
                doc.Add(new StoredField("id", "5"));
                doc.Add(new BinaryDocValuesField("field", new BytesRef()));
                w.AddDocument(doc);
                IndexReader r = w.Reader;
                w.Dispose();

                AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);
                BinaryDocValues values = ar.GetBinaryDocValues("field");
                BytesRef result = new BytesRef();
                for (int j = 0; j < 5; j++)
                {
                    values.Get(0, result);
                    Assert.IsTrue(result.Length == 0 || result.Length == 1 << i);
                }
                ar.Dispose();
                dir.Dispose();
            }
        }

        protected internal virtual bool CodecAcceptsHugeBinaryValues(string field)
        {
            return true;
        }
    }
}