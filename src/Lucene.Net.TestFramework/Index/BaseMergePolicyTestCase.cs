using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using System;
using NUnit.Framework;

namespace Lucene.Net.Index
{
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
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

    /// <summary>
    /// Base test case for <see cref="Index.MergePolicy"/>.
    /// </summary>
    public abstract class BaseMergePolicyTestCase : LuceneTestCase
    {
        /// <summary>
        /// Create a new <see cref="Index.MergePolicy"/> instance. </summary>
        protected abstract MergePolicy NewMergePolicy(); // LUCENENET specific - renamed from MergePolicy() to be consistent

        [Test]
        public virtual void TestForceMergeNotNeeded()
        {
            using (Directory dir = NewDirectory())
            {
                AtomicBoolean mayMerge = new AtomicBoolean(true);
                MergeScheduler mergeScheduler = new SerialMergeSchedulerAnonymousInnerClassHelper(this, mayMerge);
                using (IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(mergeScheduler).SetMergePolicy(NewMergePolicy())))
                {
                    writer.Config.MergePolicy.NoCFSRatio = Random.NextBoolean() ? 0 : 1;
                    int numSegments = TestUtil.NextInt32(Random, 2, 20);
                    for (int i = 0; i < numSegments; ++i)
                    {
                        int numDocs = TestUtil.NextInt32(Random, 1, 5);
                        for (int j = 0; j < numDocs; ++j)
                        {
                            writer.AddDocument(new Document());
                        }
                        writer.GetReader().Dispose();
                    }
                    for (int i = 5; i >= 0; --i)
                    {
                        int segmentCount = writer.SegmentCount;
                        int maxNumSegments = i == 0 ? 1 : TestUtil.NextInt32(Random, 1, 10);
                        mayMerge.Set(segmentCount > maxNumSegments);
                        writer.ForceMerge(maxNumSegments);
                    }
                } // writer.Dispose();
            } // dir.Dispose();
        }

        private class SerialMergeSchedulerAnonymousInnerClassHelper : SerialMergeScheduler
        {
            private readonly BaseMergePolicyTestCase outerInstance;

            private AtomicBoolean mayMerge;

            public SerialMergeSchedulerAnonymousInnerClassHelper(BaseMergePolicyTestCase outerInstance, AtomicBoolean mayMerge)
            {
                this.outerInstance = outerInstance;
                this.mayMerge = mayMerge;
            }

            public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
            {
                lock (this)
                {
                    if (!mayMerge.Get() && writer.NextMerge() != null)
                    {
                        throw new InvalidOperationException();
                    }
                    base.Merge(writer, trigger, newMergesFound);
                }
            }
        }
    }
}