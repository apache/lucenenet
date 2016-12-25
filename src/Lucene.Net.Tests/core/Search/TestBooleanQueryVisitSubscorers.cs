using System.Collections.Generic;
using Lucene.Net.Documents;

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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using ChildScorer = Lucene.Net.Search.Scorer.ChildScorer;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Store = Field.Store;
    using Term = Lucene.Net.Index.Term;
    using TextField = TextField;

    // TODO: refactor to a base class, that collects freqs from the scorer tree
    // and test all queries with it
    [TestFixture]
    public class TestBooleanQueryVisitSubscorers : LuceneTestCase
    {
        internal Analyzer Analyzer;
        internal IndexReader Reader;
        internal IndexSearcher Searcher;
        internal Directory Dir;

        internal const string F1 = "title";
        internal const string F2 = "body";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Analyzer = new MockAnalyzer(Random());
            Dir = NewDirectory();
            IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, Analyzer);
            config.SetMergePolicy(NewLogMergePolicy()); // we will use docids to validate
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Dir, config);
            writer.AddDocument(Doc("lucene", "lucene is a very popular search engine library"));
            writer.AddDocument(Doc("solr", "solr is a very popular search server and is using lucene"));
            writer.AddDocument(Doc("nutch", "nutch is an internet search engine with web crawler and is using lucene and hadoop"));
            Reader = writer.Reader;
            writer.Dispose();
            Searcher = NewSearcher(Reader);
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestDisjunctions()
        {
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term(F1, "lucene")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term(F2, "lucene")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term(F2, "search")), Occur.SHOULD);
            IDictionary<int, int> tfs = GetDocCounts(Searcher, bq);
            Assert.AreEqual(3, tfs.Count); // 3 documents
            Assert.AreEqual(3, (int)tfs[0]); // f1:lucene + f2:lucene + f2:search
            Assert.AreEqual(2, (int)tfs[1]); // f2:search + f2:lucene
            Assert.AreEqual(2, (int)tfs[2]); // f2:search + f2:lucene
        }

        [Test]
        public virtual void TestNestedDisjunctions()
        {
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term(F1, "lucene")), Occur.SHOULD);
            BooleanQuery bq2 = new BooleanQuery();
            bq2.Add(new TermQuery(new Term(F2, "lucene")), Occur.SHOULD);
            bq2.Add(new TermQuery(new Term(F2, "search")), Occur.SHOULD);
            bq.Add(bq2, Occur.SHOULD);
            IDictionary<int, int> tfs = GetDocCounts(Searcher, bq);
            Assert.AreEqual(3, tfs.Count); // 3 documents
            Assert.AreEqual(3, (int)tfs[0]); // f1:lucene + f2:lucene + f2:search
            Assert.AreEqual(2, (int)tfs[1]); // f2:search + f2:lucene
            Assert.AreEqual(2, (int)tfs[2]); // f2:search + f2:lucene
        }

        [Test]
        public virtual void TestConjunctions()
        {
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term(F2, "lucene")), Occur.MUST);
            bq.Add(new TermQuery(new Term(F2, "is")), Occur.MUST);
            IDictionary<int, int> tfs = GetDocCounts(Searcher, bq);
            Assert.AreEqual(3, tfs.Count); // 3 documents
            Assert.AreEqual(2, (int)tfs[0]); // f2:lucene + f2:is
            Assert.AreEqual(3, (int)tfs[1]); // f2:is + f2:is + f2:lucene
            Assert.AreEqual(3, (int)tfs[2]); // f2:is + f2:is + f2:lucene
        }

        internal static Document Doc(string v1, string v2)
        {
            Document doc = new Document();
            doc.Add(new TextField(F1, v1, Store.YES));
            doc.Add(new TextField(F2, v2, Store.YES));
            return doc;
        }

        internal static IDictionary<int, int> GetDocCounts(IndexSearcher searcher, Query query)
        {
            MyCollector collector = new MyCollector();
            searcher.Search(query, collector);
            return collector.DocCounts;
        }

        internal class MyCollector : Collector
        {
            internal TopDocsCollector<ScoreDoc> Collector;
            internal int DocBase;

            public readonly IDictionary<int, int> DocCounts = new Dictionary<int, int>();
            internal readonly HashSet<Scorer> TqsSet = new HashSet<Scorer>();

            internal MyCollector()
            {
                Collector = TopScoreDocCollector.Create(10, true);
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return false; }
            }

            public override void Collect(int doc)
            {
                int freq = 0;
                foreach (Scorer scorer in TqsSet)
                {
                    if (doc == scorer.DocID)
                    {
                        freq += scorer.Freq;
                    }
                }
                DocCounts[doc + DocBase] = freq;
                Collector.Collect(doc);
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                this.DocBase = context.DocBase;
                Collector.SetNextReader(context);
            }

            public override void SetScorer(Scorer scorer)
            {
                Collector.SetScorer(scorer);
                TqsSet.Clear();
                FillLeaves(scorer, TqsSet);
            }

            internal virtual void FillLeaves(Scorer scorer, ISet<Scorer> set)
            {
                if (scorer.Weight.Query is TermQuery)
                {
                    set.Add(scorer);
                }
                else
                {
                    foreach (ChildScorer child in scorer.Children)
                    {
                        FillLeaves(child.Child, set);
                    }
                }
            }

            public virtual TopDocs TopDocs()
            {
                return Collector.TopDocs();
            }

            public virtual int Freq(int doc)
            {
                return DocCounts[doc];
            }
        }
    }
}