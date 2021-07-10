using Lucene.Net.Diagnostics;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;

    // TODO
    //   - mix in forceMerge, addIndexes
    //   - randomoly mix in non-congruent docs
    [SuppressCodecs("SimpleText", "Memory", "Direct")]
    [TestFixture]
    public class TestNRTThreads : ThreadedIndexingAndSearchingTestCase
    {
        private bool useNonNrtReaders = true;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            useNonNrtReaders = Random.NextBoolean();
        }

        protected override void DoSearching(TaskScheduler es, long stopTime)
        {
            bool anyOpenDelFiles = false;

            DirectoryReader r = DirectoryReader.Open(m_writer, true);

            while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < stopTime && !m_failed) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            {
                if (Random.NextBoolean())
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: now reopen r=" + r);
                    }
                    DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
                    if (r2 != null)
                    {
                        r.Dispose();
                        r = r2;
                    }
                }
                else
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: now close reader=" + r);
                    }
                    r.Dispose();
                    m_writer.Commit();
                    ICollection<string> openDeletedFiles = ((MockDirectoryWrapper)m_dir).GetOpenDeletedFiles();
                    if (openDeletedFiles.Count > 0)
                    {
                        Console.WriteLine("OBD files: " + openDeletedFiles);
                    }
                    anyOpenDelFiles |= openDeletedFiles.Count > 0;
                    //Assert.AreEqual("open but deleted: " + openDeletedFiles, 0, openDeletedFiles.Size());
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: now open");
                    }
                    r = DirectoryReader.Open(m_writer, true);
                }
                if (Verbose)
                {
                    Console.WriteLine("TEST: got new reader=" + r);
                }
                //System.out.println("numDocs=" + r.NumDocs + "
                //openDelFileCount=" + dir.openDeleteFileCount());

                if (r.NumDocs > 0)
                {
                    fixedSearcher = new IndexSearcher(r, es);
                    SmokeTestSearcher(fixedSearcher);
                    RunSearchThreads((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + 500); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                }
            }
            r.Dispose();

            //System.out.println("numDocs=" + r.NumDocs + " openDelFileCount=" + dir.openDeleteFileCount());
            ICollection<string> openDeletedFiles_ = ((MockDirectoryWrapper)m_dir).GetOpenDeletedFiles();
            if (openDeletedFiles_.Count > 0)
            {
                Console.WriteLine("OBD files: " + openDeletedFiles_);
            }
            anyOpenDelFiles |= openDeletedFiles_.Count > 0;

            Assert.IsFalse(anyOpenDelFiles, "saw non-zero open-but-deleted count");
        }

        protected override Directory GetDirectory(Directory @in)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(@in is MockDirectoryWrapper);
            if (!useNonNrtReaders)
            {
                ((MockDirectoryWrapper)@in).AssertNoDeleteOpenFile = true;
            }
            return @in;
        }

        protected override void DoAfterWriter(TaskScheduler es)
        {
            // Force writer to do reader pooling, always, so that
            // all merged segments, even for merges before
            // doSearching is called, are warmed:
            m_writer.GetReader().Dispose();
        }

        private IndexSearcher fixedSearcher;

        protected override IndexSearcher GetCurrentSearcher()
        {
            return fixedSearcher;
        }

        protected override void ReleaseSearcher(IndexSearcher s)
        {
            if (s != fixedSearcher)
            {
                // Final searcher:
                s.IndexReader.Dispose();
            }
        }

        protected override IndexSearcher GetFinalSearcher()
        {
            IndexReader r2;
            if (useNonNrtReaders)
            {
                if (Random.NextBoolean())
                {
                    r2 = m_writer.GetReader();
                }
                else
                {
                    m_writer.Commit();
                    r2 = DirectoryReader.Open(m_dir);
                }
            }
            else
            {
                r2 = m_writer.GetReader();
            }
            return NewSearcher(r2);
        }

        [Test]
        [Slow] // (occasionally)
        public virtual void TestNRTThreads_Mem()
        {
            RunTest("TestNRTThreads");
        }
    }
}