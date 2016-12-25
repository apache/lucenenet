using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Documents;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using CompositeReaderContext = Lucene.Net.Index.CompositeReaderContext;
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
    using FloatField = FloatField;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using IntField = IntField;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using ReaderUtil = Lucene.Net.Index.ReaderUtil;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestTopDocsMerge : LuceneTestCaseWithReducedFloatPrecision
    {
        private class ShardSearcher : IndexSearcher
        {
            private readonly IList<AtomicReaderContext> Ctx;

            public ShardSearcher(AtomicReaderContext ctx, IndexReaderContext parent)
                : base(parent)
            {
                this.Ctx = new List<AtomicReaderContext> { ctx };
            }

            public virtual void Search(Weight weight, Collector collector)
            {
                Search(Ctx, weight, collector);
            }

            public virtual TopDocs Search(Weight weight, int topN)
            {
                return Search(Ctx, weight, null, topN);
            }

            public override string ToString()
            {
                return "ShardSearcher(" + Ctx[0] + ")";
            }
        }

        [Test]
        public virtual void TestSort_1()
        {
            TestSort(false);
        }

        [Test]
        public virtual void TestSort_2()
        {
            TestSort(true);
        }

        internal virtual void TestSort(bool useFrom)
        {
            IndexReader reader = null;
            Directory dir = null;

            int numDocs = AtLeast(1000);
            //final int numDocs = AtLeast(50);

            string[] tokens = new string[] { "a", "b", "c", "d", "e" };

            if (VERBOSE)
            {
                Console.WriteLine("TEST: make index");
            }

            {
                dir = NewDirectory();
                RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
                // w.setDoRandomForceMerge(false);

                // w.w.getConfig().SetMaxBufferedDocs(AtLeast(100));

                string[] content = new string[AtLeast(20)];

                for (int contentIDX = 0; contentIDX < content.Length; contentIDX++)
                {
                    StringBuilder sb = new StringBuilder();
                    int numTokens = TestUtil.NextInt(Random(), 1, 10);
                    for (int tokenIDX = 0; tokenIDX < numTokens; tokenIDX++)
                    {
                        sb.Append(tokens[Random().Next(tokens.Length)]).Append(' ');
                    }
                    content[contentIDX] = sb.ToString();
                }

                for (int docIDX = 0; docIDX < numDocs; docIDX++)
                {
                    Document doc = new Document();
                    doc.Add(NewStringField("string", TestUtil.RandomRealisticUnicodeString(Random()), Field.Store.NO));
                    doc.Add(NewTextField("text", content[Random().Next(content.Length)], Field.Store.NO));
                    doc.Add(new FloatField("float", (float)Random().NextDouble(), Field.Store.NO));
                    int intValue;
                    if (Random().Next(100) == 17)
                    {
                        intValue = int.MinValue;
                    }
                    else if (Random().Next(100) == 17)
                    {
                        intValue = int.MaxValue;
                    }
                    else
                    {
                        intValue = Random().Next();
                    }
                    doc.Add(new IntField("int", intValue, Field.Store.NO));
                    if (VERBOSE)
                    {
                        Console.WriteLine("  doc=" + doc);
                    }
                    w.AddDocument(doc);
                }

                reader = w.Reader;
                w.Dispose();
            }

            // NOTE: sometimes reader has just one segment, which is
            // important to test
            IndexSearcher searcher = NewSearcher(reader);
            IndexReaderContext ctx = searcher.TopReaderContext;

            ShardSearcher[] subSearchers;
            int[] docStarts;

            if (ctx is AtomicReaderContext)
            {
                subSearchers = new ShardSearcher[1];
                docStarts = new int[1];
                subSearchers[0] = new ShardSearcher((AtomicReaderContext)ctx, ctx);
                docStarts[0] = 0;
            }
            else
            {
                CompositeReaderContext compCTX = (CompositeReaderContext)ctx;
                int size = compCTX.Leaves.Count;
                subSearchers = new ShardSearcher[size];
                docStarts = new int[size];
                int docBase = 0;
                for (int searcherIDX = 0; searcherIDX < subSearchers.Length; searcherIDX++)
                {
                    AtomicReaderContext leave = compCTX.Leaves[searcherIDX];
                    subSearchers[searcherIDX] = new ShardSearcher(leave, compCTX);
                    docStarts[searcherIDX] = docBase;
                    docBase += leave.Reader.MaxDoc;
                }
            }

            IList<SortField> sortFields = new List<SortField>();
            sortFields.Add(new SortField("string", SortFieldType.STRING, true));
            sortFields.Add(new SortField("string", SortFieldType.STRING, false));
            sortFields.Add(new SortField("int", SortFieldType.INT, true));
            sortFields.Add(new SortField("int", SortFieldType.INT, false));
            sortFields.Add(new SortField("float", SortFieldType.FLOAT, true));
            sortFields.Add(new SortField("float", SortFieldType.FLOAT, false));
            sortFields.Add(new SortField(null, SortFieldType.SCORE, true));
            sortFields.Add(new SortField(null, SortFieldType.SCORE, false));
            sortFields.Add(new SortField(null, SortFieldType.DOC, true));
            sortFields.Add(new SortField(null, SortFieldType.DOC, false));

            for (int iter = 0; iter < 1000 * RANDOM_MULTIPLIER; iter++)
            {
                // TODO: custom FieldComp...
                Query query = new TermQuery(new Term("text", tokens[Random().Next(tokens.Length)]));

                Sort sort;
                if (Random().Next(10) == 4)
                {
                    // Sort by score
                    sort = null;
                }
                else
                {
                    SortField[] randomSortFields = new SortField[TestUtil.NextInt(Random(), 1, 3)];
                    for (int sortIDX = 0; sortIDX < randomSortFields.Length; sortIDX++)
                    {
                        randomSortFields[sortIDX] = sortFields[Random().Next(sortFields.Count)];
                    }
                    sort = new Sort(randomSortFields);
                }

                int numHits = TestUtil.NextInt(Random(), 1, numDocs + 5);
                //final int numHits = 5;

                if (VERBOSE)
                {
                    Console.WriteLine("TEST: search query=" + query + " sort=" + sort + " numHits=" + numHits);
                }

                int from = -1;
                int size = -1;
                // First search on whole index:
                TopDocs topHits;
                if (sort == null)
                {
                    if (useFrom)
                    {
                        TopScoreDocCollector c = TopScoreDocCollector.Create(numHits, Random().NextBoolean());
                        searcher.Search(query, c);
                        from = TestUtil.NextInt(Random(), 0, numHits - 1);
                        size = numHits - from;
                        TopDocs tempTopHits = c.TopDocs();
                        if (from < tempTopHits.ScoreDocs.Length)
                        {
                            // Can't use TopDocs#topDocs(start, howMany), since it has different behaviour when start >= hitCount
                            // than TopDocs#merge currently has
                            ScoreDoc[] newScoreDocs = new ScoreDoc[Math.Min(size, tempTopHits.ScoreDocs.Length - from)];
                            Array.Copy(tempTopHits.ScoreDocs, from, newScoreDocs, 0, newScoreDocs.Length);
                            tempTopHits.ScoreDocs = newScoreDocs;
                            topHits = tempTopHits;
                        }
                        else
                        {
                            topHits = new TopDocs(tempTopHits.TotalHits, new ScoreDoc[0], tempTopHits.MaxScore);
                        }
                    }
                    else
                    {
                        topHits = searcher.Search(query, numHits);
                    }
                }
                else
                {
                    TopFieldCollector c = TopFieldCollector.Create(sort, numHits, true, true, true, Random().NextBoolean());
                    searcher.Search(query, c);
                    if (useFrom)
                    {
                        from = TestUtil.NextInt(Random(), 0, numHits - 1);
                        size = numHits - from;
                        TopDocs tempTopHits = c.TopDocs();
                        if (from < tempTopHits.ScoreDocs.Length)
                        {
                            // Can't use TopDocs#topDocs(start, howMany), since it has different behaviour when start >= hitCount
                            // than TopDocs#merge currently has
                            ScoreDoc[] newScoreDocs = new ScoreDoc[Math.Min(size, tempTopHits.ScoreDocs.Length - from)];
                            Array.Copy(tempTopHits.ScoreDocs, from, newScoreDocs, 0, newScoreDocs.Length);
                            tempTopHits.ScoreDocs = newScoreDocs;
                            topHits = tempTopHits;
                        }
                        else
                        {
                            topHits = new TopDocs(tempTopHits.TotalHits, new ScoreDoc[0], tempTopHits.MaxScore);
                        }
                    }
                    else
                    {
                        topHits = c.TopDocs(0, numHits);
                    }
                }

                if (VERBOSE)
                {
                    if (useFrom)
                    {
                        Console.WriteLine("from=" + from + " size=" + size);
                    }
                    Console.WriteLine("  top search: " + topHits.TotalHits + " totalHits; hits=" + (topHits.ScoreDocs == null ? "null" : topHits.ScoreDocs.Length + " maxScore=" + topHits.MaxScore));
                    if (topHits.ScoreDocs != null)
                    {
                        for (int hitIDX = 0; hitIDX < topHits.ScoreDocs.Length; hitIDX++)
                        {
                            ScoreDoc sd = topHits.ScoreDocs[hitIDX];
                            Console.WriteLine("    doc=" + sd.Doc + " score=" + sd.Score);
                        }
                    }
                }

                // ... then all shards:
                Weight w = searcher.CreateNormalizedWeight(query);

                TopDocs[] shardHits = new TopDocs[subSearchers.Length];
                for (int shardIDX = 0; shardIDX < subSearchers.Length; shardIDX++)
                {
                    TopDocs subHits;
                    ShardSearcher subSearcher = subSearchers[shardIDX];
                    if (sort == null)
                    {
                        subHits = subSearcher.Search(w, numHits);
                    }
                    else
                    {
                        TopFieldCollector c = TopFieldCollector.Create(sort, numHits, true, true, true, Random().NextBoolean());
                        subSearcher.Search(w, c);
                        subHits = c.TopDocs(0, numHits);
                    }

                    shardHits[shardIDX] = subHits;
                    if (VERBOSE)
                    {
                        Console.WriteLine("  shard=" + shardIDX + " " + subHits.TotalHits + " totalHits hits=" + (subHits.ScoreDocs == null ? "null" : subHits.ScoreDocs.Length.ToString()));
                        if (subHits.ScoreDocs != null)
                        {
                            foreach (ScoreDoc sd in subHits.ScoreDocs)
                            {
                                Console.WriteLine("    doc=" + sd.Doc + " score=" + sd.Score);
                            }
                        }
                    }
                }

                // Merge:
                TopDocs mergedHits;
                if (useFrom)
                {
                    mergedHits = TopDocs.Merge(sort, from, size, shardHits);
                }
                else
                {
                    mergedHits = TopDocs.Merge(sort, numHits, shardHits);
                }

                if (mergedHits.ScoreDocs != null)
                {
                    // Make sure the returned shards are correct:
                    for (int hitIDX = 0; hitIDX < mergedHits.ScoreDocs.Length; hitIDX++)
                    {
                        ScoreDoc sd = mergedHits.ScoreDocs[hitIDX];
                        Assert.AreEqual(ReaderUtil.SubIndex(sd.Doc, docStarts), sd.ShardIndex, "doc=" + sd.Doc + " wrong shard");
                    }
                }

                TestUtil.AssertEquals(topHits, mergedHits);
            }
            reader.Dispose();
            dir.Dispose();
        }
    }
}