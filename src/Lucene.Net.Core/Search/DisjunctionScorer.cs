using System.Collections.Generic;
using System.Diagnostics;

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

    /// <summary>
    /// Base class for Scorers that score disjunctions.
    /// Currently this just provides helper methods to manage the heap.
    /// </summary>
    internal abstract class DisjunctionScorer : Scorer
    {
        protected readonly Scorer[] SubScorers; // LUCENENET TODO: Rename (private)

        /// <summary>
        /// The document number of the current match. </summary>
        protected int Doc = -1; // LUCENENET TODO: Rename (private)

        protected int NumScorers; // LUCENENET TODO: Rename (private)

        protected DisjunctionScorer(Weight weight, Scorer[] subScorers)
            : base(weight)
        {
            this.SubScorers = subScorers;
            this.NumScorers = subScorers.Length;
            Heapify();
        }

        /// <summary>
        /// Organize subScorers into a min heap with scorers generating the earliest document on top.
        /// </summary>
        protected void Heapify()
        {
            for (int i = (NumScorers >> 1) - 1; i >= 0; i--)
            {
                HeapAdjust(i);
            }
        }

        /// <summary>
        /// The subtree of subScorers at root is a min heap except possibly for its root element.
        /// Bubble the root down as required to make the subtree a heap.
        /// </summary>
        protected void HeapAdjust(int root)
        {
            Scorer scorer = SubScorers[root];
            int doc = scorer.DocID();
            int i = root;
            while (i <= (NumScorers >> 1) - 1)
            {
                int lchild = (i << 1) + 1;
                Scorer lscorer = SubScorers[lchild];
                int ldoc = lscorer.DocID();
                int rdoc = int.MaxValue, rchild = (i << 1) + 2;
                Scorer rscorer = null;
                if (rchild < NumScorers)
                {
                    rscorer = SubScorers[rchild];
                    rdoc = rscorer.DocID();
                }
                if (ldoc < doc)
                {
                    if (rdoc < ldoc)
                    {
                        SubScorers[i] = rscorer;
                        SubScorers[rchild] = scorer;
                        i = rchild;
                    }
                    else
                    {
                        SubScorers[i] = lscorer;
                        SubScorers[lchild] = scorer;
                        i = lchild;
                    }
                }
                else if (rdoc < doc)
                {
                    SubScorers[i] = rscorer;
                    SubScorers[rchild] = scorer;
                    i = rchild;
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Remove the root Scorer from subScorers and re-establish it as a heap
        /// </summary>
        protected void HeapRemoveRoot()
        {
            if (NumScorers == 1)
            {
                SubScorers[0] = null;
                NumScorers = 0;
            }
            else
            {
                SubScorers[0] = SubScorers[NumScorers - 1];
                SubScorers[NumScorers - 1] = null;
                --NumScorers;
                HeapAdjust(0);
            }
        }

        public override sealed ICollection<ChildScorer> Children
        {
            get
            {
                List<ChildScorer> children = new List<ChildScorer>(NumScorers);
                for (int i = 0; i < NumScorers; i++)
                {
                    children.Add(new ChildScorer(SubScorers[i], "SHOULD"));
                }
                return children;
            }
        }

        public override long Cost()
        {
            long sum = 0;
            for (int i = 0; i < NumScorers; i++)
            {
                sum += SubScorers[i].Cost();
            }
            return sum;
        }

        public override int DocID()
        {
            return Doc;
        }

        public override int NextDoc()
        {
            Debug.Assert(Doc != NO_MORE_DOCS);
            while (true)
            {
                if (SubScorers[0].NextDoc() != NO_MORE_DOCS)
                {
                    HeapAdjust(0);
                }
                else
                {
                    HeapRemoveRoot();
                    if (NumScorers == 0)
                    {
                        return Doc = NO_MORE_DOCS;
                    }
                }
                if (SubScorers[0].DocID() != Doc)
                {
                    AfterNext();
                    return Doc;
                }
            }
        }

        public override int Advance(int target)
        {
            Debug.Assert(Doc != NO_MORE_DOCS);
            while (true)
            {
                if (SubScorers[0].Advance(target) != NO_MORE_DOCS)
                {
                    HeapAdjust(0);
                }
                else
                {
                    HeapRemoveRoot();
                    if (NumScorers == 0)
                    {
                        return Doc = NO_MORE_DOCS;
                    }
                }
                if (SubScorers[0].DocID() >= target)
                {
                    AfterNext();
                    return Doc;
                }
            }
        }

        /// <summary>
        /// Called after next() or advance() land on a new document.
        /// <p>
        /// {@code subScorers[0]} will be positioned to the new docid,
        /// which could be {@code NO_MORE_DOCS} (subclass must handle this).
        /// <p>
        /// implementations should assign {@code doc} appropriately, and do any
        /// other work necessary to implement {@code score()} and {@code freq()}
        /// </summary>
        // TODO: make this less horrible
        protected abstract void AfterNext();
    }
}