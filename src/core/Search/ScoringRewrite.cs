using System;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
    public abstract class ScoringRewrite<Q> : TermCollectingRewrite<Q>
        where Q : Query
    {
        public static readonly ScoringRewrite<BooleanQuery> SCORING_BOOLEAN_QUERY_REWRITE = new AnonymounsScoringBooleanQueryRewrite();

        private class AnonymounsScoringBooleanQueryRewrite : ScoringRewrite<BooleanQuery>
        {
            protected override BooleanQuery TopLevelQuery
            {
                get
                {
                    return new BooleanQuery(true);
                }
            }

            protected override void AddClause(BooleanQuery topLevel, Term term, int docCount,
                float boost, TermContext states)
            {
                var tq = new TermQuery(term, states) {Boost = boost};
                topLevel.Add(tq, Occur.SHOULD);
            }

            protected override void CheckMaxClauseCount(int count)
            {
                if (count > BooleanQuery.MaxClauseCount)
                    throw new BooleanQuery.TooManyClauses();
            }
        }


        public static readonly MultiTermQuery.RewriteMethod CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE = new AnonymousConstantScoreBooleanQueryRewrite();

        private sealed class AnonymousConstantScoreBooleanQueryRewrite : MultiTermQuery.RewriteMethod
        {
            public override Query Rewrite(IndexReader reader, MultiTermQuery query) 
            {
              var bq = SCORING_BOOLEAN_QUERY_REWRITE.Rewrite(reader, query);
              // TODO: if empty boolean query return NullQuery?
              if (!bq.Clauses.Any())
                return bq;
              // strip the scores off
              var result = new ConstantScoreQuery(bq) {Boost = query.Boost};
                return result;
            }
        }

        protected abstract void CheckMaxClauseCount(int count);

        public override sealed Query Rewrite(IndexReader reader, MultiTermQuery query)
        {
            var result = GetTopLevelQuery();
            var col = new ParallelArraysTermCollector(this);
            CollectTerms(reader, query, col);

            var size = col.terms.Size;
            if (size > 0)
            {
                var sort = col.terms.Sort(col.termsEnum.Comparator);
                var boost = col.array.boost;
                var termStates = col.array.termState;
                for (var i = 0; i < size; i++)
                {
                    var pos = sort[i];
                    var term = new Term(query.Field, col.terms.Get(pos, new BytesRef()));
                    //assert reader.docFreq(term) == termStates[pos].docFreq();
                    AddClause(result, term, termStates[pos].DocFreq, query.Boost * boost[pos], termStates[pos]);
                }
            }
            return result;
        }

        internal sealed class ParallelArraysTermCollector : TermCollector
        {
            public TermFreqBoostByteStart array;
            public BytesRefHash terms;
            public TermsEnum termsEnum;

            private BoostAttribute boostAtt;

            private readonly ScoringRewrite<Q> parent;
            public ParallelArraysTermCollector(ScoringRewrite<Q> parent)
            {
                this.parent = parent;
                array = new TermFreqBoostByteStart(16);
                terms = new BytesRefHash(new ByteBlockPool(new ByteBlockPool.DirectAllocator()), 16, array);
            }

            public override void SetNextEnum(TermsEnum termsEnum)
            {
                this.termsEnum = termsEnum;
                this.boostAtt = termsEnum.Attributes.AddAttribute<BoostAttribute>();
            }

            public override bool Collect(BytesRef bytes)
            {
                var e = terms.Add(bytes);
                var state = termsEnum.TermState;
                //assert state != null; 
                if (e < 0)
                {
                    // duplicate term: update docFreq
                    var pos = (-e) - 1;
                    array.termState[pos].Register(state, readerContext.ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                    //assert array.boost[pos] == boostAtt.getBoost() : "boost should be equal in all segment TermsEnums";
                }
                else
                {
                    // new entry: we populate the entry initially
                    array.boost[e] = boostAtt.Boost;
                    array.termState[e] = new TermContext(topReaderContext, state, readerContext.ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                    parent.CheckMaxClauseCount(terms.Size);
                }
                return true;
            }
        }

        /** Special implementation of BytesStartArray that keeps parallel arrays for boost and docFreq */
        internal sealed class TermFreqBoostByteStart : BytesRefHash.DirectBytesStartArray
        {
            public float[] boost;
            public TermContext[] termState;

            public TermFreqBoostByteStart(int initSize)
                : base(initSize)
            {
            }

            public override int[] Init()
            {
                int[] ord = base.Init();
                boost = new float[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_FLOAT)];
                termState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                //assert termState.Length >= ord.Length && boost.Length >= ord.Length;
                return ord;
            }

            public override int[] Grow()
            {
                int[] ord = base.Grow();
                boost = ArrayUtil.Grow(boost, ord.Length);
                if (termState.Length < ord.Length)
                {
                    var tmpTermState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(termState, 0, tmpTermState, 0, termState.Length);
                    termState = tmpTermState;
                }
                //assert termState.length >= ord.length && boost.length >= ord.length;
                return ord;
            }

            public override int[] Clear()
            {
                boost = null;
                termState = null;
                return base.Clear();
            }
        }
    }
}
