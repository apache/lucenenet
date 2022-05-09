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
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestIndexWriterForceMerge : LuceneTestCase
    {
        private static readonly FieldType storedTextType = new FieldType(TextField.TYPE_NOT_STORED);

        [Test]
        [Slow] // Occasionally
        [Timeout(1_200_000)] // 20 minutes
        public virtual void TestPartialMerge()
        {
            Directory dir = NewDirectory();

            Document doc = new Document();
            doc.Add(NewStringField("content", "aaa", Field.Store.NO));
            int incrMin = TestNightly ? 15 : 40;
            for (int numDocs = 10; numDocs < 500; numDocs += TestUtil.NextInt32(Random, incrMin, 5 * incrMin))
            {
                LogDocMergePolicy ldmp = new LogDocMergePolicy();
                ldmp.MinMergeDocs = 1;
                ldmp.MergeFactor = 5;
                IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(2).SetMergePolicy(ldmp));
                for (int j = 0; j < numDocs; j++)
                {
                    writer.AddDocument(doc);
                }
                writer.Dispose();

                SegmentInfos sis = new SegmentInfos();
                sis.Read(dir);
                int segCount = sis.Count;

                ldmp = new LogDocMergePolicy();
                ldmp.MergeFactor = 5;
                writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(ldmp));
                writer.ForceMerge(3);
                writer.Dispose();

                sis = new SegmentInfos();
                sis.Read(dir);
                int optSegCount = sis.Count;

                if (segCount < 3)
                {
                    Assert.AreEqual(segCount, optSegCount);
                }
                else
                {
                    Assert.AreEqual(3, optSegCount);
                }
            }
            dir.Dispose();
        }

        [Test]
        public virtual void TestMaxNumSegments2()
        {
            Directory dir = NewDirectory();

            Document doc = new Document();
            doc.Add(NewStringField("content", "aaa", Field.Store.NO));

            LogDocMergePolicy ldmp = new LogDocMergePolicy();
            ldmp.MinMergeDocs = 1;
            ldmp.MergeFactor = 4;
            var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                            .SetMaxBufferedDocs(2)
                            .SetMergePolicy(ldmp)
                            .SetMergeScheduler(new ConcurrentMergeScheduler());
            IndexWriter writer = new IndexWriter(dir, config);

            for (int iter = 0; iter < 10; iter++)
            {
                for (int i = 0; i < 19; i++)
                {
                    writer.AddDocument(doc);
                }

                writer.Commit();
                writer.WaitForMerges();
                writer.Commit();

                SegmentInfos sis = new SegmentInfos();
                sis.Read(dir);

                int segCount = sis.Count;
                writer.ForceMerge(7);
                writer.Commit();
                writer.WaitForMerges();

                sis = new SegmentInfos();
                sis.Read(dir);
                int optSegCount = sis.Count;

                if (segCount < 7)
                {
                    Assert.AreEqual(segCount, optSegCount);
                }
                else
                {
                    Assert.AreEqual(7, optSegCount, "seg: " + segCount);
                }
            }
            writer.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Make sure forceMerge doesn't use any more than 1X
        /// starting index size as its temporary free space
        /// required.
        /// </summary>
        [Test]
        public virtual void TestForceMergeTempSpaceUsage()
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(10).SetMergePolicy(NewLogMergePolicy()));
            if (Verbose)
            {
                Console.WriteLine("TEST: config1=" + writer.Config);
            }

            for (int j = 0; j < 500; j++)
            {
                AddDocWithIndex(writer, j);
            }
            int termIndexInterval = writer.Config.TermIndexInterval;
            // force one extra segment w/ different doc store so
            // we see the doc stores get merged
            writer.Commit();
            AddDocWithIndex(writer, 500);
            writer.Dispose();

            if (Verbose)
            {
                Console.WriteLine("TEST: start disk usage");
            }
            long startDiskUsage = 0;
            string[] files = dir.ListAll();
            for (int i = 0; i < files.Length; i++)
            {
                startDiskUsage += dir.FileLength(files[i]);
                if (Verbose)
                {
                    Console.WriteLine(files[i] + ": " + dir.FileLength(files[i]));
                }
            }

            dir.ResetMaxUsedSizeInBytes();
            dir.TrackDiskUsage = true;

            // Import to use same term index interval else a
            // smaller one here could increase the disk usage and
            // cause a false failure:
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND).SetTermIndexInterval(termIndexInterval).SetMergePolicy(NewLogMergePolicy()));
            writer.ForceMerge(1);
            writer.Dispose();
            long maxDiskUsage = dir.MaxUsedSizeInBytes;
            Assert.IsTrue(maxDiskUsage <= 4 * startDiskUsage, "forceMerge used too much temporary space: starting usage was " + startDiskUsage + " bytes; max temp usage was " + maxDiskUsage + " but should have been " + (4 * startDiskUsage) + " (= 4X starting usage)");
            dir.Dispose();
        }

        // Test calling forceMerge(1, false) whereby forceMerge is kicked
        // off but we don't wait for it to finish (but
        // writer.Dispose()) does wait
        [Test]
        public virtual void TestBackgroundForceMerge()
        {
            Directory dir = NewDirectory();
            for (int pass = 0; pass < 2; pass++)
            {
                IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(51)));
                Document doc = new Document();
                doc.Add(NewStringField("field", "aaa", Field.Store.NO));
                for (int i = 0; i < 100; i++)
                {
                    writer.AddDocument(doc);
                }
                writer.ForceMerge(1, false);

                if (0 == pass)
                {
                    writer.Dispose();
                    DirectoryReader reader = DirectoryReader.Open(dir);
                    Assert.AreEqual(1, reader.Leaves.Count);
                    reader.Dispose();
                }
                else
                {
                    // Get another segment to flush so we can verify it is
                    // NOT included in the merging
                    writer.AddDocument(doc);
                    writer.AddDocument(doc);
                    writer.Dispose();

                    DirectoryReader reader = DirectoryReader.Open(dir);
                    Assert.IsTrue(reader.Leaves.Count > 1);
                    reader.Dispose();

                    SegmentInfos infos = new SegmentInfos();
                    infos.Read(dir);
                    Assert.AreEqual(2, infos.Count);
                }
            }

            dir.Dispose();
        }

        /// <summary>
        /// LUCENENET specific
        ///
        /// Copied from <seealso cref="TestIndexWriter.AddDoc(IndexWriter)"/>
        /// to remove inter-class dependency on TestIndexWriter.
        /// </summary>
        private void AddDoc(IndexWriter writer)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            writer.AddDocument(doc);
        }

        private void AddDocWithIndex(IndexWriter writer, int index)
        {
            Document doc = new Document();
            doc.Add(NewField("content", "aaa " + index, storedTextType));
            doc.Add(NewField("id", "" + index, storedTextType));
            writer.AddDocument(doc);
        }

    }
}