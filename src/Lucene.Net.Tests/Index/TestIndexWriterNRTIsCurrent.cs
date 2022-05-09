using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.IO;
using System.Threading;
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TextField = TextField;

    [TestFixture]
    public class TestIndexWriterNRTIsCurrent : LuceneTestCase
    {
        public class ReaderHolder
        {
            internal volatile DirectoryReader reader;
            internal volatile bool stop = false;
        }

        [Test]
        public virtual void TestIsCurrentWithThreads()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(dir, conf);
            ReaderHolder holder = new ReaderHolder();
            ReaderThread[] threads = new ReaderThread[AtLeast(3)];
            CountdownEvent latch = new CountdownEvent(1);
            WriterThread writerThread = new WriterThread(holder, writer, AtLeast(500), Random, latch);
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ReaderThread(holder, latch);
                threads[i].Start();
            }
            writerThread.Start();

            writerThread.Join();
            bool failed = writerThread.failed != null;
            if (failed)
            {
                Console.WriteLine(writerThread.failed.ToString());
                Console.Write(writerThread.failed.StackTrace);
            }
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
                if (threads[i].failed != null)
                {
                    Console.WriteLine(threads[i].failed.ToString());
                    Console.Write(threads[i].failed.StackTrace);
                    failed = true;
                }
            }
            Assert.IsFalse(failed);
            writer.Dispose();
            dir.Dispose();
        }

        public class WriterThread : ThreadJob
        {
            internal readonly ReaderHolder holder;
            internal readonly IndexWriter writer;
            internal readonly int numOps;
            internal bool countdown = true;
            internal readonly CountdownEvent latch;
            internal Exception failed;

            internal WriterThread(ReaderHolder holder, IndexWriter writer, int numOps, Random random, CountdownEvent latch)
                : base()
            {
                this.holder = holder;
                this.writer = writer;
                this.numOps = numOps;
                this.latch = latch;
            }

            public override void Run()
            {
                DirectoryReader currentReader = null;
                Random random = LuceneTestCase.Random;
                try
                {
                    Document doc = new Document();
                    doc.Add(new TextField("id", "1", Field.Store.NO));
                    writer.AddDocument(doc);
                    holder.reader = currentReader = writer.GetReader(true);
                    Term term = new Term("id");
                    for (int i = 0; i < numOps && !holder.stop; i++)
                    {
                        float nextOp = (float)random.NextDouble();
                        if (nextOp < 0.3)
                        {
                            term.Set("id", new BytesRef("1"));
                            writer.UpdateDocument(term, doc);
                        }
                        else if (nextOp < 0.5)
                        {
                            writer.AddDocument(doc);
                        }
                        else
                        {
                            term.Set("id", new BytesRef("1"));
                            writer.DeleteDocuments(term);
                        }
                        if (holder.reader != currentReader)
                        {
                            holder.reader = currentReader;
                            if (countdown)
                            {
                                countdown = false;
                                latch.Signal();
                            }
                        }
                        if (random.NextBoolean())
                        {
                            writer.Commit();
                            DirectoryReader newReader = DirectoryReader.OpenIfChanged(currentReader);
                            if (newReader != null)
                            {
                                currentReader.DecRef();
                                currentReader = newReader;
                            }
                            if (currentReader.NumDocs == 0)
                            {
                                writer.AddDocument(doc);
                            }
                        }
                    }
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    failed = e;
                }
                finally
                {
                    holder.reader = null;
                    if (countdown)
                    {
                        latch.Signal();
                    }
                    if (currentReader != null)
                    {
                        try
                        {
                            currentReader.DecRef();
                        }
                        catch (Exception e) when (e.IsIOException())
                        {
                        }
                    }
                }
                if (Verbose)
                {
                    Console.WriteLine("writer stopped - forced by reader: " + holder.stop);
                }
            }
        }

        public sealed class ReaderThread : ThreadJob
        {
            internal readonly ReaderHolder holder;
            internal readonly CountdownEvent latch;
            internal Exception failed;

            internal ReaderThread(ReaderHolder holder, CountdownEvent latch)
                : base()
            {
                this.holder = holder;
                this.latch = latch;
            }

            public override void Run()
            {
                try
                {
                    latch.Wait();
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                    failed = e;
                    return;
                }
                DirectoryReader reader;
                while ((reader = holder.reader) != null)
                {
                    if (reader.TryIncRef())
                    {
                        try
                        {
                            bool current = reader.IsCurrent();
                            if (Verbose)
                            {
                                Console.WriteLine("Thread: " + Thread.CurrentThread + " Reader: " + reader + " isCurrent:" + current);
                            }

                            Assert.IsFalse(current);
                        }
                        catch (Exception e) when (e.IsThrowable())
                        {
                            if (Verbose)
                            {
                                Console.WriteLine("FAILED Thread: " + Thread.CurrentThread + " Reader: " + reader + " isCurrent: false");
                            }
                            failed = e;
                            holder.stop = true;
                            return;
                        }
                        finally
                        {
                            try
                            {
                                reader.DecRef();
                            }
                            catch (Exception e) when (e.IsIOException())
                            {
                                if (failed is null)
                                {
                                    failed = e;
                                }
                            }
                        }
                        return;
                    }
                }
            }
        }
    }
}