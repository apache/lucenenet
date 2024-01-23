using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    public class TestCachingCollector : LuceneTestCase
    {
        private const double ONE_BYTE = 1.0 / (1024 * 1024); // 1 byte out of MB

        private class MockScorer : Scorer
        {
            // LUCENENET: static instance to reduce allocations
            public static readonly MockScorer Default = new MockScorer();

            // LUCENENET: Made private
            private MockScorer()
                : base((Weight)null)
            {
            }

            public override float GetScore()
            {
                return 0;
            }

            public override int Freq => 0;

            public override int DocID => 0;

            public override int NextDoc()
            {
                return 0;
            }

            public override int Advance(int target)
            {
                return 0;
            }

            public override long GetCost()
            {
                return 1;
            }
        }

        private class NoOpCollector : ICollector
        {
            // LUCENENET: static instances to reduce allocations
            public static readonly NoOpCollector FalseInstance = new NoOpCollector(false);
            public static readonly NoOpCollector TrueInstance = new NoOpCollector(true);

            private readonly bool acceptDocsOutOfOrder;

            // LUCENENET: Made private
            private NoOpCollector(bool acceptDocsOutOfOrder)
            {
                this.acceptDocsOutOfOrder = acceptDocsOutOfOrder;
            }

            public virtual void SetScorer(Scorer scorer)
            {
            }

            public virtual void Collect(int doc)
            {
            }

            public virtual void SetNextReader(AtomicReaderContext context)
            {
            }

            public virtual bool AcceptsDocsOutOfOrder => acceptDocsOutOfOrder;
        }

        [Test]
        public virtual void TestBasic()
        {
            foreach (bool cacheScores in new bool[] { false, true })
            {
                CachingCollector cc = CachingCollector.Create(NoOpCollector.FalseInstance, cacheScores, 1.0); // LUCENENET: use static instance of NoOpCollector
                cc.SetScorer(MockScorer.Default); // LUCENENET: use static instance of MockScorer

                // collect 1000 docs
                for (int i = 0; i < 1000; i++)
                {
                    cc.Collect(i);
                }

                // now replay them
                cc.Replay(new CollectorAnonymousClass());
            }
        }

        private sealed class CollectorAnonymousClass : ICollector
        {
            internal int prevDocID = -1;

            public void SetScorer(Scorer scorer)
            {
            }

            public void SetNextReader(AtomicReaderContext context)
            {
            }

            public void Collect(int doc)
            {
                Assert.AreEqual(prevDocID + 1, doc);
                prevDocID = doc;
            }

            public bool AcceptsDocsOutOfOrder => false;
        }

        [Test]
        public virtual void TestIllegalStateOnReplay()
        {
            CachingCollector cc = CachingCollector.Create(NoOpCollector.FalseInstance, true, 50 * ONE_BYTE); // LUCENENET: use static instance of NoOpCollector
            cc.SetScorer(MockScorer.Default); // LUCENENET: use static instance of MockScorer

            // collect 130 docs, this should be enough for triggering cache abort.
            for (int i = 0; i < 130; i++)
            {
                cc.Collect(i);
            }

            Assert.IsFalse(cc.IsCached, "CachingCollector should not be cached due to low memory limit");

            try
            {
                cc.Replay(NoOpCollector.FalseInstance); // LUCENENET: use static instance of NoOpCollector
                Assert.Fail("replay should fail if CachingCollector is not cached");
            }
            catch (Exception e) when (e.IsIllegalStateException())
            {
                // expected
            }
        }

        [Test]
        public virtual void TestIllegalCollectorOnReplay()
        {
            // tests that the Collector passed to replay() has an out-of-order mode that
            // is valid with the Collector passed to the ctor

            // 'src' Collector does not support out-of-order
            CachingCollector cc = CachingCollector.Create(NoOpCollector.FalseInstance, true, 50 * ONE_BYTE); // LUCENENET: use static instance of NoOpCollector
            cc.SetScorer(MockScorer.Default); // LUCENENET: use static instance of MockScorer
            for (int i = 0; i < 10; i++)
            {
                cc.Collect(i);
            }
            cc.Replay(NoOpCollector.TrueInstance); // this call should not fail  // LUCENENET: use static instance of NoOpCollector
            cc.Replay(NoOpCollector.FalseInstance); // this call should not fail  // LUCENENET: use static instance of NoOpCollector

            // 'src' Collector supports out-of-order
            cc = CachingCollector.Create(NoOpCollector.TrueInstance, true, 50 * ONE_BYTE);  // LUCENENET: use static instance of NoOpCollector
            cc.SetScorer(MockScorer.Default); // LUCENENET: use static instance of MockScorer
            for (int i = 0; i < 10; i++)
            {
                cc.Collect(i);
            }
            cc.Replay(NoOpCollector.TrueInstance); // this call should not fail  // LUCENENET: use static instance of NoOpCollector
            try
            {
                cc.Replay(NoOpCollector.FalseInstance); // this call should fail  // LUCENENET: use static instance of NoOpCollector
                Assert.Fail("should have failed if an in-order Collector was given to replay(), " + "while CachingCollector was initialized with out-of-order collection");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // ok
            }
        }

        [Test]
        public virtual void TestCachedArraysAllocation()
        {
            // tests the cached arrays allocation -- if the 'nextLength' was too high,
            // caching would terminate even if a smaller length would suffice.

            // set RAM limit enough for 150 docs + random(10000)
            int numDocs = Random.Next(10000) + 150;
            foreach (bool cacheScores in new bool[] { false, true })
            {
                int bytesPerDoc = cacheScores ? 8 : 4;
                CachingCollector cc = CachingCollector.Create(NoOpCollector.FalseInstance, cacheScores, bytesPerDoc * ONE_BYTE * numDocs);  // LUCENENET: use static instance of NoOpCollector
                cc.SetScorer(MockScorer.Default); // LUCENENET: use static instance of MockScorer
                for (int i = 0; i < numDocs; i++)
                {
                    cc.Collect(i);
                }
                Assert.IsTrue(cc.IsCached);

                // The 151's document should terminate caching
                cc.Collect(numDocs);
                Assert.IsFalse(cc.IsCached);
            }
        }

        [Test]
        public virtual void TestNoWrappedCollector()
        {
            foreach (bool cacheScores in new bool[] { false, true })
            {
                // create w/ null wrapped collector, and test that the methods work
                CachingCollector cc = CachingCollector.Create(true, cacheScores, 50 * ONE_BYTE);
                cc.SetNextReader(null);
                cc.SetScorer(MockScorer.Default); // LUCENENET: use static instance of MockScorer
                cc.Collect(0);

                Assert.IsTrue(cc.IsCached);
                cc.Replay(NoOpCollector.TrueInstance); // LUCENENET: use static instance of NoOpCollector
            }
        }
    }
}
