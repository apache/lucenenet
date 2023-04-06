using J2N;
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
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
    /// A <see cref="Scorer"/> for OR like queries, counterpart of <see cref="ConjunctionScorer"/>.
    /// This <see cref="Scorer"/> implements <see cref="DocIdSetIterator.Advance(int)"/> and uses Advance() on the given <see cref="Scorer"/>s.
    /// <para/>
    /// This implementation uses the minimumMatch constraint actively to efficiently
    /// prune the number of candidates, it is hence a mixture between a pure <see cref="DisjunctionScorer"/>
    /// and a <see cref="ConjunctionScorer"/>.
    /// </summary>
    internal class MinShouldMatchSumScorer : Scorer
    {
        /// <summary>
        /// The overall number of non-finalized scorers </summary>
        private int numScorers;

        /// <summary>
        /// The minimum number of scorers that should match </summary>
        private readonly int mm;

        /// <summary>
        /// A static array of all subscorers sorted by decreasing cost </summary>
        private readonly Scorer[] sortedSubScorers;

        /// <summary>
        /// A monotonically increasing index into the array pointing to the next subscorer that is to be excluded </summary>
        private int sortedSubScorersIdx = 0;

        private readonly Scorer[] subScorers; // the first numScorers-(mm-1) entries are valid
        private int nrInHeap; // 0..(numScorers-(mm-1)-1)

        /// <summary>
        /// mmStack is supposed to contain the most costly subScorers that still did
        /// not run out of docs, sorted by increasing sparsity of docs returned by that subScorer.
        /// For now, the cost of subscorers is assumed to be inversely correlated with sparsity.
        /// </summary>
        private readonly Scorer[] mmStack; // of size mm-1: 0..mm-2, always full

        /// <summary>
        /// The document number of the current match. </summary>
        private int doc = -1;

        /// <summary>
        /// The number of subscorers that provide the current match. </summary>
        protected int m_nrMatchers = -1;

        private double score = float.NaN;

        /// <summary>
        /// Construct a <see cref="MinShouldMatchSumScorer"/>.
        /// </summary>
        /// <param name="weight"> The weight to be used. </param>
        /// <param name="subScorers"> A collection of at least two subscorers. </param>
        /// <param name="minimumNrMatchers"> The positive minimum number of subscorers that should
        /// match to match this query.
        /// <para/>When <paramref name="minimumNrMatchers"/> is bigger than
        /// the number of <paramref name="subScorers"/>, no matches will be produced.
        /// <para/>When <paramref name="minimumNrMatchers"/> equals the number of <paramref name="subScorers"/>,
        /// it is more efficient to use <see cref="ConjunctionScorer"/>. </param>
        public MinShouldMatchSumScorer(Weight weight, IList<Scorer> subScorers, int minimumNrMatchers)
            : base(weight)
        {
            this.nrInHeap = this.numScorers = subScorers.Count;

            if (minimumNrMatchers <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumNrMatchers), "Minimum nr of matchers must be positive"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (numScorers <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(numScorers), "There must be at least 2 subScorers"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            this.mm = minimumNrMatchers;
            this.sortedSubScorers = subScorers.ToArray();
            // sorting by decreasing subscorer cost should be inversely correlated with
            // next docid (assuming costs are due to generating many postings)
            ArrayUtil.TimSort(sortedSubScorers, Comparer<Scorer>.Create((o1, o2) => (o2.GetCost() - o1.GetCost()).Signum()));
            // take mm-1 most costly subscorers aside
            this.mmStack = new Scorer[mm - 1];
            for (int i = 0; i < mm - 1; i++)
            {
                mmStack[i] = sortedSubScorers[i];
            }
            nrInHeap -= mm - 1;
            this.sortedSubScorersIdx = mm - 1;
            // take remaining into heap, if any, and heapify
            this.subScorers = new Scorer[nrInHeap];
            for (int i = 0; i < nrInHeap; i++)
            {
                this.subScorers[i] = this.sortedSubScorers[mm - 1 + i];
            }
            MinheapHeapify();
            if (Debugging.AssertsEnabled) Debugging.Assert(MinheapCheck());
        }

        /// <summary>
        /// Construct a <see cref="DisjunctionScorer"/>, using one as the minimum number
        /// of matching <paramref name="subScorers"/>.
        /// </summary>
        public MinShouldMatchSumScorer(Weight weight, IList<Scorer> subScorers)
            : this(weight, subScorers, 1)
        {
        }

        public override sealed ICollection<ChildScorer> GetChildren()
        {
            IList<ChildScorer> children = new JCG.List<ChildScorer>(numScorers);
            for (int i = 0; i < numScorers; i++)
            {
                children.Add(new ChildScorer(subScorers[i], "SHOULD"));
            }
            return children;
        }

        public override int NextDoc()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(doc != NO_MORE_DOCS);
            while (true)
            {
                // to remove current doc, call next() on all subScorers on current doc within heap
                while (subScorers[0].DocID == doc)
                {
                    if (subScorers[0].NextDoc() != NO_MORE_DOCS)
                    {
                        MinheapSiftDown(0);
                    }
                    else
                    {
                        MinheapRemoveRoot();
                        numScorers--;
                        if (numScorers < mm)
                        {
                            return doc = NO_MORE_DOCS;
                        }
                    }
                    //assert minheapCheck();
                }

                EvaluateSmallestDocInHeap();

                if (m_nrMatchers >= mm) // doc satisfies mm constraint
                {
                    break;
                }
            }
            return doc;
        }

        private void EvaluateSmallestDocInHeap()
        {
            // within heap, subScorer[0] now contains the next candidate doc
            doc = subScorers[0].DocID;
            if (doc == NO_MORE_DOCS)
            {
                m_nrMatchers = int.MaxValue; // stop looping
                return;
            }
            // 1. score and count number of matching subScorers within heap
            score = subScorers[0].GetScore();
            m_nrMatchers = 1;
            CountMatches(1);
            CountMatches(2);
            // 2. score and count number of matching subScorers within stack,
            // short-circuit: stop when mm can't be reached for current doc, then perform on heap next()
            // TODO instead advance() might be possible, but complicates things
            for (int i = mm - 2; i >= 0; i--) // first advance sparsest subScorer
            {
                if (mmStack[i].DocID >= doc || mmStack[i].Advance(doc) != NO_MORE_DOCS)
                {
                    if (mmStack[i].DocID == doc) // either it was already on doc, or got there via advance()
                    {
                        m_nrMatchers++;
                        score += mmStack[i].GetScore();
                    } // scorer advanced to next after doc, check if enough scorers left for current doc
                    else
                    {
                        if (m_nrMatchers + i < mm) // too few subScorers left, abort advancing
                        {
                            return; // continue looping TODO consider advance() here
                        }
                    }
                } // subScorer exhausted
                else
                {
                    numScorers--;
                    if (numScorers < mm) // too few subScorers left
                    {
                        doc = NO_MORE_DOCS;
                        m_nrMatchers = int.MaxValue; // stop looping
                        return;
                    }
                    if (mm - 2 - i > 0)
                    {
                        // shift RHS of array left
                        Arrays.Copy(mmStack, i + 1, mmStack, i, mm - 2 - i);
                    }
                    // find next most costly subScorer within heap TODO can this be done better?
                    while (!MinheapRemove(sortedSubScorers[sortedSubScorersIdx++]))
                    {
                        //assert minheapCheck();
                    }
                    // add the subScorer removed from heap to stack
                    mmStack[mm - 2] = sortedSubScorers[sortedSubScorersIdx - 1];

                    if (m_nrMatchers + i < mm) // too few subScorers left, abort advancing
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
            if (root < nrInHeap && subScorers[root].DocID == doc)
            {
                m_nrMatchers++;
                score += subScorers[root].GetScore();
                CountMatches((root << 1) + 1);
                CountMatches((root << 1) + 2);
            }
        }

        /// <summary>
        /// Returns the score of the current document matching the query. Initially
        /// invalid, until <see cref="NextDoc()"/> is called the first time.
        /// </summary>
        public override float GetScore()
        {
            return (float)score;
        }

        public override int DocID => doc;

        public override int Freq => m_nrMatchers;

        /// <summary>
        /// Advances to the first match beyond the current whose document number is
        /// greater than or equal to a given target.
        /// <para/>
        /// The implementation uses the Advance() method on the subscorers.
        /// </summary>
        /// <param name="target"> The target document number. </param>
        /// <returns> The document whose number is greater than or equal to the given
        ///         target, or -1 if none exist. </returns>
        public override int Advance(int target)
        {
            if (numScorers < mm)
            {
                return doc = NO_MORE_DOCS;
            }
            // advance all Scorers in heap at smaller docs to at least target
            while (subScorers[0].DocID < target)
            {
                if (subScorers[0].Advance(target) != NO_MORE_DOCS)
                {
                    MinheapSiftDown(0);
                }
                else
                {
                    MinheapRemoveRoot();
                    numScorers--;
                    if (numScorers < mm)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                }
                //assert minheapCheck();
            }

            EvaluateSmallestDocInHeap();

            if (m_nrMatchers >= mm)
            {
                return doc;
            }
            else
            {
                return NextDoc();
            }
        }

        public override long GetCost()
        {
            // cost for merging of lists analog to DisjunctionSumScorer
            long costCandidateGeneration = 0;
            for (int i = 0; i < nrInHeap; i++)
            {
                costCandidateGeneration += subScorers[i].GetCost();
            }
            // TODO is cost for advance() different to cost for iteration + heap merge
            //      and how do they compare overall to pure disjunctions?
            const float c1 = 1.0f, c2 = 1.0f; // maybe a constant, maybe a proportion between costCandidateGeneration and sum(subScorer_to_be_advanced.cost())?
            return (long)(c1 * costCandidateGeneration + c2 * costCandidateGeneration * (mm - 1)); // advance() cost -  heap-merge cost
        }

        /// <summary>
        /// Organize <see cref="subScorers"/> into a min heap with scorers generating the earliest document on top.
        /// </summary>
        protected void MinheapHeapify()
        {
            for (int i = (nrInHeap >> 1) - 1; i >= 0; i--)
            {
                MinheapSiftDown(i);
            }
        }

        /// <summary>
        /// The subtree of <see cref="subScorers"/> at root is a min heap except possibly for its root element.
        /// Bubble the root down as required to make the subtree a heap.
        /// </summary>
        protected void MinheapSiftDown(int root)
        {
            // TODO could this implementation also move rather than swapping neighbours?
            Scorer scorer = subScorers[root];
            int doc = scorer.DocID;
            int i = root;
            while (i <= (nrInHeap >> 1) - 1)
            {
                int lchild = (i << 1) + 1;
                Scorer lscorer = subScorers[lchild];
                int ldoc = lscorer.DocID;
                int rdoc = int.MaxValue, rchild = (i << 1) + 2;
                Scorer rscorer = null;
                if (rchild < nrInHeap)
                {
                    rscorer = subScorers[rchild];
                    rdoc = rscorer.DocID;
                }
                if (ldoc < doc)
                {
                    if (rdoc < ldoc)
                    {
                        subScorers[i] = rscorer;
                        subScorers[rchild] = scorer;
                        i = rchild;
                    }
                    else
                    {
                        subScorers[i] = lscorer;
                        subScorers[lchild] = scorer;
                        i = lchild;
                    }
                }
                else if (rdoc < doc)
                {
                    subScorers[i] = rscorer;
                    subScorers[rchild] = scorer;
                    i = rchild;
                }
                else
                {
                    return;
                }
            }
        }

        protected void MinheapSiftUp(int i)
        {
            Scorer scorer = subScorers[i];
            int doc = scorer.DocID;
            // find right place for scorer
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                Scorer pscorer = subScorers[parent];
                int pdoc = pscorer.DocID;
                if (pdoc > doc) // move root down, make space
                {
                    subScorers[i] = subScorers[parent];
                    i = parent;
                } // done, found right place
                else
                {
                    break;
                }
            }
            subScorers[i] = scorer;
        }

        /// <summary>
        /// Remove the root <see cref="Scorer"/> from <see cref="subScorers"/> and re-establish it as a heap
        /// </summary>
        protected void MinheapRemoveRoot()
        {
            if (nrInHeap == 1)
            {
                //subScorers[0] = null; // not necessary
                nrInHeap = 0;
            }
            else
            {
                nrInHeap--;
                subScorers[0] = subScorers[nrInHeap];
                //subScorers[nrInHeap] = null; // not necessary
                MinheapSiftDown(0);
            }
        }

        /// <summary>
        /// Removes a given <see cref="Scorer"/> from the heap by placing end of heap at that
        /// position and bubbling it either up or down
        /// </summary>
        protected bool MinheapRemove(Scorer scorer)
        {
            // find scorer: O(nrInHeap)
            for (int i = 0; i < nrInHeap; i++)
            {
                if (subScorers[i] == scorer) // remove scorer
                {
                    subScorers[i] = subScorers[--nrInHeap];
                    //if (i != nrInHeap) subScorers[nrInHeap] = null; // not necessary
                    MinheapSiftUp(i);
                    MinheapSiftDown(i);
                    return true;
                }
            }
            return false; // scorer already exhausted
        }

        // LUCENENET specific - S1699 - marked non-virtual because calling virtual members
        // from the constructor is not a safe operation in .NET
        private bool MinheapCheck()
        {
            return MinheapCheck(0);
        }

        private bool MinheapCheck(int root)
        {
            if (root >= nrInHeap)
            {
                return true;
            }
            int lchild = (root << 1) + 1;
            int rchild = (root << 1) + 2;
            if (lchild < nrInHeap && subScorers[root].DocID > subScorers[lchild].DocID)
            {
                return false;
            }
            if (rchild < nrInHeap && subScorers[root].DocID > subScorers[rchild].DocID)
            {
                return false;
            }
            return MinheapCheck(lchild) && MinheapCheck(rchild);
        }
    }
}