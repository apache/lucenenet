using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
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
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using NoLockFactory = Lucene.Net.Store.NoLockFactory;

    [TestFixture]
    public class TestCrash : LuceneTestCase
    {
        private IndexWriter InitIndex(Random random, bool initialCommit)
        {
            return InitIndex(random, NewMockDirectory(random), initialCommit);
        }

        private IndexWriter InitIndex(Random random, MockDirectoryWrapper dir, bool initialCommit)
        {
            dir.SetLockFactory(NoLockFactory.GetNoLockFactory());

            IndexWriter writer = new IndexWriter(dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random))
                .SetMaxBufferedDocs(10)
                .SetMergeScheduler(new ConcurrentMergeScheduler()));

            IConcurrentMergeScheduler scheduler = writer.Config.MergeScheduler as IConcurrentMergeScheduler;
            if (scheduler != null)
            {
                scheduler.SetSuppressExceptions();
            }

            if (initialCommit)
            {
                writer.Commit();
            }

            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            doc.Add(NewTextField("id", "0", Field.Store.NO));
            for (int i = 0; i < 157; i++)
            {
                writer.AddDocument(doc);
            }

            return writer;
        }

        private void Crash(IndexWriter writer)
        {
            MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.Directory;
            var cms = (IConcurrentMergeScheduler)writer.Config.MergeScheduler;
            cms.Sync();
            dir.Crash();
            cms.Sync();
            dir.ClearCrash();
        }

        [Test]
        public virtual void TestCrashWhileIndexing()
        {
            // this test relies on being able to open a reader before any commit
            // happened, so we must create an initial commit just to allow that, but
            // before any documents were added.
            IndexWriter writer = InitIndex(Random, true);
            MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.Directory;

            // We create leftover files because merging could be
            // running when we crash:
            dir.AssertNoUnreferencedFilesOnDispose = false;

            Crash(writer);

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.IsTrue(reader.NumDocs < 157);
            reader.Dispose();

            // Make a new dir, copying from the crashed dir, and
            // open IW on it, to confirm IW "recovers" after a
            // crash:
            Directory dir2 = NewDirectory(dir);
            dir.Dispose();

            (new RandomIndexWriter(Random, dir2)).Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestWriterAfterCrash()
        {
            // this test relies on being able to open a reader before any commit
            // happened, so we must create an initial commit just to allow that, but
            // before any documents were added.
            Console.WriteLine("TEST: initIndex");
            IndexWriter writer = InitIndex(Random, true);
            Console.WriteLine("TEST: done initIndex");
            MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.Directory;

            // We create leftover files because merging could be
            // running / store files could be open when we crash:
            dir.AssertNoUnreferencedFilesOnDispose = false;

            dir.PreventDoubleWrite = false;
            Console.WriteLine("TEST: now crash");
            Crash(writer);
            writer = InitIndex(Random, dir, false);
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.IsTrue(reader.NumDocs < 314);
            reader.Dispose();

            // Make a new dir, copying from the crashed dir, and
            // open IW on it, to confirm IW "recovers" after a
            // crash:
            Directory dir2 = NewDirectory(dir);
            dir.Dispose();

            (new RandomIndexWriter(Random, dir2)).Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestCrashAfterReopen()
        {
            IndexWriter writer = InitIndex(Random, false);
            MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.Directory;

            // We create leftover files because merging could be
            // running when we crash:
            dir.AssertNoUnreferencedFilesOnDispose = false;

            writer.Dispose();
            writer = InitIndex(Random, dir, false);
            Assert.AreEqual(314, writer.MaxDoc);
            Crash(writer);

            /*
            System.out.println("\n\nTEST: open reader");
            String[] l = dir.list();
            Arrays.sort(l);
            for(int i=0;i<l.Length;i++)
              System.out.println("file " + i + " = " + l[i] + " " +
            dir.FileLength(l[i]) + " bytes");
            */

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.IsTrue(reader.NumDocs >= 157);
            reader.Dispose();

            // Make a new dir, copying from the crashed dir, and
            // open IW on it, to confirm IW "recovers" after a
            // crash:
            Directory dir2 = NewDirectory(dir);
            dir.Dispose();

            (new RandomIndexWriter(Random, dir2)).Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestCrashAfterClose()
        {
            IndexWriter writer = InitIndex(Random, false);
            MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.Directory;

            writer.Dispose();
            dir.Crash();

            /*
            String[] l = dir.list();
            Arrays.sort(l);
            for(int i=0;i<l.Length;i++)
              System.out.println("file " + i + " = " + l[i] + " " + dir.FileLength(l[i]) + " bytes");
            */

            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(157, reader.NumDocs);
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestCrashAfterCloseNoWait()
        {
            IndexWriter writer = InitIndex(Random, false);
            MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.Directory;

            writer.Dispose(false);

            dir.Crash();

            /*
            String[] l = dir.list();
            Arrays.sort(l);
            for(int i=0;i<l.Length;i++)
              System.out.println("file " + i + " = " + l[i] + " " + dir.FileLength(l[i]) + " bytes");
            */
            IndexReader reader = DirectoryReader.Open(dir);
            Assert.AreEqual(157, reader.NumDocs);
            reader.Dispose();
            dir.Dispose();
        }
    }
}