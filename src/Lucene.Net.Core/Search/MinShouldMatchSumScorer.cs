using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Search
{
    using Lucene.Net.Support;

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
    /// A Scorer for OR like queries, counterpart of <code>ConjunctionScorer</code>.
    /// this Scorer implements <seealso cref="Scorer#advance(int)"/> and uses advance() on the given Scorers.
    ///
    /// this implementation uses the minimumMatch constraint actively to efficiently
    /// prune the number of candidates, it is hence a mixture between a pure DisjunctionScorer
    /// and a ConjunctionScorer.
    /// </summary>
    internal class MinShouldMatchSumScorer : Scorer
    {
        /// <summary>
        /// The overall number of non-finalized scorers </summary>
        private int NumScorers;

        /// <summary>
        /// The minimum number of scorers that should match </summary>
        private readonly int Mm;

        /// <summary>
        /// A static array of all subscorers sorted by decreasing cost </summary>
        private readonly Scorer[] SortedSubScorers;

        /// <summary>
        /// A monotonically increasing index into the array pointing to the next subscorer that is to be excluded </summary>
        private int SortedSubScorersIdx = 0;

        private readonly Scorer[] SubScorers; // the first numScorers-(mm-1) entries are valid
        private int NrInHeap; // 0..(numScorers-(mm-1)-1)

        /// <summary>
        /// mmStack is supposed to contain the most costly subScorers that still did
        ///  not run out of docs, sorted by increasing sparsity of docs returned by that subScorer.
        ///  For now, the cost of subscorers is assumed to be inversely correlated with sparsity.
        /// </summary>
        private readonly Scorer[] MmStack; // of size mm-1: 0..mm-2, always full

        /// <summary>
        /// The document number of the current match. </summary>
        private int Doc = -1;

        /// <summary>
        /// The number of subscorers that provide the current match. </summary>
        protected internal int NrMatchers = -1;

        private double Score_Renamed = float.NaN;

        /// <summary>
        /// Construct a <code>MinShouldMatchSumScorer</code>.
        /// </summary>
        /// <param name="weight"> The weight to be used. </param>
        /// <param name="subScorers"> A collection of at least two subscorers. </param>
        /// <param name="minimumNrMatchers"> The positive minimum number of subscorers that should
        /// match to match this query.
        /// <br>When <code>minimumNrMatchers</code> is bigger than
        /// the number of <code>subScorers</code>, no matches will be produced.
        /// <br>When minimumNrMatchers equals the number of subScorers,
        /// it is more efficient to use <code>ConjunctionScorer</code>. </param>
        public MinShouldMatchSumScorer(Weight weight, IList<Scorer> subScorers, int minimumNrMatchers)
            : base(weight)
        {
            this.NrInHeap = this.NumScorers = subScorers.Count;

            if (minimumNrMatchers <= 0)
            {
                throw new System.ArgumentException("Minimum nr of matchers must be positive");
            }
            if (NumScorers <= 1)
            {
                throw new System.ArgumentException("There must be at least 2 subScorers");
            }

            this.Mm = minimumNrMatchers;
            this.SortedSubScorers = subScorers.ToArray();
            // sorting by decreasing subscorer cost should be inversely correlated with
            // next docid (assuming costs are due to generating many postings)
            ArrayUtil.TimSort(SortedSubScorers, new ComparatorAnonymousInnerClassHelper(this));
            // take mm-1 most costly subscorers aside
            this.MmStack = new Scorer[Mm - 1];
            for (int i = 0; i < Mm - 1; i++)
            {
                MmStack[i] = SortedSubScorers[i];
            }
            NrInHeap -= Mm - 1;
            this.SortedSubScorersIdx = Mm - 1;
            // take remaining into heap, if any, and heapify
            this.SubScorers = new Scorer[NrInHeap];
            for (int i = 0; i < NrInHeap; i++)
            {
                this.SubScorers[i] = this.SortedSubScorers[Mm - 1 + i];
            }
            MinheapHeapify();
            Debug.Assert(MinheapCheck());
        }

        private class ComparatorAnonymousInnerClassHelper : IComparer<Scorer>
        {
            private readonly MinShouldMatchSumScorer OuterInstance;

            public ComparatorAnonymousInnerClassHelper(MinShouldMatchSumScorer outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public virtual int Compare(Scorer o1, Scorer o2)
            {
                return Number.Signum(o2.Cost() - o1.Cost());
            }
        }

        /// <summary>
        /// Construct a <code>DisjunctionScorer</code>, using one as the minimum number
        /// of matching subscorers.
        /// </summary>
        public MinShouldMatchSumScorer(Weight weight, IList<Scorer> subScorers)
            : this(weight, subScorers, 1)
        {
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

        public override int NextDoc()
        {
            Debug.Assert(Doc != NO_MORE_DOCS);
            while (true)
            {
                // to remove current doc, call next() on all subScorers on current doc within heap
                while (SubScorers[0].DocID() == Doc)
                {
                    if (SubScorers[0].NextDoc() != NO_MORE_DOCS)
                    {
                        MinheapSiftDown(0);
                    }
                    else
                    {
                        MinheapRemoveRoot();
                        NumScorers--;
                        if (NumScorers < Mm)
                        {
                            return Doc = NO_MORE_DOCS;
                        }
                    }
                    //assert minheapCheck();
                }

                EvaluateSmallestDocInHeap();

                if (NrMatchers >= Mm) // doc satisfies mm constraint
                {
                    break;
                }
            }
            return Doc;
        }

        private void EvaluateSmallestDocInHeap()
        {
            // within heap, subScorer[0] now contains the next candidate doc
            Doc = SubScorers[0].DocID();
            if (Doc == NO_MORE_DOCS)
            {
                NrMatchers = int.MaxValue; // stop looping
                return;
            }
            // 1. score and count number of matching subScorers within heap
            Score_Renamed = SubScorers[0].Score();
            NrMatchers = 1;
            CountMatches(1);
            CountMatches(2);
            // 2. score and count number of matching subScorers within stack,
            // short-circuit: stop when mm can't be reached for current doc, then perform on heap next()
            // TODO instead advance() might be possible, but complicates things
            for (int i = Mm - 2; i >= 0; i--) // first advance sparsest subScorer
            {
                if (MmStack[i].DocID() >= Doc || MmStack[i].Advance(Doc) != NO_MORE_DOCS)
                {
                    if (MmStack[i].DocID() == Doc) // either it was already on doc, or got there via advance()
                    {
                        NrMatchers++;
                        Score_Renamed += MmStack[i].Score();
                    } // scorer advanced to next after doc, check if enough scorers left for current doc
                    else
                    {
                        if (NrMatchers + i < Mm) // too few subScorers left, abort advancing
                        {
                            return; // continue looping TODO consider advance() here
                        }
                    }
                } // subScorer exhausted
                else
                {
                    NumScorers--;
                    if (NumScorers < Mm) // too few subScorers left
                    {
                        Doc = NO_MORE_DOCS;
                        NrMatchers = int.MaxValue; // stop looping
                        return;
                    }
                    if (Mm - 2 - i > 0)
                    {
                        // shift RHS of array left
                        Array.Copy(MmStack, i + 1, MmStack, i, Mm - 2 - i);
                    }
                    // find next most costly subScorer within heap TODO can this be done better?
                    while (!MinheapRemove(SortedSubScorers[SortedSubScorersIdx++]))
                    {
                        //assert minheapCheck();
                    }
                    // add the subScorer removed from heap to stack
                    MmStack[Mm - 2] = SortedSubScorers[SortedSubScorersIdx - 1];

                    if (NrMatchers + i < Mm) // too few subScorers left, abort advancing
                    {
                        return; // continue looping TODO consider advance() here
                    }
                }
            }
        }

        // TODO: this currently scores, but so did the previous impl
        // TODO: remove recursion.
        // TODO: consider separating scoring out of here, then modify this
        // and afterNext() to terminate when nrMatchers == minimumNrMatchers
        // then also change freq() to just always compute it from scratch
        private void CountMatches(int root)
        {
            if (root < NrInHeap && SubScorers[root].DocID() == Doc)
            {
                NrMatchers++;
                Score_Renamed += SubScorers[root].Score();
                CountMatches((root << 1) + 1);
                CountMatches((root << 1) + 2);
            }
        }

        /// <summary>
        /// Returns the score of the current document matching the query. Initially
        /// invalid, until <seealso cref="#nextDoc()"/> is called the first time.
        /// </summary>
        public override float Score()
        {
            return (float)Score_Renamed;
        }

        public override int DocID()
        {
            return Doc;
        }

        public override int Freq
        {
            get { return NrMatchers; }
        }

        /// <summary>
        /// Advances to the first match beyond the current whose document number is
        /// greater than or equal to a given target. <br>
        /// The implementation uses the advance() method on the subscorers.
        /// </summary>
        /// <param name="target"> the target document number. </param>
        /// <returns> the document whose number is greater than or equal to the given
        ///         target, or -1 if none exist. </returns>
        public override int Advance(int target)
        {
            if (NumScorers < Mm)
            {
                return Doc = NO_MORE_DOCS;
            }
            // advance all Scorers in heap at smaller docs to at least target
            while (SubScorers[0].DocID() < target)
            {
                if (SubScorers[0].Advance(target) != NO_MORE_DOCS)
                {
                    MinheapSiftDown(0);
                }
                else
                {
                    MinheapRemoveRoot();
                    NumScorers--;
                    if (NumScorers < Mm)
                    {
                        return Doc = NO_MORE_DOCS;
                    }
                }
                //assert minheapCheck();
            }

            EvaluateSmallestDocInHeap();

            if (NrMatchers >= Mm)
            {
                return Doc;
            }
            else
            {
                return NextDoc();
            }
        }

        public override long Cost()
        {
            // cost for merging of lists analog to DisjunctionSumScorer
            long costCandidateGeneration = 0;
            for (int i = 0; i < NrInHeap; i++)
            {
                costCandidateGeneration += SubScorers[i].Cost();
            }
            // TODO is cost for advance() different to cost for iteration + heap merge
            //      and how do they compare overall to pure disjunctions?
            const float c1 = 1.0f, c2 = 1.0f; // maybe a constant, maybe a proportion between costCandidateGeneration and sum(subScorer_to_be_advanced.cost())?
            return (long)(c1 * costCandidateGeneration + c2 * costCandidateGeneration * (Mm - 1)); // advance() cost -  heap-merge cost
        }

        /// <summary>
        /// Organize subScorers into a min heap with scorers generating the earliest document on top.
        /// </summary>
        protected internal void MinheapHeapify()
        {
            for (int i = (NrInHeap >> 1) - 1; i >= 0; i--)
            {
                MinheapSiftDown(i);
            }
        }

        /// <summary>
        /// The subtree of subScorers at root is a min heap except possibly for its root element.
        /// Bubble the root down as required to make the subtree a heap.
        /// </summary>
        protected internal void MinheapSiftDown(int root)
        {
            // TODO could this implementation also move rather than swapping neighbours?
            Scorer scorer = SubScorers[root];
            int doc = scorer.DocID();
            int i = root;
            while (i <= (NrInHeap >> 1) - 1)
            {
                int lchild = (i << 1) + 1;
                Scorer lscorer = SubScorers[lchild];
                int ldoc = lscorer.DocID();
                int rdoc = int.MaxValue, rchild = (i << 1) + 2;
                Scorer rscorer = null;
                if (rchild < NrInHeap)
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

        protected internal void MinheapSiftUp(int i)
        {
            Scorer scorer = SubScorers[i];
            int doc = scorer.DocID();
            // find right place for scorer
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                Scorer pscorer = SubScorers[parent];
                int pdoc = pscorer.DocID();
                if (pdoc > doc) // move root down, make space
                {
                    SubScorers[i] = SubScorers[parent];
                    i = parent;
                } // done, found right place
                else
                {
                    break;
                }
            }
            SubScorers[i] = scorer;
        }

        /// <summary>
        /// Remove the root Scorer from subScorers and re-establish it as a heap
        /// </summary>
        protected internal void MinheapRemoveRoot()
        {
            if (NrInHeap == 1)
            {
                //subScorers[0] = null; // not necessary
                NrInHeap = 0;
            }
            else
            {
                NrInHeap--;
                SubScorers[0] = SubScorers[NrInHeap];
                //subScorers[nrInHeap] = null; // not necessary
                MinheapSiftDown(0);
            }
        }

        /// <summary>
        /// Removes a given Scorer from the heap by placing end of heap at that
        /// position and bubbling it either up or down
        /// </summary>
        protected internal bool MinheapRemove(Scorer scorer)
        {
            // find scorer: O(nrInHeap)
            for (int i = 0; i < NrInHeap; i++)
            {
                if (SubScorers[i] == scorer) // remove scorer
                {
                    SubScorers[i] = SubScorers[--NrInHeap];
                    //if (i != nrInHeap) subScorers[nrInHeap] = null; // not necessary
                    MinheapSiftUp(i);
                    MinheapSiftDown(i);
                    return true;
                }
            }
            return false; // scorer already exhausted
        }

        internal virtual bool MinheapCheck()
        {
            return MinheapCheck(0);
        }

        private bool MinheapCheck(int root)
        {
            if (root >= NrInHeap)
            {
                return true;
            }
            int lchild = (root << 1) + 1;
            int rchild = (root << 1) + 2;
            if (lchild < NrInHeap && SubScorers[root].DocID() > SubScorers[lchild].DocID())
            {
                return false;
            }
            if (rchild < NrInHeap && SubScorers[root].DocID() > SubScorers[rchild].DocID())
            {
                return false;
            }
            return MinheapCheck(lchild) && MinheapCheck(rchild);
        }
    }
}