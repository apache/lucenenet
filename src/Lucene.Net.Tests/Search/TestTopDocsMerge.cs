using Lucene.Net.Documents;
using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Text;
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
    using CompositeReaderContext = Lucene.Net.Index.CompositeReaderContext;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using Int32Field = Int32Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using ReaderUtil = Lucene.Net.Index.ReaderUtil;
    using SingleField = SingleField;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestTopDocsMerge : LuceneTestCase
    {
        private class ShardSearcher : IndexSearcher
        {
            private readonly IList<AtomicReaderContext> ctx;

            public ShardSearcher(AtomicReaderContext ctx, IndexReaderContext parent)
                : base(parent)
            {
                this.ctx = new JCG.List<AtomicReaderContext> { ctx };
            }

            public virtual void Search(Weight weight, ICollector collector)
            {
                Search(ctx, weight, collector);
            }

            public virtual TopDocs Search(Weight weight, int topN)
            {
                return Search(ctx, weight, null, topN);
            }

            public override string ToString()
            {
                return "ShardSearcher(" + ctx[0] + ")";
            }
        }

        [Test]
        public virtual void TestSort_1()
        {
            // LUCENENET specific: NUnit will crash with an OOM if we do the full test
            // with verbosity enabled. So, making this a manual setting that can be
            // turned on if, and only if, needed for debugging. If the setting is turned
            // on, we are decresing the number of docs to 50, which seems to
            // keep it from crashing.
            bool isVerbose = false;

            TestSort(false, isVerbose);
        }

        [Test]
        public virtual void TestSort_2()
        {
            // LUCENENET specific: NUnit will crash with an OOM if we do the full test
            // with verbosity enabled. So, making this a manual setting that can be
            // turned on if, and only if, needed for debugging. If the setting is turned
            // on, we are decresing the number of docs to 50, which seems to
            // keep it from crashing.
            bool isVerbose = false;

            TestSort(true, isVerbose);
        }

        internal virtual void TestSort(bool useFrom, bool VERBOSE)
        {
            IndexReader reader = null;
            Directory dir = null;

            if (!VERBOSE)
            {
                Console.WriteLine("Verbosity disabled. Enable manually if needed.");
            }

            int numDocs = VERBOSE ? AtLeast(50) : AtLeast(1000);
            //final int numDocs = AtLeast(50);

            string[] tokens = new string[] { "a", "b", "c", "d", "e" };

            if (VERBOSE)
            {
                Console.WriteLine("TEST: make index");
            }

            {
                dir = NewDirectory();
                RandomIndexWriter w = new RandomIndexWriter(Random, dir);
                // w.setDoRandomForceMerge(false);

                // w.w.getConfig().SetMaxBufferedDocs(AtLeast(100));

                string[] content = new string[AtLeast(20)];

                for (int contentIDX = 0; contentIDX < content.Length; contentIDX++)
                {
                    StringBuilder sb = new StringBuilder();
                    int numTokens = TestUtil.NextInt32(Random, 1, 10);
                    for (int tokenIDX = 0; tokenIDX < numTokens; tokenIDX++)
                    {
                        sb.Append(tokens[Random.Next(tokens.Length)]).Append(' ');
                    }
                    content[contentIDX] = sb.ToString();
                }

                for (int docIDX = 0; docIDX < numDocs; docIDX++)
                {
                    Document doc = new Document();
                    doc.Add(NewStringField("string", TestUtil.RandomRealisticUnicodeString(Random), Field.Store.NO));
                    doc.Add(NewTextField("text", content[Random.Next(content.Length)], Field.Store.NO));
                    doc.Add(new SingleField("float", (float)Random.NextDouble(), Field.Store.NO));
                    int intValue;
                    if (Random.Next(100) == 17)
                    {
                        intValue = int.MinValue;
                    }
                    else if (Random.Next(100) == 17)
                    {
                        intValue = int.MaxValue;
                    }
                    else
                    {
                        intValue = Random.Next();
                    }
                    doc.Add(new Int32Field("int", intValue, Field.Store.NO));
                    if (VERBOSE)
                    {
                        Console.WriteLine("  doc=" + doc);
                    }
                    w.AddDocument(doc);
                }

                reader = w.GetReader();
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

            IList<SortField> sortFields = new JCG.List<SortField>();
            sortFields.Add(new SortField("string", SortFieldType.STRING, true));
            sortFields.Add(new SortField("string", SortFieldType.STRING, false));
            sortFields.Add(new SortField("int", SortFieldType.INT32, true));
            sortFields.Add(new SortField("int", SortFieldType.INT32, false));
            sortFields.Add(new SortField("float", SortFieldType.SINGLE, true));
            sortFields.Add(new SortField("float", SortFieldType.SINGLE, false));
            sortFields.Add(new SortField(null, SortFieldType.SCORE, true));
            sortFields.Add(new SortField(null, SortFieldType.SCORE, false));
            sortFields.Add(new SortField(null, SortFieldType.DOC, true));
            sortFields.Add(new SortField(null, SortFieldType.DOC, false));

            for (int iter = 0; iter < 1000 * RandomMultiplier; iter++)
            {
                // TODO: custom FieldComp...
                Query query = new TermQuery(new Term("text", tokens[Random.Next(tokens.Length)]));

                Sort sort;
                if (Random.Next(10) == 4)
                {
                    // Sort by score
                    sort = null;
                }
                else
                {
                    SortField[] randomSortFields = new SortField[TestUtil.NextInt32(Random, 1, 3)];
                    for (int sortIDX = 0; sortIDX < randomSortFields.Length; sortIDX++)
                    {
                        randomSortFields[sortIDX] = sortFields[Random.Next(sortFields.Count)];
                    }
                    sort = new Sort(randomSortFields);
                }

                int numHits = TestUtil.NextInt32(Random, 1, numDocs + 5);
                //final int numHits = 5;

                if (VERBOSE)
                {
                    Console.WriteLine("TEST: search query=" + query + " sort=" + sort + " numHits=" + numHits);
                }

                int from = -1;
                int size = -1;
                // First search on whole index:
                TopDocs topHits;
                if (sort is null)
                {
                    if (useFrom)
                    {
                        TopScoreDocCollector c = TopScoreDocCollector.Create(numHits, Random.NextBoolean());
                        searcher.Search(query, c);
                        from = TestUtil.NextInt32(Random, 0, numHits - 1);
                        size = numHits - from;
                        TopDocs tempTopHits = c.GetTopDocs();
                        if (from < tempTopHits.ScoreDocs.Length)
                        {
                            // Can't use TopDocs#topDocs(start, howMany), since it has different behaviour when start >= hitCount
                            // than TopDocs#merge currently has
                            ScoreDoc[] newScoreDocs = new ScoreDoc[Math.Min(size, tempTopHits.ScoreDocs.Length - from)];
                            Arrays.Copy(tempTopHits.ScoreDocs, from, newScoreDocs, 0, newScoreDocs.Length);
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
                    TopFieldCollector c = TopFieldCollector.Create(sort, numHits, true, true, true, Random.NextBoolean());
                    searcher.Search(query, c);
                    if (useFrom)
                    {
                        from = TestUtil.NextInt32(Random, 0, numHits - 1);
                        size = numHits - from;
                        TopDocs tempTopHits = c.GetTopDocs();
                        if (from < tempTopHits.ScoreDocs.Length)
                        {
                            // Can't use TopDocs#topDocs(start, howMany), since it has different behaviour when start >= hitCount
                            // than TopDocs#merge currently has
                            ScoreDoc[] newScoreDocs = new ScoreDoc[Math.Min(size, tempTopHits.ScoreDocs.Length - from)];
                            Arrays.Copy(tempTopHits.ScoreDocs, from, newScoreDocs, 0, newScoreDocs.Length);
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
                        topHits = c.GetTopDocs(0, numHits);
                    }
                }

                if (VERBOSE)
                {
                    if (useFrom)
                    {
                        Console.WriteLine("from=" + from + " size=" + size);
                    }
                    Console.WriteLine("  top search: " + topHits.TotalHits + " totalHits; hits=" + (topHits.ScoreDocs is null ? "null" : topHits.ScoreDocs.Length + " maxScore=" + topHits.MaxScore));
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
                    if (sort is null)
                    {
                        subHits = subSearcher.Search(w, numHits);
                    }
                    else
                    {
                        TopFieldCollector c = TopFieldCollector.Create(sort, numHits, true, true, true, Random.NextBoolean());
                        subSearcher.Search(w, c);
                        subHits = c.GetTopDocs(0, numHits);
                    }

                    shardHits[shardIDX] = subHits;
                    if (VERBOSE)
                    {
                        Console.WriteLine("  shard=" + shardIDX + " " + subHits.TotalHits + " totalHits hits=" + (subHits.ScoreDocs is null ? "null" : subHits.ScoreDocs.Length.ToString()));
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