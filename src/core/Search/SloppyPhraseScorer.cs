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

using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{

    sealed class SloppyPhraseScorer : Scorer
    {
        private PhrasePositions min, max;

        private float sloppyFreq; //phrase frequency in current doc as computed by phraseFreq().

        private readonly Similarity.SloppySimScorer docScorer;
        private readonly int slop;
        private readonly int numPostings;
        private readonly PhraseQueue pq; // for advancing min position

        private int end; // current largest phrase position  

        private bool hasRpts; // flag indicating that there are repetitions (as checked in first candidate doc)
        private bool checkedRpts; // flag to only check for repetitions in first candidate doc
        private bool hasMultiTermRpts; //  
        private PhrasePositions[][] rptGroups; // in each group are PPs that repeats each other (i.e. same term), sorted by (query) offset 
        private PhrasePositions[] rptStack; // temporary stack for switching colliding repeating pps 

        private int numMatches;
        private readonly long cost;

        SloppyPhraseScorer(Weight weight, PhraseQuery.PostingsAndFreq[] postings,
            int slop, Similarity.SloppySimScorer docScorer)
            : base(weight)
        {
            this.docScorer = docScorer;
            this.slop = slop;
            this.numPostings = postings == null ? 0 : postings.Length;
            pq = new PhraseQueue(postings.Length);
            // min(cost)
            cost = postings[0].postings.Cost;
            // convert tps to a list of phrase positions.
            // note: phrase-position differs from term-position in that its position
            // reflects the phrase offset: pp.pos = tp.pos - offset.
            // this allows to easily identify a matching (exact) phrase 
            // when all PhrasePositions have exactly the same position.
            if (postings.Length > 0)
            {
                min = new PhrasePositions(postings[0].postings, postings[0].position, 0, postings[0].terms);
                max = min;
                max.doc = -1;
                for (int i = 1; i < postings.Length; i++)
                {
                    PhrasePositions pp = new PhrasePositions(postings[i].postings, postings[i].position, i, postings[i].terms);
                    max.next = pp;
                    max = pp;
                    max.doc = -1;
                }
                max.next = min; // make it cyclic for easier manipulation
            }
        }

        private float phraseFreq()
        {
            if (!InitPhrasePositions())
            {
                return 0.0f;
            }
            float freq = 0.0f;
            numMatches = 0;
            PhrasePositions pp = pq.Pop();
            int matchLength = end - pp.position;
            int next = pq.Top().position;
            while (AdvancePP(pp))
            {
                if (hasRpts && !AdvanceRpts(pp))
                {
                    break; // pps exhausted
                }
                if (pp.position > next)
                { // done minimizing current match-Length 
                    if (matchLength <= slop)
                    {
                        freq += docScorer.ComputeSlopFactor(matchLength); // Score match
                        numMatches++;
                    }
                    pq.Add(pp);
                    pp = pq.Pop();
                    next = pq.Top().position;
                    matchLength = end - pp.position;
                }
                else
                {
                    int matchLength2 = end - pp.position;
                    if (matchLength2 < matchLength)
                    {
                        matchLength = matchLength2;
                    }
                }
            }
            if (matchLength <= slop)
            {
                freq += docScorer.ComputeSlopFactor(matchLength); // Score match
                numMatches++;
            }
            return freq;
        }

        private bool AdvancePP(PhrasePositions pp)
        {
            if (!pp.NextPosition())
            {
                return false;
            }
            if (pp.position > end)
            {
                end = pp.position;
            }
            return true;
        }

        private bool AdvanceRpts(PhrasePositions pp)
        {
            if (pp.rptGroup < 0)
            {
                return true; // not a repeater
            }
            PhrasePositions[] rg = rptGroups[pp.rptGroup];
            OpenBitSet bits = new OpenBitSet(rg.Length); // for re-queuing after collisions are resolved
            int k0 = pp.rptInd;
            int k;
            while ((k = Collide(pp)) >= 0)
            {
                pp = Lesser(pp, rg[k]); // always advance the Lesser of the (only) two colliding pps
                if (!AdvancePP(pp))
                {
                    return false; // exhausted
                }
                if (k != k0)
                { // careful: mark only those currently in the queue
                    bits.Set(k); // mark that pp2 need to be re-queued
                }
            }
            // collisions resolved, now re-queue
            // empty (partially) the queue until seeing all pps advanced for resolving collisions
            int n = 0;
            while (bits.Cardinality > 0)
            {
                PhrasePositions pp2 = pq.Pop();
                rptStack[n++] = pp2;
                if (pp2.rptGroup >= 0 && bits.Get(pp2.rptInd))
                {
                    bits.Clear(pp2.rptInd);
                }
            }
            // Add back to queue
            for (int i = n - 1; i >= 0; i--)
            {
                pq.Add(rptStack[i]);
            }
            return true;
        }

        private PhrasePositions Lesser(PhrasePositions pp, PhrasePositions pp2)
        {
            if (pp.position < pp2.position ||
                (pp.position == pp2.position && pp.offset < pp2.offset))
            {
                return pp;
            }
            return pp2;
        }

        private int Collide(PhrasePositions pp)
        {
            int tpPos = TpPos(pp);
            PhrasePositions[] rg = rptGroups[pp.rptGroup];
            for (int i = 0; i < rg.Length; i++)
            {
                PhrasePositions pp2 = rg[i];
                if (pp2 != pp && TpPos(pp2) == tpPos)
                {
                    return pp2.rptInd;
                }
            }
            return -1;
        }

        private bool InitPhrasePositions()
        {
            end = int.MinValue;
            if (!checkedRpts)
            {
                return InitFirstTime();
            }
            if (!hasRpts)
            {
                InitSimple();
                return true; // PPs available
            }
            return InitComplex();
        }

        private void InitSimple()
        {
            //System.err.println("InitSimple: doc: "+min.doc);
            pq.Clear();
            // position pps and build queue from list
            for (PhrasePositions pp = min, prev = null; prev != max; pp = (prev = pp).next)
            {  // iterate cyclic list: done once handled max
                pp.FirstPosition();
                if (pp.position > end)
                {
                    end = pp.position;
                }
                pq.Add(pp);
            }
        }

        private bool InitComplex()
        {
            PlaceFirstPositions();
            if (!AdvanceRepeatGroups())
            {
                return false; // PPs exhausted
            }
            FillQueue();
            return true; // PPs available
        }

        private void PlaceFirstPositions()
        {
            for (PhrasePositions pp = min, prev = null; prev != max; pp = (prev = pp).next)
            { // iterate cyclic list: done once handled max
                pp.FirstPosition();
            }
        }

        private void FillQueue()
        {
            pq.Clear();
            for (PhrasePositions pp = min, prev = null; prev != max; pp = (prev = pp).next)
            {  // iterate cyclic list: done once handled max
                if (pp.position > end)
                {
                    end = pp.position;
                }
                pq.Add(pp);
            }
        }

        private bool AdvanceRepeatGroups()
        {
            foreach (var rg in rptGroups)
            {
                if (hasMultiTermRpts)
                {
                    // more involved, some may not Collide
                    int incr;
                    for (int i = 0; i < rg.Length; i += incr)
                    {
                        incr = 1;
                        PhrasePositions pp = rg[i];
                        int k;
                        while ((k = Collide(pp)) >= 0)
                        {
                            PhrasePositions pp2 = Lesser(pp, rg[k]);
                            if (!AdvancePP(pp2))
                            {  // at initialization always advance pp with higher offset
                                return false; // exhausted
                            }
                            if (pp2.rptInd < i)
                            { // should not happen?
                                incr = 0;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // simpler, we know exactly how much to advance
                    for (int j = 1; j < rg.Length; j++)
                    {
                        for (int k = 0; k < j; k++)
                        {
                            if (!rg[j].NextPosition())
                            {
                                return false; // PPs exhausted
                            }
                        }
                    }
                }
            }
            return true; // PPs available
        }

        private bool InitFirstTime()
        {
            //System.err.println("InitFirstTime: doc: "+min.doc);
            checkedRpts = true;
            PlaceFirstPositions();

            var rptTerms = RepeatingTerms();
            hasRpts = rptTerms.Count > 0;

            if (hasRpts)
            {
                rptStack = new PhrasePositions[numPostings]; // needed with repetitions
                var rgs = GatherRptGroups(rptTerms);
                SortRptGroups(rgs);
                if (!AdvanceRepeatGroups())
                {
                    return false; // PPs exhausted
                }
            }

            FillQueue();
            return true; // PPs available
        }

        private sealed class AnonymousPhrasePositionsComparer : IComparer<PhrasePositions>
        {
            public int Compare(PhrasePositions pp1, PhrasePositions pp2)
            {
                return pp1.offset - pp2.offset;
            }
        }

        private void SortRptGroups(List<List<PhrasePositions>> rgs)
        {
            rptGroups = new PhrasePositions[rgs.Count][];
            var cmprtr = new AnonymousPhrasePositionsComparer();
            for (int i = 0; i < rptGroups.Length; i++)
            {
                PhrasePositions[] rg = rgs[i].ToArray();
                Array.Sort(rg, cmprtr);
                rptGroups[i] = rg;
                for (int j = 0; j < rg.Length; j++)
                {
                    rg[j].rptInd = j; // we use this index for efficient re-queuing
                }
            }
        }

        /** Detect repetition groups. Done once - for first doc */
        private List<List<PhrasePositions>> GatherRptGroups(HashMap<Term, int?> rptTerms)
        {
            var rpp = RepeatingPPs(rptTerms);
            var res = new List<List<PhrasePositions>>();
            if (!hasMultiTermRpts)
            {
                // simpler - no multi-terms - can base on positions in first doc
                for (int i = 0; i < rpp.Length; i++)
                {
                    var pp = rpp[i];
                    if (pp.rptGroup >= 0) continue; // already marked as a repetition
                    var tpPos = TpPos(pp);
                    for (var j = i + 1; j < rpp.Length; j++)
                    {
                        var pp2 = rpp[j];
                        if (
                            pp2.rptGroup >= 0        // already marked as a repetition
                            || pp2.offset == pp.offset // not a repetition: two PPs are originally in same offset in the query! 
                            || TpPos(pp2) != tpPos)
                        {  // not a repetition
                            continue;
                        }
                        // a repetition
                        var g = pp.rptGroup;
                        if (g < 0)
                        {
                            g = res.Count;
                            pp.rptGroup = g;
                            var rl = new List<PhrasePositions>(2) {pp};
                            res.Add(rl);
                        }
                        pp2.rptGroup = g;
                        res[g].Add(pp2);
                    }
                }
            }
            else
            {
                // more involved - has multi-terms
                var tmp = new List<HashSet<PhrasePositions>>();
                var bb = PpTermsBitSets(rpp, rptTerms);
                UnionTermGroups(bb);
                var tg = TermGroups(rptTerms, bb);
                var distinctGroupIDs = new HashSet<int>(tg.Values);
                for (var i = 0; i < distinctGroupIDs.Count; i++)
                {
                    tmp.Add(new HashSet<PhrasePositions>());
                }
                foreach (var pp in rpp)
                {
                    foreach (var g in from t in pp.terms where rptTerms.ContainsKey(t) select tg[t])
                    {
                        tmp[g].Add(pp);
                        pp.rptGroup = g;
                    }
                }
                res.AddRange(tmp.Select(hs => new List<PhrasePositions>(hs)));
            }
            return res;
        }

        private int TpPos(PhrasePositions pp)
        {
            return pp.position + pp.offset;
        }

        private HashMap<Term, int?> RepeatingTerms()
        {
            var tord = new HashMap<Term, int?>();
            var tcnt = new HashMap<Term, int?>();
            for (PhrasePositions pp = min, prev = null; prev != max; pp = (prev = pp).next)
            { // iterate cyclic list: done once handled max
                foreach (var t in pp.terms)
                {
                    var cnt0 = tcnt[t];
                    var cnt = !cnt0.HasValue ? 1 : 1 + cnt0.Value;
                    tcnt.Add(t, cnt);
                    if (cnt == 2)
                    {
                        tord.Add(t, tord.Count);
                    }
                }
            }
            return tord;
        }

        private PhrasePositions[] RepeatingPPs(HashMap<Term, int?> rptTerms)
        {
            var rp = new List<PhrasePositions>();
            for (PhrasePositions pp = min, prev = null; prev != max; pp = (prev = pp).next)
            { // iterate cyclic list: done once handled max
                if (pp.terms.Any(t => rptTerms.ContainsKey(t)))
                {
                    rp.Add(pp);
                    hasMultiTermRpts |= (pp.terms.Length > 1);
                }
            }
            return rp.ToArray();
        }

        private List<OpenBitSet> PpTermsBitSets(PhrasePositions[] rpp, HashMap<Term, int?> tord)
        {
            var bb = new List<OpenBitSet>(rpp.Length);
            foreach (var pp in rpp)
            {
                var b = new OpenBitSet(tord.Count);
                var ord = new int?();
                foreach (var t in pp.terms.Where(t => (ord = tord[t]) != null))
                {
                    b.Set((long)ord);
                }
                bb.Add(b);
            }
            return bb;
        }

        private void UnionTermGroups(List<OpenBitSet> bb)
        {
            int incr;
            for (int i = 0; i < bb.Count - 1; i += incr)
            {
                incr = 1;
                int j = i + 1;
                while (j < bb.Count)
                {
                    if (bb[i].Intersects(bb[j]))
                    {
                        bb[i].Union(bb[j]);
                        bb.Remove(bb[j]);
                        incr = 0;
                    }
                    else
                    {
                        ++j;
                    }
                }
            }
        }

        private HashMap<Term, int> TermGroups(HashMap<Term, int?> tord, List<OpenBitSet> bb)
        {
            var tg = new HashMap<Term, int>();
            Term[] t = tord.Keys.ToArray();
            for (int i = 0; i < bb.Count; i++)
            { // i is the group no.
                var bits = bb[i].Iterator();
                int ord;
                while ((ord = bits.NextDoc()) != NO_MORE_DOCS)
                {
                    tg.Add(t[ord], i);
                }
            }
            return tg;
        }

        public override int Freq()
        {
            return numMatches;
        }

        float SloppyFreq()
        {
            return sloppyFreq;
        }

        private bool AdvanceMin(int target)
        {
            if (!min.SkipTo(target))
            {
                max.doc = NO_MORE_DOCS; // for further calls to docID() 
                return false;
            }
            min = min.next; // cyclic
            max = max.next; // cyclic
            return true;
        }

        public override int DocID
        {
            get { return max.doc; }
        }

        public override int NextDoc()
        {
            return Advance(max.doc + 1); // advance to the next doc after #docID()
        }

        public override float Score()
        {
            return docScorer.Score(max.doc, sloppyFreq);
        }

        public override int Advance(int target)
        {
            //assert target > docID();
            do
            {
                if (!AdvanceMin(target))
                {
                    return NO_MORE_DOCS;
                }
                while (min.doc < max.doc)
                {
                    if (!AdvanceMin(max.doc))
                    {
                        return NO_MORE_DOCS;
                    }
                }
                // found a doc with all of the terms
                sloppyFreq = phraseFreq(); // check for phrase
                target = min.doc + 1; // next target in case sloppyFreq is still 0
            } while (sloppyFreq == 0f);

            // found a match
            return max.doc;
        }

        public override long Cost
        {
            get { return cost; }
        }

        public override String ToString() { return "scorer(" + Weight + ")"; }
    }
}