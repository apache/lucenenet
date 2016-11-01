using System;
using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using System.IO;
    using Directory = Lucene.Net.Store.Directory;
    using IOContext = Lucene.Net.Store.IOContext;
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
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using PrintStreamInfoStream = Lucene.Net.Util.PrintStreamInfoStream;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestIndexWriterOutOfFileDescriptors : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            MockDirectoryWrapper dir = NewMockFSDirectory(CreateTempDir("TestIndexWriterOutOfFileDescriptors"));
            dir.PreventDoubleWrite = false;
            double rate = Random().NextDouble() * 0.01;
            //System.out.println("rate=" + rate);
            dir.RandomIOExceptionRateOnOpen = rate;
            int iters = AtLeast(20);
            LineFileDocs docs = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
            IndexReader r = null;
            DirectoryReader r2 = null;
            bool any = false;
            MockDirectoryWrapper dirCopy = null;
            int lastNumDocs = 0;
            for (int iter = 0; iter < iters; iter++)
            {
                IndexWriter w = null;
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: iter=" + iter);
                }
                try
                {
                    MockAnalyzer analyzer = new MockAnalyzer(Random());
                    analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);
                    IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);

                    if (VERBOSE)
                    {
                        // Do this ourselves instead of relying on LTC so
                        // we see incrementing messageID:
                        iwc.InfoStream = new PrintStreamInfoStream(Console.Out);
                    }
                    var ms = iwc.MergeScheduler;
                    if (ms is IConcurrentMergeScheduler)
                    {
                        ((IConcurrentMergeScheduler)ms).SetSuppressExceptions();
                    }
                    w = new IndexWriter(dir, iwc);
                    if (r != null && Random().Next(5) == 3)
                    {
                        if (Random().NextBoolean())
                        {
                            if (VERBOSE)
                            {
                                Console.WriteLine("TEST: addIndexes IR[]");
                            }
                            w.AddIndexes(new IndexReader[] { r });
                        }
                        else
                        {
                            if (VERBOSE)
                            {
                                Console.WriteLine("TEST: addIndexes Directory[]");
                            }
                            w.AddIndexes(new Directory[] { dirCopy });
                        }
                    }
                    else
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: addDocument");
                        }
                        w.AddDocument(docs.NextDoc());
                    }
                    dir.RandomIOExceptionRateOnOpen = 0.0;
                    w.Dispose();
                    w = null;

                    // NOTE: this is O(N^2)!  Only enable for temporary debugging:
                    //dir.setRandomIOExceptionRateOnOpen(0.0);
                    //TestUtil.CheckIndex(dir);
                    //dir.setRandomIOExceptionRateOnOpen(rate);

                    // Verify numDocs only increases, to catch IndexWriter
                    // accidentally deleting the index:
                    dir.RandomIOExceptionRateOnOpen = 0.0;
                    Assert.IsTrue(DirectoryReader.IndexExists(dir));
                    if (r2 == null)
                    {
                        r2 = DirectoryReader.Open(dir);
                    }
                    else
                    {
                        DirectoryReader r3 = DirectoryReader.OpenIfChanged(r2);
                        if (r3 != null)
                        {
                            r2.Dispose();
                            r2 = r3;
                        }
                    }
                    Assert.IsTrue(r2.NumDocs >= lastNumDocs, "before=" + lastNumDocs + " after=" + r2.NumDocs);
                    lastNumDocs = r2.NumDocs;
                    //System.out.println("numDocs=" + lastNumDocs);
                    dir.RandomIOExceptionRateOnOpen = rate;

                    any = true;
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: iter=" + iter + ": success");
                    }
                }
                catch (IOException ioe)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: iter=" + iter + ": exception");
                        Console.WriteLine(ioe.ToString());
                        Console.Write(ioe.StackTrace);
                    }
                    if (w != null)
                    {
                        // NOTE: leave random IO exceptions enabled here,
                        // to verify that rollback does not try to write
                        // anything:
                        w.Rollback();
                    }
                }

                if (any && r == null && Random().NextBoolean())
                {
                    // Make a copy of a non-empty index so we can use
                    // it to addIndexes later:
                    dir.RandomIOExceptionRateOnOpen = 0.0;
                    r = DirectoryReader.Open(dir);
                    dirCopy = NewMockFSDirectory(CreateTempDir("TestIndexWriterOutOfFileDescriptors.copy"));
                    HashSet<string> files = new HashSet<string>();
                    foreach (string file in dir.ListAll())
                    {
                        dir.Copy(dirCopy, file, file, IOContext.DEFAULT);
                        files.Add(file);
                    }
                    dirCopy.Sync(files);
                    // Have IW kiss the dir so we remove any leftover
                    // files ... we can easily have leftover files at
                    // the time we take a copy because we are holding
                    // open a reader:
                    (new IndexWriter(dirCopy, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())))).Dispose();
                    dirCopy.RandomIOExceptionRate = rate;
                    dir.RandomIOExceptionRateOnOpen = rate;
                }
            }

            if (r2 != null)
            {
                r2.Dispose();
            }
            if (r != null)
            {
                r.Dispose();
                dirCopy.Dispose();
            }
            dir.Dispose();
        }
    }
}