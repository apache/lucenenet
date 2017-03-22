using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using Search;
    using System.IO;
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using FailOnNonBulkMergesInfoStream = Lucene.Net.Util.FailOnNonBulkMergesInfoStream;
    using Field = Field;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
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
    using PhraseQuery = Lucene.Net.Search.PhraseQuery;
    using PrintStreamInfoStream = Lucene.Net.Util.PrintStreamInfoStream;
    using Query = Lucene.Net.Search.Query;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using Sort = Lucene.Net.Search.Sort;
    using SortField = Lucene.Net.Search.SortField;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TopDocs = Lucene.Net.Search.TopDocs;

    // TODO
    //   - mix in forceMerge, addIndexes
    //   - randomly mix in non-congruent docs

    /// <summary>
    /// Utility class that spawns multiple indexing and
    ///  searching threads.
    /// </summary>
    public abstract class ThreadedIndexingAndSearchingTestCase : LuceneTestCase
    {
        protected internal readonly AtomicBoolean failed = new AtomicBoolean();
        protected internal readonly AtomicInt32 addCount = new AtomicInt32();
        protected internal readonly AtomicInt32 delCount = new AtomicInt32();
        protected internal readonly AtomicInt32 packCount = new AtomicInt32();

        protected internal Directory dir;
        protected internal IndexWriter writer;

        private class SubDocs
        {
            public readonly string packID;
            public readonly IList<string> subIDs;
            public bool deleted;

            public SubDocs(string packID, IList<string> subIDs)
            {
                this.packID = packID;
                this.subIDs = subIDs;
            }
        }

        // Called per-search
        protected internal abstract IndexSearcher CurrentSearcher { get; }

        protected internal abstract IndexSearcher FinalSearcher { get; }

        protected internal virtual void ReleaseSearcher(IndexSearcher s)
        {
        }

        // Called once to run searching
        protected internal abstract void DoSearching(TaskScheduler es, long stopTime);

        protected internal virtual Directory GetDirectory(Directory @in)
        {
            return @in;
        }

        protected internal virtual void UpdateDocuments(Term id, IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            writer.UpdateDocuments(id, docs);
        }

        protected internal virtual void AddDocuments(Term id, IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            writer.AddDocuments(docs);
        }

        protected internal virtual void AddDocument(Term id, IEnumerable<IIndexableField> doc)
        {
            writer.AddDocument(doc);
        }

        protected internal virtual void UpdateDocument(Term term, IEnumerable<IIndexableField> doc)
        {
            writer.UpdateDocument(term, doc);
        }

        protected internal virtual void DeleteDocuments(Term term)
        {
            writer.DeleteDocuments(term);
        }

        protected internal virtual void DoAfterIndexingThreadDone()
        {
        }

        private ThreadClass[] LaunchIndexingThreads(LineFileDocs docs, 
                                                    int numThreads, 
                                                    long stopTime, 
                                                    ISet<string> delIDs, 
                                                    ISet<string> delPackIDs, 
                                                    IList<SubDocs> allSubDocs)
        {
            ThreadClass[] threads = new ThreadClass[numThreads];
            for (int thread = 0; thread < numThreads; thread++)
            {
                threads[thread] = new ThreadAnonymousInnerClassHelper(this, docs, stopTime, delIDs, delPackIDs, allSubDocs);
                threads[thread].SetDaemon(true);
                threads[thread].Start();
            }

            return threads;
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly ThreadedIndexingAndSearchingTestCase outerInstance;

            private LineFileDocs docs;
            private long stopTime;
            private ISet<string> delIDs;
            private ISet<string> delPackIDs;
            private IList<SubDocs> allSubDocs;

            public ThreadAnonymousInnerClassHelper(ThreadedIndexingAndSearchingTestCase outerInstance, LineFileDocs docs, long stopTime, ISet<string> delIDs, ISet<string> delPackIDs, IList<SubDocs> allSubDocs)
            {
                this.outerInstance = outerInstance;
                this.docs = docs;
                this.stopTime = stopTime;
                this.delIDs = delIDs;
                this.delPackIDs = delPackIDs;
                this.allSubDocs = allSubDocs;
            }

            public override void Run()
            {
                // TODO: would be better if this were cross thread, so that we make sure one thread deleting anothers added docs works:
                IList<string> toDeleteIDs = new List<string>();
                IList<SubDocs> toDeleteSubDocs = new List<SubDocs>();
                while (Environment.TickCount < stopTime && !outerInstance.failed.Get())
                {
                    try
                    {
                        // Occasional longish pause if running
                        // nightly
                        if (LuceneTestCase.TEST_NIGHTLY && Random().Next(6) == 3)
                        {
                            if (VERBOSE)
                            {
                                Console.WriteLine(Thread.CurrentThread.Name + ": now long sleep");
                            }
                            Thread.Sleep(TestUtil.NextInt(Random(), 50, 500));
                        }

                        // Rate limit ingest rate:
                        if (Random().Next(7) == 5)
                        {
                            Thread.Sleep(TestUtil.NextInt(Random(), 1, 10));
                            if (VERBOSE)
                            {
                                Console.WriteLine(Thread.CurrentThread.Name + ": done sleep");
                            }
                        }

                        Document doc = docs.NextDoc();
                        if (doc == null)
                        {
                            break;
                        }

                        // Maybe add randomly named field
                        string addedField;
                        if (Random().NextBoolean())
                        {
                            addedField = "extra" + Random().Next(40);
                            doc.Add(outerInstance.NewTextField(addedField, "a random field", Field.Store.YES));
                        }
                        else
                        {
                            addedField = null;
                        }

                        if (Random().NextBoolean())
                        {
                            if (Random().NextBoolean())
                            {
                                // Add/update doc block:
                                string packID;
                                SubDocs delSubDocs;
                                if (toDeleteSubDocs.Count > 0 && Random().NextBoolean())
                                {
                                    delSubDocs = toDeleteSubDocs[Random().Next(toDeleteSubDocs.Count)];
                                    Debug.Assert(!delSubDocs.deleted);
                                    toDeleteSubDocs.Remove(delSubDocs);
                                    // Update doc block, replacing prior packID
                                    packID = delSubDocs.packID;
                                }
                                else
                                {
                                    delSubDocs = null;
                                    // Add doc block, using new packID
                                    packID = outerInstance.packCount.GetAndIncrement().ToString(CultureInfo.InvariantCulture);
                                }

                                Field packIDField = outerInstance.NewStringField("packID", packID, Field.Store.YES);
                                IList<string> docIDs = new List<string>();
                                SubDocs subDocs = new SubDocs(packID, docIDs);
                                IList<Document> docsList = new List<Document>();

                                allSubDocs.Add(subDocs);
                                doc.Add(packIDField);
                                docsList.Add(TestUtil.CloneDocument(doc));
                                docIDs.Add(doc.Get("docid"));

                                int maxDocCount = TestUtil.NextInt(Random(), 1, 10);
                                while (docsList.Count < maxDocCount)
                                {
                                    doc = docs.NextDoc();
                                    if (doc == null)
                                    {
                                        break;
                                    }
                                    docsList.Add(TestUtil.CloneDocument(doc));
                                    docIDs.Add(doc.Get("docid"));
                                }
                                outerInstance.addCount.AddAndGet(docsList.Count);

                                Term packIDTerm = new Term("packID", packID);

                                if (delSubDocs != null)
                                {
                                    delSubDocs.deleted = true;
                                    delIDs.AddAll(delSubDocs.subIDs);
                                    outerInstance.delCount.AddAndGet(delSubDocs.subIDs.Count);
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine(Thread.CurrentThread.Name + ": update pack packID=" + delSubDocs.packID + " count=" + docsList.Count + " docs=" + Arrays.ToString(docIDs));
                                    }
                                    outerInstance.UpdateDocuments(packIDTerm, docsList);
                                }
                                else
                                {
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine(Thread.CurrentThread.Name + ": add pack packID=" + packID + " count=" + docsList.Count + " docs=" + Arrays.ToString(docIDs));
                                    }
                                    outerInstance.AddDocuments(packIDTerm, docsList);
                                }
                                doc.RemoveField("packID");

                                if (Random().Next(5) == 2)
                                {
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine(Thread.CurrentThread.Name + ": buffer del id:" + packID);
                                    }
                                    toDeleteSubDocs.Add(subDocs);
                                }
                            }
                            else
                            {
                                // Add single doc
                                string docid = doc.Get("docid");
                                if (VERBOSE)
                                {
                                    Console.WriteLine(Thread.CurrentThread.Name + ": add doc docid:" + docid);
                                }
                                outerInstance.AddDocument(new Term("docid", docid), doc);
                                outerInstance.addCount.GetAndIncrement();

                                if (Random().Next(5) == 3)
                                {
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine(Thread.CurrentThread.Name + ": buffer del id:" + doc.Get("docid"));
                                    }
                                    toDeleteIDs.Add(docid);
                                }
                            }
                        }
                        else
                        {
                            // Update single doc, but we never re-use
                            // and ID so the delete will never
                            // actually happen:
                            if (VERBOSE)
                            {
                                Console.WriteLine(Thread.CurrentThread.Name + ": update doc id:" + doc.Get("docid"));
                            }
                            string docid = doc.Get("docid");
                            outerInstance.UpdateDocument(new Term("docid", docid), doc);
                            outerInstance.addCount.GetAndIncrement();

                            if (Random().Next(5) == 3)
                            {
                                if (VERBOSE)
                                {
                                    Console.WriteLine(Thread.CurrentThread.Name + ": buffer del id:" + doc.Get("docid"));
                                }
                                toDeleteIDs.Add(docid);
                            }
                        }

                        if (Random().Next(30) == 17)
                        {
                            if (VERBOSE)
                            {
                                Console.WriteLine(Thread.CurrentThread.Name + ": apply " + toDeleteIDs.Count + " deletes");
                            }
                            foreach (string id in toDeleteIDs)
                            {
                                if (VERBOSE)
                                {
                                    Console.WriteLine(Thread.CurrentThread.Name + ": del term=id:" + id);
                                }
                                outerInstance.DeleteDocuments(new Term("docid", id));
                            }
                            int count = outerInstance.delCount.AddAndGet(toDeleteIDs.Count);
                            if (VERBOSE)
                            {
                                Console.WriteLine(Thread.CurrentThread.Name + ": tot " + count + " deletes");
                            }
                            delIDs.AddAll(toDeleteIDs);
                            toDeleteIDs.Clear();

                            foreach (SubDocs subDocs in toDeleteSubDocs)
                            {
                                Debug.Assert(!subDocs.deleted);
                                delPackIDs.Add(subDocs.packID);
                                outerInstance.DeleteDocuments(new Term("packID", subDocs.packID));
                                subDocs.deleted = true;
                                if (VERBOSE)
                                {
                                    Console.WriteLine(Thread.CurrentThread.Name + ": del subs: " + subDocs.subIDs + " packID=" + subDocs.packID);
                                }
                                delIDs.AddAll(subDocs.subIDs);
                                outerInstance.delCount.AddAndGet(subDocs.subIDs.Count);
                            }
                            toDeleteSubDocs.Clear();
                        }
                        if (addedField != null)
                        {
                            doc.RemoveField(addedField);
                        }
                    }
                    catch (Exception t)
                    {
                        Console.WriteLine(Thread.CurrentThread.Name + ": hit exc");
                        Console.WriteLine(t.ToString());
                        Console.Write(t.StackTrace);
                        outerInstance.failed.Set(true);
                        throw new Exception(t.ToString(), t);
                    }
                }
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": indexing done");
                }

                outerInstance.DoAfterIndexingThreadDone();
            }
        }

        protected internal virtual void RunSearchThreads(long stopTime)
        {
            int numThreads = TestUtil.NextInt(Random(), 1, 5);
            ThreadClass[] searchThreads = new ThreadClass[numThreads];
            AtomicInt32 totHits = new AtomicInt32();

            // silly starting guess:
            AtomicInt32 totTermCount = new AtomicInt32(100);

            // TODO: we should enrich this to do more interesting searches
            for (int thread = 0; thread < searchThreads.Length; thread++)
            {
                searchThreads[thread] = new ThreadAnonymousInnerClassHelper2(this, stopTime, totHits, totTermCount);
                searchThreads[thread].SetDaemon(true);
                searchThreads[thread].Start();
            }

            for (int thread = 0; thread < searchThreads.Length; thread++)
            {
                searchThreads[thread].Join();
            }

            if (VERBOSE)
            {
                Console.WriteLine("TEST: DONE search: totHits=" + totHits);
            }
        }

        private class ThreadAnonymousInnerClassHelper2 : ThreadClass
        {
            private readonly ThreadedIndexingAndSearchingTestCase outerInstance;

            private long stopTimeMS;
            private AtomicInt32 totHits;
            private AtomicInt32 totTermCount;

            public ThreadAnonymousInnerClassHelper2(ThreadedIndexingAndSearchingTestCase outerInstance, long stopTimeMS, AtomicInt32 totHits, AtomicInt32 totTermCount)
            {
                this.outerInstance = outerInstance;
                this.stopTimeMS = stopTimeMS;
                this.totHits = totHits;
                this.totTermCount = totTermCount;
            }

            public override void Run()
            {
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": launch search thread");
                }
                while (Environment.TickCount < stopTimeMS)
                {
                    try
                    {
                        IndexSearcher s = outerInstance.CurrentSearcher;
                        try
                        {
                            // Verify 1) IW is correctly setting
                            // diagnostics, and 2) segment warming for
                            // merged segments is actually happening:
                            foreach (AtomicReaderContext sub in s.IndexReader.Leaves)
                            {
                                SegmentReader segReader = (SegmentReader)sub.Reader;
                                IDictionary<string, string> diagnostics = segReader.SegmentInfo.Info.Diagnostics;
                                assertNotNull(diagnostics);
                                string source;
                                diagnostics.TryGetValue("source", out source);
                                assertNotNull(source);
                                if (source.Equals("merge", StringComparison.Ordinal))
                                {
                                    assertTrue("sub reader " + sub + " wasn't warmed: warmed=" + outerInstance.warmed + " diagnostics=" + diagnostics + " si=" + segReader.SegmentInfo,
                                        !outerInstance.assertMergedSegmentsWarmed || outerInstance.warmed.ContainsKey(segReader.core));
                                }
                            }
                            if (s.IndexReader.NumDocs > 0)
                            {
                                outerInstance.SmokeTestSearcher(s);
                                Fields fields = MultiFields.GetFields(s.IndexReader);
                                if (fields == null)
                                {
                                    continue;
                                }
                                Terms terms = fields.GetTerms("body");
                                if (terms == null)
                                {
                                    continue;
                                }
                                TermsEnum termsEnum = terms.GetIterator(null);
                                int seenTermCount = 0;
                                int shift;
                                int trigger;
                                if (totTermCount.Get() < 30)
                                {
                                    shift = 0;
                                    trigger = 1;
                                }
                                else
                                {
                                    trigger = totTermCount.Get() / 30;
                                    shift = Random().Next(trigger);
                                }
                                while (Environment.TickCount < stopTimeMS)
                                {
                                    BytesRef term = termsEnum.Next();
                                    if (term == null)
                                    {
                                        totTermCount.Set(seenTermCount);
                                        break;
                                    }
                                    seenTermCount++;
                                    // search 30 terms
                                    if ((seenTermCount + shift) % trigger == 0)
                                    {
                                        //if (VERBOSE) {
                                        //System.out.println(Thread.currentThread().getName() + " now search body:" + term.Utf8ToString());
                                        //}
                                        totHits.AddAndGet(outerInstance.RunQuery(s, new TermQuery(new Term("body", term))));
                                    }
                                }
                                //if (VERBOSE) {
                                //System.out.println(Thread.currentThread().getName() + ": search done");
                                //}
                            }
                        }
                        finally
                        {
                            outerInstance.ReleaseSearcher(s);
                        }
                    }
                    catch (Exception t)
                    {
                        Console.WriteLine(Thread.CurrentThread.Name + ": hit exc");
                        outerInstance.failed.Set(true);
                        Console.WriteLine(t.ToString());
                        throw new Exception(t.ToString(), t);
                    }
                }
            }
        }

        protected internal virtual void DoAfterWriter(TaskScheduler es)
        {
        }

        protected internal virtual void DoClose()
        {
        }

        protected internal bool assertMergedSegmentsWarmed = true;

        private readonly IDictionary<SegmentCoreReaders, bool?> warmed = new WeakDictionary<SegmentCoreReaders, bool?>(); //new ConcurrentHashMapWrapper<SegmentCoreReaders, bool?>(new HashMap<SegmentCoreReaders, bool?>());
        // Collections.synchronizedMap(new WeakHashMap<SegmentCoreReaders, bool?>());

        public virtual void RunTest(string testName)
        {
            failed.Set(false);
            addCount.Set(0);
            delCount.Set(0);
            packCount.Set(0);

            long t0 = Environment.TickCount;

            Random random = new Random(Random().Next());
            LineFileDocs docs = new LineFileDocs(random, DefaultCodecSupportsDocValues());
            DirectoryInfo tempDir = CreateTempDir(testName);
            dir = GetDirectory(NewMockFSDirectory(tempDir)); // some subclasses rely on this being MDW
            if (dir is BaseDirectoryWrapper)
            {
                ((BaseDirectoryWrapper)dir).CheckIndexOnClose = false; // don't double-checkIndex, we do it ourselves.
            }
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetInfoStream(new FailOnNonBulkMergesInfoStream());

            if (LuceneTestCase.TEST_NIGHTLY)
            {
                // newIWConfig makes smallish max seg size, which
                // results in tons and tons of segments for this test
                // when run nightly:
                MergePolicy mp = conf.MergePolicy;
                if (mp is TieredMergePolicy)
                {
                    ((TieredMergePolicy)mp).MaxMergedSegmentMB = 5000.0;
                }
                else if (mp is LogByteSizeMergePolicy)
                {
                    ((LogByteSizeMergePolicy)mp).MaxMergeMB = 1000.0;
                }
                else if (mp is LogMergePolicy)
                {
                    ((LogMergePolicy)mp).MaxMergeDocs = 100000;
                }
            }

            conf.SetMergedSegmentWarmer(new IndexReaderWarmerAnonymousInnerClassHelper(this));

            if (VERBOSE)
            {
                conf.SetInfoStream(new PrintStreamInfoStreamAnonymousInnerClassHelper(this, Console.Out));
            }
            writer = new IndexWriter(dir, conf);
            TestUtil.ReduceOpenFiles(writer);

            TaskScheduler es = Random().NextBoolean() ? null : TaskScheduler.Default;

            DoAfterWriter(es);

            int NUM_INDEX_THREADS = TestUtil.NextInt(Random(), 2, 4);

            int RUN_TIME_SEC = LuceneTestCase.TEST_NIGHTLY ? 300 : RANDOM_MULTIPLIER;

            ISet<string> delIDs = new ConcurrentHashSet<string>(new HashSet<string>());
            ISet<string> delPackIDs = new ConcurrentHashSet<string>(new HashSet<string>());
            ConcurrentQueue<SubDocs> allSubDocs = new ConcurrentQueue<SubDocs>();

            long stopTime = Environment.TickCount + (RUN_TIME_SEC * 1000);

            ThreadClass[] indexThreads = LaunchIndexingThreads(docs, NUM_INDEX_THREADS, stopTime, delIDs, delPackIDs, allSubDocs.ToList());

            if (VERBOSE)
            {
                Console.WriteLine("TEST: DONE start " + NUM_INDEX_THREADS + " indexing threads [" + (Environment.TickCount - t0) + " ms]");
            }

            // Let index build up a bit
            Thread.Sleep(100);

            DoSearching(es, stopTime);

            if (VERBOSE)
            {
                Console.WriteLine("TEST: all searching done [" + (Environment.TickCount - t0) + " ms]");
            }

            for (int thread = 0; thread < indexThreads.Length; thread++)
            {
                indexThreads[thread].Join();
            }

            if (VERBOSE)
            {
                Console.WriteLine("TEST: done join indexing threads [" + (Environment.TickCount - t0) + " ms]; addCount=" + addCount + " delCount=" + delCount);
            }

            IndexSearcher s = FinalSearcher;
            if (VERBOSE)
            {
                Console.WriteLine("TEST: finalSearcher=" + s);
            }

            assertFalse(failed.Get());

            bool doFail = false;

            // Verify: make sure delIDs are in fact deleted:
            foreach (string id in delIDs)
            {
                TopDocs hits = s.Search(new TermQuery(new Term("docid", id)), 1);
                if (hits.TotalHits != 0)
                {
                    Console.WriteLine("doc id=" + id + " is supposed to be deleted, but got " + hits.TotalHits + " hits; first docID=" + hits.ScoreDocs[0].Doc);
                    doFail = true;
                }
            }

            // Verify: make sure delPackIDs are in fact deleted:
            foreach (string id in delPackIDs)
            {
                TopDocs hits = s.Search(new TermQuery(new Term("packID", id)), 1);
                if (hits.TotalHits != 0)
                {
                    Console.WriteLine("packID=" + id + " is supposed to be deleted, but got " + hits.TotalHits + " matches");
                    doFail = true;
                }
            }

            // Verify: make sure each group of sub-docs are still in docID order:
            foreach (SubDocs subDocs in allSubDocs.ToList())
            {
                TopDocs hits = s.Search(new TermQuery(new Term("packID", subDocs.packID)), 20);
                if (!subDocs.deleted)
                {
                    // We sort by relevance but the scores should be identical so sort falls back to by docID:
                    if (hits.TotalHits != subDocs.subIDs.Count)
                    {
                        Console.WriteLine("packID=" + subDocs.packID + ": expected " + subDocs.subIDs.Count + " hits but got " + hits.TotalHits);
                        doFail = true;
                    }
                    else
                    {
                        int lastDocID = -1;
                        int startDocID = -1;
                        foreach (ScoreDoc scoreDoc in hits.ScoreDocs)
                        {
                            int docID = scoreDoc.Doc;
                            if (lastDocID != -1)
                            {
                                assertEquals(1 + lastDocID, docID);
                            }
                            else
                            {
                                startDocID = docID;
                            }
                            lastDocID = docID;
                            Document doc = s.Doc(docID);
                            assertEquals(subDocs.packID, doc.Get("packID"));
                        }

                        lastDocID = startDocID - 1;
                        foreach (string subID in subDocs.subIDs)
                        {
                            hits = s.Search(new TermQuery(new Term("docid", subID)), 1);
                            assertEquals(1, hits.TotalHits);
                            int docID = hits.ScoreDocs[0].Doc;
                            if (lastDocID != -1)
                            {
                                assertEquals(1 + lastDocID, docID);
                            }
                            lastDocID = docID;
                        }
                    }
                }
                else
                {
                    // Pack was deleted -- make sure its docs are
                    // deleted.  We can't verify packID is deleted
                    // because we can re-use packID for update:
                    foreach (string subID in subDocs.subIDs)
                    {
                        assertEquals(0, s.Search(new TermQuery(new Term("docid", subID)), 1).TotalHits);
                    }
                }
            }

            // Verify: make sure all not-deleted docs are in fact
            // not deleted:
            int endID = Convert.ToInt32(docs.NextDoc().Get("docid"), CultureInfo.InvariantCulture);
            docs.Dispose();

            for (int id = 0; id < endID; id++)
            {
                string stringID = id.ToString(CultureInfo.InvariantCulture);
                if (!delIDs.Contains(stringID))
                {
                    TopDocs hits = s.Search(new TermQuery(new Term("docid", stringID)), 1);
                    if (hits.TotalHits != 1)
                    {
                        Console.WriteLine("doc id=" + stringID + " is not supposed to be deleted, but got hitCount=" + hits.TotalHits + "; delIDs=" + string.Join(",",  delIDs.ToArray()));
                        doFail = true;
                    }
                }
            }
            assertFalse(doFail);

            assertEquals("index=" + writer.SegString() + " addCount=" + addCount + " delCount=" + delCount, addCount.Get() - delCount.Get(), s.IndexReader.NumDocs);
            ReleaseSearcher(s);

            writer.Commit();

            assertEquals("index=" + writer.SegString() + " addCount=" + addCount + " delCount=" + delCount, addCount.Get() - delCount.Get(), writer.NumDocs);

            DoClose();
            writer.Dispose(false);

            // Cannot shutdown until after writer is closed because
            // writer has merged segment warmer that uses IS to run
            // searches, and that IS may be using this es!
            /*if (es != null)
            {
              es.shutdown();
              es.awaitTermination(1, TimeUnit.SECONDS);
            }*/

            TestUtil.CheckIndex(dir);
            dir.Dispose();
            System.IO.Directory.Delete(tempDir.FullName, true);

            if (VERBOSE)
            {
                Console.WriteLine("TEST: done [" + (Environment.TickCount - t0) + " ms]");
            }
        }

        private class IndexReaderWarmerAnonymousInnerClassHelper : IndexWriter.IndexReaderWarmer
        {
            private readonly ThreadedIndexingAndSearchingTestCase outerInstance;

            public IndexReaderWarmerAnonymousInnerClassHelper(ThreadedIndexingAndSearchingTestCase outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override void Warm(AtomicReader reader)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: now warm merged reader=" + reader);
                }
                outerInstance.warmed[((SegmentReader)reader).core] = true;
                int maxDoc = reader.MaxDoc;
                IBits liveDocs = reader.LiveDocs;
                int sum = 0;
                int inc = Math.Max(1, maxDoc / 50);
                for (int docID = 0; docID < maxDoc; docID += inc)
                {
                    if (liveDocs == null || liveDocs.Get(docID))
                    {
                        Document doc = reader.Document(docID);
                        sum += doc.Fields.Count;
                    }
                }

                IndexSearcher searcher = outerInstance.NewSearcher(reader);
                sum += searcher.Search(new TermQuery(new Term("body", "united")), 10).TotalHits;

                if (VERBOSE)
                {
                    Console.WriteLine("TEST: warm visited " + sum + " fields");
                }
            }
        }

        private class PrintStreamInfoStreamAnonymousInnerClassHelper : PrintStreamInfoStream
        {
            private readonly ThreadedIndexingAndSearchingTestCase outerInstance;

            public PrintStreamInfoStreamAnonymousInnerClassHelper(ThreadedIndexingAndSearchingTestCase outerInstance, TextWriter @out)
                : base(@out)
            {
                this.outerInstance = outerInstance;
            }

            public override void Message(string component, string message)
            {
                if ("TP".Equals(component, StringComparison.Ordinal))
                {
                    return; // ignore test points!
                }
                base.Message(component, message);
            }
        }

        private int RunQuery(IndexSearcher s, Query q)
        {
            s.Search(q, 10);
            int hitCount = s.Search(q, null, 10, new Sort(new SortField("title", SortFieldType.STRING))).TotalHits;
            if (DefaultCodecSupportsDocValues())
            {
                Sort dvSort = new Sort(new SortField("title", SortFieldType.STRING));
                int hitCount2 = s.Search(q, null, 10, dvSort).TotalHits;
                assertEquals(hitCount, hitCount2);
            }
            return hitCount;
        }

        protected internal virtual void SmokeTestSearcher(IndexSearcher s)
        {
            RunQuery(s, new TermQuery(new Term("body", "united")));
            RunQuery(s, new TermQuery(new Term("titleTokenized", "states")));
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("body", "united"));
            pq.Add(new Term("body", "states"));
            RunQuery(s, pq);
        }
    }
}