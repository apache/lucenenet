using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

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
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using StringField = StringField;

    [TestFixture]
    public class TestSizeBoundedForceMerge : LuceneTestCase
    {
        private void AddDocs(IndexWriter writer, int numDocs)
        {
            AddDocs(writer, numDocs, false);
        }

        private void AddDocs(IndexWriter writer, int numDocs, bool withID)
        {
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                if (withID)
                {
                    doc.Add(new StringField("id", "" + i, Field.Store.NO));
                }
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        private IndexWriterConfig NewWriterConfig()
        {
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
            conf.SetRAMBufferSizeMB(IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB);
            // prevent any merges by default.
            conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
            return conf;
        }

        [Test]
        public virtual void TestByteSizeLimit()
        {
            // tests that the max merge size constraint is applied during forceMerge.
            Directory dir = new RAMDirectory();

            // Prepare an index w/ several small segments and a large one.
            IndexWriterConfig conf = NewWriterConfig();
            IndexWriter writer = new IndexWriter(dir, conf);
            const int numSegments = 15;
            for (int i = 0; i < numSegments; i++)
            {
                int numDocs = i == 7 ? 30 : 1;
                AddDocs(writer, numDocs);
            }
            writer.Dispose();

            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            double min = sis[0].GetSizeInBytes();

            conf = NewWriterConfig();
            LogByteSizeMergePolicy lmp = new LogByteSizeMergePolicy();
            lmp.MaxMergeMBForForcedMerge = (min + 1) / (1 << 20);
            conf.SetMergePolicy(lmp);

            writer = new IndexWriter(dir, conf);
            writer.ForceMerge(1);
            writer.Dispose();

            // Should only be 3 segments in the index, because one of them exceeds the size limit
            sis = new SegmentInfos();
            sis.Read(dir);
            Assert.AreEqual(3, sis.Count);
        }

        [Test]
        public virtual void TestNumDocsLimit()
        {
            // tests that the max merge docs constraint is applied during forceMerge.
            Directory dir = new RAMDirectory();

            // Prepare an index w/ several small segments and a large one.
            IndexWriterConfig conf = NewWriterConfig();
            IndexWriter writer = new IndexWriter(dir, conf);

            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 5);
            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 3);

            writer.Dispose();

            conf = NewWriterConfig();
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MaxMergeDocs = 3;
            conf.SetMergePolicy(lmp);

            writer = new IndexWriter(dir, conf);
            writer.ForceMerge(1);
            writer.Dispose();

            // Should only be 3 segments in the index, because one of them exceeds the size limit
            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            Assert.AreEqual(3, sis.Count);
        }

        [Test]
        public virtual void TestLastSegmentTooLarge()
        {
            Directory dir = new RAMDirectory();

            IndexWriterConfig conf = NewWriterConfig();
            IndexWriter writer = new IndexWriter(dir, conf);

            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 5);

            writer.Dispose();

            conf = NewWriterConfig();
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MaxMergeDocs = 3;
            conf.SetMergePolicy(lmp);

            writer = new IndexWriter(dir, conf);
            writer.ForceMerge(1);
            writer.Dispose();

            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            Assert.AreEqual(2, sis.Count);
        }

        [Test]
        public virtual void TestFirstSegmentTooLarge()
        {
            Directory dir = new RAMDirectory();

            IndexWriterConfig conf = NewWriterConfig();
            IndexWriter writer = new IndexWriter(dir, conf);

            AddDocs(writer, 5);
            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 3);

            writer.Dispose();

            conf = NewWriterConfig();
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MaxMergeDocs = 3;
            conf.SetMergePolicy(lmp);

            writer = new IndexWriter(dir, conf);
            writer.ForceMerge(1);
            writer.Dispose();

            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            Assert.AreEqual(2, sis.Count);
        }

        [Test]
        public virtual void TestAllSegmentsSmall()
        {
            Directory dir = new RAMDirectory();

            IndexWriterConfig conf = NewWriterConfig();
            IndexWriter writer = new IndexWriter(dir, conf);

            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 3);

            writer.Dispose();

            conf = NewWriterConfig();
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MaxMergeDocs = 3;
            conf.SetMergePolicy(lmp);

            writer = new IndexWriter(dir, conf);
            writer.ForceMerge(1);
            writer.Dispose();

            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            Assert.AreEqual(1, sis.Count);
        }

        [Test]
        public virtual void TestAllSegmentsLarge()
        {
            Directory dir = new RAMDirectory();

            IndexWriterConfig conf = NewWriterConfig();
            IndexWriter writer = new IndexWriter(dir, conf);

            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 3);

            writer.Dispose();

            conf = NewWriterConfig();
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MaxMergeDocs = 2;
            conf.SetMergePolicy(lmp);

            writer = new IndexWriter(dir, conf);
            writer.ForceMerge(1);
            writer.Dispose();

            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            Assert.AreEqual(3, sis.Count);
        }

        [Test]
        public virtual void TestOneLargeOneSmall()
        {
            Directory dir = new RAMDirectory();

            IndexWriterConfig conf = NewWriterConfig();
            IndexWriter writer = new IndexWriter(dir, conf);

            AddDocs(writer, 3);
            AddDocs(writer, 5);
            AddDocs(writer, 3);
            AddDocs(writer, 5);

            writer.Dispose();

            conf = NewWriterConfig();
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MaxMergeDocs = 3;
            conf.SetMergePolicy(lmp);

            writer = new IndexWriter(dir, conf);
            writer.ForceMerge(1);
            writer.Dispose();

            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            Assert.AreEqual(4, sis.Count);
        }

        [Test]
        public virtual void TestMergeFactor()
        {
            Directory dir = new RAMDirectory();

            IndexWriterConfig conf = NewWriterConfig();
            IndexWriter writer = new IndexWriter(dir, conf);

            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 3);
            AddDocs(writer, 5);
            AddDocs(writer, 3);
            AddDocs(writer, 3);

            writer.Dispose();

            conf = NewWriterConfig();
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MaxMergeDocs = 3;
            lmp.MergeFactor = 2;
            conf.SetMergePolicy(lmp);

            writer = new IndexWriter(dir, conf);
            writer.ForceMerge(1);
            writer.Dispose();

            // Should only be 4 segments in the index, because of the merge factor and
            // max merge docs settings.
            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            Assert.AreEqual(4, sis.Count);
        }

        [Test]
        public virtual void TestSingleMergeableSegment()
        {
            Directory dir = new RAMDirectory();

            IndexWriterConfig conf = NewWriterConfig();
            IndexWriter writer = new IndexWriter(dir, conf);

            AddDocs(writer, 3);
            AddDocs(writer, 5);
            AddDocs(writer, 3);

            // delete the last document, so that the last segment is merged.
            writer.DeleteDocuments(new Term("id", "10"));
            writer.Dispose();

            conf = NewWriterConfig();
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MaxMergeDocs = 3;
            conf.SetMergePolicy(lmp);

            writer = new IndexWriter(dir, conf);
            writer.ForceMerge(1);
            writer.Dispose();

            // Verify that the last segment does not have deletions.
            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            Assert.AreEqual(3, sis.Count);
            Assert.IsFalse(sis[2].HasDeletions);
        }

        [Test]
        public virtual void TestSingleNonMergeableSegment()
        {
            Directory dir = new RAMDirectory();

            IndexWriterConfig conf = NewWriterConfig();
            IndexWriter writer = new IndexWriter(dir, conf);

            AddDocs(writer, 3, true);

            writer.Dispose();

            conf = NewWriterConfig();
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MaxMergeDocs = 3;
            conf.SetMergePolicy(lmp);

            writer = new IndexWriter(dir, conf);
            writer.ForceMerge(1);
            writer.Dispose();

            // Verify that the last segment does not have deletions.
            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            Assert.AreEqual(1, sis.Count);
        }

        [Test]
        public virtual void TestSingleMergeableTooLargeSegment()
        {
            Directory dir = new RAMDirectory();

            IndexWriterConfig conf = NewWriterConfig();
            IndexWriter writer = new IndexWriter(dir, conf);

            AddDocs(writer, 5, true);

            // delete the last document

            writer.DeleteDocuments(new Term("id", "4"));
            writer.Dispose();

            conf = NewWriterConfig();
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MaxMergeDocs = 2;
            conf.SetMergePolicy(lmp);

            writer = new IndexWriter(dir, conf);
            writer.ForceMerge(1);
            writer.Dispose();

            // Verify that the last segment does not have deletions.
            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            Assert.AreEqual(1, sis.Count);
            Assert.IsTrue(sis[0].HasDeletions);
        }
    }
}