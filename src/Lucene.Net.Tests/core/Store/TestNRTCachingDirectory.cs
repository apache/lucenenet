using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Store
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
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TopDocs = Lucene.Net.Search.TopDocs;

    [TestFixture]
    public class TestNRTCachingDirectory : LuceneTestCase
    {
        [Test]
        public virtual void TestNRTAndCommit()
        {
            Directory dir = NewDirectory();
            NRTCachingDirectory cachedDir = new NRTCachingDirectory(dir, 2.0, 25.0);
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            RandomIndexWriter w = new RandomIndexWriter(Random(), cachedDir, conf);
            LineFileDocs docs = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
            int numDocs = TestUtil.NextInt(Random(), 100, 400);

            if (VERBOSE)
            {
                Console.WriteLine("TEST: numDocs=" + numDocs);
            }

            IList<BytesRef> ids = new List<BytesRef>();
            DirectoryReader r = null;
            for (int docCount = 0; docCount < numDocs; docCount++)
            {
                Document doc = docs.NextDoc();
                ids.Add(new BytesRef(doc.Get("docid")));
                w.AddDocument(doc);
                if (Random().Next(20) == 17)
                {
                    if (r == null)
                    {
                        r = DirectoryReader.Open(w.w, false);
                    }
                    else
                    {
                        DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
                        if (r2 != null)
                        {
                            r.Dispose();
                            r = r2;
                        }
                    }
                    Assert.AreEqual(1 + docCount, r.NumDocs);
                    IndexSearcher s = NewSearcher(r);
                    // Just make sure search can run; we can't assert
                    // totHits since it could be 0
                    TopDocs hits = s.Search(new TermQuery(new Term("body", "the")), 10);
                    // System.out.println("tot hits " + hits.totalHits);
                }
            }

            if (r != null)
            {
                r.Dispose();
            }

            // Close should force cache to clear since all files are sync'd
            w.Dispose();

            string[] cachedFiles = cachedDir.ListCachedFiles();
            foreach (string file in cachedFiles)
            {
                Console.WriteLine("FAIL: cached file " + file + " remains after sync");
            }
            Assert.AreEqual(0, cachedFiles.Length);

            r = DirectoryReader.Open(dir);
            foreach (BytesRef id in ids)
            {
                Assert.AreEqual(1, r.DocFreq(new Term("docid", id)));
            }
            r.Dispose();
            cachedDir.Dispose();
            docs.Dispose();
        }

        // NOTE: not a test; just here to make sure the code frag
        // in the javadocs is correct!
        public virtual void VerifyCompiles()
        {
            Analyzer analyzer = null;

            Directory fsDir = FSDirectory.Open(new DirectoryInfo("/path/to/index"));
            NRTCachingDirectory cachedFSDir = new NRTCachingDirectory(fsDir, 2.0, 25.0);
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            IndexWriter writer = new IndexWriter(cachedFSDir, conf);
        }

        [Test]
        public virtual void TestDeleteFile()
        {
            Directory dir = new NRTCachingDirectory(NewDirectory(), 2.0, 25.0);
            dir.CreateOutput("foo.txt", IOContext.DEFAULT).Dispose();
            dir.DeleteFile("foo.txt");
            Assert.AreEqual(0, dir.ListAll().Length);
            dir.Dispose();
        }

        // LUCENE-3382 -- make sure we get exception if the directory really does not exist.
        [Test]
        public virtual void TestNoDir()
        {
            var tempDir = CreateTempDir("doesnotexist").FullName;
            System.IO.Directory.Delete(tempDir, true);
            using (Directory dir = new NRTCachingDirectory(NewFSDirectory(new DirectoryInfo(tempDir)), 2.0, 25.0))
            {
                try
                {
                    Assert.False(System.IO.Directory.Exists(tempDir));
                    DirectoryReader.Open(dir);
                    Assert.Fail("did not hit expected exception");
                }
                catch (NoSuchDirectoryException)
                {
                    // expected
                }
            }
        }

        // LUCENE-3382 test that we can add a file, and then when we call list() we get it back
        [Test]
        public virtual void TestDirectoryFilter()
        {
            Directory dir = new NRTCachingDirectory(NewFSDirectory(CreateTempDir("foo")), 2.0, 25.0);
            string name = "file";
            try
            {
                dir.CreateOutput(name, NewIOContext(Random())).Dispose();
                Assert.IsTrue(SlowFileExists(dir, name));
                Assert.IsTrue(Arrays.AsList(dir.ListAll()).Contains(name));
            }
            finally
            {
                dir.Dispose();
            }
        }

        // LUCENE-3382 test that delegate compound files correctly.
        [Test]
        public virtual void TestCompoundFileAppendTwice()
        {
            Directory newDir = new NRTCachingDirectory(NewDirectory(), 2.0, 25.0);
            CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random()), true);
            CreateSequenceFile(newDir, "d1", (sbyte)0, 15);
            IndexOutput @out = csw.CreateOutput("d.xyz", NewIOContext(Random()));
            @out.WriteInt(0);
            @out.Dispose();
            Assert.AreEqual(1, csw.ListAll().Length);
            Assert.AreEqual("d.xyz", csw.ListAll()[0]);

            csw.Dispose();

            CompoundFileDirectory cfr = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random()), false);
            Assert.AreEqual(1, cfr.ListAll().Length);
            Assert.AreEqual("d.xyz", cfr.ListAll()[0]);
            cfr.Dispose();
            newDir.Dispose();
        }

        /// <summary>
        /// Creates a file of the specified size with sequential data. The first
        ///  byte is written as the start byte provided. All subsequent bytes are
        ///  computed as start + offset where offset is the number of the byte.
        /// </summary>
        private void CreateSequenceFile(Directory dir, string name, sbyte start, int size)
        {
            IndexOutput os = dir.CreateOutput(name, NewIOContext(Random()));
            for (int i = 0; i < size; i++)
            {
                os.WriteByte((byte)start);
                start++;
            }
            os.Dispose();
        }
    }
}