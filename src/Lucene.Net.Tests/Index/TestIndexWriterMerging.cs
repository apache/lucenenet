using J2N.Threading;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TextField = TextField;

    [TestFixture]
    public class TestIndexWriterMerging : LuceneTestCase
    {
        /// <summary>
        /// Tests that index merging (specifically addIndexes(Directory...)) doesn't
        /// change the index order of documents.
        /// </summary>
        [Test]
        public virtual void TestLucene()
        {
            int num = 100;

            Directory indexA = NewDirectory();
            Directory indexB = NewDirectory();

            FillIndex(Random, indexA, 0, num);
            bool fail = VerifyIndex(indexA, 0);
            if (fail)
            {
                Assert.Fail("Index a is invalid");
            }

            FillIndex(Random, indexB, num, num);
            fail = VerifyIndex(indexB, num);
            if (fail)
            {
                Assert.Fail("Index b is invalid");
            }

            Directory merged = NewDirectory();

            IndexWriter writer = new IndexWriter(merged, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy(2)));
            writer.AddIndexes(indexA, indexB);
            writer.ForceMerge(1);
            writer.Dispose();

            fail = VerifyIndex(merged, 0);

            Assert.IsFalse(fail, "The merged index is invalid");
            indexA.Dispose();
            indexB.Dispose();
            merged.Dispose();
        }

        private bool VerifyIndex(Directory directory, int startAt)
        {
            bool fail = false;
            IndexReader reader = DirectoryReader.Open(directory);

            int max = reader.MaxDoc;
            for (int i = 0; i < max; i++)
            {
                Document temp = reader.Document(i);
                //System.out.println("doc "+i+"="+temp.GetField("count").StringValue);
                //compare the index doc number to the value that it should be
                if (!temp.GetField("count").GetStringValue().Equals((i + startAt) + "", StringComparison.Ordinal))
                {
                    fail = true;
                    Console.WriteLine("Document " + (i + startAt) + " is returning document " + temp.GetField("count").GetStringValue());
                }
            }
            reader.Dispose();
            return fail;
        }

        private void FillIndex(Random random, Directory dir, int start, int numDocs)
        {
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(2)));

            for (int i = start; i < (start + numDocs); i++)
            {
                Document temp = new Document();
                temp.Add(NewStringField("count", ("" + i), Field.Store.YES));

                writer.AddDocument(temp);
            }
            writer.Dispose();
        }

        // LUCENE-325: test forceMergeDeletes, when 2 singular merges
        // are required
        [Test]
        public virtual void TestForceMergeDeletes()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH));
            Document document = new Document();

            FieldType customType = new FieldType();
            customType.IsStored = true;

            FieldType customType1 = new FieldType(TextField.TYPE_NOT_STORED);
            customType1.IsTokenized = false;
            customType1.StoreTermVectors = true;
            customType1.StoreTermVectorPositions = true;
            customType1.StoreTermVectorOffsets = true;

            Field idField = NewStringField("id", "", Field.Store.NO);
            document.Add(idField);
            Field storedField = NewField("stored", "stored", customType);
            document.Add(storedField);
            Field termVectorField = NewField("termVector", "termVector", customType1);
            document.Add(termVectorField);
            for (int i = 0; i < 10; i++)
            {
                idField.SetStringValue("" + i);
                writer.AddDocument(document);
            }
            writer.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            Assert.AreEqual(10, ir.MaxDoc);
            Assert.AreEqual(10, ir.NumDocs);
            ir.Dispose();

            IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            writer = new IndexWriter(dir, dontMergeConfig);
            writer.DeleteDocuments(new Term("id", "0"));
            writer.DeleteDocuments(new Term("id", "7"));
            writer.Dispose();

            ir = DirectoryReader.Open(dir);
            Assert.AreEqual(8, ir.NumDocs);
            ir.Dispose();

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            Assert.AreEqual(8, writer.NumDocs);
            Assert.AreEqual(10, writer.MaxDoc);
            writer.ForceMergeDeletes();
            Assert.AreEqual(8, writer.NumDocs);
            writer.Dispose();
            ir = DirectoryReader.Open(dir);
            Assert.AreEqual(8, ir.MaxDoc);
            Assert.AreEqual(8, ir.NumDocs);
            ir.Dispose();
            dir.Dispose();
        }

        // LUCENE-325: test forceMergeDeletes, when many adjacent merges are required
        [Test]
        public virtual void TestForceMergeDeletes2()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).SetMergePolicy(NewLogMergePolicy(50)));

            Document document = new Document();

            FieldType customType = new FieldType();
            customType.IsStored = true;

            FieldType customType1 = new FieldType(TextField.TYPE_NOT_STORED);
            customType1.IsTokenized = false;
            customType1.StoreTermVectors = true;
            customType1.StoreTermVectorPositions = true;
            customType1.StoreTermVectorOffsets = true;

            Field storedField = NewField("stored", "stored", customType);
            document.Add(storedField);
            Field termVectorField = NewField("termVector", "termVector", customType1);
            document.Add(termVectorField);
            Field idField = NewStringField("id", "", Field.Store.NO);
            document.Add(idField);
            for (int i = 0; i < 98; i++)
            {
                idField.SetStringValue("" + i);
                writer.AddDocument(document);
            }
            writer.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            Assert.AreEqual(98, ir.MaxDoc);
            Assert.AreEqual(98, ir.NumDocs);
            ir.Dispose();

            IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            writer = new IndexWriter(dir, dontMergeConfig);
            for (int i = 0; i < 98; i += 2)
            {
                writer.DeleteDocuments(new Term("id", "" + i));
            }
            writer.Dispose();

            ir = DirectoryReader.Open(dir);
            Assert.AreEqual(49, ir.NumDocs);
            ir.Dispose();

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy(3)));
            Assert.AreEqual(49, writer.NumDocs);
            writer.ForceMergeDeletes();
            writer.Dispose();
            ir = DirectoryReader.Open(dir);
            Assert.AreEqual(49, ir.MaxDoc);
            Assert.AreEqual(49, ir.NumDocs);
            ir.Dispose();
            dir.Dispose();
        }

        // LUCENE-325: test forceMergeDeletes without waiting, when
        // many adjacent merges are required
        [Test]
        [Slow]
        public virtual void TestForceMergeDeletes3()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).SetMergePolicy(NewLogMergePolicy(50)));

            FieldType customType = new FieldType();
            customType.IsStored = true;

            FieldType customType1 = new FieldType(TextField.TYPE_NOT_STORED);
            customType1.IsTokenized = false;
            customType1.StoreTermVectors = true;
            customType1.StoreTermVectorPositions = true;
            customType1.StoreTermVectorOffsets = true;

            Document document = new Document();
            Field storedField = NewField("stored", "stored", customType);
            document.Add(storedField);
            Field termVectorField = NewField("termVector", "termVector", customType1);
            document.Add(termVectorField);
            Field idField = NewStringField("id", "", Field.Store.NO);
            document.Add(idField);
            for (int i = 0; i < 98; i++)
            {
                idField.SetStringValue("" + i);
                writer.AddDocument(document);
            }
            writer.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            Assert.AreEqual(98, ir.MaxDoc);
            Assert.AreEqual(98, ir.NumDocs);
            ir.Dispose();

            IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            writer = new IndexWriter(dir, dontMergeConfig);
            for (int i = 0; i < 98; i += 2)
            {
                writer.DeleteDocuments(new Term("id", "" + i));
            }
            writer.Dispose();
            ir = DirectoryReader.Open(dir);
            Assert.AreEqual(49, ir.NumDocs);
            ir.Dispose();

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy(3)));
            writer.ForceMergeDeletes(false);
            writer.Dispose();
            ir = DirectoryReader.Open(dir);
            Assert.AreEqual(49, ir.MaxDoc);
            Assert.AreEqual(49, ir.NumDocs);
            ir.Dispose();
            dir.Dispose();
        }

        // Just intercepts all merges & verifies that we are never
        // merging a segment with >= 20 (maxMergeDocs) docs
        private class MyMergeScheduler : MergeScheduler
        {
            private readonly TestIndexWriterMerging outerInstance;

            public MyMergeScheduler(TestIndexWriterMerging outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    while (true)
                    {
                        MergePolicy.OneMerge merge = writer.NextMerge();
                        if (merge is null)
                        {
                            break;
                        }
                        for (int i = 0; i < merge.Segments.Count; i++)
                        {
                            if (Debugging.AssertsEnabled) Debugging.Assert(merge.Segments[i].Info.DocCount < 20);
                        }
                        writer.Merge(merge);
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            protected override void Dispose(bool disposing)
            {
            }
        }

        // LUCENE-1013
        [Test]
        public virtual void TestSetMaxMergeDocs()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(new MyMergeScheduler(this)).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy());
            LogMergePolicy lmp = (LogMergePolicy)conf.MergePolicy;
            lmp.MaxMergeDocs = 20;
            lmp.MergeFactor = 2;
            IndexWriter iw = new IndexWriter(dir, conf);
            Document document = new Document();

            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;

            document.Add(NewField("tvtest", "a b c", customType));
            for (int i = 0; i < 177; i++)
            {
                iw.AddDocument(document);
            }
            iw.Dispose();
            dir.Dispose();
        }

        [Test]
        [Slow]
        public virtual void TestNoWaitClose()
        {
            Directory directory = NewDirectory();

            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.IsTokenized = false;

            Field idField = NewField("id", "", customType);
            doc.Add(idField);

            for (int pass = 0; pass < 2; pass++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: pass=" + pass);
                }

                IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy());
                if (pass == 2)
                {
                    conf.SetMergeScheduler(new SerialMergeScheduler());
                }

                IndexWriter writer = new IndexWriter(directory, conf);
                ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = 100;

                for (int iter = 0; iter < 10; iter++)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: iter=" + iter);
                    }
                    for (int j = 0; j < 199; j++)
                    {
                        idField.SetStringValue(Convert.ToString(iter * 201 + j));
                        writer.AddDocument(doc);
                    }

                    int delID = iter * 199;
                    for (int j = 0; j < 20; j++)
                    {
                        writer.DeleteDocuments(new Term("id", Convert.ToString(delID)));
                        delID += 5;
                    }

                    // Force a bunch of merge threads to kick off so we
                    // stress out aborting them on close:
                    ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = 2;

                    IndexWriter finalWriter = writer;
                    IList<Exception> failure = new JCG.List<Exception>();
                    ThreadJob t1 = new ThreadAnonymousClass(this, doc, finalWriter, failure);

                    if (failure.Count > 0)
                    {
                        ExceptionDispatchInfo.Capture(failure[0]).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                    }

                    t1.Start();

                    writer.Dispose(false);
                    t1.Join();

                    // Make sure reader can read
                    IndexReader reader = DirectoryReader.Open(directory);
                    reader.Dispose();

                    // Reopen
                    writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy()));
                }
                writer.Dispose();
            }

            directory.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestIndexWriterMerging outerInstance;

            private Document doc;
            private IndexWriter finalWriter;
            private IList<Exception> failure;

            public ThreadAnonymousClass(TestIndexWriterMerging outerInstance, Document doc, IndexWriter finalWriter, IList<Exception> failure)
            {
                this.outerInstance = outerInstance;
                this.doc = doc;
                this.finalWriter = finalWriter;
                this.failure = failure;
            }

            public override void Run()
            {
                bool done = false;
                while (!done)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        try
                        {
                            finalWriter.AddDocument(doc);
                        }
                        catch (Exception e) when (e.IsAlreadyClosedException())
                        {
                            done = true;
                            break;
                        }
#pragma warning disable 168
                        catch (NullReferenceException e) // LUCENENET TODO: We should fix the components so this cannot occur (assuming it can).
#pragma warning restore 168
                        {
                            done = true;
                            break;
                        }
                        catch (Exception e) when (e.IsThrowable())
                        {
                            Console.WriteLine(e.StackTrace);
                            failure.Add(e);
                            done = true;
                            break;
                        }
                    }
                    Thread.Yield();
                }
            }
        }
    }
}