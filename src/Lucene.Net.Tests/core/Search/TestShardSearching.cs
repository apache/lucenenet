using System;
using System.Collections.Generic;

namespace Lucene.Net.Search
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using MultiReader = Lucene.Net.Index.MultiReader;
    using Term = Lucene.Net.Index.Term;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestUtil = Lucene.Net.Util.TestUtil;

    // TODO
    //   - other queries besides PrefixQuery & TermQuery (but:
    //     FuzzyQ will be problematic... the top N terms it
    //     takes means results will differ)
    //   - NRQ/F
    //   - BQ, negated clauses, negated prefix clauses
    //   - test pulling docs in 2nd round trip...
    //   - filter too

    [SuppressCodecs("SimpleText", "Memory", "Direct")]
    [TestFixture]
    public class TestShardSearching : ShardSearchingTestBase
    {
        private class PreviousSearchState
        {
            public readonly long SearchTimeNanos;
            public readonly long[] Versions;
            public readonly ScoreDoc SearchAfterLocal;
            public readonly ScoreDoc SearchAfterShard;
            public readonly Sort Sort;
            public readonly Query Query;
            public readonly int NumHitsPaged;

            public PreviousSearchState(Query query, Sort sort, ScoreDoc searchAfterLocal, ScoreDoc searchAfterShard, long[] versions, int numHitsPaged)
            {
                this.Versions = (long[])versions.Clone();
                this.SearchAfterLocal = searchAfterLocal;
                this.SearchAfterShard = searchAfterShard;
                this.Sort = sort;
                this.Query = query;
                this.NumHitsPaged = numHitsPaged;
                SearchTimeNanos = TimeHelper.NanoTime();
            }
        }

        [Test]
        public virtual void TestSimple()
        {
            int numNodes = TestUtil.NextInt(Random(), 1, 10);

            double runTimeSec = AtLeast(3);

            int minDocsToMakeTerms = TestUtil.NextInt(Random(), 5, 20);

            int maxSearcherAgeSeconds = TestUtil.NextInt(Random(), 1, 3);

            if (VERBOSE)
            {
                Console.WriteLine("TEST: numNodes=" + numNodes + " runTimeSec=" + runTimeSec + " maxSearcherAgeSeconds=" + maxSearcherAgeSeconds);
            }

            Start(numNodes, runTimeSec, maxSearcherAgeSeconds);

            List<PreviousSearchState> priorSearches = new List<PreviousSearchState>();
            List<BytesRef> terms = null;
            while (DateTime.UtcNow < EndTime)
            {
                bool doFollowon = priorSearches.Count > 0 && Random().Next(7) == 1;

                // Pick a random node; we will run the query on this node:
                int myNodeID = Random().Next(numNodes);

                NodeState.ShardIndexSearcher localShardSearcher;

                PreviousSearchState prevSearchState;

                if (doFollowon)
                {
                    // Pretend user issued a followon query:
                    prevSearchState = priorSearches[Random().Next(priorSearches.Count)];

                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: follow-on query age=" + ((TimeHelper.NanoTime() - prevSearchState.SearchTimeNanos) / 1000000000.0));
                    }

                    try
                    {
                        localShardSearcher = Nodes[myNodeID].Acquire(prevSearchState.Versions);
                    }
                    catch (SearcherExpiredException see)
                    {
                        // Expected, sometimes; in a "real" app we would
                        // either forward this error to the user ("too
                        // much time has passed; please re-run your
                        // search") or sneakily just switch to newest
                        // searcher w/o telling them...
                        if (VERBOSE)
                        {
                            Console.WriteLine("  searcher expired during local shard searcher init: " + see);
                        }
                        priorSearches.Remove(prevSearchState);
                        continue;
                    }
                }
                else
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: fresh query");
                    }
                    // Do fresh query:
                    localShardSearcher = Nodes[myNodeID].Acquire();
                    prevSearchState = null;
                }

                IndexReader[] subs = new IndexReader[numNodes];

                PreviousSearchState searchState = null;

                try
                {
                    // Mock: now make a single reader (MultiReader) from all node
                    // searchers.  In a real shard env you can't do this... we
                    // do it to confirm results from the shard searcher
                    // are correct:
                    int docCount = 0;
                    try
                    {
                        for (int nodeID = 0; nodeID < numNodes; nodeID++)
                        {
                            long subVersion = localShardSearcher.NodeVersions[nodeID];
                            IndexSearcher sub = Nodes[nodeID].Searchers.Acquire(subVersion);
                            if (sub == null)
                            {
                                nodeID--;
                                while (nodeID >= 0)
                                {
                                    subs[nodeID].DecRef();
                                    subs[nodeID] = null;
                                    nodeID--;
                                }
                                throw new SearcherExpiredException("nodeID=" + nodeID + " version=" + subVersion);
                            }
                            subs[nodeID] = sub.IndexReader;
                            docCount += subs[nodeID].MaxDoc;
                        }
                    }
                    catch (SearcherExpiredException see)
                    {
                        // Expected
                        if (VERBOSE)
                        {
                            Console.WriteLine("  searcher expired during mock reader init: " + see);
                        }
                        continue;
                    }

                    IndexReader mockReader = new MultiReader(subs);
                    IndexSearcher mockSearcher = new IndexSearcher(mockReader);

                    Query query;
                    Sort sort;

                    if (prevSearchState != null)
                    {
                        query = prevSearchState.Query;
                        sort = prevSearchState.Sort;
                    }
                    else
                    {
                        if (terms == null && docCount > minDocsToMakeTerms)
                        {
                            // TODO: try to "focus" on high freq terms sometimes too
                            // TODO: maybe also periodically reset the terms...?
                            TermsEnum termsEnum = MultiFields.GetTerms(mockReader, "body").Iterator(null);
                            terms = new List<BytesRef>();
                            while (termsEnum.Next() != null)
                            {
                                terms.Add(BytesRef.DeepCopyOf(termsEnum.Term()));
                            }
                            if (VERBOSE)
                            {
                                Console.WriteLine("TEST: init terms: " + terms.Count + " terms");
                            }
                            if (terms.Count == 0)
                            {
                                terms = null;
                            }
                        }

                        if (VERBOSE)
                        {
                            Console.WriteLine("  maxDoc=" + mockReader.MaxDoc);
                        }

                        if (terms != null)
                        {
                            if (Random().NextBoolean())
                            {
                                query = new TermQuery(new Term("body", terms[Random().Next(terms.Count)]));
                            }
                            else
                            {
                                string t = terms[Random().Next(terms.Count)].Utf8ToString();
                                string prefix;
                                if (t.Length <= 1)
                                {
                                    prefix = t;
                                }
                                else
                                {
                                    prefix = t.Substring(0, TestUtil.NextInt(Random(), 1, 2));
                                }
                                query = new PrefixQuery(new Term("body", prefix));
                            }

                            if (Random().NextBoolean())
                            {
                                sort = null;
                            }
                            else
                            {
                                // TODO: sort by more than 1 field
                                int what = Random().Next(3);
                                if (what == 0)
                                {
                                    sort = new Sort(SortField.FIELD_SCORE);
                                }
                                else if (what == 1)
                                {
                                    // TODO: this sort doesn't merge
                                    // correctly... it's tricky because you
                                    // could have > 2.1B docs across all shards:
                                    //sort = new Sort(SortField.FIELD_DOC);
                                    sort = null;
                                }
                                else if (what == 2)
                                {
                                    sort = new Sort(new SortField[] { new SortField("docid", SortField.Type_e.INT, Random().NextBoolean()) });
                                }
                                else
                                {
                                    sort = new Sort(new SortField[] { new SortField("title", SortField.Type_e.STRING, Random().NextBoolean()) });
                                }
                            }
                        }
                        else
                        {
                            query = null;
                            sort = null;
                        }
                    }

                    if (query != null)
                    {
                        try
                        {
                            searchState = AssertSame(mockSearcher, localShardSearcher, query, sort, prevSearchState);
                        }
                        catch (SearcherExpiredException see)
                        {
                            // Expected; in a "real" app we would
                            // either forward this error to the user ("too
                            // much time has passed; please re-run your
                            // search") or sneakily just switch to newest
                            // searcher w/o telling them...
                            if (VERBOSE)
                            {
                                Console.WriteLine("  searcher expired during search: " + see);
                                Console.Out.Write(see.StackTrace);
                            }
                            // We can't do this in general: on a very slow
                            // computer it's possible the local searcher
                            // expires before we can finish our search:
                            // assert prevSearchState != null;
                            if (prevSearchState != null)
                            {
                                priorSearches.Remove(prevSearchState);
                            }
                        }
                    }
                }
                finally
                {
                    Nodes[myNodeID].Release(localShardSearcher);
                    foreach (IndexReader sub in subs)
                    {
                        if (sub != null)
                        {
                            sub.DecRef();
                        }
                    }
                }

                if (searchState != null && searchState.SearchAfterLocal != null && Random().Next(5) == 3)
                {
                    priorSearches.Add(searchState);
                    if (priorSearches.Count > 200)
                    {
                        priorSearches = (List<PreviousSearchState>)CollectionsHelper.Shuffle(priorSearches);
                        priorSearches.SubList(100, priorSearches.Count).Clear();
                    }
                }
            }

            Finish();
        }

        private PreviousSearchState AssertSame(IndexSearcher mockSearcher, NodeState.ShardIndexSearcher shardSearcher, Query q, Sort sort, PreviousSearchState state)
        {
            int numHits = TestUtil.NextInt(Random(), 1, 100);
            if (state != null && state.SearchAfterLocal == null)
            {
                // In addition to what we last searched:
                numHits += state.NumHitsPaged;
            }

            if (VERBOSE)
            {
                Console.WriteLine("TEST: query=" + q + " sort=" + sort + " numHits=" + numHits);
                if (state != null)
                {
                    Console.WriteLine("  prev: searchAfterLocal=" + state.SearchAfterLocal + " searchAfterShard=" + state.SearchAfterShard + " numHitsPaged=" + state.NumHitsPaged);
                }
            }

            // Single (mock local) searcher:
            TopDocs hits;
            if (sort == null)
            {
                if (state != null && state.SearchAfterLocal != null)
                {
                    hits = mockSearcher.SearchAfter(state.SearchAfterLocal, q, numHits);
                }
                else
                {
                    hits = mockSearcher.Search(q, numHits);
                }
            }
            else
            {
                hits = mockSearcher.Search(q, numHits, sort);
            }

            // Shard searcher
            TopDocs shardHits;
            if (sort == null)
            {
                if (state != null && state.SearchAfterShard != null)
                {
                    shardHits = shardSearcher.SearchAfter(state.SearchAfterShard, q, numHits);
                }
                else
                {
                    shardHits = shardSearcher.Search(q, numHits);
                }
            }
            else
            {
                shardHits = shardSearcher.Search(q, numHits, sort);
            }

            int numNodes = shardSearcher.NodeVersions.Length;
            int[] @base = new int[numNodes];
            IList<IndexReaderContext> subs = mockSearcher.TopReaderContext.Children;
            Assert.AreEqual(numNodes, subs.Count);

            for (int nodeID = 0; nodeID < numNodes; nodeID++)
            {
                @base[nodeID] = subs[nodeID].DocBaseInParent;
            }

            if (VERBOSE)
            {
                /*
                for(int shardID=0;shardID<shardSearchers.Length;shardID++) {
                  System.out.println("  shard=" + shardID + " maxDoc=" + shardSearchers[shardID].searcher.getIndexReader().MaxDoc);
                }
                */
                Console.WriteLine("  single searcher: " + hits.TotalHits + " totalHits maxScore=" + hits.MaxScore);
                for (int i = 0; i < hits.ScoreDocs.Length; i++)
                {
                    ScoreDoc sd = hits.ScoreDocs[i];
                    Console.WriteLine("    doc=" + sd.Doc + " score=" + sd.Score);
                }
                Console.WriteLine("  shard searcher: " + shardHits.TotalHits + " totalHits maxScore=" + shardHits.MaxScore);
                for (int i = 0; i < shardHits.ScoreDocs.Length; i++)
                {
                    ScoreDoc sd = shardHits.ScoreDocs[i];
                    Console.WriteLine("    doc=" + sd.Doc + " (rebased: " + (sd.Doc + @base[sd.ShardIndex]) + ") score=" + sd.Score + " shard=" + sd.ShardIndex);
                }
            }

            int numHitsPaged;
            if (state != null && state.SearchAfterLocal != null)
            {
                numHitsPaged = hits.ScoreDocs.Length;
                if (state != null)
                {
                    numHitsPaged += state.NumHitsPaged;
                }
            }
            else
            {
                numHitsPaged = hits.ScoreDocs.Length;
            }

            bool moreHits;

            ScoreDoc bottomHit;
            ScoreDoc bottomHitShards;

            if (numHitsPaged < hits.TotalHits)
            {
                // More hits to page through
                moreHits = true;
                if (sort == null)
                {
                    bottomHit = hits.ScoreDocs[hits.ScoreDocs.Length - 1];
                    ScoreDoc sd = shardHits.ScoreDocs[shardHits.ScoreDocs.Length - 1];
                    // Must copy because below we rebase:
                    bottomHitShards = new ScoreDoc(sd.Doc, sd.Score, sd.ShardIndex);
                    if (VERBOSE)
                    {
                        Console.WriteLine("  save bottomHit=" + bottomHit);
                    }
                }
                else
                {
                    bottomHit = null;
                    bottomHitShards = null;
                }
            }
            else
            {
                Assert.AreEqual(hits.TotalHits, numHitsPaged);
                bottomHit = null;
                bottomHitShards = null;
                moreHits = false;
            }

            // Must rebase so Assert.AreEqual passes:
            for (int hitID = 0; hitID < shardHits.ScoreDocs.Length; hitID++)
            {
                ScoreDoc sd = shardHits.ScoreDocs[hitID];
                sd.Doc += @base[sd.ShardIndex];
            }

            TestUtil.AssertEquals(hits, shardHits);

            if (moreHits)
            {
                // Return a continuation:
                return new PreviousSearchState(q, sort, bottomHit, bottomHitShards, shardSearcher.NodeVersions, numHitsPaged);
            }
            else
            {
                return null;
            }
        }
    }
}