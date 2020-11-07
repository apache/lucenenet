using J2N.Threading.Atomic;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;

#if TESTFRAMEWORK_MSTEST
using Test = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#elif TESTFRAMEWORK_NUNIT
using Test = NUnit.Framework.TestAttribute;
#elif TESTFRAMEWORK_XUNIT
using Test = Lucene.Net.TestFramework.SkippableFactAttribute;
#endif

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

    /// <summary>
    /// Base test case for <see cref="Index.MergePolicy"/>.
    /// </summary>
    public abstract class BaseMergePolicyTestCase : LuceneTestCase
#if TESTFRAMEWORK_XUNIT
        , Xunit.IClassFixture<BeforeAfterClass>
    {
        public BaseMergePolicyTestCase(BeforeAfterClass beforeAfter)
            : base(beforeAfter)
        {
        }
#else
    {
#endif
        /// <summary>
        /// Create a new <see cref="Index.MergePolicy"/> instance. </summary>
        new protected abstract MergePolicy NewMergePolicy(); // LUCENENET specific - renamed from MergePolicy() to be consistent

        [Test]
        public virtual void TestForceMergeNotNeeded()
        {
            using (Directory dir = NewDirectory())
            {
                AtomicBoolean mayMerge = new AtomicBoolean(true);
                MergeScheduler mergeScheduler = new SerialMergeSchedulerAnonymousInnerClassHelper(mayMerge);
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
                        mayMerge.Value = (segmentCount > maxNumSegments);
                        writer.ForceMerge(maxNumSegments);
                    }
                } // writer.Dispose();
            } // dir.Dispose();
        }

        private class SerialMergeSchedulerAnonymousInnerClassHelper : SerialMergeScheduler
        {
            private readonly AtomicBoolean mayMerge;

            public SerialMergeSchedulerAnonymousInnerClassHelper(AtomicBoolean mayMerge)
            {
                this.mayMerge = mayMerge;
            }

            public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
            {
                lock (this)
                {
                    if (!mayMerge.Value && writer.NextMerge() != null)
                    {
                        throw new InvalidOperationException();
                    }
                    base.Merge(writer, trigger, newMergesFound);
                }
            }
        }
    }
}