using Lucene.Net.Documents;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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
    using Field = Field;
    using IBits = Lucene.Net.Util.IBits;
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
            DocIdSet innerSet = new DocIdSetAnonymousClass(this, maxdoc);

            DocIdSet filteredSet = new FilteredDocIdSetAnonymousClass(this, innerSet);

            DocIdSetIterator iter = filteredSet.GetIterator();
            IList<int> list = new JCG.List<int>();
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
            using IEnumerator<int> intIter = list.GetEnumerator();
            while (intIter.MoveNext())
            {
                docs[c++] = intIter.Current;
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

        private sealed class DocIdSetAnonymousClass : DocIdSet
        {
            private readonly TestDocIdSet outerInstance;

            private readonly int maxdoc;

            public DocIdSetAnonymousClass(TestDocIdSet outerInstance, int maxdoc)
            {
                this.outerInstance = outerInstance;
                this.maxdoc = maxdoc;
            }

            public override DocIdSetIterator GetIterator()
            {
                return new DocIdSetIteratorAnonymousClass(this);
            }

            private sealed class DocIdSetIteratorAnonymousClass : DocIdSetIterator
            {
                private readonly DocIdSetAnonymousClass outerInstance;

                public DocIdSetIteratorAnonymousClass(DocIdSetAnonymousClass outerInstance)
                {
                    this.outerInstance = outerInstance;
                    docid = -1;
                }

                internal int docid;

                public override int DocID => docid;

                public override int NextDoc()
                {
                    docid++;
                    return docid < outerInstance.maxdoc ? docid : (docid = NO_MORE_DOCS);
                }

                public override int Advance(int target)
                {
                    return SlowAdvance(target);
                }

                public override long GetCost()
                {
                    return 1;
                }
            }
        }

        private sealed class FilteredDocIdSetAnonymousClass : FilteredDocIdSet
        {
            private readonly TestDocIdSet outerInstance;

            public FilteredDocIdSetAnonymousClass(TestDocIdSet outerInstance, DocIdSet innerSet)
                : base(innerSet)
            {
                this.outerInstance = outerInstance;
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
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("c", "val", Field.Store.NO));
            writer.AddDocument(doc);
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            // First verify the document is searchable.
            IndexSearcher searcher = NewSearcher(reader);
            Assert.AreEqual(1, searcher.Search(new MatchAllDocsQuery(), 10).TotalHits);

            // Now search w/ a Filter which returns a null DocIdSet
            Filter f = new FilterAnonymousClass(this);

            Assert.AreEqual(0, searcher.Search(new MatchAllDocsQuery(), f, 10).TotalHits);
            reader.Dispose();
            dir.Dispose();
        }

        private sealed class FilterAnonymousClass : Filter
        {
            private readonly TestDocIdSet outerInstance;

            public FilterAnonymousClass(TestDocIdSet outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return null;
            }
        }

        [Test]
        public virtual void TestNullIteratorFilteredDocIdSet()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("c", "val", Field.Store.NO));
            writer.AddDocument(doc);
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            // First verify the document is searchable.
            IndexSearcher searcher = NewSearcher(reader);
            Assert.AreEqual(1, searcher.Search(new MatchAllDocsQuery(), 10).TotalHits);

            // Now search w/ a Filter which returns a null DocIdSet
            Filter f = new FilterAnonymousClass2(this);

            Assert.AreEqual(0, searcher.Search(new MatchAllDocsQuery(), f, 10).TotalHits);
            reader.Dispose();
            dir.Dispose();
        }

        private sealed class FilterAnonymousClass2 : Filter
        {
            private readonly TestDocIdSet outerInstance;

            public FilterAnonymousClass2(TestDocIdSet outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                DocIdSet innerNullIteratorSet = new DocIdSetAnonymousClass2(this);
                return new FilteredDocIdSetAnonymousClass2(this, innerNullIteratorSet);
            }

            private sealed class DocIdSetAnonymousClass2 : DocIdSet
            {
                private readonly FilterAnonymousClass2 outerInstance;

                public DocIdSetAnonymousClass2(FilterAnonymousClass2 outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                public override DocIdSetIterator GetIterator()
                {
                    return null;
                }
            }

            private sealed class FilteredDocIdSetAnonymousClass2 : FilteredDocIdSet
            {
                private readonly FilterAnonymousClass2 outerInstance;

                public FilteredDocIdSetAnonymousClass2(FilterAnonymousClass2 outerInstance, DocIdSet innerNullIteratorSet)
                    : base(innerNullIteratorSet)
                {
                    this.outerInstance = outerInstance;
                }

                protected override bool Match(int docid)
                {
                    return true;
                }
            }
        }
    }
}