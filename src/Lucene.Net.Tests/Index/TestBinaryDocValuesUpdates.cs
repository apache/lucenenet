using J2N;
using J2N.Numerics;
using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

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

    using AssertingDocValuesFormat = Lucene.Net.Codecs.Asserting.AssertingDocValuesFormat;
    using BinaryDocValuesField = BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;
    using IBits = Lucene.Net.Util.IBits;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using Lucene40RWCodec = Lucene.Net.Codecs.Lucene40.Lucene40RWCodec;
    using Lucene41RWCodec = Lucene.Net.Codecs.Lucene41.Lucene41RWCodec;
    using Lucene42RWCodec = Lucene.Net.Codecs.Lucene42.Lucene42RWCodec;
    using Lucene45DocValuesFormat = Lucene.Net.Codecs.Lucene45.Lucene45DocValuesFormat;
    using Lucene45RWCodec = Lucene.Net.Codecs.Lucene45.Lucene45RWCodec;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using NumericDocValuesField = NumericDocValuesField;
    using SortedDocValuesField = SortedDocValuesField;
    using SortedSetDocValuesField = SortedSetDocValuesField;
    using Store = Field.Store;
    using StringField = StringField;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [SuppressCodecs("Appending", "Lucene3x", "Lucene40", "Lucene41", "Lucene42", "Lucene45")]
    [TestFixture]
    public class TestBinaryDocValuesUpdates : LuceneTestCase
    {
        internal static long GetValue(BinaryDocValues bdv, int idx, BytesRef scratch)
        {
            bdv.Get(idx, scratch);
            idx = scratch.Offset;
            var b = scratch.Bytes[idx++];
            long value = b & 0x7FL;
            for (int shift = 7; (b & 0x80L) != 0; shift += 7)
            {
                b = scratch.Bytes[idx++];
                value |= (b & 0x7FL) << shift;
            }
            return value;
        }

        // encodes a long into a BytesRef as VLong so that we get varying number of bytes when we update
        internal static BytesRef ToBytes(long value)
        {
            //    long orig = value;
            BytesRef bytes = new BytesRef(10); // negative longs may take 10 bytes
            while ((value & ~0x7FL) != 0L)
            {
                bytes.Bytes[bytes.Length++] = unchecked((byte)((value & 0x7FL) | 0x80L));
                value = value.TripleShift(7);
            }
            bytes.Bytes[bytes.Length++] = (byte)value;
            //    System.err.println("[" + Thread.currentThread().getName() + "] value=" + orig + ", bytes=" + bytes);
            return bytes;
        }

        private Document Doc(int id)
        {
            Document doc = new Document();
            doc.Add(new StringField("id", "doc-" + id, Store.NO));
            doc.Add(new BinaryDocValuesField("val", ToBytes(id + 1)));
            return doc;
        }

#if FEATURE_INDEXWRITER_TESTS

        [Test]
        public virtual void TestUpdatesAreFlushed()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetRAMBufferSizeMB(0.00000001));
            writer.AddDocument(Doc(0)); // val=1
            writer.AddDocument(Doc(1)); // val=2
            writer.AddDocument(Doc(3)); // val=2
            writer.Commit();
            Assert.AreEqual(1, writer.FlushDeletesCount);
            writer.UpdateBinaryDocValue(new Term("id", "doc-0"), "val", ToBytes(5));
            Assert.AreEqual(2, writer.FlushDeletesCount);
            writer.UpdateBinaryDocValue(new Term("id", "doc-1"), "val", ToBytes(6));
            Assert.AreEqual(3, writer.FlushDeletesCount);
            writer.UpdateBinaryDocValue(new Term("id", "doc-2"), "val", ToBytes(7));
            Assert.AreEqual(4, writer.FlushDeletesCount);
            writer.Config.SetRAMBufferSizeMB(1000d);
            writer.UpdateBinaryDocValue(new Term("id", "doc-2"), "val", ToBytes(7));
            Assert.AreEqual(4, writer.FlushDeletesCount);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSimple()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            // make sure random config doesn't flush on us
            conf.SetMaxBufferedDocs(10);
            conf.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
            IndexWriter writer = new IndexWriter(dir, conf);
            writer.AddDocument(Doc(0)); // val=1
            writer.AddDocument(Doc(1)); // val=2
            if (Random.NextBoolean()) // randomly commit before the update is sent
            {
                writer.Commit();
            }
            writer.UpdateBinaryDocValue(new Term("id", "doc-0"), "val", ToBytes(2)); // doc=0, exp=2

            DirectoryReader reader;
            if (Random.NextBoolean()) // not NRT
            {
                writer.Dispose();
                reader = DirectoryReader.Open(dir);
            } // NRT
            else
            {
                reader = DirectoryReader.Open(writer, true);
                writer.Dispose();
            }

            Assert.AreEqual(1, reader.Leaves.Count);
            AtomicReader r = (AtomicReader)reader.Leaves[0].Reader;
            BinaryDocValues bdv = r.GetBinaryDocValues("val");
            BytesRef scratch = new BytesRef();
            Assert.AreEqual(2, GetValue(bdv, 0, scratch));
            Assert.AreEqual(2, GetValue(bdv, 1, scratch));
            reader.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateFewSegments()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetMaxBufferedDocs(2); // generate few segments
            conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES); // prevent merges for this test
            IndexWriter writer = new IndexWriter(dir, conf);
            int numDocs = 10;
            long[] expectedValues = new long[numDocs];
            for (int i = 0; i < numDocs; i++)
            {
                writer.AddDocument(Doc(i));
                expectedValues[i] = i + 1;
            }
            writer.Commit();

            // update few docs
            for (int i = 0; i < numDocs; i++)
            {
                if (Random.NextDouble() < 0.4)
                {
                    long value = (i + 1) * 2;
                    writer.UpdateBinaryDocValue(new Term("id", "doc-" + i), "val", ToBytes(value));
                    expectedValues[i] = value;
                }
            }

            DirectoryReader reader;
            if (Random.NextBoolean()) // not NRT
            {
                writer.Dispose();
                reader = DirectoryReader.Open(dir);
            } // NRT
            else
            {
                reader = DirectoryReader.Open(writer, true);
                writer.Dispose();
            }

            BytesRef scratch = new BytesRef();
            foreach (AtomicReaderContext context in reader.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                BinaryDocValues bdv = r.GetBinaryDocValues("val");
                Assert.IsNotNull(bdv);
                for (int i = 0; i < r.MaxDoc; i++)
                {
                    long expected = expectedValues[i + context.DocBase];
                    long actual = GetValue(bdv, i, scratch);
                    Assert.AreEqual(expected, actual);
                }
            }

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestReopen()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);
            writer.AddDocument(Doc(0));
            writer.AddDocument(Doc(1));

            bool isNRT = Random.NextBoolean();
            DirectoryReader reader1;
            if (isNRT)
            {
                reader1 = DirectoryReader.Open(writer, true);
            }
            else
            {
                writer.Commit();
                reader1 = DirectoryReader.Open(dir);
            }

            // update doc
            writer.UpdateBinaryDocValue(new Term("id", "doc-0"), "val", ToBytes(10)); // update doc-0's value to 10
            if (!isNRT)
            {
                writer.Commit();
            }

            // reopen reader and assert only it sees the update
            DirectoryReader reader2 = DirectoryReader.OpenIfChanged(reader1);
            Assert.IsNotNull(reader2);
            Assert.IsTrue(reader1 != reader2);

            BytesRef scratch = new BytesRef();
            BinaryDocValues bdv1 = ((AtomicReader)reader1.Leaves[0].Reader).GetBinaryDocValues("val");
            BinaryDocValues bdv2 = ((AtomicReader)reader2.Leaves[0].Reader).GetBinaryDocValues("val");
            Assert.AreEqual(1, GetValue(bdv1, 0, scratch));
            Assert.AreEqual(10, GetValue(bdv2, 0, scratch));

            IOUtils.Dispose(writer, reader1, reader2, dir);
        }

        [Test]
        public virtual void TestUpdatesAndDeletes()
        {
            // create an index with a segment with only deletes, a segment with both
            // deletes and updates and a segment with only updates
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetMaxBufferedDocs(10); // control segment flushing
            conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES); // prevent merges for this test
            IndexWriter writer = new IndexWriter(dir, conf);

            for (int i = 0; i < 6; i++)
            {
                writer.AddDocument(Doc(i));
                if (i % 2 == 1)
                {
                    writer.Commit(); // create 2-docs segments
                }
            }

            // delete doc-1 and doc-2
            writer.DeleteDocuments(new Term("id", "doc-1"), new Term("id", "doc-2")); // 1st and 2nd segments

            // update docs 3 and 5
            writer.UpdateBinaryDocValue(new Term("id", "doc-3"), "val", ToBytes(17L));
            writer.UpdateBinaryDocValue(new Term("id", "doc-5"), "val", ToBytes(17L));

            DirectoryReader reader;
            if (Random.NextBoolean()) // not NRT
            {
                writer.Dispose();
                reader = DirectoryReader.Open(dir);
            } // NRT
            else
            {
                reader = DirectoryReader.Open(writer, true);
                writer.Dispose();
            }

            AtomicReader slow = SlowCompositeReaderWrapper.Wrap(reader);

            IBits liveDocs = slow.LiveDocs;
            bool[] expectedLiveDocs = new bool[] { true, false, false, true, true, true };
            for (int i = 0; i < expectedLiveDocs.Length; i++)
            {
                Assert.AreEqual(expectedLiveDocs[i], liveDocs.Get(i));
            }

            long[] expectedValues = new long[] { 1, 2, 3, 17, 5, 17 };
            BinaryDocValues bdv = slow.GetBinaryDocValues("val");
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.AreEqual(expectedValues[i], GetValue(bdv, i, scratch));
            }

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdatesWithDeletes()
        {
            // update and delete different documents in the same commit session
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetMaxBufferedDocs(10); // control segment flushing
            IndexWriter writer = new IndexWriter(dir, conf);

            writer.AddDocument(Doc(0));
            writer.AddDocument(Doc(1));

            if (Random.NextBoolean())
            {
                writer.Commit();
            }

            writer.DeleteDocuments(new Term("id", "doc-0"));
            writer.UpdateBinaryDocValue(new Term("id", "doc-1"), "val", ToBytes(17L));

            DirectoryReader reader;
            if (Random.NextBoolean()) // not NRT
            {
                writer.Dispose();
                reader = DirectoryReader.Open(dir);
            } // NRT
            else
            {
                reader = DirectoryReader.Open(writer, true);
                writer.Dispose();
            }

            AtomicReader r = (AtomicReader)reader.Leaves[0].Reader;
            Assert.IsFalse(r.LiveDocs.Get(0));
            Assert.AreEqual(17, GetValue(r.GetBinaryDocValues("val"), 1, new BytesRef()));

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateAndDeleteSameDocument()
        {
            // update and delete same document in same commit session
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetMaxBufferedDocs(10); // control segment flushing
            IndexWriter writer = new IndexWriter(dir, conf);

            writer.AddDocument(Doc(0));
            writer.AddDocument(Doc(1));

            if (Random.NextBoolean())
            {
                writer.Commit();
            }

            writer.DeleteDocuments(new Term("id", "doc-0"));
            writer.UpdateBinaryDocValue(new Term("id", "doc-0"), "val", ToBytes(17L));

            DirectoryReader reader;
            if (Random.NextBoolean()) // not NRT
            {
                writer.Dispose();
                reader = DirectoryReader.Open(dir);
            } // NRT
            else
            {
                reader = DirectoryReader.Open(writer, true);
                writer.Dispose();
            }

            AtomicReader r = (AtomicReader)reader.Leaves[0].Reader;
            Assert.IsFalse(r.LiveDocs.Get(0));
            Assert.AreEqual(1, GetValue(r.GetBinaryDocValues("val"), 0, new BytesRef())); // deletes are currently applied first

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestMultipleDocValuesTypes()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetMaxBufferedDocs(10); // prevent merges
            IndexWriter writer = new IndexWriter(dir, conf);

            for (int i = 0; i < 4; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("dvUpdateKey", "dv", Store.NO));
                doc.Add(new NumericDocValuesField("ndv", i));
                doc.Add(new BinaryDocValuesField("bdv", new BytesRef(Convert.ToString(i))));
                doc.Add(new SortedDocValuesField("sdv", new BytesRef(Convert.ToString(i))));
                doc.Add(new SortedSetDocValuesField("ssdv", new BytesRef(Convert.ToString(i))));
                doc.Add(new SortedSetDocValuesField("ssdv", new BytesRef(Convert.ToString(i * 2))));
                writer.AddDocument(doc);
            }
            writer.Commit();

            // update all docs' bdv field
            writer.UpdateBinaryDocValue(new Term("dvUpdateKey", "dv"), "bdv", ToBytes(17L));
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader r = (AtomicReader)reader.Leaves[0].Reader;
            NumericDocValues ndv = r.GetNumericDocValues("ndv");
            BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
            SortedDocValues sdv = r.GetSortedDocValues("sdv");
            SortedSetDocValues ssdv = r.GetSortedSetDocValues("ssdv");
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < r.MaxDoc; i++)
            {
                Assert.AreEqual(i, ndv.Get(i));
                Assert.AreEqual(17, GetValue(bdv, i, scratch));
                sdv.Get(i, scratch);
                Assert.AreEqual(new BytesRef(Convert.ToString(i)), scratch);
                ssdv.SetDocument(i);
                long ord = ssdv.NextOrd();
                ssdv.LookupOrd(ord, scratch);
                Assert.AreEqual(i, Convert.ToInt32(scratch.Utf8ToString()));
                if (i != 0)
                {
                    ord = ssdv.NextOrd();
                    ssdv.LookupOrd(ord, scratch);
                    Assert.AreEqual(i * 2, Convert.ToInt32(scratch.Utf8ToString()));
                }
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, ssdv.NextOrd());
            }

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestMultipleBinaryDocValues()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetMaxBufferedDocs(10); // prevent merges
            IndexWriter writer = new IndexWriter(dir, conf);

            for (int i = 0; i < 2; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("dvUpdateKey", "dv", Store.NO));
                doc.Add(new BinaryDocValuesField("bdv1", ToBytes(i)));
                doc.Add(new BinaryDocValuesField("bdv2", ToBytes(i)));
                writer.AddDocument(doc);
            }
            writer.Commit();

            // update all docs' bdv1 field
            writer.UpdateBinaryDocValue(new Term("dvUpdateKey", "dv"), "bdv1", ToBytes(17L));
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader r = (AtomicReader)reader.Leaves[0].Reader;

            BinaryDocValues bdv1 = r.GetBinaryDocValues("bdv1");
            BinaryDocValues bdv2 = r.GetBinaryDocValues("bdv2");
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < r.MaxDoc; i++)
            {
                Assert.AreEqual(17, GetValue(bdv1, i, scratch));
                Assert.AreEqual(i, GetValue(bdv2, i, scratch));
            }

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDocumentWithNoValue()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            for (int i = 0; i < 2; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("dvUpdateKey", "dv", Store.NO));
                if (i == 0) // index only one document with value
                {
                    doc.Add(new BinaryDocValuesField("bdv", ToBytes(5L)));
                }
                writer.AddDocument(doc);
            }
            writer.Commit();

            // update all docs' bdv field
            writer.UpdateBinaryDocValue(new Term("dvUpdateKey", "dv"), "bdv", ToBytes(17L));
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader r = (AtomicReader)reader.Leaves[0].Reader;
            BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < r.MaxDoc; i++)
            {
                Assert.AreEqual(17, GetValue(bdv, i, scratch));
            }

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestUnsetValue()
        {
            AssumeTrue("codec does not support docsWithField", DefaultCodecSupportsDocsWithField);
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            for (int i = 0; i < 2; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("id", "doc" + i, Store.NO));
                doc.Add(new BinaryDocValuesField("bdv", ToBytes(5L)));
                writer.AddDocument(doc);
            }
            writer.Commit();

            // unset the value of 'doc0'
            writer.UpdateBinaryDocValue(new Term("id", "doc0"), "bdv", null);
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader r = (AtomicReader)reader.Leaves[0].Reader;
            BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < r.MaxDoc; i++)
            {
                if (i == 0)
                {
                    bdv.Get(i, scratch);
                    Assert.AreEqual(0, scratch.Length);
                }
                else
                {
                    Assert.AreEqual(5, GetValue(bdv, i, scratch));
                }
            }

            IBits docsWithField = r.GetDocsWithField("bdv");
            Assert.IsFalse(docsWithField.Get(0));
            Assert.IsTrue(docsWithField.Get(1));

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestUnsetAllValues()
        {
            AssumeTrue("codec does not support docsWithField", DefaultCodecSupportsDocsWithField);
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            for (int i = 0; i < 2; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("id", "doc", Store.NO));
                doc.Add(new BinaryDocValuesField("bdv", ToBytes(5L)));
                writer.AddDocument(doc);
            }
            writer.Commit();

            // unset the value of 'doc'
            writer.UpdateBinaryDocValue(new Term("id", "doc"), "bdv", null);
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader r = (AtomicReader)reader.Leaves[0].Reader;
            BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < r.MaxDoc; i++)
            {
                bdv.Get(i, scratch);
                Assert.AreEqual(0, scratch.Length);
            }

            IBits docsWithField = r.GetDocsWithField("bdv");
            Assert.IsFalse(docsWithField.Get(0));
            Assert.IsFalse(docsWithField.Get(1));

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateNonBinaryDocValuesField()
        {
            // we don't support adding new fields or updating existing non-binary-dv
            // fields through binary updates
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            doc.Add(new StringField("key", "doc", Store.NO));
            doc.Add(new StringField("foo", "bar", Store.NO));
            writer.AddDocument(doc); // flushed document
            writer.Commit();
            writer.AddDocument(doc); // in-memory document

            try
            {
                writer.UpdateBinaryDocValue(new Term("key", "doc"), "bdv", ToBytes(17L));
                Assert.Fail("should not have allowed creating new fields through update");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // ok
            }

            try
            {
                writer.UpdateBinaryDocValue(new Term("key", "doc"), "foo", ToBytes(17L));
                Assert.Fail("should not have allowed updating an existing field to binary-dv");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // ok
            }

            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDifferentDVFormatPerField()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetCodec(new Lucene46CodecAnonymousClass(this));
            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            doc.Add(new StringField("key", "doc", Store.NO));
            doc.Add(new BinaryDocValuesField("bdv", ToBytes(5L)));
            doc.Add(new SortedDocValuesField("sorted", new BytesRef("value")));
            writer.AddDocument(doc); // flushed document
            writer.Commit();
            writer.AddDocument(doc); // in-memory document

            writer.UpdateBinaryDocValue(new Term("key", "doc"), "bdv", ToBytes(17L));
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);

            AtomicReader r = SlowCompositeReaderWrapper.Wrap(reader);
            BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
            SortedDocValues sdv = r.GetSortedDocValues("sorted");
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < r.MaxDoc; i++)
            {
                Assert.AreEqual(17, GetValue(bdv, i, scratch));
                sdv.Get(i, scratch);
                Assert.AreEqual(new BytesRef("value"), scratch);
            }

            reader.Dispose();
            dir.Dispose();
        }

        private sealed class Lucene46CodecAnonymousClass : Lucene46Codec
        {
            private readonly TestBinaryDocValuesUpdates outerInstance;

            public Lucene46CodecAnonymousClass(TestBinaryDocValuesUpdates outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                return new Lucene45DocValuesFormat();
            }
        }

        [Test]
        public virtual void TestUpdateSameDocMultipleTimes()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            doc.Add(new StringField("key", "doc", Store.NO));
            doc.Add(new BinaryDocValuesField("bdv", ToBytes(5L)));
            writer.AddDocument(doc); // flushed document
            writer.Commit();
            writer.AddDocument(doc); // in-memory document

            writer.UpdateBinaryDocValue(new Term("key", "doc"), "bdv", ToBytes(17L)); // update existing field
            writer.UpdateBinaryDocValue(new Term("key", "doc"), "bdv", ToBytes(3L)); // update existing field 2nd time in this commit
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader r = SlowCompositeReaderWrapper.Wrap(reader);
            BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < r.MaxDoc; i++)
            {
                Assert.AreEqual(3, GetValue(bdv, i, scratch));
            }
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSegmentMerges()
        {
            Directory dir = NewDirectory();
            Random random = Random;
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random));
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());

            int docid = 0;
            int numRounds = AtLeast(10);
            for (int rnd = 0; rnd < numRounds; rnd++)
            {
                Document doc = new Document();
                doc.Add(new StringField("key", "doc", Store.NO));
                doc.Add(new BinaryDocValuesField("bdv", ToBytes(-1)));
                int numDocs = AtLeast(30);
                for (int i = 0; i < numDocs; i++)
                {
                    doc.RemoveField("id");
                    doc.Add(new StringField("id", Convert.ToString(docid++), Store.NO));
                    writer.AddDocument(doc);
                }

                long value = rnd + 1;
                writer.UpdateBinaryDocValue(new Term("key", "doc"), "bdv", ToBytes(value));

                if (random.NextDouble() < 0.2) // randomly delete some docs
                {
                    writer.DeleteDocuments(new Term("id", Convert.ToString(random.Next(docid))));
                }

                // randomly commit or reopen-IW (or nothing), before forceMerge
                if (random.NextDouble() < 0.4)
                {
                    writer.Commit();
                }
                else if (random.NextDouble() < 0.1)
                {
                    writer.Dispose();
                    writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
                }

                // add another document with the current value, to be sure forceMerge has
                // something to merge (for instance, it could be that CMS finished merging
                // all segments down to 1 before the delete was applied, so when
                // forceMerge is called, the index will be with one segment and deletes
                // and some MPs might now merge it, thereby invalidating test's
                // assumption that the reader has no deletes).
                doc = new Document();
                doc.Add(new StringField("id", Convert.ToString(docid++), Store.NO));
                doc.Add(new StringField("key", "doc", Store.NO));
                doc.Add(new BinaryDocValuesField("bdv", ToBytes(value)));
                writer.AddDocument(doc);

                writer.ForceMerge(1, true);
                DirectoryReader reader;
                if (random.NextBoolean())
                {
                    writer.Commit();
                    reader = DirectoryReader.Open(dir);
                }
                else
                {
                    reader = DirectoryReader.Open(writer, true);
                }

                Assert.AreEqual(1, reader.Leaves.Count);
                AtomicReader r = (AtomicReader)reader.Leaves[0].Reader;
                Assert.IsNull(r.LiveDocs, "index should have no deletes after forceMerge");
                BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
                Assert.IsNotNull(bdv);
                BytesRef scratch = new BytesRef();
                for (int i = 0; i < r.MaxDoc; i++)
                {
                    Assert.AreEqual(value, GetValue(bdv, i, scratch));
                }
                reader.Dispose();
            }

            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateDocumentByMultipleTerms()
        {
            // make sure the order of updates is respected, even when multiple terms affect same document
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            doc.Add(new StringField("k1", "v1", Store.NO));
            doc.Add(new StringField("k2", "v2", Store.NO));
            doc.Add(new BinaryDocValuesField("bdv", ToBytes(5L)));
            writer.AddDocument(doc); // flushed document
            writer.Commit();
            writer.AddDocument(doc); // in-memory document

            writer.UpdateBinaryDocValue(new Term("k1", "v1"), "bdv", ToBytes(17L));
            writer.UpdateBinaryDocValue(new Term("k2", "v2"), "bdv", ToBytes(3L));
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader r = SlowCompositeReaderWrapper.Wrap(reader);
            BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < r.MaxDoc; i++)
            {
                Assert.AreEqual(3, GetValue(bdv, i, scratch));
            }
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestManyReopensAndFields()
        {
            Directory dir = NewDirectory();
            Random random = Random;
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random));
            LogMergePolicy lmp = NewLogMergePolicy();
            lmp.MergeFactor = 3; // merge often
            conf.SetMergePolicy(lmp);
            IndexWriter writer = new IndexWriter(dir, conf);

            bool isNRT = random.NextBoolean();
            DirectoryReader reader;
            if (isNRT)
            {
                reader = DirectoryReader.Open(writer, true);
            }
            else
            {
                writer.Commit();
                reader = DirectoryReader.Open(dir);
            }

            int numFields = random.Next(4) + 3; // 3-7
            long[] fieldValues = new long[numFields];
            bool[] fieldHasValue = new bool[numFields];
            Arrays.Fill(fieldHasValue, true);
            for (int i = 0; i < fieldValues.Length; i++)
            {
                fieldValues[i] = 1;
            }

            int numRounds = AtLeast(15);
            int docID = 0;
            for (int i = 0; i < numRounds; i++)
            {
                int numDocs = AtLeast(5);
                //      System.out.println("[" + Thread.currentThread().getName() + "]: round=" + i + ", numDocs=" + numDocs);
                for (int j = 0; j < numDocs; j++)
                {
                    Document doc = new Document();
                    doc.Add(new StringField("id", "doc-" + docID, Store.NO));
                    doc.Add(new StringField("key", "all", Store.NO)); // update key
                    // add all fields with their current value
                    for (int f = 0; f < fieldValues.Length; f++)
                    {
                        doc.Add(new BinaryDocValuesField("f" + f, ToBytes(fieldValues[f])));
                    }
                    writer.AddDocument(doc);
                    ++docID;
                }

                // if field's value was unset before, unset it from all new added documents too
                for (int field = 0; field < fieldHasValue.Length; field++)
                {
                    if (!fieldHasValue[field])
                    {
                        writer.UpdateBinaryDocValue(new Term("key", "all"), "f" + field, null);
                    }
                }

                int fieldIdx = random.Next(fieldValues.Length);
                string updateField = "f" + fieldIdx;
                if (random.NextBoolean())
                {
                    //        System.out.println("[" + Thread.currentThread().getName() + "]: unset field '" + updateField + "'");
                    fieldHasValue[fieldIdx] = false;
                    writer.UpdateBinaryDocValue(new Term("key", "all"), updateField, null);
                }
                else
                {
                    fieldHasValue[fieldIdx] = true;
                    writer.UpdateBinaryDocValue(new Term("key", "all"), updateField, ToBytes(++fieldValues[fieldIdx]));
                    //        System.out.println("[" + Thread.currentThread().getName() + "]: updated field '" + updateField + "' to value " + fieldValues[fieldIdx]);
                }

                if (random.NextDouble() < 0.2)
                {
                    int deleteDoc = random.Next(docID); // might also delete an already deleted document, ok!
                    writer.DeleteDocuments(new Term("id", "doc-" + deleteDoc));
                    //        System.out.println("[" + Thread.currentThread().getName() + "]: deleted document: doc-" + deleteDoc);
                }

                // verify reader
                if (!isNRT)
                {
                    writer.Commit();
                }

                //      System.out.println("[" + Thread.currentThread().getName() + "]: reopen reader: " + reader);
                DirectoryReader newReader = DirectoryReader.OpenIfChanged(reader);
                Assert.IsNotNull(newReader);
                reader.Dispose();
                reader = newReader;
                //      System.out.println("[" + Thread.currentThread().getName() + "]: reopened reader: " + reader);
                Assert.IsTrue(reader.NumDocs > 0); // we delete at most one document per round
                BytesRef scratch = new BytesRef();
                foreach (AtomicReaderContext context in reader.Leaves)
                {
                    AtomicReader r = context.AtomicReader;
                    //        System.out.println(((SegmentReader) r).getSegmentName());
                    IBits liveDocs = r.LiveDocs;
                    for (int field = 0; field < fieldValues.Length; field++)
                    {
                        string f = "f" + field;
                        BinaryDocValues bdv = r.GetBinaryDocValues(f);
                        IBits docsWithField = r.GetDocsWithField(f);
                        Assert.IsNotNull(bdv);
                        int maxDoc = r.MaxDoc;
                        for (int doc = 0; doc < maxDoc; doc++)
                        {
                            if (liveDocs is null || liveDocs.Get(doc))
                            {
                                //              System.out.println("doc=" + (doc + context.DocBase) + " f='" + f + "' vslue=" + getValue(bdv, doc, scratch));
                                if (fieldHasValue[field])
                                {
                                    Assert.IsTrue(docsWithField.Get(doc));
                                    Assert.AreEqual(fieldValues[field], GetValue(bdv, doc, scratch), "invalid value for doc=" + doc + ", field=" + f + ", reader=" + r);
                                }
                                else
                                {
                                    Assert.IsFalse(docsWithField.Get(doc));
                                }
                            }
                        }
                    }
                }
                //      System.out.println();
            }

            IOUtils.Dispose(writer, reader, dir);
        }

        [Test]
        public virtual void TestUpdateSegmentWithNoDocValues()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            // prevent merges, otherwise by the time updates are applied
            // (writer.Dispose()), the segments might have merged and that update becomes
            // legit.
            conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            IndexWriter writer = new IndexWriter(dir, conf);

            // first segment with BDV
            Document doc = new Document();
            doc.Add(new StringField("id", "doc0", Store.NO));
            doc.Add(new BinaryDocValuesField("bdv", ToBytes(3L)));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new StringField("id", "doc4", Store.NO)); // document without 'bdv' field
            writer.AddDocument(doc);
            writer.Commit();

            // second segment with no BDV
            doc = new Document();
            doc.Add(new StringField("id", "doc1", Store.NO));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new StringField("id", "doc2", Store.NO)); // document that isn't updated
            writer.AddDocument(doc);
            writer.Commit();

            // update document in the first segment - should not affect docsWithField of
            // the document without BDV field
            writer.UpdateBinaryDocValue(new Term("id", "doc0"), "bdv", ToBytes(5L));

            // update document in the second segment - field should be added and we should
            // be able to handle the other document correctly (e.g. no NPE)
            writer.UpdateBinaryDocValue(new Term("id", "doc1"), "bdv", ToBytes(5L));
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            BytesRef scratch = new BytesRef();
            foreach (AtomicReaderContext context in reader.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
                IBits docsWithField = r.GetDocsWithField("bdv");
                Assert.IsNotNull(docsWithField);
                Assert.IsTrue(docsWithField.Get(0));
                Assert.AreEqual(5L, GetValue(bdv, 0, scratch));
                Assert.IsFalse(docsWithField.Get(1));
                bdv.Get(1, scratch);
                Assert.AreEqual(0, scratch.Length);
            }
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateSegmentWithPostingButNoDocValues()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            // prevent merges, otherwise by the time updates are applied
            // (writer.Dispose()), the segments might have merged and that update becomes
            // legit.
            conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            IndexWriter writer = new IndexWriter(dir, conf);

            // first segment with BDV
            Document doc = new Document();
            doc.Add(new StringField("id", "doc0", Store.NO));
            doc.Add(new StringField("bdv", "mock-value", Store.NO));
            doc.Add(new BinaryDocValuesField("bdv", ToBytes(5L)));
            writer.AddDocument(doc);
            writer.Commit();

            // second segment with no BDV
            doc = new Document();
            doc.Add(new StringField("id", "doc1", Store.NO));
            doc.Add(new StringField("bdv", "mock-value", Store.NO));
            writer.AddDocument(doc);
            writer.Commit();

            // update document in the second segment
            writer.UpdateBinaryDocValue(new Term("id", "doc1"), "bdv", ToBytes(5L));
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            BytesRef scratch = new BytesRef();
            foreach (AtomicReaderContext context in reader.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
                for (int i = 0; i < r.MaxDoc; i++)
                {
                    Assert.AreEqual(5L, GetValue(bdv, i, scratch));
                }
            }
            reader.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateBinaryDVFieldWithSameNameAsPostingField()
        {
            // this used to fail because FieldInfos.Builder neglected to update
            // globalFieldMaps.docValueTypes map
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            doc.Add(new StringField("f", "mock-value", Store.NO));
            doc.Add(new BinaryDocValuesField("f", ToBytes(5L)));
            writer.AddDocument(doc);
            writer.Commit();
            writer.UpdateBinaryDocValue(new Term("f", "mock-value"), "f", ToBytes(17L));
            writer.Dispose();

            DirectoryReader r = DirectoryReader.Open(dir);
            BinaryDocValues bdv = ((AtomicReader)r.Leaves[0].Reader).GetBinaryDocValues("f");
            Assert.AreEqual(17, GetValue(bdv, 0, new BytesRef()));
            r.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateOldSegments()
        {
            Codec[] oldCodecs = new Codec[] {
                new Lucene40RWCodec(),
                new Lucene41RWCodec(),
                new Lucene42RWCodec(),
                new Lucene45RWCodec()
            };
            Directory dir = NewDirectory();

            // create a segment with an old Codec
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetCodec(oldCodecs[Random.Next(oldCodecs.Length)]);
            OldFormatImpersonationIsActive = true;
            IndexWriter writer = new IndexWriter(dir, conf);
            Document doc = new Document();
            doc.Add(new StringField("id", "doc", Store.NO));
            doc.Add(new BinaryDocValuesField("f", ToBytes(5L)));
            writer.AddDocument(doc);
            writer.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public virtual void TestUpdateOldSegments_OldFormatNotActive()
        {
            bool oldValue = OldFormatImpersonationIsActive;

            OldFormatImpersonationIsActive = false;

            Codec[] oldCodecs = new Codec[] {
                new Lucene40RWCodec(),
                new Lucene41RWCodec(),
                new Lucene42RWCodec(),
                new Lucene45RWCodec()
            };

            Directory dir = NewDirectory();
            Document doc = new Document();
            doc.Add(new StringField("id", "doc", Store.NO));
            doc.Add(new BinaryDocValuesField("f", ToBytes(5L)));

            var conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetCodec(oldCodecs[Random.Next(oldCodecs.Length)]);

            var writer = new IndexWriter(dir, conf);
            writer.AddDocument(doc);
            writer.UpdateBinaryDocValue(new Term("id", "doc"), "f", ToBytes(4L));

            try
            {
                writer.Dispose();
                Assert.Fail("should not have succeeded to update a segment written with an old Codec");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                writer.Rollback();
            }
            finally
            {
                OldFormatImpersonationIsActive = oldValue;
            }

            dir.Dispose();
        }

        [Test]
        public virtual void TestStressMultiThreading()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            // create index
            int numThreads = TestUtil.NextInt32(Random, 3, 6);
            int numDocs = AtLeast(2000);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("id", "doc" + i, Store.NO));
                double group = Random.NextDouble();
                string g;
                if (group < 0.1)
                {
                    g = "g0";
                }
                else if (group < 0.5)
                {
                    g = "g1";
                }
                else if (group < 0.8)
                {
                    g = "g2";
                }
                else
                {
                    g = "g3";
                }
                doc.Add(new StringField("updKey", g, Store.NO));
                for (int j = 0; j < numThreads; j++)
                {
                    long value = Random.Next();
                    doc.Add(new BinaryDocValuesField("f" + j, ToBytes(value)));
                    doc.Add(new BinaryDocValuesField("cf" + j, ToBytes(value * 2))); // control, always updated to f * 2
                }
                writer.AddDocument(doc);
            }

            CountdownEvent done = new CountdownEvent(numThreads);
            AtomicInt32 numUpdates = new AtomicInt32(AtLeast(100));

            // same thread updates a field as well as reopens
            ThreadJob[] threads = new ThreadJob[numThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                string f = "f" + i;
                string cf = "cf" + i;
                threads[i] = new ThreadAnonymousClass(this, "UpdateThread-" + i, writer, numDocs, done, numUpdates, f, cf);
            }

            foreach (ThreadJob t in threads)
            {
                t.Start();
            }
            done.Wait();
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            BytesRef scratch = new BytesRef();
            foreach (AtomicReaderContext context in reader.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                for (int i = 0; i < numThreads; i++)
                {
                    BinaryDocValues bdv = r.GetBinaryDocValues("f" + i);
                    BinaryDocValues control = r.GetBinaryDocValues("cf" + i);
                    IBits docsWithBdv = r.GetDocsWithField("f" + i);
                    IBits docsWithControl = r.GetDocsWithField("cf" + i);
                    IBits liveDocs = r.LiveDocs;
                    for (int j = 0; j < r.MaxDoc; j++)
                    {
                        if (liveDocs is null || liveDocs.Get(j))
                        {
                            Assert.AreEqual(docsWithBdv.Get(j), docsWithControl.Get(j));
                            if (docsWithBdv.Get(j))
                            {
                                Assert.AreEqual(GetValue(control, j, scratch), GetValue(bdv, j, scratch) * 2);
                            }
                        }
                    }
                }
            }
            reader.Dispose();

            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestBinaryDocValuesUpdates outerInstance;

            private readonly IndexWriter writer;
            private readonly int numDocs;
            private readonly CountdownEvent done;
            private readonly AtomicInt32 numUpdates;
            private readonly string f;
            private readonly string cf;

            public ThreadAnonymousClass(TestBinaryDocValuesUpdates outerInstance, string str, IndexWriter writer, int numDocs, CountdownEvent done, AtomicInt32 numUpdates, string f, string cf)
                : base(str)
            {
                this.outerInstance = outerInstance;
                this.writer = writer;
                this.numDocs = numDocs;
                this.done = done;
                this.numUpdates = numUpdates;
                this.f = f;
                this.cf = cf;
            }

            public override void Run()
            {
                DirectoryReader reader = null;
                bool success = false;
                try
                {
                    Random random = Random;
                    while (numUpdates.GetAndDecrement() > 0)
                    {
                        double group = random.NextDouble();
                        Term t;
                        if (group < 0.1)
                        {
                            t = new Term("updKey", "g0");
                        }
                        else if (group < 0.5)
                        {
                            t = new Term("updKey", "g1");
                        }
                        else if (group < 0.8)
                        {
                            t = new Term("updKey", "g2");
                        }
                        else
                        {
                            t = new Term("updKey", "g3");
                        }
                        //              System.out.println("[" + Thread.currentThread().getName() + "] numUpdates=" + numUpdates + " updateTerm=" + t);
                        if (random.NextBoolean()) // sometimes unset a value
                        {
                            writer.UpdateBinaryDocValue(t, f, null);
                            writer.UpdateBinaryDocValue(t, cf, null);
                        }
                        else
                        {
                            long updValue = random.Next();
                            writer.UpdateBinaryDocValue(t, f, ToBytes(updValue));
                            writer.UpdateBinaryDocValue(t, cf, ToBytes(updValue * 2));
                        }

                        if (random.NextDouble() < 0.2)
                        {
                            // delete a random document
                            int doc = random.Next(numDocs);
                            //                System.out.println("[" + Thread.currentThread().getName() + "] deleteDoc=doc" + doc);
                            writer.DeleteDocuments(new Term("id", "doc" + doc));
                        }

                        if (random.NextDouble() < 0.05) // commit every 20 updates on average
                        {
                            //                  System.out.println("[" + Thread.currentThread().getName() + "] commit");
                            writer.Commit();
                        }

                        if (random.NextDouble() < 0.1) // reopen NRT reader (apply updates), on average once every 10 updates
                        {
                            if (reader is null)
                            {
                                //                  System.out.println("[" + Thread.currentThread().getName() + "] open NRT");
                                reader = DirectoryReader.Open(writer, true);
                            }
                            else
                            {
                                //                  System.out.println("[" + Thread.currentThread().getName() + "] reopen NRT");
                                DirectoryReader r2 = DirectoryReader.OpenIfChanged(reader, writer, true);
                                if (r2 != null)
                                {
                                    reader.Dispose();
                                    reader = r2;
                                }
                            }
                        }
                    }
                    //            System.out.println("[" + Thread.currentThread().getName() + "] DONE");
                    success = true;
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
                finally
                {
                    if (reader != null)
                    {
                        try
                        {
                            reader.Dispose();
                        }
                        catch (Exception e) when (e.IsIOException())
                        {
                            if (success) // suppress this exception only if there was another exception
                            {
                                throw RuntimeException.Create(e);
                            }
                        }
                    }
                    done.Signal();
                }
            }
        }

        [Test]
        public virtual void TestUpdateDifferentDocsInDifferentGens()
        {
            // update same document multiple times across generations
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetMaxBufferedDocs(4);
            IndexWriter writer = new IndexWriter(dir, conf);
            int numDocs = AtLeast(10);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("id", "doc" + i, Store.NO));
                long value = Random.Next();
                doc.Add(new BinaryDocValuesField("f", ToBytes(value)));
                doc.Add(new BinaryDocValuesField("cf", ToBytes(value * 2)));
                writer.AddDocument(doc);
            }

            int numGens = AtLeast(5);
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < numGens; i++)
            {
                int doc = Random.Next(numDocs);
                Term t = new Term("id", "doc" + doc);
                long value = Random.NextInt64();
                writer.UpdateBinaryDocValue(t, "f", ToBytes(value));
                writer.UpdateBinaryDocValue(t, "cf", ToBytes(value * 2));
                DirectoryReader reader = DirectoryReader.Open(writer, true);
                foreach (AtomicReaderContext context in reader.Leaves)
                {
                    AtomicReader r = context.AtomicReader;
                    BinaryDocValues fbdv = r.GetBinaryDocValues("f");
                    BinaryDocValues cfbdv = r.GetBinaryDocValues("cf");
                    for (int j = 0; j < r.MaxDoc; j++)
                    {
                        Assert.AreEqual(GetValue(cfbdv, j, scratch), GetValue(fbdv, j, scratch) * 2);
                    }
                }
                reader.Dispose();
            }
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestChangeCodec()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES); // disable merges to simplify test assertions.
            conf.SetCodec(new Lucene46CodecAnonymousClass2(this));
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            Document doc = new Document();
            doc.Add(new StringField("id", "d0", Store.NO));
            doc.Add(new BinaryDocValuesField("f1", ToBytes(5L)));
            doc.Add(new BinaryDocValuesField("f2", ToBytes(13L)));
            writer.AddDocument(doc);
            writer.Dispose();

            // change format
            conf.SetCodec(new Lucene46CodecAnonymousClass3(this));
            writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            doc = new Document();
            doc.Add(new StringField("id", "d1", Store.NO));
            doc.Add(new BinaryDocValuesField("f1", ToBytes(17L)));
            doc.Add(new BinaryDocValuesField("f2", ToBytes(2L)));
            writer.AddDocument(doc);
            writer.UpdateBinaryDocValue(new Term("id", "d0"), "f1", ToBytes(12L));
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader r = SlowCompositeReaderWrapper.Wrap(reader);
            BinaryDocValues f1 = r.GetBinaryDocValues("f1");
            BinaryDocValues f2 = r.GetBinaryDocValues("f2");
            BytesRef scratch = new BytesRef();
            Assert.AreEqual(12L, GetValue(f1, 0, scratch));
            Assert.AreEqual(13L, GetValue(f2, 0, scratch));
            Assert.AreEqual(17L, GetValue(f1, 1, scratch));
            Assert.AreEqual(2L, GetValue(f2, 1, scratch));
            reader.Dispose();
            dir.Dispose();
        }

        private sealed class Lucene46CodecAnonymousClass2 : Lucene46Codec
        {
            private readonly TestBinaryDocValuesUpdates outerInstance;

            public Lucene46CodecAnonymousClass2(TestBinaryDocValuesUpdates outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                return new Lucene45DocValuesFormat();
            }
        }

        private sealed class Lucene46CodecAnonymousClass3 : Lucene46Codec
        {
            private readonly TestBinaryDocValuesUpdates outerInstance;

            public Lucene46CodecAnonymousClass3(TestBinaryDocValuesUpdates outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                return new AssertingDocValuesFormat();
            }
        }

        [Test]
        public virtual void TestAddIndexes()
        {
            Directory dir1 = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir1, conf);

            int numDocs = AtLeast(50);
            int numTerms = TestUtil.NextInt32(Random, 1, numDocs / 5);
            ISet<string> randomTerms = new JCG.HashSet<string>();
            while (randomTerms.Count < numTerms)
            {
                randomTerms.Add(TestUtil.RandomSimpleString(Random));
            }

            // create first index
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("id", RandomPicks.RandomFrom(Random, randomTerms), Store.NO));
                doc.Add(new BinaryDocValuesField("bdv", ToBytes(4L)));
                doc.Add(new BinaryDocValuesField("control", ToBytes(8L)));
                writer.AddDocument(doc);
            }

            if (Random.NextBoolean())
            {
                writer.Commit();
            }

            // update some docs to a random value
            long value = Random.Next();
            Term term = new Term("id", RandomPicks.RandomFrom(Random, randomTerms));
            writer.UpdateBinaryDocValue(term, "bdv", ToBytes(value));
            writer.UpdateBinaryDocValue(term, "control", ToBytes(value * 2));
            writer.Dispose();

            Directory dir2 = NewDirectory();
            conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            writer = new IndexWriter(dir2, conf);
            if (Random.NextBoolean())
            {
                writer.AddIndexes(dir1);
            }
            else
            {
                DirectoryReader reader = DirectoryReader.Open(dir1);
                writer.AddIndexes(reader);
                reader.Dispose();
            }
            writer.Dispose();

            DirectoryReader reader_ = DirectoryReader.Open(dir2);
            BytesRef scratch = new BytesRef();
            foreach (AtomicReaderContext context in reader_.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
                BinaryDocValues control = r.GetBinaryDocValues("control");
                for (int i = 0; i < r.MaxDoc; i++)
                {
                    Assert.AreEqual(GetValue(bdv, i, scratch) * 2, GetValue(control, i, scratch));
                }
            }
            reader_.Dispose();

            IOUtils.Dispose(dir1, dir2);
        }

        [Test]
        public virtual void TestDeleteUnusedUpdatesFiles()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            doc.Add(new StringField("id", "d0", Store.NO));
            doc.Add(new BinaryDocValuesField("f", ToBytes(1L)));
            writer.AddDocument(doc);

            // create first gen of update files
            writer.UpdateBinaryDocValue(new Term("id", "d0"), "f", ToBytes(2L));
            writer.Commit();
            int numFiles = dir.ListAll().Length;

            DirectoryReader r = DirectoryReader.Open(dir);
            BytesRef scratch = new BytesRef();
            Assert.AreEqual(2L, GetValue(((AtomicReader)r.Leaves[0].Reader).GetBinaryDocValues("f"), 0, scratch));
            r.Dispose();

            // create second gen of update files, first gen should be deleted
            writer.UpdateBinaryDocValue(new Term("id", "d0"), "f", ToBytes(5L));
            writer.Commit();
            Assert.AreEqual(numFiles, dir.ListAll().Length);

            r = DirectoryReader.Open(dir);
            Assert.AreEqual(5L, GetValue(((AtomicReader)r.Leaves[0].Reader).GetBinaryDocValues("f"), 0, scratch));
            r.Dispose();

            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        [Slow]
        public virtual void TestTonsOfUpdates()
        {
            // LUCENE-5248: make sure that when there are many updates, we don't use too much RAM
            Directory dir = NewDirectory();
            Random random = Random;
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random));
            conf.SetRAMBufferSizeMB(IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB);
            conf.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH); // don't flush by doc
            IndexWriter writer = new IndexWriter(dir, conf);

            // test data: lots of documents (few 10Ks) and lots of update terms (few hundreds)
            int numDocs = AtLeast(20000);
            int numBinaryFields = AtLeast(5);
            int numTerms = TestUtil.NextInt32(random, 10, 100); // terms should affect many docs
            ISet<string> updateTerms = new JCG.HashSet<string>();
            while (updateTerms.Count < numTerms)
            {
                updateTerms.Add(TestUtil.RandomSimpleString(random));
            }

            //    System.out.println("numDocs=" + numDocs + " numBinaryFields=" + numBinaryFields + " numTerms=" + numTerms);

            // build a large index with many BDV fields and update terms
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                int numUpdateTerms = TestUtil.NextInt32(random, 1, numTerms / 10);
                for (int j = 0; j < numUpdateTerms; j++)
                {
                    doc.Add(new StringField("upd", RandomPicks.RandomFrom(random, updateTerms), Store.NO));
                }
                for (int j = 0; j < numBinaryFields; j++)
                {
                    long val = random.Next();
                    doc.Add(new BinaryDocValuesField("f" + j, ToBytes(val)));
                    doc.Add(new BinaryDocValuesField("cf" + j, ToBytes(val * 2)));
                }
                writer.AddDocument(doc);
            }

            writer.Commit(); // commit so there's something to apply to

            // set to flush every 2048 bytes (approximately every 12 updates), so we get
            // many flushes during binary updates
            writer.Config.SetRAMBufferSizeMB(2048.0 / 1024 / 1024);
            int numUpdates = AtLeast(100);
            //    System.out.println("numUpdates=" + numUpdates);
            for (int i = 0; i < numUpdates; i++)
            {
                int field = random.Next(numBinaryFields);
                Term updateTerm = new Term("upd", RandomPicks.RandomFrom(random, updateTerms));
                long value = random.Next();
                writer.UpdateBinaryDocValue(updateTerm, "f" + field, ToBytes(value));
                writer.UpdateBinaryDocValue(updateTerm, "cf" + field, ToBytes(value * 2));
            }

            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            BytesRef scratch = new BytesRef();
            foreach (AtomicReaderContext context in reader.Leaves)
            {
                for (int i = 0; i < numBinaryFields; i++)
                {
                    AtomicReader r = context.AtomicReader;
                    BinaryDocValues f = r.GetBinaryDocValues("f" + i);
                    BinaryDocValues cf = r.GetBinaryDocValues("cf" + i);
                    for (int j = 0; j < r.MaxDoc; j++)
                    {
                        Assert.AreEqual(GetValue(cf, j, scratch), GetValue(f, j, scratch) * 2, "reader=" + r + ", field=f" + i + ", doc=" + j);
                    }
                }
            }
            reader.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdatesOrder()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            doc.Add(new StringField("upd", "t1", Store.NO));
            doc.Add(new StringField("upd", "t2", Store.NO));
            doc.Add(new BinaryDocValuesField("f1", ToBytes(1L)));
            doc.Add(new BinaryDocValuesField("f2", ToBytes(1L)));
            writer.AddDocument(doc);
            writer.UpdateBinaryDocValue(new Term("upd", "t1"), "f1", ToBytes(2L)); // update f1 to 2
            writer.UpdateBinaryDocValue(new Term("upd", "t1"), "f2", ToBytes(2L)); // update f2 to 2
            writer.UpdateBinaryDocValue(new Term("upd", "t2"), "f1", ToBytes(3L)); // update f1 to 3
            writer.UpdateBinaryDocValue(new Term("upd", "t2"), "f2", ToBytes(3L)); // update f2 to 3
            writer.UpdateBinaryDocValue(new Term("upd", "t1"), "f1", ToBytes(4L)); // update f1 to 4 (but not f2)
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            BytesRef scratch = new BytesRef();
            Assert.AreEqual(4, GetValue(((AtomicReader)reader.Leaves[0].Reader).GetBinaryDocValues("f1"), 0, scratch));
            Assert.AreEqual(3, GetValue(((AtomicReader)reader.Leaves[0].Reader).GetBinaryDocValues("f2"), 0, scratch));
            reader.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateAllDeletedSegment()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            doc.Add(new StringField("id", "doc", Store.NO));
            doc.Add(new BinaryDocValuesField("f1", ToBytes(1L)));
            writer.AddDocument(doc);
            writer.AddDocument(doc);
            writer.Commit();
            writer.DeleteDocuments(new Term("id", "doc")); // delete all docs in the first segment
            writer.AddDocument(doc);
            writer.UpdateBinaryDocValue(new Term("id", "doc"), "f1", ToBytes(2L));
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(1, reader.Leaves.Count);
            Assert.AreEqual(2L, GetValue(((AtomicReader)reader.Leaves[0].Reader).GetBinaryDocValues("f1"), 0, new BytesRef()));
            reader.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateTwoNonexistingTerms()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            doc.Add(new StringField("id", "doc", Store.NO));
            doc.Add(new BinaryDocValuesField("f1", ToBytes(1L)));
            writer.AddDocument(doc);
            // update w/ multiple nonexisting terms in same field
            writer.UpdateBinaryDocValue(new Term("c", "foo"), "f1", ToBytes(2L));
            writer.UpdateBinaryDocValue(new Term("c", "bar"), "f1", ToBytes(2L));
            writer.Dispose();

            DirectoryReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(1, reader.Leaves.Count);
            Assert.AreEqual(1L, GetValue(((AtomicReader)reader.Leaves[0].Reader).GetBinaryDocValues("f1"), 0, new BytesRef()));
            reader.Dispose();

            dir.Dispose();
        }

        /// <summary>
        /// Using the exact translation from Lucene, we had an issue where loss of float precision was causing
        /// index corruption when using binary doc values in combination with a 32 bit application to write the index. 
        /// Consequently, a 64 bit application could not read an index generated by a 32 bit application and a 
        /// 32 bit application could not read an index written by Java Lucene (regardless of bitness).
        /// <para/>
        /// This test is to verify that the current test environment (be it 32 or 64 bit) can read an index with
        /// binary doc values that was written by Lucene in Java.
        /// <para/>
        /// To be thorough, we should setup the testing to run both in 32 bit and 64 bit on each platform. It would
        /// be better if we could somehow generate the file under one bitness and consume it under the other, but
        /// that would be difficult to set up. So, we are just reading a known good index and if we cannot read it
        /// we know there is a problem. If the reading is fixed and the writing is different, it will fail several tests anyway.
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadIndexBitness()
        {
            string[] alphabet = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
            int iterations = 500;
            FacetsConfig facetConfig = GetFacetConfig();

            // Unzip index
            DirectoryInfo indexDir = CreateTempDir("index");
            using (Stream zipFileStream = this.GetType().FindAndGetManifestResourceStream("index.48.w-binary-doc-values.zip"))
            {
                TestUtil.Unzip(zipFileStream, indexDir);
            }

            // Unzip taxonomy
            DirectoryInfo taxoDir = CreateTempDir("taxo");
            using (Stream zipFileStream = this.GetType().FindAndGetManifestResourceStream("taxo.48.w-binary-doc-values.zip"))
            {
                TestUtil.Unzip(zipFileStream, taxoDir);
            }


            SimpleFSDirectory nFsd = new SimpleFSDirectory(indexDir);
            IndexReader indexReader = DirectoryReader.Open(nFsd);
            IndexSearcher searcher = new IndexSearcher(indexReader);

            DirectoryTaxonomyReader taxoReader =
                new DirectoryTaxonomyReader(FSDirectory.Open(taxoDir));

            try
            {
                foreach (string letter in alphabet)
                {
                    FacetsCollector c = new FacetsCollector();
                    searcher.Search(new TermQuery(new Term("Field1", letter)), c);

                    Facets facets = new FastTaxonomyFacetCounts(taxoReader, facetConfig, c);
                    FacetResult result = facets.GetTopChildren(int.MaxValue, "facetField1");

                    assertEquals(iterations, System.Convert.ToInt32(result.LabelValues[0].Value));

                    FacetResult result2 = facets.GetTopChildren(int.MaxValue, "facetField2");

                    
                    for (int i = 0; i < iterations; i++)
                    {
                        assertEquals(i, System.Convert.ToInt32(result2.LabelValues[i].Label));
                    }
                }

                Console.WriteLine("Success");
            }
            finally
            {
                IOUtils.Dispose(indexReader, taxoReader, nFsd);
            }
        }

#endif

        private FacetsConfig GetFacetConfig()
        {
            FacetsConfig facetConfig = new FacetsConfig();
            FacetsConfig config = new FacetsConfig();

            config.SetRequireDimCount("facetField1", true);
            config.SetRequireDimCount("facetField2", true);
            return facetConfig;
        }
    }
}