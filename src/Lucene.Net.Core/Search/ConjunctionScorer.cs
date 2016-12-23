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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;

    /// <summary>
    /// Scorer for conjunctions, sets of queries, all of which are required. </summary>
    internal class ConjunctionScorer : Scorer
    {
        protected int LastDoc = -1; // LUCENENET TODO: rename
        protected readonly DocsAndFreqs[] docsAndFreqs;
        private readonly DocsAndFreqs Lead; // LUCENENET TODO: rename (private)
        private readonly float Coord; // LUCENENET TODO: rename (private)

        internal ConjunctionScorer(Weight weight, Scorer[] scorers)
            : this(weight, scorers, 1f)
        {
        }

        internal ConjunctionScorer(Weight weight, Scorer[] scorers, float coord)
            : base(weight)
        {
            this.Coord = coord;
            this.docsAndFreqs = new DocsAndFreqs[scorers.Length];
            for (int i = 0; i < scorers.Length; i++)
            {
                docsAndFreqs[i] = new DocsAndFreqs(scorers[i]);
            }
            // Sort the array the first time to allow the least frequent DocsEnum to
            // lead the matching.
            ArrayUtil.TimSort(docsAndFreqs, new ComparatorAnonymousInnerClassHelper(this));

            Lead = docsAndFreqs[0]; // least frequent DocsEnum leads the intersection
        }

        private class ComparatorAnonymousInnerClassHelper : IComparer<DocsAndFreqs>
        {
            private readonly ConjunctionScorer OuterInstance;

            public ComparatorAnonymousInnerClassHelper(ConjunctionScorer outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public virtual int Compare(DocsAndFreqs o1, DocsAndFreqs o2)
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
            }
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
                    for (int i = 1; i < docsAndFreqs.Length; i++)
                    {
                        // invariant: docsAndFreqs[i].doc <= doc at this point.

                        // docsAndFreqs[i].doc may already be equal to doc if we "broke advanceHead"
                        // on the previous iteration and the advance on the lead scorer exactly matched.
                        if (docsAndFreqs[i].Doc < doc)
                        {
                            docsAndFreqs[i].Doc = docsAndFreqs[i].Scorer.Advance(doc);

                            if (docsAndFreqs[i].Doc > doc)
                            {
                                // DocsEnum beyond the current doc - break and advance lead to the new highest doc.
                                doc = docsAndFreqs[i].Doc;
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
                doc = Lead.Doc = Lead.Scorer.Advance(doc);
            }
        }

        public override int Advance(int target)
        {
            Lead.Doc = Lead.Scorer.Advance(target);
            return LastDoc = DoNext(Lead.Doc);
        }

        public override int DocID()
        {
            return LastDoc;
        }

        public override int NextDoc()
        {
            Lead.Doc = Lead.Scorer.NextDoc();
            return LastDoc = DoNext(Lead.Doc);
        }

        public override float Score()
        {
            // TODO: sum into a double and cast to float if we ever send required clauses to BS1
            float sum = 0.0f;
            foreach (DocsAndFreqs docs in docsAndFreqs)
            {
                sum += docs.Scorer.Score();
            }
            return sum * Coord;
        }

        public override int Freq
        {
            get { return docsAndFreqs.Length; }
        }

        public override long Cost()
        {
            return Lead.Scorer.Cost();
        }

        public override ICollection<ChildScorer> Children
        {
            get
            {
                List<ChildScorer> children = new List<ChildScorer>(docsAndFreqs.Length);
                foreach (DocsAndFreqs docs in docsAndFreqs)
                {
                    children.Add(new ChildScorer(docs.Scorer, "MUST"));
                }
                return children;
            }
        }

        internal sealed class DocsAndFreqs
        {
            internal readonly long Cost;  // LUCENENET TODO: make property
            internal readonly Scorer Scorer;  // LUCENENET TODO: make property
            internal int Doc = -1;  // LUCENENET TODO: make property

            internal DocsAndFreqs(Scorer scorer)
            {
                this.Scorer = scorer;
                this.Cost = scorer.Cost();
            }
        }
    }
}