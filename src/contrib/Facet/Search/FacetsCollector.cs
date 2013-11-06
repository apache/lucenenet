using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public abstract class FacetsCollector : Collector
    {
        private sealed class DocsAndScoresCollector : FacetsCollector
        {
            private AtomicReaderContext context;
            private Scorer scorer;
            private FixedBitSet bits;
            private int totalHits;
            private float[] scores;

            public DocsAndScoresCollector(FacetsAccumulator accumulator)
                : base(accumulator)
            {
            }

            protected override void Finish()
            {
                if (bits != null)
                {
                    matchingDocs.Add(new MatchingDocs(this.context, bits, totalHits, scores));
                    bits = null;
                    scores = null;
                    context = null;
                }
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get
                {
                    return false;
                }
            }

            public override void Collect(int doc)
            {
                bits.Set(doc);
                if (totalHits >= scores.Length)
                {
                    float[] newScores = new float[ArrayUtil.Oversize(totalHits + 1, 4)];
                    Array.Copy(scores, 0, newScores, 0, totalHits);
                    scores = newScores;
                }

                scores[totalHits] = scorer.Score();
                totalHits++;
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            protected override void DoSetNextReader(AtomicReaderContext context)
            {
                if (bits != null)
                {
                    matchingDocs.Add(new MatchingDocs(this.context, bits, totalHits, scores));
                }

                bits = new FixedBitSet(context.AtomicReader.MaxDoc);
                totalHits = 0;
                scores = new float[64];
                this.context = context;
            }
        }

        private sealed class DocsOnlyCollector : FacetsCollector
        {
            private AtomicReaderContext context;
            private FixedBitSet bits;
            private int totalHits;

            public DocsOnlyCollector(FacetsAccumulator accumulator)
                : base(accumulator)
            {
            }

            protected override void Finish()
            {
                if (bits != null)
                {
                    matchingDocs.Add(new MatchingDocs(this.context, bits, totalHits, null));
                    bits = null;
                    context = null;
                }
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get
                {
                    return true;
                }
            }

            public override void Collect(int doc)
            {
                totalHits++;
                bits.Set(doc);
            }

            public override void SetScorer(Scorer scorer)
            {
            }

            protected override void DoSetNextReader(AtomicReaderContext context)
            {
                if (bits != null)
                {
                    matchingDocs.Add(new MatchingDocs(this.context, bits, totalHits, null));
                }

                bits = new FixedBitSet(context.AtomicReader.MaxDoc);
                totalHits = 0;
                this.context = context;
            }
        }

        public sealed class MatchingDocs
        {
            public readonly AtomicReaderContext context;
            public readonly FixedBitSet bits;
            public readonly float[] scores;
            public readonly int totalHits;

            public MatchingDocs(AtomicReaderContext context, FixedBitSet bits, int totalHits, float[] scores)
            {
                this.context = context;
                this.bits = bits;
                this.scores = scores;
                this.totalHits = totalHits;
            }
        }

        public static FacetsCollector Create(FacetSearchParams fsp, IndexReader indexReader, TaxonomyReader taxoReader)
        {
            return Create(FacetsAccumulator.Create(fsp, indexReader, taxoReader));
        }

        public static FacetsCollector Create(FacetsAccumulator accumulator)
        {
            if (accumulator.Aggregator.RequiresDocScores)
            {
                return new DocsAndScoresCollector(accumulator);
            }
            else
            {
                return new DocsOnlyCollector(accumulator);
            }
        }

        private readonly FacetsAccumulator accumulator;
        private List<FacetResult> cachedResults;
        protected readonly List<MatchingDocs> matchingDocs = new List<MatchingDocs>();

        protected FacetsCollector(FacetsAccumulator accumulator)
        {
            this.accumulator = accumulator;
        }

        protected abstract void Finish();
        
        protected abstract void DoSetNextReader(AtomicReaderContext context);

        public List<FacetResult> GetFacetResults()
        {
            if (cachedResults == null)
            {
                Finish();
                cachedResults = accumulator.Accumulate(matchingDocs);
            }

            return cachedResults;
        }

        public List<MatchingDocs> GetMatchingDocs()
        {
            Finish();
            return matchingDocs;
        }

        public void Reset()
        {
            Finish();
            matchingDocs.Clear();
            cachedResults = null;
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
            cachedResults = null;
            DoSetNextReader(context);
        }
    }
}
