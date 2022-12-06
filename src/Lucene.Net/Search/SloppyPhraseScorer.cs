using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Term = Lucene.Net.Index.Term;

    internal sealed class SloppyPhraseScorer : Scorer
    {
        private PhrasePositions min, max;

        private float sloppyFreq; //phrase frequency in current doc as computed by phraseFreq().

        private readonly Similarity.SimScorer docScorer;

        private readonly int slop;
        private readonly int numPostings;
        private readonly PhraseQueue pq; // for advancing min position

        private int end; // current largest phrase position

        private bool hasRpts; // flag indicating that there are repetitions (as checked in first candidate doc)
        private bool checkedRpts; // flag to only check for repetitions in first candidate doc
        private bool hasMultiTermRpts;
        private PhrasePositions[][] rptGroups; // in each group are PPs that repeats each other (i.e. same term), sorted by (query) offset
        private PhrasePositions[] rptStack; // temporary stack for switching colliding repeating pps

        private int numMatches;
        private readonly long cost;

        internal SloppyPhraseScorer(Weight weight, PhraseQuery.PostingsAndFreq[] postings, int slop, Similarity.SimScorer docScorer)
            : base(weight)
        {
            this.docScorer = docScorer;
            this.slop = slop;
            this.numPostings = postings is null ? 0 : postings.Length;
            pq = new PhraseQueue(postings.Length);
            // min(cost)
            cost = postings[0].postings.GetCost();
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

        /// <summary>
        /// Score a candidate doc for all slop-valid position-combinations (matches)
        /// encountered while traversing/hopping the PhrasePositions.
        /// <para/> The score contribution of a match depends on the distance:
        /// <para/> - highest score for distance=0 (exact match).
        /// <para/> - score gets lower as distance gets higher.
        /// <para/>Example: for query "a b"~2, a document "x a b a y" can be scored twice:
        /// once for "a b" (distance=0), and once for "b a" (distance=2).
        /// <para/>Possibly not all valid combinations are encountered, because for efficiency
        /// we always propagate the least PhrasePosition. This allows to base on
        /// <see cref="Util.PriorityQueue{T}"/> and move forward faster.
        /// As result, for example, document "a b c b a"
        /// would score differently for queries "a b c"~4 and "c b a"~4, although
        /// they really are equivalent.
        /// Similarly, for doc "a b c b a f g", query "c b"~2
        /// would get same score as "g f"~2, although "c b"~2 could be matched twice.
        /// We may want to fix this in the future (currently not, for performance reasons).
        /// </summary>
        private float PhraseFreq()
        {
            if (!InitPhrasePositions())
            {
                return 0.0f;
            }
            float freq = 0.0f;
            numMatches = 0;
            PhrasePositions pp = pq.Pop();
            int matchLength = end - pp.position;
            int next = pq.Top.position;
            while (AdvancePP(pp))
            {
                if (hasRpts && !AdvanceRpts(pp))
                {
                    break; // pps exhausted
                }
                if (pp.position > next) // done minimizing current match-length
                {
                    if (matchLength <= slop)
                    {
                        freq += docScorer.ComputeSlopFactor(matchLength); // score match
                        numMatches++;
                    }
                    pq.Add(pp);
                    pp = pq.Pop();
                    next = pq.Top.position;
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
                freq += docScorer.ComputeSlopFactor(matchLength); // score match
                numMatches++;
            }
            return freq;
        }

        /// <summary>
        /// Advance a PhrasePosition and update 'end', return false if exhausted </summary>
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

        /// <summary>
        /// pp was just advanced. If that caused a repeater collision, resolve by advancing the lesser
        /// of the two colliding pps. Note that there can only be one collision, as by the initialization
        /// there were no collisions before pp was advanced.
        /// </summary>
        private bool AdvanceRpts(PhrasePositions pp)
        {
            if (pp.rptGroup < 0)
            {
                return true; // not a repeater
            }
            PhrasePositions[] rg = rptGroups[pp.rptGroup];
            FixedBitSet bits = new FixedBitSet(rg.Length); // for re-queuing after collisions are resolved
            int k0 = pp.rptInd;
            int k;
            while ((k = Collide(pp)) >= 0)
            {
                pp = Lesser(pp, rg[k]); // always advance the lesser of the (only) two colliding pps
                if (!AdvancePP(pp))
                {
                    return false; // exhausted
                }
                if (k != k0) // careful: mark only those currently in the queue
                {
                    bits = FixedBitSet.EnsureCapacity(bits, k);
                    bits.Set(k); // mark that pp2 need to be re-queued
                }
            }
            // collisions resolved, now re-queue
            // empty (partially) the queue until seeing all pps advanced for resolving collisions
            int n = 0;
            // TODO would be good if we can avoid calling cardinality() in each iteration!
            int numBits = bits.Length; // larges bit we set
            while (bits.Cardinality > 0)
            {
                PhrasePositions pp2 = pq.Pop();
                rptStack[n++] = pp2;
                if (pp2.rptGroup >= 0 && pp2.rptInd < numBits && bits.Get(pp2.rptInd)) // this bit may not have been set
                {
                    bits.Clear(pp2.rptInd);
                }
            }
            // add back to queue
            for (int i = n - 1; i >= 0; i--)
            {
                pq.Add(rptStack[i]);
            }
            return true;
        }

        /// <summary>
        /// Compare two pps, but only by position and offset </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PhrasePositions Lesser(PhrasePositions pp, PhrasePositions pp2) // LUCENENET: CA1822: Mark members as static
        {
            if (pp.position < pp2.position || (pp.position == pp2.position && pp.offset < pp2.offset))
            {
                return pp;
            }
            return pp2;
        }

        /// <summary>
        /// Index of a pp2 colliding with pp, or -1 if none </summary>
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

        /// <summary>
        /// Initialize <see cref="PhrasePositions"/> in place.
        /// A one time initialization for this scorer (on first doc matching all terms):
        /// <list type="bullet">
        ///     <item><description>Check if there are repetitions</description></item>
        ///     <item><description>If there are, find groups of repetitions.</description></item>
        /// </list>
        /// Examples:
        /// <list type="number">
        ///     <item><description>no repetitions: <b>"ho my"~2</b></description></item>
        ///     <item><description>>repetitions: <b>"ho my my"~2</b></description></item>
        ///     <item><description>repetitions: <b>"my ho my"~2</b></description></item>
        /// </list>
        /// </summary>
        /// <returns> <c>false</c> if PPs are exhausted (and so current doc will not be a match)  </returns>
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

        /// <summary>
        /// No repeats: simplest case, and most common. It is important to keep this piece of the code simple and efficient </summary>
        private void InitSimple()
        {
            //System.err.println("initSimple: doc: "+min.doc);
            pq.Clear();
            // position pps and build queue from list
            for (PhrasePositions pp = min, prev = null; prev != max; pp = (prev = pp).next) // iterate cyclic list: done once handled max
            {
                pp.FirstPosition();
                if (pp.position > end)
                {
                    end = pp.position;
                }
                pq.Add(pp);
            }
        }

        /// <summary>
        /// With repeats: not so simple. </summary>
        private bool InitComplex()
        {
            //System.err.println("initComplex: doc: "+min.doc);
            PlaceFirstPositions();
            if (!AdvanceRepeatGroups())
            {
                return false; // PPs exhausted
            }
            FillQueue();
            return true; // PPs available
        }

        /// <summary>
        /// Move all PPs to their first position </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PlaceFirstPositions()
        {
            for (PhrasePositions pp = min, prev = null; prev != max; pp = (prev = pp).next) // iterate cyclic list: done once handled max
            {
                pp.FirstPosition();
            }
        }

        /// <summary>
        /// Fill the queue (all pps are already placed) </summary>
        private void FillQueue()
        {
            pq.Clear();
            for (PhrasePositions pp = min, prev = null; prev != max; pp = (prev = pp).next) // iterate cyclic list: done once handled max
            {
                if (pp.position > end)
                {
                    end = pp.position;
                }
                pq.Add(pp);
            }
        }

        /// <summary>
        /// At initialization (each doc), each repetition group is sorted by (query) offset.
        /// this provides the start condition: no collisions.
        /// <para/>Case 1: no multi-term repeats
        /// <para/>
        /// It is sufficient to advance each pp in the group by one less than its group index.
        /// So lesser pp is not advanced, 2nd one advance once, 3rd one advanced twice, etc.
        /// <para/>Case 2: multi-term repeats
        /// </summary>
        /// <returns> <c>false</c> if PPs are exhausted.  </returns>
        private bool AdvanceRepeatGroups()
        {
            foreach (PhrasePositions[] rg in rptGroups)
            {
                if (hasMultiTermRpts)
                {
                    // more involved, some may not collide
                    int incr;
                    for (int i = 0; i < rg.Length; i += incr)
                    {
                        incr = 1;
                        PhrasePositions pp = rg[i];
                        int k;
                        while ((k = Collide(pp)) >= 0)
                        {
                            PhrasePositions pp2 = Lesser(pp, rg[k]);
                            if (!AdvancePP(pp2)) // at initialization always advance pp with higher offset
                            {
                                return false; // exhausted
                            }
                            if (pp2.rptInd < i) // should not happen?
                            {
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

        /// <summary>
        /// Initialize with checking for repeats. Heavy work, but done only for the first candidate doc.
        /// <para/>
        /// If there are repetitions, check if multi-term postings (MTP) are involved.
        /// <para/>
        /// Without MTP, once PPs are placed in the first candidate doc, repeats (and groups) are visible.
        /// <para/>
        /// With MTP, a more complex check is needed, up-front, as there may be "hidden collisions".
        /// <para/>
        /// For example P1 has {A,B}, P1 has {B,C}, and the first doc is: "A C B". At start, P1 would point
        /// to "A", p2 to "C", and it will not be identified that P1 and P2 are repetitions of each other.
        /// <para/>
        /// The more complex initialization has two parts:
        /// <para/>
        /// (1) identification of repetition groups.
        /// <para/>
        /// (2) advancing repeat groups at the start of the doc.
        /// <para/>
        /// For (1), a possible solution is to just create a single repetition group,
        /// made of all repeating pps. But this would slow down the check for collisions,
        /// as all pps would need to be checked. Instead, we compute "connected regions"
        /// on the bipartite graph of postings and terms.
        /// </summary>
        private bool InitFirstTime()
        {
            //System.err.println("initFirstTime: doc: "+min.doc);
            checkedRpts = true;
            PlaceFirstPositions();

            var rptTerms = RepeatingTerms();
            hasRpts = rptTerms.Count > 0;

            if (hasRpts)
            {
                rptStack = new PhrasePositions[numPostings]; // needed with repetitions
                IList<IList<PhrasePositions>> rgs = GatherRptGroups(rptTerms);
                SortRptGroups(rgs);
                if (!AdvanceRepeatGroups())
                {
                    return false; // PPs exhausted
                }
            }

            FillQueue();
            return true; // PPs available
        }

        /// <summary>
        /// Sort each repetition group by (query) offset.
        /// Done only once (at first doc) and allows to initialize faster for each doc.
        /// </summary>
        private void SortRptGroups(IList<IList<PhrasePositions>> rgs)
        {
            rptGroups = new PhrasePositions[rgs.Count][];
            IComparer<PhrasePositions> cmprtr = Comparer<PhrasePositions>.Create((pp1, pp2) => pp1.offset - pp2.offset);
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

        /// <summary>
        /// Detect repetition groups. Done once - for first doc. </summary>
        private IList<IList<PhrasePositions>> GatherRptGroups(JCG.LinkedDictionary<Term, int> rptTerms)
        {
            PhrasePositions[] rpp = RepeatingPPs(rptTerms);
            IList<IList<PhrasePositions>> res = new JCG.List<IList<PhrasePositions>>();
            if (!hasMultiTermRpts)
            {
                // simpler - no multi-terms - can base on positions in first doc
                for (int i = 0; i < rpp.Length; i++)
                {
                    PhrasePositions pp = rpp[i];
                    if (pp.rptGroup >= 0) // already marked as a repetition
                    {
                        continue;
                    }
                    int tpPos = TpPos(pp);
                    for (int j = i + 1; j < rpp.Length; j++)
                    {
                        PhrasePositions pp2 = rpp[j];
                        if (pp2.rptGroup >= 0 || pp2.offset == pp.offset || TpPos(pp2) != tpPos) // not a repetition -  not a repetition: two PPs are originally in same offset in the query! -  already marked as a repetition
                        {
                            continue;
                        }
                        // a repetition
                        int g = pp.rptGroup;
                        if (g < 0)
                        {
                            g = res.Count;
                            pp.rptGroup = g;
                            IList<PhrasePositions> rl = new JCG.List<PhrasePositions>(2)
                            {
                                pp
                            };
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
                IList<JCG.HashSet<PhrasePositions>> tmp = new JCG.List<JCG.HashSet<PhrasePositions>>();
                IList<FixedBitSet> bb = PpTermsBitSets(rpp, rptTerms);
                UnionTermGroups(bb);
                IDictionary<Term, int> tg = TermGroups(rptTerms, bb);
                JCG.HashSet<int> distinctGroupIDs = new JCG.HashSet<int>(tg.Values);
                for (int i = 0; i < distinctGroupIDs.Count; i++)
                {
                    tmp.Add(new JCG.HashSet<PhrasePositions>());
                }
                foreach (PhrasePositions pp in rpp)
                {
                    foreach (Term t in pp.terms)
                    {
                        if (rptTerms.ContainsKey(t))
                        {
                            int g = tg[t];
                            tmp[g].Add(pp);
                            if (Debugging.AssertsEnabled) Debugging.Assert(pp.rptGroup == -1 || pp.rptGroup == g);
                            pp.rptGroup = g;
                        }
                    }
                }
                foreach (JCG.HashSet<PhrasePositions> hs in tmp)
                {
                    res.Add(new JCG.List<PhrasePositions>(hs));
                }
            }
            return res;
        }

        /// <summary>
        /// Actual position in doc of a PhrasePosition, relies on that position = tpPos - offset) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TpPos(PhrasePositions pp) // LUCENENET: CA1822: Mark members as static
        {
            return pp.position + pp.offset;
        }

        /// <summary>
        /// Find repeating terms and assign them ordinal values </summary>
        private JCG.LinkedDictionary<Term, int> RepeatingTerms()
        {
            JCG.LinkedDictionary<Term, int> tord = new JCG.LinkedDictionary<Term, int>();
            Dictionary<Term, int> tcnt = new Dictionary<Term, int>();
            for (PhrasePositions pp = min, prev = null; prev != max; pp = (prev = pp).next) // iterate cyclic list: done once handled max
            {
                foreach (Term t in pp.terms)
                {
                    int cnt = !tcnt.TryGetValue(t, out int cnt0) ? 1 : 1 + cnt0;
                    tcnt[t] = cnt;
                    if (cnt == 2)
                    {
                        tord[t] = tord.Count;
                    }
                }
            }
            return tord;
        }

        /// <summary>
        /// Find repeating pps, and for each, if has multi-terms, update this.hasMultiTermRpts </summary>
        private PhrasePositions[] RepeatingPPs(IDictionary<Term, int> rptTerms)
        {
            IList<PhrasePositions> rp = new JCG.List<PhrasePositions>();
            for (PhrasePositions pp = min, prev = null; prev != max; pp = (prev = pp).next) // iterate cyclic list: done once handled max
            {
                foreach (Term t in pp.terms)
                {
                    if (rptTerms.ContainsKey(t))
                    {
                        rp.Add(pp);
                        hasMultiTermRpts |= (pp.terms.Length > 1);
                        break;
                    }
                }
            }
            return rp.ToArray();
        }

        /// <summary>
        /// bit-sets - for each repeating pp, for each of its repeating terms, the term ordinal values is set </summary>
        private static IList<FixedBitSet> PpTermsBitSets(PhrasePositions[] rpp, IDictionary<Term, int> tord) // LUCENENET: CA1822: Mark members as static
        {
            IList<FixedBitSet> bb = new JCG.List<FixedBitSet>(rpp.Length);
            foreach (PhrasePositions pp in rpp)
            {
                FixedBitSet b = new FixedBitSet(tord.Count);
                foreach (var t in pp.terms)
                {
                    if (tord.TryGetValue(t, out int ord))
                        b.Set(ord);
                }
                bb.Add(b);
            }
            return bb;
        }

        /// <summary>
        /// Union (term group) bit-sets until they are disjoint (O(n^^2)), and each group have different terms </summary>
        private static void UnionTermGroups(IList<FixedBitSet> bb) // LUCENENET: CA1822: Mark members as static
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
                        bb[i].Or(bb[j]);
                        bb.RemoveAt(j);
                        incr = 0;
                    }
                    else
                    {
                        ++j;
                    }
                }
            }
        }

        /// <summary>
        /// Map each term to the single group that contains it </summary>
        private static IDictionary<Term, int> TermGroups(JCG.LinkedDictionary<Term, int> tord, IList<FixedBitSet> bb) // LUCENENET: CA1822: Mark members as static
        {
            Dictionary<Term, int> tg = new Dictionary<Term, int>();
            Term[] t = tord.Keys.ToArray(/*new Term[0]*/);
            for (int i = 0; i < bb.Count; i++) // i is the group no.
            {
                DocIdSetIterator bits = bb[i].GetIterator();
                int ord;
                while ((ord = bits.NextDoc()) != NO_MORE_DOCS)
                {
                    tg[t[ord]] = i;
                }
            }
            return tg;
        }

        public override int Freq => numMatches;

        internal float SloppyFreq => sloppyFreq;

        //  private void printQueue(PrintStream ps, PhrasePositions ext, String title) {
        //    //if (min.doc != ?) return;
        //    ps.println();
        //    ps.println("---- "+title);
        //    ps.println("EXT: "+ext);
        //    PhrasePositions[] t = new PhrasePositions[pq.size()];
        //    if (pq.size()>0) {
        //      t[0] = pq.pop();
        //      ps.println("  " + 0 + "  " + t[0]);
        //      for (int i=1; i<t.length; i++) {
        //        t[i] = pq.pop();
        //        assert t[i-1].position <= t[i].position;
        //        ps.println("  " + i + "  " + t[i]);
        //      }
        //      // add them back
        //      for (int i=t.length-1; i>=0; i--) {
        //        pq.add(t[i]);
        //      }
        //    }
        //  }

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

        public override int DocID => max.doc;

        public override int NextDoc()
        {
            return Advance(max.doc + 1); // advance to the next doc after #docID()
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float GetScore()
        {
            return docScorer.Score(max.doc, sloppyFreq);
        }

        public override int Advance(int target)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(target > DocID);
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
                sloppyFreq = PhraseFreq(); // check for phrase
                target = min.doc + 1; // next target in case sloppyFreq is still 0
            } while (sloppyFreq == 0f);

            // found a match
            return max.doc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long GetCost()
        {
            return cost;
        }

        public override string ToString()
        {
            return "scorer(" + m_weight + ")";
        }
    }
}