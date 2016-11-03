using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System.IO;
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using Bits = Lucene.Net.Util.Bits;
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
        protected internal readonly AtomicBoolean Failed = new AtomicBoolean();
        protected internal readonly AtomicInteger AddCount = new AtomicInteger();
        protected internal readonly AtomicInteger DelCount = new AtomicInteger();
        protected internal readonly AtomicInteger PackCount = new AtomicInteger();

        protected internal Directory Dir;
        protected internal IndexWriter Writer;

        private class SubDocs
        {
            public readonly string PackID;
            public readonly IList<string> SubIDs;
            public bool Deleted;

            public SubDocs(string packID, IList<string> subIDs)
            {
                this.PackID = packID;
                this.SubIDs = subIDs;
            }
        }

        // Called per-search
        protected internal abstract IndexSearcher CurrentSearcher { get; }

        protected internal abstract IndexSearcher FinalSearcher { get; }

        protected internal virtual void ReleaseSearcher(IndexSearcher s)
        {
        }

        // Called once to run searching
        protected internal abstract void DoSearching(TaskScheduler es, DateTime stopTime);

        protected internal virtual Directory GetDirectory(Directory @in)
        {
            return @in;
        }

        protected internal virtual void UpdateDocuments(Term id, IEnumerable<IEnumerable<IndexableField>> docs)
        {
            Writer.UpdateDocuments(id, docs);
        }

        protected internal virtual void AddDocuments(Term id, IEnumerable<IEnumerable<IndexableField>> docs)
        {
            Writer.AddDocuments(docs);
        }

        protected internal virtual void AddDocument(Term id, IEnumerable<IndexableField> doc)
        {
            Writer.AddDocument(doc);
        }

        protected internal virtual void UpdateDocument(Term term, IEnumerable<IndexableField> doc)
        {
            Writer.UpdateDocument(term, doc);
        }

        protected internal virtual void DeleteDocuments(Term term)
        {
            Writer.DeleteDocuments(term);
        }

        protected internal virtual void DoAfterIndexingThreadDone()
        {
        }

        private ThreadClass[] LaunchIndexingThreads(LineFileDocs docs, int numThreads, DateTime stopTime, ISet<string> delIDs, ISet<string> delPackIDs, IList<SubDocs> allSubDocs)
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
            private readonly ThreadedIndexingAndSearchingTestCase OuterInstance;

            private LineFileDocs Docs;
            private DateTime StopTime;
            private ISet<string> DelIDs;
            private ISet<string> DelPackIDs;
            private IList<SubDocs> AllSubDocs;

            public ThreadAnonymousInnerClassHelper(ThreadedIndexingAndSearchingTestCase outerInstance, LineFileDocs docs, DateTime stopTime, ISet<string> delIDs, ISet<string> delPackIDs, IList<SubDocs> allSubDocs)
            {
                this.OuterInstance = outerInstance;
                this.Docs = docs;
                this.StopTime = stopTime;
                this.DelIDs = delIDs;
                this.DelPackIDs = delPackIDs;
                this.AllSubDocs = allSubDocs;
            }

            public override void Run()
            {
                // TODO: would be better if this were cross thread, so that we make sure one thread deleting anothers added docs works:
                IList<string> toDeleteIDs = new List<string>();
                IList<SubDocs> toDeleteSubDocs = new List<SubDocs>();
                while (DateTime.UtcNow < StopTime && !OuterInstance.Failed.Get())
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

                        Document doc = Docs.NextDoc();
                        if (doc == null)
                        {
                            break;
                        }

                        // Maybe add randomly named field
                        string addedField;
                        if (Random().NextBoolean())
                        {
                            addedField = "extra" + Random().Next(40);
                            doc.Add(OuterInstance.NewTextField(addedField, "a random field", Field.Store.YES));
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
                                    Debug.Assert(!delSubDocs.Deleted);
                                    toDeleteSubDocs.Remove(delSubDocs);
                                    // Update doc block, replacing prior packID
                                    packID = delSubDocs.PackID;
                                }
                                else
                                {
                                    delSubDocs = null;
                                    // Add doc block, using new packID
                                    packID = OuterInstance.PackCount.IncrementAndGet() + "";
                                }

                                Field packIDField = OuterInstance.NewStringField("packID", packID, Field.Store.YES);
                                IList<string> docIDs = new List<string>();
                                SubDocs subDocs = new SubDocs(packID, docIDs);
                                IList<Document> docsList = new List<Document>();

                                AllSubDocs.Add(subDocs);
                                doc.Add(packIDField);
                                docsList.Add(TestUtil.CloneDocument(doc));
                                docIDs.Add(doc.Get("docid"));

                                int maxDocCount = TestUtil.NextInt(Random(), 1, 10);
                                while (docsList.Count < maxDocCount)
                                {
                                    doc = Docs.NextDoc();
                                    if (doc == null)
                                    {
                                        break;
                                    }
                                    docsList.Add(TestUtil.CloneDocument(doc));
                                    docIDs.Add(doc.Get("docid"));
                                }
                                OuterInstance.AddCount.AddAndGet(docsList.Count);

                                Term packIDTerm = new Term("packID", packID);

                                if (delSubDocs != null)
                                {
                                    delSubDocs.Deleted = true;
                                    DelIDs.AddAll(delSubDocs.SubIDs);
                                    OuterInstance.DelCount.AddAndGet(delSubDocs.SubIDs.Count);
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine(Thread.CurrentThread.Name + ": update pack packID=" + delSubDocs.PackID + " count=" + docsList.Count + " docs=" + docIDs);
                                    }
                                    OuterInstance.UpdateDocuments(packIDTerm, docsList);
                                }
                                else
                                {
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine(Thread.CurrentThread.Name + ": add pack packID=" + packID + " count=" + docsList.Count + " docs=" + docIDs);
                                    }
                                    OuterInstance.AddDocuments(packIDTerm, docsList);
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
                                OuterInstance.AddDocument(new Term("docid", docid), doc);
                                OuterInstance.AddCount.IncrementAndGet();

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
                            OuterInstance.UpdateDocument(new Term("docid", docid), doc);
                            OuterInstance.AddCount.IncrementAndGet();

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
                                OuterInstance.DeleteDocuments(new Term("docid", id));
                            }
                            int count = OuterInstance.DelCount.AddAndGet(toDeleteIDs.Count);
                            if (VERBOSE)
                            {
                                Console.WriteLine(Thread.CurrentThread.Name + ": tot " + count + " deletes");
                            }
                            DelIDs.AddAll(toDeleteIDs);
                            toDeleteIDs.Clear();

                            foreach (SubDocs subDocs in toDeleteSubDocs)
                            {
                                Debug.Assert(!subDocs.Deleted);
                                DelPackIDs.Add(subDocs.PackID);
                                OuterInstance.DeleteDocuments(new Term("packID", subDocs.PackID));
                                subDocs.Deleted = true;
                                if (VERBOSE)
                                {
                                    Console.WriteLine(Thread.CurrentThread.Name + ": del subs: " + subDocs.SubIDs + " packID=" + subDocs.PackID);
                                }
                                DelIDs.AddAll(subDocs.SubIDs);
                                OuterInstance.DelCount.AddAndGet(subDocs.SubIDs.Count);
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
                        OuterInstance.Failed.Set(true);
                        throw new Exception(t.Message, t);
                    }
                }
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": indexing done");
                }

                OuterInstance.DoAfterIndexingThreadDone();
            }
        }

        protected internal virtual void RunSearchThreads(DateTime stopTime)
        {
            int numThreads = TestUtil.NextInt(Random(), 1, 5);
            ThreadClass[] searchThreads = new ThreadClass[numThreads];
            AtomicInteger totHits = new AtomicInteger();

            // silly starting guess:
            AtomicInteger totTermCount = new AtomicInteger(100);

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
            private readonly ThreadedIndexingAndSearchingTestCase OuterInstance;

            private DateTime StopTime;
            private AtomicInteger TotHits;
            private AtomicInteger TotTermCount;

            public ThreadAnonymousInnerClassHelper2(ThreadedIndexingAndSearchingTestCase outerInstance, DateTime stopTime, AtomicInteger totHits, AtomicInteger totTermCount)
            {
                this.OuterInstance = outerInstance;
                this.StopTime = stopTime;
                this.TotHits = totHits;
                this.TotTermCount = totTermCount;
            }

            public override void Run()
            {
                if (VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": launch search thread");
                }
                while (DateTime.UtcNow < StopTime)
                {
                    try
                    {
                        IndexSearcher s = OuterInstance.CurrentSearcher;
                        try
                        {
                            // Verify 1) IW is correctly setting
                            // diagnostics, and 2) segment warming for
                            // merged segments is actually happening:
                            foreach (AtomicReaderContext sub in s.IndexReader.Leaves)
                            {
                                SegmentReader segReader = (SegmentReader)sub.Reader;
                                IDictionary<string, string> diagnostics = segReader.SegmentInfo.Info.Diagnostics;
                                Assert.IsNotNull(diagnostics);
                                string source = diagnostics["source"];
                                Assert.IsNotNull(source);
                                if (source.Equals("merge"))
                                {
                                    Assert.IsTrue(!OuterInstance.AssertMergedSegmentsWarmed || OuterInstance.Warmed.ContainsKey((SegmentCoreReaders)segReader.CoreCacheKey), "sub reader " + sub + " wasn't warmed: warmed=" + OuterInstance.Warmed + " diagnostics=" + diagnostics + " si=" + segReader.SegmentInfo);
                                }
                            }
                            if (s.IndexReader.NumDocs > 0)
                            {
                                OuterInstance.SmokeTestSearcher(s);
                                Fields fields = MultiFields.GetFields(s.IndexReader);
                                if (fields == null)
                                {
                                    continue;
                                }
                                Terms terms = fields.Terms("body");
                                if (terms == null)
                                {
                                    continue;
                                }
                                TermsEnum termsEnum = terms.Iterator(null);
                                int seenTermCount = 0;
                                int shift;
                                int trigger;
                                if (TotTermCount.Get() < 30)
                                {
                                    shift = 0;
                                    trigger = 1;
                                }
                                else
                                {
                                    trigger = TotTermCount.Get() / 30;
                                    shift = Random().Next(trigger);
                                }
                                while (DateTime.UtcNow < StopTime)
                                {
                                    BytesRef term = termsEnum.Next();
                                    if (term == null)
                                    {
                                        TotTermCount.Set(seenTermCount);
                                        break;
                                    }
                                    seenTermCount++;
                                    // search 30 terms
                                    if ((seenTermCount + shift) % trigger == 0)
                                    {
                                        //if (VERBOSE) {
                                        //System.out.println(Thread.currentThread().getName() + " now search body:" + term.Utf8ToString());
                                        //}
                                        TotHits.AddAndGet(OuterInstance.RunQuery(s, new TermQuery(new Term("body", term))));
                                    }
                                }
                                //if (VERBOSE) {
                                //System.out.println(Thread.currentThread().getName() + ": search done");
                                //}
                            }
                        }
                        finally
                        {
                            OuterInstance.ReleaseSearcher(s);
                        }
                    }
                    catch (Exception t)
                    {
                        Console.WriteLine(Thread.CurrentThread.Name + ": hit exc");
                        OuterInstance.Failed.Set(true);
                        Console.WriteLine(t.StackTrace);
                        throw new Exception(t.Message, t);
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

        protected internal bool AssertMergedSegmentsWarmed = true;

        private readonly IDictionary<SegmentCoreReaders, bool?> Warmed = new ConcurrentHashMapWrapper<SegmentCoreReaders, bool?>(new HashMap<SegmentCoreReaders, bool?>());
        // Collections.synchronizedMap(new WeakHashMap<SegmentCoreReaders, bool?>());

        public virtual void RunTest(string testName)
        {
            Failed.Set(false);
            AddCount.Set(0);
            DelCount.Set(0);
            PackCount.Set(0);

            DateTime t0 = DateTime.UtcNow;

            Random random = new Random(Random().Next());
            LineFileDocs docs = new LineFileDocs(random, DefaultCodecSupportsDocValues());
            DirectoryInfo tempDir = CreateTempDir(testName);
            Dir = GetDirectory(NewMockFSDirectory(tempDir)); // some subclasses rely on this being MDW
            if (Dir is BaseDirectoryWrapper)
            {
                ((BaseDirectoryWrapper)Dir).CheckIndexOnClose = false; // don't double-checkIndex, we do it ourselves.
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
                conf.InfoStream = new PrintStreamInfoStreamAnonymousInnerClassHelper(this, Console.Out);
            }
            Writer = new IndexWriter(Dir, conf);
            TestUtil.ReduceOpenFiles(Writer);

            TaskScheduler es = Random().NextBoolean() ? null : TaskScheduler.Default;

            DoAfterWriter(es);

            int NUM_INDEX_THREADS = TestUtil.NextInt(Random(), 2, 4);

            int RUN_TIME_SEC = LuceneTestCase.TEST_NIGHTLY ? 300 : RANDOM_MULTIPLIER;

            ISet<string> delIDs = new ConcurrentHashSet<string>(new HashSet<string>());
            ISet<string> delPackIDs = new ConcurrentHashSet<string>(new HashSet<string>());
            ConcurrentQueue<SubDocs> allSubDocs = new ConcurrentQueue<SubDocs>();

            DateTime stopTime = DateTime.UtcNow.AddSeconds(RUN_TIME_SEC);

            ThreadClass[] indexThreads = LaunchIndexingThreads(docs, NUM_INDEX_THREADS, stopTime, delIDs, delPackIDs, allSubDocs.ToList());

            if (VERBOSE)
            {
                Console.WriteLine("TEST: DONE start " + NUM_INDEX_THREADS + " indexing threads [" + (DateTime.UtcNow - t0).TotalMilliseconds + " ms]");
            }

            // Let index build up a bit
            Thread.Sleep(100);

            DoSearching(es, stopTime);

            if (VERBOSE)
            {
                Console.WriteLine("TEST: all searching done [" + (DateTime.UtcNow - t0).TotalMilliseconds + " ms]");
            }

            for (int thread = 0; thread < indexThreads.Length; thread++)
            {
                indexThreads[thread].Join();
            }

            if (VERBOSE)
            {
                Console.WriteLine("TEST: done join indexing threads [" + (DateTime.UtcNow - t0).TotalMilliseconds + " ms]; addCount=" + AddCount + " delCount=" + DelCount);
            }

            IndexSearcher s = FinalSearcher;
            if (VERBOSE)
            {
                Console.WriteLine("TEST: finalSearcher=" + s);
            }

            Assert.IsFalse(Failed.Get());

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
                TopDocs hits = s.Search(new TermQuery(new Term("packID", subDocs.PackID)), 20);
                if (!subDocs.Deleted)
                {
                    // We sort by relevance but the scores should be identical so sort falls back to by docID:
                    if (hits.TotalHits != subDocs.SubIDs.Count)
                    {
                        Console.WriteLine("packID=" + subDocs.PackID + ": expected " + subDocs.SubIDs.Count + " hits but got " + hits.TotalHits);
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
                                Assert.AreEqual(1 + lastDocID, docID);
                            }
                            else
                            {
                                startDocID = docID;
                            }
                            lastDocID = docID;
                            Document doc = s.Doc(docID);
                            Assert.AreEqual(subDocs.PackID, doc.Get("packID"));
                        }

                        lastDocID = startDocID - 1;
                        foreach (string subID in subDocs.SubIDs)
                        {
                            hits = s.Search(new TermQuery(new Term("docid", subID)), 1);
                            Assert.AreEqual(1, hits.TotalHits);
                            int docID = hits.ScoreDocs[0].Doc;
                            if (lastDocID != -1)
                            {
                                Assert.AreEqual(1 + lastDocID, docID);
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
                    foreach (string subID in subDocs.SubIDs)
                    {
                        Assert.AreEqual(0, s.Search(new TermQuery(new Term("docid", subID)), 1).TotalHits);
                    }
                }
            }

            // Verify: make sure all not-deleted docs are in fact
            // not deleted:
            int endID = Convert.ToInt32(docs.NextDoc().Get("docid"));
            docs.Dispose();

            for (int id = 0; id < endID; id++)
            {
                string stringID = "" + id;
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
            Assert.IsFalse(doFail);

            Assert.AreEqual(AddCount.Get() - DelCount.Get(), s.IndexReader.NumDocs, "index=" + Writer.SegString() + " addCount=" + AddCount + " delCount=" + DelCount);
            ReleaseSearcher(s);

            Writer.Commit();

            Assert.AreEqual(AddCount.Get() - DelCount.Get(), Writer.NumDocs(), "index=" + Writer.SegString() + " addCount=" + AddCount + " delCount=" + DelCount);

            DoClose();
            Writer.Dispose(false);

            // Cannot shutdown until after writer is closed because
            // writer has merged segment warmer that uses IS to run
            // searches, and that IS may be using this es!
            /*if (es != null)
            {
              es.shutdown();
              es.awaitTermination(1, TimeUnit.SECONDS);
            }*/

            TestUtil.CheckIndex(Dir);
            Dir.Dispose();
            System.IO.Directory.Delete(tempDir.FullName, true);

            if (VERBOSE)
            {
                Console.WriteLine("TEST: done [" + (DateTime.UtcNow - t0).TotalMilliseconds + " ms]");
            }
        }

        private class IndexReaderWarmerAnonymousInnerClassHelper : IndexWriter.IndexReaderWarmer
        {
            private readonly ThreadedIndexingAndSearchingTestCase OuterInstance;

            public IndexReaderWarmerAnonymousInnerClassHelper(ThreadedIndexingAndSearchingTestCase outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override void Warm(AtomicReader reader)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: now warm merged reader=" + reader);
                }
                OuterInstance.Warmed[(SegmentCoreReaders)reader.CoreCacheKey] = true;
                int maxDoc = reader.MaxDoc;
                Bits liveDocs = reader.LiveDocs;
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

                IndexSearcher searcher = OuterInstance.NewSearcher(reader);
                sum += searcher.Search(new TermQuery(new Term("body", "united")), 10).TotalHits;

                if (VERBOSE)
                {
                    Console.WriteLine("TEST: warm visited " + sum + " fields");
                }
            }
        }

        private class PrintStreamInfoStreamAnonymousInnerClassHelper : PrintStreamInfoStream
        {
            private readonly ThreadedIndexingAndSearchingTestCase OuterInstance;

            public PrintStreamInfoStreamAnonymousInnerClassHelper(ThreadedIndexingAndSearchingTestCase outerInstance, TextWriter @out)
                : base(@out)
            {
                this.OuterInstance = outerInstance;
            }

            public override void Message(string component, string message)
            {
                if ("TP".Equals(component))
                {
                    return; // ignore test points!
                }
                base.Message(component, message);
            }
        }

        private int RunQuery(IndexSearcher s, Query q)
        {
            s.Search(q, 10);
            int hitCount = s.Search(q, null, 10, new Sort(new SortField("title", SortField.Type_e.STRING))).TotalHits;
            if (DefaultCodecSupportsDocValues())
            {
                Sort dvSort = new Sort(new SortField("title", SortField.Type_e.STRING));
                int hitCount2 = s.Search(q, null, 10, dvSort).TotalHits;
                Assert.AreEqual(hitCount, hitCount2);
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