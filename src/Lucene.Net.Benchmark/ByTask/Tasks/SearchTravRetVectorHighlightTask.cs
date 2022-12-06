using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.VectorHighlight;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;

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
    /// Search and Traverse and Retrieve docs task.  Highlight the fields in the retrieved documents by using <see cref="FastVectorHighlighter"/>.
    /// </summary>
    /// <remarks>
    /// Note: This task reuses the reader if it is already open.
    /// Otherwise a reader is opened at start and closed at the end.
    /// <para/>
    /// Takes optional multivalued, comma separated param string as:
    /// <code>
    /// size[&lt;traversal size&gt;],highlight[&lt;int&gt;],maxFrags[&lt;int&gt;],mergeContiguous[&lt;boolean&gt;],fields[name1;name2;...]
    /// </code>
    /// <list type="bullet">
    ///     <item><term>traversal size</term><description>The number of hits to traverse, otherwise all will be traversed.</description></item>
    ///     <item><term>highlight</term><description>The number of the hits to highlight.  Will always be less than or equal to traversal size.  Default is <see cref="int.MaxValue"/> (i.e. hits.Length).</description></item>
    ///     <item><term>maxFrags</term><description>The maximum number of fragments to score by the highlighter.</description></item>
    ///     <item><term>fragSize</term><description>The length of fragments.</description></item>
    ///     <item><term>fields</term><description>The fields to highlight.  If not specified all fields will be highlighted (or at least attempted).</description></item>
    /// </list>
    /// <para/>
    /// Example:
    /// <code>
    /// "SearchVecHlgtSameRdr" SearchTravRetVectorHighlight(size[10],highlight[10],maxFrags[3],fields[body]) > : 1000
    /// </code>
    /// <para/>
    /// Fields must be stored and term vector offsets and positions in order must be true for this task to work.
    /// <para/>
    /// Other side effects: counts additional 1 (record) for each traversed hit,
    /// and 1 more for each retrieved (non null) document and 1 for each fragment returned.
    /// </remarks>
    public class SearchTravRetVectorHighlightTask : SearchTravTask
    {
        protected int m_numToHighlight = int.MaxValue;
        protected int m_maxFrags = 2;
        protected int m_fragSize = 100;
        protected ISet<string> m_paramFields = Collections.EmptySet<string>();
        protected FastVectorHighlighter m_highlighter;

        public SearchTravRetVectorHighlightTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override void Setup()
        {
            base.Setup();
            //check to make sure either the doc is being stored
            PerfRunData data = RunData;
            if (data.Config.Get("doc.stored", false) == false)
            {
                throw new Exception("doc.stored must be set to true");
            }
            if (data.Config.Get("doc.term.vector.offsets", false) == false)
            {
                throw new Exception("doc.term.vector.offsets must be set to true");
            }
            if (data.Config.Get("doc.term.vector.positions", false) == false)
            {
                throw new Exception("doc.term.vector.positions must be set to true");
            }
        }

        public override bool WithRetrieve => true;

        public override int NumToHighlight => m_numToHighlight;

        protected override BenchmarkHighlighter GetBenchmarkHighlighter(Query q)
        {
            m_highlighter = new FastVectorHighlighter(false, false);
            Query myq = q;
            return new BenchmarkHighlighterAnonymousClass(this, m_highlighter, myq);
        }

        private sealed class BenchmarkHighlighterAnonymousClass : BenchmarkHighlighter
        {
            private readonly SearchTravRetVectorHighlightTask outerInstance;
            private readonly FastVectorHighlighter highlighter;
            private readonly Query myq;
            public BenchmarkHighlighterAnonymousClass(SearchTravRetVectorHighlightTask outerInstance, FastVectorHighlighter highlighter, Query myq)
            {
                this.outerInstance = outerInstance;
                this.highlighter = highlighter;
                this.myq = myq;
            }
            public override int DoHighlight(IndexReader reader, int doc, string field, Document document, Analyzer analyzer, string text)
            {
                FieldQuery fq = highlighter.GetFieldQuery(myq, reader);
                string[] fragments = highlighter.GetBestFragments(fq, reader, doc, field, outerInstance.m_fragSize, outerInstance.m_maxFrags);
                return fragments != null ? fragments.Length : 0;
            }
        }

        protected override ICollection<string> GetFieldsToHighlight(Document document)
        {
            ICollection<string> result = base.GetFieldsToHighlight(document);
            //if stored is false, then result will be empty, in which case just get all the param fields
            if (m_paramFields.Count > 0 && result.Count > 0)
            {
                result.RetainAll(m_paramFields);
            }
            else
            {
                result = m_paramFields;
            }
            return result;
        }

        public override void SetParams(string @params)
        {
            // can't call super because super doesn't understand our
            // params syntax
            string[] splits = @params.Split(',').TrimEnd();
            for (int i = 0; i < splits.Length; i++)
            {
                if (splits[i].StartsWith("size[", StringComparison.Ordinal) == true)
                {
                    int len = "size[".Length;
                    m_traversalSize = (int)float.Parse(splits[i].Substring(len, (splits[i].Length - 1) - len), CultureInfo.InvariantCulture);
                }
                else if (splits[i].StartsWith("highlight[", StringComparison.Ordinal) == true)
                {
                    int len = "highlight[".Length;
                    m_numToHighlight = (int)float.Parse(splits[i].Substring(len, (splits[i].Length - 1) - len), CultureInfo.InvariantCulture);
                }
                else if (splits[i].StartsWith("maxFrags[", StringComparison.Ordinal) == true)
                {
                    int len = "maxFrags[".Length;
                    m_maxFrags = (int)float.Parse(splits[i].Substring(len, (splits[i].Length - 1) - len), CultureInfo.InvariantCulture);
                }
                else if (splits[i].StartsWith("fragSize[", StringComparison.Ordinal) == true)
                {
                    int len = "fragSize[".Length;
                    m_fragSize = (int)float.Parse(splits[i].Substring(len, (splits[i].Length - 1) - len), CultureInfo.InvariantCulture);
                }
                else if (splits[i].StartsWith("fields[", StringComparison.Ordinal) == true)
                {
                    m_paramFields = new JCG.HashSet<string>();
                    int len = "fields[".Length;
                    string fieldNames = splits[i].Substring(len, (splits[i].Length - 1) - len);
                    string[] fieldSplits = fieldNames.Split(';').TrimEnd();
                    for (int j = 0; j < fieldSplits.Length; j++)
                    {
                        m_paramFields.Add(fieldSplits[j]);
                    }

                }
            }
        }
    }
}
