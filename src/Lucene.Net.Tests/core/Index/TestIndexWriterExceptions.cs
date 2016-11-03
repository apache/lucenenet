using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System.IO;
    using Util;
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
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using BinaryDocValuesField = BinaryDocValuesField;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using NumericDocValuesField = NumericDocValuesField;
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using PhraseQuery = Lucene.Net.Search.PhraseQuery;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using SortedDocValuesField = SortedDocValuesField;
    using SortedSetDocValuesField = SortedSetDocValuesField;
    using StringField = StringField;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;
    using Token = Lucene.Net.Analysis.Token;
    using TokenFilter = Lucene.Net.Analysis.TokenFilter;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    [TestFixture]
    public class TestIndexWriterExceptions : LuceneTestCase
    {
        private class DocCopyIterator : IEnumerable<Document>
        {
            internal readonly Document Doc;
            internal readonly int Count;

            /* private field types */
            /* private field types */

            internal static readonly FieldType Custom1 = new FieldType(TextField.TYPE_NOT_STORED);
            internal static readonly FieldType Custom2 = new FieldType();
            internal static readonly FieldType Custom3 = new FieldType();
            internal static readonly FieldType Custom4 = new FieldType(StringField.TYPE_NOT_STORED);
            internal static readonly FieldType Custom5 = new FieldType(TextField.TYPE_STORED);

            static DocCopyIterator()
            {
                Custom1.StoreTermVectors = true;
                Custom1.StoreTermVectorPositions = true;
                Custom1.StoreTermVectorOffsets = true;

                Custom2.Stored = true;
                Custom2.Indexed = true;

                Custom3.Stored = true;

                Custom4.StoreTermVectors = true;
                Custom4.StoreTermVectorPositions = true;
                Custom4.StoreTermVectorOffsets = true;

                Custom5.StoreTermVectors = true;
                Custom5.StoreTermVectorPositions = true;
                Custom5.StoreTermVectorOffsets = true;
            }

            public DocCopyIterator(Document doc, int count)
            {
                this.Count = count;
                this.Doc = doc;
            }

            public virtual IEnumerator<Document> GetEnumerator()
            {
                return new IteratorAnonymousInnerClassHelper(this);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class IteratorAnonymousInnerClassHelper : IEnumerator<Document>
            {
                private readonly DocCopyIterator OuterInstance;

                public IteratorAnonymousInnerClassHelper(DocCopyIterator outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                internal int upto;
                private Document current;

                public bool MoveNext()
                {
                    if (upto >= OuterInstance.Count)
                    {
                        return false;
                    }

                    upto++;
                    current = OuterInstance.Doc;
                    return true;
                }

                public Document Current
                {
                    get { return current; }
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }

                public void Dispose()
                {
                }
            }
        }

        private class IndexerThread : ThreadClass
        {
            private DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            private readonly TestIndexWriterExceptions OuterInstance;

            internal IndexWriter Writer;

            internal readonly Random r = new Random(Random().Next());
            internal volatile Exception Failure = null;

            public IndexerThread(TestIndexWriterExceptions outerInstance, int i, IndexWriter writer)
            {
                this.OuterInstance = outerInstance;
                Name = "Indexer " + i;
                this.Writer = writer;
            }

            public override void Run()
            {
                Document doc = new Document();

                doc.Add(OuterInstance.NewTextField(r, "content1", "aaa bbb ccc ddd", Field.Store.YES));
                doc.Add(OuterInstance.NewField(r, "content6", "aaa bbb ccc ddd", DocCopyIterator.Custom1));
                doc.Add(OuterInstance.NewField(r, "content2", "aaa bbb ccc ddd", DocCopyIterator.Custom2));
                doc.Add(OuterInstance.NewField(r, "content3", "aaa bbb ccc ddd", DocCopyIterator.Custom3));

                doc.Add(OuterInstance.NewTextField(r, "content4", "aaa bbb ccc ddd", Field.Store.NO));
                doc.Add(OuterInstance.NewStringField(r, "content5", "aaa bbb ccc ddd", Field.Store.NO));
                if (DefaultCodecSupportsDocValues())
                {
                    doc.Add(new NumericDocValuesField("numericdv", 5));
                    doc.Add(new BinaryDocValuesField("binarydv", new BytesRef("hello")));
                    doc.Add(new SortedDocValuesField("sorteddv", new BytesRef("world")));
                }
                if (DefaultCodecSupportsSortedSet())
                {
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("hellllo")));
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("again")));
                }

                doc.Add(OuterInstance.NewField(r, "content7", "aaa bbb ccc ddd", DocCopyIterator.Custom4));

                Field idField = OuterInstance.NewField(r, "id", "", DocCopyIterator.Custom2);
                doc.Add(idField);

                long stopTime = ((long)(DateTime.UtcNow - unixEpoch).TotalMilliseconds) + 500;

                do
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine(Thread.CurrentThread.Name + ": TEST: IndexerThread: cycle");
                    }
                    OuterInstance.DoFail.Value = (this.Instance);
                    string id = "" + r.Next(50);
                    idField.StringValue = id;
                    Term idTerm = new Term("id", id);
                    try
                    {
                        if (r.NextBoolean())
                        {
                            Writer.UpdateDocuments(idTerm, new DocCopyIterator(doc, TestUtil.NextInt(r, 1, 20)));
                        }
                        else
                        {
                            Writer.UpdateDocument(idTerm, doc);
                        }
                    }
                    catch (TestPoint1Exception re)
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": EXC: ");
                            Console.WriteLine(re.StackTrace);
                        }
                        try
                        {
                            TestUtil.CheckIndex(Writer.Directory);
                        }
                        catch (IOException ioe)
                        {
                            Console.WriteLine(Thread.CurrentThread.Name + ": unexpected exception1");
                            Console.WriteLine(ioe.StackTrace);
                            Failure = ioe;
                            break;
                        }
                    }
                    catch (Exception t)
                    {
                        Console.WriteLine(Thread.CurrentThread.Name + ": unexpected exception2");
                        Console.WriteLine(t.StackTrace);
                        Failure = t;
                        break;
                    }

                    OuterInstance.DoFail.Value = (null);

                    // After a possible exception (above) I should be able
                    // to add a new document without hitting an
                    // exception:
                    try
                    {
                        Writer.UpdateDocument(idTerm, doc);
                    }
                    catch (Exception t)
                    {
                        Console.WriteLine(Thread.CurrentThread.Name + ": unexpected exception3");
                        Console.WriteLine(t.StackTrace);
                        Failure = t;
                        break;
                    }
                } while (((long)(DateTime.UtcNow - unixEpoch).TotalMilliseconds) < stopTime);
            }
        }

        internal ThreadLocal<Thread> DoFail = new ThreadLocal<Thread>();

        private class TestPoint1 : RandomIndexWriter.TestPoint
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            public TestPoint1(TestIndexWriterExceptions outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            internal Random r = new Random(Random().Next());

            public void Apply(string name)
            {
                if (OuterInstance.DoFail.Value != null && !name.Equals("startDoFlush") && r.Next(40) == 17)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine(Thread.CurrentThread.Name + ": NOW FAIL: " + name);
                        Console.WriteLine((new Exception()).StackTrace);
                    }
                    throw new TestPoint1Exception(Thread.CurrentThread.Name + ": intentionally failing at " + name);
                }
            }
        }

        private class TestPoint1Exception : Exception
        {
            public TestPoint1Exception(string message) : base(message)
            {
            }
        }

        [Test]
        public virtual void TestRandomExceptions([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            if (VERBOSE)
            {
                Console.WriteLine("\nTEST: start testRandomExceptions");
            }
            Directory dir = NewDirectory();

            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.


            var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer)
                            .SetRAMBufferSizeMB(0.1)
                            .SetMergeScheduler(scheduler);

            scheduler.SetSuppressExceptions();

            IndexWriter writer = RandomIndexWriter.MockIndexWriter(dir, config , new TestPoint1(this));
            //writer.SetMaxBufferedDocs(10);
            if (VERBOSE)
            {
                Console.WriteLine("TEST: initial commit");
            }
            writer.Commit();

            IndexerThread thread = new IndexerThread(this, 0, writer);
            thread.Run();
            if (thread.Failure != null)
            {
                Console.WriteLine(thread.Failure.StackTrace);
                Assert.Fail("thread " + thread.Name + ": hit unexpected failure");
            }

            if (VERBOSE)
            {
                Console.WriteLine("TEST: commit after thread start");
            }
            writer.Commit();

            try
            {
                writer.Dispose();
            }
            catch (Exception t)
            {
                Console.WriteLine("exception during close:");
                Console.WriteLine(t.StackTrace);
                writer.Rollback();
            }

            // Confirm that when doc hits exception partway through tokenization, it's deleted:
            IndexReader r2 = DirectoryReader.Open(dir);
            int count = r2.DocFreq(new Term("content4", "aaa"));
            int count2 = r2.DocFreq(new Term("content4", "ddd"));
            Assert.AreEqual(count, count2);
            r2.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestRandomExceptionsThreads([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.

            var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer)
                            .SetRAMBufferSizeMB(0.2)
                            .SetMergeScheduler(scheduler);

            IndexWriter writer = RandomIndexWriter.MockIndexWriter(dir, config, new TestPoint1(this));
            scheduler.SetSuppressExceptions();

            //writer.SetMaxBufferedDocs(10);
            writer.Commit();

            const int NUM_THREADS = 4;

            IndexerThread[] threads = new IndexerThread[NUM_THREADS];
            for (int i = 0; i < NUM_THREADS; i++)
            {
                threads[i] = new IndexerThread(this, i, writer);
                threads[i].Start();
            }

            for (int i = 0; i < NUM_THREADS; i++)
            {
                threads[i].Join();
            }

            for (int i = 0; i < NUM_THREADS; i++)
            {
                if (threads[i].Failure != null)
                {
                    Assert.Fail("thread " + threads[i].Name + ": hit unexpected failure");
                }
            }

            writer.Commit();

            try
            {
                writer.Dispose();
            }
            catch (Exception t)
            {
                Console.WriteLine("exception during close:");
                Console.WriteLine(t.StackTrace);
                writer.Rollback();
            }

            // Confirm that when doc hits exception partway through tokenization, it's deleted:
            IndexReader r2 = DirectoryReader.Open(dir);
            int count = r2.DocFreq(new Term("content4", "aaa"));
            int count2 = r2.DocFreq(new Term("content4", "ddd"));
            Assert.AreEqual(count, count2);
            r2.Dispose();

            dir.Dispose();
        }

        // LUCENE-1198
        private sealed class TestPoint2 : RandomIndexWriter.TestPoint
        {
            internal bool DoFail;

            public void Apply(string name)
            {
                if (DoFail && name.Equals("DocumentsWriterPerThread addDocument start"))
                {
                    throw new Exception("intentionally failing");
                }
            }
        }

        private static string CRASH_FAIL_MESSAGE = "I'm experiencing problems";

        private class CrashingFilter : TokenFilter
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            internal string FieldName;
            internal int Count;

            public CrashingFilter(TestIndexWriterExceptions outerInstance, string fieldName, TokenStream input)
                : base(input)
            {
                this.OuterInstance = outerInstance;
                this.FieldName = fieldName;
            }

            public sealed override bool IncrementToken()
            {
                if (this.FieldName.Equals("crash") && Count++ >= 4)
                {
                    throw new IOException(CRASH_FAIL_MESSAGE);
                }
                return input.IncrementToken();
            }

            public override void Reset()
            {
                base.Reset();
                Count = 0;
            }
        }

        [Test]
        public virtual void TestExceptionDocumentsWriterInit()
        {
            Directory dir = NewDirectory();
            TestPoint2 testPoint = new TestPoint2();
            IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())), testPoint);
            Document doc = new Document();
            doc.Add(NewTextField("field", "a field", Field.Store.YES));
            w.AddDocument(doc);
            testPoint.DoFail = true;
            try
            {
                w.AddDocument(doc);
                Assert.Fail("did not hit exception");
            }
            catch (Exception re)
            {
                // expected
            }
            w.Dispose();
            dir.Dispose();
        }

        // LUCENE-1208
        [Test]
        public virtual void TestExceptionJustBeforeFlush()
        {
            Directory dir = NewDirectory();
            IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2), new TestPoint1(this));
            Document doc = new Document();
            doc.Add(NewTextField("field", "a field", Field.Store.YES));
            w.AddDocument(doc);

            Analyzer analyzer = new TEJBFAnalyzerAnonymousInnerClassHelper(this, Analyzer.PER_FIELD_REUSE_STRATEGY);

            Document crashDoc = new Document();
            crashDoc.Add(NewTextField("crash", "do it on token 4", Field.Store.YES));
            try
            {
                w.AddDocument(crashDoc, analyzer);
                Assert.Fail("did not hit expected exception");
            }
            catch (IOException ioe)
            {
                // expected
            }
            w.AddDocument(doc);
            w.Dispose();
            dir.Dispose();
        }

        private class TEJBFAnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            public TEJBFAnalyzerAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance, Analyzer.ReuseStrategy PER_FIELD_REUSE_STRATEGY)
                : base(PER_FIELD_REUSE_STRATEGY)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
                return new TokenStreamComponents(tokenizer, new CrashingFilter(OuterInstance, fieldName, tokenizer));
            }
        }

        private sealed class TestPoint3 : RandomIndexWriter.TestPoint
        {
            internal bool DoFail;
            internal bool Failed;

            public void Apply(string name)
            {
                if (DoFail && name.Equals("startMergeInit"))
                {
                    Failed = true;
                    throw new Exception("intentionally failing");
                }
            }
        }

        // LUCENE-1210
        [Test]
        public virtual void TestExceptionOnMergeInit([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy());

            scheduler.SetSuppressExceptions();
            conf.SetMergeScheduler(scheduler);
            ((LogMergePolicy)conf.MergePolicy).MergeFactor = 2;
            TestPoint3 testPoint = new TestPoint3();
            IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, conf, testPoint);
            testPoint.DoFail = true;
            Document doc = new Document();
            doc.Add(NewTextField("field", "a field", Field.Store.YES));
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    w.AddDocument(doc);
                }
                catch (Exception)
                {
                    break;
                }
            }

            ((IConcurrentMergeScheduler)w.Config.MergeScheduler).Sync();
            Assert.IsTrue(testPoint.Failed);
            w.Dispose();
            dir.Dispose();
        }

        // LUCENE-1072
        [Test]
        public virtual void TestExceptionFromTokenStream()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new TEFTSAnalyzerAnonymousInnerClassHelper(this));
            conf.SetMaxBufferedDocs(Math.Max(3, conf.MaxBufferedDocs));

            IndexWriter writer = new IndexWriter(dir, conf);

            Document doc = new Document();
            string contents = "aa bb cc dd ee ff gg hh ii jj kk";
            doc.Add(NewTextField("content", contents, Field.Store.NO));
            try
            {
                writer.AddDocument(doc);
                Assert.Fail("did not hit expected exception");
            }
            catch (Exception e)
            {
            }

            // Make sure we can add another normal document
            doc = new Document();
            doc.Add(NewTextField("content", "aa bb cc dd", Field.Store.NO));
            writer.AddDocument(doc);

            // Make sure we can add another normal document
            doc = new Document();
            doc.Add(NewTextField("content", "aa bb cc dd", Field.Store.NO));
            writer.AddDocument(doc);

            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            Term t = new Term("content", "aa");
            Assert.AreEqual(3, reader.DocFreq(t));

            // Make sure the doc that hit the exception was marked
            // as deleted:
            DocsEnum tdocs = TestUtil.Docs(Random(), reader, t.Field, new BytesRef(t.Text()), MultiFields.GetLiveDocs(reader), null, 0);

            int count = 0;
            while (tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                count++;
            }
            Assert.AreEqual(2, count);

            Assert.AreEqual(reader.DocFreq(new Term("content", "gg")), 0);
            reader.Dispose();
            dir.Dispose();
        }

        private class TEFTSAnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            public TEFTSAnalyzerAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
                return new TokenStreamComponents(tokenizer, new TokenFilterAnonymousInnerClassHelper(this, tokenizer));
            }

            private class TokenFilterAnonymousInnerClassHelper : TokenFilter
            {
                private readonly TEFTSAnalyzerAnonymousInnerClassHelper OuterInstance;

                public TokenFilterAnonymousInnerClassHelper(TEFTSAnalyzerAnonymousInnerClassHelper outerInstance, MockTokenizer tokenizer)
                    : base(tokenizer)
                {
                    this.OuterInstance = outerInstance;
                    count = 0;
                }

                private int count;

                public sealed override bool IncrementToken()
                {
                    if (count++ == 5)
                    {
                        throw new IOException();
                    }
                    return input.IncrementToken();
                }

                public override void Reset()
                {
                    base.Reset();
                    this.count = 0;
                }
            }
        }

        private class FailOnlyOnFlush : MockDirectoryWrapper.Failure
        {
            internal bool DoFail = false;
            internal int Count;

            public override void SetDoFail()
            {
                this.DoFail = true;
            }

            public override void ClearDoFail()
            {
                this.DoFail = false;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (DoFail)
                {
                    bool sawAppend = StackTraceHelper.DoesStackTraceContainMethod(typeof(FreqProxTermsWriterPerField).Name, "Flush");
                    bool sawFlush = StackTraceHelper.DoesStackTraceContainMethod("Flush");

                    if (sawAppend && sawFlush && Count++ >= 30)
                    {
                        DoFail = false;
                        throw new IOException("now failing during flush");
                    }
                }
            }
        }

        // LUCENE-1072: make sure an errant exception on flushing
        // one segment only takes out those docs in that one flush
        [Test]
        public virtual void TestDocumentsWriterAbort()
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            FailOnlyOnFlush failure = new FailOnlyOnFlush();
            failure.SetDoFail();
            dir.FailOn(failure);

            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2));
            Document doc = new Document();
            string contents = "aa bb cc dd ee ff gg hh ii jj kk";
            doc.Add(NewTextField("content", contents, Field.Store.NO));
            bool hitError = false;
            for (int i = 0; i < 200; i++)
            {
                try
                {
                    writer.AddDocument(doc);
                }
                catch (IOException ioe)
                {
                    // only one flush should fail:
                    Assert.IsFalse(hitError);
                    hitError = true;
                }
            }
            Assert.IsTrue(hitError);
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(198, reader.DocFreq(new Term("content", "aa")));
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDocumentsWriterExceptions()
        {
            Analyzer analyzer = new TDWEAnalyzerAnonymousInnerClassHelper(this, Analyzer.PER_FIELD_REUSE_STRATEGY);

            for (int i = 0; i < 2; i++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: cycle i=" + i);
                }
                Directory dir = NewDirectory();
                IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMergePolicy(NewLogMergePolicy()));

                // don't allow a sudden merge to clean up the deleted
                // doc below:
                LogMergePolicy lmp = (LogMergePolicy)writer.Config.MergePolicy;
                lmp.MergeFactor = Math.Max(lmp.MergeFactor, 5);

                Document doc = new Document();
                doc.Add(NewField("contents", "here are some contents", DocCopyIterator.Custom5));
                writer.AddDocument(doc);
                writer.AddDocument(doc);
                doc.Add(NewField("crash", "this should crash after 4 terms", DocCopyIterator.Custom5));
                doc.Add(NewField("other", "this will not get indexed", DocCopyIterator.Custom5));
                try
                {
                    writer.AddDocument(doc);
                    Assert.Fail("did not hit expected exception");
                }
                catch (IOException ioe)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: hit expected exception");
                        Console.WriteLine(ioe.StackTrace);
                    }
                }

                if (0 == i)
                {
                    doc = new Document();
                    doc.Add(NewField("contents", "here are some contents", DocCopyIterator.Custom5));
                    writer.AddDocument(doc);
                    writer.AddDocument(doc);
                }
                writer.Dispose();

                if (VERBOSE)
                {
                    Console.WriteLine("TEST: open reader");
                }
                IndexReader reader = DirectoryReader.Open(dir);
                if (i == 0)
                {
                    int expected = 5;
                    Assert.AreEqual(expected, reader.DocFreq(new Term("contents", "here")));
                    Assert.AreEqual(expected, reader.MaxDoc);
                    int numDel = 0;
                    Bits liveDocs = MultiFields.GetLiveDocs(reader);
                    Assert.IsNotNull(liveDocs);
                    for (int j = 0; j < reader.MaxDoc; j++)
                    {
                        if (!liveDocs.Get(j))
                        {
                            numDel++;
                        }
                        else
                        {
                            reader.Document(j);
                            reader.GetTermVectors(j);
                        }
                    }
                    Assert.AreEqual(1, numDel);
                }
                reader.Dispose();

                writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(10));
                doc = new Document();
                doc.Add(NewField("contents", "here are some contents", DocCopyIterator.Custom5));
                for (int j = 0; j < 17; j++)
                {
                    writer.AddDocument(doc);
                }
                writer.ForceMerge(1);
                writer.Dispose();

                reader = DirectoryReader.Open(dir);
                int expected_ = 19 + (1 - i) * 2;
                Assert.AreEqual(expected_, reader.DocFreq(new Term("contents", "here")));
                Assert.AreEqual(expected_, reader.MaxDoc);
                int numDel_ = 0;
                Assert.IsNull(MultiFields.GetLiveDocs(reader));
                for (int j = 0; j < reader.MaxDoc; j++)
                {
                    reader.Document(j);
                    reader.GetTermVectors(j);
                }
                reader.Dispose();
                Assert.AreEqual(0, numDel_);

                dir.Dispose();
            }
        }

        private class TDWEAnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            public TDWEAnalyzerAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance, Analyzer.ReuseStrategy PER_FIELD_REUSE_STRATEGY)
                : base(PER_FIELD_REUSE_STRATEGY)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
                return new TokenStreamComponents(tokenizer, new CrashingFilter(OuterInstance, fieldName, tokenizer));
            }
        }

        [Test]
        public virtual void TestDocumentsWriterExceptionThreads()
        {
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper2(this, Analyzer.PER_FIELD_REUSE_STRATEGY);

            const int NUM_THREAD = 3;
            const int NUM_ITER = 100;

            for (int i = 0; i < 2; i++)
            {
                Directory dir = NewDirectory();

                {
                    IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(-1).SetMergePolicy(Random().NextBoolean() ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy.NO_COMPOUND_FILES));
                    // don't use a merge policy here they depend on the DWPThreadPool and its max thread states etc.
                    int finalI = i;

                    ThreadClass[] threads = new ThreadClass[NUM_THREAD];
                    for (int t = 0; t < NUM_THREAD; t++)
                    {
                        threads[t] = new ThreadAnonymousInnerClassHelper(this, NUM_ITER, writer, finalI, t);
                        threads[t].Start();
                    }

                    for (int t = 0; t < NUM_THREAD; t++)
                    {
                        threads[t].Join();
                    }

                    writer.Dispose();
                }

                IndexReader reader = DirectoryReader.Open(dir);
                int expected = (3 + (1 - i) * 2) * NUM_THREAD * NUM_ITER;
                Assert.AreEqual(expected, reader.DocFreq(new Term("contents", "here")), "i=" + i);
                Assert.AreEqual(expected, reader.MaxDoc);
                int numDel = 0;
                Bits liveDocs = MultiFields.GetLiveDocs(reader);
                Assert.IsNotNull(liveDocs);
                for (int j = 0; j < reader.MaxDoc; j++)
                {
                    if (!liveDocs.Get(j))
                    {
                        numDel++;
                    }
                    else
                    {
                        reader.Document(j);
                        reader.GetTermVectors(j);
                    }
                }
                reader.Dispose();

                Assert.AreEqual(NUM_THREAD * NUM_ITER, numDel);

                IndexWriter indWriter = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(10));
                Document doc = new Document();
                doc.Add(NewField("contents", "here are some contents", DocCopyIterator.Custom5));
                for (int j = 0; j < 17; j++)
                {
                    indWriter.AddDocument(doc);
                }
                indWriter.ForceMerge(1);
                indWriter.Dispose();

                reader = DirectoryReader.Open(dir);
                expected += 17 - NUM_THREAD * NUM_ITER;
                Assert.AreEqual(expected, reader.DocFreq(new Term("contents", "here")));
                Assert.AreEqual(expected, reader.MaxDoc);
                Assert.IsNull(MultiFields.GetLiveDocs(reader));
                for (int j = 0; j < reader.MaxDoc; j++)
                {
                    reader.Document(j);
                    reader.GetTermVectors(j);
                }
                reader.Dispose();

                dir.Dispose();
            }
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            public AnalyzerAnonymousInnerClassHelper2(TestIndexWriterExceptions outerInstance, Analyzer.ReuseStrategy PER_FIELD_REUSE_STRATEGY)
                : base(PER_FIELD_REUSE_STRATEGY)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
                return new TokenStreamComponents(tokenizer, new CrashingFilter(OuterInstance, fieldName, tokenizer));
            }
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            private int NUM_ITER;
            private IndexWriter Writer;
            private int FinalI;
            private int t;

            public ThreadAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance, int NUM_ITER, IndexWriter writer, int finalI, int t)
            {
                this.OuterInstance = outerInstance;
                this.NUM_ITER = NUM_ITER;
                this.Writer = writer;
                this.FinalI = finalI;
                this.t = t;
            }

            public override void Run()
            {
                try
                {
                    for (int iter = 0; iter < NUM_ITER; iter++)
                    {
                        Document doc = new Document();
                        doc.Add(OuterInstance.NewField("contents", "here are some contents", DocCopyIterator.Custom5));
                        Writer.AddDocument(doc);
                        Writer.AddDocument(doc);
                        doc.Add(OuterInstance.NewField("crash", "this should crash after 4 terms", DocCopyIterator.Custom5));
                        doc.Add(OuterInstance.NewField("other", "this will not get indexed", DocCopyIterator.Custom5));
                        try
                        {
                            Writer.AddDocument(doc);
                            Assert.Fail("did not hit expected exception");
                        }
                        catch (IOException ioe)
                        {
                        }

                        if (0 == FinalI)
                        {
                            doc = new Document();
                            doc.Add(OuterInstance.NewField("contents", "here are some contents", DocCopyIterator.Custom5));
                            Writer.AddDocument(doc);
                            Writer.AddDocument(doc);
                        }
                    }
                }
                catch (Exception t)
                {
                    lock (this)
                    {
                        Console.WriteLine(Thread.CurrentThread.Name + ": ERROR: hit unexpected exception");
                        Console.WriteLine(t.StackTrace);
                    }
                    Assert.Fail();
                }
            }
        }

        // Throws IOException during MockDirectoryWrapper.sync
        private class FailOnlyInSync : MockDirectoryWrapper.Failure
        {
            internal bool DidFail;

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (DoFail)
                {
                    bool foundMethod =
                        StackTraceHelper.DoesStackTraceContainMethod(typeof(MockDirectoryWrapper).Name, "Sync");

                    if (DoFail && foundMethod)
                    {
                        DidFail = true;
                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: now throw exc:");
                            Console.WriteLine(Environment.StackTrace);
                        }
                        throw new IOException("now failing on purpose during sync");
                    }
                }
            }
        }

        // TODO: these are also in TestIndexWriter... add a simple doc-writing method
        // like this to LuceneTestCase?
        private void AddDoc(IndexWriter writer)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            writer.AddDocument(doc);
        }

        // LUCENE-1044: test exception during sync
        [Test]
        public virtual void TestExceptionDuringSync([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            FailOnlyInSync failure = new FailOnlyInSync();
            dir.FailOn(failure);

            var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                            .SetMaxBufferedDocs(2)
                            .SetMergeScheduler(scheduler)
                            .SetMergePolicy(NewLogMergePolicy(5));

            IndexWriter writer = new IndexWriter(dir, config);
            failure.SetDoFail();

            for (int i = 0; i < 23; i++)
            {
                AddDoc(writer);
                if ((i - 1) % 2 == 0)
                {
                    try
                    {
                        writer.Commit();
                    }
                    catch (IOException ioe)
                    {
                        // expected
                    }
                }
            }
            ((IConcurrentMergeScheduler)writer.Config.MergeScheduler).Sync();
            Assert.IsTrue(failure.DidFail);
            failure.ClearDoFail();
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(23, reader.NumDocs);
            reader.Dispose();
            dir.Dispose();
        }

        private class FailOnlyInCommit : MockDirectoryWrapper.Failure
        {
            internal bool FailOnCommit, FailOnDeleteFile;
            internal readonly bool DontFailDuringGlobalFieldMap;
            internal const string PREPARE_STAGE = "PrepareCommit";
            internal const string FINISH_STAGE = "FinishCommit";
            internal readonly string Stage;

            public FailOnlyInCommit(bool dontFailDuringGlobalFieldMap, string stage)
            {
                this.DontFailDuringGlobalFieldMap = dontFailDuringGlobalFieldMap;
                this.Stage = stage;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                bool isCommit = StackTraceHelper.DoesStackTraceContainMethod(typeof(SegmentInfos).Name, Stage);
                bool isDelete = StackTraceHelper.DoesStackTraceContainMethod(typeof(MockDirectoryWrapper).Name, "DeleteFile");
                bool isInGlobalFieldMap = StackTraceHelper.DoesStackTraceContainMethod(typeof(SegmentInfos).Name, "WriteGlobalFieldMap");

                if (isInGlobalFieldMap && DontFailDuringGlobalFieldMap)
                {
                    isCommit = false;
                }
                if (isCommit)
                {
                    if (!isDelete)
                    {
                        FailOnCommit = true;
                        throw new Exception("now fail first");
                    }
                    else
                    {
                        FailOnDeleteFile = true;
                        throw new IOException("now fail during delete");
                    }
                }
            }
        }

        [Test]
        public virtual void TestExceptionsDuringCommit()
        {
            FailOnlyInCommit[] failures = new FailOnlyInCommit[] { new FailOnlyInCommit(false, FailOnlyInCommit.PREPARE_STAGE), new FailOnlyInCommit(true, FailOnlyInCommit.PREPARE_STAGE), new FailOnlyInCommit(false, FailOnlyInCommit.FINISH_STAGE) };

            foreach (FailOnlyInCommit failure in failures)
            {
                MockDirectoryWrapper dir = NewMockDirectory();
                dir.FailOnCreateOutput = false;
                IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
                Document doc = new Document();
                doc.Add(NewTextField("field", "a field", Field.Store.YES));
                w.AddDocument(doc);
                dir.FailOn(failure);
                try
                {
                    w.Dispose();
                    Assert.Fail();
                }
                catch (IOException ioe)
                {
                    Assert.Fail("expected only RuntimeException");
                }
                catch (Exception re)
                {
                    // Expected
                }
                Assert.IsTrue(failure.FailOnCommit && failure.FailOnDeleteFile);
                w.Rollback();
                string[] files = dir.ListAll();
                Assert.IsTrue(files.Length == 0 || Arrays.Equals(files, new string[] { IndexWriter.WRITE_LOCK_NAME }));
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestForceMergeExceptions([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            Directory startDir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy());
            ((LogMergePolicy)conf.MergePolicy).MergeFactor = 100;
            IndexWriter w = new IndexWriter(startDir, conf);
            for (int i = 0; i < 27; i++)
            {
                AddDoc(w);
            }
            w.Dispose();

            int iter = TEST_NIGHTLY ? 200 : 10;
            for (int i = 0; i < iter; i++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: iter " + i);
                }
                MockDirectoryWrapper dir = new MockDirectoryWrapper(Random(), new RAMDirectory(startDir, NewIOContext(Random())));
                conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergeScheduler(scheduler);
                scheduler.SetSuppressExceptions();
                w = new IndexWriter(dir, conf);
                dir.RandomIOExceptionRate = 0.5;
                try
                {
                    w.ForceMerge(1);
                }
                catch (IOException ioe)
                {
                    if (ioe.InnerException == null)
                    {
                        Assert.Fail("forceMerge threw IOException without root cause");
                    }
                }
                dir.RandomIOExceptionRate = 0;
                w.Dispose();
                dir.Dispose();
            }
            startDir.Dispose();
        }

        // LUCENE-1429
        [Test]
        public virtual void TestOutOfMemoryErrorCausesCloseToFail()
        {
            AtomicBoolean thrown = new AtomicBoolean(false);
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetInfoStream(new TOOMInfoStreamAnonymousInnerClassHelper(this, thrown)));

            try
            {
                writer.Dispose();
                Assert.Fail("OutOfMemoryError expected");
            }
            catch (System.OutOfMemoryException expected)
            {
            }

            // throws IllegalStateEx w/o bug fix
            writer.Dispose();
            dir.Dispose();
        }

        private class TOOMInfoStreamAnonymousInnerClassHelper : InfoStream
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            private AtomicBoolean Thrown;

            public TOOMInfoStreamAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance, AtomicBoolean thrown)
            {
                this.OuterInstance = outerInstance;
                this.Thrown = thrown;
            }

            public override void Message(string component, string message)
            {
                if (message.StartsWith("now flush at close") && Thrown.CompareAndSet(false, true))
                {
                    throw new System.OutOfMemoryException("fake OOME at " + message);
                }
            }

            public override bool IsEnabled(string component)
            {
                return true;
            }

            public override void Dispose()
            {
            }
        }

        // LUCENE-1347
        private sealed class TestPoint4 : RandomIndexWriter.TestPoint
        {
            internal bool DoFail;

            public void Apply(string name)
            {
                if (DoFail && name.Equals("rollback before checkpoint"))
                {
                    throw new Exception("intentionally failing");
                }
            }
        }

        // LUCENE-1347
        [Test]
        public virtual void TestRollbackExceptionHang()
        {
            Directory dir = NewDirectory();
            TestPoint4 testPoint = new TestPoint4();
            IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())), testPoint);

            AddDoc(w);
            testPoint.DoFail = true;
            try
            {
                w.Rollback();
                Assert.Fail("did not hit intentional RuntimeException");
            }
            catch (Exception re)
            {
                // expected
            }

            testPoint.DoFail = false;
            w.Rollback();
            dir.Dispose();
        }

        // LUCENE-1044: Simulate checksum error in segments_N
        [Test]
        public virtual void TestSegmentsChecksumError()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = null;

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }

            // close
            writer.Dispose();

            long gen = SegmentInfos.GetLastCommitGeneration(dir);
            Assert.IsTrue(gen > 0, "segment generation should be > 0 but got " + gen);

            string segmentsFileName = SegmentInfos.GetLastCommitSegmentsFileName(dir);
            IndexInput @in = dir.OpenInput(segmentsFileName, NewIOContext(Random()));
            IndexOutput @out = dir.CreateOutput(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", 1 + gen), NewIOContext(Random()));
            @out.CopyBytes(@in, @in.Length() - 1);
            byte b = @in.ReadByte();
            @out.WriteByte((byte)(sbyte)(1 + b));
            @out.Dispose();
            @in.Dispose();

            IndexReader reader = null;
            try
            {
                reader = DirectoryReader.Open(dir);
            }
            catch (IOException e)
            {
                Console.WriteLine(e.StackTrace);
                Assert.Fail("segmentInfos failed to retry fallback to correct segments_N file");
            }
            reader.Dispose();

            // should remove the corrumpted segments_N
            (new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null))).Dispose();
            dir.Dispose();
        }

        // Simulate a corrupt index by removing last byte of
        // latest segments file and make sure we get an
        // IOException trying to open the index:
        [Test]
        public virtual void TestSimulatedCorruptIndex1()
        {
            BaseDirectoryWrapper dir = NewDirectory();
            dir.CheckIndexOnClose = false; // we are corrupting it!

            IndexWriter writer = null;

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }

            // close
            writer.Dispose();

            long gen = SegmentInfos.GetLastCommitGeneration(dir);
            Assert.IsTrue(gen > 0, "segment generation should be > 0 but got " + gen);

            string fileNameIn = SegmentInfos.GetLastCommitSegmentsFileName(dir);
            string fileNameOut = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", 1 + gen);
            IndexInput @in = dir.OpenInput(fileNameIn, NewIOContext(Random()));
            IndexOutput @out = dir.CreateOutput(fileNameOut, NewIOContext(Random()));
            long length = @in.Length();
            for (int i = 0; i < length - 1; i++)
            {
                @out.WriteByte(@in.ReadByte());
            }
            @in.Dispose();
            @out.Dispose();
            dir.DeleteFile(fileNameIn);

            IndexReader reader = null;
            try
            {
                reader = DirectoryReader.Open(dir);
                Assert.Fail("reader did not hit IOException on opening a corrupt index");
            }
            catch (Exception e)
            {
            }
            if (reader != null)
            {
                reader.Dispose();
            }
            dir.Dispose();
        }

        // Simulate a corrupt index by removing one of the cfs
        // files and make sure we get an IOException trying to
        // open the index:
        [Test]
        public virtual void TestSimulatedCorruptIndex2()
        {
            BaseDirectoryWrapper dir = NewDirectory();
            dir.CheckIndexOnClose = false; // we are corrupting it!
            IndexWriter writer = null;

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy(true)).SetUseCompoundFile(true));
            MergePolicy lmp = writer.Config.MergePolicy;
            // Force creation of CFS:
            lmp.NoCFSRatio = 1.0;
            lmp.MaxCFSSegmentSizeMB = double.PositiveInfinity;

            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }

            // close
            writer.Dispose();

            long gen = SegmentInfos.GetLastCommitGeneration(dir);
            Assert.IsTrue(gen > 0, "segment generation should be > 0 but got " + gen);

            string[] files = dir.ListAll();
            bool corrupted = false;
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].EndsWith(".cfs"))
                {
                    dir.DeleteFile(files[i]);
                    corrupted = true;
                    break;
                }
            }
            Assert.IsTrue(corrupted, "failed to find cfs file to remove");

            IndexReader reader = null;
            try
            {
                reader = DirectoryReader.Open(dir);
                Assert.Fail("reader did not hit IOException on opening a corrupt index");
            }
            catch (Exception e)
            {
            }
            if (reader != null)
            {
                reader.Dispose();
            }
            dir.Dispose();
        }

        // Simulate a writer that crashed while writing segments
        // file: make sure we can still open the index (ie,
        // gracefully fallback to the previous segments file),
        // and that we can add to the index:
        [Test]
        public virtual void TestSimulatedCrashedWriter()
        {
            Directory dir = NewDirectory();
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).PreventDoubleWrite = false;
            }

            IndexWriter writer = null;

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }

            // close
            writer.Dispose();

            long gen = SegmentInfos.GetLastCommitGeneration(dir);
            Assert.IsTrue(gen > 0, "segment generation should be > 0 but got " + gen);

            // Make the next segments file, with last byte
            // missing, to simulate a writer that crashed while
            // writing segments file:
            string fileNameIn = SegmentInfos.GetLastCommitSegmentsFileName(dir);
            string fileNameOut = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", 1 + gen);
            IndexInput @in = dir.OpenInput(fileNameIn, NewIOContext(Random()));
            IndexOutput @out = dir.CreateOutput(fileNameOut, NewIOContext(Random()));
            long length = @in.Length();
            for (int i = 0; i < length - 1; i++)
            {
                @out.WriteByte(@in.ReadByte());
            }
            @in.Dispose();
            @out.Dispose();

            IndexReader reader = null;
            try
            {
                reader = DirectoryReader.Open(dir);
            }
            catch (Exception e)
            {
                Assert.Fail("reader failed to open on a crashed index");
            }
            reader.Dispose();

            try
            {
                writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Assert.Fail("writer failed to open on a crashed index");
            }

            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }

            // close
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestTermVectorExceptions()
        {
            FailOnTermVectors[] failures = new FailOnTermVectors[] { new FailOnTermVectors(FailOnTermVectors.AFTER_INIT_STAGE), new FailOnTermVectors(FailOnTermVectors.INIT_STAGE) };
            int num = AtLeast(1);
            for (int j = 0; j < num; j++)
            {
                foreach (FailOnTermVectors failure in failures)
                {
                    MockDirectoryWrapper dir = NewMockDirectory();
                    IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
                    dir.FailOn(failure);
                    int numDocs = 10 + Random().Next(30);
                    for (int i = 0; i < numDocs; i++)
                    {
                        Document doc = new Document();
                        Field field = NewTextField(Random(), "field", "a field", Field.Store.YES);
                        doc.Add(field);
                        // random TV
                        try
                        {
                            w.AddDocument(doc);
                            Assert.IsFalse(field.FieldType.StoreTermVectors);
                        }
                        catch (Exception e)
                        {
                            Assert.IsTrue(e.Message.StartsWith(FailOnTermVectors.EXC_MSG));
                        }
                        if (Random().Next(20) == 0)
                        {
                            w.Commit();
                            TestUtil.CheckIndex(dir);
                        }
                    }
                    Document document = new Document();
                    document.Add(new TextField("field", "a field", Field.Store.YES));
                    w.AddDocument(document);

                    for (int i = 0; i < numDocs; i++)
                    {
                        Document doc = new Document();
                        Field field = NewTextField(Random(), "field", "a field", Field.Store.YES);
                        doc.Add(field);
                        // random TV
                        try
                        {
                            w.AddDocument(doc);
                            Assert.IsFalse(field.FieldType.StoreTermVectors);
                        }
                        catch (Exception e)
                        {
                            Assert.IsTrue(e.Message.StartsWith(FailOnTermVectors.EXC_MSG));
                        }
                        if (Random().Next(20) == 0)
                        {
                            w.Commit();
                            TestUtil.CheckIndex(dir);
                        }
                    }
                    document = new Document();
                    document.Add(new TextField("field", "a field", Field.Store.YES));
                    w.AddDocument(document);
                    w.Dispose();
                    IndexReader reader = DirectoryReader.Open(dir);
                    Assert.IsTrue(reader.NumDocs > 0);
                    SegmentInfos sis = new SegmentInfos();
                    sis.Read(dir);
                    foreach (AtomicReaderContext context in reader.Leaves)
                    {
                        Assert.IsFalse((context.AtomicReader).FieldInfos.HasVectors());
                    }
                    reader.Dispose();
                    dir.Dispose();
                }
            }
        }

        private class FailOnTermVectors : MockDirectoryWrapper.Failure
        {
            internal const string INIT_STAGE = "InitTermVectorsWriter";
            internal const string AFTER_INIT_STAGE = "FinishDocument";
            internal const string EXC_MSG = "FOTV";
            internal readonly string Stage;

            public FailOnTermVectors(string stage)
            {
                this.Stage = stage;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                bool fail = StackTraceHelper.DoesStackTraceContainMethod(typeof(TermVectorsConsumer).Name, Stage);

                if (fail)
                {
                    throw new Exception(EXC_MSG);
                }
            }
        }

        [Test]
        public virtual void TestAddDocsNonAbortingException()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            int numDocs1 = Random().Next(25);
            for (int docCount = 0; docCount < numDocs1; docCount++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("content", "good content", Field.Store.NO));
                w.AddDocument(doc);
            }

            IList<Document> docs = new List<Document>();
            for (int docCount = 0; docCount < 7; docCount++)
            {
                Document doc = new Document();
                docs.Add(doc);
                doc.Add(NewStringField("id", docCount + "", Field.Store.NO));
                doc.Add(NewTextField("content", "silly content " + docCount, Field.Store.NO));
                if (docCount == 4)
                {
                    Field f = NewTextField("crash", "", Field.Store.NO);
                    doc.Add(f);
                    MockTokenizer tokenizer = new MockTokenizer(new StringReader("crash me on the 4th token"), MockTokenizer.WHITESPACE, false);
                    tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
                    f.TokenStream = new CrashingFilter(this, "crash", tokenizer);
                }
            }
            try
            {
                w.AddDocuments(docs);
                // BUG: CrashingFilter didn't
                Assert.Fail("did not hit expected exception");
            }
            catch (IOException ioe)
            {
                // expected
                Assert.AreEqual(CRASH_FAIL_MESSAGE, ioe.Message);
            }

            int numDocs2 = Random().Next(25);
            for (int docCount = 0; docCount < numDocs2; docCount++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("content", "good content", Field.Store.NO));
                w.AddDocument(doc);
            }

            IndexReader r = w.Reader;
            w.Dispose();

            IndexSearcher s = NewSearcher(r);
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("content", "silly"));
            pq.Add(new Term("content", "content"));
            Assert.AreEqual(0, s.Search(pq, 1).TotalHits);

            pq = new PhraseQuery();
            pq.Add(new Term("content", "good"));
            pq.Add(new Term("content", "content"));
            Assert.AreEqual(numDocs1 + numDocs2, s.Search(pq, 1).TotalHits);
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateDocsNonAbortingException()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            int numDocs1 = Random().Next(25);
            for (int docCount = 0; docCount < numDocs1; docCount++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("content", "good content", Field.Store.NO));
                w.AddDocument(doc);
            }

            // Use addDocs (no exception) to get docs in the index:
            IList<Document> docs = new List<Document>();
            int numDocs2 = Random().Next(25);
            for (int docCount = 0; docCount < numDocs2; docCount++)
            {
                Document doc = new Document();
                docs.Add(doc);
                doc.Add(NewStringField("subid", "subs", Field.Store.NO));
                doc.Add(NewStringField("id", docCount + "", Field.Store.NO));
                doc.Add(NewTextField("content", "silly content " + docCount, Field.Store.NO));
            }
            w.AddDocuments(docs);

            int numDocs3 = Random().Next(25);
            for (int docCount = 0; docCount < numDocs3; docCount++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("content", "good content", Field.Store.NO));
                w.AddDocument(doc);
            }

            docs.Clear();
            int limit = TestUtil.NextInt(Random(), 2, 25);
            int crashAt = Random().Next(limit);
            for (int docCount = 0; docCount < limit; docCount++)
            {
                Document doc = new Document();
                docs.Add(doc);
                doc.Add(NewStringField("id", docCount + "", Field.Store.NO));
                doc.Add(NewTextField("content", "silly content " + docCount, Field.Store.NO));
                if (docCount == crashAt)
                {
                    Field f = NewTextField("crash", "", Field.Store.NO);
                    doc.Add(f);
                    MockTokenizer tokenizer = new MockTokenizer(new StringReader("crash me on the 4th token"), MockTokenizer.WHITESPACE, false);
                    tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
                    f.TokenStream = new CrashingFilter(this, "crash", tokenizer);
                }
            }

            try
            {
                w.UpdateDocuments(new Term("subid", "subs"), docs);
                // BUG: CrashingFilter didn't
                Assert.Fail("did not hit expected exception");
            }
            catch (IOException ioe)
            {
                // expected
                Assert.AreEqual(CRASH_FAIL_MESSAGE, ioe.Message);
            }

            int numDocs4 = Random().Next(25);
            for (int docCount = 0; docCount < numDocs4; docCount++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("content", "good content", Field.Store.NO));
                w.AddDocument(doc);
            }

            IndexReader r = w.Reader;
            w.Dispose();

            IndexSearcher s = NewSearcher(r);
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("content", "silly"));
            pq.Add(new Term("content", "content"));
            Assert.AreEqual(numDocs2, s.Search(pq, 1).TotalHits);

            pq = new PhraseQuery();
            pq.Add(new Term("content", "good"));
            pq.Add(new Term("content", "content"));
            Assert.AreEqual(numDocs1 + numDocs3 + numDocs4, s.Search(pq, 1).TotalHits);
            r.Dispose();
            dir.Dispose();
        }

        internal class UOEDirectory : RAMDirectory
        {
            internal bool DoFail = false;

            public override IndexInput OpenInput(string name, IOContext context)
            {
                if (DoFail
                    && name.StartsWith("segments_")
                    && StackTraceHelper.DoesStackTraceContainMethod("Read"))
                {
                    throw new NotSupportedException("expected UOE");
                }

                return base.OpenInput(name, context);
            }
        }

        [Test]
        public virtual void TestExceptionOnCtor()
        {
            UOEDirectory uoe = new UOEDirectory();
            Directory d = new MockDirectoryWrapper(Random(), uoe);
            IndexWriter iw = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            iw.AddDocument(new Document());
            iw.Dispose();
            uoe.DoFail = true;
            try
            {
                new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
                Assert.Fail("should have gotten a UOE");
            }
            catch (System.NotSupportedException expected)
            {
            }

            uoe.DoFail = false;
            d.Dispose();
        }

        [Test]
        public virtual void TestIllegalPositions()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            Document doc = new Document();
            Token t1 = new Token("foo", 0, 3);
            t1.PositionIncrement = int.MaxValue;
            Token t2 = new Token("bar", 4, 7);
            t2.PositionIncrement = 200;
            TokenStream overflowingTokenStream = new CannedTokenStream(new Token[] { t1, t2 });
            Field field = new TextField("foo", overflowingTokenStream);
            doc.Add(field);
            try
            {
                iw.AddDocument(doc);
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected exception
            }
            iw.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestLegalbutVeryLargePositions()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            Document doc = new Document();
            Token t1 = new Token("foo", 0, 3);
            t1.PositionIncrement = int.MaxValue - 500;
            if (Random().NextBoolean())
            {
                t1.Payload = new BytesRef(new byte[] { 0x1 });
            }
            TokenStream overflowingTokenStream = new CannedTokenStream(new Token[] { t1 });
            Field field = new TextField("foo", overflowingTokenStream);
            doc.Add(field);
            iw.AddDocument(doc);
            iw.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestBoostOmitNorms()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMergePolicy(NewLogMergePolicy());
            IndexWriter iw = new IndexWriter(dir, iwc);
            Document doc = new Document();
            doc.Add(new StringField("field1", "sometext", Field.Store.YES));
            doc.Add(new TextField("field2", "sometext", Field.Store.NO));
            doc.Add(new StringField("foo", "bar", Field.Store.NO));
            iw.AddDocument(doc); // add an 'ok' document
            try
            {
                doc = new Document();
                // try to boost with norms omitted
                IList<IndexableField> list = new List<IndexableField>();
                list.Add(new IndexableFieldAnonymousInnerClassHelper(this));
                iw.AddDocument(list);
                Assert.Fail("didn't get any exception, boost silently discarded");
            }
            catch (System.NotSupportedException expected)
            {
                // expected
            }
            DirectoryReader ir = DirectoryReader.Open(iw, false);
            Assert.AreEqual(1, ir.NumDocs);
            Assert.AreEqual("sometext", ir.Document(0).Get("field1"));
            ir.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        private class IndexableFieldAnonymousInnerClassHelper : IndexableField
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            public IndexableFieldAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public string Name
            {
                get { return "foo"; }
            }

            public IndexableFieldType FieldType
            {
                get { return StringField.TYPE_NOT_STORED; }
            }

            public float Boost
            {
                get { return 5f; }
            }

            public BytesRef BinaryValue
            {
                get { return null; }
            }

            public string StringValue
            {
                get
                {
                    return "baz";
                }
            }

            public TextReader ReaderValue
            {
                get
                {
                    return null;
                }
            }

            public object NumericValue
            {
                get
                {
                    return null;
                }
            }

            public TokenStream GetTokenStream(Analyzer analyzer)
            {
                return null;
            }
        }

        // See LUCENE-4870 TooManyOpenFiles errors are thrown as
        // FNFExceptions which can trigger data loss.
        [Test]
        public virtual void TestTooManyFileException()
        {
            // Create failure that throws Too many open files exception randomly
            MockDirectoryWrapper.Failure failure = new FailureAnonymousInnerClassHelper(this);

            MockDirectoryWrapper dir = NewMockDirectory();
            // The exception is only thrown on open input
            dir.FailOnOpenInput = true;
            dir.FailOn(failure);

            // Create an index with one document
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            IndexWriter iw = new IndexWriter(dir, iwc);
            Document doc = new Document();
            doc.Add(new StringField("foo", "bar", Field.Store.NO));
            iw.AddDocument(doc); // add a document
            iw.Commit();
            DirectoryReader ir = DirectoryReader.Open(dir);
            Assert.AreEqual(1, ir.NumDocs);
            ir.Dispose();
            iw.Dispose();

            // Open and close the index a few times
            for (int i = 0; i < 10; i++)
            {
                failure.SetDoFail();
                iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
                try
                {
                    iw = new IndexWriter(dir, iwc);
                }
                catch (CorruptIndexException ex)
                {
                    // Exceptions are fine - we are running out of file handlers here
                    continue;
                }
                catch (FileNotFoundException/* | NoSuchFileException*/ ex)
                {
                    continue;
                }
                failure.ClearDoFail();
                iw.Dispose();
                ir = DirectoryReader.Open(dir);
                Assert.AreEqual(1, ir.NumDocs, "lost document after iteration: " + i);
                ir.Dispose();
            }

            // Check if document is still there
            failure.ClearDoFail();
            ir = DirectoryReader.Open(dir);
            Assert.AreEqual(1, ir.NumDocs);
            ir.Dispose();

            dir.Dispose();
        }

        private class FailureAnonymousInnerClassHelper : MockDirectoryWrapper.Failure
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            public FailureAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override MockDirectoryWrapper.Failure Reset()
            {
                DoFail = false;
                return this;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (DoFail)
                {
                    if (Random().NextBoolean())
                    {
                        throw new FileNotFoundException("some/file/name.ext (Too many open files)");
                    }
                }
            }
        }

        // Make sure if we hit a transient IOException (e.g., disk
        // full), and then the exception stops (e.g., disk frees
        // up), so we successfully close IW or open an NRT
        // reader, we don't lose any deletes or updates:
        [Test]
        public virtual void TestNoLostDeletesOrUpdates()
        {
            int deleteCount = 0;
            int docBase = 0;
            int docCount = 0;

            MockDirectoryWrapper dir = NewMockDirectory();
            AtomicBoolean shouldFail = new AtomicBoolean();
            dir.FailOn(new FailureAnonymousInnerClassHelper2(shouldFail));

            RandomIndexWriter w = null;

            for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
            {
                int numDocs = AtLeast(100);
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter + " numDocs=" + numDocs + ".DocBase=" + docBase + " delCount=" + deleteCount);
                }
                if (w == null)
                {
                    IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
                    IMergeScheduler ms = iwc.MergeScheduler;
                    if (ms is IConcurrentMergeScheduler)
                    {
                        ConcurrentMergeScheduler suppressFakeIOE = new ConcurrentMergeSchedulerAnonymousInnerClassHelper(this);

                        IConcurrentMergeScheduler cms = (IConcurrentMergeScheduler)ms;
                        suppressFakeIOE.SetMaxMergesAndThreads(cms.MaxMergeCount, cms.MaxThreadCount);
                        suppressFakeIOE.MergeThreadPriority = cms.MergeThreadPriority;
                        iwc.SetMergeScheduler(suppressFakeIOE);
                    }

                    w = new RandomIndexWriter(Random(), dir, iwc);
                    // Since we hit exc during merging, a partial
                    // forceMerge can easily return when there are still
                    // too many segments in the index:
                    w.DoRandomForceMergeAssert = false;
                }
                for (int i = 0; i < numDocs; i++)
                {
                    Document doc = new Document();
                    doc.Add(new StringField("id", (docBase + i).ToString(), Field.Store.NO));
                    if (DefaultCodecSupportsDocValues())
                    {
                        doc.Add(new NumericDocValuesField("f", 1L));
                        doc.Add(new NumericDocValuesField("cf", 2L));
                        doc.Add(new BinaryDocValuesField("bf", TestBinaryDocValuesUpdates.ToBytes(1L)));
                        doc.Add(new BinaryDocValuesField("bcf", TestBinaryDocValuesUpdates.ToBytes(2L)));
                    }
                    w.AddDocument(doc);
                }
                docCount += numDocs;

                // TODO: we could make the test more evil, by letting
                // it throw more than one exc, randomly, before "recovering"

                // TODO: we could also install an infoStream and try
                // to fail in "more evil" places inside BDS

                shouldFail.Set(true);
                bool doClose = false;

                try
                {
                    bool defaultCodecSupportsFieldUpdates = DefaultCodecSupportsFieldUpdates();
                    for (int i = 0; i < numDocs; i++)
                    {
                        if (Random().Next(10) == 7)
                        {
                            bool fieldUpdate = defaultCodecSupportsFieldUpdates && Random().NextBoolean();
                            if (fieldUpdate)
                            {
                                long value = iter;
                                if (VERBOSE)
                                {
                                    Console.WriteLine("  update id=" + docBase + i + " to value " + value);
                                }
                                if (Random().NextBoolean()) // update only numeric field
                                {
                                    w.UpdateNumericDocValue(new Term("id", (docBase + i).ToString()), "f", value);
                                    w.UpdateNumericDocValue(new Term("id", (docBase + i).ToString()), "cf", value * 2);
                                }
                                else if (Random().NextBoolean())
                                {
                                    w.UpdateBinaryDocValue(new Term("id", (docBase + i).ToString()), "bf", TestBinaryDocValuesUpdates.ToBytes(value));
                                    w.UpdateBinaryDocValue(new Term("id", (docBase + i).ToString()), "bcf", TestBinaryDocValuesUpdates.ToBytes(value * 2));
                                }
                                else
                                {
                                    w.UpdateNumericDocValue(new Term("id", (docBase + i).ToString()), "f", value);
                                    w.UpdateNumericDocValue(new Term("id", (docBase + i).ToString()), "cf", value * 2);
                                    w.UpdateBinaryDocValue(new Term("id", (docBase + i).ToString()), "bf", TestBinaryDocValuesUpdates.ToBytes(value));
                                    w.UpdateBinaryDocValue(new Term("id", (docBase + i).ToString()), "bcf", TestBinaryDocValuesUpdates.ToBytes(value * 2));
                                }
                            }

                            // sometimes do both deletes and updates
                            if (!fieldUpdate || Random().NextBoolean())
                            {
                                if (VERBOSE)
                                {
                                    Console.WriteLine("  delete id=" + (docBase + i).ToString());
                                }
                                deleteCount++;
                                w.DeleteDocuments(new Term("id", "" + (docBase + i).ToString()));
                            }
                        }
                    }

                    // Trigger writeLiveDocs so we hit fake exc:
                    IndexReader r = w.GetReader(true);

                    // Sometimes we will make it here (we only randomly
                    // throw the exc):
                    Assert.AreEqual(docCount - deleteCount, r.NumDocs);
                    r.Dispose();

                    // Sometimes close, so the disk full happens on close:
                    if (Random().NextBoolean())
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("  now close writer");
                        }
                        doClose = true;
                        w.Dispose();
                        w = null;
                    }
                }
                catch (IOException ioe)
                {
                    // FakeIOException can be thrown from mergeMiddle, in which case IW
                    // registers it before our CMS gets to suppress it. IW.forceMerge later
                    // throws it as a wrapped IOE, so don't fail in this case.
                    if (ioe is MockDirectoryWrapper.FakeIOException || (ioe.InnerException != null && ioe.InnerException is MockDirectoryWrapper.FakeIOException))
                    {
                        // expected
                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: w.Dispose() hit expected IOE");
                        }
                    }
                    else
                    {
                        throw ioe;
                    }
                }
                shouldFail.Set(false);

                IndexReader ir;

                if (doClose && w != null)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  now 2nd close writer");
                    }
                    w.Dispose();
                    w = null;
                }

                if (w == null || Random().NextBoolean())
                {
                    // Open non-NRT reader, to make sure the "on
                    // disk" bits are good:
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: verify against non-NRT reader");
                    }
                    if (w != null)
                    {
                        w.Commit();
                    }
                    ir = DirectoryReader.Open(dir);
                }
                else
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: verify against NRT reader");
                    }
                    ir = w.Reader;
                }
                Assert.AreEqual(docCount - deleteCount, ir.NumDocs);
                if (DefaultCodecSupportsDocValues())
                {
                    BytesRef scratch = new BytesRef();
                    foreach (AtomicReaderContext context in ir.Leaves)
                    {
                        AtomicReader reader = context.AtomicReader;
                        Bits liveDocs = reader.LiveDocs;
                        NumericDocValues f = reader.GetNumericDocValues("f");
                        NumericDocValues cf = reader.GetNumericDocValues("cf");
                        BinaryDocValues bf = reader.GetBinaryDocValues("bf");
                        BinaryDocValues bcf = reader.GetBinaryDocValues("bcf");
                        for (int i = 0; i < reader.MaxDoc; i++)
                        {
                            if (liveDocs == null || liveDocs.Get(i))
                            {
                                Assert.AreEqual(cf.Get(i), f.Get(i) * 2, "doc=" + (docBase + i).ToString());
                                Assert.AreEqual(TestBinaryDocValuesUpdates.GetValue(bcf, i, scratch), TestBinaryDocValuesUpdates.GetValue(bf, i, scratch) * 2, "doc=" + (docBase + i).ToString());
                            }
                        }
                    }
                }

                ir.Dispose();

                // Sometimes re-use RIW, other times open new one:
                if (w != null && Random().NextBoolean())
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: close writer");
                    }
                    w.Dispose();
                    w = null;
                }

                docBase += numDocs;
            }

            if (w != null)
            {
                w.Dispose();
            }

            // Final verify:
            IndexReader indRdr = DirectoryReader.Open(dir);
            Assert.AreEqual(docCount - deleteCount, indRdr.NumDocs);
            indRdr.Dispose();

            dir.Dispose();
        }

        private class FailureAnonymousInnerClassHelper2 : MockDirectoryWrapper.Failure
        {
            private AtomicBoolean ShouldFail;

            public FailureAnonymousInnerClassHelper2(AtomicBoolean shouldFail)
            {
                this.ShouldFail = shouldFail;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (ShouldFail.Get() == false)
                {
                    return;
                }

                bool sawSeal = StackTraceHelper.DoesStackTraceContainMethod("SealFlushedSegment");
                bool sawWrite = StackTraceHelper.DoesStackTraceContainMethod("WriteLiveDocs")
                        || StackTraceHelper.DoesStackTraceContainMethod("WriteFieldUpdates");

                // Don't throw exc if we are "flushing", else
                // the segment is aborted and docs are lost:
                if (sawWrite && !sawSeal && Random().Next(3) == 2)
                {
                    // Only sometimes throw the exc, so we get
                    // it sometimes on creating the file, on
                    // flushing buffer, on closing the file:
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: now fail; thread=" + Thread.CurrentThread.Name + " exc:");
                        Console.WriteLine((new Exception()).StackTrace);
                    }
                    ShouldFail.Set(false);
                    throw new MockDirectoryWrapper.FakeIOException();
                }
            }
        }

        private class ConcurrentMergeSchedulerAnonymousInnerClassHelper :
#if NETCORE
            TaskMergeScheduler
#else
            ConcurrentMergeScheduler
#endif
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            public ConcurrentMergeSchedulerAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override void HandleMergeException(Exception exc)
            {
                // suppress only FakeIOException:
                if (!(exc is MockDirectoryWrapper.FakeIOException))
                {
                    base.HandleMergeException(exc);
                }
            }
        }

        [Test]
        public virtual void TestExceptionDuringRollback()
        {
            // currently: fail in two different places
            string messageToFailOn = Random().NextBoolean() ? "rollback: done finish merges" : "rollback before checkpoint";

            // infostream that throws exception during rollback
            InfoStream evilInfoStream = new TEDRInfoStreamAnonymousInnerClassHelper(this, messageToFailOn);

            Directory dir = NewMockDirectory(); // we want to ensure we don't leak any locks or file handles
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            iwc.InfoStream = evilInfoStream;
            IndexWriter iw = new IndexWriter(dir, iwc);
            Document doc = new Document();
            for (int i = 0; i < 10; i++)
            {
                iw.AddDocument(doc);
            }
            iw.Commit();

            iw.AddDocument(doc);

            // pool readers
            DirectoryReader r = DirectoryReader.Open(iw, false);

            // sometimes sneak in a pending commit: we don't want to leak a file handle to that segments_N
            if (Random().NextBoolean())
            {
                iw.PrepareCommit();
            }

            try
            {
                iw.Rollback();
                Assert.Fail();
            }
            catch (Exception expected)
            {
                Assert.AreEqual("BOOM!", expected.Message);
            }

            r.Dispose();

            // even though we hit exception: we are closed, no locks or files held, index in good state
            Assert.IsTrue(iw.Closed);
            Assert.IsFalse(IndexWriter.IsLocked(dir));

            r = DirectoryReader.Open(dir);
            Assert.AreEqual(10, r.MaxDoc);
            r.Dispose();

            // no leaks
            dir.Dispose();
        }

        private class TEDRInfoStreamAnonymousInnerClassHelper : InfoStream
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            private string MessageToFailOn;

            public TEDRInfoStreamAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance, string messageToFailOn)
            {
                this.OuterInstance = outerInstance;
                this.MessageToFailOn = messageToFailOn;
            }

            public override void Message(string component, string message)
            {
                if (MessageToFailOn.Equals(message))
                {
                    throw new Exception("BOOM!");
                }
            }

            public override bool IsEnabled(string component)
            {
                return true;
            }

            public override void Dispose()
            {
            }
        }

        [Test]
        public virtual void TestRandomExceptionDuringRollback()
        {
            // fail in random places on i/o
            int numIters = RANDOM_MULTIPLIER * 75;
            for (int iter = 0; iter < numIters; iter++)
            {
                MockDirectoryWrapper dir = NewMockDirectory();
                dir.FailOn(new FailureAnonymousInnerClassHelper3(this, dir));

                IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
                IndexWriter iw = new IndexWriter(dir, iwc);
                Document doc = new Document();
                for (int i = 0; i < 10; i++)
                {
                    iw.AddDocument(doc);
                }
                iw.Commit();

                iw.AddDocument(doc);

                // pool readers
                DirectoryReader r = DirectoryReader.Open(iw, false);

                // sometimes sneak in a pending commit: we don't want to leak a file handle to that segments_N
                if (Random().NextBoolean())
                {
                    iw.PrepareCommit();
                }

                try
                {
                    iw.Rollback();
                }
                catch (MockDirectoryWrapper.FakeIOException expected)
                {
                }

                r.Dispose();

                // even though we hit exception: we are closed, no locks or files held, index in good state
                Assert.IsTrue(iw.Closed);
                Assert.IsFalse(IndexWriter.IsLocked(dir));

                r = DirectoryReader.Open(dir);
                Assert.AreEqual(10, r.MaxDoc);
                r.Dispose();

                // no leaks
                dir.Dispose();
            }
        }

        private class FailureAnonymousInnerClassHelper3 : MockDirectoryWrapper.Failure
        {
            private readonly TestIndexWriterExceptions OuterInstance;

            private MockDirectoryWrapper Dir;

            public FailureAnonymousInnerClassHelper3(TestIndexWriterExceptions outerInstance, MockDirectoryWrapper dir)
            {
                this.OuterInstance = outerInstance;
                this.Dir = dir;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                bool maybeFail = StackTraceHelper.DoesStackTraceContainMethod("RollbackInternal");

                if (maybeFail && Random().Next(10) == 0)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: now fail; thread=" + Thread.CurrentThread.Name + " exc:");
                        Console.WriteLine((new Exception()).StackTrace);
                    }
                    throw new MockDirectoryWrapper.FakeIOException();
                }
            }
        }
    }
}