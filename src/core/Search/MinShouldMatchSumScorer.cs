using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Search
{
    internal class MinShouldMatchSumScorer : Scorer
    {
        /** The overall number of non-finalized scorers */
        private int numScorers;
        /** The minimum number of scorers that should match */
        private readonly int mm;

        /** A static array of all subscorers sorted by decreasing cost */
        private readonly Scorer[] sortedSubScorers;
        /** A monotonically increasing index into the array pointing to the next subscorer that is to be excluded */
        private int sortedSubScorersIdx = 0;

        private readonly Scorer[] subScorers; // the first numScorers-(mm-1) entries are valid
        private int nrInHeap; // 0..(numScorers-(mm-1)-1)

        /** mmStack is supposed to contain the most costly subScorers that still did
         *  not run out of docs, sorted by increasing sparsity of docs returned by that subScorer.
         *  For now, the cost of subscorers is assumed to be inversely correlated with sparsity.
         */
        private readonly Scorer[] mmStack; // of size mm-1: 0..mm-2, always full

        /** The document number of the current match. */
        private int doc = -1;
        /** The number of subscorers that provide the current match. */
        protected int nrMatchers = -1;
        private double score = float.NaN;

        public MinShouldMatchSumScorer(Weight weight, IList<Scorer> subScorers, int minimumNrMatchers)
            : base(weight)
        {
            this.nrInHeap = this.numScorers = subScorers.Count;

            if (minimumNrMatchers <= 0)
            {
                throw new ArgumentException("Minimum nr of matchers must be positive");
            }
            if (numScorers <= 1)
            {
                throw new ArgumentException("There must be at least 2 subScorers");
            }

            this.mm = minimumNrMatchers;
            this.sortedSubScorers = subScorers.ToArray();
            // sorting by decreasing subscorer cost should be inversely correlated with
            // next docid (assuming costs are due to generating many postings)
            ArrayUtil.MergeSort(sortedSubScorers, new DelegatedComparer<Scorer>((o1, o2) =>
            {
                return (o2.Cost - o1.Cost).Signum();
            }));

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
            //assert minheapCheck();
        }

        public MinShouldMatchSumScorer(Weight weight, IList<Scorer> subScorers)
            : this(weight, subScorers, 1)
        {
        }

        public override ICollection<ChildScorer> Children
        {
            get
            {
                var children = new List<ChildScorer>(numScorers);
                for (var i = 0; i < numScorers; i++)
                {
                    children.Add(new ChildScorer(subScorers[i], "SHOULD"));
                }
                return children;
            }
        }

        public override int NextDoc()
        {
            //assert doc != NO_MORE_DOCS;
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

                if (nrMatchers >= mm)
                { // doc satisfies mm constraint
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
                nrMatchers = int.MaxValue; // stop looping
                return;
            }
            // 1. score and count number of matching subScorers within heap
            score = subScorers[0].Score();
            nrMatchers = 1;
            CountMatches(1);
            CountMatches(2);
            // 2. score and count number of matching subScorers within stack,
            // short-circuit: stop when mm can't be reached for current doc, then perform on heap next()
            // TODO instead advance() might be possible, but complicates things
            for (int i = mm - 2; i >= 0; i--)
            { // first advance sparsest subScorer
                if (mmStack[i].DocID >= doc || mmStack[i].Advance(doc) != NO_MORE_DOCS)
                {
                    if (mmStack[i].DocID == doc)
                    { // either it was already on doc, or got there via advance()
                        nrMatchers++;
                        score += mmStack[i].Score();
                    }
                    else
                    { // scorer advanced to next after doc, check if enough scorers left for current doc
                        if (nrMatchers + i < mm)
                        { // too few subScorers left, abort advancing
                            return; // continue looping TODO consider advance() here
                        }
                    }
                }
                else
                { // subScorer exhausted
                    numScorers--;
                    if (numScorers < mm)
                    { // too few subScorers left
                        doc = NO_MORE_DOCS;
                        nrMatchers = int.MaxValue; // stop looping
                        return;
                    }
                    if (mm - 2 - i > 0)
                    {
                        // shift RHS of array left
                        Array.Copy(mmStack, i + 1, mmStack, i, mm - 2 - i);
                    }
                    // find next most costly subScorer within heap TODO can this be done better?
                    while (!MinheapRemove(sortedSubScorers[sortedSubScorersIdx++]))
                    {
                        //assert minheapCheck();
                    }
                    // add the subScorer removed from heap to stack
                    mmStack[mm - 2] = sortedSubScorers[sortedSubScorersIdx - 1];

                    if (nrMatchers + i < mm)
                    { // too few subScorers left, abort advancing
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
                nrMatchers++;
                score += subScorers[root].Score();
                CountMatches((root << 1) + 1);
                CountMatches((root << 1) + 2);
            }
        }

        public override float Score()
        {
            return (float)score;
        }

        public override int DocID
        {
            get { return doc; }
        }

        public override int Freq
        {
            get { return nrMatchers; }
        }

        public override int Advance(int target)
        {
            if (numScorers < mm)
                return doc = NO_MORE_DOCS;
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

            if (nrMatchers >= mm)
            {
                return doc;
            }
            else
            {
                return NextDoc();
            }
        }

        public override long Cost
        {
            get
            {
                // cost for merging of lists analog to DisjunctionSumScorer
                long costCandidateGeneration = 0;
                for (int i = 0; i < nrInHeap; i++)
                    costCandidateGeneration += subScorers[i].Cost;
                // TODO is cost for advance() different to cost for iteration + heap merge
                //      and how do they compare overall to pure disjunctions? 
                float c1 = 1.0f,
                      c2 = 1.0f; // maybe a constant, maybe a proportion between costCandidateGeneration and sum(subScorer_to_be_advanced.cost())?
                return (long)(
                       c1 * costCandidateGeneration +        // heap-merge cost
                       c2 * costCandidateGeneration * (mm - 1) // advance() cost
                       );
            }
        }

        protected void MinheapHeapify()
        {
            for (int i = (nrInHeap >> 1) - 1; i >= 0; i--)
            {
                MinheapSiftDown(i);
            }
        }

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
                if (pdoc > doc)
                { // move root down, make space
                    subScorers[i] = subScorers[parent];
                    i = parent;
                }
                else
                { // done, found right place
                    break;
                }
            }
            subScorers[i] = scorer;
        }

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

        protected bool MinheapRemove(Scorer scorer)
        {
            // find scorer: O(nrInHeap)
            for (int i = 0; i < nrInHeap; i++)
            {
                if (subScorers[i] == scorer)
                { // remove scorer
                    subScorers[i] = subScorers[--nrInHeap];
                    //if (i != nrInHeap) subScorers[nrInHeap] = null; // not necessary
                    MinheapSiftUp(i);
                    MinheapSiftDown(i);
                    return true;
                }
            }
            return false; // scorer already exhausted
        }

        internal bool MinheapCheck()
        {
            return MinheapCheck(0);
        }

        private bool MinheapCheck(int root)
        {
            if (root >= nrInHeap)
                return true;
            int lchild = (root << 1) + 1;
            int rchild = (root << 1) + 2;
            if (lchild < nrInHeap && subScorers[root].DocID > subScorers[lchild].DocID)
                return false;
            if (rchild < nrInHeap && subScorers[root].DocID > subScorers[rchild].DocID)
                return false;
            return MinheapCheck(lchild) && MinheapCheck(rchild);
        }
    }
}
