using Lucene.Net.Analysis;
using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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

    /// <summary>
    /// Read index (abstract) task.
    /// Sub classes implement <see cref="WithSearch"/>, <see cref="WithWarm"/>, <see cref="WithTraverse"/> and <see cref="WithRetrieve"/>
    /// </summary>
    /// <remarks>
    /// Note: All ReadTasks reuse the reader if it is already open.
    /// Otherwise a reader is opened at start and closed at the end.
    /// <para/>
    /// The <c>search.num.hits</c> config parameter sets
    /// the top number of hits to collect during searching.  If
    /// <c>print.hits.field</c> is set, then each hit is
    /// printed along with the value of that field.
    /// <para/>
    /// Other side effects: none.
    /// </remarks>
    public abstract class ReadTask : PerfTask
    {
        #nullable enable
        private readonly IQueryMaker? queryMaker;

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "Required for continuity with Lucene's design")]    
        protected ReadTask(PerfRunData runData) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : this(runData, queryMaker: null)
        {
            if (WithSearch)
            {
                queryMaker = GetQueryMaker();
            }
            else
            {
                queryMaker = null;
            }
        }
        // LUCENENET specific - added this constructor to allow subclasses to initialize it
        // without having to call constructor that makes a virtual method call
        protected ReadTask(PerfRunData runData, IQueryMaker? queryMaker)
            : base(runData)
        {
            this.queryMaker = queryMaker;
        }
        #nullable restore

        public override int DoLogic()
        {
            int res = 0;

            // open reader or use existing one
            IndexSearcher searcher = RunData.GetIndexSearcher();

            IndexReader reader;

            bool closeSearcher;
            if (searcher is null)
            {
                // open our own reader
                Directory dir = RunData.Directory;
                reader = DirectoryReader.Open(dir);
                searcher = new IndexSearcher(reader);
                closeSearcher = true;
            }
            else
            {
                // use existing one; this passes +1 ref to us
                reader = searcher.IndexReader;
                closeSearcher = false;
            }

            // optionally warm and add num docs traversed to count
            if (WithWarm)
            {
                Document doc; // LUCENENET: IDE0059: Remove unnecessary value assignment
                IBits liveDocs = MultiFields.GetLiveDocs(reader);
                for (int m = 0; m < reader.MaxDoc; m++)
                {
                    if (null == liveDocs || liveDocs.Get(m))
                    {
                        doc = reader.Document(m);
                        res += (doc is null ? 0 : 1);
                    }
                }
            }

            if (WithSearch)
            {
                res++;
                Query q = queryMaker.MakeQuery();
                Sort sort = Sort;
                TopDocs hits = null;
                int numHits = NumHits;
                if (numHits > 0)
                {
                    if (WithCollector == false)
                    {
                        if (sort != null)
                        {
                            // TODO: instead of always passing false we
                            // should detect based on the query; if we make
                            // the IndexSearcher search methods that take
                            // Weight public again, we can go back to
                            // pulling the Weight ourselves:
                            TopFieldCollector collector = TopFieldCollector.Create(sort, numHits,
                                                                                   true, WithScore,
                                                                                   WithMaxScore,
                                                                                   false);
                            searcher.Search(q, null, collector);
                            hits = collector.GetTopDocs();
                        }
                        else
                        {
                            hits = searcher.Search(q, numHits);
                        }
                    }
                    else
                    {
                        ICollector collector = CreateCollector();
                        searcher.Search(q, null, collector);
                        //hits = collector.topDocs();
                    }

                    string printHitsField = RunData.Config.Get("print.hits.field", null);
                    if (hits != null && printHitsField != null && printHitsField.Length > 0)
                    {
                        Console.WriteLine("totalHits = " + hits.TotalHits);
                        Console.WriteLine("maxDoc()  = " + reader.MaxDoc);
                        Console.WriteLine("numDocs() = " + reader.NumDocs);
                        for (int i = 0; i < hits.ScoreDocs.Length; i++)
                        {
                            int docID = hits.ScoreDocs[i].Doc;
                            Document doc = reader.Document(docID);
                            Console.WriteLine("  " + i + ": doc=" + docID + " score=" + hits.ScoreDocs[i].Score + " " + printHitsField + " =" + doc.Get(printHitsField));
                        }
                    }

                    if (WithTraverse)
                    {
                        ScoreDoc[] scoreDocs = hits.ScoreDocs;
                        int traversalSize = Math.Min(scoreDocs.Length, TraversalSize);

                        if (traversalSize > 0)
                        {
                            bool retrieve = WithRetrieve;
                            int numHighlight = Math.Min(NumToHighlight, scoreDocs.Length);
                            Analyzer analyzer = RunData.Analyzer;
                            BenchmarkHighlighter highlighter = null;
                            if (numHighlight > 0)
                            {
                                highlighter = GetBenchmarkHighlighter(q);
                            }
                            for (int m = 0; m < traversalSize; m++)
                            {
                                int id = scoreDocs[m].Doc;
                                res++;
                                if (retrieve)
                                {
                                    Document document = RetrieveDoc(reader, id);
                                    res += document != null ? 1 : 0;
                                    if (numHighlight > 0 && m < numHighlight)
                                    {
                                        ICollection<string> fieldsToHighlight = GetFieldsToHighlight(document);
                                        foreach (string field in fieldsToHighlight)
                                        {
                                            string text = document.Get(field);
                                            res += highlighter.DoHighlight(reader, id, field, document, analyzer, text);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (closeSearcher)
            {
                reader.Dispose();
            }
            else
            {
                // Release our +1 ref from above
                reader.DecRef();
            }
            return res;
        }

        protected virtual ICollector CreateCollector()
        {
            return TopScoreDocCollector.Create(NumHits, docsScoredInOrder: true);
        }


        protected virtual Document RetrieveDoc(IndexReader ir, int id)
        {
            return ir.Document(id);
        }

        /// <summary>
        /// Return query maker used for this task.
        /// </summary>
        public abstract IQueryMaker GetQueryMaker();

        /// <summary>
        /// Return <c>true</c> if search should be performed.
        /// </summary>
        public abstract bool WithSearch { get; }

        public virtual bool WithCollector => false;


        /// <summary>
        /// Return <c>true</c> if warming should be performed.
        /// </summary>
        public abstract bool WithWarm { get; }

        /// <summary>
        /// Return <c>true</c> if, with search, results should be traversed.
        /// </summary>
        public abstract bool WithTraverse { get; }

        /// <summary>
        /// Whether scores should be computed (only useful with
        /// field sort)
        /// </summary>
        public virtual bool WithScore => true;

        /// <summary>
        /// Whether maxScores should be computed (only useful with
        /// field sort)
        /// </summary>
        public virtual bool WithMaxScore => true;

        /// <summary>
        /// Specify the number of hits to traverse.  Tasks should override this if they want to restrict the number
        /// of hits that are traversed when <see cref="WithTraverse"/> is <c>true</c>. Must be greater than 0.
        /// <para/>
        /// Read task calculates the traversal as: <c>Math.Min(hits.Length, TraversalSize)</c>
        /// </summary>
        /// <remarks>
        /// Unless overridden, the return value is <see cref="int.MaxValue"/>.
        /// </remarks>
        public virtual int TraversalSize => int.MaxValue;

        internal const int DEFAULT_SEARCH_NUM_HITS = 10;
        private int numHits;

        public override void Setup()
        {
            base.Setup();
            numHits = RunData.Config.Get("search.num.hits", DEFAULT_SEARCH_NUM_HITS);
        }

        /// <summary>
        /// Specify the number of hits to retrieve.  Tasks should override this if they want to restrict the number
        /// of hits that are collected during searching. Must be greater than 0.
        /// <para/>
        /// Returns 10 by default, or <c>search.num.hits</c> config if set.
        /// </summary>
        public virtual int NumHits => numHits;

        /// <summary>
        /// Return <c>true</c> if, with search &amp; results traversing, docs should be retrieved.
        /// </summary>
        public abstract bool WithRetrieve { get; }

        /// <summary>
        /// The number of documents to highlight. 0 means no docs will be highlighted.
        /// </summary>
        public virtual int NumToHighlight => 0;

        /// <summary>
        /// Return an appropriate highlighter to be used with
        /// highlighting tasks.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        protected virtual BenchmarkHighlighter GetBenchmarkHighlighter(Query q)
        {
            return null;
        }

        public virtual Sort Sort => null;

        /// <summary>
        /// Define the fields to highlight.  Base implementation returns all fields.
        /// </summary>
        /// <param name="document">The <see cref="Document"/>.</param>
        /// <returns>An <see cref="T:ICollection{string}"/> of <see cref="Field"/> names.</returns>
        protected virtual ICollection<string> GetFieldsToHighlight(Document document)
        {
            IList<IIndexableField> fields = document.Fields;
            ISet<string> result = new JCG.HashSet<string>(fields.Count);
            foreach (IIndexableField f in fields)
            {
                result.Add(f.Name);
            }
            return result;
        }
    }
}
