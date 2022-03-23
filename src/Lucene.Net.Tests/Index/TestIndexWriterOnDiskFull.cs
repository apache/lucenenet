using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
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
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using NumericDocValuesField = NumericDocValuesField;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    /// <summary>
    /// Tests for IndexWriter when the disk runs out of space
    /// </summary>
    [TestFixture]
    [Timeout(1_200_000)] // 20 minutes
    public class TestIndexWriterOnDiskFull : LuceneTestCase
    {
        /*
         * Make sure IndexWriter cleans up on hitting a disk
         * full exception in addDocument.
         * TODO: how to do this on windows with FSDirectory?
         */

        [Test]
        public virtual void TestAddDocumentOnDiskFull()
        {
            for (int pass = 0; pass < 2; pass++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: pass=" + pass);
                }
                bool doAbort = pass == 1;
                long diskFree = TestUtil.NextInt32(Random, 100, 300);
                while (true)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: cycle: diskFree=" + diskFree);
                    }
                    MockDirectoryWrapper dir = new MockDirectoryWrapper(Random, new RAMDirectory());
                    dir.MaxSizeInBytes = diskFree;
                    IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                    IMergeScheduler ms = writer.Config.MergeScheduler;
                    if (ms is IConcurrentMergeScheduler)
                    {
                        // this test intentionally produces exceptions
                        // in the threads that CMS launches; we don't
                        // want to pollute test output with these.
                        ((IConcurrentMergeScheduler)ms).SetSuppressExceptions();
                    }

                    bool hitError = false;
                    try
                    {
                        for (int i = 0; i < 200; i++)
                        {
                            AddDoc(writer);
                        }
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: done adding docs; now commit");
                        }
                        writer.Commit();
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: exception on addDoc");
                            Console.WriteLine(e.StackTrace);
                        }
                        hitError = true;
                    }

                    if (hitError)
                    {
                        if (doAbort)
                        {
                            if (Verbose)
                            {
                                Console.WriteLine("TEST: now rollback");
                            }
                            writer.Rollback();
                        }
                        else
                        {
                            try
                            {
                                if (Verbose)
                                {
                                    Console.WriteLine("TEST: now close");
                                }
                                writer.Dispose();
                            }
                            catch (Exception e) when (e.IsIOException())
                            {
                                if (Verbose)
                                {
                                    Console.WriteLine("TEST: exception on close; retry w/ no disk space limit");
                                    Console.WriteLine(e.StackTrace);
                                }
                                dir.MaxSizeInBytes = 0;
                                writer.Dispose();
                            }
                        }

                        //TestUtil.SyncConcurrentMerges(ms);

                        if (TestUtil.AnyFilesExceptWriteLock(dir))
                        {
                            TestIndexWriter.AssertNoUnreferencedFiles(dir, "after disk full during addDocument");

                            // Make sure reader can open the index:
                            DirectoryReader.Open(dir).Dispose();
                        }

                        dir.Dispose();
                        // Now try again w/ more space:

                        diskFree += TestNightly ? TestUtil.NextInt32(Random, 400, 600) : TestUtil.NextInt32(Random, 3000, 5000);
                    }
                    else
                    {
                        //TestUtil.SyncConcurrentMerges(writer);
                        dir.MaxSizeInBytes = 0;
                        writer.Dispose();
                        dir.Dispose();
                        break;
                    }
                }
            }
        }

        // TODO: make @Nightly variant that provokes more disk
        // fulls

        // TODO: have test fail if on any given top
        // iter there was not a single IOE hit

        /*
        Test: make sure when we run out of disk space or hit
        random IOExceptions in any of the addIndexes(*) calls
        that 1) index is not corrupt (searcher can open/search
        it) and 2) transactional semantics are followed:
        either all or none of the incoming documents were in
        fact added.
         */

        [Test]
        public virtual void TestAddIndexOnDiskFull()
        {
            // MemoryCodec, since it uses FST, is not necessarily
            // "additive", ie if you add up N small FSTs, then merge
            // them, the merged result can easily be larger than the
            // sum because the merged FST may use array encoding for
            // some arcs (which uses more space):

            string idFormat = TestUtil.GetPostingsFormat("id");
            string contentFormat = TestUtil.GetPostingsFormat("content");
            AssumeFalse("this test cannot run with Memory codec", idFormat.Equals("Memory", StringComparison.Ordinal) || contentFormat.Equals("Memory", StringComparison.Ordinal));

            int START_COUNT = 57;
            int NUM_DIR = TestNightly ? 50 : 5;
            int END_COUNT = START_COUNT + NUM_DIR * (TestNightly ? 25 : 5);

            // Build up a bunch of dirs that have indexes which we
            // will then merge together by calling addIndexes(*):
            Directory[] dirs = new Directory[NUM_DIR];
            long inputDiskUsage = 0;
            for (int i = 0; i < NUM_DIR; i++)
            {
                dirs[i] = NewDirectory();
                IndexWriter writer = new IndexWriter(dirs[i], NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                for (int j = 0; j < 25; j++)
                {
                    AddDocWithIndex(writer, 25 * i + j);
                }
                writer.Dispose();
                string[] files = dirs[i].ListAll();
                for (int j = 0; j < files.Length; j++)
                {
                    inputDiskUsage += dirs[i].FileLength(files[j]);
                }
            }

            // Now, build a starting index that has START_COUNT docs.  We
            // will then try to addIndexes into a copy of this:
            MockDirectoryWrapper startDir = NewMockDirectory();
            IndexWriter indWriter = new IndexWriter(startDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            for (int j = 0; j < START_COUNT; j++)
            {
                AddDocWithIndex(indWriter, j);
            }
            indWriter.Dispose();

            // Make sure starting index seems to be working properly:
            Term searchTerm = new Term("content", "aaa");
            IndexReader reader = DirectoryReader.Open(startDir);
            Assert.AreEqual(57, reader.DocFreq(searchTerm), "first docFreq");

            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            Assert.AreEqual(57, hits.Length, "first number of hits");
            reader.Dispose();

            // Iterate with larger and larger amounts of free
            // disk space.  With little free disk space,
            // addIndexes will certainly run out of space &
            // fail.  Verify that when this happens, index is
            // not corrupt and index in fact has added no
            // documents.  Then, we increase disk space by 2000
            // bytes each iteration.  At some point there is
            // enough free disk space and addIndexes should
            // succeed and index should show all documents were
            // added.

            // String[] files = startDir.ListAll();
            long diskUsage = startDir.GetSizeInBytes();

            long startDiskUsage = 0;
            string[] files_ = startDir.ListAll();
            for (int i = 0; i < files_.Length; i++)
            {
                startDiskUsage += startDir.FileLength(files_[i]);
            }

            for (int iter = 0; iter < 3; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + iter);
                }

                // Start with 100 bytes more than we are currently using:
                long diskFree = diskUsage + TestUtil.NextInt32(Random, 50, 200);

                int method = iter;

                bool success = false;
                bool done = false;

                string methodName;
                if (0 == method)
                {
                    methodName = "addIndexes(Directory[]) + forceMerge(1)";
                }
                else if (1 == method)
                {
                    methodName = "addIndexes(IndexReader[])";
                }
                else
                {
                    methodName = "addIndexes(Directory[])";
                }

                while (!done)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: cycle...");
                    }

                    // Make a new dir that will enforce disk usage:
                    MockDirectoryWrapper dir = new MockDirectoryWrapper(Random, new RAMDirectory(startDir, NewIOContext(Random)));
                    indWriter = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy(false)));
                    Exception err = null; // LUCENENET: No need to cast to IOExcpetion

                    IMergeScheduler ms = indWriter.Config.MergeScheduler;
                    for (int x = 0; x < 2; x++)
                    {
                        if (ms is IConcurrentMergeScheduler)
                        // this test intentionally produces exceptions
                        // in the threads that CMS launches; we don't
                        // want to pollute test output with these.
                        {
                            if (0 == x)
                            {
                                ((IConcurrentMergeScheduler)ms).SetSuppressExceptions();
                            }
                            else
                            {
                                ((IConcurrentMergeScheduler)ms).ClearSuppressExceptions();
                            }
                        }

                        // Two loops: first time, limit disk space &
                        // throw random IOExceptions; second time, no
                        // disk space limit:

                        double rate = 0.05;
                        double diskRatio = ((double)diskFree) / diskUsage;
                        long thisDiskFree;

                        string testName = null;

                        if (0 == x)
                        {
                            dir.RandomIOExceptionRateOnOpen = Random.NextDouble() * 0.01;
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
                                testName = "disk full test " + methodName + " with disk full at " + diskFree + " bytes";
                            }
                        }
                        else
                        {
                            dir.RandomIOExceptionRateOnOpen = 0.0;
                            thisDiskFree = 0;
                            rate = 0.0;
                            if (Verbose)
                            {
                                testName = "disk full test " + methodName + " with unlimited disk space";
                            }
                        }

                        if (Verbose)
                        {
                            Console.WriteLine("\ncycle: " + testName);
                        }

                        dir.TrackDiskUsage = true;
                        dir.MaxSizeInBytes = thisDiskFree;
                        dir.RandomIOExceptionRate = rate;

                        try
                        {
                            if (0 == method)
                            {
                                if (Verbose)
                                {
                                    Console.WriteLine("TEST: now addIndexes count=" + dirs.Length);
                                }
                                indWriter.AddIndexes(dirs);
                                if (Verbose)
                                {
                                    Console.WriteLine("TEST: now forceMerge");
                                }
                                indWriter.ForceMerge(1);
                            }
                            else if (1 == method)
                            {
                                IndexReader[] readers = new IndexReader[dirs.Length];
                                for (int i = 0; i < dirs.Length; i++)
                                {
                                    readers[i] = DirectoryReader.Open(dirs[i]);
                                }
                                try
                                {
                                    indWriter.AddIndexes(readers);
                                }
                                finally
                                {
                                    for (int i = 0; i < dirs.Length; i++)
                                    {
                                        readers[i].Dispose();
                                    }
                                }
                            }
                            else
                            {
                                indWriter.AddIndexes(dirs);
                            }

                            success = true;
                            if (Verbose)
                            {
                                Console.WriteLine("  success!");
                            }

                            if (0 == x)
                            {
                                done = true;
                            }
                        }
                        catch (Exception e) when (e.IsIOException())
                        {
                            success = false;
                            err = e;
                            if (Verbose)
                            {
                                Console.WriteLine("  hit IOException: " + e);
                                Console.WriteLine(e.StackTrace);
                            }

                            if (1 == x)
                            {
                                Console.WriteLine(e.StackTrace);
                                Assert.Fail(methodName + " hit IOException after disk space was freed up");
                            }
                        }

                        // Make sure all threads from
                        // ConcurrentMergeScheduler are done
                        TestUtil.SyncConcurrentMerges(indWriter);

                        if (Verbose)
                        {
                            Console.WriteLine("  now test readers");
                        }

                        // Finally, verify index is not corrupt, and, if
                        // we succeeded, we see all docs added, and if we
                        // failed, we see either all docs or no docs added
                        // (transactional semantics):
                        dir.RandomIOExceptionRateOnOpen = 0.0;
                        try
                        {
                            reader = DirectoryReader.Open(dir);
                        }
                        catch (Exception e) when (e.IsIOException())
                        {
                            Console.WriteLine(e.StackTrace);
                            Assert.Fail(testName + ": exception when creating IndexReader: " + e);
                        }
                        int result = reader.DocFreq(searchTerm);
                        if (success)
                        {
                            if (result != START_COUNT)
                            {
                                Assert.Fail(testName + ": method did not throw exception but docFreq('aaa') is " + result + " instead of expected " + START_COUNT);
                            }
                        }
                        else
                        {
                            // On hitting exception we still may have added
                            // all docs:
                            if (result != START_COUNT && result != END_COUNT)
                            {
                                Console.WriteLine(err.StackTrace);
                                Assert.Fail(testName + ": method did throw exception but docFreq('aaa') is " + result + " instead of expected " + START_COUNT + " or " + END_COUNT);
                            }
                        }

                        searcher = NewSearcher(reader);
                        try
                        {
                            hits = searcher.Search(new TermQuery(searchTerm), null, END_COUNT).ScoreDocs;
                        }
                        catch (Exception e) when (e.IsIOException())
                        {
                            Console.WriteLine(e.StackTrace);
                            Assert.Fail(testName + ": exception when searching: " + e);
                        }
                        int result2 = hits.Length;
                        if (success)
                        {
                            if (result2 != result)
                            {
                                Assert.Fail(testName + ": method did not throw exception but hits.Length for search on term 'aaa' is " + result2 + " instead of expected " + result);
                            }
                        }
                        else
                        {
                            // On hitting exception we still may have added
                            // all docs:
                            if (result2 != result)
                            {
                                Console.WriteLine(err.StackTrace);
                                Assert.Fail(testName + ": method did throw exception but hits.Length for search on term 'aaa' is " + result2 + " instead of expected " + result);
                            }
                        }

                        reader.Dispose();
                        if (Verbose)
                        {
                            Console.WriteLine("  count is " + result);
                        }

                        if (done || result == END_COUNT)
                        {
                            break;
                        }
                    }

                    if (Verbose)
                    {
                        Console.WriteLine("  start disk = " + startDiskUsage + "; input disk = " + inputDiskUsage + "; max used = " + dir.MaxUsedSizeInBytes);
                    }

                    if (done)
                    {
                        // Javadocs state that temp free Directory space
                        // required is at most 2X total input size of
                        // indices so let's make sure:
                        Assert.IsTrue((dir.MaxUsedSizeInBytes - startDiskUsage) < 2 * (startDiskUsage + inputDiskUsage), "max free Directory space required exceeded 1X the total input index sizes during " + methodName + ": max temp usage = " + (dir.MaxUsedSizeInBytes - startDiskUsage) + " bytes vs limit=" + (2 * (startDiskUsage + inputDiskUsage)) + "; starting disk usage = " + startDiskUsage + " bytes; " + "input index disk usage = " + inputDiskUsage + " bytes");
                    }

                    // Make sure we don't hit disk full during close below:
                    dir.MaxSizeInBytes = 0;
                    dir.RandomIOExceptionRate = 0.0;
                    dir.RandomIOExceptionRateOnOpen = 0.0;

                    indWriter.Dispose();

                    // Wait for all BG threads to finish else
                    // dir.Dispose() will throw IOException because
                    // there are still open files
                    TestUtil.SyncConcurrentMerges(ms);

                    dir.Dispose();

                    // Try again with more free space:
                    diskFree += TestNightly ? TestUtil.NextInt32(Random, 4000, 8000) : TestUtil.NextInt32(Random, 40000, 80000);
                }
            }

            startDir.Dispose();
            foreach (Directory dir in dirs)
            {
                dir.Dispose();
            }
        }

        private class FailTwiceDuringMerge : Failure
        {
            public bool didFail1;
            public bool didFail2;

            public override void Eval(MockDirectoryWrapper dir)
            {
                if (!m_doFail)
                {
                    return;
                }

                // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                if (StackTraceHelper.DoesStackTraceContainMethod(typeof(SegmentMerger).Name, "MergeTerms") && !didFail1)
                {
                    didFail1 = true;
                    throw new IOException("fake disk full during mergeTerms");
                }

                // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                if (StackTraceHelper.DoesStackTraceContainMethod(typeof(LiveDocsFormat).Name, "WriteLiveDocs") && !didFail2)
                {
                    didFail2 = true;
                    throw new IOException("fake disk full while writing LiveDocs");
                }
            }
        }

        // LUCENE-2593
        [Test]
        public virtual void TestCorruptionAfterDiskFullDuringMerge()
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            //IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setReaderPooling(true));
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(new SerialMergeScheduler()).SetReaderPooling(true).SetMergePolicy(NewLogMergePolicy(2)));
            // we can do this because we add/delete/add (and dont merge to "nothing")
            w.KeepFullyDeletedSegments = true;

            Document doc = new Document();

            doc.Add(NewTextField("f", "doctor who", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();

            w.DeleteDocuments(new Term("f", "who"));
            w.AddDocument(doc);

            // disk fills up!
            FailTwiceDuringMerge ftdm = new FailTwiceDuringMerge();
            ftdm.SetDoFail();
            dir.FailOn(ftdm);

            try
            {
                w.Commit();
                Assert.Fail("fake disk full IOExceptions not hit");
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                // expected
                Assert.IsTrue(ftdm.didFail1 || ftdm.didFail2);
            }
            TestUtil.CheckIndex(dir);
            ftdm.ClearDoFail();
            w.AddDocument(doc);
            w.Dispose();

            dir.Dispose();
        }

        // LUCENE-1130: make sure immeidate disk full on creating
        // an IndexWriter (hit during DW.ThreadState.Init()) is
        // OK:
        [Test]
        public virtual void TestImmediateDiskFull()
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                            .SetMaxBufferedDocs(2)
                            .SetMergeScheduler(new ConcurrentMergeScheduler());
            IndexWriter writer = new IndexWriter(dir, config);
            dir.MaxSizeInBytes = Math.Max(1, dir.GetRecomputedActualSizeInBytes());
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            doc.Add(NewField("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj", customType));
            try
            {
                writer.AddDocument(doc);
                Assert.Fail("did not hit disk full");
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
            }
            // Without fix for LUCENE-1130: this call will hang:
            try
            {
                writer.AddDocument(doc);
                Assert.Fail("did not hit disk full");
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
            }
            try
            {
                writer.Dispose(false);
                Assert.Fail("did not hit disk full");
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
            }

            // Make sure once disk space is avail again, we can
            // cleanly close:
            dir.MaxSizeInBytes = 0;
            writer.Dispose(false);
            dir.Dispose();
        }

        // TODO: these are also in TestIndexWriter... add a simple doc-writing method
        // like this to LuceneTestCase?
        private void AddDoc(IndexWriter writer)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            if (DefaultCodecSupportsDocValues)
            {
                doc.Add(new NumericDocValuesField("numericdv", 1));
            }
            writer.AddDocument(doc);
        }

        private void AddDocWithIndex(IndexWriter writer, int index)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa " + index, Field.Store.NO));
            doc.Add(NewTextField("id", "" + index, Field.Store.NO));
            if (DefaultCodecSupportsDocValues)
            {
                doc.Add(new NumericDocValuesField("numericdv", 1));
            }
            writer.AddDocument(doc);
        }
    }
}