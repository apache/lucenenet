using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;

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
    /// Test Search task which counts number of searches.
    /// </summary>
    public class CountingHighlighterTestTask : SearchTravRetHighlightTask
    {
        public static int numHighlightedResults = 0;
        public static int numDocsRetrieved = 0;

        public CountingHighlighterTestTask(PerfRunData runData)
            : base(runData)
        {
        }

        protected override Document RetrieveDoc(IndexReader ir, int id)
        {
            Document document = ir.Document(id);
            if (document != null)
            {
                numDocsRetrieved++;
            }
            return document;
        }

        private sealed class BenchmarkHighlighterAnonymousClass : BenchmarkHighlighter
        {
            private readonly CountingHighlighterTestTask outerInstance;
            private readonly Highlighter highlighter;
            public BenchmarkHighlighterAnonymousClass(CountingHighlighterTestTask outerInstance, Highlighter highlighter)
            {
                this.outerInstance = outerInstance;
                this.highlighter = highlighter;
            }
            public override int DoHighlight(IndexReader reader, int doc, string field, Document document, Analyzer analyzer, string text)
            {
                TokenStream ts = TokenSources.GetAnyTokenStream(reader, doc, field, document, analyzer);
                TextFragment[]
                frag = highlighter.GetBestTextFragments(ts, text, outerInstance.m_mergeContiguous, outerInstance.m_maxFrags);
                numHighlightedResults += frag != null ? frag.Length : 0;
                return frag != null ? frag.Length : 0;
            }
        }

        protected override BenchmarkHighlighter GetBenchmarkHighlighter(Query q)
        {
            m_highlighter = new Highlighter(new SimpleHTMLFormatter(), new QueryScorer(q));
            return new BenchmarkHighlighterAnonymousClass(this, m_highlighter);
            //        return new BenchmarkHighlighter() {
            //  @Override
            //  public int doHighlight(IndexReader reader, int doc, String field, Document document, Analyzer analyzer, String text) 
            //    {
            //        TokenStream ts = TokenSources.GetAnyTokenStream(reader, doc, field, document, analyzer);
            //        TextFragment []
            //        frag = highlighter.GetBestTextFragments(ts, text, mergeContiguous, maxFrags);
            //        numHighlightedResults += frag != null ? frag.Length : 0;
            //    return frag != null ? frag.Length : 0;
            //    }
            //};
        }
    }
}
