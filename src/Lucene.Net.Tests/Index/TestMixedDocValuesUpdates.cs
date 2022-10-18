using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;
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

    using BinaryDocValuesField = BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using IBits = Lucene.Net.Util.IBits;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NumericDocValuesField = NumericDocValuesField;
    using Store = Field.Store;
    using StringField = StringField;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [SuppressCodecs("Appending", "Lucene3x", "Lucene40", "Lucene41", "Lucene42", "Lucene45")]
    [TestFixture]
    public class TestMixedDocValuesUpdates : LuceneTestCase
    {
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
            int numNDVFields = random.Next(numFields / 2) + 1; // 1-3
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
                        if (f < numNDVFields)
                        {
                            doc.Add(new NumericDocValuesField("f" + f, fieldValues[f]));
                        }
                        else
                        {
                            doc.Add(new BinaryDocValuesField("f" + f, TestBinaryDocValuesUpdates.ToBytes(fieldValues[f])));
                        }
                    }
                    writer.AddDocument(doc);
                    ++docID;
                }

                // if field's value was unset before, unset it from all new added documents too
                for (int field = 0; field < fieldHasValue.Length; field++)
                {
                    if (!fieldHasValue[field])
                    {
                        if (field < numNDVFields)
                        {
                            writer.UpdateNumericDocValue(new Term("key", "all"), "f" + field, null);
                        }
                        else
                        {
                            writer.UpdateBinaryDocValue(new Term("key", "all"), "f" + field, null);
                        }
                    }
                }

                int fieldIdx = random.Next(fieldValues.Length);
                string updateField = "f" + fieldIdx;
                if (random.NextBoolean())
                {
                    //        System.out.println("[" + Thread.currentThread().getName() + "]: unset field '" + updateField + "'");
                    fieldHasValue[fieldIdx] = false;
                    if (fieldIdx < numNDVFields)
                    {
                        writer.UpdateNumericDocValue(new Term("key", "all"), updateField, null);
                    }
                    else
                    {
                        writer.UpdateBinaryDocValue(new Term("key", "all"), updateField, null);
                    }
                }
                else
                {
                    fieldHasValue[fieldIdx] = true;
                    if (fieldIdx < numNDVFields)
                    {
                        writer.UpdateNumericDocValue(new Term("key", "all"), updateField, ++fieldValues[fieldIdx]);
                    }
                    else
                    {
                        writer.UpdateBinaryDocValue(new Term("key", "all"), updateField, TestBinaryDocValuesUpdates.ToBytes(++fieldValues[fieldIdx]));
                    }
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
                        NumericDocValues ndv = r.GetNumericDocValues(f);
                        IBits docsWithField = r.GetDocsWithField(f);
                        if (field < numNDVFields)
                        {
                            Assert.IsNotNull(ndv);
                            Assert.IsNull(bdv);
                        }
                        else
                        {
                            Assert.IsNull(ndv);
                            Assert.IsNotNull(bdv);
                        }
                        int maxDoc = r.MaxDoc;
                        for (int doc = 0; doc < maxDoc; doc++)
                        {
                            if (liveDocs is null || liveDocs.Get(doc))
                            {
                                //              System.out.println("doc=" + (doc + context.docBase) + " f='" + f + "' vslue=" + getValue(bdv, doc, scratch));
                                if (fieldHasValue[field])
                                {
                                    Assert.IsTrue(docsWithField.Get(doc));
                                    if (field < numNDVFields)
                                    {
                                        Assert.AreEqual(fieldValues[field], ndv.Get(doc), "invalid value for doc=" + doc + ", field=" + f + ", reader=" + r);
                                    }
                                    else
                                    {
                                        Assert.AreEqual(fieldValues[field], TestBinaryDocValuesUpdates.GetValue(bdv, doc, scratch), "invalid value for doc=" + doc + ", field=" + f + ", reader=" + r);
                                    }
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
                    doc.Add(new BinaryDocValuesField("f" + j, TestBinaryDocValuesUpdates.ToBytes(value)));
                    doc.Add(new NumericDocValuesField("cf" + j, value * 2)); // control, always updated to f * 2
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
                    NumericDocValues control = r.GetNumericDocValues("cf" + i);
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
                                long ctrlValue = control.Get(j);
                                long bdvValue = TestBinaryDocValuesUpdates.GetValue(bdv, j, scratch) * 2;
                                //              if (ctrlValue != bdvValue) {
                                //                System.out.println("seg=" + r + ", f=f" + i + ", doc=" + j + ", group=" + r.Document(j).Get("updKey") + ", ctrlValue=" + ctrlValue + ", bdvBytes=" + scratch);
                                //              }
                                Assert.AreEqual(ctrlValue, bdvValue);
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
            private readonly TestMixedDocValuesUpdates outerInstance;

            private readonly IndexWriter writer;
            private readonly int numDocs;
            private readonly CountdownEvent done;
            private readonly AtomicInt32 numUpdates;
            private readonly string f;
            private readonly string cf;

            public ThreadAnonymousClass(TestMixedDocValuesUpdates outerInstance, string str, IndexWriter writer, int numDocs, CountdownEvent done, AtomicInt32 numUpdates, string f, string cf)
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
                            //                System.err.println("[" + Thread.currentThread().getName() + "] t=" + t + ", f=" + f + ", updValue=UNSET");
                            writer.UpdateBinaryDocValue(t, f, null);
                            writer.UpdateNumericDocValue(t, cf, null);
                        }
                        else
                        {
                            long updValue = random.Next();
                            //                System.err.println("[" + Thread.currentThread().getName() + "] t=" + t + ", f=" + f + ", updValue=" + updValue);
                            writer.UpdateBinaryDocValue(t, f, TestBinaryDocValuesUpdates.ToBytes(updValue));
                            writer.UpdateNumericDocValue(t, cf, updValue * 2);
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
                doc.Add(new BinaryDocValuesField("f", TestBinaryDocValuesUpdates.ToBytes(value)));
                doc.Add(new NumericDocValuesField("cf", value * 2));
                writer.AddDocument(doc);
            }

            int numGens = AtLeast(5);
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < numGens; i++)
            {
                int doc = Random.Next(numDocs);
                Term t = new Term("id", "doc" + doc);
                long value = Random.NextInt64();
                writer.UpdateBinaryDocValue(t, "f", TestBinaryDocValuesUpdates.ToBytes(value));
                writer.UpdateNumericDocValue(t, "cf", value * 2);
                DirectoryReader reader = DirectoryReader.Open(writer, true);
                foreach (AtomicReaderContext context in reader.Leaves)
                {
                    AtomicReader r = context.AtomicReader;
                    BinaryDocValues fbdv = r.GetBinaryDocValues("f");
                    NumericDocValues cfndv = r.GetNumericDocValues("cf");
                    for (int j = 0; j < r.MaxDoc; j++)
                    {
                        Assert.AreEqual(cfndv.Get(j), TestBinaryDocValuesUpdates.GetValue(fbdv, j, scratch) * 2);
                    }
                }
                reader.Dispose();
            }
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
                    doc.Add(new BinaryDocValuesField("f" + j, TestBinaryDocValuesUpdates.ToBytes(val)));
                    doc.Add(new NumericDocValuesField("cf" + j, val * 2));
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
                writer.UpdateBinaryDocValue(updateTerm, "f" + field, TestBinaryDocValuesUpdates.ToBytes(value));
                writer.UpdateNumericDocValue(updateTerm, "cf" + field, value * 2);
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
                    NumericDocValues cf = r.GetNumericDocValues("cf" + i);
                    for (int j = 0; j < r.MaxDoc; j++)
                    {
                        Assert.AreEqual(cf.Get(j), TestBinaryDocValuesUpdates.GetValue(f, j, scratch) * 2, "reader=" + r + ", field=f" + i + ", doc=" + j);
                    }
                }
            }
            reader.Dispose();

            dir.Dispose();
        }
    }
}