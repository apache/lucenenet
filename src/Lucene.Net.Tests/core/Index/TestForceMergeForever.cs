using System;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;
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
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestForceMergeForever : LuceneTestCase
    {
        // Just counts how many merges are done
        private class MyIndexWriter : IndexWriter
        {
            internal AtomicInteger MergeCount = new AtomicInteger();
            internal bool First;

            public MyIndexWriter(Directory dir, IndexWriterConfig conf)
                : base(dir, conf)
            {
            }

            public override void Merge(MergePolicy.OneMerge merge)
            {
                if (merge.maxNumSegments != -1 && (First || merge.Segments.Count == 1))
                {
                    First = false;
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: maxNumSegments merge");
                    }
                    MergeCount.IncrementAndGet();
                }
                base.Merge(merge);
            }
        }

        [Test]
        public virtual void Test()
        {
            Directory d = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);

            MyIndexWriter w = new MyIndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));

            // Try to make an index that requires merging:
            w.Config.SetMaxBufferedDocs(TestUtil.NextInt(Random(), 2, 11));
            int numStartDocs = AtLeast(20);
            LineFileDocs docs = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
            for (int docIDX = 0; docIDX < numStartDocs; docIDX++)
            {
                w.AddDocument(docs.NextDoc());
            }
            MergePolicy mp = w.Config.MergePolicy;
            int mergeAtOnce = 1 + w.segmentInfos.Size();
            if (mp is TieredMergePolicy)
            {
                ((TieredMergePolicy)mp).MaxMergeAtOnce = mergeAtOnce;
            }
            else if (mp is LogMergePolicy)
            {
                ((LogMergePolicy)mp).MergeFactor = mergeAtOnce;
            }
            else
            {
                // skip test
                w.Dispose();
                d.Dispose();
                return;
            }

            AtomicBoolean doStop = new AtomicBoolean();
            w.Config.SetMaxBufferedDocs(2);
            ThreadClass t = new ThreadAnonymousInnerClassHelper(this, w, numStartDocs, docs, doStop);
            t.Start();
            w.ForceMerge(1);
            doStop.Set(true);
            t.Join();
            Assert.IsTrue(w.MergeCount.Get() <= 1, "merge count is " + w.MergeCount.Get());
            w.Dispose();
            d.Dispose();
            docs.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestForceMergeForever OuterInstance;

            private Lucene.Net.Index.TestForceMergeForever.MyIndexWriter w;
            private int NumStartDocs;
            private LineFileDocs Docs;
            private AtomicBoolean DoStop;

            public ThreadAnonymousInnerClassHelper(TestForceMergeForever outerInstance, Lucene.Net.Index.TestForceMergeForever.MyIndexWriter w, int numStartDocs, LineFileDocs docs, AtomicBoolean doStop)
            {
                this.OuterInstance = outerInstance;
                this.w = w;
                this.NumStartDocs = numStartDocs;
                this.Docs = docs;
                this.DoStop = doStop;
            }

            public override void Run()
            {
                try
                {
                    while (!DoStop.Get())
                    {
                        w.UpdateDocument(new Term("docid", "" + Random().Next(NumStartDocs)), Docs.NextDoc());
                        // Force deletes to apply
                        w.Reader.Dispose();
                    }
                }
                catch (Exception t)
                {
                    throw new Exception(t.Message, t);
                }
            }
        }
    }
}