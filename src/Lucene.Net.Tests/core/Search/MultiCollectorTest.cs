namespace Lucene.Net.Search
{
    using NUnit.Framework;

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
    public class MultiCollectorTest : LuceneTestCase
    {
        private class DummyCollector : Collector
        {
            internal bool AcceptsDocsOutOfOrderCalled = false;
            internal bool CollectCalled = false;
            internal bool SetNextReaderCalled = false;
            internal bool SetScorerCalled = false;

            public override bool AcceptsDocsOutOfOrder
            {
                get
                {
                    AcceptsDocsOutOfOrderCalled = true;
                    return true;
                }
            }

            public override void Collect(int doc)
            {
                CollectCalled = true;
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                SetNextReaderCalled = true;
            }

            public override void SetScorer(Scorer scorer)
            {
                SetScorerCalled = true;
            }
        }

        [Test]
        public virtual void TestNullCollectors()
        {
            // Tests that the collector rejects all null collectors.
            try
            {
                MultiCollector.Wrap(null, null);
                Assert.Fail("only null collectors should not be supported");
            }
            catch (System.ArgumentException e)
            {
                // expected
            }

            // Tests that the collector handles some null collectors well. If it
            // doesn't, an NPE would be thrown.
            Collector c = MultiCollector.Wrap(new DummyCollector(), null, new DummyCollector());
            Assert.IsTrue(c is MultiCollector);
            Assert.IsTrue(c.AcceptsDocsOutOfOrder);
            c.Collect(1);
            c.SetNextReader(null);
            c.SetScorer(null);
        }

        [Test]
        public virtual void TestSingleCollector()
        {
            // Tests that if a single Collector is input, it is returned (and not MultiCollector).
            DummyCollector dc = new DummyCollector();
            Assert.AreSame(dc, MultiCollector.Wrap(dc));
            Assert.AreSame(dc, MultiCollector.Wrap(dc, null));
        }

        [Test]
        public virtual void TestCollector()
        {
            // Tests that the collector delegates calls to input collectors properly.

            // Tests that the collector handles some null collectors well. If it
            // doesn't, an NPE would be thrown.
            DummyCollector[] dcs = new DummyCollector[] { new DummyCollector(), new DummyCollector() };
            Collector c = MultiCollector.Wrap(dcs);
            Assert.IsTrue(c.AcceptsDocsOutOfOrder);
            c.Collect(1);
            c.SetNextReader(null);
            c.SetScorer(null);

            foreach (DummyCollector dc in dcs)
            {
                Assert.IsTrue(dc.AcceptsDocsOutOfOrderCalled);
                Assert.IsTrue(dc.CollectCalled);
                Assert.IsTrue(dc.SetNextReaderCalled);
                Assert.IsTrue(dc.SetScorerCalled);
            }
        }
    }
}