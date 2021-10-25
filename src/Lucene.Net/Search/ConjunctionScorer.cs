using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;

    /// <summary>
    /// Scorer for conjunctions, sets of queries, all of which are required. </summary>
    internal class ConjunctionScorer : Scorer
    {
        protected int m_lastDoc = -1;
        protected readonly DocsAndFreqs[] m_docsAndFreqs;
        private readonly DocsAndFreqs lead;
        private readonly float coord;

        internal ConjunctionScorer(Weight weight, Scorer[] scorers)
            : this(weight, scorers, 1f)
        {
        }

        internal ConjunctionScorer(Weight weight, Scorer[] scorers, float coord)
            : base(weight)
        {
            this.coord = coord;
            this.m_docsAndFreqs = new DocsAndFreqs[scorers.Length];
            for (int i = 0; i < scorers.Length; i++)
            {
                m_docsAndFreqs[i] = new DocsAndFreqs(scorers[i]);
            }
            // Sort the array the first time to allow the least frequent DocsEnum to
            // lead the matching.
            ArrayUtil.TimSort(m_docsAndFreqs, Comparer<DocsAndFreqs>.Create((o1, o2) =>
            {
                if (o1.Cost < o2.Cost)
                {
                    return -1;
                }
                else if (o1.Cost > o2.Cost)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }));

            lead = m_docsAndFreqs[0]; // least frequent DocsEnum leads the intersection
        }

        private int DoNext(int doc)
        {
            for (; ; )
            {
                // doc may already be NO_MORE_DOCS here, but we don't check explicitly
                // since all scorers should advance to NO_MORE_DOCS, match, then
                // return that value.
                for (; ; )
                {
                    for (int i = 1; i < m_docsAndFreqs.Length; i++)
                    {
                        // invariant: docsAndFreqs[i].doc <= doc at this point.

                        // docsAndFreqs[i].doc may already be equal to doc if we "broke advanceHead"
                        // on the previous iteration and the advance on the lead scorer exactly matched.
                        if (m_docsAndFreqs[i].Doc < doc)
                        {
                            m_docsAndFreqs[i].Doc = m_docsAndFreqs[i].Scorer.Advance(doc);

                            if (m_docsAndFreqs[i].Doc > doc)
                            {
                                // DocsEnum beyond the current doc - break and advance lead to the new highest doc.
                                doc = m_docsAndFreqs[i].Doc;
                                goto advanceHeadBreak;
                            }
                        }
                    }
                    // success - all DocsEnums are on the same doc
                    return doc;
                    //advanceHeadContinue:;
                }
            advanceHeadBreak:
                // advance head for next iteration
                doc = lead.Doc = lead.Scorer.Advance(doc);
            }
        }

        public override int Advance(int target)
        {
            lead.Doc = lead.Scorer.Advance(target);
            return m_lastDoc = DoNext(lead.Doc);
        }

        public override int DocID => m_lastDoc;

        public override int NextDoc()
        {
            lead.Doc = lead.Scorer.NextDoc();
            return m_lastDoc = DoNext(lead.Doc);
        }

        public override float GetScore()
        {
            // TODO: sum into a double and cast to float if we ever send required clauses to BS1
            float sum = 0.0f;
            foreach (DocsAndFreqs docs in m_docsAndFreqs)
            {
                sum += docs.Scorer.GetScore();
            }
            return sum * coord;
        }

        public override int Freq => m_docsAndFreqs.Length;

        public override long GetCost()
        {
            return lead.Scorer.GetCost();
        }

        public override ICollection<ChildScorer> GetChildren()
        {
            IList<ChildScorer> children = new JCG.List<ChildScorer>(m_docsAndFreqs.Length);
            foreach (DocsAndFreqs docs in m_docsAndFreqs)
            {
                children.Add(new ChildScorer(docs.Scorer, "MUST"));
            }
            return children;
        }

        internal sealed class DocsAndFreqs
        {
            internal long Cost { get; private set; }
            internal Scorer Scorer { get; private set; }
            internal int Doc { get; set; }

            internal DocsAndFreqs(Scorer scorer)
            {
                this.Scorer = scorer;
                this.Cost = scorer.GetCost();
                this.Doc = -1;
            }
        }
    }
}