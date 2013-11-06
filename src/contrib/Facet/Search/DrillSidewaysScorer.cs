using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    internal class DrillSidewaysScorer : Scorer
    {
        private readonly Collector drillDownCollector;
        private readonly DocsEnumsAndFreq[] dims;
        private readonly Scorer baseScorer;
        private readonly AtomicReaderContext context;
        private static readonly int CHUNK = 2048;
        private static readonly int MASK = CHUNK - 1;
        private int collectDocID = -1;
        private float collectScore;

        internal DrillSidewaysScorer(Weight w, AtomicReaderContext context, Scorer baseScorer, Collector drillDownCollector, DocsEnumsAndFreq[] dims)
            : base(w)
        {
            this.dims = dims;
            this.context = context;
            this.baseScorer = baseScorer;
            this.drillDownCollector = drillDownCollector;
        }

        public override void Score(Collector collector)
        {
            collector.SetScorer(this);
            drillDownCollector.SetScorer(this);
            drillDownCollector.SetNextReader(context);
            foreach (DocsEnumsAndFreq dim in dims)
            {
                dim.sidewaysCollector.SetScorer(this);
                dim.sidewaysCollector.SetNextReader(context);
            }

            int baseDocID = baseScorer.NextDoc();
            foreach (DocsEnumsAndFreq dim in dims)
            {
                foreach (DocsEnum docsEnum in dim.docsEnums)
                {
                    if (docsEnum != null)
                    {
                        docsEnum.NextDoc();
                    }
                }
            }

            int numDims = dims.Length;
            DocsEnum[][] docsEnums = new DocsEnum[numDims][];
            Collector[] sidewaysCollectors = new Collector[numDims];
            int maxFreq = 0;
            for (int dim = 0; dim < numDims; dim++)
            {
                docsEnums[dim] = dims[dim].docsEnums;
                sidewaysCollectors[dim] = dims[dim].sidewaysCollector;
                maxFreq = Math.Max(maxFreq, dims[dim].freq);
            }

            int estBaseHitCount = context.AtomicReader.MaxDoc / (1 + baseDocID);
            if (estBaseHitCount < maxFreq / 10)
            {
                DoBaseAdvanceScoring(collector, docsEnums, sidewaysCollectors);
            }
            else if (numDims > 1 && (dims[1].freq < estBaseHitCount / 10))
            {
                DoDrillDownAdvanceScoring(collector, docsEnums, sidewaysCollectors);
            }
            else
            {
                DoUnionScoring(collector, docsEnums, sidewaysCollectors);
            }
        }

        private void DoDrillDownAdvanceScoring(Collector collector, DocsEnum[][] docsEnums, Collector[] sidewaysCollectors)
        {
            int maxDoc = context.AtomicReader.MaxDoc;
            int numDims = dims.Length;
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
                foreach (DocsEnum docsEnum in docsEnums[0])
                {
                    if (docsEnum == null)
                    {
                        continue;
                    }

                    int docID = docsEnum.DocID;
                    while (docID < nextChunkStart)
                    {
                        int slot = docID & MASK;
                        if (docIDs[slot] != docID)
                        {
                            seen.Set(slot);
                            docIDs[slot] = docID;
                            missingDims[slot] = 1;
                            counts[slot] = 1;
                        }

                        docID = docsEnum.NextDoc();
                    }
                }

                foreach (DocsEnum docsEnum in docsEnums[1])
                {
                    if (docsEnum == null)
                    {
                        continue;
                    }

                    int docID = docsEnum.DocID;
                    while (docID < nextChunkStart)
                    {
                        int slot = docID & MASK;
                        if (docIDs[slot] != docID)
                        {
                            seen.Set(slot);
                            docIDs[slot] = docID;
                            missingDims[slot] = 0;
                            counts[slot] = 1;
                        }
                        else
                        {
                            if (missingDims[slot] >= 1)
                            {
                                missingDims[slot] = 2;
                                counts[slot] = 2;
                            }
                            else
                            {
                                counts[slot] = 1;
                            }
                        }

                        docID = docsEnum.NextDoc();
                    }
                }

                int filledCount = 0;
                int slot0 = 0;
                while (slot0 < CHUNK && (slot0 = seen.NextSetBit(slot0)) != -1)
                {
                    int ddDocID = docIDs[slot0];
                    int baseDocID = baseScorer.DocID;
                    if (baseDocID < ddDocID)
                    {
                        baseDocID = baseScorer.Advance(ddDocID);
                    }

                    if (baseDocID == ddDocID)
                    {
                        scores[slot0] = baseScorer.Score();
                        filledSlots[filledCount++] = slot0;
                        counts[slot0]++;
                    }
                    else
                    {
                        docIDs[slot0] = -1;
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

                for (int dim = 2; dim < numDims; dim++)
                {
                    foreach (DocsEnum docsEnum in docsEnums[dim])
                    {
                        if (docsEnum == null)
                        {
                            continue;
                        }

                        int docID = docsEnum.DocID;
                        while (docID < nextChunkStart)
                        {
                            int slot = docID & MASK;
                            if (docIDs[slot] == docID && counts[slot] >= dim)
                            {
                                if (missingDims[slot] >= dim)
                                {
                                    missingDims[slot] = dim + 1;
                                    counts[slot] = dim + 2;
                                }
                                else
                                {
                                    counts[slot] = dim + 1;
                                }
                            }

                            docID = docsEnum.NextDoc();
                        }
                    }
                }

                for (int i = 0; i < filledCount; i++)
                {
                    int slot = filledSlots[i];
                    collectDocID = docIDs[slot];
                    collectScore = scores[slot];
                    if (counts[slot] == 1 + numDims)
                    {
                        CollectHit(collector, sidewaysCollectors);
                    }
                    else if (counts[slot] == numDims)
                    {
                        CollectNearMiss(sidewaysCollectors, missingDims[slot]);
                    }
                }

                if (nextChunkStart >= maxDoc)
                {
                    break;
                }

                nextChunkStart += CHUNK;
            }
        }

        private void DoBaseAdvanceScoring(Collector collector, DocsEnum[][] docsEnums, Collector[] sidewaysCollectors)
        {
            int docID = baseScorer.DocID;
            int numDims = dims.Length;
        
            while (docID != NO_MORE_DOCS)
            {
                int failedDim = -1;
                bool shouldContinueOuter = false;

                for (int dim = 0; dim < numDims; dim++)
                {
                    bool found = false;
                    foreach (DocsEnum docsEnum in docsEnums[dim])
                    {
                        if (docsEnum == null)
                        {
                            continue;
                        }

                        if (docsEnum.DocID < docID)
                        {
                            docsEnum.Advance(docID);
                        }

                        if (docsEnum.DocID == docID)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        if (failedDim != -1)
                        {
                            docID = baseScorer.NextDoc();
                            shouldContinueOuter = true;
                            break;
                        }
                        else
                        {
                            failedDim = dim;
                        }
                    }
                }

                if (shouldContinueOuter)
                    continue;

                collectDocID = docID;
                collectScore = baseScorer.Score();
                if (failedDim == -1)
                {
                    CollectHit(collector, sidewaysCollectors);
                }
                else
                {
                    CollectNearMiss(sidewaysCollectors, failedDim);
                }

                docID = baseScorer.NextDoc();
            }
        }

        private void CollectHit(Collector collector, Collector[] sidewaysCollectors)
        {
            collector.Collect(collectDocID);
            drillDownCollector.Collect(collectDocID);
            for (int dim = 0; dim < sidewaysCollectors.Length; dim++)
            {
                sidewaysCollectors[dim].Collect(collectDocID);
            }
        }

        private void CollectNearMiss(Collector[] sidewaysCollectors, int dim)
        {
            sidewaysCollectors[dim].Collect(collectDocID);
        }

        private void DoUnionScoring(Collector collector, DocsEnum[][] docsEnums, Collector[] sidewaysCollectors)
        {
            int maxDoc = context.AtomicReader.MaxDoc;
            int numDims = dims.Length;
            int[] filledSlots = new int[CHUNK];
            int[] docIDs = new int[CHUNK];
            float[] scores = new float[CHUNK];
            int[] missingDims = new int[CHUNK];
            int[] counts = new int[CHUNK];
            docIDs[0] = -1;
            int nextChunkStart = CHUNK;
            while (true)
            {
                int filledCount = 0;
                int docID = baseScorer.DocID;
                while (docID < nextChunkStart)
                {
                    int slot = docID & MASK;
                    docIDs[slot] = docID;
                    scores[slot] = baseScorer.Score();
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

                foreach (DocsEnum docsEnum in docsEnums[0])
                {
                    if (docsEnum == null)
                    {
                        continue;
                    }

                    docID = docsEnum.DocID;
                    while (docID < nextChunkStart)
                    {
                        int slot = docID & MASK;
                        if (docIDs[slot] == docID)
                        {
                            missingDims[slot] = 1;
                            counts[slot] = 2;
                        }

                        docID = docsEnum.NextDoc();
                    }
                }

                for (int dim = 1; dim < numDims; dim++)
                {
                    foreach (DocsEnum docsEnum in docsEnums[dim])
                    {
                        if (docsEnum == null)
                        {
                            continue;
                        }

                        docID = docsEnum.DocID;
                        while (docID < nextChunkStart)
                        {
                            int slot = docID & MASK;
                            if (docIDs[slot] == docID && counts[slot] >= dim)
                            {
                                if (missingDims[slot] >= dim)
                                {
                                    missingDims[slot] = dim + 1;
                                    counts[slot] = dim + 2;
                                }
                                else
                                {
                                    counts[slot] = dim + 1;
                                }
                            }

                            docID = docsEnum.NextDoc();
                        }
                    }
                }

                for (int i = 0; i < filledCount; i++)
                {
                    int slot = filledSlots[i];
                    collectDocID = docIDs[slot];
                    collectScore = scores[slot];
                    if (counts[slot] == 1 + numDims)
                    {
                        CollectHit(collector, sidewaysCollectors);
                    }
                    else if (counts[slot] == numDims)
                    {
                        CollectNearMiss(sidewaysCollectors, missingDims[slot]);
                    }
                }

                if (nextChunkStart >= maxDoc)
                {
                    break;
                }

                nextChunkStart += CHUNK;
            }
        }

        public override int DocID
        {
            get
            {
                return collectDocID;
            }
        }

        public override float Score()
        {
            return collectScore;
        }

        public override int Freq
        {
            get
            {
                return 1 + dims.Length;
            }
        }

        public override int NextDoc()
        {
            throw new NotSupportedException();
        }

        public override int Advance(int target)
        {
            throw new NotSupportedException();
        }

        public override long Cost
        {
            get
            {
                return baseScorer.Cost;
            }
        }

        public override ICollection<ChildScorer> Children
        {
            get
            {
                return new List<ChildScorer>() { new ChildScorer(baseScorer, @"MUST") };
            }
        }

        internal class DocsEnumsAndFreq : IComparable<DocsEnumsAndFreq>
        {
            internal DocsEnum[] docsEnums;
            internal int freq;
            internal Collector sidewaysCollector;
            internal string dim;

            public int CompareTo(DocsEnumsAndFreq other)
            {
                return freq - other.freq;
            }
        }
    }
}
