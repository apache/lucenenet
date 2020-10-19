using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;

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

    /// <summary>
    /// A <see cref="Rescorer"/> that re-sorts according to a provided
    /// Sort.
    /// </summary>

    public class SortRescorer : Rescorer
    {
        private readonly Sort sort;

        /// <summary>
        /// Sole constructor. </summary>
        public SortRescorer(Sort sort)
        {
            this.sort = sort;
        }

        public override TopDocs Rescore(IndexSearcher searcher, TopDocs firstPassTopDocs, int topN)
        {
            // Copy ScoreDoc[] and sort by ascending docID:
            ScoreDoc[] hits = (ScoreDoc[])firstPassTopDocs.ScoreDocs.Clone();
            Array.Sort(hits, Comparer<ScoreDoc>.Create((a, b) => a.Doc - b.Doc));

            IList<AtomicReaderContext> leaves = searcher.IndexReader.Leaves;

            TopFieldCollector collector = TopFieldCollector.Create(sort, topN, true, true, true, false);

            // Now merge sort docIDs from hits, with reader's leaves:
            int hitUpto = 0;
            int readerUpto = -1;
            int endDoc = 0;
            int docBase = 0;

            FakeScorer fakeScorer = new FakeScorer();

            while (hitUpto < hits.Length)
            {
                ScoreDoc hit = hits[hitUpto];
                int docID = hit.Doc;
                AtomicReaderContext readerContext = null;
                while (docID >= endDoc)
                {
                    readerUpto++;
                    readerContext = leaves[readerUpto];
                    endDoc = readerContext.DocBase + readerContext.Reader.MaxDoc;
                }

                if (readerContext != null)
                {
                    // We advanced to another segment:
                    collector.SetNextReader(readerContext);
                    collector.SetScorer(fakeScorer);
                    docBase = readerContext.DocBase;
                }

                fakeScorer.score = hit.Score;
                fakeScorer.doc = docID - docBase;

                collector.Collect(fakeScorer.doc);

                hitUpto++;
            }

            return collector.GetTopDocs();
        }

        public override Explanation Explain(IndexSearcher searcher, Explanation firstPassExplanation, int docID)
        {
            TopDocs oneHit = new TopDocs(1, new ScoreDoc[] { new ScoreDoc(docID, firstPassExplanation.Value) });
            TopDocs hits = Rescore(searcher, oneHit, 1);
            if (Debugging.AssertsEnabled) Debugging.Assert(hits.TotalHits == 1);

            // TODO: if we could ask the Sort to explain itself then
            // we wouldn't need the separate ExpressionRescorer...
            Explanation result = new Explanation(0.0f, "sort field values for sort=" + sort.ToString());

            // Add first pass:
            Explanation first = new Explanation(firstPassExplanation.Value, "first pass score");
            first.AddDetail(firstPassExplanation);
            result.AddDetail(first);

            FieldDoc fieldDoc = (FieldDoc)hits.ScoreDocs[0];

            // Add sort values:
            SortField[] sortFields = sort.GetSort();
            for (int i = 0; i < sortFields.Length; i++)
            {
                result.AddDetail(new Explanation(0.0f, "sort field " + sortFields[i].ToString() + " value=" + fieldDoc.Fields[i]));
            }

            return result;
        }
    }
}