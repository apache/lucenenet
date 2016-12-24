namespace Lucene.Net.Search
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Directory = Lucene.Net.Store.Directory;

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

    using Document = Documents.Document;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;

    [TestFixture]
    public class TestEarlyTermination : LuceneTestCase
    {
        internal Directory Dir;
        internal RandomIndexWriter Writer;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Dir = NewDirectory();
            Writer = new RandomIndexWriter(Random(), Dir, Similarity, TimeZone);
            int numDocs = AtLeast(100);
            for (int i = 0; i < numDocs; i++)
            {
                Writer.AddDocument(new Document());
                if (Rarely())
                {
                    Writer.Commit();
                }
            }
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            Writer.Dispose();
            Dir.Dispose();
        }

        [Test]
        public virtual void TestEarlyTermination_Mem()
        {
            int iters = AtLeast(5);
            IndexReader reader = Writer.Reader;

            for (int i = 0; i < iters; ++i)
            {
                IndexSearcher searcher = NewSearcher(reader);
                Collector collector = new CollectorAnonymousInnerClassHelper(this);

                searcher.Search(new MatchAllDocsQuery(), collector);
            }
            reader.Dispose();
        }

        private class CollectorAnonymousInnerClassHelper : Collector
        {
            private readonly TestEarlyTermination OuterInstance;

            public CollectorAnonymousInnerClassHelper(TestEarlyTermination outerInstance)
            {
                this.OuterInstance = outerInstance;
                outOfOrder = Random().NextBoolean();
                collectionTerminated = true;
            }

            internal readonly bool outOfOrder;
            internal bool collectionTerminated;

            public override void SetScorer(Scorer scorer)
            {
            }

            public override void Collect(int doc)
            {
                Assert.IsFalse(collectionTerminated);
                if (Rarely())
                {
                    collectionTerminated = true;
                    throw new CollectionTerminatedException();
                }
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                if (Random().NextBoolean())
                {
                    collectionTerminated = true;
                    throw new CollectionTerminatedException();
                }
                else
                {
                    collectionTerminated = false;
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return outOfOrder;
            }
        }
    }
}