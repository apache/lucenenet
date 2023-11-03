using J2N.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
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
            assertTrue(b >= a);
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestAfterRefresh()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            long a = w.AddDocument(new Document());
            DirectoryReader.Open(w,true).Dispose();
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

      /*  [Test]
        public void TestStressUpdateSameID()
        {
            int iters = AtLeast(100);
            for (int iter = 0; iter < iters; iter++)
            {
                Directory dir = NewDirectory();
                // nocommit use RandomIndexWriter
                IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))));
            ThreadJob[] threads = new ThreadJob[TestUtil.NextInt32(Random, 2, 5)];
            CountdownEvent startingGun = new CountdownEvent(1);
            long[] seqNos = new long[threads.Length];
            Term id = new Term("id", "id");
            // multiple threads update the same document
            for (int i = 0; i < threads.Length; i++)
            {
                int threadID = i;
                //threads[i] = new Thread()
                //{
                    *//*public void run()
                            {
                                try
                                {
                                    Document doc = new Document();
                                    doc.add(new StoredField("thread", threadID));
                                    doc.add(new StringField("id", "id", Field.Store.NO));
                                    startingGun.await();
                                    for (int j = 0; j < 100; j++)
                                    {
                                        seqNos[threadID] = w.updateDocument(id, doc);
                                    }
                                }
                                catch (Exception e)
                                {
                                    throw new RuntimeException(e);
                                }
                            }
                        };
                        threads[i].start();*//*
                //};
                startingGun.CountDown();
                foreach (ThreadJob thread in threads)
                {
                    thread.Join();
                }

                // now confirm that the reported sequence numbers agree with the index:
                int maxThread = 0;
                var allSeqNos = new HashSet<long>();
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
                DirectoryReader r = DirectoryReader.Open(w);
                IndexSearcher s = NewSearcher(r);
                TopDocs hits = s.Search(new TermQuery(id), 1);
                assertEquals(1, hits.TotalHits);
                Document doc = r.Document(hits.ScoreDocs[0].Doc);
                assertEquals(maxThread, doc.GetField("thread").NumericValue.intValue());
                r.Dispose();
                w.Dispose();
                dir.Dispose();
            }
        }

    static class Operation
    {
            // 0 = update, 1 = delete, 2 = commit
        static byte what;
        static int id;
        static int threadID;
        static long seqNo;
    }

    public void testStressConcurrentCommit()
    {
         int opCount = AtLeast(10000);
         int idCount = TestUtil.NextInt32(Random, 10, 1000);

        Directory dir = NewDirectory();
        // nocommit use RandomIndexWriter
        IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
        iwc.IndexDeletionPolicy = (NoDeletionPolicy.INSTANCE);
         IndexWriter w = new IndexWriter(dir, iwc);
         int numThreads = TestUtil.NextInt32(Random, 2, 5);
        Thread[] threads = new Thread[numThreads];
        //System.out.println("TEST: iter=" + iter + " opCount=" + opCount + " idCount=" + idCount + " threadCount=" + threads.length);
         CountDownLatch startingGun = new CountDownLatch(1);
        List<List<Operation>> threadOps = new ArrayList<>();

        Object commitLock = new Object();
         List<Operation> commits = new ArrayList<>();
         AtomicInteger opsSinceCommit = new AtomicInteger();

        // multiple threads update the same set of documents, and we randomly commit
        for (int i = 0; i < threads.length; i++)
        {
             List<Operation> ops = new ArrayList<>();
            threadOps.add(ops);
             int threadID = i;
            threads[i] = new Thread() {
          public void run()
            {
                try
                {
                    startingGun.await();
                    for (int i = 0; i < opCount; i++)
                    {
                        Operation op = new Operation();
                        op.threadID = threadID;
                        if (random().nextInt(500) == 17)
                        {
                            op.what = 2;
                            synchronized(commitLock) {
                                // nocommit why does this sometimes fail :)
                                //if (w.hasUncommittedChanges()) {
                                if (opsSinceCommit.get() > numThreads)
                                {
                                    op.seqNo = w.commit();
                                    commits.add(op);
                                    opsSinceCommit.set(0);
                                }
                                //System.out.println("done commit seqNo=" + op.seqNo);
                            }
                        }
                        else
                        {
                            op.id = random().nextInt(idCount);
                            Term idTerm = new Term("id", "" + op.id);
                            if (random().nextInt(10) == 1)
                            {
                                op.what = 1;
                                op.seqNo = w.deleteDocuments(idTerm);
                            }
                            else
                            {
                                Document doc = new Document();
                                doc.add(new StoredField("thread", threadID));
                                doc.add(new StringField("id", "" + op.id, Field.Store.NO));
                                op.seqNo = w.UpdateDocument(idTerm, doc);
                                op.what = 2;
                            }
                            ops.Add(op);
                            opsSinceCommit.getAndIncrement();
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new RuntimeException(e);
                }
            }
        };
        threads[i].start();
    }
    startingGun.countDown();
for (Thread thread : threads)
{
    thread.join();
}

Operation commitOp = new Operation();
synchronized(commitLock) {
    commitOp.seqNo = w.commit();
    commits.add(commitOp);
}

List<IndexCommit> indexCommits = DirectoryReader.listCommits(dir);
assertEquals(commits.size(), indexCommits.size());

int[] expectedThreadIDs = new int[idCount];
long[] seqNos = new long[idCount];

//System.out.println("TEST: " + commits.size() + " commits");
for (int i = 0; i < commits.size(); i++)
{
    // this commit point should reflect all operations <= this seqNo
    long commitSeqNo = commits.get(i).seqNo;
    //System.out.println("  commit " + i + ": seqNo=" + commitSeqNo + " segs=" + indexCommits.get(i));

    Arrays.fill(expectedThreadIDs, -1);
    Arrays.fill(seqNos, 0);

    for (int threadID = 0; threadID < threadOps.size(); threadID++)
    {
        long lastSeqNo = 0;
        for (Operation op : threadOps.get(threadID))
        {
            if (op.seqNo <= commitSeqNo && op.seqNo > seqNos[op.id])
            {
                seqNos[op.id] = op.seqNo;
                if (op.what == 2)
                {
                    expectedThreadIDs[op.id] = threadID;
                }
                else
                {
                    expectedThreadIDs[op.id] = -1;
                }
            }

            assertTrue(op.seqNo >= lastSeqNo);
            lastSeqNo = op.seqNo;
        }
    }

    DirectoryReader r = DirectoryReader.open(indexCommits.get(i));
    IndexSearcher s = new IndexSearcher(r);

    for (int id = 0; id < idCount; id++)
    {
        //System.out.println("TEST: check id=" + id + " expectedThreadID=" + expectedThreadIDs[id]);
        TopDocs hits = s.search(new TermQuery(new Term("id", "" + id)), 1);

        if (expectedThreadIDs[id] != -1)
        {
            assertEquals(1, hits.totalHits);
            Document doc = r.document(hits.scoreDocs[0].doc);
            int actualThreadID = doc.getField("thread").numericValue().intValue();
            if (expectedThreadIDs[id] != actualThreadID)
            {
                System.out.println("FAIL: id=" + id + " expectedThreadID=" + expectedThreadIDs[id] + " vs actualThreadID=" + actualThreadID);
                for (int threadID = 0; threadID < threadOps.size(); threadID++)
                {
                    for (Operation op : threadOps.get(threadID))
                    {
                        if (id == op.id)
                        {
                            System.out.println("  threadID=" + threadID + " seqNo=" + op.seqNo + " " + (op.what == 2 ? "updated" : "deleted"));
                        }
                    }
                }
                assertEquals("id=" + id, expectedThreadIDs[id], actualThreadID);
            }
        }
        else if (hits.totalHits != 0)
        {
            System.out.println("FAIL: id=" + id + " expectedThreadID=" + expectedThreadIDs[id] + " vs totalHits=" + hits.totalHits);
            for (int threadID = 0; threadID < threadOps.size(); threadID++)
            {
                for (Operation op : threadOps.get(threadID))
                {
                    if (id == op.id)
                    {
                        System.out.println("  threadID=" + threadID + " seqNo=" + op.seqNo + " " + (op.what == 2 ? "updated" : "del"));
                    }
                }
            }
            assertEquals(0, hits.totalHits);
        }
    }
    w.close();
    r.close();
}

dir.close();
  }*/

  // nocommit test that does n ops across threads, then does it again with a single index / single thread, and assert indices are the same
    }
}
