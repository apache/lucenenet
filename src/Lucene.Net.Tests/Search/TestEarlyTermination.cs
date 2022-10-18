using NUnit.Framework;
using RandomizedTesting.Generators;
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
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;

    [TestFixture]
    public class TestEarlyTermination : LuceneTestCase
    {
        private Directory dir;
        private RandomIndexWriter writer;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            writer = new RandomIndexWriter(Random, dir);
            int numDocs = AtLeast(100);
            for (int i = 0; i < numDocs; i++)
            {
                writer.AddDocument(new Document());
                if (Rarely())
                {
                    writer.Commit();
                }
            }
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEarlyTermination_Mem()
        {
            int iters = AtLeast(5);
            IndexReader reader = writer.GetReader();

            for (int i = 0; i < iters; ++i)
            {
                IndexSearcher searcher = NewSearcher(reader);
                ICollector collector = new CollectorAnonymousClass(this);

                searcher.Search(new MatchAllDocsQuery(), collector);
            }
            reader.Dispose();
        }

        private sealed class CollectorAnonymousClass : ICollector
        {
            private readonly TestEarlyTermination outerInstance;

            public CollectorAnonymousClass(TestEarlyTermination outerInstance)
            {
                this.outerInstance = outerInstance;
                outOfOrder = Random.NextBoolean();
                collectionTerminated = true;
            }

            internal readonly bool outOfOrder;
            internal bool collectionTerminated;

            public void SetScorer(Scorer scorer)
            {
            }

            public void Collect(int doc)
            {
                Assert.IsFalse(collectionTerminated);
                if (Rarely())
                {
                    collectionTerminated = true;
                    throw new CollectionTerminatedException();
                }
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                if (Random.NextBoolean())
                {
                    collectionTerminated = true;
                    throw new CollectionTerminatedException();
                }
                else
                {
                    collectionTerminated = false;
                }
            }

            public bool AcceptsDocsOutOfOrder => outOfOrder;
        }
    }
}