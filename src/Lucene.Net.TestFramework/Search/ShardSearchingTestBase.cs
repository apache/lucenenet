using J2N.Collections.Generic.Extensions;
using J2N.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif

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

    // TODO: maybe SLM should throw this instead of returning null...
    /// <summary>
    /// Thrown when the lease for a searcher has expired.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public class SearcherExpiredException : Exception, IRuntimeException // LUCENENET specific: Added IRuntimeException for identification of the Java superclass in .NET
    {
        public SearcherExpiredException(string message)
            : base(message)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected SearcherExpiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    // TODO
    //   - doc blocks?  so we can test joins/grouping...
    //   - controlled consistency (NRTMgr)

    /// <summary>
    /// Base test class for simulating distributed search across multiple shards.
    /// </summary>
    // LUCENENET specific - Specify to unzip the line file docs
    [UseTempLineDocsFile]
    public abstract class ShardSearchingTestBase : LuceneTestCase
    {
        // LUCENENET specific - de-nested SearcherExpiredException

        internal class FieldAndShardVersion
        {
            private readonly long version;
            private readonly int nodeID;
            private readonly string field;

            public FieldAndShardVersion(int nodeID, long version, string field)
            {
                this.nodeID = nodeID;
                this.version = version;
                this.field = field;
            }

            public override int GetHashCode()
            {
                return (int)(version * nodeID + field.GetHashCode());
            }

            public override bool Equals(object other)
            {
                if (!(other is FieldAndShardVersion))
                {
                    return false;
                }

                FieldAndShardVersion other_ = (FieldAndShardVersion)other;

                return field.Equals(other_.field, StringComparison.Ordinal) && version == other_.version && nodeID == other_.nodeID;
            }

            public override string ToString()
            {
                return "FieldAndShardVersion(field=" + field + " nodeID=" + nodeID + " version=" + version + ")";
            }
        }

        internal class TermAndShardVersion
        {
            private readonly long version;
            private readonly int nodeID;
            private readonly Term term;

            public TermAndShardVersion(int nodeID, long version, Term term)
            {
                this.nodeID = nodeID;
                this.version = version;
                this.term = term;
            }

            public override int GetHashCode()
            {
                return (int)(version * nodeID + term.GetHashCode());
            }

            public override bool Equals(object other)
            {
                if (!(other is TermAndShardVersion))
                {
                    return false;
                }

                TermAndShardVersion other_ = (TermAndShardVersion)other;

                return term.Equals(other_.term) && version == other_.version && nodeID == other_.nodeID;
            }
        }

        // We share collection stats for these fields on each node
        // reopen:
        private readonly string[] fieldsToShare = new string[] { "body", "title" };

        // Called by one node once it has reopened, to notify all
        // other nodes.  this is just a mock (since it goes and
        // directly updates all other nodes, in RAM)... in a real
        // env this would hit the wire, sending version &
        // collection stats to all other nodes:
        internal virtual void BroadcastNodeReopen(int nodeID, long version, IndexSearcher newSearcher)
        {
            if (Verbose)
            {
                Console.WriteLine("REOPEN: nodeID=" + nodeID + " version=" + version + " maxDoc=" + newSearcher.IndexReader.MaxDoc);
            }

            // Broadcast new collection stats for this node to all
            // other nodes:
            foreach (string field in fieldsToShare)
            {
                CollectionStatistics stats = newSearcher.CollectionStatistics(field);
                foreach (NodeState node in m_nodes)
                {
                    // Don't put my own collection stats into the cache;
                    // we pull locally:
                    if (node.MyNodeID != nodeID)
                    {
                        node.collectionStatsCache[new FieldAndShardVersion(nodeID, version, field)] = stats;
                    }
                }
            }
            foreach (NodeState node in m_nodes)
            {
                node.UpdateNodeVersion(nodeID, version);
            }
        }

        // TODO: broadcastNodeExpire?  then we can purge the
        // known-stale cache entries...

        // MOCK: in a real env you have to hit the wire
        // (send this query to all remote nodes
        // concurrently):
        internal virtual TopDocs SearchNode(int nodeID, long[] nodeVersions, Query q, Sort sort, int numHits, ScoreDoc searchAfter)
        {
            NodeState.ShardIndexSearcher s = m_nodes[nodeID].Acquire(nodeVersions);
            try
            {
                if (sort is null)
                {
                    if (searchAfter != null)
                    {
                        return s.LocalSearchAfter(searchAfter, q, numHits);
                    }
                    else
                    {
                        return s.LocalSearch(q, numHits);
                    }
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(searchAfter is null); // not supported yet
                    return s.LocalSearch(q, numHits, sort);
                }
            }
            finally
            {
                NodeState.Release(s); // LUCENENET: made static per CA1822 and eliminated array lookup
            }
        }

        // Mock: in a real env, this would hit the wire and get
        // term stats from remote node
        internal virtual IDictionary<Term, TermStatistics> GetNodeTermStats(ISet<Term> terms, int nodeID, long version)
        {
            NodeState node = m_nodes[nodeID];
            IDictionary<Term, TermStatistics> stats = new Dictionary<Term, TermStatistics>();
            IndexSearcher s = node.Searchers.Acquire(version);
            if (s is null)
            {
                throw new SearcherExpiredException("node=" + nodeID + " version=" + version);
            }
            try
            {
                foreach (Term term in terms)
                {
                    TermContext termContext = TermContext.Build(s.IndexReader.Context, term);
                    stats[term] = s.TermStatistics(term, termContext);
                }
            }
            finally
            {
                node.Searchers.Release(s);
            }
            return stats;
        }

        protected sealed class NodeState : IDisposable
        {
            private readonly ShardSearchingTestBase outerInstance;

            public Directory Dir { get; private set; }
            public IndexWriter Writer { get; private set; }
            public SearcherLifetimeManager Searchers { get; private set; }
            public SearcherManager Mgr { get; private set; }
            public int MyNodeID { get; private set; }

            private readonly long[] currentNodeVersions;

            public long[] GetCurrentNodeVersions() // LUCENENET specific - made into a method so we don't expose a writable array to the outside world.
            {
                return (long[])currentNodeVersions.Clone();
            }

            // TODO: nothing evicts from here!!!  Somehow, on searcher
            // expiration on remote nodes we must evict from our
            // local cache...?  And still LRU otherwise (for the
            // still-live searchers).

            internal readonly IDictionary<FieldAndShardVersion, CollectionStatistics> collectionStatsCache = new ConcurrentDictionary<FieldAndShardVersion, CollectionStatistics>();
            internal readonly IDictionary<TermAndShardVersion, TermStatistics> termStatsCache = new ConcurrentDictionary<TermAndShardVersion, TermStatistics>();

            /// <summary>
            /// Matches docs in the local shard but scores based on
            /// aggregated stats ("mock distributed scoring") from all
            /// nodes.
            /// </summary>
            public class ShardIndexSearcher : IndexSearcher
            {
                private readonly ShardSearchingTestBase.NodeState outerInstance;

                // Version for the node searchers we search:
                private readonly long[] nodeVersions;

                public long[] GetNodeVersions() // LUCENENET specific - made into a method as per MSDN guidelines.
                {
                    return nodeVersions.ToArray();
                }

                public int MyNodeID { get; private set; }

                public ShardIndexSearcher(ShardSearchingTestBase.NodeState nodeState, long[] nodeVersions, IndexReader localReader, int nodeID)
                    : base(localReader)
                {
                    this.outerInstance = nodeState;
                    this.nodeVersions = nodeVersions;
                    MyNodeID = nodeID;
                    if (Debugging.AssertsEnabled) Debugging.Assert(MyNodeID == nodeState.MyNodeID, "myNodeID={0} nodeState.MyNodeID={1}", nodeID, nodeState.MyNodeID);
                }

                public override Query Rewrite(Query original)
                {
                    Query rewritten = base.Rewrite(original);
                    ISet<Term> terms = new JCG.HashSet<Term>();
                    rewritten.ExtractTerms(terms);

                    // Make a single request to remote nodes for term
                    // stats:
                    for (int nodeID = 0; nodeID < nodeVersions.Length; nodeID++)
                    {
                        if (nodeID == MyNodeID)
                        {
                            continue;
                        }

                        ISet<Term> missing = new JCG.HashSet<Term>();
                        foreach (Term term in terms)
                        {
                            TermAndShardVersion key = new TermAndShardVersion(nodeID, nodeVersions[nodeID], term);
                            if (!outerInstance.termStatsCache.ContainsKey(key))
                            {
                                missing.Add(term);
                            }
                        }
                        if (missing.Count != 0)
                        {
                            foreach (KeyValuePair<Term, TermStatistics> ent in outerInstance.outerInstance.GetNodeTermStats(missing, nodeID, nodeVersions[nodeID]))
                            {
                                TermAndShardVersion key = new TermAndShardVersion(nodeID, nodeVersions[nodeID], ent.Key);
                                outerInstance.termStatsCache[key] = ent.Value;
                            }
                        }
                    }

                    return rewritten;
                }

                public override TermStatistics TermStatistics(Term term, TermContext context)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(term != null);
                    long docFreq = 0;
                    long totalTermFreq = 0;
                    for (int nodeID = 0; nodeID < nodeVersions.Length; nodeID++)
                    {
                        TermStatistics subStats;
                        if (nodeID == MyNodeID)
                        {
                            subStats = base.TermStatistics(term, context);
                        }
                        else
                        {
                            TermAndShardVersion key = new TermAndShardVersion(nodeID, nodeVersions[nodeID], term);
                            subStats = outerInstance.termStatsCache[key];
                            // We pre-cached during rewrite so all terms
                            // better be here...
                            if (Debugging.AssertsEnabled) Debugging.Assert(subStats != null);
                        }

                        long nodeDocFreq = subStats.DocFreq;
                        if (docFreq >= 0 && nodeDocFreq >= 0)
                        {
                            docFreq += nodeDocFreq;
                        }
                        else
                        {
                            docFreq = -1;
                        }

                        long nodeTotalTermFreq = subStats.TotalTermFreq;
                        if (totalTermFreq >= 0 && nodeTotalTermFreq >= 0)
                        {
                            totalTermFreq += nodeTotalTermFreq;
                        }
                        else
                        {
                            totalTermFreq = -1;
                        }
                    }

                    return new TermStatistics(term.Bytes, docFreq, totalTermFreq);
                }

                public override CollectionStatistics CollectionStatistics(string field)
                {
                    // TODO: we could compute this on init and cache,
                    // since we are re-inited whenever any nodes have a
                    // new reader
                    long docCount = 0;
                    long sumTotalTermFreq = 0;
                    long sumDocFreq = 0;
                    long maxDoc = 0;

                    for (int nodeID = 0; nodeID < nodeVersions.Length; nodeID++)
                    {
                        FieldAndShardVersion key = new FieldAndShardVersion(nodeID, nodeVersions[nodeID], field);
                        CollectionStatistics nodeStats;
                        if (nodeID == MyNodeID)
                        {
                            nodeStats = base.CollectionStatistics(field);
                        }
                        else
                        {
                            outerInstance.collectionStatsCache.TryGetValue(key, out nodeStats);
                        }
                        if (nodeStats is null)
                        {
                            Console.WriteLine("coll stats myNodeID=" + MyNodeID + ": " + Collections.ToString(outerInstance.collectionStatsCache.Keys));
                        }
                        // Collection stats are pre-shared on reopen, so,
                        // we better not have a cache miss:
                        if (Debugging.AssertsEnabled) Debugging.Assert(nodeStats != null, "myNodeID={0} nodeID={1} version={2} field={3}", MyNodeID, nodeID, nodeVersions[nodeID], field);

                        long nodeDocCount = nodeStats.DocCount;
                        if (docCount >= 0 && nodeDocCount >= 0)
                        {
                            docCount += nodeDocCount;
                        }
                        else
                        {
                            docCount = -1;
                        }

                        long nodeSumTotalTermFreq = nodeStats.SumTotalTermFreq;
                        if (sumTotalTermFreq >= 0 && nodeSumTotalTermFreq >= 0)
                        {
                            sumTotalTermFreq += nodeSumTotalTermFreq;
                        }
                        else
                        {
                            sumTotalTermFreq = -1;
                        }

                        long nodeSumDocFreq = nodeStats.SumDocFreq;
                        if (sumDocFreq >= 0 && nodeSumDocFreq >= 0)
                        {
                            sumDocFreq += nodeSumDocFreq;
                        }
                        else
                        {
                            sumDocFreq = -1;
                        }

                        if (Debugging.AssertsEnabled) Debugging.Assert(nodeStats.MaxDoc >= 0);
                        maxDoc += nodeStats.MaxDoc;
                    }

                    return new CollectionStatistics(field, maxDoc, docCount, sumTotalTermFreq, sumDocFreq);
                }

                public override TopDocs Search(Query query, int numHits)
                {
                    TopDocs[] shardHits = new TopDocs[nodeVersions.Length];
                    for (int nodeID = 0; nodeID < nodeVersions.Length; nodeID++)
                    {
                        if (nodeID == MyNodeID)
                        {
                            // My node; run using local shard searcher we
                            // already aquired:
                            shardHits[nodeID] = LocalSearch(query, numHits);
                        }
                        else
                        {
                            shardHits[nodeID] = outerInstance.outerInstance.SearchNode(nodeID, nodeVersions, query, null, numHits, null);
                        }
                    }

                    // Merge:
                    return TopDocs.Merge(null, numHits, shardHits);
                }

                public virtual TopDocs LocalSearch(Query query, int numHits)
                {
                    return base.Search(query, numHits);
                }

                public override TopDocs SearchAfter(ScoreDoc after, Query query, int numHits)
                {
                    TopDocs[] shardHits = new TopDocs[nodeVersions.Length];
                    // results are merged in that order: score, shardIndex, doc. therefore we set
                    // after to after.Score and depending on the nodeID we set doc to either:
                    // - not collect any more documents with that score (only with worse score)
                    // - collect more documents with that score (and worse) following the last collected document
                    // - collect all documents with that score (and worse)
                    ScoreDoc shardAfter = new ScoreDoc(after.Doc, after.Score);
                    for (int nodeID = 0; nodeID < nodeVersions.Length; nodeID++)
                    {
                        if (nodeID < after.ShardIndex)
                        {
                            // all documents with after.Score were already collected, so collect
                            // only documents with worse scores.
                            NodeState.ShardIndexSearcher s = outerInstance.outerInstance.m_nodes[nodeID].Acquire(nodeVersions);
                            try
                            {
                                // Setting after.Doc to reader.MaxDoc-1 is a way to tell
                                // TopScoreDocCollector that no more docs with that score should
                                // be collected. note that in practice the shard which sends the
                                // request to a remote shard won't have reader.MaxDoc at hand, so
                                // it will send some arbitrary value which will be fixed on the
                                // other end.
                                shardAfter.Doc = s.IndexReader.MaxDoc - 1;
                            }
                            finally
                            {
                                Release(s); // LUCENENET: Made static per CA1822 and eliminated array lookup
                            }
                        }
                        else if (nodeID == after.ShardIndex)
                        {
                            // collect all documents following the last collected doc with
                            // after.Score + documents with worse scores.
                            shardAfter.Doc = after.Doc;
                        }
                        else
                        {
                            // all documents with after.Score (and worse) should be collected
                            // because they didn't make it to top-N in the previous round.
                            shardAfter.Doc = -1;
                        }
                        if (nodeID == MyNodeID)
                        {
                            // My node; run using local shard searcher we
                            // already aquired:
                            shardHits[nodeID] = LocalSearchAfter(shardAfter, query, numHits);
                        }
                        else
                        {
                            shardHits[nodeID] = outerInstance.outerInstance.SearchNode(nodeID, nodeVersions, query, null, numHits, shardAfter);
                        }
                        //System.out.println("  node=" + nodeID + " totHits=" + shardHits[nodeID].TotalHits);
                    }

                    // Merge:
                    return TopDocs.Merge(null, numHits, shardHits);
                }

                public virtual TopDocs LocalSearchAfter(ScoreDoc after, Query query, int numHits)
                {
                    return base.SearchAfter(after, query, numHits);
                }

                public override TopFieldDocs Search(Query query, int numHits, Sort sort)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(sort != null);
                    TopDocs[] shardHits = new TopDocs[nodeVersions.Length];
                    for (int nodeID = 0; nodeID < nodeVersions.Length; nodeID++)
                    {
                        if (nodeID == MyNodeID)
                        {
                            // My node; run using local shard searcher we
                            // already aquired:
                            shardHits[nodeID] = LocalSearch(query, numHits, sort);
                        }
                        else
                        {
                            shardHits[nodeID] = outerInstance.outerInstance.SearchNode(nodeID, nodeVersions, query, sort, numHits, null);
                        }
                    }

                    // Merge:
                    return (TopFieldDocs)TopDocs.Merge(sort, numHits, shardHits);
                }

                public virtual TopFieldDocs LocalSearch(Query query, int numHits, Sort sort)
                {
                    return base.Search(query, numHits, sort);
                }
            }

            internal volatile ShardIndexSearcher currentShardSearcher;

#pragma warning disable IDE0060 // Remove unused parameter
            public NodeState(ShardSearchingTestBase shardSearchingTestBase, Random random, int nodeID, int numNodes)
#pragma warning restore IDE0060 // Remove unused parameter
            {
                this.outerInstance = shardSearchingTestBase;
                MyNodeID = nodeID;
                Dir = NewFSDirectory(CreateTempDir("ShardSearchingTestBase"));
                // TODO: set warmer
                MockAnalyzer analyzer = new MockAnalyzer(LuceneTestCase.Random);
                analyzer.MaxTokenLength = TestUtil.NextInt32(LuceneTestCase.Random, 1, IndexWriter.MAX_TERM_LENGTH);
                IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
                iwc.SetOpenMode(OpenMode.CREATE);
                if (Verbose)
                {
                    iwc.SetInfoStream(new TextWriterInfoStream(Console.Out));
                }
                Writer = new IndexWriter(Dir, iwc);
                Mgr = new SearcherManager(Writer, true, null);
                Searchers = new SearcherLifetimeManager();

                // Init w/ 0s... caller above will do initial
                // "broadcast" by calling initSearcher:
                currentNodeVersions = new long[numNodes];
            }

            public void InitSearcher(long[] nodeVersions)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(currentShardSearcher is null);
                Arrays.Copy(nodeVersions, 0, currentNodeVersions, 0, currentNodeVersions.Length);
                currentShardSearcher = new ShardIndexSearcher(this, GetCurrentNodeVersions(), Mgr.Acquire().IndexReader, MyNodeID);
            }

            public void UpdateNodeVersion(int nodeID, long version)
            {
                currentNodeVersions[nodeID] = version;
                if (currentShardSearcher != null)
                {
                    currentShardSearcher.IndexReader.DecRef();
                }
                currentShardSearcher = new ShardIndexSearcher(this, GetCurrentNodeVersions(), Mgr.Acquire().IndexReader, MyNodeID);
            }

            // Get the current (fresh) searcher for this node
            public ShardIndexSearcher Acquire()
            {
                while (true)
                {
                    ShardIndexSearcher s = currentShardSearcher;
                    // In theory the reader could get decRef'd to 0
                    // before we have a chance to incRef, ie if a reopen
                    // happens right after the above line, this thread
                    // gets stalled, and the old IR is closed.  So we
                    // must try/retry until incRef succeeds:
                    if (s.IndexReader.TryIncRef())
                    {
                        return s;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Release(ShardIndexSearcher s) // LUCENENET: CA1822: Mark members as static
            {
                s.IndexReader.DecRef();
            }

            // Get and old searcher matching the specified versions:
            public ShardIndexSearcher Acquire(long[] nodeVersions)
            {
                IndexSearcher s = Searchers.Acquire(nodeVersions[MyNodeID]);
                if (s is null)
                {
                    throw new SearcherExpiredException("nodeID=" + MyNodeID + " version=" + nodeVersions[MyNodeID]);
                }
                return new ShardIndexSearcher(this, nodeVersions, s.IndexReader, MyNodeID);
            }

            // Reopen local reader
            public void Reopen()
            {
                IndexSearcher before = Mgr.Acquire();
                Mgr.Release(before);

                Mgr.MaybeRefresh();
                IndexSearcher after = Mgr.Acquire();
                try
                {
                    if (after != before)
                    {
                        // New searcher was opened
                        long version = Searchers.Record(after);
                        Searchers.Prune(new SearcherLifetimeManager.PruneByAge(outerInstance.maxSearcherAgeSeconds));
                        outerInstance.BroadcastNodeReopen(MyNodeID, version, after);
                    }
                }
                finally
                {
                    Mgr.Release(after);
                }
            }

            public void Dispose()
            {
                if (currentShardSearcher != null)
                {
                    currentShardSearcher.IndexReader.DecRef();
                }
                Searchers.Dispose();
                Mgr.Dispose();
                Writer.Dispose();
                Dir.Dispose();
            }
        }

        // TODO: make this more realistic, ie, each node should
        // have its own thread, so we have true node to node
        // concurrency
        private sealed class ChangeIndices : ThreadJob
        {
            private readonly ShardSearchingTestBase outerInstance;

            public ChangeIndices(ShardSearchingTestBase outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override void Run()
            {
                try
                {
                    LineFileDocs docs = new LineFileDocs(Random, DefaultCodecSupportsDocValues);
                    int numDocs = 0;
                    while (J2N.Time.NanoTime() < outerInstance.endTimeNanos)
                    {
                        int what = Random.Next(3);
                        NodeState node = outerInstance.m_nodes[Random.Next(outerInstance.m_nodes.Length)];
                        if (numDocs == 0 || what == 0)
                        {
                            node.Writer.AddDocument(docs.NextDoc());
                            numDocs++;
                        }
                        else if (what == 1)
                        {
                            node.Writer.UpdateDocument(new Term("docid", "" + Random.Next(numDocs)), docs.NextDoc());
                            numDocs++;
                        }
                        else
                        {
                            node.Writer.DeleteDocuments(new Term("docid", "" + Random.Next(numDocs)));
                        }
                        // TODO: doc blocks too

                        if (Random.Next(17) == 12)
                        {
                            node.Writer.Commit();
                        }

                        if (Random.Next(17) == 12)
                        {
                            outerInstance.m_nodes[Random.Next(outerInstance.m_nodes.Length)].Reopen();
                        }
                    }
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    Console.WriteLine("FAILED:");
                    Console.Out.WriteLine(t.StackTrace);
                    throw RuntimeException.Create(t);
                }
            }
        }

        protected NodeState[] m_nodes;
        internal int maxSearcherAgeSeconds;
        internal long endTimeNanos;
        private ThreadJob changeIndicesThread;

        protected virtual void Start(int numNodes, double runTimeSec, int maxSearcherAgeSeconds)
        {
            endTimeNanos = J2N.Time.NanoTime() + (long)(runTimeSec * 1000000000);
            this.maxSearcherAgeSeconds = maxSearcherAgeSeconds;

            m_nodes = new NodeState[numNodes];
            for (int nodeID = 0; nodeID < numNodes; nodeID++)
            {
                m_nodes[nodeID] = new NodeState(this, Random, nodeID, numNodes);
            }

            long[] nodeVersions = new long[m_nodes.Length];
            for (int nodeID = 0; nodeID < numNodes; nodeID++)
            {
                IndexSearcher s = m_nodes[nodeID].Mgr.Acquire();
                try
                {
                    nodeVersions[nodeID] = m_nodes[nodeID].Searchers.Record(s);
                }
                finally
                {
                    m_nodes[nodeID].Mgr.Release(s);
                }
            }

            for (int nodeID = 0; nodeID < numNodes; nodeID++)
            {
                IndexSearcher s = m_nodes[nodeID].Mgr.Acquire();
                if (Debugging.AssertsEnabled) Debugging.Assert(nodeVersions[nodeID] == m_nodes[nodeID].Searchers.Record(s));
                if (Debugging.AssertsEnabled) Debugging.Assert(s != null);
                try
                {
                    BroadcastNodeReopen(nodeID, nodeVersions[nodeID], s);
                }
                finally
                {
                    m_nodes[nodeID].Mgr.Release(s);
                }
            }

            changeIndicesThread = new ChangeIndices(this);
            changeIndicesThread.Start();
        }

        protected virtual void Finish()
        {
            changeIndicesThread.Join();
            foreach (NodeState node in m_nodes)
            {
                node.Dispose();
            }
        }

        /// <summary>
        /// An <see cref="IndexSearcher"/> and associated version (lease)
        /// </summary>
        protected class SearcherAndVersion
        {
            public IndexSearcher Searcher { get; private set; }
            public long Version { get; private set; }

            public SearcherAndVersion(IndexSearcher searcher, long version)
            {
                this.Searcher = searcher;
                this.Version = version;
            }
        }
    }
}