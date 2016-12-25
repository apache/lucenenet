using System;
using System.Collections.Generic;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
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
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;

    [TestFixture]
    public class TestDocIdSet : LuceneTestCase
    {
        [Test]
        public virtual void TestFilteredDocIdSet()
        {
            const int maxdoc = 10;
            DocIdSet innerSet = new DocIdSetAnonymousInnerClassHelper(this, maxdoc);

            DocIdSet filteredSet = new FilteredDocIdSetAnonymousInnerClassHelper(this, innerSet);

            DocIdSetIterator iter = filteredSet.GetIterator();
            List<int?> list = new List<int?>();
            int doc = iter.Advance(3);
            if (doc != DocIdSetIterator.NO_MORE_DOCS)
            {
                list.Add(Convert.ToInt32(doc));
                while ((doc = iter.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    list.Add(Convert.ToInt32(doc));
                }
            }

            int[] docs = new int[list.Count];
            int c = 0;
            IEnumerator<int?> intIter = list.GetEnumerator();
            while (intIter.MoveNext())
            {
                docs[c++] = (int)intIter.Current;
            }
            int[] answer = new int[] { 4, 6, 8 };
            bool same = Arrays.Equals(answer, docs);
            if (!same)
            {
                Console.WriteLine("answer: " + Arrays.ToString(answer));
                Console.WriteLine("gotten: " + Arrays.ToString(docs));
                Assert.Fail();
            }
        }

        private class DocIdSetAnonymousInnerClassHelper : DocIdSet
        {
            private readonly TestDocIdSet OuterInstance;

            private int Maxdoc;

            public DocIdSetAnonymousInnerClassHelper(TestDocIdSet outerInstance, int maxdoc)
            {
                this.OuterInstance = outerInstance;
                this.Maxdoc = maxdoc;
            }

            public override DocIdSetIterator GetIterator()
            {
                return new DocIdSetIteratorAnonymousInnerClassHelper(this);
            }

            private class DocIdSetIteratorAnonymousInnerClassHelper : DocIdSetIterator
            {
                private readonly DocIdSetAnonymousInnerClassHelper OuterInstance;

                public DocIdSetIteratorAnonymousInnerClassHelper(DocIdSetAnonymousInnerClassHelper outerInstance)
                {
                    this.OuterInstance = outerInstance;
                    docid = -1;
                }

                internal int docid;

                public override int DocID
                {
                    get { return docid; }
                }

                public override int NextDoc()
                {
                    docid++;
                    return docid < OuterInstance.Maxdoc ? docid : (docid = NO_MORE_DOCS);
                }

                public override int Advance(int target)
                {
                    return SlowAdvance(target);
                }

                public override long Cost()
                {
                    return 1;
                }
            }
        }

        private class FilteredDocIdSetAnonymousInnerClassHelper : FilteredDocIdSet
        {
            private readonly TestDocIdSet OuterInstance;

            public FilteredDocIdSetAnonymousInnerClassHelper(TestDocIdSet outerInstance, DocIdSet innerSet)
                : base(innerSet)
            {
                this.OuterInstance = outerInstance;
            }

            protected override bool Match(int docid)
            {
                return docid % 2 == 0; //validate only even docids
            }
        }

        [Test]
        public virtual void TestNullDocIdSet()
        {
            // Tests that if a Filter produces a null DocIdSet, which is given to
            // IndexSearcher, everything works fine. this came up in LUCENE-1754.
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("c", "val", Field.Store.NO));
            writer.AddDocument(doc);
            IndexReader reader = writer.Reader;
            writer.Dispose();

            // First verify the document is searchable.
            IndexSearcher searcher = NewSearcher(reader);
            Assert.AreEqual(1, searcher.Search(new MatchAllDocsQuery(), 10).TotalHits);

            // Now search w/ a Filter which returns a null DocIdSet
            Filter f = new FilterAnonymousInnerClassHelper(this);

            Assert.AreEqual(0, searcher.Search(new MatchAllDocsQuery(), f, 10).TotalHits);
            reader.Dispose();
            dir.Dispose();
        }

        private class FilterAnonymousInnerClassHelper : Filter
        {
            private readonly TestDocIdSet OuterInstance;

            public FilterAnonymousInnerClassHelper(TestDocIdSet outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                return null;
            }
        }

        [Test]
        public virtual void TestNullIteratorFilteredDocIdSet()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("c", "val", Field.Store.NO));
            writer.AddDocument(doc);
            IndexReader reader = writer.Reader;
            writer.Dispose();

            // First verify the document is searchable.
            IndexSearcher searcher = NewSearcher(reader);
            Assert.AreEqual(1, searcher.Search(new MatchAllDocsQuery(), 10).TotalHits);

            // Now search w/ a Filter which returns a null DocIdSet
            Filter f = new FilterAnonymousInnerClassHelper2(this);

            Assert.AreEqual(0, searcher.Search(new MatchAllDocsQuery(), f, 10).TotalHits);
            reader.Dispose();
            dir.Dispose();
        }

        private class FilterAnonymousInnerClassHelper2 : Filter
        {
            private readonly TestDocIdSet OuterInstance;

            public FilterAnonymousInnerClassHelper2(TestDocIdSet outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                DocIdSet innerNullIteratorSet = new DocIdSetAnonymousInnerClassHelper2(this);
                return new FilteredDocIdSetAnonymousInnerClassHelper2(this, innerNullIteratorSet);
            }

            private class DocIdSetAnonymousInnerClassHelper2 : DocIdSet
            {
                private readonly FilterAnonymousInnerClassHelper2 OuterInstance;

                public DocIdSetAnonymousInnerClassHelper2(FilterAnonymousInnerClassHelper2 outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                public override DocIdSetIterator GetIterator()
                {
                    return null;
                }
            }

            private class FilteredDocIdSetAnonymousInnerClassHelper2 : FilteredDocIdSet
            {
                private readonly FilterAnonymousInnerClassHelper2 OuterInstance;

                public FilteredDocIdSetAnonymousInnerClassHelper2(FilterAnonymousInnerClassHelper2 outerInstance, DocIdSet innerNullIteratorSet)
                    : base(innerNullIteratorSet)
                {
                    this.OuterInstance = outerInstance;
                }

                protected override bool Match(int docid)
                {
                    return true;
                }
            }
        }
    }
}