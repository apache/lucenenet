using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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

    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using TestUtil = Lucene.Net.Util.TestUtil;

    // Make sure if you use NoDeletionPolicy that no file
    // referenced by a commit point is ever deleted

    [TestFixture]
    public class TestNeverDelete : LuceneTestCase
    {
        [Test]
        public virtual void TestIndexing()
        {
            DirectoryInfo tmpDir = CreateTempDir("TestNeverDelete");
            BaseDirectoryWrapper d = NewFSDirectory(tmpDir);

            // We want to "see" files removed if Lucene removed
            // them.  this is still worth running on Windows since
            // some files the IR opens and closes.
            if (d is MockDirectoryWrapper mockDirectoryWrapper)
            {
                mockDirectoryWrapper.NoDeleteOpenFile = false;
            }
            RandomIndexWriter w = new RandomIndexWriter(Random, d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE));
            w.IndexWriter.Config.SetMaxBufferedDocs(TestUtil.NextInt32(Random, 5, 30));

            w.Commit();
            ThreadJob[] indexThreads = new ThreadJob[Random.Next(4)];
            long stopTime = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + AtLeast(1000); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            for (int x = 0; x < indexThreads.Length; x++)
            {
                indexThreads[x] = new ThreadAnonymousClass(w, stopTime);
                indexThreads[x].Name = "Thread " + x;
                indexThreads[x].Start();
            }

            ISet<string> allFiles = new JCG.HashSet<string>();

            DirectoryReader r = DirectoryReader.Open(d);
            while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < stopTime) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            {
                IndexCommit ic = r.IndexCommit;
                if (Verbose)
                {
                    Console.WriteLine("TEST: check files: " + ic.FileNames);
                }
                allFiles.UnionWith(ic.FileNames);
                // Make sure no old files were removed
                foreach (string fileName in allFiles)
                {
                    Assert.IsTrue(SlowFileExists(d, fileName), "file " + fileName + " does not exist");
                }
                DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
                if (r2 != null)
                {
                    r.Dispose();
                    r = r2;
                }
                Thread.Sleep(1);
            }
            r.Dispose();

            foreach (ThreadJob t in indexThreads)
            {
                t.Join();
            }
            w.Dispose();
            d.Dispose();

            Directory.Delete(tmpDir.FullName, true);
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly RandomIndexWriter w;
            private readonly long stopTime;

            public ThreadAnonymousClass(RandomIndexWriter w, long stopTime)
            {
                this.w = w;
                this.stopTime = stopTime;
            }

            public override void Run()
            {
                try
                {
                    int docCount = 0;
                    while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < stopTime) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                    {
                        Document doc = new Document();
                        doc.Add(NewStringField("dc", "" + docCount, Field.Store.YES));
                        doc.Add(NewTextField("field", "here is some text", Field.Store.YES));
                        w.AddDocument(doc);

                        if (docCount % 13 == 0)
                        {
                            w.Commit();
                        }
                        docCount++;
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
