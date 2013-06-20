using System;
using System.Linq;
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
            protected override BooleanQuery GetTopLevelQuery()
            {
                return new BooleanQuery(true);
            }

            protected override void AddClause(BooleanQuery topLevel, Term term, int docCount,
                float boost, TermContext states)
            {
                TermQuery tq = new TermQuery(term, states);
                tq.Boost = boost;
                topLevel.Add(tq, BooleanClause.Occur.SHOULD);
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
              BooleanQuery bq = SCORING_BOOLEAN_QUERY_REWRITE.Rewrite(reader, query);
              // TODO: if empty boolean query return NullQuery?
              if (!bq.Clauses.Any())
                return bq;
              // strip the scores off
              Query result = new ConstantScoreQuery(bq);
              result.Boost = query.Boost;
              return result;
            }
        }

        protected abstract void CheckMaxClauseCount(int count);

        public override sealed Query Rewrite(IndexReader reader, MultiTermQuery query)
        {
            var result = GetTopLevelQuery();
            ParallelArraysTermCollector col = new ParallelArraysTermCollector(this);
            CollectTerms(reader, query, col);

            int size = col.terms.Size;
            if (size > 0)
            {
                int[] sort = col.terms.Sort(col.termsEnum.Comparator);
                float[] boost = col.array.boost;
                TermContext[] termStates = col.array.TermState;
                for (int i = 0; i < size; i++)
                {
                    int pos = sort[i];
                    Term term = new Term(query.Field, col.terms.Get(pos, new BytesRef()));
                    //assert reader.docFreq(term) == termStates[pos].docFreq();
                    AddClause(result, term, termStates[pos].docFreq(), query.Boost * boost[pos], termStates[pos]);
                }
            }
            return result;
        }

        internal sealed class ParallelArraysTermCollector : TermCollector
        {
            public TermFreqBoostByteStart array = new TermFreqBoostByteStart(16);
            public BytesRefHash terms = new BytesRefHash(new ByteBlockPool(new ByteBlockPool.DirectAllocator()), 16, array);
            public TermsEnum termsEnum;

            private BoostAttribute boostAtt;

            private readonly ScoringRewrite<Q> parent;
            public ParallelArraysTermCollector(ScoringRewrite<Q> parent)
            {
                this.parent = parent;
            }

            public override void SetNextEnum(TermsEnum termsEnum)
            {
                this.termsEnum = termsEnum;
                this.boostAtt = termsEnum.Attributes.AddAttribute<BoostAttribute>();
            }

            public override bool Collect(BytesRef bytes)
            {
                int e = terms.Add(bytes);
                TermState state = termsEnum.TermState;
                //assert state != null; 
                if (e < 0)
                {
                    // duplicate term: update docFreq
                    int pos = (-e) - 1;
                    array.TermState[pos].register(state, readerContext.ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                    //assert array.boost[pos] == boostAtt.getBoost() : "boost should be equal in all segment TermsEnums";
                }
                else
                {
                    // new entry: we populate the entry initially
                    array.boost[e] = boostAtt.Boost;
                    array.TermState[e] = new TermContext(topReaderContext, state, readerContext.ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                    parent.CheckMaxClauseCount(terms.Size);
                }
                return true;
            }
        }

        /** Special implementation of BytesStartArray that keeps parallel arrays for boost and docFreq */
        internal sealed class TermFreqBoostByteStart : BytesRefHash.DirectBytesStartArray
        {
            float[] boost;
            TermContext[] termState;

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
                    TermContext[] tmpTermState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
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
