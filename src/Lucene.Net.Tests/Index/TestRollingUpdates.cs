using J2N.Threading;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
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

    using Codec = Lucene.Net.Codecs.Codec;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TopDocs = Lucene.Net.Search.TopDocs;

    [TestFixture]
    public class TestRollingUpdates : LuceneTestCase
    {
        // Just updates the same set of N docs over and over, to
        // stress out deletions

        [Test]
        public virtual void TestRollingUpdates_Mem()
        {
            Random random = new J2N.Randomizer(Random.NextInt64());
            BaseDirectoryWrapper dir = NewDirectory();
            LineFileDocs docs = new LineFileDocs(random, DefaultCodecSupportsDocValues);

            //provider.register(new MemoryCodec());
            if ((!"Lucene3x".Equals(Codec.Default.Name, StringComparison.Ordinal)) && LuceneTestCase.Random.NextBoolean())
            {
                Codec.Default =
                    TestUtil.AlwaysPostingsFormat(new MemoryPostingsFormat(LuceneTestCase.Random.nextBoolean(), random.NextSingle()));
            }

            MockAnalyzer analyzer = new MockAnalyzer(LuceneTestCase.Random);
            analyzer.MaxTokenLength = TestUtil.NextInt32(LuceneTestCase.Random, 1, IndexWriter.MAX_TERM_LENGTH);

            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            int SIZE = AtLeast(20);
            int id = 0;
            IndexReader r = null;
            IndexSearcher s = null;
            int numUpdates = (int)(SIZE * (2 + (TestNightly ? 200 * LuceneTestCase.Random.NextDouble() : 5 * LuceneTestCase.Random.NextDouble())));
            if (Verbose)
            {
                Console.WriteLine("TEST: numUpdates=" + numUpdates);
            }
            int updateCount = 0;
            // TODO: sometimes update ids not in order...
            for (int docIter = 0; docIter < numUpdates; docIter++)
            {
                Documents.Document doc = docs.NextDoc();
                string myID = "" + id;
                if (id == SIZE - 1)
                {
                    id = 0;
                }
                else
                {
                    id++;
                }
                if (Verbose)
                {
                    Console.WriteLine("  docIter=" + docIter + " id=" + id);
                }
                ((Field)doc.GetField("docid")).SetStringValue(myID);

                Term idTerm = new Term("docid", myID);

                bool doUpdate;
                if (s != null && updateCount < SIZE)
                {
                    TopDocs hits = s.Search(new TermQuery(idTerm), 1);
                    Assert.AreEqual(1, hits.TotalHits);
                    doUpdate = !w.TryDeleteDocument(r, hits.ScoreDocs[0].Doc);
                    if (Verbose)
                    {
                        if (doUpdate)
                        {
                            Console.WriteLine("  tryDeleteDocument failed");
                        }
                        else
                        {
                            Console.WriteLine("  tryDeleteDocument succeeded");
                        }
                    }
                }
                else
                {
                    doUpdate = true;
                    if (Verbose)
                    {
                        Console.WriteLine("  no searcher: doUpdate=true");
                    }
                }

                updateCount++;

                if (doUpdate)
                {
                    w.UpdateDocument(idTerm, doc);
                }
                else
                {
                    w.AddDocument(doc);
                }

                if (docIter >= SIZE && LuceneTestCase.Random.Next(50) == 17)
                {
                    if (r != null)
                    {
                        r.Dispose();
                    }

                    bool applyDeletions = LuceneTestCase.Random.NextBoolean();

                    if (Verbose)
                    {
                        Console.WriteLine("TEST: reopen applyDeletions=" + applyDeletions);
                    }

                    r = w.GetReader(applyDeletions);
                    if (applyDeletions)
                    {
                        s = NewSearcher(r);
                    }
                    else
                    {
                        s = null;
                    }
                    Assert.IsTrue(!applyDeletions || r.NumDocs == SIZE, "applyDeletions=" + applyDeletions + " r.NumDocs=" + r.NumDocs + " vs SIZE=" + SIZE);
                    updateCount = 0;
                }
            }

            if (r != null)
            {
                r.Dispose();
            }

            w.Commit();
            Assert.AreEqual(SIZE, w.NumDocs);

            w.Dispose();

            TestIndexWriter.AssertNoUnreferencedFiles(dir, "leftover files after rolling updates");

            docs.Dispose();

            // LUCENE-4455:
            SegmentInfos infos = new SegmentInfos();
            infos.Read(dir);
            long totalBytes = 0;
            foreach (SegmentCommitInfo sipc in infos.Segments)
            {
                totalBytes += sipc.GetSizeInBytes();
            }
            long totalBytes2 = 0;
            foreach (string fileName in dir.ListAll())
            {
                if (!fileName.StartsWith(IndexFileNames.SEGMENTS, StringComparison.Ordinal))
                {
                    totalBytes2 += dir.FileLength(fileName);
                }
            }
            Assert.AreEqual(totalBytes2, totalBytes);
            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateSameDoc()
        {
            Directory dir = NewDirectory();

            LineFileDocs docs = new LineFileDocs(Random);
            for (int r = 0; r < 3; r++)
            {
                IndexWriter w = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2));
                int numUpdates = AtLeast(20);
                int numThreads = TestUtil.NextInt32(Random, 2, 6);
                IndexingThread[] threads = new IndexingThread[numThreads];
                for (int i = 0; i < numThreads; i++)
                {
                    threads[i] = new IndexingThread(docs, w, numUpdates, NewStringField);
                    threads[i].Start();
                }

                for (int i = 0; i < numThreads; i++)
                {
                    threads[i].Join();
                }

                w.Dispose();
            }

            IndexReader open = DirectoryReader.Open(dir);
            Assert.AreEqual(1, open.NumDocs);
            open.Dispose();
            docs.Dispose();
            dir.Dispose();
        }

        internal class IndexingThread : ThreadJob
        {
            internal readonly LineFileDocs docs;
            internal readonly IndexWriter writer;
            internal readonly int num;

            private readonly Func<string, string, Field.Store, Field> newStringField;

            /// <param name="newStringField">
            /// LUCENENET specific
            /// Passed in because <see cref="LuceneTestCase.NewStringField(string, string, Field.Store)"/>
            /// is no longer static.
            /// </param>
            public IndexingThread(LineFileDocs docs, IndexWriter writer, int num, Func<string, string, Field.Store, Field> newStringField)
                : base()
            {
                this.docs = docs;
                this.writer = writer;
                this.num = num;
                this.newStringField = newStringField;
            }

            public override void Run()
            {
                try
                {
                    DirectoryReader open = null;
                    for (int i = 0; i < num; i++)
                    {
                        Documents.Document doc = new Documents.Document(); // docs.NextDoc();
                        doc.Add(newStringField("id", "test", Field.Store.NO));
                        writer.UpdateDocument(new Term("id", "test"), doc);
                        if (Random.Next(3) == 0)
                        {
                            if (open is null)
                            {
                                open = DirectoryReader.Open(writer, true);
                            }
                            DirectoryReader reader = DirectoryReader.OpenIfChanged(open);
                            if (reader != null)
                            {
                                open.Dispose();
                                open = reader;
                            }
                            Assert.AreEqual(1, open.NumDocs, "iter: " + i + " numDocs: " + open.NumDocs + " del: " + open.NumDeletedDocs + " max: " + open.MaxDoc);
                        }
                    }
                    if (open != null)
                    {
                        open.Dispose();
                    }
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }
    }
}