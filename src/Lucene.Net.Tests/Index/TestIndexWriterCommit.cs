using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
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
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestIndexWriterCommit : LuceneTestCase
    {
        private static readonly FieldType storedTextType = new FieldType(TextField.TYPE_NOT_STORED);

        /*
         * Simple test for "commit on close": open writer then
         * add a bunch of docs, making sure reader does not see
         * these docs until writer is closed.
         */

        [Test]
        public virtual void TestCommitOnClose()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            for (int i = 0; i < 14; i++)
            {
                AddDoc(writer);
            }
            writer.Dispose();

            Term searchTerm = new Term("content", "aaa");
            DirectoryReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            Assert.AreEqual(14, hits.Length, "first number of hits");
            reader.Dispose();

            reader = DirectoryReader.Open(dir);

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 11; j++)
                {
                    AddDoc(writer);
                }
                IndexReader r = DirectoryReader.Open(dir);
                searcher = NewSearcher(r);
                hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
                Assert.AreEqual(14, hits.Length, "reader incorrectly sees changes from writer");
                r.Dispose();
                Assert.IsTrue(reader.IsCurrent(), "reader should have still been current");
            }

            // Now, close the writer:
            writer.Dispose();
            Assert.IsFalse(reader.IsCurrent(), "reader should not be current now");

            IndexReader ir = DirectoryReader.Open(dir);
            searcher = NewSearcher(ir);
            hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            Assert.AreEqual(47, hits.Length, "reader did not see changes after writer was closed");
            ir.Dispose();
            reader.Dispose();
            dir.Dispose();
        }

        /*
         * Simple test for "commit on close": open writer, then
         * add a bunch of docs, making sure reader does not see
         * them until writer has closed.  Then instead of
         * closing the writer, call abort and verify reader sees
         * nothing was added.  Then verify we can open the index
         * and add docs to it.
         */

        [Test]
        public virtual void TestCommitOnCloseAbort()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(10));
            for (int i = 0; i < 14; i++)
            {
                AddDoc(writer);
            }
            writer.Dispose();

            Term searchTerm = new Term("content", "aaa");
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            Assert.AreEqual(14, hits.Length, "first number of hits");
            reader.Dispose();

            writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMaxBufferedDocs(10));
            for (int j = 0; j < 17; j++)
            {
                AddDoc(writer);
            }
            // Delete all docs:
            writer.DeleteDocuments(searchTerm);

            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            Assert.AreEqual(14, hits.Length, "reader incorrectly sees changes from writer");
            reader.Dispose();

            // Now, close the writer:
            writer.Rollback();

            TestIndexWriter.AssertNoUnreferencedFiles(dir, "unreferenced files remain after rollback()");

            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            Assert.AreEqual(14, hits.Length, "saw changes after writer.abort");
            reader.Dispose();

            // Now make sure we can re-open the index, add docs,
            // and all is good:
            writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMaxBufferedDocs(10));

            // On abort, writer in fact may write to the same
            // segments_N file:
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).PreventDoubleWrite = false;
            }

            for (int i = 0; i < 12; i++)
            {
                for (int j = 0; j < 17; j++)
                {
                    AddDoc(writer);
                }
                IndexReader r = DirectoryReader.Open(dir);
                searcher = NewSearcher(r);
                hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
                Assert.AreEqual(14, hits.Length, "reader incorrectly sees changes from writer");
                r.Dispose();
            }

            writer.Dispose();
            IndexReader ir = DirectoryReader.Open(dir);
            searcher = NewSearcher(ir);
            hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            Assert.AreEqual(218, hits.Length, "didn't see changes after close");
            ir.Dispose();

            dir.Dispose();
        }

        /*
         * Verify that a writer with "commit on close" indeed
         * cleans up the temp segments created after opening
         * that are not referenced by the starting segments
         * file.  We check this by using MockDirectoryWrapper to
         * measure max temp disk space used.
         */

        [Test]
        public virtual void TestCommitOnCloseDiskUsage()
        {
            // MemoryCodec, since it uses FST, is not necessarily
            // "additive", ie if you add up N small FSTs, then merge
            // them, the merged result can easily be larger than the
            // sum because the merged FST may use array encoding for
            // some arcs (which uses more space):

            string idFormat = TestUtil.GetPostingsFormat("id");
            string contentFormat = TestUtil.GetPostingsFormat("content");
            AssumeFalse("this test cannot run with Memory codec", idFormat.Equals("Memory", StringComparison.Ordinal) || contentFormat.Equals("Memory", StringComparison.Ordinal));
            MockDirectoryWrapper dir = NewMockDirectory();
            Analyzer analyzer;
            if (Random.NextBoolean())
            {
                // no payloads
                analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer.WHITESPACE, true));
                });
            }
            else
            {
                // fixed length payloads
                int length = Random.Next(200);
                analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
                    return new TokenStreamComponents(tokenizer, new MockFixedLengthPayloadFilter(Random, tokenizer, length));
                });
            }

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(10).SetReaderPooling(false).SetMergePolicy(NewLogMergePolicy(10)));
            for (int j = 0; j < 30; j++)
            {
                AddDocWithIndex(writer, j);
            }
            writer.Dispose();
            dir.ResetMaxUsedSizeInBytes();

            dir.TrackDiskUsage = true;
            long startDiskUsage = dir.MaxUsedSizeInBytes;
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetOpenMode(OpenMode.APPEND).SetMaxBufferedDocs(10).SetMergeScheduler(new SerialMergeScheduler()).SetReaderPooling(false).SetMergePolicy(NewLogMergePolicy(10)));
            for (int j = 0; j < 1470; j++)
            {
                AddDocWithIndex(writer, j);
            }
            long midDiskUsage = dir.MaxUsedSizeInBytes;
            dir.ResetMaxUsedSizeInBytes();
            writer.ForceMerge(1);
            writer.Dispose();

            DirectoryReader.Open(dir).Dispose();

            long endDiskUsage = dir.MaxUsedSizeInBytes;

            // Ending index is 50X as large as starting index; due
            // to 3X disk usage normally we allow 150X max
            // transient usage.  If something is wrong w/ deleter
            // and it doesn't delete intermediate segments then it
            // will exceed this 150X:
            // System.out.println("start " + startDiskUsage + "; mid " + midDiskUsage + ";end " + endDiskUsage);
            Assert.IsTrue(midDiskUsage < 150 * startDiskUsage, "writer used too much space while adding documents: mid=" + midDiskUsage + " start=" + startDiskUsage + " end=" + endDiskUsage + " max=" + (startDiskUsage * 150));
            Assert.IsTrue(endDiskUsage < 150 * startDiskUsage, "writer used too much space after close: endDiskUsage=" + endDiskUsage + " startDiskUsage=" + startDiskUsage + " max=" + (startDiskUsage * 150));
            dir.Dispose();
        }

        /*
         * Verify that calling forceMerge when writer is open for
         * "commit on close" works correctly both for rollback()
         * and close().
         */

        [Test]
        public virtual void TestCommitOnCloseForceMerge()
        {
            Directory dir = NewDirectory();
            // Must disable throwing exc on double-write: this
            // test uses IW.rollback which easily results in
            // writing to same file more than once
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).PreventDoubleWrite = false;
            }
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(10).SetMergePolicy(NewLogMergePolicy(10)));
            for (int j = 0; j < 17; j++)
            {
                AddDocWithIndex(writer, j);
            }
            writer.Dispose();

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            writer.ForceMerge(1);

            // Open a reader before closing (commiting) the writer:
            DirectoryReader reader = DirectoryReader.Open(dir);

            // Reader should see index as multi-seg at this
            // point:
            Assert.IsTrue(reader.Leaves.Count > 1, "Reader incorrectly sees one segment");
            reader.Dispose();

            // Abort the writer:
            writer.Rollback();
            TestIndexWriter.AssertNoUnreferencedFiles(dir, "aborted writer after forceMerge");

            // Open a reader after aborting writer:
            reader = DirectoryReader.Open(dir);

            // Reader should still see index as multi-segment
            Assert.IsTrue(reader.Leaves.Count > 1, "Reader incorrectly sees one segment");
            reader.Dispose();

            if (Verbose)
            {
                Console.WriteLine("TEST: do real full merge");
            }
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            writer.ForceMerge(1);
            writer.Dispose();

            if (Verbose)
            {
                Console.WriteLine("TEST: writer closed");
            }
            TestIndexWriter.AssertNoUnreferencedFiles(dir, "aborted writer after forceMerge");

            // Open a reader after aborting writer:
            reader = DirectoryReader.Open(dir);

            // Reader should see index as one segment
            Assert.AreEqual(1, reader.Leaves.Count, "Reader incorrectly sees more than one segment");
            reader.Dispose();
            dir.Dispose();
        }

        // LUCENE-2095: make sure with multiple threads commit
        // doesn't return until all changes are in fact in the
        // index
        [Test]
        public virtual void TestCommitThreadSafety()
        {
            const int NUM_THREADS = 5;
            const double RUN_SEC = 0.5;
            var dir = NewDirectory();
            var w = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            TestUtil.ReduceOpenFiles(w.IndexWriter);
            w.Commit();
            var failed = new AtomicBoolean();
            var threads = new ThreadJob[NUM_THREADS];
            long endTime = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + ((long)(RUN_SEC * 1000)); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            for (int i = 0; i < NUM_THREADS; i++)
            {
                int finalI = i;
                threads[i] = new ThreadAnonymousClass(dir, w, failed, endTime, finalI, NewStringField);
                threads[i].Start();
            }
            for (int i = 0; i < NUM_THREADS; i++)
            {
                threads[i].Join();
            }
            Assert.IsFalse(failed);
            w.Dispose();
            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly Func<string, string, Field.Store, Field> newStringField;
            private Directory dir;
            private RandomIndexWriter w;
            private AtomicBoolean failed;
            private long endTime;
            private int finalI;

            /// <param name="newStringField">
            /// LUCENENET specific
            /// This is passed in because <see cref="LuceneTestCase.NewStringField(string, string, Field.Store)"/>
            /// is no longer static.
            /// </param>
            public ThreadAnonymousClass(Directory dir, RandomIndexWriter w, AtomicBoolean failed, long endTime, int finalI, Func<string, string, Field.Store, Field> newStringField)
            {
                this.newStringField = newStringField;
                this.dir = dir;
                this.w = w;
                this.failed = failed;
                this.endTime = endTime;
                this.finalI = finalI;
            }

            public override void Run()
            {
                try
                {
                    Document doc = new Document();
                    DirectoryReader r = DirectoryReader.Open(dir);
                    Field f = newStringField("f", "", Field.Store.NO);
                    doc.Add(f);
                    int count = 0;
                    do
                    {
                        if (failed)
                        {
                            break;
                        }
                        for (int j = 0; j < 10; j++)
                        {
                            string s = finalI + "_" + Convert.ToString(count++);
                            f.SetStringValue(s);
                            w.AddDocument(doc);
                            w.Commit();
                            DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
                            Assert.IsNotNull(r2);
                            Assert.IsTrue(!r2.Equals(r));
                            r.Dispose();
                            r = r2;
                            Assert.AreEqual(1, r.DocFreq(new Term("f", s)), "term=f:" + s + "; r=" + r);
                        }
                    } while ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) < endTime); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                    r.Dispose();
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    failed.Value = (true);
                    throw RuntimeException.Create(t);
                }
            }
        }

        // LUCENE-1044: test writer.Commit() when ac=false
        [Test]
        public virtual void TestForceCommit()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(5)));
            writer.Commit();

            for (int i = 0; i < 23; i++)
            {
                AddDoc(writer);
            }

            DirectoryReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(0, reader.NumDocs);
            writer.Commit();
            DirectoryReader reader2 = DirectoryReader.OpenIfChanged(reader);
            Assert.IsNotNull(reader2);
            Assert.AreEqual(0, reader.NumDocs);
            Assert.AreEqual(23, reader2.NumDocs);
            reader.Dispose();

            for (int i = 0; i < 17; i++)
            {
                AddDoc(writer);
            }
            Assert.AreEqual(23, reader2.NumDocs);
            reader2.Dispose();
            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(23, reader.NumDocs);
            reader.Dispose();
            writer.Commit();

            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(40, reader.NumDocs);
            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestFutureCommit()
        {
            Directory dir = NewDirectory();

            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE));
            Document doc = new Document();
            w.AddDocument(doc);

            // commit to "first"
            IDictionary<string, string> commitData = new Dictionary<string, string>();
            commitData["tag"] = "first";
            w.SetCommitData(commitData);
            w.Commit();

            // commit to "second"
            w.AddDocument(doc);
            commitData["tag"] = "second";
            w.SetCommitData(commitData);
            w.Dispose();

            // open "first" with IndexWriter
            IndexCommit commit = null;
            foreach (IndexCommit c in DirectoryReader.ListCommits(dir))
            {
                if (c.UserData["tag"].Equals("first", StringComparison.Ordinal))
                {
                    commit = c;
                    break;
                }
            }

            Assert.IsNotNull(commit);

            w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE).SetIndexCommit(commit));

            Assert.AreEqual(1, w.NumDocs);

            // commit IndexWriter to "third"
            w.AddDocument(doc);
            commitData["tag"] = "third";
            w.SetCommitData(commitData);
            w.Dispose();

            // make sure "second" commit is still there
            commit = null;
            foreach (IndexCommit c in DirectoryReader.ListCommits(dir))
            {
                if (c.UserData["tag"].Equals("second", StringComparison.Ordinal))
                {
                    commit = c;
                    break;
                }
            }

            Assert.IsNotNull(commit);

            dir.Dispose();
        }

        [Test]
        public virtual void TestZeroCommits()
        {
            // Tests that if we don't call commit(), the directory has 0 commits. this has
            // changed since LUCENE-2386, where before IW would always commit on a fresh
            // new index.
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            try
            {
                DirectoryReader.ListCommits(dir);
                Assert.Fail("listCommits should have thrown an exception over empty index");
            }
#pragma warning disable 168
            catch (IndexNotFoundException e)
#pragma warning restore 168
            {
                // that's expected !
            }
            // No changes still should generate a commit, because it's a new index.
            writer.Dispose();
            Assert.AreEqual(1, DirectoryReader.ListCommits(dir).Count, "expected 1 commits!");
            dir.Dispose();
        }

        // LUCENE-1274: test writer.PrepareCommit()
        [Test]
        public virtual void TestPrepareCommit()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(5)));
            writer.Commit();

            for (int i = 0; i < 23; i++)
            {
                AddDoc(writer);
            }

            DirectoryReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(0, reader.NumDocs);

            writer.PrepareCommit();

            IndexReader reader2 = DirectoryReader.Open(dir);
            Assert.AreEqual(0, reader2.NumDocs);

            writer.Commit();

            IndexReader reader3 = DirectoryReader.OpenIfChanged(reader);
            Assert.IsNotNull(reader3);
            Assert.AreEqual(0, reader.NumDocs);
            Assert.AreEqual(0, reader2.NumDocs);
            Assert.AreEqual(23, reader3.NumDocs);
            reader.Dispose();
            reader2.Dispose();

            for (int i = 0; i < 17; i++)
            {
                AddDoc(writer);
            }

            Assert.AreEqual(23, reader3.NumDocs);
            reader3.Dispose();
            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(23, reader.NumDocs);
            reader.Dispose();

            writer.PrepareCommit();

            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(23, reader.NumDocs);
            reader.Dispose();

            writer.Commit();
            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(40, reader.NumDocs);
            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        // LUCENE-1274: test writer.PrepareCommit()
        [Test]
        public virtual void TestPrepareCommitRollback()
        {
            Directory dir = NewDirectory();
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).PreventDoubleWrite = false;
            }

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(5)));
            writer.Commit();

            for (int i = 0; i < 23; i++)
            {
                AddDoc(writer);
            }

            DirectoryReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(0, reader.NumDocs);

            writer.PrepareCommit();

            IndexReader reader2 = DirectoryReader.Open(dir);
            Assert.AreEqual(0, reader2.NumDocs);

            writer.Rollback();

            IndexReader reader3 = DirectoryReader.OpenIfChanged(reader);
            Assert.IsNull(reader3);
            Assert.AreEqual(0, reader.NumDocs);
            Assert.AreEqual(0, reader2.NumDocs);
            reader.Dispose();
            reader2.Dispose();

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            for (int i = 0; i < 17; i++)
            {
                AddDoc(writer);
            }

            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(0, reader.NumDocs);
            reader.Dispose();

            writer.PrepareCommit();

            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(0, reader.NumDocs);
            reader.Dispose();

            writer.Commit();
            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(17, reader.NumDocs);
            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        // LUCENE-1274
        [Test]
        public virtual void TestPrepareCommitNoChanges()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            writer.PrepareCommit();
            writer.Commit();
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(0, reader.NumDocs);
            reader.Dispose();
            dir.Dispose();
        }

        // LUCENE-1382
        [Test]
        public virtual void TestCommitUserData()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2));
            for (int j = 0; j < 17; j++)
            {
                AddDoc(w);
            }
            w.Dispose();

            DirectoryReader r = DirectoryReader.Open(dir);
            // commit(Map) never called for this index
            Assert.AreEqual(0, r.IndexCommit.UserData.Count);
            r.Dispose();

            w = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2));
            for (int j = 0; j < 17; j++)
            {
                AddDoc(w);
            }
            IDictionary<string, string> data = new Dictionary<string, string>();
            data["label"] = "test1";
            w.SetCommitData(data);
            w.Dispose();

            r = DirectoryReader.Open(dir);
            Assert.AreEqual("test1", r.IndexCommit.UserData["label"]);
            r.Dispose();

            w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            w.ForceMerge(1);
            w.Dispose();

            dir.Dispose();
        }

        /// <summary>
        /// LUCENENET specific
        /// Copied from <see cref="TestIndexWriter.AddDoc(IndexWriter)"/>
        /// to remove inter-class dependency on <see cref="TestIndexWriter"/>
        /// </summary>
        private void AddDoc(IndexWriter writer)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            writer.AddDocument(doc);
        }

        /// <summary>
        /// LUCENENET specific
        /// Copied from <seealso cref="TestIndexWriter.AddDocWithIndex(IndexWriter, int)"/>
        /// to remove inter-class dependency on <see cref="TestIndexWriter"/>.
        /// </summary>
        private void AddDocWithIndex(IndexWriter writer, int index)
        {
            Document doc = new Document();
            doc.Add(NewField("content", "aaa " + index, storedTextType));
            doc.Add(NewField("id", "" + index, storedTextType));
            writer.AddDocument(doc);
        }

    }
}