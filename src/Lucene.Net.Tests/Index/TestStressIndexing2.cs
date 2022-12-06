using J2N.Collections.Generic.Extensions;
using J2N.Numerics;
using J2N.Text;
using J2N.Threading;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using JCG = J2N.Collections.Generic;
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
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TextField = TextField;

    [TestFixture]
    public class TestStressIndexing2 : LuceneTestCase
    {
        private static int maxFields = 4;
        private static int bigFieldSize = 10;
        private static bool sameFieldOrder = false;
        private static int mergeFactor = 3;
        private static int maxBufferedDocs = 3;
        private static int seed = 0;

        public sealed class YieldTestPoint : ITestPoint
        {
            private readonly TestStressIndexing2 outerInstance;

            public YieldTestPoint(TestStressIndexing2 outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public void Apply(string name)
            {
                //      if (name.equals("startCommit")) {
                if (Random.Next(4) == 2)
                {
                    Thread.Yield();
                }
            }
        }

        //
        [Test]
        public virtual void TestRandomIWReader()
        {
            Directory dir = NewDirectory();

            // TODO: verify equals using IW.getReader
            DocsAndWriter dw = IndexRandomIWReader(5, 3, 100, dir);
            DirectoryReader reader = dw.writer.GetReader();
            dw.writer.Commit();
            VerifyEquals(Random, reader, dir, "id");
            reader.Dispose();
            dw.writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestRandom()
        {
            Directory dir1 = NewDirectory();
            Directory dir2 = NewDirectory();
            // mergeFactor=2; maxBufferedDocs=2; Map docs = indexRandom(1, 3, 2, dir1);
            int maxThreadStates = 1 + Random.Next(10);
            bool doReaderPooling = Random.NextBoolean();
            IDictionary<string, Document> docs = IndexRandom(5, 3, 100, dir1, maxThreadStates, doReaderPooling);
            IndexSerial(Random, docs, dir2);

            // verifying verify
            // verifyEquals(dir1, dir1, "id");
            // verifyEquals(dir2, dir2, "id");

            VerifyEquals(dir1, dir2, "id");
            dir1.Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestMultiConfig()
        {
            // test lots of smaller different params together

            int num = AtLeast(3);
            for (int i = 0; i < num; i++) // increase iterations for better testing
            {
                if (Verbose)
                {
                    Console.WriteLine("\n\nTEST: top iter=" + i);
                }
                sameFieldOrder = Random.NextBoolean();
                mergeFactor = Random.Next(3) + 2;
                maxBufferedDocs = Random.Next(3) + 2;
                int maxThreadStates = 1 + Random.Next(10);
                bool doReaderPooling = Random.NextBoolean();
                seed++;

                int nThreads = Random.Next(5) + 1;
                int iter = Random.Next(5) + 1;
                int range = Random.Next(20) + 1;
                Directory dir1 = NewDirectory();
                Directory dir2 = NewDirectory();
                if (Verbose)
                {
                    Console.WriteLine("  nThreads=" + nThreads + " iter=" + iter + " range=" + range + " doPooling=" + doReaderPooling + " maxThreadStates=" + maxThreadStates + " sameFieldOrder=" + sameFieldOrder + " mergeFactor=" + mergeFactor + " maxBufferedDocs=" + maxBufferedDocs);
                }
                IDictionary<string, Document> docs = IndexRandom(nThreads, iter, range, dir1, maxThreadStates, doReaderPooling);
                if (Verbose)
                {
                    Console.WriteLine("TEST: index serial");
                }
                IndexSerial(Random, docs, dir2);
                if (Verbose)
                {
                    Console.WriteLine("TEST: verify");
                }
                VerifyEquals(dir1, dir2, "id");
                dir1.Dispose();
                dir2.Dispose();
            }
        }

        internal static Term idTerm = new Term("id", "");
        internal IndexingThread[] threads;

        internal static IComparer<IIndexableField> fieldNameComparer = Comparer<IIndexableField>.Create((o1, o2) => o1.Name.CompareToOrdinal(o2.Name));

        // this test avoids using any extra synchronization in the multiple
        // indexing threads to test that IndexWriter does correctly synchronize
        // everything.

        public class DocsAndWriter
        {
            internal IDictionary<string, Document> docs;
            internal IndexWriter writer;
        }

        public virtual DocsAndWriter IndexRandomIWReader(int nThreads, int iterations, int range, Directory dir)
        {
            IDictionary<string, Document> docs = new Dictionary<string, Document>();
            IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetRAMBufferSizeMB(0.1).SetMaxBufferedDocs(maxBufferedDocs).SetMergePolicy(NewLogMergePolicy()), new YieldTestPoint(this));
            w.Commit();
            LogMergePolicy lmp = (LogMergePolicy)w.Config.MergePolicy;
            lmp.NoCFSRatio = 0.0;
            lmp.MergeFactor = mergeFactor;
            /*
            ///    w.setMaxMergeDocs(Integer.MAX_VALUE);
            ///    w.setMaxFieldLength(10000);
            ///    w.SetRAMBufferSizeMB(1);
            ///    w.setMergeFactor(10);
            */

            threads = new IndexingThread[nThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                IndexingThread th = new IndexingThread(this);
                th.w = w;
                th.@base = 1000000 * i;
                th.range = range;
                th.iterations = iterations;
                threads[i] = th;
            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Start();
            }
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            // w.ForceMerge(1);
            //w.Dispose();

            for (int i = 0; i < threads.Length; i++)
            {
                IndexingThread th = threads[i];
                UninterruptableMonitor.Enter(th);
                try
                {
                    docs.PutAll(th.docs);
                }
                finally
                {
                    UninterruptableMonitor.Exit(th);
                }
            }

            TestUtil.CheckIndex(dir);
            DocsAndWriter dw = new DocsAndWriter();
            dw.docs = docs;
            dw.writer = w;
            return dw;
        }

        public virtual IDictionary<string, Document> IndexRandom(int nThreads, int iterations, int range, Directory dir, int maxThreadStates, bool doReaderPooling)
        {
            IDictionary<string, Document> docs = new Dictionary<string, Document>();
            IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetRAMBufferSizeMB(0.1).SetMaxBufferedDocs(maxBufferedDocs).SetIndexerThreadPool(new DocumentsWriterPerThreadPool(maxThreadStates)).SetReaderPooling(doReaderPooling).SetMergePolicy(NewLogMergePolicy()), new YieldTestPoint(this));
            LogMergePolicy lmp = (LogMergePolicy)w.Config.MergePolicy;
            lmp.NoCFSRatio = 0.0;
            lmp.MergeFactor = mergeFactor;

            threads = new IndexingThread[nThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                IndexingThread th = new IndexingThread(this);
                th.w = w;
                th.@base = 1000000 * i;
                th.range = range;
                th.iterations = iterations;
                threads[i] = th;
            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Start();
            }
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            //w.ForceMerge(1);
            w.Dispose();

            for (int i = 0; i < threads.Length; i++)
            {
                IndexingThread th = threads[i];
                UninterruptableMonitor.Enter(th);
                try
                {
                    docs.PutAll(th.docs);
                }
                finally
                {
                    UninterruptableMonitor.Exit(th);
                }
            }

            //System.out.println("TEST: checkindex");
            TestUtil.CheckIndex(dir);

            return docs;
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        public void IndexSerial(Random random, IDictionary<string, Document> docs, Directory dir)
        {
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(random, TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetMergePolicy(NewLogMergePolicy()));

            // index all docs in a single thread
            IEnumerator<Document> iter = docs.Values.GetEnumerator();
            while (iter.MoveNext())
            {
                Document d = iter.Current;
                IList<IIndexableField> fields = new JCG.List<IIndexableField>();
                fields.AddRange(d.Fields);
                // put fields in same order each time
                fields.Sort(fieldNameComparer);

                Document d1 = new Document();
                for (int i = 0; i < fields.Count; i++)
                {
                    d1.Add(fields[i]);
                }
                w.AddDocument(d1);
                // System.out.println("indexing "+d1);
            }

            w.Dispose();
        }

        public virtual void VerifyEquals(Random r, DirectoryReader r1, Directory dir2, string idField)
        {
            DirectoryReader r2 = DirectoryReader.Open(dir2);
            VerifyEquals(r1, r2, idField);
            r2.Dispose();
        }

        public virtual void VerifyEquals(Directory dir1, Directory dir2, string idField)
        {
            DirectoryReader r1 = DirectoryReader.Open(dir1);
            DirectoryReader r2 = DirectoryReader.Open(dir2);
            VerifyEquals(r1, r2, idField);
            r1.Dispose();
            r2.Dispose();
        }

        private static void PrintDocs(DirectoryReader r)
        {
            foreach (AtomicReaderContext ctx in r.Leaves)
            {
                // TODO: improve this
                AtomicReader sub = (AtomicReader)ctx.Reader;
                IBits liveDocs = sub.LiveDocs;
                Console.WriteLine("  " + ((SegmentReader)sub).SegmentInfo);
                for (int docID = 0; docID < sub.MaxDoc; docID++)
                {
                    Document doc = sub.Document(docID);
                    if (liveDocs is null || liveDocs.Get(docID))
                    {
                        Console.WriteLine("    docID=" + docID + " id:" + doc.Get("id"));
                    }
                    else
                    {
                        Console.WriteLine("    DEL docID=" + docID + " id:" + doc.Get("id"));
                    }
                }
            }
        }

        public virtual void VerifyEquals(DirectoryReader r1, DirectoryReader r2, string idField)
        {
            if (Verbose)
            {
                Console.WriteLine("\nr1 docs:");
                PrintDocs(r1);
                Console.WriteLine("\nr2 docs:");
                PrintDocs(r2);
            }
            if (r1.NumDocs != r2.NumDocs)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(false,"r1.NumDocs={0} vs r2.NumDocs={1}", r1.NumDocs, r2.NumDocs);
            }
            bool hasDeletes = !(r1.MaxDoc == r2.MaxDoc && r1.NumDocs == r1.MaxDoc);

            int[] r2r1 = new int[r2.MaxDoc]; // r2 id to r1 id mapping

            // create mapping from id2 space to id2 based on idField
            Fields f1 = MultiFields.GetFields(r1);
            if (f1 is null)
            {
                // make sure r2 is empty
                Assert.IsNull(MultiFields.GetFields(r2));
                return;
            }
            Terms terms1 = f1.GetTerms(idField);
            if (terms1 is null)
            {
                Assert.IsTrue(MultiFields.GetFields(r2) is null || MultiFields.GetFields(r2).GetTerms(idField) is null);
                return;
            }
            TermsEnum termsEnum = terms1.GetEnumerator();

            IBits liveDocs1 = MultiFields.GetLiveDocs(r1);
            IBits liveDocs2 = MultiFields.GetLiveDocs(r2);

            Fields fields = MultiFields.GetFields(r2);
            if (fields is null)
            {
                // make sure r1 is in fact empty (eg has only all
                // deleted docs):
                IBits liveDocs = MultiFields.GetLiveDocs(r1);
                DocsEnum docs = null;
                while (termsEnum.MoveNext())
                {
                    docs = TestUtil.Docs(Random, termsEnum, liveDocs, docs, DocsFlags.NONE);
                    while (docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        Assert.Fail("r1 is not empty but r2 is");
                    }
                }
                return;
            }
            Terms terms2 = fields.GetTerms(idField);
            TermsEnum termsEnum2 = terms2.GetEnumerator();

            DocsEnum termDocs1 = null;
            DocsEnum termDocs2 = null;

            while (termsEnum.MoveNext())
            {
                BytesRef term = termsEnum.Term;
                //System.out.println("TEST: match id term=" + term);

                termDocs1 = TestUtil.Docs(Random, termsEnum, liveDocs1, termDocs1, DocsFlags.NONE);
                if (termsEnum2.SeekExact(term))
                {
                    termDocs2 = TestUtil.Docs(Random, termsEnum2, liveDocs2, termDocs2, DocsFlags.NONE);
                }
                else
                {
                    termDocs2 = null;
                }

                if (termDocs1.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
                {
                    // this doc is deleted and wasn't replaced
                    Assert.IsTrue(termDocs2 is null || termDocs2.NextDoc() == DocIdSetIterator.NO_MORE_DOCS);
                    continue;
                }

                int id1 = termDocs1.DocID;
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, termDocs1.NextDoc());

                Assert.IsTrue(termDocs2.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                int id2 = termDocs2.DocID;
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, termDocs2.NextDoc());

                r2r1[id2] = id1;

                // verify stored fields are equivalent
                try
                {
                    VerifyEquals(r1.Document(id1), r2.Document(id2));
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    Console.WriteLine("FAILED id=" + term + " id1=" + id1 + " id2=" + id2 + " term=" + term);
                    Console.WriteLine("  d1=" + r1.Document(id1));
                    Console.WriteLine("  d2=" + r2.Document(id2));
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }

                try
                {
                    // verify term vectors are equivalent
                    VerifyEquals(r1.GetTermVectors(id1), r2.GetTermVectors(id2));
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    Console.WriteLine("FAILED id=" + term + " id1=" + id1 + " id2=" + id2);
                    Fields tv1 = r1.GetTermVectors(id1);
                    Console.WriteLine("  d1=" + tv1);
                    if (tv1 != null)
                    {
                        DocsAndPositionsEnum dpEnum = null;
                        DocsEnum dEnum = null;
                        foreach (string field in tv1)
                        {
                            Console.WriteLine("    " + field + ":");
                            Terms terms3 = tv1.GetTerms(field);
                            Assert.IsNotNull(terms3);
                            TermsEnum termsEnum3 = terms3.GetEnumerator();
                            while (termsEnum3.MoveNext())
                            {
                                Console.WriteLine("      " + termsEnum3.Term.Utf8ToString() + ": freq=" + termsEnum3.TotalTermFreq);
                                dpEnum = termsEnum3.DocsAndPositions(null, dpEnum);
                                if (dpEnum != null)
                                {
                                    Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                                    int freq = dpEnum.Freq;
                                    Console.WriteLine("        doc=" + dpEnum.DocID + " freq=" + freq);
                                    for (int posUpto = 0; posUpto < freq; posUpto++)
                                    {
                                        Console.WriteLine("          pos=" + dpEnum.NextPosition());
                                    }
                                }
                                else
                                {
                                    dEnum = TestUtil.Docs(Random, termsEnum3, null, dEnum, DocsFlags.FREQS);
                                    Assert.IsNotNull(dEnum);
                                    Assert.IsTrue(dEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                                    int freq = dEnum.Freq;
                                    Console.WriteLine("        doc=" + dEnum.DocID + " freq=" + freq);
                                }
                            }
                        }
                    }

                    Fields tv2 = r2.GetTermVectors(id2);
                    Console.WriteLine("  d2=" + tv2);
                    if (tv2 != null)
                    {
                        DocsAndPositionsEnum dpEnum = null;
                        DocsEnum dEnum = null;
                        foreach (string field in tv2)
                        {
                            Console.WriteLine("    " + field + ":");
                            Terms terms3 = tv2.GetTerms(field);
                            Assert.IsNotNull(terms3);
                            TermsEnum termsEnum3 = terms3.GetEnumerator();
                            while (termsEnum3.MoveNext())
                            {
                                Console.WriteLine("      " + termsEnum3.Term.Utf8ToString() + ": freq=" + termsEnum3.TotalTermFreq);
                                dpEnum = termsEnum3.DocsAndPositions(null, dpEnum);
                                if (dpEnum != null)
                                {
                                    Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                                    int freq = dpEnum.Freq;
                                    Console.WriteLine("        doc=" + dpEnum.DocID + " freq=" + freq);
                                    for (int posUpto = 0; posUpto < freq; posUpto++)
                                    {
                                        Console.WriteLine("          pos=" + dpEnum.NextPosition());
                                    }
                                }
                                else
                                {
                                    dEnum = TestUtil.Docs(Random, termsEnum3, null, dEnum, DocsFlags.FREQS);
                                    Assert.IsNotNull(dEnum);
                                    Assert.IsTrue(dEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                                    int freq = dEnum.Freq;
                                    Console.WriteLine("        doc=" + dEnum.DocID + " freq=" + freq);
                                }
                            }
                        }
                    }

                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
            }

            //System.out.println("TEST: done match id");

            // Verify postings
            //System.out.println("TEST: create te1");
            Fields fields1 = MultiFields.GetFields(r1);
            IEnumerator<string> fields1Enum = fields1.GetEnumerator();
            Fields fields2 = MultiFields.GetFields(r2);
            IEnumerator<string> fields2Enum = fields2.GetEnumerator();

            string field1 = null, field2 = null;
            TermsEnum termsEnum1 = null;
            termsEnum2 = null;
            DocsEnum docs1 = null, docs2 = null;

            // pack both doc and freq into single element for easy sorting
            long[] info1 = new long[r1.NumDocs];
            long[] info2 = new long[r2.NumDocs];

            for (; ; )
            {
                BytesRef term1 = null, term2 = null;

                // iterate until we get some docs
                int len1;
                for (; ; )
                {
                    len1 = 0;
                    if (termsEnum1 is null)
                    {
                        if (!fields1Enum.MoveNext())
                        {
                            break;
                        }
                        field1 = fields1Enum.Current;
                        Terms terms = fields1.GetTerms(field1);
                        if (terms is null)
                        {
                            continue;
                        }
                        termsEnum1 = terms.GetEnumerator();
                    }
                    if (!termsEnum1.MoveNext())
                    {
                        term1 = null;
                        // no more terms in this field
                        termsEnum1 = null;
                        continue;
                    }
                    term1 = termsEnum1.Term;

                    //System.out.println("TEST: term1=" + term1);
                    docs1 = TestUtil.Docs(Random, termsEnum1, liveDocs1, docs1, DocsFlags.FREQS);
                    while (docs1.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        int d = docs1.DocID;
                        int f = docs1.Freq;
                        info1[len1] = (((long)d) << 32) | (uint)f;
                        len1++;
                    }
                    if (len1 > 0)
                    {
                        break;
                    }
                }

                // iterate until we get some docs
                int len2;
                for (; ; )
                {
                    len2 = 0;
                    if (termsEnum2 is null)
                    {
                        if (!fields2Enum.MoveNext())
                        {
                            break;
                        }
                        field2 = fields2Enum.Current;
                        Terms terms = fields2.GetTerms(field2);
                        if (terms is null)
                        {
                            continue;
                        }
                        termsEnum2 = terms.GetEnumerator();
                    }
                    if (!termsEnum2.MoveNext())
                    {
                        term2 = null;
                        // no more terms in this field
                        termsEnum2 = null;
                        continue;
                    }
                    term2 = termsEnum2.Term;

                    //System.out.println("TEST: term1=" + term1);
                    docs2 = TestUtil.Docs(Random, termsEnum2, liveDocs2, docs2, DocsFlags.FREQS);
                    while (docs2.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        int d = r2r1[docs2.DocID];
                        int f = docs2.Freq;
                        info2[len2] = (((long)d) << 32) | (uint)f;
                        len2++;
                    }
                    if (len2 > 0)
                    {
                        break;
                    }
                }

                Assert.AreEqual(len1, len2);
                if (len1 == 0) // no more terms
                {
                    break;
                }

                Assert.AreEqual(field1, field2);
                Assert.IsTrue(term1.BytesEquals(term2));

                if (!hasDeletes)
                {
                    Assert.AreEqual(termsEnum1.DocFreq, termsEnum2.DocFreq);
                }

                Assert.AreEqual(term1, term2, "len1=" + len1 + " len2=" + len2 + " deletes?=" + hasDeletes);

                // sort info2 to get it into ascending docid
                Array.Sort(info2, 0, len2);

                // now compare
                for (int i = 0; i < len1; i++)
                {
                    Assert.AreEqual(info1[i], info2[i], "i=" + i + " len=" + len1 + " d1=" + (info1[i].TripleShift(32)) + " f1=" + (info1[i] & int.MaxValue) + " d2=" + (info2[i].TripleShift(32)) + " f2=" + (info2[i] & int.MaxValue) + " field=" + field1 + " term=" + term1.Utf8ToString());
                }
            }
        }

        public static void VerifyEquals(Document d1, Document d2)
        {
            IList<IIndexableField> ff1 = d1.Fields;
            IList<IIndexableField> ff2 = d2.Fields;

            ff1.Sort(fieldNameComparer);
            ff2.Sort(fieldNameComparer);

            Assert.AreEqual(ff1.Count, ff2.Count, ff1 + " : " + ff2);

            for (int i = 0; i < ff1.Count; i++)
            {
                IIndexableField f1 = ff1[i];
                IIndexableField f2 = ff2[i];
                if (f1.GetBinaryValue() != null)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(f2.GetBinaryValue() != null);
                }
                else
                {
                    string s1 = f1.GetStringValue();
                    string s2 = f2.GetStringValue();
                    Assert.AreEqual(s1, s2, ff1 + " : " + ff2);
                }
            }
        }

        public static void VerifyEquals(Fields d1, Fields d2)
        {
            if (d1 is null)
            {
                Assert.IsTrue(d2 is null || d2.Count == 0);
                return;
            }
            Assert.IsTrue(d2 != null);

            IEnumerator<string> fieldsEnum2 = d2.GetEnumerator();

            foreach (string field1 in d1)
            {
                fieldsEnum2.MoveNext();
                string field2 = fieldsEnum2.Current;
                Assert.AreEqual(field1, field2);

                Terms terms1 = d1.GetTerms(field1);
                Assert.IsNotNull(terms1);
                TermsEnum termsEnum1 = terms1.GetEnumerator();

                Terms terms2 = d2.GetTerms(field2);
                Assert.IsNotNull(terms2);
                TermsEnum termsEnum2 = terms2.GetEnumerator();

                DocsAndPositionsEnum dpEnum1 = null;
                DocsAndPositionsEnum dpEnum2 = null;
                DocsEnum dEnum1 = null;
                DocsEnum dEnum2 = null;

                BytesRef term1;
                while (termsEnum1.MoveNext())
                {
                    term1 = termsEnum1.Term;
                    termsEnum2.MoveNext();
                    BytesRef term2 = termsEnum2.Term;
                    Assert.AreEqual(term1, term2);
                    Assert.AreEqual(termsEnum1.TotalTermFreq, termsEnum2.TotalTermFreq);

                    dpEnum1 = termsEnum1.DocsAndPositions(null, dpEnum1);
                    dpEnum2 = termsEnum2.DocsAndPositions(null, dpEnum2);
                    if (dpEnum1 != null)
                    {
                        Assert.IsNotNull(dpEnum2);
                        int docID1 = dpEnum1.NextDoc();
                        dpEnum2.NextDoc();
                        // docIDs are not supposed to be equal
                        //int docID2 = dpEnum2.NextDoc();
                        //Assert.AreEqual(docID1, docID2);
                        Assert.IsTrue(docID1 != DocIdSetIterator.NO_MORE_DOCS);

                        int freq1 = dpEnum1.Freq;
                        int freq2 = dpEnum2.Freq;
                        Assert.AreEqual(freq1, freq2);
                        IOffsetAttribute offsetAtt1 = dpEnum1.Attributes.HasAttribute<IOffsetAttribute>() ? dpEnum1.Attributes.GetAttribute<IOffsetAttribute>() : null;
                        IOffsetAttribute offsetAtt2 = dpEnum2.Attributes.HasAttribute<IOffsetAttribute>() ? dpEnum2.Attributes.GetAttribute<IOffsetAttribute>() : null;

                        if (offsetAtt1 != null)
                        {
                            Assert.IsNotNull(offsetAtt2);
                        }
                        else
                        {
                            Assert.IsNull(offsetAtt2);
                        }

                        for (int posUpto = 0; posUpto < freq1; posUpto++)
                        {
                            int pos1 = dpEnum1.NextPosition();
                            int pos2 = dpEnum2.NextPosition();
                            Assert.AreEqual(pos1, pos2);
                            if (offsetAtt1 != null)
                            {
                                Assert.AreEqual(offsetAtt1.StartOffset, offsetAtt2.StartOffset);
                                Assert.AreEqual(offsetAtt1.EndOffset, offsetAtt2.EndOffset);
                            }
                        }
                        Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum1.NextDoc());
                        Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum2.NextDoc());
                    }
                    else
                    {
                        dEnum1 = TestUtil.Docs(Random, termsEnum1, null, dEnum1, DocsFlags.FREQS);
                        dEnum2 = TestUtil.Docs(Random, termsEnum2, null, dEnum2, DocsFlags.FREQS);
                        Assert.IsNotNull(dEnum1);
                        Assert.IsNotNull(dEnum2);
                        int docID1 = dEnum1.NextDoc();
                        dEnum2.NextDoc();
                        // docIDs are not supposed to be equal
                        //int docID2 = dEnum2.NextDoc();
                        //Assert.AreEqual(docID1, docID2);
                        Assert.IsTrue(docID1 != DocIdSetIterator.NO_MORE_DOCS);
                        int freq1 = dEnum1.Freq;
                        int freq2 = dEnum2.Freq;
                        Assert.AreEqual(freq1, freq2);
                        Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dEnum1.NextDoc());
                        Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dEnum2.NextDoc());
                    }
                }

                Assert.IsFalse(termsEnum2.MoveNext());
            }
            Assert.IsFalse(fieldsEnum2.MoveNext());
        }

        internal class IndexingThread : ThreadJob
        {
            private readonly TestStressIndexing2 outerInstance;

            public IndexingThread(TestStressIndexing2 outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            internal IndexWriter w;
            internal int @base;
            internal int range;
            internal int iterations;
            internal IDictionary<string, Document> docs = new Dictionary<string, Document>();
            internal Random r;

            public virtual int NextInt(int lim)
            {
                return r.Next(lim);
            }

            // start is inclusive and end is exclusive
            public virtual int NextInt(int start, int end)
            {
                return start + r.Next(end - start);
            }

            internal char[] buffer = new char[100];

            internal virtual int AddUTF8Token(int start)
            {
                int end = start + NextInt(20);
                if (buffer.Length < 1 + end)
                {
                    char[] newBuffer = new char[(int)((1 + end) * 1.25)];
                    Arrays.Copy(buffer, 0, newBuffer, 0, buffer.Length);
                    buffer = newBuffer;
                }

                for (int i = start; i < end; i++)
                {
                    int t = NextInt(5);
                    if (0 == t && i < end - 1)
                    {
                        // Make a surrogate pair
                        // High surrogate
                        buffer[i++] = (char)NextInt(0xd800, 0xdc00);
                        // Low surrogate
                        buffer[i] = (char)NextInt(0xdc00, 0xe000);
                    }
                    else if (t <= 1)
                    {
                        buffer[i] = (char)NextInt(0x80);
                    }
                    else if (2 == t)
                    {
                        buffer[i] = (char)NextInt(0x80, 0x800);
                    }
                    else if (3 == t)
                    {
                        buffer[i] = (char)NextInt(0x800, 0xd800);
                    }
                    else if (4 == t)
                    {
                        buffer[i] = (char)NextInt(0xe000, 0xffff);
                    }
                }
                buffer[end] = ' ';
                return 1 + end;
            }

            public virtual string GetString(int nTokens)
            {
                nTokens = nTokens != 0 ? nTokens : r.Next(4) + 1;

                // Half the time make a random UTF8 string
                if (r.NextBoolean())
                {
                    return GetUTF8String(nTokens);
                }

                // avoid StringBuffer because it adds extra synchronization.
                char[] arr = new char[nTokens * 2];
                for (int i = 0; i < nTokens; i++)
                {
                    arr[i * 2] = (char)('A' + r.Next(10));
                    arr[i * 2 + 1] = ' ';
                }
                return new string(arr);
            }

            public virtual string GetUTF8String(int nTokens)
            {
                int upto = 0;
                Arrays.Fill(buffer, (char)0);
                for (int i = 0; i < nTokens; i++)
                {
                    upto = AddUTF8Token(upto);
                }
                return new string(buffer, 0, upto);
            }

            public virtual string IdString => Convert.ToString(@base + NextInt(range), CultureInfo.InvariantCulture);

            public virtual void IndexDoc()
            {
                Document d = new Document();

                FieldType customType1 = new FieldType(TextField.TYPE_STORED);
                customType1.IsTokenized = false;
                customType1.OmitNorms = true;

                IList<Field> fields = new JCG.List<Field>();
                string idString = IdString;
                Field idField = NewField("id", idString, customType1);
                fields.Add(idField);

                int nFields = NextInt(maxFields);
                for (int i = 0; i < nFields; i++)
                {
                    FieldType customType = new FieldType();
                    switch (NextInt(4))
                    {
                        case 0:
                            break;

                        case 1:
                            customType.StoreTermVectors = true;
                            break;

                        case 2:
                            customType.StoreTermVectors = true;
                            customType.StoreTermVectorPositions = true;
                            break;

                        case 3:
                            customType.StoreTermVectors = true;
                            customType.StoreTermVectorOffsets = true;
                            break;
                    }

                    switch (NextInt(4))
                    {
                        case 0:
                            customType.IsStored = true;
                            customType.OmitNorms = true;
                            customType.IsIndexed = true;
                            fields.Add(NewField("f" + NextInt(100), GetString(1), customType));
                            break;

                        case 1:
                            customType.IsIndexed = true;
                            customType.IsTokenized = true;
                            fields.Add(NewField("f" + NextInt(100), GetString(0), customType));
                            break;

                        case 2:
                            customType.IsStored = true;
                            customType.StoreTermVectors = false;
                            customType.StoreTermVectorOffsets = false;
                            customType.StoreTermVectorPositions = false;
                            fields.Add(NewField("f" + NextInt(100), GetString(0), customType));
                            break;

                        case 3:
                            customType.IsStored = true;
                            customType.IsIndexed = true;
                            customType.IsTokenized = true;
                            fields.Add(NewField("f" + NextInt(100), GetString(bigFieldSize), customType));
                            break;
                    }
                }

                if (sameFieldOrder)
                {
                    fields.Sort(fieldNameComparer);
                }
                else
                {
                    // random placement of id field also
                    fields.Swap(NextInt(fields.Count), 0);
                }

                for (int i = 0; i < fields.Count; i++)
                {
                    d.Add(fields[i]);
                }
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": indexing id:" + idString);
                }
                w.UpdateDocument(new Term("id", idString), d);
                //System.out.println(Thread.currentThread().getName() + ": indexing "+d);
                docs[idString] = d;
            }

            public virtual void DeleteDoc()
            {
                string idString = IdString;
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": del id:" + idString);
                }
                w.DeleteDocuments(new Term("id", idString));
                docs.Remove(idString);
            }

            public virtual void DeleteByQuery()
            {
                string idString = IdString;
                if (Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": del query id:" + idString);
                }
                w.DeleteDocuments(new TermQuery(new Term("id", idString)));
                docs.Remove(idString);
            }

            public override void Run()
            {
                try
                {
                    r = new J2N.Randomizer(@base + range + seed);
                    for (int i = 0; i < iterations; i++)
                    {
                        int what = NextInt(100);
                        if (what < 5)
                        {
                            DeleteDoc();
                        }
                        else if (what < 10)
                        {
                            DeleteByQuery();
                        }
                        else
                        {
                            IndexDoc();
                        }
                    }
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    Console.WriteLine(e.ToString());
                    Console.Write(e.StackTrace);
                    Assert.Fail(e.ToString());
                }

                UninterruptableMonitor.Enter(this);
                try
                {
                    int dummy = docs.Count;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }
    }
}