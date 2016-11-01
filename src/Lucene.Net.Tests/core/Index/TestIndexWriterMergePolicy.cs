using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;

    [TestFixture]
    public class TestIndexWriterMergePolicy : LuceneTestCase
    {
        // Test the normal case
        [Test]
        public virtual void TestNormalCase()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10).SetMergePolicy(new LogDocMergePolicy()));

            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
                CheckInvariants(writer);
            }

            writer.Dispose();
            dir.Dispose();
        }

        // Test to see if there is over merge
        [Test]
        public virtual void TestNoOverMerge()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10).SetMergePolicy(new LogDocMergePolicy()));

            bool noOverMerge = false;
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
                CheckInvariants(writer);
                if (writer.NumBufferedDocuments + writer.SegmentCount >= 18)
                {
                    noOverMerge = true;
                }
            }
            Assert.IsTrue(noOverMerge);

            writer.Dispose();
            dir.Dispose();
        }

        // Test the case where flush is forced after every addDoc
        [Test]
        public virtual void TestForceFlush()
        {
            Directory dir = NewDirectory();

            LogDocMergePolicy mp = new LogDocMergePolicy();
            mp.MinMergeDocs = 100;
            mp.MergeFactor = 10;
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10).SetMergePolicy(mp));

            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
                writer.Dispose();

                mp = new LogDocMergePolicy();
                mp.MergeFactor = 10;
                writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetMaxBufferedDocs(10).SetMergePolicy(mp));
                mp.MinMergeDocs = 100;
                CheckInvariants(writer);
            }

            writer.Dispose();
            dir.Dispose();
        }

        // Test the case where mergeFactor changes
        [Test]
        public virtual void TestMergeFactorChange()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10).SetMergePolicy(NewLogMergePolicy()).SetMergeScheduler(new SerialMergeScheduler()));

            for (int i = 0; i < 250; i++)
            {
                AddDoc(writer);
                CheckInvariants(writer);
            }

            ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = 5;

            // merge policy only fixes segments on levels where merges
            // have been triggered, so check invariants after all adds
            for (int i = 0; i < 10; i++)
            {
                AddDoc(writer);
            }
            CheckInvariants(writer);

            writer.Dispose();
            dir.Dispose();
        }

        // Test the case where both mergeFactor and maxBufferedDocs change
        [Test]
        public virtual void TestMaxBufferedDocsChange()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(101).SetMergePolicy(new LogDocMergePolicy()).SetMergeScheduler(new SerialMergeScheduler()));

            // leftmost* segment has 1 doc
            // rightmost* segment has 100 docs
            for (int i = 1; i <= 100; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    AddDoc(writer);
                    CheckInvariants(writer);
                }
                writer.Dispose();

                writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetMaxBufferedDocs(101).SetMergePolicy(new LogDocMergePolicy()).SetMergeScheduler(new SerialMergeScheduler()));
            }

            writer.Dispose();
            LogDocMergePolicy ldmp = new LogDocMergePolicy();
            ldmp.MergeFactor = 10;
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetMaxBufferedDocs(10).SetMergePolicy(ldmp).SetMergeScheduler(new SerialMergeScheduler()));

            // merge policy only fixes segments on levels where merges
            // have been triggered, so check invariants after all adds
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
            CheckInvariants(writer);

            for (int i = 100; i < 1000; i++)
            {
                AddDoc(writer);
            }
            writer.Commit();
            writer.WaitForMerges();
            writer.Commit();
            CheckInvariants(writer);

            writer.Dispose();
            dir.Dispose();
        }

        // Test the case where a merge results in no doc at all
        [Test]
        public virtual void TestMergeDocCount0([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            Directory dir = NewDirectory();

            LogDocMergePolicy ldmp = new LogDocMergePolicy();
            ldmp.MergeFactor = 100;
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10).SetMergePolicy(ldmp));

            for (int i = 0; i < 250; i++)
            {
                AddDoc(writer);
                CheckInvariants(writer);
            }
            writer.Dispose();

            // delete some docs without merging
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
            writer.DeleteDocuments(new Term("content", "aaa"));
            writer.Dispose();

            ldmp = new LogDocMergePolicy();
            ldmp.MergeFactor = 5;
            var config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                .SetOpenMode(OpenMode_e.APPEND)
                .SetMaxBufferedDocs(10)
                .SetMergePolicy(ldmp)
                .SetMergeScheduler(scheduler);
            writer = new IndexWriter(dir, config);

            // merge factor is changed, so check invariants after all adds
            for (int i = 0; i < 10; i++)
            {
                AddDoc(writer);
            }
            writer.Commit();
            writer.WaitForMerges();
            writer.Commit();
            CheckInvariants(writer);
            Assert.AreEqual(10, writer.MaxDoc);

            writer.Dispose();
            dir.Dispose();
        }

        private void AddDoc(IndexWriter writer)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            writer.AddDocument(doc);
        }

        private void CheckInvariants(IndexWriter writer)
        {
            writer.WaitForMerges();
            int maxBufferedDocs = writer.Config.MaxBufferedDocs;
            int mergeFactor = ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor;
            int maxMergeDocs = ((LogMergePolicy)writer.Config.MergePolicy).MaxMergeDocs;

            int ramSegmentCount = writer.NumBufferedDocuments;
            Assert.IsTrue(ramSegmentCount < maxBufferedDocs);

            int lowerBound = -1;
            int upperBound = maxBufferedDocs;
            int numSegments = 0;

            int segmentCount = writer.SegmentCount;
            for (int i = segmentCount - 1; i >= 0; i--)
            {
                int docCount = writer.GetDocCount(i);
                Assert.IsTrue(docCount > lowerBound, "docCount=" + docCount + " lowerBound=" + lowerBound + " upperBound=" + upperBound + " i=" + i + " segmentCount=" + segmentCount + " index=" + writer.SegString() + " config=" + writer.Config);

                if (docCount <= upperBound)
                {
                    numSegments++;
                }
                else
                {
                    if (upperBound * mergeFactor <= maxMergeDocs)
                    {
                        Assert.IsTrue(numSegments < mergeFactor, "maxMergeDocs=" + maxMergeDocs + "; numSegments=" + numSegments + "; upperBound=" + upperBound + "; mergeFactor=" + mergeFactor + "; segs=" + writer.SegString() + " config=" + writer.Config);
                    }

                    do
                    {
                        lowerBound = upperBound;
                        upperBound *= mergeFactor;
                    } while (docCount > upperBound);
                    numSegments = 1;
                }
            }
            if (upperBound * mergeFactor <= maxMergeDocs)
            {
                Assert.IsTrue(numSegments < mergeFactor);
            }
        }

        private const double EPSILON = 1E-14;

        [Test]
        public virtual void TestSetters()
        {
            AssertSetters(new LogByteSizeMergePolicy());
            AssertSetters(new LogDocMergePolicy());
        }

        private void AssertSetters(MergePolicy lmp)
        {
            lmp.MaxCFSSegmentSizeMB = 2.0;
            Assert.AreEqual(2.0, lmp.MaxCFSSegmentSizeMB, EPSILON);

            lmp.MaxCFSSegmentSizeMB = double.PositiveInfinity;
            Assert.AreEqual(long.MaxValue / 1024 / 1024.0, lmp.MaxCFSSegmentSizeMB, EPSILON * long.MaxValue);

            lmp.MaxCFSSegmentSizeMB = long.MaxValue / 1024 / 1024.0;
            Assert.AreEqual(long.MaxValue / 1024 / 1024.0, lmp.MaxCFSSegmentSizeMB, EPSILON * long.MaxValue);

            try
            {
                lmp.MaxCFSSegmentSizeMB = -2.0;
                Assert.Fail("Didn't throw IllegalArgumentException");
            }
            catch (System.ArgumentException iae)
            {
                // pass
            }

            // TODO: Add more checks for other non-double setters!
        }
    }
}