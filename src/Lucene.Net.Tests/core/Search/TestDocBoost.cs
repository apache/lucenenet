using System;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    
    using NUnit.Framework;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Directory = Lucene.Net.Store.Directory;
    using IndexReader = Lucene.Net.Index.IndexReader;
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
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Document boost unit test.
    ///
    ///
    /// </summary>
    [TestFixture]
    public class TestDocBoost : LuceneTestCase
    {
        [Test]
        public virtual void TestDocBoost_Mem()
        {
            Directory store = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), store, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));

            Field f1 = NewTextField("field", "word", Field.Store.YES);
            Field f2 = NewTextField("field", "word", Field.Store.YES);
            f2.Boost = 2.0f;

            Documents.Document d1 = new Documents.Document();
            Documents.Document d2 = new Documents.Document();

            d1.Add(f1); // boost = 1
            d2.Add(f2); // boost = 2

            writer.AddDocument(d1);
            writer.AddDocument(d2);

            IndexReader reader = writer.Reader;
            writer.Dispose();

            float[] scores = new float[4];

            IndexSearcher searcher = NewSearcher(reader);
            searcher.Search(new TermQuery(new Term("field", "word")), new CollectorAnonymousInnerClassHelper(this, scores));

            float lastScore = 0.0f;

            for (int i = 0; i < 2; i++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine(searcher.Explain(new TermQuery(new Term("field", "word")), i));
                }
                Assert.IsTrue(scores[i] > lastScore, "score: " + scores[i] + " should be > lastScore: " + lastScore);
                lastScore = scores[i];
            }

            reader.Dispose();
            store.Dispose();
        }

        private class CollectorAnonymousInnerClassHelper : Collector
        {
            private readonly TestDocBoost OuterInstance;

            private float[] Scores;

            public CollectorAnonymousInnerClassHelper(TestDocBoost outerInstance, float[] scores)
            {
                this.OuterInstance = outerInstance;
                this.Scores = scores;
                @base = 0;
            }

            private int @base;
            private Scorer scorer;

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override void Collect(int doc)
            {
                Scores[doc + @base] = scorer.Score();
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                @base = context.DocBase;
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }
    }
}