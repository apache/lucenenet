/**
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

using NUnit.Framework;

using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using _TestUtil = Lucene.Net.Util._TestUtil;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using Directory = Lucene.Net.Store.Directory;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using CloseableThreadLocal = Lucene.Net.Util.CloseableThreadLocal;

namespace Lucene.Net.Index
{
    [TestFixture]
    public class TestIndexWriterExceptions : LuceneTestCase
    {

        private const bool DEBUG = false;

        private class IndexerThread : SupportClass.ThreadClass
        {
            private TestIndexWriterExceptions enclosingInstance;
            internal IndexWriter writer;

            internal readonly System.Random r = new System.Random(47);
            internal System.Exception failure;

            public IndexerThread(int i, IndexWriter writer, TestIndexWriterExceptions enclosingInstance)
                : base("Indexer " + i)
            {
                this.writer = writer;
                this.enclosingInstance = enclosingInstance;
            }

            override public void Run()
            {

                Document doc = new Document();

                doc.Add(new Field("content1", "aaa bbb ccc ddd", Field.Store.YES, Field.Index.ANALYZED));
                doc.Add(new Field("content6", "aaa bbb ccc ddd", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
                doc.Add(new Field("content2", "aaa bbb ccc ddd", Field.Store.YES, Field.Index.NOT_ANALYZED));
                doc.Add(new Field("content3", "aaa bbb ccc ddd", Field.Store.YES, Field.Index.NO));

                doc.Add(new Field("content4", "aaa bbb ccc ddd", Field.Store.NO, Field.Index.ANALYZED));
                doc.Add(new Field("content5", "aaa bbb ccc ddd", Field.Store.NO, Field.Index.NOT_ANALYZED));

                doc.Add(new Field("content7", "aaa bbb ccc ddd", Field.Store.NO, Field.Index.NOT_ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));

                Field idField = new Field("id", "", Field.Store.YES, Field.Index.NOT_ANALYZED);
                doc.Add(idField);

                System.DateTime stopTime = System.DateTime.Now.AddSeconds(3);

                while (System.DateTime.Now < stopTime)
                {
                    enclosingInstance.doFail.Set(this);
                    string id = "" + r.Next(50);
                    idField.SetValue(id);
                    Term idTerm = new Term("id", id);
                    try
                    {
                        writer.UpdateDocument(idTerm, doc);
                    }
                    catch (System.Exception re)
                    {
                        if (DEBUG)
                        {
                            System.Console.Out.WriteLine("EXC: ");
                            System.Console.Out.WriteLine(re.StackTrace);
                        }
                        try
                        {
                            _TestUtil.CheckIndex(writer.GetDirectory());
                        }
                        catch (System.IO.IOException ioe)
                        {
                            System.Console.Out.WriteLine(System.Threading.Thread.CurrentThread.Name + ": unexpected exception1");
                            System.Console.Out.WriteLine(ioe.StackTrace);
                            failure = ioe;
                            break;
                        }
                        // this, in Java, was catch Throwable, and the catch above (at the same nesting level)
                        // was catch RuntimeException... as all exceptions in C# are unchecked, these both come
                        // down to System.Exception
                        /*
                    } catch (System.Exception t) {
                      System.Console.Out.WriteLine(System.Threading.Thread.CurrentThread.Name + ": unexpected exception2");
                      System.Console.Out.WriteLine(t.StackTrace);
                      failure = t;
                      break;
                         */
                    }

                    enclosingInstance.doFail.Set(null);

                    // After a possible exception (above) I should be able
                    // to add a new document without hitting an
                    // exception:
                    try
                    {
                        writer.UpdateDocument(idTerm, doc);
                    }
                    catch (System.Exception t)
                    {
                        System.Console.Out.WriteLine(System.Threading.Thread.CurrentThread.Name + ": unexpected exception3");
                        System.Console.Out.WriteLine(t.StackTrace);
                        failure = t;
                        break;
                    }
                }
            }
        }

        CloseableThreadLocal doFail = new CloseableThreadLocal();

        public class MockIndexWriter : IndexWriter
        {
            private TestIndexWriterExceptions enclosingInstance;

            internal System.Random r = new System.Random(17);

            public MockIndexWriter(Directory dir, Analyzer a, bool create, MaxFieldLength mfl, TestIndexWriterExceptions enclosingInstance)
                : base(dir, a, create, mfl)
            {
                this.enclosingInstance = enclosingInstance;
            }

            protected override bool TestPoint(string name)
            {
                if (enclosingInstance.doFail.Get() != null && !name.Equals("startDoFlush") && r.Next(20) == 17)
                {
                    if (DEBUG)
                    {
                        System.Console.Out.WriteLine(System.Threading.Thread.CurrentThread.Name + ": NOW FAIL: " + name);
                    }
                    throw new System.Exception(System.Threading.Thread.CurrentThread.Name + ": intentionally failing at " + name);
                }
                return true;
            }
        }

        [Test]
        public void TestRandomExceptions()
        {
            MockRAMDirectory dir = new MockRAMDirectory();

            MockIndexWriter writer = new MockIndexWriter(dir, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED, this);
            ((ConcurrentMergeScheduler)writer.GetMergeScheduler()).SetSuppressExceptions_ForNUnitTest();
            //writer.setMaxBufferedDocs(10);
            writer.SetRAMBufferSizeMB(0.1);

            if (DEBUG)
                writer.SetInfoStream(System.Console.Out);

            IndexerThread thread = new IndexerThread(0, writer, this);
            thread.Run();
            if (thread.failure != null)
            {
                System.Console.Out.WriteLine(thread.failure.StackTrace);
                Assert.Fail("thread " + thread.Name + ": hit unexpected failure");
            }

            writer.Commit();

            try
            {
                writer.Close();
            }
            catch (System.Exception t)
            {
                System.Console.Out.WriteLine("exception during close:");
                System.Console.Out.WriteLine(t.StackTrace);
                writer.Rollback();
            }

            // Confirm that when doc hits exception partway through tokenization, it's deleted:
            IndexReader r2 = IndexReader.Open(dir);
            int count = r2.DocFreq(new Term("content4", "aaa"));
            int count2 = r2.DocFreq(new Term("content4", "ddd"));
            Assert.AreEqual(count, count2);
            r2.Close();

            _TestUtil.CheckIndex(dir);
        }

        [Test]
        public void TestRandomExceptionsThreads()
        {

            MockRAMDirectory dir = new MockRAMDirectory();
            MockIndexWriter writer = new MockIndexWriter(dir, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED, this);
            ((ConcurrentMergeScheduler)writer.GetMergeScheduler()).SetSuppressExceptions_ForNUnitTest();
            //writer.setMaxBufferedDocs(10);
            writer.SetRAMBufferSizeMB(0.2);

            if (DEBUG)
                writer.SetInfoStream(System.Console.Out);

            int NUM_THREADS = 4;

            IndexerThread[] threads = new IndexerThread[NUM_THREADS];
            for (int i = 0; i < NUM_THREADS; i++)
            {
                threads[i] = new IndexerThread(i, writer, this);
                threads[i].Start();
            }

            for (int i = 0; i < NUM_THREADS; i++)
                threads[i].Join();

            for (int i = 0; i < NUM_THREADS; i++)
                if (threads[i].failure != null)
                    Assert.Fail("thread " + threads[i].Name + ": hit unexpected failure");

            writer.Commit();

            try
            {
                writer.Close();
            }
            catch (System.Exception t)
            {
                System.Console.Out.WriteLine("exception during close:");
                System.Console.Out.WriteLine(t.StackTrace);
                writer.Rollback();
            }

            // Confirm that when doc hits exception partway through tokenization, it's deleted:
            IndexReader r2 = IndexReader.Open(dir);
            int count = r2.DocFreq(new Term("content4", "aaa"));
            int count2 = r2.DocFreq(new Term("content4", "ddd"));
            Assert.AreEqual(count, count2);
            r2.Close();

            _TestUtil.CheckIndex(dir);
        }
    }
}
