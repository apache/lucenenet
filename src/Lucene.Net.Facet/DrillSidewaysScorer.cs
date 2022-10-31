// Lucene version compatibility level 4.8.1 + LUCENE-6001
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Facet
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
    using IBits = Lucene.Net.Util.IBits;
    using BulkScorer = Lucene.Net.Search.BulkScorer;
    using ICollector = Lucene.Net.Search.ICollector;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using Scorer = Lucene.Net.Search.Scorer;
    using Weight = Lucene.Net.Search.Weight;

    internal class DrillSidewaysScorer : BulkScorer
    {
        //private static boolean DEBUG = false;

        private readonly ICollector drillDownCollector;

        private readonly DocsAndCost[] dims;

        // DrillDown DocsEnums:
        private readonly Scorer baseScorer;

        private readonly AtomicReaderContext context;

        internal readonly bool scoreSubDocsAtOnce;

        private const int CHUNK = 2048;
        private const int MASK = CHUNK - 1;

        private int collectDocID = -1;
        private float collectScore;

        internal DrillSidewaysScorer(AtomicReaderContext context, Scorer baseScorer,
            ICollector drillDownCollector, DocsAndCost[] dims, bool scoreSubDocsAtOnce)
        {
            this.dims = dims;
            this.context = context;
            this.baseScorer = baseScorer;
            this.drillDownCollector = drillDownCollector;
            this.scoreSubDocsAtOnce = scoreSubDocsAtOnce;
        }

        public override bool Score(ICollector collector, int maxDoc)
        {
            if (maxDoc != int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDoc), "maxDoc must be System.Int32.MaxValue"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            //if (DEBUG) {
            //  System.out.println("\nscore: reader=" + context.reader());
            //}
            //System.out.println("score r=" + context.reader());
            FakeScorer scorer = new FakeScorer(this);
            collector.SetScorer(scorer);
            if (drillDownCollector != null)
            {
                drillDownCollector.SetScorer(scorer);
                drillDownCollector.SetNextReader(context);
            }
            foreach (DocsAndCost dim in dims)
            {
                dim.sidewaysCollector.SetScorer(scorer);
                dim.sidewaysCollector.SetNextReader(context);
            }

            // TODO: if we ever allow null baseScorer ... it will
            // mean we DO score docs out of order ... hmm, or if we
            // change up the order of the conjuntions below
            if (Debugging.AssertsEnabled) Debugging.Assert(baseScorer != null);

            // some scorers, eg ReqExlScorer, can hit NPE if cost is called after nextDoc
            long baseQueryCost = baseScorer.GetCost();

            int numDims = dims.Length;

            long drillDownCost = 0;
            for (int dim = 0; dim < numDims; dim++)
            {
                DocIdSetIterator disi = dims[dim].disi;
                if (dims[dim].bits is null && disi != null)
                {
                    drillDownCost += disi.GetCost();
                }
            }

            long drillDownAdvancedCost = 0;
            if (numDims > 1 && dims[1].disi != null)
            {
                drillDownAdvancedCost = dims[1].disi.GetCost();
            }

            // Position all scorers to their first matching doc:
            baseScorer.NextDoc();
            int numBits = 0;
            foreach (DocsAndCost dim in dims)
            {
                if (dim.disi != null)
                {
                    dim.disi.NextDoc();
                }
                else if (dim.bits != null)
                {
                    numBits++;
                }
            }

            IBits[] bits = new IBits[numBits];
            ICollector[] bitsSidewaysCollectors = new ICollector[numBits];

            DocIdSetIterator[] disis = new DocIdSetIterator[numDims - numBits];
            ICollector[] sidewaysCollectors = new ICollector[numDims - numBits];
            int disiUpto = 0;
            int bitsUpto = 0;
            for (int dim = 0; dim < numDims; dim++)
            {
                DocIdSetIterator disi = dims[dim].disi;
                if (dims[dim].bits is null)
                {
                    disis[disiUpto] = disi;
                    sidewaysCollectors[disiUpto] = dims[dim].sidewaysCollector;
                    disiUpto++;
                }
                else
                {
                    bits[bitsUpto] = dims[dim].bits;
                    bitsSidewaysCollectors[bitsUpto] = dims[dim].sidewaysCollector;
                    bitsUpto++;
                }
            }

            /*
            System.out.println("\nbaseDocID=" + baseScorer.docID() + " est=" + estBaseHitCount);
            System.out.println("  maxDoc=" + context.reader().maxDoc());
            System.out.println("  maxCost=" + maxCost);
            System.out.println("  dims[0].freq=" + dims[0].freq);
            if (numDims > 1) {
              System.out.println("  dims[1].freq=" + dims[1].freq);
            }
            */

            if (bitsUpto > 0 || scoreSubDocsAtOnce || baseQueryCost < drillDownCost / 10)
            {
                //System.out.println("queryFirst: baseScorer=" + baseScorer + " disis.length=" + disis.length + " bits.length=" + bits.length);
                DoQueryFirstScoring(collector, disis, sidewaysCollectors, bits, bitsSidewaysCollectors);
            }
            else if (numDims > 1 && (dims[1].disi is null || drillDownAdvancedCost < baseQueryCost / 10))
            {
                //System.out.println("drillDownAdvance");
                DoDrillDownAdvanceScoring(collector, disis, sidewaysCollectors);
            }
            else
            {
                //System.out.println("union");
                DoUnionScoring(collector, disis, sidewaysCollectors);
            }

            return false;
        }

        /// <summary>
        /// Used when base query is highly constraining vs the
        /// drilldowns, or when the docs must be scored at once
        /// (i.e., like <see cref="Search.BooleanScorer2"/>, not <see cref="Search.BooleanScorer"/>).  In
        /// this case we just .Next() on base and .Advance() on
        /// the dim filters.
        /// </summary>
        private void DoQueryFirstScoring(ICollector collector, DocIdSetIterator[] disis,
            ICollector[] sidewaysCollectors, IBits[] bits, ICollector[] bitsSidewaysCollectors)
        {
            //if (DEBUG) {
            //  System.out.println("  doQueryFirstScoring");
            //}
            int docID = baseScorer.DocID;

            while (docID != DocsEnum.NO_MORE_DOCS)
            {
                ICollector failedCollector = null;
                for (int i = 0; i < disis.Length; i++)
                {
                    // TODO: should we sort this 2nd dimension of
                    // docsEnums from most frequent to least?
                    DocIdSetIterator disi = disis[i];
                    if (disi != null && disi.DocID < docID)
                    {
                        disi.Advance(docID);
                    }
                    if (disi is null || disi.DocID > docID)
                    {
                        if (failedCollector != null)
                        {
                            // More than one dim fails on this document, so
                            // it's neither a hit nor a near-miss; move to
                            // next doc:
                            docID = baseScorer.NextDoc();
                            goto nextDocContinue;
                        }
                        else
                        {
                            failedCollector = sidewaysCollectors[i];
                        }
                    }
                }

                // TODO: for the "non-costly Bits" we really should
                // have passed them down as acceptDocs, but
                // unfortunately we cannot distinguish today betwen
                // "bits() is so costly that you should apply it last"
                // from "bits() is so cheap that you should apply it
                // everywhere down low"

                // Fold in Filter Bits last, since they may be costly:
                for (int i = 0; i < bits.Length; i++)
                {
                    if (bits[i].Get(docID) == false)
                    {
                        if (failedCollector != null)
                        {
                            // More than one dim fails on this document, so
                            // it's neither a hit nor a near-miss; move to
                            // next doc:
                            docID = baseScorer.NextDoc();
                            goto nextDocContinue;
                        }
                        else
                        {
                            failedCollector = bitsSidewaysCollectors[i];
                        }
                    }
                }

                collectDocID = docID;

                // TODO: we could score on demand instead since we are
                // daat here:
                collectScore = baseScorer.GetScore();

                if (failedCollector is null)
                {
                    // Hit passed all filters, so it's "real":
                    CollectHit(collector, sidewaysCollectors, bitsSidewaysCollectors);
                }
                else
                {
                    // Hit missed exactly one filter:
                    CollectNearMiss(failedCollector);
                }

                docID = baseScorer.NextDoc();
                nextDocContinue: {/* LUCENENET: intentionally blank */}
            }
            //nextDocBreak: // Not referenced
        }

        /// <summary>
        /// Used when drill downs are highly constraining vs
        /// baseQuery.
        /// </summary>
        private void DoDrillDownAdvanceScoring(ICollector collector, DocIdSetIterator[] disis, ICollector[] sidewaysCollectors)
        {
            int maxDoc = context.Reader.MaxDoc;
            int numDims = dims.Length;

            //if (DEBUG) {
            //  System.out.println("  doDrillDownAdvanceScoring");
            //}

            // TODO: maybe a class like BS, instead of parallel arrays
            int[] filledSlots = new int[CHUNK];
            int[] docIDs = new int[CHUNK];
            float[] scores = new float[CHUNK];
            int[] missingDims = new int[CHUNK];
            int[] counts = new int[CHUNK];

            docIDs[0] = -1;
            int nextChunkStart = CHUNK;

            FixedBitSet seen = new FixedBitSet(CHUNK);

            while (true)
            {
                //if (DEBUG) {
                //  System.out.println("\ncycle nextChunkStart=" + nextChunkStart + " docIds[0]=" + docIDs[0]);
                //}

                // First dim:
                //if (DEBUG) {
                //  System.out.println("  dim0");
                //}
                DocIdSetIterator disi = disis[0];
                if (disi != null)
                {
                    int docID = disi.DocID;
                    while (docID < nextChunkStart)
                    {
                        int slot = docID & MASK;

                        if (docIDs[slot] != docID)
                        {
                            seen.Set(slot);
                            // Mark slot as valid:
                            //if (DEBUG) {
                            //  System.out.println("    set docID=" + docID + " id=" + context.reader().document(docID).get("id"));
                            //}
                            docIDs[slot] = docID;
                            missingDims[slot] = 1;
                            counts[slot] = 1;
                        }

                        docID = disi.NextDoc();
                    }
                }

                // Second dim:
                //if (DEBUG) {
                //  System.out.println("  dim1");
                //}
                disi = disis[1];
                if (disi != null)
                {
                    int docID = disi.DocID;
                    while (docID < nextChunkStart)
                    {
                        int slot = docID & MASK;

                        if (docIDs[slot] != docID)
                        {
                            // Mark slot as valid:
                            seen.Set(slot);
                            //if (DEBUG) {
                            //  System.out.println("    set docID=" + docID + " missingDim=0 id=" + context.reader().document(docID).get("id"));
                            //}
                            docIDs[slot] = docID;
                            missingDims[slot] = 0;
                            counts[slot] = 1;
                        }
                        else
                        {
                            // TODO: single-valued dims will always be true
                            // below; we could somehow specialize
                            if (missingDims[slot] >= 1)
                            {
                                missingDims[slot] = 2;
                                counts[slot] = 2;
                                //if (DEBUG) {
                                //  System.out.println("    set docID=" + docID + " missingDim=2 id=" + context.reader().document(docID).get("id"));
                                //}
                            }
                            else
                            {
                                counts[slot] = 1;
                                //if (DEBUG) {
                                //  System.out.println("    set docID=" + docID + " missingDim=" + missingDims[slot] + " id=" + context.reader().document(docID).get("id"));
                                //}
                            }
                        }

                        docID = disi.NextDoc();
                    }
                }

                // After this we can "upgrade" to conjunction, because
                // any doc not seen by either dim 0 or dim 1 cannot be
                // a hit or a near miss:

                //if (DEBUG) {
                //  System.out.println("  baseScorer");
                //}

                // Fold in baseScorer, using advance:
                int filledCount = 0;
                int slot0 = 0;
                while (slot0 < CHUNK && (slot0 = seen.NextSetBit(slot0)) != -1)
                {
                    int ddDocID = docIDs[slot0];
                    if (Debugging.AssertsEnabled) Debugging.Assert(ddDocID != -1);

                    int baseDocID = baseScorer.DocID;
                    if (baseDocID < ddDocID)
                    {
                        baseDocID = baseScorer.Advance(ddDocID);
                    }
                    if (baseDocID == ddDocID)
                    {
                        //if (DEBUG) {
                        //  System.out.println("    keep docID=" + ddDocID + " id=" + context.reader().document(ddDocID).get("id"));
                        //}
                        scores[slot0] = baseScorer.GetScore();
                        filledSlots[filledCount++] = slot0;
                        counts[slot0]++;
                    }
                    else
                    {
                        //if (DEBUG) {
                        //  System.out.println("    no docID=" + ddDocID + " id=" + context.reader().document(ddDocID).get("id"));
                        //}
                        docIDs[slot0] = -1;

                        // TODO: we could jump slot0 forward to the
                        // baseDocID ... but we'd need to set docIDs for
                        // intervening slots to -1
                    }
                    slot0++;
                }
                seen.Clear(0, CHUNK);

                if (filledCount == 0)
                {
                    if (nextChunkStart >= maxDoc)
                    {
                        break;
                    }
                    nextChunkStart += CHUNK;
                    continue;
                }

                // TODO: factor this out & share w/ union scorer,
                // except we start from dim=2 instead:
                for (int dim = 2; dim < numDims; dim++)
                {
                    //if (DEBUG) {
                    //  System.out.println("  dim=" + dim + " [" + dims[dim].dim + "]");
                    //}
                    disi = disis[dim];
                    if (disi != null)
                    {
                        int docID = disi.DocID;
                        while (docID < nextChunkStart)
                        {
                            int slot = docID & MASK;
                            if (docIDs[slot] == docID && counts[slot] >= dim)
                            {
                                // TODO: single-valued dims will always be true
                                // below; we could somehow specialize
                                if (missingDims[slot] >= dim)
                                {
                                    //if (DEBUG) {
                                    //  System.out.println("    set docID=" + docID + " count=" + (dim+2));
                                    //}
                                    missingDims[slot] = dim + 1;
                                    counts[slot] = dim + 2;
                                }
                                else
                                {
                                    //if (DEBUG) {
                                    //  System.out.println("    set docID=" + docID + " missing count=" + (dim+1));
                                    //}
                                    counts[slot] = dim + 1;
                                }
                            }

                            // TODO: sometimes use advance?
                            docID = disi.NextDoc();
                        }
                    }
                }

                // Collect:
                //if (DEBUG) {
                //  System.out.println("  now collect: " + filledCount + " hits");
                //}
                for (int i = 0; i < filledCount; i++)
                {
                    int slot = filledSlots[i];
                    collectDocID = docIDs[slot];
                    collectScore = scores[slot];
                    //if (DEBUG) {
                    //  System.out.println("    docID=" + docIDs[slot] + " count=" + counts[slot]);
                    //}
                    if (counts[slot] == 1 + numDims)
                    {
                        CollectHit(collector, sidewaysCollectors);
                    }
                    else if (counts[slot] == numDims)
                    {
                        CollectNearMiss(sidewaysCollectors[missingDims[slot]]);
                    }
                }

                if (nextChunkStart >= maxDoc)
                {
                    break;
                }

                nextChunkStart += CHUNK;
            }
        }

        private void DoUnionScoring(ICollector collector, DocIdSetIterator[] disis, ICollector[] sidewaysCollectors)
        {
            //if (DEBUG) {
            //  System.out.println("  doUnionScoring");
            //}

            int maxDoc = context.Reader.MaxDoc;
            int numDims = dims.Length;

            // TODO: maybe a class like BS, instead of parallel arrays
            int[] filledSlots = new int[CHUNK];
            int[] docIDs = new int[CHUNK];
            float[] scores = new float[CHUNK];
            int[] missingDims = new int[CHUNK];
            int[] counts = new int[CHUNK];

            docIDs[0] = -1;

            // NOTE: this is basically a specialized version of
            // BooleanScorer, to the minShouldMatch=N-1 case, but
            // carefully tracking which dimension failed to match

            int nextChunkStart = CHUNK;

            while (true)
            {
                //if (DEBUG) {
                //  System.out.println("\ncycle nextChunkStart=" + nextChunkStart + " docIds[0]=" + docIDs[0]);
                //}
                int filledCount = 0;
                int docID = baseScorer.DocID;
                //if (DEBUG) {
                //  System.out.println("  base docID=" + docID);
                //}
                while (docID < nextChunkStart)
                {
                    int slot = docID & MASK;
                    //if (DEBUG) {
                    //  System.out.println("    docIDs[slot=" + slot + "]=" + docID + " id=" + context.reader().document(docID).get("id"));
                    //}

                    // Mark slot as valid:
                    if (Debugging.AssertsEnabled) Debugging.Assert(docIDs[slot] != docID, "slot={0} docID={1}", slot, docID);
                    docIDs[slot] = docID;
                    scores[slot] = baseScorer.GetScore();
                    filledSlots[filledCount++] = slot;
                    missingDims[slot] = 0;
                    counts[slot] = 1;

                    docID = baseScorer.NextDoc();
                }

                if (filledCount == 0)
                {
                    if (nextChunkStart >= maxDoc)
                    {
                        break;
                    }
                    nextChunkStart += CHUNK;
                    continue;
                }

                // First drill-down dim, basically adds SHOULD onto
                // the baseQuery:
                //if (DEBUG) {
                //  System.out.println("  dim=0 [" + dims[0].dim + "]");
                //}
                DocIdSetIterator disi = disis[0];
                if (disi != null)
                {
                    docID = disi.DocID;
                    //if (DEBUG) {
                    //  System.out.println("    start docID=" + docID);
                    //}
                    while (docID < nextChunkStart)
                    {
                        int slot = docID & MASK;
                        if (docIDs[slot] == docID)
                        {
                            //if (DEBUG) {
                            //  System.out.println("      set docID=" + docID + " count=2");
                            //}
                            missingDims[slot] = 1;
                            counts[slot] = 2;
                        }
                        docID = disi.NextDoc();
                    }
                }

                for (int dim = 1; dim < numDims; dim++)
                {
                    //if (DEBUG) {
                    //  System.out.println("  dim=" + dim + " [" + dims[dim].dim + "]");
                    //}

                    disi = disis[dim];
                    if (disi != null)
                    {
                        docID = disi.DocID;
                        //if (DEBUG) {
                        //  System.out.println("    start docID=" + docID);
                        //}
                        while (docID < nextChunkStart)
                        {
                            int slot = docID & MASK;
                            if (docIDs[slot] == docID && counts[slot] >= dim)
                            {
                                // This doc is still in the running...
                                // TODO: single-valued dims will always be true
                                // below; we could somehow specialize
                                if (missingDims[slot] >= dim)
                                {
                                    //if (DEBUG) {
                                    //  System.out.println("      set docID=" + docID + " count=" + (dim+2));
                                    //}
                                    missingDims[slot] = dim + 1;
                                    counts[slot] = dim + 2;
                                }
                                else
                                {
                                    //if (DEBUG) {
                                    //  System.out.println("      set docID=" + docID + " missing count=" + (dim+1));
                                    //}
                                    counts[slot] = dim + 1;
                                }
                            }
                            docID = disi.NextDoc();
                        }
                    }
                }

                // Collect:
                //System.out.println("  now collect: " + filledCount + " hits");
                for (int i = 0; i < filledCount; i++)
                {
                    // NOTE: This is actually in-order collection,
                    // because we only accept docs originally returned by
                    // the baseScorer (ie that Scorer is AND'd)
                    int slot = filledSlots[i];
                    collectDocID = docIDs[slot];
                    collectScore = scores[slot];
                    //if (DEBUG) {
                    //  System.out.println("    docID=" + docIDs[slot] + " count=" + counts[slot]);
                    //}
                    //System.out.println("  collect doc=" + collectDocID + " main.freq=" + (counts[slot]-1) + " main.doc=" + collectDocID + " exactCount=" + numDims);
                    if (counts[slot] == 1 + numDims)
                    {
                        //System.out.println("    hit");
                        CollectHit(collector, sidewaysCollectors);
                    }
                    else if (counts[slot] == numDims)
                    {
                        //System.out.println("    sw");
                        CollectNearMiss(sidewaysCollectors[missingDims[slot]]);
                    }
                }

                if (nextChunkStart >= maxDoc)
                {
                    break;
                }

                nextChunkStart += CHUNK;
            }
        }

        private void CollectHit(ICollector collector, ICollector[] sidewaysCollectors)
        {
            //if (DEBUG) {
            //  System.out.println("      hit");
            //}

            collector.Collect(collectDocID);
            if (drillDownCollector != null)
            {
                drillDownCollector.Collect(collectDocID);
            }

            // TODO: we could "fix" faceting of the sideways counts
            // to do this "union" (of the drill down hits) in the
            // end instead:

            // Tally sideways counts:
            for (int dim = 0; dim < sidewaysCollectors.Length; dim++)
            {
                sidewaysCollectors[dim].Collect(collectDocID);
            }
        }

        private void CollectHit(ICollector collector, ICollector[] sidewaysCollectors, ICollector[] sidewaysCollectors2)
        {
            //if (DEBUG) {
            //  System.out.println("      hit");
            //}

            collector.Collect(collectDocID);
            if (drillDownCollector != null)
            {
                drillDownCollector.Collect(collectDocID);
            }

            // TODO: we could "fix" faceting of the sideways counts
            // to do this "union" (of the drill down hits) in the
            // end instead:

            // Tally sideways counts:
            for (int i = 0; i < sidewaysCollectors.Length; i++)
            {
                sidewaysCollectors[i].Collect(collectDocID);
            }
            for (int i = 0; i < sidewaysCollectors2.Length; i++)
            {
                sidewaysCollectors2[i].Collect(collectDocID);
            }
        }

        private void CollectNearMiss(ICollector sidewaysCollector)
        {
            //if (DEBUG) {
            //  System.out.println("      missingDim=" + dim);
            //}
            sidewaysCollector.Collect(collectDocID);
        }

        private sealed class FakeScorer : Scorer
        {
            private readonly DrillSidewaysScorer outerInstance;

            //internal float score; // not used
            //internal int doc; // not used

            public FakeScorer(DrillSidewaysScorer outerInstance)
                : base(null)
            {
                this.outerInstance = outerInstance;
            }

            public override int Advance(int target)
            {
                throw UnsupportedOperationException.Create("FakeScorer doesn't support Advance(int)");
            }

            public override int DocID => outerInstance.collectDocID;

            public override int Freq => 1 + outerInstance.dims.Length;

            public override int NextDoc()
            {
                throw UnsupportedOperationException.Create("FakeScorer doesn't support NextDoc()");
            }

            public override float GetScore()
            {
                return outerInstance.collectScore;
            }

            public override long GetCost()
            {
                return outerInstance.baseScorer.GetCost();
            }

            public override ICollection<ChildScorer> GetChildren()
            {
                return new[] { new Scorer.ChildScorer(outerInstance.baseScorer, "MUST") };
            }

            public override Weight Weight => throw UnsupportedOperationException.Create();
        }

        internal class DocsAndCost : IComparable<DocsAndCost>
        {
            // Iterator for docs matching this dim's filter, or ...
            internal DocIdSetIterator disi;
            // Random access bits:
            internal IBits bits;
            internal ICollector sidewaysCollector;
            internal string dim;

            public virtual int CompareTo(DocsAndCost other)
            {
                if (disi is null)
                {
                    if (other.disi is null)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else if (other.disi is null)
                {
                    return -1;
                }
                else if (disi.GetCost() < other.disi.GetCost())
                {
                    return -1;
                }
                else if (disi.GetCost() > other.disi.GetCost())
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}