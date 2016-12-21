using Lucene.Net.Attributes;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lucene.Net.Index
{
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;

    //using SimpleTextCodec = Lucene.Net.Codecs.simpletext.SimpleTextCodec;
    using Document = Documents.Document;
    using DoubleField = DoubleField;
    using Field = Field;
    using FieldType = FieldType;
    using FloatField = FloatField;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IntField = IntField;
    using LongField = LongField;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;
    using MMapDirectory = Lucene.Net.Store.MMapDirectory;

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
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using NumericRangeQuery = Lucene.Net.Search.NumericRangeQuery;
    using Query = Lucene.Net.Search.Query;
    using StoredField = StoredField;
    using StringField = StringField;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;
    using TopDocs = Lucene.Net.Search.TopDocs;

    /// <summary>
    /// Base class aiming at testing <seealso cref="StoredFieldsFormat"/>.
    /// To test a new format, all you need is to register a new <seealso cref="Codec"/> which
    /// uses it and extend this class and override <seealso cref="#getCodec()"/>.
    /// @lucene.experimental
    /// </summary>
    public abstract class BaseStoredFieldsFormatTestCase : BaseIndexFileFormatTestCase
    {
        protected internal override void AddRandomFields(Document d)
        {
            int numValues = Random().Next(3);
            for (int i = 0; i < numValues; ++i)
            {
                d.Add(new StoredField("f", TestUtil.RandomSimpleString(Random(), 100)));
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestRandomStoredFields()
        {
            Directory dir = NewDirectory();
            Random rand = Random();
            RandomIndexWriter w = new RandomIndexWriter(rand, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(TestUtil.NextInt(rand, 5, 20)));
            //w.w.setNoCFSRatio(0.0);
            int docCount = AtLeast(200);
            int fieldCount = TestUtil.NextInt(rand, 1, 5);

            IList<int?> fieldIDs = new List<int?>();

            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.IsTokenized = false;
            Field idField = NewField("id", "", customType);

            for (int i = 0; i < fieldCount; i++)
            {
                fieldIDs.Add(i);
            }

            IDictionary<string, Document> docs = new Dictionary<string, Document>();

            if (VERBOSE)
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
                if (VERBOSE)
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
                    fieldIDs = CollectionsHelper.Shuffle(fieldIDs);
                }
                if (rand.Next(5) == 3 && i > 0)
                {
                    string delID = "" + rand.Next(i);
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: delete doc id=" + delID);
                    }
                    w.DeleteDocuments(new Term("id", delID));
                    docs.Remove(delID);
                }
            }

            if (VERBOSE)
            {
                Console.WriteLine("TEST: " + docs.Count + " docs in index; now load fields");
            }
            if (docs.Count > 0)
            {
                string[] idsList = docs.Keys.ToArray(/*new string[docs.Count]*/);

                for (int x = 0; x < 2; x++)
                {
                    IndexReader r = w.Reader;
                    IndexSearcher s = NewSearcher(r);

                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: cycle x=" + x + " r=" + r);
                    }

                    int num = AtLeast(1000);
                    for (int iter = 0; iter < num; iter++)
                    {
                        string testID = idsList[rand.Next(idsList.Length)];
                        if (VERBOSE)
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
                    r.Dispose();
                    w.ForceMerge(1);
                }
            }
            w.Dispose();
            dir.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        // LUCENE-1727: make sure doc fields are stored in order
        public virtual void TestStoredFieldsOrder()
        {
            Directory d = NewDirectory();
            IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();

            FieldType customType = new FieldType();
            customType.IsStored = true;
            doc.Add(NewField("zzz", "a b c", customType));
            doc.Add(NewField("aaa", "a b c", customType));
            doc.Add(NewField("zzz", "1 2 3", customType));
            w.AddDocument(doc);
            IndexReader r = w.Reader;
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
            r.Dispose();
            w.Dispose();
            d.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        // LUCENE-1219
        public virtual void TestBinaryFieldOffsetLength()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            var b = new byte[50];
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
            w.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            Document doc2 = ir.Document(0);
            IIndexableField f2 = doc2.GetField("binary");
            b = f2.GetBinaryValue().Bytes;
            Assert.IsTrue(b != null);
            Assert.AreEqual(17, b.Length, 17);
            Assert.AreEqual(87, b[0]);
            ir.Dispose();
            dir.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestNumericField()
        {
            Directory dir = NewDirectory();
            var w = new RandomIndexWriter(Random(), dir, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
            var numDocs = AtLeast(500);
            var answers = new object[numDocs];
            NumericType[] typeAnswers = new NumericType[numDocs];
            for (int id = 0; id < numDocs; id++)
            {
                Document doc = new Document();
                Field nf;
                Field sf;
                object answer;
                NumericType typeAnswer;
                if (Random().NextBoolean())
                {
                    // float/double
                    if (Random().NextBoolean())
                    {
                        float f = Random().NextFloat();
                        answer = Convert.ToSingle(f);
                        nf = new FloatField("nf", f, Field.Store.NO);
                        sf = new StoredField("nf", f);
                        typeAnswer = NumericType.FLOAT;
                    }
                    else
                    {
                        double d = Random().NextDouble();
                        answer = Convert.ToDouble(d);
                        nf = new DoubleField("nf", d, Field.Store.NO);
                        sf = new StoredField("nf", d);
                        typeAnswer = NumericType.DOUBLE;
                    }
                }
                else
                {
                    // int/long
                    if (Random().NextBoolean())
                    {
                        int i = Random().Next();
                        answer = Convert.ToInt32(i);
                        nf = new IntField("nf", i, Field.Store.NO);
                        sf = new StoredField("nf", i);
                        typeAnswer = NumericType.INT;
                    }
                    else
                    {
                        long l = Random().NextLong();
                        answer = Convert.ToInt64(l);
                        nf = new LongField("nf", l, Field.Store.NO);
                        sf = new StoredField("nf", l);
                        typeAnswer = NumericType.LONG;
                    }
                }
                doc.Add(nf);
                doc.Add(sf);
                answers[id] = answer;
                typeAnswers[id] = typeAnswer;
                FieldType ft = new FieldType(IntField.TYPE_STORED);
                ft.NumericPrecisionStep = int.MaxValue;
                doc.Add(new IntField("id", id, ft));
                w.AddDocument(doc);
            }
            DirectoryReader r = w.Reader;
            w.Dispose();

            Assert.AreEqual(numDocs, r.NumDocs);

            foreach (AtomicReaderContext ctx in r.Leaves)
            {
                AtomicReader sub = (AtomicReader)ctx.Reader;
                FieldCache.Ints ids = FieldCache.DEFAULT.GetInts(sub, "id", false);
                for (int docID = 0; docID < sub.NumDocs; docID++)
                {
                    Document doc = sub.Document(docID);
                    Field f = (Field)doc.GetField("nf");
                    Assert.IsTrue(f is StoredField, "got f=" + f);
                    Assert.AreEqual(answers[ids.Get(docID)], f.GetNumericValue());
                }
            }
            r.Dispose();
            dir.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestIndexedBit()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
            Document doc = new Document();
            FieldType onlyStored = new FieldType();
            onlyStored.IsStored = true;
            doc.Add(new Field("field", "value", onlyStored));
            doc.Add(new StringField("field2", "value", Field.Store.YES));
            w.AddDocument(doc);
            IndexReader r = w.Reader;
            w.Dispose();
            Assert.IsFalse(r.Document(0).GetField("field").FieldType.IsIndexed);
            Assert.IsTrue(r.Document(0).GetField("field2").FieldType.IsIndexed);
            r.Dispose();
            dir.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestReadSkip()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwConf.SetMaxBufferedDocs(RandomInts.NextIntBetween(Random(), 2, 30));
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);

            FieldType ft = new FieldType();
            ft.IsStored = true;
            ft.Freeze();

            string @string = TestUtil.RandomSimpleString(Random(), 50);
            var bytes = @string.GetBytes(IOUtils.CHARSET_UTF_8);
            long l = Random().NextBoolean() ? Random().Next(42) : Random().NextLong();
            int i = Random().NextBoolean() ? Random().Next(42) : Random().Next();
            float f = Random().NextFloat();
            double d = Random().NextDouble();

            IList<Field> fields = Arrays.AsList(new Field("bytes", bytes, ft), new Field("string", @string, ft), new LongField("long", l, Field.Store.YES), new IntField("int", i, Field.Store.YES), new FloatField("float", f, Field.Store.YES), new DoubleField("double", d, Field.Store.YES)
           );

            for (int k = 0; k < 100; ++k)
            {
                Document doc = new Document();
                foreach (Field fld in fields)
                {
                    doc.Add(fld);
                }
                iw.w.AddDocument(doc);
            }
            iw.Commit();

            DirectoryReader reader = DirectoryReader.Open(dir);
            int docID = Random().Next(100);
            foreach (Field fld in fields)
            {
                string fldName = fld.Name;
                Document sDoc = reader.Document(docID, Collections.Singleton(fldName));
                IIndexableField sField = sDoc.GetField(fldName);
                if (typeof(Field) == fld.GetType())
                {
                    Assert.AreEqual(fld.GetBinaryValue(), sField.GetBinaryValue());
                    Assert.AreEqual(fld.GetStringValue(), sField.GetStringValue());
                }
                else
                {
                    Assert.AreEqual(fld.GetNumericValue(), sField.GetNumericValue());
                }
            }
            reader.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        // [Test, Timeout(300000)] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestEmptyDocs()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwConf.SetMaxBufferedDocs(RandomInts.NextIntBetween(Random(), 2, 30));
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);

            // make sure that the fact that documents might be empty is not a problem
            Document emptyDoc = new Document();
            int numDocs = Random().NextBoolean() ? 1 : AtLeast(1000);
            for (int i = 0; i < numDocs; ++i)
            {
                iw.AddDocument(emptyDoc);
            }
            iw.Commit();
            DirectoryReader rd = DirectoryReader.Open(dir);
            for (int i = 0; i < numDocs; ++i)
            {
                Document doc = rd.Document(i);
                Assert.IsNotNull(doc);
                Assert.IsTrue(doc.Fields.Count <= 0);
            }
            rd.Dispose();

            iw.Dispose();
            dir.Dispose();
        }

        // [Test, Timeout(300000)] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestConcurrentReads()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwConf.SetMaxBufferedDocs(RandomInts.NextIntBetween(Random(), 2, 30));
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);

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

            DirectoryReader rd = DirectoryReader.Open(dir);
            IndexSearcher searcher = new IndexSearcher(rd);
            int concurrentReads = AtLeast(5);
            int readsPerThread = AtLeast(50);
            IList<ThreadClass> readThreads = new List<ThreadClass>();
            AtomicObject<Exception> ex = new AtomicObject<Exception>();
            for (int i = 0; i < concurrentReads; ++i)
            {
                readThreads.Add(new ThreadAnonymousInnerClassHelper(numDocs, rd, searcher, readsPerThread, ex, i));
            }
            foreach (ThreadClass thread in readThreads)
            {
                thread.Start();
            }
            foreach (ThreadClass thread in readThreads)
            {
                thread.Join();
            }
            rd.Dispose();
            if (ex.Value != null)
            {
                throw ex.Value;
            }

            iw.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private int NumDocs;
            private readonly DirectoryReader Rd;
            private readonly IndexSearcher Searcher;
            private int ReadsPerThread;
            private AtomicObject<Exception> Ex;
            private int i;
            private readonly int[] queries;

            public ThreadAnonymousInnerClassHelper(int numDocs, DirectoryReader rd, IndexSearcher searcher, int readsPerThread, AtomicObject<Exception> ex, int i)
            {
                this.NumDocs = numDocs;
                this.Rd = rd;
                this.Searcher = searcher;
                this.ReadsPerThread = readsPerThread;
                this.Ex = ex;
                this.i = i;

                queries = new int[ReadsPerThread];
                for (int j = 0; j < queries.Length; ++j)
                {
                    queries[j] = Random().Next(NumDocs);
                }
            }

            public override void Run()
            {
                foreach (int q in queries)
                {
                    Query query = new TermQuery(new Term("fld", "" + q));
                    try
                    {
                        TopDocs topDocs = Searcher.Search(query, 1);
                        if (topDocs.TotalHits != 1)
                        {
                            Console.WriteLine(query);
                            throw new InvalidOperationException("Expected 1 hit, got " + topDocs.TotalHits);
                        }
                        Document sdoc = Rd.Document(topDocs.ScoreDocs[0].Doc);
                        if (sdoc == null || sdoc.Get("fld") == null)
                        {
                            throw new InvalidOperationException("Could not find document " + q);
                        }
                        if (!Convert.ToString(q).Equals(sdoc.Get("fld")))
                        {
                            throw new InvalidOperationException("Expected " + q + ", but got " + sdoc.Get("fld"));
                        }
                    }
                    catch (Exception e)
                    {
                        Ex.Value = e;
                    }
                }
            }
        }

        private static byte[] RandomByteArray(int length, int max)
        {
            var result = new byte[length];
            for (int i = 0; i < length; ++i)
            {
                result[i] = (byte)Random().Next(max);
            }
            return result;
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestWriteReadMerge()
        {
            // get another codec, other than the default: so we are merging segments across different codecs
            Codec otherCodec;
            /*if ("SimpleText".Equals(Codec.Default.Name))
            {*/
            otherCodec = new Lucene46Codec();
            /*}
            else
            {
              otherCodec = new SimpleTextCodec();
            }*/
            Directory dir = NewDirectory();
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwConf.SetMaxBufferedDocs(RandomInts.NextIntBetween(Random(), 2, 30));
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, (IndexWriterConfig)iwConf.Clone());

            int docCount = AtLeast(200);
            var data = new byte[docCount][][];
            for (int i = 0; i < docCount; ++i)
            {
                int fieldCount = Rarely() ? RandomInts.NextIntBetween(Random(), 1, 500) : RandomInts.NextIntBetween(Random(), 1, 5);
                data[i] = new byte[fieldCount][];
                for (int j = 0; j < fieldCount; ++j)
                {
                    int length = Rarely() ? Random().Next(1000) : Random().Next(10);
                    int max = Rarely() ? 256 : 2;
                    data[i][j] = RandomByteArray(length, max);
                }
            }

            FieldType type = new FieldType(StringField.TYPE_STORED);
            type.IsIndexed = false;
            type.Freeze();
            IntField id = new IntField("id", 0, Field.Store.YES);
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
                iw.w.AddDocument(doc);
                if (Random().NextBoolean() && (i % (data.Length / 10) == 0))
                {
                    iw.w.Dispose();
                    // test merging against a non-compressing codec
                    if (iwConf.Codec == otherCodec)
                    {
                        iwConf.SetCodec(Codec.Default);
                    }
                    else
                    {
                        iwConf.SetCodec(otherCodec);
                    }
                    iw = new RandomIndexWriter(Random(), dir, (IndexWriterConfig)iwConf.Clone());
                }
            }

            for (int i = 0; i < 10; ++i)
            {
                int min = Random().Next(data.Length);
                int max = min + Random().Next(20);
                iw.DeleteDocuments(NumericRangeQuery.NewIntRange("id", min, max, true, false));
            }

            iw.ForceMerge(2); // force merges with deletions

            iw.Commit();

            DirectoryReader ir = DirectoryReader.Open(dir);
            Assert.IsTrue(ir.NumDocs > 0);
            int numDocs = 0;
            for (int i = 0; i < ir.MaxDoc; ++i)
            {
                Document doc = ir.Document(i);
                if (doc == null)
                {
                    continue;
                }
                ++numDocs;
                int docId = (int)doc.GetField("id").GetNumericValue();
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
            ir.Dispose();

            iw.DeleteAll();
            iw.Commit();
            iw.ForceMerge(1);

            iw.Dispose();
            dir.Dispose();
        }

        // [Test, LongRunningTest, Timeout(int.MaxValue)] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestBigDocuments()
        {
            // "big" as "much bigger than the chunk size"
            // for this test we force a FS dir
            // we can't just use newFSDirectory, because this test doesn't really index anything.
            // so if we get NRTCachingDir+SimpleText, we make massive stored fields and OOM (LUCENE-4484)
            Directory dir = new MockDirectoryWrapper(Random(), new MMapDirectory(CreateTempDir("testBigDocuments")));
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwConf.SetMaxBufferedDocs(RandomInts.NextIntBetween(Random(), 2, 30));
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);

            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling_e.NEVER;
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

            Field smallField = new Field("fld", RandomByteArray(Random().Next(10), 256), onlyStored);
            int numFields = RandomInts.NextIntBetween(Random(), 500000, 1000000);
            for (int i = 0; i < numFields; ++i)
            {
                bigDoc1.Add(smallField);
            }

            Field bigField = new Field("fld", RandomByteArray(RandomInts.NextIntBetween(Random(), 1000000, 5000000), 2), onlyStored);
            bigDoc2.Add(bigField);

            int numDocs = AtLeast(5);
            Document[] docs = new Document[numDocs];
            for (int i = 0; i < numDocs; ++i)
            {
                docs[i] = RandomInts.RandomFrom(Random(), Arrays.AsList(emptyDoc, bigDoc1, bigDoc2));
            }
            for (int i = 0; i < numDocs; ++i)
            {
                idField.SetStringValue("" + i);
                iw.AddDocument(docs[i]);
                if (Random().Next(numDocs) == 0)
                {
                    iw.Commit();
                }
            }
            iw.Commit();
            iw.ForceMerge(1); // look at what happens when big docs are merged
            DirectoryReader rd = DirectoryReader.Open(dir);
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
            rd.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestBulkMergeWithDeletes()
        {
            int numDocs = AtLeast(200);
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
            for (int i = 0; i < numDocs; ++i)
            {
                Document doc = new Document();
                doc.Add(new StringField("id", Convert.ToString(i), Field.Store.YES));
                doc.Add(new StoredField("f", TestUtil.RandomSimpleString(Random())));
                w.AddDocument(doc);
            }
            int deleteCount = TestUtil.NextInt(Random(), 5, numDocs);
            for (int i = 0; i < deleteCount; ++i)
            {
                int id = Random().Next(numDocs);
                w.DeleteDocuments(new Term("id", Convert.ToString(id)));
            }
            w.Commit();
            w.Dispose();
            w = new RandomIndexWriter(Random(), dir, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
            w.ForceMerge(TestUtil.NextInt(Random(), 1, 3));
            w.Commit();
            w.Dispose();
            TestUtil.CheckIndex(dir);
            dir.Dispose();
        }
    }
}