using J2N.Collections.Generic.Extensions;
using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using Assert = Lucene.Net.TestFramework.Assert;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using NumericDocValuesField = NumericDocValuesField;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using StringField = StringField;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestIndexWriterDelete : LuceneTestCase
    {
        // test the simple case
        [Test]
        public virtual void TestSimpleCase()
        {
            string[] keywords = new string[] { "1", "2" };
            string[] unindexed = new string[] { "Netherlands", "Italy" };
            string[] unstored = new string[] { "Amsterdam has lots of bridges", "Venice has lots of canals" };
            string[] text = new string[] { "Amsterdam", "Venice" };

            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetMaxBufferedDeleteTerms(1));

            FieldType custom1 = new FieldType();
            custom1.IsStored = true;
            for (int i = 0; i < keywords.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", keywords[i], Field.Store.YES));
                doc.Add(NewField("country", unindexed[i], custom1));
                doc.Add(NewTextField("contents", unstored[i], Field.Store.NO));
                doc.Add(NewTextField("city", text[i], Field.Store.YES));
                modifier.AddDocument(doc);
            }
            modifier.ForceMerge(1);
            modifier.Commit();

            Term term = new Term("city", "Amsterdam");
            int hitCount = GetHitCount(dir, term);
            Assert.AreEqual(1, hitCount);
            if (Verbose)
            {
                Console.WriteLine("\nTEST: now delete by term=" + term);
            }
            modifier.DeleteDocuments(term);
            modifier.Commit();

            if (Verbose)
            {
                Console.WriteLine("\nTEST: now getHitCount");
            }
            hitCount = GetHitCount(dir, term);
            Assert.AreEqual(0, hitCount);

            modifier.Dispose();
            dir.Dispose();
        }

        // test when delete terms only apply to disk segments
        [Test]
        public virtual void TestNonRAMDelete()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetMaxBufferedDocs(2).SetMaxBufferedDeleteTerms(2));
            int id = 0;
            int value = 100;

            for (int i = 0; i < 7; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();

            Assert.AreEqual(0, modifier.NumBufferedDocuments);
            Assert.IsTrue(0 < modifier.SegmentCount);

            modifier.Commit();

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(7, reader.NumDocs);
            reader.Dispose();

            modifier.DeleteDocuments(new Term("value", Convert.ToString(value)));

            modifier.Commit();

            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(0, reader.NumDocs);
            reader.Dispose();
            modifier.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestMaxBufferedDeletes()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetMaxBufferedDeleteTerms(1));

            writer.AddDocument(new Document());
            writer.DeleteDocuments(new Term("foobar", "1"));
            writer.DeleteDocuments(new Term("foobar", "1"));
            writer.DeleteDocuments(new Term("foobar", "1"));
            Assert.AreEqual(3, writer.FlushDeletesCount);
            writer.Dispose();
            dir.Dispose();
        }

        // test when delete terms only apply to ram segments
        [Test]
        public virtual void TestRAMDeletes()
        {
            for (int t = 0; t < 2; t++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: t=" + t);
                }
                Directory dir = NewDirectory();
                IndexWriter modifier = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetMaxBufferedDocs(4).SetMaxBufferedDeleteTerms(4));
                int id = 0;
                int value = 100;

                AddDoc(modifier, ++id, value);
                if (0 == t)
                {
                    modifier.DeleteDocuments(new Term("value", Convert.ToString(value)));
                }
                else
                {
                    modifier.DeleteDocuments(new TermQuery(new Term("value", Convert.ToString(value))));
                }
                AddDoc(modifier, ++id, value);
                if (0 == t)
                {
                    modifier.DeleteDocuments(new Term("value", Convert.ToString(value)));
                    Assert.AreEqual(2, modifier.NumBufferedDeleteTerms);
                    Assert.AreEqual(1, modifier.BufferedDeleteTermsSize);
                }
                else
                {
                    modifier.DeleteDocuments(new TermQuery(new Term("value", Convert.ToString(value))));
                }

                AddDoc(modifier, ++id, value);
                Assert.AreEqual(0, modifier.SegmentCount);
                modifier.Commit();

                IndexReader reader = DirectoryReader.Open(dir);
                Assert.AreEqual(1, reader.NumDocs);

                int hitCount = GetHitCount(dir, new Term("id", Convert.ToString(id)));
                Assert.AreEqual(1, hitCount);
                reader.Dispose();
                modifier.Dispose();
                dir.Dispose();
            }
        }

        // test when delete terms apply to both disk and ram segments
        [Test]
        public virtual void TestBothDeletes()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetMaxBufferedDocs(100).SetMaxBufferedDeleteTerms(100));

            int id = 0;
            int value = 100;

            for (int i = 0; i < 5; i++)
            {
                AddDoc(modifier, ++id, value);
            }

            value = 200;
            for (int i = 0; i < 5; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();

            for (int i = 0; i < 5; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.DeleteDocuments(new Term("value", Convert.ToString(value)));

            modifier.Commit();

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(5, reader.NumDocs);
            modifier.Dispose();
            reader.Dispose();
            dir.Dispose();
        }

        // test that batched delete terms are flushed together
        [Test]
        public virtual void TestBatchDeletes()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetMaxBufferedDocs(2).SetMaxBufferedDeleteTerms(2));

            int id = 0;
            int value = 100;

            for (int i = 0; i < 7; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(7, reader.NumDocs);
            reader.Dispose();

            id = 0;
            modifier.DeleteDocuments(new Term("id", Convert.ToString(++id)));
            modifier.DeleteDocuments(new Term("id", Convert.ToString(++id)));

            modifier.Commit();

            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(5, reader.NumDocs);
            reader.Dispose();

            Term[] terms = new Term[3];
            for (int i = 0; i < terms.Length; i++)
            {
                terms[i] = new Term("id", Convert.ToString(++id));
            }
            modifier.DeleteDocuments(terms);
            modifier.Commit();
            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(2, reader.NumDocs);
            reader.Dispose();

            modifier.Dispose();
            dir.Dispose();
        }

        // test deleteAll()
        [Test]
        public virtual void TestDeleteAll()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetMaxBufferedDocs(2).SetMaxBufferedDeleteTerms(2));

            int id = 0;
            int value = 100;

            for (int i = 0; i < 7; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(7, reader.NumDocs);
            reader.Dispose();

            // Add 1 doc (so we will have something buffered)
            AddDoc(modifier, 99, value);

            // Delete all
            modifier.DeleteAll();

            // Delete all shouldn't be on disk yet
            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(7, reader.NumDocs);
            reader.Dispose();

            // Add a doc and update a doc (after the deleteAll, before the commit)
            AddDoc(modifier, 101, value);
            UpdateDoc(modifier, 102, value);

            // commit the delete all
            modifier.Commit();

            // Validate there are no docs left
            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(2, reader.NumDocs);
            reader.Dispose();

            modifier.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDeleteAllNoDeadLock()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter modifier = new RandomIndexWriter(Random, dir);
            int numThreads = AtLeast(2);
            ThreadJob[] threads = new ThreadJob[numThreads];
            CountdownEvent latch = new CountdownEvent(1);
            CountdownEvent doneLatch = new CountdownEvent(numThreads);
            for (int i = 0; i < numThreads; i++)
            {
                int offset = i;
                threads[i] = new ThreadAnonymousClass(this, modifier, latch, doneLatch, offset);
                threads[i].Start();
            }
            latch.Signal();
            //Wait for 1 millisecond
            while (!doneLatch.Wait(new TimeSpan(0, 0, 0, 0, 1)))
            {
                modifier.DeleteAll();
                if (Verbose)
                {
                    Console.WriteLine("del all");
                }
            }

            modifier.DeleteAll();
            foreach (ThreadJob thread in threads)
            {
                thread.Join();
            }

            modifier.Dispose();
            DirectoryReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(reader.MaxDoc, 0);
            Assert.AreEqual(reader.NumDocs, 0);
            Assert.AreEqual(reader.NumDeletedDocs, 0);
            reader.Dispose();

            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestIndexWriterDelete outerInstance;

            private readonly RandomIndexWriter modifier;
            private readonly CountdownEvent latch;
            private readonly CountdownEvent doneLatch;
            private readonly int offset;

            public ThreadAnonymousClass(TestIndexWriterDelete outerInstance, RandomIndexWriter modifier, CountdownEvent latch, CountdownEvent doneLatch, int offset)
            {
                this.outerInstance = outerInstance;
                this.modifier = modifier;
                this.latch = latch;
                this.doneLatch = doneLatch;
                this.offset = offset;
            }

            public override void Run()
            {
                int id = offset * 1000;
                int value = 100;
                try
                {
                    latch.Wait();
                    for (int j = 0; j < 1000; j++)
                    {
                        Document doc = new Document();
                        doc.Add(NewTextField("content", "aaa", Field.Store.NO));
                        doc.Add(NewStringField("id", Convert.ToString(id++), Field.Store.YES));
                        doc.Add(NewStringField("value", Convert.ToString(value), Field.Store.NO));
                        if (DefaultCodecSupportsDocValues)
                        {
                            doc.Add(new NumericDocValuesField("dv", value));
                        }
                        modifier.AddDocument(doc);
                        if (Verbose)
                        {
                            Console.WriteLine("\tThread[" + offset + "]: add doc: " + id);
                        }
                    }
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
                finally
                {
                    doneLatch.Signal();
                    if (Verbose)
                    {
                        Console.WriteLine("\tThread[" + offset + "]: done indexing");
                    }
                }
            }
        }

        // test rollback of deleteAll()
        [Test]
        public virtual void TestDeleteAllRollback()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetMaxBufferedDocs(2).SetMaxBufferedDeleteTerms(2));

            int id = 0;
            int value = 100;

            for (int i = 0; i < 7; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();

            AddDoc(modifier, ++id, value);

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(7, reader.NumDocs);
            reader.Dispose();

            // Delete all
            modifier.DeleteAll();

            // Roll it back
            modifier.Rollback();
            modifier.Dispose();

            // Validate that the docs are still there
            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(7, reader.NumDocs);
            reader.Dispose();

            dir.Dispose();
        }

        // test deleteAll() w/ near real-time reader
        [Test]
        public virtual void TestDeleteAllNRT()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetMaxBufferedDocs(2).SetMaxBufferedDeleteTerms(2));

            int id = 0;
            int value = 100;

            for (int i = 0; i < 7; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();

            IndexReader reader = modifier.GetReader();
            Assert.AreEqual(7, reader.NumDocs);
            reader.Dispose();

            AddDoc(modifier, ++id, value);
            AddDoc(modifier, ++id, value);

            // Delete all
            modifier.DeleteAll();

            reader = modifier.GetReader();
            Assert.AreEqual(0, reader.NumDocs);
            reader.Dispose();

            // Roll it back
            modifier.Rollback();
            modifier.Dispose();

            // Validate that the docs are still there
            reader = DirectoryReader.Open(dir);
            Assert.AreEqual(7, reader.NumDocs);
            reader.Dispose();

            dir.Dispose();
        }

        private void UpdateDoc(IndexWriter modifier, int id, int value)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            doc.Add(NewStringField("id", Convert.ToString(id), Field.Store.YES));
            doc.Add(NewStringField("value", Convert.ToString(value), Field.Store.NO));
            if (DefaultCodecSupportsDocValues)
            {
                doc.Add(new NumericDocValuesField("dv", value));
            }
            modifier.UpdateDocument(new Term("id", Convert.ToString(id)), doc);
        }

        private void AddDoc(IndexWriter modifier, int id, int value)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            doc.Add(NewStringField("id", Convert.ToString(id), Field.Store.YES));
            doc.Add(NewStringField("value", Convert.ToString(value), Field.Store.NO));
            if (DefaultCodecSupportsDocValues)
            {
                doc.Add(new NumericDocValuesField("dv", value));
            }
            modifier.AddDocument(doc);
        }

        private int GetHitCount(Directory dir, Term term)
        {
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            int hitCount = searcher.Search(new TermQuery(term), null, 1000).TotalHits;
            reader.Dispose();
            return hitCount;
        }

        [Test]
        public virtual void TestDeletesOnDiskFull()
        {
            DoTestOperationsOnDiskFull(false);
        }

        [Test]
        public virtual void TestUpdatesOnDiskFull()
        {
            DoTestOperationsOnDiskFull(true);
        }

        /// <summary>
        /// Make sure if modifier tries to commit but hits disk full that modifier
        /// remains consistent and usable. Similar to TestIndexReader.testDiskFull().
        /// </summary>
        private void DoTestOperationsOnDiskFull(bool updates)
        {
            Term searchTerm = new Term("content", "aaa");
            int START_COUNT = 157;
            int END_COUNT = 144;

            // First build up a starting index:
            MockDirectoryWrapper startDir = NewMockDirectory();
            // TODO: find the resource leak that only occurs sometimes here.
            startDir.NoDeleteOpenFile = false;
            IndexWriter writer = new IndexWriter(startDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            for (int i = 0; i < 157; i++)
            {
                Document d = new Document();
                d.Add(NewStringField("id", Convert.ToString(i), Field.Store.YES));
                d.Add(NewTextField("content", "aaa " + i, Field.Store.NO));
                if (DefaultCodecSupportsDocValues)
                {
                    d.Add(new NumericDocValuesField("dv", i));
                }
                writer.AddDocument(d);
            }
            writer.Dispose();

            long diskUsage = startDir.GetSizeInBytes();
            long diskFree = diskUsage + 10;

            Exception err = null; // LUCENENET: No need to cast to IOExcpetion

            bool done = false;

            // Iterate w/ ever increasing free disk space:
            while (!done)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: cycle");
                }
                MockDirectoryWrapper dir = new MockDirectoryWrapper(Random, new RAMDirectory(startDir, NewIOContext(Random)));
                dir.PreventDoubleWrite = false;
                dir.AllowRandomFileNotFoundException = false;

                var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false))
                                .SetMaxBufferedDocs(1000)
                                .SetMaxBufferedDeleteTerms(1000)
                                .SetMergeScheduler(new ConcurrentMergeScheduler());

                IConcurrentMergeScheduler scheduler = config.MergeScheduler as IConcurrentMergeScheduler;
                if (scheduler != null)
                {
                    scheduler.SetSuppressExceptions();
                }

                IndexWriter modifier = new IndexWriter(dir, config);

                // For each disk size, first try to commit against
                // dir that will hit random IOExceptions & disk
                // full; after, give it infinite disk space & turn
                // off random IOExceptions & retry w/ same reader:
                bool success = false;

                for (int x = 0; x < 2; x++)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: x=" + x);
                    }

                    double rate = 0.1;
                    double diskRatio = ((double)diskFree) / diskUsage;
                    long thisDiskFree;
                    string testName;

                    if (0 == x)
                    {
                        thisDiskFree = diskFree;
                        if (diskRatio >= 2.0)
                        {
                            rate /= 2;
                        }
                        if (diskRatio >= 4.0)
                        {
                            rate /= 2;
                        }
                        if (diskRatio >= 6.0)
                        {
                            rate = 0.0;
                        }
                        if (Verbose)
                        {
                            Console.WriteLine("\ncycle: " + diskFree + " bytes");
                        }
                        testName = "disk full during reader.Dispose() @ " + thisDiskFree + " bytes";
                        dir.RandomIOExceptionRateOnOpen = Random.NextDouble() * 0.01;
                    }
                    else
                    {
                        thisDiskFree = 0;
                        rate = 0.0;
                        if (Verbose)
                        {
                            Console.WriteLine("\ncycle: same writer: unlimited disk space");
                        }
                        testName = "reader re-use after disk full";
                        dir.RandomIOExceptionRateOnOpen = 0.0;
                    }

                    dir.MaxSizeInBytes = thisDiskFree;
                    dir.RandomIOExceptionRate = rate;

                    try
                    {
                        if (0 == x)
                        {
                            int docId = 12;
                            for (int i = 0; i < 13; i++)
                            {
                                if (updates)
                                {
                                    Document d = new Document();
                                    d.Add(NewStringField("id", Convert.ToString(i), Field.Store.YES));
                                    d.Add(NewTextField("content", "bbb " + i, Field.Store.NO));
                                    if (DefaultCodecSupportsDocValues)
                                    {
                                        d.Add(new NumericDocValuesField("dv", i));
                                    }
                                    modifier.UpdateDocument(new Term("id", Convert.ToString(docId)), d);
                                } // deletes
                                else
                                {
                                    modifier.DeleteDocuments(new Term("id", Convert.ToString(docId)));
                                    // modifier.setNorm(docId, "contents", (float)2.0);
                                }
                                docId += 12;
                            }
                        }
                        modifier.Dispose();
                        success = true;
                        if (0 == x)
                        {
                            done = true;
                        }
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("  hit IOException: " + e);
                            Console.WriteLine(e.StackTrace);
                        }
                        err = e;
                        if (1 == x)
                        {
                            Console.WriteLine(e.ToString());
                            Console.Write(e.StackTrace);
                            Assert.Fail(testName + " hit IOException after disk space was freed up");
                        }
                    }
                    // prevent throwing a random exception here!!
                    double randomIOExceptionRate = dir.RandomIOExceptionRate;
                    long maxSizeInBytes = dir.MaxSizeInBytes;
                    dir.RandomIOExceptionRate = 0.0;
                    dir.RandomIOExceptionRateOnOpen = 0.0;
                    dir.MaxSizeInBytes = 0;
                    if (!success)
                    {
                        // Must force the close else the writer can have
                        // open files which cause exc in MockRAMDir.close
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: now rollback");
                        }
                        modifier.Rollback();
                    }

                    // If the close() succeeded, make sure there are
                    // no unreferenced files.
                    if (success)
                    {
                        TestUtil.CheckIndex(dir);
                        TestIndexWriter.AssertNoUnreferencedFiles(dir, "after writer.close");
                    }
                    dir.RandomIOExceptionRate = randomIOExceptionRate;
                    dir.MaxSizeInBytes = maxSizeInBytes;

                    // Finally, verify index is not corrupt, and, if
                    // we succeeded, we see all docs changed, and if
                    // we failed, we see either all docs or no docs
                    // changed (transactional semantics):
                    IndexReader newReader = null;
                    try
                    {
                        newReader = DirectoryReader.Open(dir);
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        Console.WriteLine(e.ToString());
                        Console.Write(e.StackTrace);
                        Assert.Fail(testName + ":exception when creating IndexReader after disk full during close: " + e);
                    }

                    IndexSearcher searcher = NewSearcher(newReader);
                    ScoreDoc[] hits = null;
                    try
                    {
                        hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        Console.WriteLine(e.ToString());
                        Console.Write(e.StackTrace);
                        Assert.Fail(testName + ": exception when searching: " + e);
                    }
                    int result2 = hits.Length;
                    if (success)
                    {
                        if (x == 0 && result2 != END_COUNT)
                        {
                            Assert.Fail(testName + ": method did not throw exception but hits.Length for search on term 'aaa' is " + result2 + " instead of expected " + END_COUNT);
                        }
                        else if (x == 1 && result2 != START_COUNT && result2 != END_COUNT)
                        {
                            // It's possible that the first exception was
                            // "recoverable" wrt pending deletes, in which
                            // case the pending deletes are retained and
                            // then re-flushing (with plenty of disk
                            // space) will succeed in flushing the
                            // deletes:
                            Assert.Fail(testName + ": method did not throw exception but hits.Length for search on term 'aaa' is " + result2 + " instead of expected " + START_COUNT + " or " + END_COUNT);
                        }
                    }
                    else
                    {
                        // On hitting exception we still may have added
                        // all docs:
                        if (result2 != START_COUNT && result2 != END_COUNT)
                        {
                            Console.WriteLine(err.ToString());
                            Console.Write(err.StackTrace);
                            Assert.Fail(testName + ": method did throw exception but hits.Length for search on term 'aaa' is " + result2 + " instead of expected " + START_COUNT + " or " + END_COUNT);
                        }
                    }
                    newReader.Dispose();
                    if (result2 == END_COUNT)
                    {
                        break;
                    }
                }
                dir.Dispose();
                modifier.Dispose();

                // Try again with 10 more bytes of free space:
                diskFree += 10;
            }
            startDir.Dispose();
        }

        // this test tests that buffered deletes are cleared when
        // an Exception is hit during flush.
        [Test]
        public virtual void TestErrorAfterApplyDeletes()
        {
            Failure failure = new FailureAnonymousClass(this);

            // create a couple of files

            string[] keywords = new string[] { "1", "2" };
            string[] unindexed = new string[] { "Netherlands", "Italy" };
            string[] unstored = new string[] { "Amsterdam has lots of bridges", "Venice has lots of canals" };
            string[] text = new string[] { "Amsterdam", "Venice" };

            MockDirectoryWrapper dir = NewMockDirectory();
            IndexWriter modifier = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetMaxBufferedDeleteTerms(2).SetReaderPooling(false).SetMergePolicy(NewLogMergePolicy()));

            MergePolicy lmp = modifier.Config.MergePolicy;
            lmp.NoCFSRatio = 1.0;

            dir.FailOn(failure.Reset());

            FieldType custom1 = new FieldType();
            custom1.IsStored = true;
            for (int i = 0; i < keywords.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", keywords[i], Field.Store.YES));
                doc.Add(NewField("country", unindexed[i], custom1));
                doc.Add(NewTextField("contents", unstored[i], Field.Store.NO));
                doc.Add(NewTextField("city", text[i], Field.Store.YES));
                modifier.AddDocument(doc);
            }
            // flush (and commit if ac)

            if (Verbose)
            {
                Console.WriteLine("TEST: now full merge");
            }

            modifier.ForceMerge(1);
            if (Verbose)
            {
                Console.WriteLine("TEST: now commit");
            }
            modifier.Commit();

            // one of the two files hits

            Term term = new Term("city", "Amsterdam");
            int hitCount = GetHitCount(dir, term);
            Assert.AreEqual(1, hitCount);

            // open the writer again (closed above)

            // delete the doc
            // max buf del terms is two, so this is buffered

            if (Verbose)
            {
                Console.WriteLine("TEST: delete term=" + term);
            }

            modifier.DeleteDocuments(term);

            // add a doc (needed for the !ac case; see below)
            // doc remains buffered

            if (Verbose)
            {
                Console.WriteLine("TEST: add empty doc");
            }
            Document doc_ = new Document();
            modifier.AddDocument(doc_);

            // commit the changes, the buffered deletes, and the new doc

            // The failure object will fail on the first write after the del
            // file gets created when processing the buffered delete

            // in the ac case, this will be when writing the new segments
            // files so we really don't need the new doc, but it's harmless

            // a new segments file won't be created but in this
            // case, creation of the cfs file happens next so we
            // need the doc (to test that it's okay that we don't
            // lose deletes if failing while creating the cfs file)
            bool failed = false;
            try
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: now commit for failure");
                }
                modifier.Commit();
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                // expected
                failed = true;
            }

            Assert.IsTrue(failed);

            // The commit above failed, so we need to retry it (which will
            // succeed, because the failure is a one-shot)

            modifier.Commit();

            hitCount = GetHitCount(dir, term);

            // Make sure the delete was successfully flushed:
            Assert.AreEqual(0, hitCount);

            modifier.Dispose();
            dir.Dispose();
        }

        private sealed class FailureAnonymousClass : Failure
        {
            private readonly TestIndexWriterDelete outerInstance;

            public FailureAnonymousClass(TestIndexWriterDelete outerInstance)
            {
                this.outerInstance = outerInstance;
                sawMaybe = false;
                failed = false;
            }

            internal bool sawMaybe;
            internal bool failed;
            internal Thread thread;

            public override Failure Reset()
            {
                thread = Thread.CurrentThread;
                sawMaybe = false;
                failed = false;
                return this;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (Thread.CurrentThread != thread)
                {
                    // don't fail during merging
                    return;
                }
                if (sawMaybe && !failed)
                {
                    // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                    // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                    bool seen = 
                        StackTraceHelper.DoesStackTraceContainMethod("ApplyDeletesAndUpdates") ||
                        StackTraceHelper.DoesStackTraceContainMethod("SlowFileExists");                 

                    if (!seen)
                    {
                        // Only fail once we are no longer in applyDeletes
                        failed = true;
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: mock failure: now fail");
                            Console.WriteLine(Environment.StackTrace);
                        }
                        throw new IOException("fail after applyDeletes");
                    }
                }
                if (!failed)
                {
                    // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                    // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                    if (StackTraceHelper.DoesStackTraceContainMethod("ApplyDeletesAndUpdates"))
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: mock failure: saw applyDeletes");
                            Console.WriteLine(Environment.StackTrace);
                        }
                        sawMaybe = true;
                    }              
                }
            }
        }

        // this test tests that the files created by the docs writer before
        // a segment is written are cleaned up if there's an i/o error
        [Test]
        public virtual void TestErrorInDocsWriterAdd()
        {
            Failure failure = new FailureAnonymousClass2(this);

            // create a couple of files

            string[] keywords = new string[] { "1", "2" };
            string[] unindexed = new string[] { "Netherlands", "Italy" };
            string[] unstored = new string[] { "Amsterdam has lots of bridges", "Venice has lots of canals" };
            string[] text = new string[] { "Amsterdam", "Venice" };

            MockDirectoryWrapper dir = NewMockDirectory();
            IndexWriter modifier = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            modifier.Commit();
            dir.FailOn(failure.Reset());

            FieldType custom1 = new FieldType();
            custom1.IsStored = true;
            for (int i = 0; i < keywords.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", keywords[i], Field.Store.YES));
                doc.Add(NewField("country", unindexed[i], custom1));
                doc.Add(NewTextField("contents", unstored[i], Field.Store.NO));
                doc.Add(NewTextField("city", text[i], Field.Store.YES));
                try
                {
                    modifier.AddDocument(doc);
                }
                catch (Exception io) when (io.IsIOException())
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: got expected exc:");
                        Console.WriteLine(io.StackTrace);
                    }
                    break;
                }
            }

            modifier.Dispose();
            TestIndexWriter.AssertNoUnreferencedFiles(dir, "docsWriter.abort() failed to delete unreferenced files");
            dir.Dispose();
        }

        private sealed class FailureAnonymousClass2 : Failure
        {
            private readonly TestIndexWriterDelete outerInstance;

            public FailureAnonymousClass2(TestIndexWriterDelete outerInstance)
            {
                this.outerInstance = outerInstance;
                failed = false;
            }

            internal bool failed;

            public override Failure Reset()
            {
                failed = false;
                return this;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (!failed)
                {
                    failed = true;
                    throw new IOException("fail in add doc");
                }
            }
        }

        [Test]
        public virtual void TestDeleteNullQuery()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));

            for (int i = 0; i < 5; i++)
            {
                AddDoc(modifier, i, 2 * i);
            }

            modifier.DeleteDocuments(new TermQuery(new Term("nada", "nada")));
            modifier.Commit();
            Assert.AreEqual(5, modifier.NumDocs);
            modifier.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDeleteAllSlowly()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            int NUM_DOCS = AtLeast(1000);
            IList<int> ids = new JCG.List<int>(NUM_DOCS);
            for (int id = 0; id < NUM_DOCS; id++)
            {
                ids.Add(id);
            }
            ids.Shuffle(Random);
            foreach (int id in ids)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", "" + id, Field.Store.NO));
                w.AddDocument(doc);
            }
            ids.Shuffle(Random);
            int upto = 0;
            while (upto < ids.Count)
            {
                int left = ids.Count - upto;
                int inc = Math.Min(left, TestUtil.NextInt32(Random, 1, 20));
                int limit = upto + inc;
                while (upto < limit)
                {
                    w.DeleteDocuments(new Term("id", "" + ids[upto++]));
                }
                IndexReader r = w.GetReader();
                Assert.AreEqual(NUM_DOCS - upto, r.NumDocs);
                r.Dispose();
            }

            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIndexingThenDeleting()
        {
            // TODO: move this test to its own class and just @SuppressCodecs?
            // TODO: is it enough to just use newFSDirectory?
            string fieldFormat = TestUtil.GetPostingsFormat("field");
            AssumeFalse("this test cannot run with Memory codec", fieldFormat.Equals("Memory", StringComparison.Ordinal));
            AssumeFalse("this test cannot run with SimpleText codec", fieldFormat.Equals("SimpleText", StringComparison.Ordinal));
            AssumeFalse("this test cannot run with Direct codec", fieldFormat.Equals("Direct", StringComparison.Ordinal));
            Random r = Random;
            Directory dir = NewDirectory();
            // note this test explicitly disables payloads
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer.WHITESPACE, true));
            });
            IndexWriter w = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetRAMBufferSizeMB(1.0).SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH));
            Document doc = new Document();
            doc.Add(NewTextField("field", "go 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20", Field.Store.NO));
            int num = AtLeast(3);
            for (int iter = 0; iter < num; iter++)
            {
                int count = 0;

                bool doIndexing = r.NextBoolean();
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter doIndexing=" + doIndexing);
                }
                if (doIndexing)
                {
                    // Add docs until a flush is triggered
                    int startFlushCount = w.FlushCount;
                    while (w.FlushCount == startFlushCount)
                    {
                        w.AddDocument(doc);
                        count++;
                    }
                }
                else
                {
                    // Delete docs until a flush is triggered
                    int startFlushCount = w.FlushCount;
                    while (w.FlushCount == startFlushCount)
                    {
                        w.DeleteDocuments(new Term("foo", "" + count));
                        count++;
                    }
                }
                Assert.IsTrue(count > 2500, "flush happened too quickly during " + (doIndexing ? "indexing" : "deleting") + " count=" + count);
            }
            w.Dispose();
            dir.Dispose();
        }

        // LUCENE-3340: make sure deletes that we don't apply
        // during flush (ie are just pushed into the stream) are
        // in fact later flushed due to their RAM usage:
        [Test]
        public virtual void TestFlushPushedDeletesByRAM()
        {
            Directory dir = NewDirectory();
            // Cannot use RandomIndexWriter because we don't want to
            // ever call commit() for this test:
            // note: tiny rambuffer used, as with a 1MB buffer the test is too slow (flush @ 128,999)
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetRAMBufferSizeMB(0.1f).SetMaxBufferedDocs(1000).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES).SetReaderPooling(false));
            int count = 0;
            while (true)
            {
                Document doc = new Document();
                doc.Add(new StringField("id", count + "", Field.Store.NO));
                Term delTerm;
                if (count == 1010)
                {
                    // this is the only delete that applies
                    delTerm = new Term("id", "" + 0);
                }
                else
                {
                    // These get buffered, taking up RAM, but delete
                    // nothing when applied:
                    delTerm = new Term("id", "x" + count);
                }
                w.UpdateDocument(delTerm, doc);
                // Eventually segment 0 should get a del docs:
                // TODO: fix this test
                if (SlowFileExists(dir, "_0_1.del") || SlowFileExists(dir, "_0_1.liv"))
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: deletes created @ count=" + count);
                    }
                    break;
                }
                count++;

                // Today we applyDeletes @ count=21553; even if we make
                // sizable improvements to RAM efficiency of buffered
                // del term we're unlikely to go over 100K:
                if (count > 100000)
                {
                    Assert.Fail("delete's were not applied");
                }
            }
            w.Dispose();
            dir.Dispose();
        }

        // LUCENE-3340: make sure deletes that we don't apply
        // during flush (ie are just pushed into the stream) are
        // in fact later flushed due to their RAM usage:
        [Test]
        public virtual void TestFlushPushedDeletesByCount()
        {
            Directory dir = NewDirectory();
            // Cannot use RandomIndexWriter because we don't want to
            // ever call commit() for this test:
            int flushAtDelCount = AtLeast(1020);
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDeleteTerms(flushAtDelCount).SetMaxBufferedDocs(1000).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES).SetReaderPooling(false));
            int count = 0;
            while (true)
            {
                Document doc = new Document();
                doc.Add(new StringField("id", count + "", Field.Store.NO));
                Term delTerm;
                if (count == 1010)
                {
                    // this is the only delete that applies
                    delTerm = new Term("id", "" + 0);
                }
                else
                {
                    // These get buffered, taking up RAM, but delete
                    // nothing when applied:
                    delTerm = new Term("id", "x" + count);
                }
                w.UpdateDocument(delTerm, doc);
                // Eventually segment 0 should get a del docs:
                // TODO: fix this test
                if (SlowFileExists(dir, "_0_1.del") || SlowFileExists(dir, "_0_1.liv"))
                {
                    break;
                }
                count++;
                if (count > flushAtDelCount)
                {
                    Assert.Fail("delete's were not applied at count=" + flushAtDelCount);
                }
            }
            w.Dispose();
            dir.Dispose();
        }

        // Make sure buffered (pushed) deletes don't use up so
        // much RAM that it forces long tail of tiny segments:
        [Test]
        [Nightly]
        [Timeout(900_000)] // 15 minutes
        public virtual void TestApplyDeletesOnFlush()
        {
            Directory dir = NewDirectory();
            // Cannot use RandomIndexWriter because we don't want to
            // ever call commit() for this test:
            AtomicInt32 docsInSegment = new AtomicInt32();
            AtomicBoolean closing = new AtomicBoolean();
            AtomicBoolean sawAfterFlush = new AtomicBoolean();
            IndexWriter w = new IndexWriterAnonymousClass(this, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetRAMBufferSizeMB(0.5).SetMaxBufferedDocs(-1).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES).SetReaderPooling(false), docsInSegment, closing, sawAfterFlush);
            int id = 0;
            while (true)
            {
                StringBuilder sb = new StringBuilder();
                for (int termIDX = 0; termIDX < 100; termIDX++)
                {
                    sb.Append(' ').Append(TestUtil.RandomRealisticUnicodeString(Random));
                }
                if (id == 500)
                {
                    w.DeleteDocuments(new Term("id", "0"));
                }
                Document doc = new Document();
                doc.Add(NewStringField("id", "" + id, Field.Store.NO));
                doc.Add(NewTextField("body", sb.ToString(), Field.Store.NO));
                w.UpdateDocument(new Term("id", "" + id), doc);
                docsInSegment.IncrementAndGet();
                // TODO: fix this test
                if (SlowFileExists(dir, "_0_1.del") || SlowFileExists(dir, "_0_1.liv"))
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: deletes created @ id=" + id);
                    }
                    break;
                }
                id++;
            }
            closing.Value = (true);
            Assert.IsTrue(sawAfterFlush);
            w.Dispose();
            dir.Dispose();
        }

        private sealed class IndexWriterAnonymousClass : IndexWriter
        {
            private readonly TestIndexWriterDelete outerInstance;

            private readonly AtomicInt32 docsInSegment;
            private readonly AtomicBoolean closing;
            private readonly AtomicBoolean sawAfterFlush;

            public IndexWriterAnonymousClass(TestIndexWriterDelete outerInstance, Directory dir, IndexWriterConfig setReaderPooling, AtomicInt32 docsInSegment, AtomicBoolean closing, AtomicBoolean sawAfterFlush)
                : base(dir, setReaderPooling)
            {
                this.outerInstance = outerInstance;
                this.docsInSegment = docsInSegment;
                this.closing = closing;
                this.sawAfterFlush = sawAfterFlush;
            }

            protected override void DoAfterFlush()
            {
                Assert.IsTrue(closing || docsInSegment >= 7, "only " + docsInSegment + " in segment");
                docsInSegment.Value = 0;
                sawAfterFlush.Value = (true);
            }
        }

        // LUCENE-4455
        [Test]
        public virtual void TestDeletesCheckIndexOutput()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMaxBufferedDocs(2);
            IndexWriter w = new IndexWriter(dir, (IndexWriterConfig)iwc.Clone());
            Document doc = new Document();
            doc.Add(NewField("field", "0", StringField.TYPE_NOT_STORED));
            w.AddDocument(doc);

            doc = new Document();
            doc.Add(NewField("field", "1", StringField.TYPE_NOT_STORED));
            w.AddDocument(doc);
            w.Commit();
            Assert.AreEqual(1, w.SegmentCount);

            w.DeleteDocuments(new Term("field", "0"));
            w.Commit();
            Assert.AreEqual(1, w.SegmentCount);
            w.Dispose();

            ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
            //MemoryStream bos = new MemoryStream(1024);
            CheckIndex checker = new CheckIndex(dir);
            checker.InfoStream = new StreamWriter(bos, Encoding.UTF8);
            CheckIndex.Status indexStatus = checker.DoCheckIndex(null);
            Assert.IsTrue(indexStatus.Clean);
            checker.FlushInfoStream();
            string s = bos.ToString();

            // Segment should have deletions:
            Assert.IsTrue(s.Contains("has deletions"), "string was: " + s);
            w = new IndexWriter(dir, (IndexWriterConfig)iwc.Clone());
            w.ForceMerge(1);
            w.Dispose();

            bos = new ByteArrayOutputStream(1024);
            checker.InfoStream = new StreamWriter(bos, Encoding.UTF8);
            indexStatus = checker.DoCheckIndex(null);
            Assert.IsTrue(indexStatus.Clean);
            checker.FlushInfoStream();
            s = bos.ToString();
            Assert.IsFalse(s.Contains("has deletions"));
            dir.Dispose();
        }

        [Test]
        public virtual void TestTryDeleteDocument()
        {
            Directory d = NewDirectory();

            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter w = new IndexWriter(d, iwc);
            Document doc = new Document();
            w.AddDocument(doc);
            w.AddDocument(doc);
            w.AddDocument(doc);
            w.Dispose();

            iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetOpenMode(OpenMode.APPEND);
            w = new IndexWriter(d, iwc);
            IndexReader r = DirectoryReader.Open(w, false);
            Assert.IsTrue(w.TryDeleteDocument(r, 1));
            Assert.IsTrue(w.TryDeleteDocument(r.Leaves[0].Reader, 0));
            r.Dispose();
            w.Dispose();

            r = DirectoryReader.Open(d);
            Assert.AreEqual(2, r.NumDeletedDocs);
            Assert.IsNotNull(MultiFields.GetLiveDocs(r));
            r.Dispose();
            d.Dispose();
        }
    }
}