using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using NUnit.Framework;
using System;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;

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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BinaryDocValuesField = BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IBits = Lucene.Net.Util.IBits;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NumericDocValuesField = NumericDocValuesField;
    using SortedDocValuesField = SortedDocValuesField;
    using SortedSetDocValuesField = SortedSetDocValuesField;
    using StringField = StringField;
    using TextField = TextField;

    ///
    /// <summary>
    /// Tests DocValues integration into IndexWriter
    ///
    /// </summary>
    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestDocValuesIndexing : LuceneTestCase
    {
        /*
         * - add test for multi segment case with deletes
         * - add multithreaded tests / integrate into stress indexing?
         */

        [Test]
        public virtual void TestAddIndexes()
        {
            Directory d1 = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d1);
            Document doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            doc.Add(new NumericDocValuesField("dv", 1));
            w.AddDocument(doc);
            IndexReader r1 = w.GetReader();
            w.Dispose();

            Directory d2 = NewDirectory();
            w = new RandomIndexWriter(Random, d2);
            doc = new Document();
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            doc.Add(new NumericDocValuesField("dv", 2));
            w.AddDocument(doc);
            IndexReader r2 = w.GetReader();
            w.Dispose();

            Directory d3 = NewDirectory();
            w = new RandomIndexWriter(Random, d3);
            w.AddIndexes(SlowCompositeReaderWrapper.Wrap(r1), SlowCompositeReaderWrapper.Wrap(r2));
            r1.Dispose();
            d1.Dispose();
            r2.Dispose();
            d2.Dispose();

            w.ForceMerge(1);
            DirectoryReader r3 = w.GetReader();
            w.Dispose();
            AtomicReader sr = GetOnlySegmentReader(r3);
            Assert.AreEqual(2, sr.NumDocs);
            NumericDocValues docValues = sr.GetNumericDocValues("dv");
            Assert.IsNotNull(docValues);
            r3.Dispose();
            d3.Dispose();
        }

        [Test]
        public virtual void TestMultiValuedDocValuesField()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            Field f = new NumericDocValuesField("field", 17);
            // Index doc values are single-valued so we should not
            // be able to add same field more than once:
            doc.Add(f);
            doc.Add(f);
            try
            {
                w.AddDocument(doc);
                Assert.Fail("didn't hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }

            doc = new Document();
            doc.Add(f);
            w.AddDocument(doc);
            w.ForceMerge(1);
            DirectoryReader r = w.GetReader();
            w.Dispose();
            Assert.AreEqual(17, FieldCache.DEFAULT.GetInt32s(GetOnlySegmentReader(r), "field", false).Get(0));
            r.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestDifferentTypedDocValuesField()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            // Index doc values are single-valued so we should not
            // be able to add same field more than once:
            Field f;
            doc.Add(f = new NumericDocValuesField("field", 17));
            doc.Add(new BinaryDocValuesField("field", new BytesRef("blah")));
            try
            {
                w.AddDocument(doc);
                Assert.Fail("didn't hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }

            doc = new Document();
            doc.Add(f);
            w.AddDocument(doc);
            w.ForceMerge(1);
            DirectoryReader r = w.GetReader();
            w.Dispose();
            Assert.AreEqual(17, FieldCache.DEFAULT.GetInt32s(GetOnlySegmentReader(r), "field", false).Get(0));
            r.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestDifferentTypedDocValuesField2()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            // Index doc values are single-valued so we should not
            // be able to add same field more than once:
            Field f = new NumericDocValuesField("field", 17);
            doc.Add(f);
            doc.Add(new SortedDocValuesField("field", new BytesRef("hello")));
            try
            {
                w.AddDocument(doc);
                Assert.Fail("didn't hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            doc = new Document();
            doc.Add(f);
            w.AddDocument(doc);
            w.ForceMerge(1);
            DirectoryReader r = w.GetReader();
            Assert.AreEqual(17, GetOnlySegmentReader(r).GetNumericDocValues("field").Get(0));
            r.Dispose();
            w.Dispose();
            d.Dispose();
        }

        // LUCENE-3870
        [Test]
        public virtual void TestLengthPrefixAcrossTwoPages()
        {
            Directory d = NewDirectory();
            IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            var bytes = new byte[32764];
            BytesRef b = new BytesRef();
            b.Bytes = bytes;
            b.Length = bytes.Length;
            doc.Add(new SortedDocValuesField("field", b));
            w.AddDocument(doc);
            bytes[0] = 1;
            w.AddDocument(doc);
            w.ForceMerge(1);
            DirectoryReader r = w.GetReader();
            BinaryDocValues s = FieldCache.DEFAULT.GetTerms(GetOnlySegmentReader(r), "field", false);

            BytesRef bytes1 = new BytesRef();
            s.Get(0, bytes1);
            Assert.AreEqual(bytes.Length, bytes1.Length);
            bytes[0] = 0;
            Assert.AreEqual(b, bytes1);

            s.Get(1, bytes1);
            Assert.AreEqual(bytes.Length, bytes1.Length);
            bytes[0] = 1;
            Assert.AreEqual(b, bytes1);
            r.Dispose();
            w.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestDocValuesUnstored()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwconfig.SetMergePolicy(NewLogMergePolicy());
            IndexWriter writer = new IndexWriter(dir, iwconfig);
            for (int i = 0; i < 50; i++)
            {
                Document doc = new Document();
                doc.Add(new NumericDocValuesField("dv", i));
                doc.Add(new TextField("docId", "" + i, Field.Store.YES));
                writer.AddDocument(doc);
            }
            DirectoryReader r = writer.GetReader();
            AtomicReader slow = SlowCompositeReaderWrapper.Wrap(r);
            FieldInfos fi = slow.FieldInfos;
            FieldInfo dvInfo = fi.FieldInfo("dv");
            Assert.IsTrue(dvInfo.HasDocValues);
            NumericDocValues dv = slow.GetNumericDocValues("dv");
            for (int i = 0; i < 50; i++)
            {
                Assert.AreEqual(i, dv.Get(i));
                Document d = slow.Document(i);
                // cannot use d.Get("dv") due to another bug!
                Assert.IsNull(d.GetField("dv"));
                Assert.AreEqual(Convert.ToString(i), d.Get("docId"));
            }
            slow.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        // Same field in one document as different types:
        [Test]
        public virtual void TestMixedTypesSameDocument()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("foo", 0));
            doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
            try
            {
                w.AddDocument(doc);
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            w.Dispose();
            dir.Dispose();
        }

        // Two documents with same field as different types:
        [Test]
        public virtual void TestMixedTypesDifferentDocuments()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("foo", 0));
            w.AddDocument(doc);

            doc = new Document();
            doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
            try
            {
                w.AddDocument(doc);
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestAddSortedTwice()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            Directory directory = NewDirectory();
            // we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwc.SetMergePolicy(NewLogMergePolicy());
            IndexWriter iwriter = new IndexWriter(directory, iwc);
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo!")));
            doc.Add(new SortedDocValuesField("dv", new BytesRef("bar!")));
            try
            {
                iwriter.AddDocument(doc);
                Assert.Fail("didn't hit expected exception");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }

            iwriter.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestAddBinaryTwice()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            Directory directory = NewDirectory();
            // we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwc.SetMergePolicy(NewLogMergePolicy());
            IndexWriter iwriter = new IndexWriter(directory, iwc);
            Document doc = new Document();
            doc.Add(new BinaryDocValuesField("dv", new BytesRef("foo!")));
            doc.Add(new BinaryDocValuesField("dv", new BytesRef("bar!")));
            try
            {
                iwriter.AddDocument(doc);
                Assert.Fail("didn't hit expected exception");
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                // expected
            }

            iwriter.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestAddNumericTwice()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            Directory directory = NewDirectory();
            // we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwc.SetMergePolicy(NewLogMergePolicy());
            IndexWriter iwriter = new IndexWriter(directory, iwc);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 1));
            doc.Add(new NumericDocValuesField("dv", 2));
            try
            {
                iwriter.AddDocument(doc);
                Assert.Fail("didn't hit expected exception");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }

            iwriter.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestTooLargeSortedBytes()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            Directory directory = NewDirectory();
            // we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwc.SetMergePolicy(NewLogMergePolicy());
            IndexWriter iwriter = new IndexWriter(directory, iwc);
            Document doc = new Document();
            var bytes = new byte[100000];
            BytesRef b = new BytesRef(bytes);
            Random.NextBytes(bytes);
            doc.Add(new SortedDocValuesField("dv", b));
            try
            {
                iwriter.AddDocument(doc);
                Assert.Fail("did not get expected exception");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
            iwriter.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestTooLargeTermSortedSetBytes()
        {
            AssumeTrue("codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            Analyzer analyzer = new MockAnalyzer(Random);

            Directory directory = NewDirectory();
            // we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwc.SetMergePolicy(NewLogMergePolicy());
            IndexWriter iwriter = new IndexWriter(directory, iwc);
            Document doc = new Document();
            byte[] bytes = new byte[100000];
            BytesRef b = new BytesRef(bytes);
            Random.NextBytes(bytes);
            doc.Add(new SortedSetDocValuesField("dv", b));
            try
            {
                iwriter.AddDocument(doc);
                Assert.Fail("did not get expected exception");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
            iwriter.Dispose();
            directory.Dispose();
        }

        // Two documents across segments
        [Test]
        public virtual void TestMixedTypesDifferentSegments()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("foo", 0));
            w.AddDocument(doc);
            w.Commit();

            doc = new Document();
            doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
            try
            {
                w.AddDocument(doc);
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            w.Dispose();
            dir.Dispose();
        }

        // Add inconsistent document after deleteAll
        [Test]
        public virtual void TestMixedTypesAfterDeleteAll()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("foo", 0));
            w.AddDocument(doc);
            w.DeleteAll();

            doc = new Document();
            doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
            w.AddDocument(doc);
            w.Dispose();
            dir.Dispose();
        }

        // Add inconsistent document after reopening IW w/ create
        [Test]
        public virtual void TestMixedTypesAfterReopenCreate()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("foo", 0));
            w.AddDocument(doc);
            w.Dispose();

            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetOpenMode(OpenMode.CREATE);
            w = new IndexWriter(dir, iwc);
            doc = new Document();
            doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
            w.AddDocument(doc);
            w.Dispose();
            dir.Dispose();
        }

        // Two documents with same field as different types, added
        // from separate threads:
        [Test]
        public virtual void TestMixedTypesDifferentThreads()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            CountdownEvent startingGun = new CountdownEvent(1);
            AtomicBoolean hitExc = new AtomicBoolean();
            ThreadJob[] threads = new ThreadJob[3];
            for (int i = 0; i < 3; i++)
            {
                Field field;
                if (i == 0)
                {
                    field = new SortedDocValuesField("foo", new BytesRef("hello"));
                }
                else if (i == 1)
                {
                    field = new NumericDocValuesField("foo", 0);
                }
                else
                {
                    field = new BinaryDocValuesField("foo", new BytesRef("bazz"));
                }
                Document doc = new Document();
                doc.Add(field);

                threads[i] = new ThreadAnonymousClass(this, w, startingGun, hitExc, doc);
                threads[i].Start();
            }

            startingGun.Signal();

            foreach (ThreadJob t in threads)
            {
                t.Join();
            }
            Assert.IsTrue(hitExc);
            w.Dispose();
            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestDocValuesIndexing outerInstance;

            private readonly IndexWriter w;
            private readonly CountdownEvent startingGun;
            private readonly AtomicBoolean hitExc;
            private readonly Document doc;

            public ThreadAnonymousClass(TestDocValuesIndexing outerInstance, IndexWriter w, CountdownEvent startingGun, AtomicBoolean hitExc, Document doc)
            {
                this.outerInstance = outerInstance;
                this.w = w;
                this.startingGun = startingGun;
                this.hitExc = hitExc;
                this.doc = doc;
            }

            public override void Run()
            {
                try
                {
                    startingGun.Wait();
                    w.AddDocument(doc);
                }
                catch (Exception iae) when (iae.IsIllegalArgumentException())
                {
                    // expected
                    hitExc.Value = (true);
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        // Adding documents via addIndexes
        [Test]
        public virtual void TestMixedTypesViaAddIndexes()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("foo", 0));
            w.AddDocument(doc);

            // Make 2nd index w/ inconsistent field
            Directory dir2 = NewDirectory();
            IndexWriter w2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            doc = new Document();
            doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
            w2.AddDocument(doc);
            w2.Dispose();

            try
            {
                w.AddIndexes(dir2);
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }

            IndexReader r = DirectoryReader.Open(dir2);
            try
            {
                w.AddIndexes(new IndexReader[] { r });
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }

            r.Dispose();
            dir2.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIllegalTypeChange()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
            try
            {
                writer.AddDocument(doc);
                Assert.Fail("did not hit exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIllegalTypeChangeAcrossSegments()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);
            writer.Dispose();

            writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
            try
            {
                writer.AddDocument(doc);
                Assert.Fail("did not hit exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestTypeChangeAfterCloseAndDeleteAll()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);
            writer.Dispose();

            writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            writer.DeleteAll();
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
            writer.AddDocument(doc);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestTypeChangeAfterDeleteAll()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);
            writer.DeleteAll();
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
            writer.AddDocument(doc);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestTypeChangeAfterCommitAndDeleteAll()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);
            writer.Commit();
            writer.DeleteAll();
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
            writer.AddDocument(doc);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestTypeChangeAfterOpenCreate()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);
            writer.Dispose();
            conf.SetOpenMode(OpenMode.CREATE);
            writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
            writer.AddDocument(doc);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestTypeChangeViaAddIndexes()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);
            writer.Dispose();

            Directory dir2 = NewDirectory();
            writer = new IndexWriter(dir2, (IndexWriterConfig)conf.Clone());
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
            writer.AddDocument(doc);
            try
            {
                writer.AddIndexes(dir);
                Assert.Fail("did not hit exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            writer.Dispose();

            dir.Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestTypeChangeViaAddIndexesIR()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);
            writer.Dispose();

            Directory dir2 = NewDirectory();
            writer = new IndexWriter(dir2, (IndexWriterConfig)conf.Clone());
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
            writer.AddDocument(doc);
            IndexReader[] readers = new IndexReader[] { DirectoryReader.Open(dir) };
            try
            {
                writer.AddIndexes(readers);
                Assert.Fail("did not hit exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            readers[0].Dispose();
            writer.Dispose();

            dir.Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestTypeChangeViaAddIndexes2()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);
            writer.Dispose();

            Directory dir2 = NewDirectory();
            writer = new IndexWriter(dir2, (IndexWriterConfig)conf.Clone());
            writer.AddIndexes(dir);
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
            try
            {
                writer.AddDocument(doc);
                Assert.Fail("did not hit exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            writer.Dispose();
            dir2.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestTypeChangeViaAddIndexesIR2()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)conf.Clone());
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);
            writer.Dispose();

            Directory dir2 = NewDirectory();
            writer = new IndexWriter(dir2, (IndexWriterConfig)conf.Clone());
            IndexReader[] readers = new IndexReader[] { DirectoryReader.Open(dir) };
            writer.AddIndexes(readers);
            readers[0].Dispose();
            doc = new Document();
            doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
            try
            {
                writer.AddDocument(doc);
                Assert.Fail("did not hit exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            writer.Dispose();
            dir2.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDocsWithField()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new TextField("dv", "some text", Field.Store.NO));
            doc.Add(new NumericDocValuesField("dv", 0L));
            writer.AddDocument(doc);

            DirectoryReader r = writer.GetReader();
            writer.Dispose();

            AtomicReader subR = (AtomicReader)r.Leaves[0].Reader;
            Assert.AreEqual(2, subR.NumDocs);

            IBits bits = FieldCache.DEFAULT.GetDocsWithField(subR, "dv");
            Assert.IsTrue(bits.Get(0));
            Assert.IsTrue(bits.Get(1));
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSameFieldNameForPostingAndDocValue()
        {
            // LUCENE-5192: FieldInfos.Builder neglected to update
            // globalFieldNumbers.docValuesType map if the field existed, resulting in
            // potentially adding the same field with different DV types.
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            doc.Add(new StringField("f", "mock-value", Field.Store.NO));
            doc.Add(new NumericDocValuesField("f", 5));
            writer.AddDocument(doc);
            writer.Commit();

            doc = new Document();
            doc.Add(new BinaryDocValuesField("f", new BytesRef("mock")));
            try
            {
                writer.AddDocument(doc);
                Assert.Fail("should not have succeeded to add a field with different DV type than what already exists");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                writer.Rollback();
            }

            dir.Dispose();
        }
    }
}