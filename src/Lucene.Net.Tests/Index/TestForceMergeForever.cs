using J2N.Threading;
using J2N.Threading.Atomic;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
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
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestForceMergeForever : LuceneTestCase
    {
        // Just counts how many merges are done
        private class MyIndexWriter : IndexWriter
        {
            internal AtomicInt32 mergeCount = new AtomicInt32();
            internal bool first;

            public MyIndexWriter(Directory dir, IndexWriterConfig conf)
                : base(dir, conf)
            {
            }

            public override void Merge(MergePolicy.OneMerge merge)
            {
                if (merge.MaxNumSegments != -1 && (first || merge.Segments.Count == 1))
                {
                    first = false;
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: maxNumSegments merge");
                    }
                    mergeCount.IncrementAndGet();
                }
                base.Merge(merge);
            }
        }

        [Test]
        public virtual void Test()
        {
            Directory d = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random);
            analyzer.MaxTokenLength = TestUtil.NextInt32(Random, 1, IndexWriter.MAX_TERM_LENGTH);

            MyIndexWriter w = new MyIndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));

            // Try to make an index that requires merging:
            w.Config.SetMaxBufferedDocs(TestUtil.NextInt32(Random, 2, 11));
            int numStartDocs = AtLeast(20);
            LineFileDocs docs = new LineFileDocs(Random, DefaultCodecSupportsDocValues);
            for (int docIDX = 0; docIDX < numStartDocs; docIDX++)
            {
                w.AddDocument(docs.NextDoc());
            }
            MergePolicy mp = w.Config.MergePolicy;
            int mergeAtOnce = 1 + w.segmentInfos.Count;
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
            ThreadJob t = new ThreadAnonymousClass(this, w, numStartDocs, docs, doStop);
            t.Start();
            w.ForceMerge(1);
            doStop.Value = true;
            t.Join();
            Assert.IsTrue(w.mergeCount <= 1, "merge count is " + w.mergeCount);
            w.Dispose();
            d.Dispose();
            docs.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestForceMergeForever outerInstance;

            private readonly MyIndexWriter w;
            private readonly int numStartDocs;
            private readonly LineFileDocs docs;
            private readonly AtomicBoolean doStop;

            public ThreadAnonymousClass(TestForceMergeForever outerInstance, Lucene.Net.Index.TestForceMergeForever.MyIndexWriter w, int numStartDocs, LineFileDocs docs, AtomicBoolean doStop)
            {
                this.outerInstance = outerInstance;
                this.w = w;
                this.numStartDocs = numStartDocs;
                this.docs = docs;
                this.doStop = doStop;
            }

            public override void Run()
            {
                try
                {
                    while (!doStop)
                    {
                        w.UpdateDocument(new Term("docid", "" + Random.Next(numStartDocs)), docs.NextDoc());
                        // Force deletes to apply
                        w.GetReader().Dispose();
                    }
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    throw RuntimeException.Create(t);
                }
            }
        }
    }
}