using J2N.Collections.Generic.Extensions;
using J2N.Text;
using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Codecs.SimpleText;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;
using Assert = Lucene.Net.TestFramework.Assert;
using RandomInts = RandomizedTesting.Generators.RandomNumbers;
using Number = J2N.Numerics.Number;
using Double = J2N.Numerics.Double;
using Int32 = J2N.Numerics.Int32;
using Int64 = J2N.Numerics.Int64;
using Single = J2N.Numerics.Single;
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
    /// Base class aiming at testing <see cref="Codecs.StoredFieldsFormat"/>.
    /// To test a new format, all you need is to register a new <see cref="Codec"/> which
    /// uses it and extend this class and override <see cref="BaseIndexFileFormatTestCase.GetCodec()"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class BaseStoredFieldsFormatTestCase : BaseIndexFileFormatTestCase
    {
        protected override void AddRandomFields(Document d)
        {
            int numValues = Random.Next(3);
            for (int i = 0; i < numValues; ++i)
            {
                d.Add(new StoredField("f", TestUtil.RandomSimpleString(Random, 100)));
            }
        }

        [Test]
        public virtual void TestRandomStoredFields()
        {
            using Directory dir = NewDirectory();
            Random rand = Random;
            using RandomIndexWriter w = new RandomIndexWriter(rand, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(TestUtil.NextInt32(rand, 5, 20)));
            //w.w.setNoCFSRatio(0.0);
            int docCount = AtLeast(200);
            int fieldCount = TestUtil.NextInt32(rand, 1, 5);

            IList<int> fieldIDs = new JCG.List<int>();

            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.IsTokenized = false;
            Field idField = NewField("id", "", customType);

            for (int i = 0; i < fieldCount; i++)
            {
                fieldIDs.Add(i);
            }

            IDictionary<string, Document> docs = new Dictionary<string, Document>();

            if (Verbose)
            {
                Console.WriteLine("TEST: build index docCount=" + docCount);
            }

            FieldType customType2 = new FieldType();
            customType2.IsStored = true;
            for (int i = 0; i < docCount; i++)
            {
                Document doc = new Document();
                doc.Add(idField);
                string id = "" + i;
                idField.SetStringValue(id);
                docs[id] = doc;
                if (Verbose)
                {
                    Console.WriteLine("TEST: add doc id=" + id);
                }

                foreach (int field in fieldIDs)
                {
                    string s;
                    if (rand.Next(4) != 3)
                    {
                        s = TestUtil.RandomUnicodeString(rand, 1000);
                        doc.Add(NewField("f" + field, s, customType2));
                    }
                    else
                    {
                        s = null;
                    }
                }
                w.AddDocument(doc);
                if (rand.Next(50) == 17)
                {
                    // mixup binding of field name -> Number every so often
                    fieldIDs.Shuffle(Random);
                }
                if (rand.Next(5) == 3 && i > 0)
                {
                    string delID = "" + rand.Next(i);
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: delete doc id=" + delID);
                    }
                    w.DeleteDocuments(new Term("id", delID));
                    docs.Remove(delID);
                }
            }

            if (Verbose)
            {
                Console.WriteLine("TEST: " + docs.Count + " docs in index; now load fields");
            }
            if (docs.Count > 0)
            {
                string[] idsList = docs.Keys.ToArray(/*new string[docs.Count]*/);

                for (int x = 0; x < 2; x++)
                {
                    using (IndexReader r = w.GetReader())
                    {
                        IndexSearcher s = NewSearcher(r);

                        if (Verbose)
                        {
                            Console.WriteLine("TEST: cycle x=" + x + " r=" + r);
                        }

                        int num = AtLeast(1000);
                        for (int iter = 0; iter < num; iter++)
                        {
                            string testID = idsList[rand.Next(idsList.Length)];
                            if (Verbose)
                            {
                                Console.WriteLine("TEST: test id=" + testID);
                            }
                            TopDocs hits = s.Search(new TermQuery(new Term("id", testID)), 1);
                            Assert.AreEqual(1, hits.TotalHits);
                            Document doc = r.Document(hits.ScoreDocs[0].Doc);
                            Document docExp = docs[testID];
                            for (int i = 0; i < fieldCount; i++)
                            {
                                assertEquals("doc " + testID + ", field f" + fieldCount + " is wrong", docExp.Get("f" + i), doc.Get("f" + i));
                            }
                        }
                    } // r.Dispose();
                    w.ForceMerge(1);
                }
            }
        }

        [Test]
        // LUCENE-1727: make sure doc fields are stored in order
        public virtual void TestStoredFieldsOrder()
        {
            using Directory d = NewDirectory();
            using IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();

            FieldType customType = new FieldType();
            customType.IsStored = true;
            doc.Add(NewField("zzz", "a b c", customType));
            doc.Add(NewField("aaa", "a b c", customType));
            doc.Add(NewField("zzz", "1 2 3", customType));
            w.AddDocument(doc);
            using IndexReader r = w.GetReader();
            Document doc2 = r.Document(0);
            IEnumerator<IIndexableField> it = doc2.Fields.GetEnumerator();
            Assert.IsTrue(it.MoveNext());
            Field f = (Field)it.Current;
            Assert.AreEqual(f.Name, "zzz");
            Assert.AreEqual(f.GetStringValue(), "a b c");

            Assert.IsTrue(it.MoveNext());
            f = (Field)it.Current;
            Assert.AreEqual(f.Name, "aaa");
            Assert.AreEqual(f.GetStringValue(), "a b c");

            Assert.IsTrue(it.MoveNext());
            f = (Field)it.Current;
            Assert.AreEqual(f.Name, "zzz");
            Assert.AreEqual(f.GetStringValue(), "1 2 3");
            Assert.IsFalse(it.MoveNext());
        }

        [Test]
        // LUCENE-1219
        public virtual void TestBinaryFieldOffsetLength()
        {
            using Directory dir = NewDirectory();
            var b = new byte[50];
            using (IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))))
            {
                for (int i = 0; i < 50; i++)
                {
                    b[i] = (byte)(i + 77);
                }

                Document doc = new Document();
                Field f = new StoredField("binary", b, 10, 17);
                var bx = f.GetBinaryValue().Bytes;
                Assert.IsTrue(bx != null);
                Assert.AreEqual(50, bx.Length);
                Assert.AreEqual(10, f.GetBinaryValue().Offset);
                Assert.AreEqual(17, f.GetBinaryValue().Length);
                doc.Add(f);
                w.AddDocument(doc);
            } // w.Dispose();

            using IndexReader ir = DirectoryReader.Open(dir);
            Document doc2 = ir.Document(0);
            IIndexableField f2 = doc2.GetField("binary");
            b = f2.GetBinaryValue().Bytes;
            Assert.IsTrue(b != null);
            Assert.AreEqual(17, b.Length, 17);
            Assert.AreEqual((byte)87, b[0]);
        }

        [Test]
        public virtual void TestNumericField()
        {
            using Directory dir = NewDirectory();
            DirectoryReader r = null;
            try
            {
                var numDocs = AtLeast(500);
                var answers = new Number[numDocs];
                using (var w = new RandomIndexWriter(Random, dir))
                {
                    NumericType[] typeAnswers = new NumericType[numDocs];
                    for (int id = 0; id < numDocs; id++)
                    {
                        Document doc = new Document();
                        Field nf;
                        Field sf;
                        Number answer;
                        NumericType typeAnswer;
                        if (Random.NextBoolean())
                        {
                            // float/double
                            if (Random.NextBoolean())
                            {
                                float f = Random.NextSingle();
                                answer = Single.GetInstance(f);
                                nf = new SingleField("nf", f, Field.Store.NO);
                                sf = new StoredField("nf", f);
                                typeAnswer = NumericType.SINGLE;
                            }
                            else
                            {
                                double d = Random.NextDouble();
                                answer = Double.GetInstance(d);
                                nf = new DoubleField("nf", d, Field.Store.NO);
                                sf = new StoredField("nf", d);
                                typeAnswer = NumericType.DOUBLE;
                            }
                        }
                        else
                        {
                            // int/long
                            if (Random.NextBoolean())
                            {
                                int i = Random.Next();
                                answer = Int32.GetInstance(i);
                                nf = new Int32Field("nf", i, Field.Store.NO);
                                sf = new StoredField("nf", i);
                                typeAnswer = NumericType.INT32;
                            }
                            else
                            {
                                long l = Random.NextInt64();
                                answer = Int64.GetInstance(l);
                                nf = new Int64Field("nf", l, Field.Store.NO);
                                sf = new StoredField("nf", l);
                                typeAnswer = NumericType.INT64;
                            }
                        }
                        doc.Add(nf);
                        doc.Add(sf);
                        answers[id] = answer;
                        typeAnswers[id] = typeAnswer;
                        FieldType ft = new FieldType(Int32Field.TYPE_STORED);
                        ft.NumericPrecisionStep = int.MaxValue;
                        doc.Add(new Int32Field("id", id, ft));
                        w.AddDocument(doc);
                    }
                    r = w.GetReader();
                } // w.Dispose();

                Assert.AreEqual(numDocs, r.NumDocs);

                foreach (AtomicReaderContext ctx in r.Leaves)
                {
                    AtomicReader sub = ctx.AtomicReader;
                    FieldCache.Int32s ids = FieldCache.DEFAULT.GetInt32s(sub, "id", false);
                    for (int docID = 0; docID < sub.NumDocs; docID++)
                    {
                        Document doc = sub.Document(docID);
                        Field f = doc.GetField<Field>("nf");
                        Assert.IsTrue(f is StoredField, "got f=" + f);
#pragma warning disable 612, 618
                        Assert.AreEqual(answers[ids.Get(docID)], f.GetNumericValue());
#pragma warning restore 612, 618
                    }
                }
            }
            finally
            {
                r?.Dispose();
            }
        }

        [Test]
        public virtual void TestIndexedBit()
        {
            using Directory dir = NewDirectory();
            IndexReader r = null;
            try
            {
                using (RandomIndexWriter w = new RandomIndexWriter(Random, dir))
                {
                    Document doc = new Document();
                    FieldType onlyStored = new FieldType();
                    onlyStored.IsStored = true;
                    doc.Add(new Field("field", "value", onlyStored));
                    doc.Add(new StringField("field2", "value", Field.Store.YES));
                    w.AddDocument(doc);
                    r = w.GetReader();
                } // w.Dispose();
                Assert.IsFalse(r.Document(0).GetField("field").IndexableFieldType.IsIndexed);
                Assert.IsTrue(r.Document(0).GetField("field2").IndexableFieldType.IsIndexed);
            }
            finally
            {
                r?.Dispose();
            }
        }

        [Test]
        public virtual void TestReadSkip()
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwConf.SetMaxBufferedDocs(RandomInts.RandomInt32Between(Random, 2, 30));
            using RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwConf);
            FieldType ft = new FieldType();
            ft.IsStored = true;
            ft.Freeze();

            string @string = TestUtil.RandomSimpleString(Random, 50);
            var bytes = @string.GetBytes(Encoding.UTF8);
            long l = Random.NextBoolean() ? Random.Next(42) : Random.NextInt64();
            int i = Random.NextBoolean() ? Random.Next(42) : Random.Next();
            float f = Random.NextSingle();
            double d = Random.NextDouble();

            Field[] fields = new Field[]
            {
                        new Field("bytes", bytes, ft),
                        new Field("string", @string, ft),
                        new Int64Field("long", l, Field.Store.YES),
                        new Int32Field("int", i, Field.Store.YES),
                        new SingleField("float", f, Field.Store.YES),
                        new DoubleField("double", d, Field.Store.YES)
            };

            for (int k = 0; k < 100; ++k)
            {
                Document doc = new Document();
                foreach (Field fld in fields)
                {
                    doc.Add(fld);
                }
                iw.IndexWriter.AddDocument(doc);
            }
            iw.Commit();

            using DirectoryReader reader = DirectoryReader.Open(dir);
            int docID = Random.Next(100);
            foreach (Field fld in fields)
            {
                string fldName = fld.Name;
                Document sDoc = reader.Document(docID, new JCG.HashSet<string> { fldName });
                IIndexableField sField = sDoc.GetField(fldName);
                if (typeof(Field) == fld.GetType())
                {
                    Assert.AreEqual(fld.GetBinaryValue(), sField.GetBinaryValue());
                    Assert.AreEqual(fld.GetStringValue(), sField.GetStringValue());
                }
                else
                {
#pragma warning disable 612, 618
                    Assert.AreEqual(fld.GetNumericValue(), sField.GetNumericValue());
#pragma warning restore 612, 618
                }
            }
        }

        [Test]
        public virtual void TestEmptyDocs()
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwConf.SetMaxBufferedDocs(RandomInts.RandomInt32Between(Random, 2, 30));
            using RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwConf);

            // make sure that the fact that documents might be empty is not a problem
            Document emptyDoc = new Document();
            int numDocs = Random.NextBoolean() ? 1 : AtLeast(1000);
            for (int i = 0; i < numDocs; ++i)
            {
                iw.AddDocument(emptyDoc);
            }
            iw.Commit();
            using DirectoryReader rd = DirectoryReader.Open(dir);
            for (int i = 0; i < numDocs; ++i)
            {
                Document doc = rd.Document(i);
                Assert.IsNotNull(doc);
                Assert.IsTrue(doc.Fields.Count <= 0);
            }
        }

        [Test]
        public virtual void TestConcurrentReads()
        {
            using Directory dir = NewDirectory();
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwConf.SetMaxBufferedDocs(RandomInts.RandomInt32Between(Random, 2, 30));
            using RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwConf);

            // make sure the readers are properly cloned
            Document doc = new Document();
            Field field = new StringField("fld", "", Field.Store.YES);
            doc.Add(field);
            int numDocs = AtLeast(1000);
            for (int i = 0; i < numDocs; ++i)
            {
                field.SetStringValue("" + i);
                iw.AddDocument(doc);
            }
            iw.Commit();

            AtomicReference<Exception> ex = new AtomicReference<Exception>();
            using (DirectoryReader rd = DirectoryReader.Open(dir))
            {
                IndexSearcher searcher = new IndexSearcher(rd);
                int concurrentReads = AtLeast(5);
                int readsPerThread = AtLeast(50);
                IList<ThreadJob> readThreads = new JCG.List<ThreadJob>();

                for (int i = 0; i < concurrentReads; ++i)
                {
                    readThreads.Add(new ThreadAnonymousClass(numDocs, rd, searcher, readsPerThread, ex));
                }
                foreach (ThreadJob thread in readThreads)
                {
                    thread.Start();
                }
                foreach (ThreadJob thread in readThreads)
                {
                    thread.Join();
                }
            } // rd.Dispose();
            if (ex.Value != null)
            {
                ExceptionDispatchInfo.Capture(ex.Value).Throw(); // LUCENENET: Rethrow to preserve stack details from the other thread
            }
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly int numDocs;
            private readonly DirectoryReader rd;
            private readonly IndexSearcher searcher;
            private readonly int readsPerThread;
            private readonly AtomicReference<Exception> ex;
            private readonly int[] queries;

            public ThreadAnonymousClass(int numDocs, DirectoryReader rd, IndexSearcher searcher, int readsPerThread, AtomicReference<Exception> ex)
            {
                this.numDocs = numDocs;
                this.rd = rd;
                this.searcher = searcher;
                this.readsPerThread = readsPerThread;
                this.ex = ex;

                queries = new int[this.readsPerThread];
                for (int j = 0; j < queries.Length; ++j)
                {
                    queries[j] = Random.Next(this.numDocs);
                }
            }

            public override void Run()
            {
                foreach (int q in queries)
                {
                    Query query = new TermQuery(new Term("fld", "" + q));
                    try
                    {
                        TopDocs topDocs = searcher.Search(query, 1);
                        if (topDocs.TotalHits != 1)
                        {
                            Console.WriteLine(query);
                            throw IllegalStateException.Create("Expected 1 hit, got " + topDocs.TotalHits);
                        }
                        Document sdoc = rd.Document(topDocs.ScoreDocs[0].Doc);
                        if (sdoc is null || sdoc.Get("fld") is null)
                        {
                            throw IllegalStateException.Create("Could not find document " + q);
                        }
                        if (!Convert.ToString(q, CultureInfo.InvariantCulture).Equals(sdoc.Get("fld"), StringComparison.Ordinal))
                        {
                            throw IllegalStateException.Create("Expected " + q + ", but got " + sdoc.Get("fld"));
                        }
                    }
                    catch (Exception e) when (e.IsException())
                    {
                        ex.CompareAndSet(null, e);
                    }
                }
            }
        }

        private static byte[] RandomByteArray(int length, int max)
        {
            var result = new byte[length];
            for (int i = 0; i < length; ++i)
            {
                result[i] = (byte)Random.Next(max);
            }
            return result;
        }

        [Test]
        public virtual void TestWriteReadMerge()
        {
            // get another codec, other than the default: so we are merging segments across different codecs
            Codec otherCodec;
            if ("SimpleText".Equals(Codec.Default.Name, StringComparison.Ordinal))
            {
                otherCodec = new Lucene46Codec();
            }
            else
            {
                otherCodec = new SimpleTextCodec();
            }
            using Directory dir = NewDirectory();
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwConf.SetMaxBufferedDocs(RandomInts.RandomInt32Between(Random, 2, 30));
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, (IndexWriterConfig)iwConf.Clone());
            try
            {

                int docCount = AtLeast(200);
                var data = new byte[docCount][][];
                for (int i = 0; i < docCount; ++i)
                {
                    int fieldCount = Rarely() ? RandomInts.RandomInt32Between(Random, 1, 500) : RandomInts.RandomInt32Between(Random, 1, 5);
                    data[i] = new byte[fieldCount][];
                    for (int j = 0; j < fieldCount; ++j)
                    {
                        int length = Rarely() ? Random.Next(1000) : Random.Next(10);
                        int max = Rarely() ? 256 : 2;
                        data[i][j] = RandomByteArray(length, max);
                    }
                }

                FieldType type = new FieldType(StringField.TYPE_STORED);
                type.IsIndexed = false;
                type.Freeze();
                Int32Field id = new Int32Field("id", 0, Field.Store.YES);
                for (int i = 0; i < data.Length; ++i)
                {
                    Document doc = new Document();
                    doc.Add(id);
                    id.SetInt32Value(i);
                    for (int j = 0; j < data[i].Length; ++j)
                    {
                        Field f = new Field("bytes" + j, data[i][j], type);
                        doc.Add(f);
                    }
                    iw.IndexWriter.AddDocument(doc);
                    if (Random.NextBoolean() && (i % (data.Length / 10) == 0))
                    {
                        iw.IndexWriter.Dispose();
                        // test merging against a non-compressing codec
                        if (iwConf.Codec == otherCodec)
                        {
                            iwConf.SetCodec(Codec.Default);
                        }
                        else
                        {
                            iwConf.SetCodec(otherCodec);
                        }
                        iw = new RandomIndexWriter(Random, dir, (IndexWriterConfig)iwConf.Clone());
                    }
                }

                for (int i = 0; i < 10; ++i)
                {
                    int min = Random.Next(data.Length);
                    int max = min + Random.Next(20);
                    iw.DeleteDocuments(NumericRangeQuery.NewInt32Range("id", min, max, true, false));
                }

                iw.ForceMerge(2); // force merges with deletions

                iw.Commit();

                using (DirectoryReader ir = DirectoryReader.Open(dir))
                {
                    Assert.IsTrue(ir.NumDocs > 0);
                    int numDocs = 0;
                    for (int i = 0; i < ir.MaxDoc; ++i)
                    {
                        Document doc = ir.Document(i);
                        if (doc is null)
                        {
                            continue;
                        }
                        ++numDocs;
                        int docId = (int)doc.GetField("id").GetInt32Value();
                        Assert.AreEqual(data[docId].Length + 1, doc.Fields.Count);
                        for (int j = 0; j < data[docId].Length; ++j)
                        {
                            var arr = data[docId][j];
                            BytesRef arr2Ref = doc.GetBinaryValue("bytes" + j);
                            var arr2 = Arrays.CopyOfRange(arr2Ref.Bytes, arr2Ref.Offset, arr2Ref.Offset + arr2Ref.Length);
                            Assert.AreEqual(arr, arr2);
                        }
                    }
                    Assert.IsTrue(ir.NumDocs <= numDocs);
                } // ir.Dispose();

                iw.DeleteAll();
                iw.Commit();
                iw.ForceMerge(1);

            }
            finally
            {
                iw.Dispose();
            }
        }

        [Test]
        [Nightly]
        public virtual void TestBigDocuments()
        {
            // "big" as "much bigger than the chunk size"
            // for this test we force a FS dir
            // we can't just use newFSDirectory, because this test doesn't really index anything.
            // so if we get NRTCachingDir+SimpleText, we make massive stored fields and OOM (LUCENE-4484)
            using Directory dir = new MockDirectoryWrapper(Random, new MMapDirectory(CreateTempDir("testBigDocuments")));
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            //iwConf.SetMaxBufferedDocs(RandomInts.RandomInt32Between(Random, 2, 30));
            // LUCENENET specific - Reduced amount to keep the total
            // Nightly test time under 1 hour
            iwConf.SetMaxBufferedDocs(RandomInts.RandomInt32Between(Random, 2, 15));
            using RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwConf);
            if (dir is MockDirectoryWrapper mockDirectoryWrapper)
            {
                mockDirectoryWrapper.Throttling = Throttling.NEVER;
            }

            Document emptyDoc = new Document(); // emptyDoc
            Document bigDoc1 = new Document(); // lot of small fields
            Document bigDoc2 = new Document(); // 1 very big field

            Field idField = new StringField("id", "", Field.Store.NO);
            emptyDoc.Add(idField);
            bigDoc1.Add(idField);
            bigDoc2.Add(idField);

            FieldType onlyStored = new FieldType(StringField.TYPE_STORED);
            onlyStored.IsIndexed = false;

            Field smallField = new Field("fld", RandomByteArray(Random.Next(10), 256), onlyStored);
            //int numFields = RandomInts.RandomInt32Between(Random, 500000, 1000000);
            // LUCENENET specific - Reduced amount to keep the total
            // Nightly test time under 1 hour
            int numFields = RandomInts.RandomInt32Between(Random, 250000, 500000);
            for (int i = 0; i < numFields; ++i)
            {
                bigDoc1.Add(smallField);
            }

            //Field bigField = new Field("fld", RandomByteArray(RandomInts.RandomInt32Between(Random, 1000000, 5000000), 2), onlyStored);
            // LUCENENET specific - Reduced amount to keep the total
            // Nightly test time under 1 hour
            Field bigField = new Field("fld", RandomByteArray(RandomInts.RandomInt32Between(Random, 500000, 2500000), 2), onlyStored);
            bigDoc2.Add(bigField);

            int numDocs = AtLeast(5);
            Document[] docs = new Document[numDocs];
            for (int i = 0; i < numDocs; ++i)
            {
                docs[i] = RandomPicks.RandomFrom(Random, new Document[] { emptyDoc, bigDoc1, bigDoc2 });
            }
            for (int i = 0; i < numDocs; ++i)
            {
                idField.SetStringValue("" + i);
                iw.AddDocument(docs[i]);
                if (Random.Next(numDocs) == 0)
                {
                    iw.Commit();
                }
            }
            iw.Commit();
            iw.ForceMerge(1); // look at what happens when big docs are merged
            using DirectoryReader rd = DirectoryReader.Open(dir);
            IndexSearcher searcher = new IndexSearcher(rd);
            for (int i = 0; i < numDocs; ++i)
            {
                Query query = new TermQuery(new Term("id", "" + i));
                TopDocs topDocs = searcher.Search(query, 1);
                Assert.AreEqual(1, topDocs.TotalHits, "" + i);
                Document doc = rd.Document(topDocs.ScoreDocs[0].Doc);
                Assert.IsNotNull(doc);
                IIndexableField[] fieldValues = doc.GetFields("fld");
                Assert.AreEqual(docs[i].GetFields("fld").Length, fieldValues.Length);
                if (fieldValues.Length > 0)
                {
                    Assert.AreEqual(docs[i].GetFields("fld")[0].GetBinaryValue(), fieldValues[0].GetBinaryValue());
                }
            }
        }

        [Test]
        public virtual void TestBulkMergeWithDeletes()
        {
            int numDocs = AtLeast(200);
            using Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
            try
            {
                for (int i = 0; i < numDocs; ++i)
                {
                    Document doc = new Document();
                    doc.Add(new StringField("id", Convert.ToString(i, CultureInfo.InvariantCulture), Field.Store.YES));
                    doc.Add(new StoredField("f", TestUtil.RandomSimpleString(Random)));
                    w.AddDocument(doc);
                }
                int deleteCount = TestUtil.NextInt32(Random, 5, numDocs);
                for (int i = 0; i < deleteCount; ++i)
                {
                    int id = Random.Next(numDocs);
                    w.DeleteDocuments(new Term("id", Convert.ToString(id, CultureInfo.InvariantCulture)));
                }
                w.Commit();
                w.Dispose();
                w = new RandomIndexWriter(Random, dir);
                w.ForceMerge(TestUtil.NextInt32(Random, 1, 3));
                w.Commit();
            }
            finally
            {
                w.Dispose();
            }
            TestUtil.CheckIndex(dir);
        }
    }
}