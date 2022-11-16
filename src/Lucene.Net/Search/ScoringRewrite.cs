using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using Lucene.Net.Support;
using System;

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
    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using BytesRefHash = Lucene.Net.Util.BytesRefHash;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using RewriteMethod = Lucene.Net.Search.MultiTermQuery.RewriteMethod;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TermState = Lucene.Net.Index.TermState;

    /// <summary>
    /// Base rewrite method that translates each term into a query, and keeps
    /// the scores as computed by the query.
    /// <para/>
    /// @lucene.internal - Only public to be accessible by spans package.
    /// </summary>
    public abstract class ScoringRewrite<Q> : TermCollectingRewrite<Q> where Q : Query
    {
        /// <summary>
        /// A rewrite method that first translates each term into
        /// <see cref="Occur.SHOULD"/> clause in a
        /// <see cref="BooleanQuery"/>, and keeps the scores as computed by the
        /// query.  Note that typically such scores are
        /// meaningless to the user, and require non-trivial CPU
        /// to compute, so it's almost always better to use 
        /// <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/> instead.
        ///
        /// <para/><b>NOTE</b>: this rewrite method will hit 
        /// <see cref="BooleanQuery.TooManyClausesException"/> if the number of terms
        /// exceeds <see cref="BooleanQuery.MaxClauseCount"/>.
        /// </summary>
        ///  <seealso cref="MultiTermQuery.MultiTermRewriteMethod"/>
        public static readonly ScoringRewrite<BooleanQuery> SCORING_BOOLEAN_QUERY_REWRITE = new ScoringRewriteAnonymousClass();

        private sealed class ScoringRewriteAnonymousClass : ScoringRewrite<BooleanQuery>
        {
            public ScoringRewriteAnonymousClass()
            {
            }

            protected override BooleanQuery GetTopLevelQuery()
            {
                return new BooleanQuery(true);
            }

            protected override void AddClause(BooleanQuery topLevel, Term term, int docCount, float boost, TermContext states)
            {
                TermQuery tq = new TermQuery(term, states);
                tq.Boost = boost;
                topLevel.Add(tq, Occur.SHOULD);
            }

            protected override void CheckMaxClauseCount(int count)
            {
                if (count > BooleanQuery.MaxClauseCount)
                {
                    throw new BooleanQuery.TooManyClausesException();
                }
            }
        }

        /// <summary>
        /// Like <see cref="SCORING_BOOLEAN_QUERY_REWRITE"/> except
        /// scores are not computed.  Instead, each matching
        /// document receives a constant score equal to the
        /// query's boost.
        ///
        /// <para/><b>NOTE</b>: this rewrite method will hit 
        /// <see cref="BooleanQuery.TooManyClausesException"/> if the number of terms
        /// exceeds <see cref="BooleanQuery.MaxClauseCount"/>.
        /// </summary>
        /// <seealso cref="MultiTermQuery.MultiTermRewriteMethod"/>
        public static readonly RewriteMethod CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE = new RewriteMethodAnonymousClass();

        private sealed class RewriteMethodAnonymousClass : RewriteMethod
        {
            public RewriteMethodAnonymousClass()
            {
            }

            public override Query Rewrite(IndexReader reader, MultiTermQuery query)
            {
                BooleanQuery bq = (BooleanQuery)SCORING_BOOLEAN_QUERY_REWRITE.Rewrite(reader, query);
                // strip the scores off
                Query result = new ConstantScoreQuery(bq);
                result.Boost = query.Boost;
                return result;
            }
        }

        /// <summary>
        /// This method is called after every new term to check if the number of max clauses
        /// (e.g. in <see cref="BooleanQuery"/>) is not exceeded. Throws the corresponding <see cref="Exception"/>.
        /// </summary>
        protected abstract void CheckMaxClauseCount(int count);

        public override Query Rewrite(IndexReader reader, MultiTermQuery query)
        {
            var result = GetTopLevelQuery();
            ParallelArraysTermCollector col = new ParallelArraysTermCollector(this);
            CollectTerms(reader, query, col);

            int size = col.terms.Count;
            if (size > 0)
            {
                int[] sort = col.terms.Sort(col.termsEnum.Comparer);
                float[] boost = col.array.boost;
                TermContext[] termStates = col.array.termState;
                for (int i = 0; i < size; i++)
                {
                    int pos = sort[i];
                    Term term = new Term(query.Field, col.terms.Get(pos, new BytesRef()));
                    if (Debugging.AssertsEnabled) Debugging.Assert(reader.DocFreq(term) == termStates[pos].DocFreq);
                    AddClause(result, term, termStates[pos].DocFreq, query.Boost * boost[pos], termStates[pos]);
                }
            }
            return result;
        }

        internal sealed class ParallelArraysTermCollector : TermCollector
        {
            private readonly ScoringRewrite<Q> outerInstance;

            public ParallelArraysTermCollector(ScoringRewrite<Q> outerInstance)
            {
                this.outerInstance = outerInstance;

                terms = new BytesRefHash(new ByteBlockPool(new ByteBlockPool.DirectAllocator()), 16, array);
            }

            internal readonly TermFreqBoostByteStart array = new TermFreqBoostByteStart(16);
            internal BytesRefHash terms;
            internal TermsEnum termsEnum;

            private IBoostAttribute boostAtt;

            public override void SetNextEnum(TermsEnum termsEnum)
            {
                this.termsEnum = termsEnum;
                this.boostAtt = termsEnum.Attributes.AddAttribute<IBoostAttribute>();
            }

            public override bool Collect(BytesRef bytes)
            {
                int e = terms.Add(bytes);
                TermState state = termsEnum.GetTermState();
                if (Debugging.AssertsEnabled) Debugging.Assert(state != null);
                if (e < 0)
                {
                    // duplicate term: update docFreq
                    int pos = (-e) - 1;
                    array.termState[pos].Register(state, m_readerContext.Ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                    // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                    if (Debugging.AssertsEnabled) Debugging.Assert(NumericUtils.SingleToSortableInt32(array.boost[pos]) == NumericUtils.SingleToSortableInt32(boostAtt.Boost), "boost should be equal in all segment TermsEnums");
                }
                else
                {
                    // new entry: we populate the entry initially
                    array.boost[e] = boostAtt.Boost;
                    array.termState[e] = new TermContext(m_topReaderContext, state, m_readerContext.Ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                    outerInstance.CheckMaxClauseCount(terms.Count);
                }
                return true;
            }
        }

        /// <summary>
        /// Special implementation of <see cref="BytesRefHash.BytesStartArray"/> that keeps parallel arrays for boost and docFreq </summary>
        internal sealed class TermFreqBoostByteStart : BytesRefHash.DirectBytesStartArray
        {
            internal float[] boost;
            internal TermContext[] termState;

            public TermFreqBoostByteStart(int initSize)
                : base(initSize)
            {
            }

            public override int[] Init()
            {
                int[] ord = base.Init();
                boost = new float[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_SINGLE)];
                termState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                if (Debugging.AssertsEnabled) Debugging.Assert(termState.Length >= ord.Length && boost.Length >= ord.Length);
                return ord;
            }

            public override int[] Grow()
            {
                int[] ord = base.Grow();
                boost = ArrayUtil.Grow(boost, ord.Length);
                if (termState.Length < ord.Length)
                {
                    TermContext[] tmpTermState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Arrays.Copy(termState, 0, tmpTermState, 0, termState.Length);
                    termState = tmpTermState;
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(termState.Length >= ord.Length && boost.Length >= ord.Length);
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