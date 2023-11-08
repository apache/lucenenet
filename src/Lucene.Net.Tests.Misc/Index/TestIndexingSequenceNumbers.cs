using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Analysis;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Tests.Misc.Index
{
    //move me to Lucene.Net.Index
    public class TestIndexingSequenceNumbers : LuceneTestCase
    {
        [Test]
        public void TestBasic()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            long a = w.AddDocument(new Document());
            long b = w.AddDocument(new Document());
            assertTrue(b > a);
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestAfterRefresh()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            long a = w.AddDocument(new Document());
            DirectoryReader.Open(w, true).Dispose();
            long b = w.AddDocument(new Document());
            assertTrue(b > a);
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestAfterCommit()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            long a = w.AddDocument(new Document());
            w.Commit();
            long b = w.AddDocument(new Document());
            assertTrue(b > a);
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestStressUpdateSameID()
        {
            int iters = AtLeast(100);
            for (int iter = 0; iter < iters; iter++)
            {
                Directory dir = NewDirectory();
                // nocommit use RandomIndexWriter
                IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                ThreadJob[] threads = new ThreadJob[TestUtil.NextInt32(Random, 2, 5)];
                CountdownEvent startingGun = new CountdownEvent(1);
                long[] seqNos = new long[threads.Length];
                Term id = new Term("id", "id");
                // multiple threads update the same document
                for (int i = 0; i < threads.Length; i++)
                {
                    int threadID = i;
                    threads[i] = new ThreadJob(() =>
                    {
                        try
                        {
                            Document doc = new Document();
                            doc.Add(new StoredField("thread", threadID));
                            doc.Add(new StringField("id", "id", Field.Store.NO));
                            startingGun.Wait();
                            for (int j = 0; j < 100; j++)
                            {
                                if (Random.nextBoolean())
                                {
                                    seqNos[threadID] = w.UpdateDocument(id, doc);
                                }
                                else
                                {
                                    List<Document> docs = new();
                                    docs.Add(doc);
                                    seqNos[threadID] = w.UpdateDocuments(id, docs);
                                }
                            }
                        }
                        catch (Exception e) when (e.IsException())
                        {
                            throw RuntimeException.Create(e);
                        }
                    });
                    threads[i].Start();
                }
                startingGun.Signal();
                foreach (ThreadJob thread in threads)
                {
                    thread.Join();
                }

                // now confirm that the reported sequence numbers agree with the index:
                int maxThread = 0;
                HashSet<long> allSeqNos = new HashSet<long>();
                for (int i = 0; i < threads.Length; i++)
                {
                    allSeqNos.add(seqNos[i]);
                    if (seqNos[i] > seqNos[maxThread])
                    {
                        maxThread = i;
                    }
                }
                // make sure all sequence numbers were different
                assertEquals(threads.Length, allSeqNos.size());
                DirectoryReader r = w.GetReader();
                IndexSearcher s = NewSearcher(r);
                TopDocs hits = s.Search(new TermQuery(id), 1);
                assertEquals(1, hits.TotalHits);
                Document doc = r.Document(hits.ScoreDocs[0].Doc);
                assertEquals(maxThread, doc.GetField("thread").GetInt32Value());
                r.Dispose();
                w.Dispose();
                dir.Dispose();
            }
        }

        private sealed class Operation
        {
            // 0 = update, 1 = delete, 2 = commit, 3 = add
            internal byte what;
            internal int id;
            internal int threadID;
            internal long seqNo;
        }

        [Test]
        public void TestStressConcurrentCommit()
        {
            int opCount = AtLeast(10000);
            int idCount = TestUtil.NextInt32(Random, 10, 1000);

            Directory dir = NewDirectory();
            // nocommit use RandomIndexWriter
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.IndexDeletionPolicy = NoDeletionPolicy.INSTANCE;
            IndexWriter w = new IndexWriter(dir, iwc);
            int numThreads = TestUtil.NextInt32(Random, 2, 5);
            ThreadJob[] threads = new ThreadJob[numThreads];
            // Console.WriteLine("TEST: iter=" + iter + " opCount=" + opCount + " idCount=" + idCount + " threadCount=" + threads.length);
            CountdownEvent startingGun = new CountdownEvent(1);
            List<List<Operation>> threadOps = new();

            Object commitLock = new Object();
            List<Operation> commits = new();
            AtomicInt32 opsSinceCommit = new();

            // multiple threads update the same set of documents, and we randonly commit
            for (int i = 0; i < threads.Length; i++)
            {
                List<Operation> ops = new();
                threadOps.Add(ops);
                int threadID = i;
                threads[i] = new ThreadJob(() =>
                {
                    try
                    {
                        startingGun.Wait();
                        for (int j = 0; j < opCount; j++)
                        {
                            Operation op = new();
                            op.threadID = threadID;
                            if (Random.Next(500) == 17)
                            {
                                op.what = 2;
                                UninterruptableMonitor.Enter(commitLock);
                                try
                                {
                                    op.seqNo = w.Commit();
                                    if (op.seqNo != -1)
                                    {
                                        commits.Add(op);
                                    }
                                }
                                finally
                                {
                                    UninterruptableMonitor.Exit(commitLock);
                                }
                            }
                            else
                            {
                                op.id = Random.Next(idCount);
                                Term idTerm = new Term("id", op.id.ToString());
                                if (Random.Next(10) == 1)
                                {
                                    op.what = 1;
                                    if (Random.nextBoolean())
                                    {
                                        op.seqNo = w.DeleteDocuments(idTerm);
                                    }
                                    else
                                    {
                                        op.seqNo = w.DeleteDocuments(new TermQuery(idTerm));
                                    }
                                }
                                else
                                {
                                    Document doc = new Document();
                                    doc.Add(new StoredField("thread", threadID));
                                    doc.Add(new StringField("id", op.id.ToString(), Field.Store.NO));
                                    if (Random.Next(2) == 0)
                                    {
                                        List<Document> docs = new List<Document> { doc };
                                        op.seqNo = w.UpdateDocuments(idTerm, docs);
                                    }
                                    else
                                    {
                                        op.seqNo = w.UpdateDocument(idTerm, doc);
                                    }
                                    op.what = 0;
                                }
                                ops.Add(op);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw RuntimeException.Create(e.Message, e);
                    }
                });
                threads[i].Start();
            }
            startingGun.Signal();
            foreach (ThreadJob thread in threads)
            {
                thread.Join();
            }

            Operation commitOp = new();
            UninterruptableMonitor.Enter(commitLock);
            try
            {
                commitOp.seqNo = w.Commit();
                commits.Add(commitOp);
            }
            finally
            {
                UninterruptableMonitor.Exit(commitLock);
            }

            IList<IndexCommit> indexCommits = DirectoryReader.ListCommits(dir);
            assertEquals(commits.size(), indexCommits.size());

            int[] expectedThreadIDs = new int[idCount];
            long[] seqNos = new long[idCount];

            // Console.WriteLine("TEST: " + commits.size() + " commits");
            for (int i = 0; i < commits.size(); i++)
            {
                // this commit point should reflect all operations <= this seqNo
                long commitSeqNo = commits[i].seqNo;
                // Console.WriteLine("  commit " + i + ": seqNo=" + commitSeqNo + " segs=" + indexCommits[i));

                Arrays.Fill(expectedThreadIDs, -1);
                Arrays.Fill(seqNos, 0);

                for (int threadID = 0; threadID < threadOps.size(); threadID++)
                {
                    long lastSeqNo = 0;
                    foreach (Operation op in threadOps[threadID])
                    {
                        if (op.seqNo <= commitSeqNo && op.seqNo > seqNos[op.id])
                        {
                            seqNos[op.id] = op.seqNo;
                            if (op.what == 0)
                            {
                                expectedThreadIDs[op.id] = threadID;
                            }
                            else
                            {
                                expectedThreadIDs[op.id] = -1;
                            }
                        }
                        assertTrue(op.seqNo > lastSeqNo);
                        lastSeqNo = op.seqNo;
                    }
                }

                DirectoryReader r = DirectoryReader.Open(indexCommits[i]);
                IndexSearcher s = new IndexSearcher(r);

                for (int id = 0; id < idCount; id++)
                {
                    // Console.WriteLine("TEST: check id=" + id + " expectedThreadID=" + expectedThreadIDs[id]);
                    TopDocs hits = s.Search(new TermQuery(new Term("id", "" + id)), 1);

                    if (expectedThreadIDs[id] != -1)
                    {
                        assertEquals(1, hits.TotalHits);
                        Document doc = r.Document(hits.ScoreDocs[0].Doc);
                        int? actualThreadID = doc.GetField("thread").GetInt32Value();
                        if (expectedThreadIDs[id] != actualThreadID)
                        {
                            Console.WriteLine("FAIL: id=" + id + " expectedThreadID=" + expectedThreadIDs[id] + " vs actualThreadID=" + actualThreadID);
                            for (int threadID = 0; threadID < threadOps.size(); threadID++)
                            {
                                foreach (Operation op in threadOps[threadID])
                                {
                                    if (id == op.id)
                                    {
                                        Console.WriteLine("  threadID=" + threadID + " seqNo=" + op.seqNo + " " + (op.what == 2 ? "updated" : "deleted"));
                                    }
                                }
                            }
                            assertEquals("id=" + id, expectedThreadIDs[id], actualThreadID);
                        }
                    }
                    else if (hits.TotalHits != 0)
                    {
                        Console.WriteLine("FAIL: id=" + id + " expectedThreadID=" + expectedThreadIDs[id] + " vs totalHits=" + hits.TotalHits);
                        for (int threadID = 0; threadID < threadOps.size(); threadID++)
                        {
                            foreach (Operation op in threadOps[threadID])
                            {
                                if (id == op.id)
                                {
                                    Console.WriteLine("  threadID=" + threadID + " seqNo=" + op.seqNo + " " + (op.what == 2 ? "updated" : "del"));
                                }
                            }
                        }
                        assertEquals(0, hits.TotalHits);
                    }
                }
                w.Dispose();
                r.Dispose();
            }
            dir.Dispose();
        }

        [Test]
        public void TestStressConcurrentDocValuesUpdatesCommit()
        {
            int opCount = AtLeast(10000);
            int idCount = TestUtil.NextInt32(Random, 10, 1000);

            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.IndexDeletionPolicy = NoDeletionPolicy.INSTANCE;

            // Cannot use RIW since it randomly commits:
            IndexWriter w = new IndexWriter(dir, iwc);

            int numThreads = TestUtil.NextInt32(Random, 2, 10);
            if (Verbose)
            {
                Console.WriteLine("TEST: numThreads=" + numThreads);
            }
            ThreadJob[] threads = new ThreadJob[numThreads];
            //Console.WriteLine("TEST: iter=" + iter + " opCount=" + opCount + " idCount=" + idCount + " threadCount=" + threads.length);
            CountdownEvent startingGun = new CountdownEvent(1);
            List<List<Operation>> threadOps = new();

            Object commitLock = new Object();
            List<Operation> commits = new();

            List<Operation> ops1 = new();
            threadOps.Add(ops1);

            // pre-index every ID so none are missing:
            for (int id = 0; id < idCount; id++)
            {
                int threadID = 0;
                Operation op = new Operation();
                op.threadID = threadID;
                op.id = id;

                Document doc = new Document();
                doc.Add(new StoredField("thread", threadID));
                doc.Add(new NumericDocValuesField("thread", threadID));
                doc.Add(new StringField("id", "" + id, Field.Store.NO));
                op.seqNo = w.AddDocument(doc);
                ops1.Add(op);
            }

            // multiple threads update the same set of documents, and we randomly commit, recording the commit seqNo and then opening each commit in
            // the end to verify it reflects the correct updates
            for (int i = 0; i < threads.Length; i++)
            {
                List<Operation> ops;
                if (i == 0)
                {
                    ops = threadOps[0];
                }
                else
                {
                    ops = new();
                    threadOps.Add(ops);
                }

                int threadID = i;
                threads[i] = new ThreadJob(() =>
                {
                    try
                    {
                        startingGun.Wait();
                        for (int i = 0; i < opCount; i++)
                        {
                            Operation op = new Operation();
                            op.threadID = threadID;
                            if (Random.Next(500) == 17)
                            {
                                op.what = 2;
                                UninterruptableMonitor.Enter(commitLock);
                                try
                                {
                                    op.seqNo = w.Commit();
                                    if (op.seqNo != -1)
                                    {
                                        commits.Add(op);
                                    }
                                }
                                finally
                                {
                                    UninterruptableMonitor.Exit(commitLock);
                                }
                            }
                            else
                            {
                                op.id = Random.Next(idCount);
                                Term idTerm = new Term("id", "" + op.id);
                                op.seqNo = w.UpdateNumericDocValue(idTerm, "thread", threadID);
                                op.what = 0;
                                ops.Add(op);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw RuntimeException.Create(e);
                    }
                });

                threads[i].Name = ("thread" + i);
                threads[i].Start();
            }
            startingGun.Signal();
            foreach (ThreadJob thread in threads)
            {
                thread.Join();
            }

            Operation commitOp = new Operation();
            commitOp.seqNo = w.Commit();
            if (commitOp.seqNo != -1)
            {
                commits.Add(commitOp);
            }

            IList<IndexCommit> indexCommits = DirectoryReader.ListCommits(dir);
            assertEquals(commits.size(), indexCommits.size());

            int[] expectedThreadIDs = new int[idCount];
            long[] seqNos = new long[idCount];

            //Console.WriteLine("TEST: " + commits.size() + " commits");
            for (int i = 0; i < commits.size(); i++)
            {
                // this commit point should reflect all operations <= this seqNo
                long commitSeqNo = commits[i].seqNo;
                //Console.WriteLine("  commit " + i + ": seqNo=" + commitSeqNo + " segs=" + indexCommits[i));

                Arrays.Fill(expectedThreadIDs, -1);
                Arrays.Fill(seqNos, 0);

                for (int threadID = 0; threadID < threadOps.size(); threadID++)
                {
                    long lastSeqNo = 0;
                    foreach (Operation op in threadOps[threadID])
                    {
                        if (op.seqNo <= commitSeqNo && op.seqNo > seqNos[op.id])
                        {
                            seqNos[op.id] = op.seqNo;
                            Debugging.Assert(op.what == 0);
                            expectedThreadIDs[op.id] = threadID;
                        }

                        assertTrue(op.seqNo > lastSeqNo);
                        lastSeqNo = op.seqNo;
                    }
                }

                DirectoryReader r = DirectoryReader.Open(indexCommits[i]);
                IndexSearcher s = new IndexSearcher(r);
                NumericDocValues docValues = MultiDocValues.GetNumericValues(r, "thread");

                for (int id = 0; id < idCount; id++)
                {
                    //Console.WriteLine("TEST: check id=" + id + " expectedThreadID=" + expectedThreadIDs[id]);
                    TopDocs hits = s.Search(new TermQuery(new Term("id", "" + id)), 1);

                    // We pre-Add all ids up front:
                    Debugging.Assert(expectedThreadIDs[id] != -1);
                    assertEquals(1, hits.TotalHits);
                    int actualThreadID = (int)docValues.Get(hits.ScoreDocs[0].Doc);
                    if (expectedThreadIDs[id] != actualThreadID)
                    {
                        Console.WriteLine("FAIL: commit=" + i + " (of " + commits.size() + ") id=" + id + " expectedThreadID=" + expectedThreadIDs[id] + " vs actualThreadID=" + actualThreadID + " commitSeqNo=" + commitSeqNo + " numThreads=" + numThreads + " reader=" + r + " commit=" + indexCommits[i]);
                        for (int threadID = 0; threadID < threadOps.size(); threadID++)
                        {
                            foreach (Operation op in threadOps[threadID])
                            {
                                if (id == op.id)
                                {
                                    Console.WriteLine("  threadID=" + threadID + " seqNo=" + op.seqNo);
                                }
                            }
                        }
                        assertEquals("id=" + id + " docID=" + hits.ScoreDocs[0].Doc, expectedThreadIDs[id], actualThreadID);
                    }
                }
                w.Dispose();
                r.Dispose();
            }

            dir.Dispose();
        }

        [Test]
        public void TestStressConcurrentAddAndDeleteAndCommit()
        {
            int opCount = AtLeast(10000);
            int idCount = TestUtil.NextInt32(Random, 10, 1000);

            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.IndexDeletionPolicy = (NoDeletionPolicy.INSTANCE);

            // Cannot use RIW since it randomly commits:
            IndexWriter w = new IndexWriter(dir, iwc);

            int numThreads = TestUtil.NextInt32(Random, 2, 5);
            ThreadJob[] threads = new ThreadJob[numThreads];
            //Console.WriteLine("TEST: iter=" + iter + " opCount=" + opCount + " idCount=" + idCount + " threadCount=" + threads.Length);
            CountdownEvent startingGun = new CountdownEvent(1);
            List<List<Operation>> threadOps = new();

            Object commitLock = new Object();
            List<Operation> commits = new();

            // multiple threads update the same set of documents, and we randomly commit
            for (int i = 0; i < threads.Length; i++)
            {
                List<Operation> ops = new();
                threadOps.Add(ops);
                int threadID = i;
                threads[i] = new ThreadJob(() =>
                {
                    try
                    {
                        startingGun.Wait();
                        for (int i = 0; i < opCount; i++)
                        {
                            Operation op = new Operation();
                            op.threadID = threadID;
                            if (Random.Next(500) == 17)
                            {
                                op.what = 2;
                                lock (commitLock)
                                {
                                    op.seqNo = w.Commit();
                                    if (op.seqNo != -1)
                                    {
                                        commits.Add(op);
                                    }
                                }
                            }
                            else
                            {
                                op.id = Random.Next(idCount);
                                Term idTerm = new Term("id", "" + op.id);
                                if (Random.Next(10) == 1)
                                {
                                    op.what = 1;
                                    if (Random.nextBoolean())
                                    {
                                        op.seqNo = w.DeleteDocuments(idTerm);
                                    }
                                    else
                                    {
                                        op.seqNo = w.DeleteDocuments(new TermQuery(idTerm));
                                    }
                                    Assert.IsTrue(w.HasDeletions()); //testing concurrenct deletions 
                                }
                                else
                                {
                                    Document doc = new Document();
                                    doc.Add(new StoredField("threadop", threadID + "-" + ops.size()));
                                    doc.Add(new StringField("id", "" + op.id, Field.Store.NO));
                                    if (Random.nextBoolean())
                                    {
                                        List<Document> docs = new();
                                        docs.Add(doc);
                                        op.seqNo = w.AddDocuments(docs);
                                    }
                                    else
                                    {
                                        op.seqNo = w.AddDocument(doc);
                                    }
                                    op.what = 3;
                                }
                                ops.Add(op);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw RuntimeException.Create(e);
                    }
                });
                threads[i].Name = ("thread" + threadID);
                threads[i].Start();
            }
            startingGun.Signal();
            foreach (ThreadJob thread in threads)
            {
                thread.Join();
            }

            Operation commitOp = new Operation();
            commitOp.seqNo = w.Commit();
            if (commitOp.seqNo != -1)
            {
                commits.Add(commitOp);
            }

            IList<IndexCommit> indexCommits = DirectoryReader.ListCommits(dir);
            assertEquals(commits.size(), indexCommits.size());

            // how many docs with this id are expected:
            int[] expectedCounts = new int[idCount];
            long[] lastDelSeqNos = new long[idCount];

            //Console.WriteLine("TEST: " + commits.size() + " commits");
            for (int i = 0; i < commits.size(); i++)
            {
                // this commit point should reflect all operations <= this seqNo
                long commitSeqNo = commits[i].seqNo;
                //Console.WriteLine("  commit " + i + ": seqNo=" + commitSeqNo + " segs=" + indexCommits[i));

                // first find the highest seqNo of the last delete op, for each id, prior to this commit:
                Arrays.Fill(lastDelSeqNos, -1);
                for (int threadID = 0; threadID < threadOps.size(); threadID++)
                {
                    long lastSeqNo = 0;
                    foreach (Operation op in threadOps[threadID])
                    {
                        if (op.what == 1 && op.seqNo <= commitSeqNo && op.seqNo > lastDelSeqNos[op.id])
                        {
                            lastDelSeqNos[op.id] = op.seqNo;
                        }

                        // within one thread the seqNos must only increase:
                        assertTrue(op.seqNo > lastSeqNo);
                        lastSeqNo = op.seqNo;
                    }
                }

                // then count how many adds happened since the last delete and before this commit:
                Arrays.Fill(expectedCounts, 0);
                for (int threadID = 0; threadID < threadOps.size(); threadID++)
                {
                    foreach (Operation op in threadOps[threadID])
                    {
                        if (op.what == 3 && op.seqNo <= commitSeqNo && op.seqNo > lastDelSeqNos[op.id])
                        {
                            expectedCounts[op.id]++;
                        }
                    }
                }

                DirectoryReader r = DirectoryReader.Open(indexCommits[i]);
                IndexSearcher s = new IndexSearcher(r);

                for (int id = 0; id < idCount; id++)
                {
                    //we don't have count menthod in IndexSearcher which counts and returns value for the given query
                    int actualCount = s.Search(new TermQuery(new Term("id", "" + id)),idCount).TotalHits;
                    if (expectedCounts[id] != actualCount)
                    {
                        Console.WriteLine("TEST: FAIL r=" + r + " id=" + id + " commitSeqNo=" + commitSeqNo);
                        for (int threadID = 0; threadID < threadOps.size(); threadID++)
                        {
                            int opCount2 = 0;
                            foreach (Operation op in threadOps[threadID])
                            {
                                if (op.id == id)
                                {
                                    bool shouldCount = op.seqNo <= commitSeqNo && op.seqNo > lastDelSeqNos[op.id];
                                    Console.WriteLine("  id=" + id + " what=" + op.what + " threadop=" + threadID + "-" + opCount2 + " seqNo=" + op.seqNo + " vs lastDelSeqNo=" + lastDelSeqNos[op.id] + " shouldCount=" + shouldCount);
                                }
                                opCount2++;
                            }
                        }
                        TopDocs hits = s.Search(new TermQuery(new Term("id", "" + id)), 1 + actualCount);
                        foreach (ScoreDoc hit in hits.ScoreDocs)
                        {
                            Console.WriteLine("  hit: " + s.Doc(hit.Doc).Get("threadop"));
                        }

                        foreach (AtomicReaderContext ctx in r.Leaves)
                        {
                            Console.WriteLine("  sub=" + ctx.Reader);
                            IBits liveDocs = ctx.AtomicReader.LiveDocs;
                            for (int docID = 0; docID < ctx.Reader.MaxDoc; docID++)
                            {
                                Console.WriteLine("docID=" + docID + " threadop=" + ctx.Reader.Document(docID).Get("threadop") + (liveDocs != null && liveDocs.Get(docID) == false ? " (deleted)" : ""));
                            }
                        }

                        assertEquals("commit " + i + " of " + commits.size() + " id=" + id + " reader=" + r, expectedCounts[id], actualCount);
                    }
                }
                w.Dispose();
                r.Dispose();
            }

            dir.Dispose();
        }

        [Test]
        public void TestDeleteAll()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            long a = w.AddDocument(new Document());
            long b = w.DeleteAll();
            assertTrue(a < b);
            long c = w.Commit();
            assertTrue(b < c);
            w.Dispose();
            dir.Dispose();
        }
    }
}
