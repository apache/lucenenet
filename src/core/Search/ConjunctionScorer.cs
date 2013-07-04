/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Search
{

    /// <summary>Scorer for conjunctions, sets of queries, all of which are required. </summary>
    class ConjunctionScorer : Scorer
    {
        protected int lastDoc = -1;
        protected readonly DocsAndFreqs[] docsAndFreqs;
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
            this.docsAndFreqs = new DocsAndFreqs[scorers.Length];
            for (int i = 0; i < scorers.Length; i++)
            {
                docsAndFreqs[i] = new DocsAndFreqs(scorers[i]);
            }
            // Sort the array the first time to allow the least frequent DocsEnum to
            // lead the matching.
            ArrayUtil.MergeSort(docsAndFreqs, new DelegatedComparer<DocsAndFreqs>((o1, o2) =>
            {
                return (o1.cost - o2.cost).Signum();
            }));

            lead = docsAndFreqs[0]; // least frequent DocsEnum leads the intersection
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
                    bool shouldBreakAdvanceHead = false;
                    for (int i = 1; i < docsAndFreqs.Length; i++)
                    {
                        // invariant: docsAndFreqs[i].doc <= doc at this point.

                        // docsAndFreqs[i].doc may already be equal to doc if we "broke advanceHead"
                        // on the previous iteration and the advance on the lead scorer exactly matched.
                        if (docsAndFreqs[i].doc < doc)
                        {
                            docsAndFreqs[i].doc = docsAndFreqs[i].scorer.Advance(doc);

                            if (docsAndFreqs[i].doc > doc)
                            {
                                // DocsEnum beyond the current doc - break and advance lead to the new highest doc.
                                doc = docsAndFreqs[i].doc;
                                shouldBreakAdvanceHead = true;
                                break;
                            }
                        }
                    }
                    if (shouldBreakAdvanceHead)
                        break;
                    // success - all DocsEnums are on the same doc
                    return doc;
                }
                // advance head for next iteration
                doc = lead.doc = lead.scorer.Advance(doc);
            }
        }

        public override int Advance(int target)
        {
            lead.doc = lead.scorer.Advance(target);
            return lastDoc = DoNext(lead.doc);
        }

        public override int DocID
        {
            get { return lastDoc; }
        }

        public override int NextDoc()
        {
            lead.doc = lead.scorer.NextDoc();
            return lastDoc = DoNext(lead.doc);
        }

        public override float Score()
        {
            // TODO: sum into a double and cast to float if we ever send required clauses to BS1
            float sum = 0.0f;
            foreach (DocsAndFreqs docs in docsAndFreqs)
            {
                sum += docs.scorer.Score();
            }
            return sum * coord;
        }

        public override int Freq
        {
            get { return docsAndFreqs.Length; }
        }

        public override long Cost
        {
            get { return lead.scorer.Cost; }
        }

        public override ICollection<ChildScorer> Children
        {
            get
            {
                List<ChildScorer> children = new List<ChildScorer>(docsAndFreqs.Length);
                foreach (DocsAndFreqs docs in docsAndFreqs)
                {
                    children.Add(new ChildScorer(docs.scorer, "MUST"));
                }
                return children;
            }
        }

        internal sealed class DocsAndFreqs
        {
            internal readonly long cost;
            internal readonly Scorer scorer;
            internal int doc = -1;

            internal DocsAndFreqs(Scorer scorer)
            {
                this.scorer = scorer;
                this.cost = scorer.Cost;
            }
        }
    }
}