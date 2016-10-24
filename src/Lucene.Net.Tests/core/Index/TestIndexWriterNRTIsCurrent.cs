using System;
using System.Threading;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System.IO;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    /*
         * Licensed to the Apache Software Foundation (ASF) under one or more
         * contributor license agreements. See the NOTICE file distributed with this
         * work for additional information regarding copyright ownership. The ASF
         * licenses this file to You under the Apache License, Version 2.0 (the
         * "License"); you may not use this file except in compliance with the License.
         * You may obtain a copy of the License at
         *
         * http://www.apache.org/licenses/LICENSE-2.0
         *
         * Unless required by applicable law or agreed to in writing, software
         * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
         * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
         * License for the specific language governing permissions and limitations under
         * the License.
         */

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TextField = TextField;

    [TestFixture]
    public class TestIndexWriterNRTIsCurrent : LuceneTestCase
    {
        public class ReaderHolder
        {
            internal volatile DirectoryReader Reader;
            internal volatile bool Stop = false;
        }

        [Test]
        public virtual void TestIsCurrentWithThreads()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            IndexWriter writer = new IndexWriter(dir, conf);
            ReaderHolder holder = new ReaderHolder();
            ReaderThread[] threads = new ReaderThread[AtLeast(3)];
            CountdownEvent latch = new CountdownEvent(1);
            WriterThread writerThread = new WriterThread(holder, writer, AtLeast(500), Random(), latch);
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ReaderThread(holder, latch);
                threads[i].Start();
            }
            writerThread.Start();

            writerThread.Join();
            bool failed = writerThread.Failed != null;
            if (failed)
            {
                Console.WriteLine(writerThread.Failed.ToString());
                Console.Write(writerThread.Failed.StackTrace);
            }
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
                if (threads[i].Failed != null)
                {
                    Console.WriteLine(threads[i].Failed.ToString());
                    Console.Write(threads[i].Failed.StackTrace);
                    failed = true;
                }
            }
            Assert.IsFalse(failed);
            writer.Dispose();
            dir.Dispose();
        }

        public class WriterThread : ThreadClass
        {
            internal readonly ReaderHolder Holder;
            internal readonly IndexWriter Writer;
            internal readonly int NumOps;
            internal bool Countdown = true;
            internal readonly CountdownEvent Latch;
            internal Exception Failed;

            internal WriterThread(ReaderHolder holder, IndexWriter writer, int numOps, Random random, CountdownEvent latch)
                : base()
            {
                this.Holder = holder;
                this.Writer = writer;
                this.NumOps = numOps;
                this.Latch = latch;
            }

            public override void Run()
            {
                DirectoryReader currentReader = null;
                Random random = LuceneTestCase.Random();
                try
                {
                    Document doc = new Document();
                    doc.Add(new TextField("id", "1", Field.Store.NO));
                    Writer.AddDocument(doc);
                    Holder.Reader = currentReader = Writer.GetReader(true);
                    Term term = new Term("id");
                    for (int i = 0; i < NumOps && !Holder.Stop; i++)
                    {
                        float nextOp = (float)random.NextDouble();
                        if (nextOp < 0.3)
                        {
                            term.Set("id", new BytesRef("1"));
                            Writer.UpdateDocument(term, doc);
                        }
                        else if (nextOp < 0.5)
                        {
                            Writer.AddDocument(doc);
                        }
                        else
                        {
                            term.Set("id", new BytesRef("1"));
                            Writer.DeleteDocuments(term);
                        }
                        if (Holder.Reader != currentReader)
                        {
                            Holder.Reader = currentReader;
                            if (Countdown)
                            {
                                Countdown = false;
                                Latch.Signal();
                            }
                        }
                        if (random.NextBoolean())
                        {
                            Writer.Commit();
                            DirectoryReader newReader = DirectoryReader.OpenIfChanged(currentReader);
                            if (newReader != null)
                            {
                                currentReader.DecRef();
                                currentReader = newReader;
                            }
                            if (currentReader.NumDocs == 0)
                            {
                                Writer.AddDocument(doc);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Failed = e;
                }
                finally
                {
                    Holder.Reader = null;
                    if (Countdown)
                    {
                        Latch.Signal();
                    }
                    if (currentReader != null)
                    {
                        try
                        {
                            currentReader.DecRef();
                        }
                        catch (IOException e)
                        {
                        }
                    }
                }
                if (VERBOSE)
                {
                    Console.WriteLine("writer stopped - forced by reader: " + Holder.Stop);
                }
            }
        }

        public sealed class ReaderThread : ThreadClass
        {
            internal readonly ReaderHolder Holder;
            internal readonly CountdownEvent Latch;
            internal Exception Failed;

            internal ReaderThread(ReaderHolder holder, CountdownEvent latch)
                : base()
            {
                this.Holder = holder;
                this.Latch = latch;
            }

            public override void Run()
            {
#if !NETSTANDARD
                try
                {
#endif
                    Latch.Wait();
#if !NETSTANDARD
                }
                catch (ThreadInterruptedException e)
                {
                    Failed = e;
                    return;
                }
#endif
                DirectoryReader reader;
                while ((reader = Holder.Reader) != null)
                {
                    if (reader.TryIncRef())
                    {
                        try
                        {
                            bool current = reader.Current;
                            if (VERBOSE)
                            {
                                Console.WriteLine("Thread: " + Thread.CurrentThread + " Reader: " + reader + " isCurrent:" + current);
                            }

                            Assert.IsFalse(current);
                        }
                        catch (Exception e)
                        {
                            if (VERBOSE)
                            {
                                Console.WriteLine("FAILED Thread: " + Thread.CurrentThread + " Reader: " + reader + " isCurrent: false");
                            }
                            Failed = e;
                            Holder.Stop = true;
                            return;
                        }
                        finally
                        {
                            try
                            {
                                reader.DecRef();
                            }
                            catch (IOException e)
                            {
                                if (Failed == null)
                                {
                                    Failed = e;
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