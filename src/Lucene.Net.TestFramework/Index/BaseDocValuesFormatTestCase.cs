using J2N.Collections.Generic.Extensions;
using J2N.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using static Lucene.Net.Index.TermsEnum;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;
using Test = NUnit.Framework.TestAttribute;

namespace Lucene.Net.Index
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

    /// <summary>
    /// Abstract class to do basic tests for a <see cref="Codecs.DocValuesFormat"/>.
    /// NOTE: this test focuses on the docvalues impl, nothing else.
    /// The [stretch] goal is for this test to be
    /// so thorough in testing a new <see cref="Codecs.DocValuesFormat"/> that if this
    /// test passes, then all Lucene/Solr tests should also pass.  Ie,
    /// if there is some bug in a given <see cref="Codecs.DocValuesFormat"/> that this
    /// test fails to catch then this test needs to be improved!
    /// </summary>
    public abstract class BaseDocValuesFormatTestCase : BaseIndexFileFormatTestCase
    {
        protected override void AddRandomFields(Document doc)
        {
            if (Usually())
            {
                doc.Add(new NumericDocValuesField("ndv", Random.Next(1 << 12)));
                doc.Add(new BinaryDocValuesField("bdv", new BytesRef(TestUtil.RandomSimpleString(Random))));
                doc.Add(new SortedDocValuesField("sdv", new BytesRef(TestUtil.RandomSimpleString(Random, 2))));
            }
            if (DefaultCodecSupportsSortedSet)
            {
                int numValues = Random.Next(5);
                for (int i = 0; i < numValues; ++i)
                {
                    doc.Add(new SortedSetDocValuesField("ssdv", new BytesRef(TestUtil.RandomSimpleString(Random, 2))));
                }
            }
        }

        [Test]
        public virtual void TestOneNumber()
        {
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            using Directory directory = NewDirectory();
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory))
            {
                Document doc = new Document();
                doc.Add(NewTextField("fieldname", text, Field.Store.YES));
                doc.Add(new NumericDocValuesField("dv", 5));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
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
                if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
                NumericDocValues dv = ((AtomicReader)((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader))).GetNumericDocValues("dv");
                Assert.AreEqual(5L, dv.Get(hits.ScoreDocs[i].Doc)); // LUCENENET specific - 5L required because types don't match (xUnit checks this)
            }
        }

        [Test]
        public virtual void TestOneSingle() // LUCENENET specific - renamed from TestOneFloat
        {
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;

            using Directory directory = NewDirectory();
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory))
            {
                Document doc = new Document();
                doc.Add(NewTextField("fieldname", text, Field.Store.YES));
                doc.Add(new SingleDocValuesField("dv", 5.7f));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
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
                if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
                NumericDocValues dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetNumericDocValues("dv");
                Assert.AreEqual((long)J2N.BitConversion.SingleToRawInt32Bits(5.7f), dv.Get(hits.ScoreDocs[i].Doc)); // LUCENENET specific - cast required because types don't match (xUnit checks this)
            }
        }

        [Test]
        public virtual void TestTwoNumbers()
        {
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            using Directory directory = NewDirectory();
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory))
            {
                Document doc = new Document();
                doc.Add(NewTextField("fieldname", text, Field.Store.YES));
                doc.Add(new NumericDocValuesField("dv1", 5));
                doc.Add(new NumericDocValuesField("dv2", 17));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
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
                if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
                NumericDocValues dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetNumericDocValues("dv1");
                Assert.AreEqual(5L, dv.Get(hits.ScoreDocs[i].Doc)); // LUCENENET specific - 5L required because types don't match (xUnit checks this)
                dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetNumericDocValues("dv2");
                Assert.AreEqual(17L, dv.Get(hits.ScoreDocs[i].Doc)); // LUCENENET specific - 17L required because types don't match (xUnit checks this)
            }
        }

        [Test]
        public virtual void TestTwoBinaryValues()
        {
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            using Directory directory = NewDirectory();
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory))
            {
                Document doc = new Document();

                doc.Add(NewTextField("fieldname", text, Field.Store.YES));
                doc.Add(new BinaryDocValuesField("dv1", new BytesRef(longTerm)));
                doc.Add(new BinaryDocValuesField("dv2", new BytesRef(text)));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = new IndexSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            BytesRef scratch = new BytesRef(); // LUCENENET: Moved this outside of the loop for performance
                                               // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
                BinaryDocValues dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetBinaryDocValues("dv1");
                dv.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef(longTerm), scratch);
                dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetBinaryDocValues("dv2");
                dv.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef(text), scratch);
            }
        }

        [Test]
        public virtual void TestTwoFieldsMixed()
        {
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            using Directory directory = NewDirectory();
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory))
            {
                Document doc = new Document();

                doc.Add(NewTextField("fieldname", text, Field.Store.YES));
                doc.Add(new NumericDocValuesField("dv1", 5));
                doc.Add(new BinaryDocValuesField("dv2", new BytesRef("hello world")));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
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
                if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
                NumericDocValues dv = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetNumericDocValues("dv1");
                Assert.AreEqual(5L, dv.Get(hits.ScoreDocs[i].Doc)); // LUCENENET specific - 5L required because types don't match (xUnit checks this)
                BinaryDocValues dv2 = ((AtomicReader)((AtomicReader)ireader.Leaves[0].Reader)).GetBinaryDocValues("dv2");
                dv2.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }
        }

        [Test]
        public virtual void TestThreeFieldsMixed()
        {
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            using Directory directory = NewDirectory();
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory))
            {
                Document doc = new Document();

                doc.Add(NewTextField("fieldname", text, Field.Store.YES));
                doc.Add(new SortedDocValuesField("dv1", new BytesRef("hello hello")));
                doc.Add(new NumericDocValuesField("dv2", 5));
                doc.Add(new BinaryDocValuesField("dv3", new BytesRef("hello world")));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
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
                if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
                SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv1");
                int ord = dv.GetOrd(0);
                dv.LookupOrd(ord, scratch);
                Assert.AreEqual(new BytesRef("hello hello"), scratch);
                NumericDocValues dv2 = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv2");
                Assert.AreEqual(5L, dv2.Get(hits.ScoreDocs[i].Doc)); // LUCENENET specific - 5L required because types don't match (xUnit checks this)
                BinaryDocValues dv3 = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv3");
                dv3.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }
        }

        [Test]
        public virtual void TestThreeFieldsMixed2()
        {
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            using Directory directory = NewDirectory();
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory))
            {
                Document doc = new Document();

                doc.Add(NewTextField("fieldname", text, Field.Store.YES));
                doc.Add(new BinaryDocValuesField("dv1", new BytesRef("hello world")));
                doc.Add(new SortedDocValuesField("dv2", new BytesRef("hello hello")));
                doc.Add(new NumericDocValuesField("dv3", 5));
                iwriter.AddDocument(doc);
            }// iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
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
                if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
                SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv2");
                int ord = dv.GetOrd(0);
                dv.LookupOrd(ord, scratch);
                Assert.AreEqual(new BytesRef("hello hello"), scratch);
                NumericDocValues dv2 = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv3");
                Assert.AreEqual(5L, dv2.Get(hits.ScoreDocs[i].Doc)); // LUCENENET specific - 5L required because types don't match (xUnit checks this)
                BinaryDocValues dv3 = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv1");
                dv3.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }
        }

        [Test]
        public virtual void TestTwoDocumentsNumeric()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new NumericDocValuesField("dv", 1));
                iwriter.AddDocument(doc);
                doc = new Document();
                doc.Add(new NumericDocValuesField("dv", 2));
                iwriter.AddDocument(doc);
                iwriter.ForceMerge(1);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv");
            Assert.AreEqual(1L, dv.Get(0)); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
            Assert.AreEqual(2L, dv.Get(1)); // LUCENENET specific - 2L required because types don't match (xUnit checks this)
        }

        [Test]
        public virtual void TestTwoDocumentsMerged()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
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
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv");
            for (int i = 0; i < 2; i++)
            {
                Document doc2 = ((AtomicReader)ireader.Leaves[0].Reader).Document(i);
                long expected;
                if (doc2.Get("id").Equals("0", StringComparison.Ordinal))
                {
                    expected = -10;
                }
                else
                {
                    expected = 99;
                }
                Assert.AreEqual(expected, dv.Get(i));
            }
        }

        [Test]
        public virtual void TestBigNumericRange()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new NumericDocValuesField("dv", long.MinValue));
                iwriter.AddDocument(doc);
                doc = new Document();
                doc.Add(new NumericDocValuesField("dv", long.MaxValue));
                iwriter.AddDocument(doc);
                iwriter.ForceMerge(1);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv");
            Assert.AreEqual(long.MinValue, dv.Get(0));
            Assert.AreEqual(long.MaxValue, dv.Get(1));
        }

        [Test]
        public virtual void TestBigNumericRange2()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new NumericDocValuesField("dv", -8841491950446638677L));
                iwriter.AddDocument(doc);
                doc = new Document();
                doc.Add(new NumericDocValuesField("dv", 9062230939892376225L));
                iwriter.AddDocument(doc);
                iwriter.ForceMerge(1);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory);
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv");
            Assert.AreEqual(-8841491950446638677L, dv.Get(0));
            Assert.AreEqual(9062230939892376225L, dv.Get(1));
        }

        [Test]
        public virtual void TestBytes()
        {
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(NewTextField("fieldname", text, Field.Store.YES));
                doc.Add(new BinaryDocValuesField("dv", new BytesRef("hello world")));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
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
                if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
                BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
                dv.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }
        }

        [Test]
        public virtual void TestBytesTwoDocumentsMerged()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
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
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < 2; i++)
            {
                Document doc2 = ((AtomicReader)ireader.Leaves[0].Reader).Document(i);
                string expected;
                if (doc2.Get("id").Equals("0", StringComparison.Ordinal))
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
        }

        [Test]
        public virtual void TestSortedBytes()
        {
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();

                doc.Add(NewTextField("fieldname", text, Field.Store.YES));
                doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world")));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
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
                if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
                SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
                dv.LookupOrd(dv.GetOrd(hits.ScoreDocs[i].Doc), scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }
        }

        [Test]
        public virtual void TestSortedBytesTwoDocuments()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
                iwriter.AddDocument(doc);
                doc = new Document();
                doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
                iwriter.AddDocument(doc);
                iwriter.ForceMerge(1);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.LookupOrd(dv.GetOrd(0), scratch);
            Assert.AreEqual("hello world 1", scratch.Utf8ToString());
            dv.LookupOrd(dv.GetOrd(1), scratch);
            Assert.AreEqual("hello world 2", scratch.Utf8ToString());
        }

        [Test]
        public virtual void TestSortedBytesThreeDocuments()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
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
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
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
        }

        [Test]
        public virtual void TestSortedBytesTwoDocumentsMerged()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
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
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
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
                if (doc2.Get("id").Equals("0", StringComparison.Ordinal))
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
        }

        [Test]
        public virtual void TestSortedMergeAwayAllValues()
        {
            using Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, iwconfig))
                {

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

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedDocValues dv = GetOnlySegmentReader(ireader).GetSortedDocValues("field");
                if (DefaultCodecSupportsDocsWithField)
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
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestBytesWithNewline()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new BinaryDocValuesField("dv", new BytesRef("hello\nworld\r1")));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual(new BytesRef("hello\nworld\r1"), scratch);
        }

        [Test]
        public virtual void TestMissingSortedBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
                iwriter.AddDocument(doc);
                // 2nd doc missing the DV field
                iwriter.AddDocument(new Document());
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory);
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.LookupOrd(dv.GetOrd(0), scratch);
            Assert.AreEqual(new BytesRef("hello world 2"), scratch);
            if (DefaultCodecSupportsDocsWithField)
            {
                Assert.AreEqual(-1, dv.GetOrd(1));
            }
            dv.Get(1, scratch);
            Assert.AreEqual(new BytesRef(""), scratch);
        }

        [Test]
        public virtual void TestSortedTermsEnum()
        {
            using Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, iwconfig))
                {

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

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedDocValues dv = GetOnlySegmentReader(ireader).GetSortedDocValues("field");
                Assert.AreEqual(3, dv.ValueCount);

                TermsEnum termsEnum = dv.GetTermsEnum();

                // next()
                Assert.IsTrue(termsEnum.MoveNext());
                Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(0L, termsEnum.Ord); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.IsTrue(termsEnum.MoveNext());
                Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(1L, termsEnum.Ord); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                Assert.IsTrue(termsEnum.MoveNext());
                Assert.AreEqual("world", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(2L, termsEnum.Ord); // LUCENENET specific - 2L required because types don't match (xUnit checks this)

                // seekCeil()
                Assert.AreEqual(SeekStatus.NOT_FOUND, termsEnum.SeekCeil(new BytesRef("ha!")));
                Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(1L, termsEnum.Ord); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                Assert.AreEqual(SeekStatus.FOUND, termsEnum.SeekCeil(new BytesRef("beer")));
                Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(0L, termsEnum.Ord); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(SeekStatus.END, termsEnum.SeekCeil(new BytesRef("zzz")));

                // seekExact()
                Assert.IsTrue(termsEnum.SeekExact(new BytesRef("beer")));
                Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(0L, termsEnum.Ord); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.IsTrue(termsEnum.SeekExact(new BytesRef("hello")));
                Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(1L, termsEnum.Ord); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                Assert.IsTrue(termsEnum.SeekExact(new BytesRef("world")));
                Assert.AreEqual("world", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(2L, termsEnum.Ord); // LUCENENET specific - 2L required because types don't match (xUnit checks this)
                Assert.IsFalse(termsEnum.SeekExact(new BytesRef("bogus")));

                // seek(ord)
                termsEnum.SeekExact(0);
                Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(0L, termsEnum.Ord); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                termsEnum.SeekExact(1);
                Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(1L, termsEnum.Ord); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                termsEnum.SeekExact(2);
                Assert.AreEqual("world", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(2L, termsEnum.Ord); // LUCENENET specific - 2L required because types don't match (xUnit checks this)
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestEmptySortedBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new SortedDocValuesField("dv", new BytesRef("")));
                iwriter.AddDocument(doc);
                doc = new Document();
                doc.Add(new SortedDocValuesField("dv", new BytesRef("")));
                iwriter.AddDocument(doc);
                iwriter.ForceMerge(1);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory);
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            BytesRef scratch = new BytesRef();
            Assert.AreEqual(0, dv.GetOrd(0));
            Assert.AreEqual(0, dv.GetOrd(1));
            dv.LookupOrd(dv.GetOrd(0), scratch);
            Assert.AreEqual("", scratch.Utf8ToString());
        }

        [Test]
        public virtual void TestEmptyBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new BinaryDocValuesField("dv", new BytesRef("")));
                iwriter.AddDocument(doc);
                doc = new Document();
                doc.Add(new BinaryDocValuesField("dv", new BytesRef("")));
                iwriter.AddDocument(doc);
                iwriter.ForceMerge(1);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory);
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual("", scratch.Utf8ToString());
            dv.Get(1, scratch);
            Assert.AreEqual("", scratch.Utf8ToString());
        }

        [Test]
        public virtual void TestVeryLargeButLegalBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            var bytes = new byte[32766];
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                BytesRef b = new BytesRef(bytes);
                Random.NextBytes(bytes);
                doc.Add(new BinaryDocValuesField("dv", b));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual(new BytesRef(bytes), scratch);
        }

        [Test]
        public virtual void TestVeryLargeButLegalSortedBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            var bytes = new byte[32766];
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                BytesRef b = new BytesRef(bytes);
                Random.NextBytes(bytes);
                doc.Add(new SortedDocValuesField("dv", b));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual(new BytesRef(bytes), scratch);
        }

        [Test]
        public virtual void TestCodecUsesOwnBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new BinaryDocValuesField("dv", new BytesRef("boo!")));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            var mybytes = new byte[20];
            BytesRef scratch = new BytesRef(mybytes);
            dv.Get(0, scratch);
            Assert.AreEqual("boo!", scratch.Utf8ToString());
            Assert.IsFalse(scratch.Bytes == mybytes);
        }

        [Test]
        public virtual void TestCodecUsesOwnSortedBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new SortedDocValuesField("dv", new BytesRef("boo!")));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            var mybytes = new byte[20];
            BytesRef scratch = new BytesRef(mybytes);
            dv.Get(0, scratch);
            Assert.AreEqual("boo!", scratch.Utf8ToString());
            Assert.IsFalse(scratch.Bytes == mybytes);
        }

        [Test]
        public virtual void TestCodecUsesOwnBytesEachTime()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new BinaryDocValuesField("dv", new BytesRef("foo!")));
                iwriter.AddDocument(doc);
                doc = new Document();
                doc.Add(new BinaryDocValuesField("dv", new BytesRef("bar!")));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual("foo!", scratch.Utf8ToString());

            BytesRef scratch2 = new BytesRef();
            dv.Get(1, scratch2);
            Assert.AreEqual("bar!", scratch2.Utf8ToString());
            // check scratch is still valid
            Assert.AreEqual("foo!", scratch.Utf8ToString());
        }

        [Test]
        public virtual void TestCodecUsesOwnSortedBytesEachTime()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new SortedDocValuesField("dv", new BytesRef("foo!")));
                iwriter.AddDocument(doc);
                doc = new Document();
                doc.Add(new SortedDocValuesField("dv", new BytesRef("bar!")));
                iwriter.AddDocument(doc);
            } // iwriter.Dispose();

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            if (Debugging.AssertsEnabled) Debugging.Assert(ireader.Leaves.Count == 1);
            BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues("dv");
            BytesRef scratch = new BytesRef();
            dv.Get(0, scratch);
            Assert.AreEqual("foo!", scratch.Utf8ToString());

            BytesRef scratch2 = new BytesRef();
            dv.Get(1, scratch2);
            Assert.AreEqual("bar!", scratch2.Utf8ToString());
            // check scratch is still valid
            Assert.AreEqual("foo!", scratch.Utf8ToString());
        }

        /// <summary>
        /// Simple test case to show how to use the API
        /// </summary>
        [Test]
        public virtual void TestDocValuesSimple()
        {
            using Directory dir = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (IndexWriter writer = new IndexWriter(dir, conf))
            {
                for (int i = 0; i < 5; i++)
                {
                    Document doc = new Document();
                    doc.Add(new NumericDocValuesField("docId", i));
                    doc.Add(new TextField("docId", "" + i, Field.Store.NO));
                    writer.AddDocument(doc);
                }
                writer.Commit();
                writer.ForceMerge(1, true);

            } // writer.Dispose();

            using DirectoryReader reader = DirectoryReader.Open(dir, 1);
            Assert.AreEqual(1, reader.Leaves.Count);

            IndexSearcher searcher = new IndexSearcher(reader);

            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("docId", "0")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("docId", "1")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("docId", "2")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("docId", "3")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("docId", "4")), Occur.SHOULD);

            TopDocs search = searcher.Search(query, 10);
            Assert.AreEqual(5, search.TotalHits);
            ScoreDoc[] scoreDocs = search.ScoreDocs;
            NumericDocValues docValues = GetOnlySegmentReader(reader).GetNumericDocValues("docId");
            for (int i = 0; i < scoreDocs.Length; i++)
            {
                Assert.AreEqual(i, scoreDocs[i].Doc);
                Assert.AreEqual((long)i, docValues.Get(scoreDocs[i].Doc)); // LUCENENET specific - cast required because types don't match (xUnit checks this)
            }
        }

        [Test]
        public virtual void TestRandomSortedBytes()
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig cfg = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            if (!DefaultCodecSupportsDocsWithField)
            {
                // if the codec doesnt support missing, we expect missing to be mapped to byte[]
                // by the impersonator, but we have to give it a chance to merge them to this
                cfg.SetMergePolicy(NewLogMergePolicy());
            }
            using RandomIndexWriter w = new RandomIndexWriter(Random, dir, cfg);
            int numDocs = AtLeast(100);
            BytesRefHash hash = new BytesRefHash();
            IDictionary<string, string> docToString = new Dictionary<string, string>();
            int maxLength = TestUtil.NextInt32(Random, 1, 50);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("id", "" + i, Field.Store.YES));
                string @string = TestUtil.RandomRealisticUnicodeString(Random, 1, maxLength);
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
            if (!DefaultCodecSupportsDocsWithField)
            {
                BytesRef bytesRef = new BytesRef();
                hash.Add(bytesRef); // add empty value for the gaps
            }
            if (Rarely())
            {
                w.Commit();
            }
            if (!DefaultCodecSupportsDocsWithField)
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
                string @string = TestUtil.RandomRealisticUnicodeString(Random, 1, maxLength);
                BytesRef br = new BytesRef(@string);
                hash.Add(br);
                docToString[id] = @string;
                doc.Add(new SortedDocValuesField("field", br));
                w.AddDocument(doc);
            }
            w.Commit();
            using IndexReader reader = w.GetReader();
            SortedDocValues docValues = MultiDocValues.GetSortedValues(reader, "field");
            int[] sort = hash.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
            BytesRef expected = new BytesRef();
            BytesRef actual = new BytesRef();
            Assert.AreEqual(hash.Count, docValues.ValueCount);
            for (int i = 0; i < hash.Count; i++)
            {
                hash.Get(sort[i], expected);
                docValues.LookupOrd(i, actual);
                Assert.AreEqual(expected.Utf8ToString(), actual.Utf8ToString());
                int ord = docValues.LookupTerm(expected);
                Assert.AreEqual(i, ord);
            }
            AtomicReader slowR = SlowCompositeReaderWrapper.Wrap(reader);

            foreach (KeyValuePair<string, string> entry in docToString)
            {
                // pk lookup
                DocsEnum termDocsEnum = slowR.GetTermDocsEnum(new Term("id", entry.Key));
                int docId = termDocsEnum.NextDoc();
                expected = new BytesRef(entry.Value);
                docValues.Get(docId, actual);
                Assert.AreEqual(expected, actual);
            }
        }

        internal abstract class Int64Producer
        {
            internal abstract long Next();
        }

        private static void DoTestNumericsVsStoredFields(long minValue, long maxValue) // LUCENENET: CA1822: Mark members as static
        {
            DoTestNumericsVsStoredFields(new Int64ProducerAnonymousClass(minValue, maxValue));
        }

        private sealed class Int64ProducerAnonymousClass : Int64Producer
        {
            private readonly long minValue;
            private readonly long maxValue;

            public Int64ProducerAnonymousClass(long minValue, long maxValue)
            {
                this.minValue = minValue;
                this.maxValue = maxValue;
            }

            internal override long Next()
            {
                return TestUtil.NextInt64(Random, minValue, maxValue);
            }
        }

        private static void DoTestNumericsVsStoredFields(Int64Producer longs) // LUCENENET: CA1822: Mark members as static
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using (RandomIndexWriter writer = new RandomIndexWriter(Random, dir, conf))
            {
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
                if (Debugging.AssertsEnabled) Debugging.Assert(numDocs > 256);
                for (int i = 0; i < numDocs; i++)
                {
                    idField.SetStringValue(Convert.ToString(i, CultureInfo.InvariantCulture));
                    long value = longs.Next();
                    storedField.SetStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                    dvField.SetInt64Value(value);
                    writer.AddDocument(doc);
                    if (Random.Next(31) == 0)
                    {
                        writer.Commit();
                    }
                }

                // delete some docs
                int numDeletions = Random.Next(numDocs / 10);
                for (int i = 0; i < numDeletions; i++)
                {
                    int id = Random.Next(numDocs);
                    writer.DeleteDocuments(new Term("id", Convert.ToString(id, CultureInfo.InvariantCulture)));
                }

                // merge some segments and ensure that at least one of them has more than
                // 256 values
                writer.ForceMerge(numDocs / 256);

            } // writer.Dispose();

            // compare
            using DirectoryReader ir = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                NumericDocValues docValues = r.GetNumericDocValues("dv");
                for (int i = 0; i < r.MaxDoc; i++)
                {
                    long storedValue = Convert.ToInt64(r.Document(i).Get("stored"), CultureInfo.InvariantCulture);
                    Assert.AreEqual(storedValue, docValues.Get(i));
                }
            }
        }

        private static void DoTestMissingVsFieldCache(long minValue, long maxValue) // LUCENENET: CA1822: Mark members as static
        {
            DoTestMissingVsFieldCache(new Int64ProducerAnonymousClass2(minValue, maxValue));
        }

        private sealed class Int64ProducerAnonymousClass2 : Int64Producer
        {
            private readonly long minValue;
            private readonly long maxValue;

            public Int64ProducerAnonymousClass2(long minValue, long maxValue)
            {
                this.minValue = minValue;
                this.maxValue = maxValue;
            }

            internal override long Next()
            {
                return TestUtil.NextInt64(Random, minValue, maxValue);
            }
        }

        private static void DoTestMissingVsFieldCache(Int64Producer longs) // LUCENENET: CA1822: Mark members as static
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField);
            using Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using (RandomIndexWriter writer = new RandomIndexWriter(Random, dir, conf))
            {
                Field idField = new StringField("id", "", Field.Store.NO);
                Field indexedField = NewStringField("indexed", "", Field.Store.NO);
                Field dvField = new NumericDocValuesField("dv", 0);

                // index some docs
                int numDocs = AtLeast(300);
                // numDocs should be always > 256 so that in case of a codec that optimizes
                // for numbers of values <= 256, all storage layouts are tested
                if (Debugging.AssertsEnabled) Debugging.Assert(numDocs > 256);
                for (int i = 0; i < numDocs; i++)
                {
                    idField.SetStringValue(Convert.ToString(i, CultureInfo.InvariantCulture));
                    long value = longs.Next();
                    indexedField.SetStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                    dvField.SetInt64Value(value);
                    Document doc = new Document();
                    doc.Add(idField);
                    // 1/4 of the time we neglect to add the fields
                    if (Random.Next(4) > 0)
                    {
                        doc.Add(indexedField);
                        doc.Add(dvField);
                    }
                    writer.AddDocument(doc);
                    if (Random.Next(31) == 0)
                    {
                        writer.Commit();
                    }
                }

                // delete some docs
                int numDeletions = Random.Next(numDocs / 10);
                for (int i = 0; i < numDeletions; i++)
                {
                    int id = Random.Next(numDocs);
                    writer.DeleteDocuments(new Term("id", Convert.ToString(id, CultureInfo.InvariantCulture)));
                }

                // merge some segments and ensure that at least one of them has more than
                // 256 values
                writer.ForceMerge(numDocs / 256);

            } // writer.Dispose();

            // compare
            using DirectoryReader ir = DirectoryReader.Open(dir);
            foreach (var context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                IBits expected = FieldCache.DEFAULT.GetDocsWithField(r, "indexed");
                IBits actual = FieldCache.DEFAULT.GetDocsWithField(r, "dv");
                AssertEquals(expected, actual);
            }
        }

        [Test]
        public virtual void TestBooleanNumericsVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestNumericsVsStoredFields(0, 1);
            }
        }

        [Test]
        public virtual void TestByteNumericsVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestNumericsVsStoredFields(sbyte.MinValue, sbyte.MaxValue);
            }
        }

        [Test]
        public virtual void TestByteMissingVsFieldCache()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestMissingVsFieldCache(sbyte.MinValue, sbyte.MaxValue);
            }
        }

        [Test]
        public virtual void TestInt16NumericsVsStoredFields() // LUCENENET specific - renamed from TestShortNumericsVsStoredFields
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestNumericsVsStoredFields(short.MinValue, short.MaxValue);
            }
        }

        [Test]
        public virtual void TestInt16MissingVsFieldCache() // LUCENENET specific - renamed from TestShortMissingVsFieldCache
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestMissingVsFieldCache(short.MinValue, short.MaxValue);
            }
        }

        [Test]
        public virtual void TestInt32NumericsVsStoredFields() // LUCENENET specific - renamed from TestIntNumericsVsStoredFields
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestNumericsVsStoredFields(int.MinValue, int.MaxValue);
            }
        }

        [Test]
        public virtual void TestInt32MissingVsFieldCache() // LUCENENET specific - renamed from TestIntMissingVsFieldCache
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestMissingVsFieldCache(int.MinValue, int.MaxValue);
            }
        }

        [Test]
        public virtual void TestInt64NumericsVsStoredFields() // LUCENENET specific - renamed from TestLongNumericsVsStoredFields
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestNumericsVsStoredFields(long.MinValue, long.MaxValue);
            }
        }

        [Test]
        public virtual void TestInt64MissingVsFieldCache() // LUCENENET specific - renamed from TestLongMissingVsFieldCache
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestMissingVsFieldCache(long.MinValue, long.MaxValue);
            }
        }

        private static void DoTestBinaryVsStoredFields(int minLength, int maxLength) // LUCENENET: CA1822: Mark members as static
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using (RandomIndexWriter writer = new RandomIndexWriter(Random, dir, conf))
            {
                Document doc = new Document();
                Field idField = new StringField("id", "", Field.Store.NO);
                Field storedField = new StoredField("stored", Arrays.Empty<byte>());
                Field dvField = new BinaryDocValuesField("dv", new BytesRef());
                doc.Add(idField);
                doc.Add(storedField);
                doc.Add(dvField);

                // index some docs
                int numDocs = AtLeast(300);
                for (int i = 0; i < numDocs; i++)
                {
                    idField.SetStringValue(Convert.ToString(i, CultureInfo.InvariantCulture));
                    int length;
                    if (minLength == maxLength)
                    {
                        length = minLength; // fixed length
                    }
                    else
                    {
                        length = TestUtil.NextInt32(Random, minLength, maxLength);
                    }
                    var buffer = new byte[length];
                    Random.NextBytes(buffer);
                    storedField.SetBytesValue(new BytesRef(buffer));
                    dvField.SetBytesValue(new BytesRef(buffer));
                    writer.AddDocument(doc);
                    if (Random.Next(31) == 0)
                    {
                        writer.Commit();
                    }
                }

                // delete some docs
                int numDeletions = Random.Next(numDocs / 10);
                for (int i = 0; i < numDeletions; i++)
                {
                    int id = Random.Next(numDocs);
                    writer.DeleteDocuments(new Term("id", Convert.ToString(id, CultureInfo.InvariantCulture)));
                }
            } // writer.Dispose();

            // compare
            using DirectoryReader ir = DirectoryReader.Open(dir);
            BytesRef scratch = new BytesRef(); // LUCENENET: Moved outside of the loop for performance
            foreach (AtomicReaderContext context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                BinaryDocValues docValues = r.GetBinaryDocValues("dv");
                for (int i = 0; i < r.MaxDoc; i++)
                {
                    BytesRef binaryValue = r.Document(i).GetBinaryValue("stored");

                    docValues.Get(i, scratch);
                    Assert.AreEqual(binaryValue, scratch);
                }
            }
        }

        [Test]
        public virtual void TestBinaryFixedLengthVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt32(Random, 0, 10);
                DoTestBinaryVsStoredFields(fixedLength, fixedLength);
            }
        }

        [Test]
        public virtual void TestBinaryVariableLengthVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestBinaryVsStoredFields(0, 10);
            }
        }

        private static void DoTestSortedVsStoredFields(int minLength, int maxLength) // LUCENENET: CA1822: Mark members as static
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using (RandomIndexWriter writer = new RandomIndexWriter(Random, dir, conf))
            {
                Document doc = new Document();
                Field idField = new StringField("id", "", Field.Store.NO);
                Field storedField = new StoredField("stored", Arrays.Empty<byte>());
                Field dvField = new SortedDocValuesField("dv", new BytesRef());
                doc.Add(idField);
                doc.Add(storedField);
                doc.Add(dvField);

                // index some docs
                int numDocs = AtLeast(300);
                for (int i = 0; i < numDocs; i++)
                {
                    idField.SetStringValue(Convert.ToString(i, CultureInfo.InvariantCulture));
                    int length;
                    if (minLength == maxLength)
                    {
                        length = minLength; // fixed length
                    }
                    else
                    {
                        length = TestUtil.NextInt32(Random, minLength, maxLength);
                    }
                    var buffer = new byte[length];
                    Random.NextBytes(buffer);
                    storedField.SetBytesValue(new BytesRef(buffer));
                    dvField.SetBytesValue(new BytesRef(buffer));
                    writer.AddDocument(doc);
                    if (Random.Next(31) == 0)
                    {
                        writer.Commit();
                    }
                }

                // delete some docs
                int numDeletions = Random.Next(numDocs / 10);
                for (int i = 0; i < numDeletions; i++)
                {
                    int id = Random.Next(numDocs);
                    writer.DeleteDocuments(new Term("id", Convert.ToString(id, CultureInfo.InvariantCulture)));
                }
            } // writer.Dispose();

            // compare
            using DirectoryReader ir = DirectoryReader.Open(dir);
            BytesRef scratch = new BytesRef(); // LUCENENET: Moved outside of the loop for performance
            foreach (AtomicReaderContext context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                BinaryDocValues docValues = r.GetSortedDocValues("dv");
                for (int i = 0; i < r.MaxDoc; i++)
                {
                    BytesRef binaryValue = r.Document(i).GetBinaryValue("stored");

                    docValues.Get(i, scratch);
                    Assert.AreEqual(binaryValue, scratch);
                }
            }
        }

        private static void DoTestSortedVsFieldCache(int minLength, int maxLength) // LUCENENET: CA1822: Mark members as static
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using (RandomIndexWriter writer = new RandomIndexWriter(Random, dir, conf))
            {
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
                    idField.SetStringValue(Convert.ToString(i, CultureInfo.InvariantCulture));
                    int length;
                    if (minLength == maxLength)
                    {
                        length = minLength; // fixed length
                    }
                    else
                    {
                        length = TestUtil.NextInt32(Random, minLength, maxLength);
                    }
                    string value = TestUtil.RandomSimpleString(Random, length);
                    indexedField.SetStringValue(value);
                    dvField.SetBytesValue(new BytesRef(value));
                    writer.AddDocument(doc);
                    if (Random.Next(31) == 0)
                    {
                        writer.Commit();
                    }
                }

                // delete some docs
                int numDeletions = Random.Next(numDocs / 10);
                for (int i = 0; i < numDeletions; i++)
                {
                    int id = Random.Next(numDocs);
                    writer.DeleteDocuments(new Term("id", Convert.ToString(id, CultureInfo.InvariantCulture)));
                }
            } // writer.Dispose();

            // compare
            using DirectoryReader ir = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                SortedDocValues expected = FieldCache.DEFAULT.GetTermsIndex(r, "indexed");
                SortedDocValues actual = r.GetSortedDocValues("dv");
                AssertEquals(r.MaxDoc, expected, actual);
            }
        }

        [Test]
        public virtual void TestSortedFixedLengthVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt32(Random, 1, 10);
                DoTestSortedVsStoredFields(fixedLength, fixedLength);
            }
        }

        [Test]
        public virtual void TestSortedFixedLengthVsFieldCache()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt32(Random, 1, 10);
                DoTestSortedVsFieldCache(fixedLength, fixedLength);
            }
        }

        [Test]
        public virtual void TestSortedVariableLengthVsFieldCache()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestSortedVsFieldCache(1, 10);
            }
        }

        [Test]
        public virtual void TestSortedVariableLengthVsStoredFields()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestSortedVsStoredFields(1, 10);
            }
        }

        [Test]
        public virtual void TestSortedSetOneValue()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory))
                {
                    Document doc = new Document();
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
                    iwriter.AddDocument(doc);

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");

                dv.SetDocument(0);
                Assert.AreEqual(0L, dv.NextOrd()); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                BytesRef bytes = new BytesRef();
                dv.LookupOrd(0, bytes);
                Assert.AreEqual(new BytesRef("hello"), bytes);
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestSortedSetTwoFields()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory))
                {

                    Document doc = new Document();
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
                    doc.Add(new SortedSetDocValuesField("field2", new BytesRef("world")));
                    iwriter.AddDocument(doc);

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");

                dv.SetDocument(0);
                Assert.AreEqual(0L, dv.NextOrd()); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                BytesRef bytes = new BytesRef();
                dv.LookupOrd(0, bytes);
                Assert.AreEqual(new BytesRef("hello"), bytes);

                dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field2");

                dv.SetDocument(0);
                Assert.AreEqual(0L, dv.NextOrd()); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                dv.LookupOrd(0, bytes);
                Assert.AreEqual(new BytesRef("world"), bytes);
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestSortedSetTwoDocumentsMerged()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, iwconfig))
                {

                    Document doc = new Document();
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
                    iwriter.AddDocument(doc);
                    iwriter.Commit();

                    doc = new Document();
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
                    iwriter.AddDocument(doc);
                    iwriter.ForceMerge(1);

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
                Assert.AreEqual(2L, dv.ValueCount); // LUCENENET specific - 2L required because types don't match (xUnit checks this)

                dv.SetDocument(0);
                Assert.AreEqual(0L, dv.NextOrd()); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                BytesRef bytes = new BytesRef();
                dv.LookupOrd(0, bytes);
                Assert.AreEqual(new BytesRef("hello"), bytes);

                dv.SetDocument(1);
                Assert.AreEqual(1L, dv.NextOrd()); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                dv.LookupOrd(1, bytes);
                Assert.AreEqual(new BytesRef("world"), bytes);
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestSortedSetTwoValues()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory))
                {

                    Document doc = new Document();
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
                    iwriter.AddDocument(doc);

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");

                dv.SetDocument(0);
                Assert.AreEqual(0L, dv.NextOrd()); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(1L, dv.NextOrd()); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                BytesRef bytes = new BytesRef();
                dv.LookupOrd(0, bytes);
                Assert.AreEqual(new BytesRef("hello"), bytes);

                dv.LookupOrd(1, bytes);
                Assert.AreEqual(new BytesRef("world"), bytes);
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestSortedSetTwoValuesUnordered()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory))
                {

                    Document doc = new Document();
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
                    iwriter.AddDocument(doc);

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");

                dv.SetDocument(0);
                Assert.AreEqual(0L, dv.NextOrd()); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(1L, dv.NextOrd()); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                BytesRef bytes = new BytesRef();
                dv.LookupOrd(0, bytes);
                Assert.AreEqual(new BytesRef("hello"), bytes);

                dv.LookupOrd(1, bytes);
                Assert.AreEqual(new BytesRef("world"), bytes);
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestSortedSetThreeValuesTwoDocs()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, iwconfig))
                {

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

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
                Assert.AreEqual(3L, dv.ValueCount); // LUCENENET specific - 3L required because types don't match (xUnit checks this)

                dv.SetDocument(0);
                Assert.AreEqual(1L, dv.NextOrd()); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                Assert.AreEqual(2L, dv.NextOrd()); // LUCENENET specific - 2L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                dv.SetDocument(1);
                Assert.AreEqual(0L, dv.NextOrd()); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(1L, dv.NextOrd()); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                BytesRef bytes = new BytesRef();
                dv.LookupOrd(0, bytes);
                Assert.AreEqual(new BytesRef("beer"), bytes);

                dv.LookupOrd(1, bytes);
                Assert.AreEqual(new BytesRef("hello"), bytes);

                dv.LookupOrd(2, bytes);
                Assert.AreEqual(new BytesRef("world"), bytes);
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestSortedSetTwoDocumentsLastMissing()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, iwconfig))
                {

                    Document doc = new Document();
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
                    iwriter.AddDocument(doc);

                    doc = new Document();
                    iwriter.AddDocument(doc);
                    iwriter.ForceMerge(1);
                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
                Assert.AreEqual(1L, dv.ValueCount); // LUCENENET specific - 1L required because types don't match (xUnit checks this)

                dv.SetDocument(0);
                Assert.AreEqual(0L, dv.NextOrd()); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                BytesRef bytes = new BytesRef();
                dv.LookupOrd(0, bytes);
                Assert.AreEqual(new BytesRef("hello"), bytes);
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestSortedSetTwoDocumentsLastMissingMerge()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, iwconfig))
                {

                    Document doc = new Document();
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
                    iwriter.AddDocument(doc);
                    iwriter.Commit();

                    doc = new Document();
                    iwriter.AddDocument(doc);
                    iwriter.ForceMerge(1);

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
                Assert.AreEqual(1L, dv.ValueCount); // LUCENENET specific - 1L required because types don't match (xUnit checks this)

                dv.SetDocument(0);
                Assert.AreEqual(0L, dv.NextOrd()); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                BytesRef bytes = new BytesRef();
                dv.LookupOrd(0, bytes);
                Assert.AreEqual(new BytesRef("hello"), bytes);
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestSortedSetTwoDocumentsFirstMissing()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, iwconfig))
                {

                    Document doc = new Document();
                    iwriter.AddDocument(doc);

                    doc = new Document();
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
                    iwriter.AddDocument(doc);

                    iwriter.ForceMerge(1);
                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
                Assert.AreEqual(1L, dv.ValueCount); // LUCENENET specific - 1L required because types don't match (xUnit checks this)

                dv.SetDocument(1);
                Assert.AreEqual(0L, dv.NextOrd()); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                BytesRef bytes = new BytesRef();
                dv.LookupOrd(0, bytes);
                Assert.AreEqual(new BytesRef("hello"), bytes);
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestSortedSetTwoDocumentsFirstMissingMerge()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, iwconfig))
                {

                    Document doc = new Document();
                    iwriter.AddDocument(doc);
                    iwriter.Commit();

                    doc = new Document();
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
                    iwriter.AddDocument(doc);
                    iwriter.ForceMerge(1);

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
                Assert.AreEqual(1L, dv.ValueCount); // LUCENENET specific - 1L required because types don't match (xUnit checks this)

                dv.SetDocument(1);
                Assert.AreEqual(0L, dv.NextOrd()); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());

                BytesRef bytes = new BytesRef();
                dv.LookupOrd(0, bytes);
                Assert.AreEqual(new BytesRef("hello"), bytes);
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestSortedSetMergeAwayAllValues()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, iwconfig))
                {

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

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
                Assert.AreEqual(0L, dv.ValueCount); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        [Test]
        public virtual void TestSortedSetTermsEnum()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            DirectoryReader ireader = null;
            try
            {
                using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, iwconfig))
                {

                    Document doc = new Document();
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
                    doc.Add(new SortedSetDocValuesField("field", new BytesRef("beer")));
                    iwriter.AddDocument(doc);

                    ireader = iwriter.GetReader();
                } // iwriter.Dispose();

                SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field");
                Assert.AreEqual(3L, dv.ValueCount); // LUCENENET specific - 3L required because types don't match (xUnit checks this)

                TermsEnum termsEnum = dv.GetTermsEnum();

                // next()
                Assert.IsTrue(termsEnum.MoveNext());
                Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(0L, termsEnum.Ord); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.IsTrue(termsEnum.MoveNext());
                Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(1L, termsEnum.Ord); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                Assert.IsTrue(termsEnum.MoveNext());
                Assert.AreEqual("world", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(2L, termsEnum.Ord); // LUCENENET specific - 2L required because types don't match (xUnit checks this)

                // seekCeil()
                Assert.AreEqual(SeekStatus.NOT_FOUND, termsEnum.SeekCeil(new BytesRef("ha!")));
                Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(1L, termsEnum.Ord); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                Assert.AreEqual(SeekStatus.FOUND, termsEnum.SeekCeil(new BytesRef("beer")));
                Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(0L, termsEnum.Ord); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.AreEqual(SeekStatus.END, termsEnum.SeekCeil(new BytesRef("zzz")));

                // seekExact()
                Assert.IsTrue(termsEnum.SeekExact(new BytesRef("beer")));
                Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(0L, termsEnum.Ord); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                Assert.IsTrue(termsEnum.SeekExact(new BytesRef("hello")));
                Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(1L, termsEnum.Ord); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                Assert.IsTrue(termsEnum.SeekExact(new BytesRef("world")));
                Assert.AreEqual("world", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(2L, termsEnum.Ord); // LUCENENET specific - 2L required because types don't match (xUnit checks this)
                Assert.IsFalse(termsEnum.SeekExact(new BytesRef("bogus")));

                // seek(ord)
                termsEnum.SeekExact(0);
                Assert.AreEqual("beer", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(0L, termsEnum.Ord); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                termsEnum.SeekExact(1);
                Assert.AreEqual("hello", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(1L, termsEnum.Ord); // LUCENENET specific - 1L required because types don't match (xUnit checks this)
                termsEnum.SeekExact(2);
                Assert.AreEqual("world", termsEnum.Term.Utf8ToString());
                Assert.AreEqual(2L, termsEnum.Ord); // LUCENENET specific - 2L required because types don't match (xUnit checks this)
            }
            finally
            {
                ireader?.Dispose();
            }
        }

        private static void DoTestSortedSetVsStoredFields(int minLength, int maxLength, int maxValuesPerDoc) // LUCENENET: CA1822: Mark members as static
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using (RandomIndexWriter writer = new RandomIndexWriter(Random, dir, conf))
            {

                // index some docs
                int numDocs = AtLeast(300);
                for (int i = 0; i < numDocs; i++)
                {
                    Document doc = new Document();
                    Field idField = new StringField("id", Convert.ToString(i, CultureInfo.InvariantCulture), Field.Store.NO);
                    doc.Add(idField);
                    int length;
                    if (minLength == maxLength)
                    {
                        length = minLength; // fixed length
                    }
                    else
                    {
                        length = TestUtil.NextInt32(Random, minLength, maxLength);
                    }
                    int numValues = TestUtil.NextInt32(Random, 0, maxValuesPerDoc);

                    // create a random set of strings
                    // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
                    JCG.SortedSet<string> values = new JCG.SortedSet<string>(StringComparer.Ordinal);
                    for (int v = 0; v < numValues; v++)
                    {
                        values.Add(TestUtil.RandomSimpleString(Random, length));
                    }

                    // add ordered to the stored field
                    foreach (string v in values)
                    {
                        doc.Add(new StoredField("stored", v));
                    }

                    // add in any order to the dv field
                    IList<string> unordered = new JCG.List<string>(values);
                    unordered.Shuffle(Random);
                    foreach (string v in unordered)
                    {
                        doc.Add(new SortedSetDocValuesField("dv", new BytesRef(v)));
                    }

                    writer.AddDocument(doc);
                    if (Random.Next(31) == 0)
                    {
                        writer.Commit();
                    }
                }

                // delete some docs
                int numDeletions = Random.Next(numDocs / 10);
                for (int i = 0; i < numDeletions; i++)
                {
                    int id = Random.Next(numDocs);
                    writer.DeleteDocuments(new Term("id", Convert.ToString(id, CultureInfo.InvariantCulture)));
                }
            } // writer.Dispose();

            // compare
            using DirectoryReader ir = DirectoryReader.Open(dir);
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
                        docValues.SetDocument(i);
                    }
                    for (int j = 0; j < stringValues.Length; j++)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(docValues != null);
                        long ord = docValues.NextOrd();
                        if (Debugging.AssertsEnabled) Debugging.Assert(ord != SortedSetDocValues.NO_MORE_ORDS);
                        docValues.LookupOrd(ord, scratch);
                        Assert.AreEqual(stringValues[j], scratch.Utf8ToString());
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(docValues is null || docValues.NextOrd() == SortedSetDocValues.NO_MORE_ORDS);
                }
            }
        }

        [Test]
        public virtual void TestSortedSetFixedLengthVsStoredFields()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt32(Random, 1, 10);
                DoTestSortedSetVsStoredFields(fixedLength, fixedLength, 16);
            }
        }

        [Test]
        public virtual void TestSortedSetVariableLengthVsStoredFields()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestSortedSetVsStoredFields(1, 10, 16);
            }
        }

        [Test]
        public virtual void TestSortedSetFixedLengthSingleValuedVsStoredFields()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt32(Random, 1, 10);
                DoTestSortedSetVsStoredFields(fixedLength, fixedLength, 1);
            }
        }

        [Test]
        public virtual void TestSortedSetVariableLengthSingleValuedVsStoredFields()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestSortedSetVsStoredFields(1, 10, 1);
            }
        }

        private static void AssertEquals(IBits expected, IBits actual) // LUCENENET: CA1822: Mark members as static
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected.Get(i), actual.Get(i));
            }
        }

        private static void AssertEquals(int maxDoc, SortedDocValues expected, SortedDocValues actual) // LUCENENET: CA1822: Mark members as static
        {
            AssertEquals(maxDoc, new SingletonSortedSetDocValues(expected), new SingletonSortedSetDocValues(actual));
        }

        private static void AssertEquals(int maxDoc, SortedSetDocValues expected, SortedSetDocValues actual) // LUCENENET: CA1822: Mark members as static
        {
            // can be null for the segment if no docs actually had any SortedDocValues
            // in this case FC.getDocTermsOrds returns EMPTY
            if (actual is null)
            {
                Assert.AreEqual(DocValues.EMPTY_SORTED_SET, expected);
                return;
            }
            Assert.AreEqual(expected.ValueCount, actual.ValueCount);
            // compare ord lists
            for (int i = 0; i < maxDoc; i++)
            {
                expected.SetDocument(i);
                actual.SetDocument(i);
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
            AssertEquals(expected.ValueCount, expected.GetTermsEnum(), actual.GetTermsEnum());
        }

        private static void AssertEquals(long numOrds, TermsEnum expected, TermsEnum actual) // LUCENENET: CA1822: Mark members as static
        {
            // sequential next() through all terms
            while (expected.MoveNext())
            {
                Assert.IsTrue(actual.MoveNext());
                Assert.AreEqual(expected.Ord, actual.Ord);
                Assert.AreEqual(expected.Term, actual.Term);
            }
            Assert.IsFalse(actual.MoveNext());

            // sequential seekExact(ord) through all terms
            for (long i = 0; i < numOrds; i++)
            {
                expected.SeekExact(i);
                actual.SeekExact(i);
                Assert.AreEqual(expected.Ord, actual.Ord);
                Assert.AreEqual(expected.Term, actual.Term);
            }

            // sequential seekExact(BytesRef) through all terms
            for (long i = 0; i < numOrds; i++)
            {
                expected.SeekExact(i);
                Assert.IsTrue(actual.SeekExact(expected.Term));
                Assert.AreEqual(expected.Ord, actual.Ord);
                Assert.AreEqual(expected.Term, actual.Term);
            }

            // sequential seekCeil(BytesRef) through all terms
            for (long i = 0; i < numOrds; i++)
            {
                expected.SeekExact(i);
                Assert.AreEqual(SeekStatus.FOUND, actual.SeekCeil(expected.Term));
                Assert.AreEqual(expected.Ord, actual.Ord);
                Assert.AreEqual(expected.Term, actual.Term);
            }

            // random seekExact(ord)
            for (long i = 0; i < numOrds; i++)
            {
                long randomOrd = TestUtil.NextInt64(Random, 0, numOrds - 1);
                expected.SeekExact(randomOrd);
                actual.SeekExact(randomOrd);
                Assert.AreEqual(expected.Ord, actual.Ord);
                Assert.AreEqual(expected.Term, actual.Term);
            }

            // random seekExact(BytesRef)
            for (long i = 0; i < numOrds; i++)
            {
                long randomOrd = TestUtil.NextInt64(Random, 0, numOrds - 1);
                expected.SeekExact(randomOrd);
                actual.SeekExact(expected.Term);
                Assert.AreEqual(expected.Ord, actual.Ord);
                Assert.AreEqual(expected.Term, actual.Term);
            }

            // random seekCeil(BytesRef)
            for (long i = 0; i < numOrds; i++)
            {
                BytesRef target = new BytesRef(TestUtil.RandomUnicodeString(Random));
                SeekStatus expectedStatus = expected.SeekCeil(target);
                Assert.AreEqual(expectedStatus, actual.SeekCeil(target));
                if (expectedStatus != SeekStatus.END)
                {
                    Assert.AreEqual(expected.Ord, actual.Ord);
                    Assert.AreEqual(expected.Term, actual.Term);
                }
            }
        }

        private static void DoTestSortedSetVsUninvertedField(int minLength, int maxLength) // LUCENENET: CA1822: Mark members as static
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using RandomIndexWriter writer = new RandomIndexWriter(Random, dir, conf);

            // index some docs
            int numDocs = AtLeast(300);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                Field idField = new StringField("id", Convert.ToString(i, CultureInfo.InvariantCulture), Field.Store.NO);
                doc.Add(idField);
                int length;
                if (minLength == maxLength)
                {
                    length = minLength; // fixed length
                }
                else
                {
                    length = TestUtil.NextInt32(Random, minLength, maxLength);
                }
                int numValues = Random.Next(17);
                // create a random list of strings
                IList<string> values = new JCG.List<string>();
                for (int v = 0; v < numValues; v++)
                {
                    values.Add(TestUtil.RandomSimpleString(Random, length));
                }

                // add in any order to the indexed field
                IList<string> unordered = new JCG.List<string>(values);
                unordered.Shuffle(Random);
                foreach (string v in unordered)
                {
                    doc.Add(NewStringField("indexed", v, Field.Store.NO));
                }

                // add in any order to the dv field
                IList<string> unordered2 = new JCG.List<string>(values);
                unordered2.Shuffle(Random);
                foreach (string v in unordered2)
                {
                    doc.Add(new SortedSetDocValuesField("dv", new BytesRef(v)));
                }

                writer.AddDocument(doc);
                if (Random.Next(31) == 0)
                {
                    writer.Commit();
                }
            }

            // delete some docs
            int numDeletions = Random.Next(numDocs / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                int id = Random.Next(numDocs);
                writer.DeleteDocuments(new Term("id", Convert.ToString(id, CultureInfo.InvariantCulture)));
            }

            // compare per-segment
            using (DirectoryReader ir = writer.GetReader())
            {
                foreach (AtomicReaderContext context in ir.Leaves)
                {
                    AtomicReader r = context.AtomicReader;
                    SortedSetDocValues expected = FieldCache.DEFAULT.GetDocTermOrds(r, "indexed");
                    SortedSetDocValues actual = r.GetSortedSetDocValues("dv");
                    AssertEquals(r.MaxDoc, expected, actual);
                }
            } // ir.Dispose();

            writer.ForceMerge(1);

            // now compare again after the merge
            using (DirectoryReader ir = writer.GetReader())
            {
                AtomicReader ar = GetOnlySegmentReader(ir);
                SortedSetDocValues expected_ = FieldCache.DEFAULT.GetDocTermOrds(ar, "indexed");
                SortedSetDocValues actual_ = ar.GetSortedSetDocValues("dv");
                AssertEquals(ir.MaxDoc, expected_, actual_);
            } // ir.Dispose();
        }

        [Test]
        public virtual void TestSortedSetFixedLengthVsUninvertedField()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                int fixedLength = TestUtil.NextInt32(Random, 1, 10);
                DoTestSortedSetVsUninvertedField(fixedLength, fixedLength);
            }
        }

        [Test]
        public virtual void TestSortedSetVariableLengthVsUninvertedField()
        {
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                DoTestSortedSetVsUninvertedField(1, 10);
            }
        }

        [Test]
        public virtual void TestGCDCompression()
        {
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                long min = -(((long)Random.Next(1 << 30)) << 32);
                long mul = Random.Next() & 0xFFFFFFFFL;
                Int64Producer longs = new Int64ProducerAnonymousClass3(min, mul);
                DoTestNumericsVsStoredFields(longs);
            }
        }

        private sealed class Int64ProducerAnonymousClass3 : Int64Producer
        {
            private readonly long min;
            private readonly long mul;

            public Int64ProducerAnonymousClass3(long min, long mul)
            {
                this.min = min;
                this.mul = mul;
            }

            internal override long Next()
            {
                return min + mul * Random.Next(1 << 20);
            }
        }

        [Test]
        public virtual void TestZeros()
        {
            DoTestNumericsVsStoredFields(0, 0);
        }

        [Test]
        public virtual void TestZeroOrMin()
        {
            // try to make GCD compression fail if the format did not anticipate that
            // the GCD of 0 and MIN_VALUE is negative
            int numIterations = AtLeast(1);
            for (int i = 0; i < numIterations; i++)
            {
                Int64Producer longs = new Int64ProducerAnonymousClass4();
                DoTestNumericsVsStoredFields(longs);
            }
        }

        private sealed class Int64ProducerAnonymousClass4 : Int64Producer
        {
            internal override long Next()
            {
                return Random.NextBoolean() ? 0 : long.MinValue;
            }
        }

        [Test]
        public virtual void TestTwoNumbersOneMissing()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField);
            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iw = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new StringField("id", "0", Field.Store.YES));
                doc.Add(new NumericDocValuesField("dv1", 0));
                iw.AddDocument(doc);
                doc = new Document();
                doc.Add(new StringField("id", "1", Field.Store.YES));
                iw.AddDocument(doc);
                iw.ForceMerge(1);
            } // iw.Dispose();

            using IndexReader ir = DirectoryReader.Open(directory);
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader ar = (AtomicReader)ir.Leaves[0].Reader;
            NumericDocValues dv = ar.GetNumericDocValues("dv1");
            Assert.AreEqual(0L, dv.Get(0)); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
            Assert.AreEqual(0L, dv.Get(1)); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
            IBits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
        }

        [Test]
        public virtual void TestTwoNumbersOneMissingWithMerging()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField);
            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iw = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new StringField("id", "0", Field.Store.YES));
                doc.Add(new NumericDocValuesField("dv1", 0));
                iw.AddDocument(doc);
                iw.Commit();
                doc = new Document();
                doc.Add(new StringField("id", "1", Field.Store.YES));
                iw.AddDocument(doc);
                iw.ForceMerge(1);
            } // iw.Dispose();

            using IndexReader ir = DirectoryReader.Open(directory);
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader ar = (AtomicReader)ir.Leaves[0].Reader;
            NumericDocValues dv = ar.GetNumericDocValues("dv1");
            Assert.AreEqual(0L, dv.Get(0)); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
            Assert.AreEqual(0L, dv.Get(1)); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
            IBits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
        }

        [Test]
        public virtual void TestThreeNumbersOneMissingWithMerging()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField);
            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iw = new RandomIndexWriter(Random, directory, conf))
            {
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
            } // iw.Dispose();

            using IndexReader ir = DirectoryReader.Open(directory);
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader ar = (AtomicReader)ir.Leaves[0].Reader;
            NumericDocValues dv = ar.GetNumericDocValues("dv1");
            Assert.AreEqual(0L, dv.Get(0)); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
            Assert.AreEqual(0L, dv.Get(1)); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
            Assert.AreEqual(5L, dv.Get(2)); // LUCENENET specific - 5L required because types don't match (xUnit checks this)
            IBits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
            Assert.IsTrue(docsWithField.Get(2));
        }

        [Test]
        public virtual void TestTwoBytesOneMissing()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField);
            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iw = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new StringField("id", "0", Field.Store.YES));
                doc.Add(new BinaryDocValuesField("dv1", new BytesRef()));
                iw.AddDocument(doc);
                doc = new Document();
                doc.Add(new StringField("id", "1", Field.Store.YES));
                iw.AddDocument(doc);
                iw.ForceMerge(1);
            } // iw.Dispose();

            using IndexReader ir = DirectoryReader.Open(directory);
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader ar = (AtomicReader)ir.Leaves[0].Reader;
            BinaryDocValues dv = ar.GetBinaryDocValues("dv1");
            BytesRef @ref = new BytesRef();
            dv.Get(0, @ref);
            Assert.AreEqual(new BytesRef(), @ref);
            dv.Get(1, @ref);
            Assert.AreEqual(new BytesRef(), @ref);
            IBits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
        }

        [Test]
        public virtual void TestTwoBytesOneMissingWithMerging()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField);
            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iw = new RandomIndexWriter(Random, directory, conf))
            {
                Document doc = new Document();
                doc.Add(new StringField("id", "0", Field.Store.YES));
                doc.Add(new BinaryDocValuesField("dv1", new BytesRef()));
                iw.AddDocument(doc);
                iw.Commit();
                doc = new Document();
                doc.Add(new StringField("id", "1", Field.Store.YES));
                iw.AddDocument(doc);
                iw.ForceMerge(1);
            } // iw.Dispose();

            using IndexReader ir = DirectoryReader.Open(directory);
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader ar = (AtomicReader)ir.Leaves[0].Reader;
            BinaryDocValues dv = ar.GetBinaryDocValues("dv1");
            BytesRef @ref = new BytesRef();
            dv.Get(0, @ref);
            Assert.AreEqual(new BytesRef(), @ref);
            dv.Get(1, @ref);
            Assert.AreEqual(new BytesRef(), @ref);
            IBits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
        }

        [Test]
        public virtual void TestThreeBytesOneMissingWithMerging()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField);
            using Directory directory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergePolicy(NewLogMergePolicy());
            using (RandomIndexWriter iw = new RandomIndexWriter(Random, directory, conf))
            {
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
            } // iw.Dispose();

            using IndexReader ir = DirectoryReader.Open(directory);
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
            IBits docsWithField = ar.GetDocsWithField("dv1");
            Assert.IsTrue(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));
            Assert.IsTrue(docsWithField.Get(2));
        }

        // LUCENE-4853
        [Test]
        public virtual void TestHugeBinaryValues()
        {
            Analyzer analyzer = new MockAnalyzer(Random);
            // FSDirectory because SimpleText will consume gobbs of
            // space when storing big binary values:
            Directory d = NewFSDirectory(CreateTempDir("hugeBinaryValues"));
            bool directoryDisposed = false;
            try
            {
                bool doFixed = Random.NextBoolean();
                int numDocs;
                int fixedLength = 0;
                if (doFixed)
                {
                    // Sometimes make all values fixed length since some
                    // codecs have different code paths for this:
                    numDocs = TestUtil.NextInt32(Random, 10, 20);
                    fixedLength = TestUtil.NextInt32(Random, 65537, 256 * 1024);
                }
                else
                {
                    numDocs = TestUtil.NextInt32(Random, 100, 200);
                }
                var docBytes = new JCG.List<byte[]>();
                DirectoryReader r = null;
                try
                {
                    using (IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer)))
                    {
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
                            else if (docID == 0 || Random.Next(5) == 3)
                            {
                                numBytes = TestUtil.NextInt32(Random, 65537, 3 * 1024 * 1024);
                            }
                            else
                            {
                                numBytes = TestUtil.NextInt32(Random, 1, 1024 * 1024);
                            }
                            totalBytes += numBytes;
                            if (totalBytes > 5 * 1024 * 1024)
                            {
                                break;
                            }
                            var bytes = new byte[numBytes];
                            Random.NextBytes(bytes);
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
                            catch (Exception iae) when (iae.IsIllegalArgumentException())
                            {
                                if (iae.Message.IndexOf("is too large", StringComparison.Ordinal) == -1)
                                {
                                    throw /*iae*/; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                                }
                                else
                                {
                                    // OK: some codecs can't handle binary DV > 32K
                                    Assert.IsFalse(CodecAcceptsHugeBinaryValues("field"));
                                    w.Rollback();
                                    d.Dispose();
                                    directoryDisposed = true; // LUCENENET specific
                                    return;
                                }
                            }
                        }

                        //DirectoryReader r; // LUCENENET: declaration moved outside w's using block
                        try
                        {
                            r = w.GetReader();
                        }
                        catch (Exception iae) when (iae.IsIllegalArgumentException())
                        {
                            if (iae.Message.IndexOf("is too large", StringComparison.Ordinal) == -1)
                            {
                                throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                            }
                            else
                            {
                                Assert.IsFalse(CodecAcceptsHugeBinaryValues("field"));

                                // OK: some codecs can't handle binary DV > 32K
                                w.Rollback();
                                d.Dispose();
                                directoryDisposed = true; // LUCENENET specific
                                return;
                            }
                        }
                    } // w.Dispose();

                    using (AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r))
                    {
                        BytesRef bytes = new BytesRef(); // LUCENENET: Moved outside of the loop for performance
                        BinaryDocValues s = FieldCache.DEFAULT.GetTerms(ar, "field", false);
                        for (int docID = 0; docID < docBytes.Count; docID++)
                        {
                            Document doc = ar.Document(docID);
                            
                            s.Get(docID, bytes);
                            var expected = docBytes[Convert.ToInt32(doc.Get("id"), CultureInfo.InvariantCulture)];
                            Assert.AreEqual(expected.Length, bytes.Length);
                            Assert.AreEqual(new BytesRef(expected), bytes);
                        }

                        Assert.IsTrue(CodecAcceptsHugeBinaryValues("field"));

                    } // ar.Dispose();
                }
                finally
                {
                    r?.Dispose(); // LUCENENET specific - small chance w.Dispose() will throw, this is just here to cover that case. It is safe to call r.Dispose() more than once.
                }
            }
            finally
            {
                // LUCENENET: MMapDirectory is not safe to call dispose on twice (a bug?), so we
                // need to ensure that if another path got it already that we don't do it again here.
                if (!directoryDisposed)
                    d.Dispose();
            }
        }

        // TODO: get this out of here and into the deprecated codecs (4.0, 4.2)
        [Test]
        public virtual void TestHugeBinaryValueLimit()
        {
            // We only test DVFormats that have a limit
            AssumeFalse("test requires codec with limits on max binary field length", CodecAcceptsHugeBinaryValues("field"));
            Analyzer analyzer = new MockAnalyzer(Random);
            // FSDirectory because SimpleText will consume gobbs of
            // space when storing big binary values:
            using Directory d = NewFSDirectory(CreateTempDir("hugeBinaryValues"));
            bool doFixed = Random.NextBoolean();
            int numDocs;
            int fixedLength = 0;
            if (doFixed)
            {
                // Sometimes make all values fixed length since some
                // codecs have different code paths for this:
                numDocs = TestUtil.NextInt32(Random, 10, 20);
#pragma warning disable 612, 618
                fixedLength = Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH;
#pragma warning restore 612, 618
            }
            else
            {
                numDocs = TestUtil.NextInt32(Random, 100, 200);
            }
            var docBytes = new JCG.List<byte[]>();
            DirectoryReader r = null;
            try
            {
                using (IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer)))
                {
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
                        else if (docID == 0 || Random.Next(5) == 3)
                        {
#pragma warning disable 612, 618
                            numBytes = Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH;
                        }
                        else
                        {
                            numBytes = TestUtil.NextInt32(Random, 1, Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH);
#pragma warning restore 612, 618
                        }
                        totalBytes += numBytes;
                        if (totalBytes > 5 * 1024 * 1024)
                        {
                            break;
                        }
                        var bytes = new byte[numBytes];
                        Random.NextBytes(bytes);
                        docBytes.Add(bytes);
                        Document doc = new Document();
                        BytesRef b = new BytesRef(bytes);
                        b.Length = bytes.Length;
                        doc.Add(new BinaryDocValuesField("field", b));
                        doc.Add(new StringField("id", "" + docID, Field.Store.YES));
                        w.AddDocument(doc);
                    }

                    r = w.GetReader();
                } // w.Dispose();

                using (AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r))
                {
                    BytesRef bytes = new BytesRef(); // LUCENENET: Moved outside of the loop for performance
                    BinaryDocValues s = FieldCache.DEFAULT.GetTerms(ar, "field", false);
                    for (int docID = 0; docID < docBytes.Count; docID++)
                    {
                        Document doc = ar.Document(docID);

                        s.Get(docID, bytes);
                        var expected = docBytes[Convert.ToInt32(doc.Get("id"), CultureInfo.InvariantCulture)];
                        Assert.AreEqual(expected.Length, bytes.Length);
                        Assert.AreEqual(new BytesRef(expected), bytes);
                    }

                } // ar.Dispose();
            }
            finally
            {
                r?.Dispose(); // LUCENENET specific - small chance w.Dispose() will throw, this is just here to cover that case. It is safe to call r.Dispose() more than once.
            }
        }

        /// <summary>
        /// Tests dv against stored fields with threads (binary/numeric/sorted, no missing)
        /// </summary>
        [Test]
        public virtual void TestThreads()
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using (RandomIndexWriter writer = new RandomIndexWriter(Random, dir, conf))
            {
                Document doc = new Document();
                Field idField = new StringField("id", "", Field.Store.NO);
                Field storedBinField = new StoredField("storedBin", Arrays.Empty<byte>());
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
                    idField.SetStringValue(Convert.ToString(i, CultureInfo.InvariantCulture));
                    int length = TestUtil.NextInt32(Random, 0, 8);
                    var buffer = new byte[length];
                    Random.NextBytes(buffer);
                    storedBinField.SetBytesValue(buffer);
                    dvBinField.SetBytesValue(buffer);
                    dvSortedField.SetBytesValue(buffer);
                    long numericValue = Random.NextInt64();
                    storedNumericField.SetStringValue(Convert.ToString(numericValue, CultureInfo.InvariantCulture));
                    dvNumericField.SetInt64Value(numericValue);
                    writer.AddDocument(doc);
                    if (Random.Next(31) == 0)
                    {
                        writer.Commit();
                    }
                }

                // delete some docs
                int numDeletions = Random.Next(numDocs / 10);
                for (int i = 0; i < numDeletions; i++)
                {
                    int id = Random.Next(numDocs);
                    writer.DeleteDocuments(new Term("id", Convert.ToString(id, CultureInfo.InvariantCulture)));
                }
            } // writer.Dispose();

            // compare
            using DirectoryReader ir = DirectoryReader.Open(dir);
            int numThreads = TestUtil.NextInt32(Random, 2, 7);
            ThreadJob[] threads = new ThreadJob[numThreads];
            using CountdownEvent startingGun = new CountdownEvent(1);
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ThreadAnonymousClass(ir, startingGun);
                threads[i].Start();
            }
            startingGun.Signal();
            foreach (ThreadJob t in threads)
            {
                t.Join();
            }
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly DirectoryReader ir;
            private readonly CountdownEvent startingGun;

            public ThreadAnonymousClass(DirectoryReader ir, CountdownEvent startingGun)
            {
                this.ir = ir;
                this.startingGun = startingGun;
            }

            public override void Run()
            {
                try
                {
                    startingGun.Wait();
                    BytesRef scratch = new BytesRef(); // LUCENENET: Moved outside of the loop for performance
                    foreach (AtomicReaderContext context in ir.Leaves)
                    {
                        AtomicReader r = context.AtomicReader;
                        BinaryDocValues binaries = r.GetBinaryDocValues("dvBin");
                        SortedDocValues sorted = r.GetSortedDocValues("dvSorted");
                        NumericDocValues numerics = r.GetNumericDocValues("dvNum");
                        for (int j = 0; j < r.MaxDoc; j++)
                        {
                            BytesRef binaryValue = r.Document(j).GetBinaryValue("storedBin");

                            binaries.Get(j, scratch);
                            Assert.AreEqual(binaryValue, scratch);
                            sorted.Get(j, scratch);
                            Assert.AreEqual(binaryValue, scratch);
                            string expected = r.Document(j).Get("storedNum");
                            Assert.AreEqual(Convert.ToInt64(expected, CultureInfo.InvariantCulture), numerics.Get(j));
                        }
                    }
                    TestUtil.CheckReader(ir);
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        /// <summary>
        /// Tests dv against stored fields with threads (all types + missing)
        /// </summary>
        [Test]
        public virtual void TestThreads2()
        {
            AssumeTrue("Codec does not support GetDocsWithField", DefaultCodecSupportsDocsWithField);
            AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            using Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using (RandomIndexWriter writer = new RandomIndexWriter(Random, dir, conf))
            {
                Field idField = new StringField("id", "", Field.Store.NO);
                Field storedBinField = new StoredField("storedBin", Arrays.Empty<byte>());
                Field dvBinField = new BinaryDocValuesField("dvBin", new BytesRef());
                Field dvSortedField = new SortedDocValuesField("dvSorted", new BytesRef());
                Field storedNumericField = new StoredField("storedNum", "");
                Field dvNumericField = new NumericDocValuesField("dvNum", 0);

                // index some docs
                int numDocs = AtLeast(300);
                for (int i = 0; i < numDocs; i++)
                {
                    idField.SetStringValue(Convert.ToString(i, CultureInfo.InvariantCulture));
                    int length = TestUtil.NextInt32(Random, 0, 8);
                    var buffer = new byte[length];
                    Random.NextBytes(buffer);
                    storedBinField.SetBytesValue(buffer);
                    dvBinField.SetBytesValue(buffer);
                    dvSortedField.SetBytesValue(buffer);
                    long numericValue = Random.NextInt64();
                    storedNumericField.SetStringValue(Convert.ToString(numericValue, CultureInfo.InvariantCulture));
                    dvNumericField.SetInt64Value(numericValue);
                    Document doc = new Document();
                    doc.Add(idField);
                    if (Random.Next(4) > 0)
                    {
                        doc.Add(storedBinField);
                        doc.Add(dvBinField);
                        doc.Add(dvSortedField);
                    }
                    if (Random.Next(4) > 0)
                    {
                        doc.Add(storedNumericField);
                        doc.Add(dvNumericField);
                    }
                    int numSortedSetFields = Random.Next(3);

                    // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
                    JCG.SortedSet<string> values = new JCG.SortedSet<string>(StringComparer.Ordinal);
                    for (int j = 0; j < numSortedSetFields; j++)
                    {
                        values.Add(TestUtil.RandomSimpleString(Random));
                    }
                    foreach (string v in values)
                    {
                        doc.Add(new SortedSetDocValuesField("dvSortedSet", new BytesRef(v)));
                        doc.Add(new StoredField("storedSortedSet", v));
                    }
                    writer.AddDocument(doc);
                    if (Random.Next(31) == 0)
                    {
                        writer.Commit();
                    }
                }

                // delete some docs
                int numDeletions = Random.Next(numDocs / 10);
                for (int i = 0; i < numDeletions; i++)
                {
                    int id = Random.Next(numDocs);
                    writer.DeleteDocuments(new Term("id", Convert.ToString(id, CultureInfo.InvariantCulture)));
                }
            } // writer.Dispose();

            // compare
            using DirectoryReader ir = DirectoryReader.Open(dir);
            int numThreads = TestUtil.NextInt32(Random, 2, 7);
            ThreadJob[] threads = new ThreadJob[numThreads];
            using CountdownEvent startingGun = new CountdownEvent(1);
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ThreadAnonymousClass2(ir, startingGun);
                threads[i].Start();
            }
            startingGun.Signal();
            foreach (ThreadJob t in threads)
            {
                t.Join();
            }
        }

        private sealed class ThreadAnonymousClass2 : ThreadJob
        {
            private readonly DirectoryReader ir;
            private readonly CountdownEvent startingGun;

            public ThreadAnonymousClass2(DirectoryReader ir, CountdownEvent startingGun)
            {
                this.ir = ir;
                this.startingGun = startingGun;
            }

            public override void Run()
            {
                try
                {
                    startingGun.Wait();
                    foreach (AtomicReaderContext context in ir.Leaves)
                    {
                        AtomicReader r = context.AtomicReader;
                        BinaryDocValues binaries = r.GetBinaryDocValues("dvBin");
                        IBits binaryBits = r.GetDocsWithField("dvBin");
                        SortedDocValues sorted = r.GetSortedDocValues("dvSorted");
                        IBits sortedBits = r.GetDocsWithField("dvSorted");
                        NumericDocValues numerics = r.GetNumericDocValues("dvNum");
                        IBits numericBits = r.GetDocsWithField("dvNum");
                        SortedSetDocValues sortedSet = r.GetSortedSetDocValues("dvSortedSet");
                        IBits sortedSetBits = r.GetDocsWithField("dvSortedSet");
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
                                    Assert.AreEqual(Convert.ToInt64(number, CultureInfo.InvariantCulture), numerics.Get(j));
                                }
                            }
                            else if (numerics != null)
                            {
                                Assert.IsFalse(numericBits.Get(j));
                                Assert.AreEqual(0L, numerics.Get(j)); // LUCENENET specific - 0L required because types don't match (xUnit checks this)
                            }

                            string[] values = r.Document(j).GetValues("storedSortedSet");
                            if (values.Length > 0)
                            {
                                Assert.IsNotNull(sortedSet);
                                sortedSet.SetDocument(j);
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
                                sortedSet.SetDocument(j);
                                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd());
                                Assert.IsFalse(sortedSetBits.Get(j));
                            }
                        }
                    }
                    TestUtil.CheckReader(ir);
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        // LUCENE-5218
        [Test]
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
                using Directory dir = NewDirectory();
                IndexReader r = null;
                try
                {
                    using (RandomIndexWriter w = new RandomIndexWriter(Random, dir))
                    {
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
                        r = w.GetReader();
                    } // w.Dispose();

                    using AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);
                    BinaryDocValues values = ar.GetBinaryDocValues("field");
                    BytesRef result = new BytesRef();
                    for (int j = 0; j < 5; j++)
                    {
                        values.Get(0, result);
                        Assert.IsTrue(result.Length == 0 || result.Length == 1 << i);
                    }
                }
                finally
                {
                    r?.Dispose(); // LUCENENET specific - small chance w.Dispose() will throw, this is just here to cover that case. It is safe to call r.Dispose() more than once.
                }
            }
        }

        protected virtual bool CodecAcceptsHugeBinaryValues(string field)
        {
            return true;
        }
    }
}