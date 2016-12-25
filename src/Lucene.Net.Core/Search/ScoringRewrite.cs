using System;
using System.Diagnostics;

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
    /// <p>
    /// @lucene.internal Only public to be accessible by spans package.
    /// </summary>
    public abstract class ScoringRewrite<Q> : TermCollectingRewrite<Q> where Q : Query
    {
        /// <summary>
        /// A rewrite method that first translates each term into
        ///  <seealso cref="Occur#SHOULD"/> clause in a
        ///  BooleanQuery, and keeps the scores as computed by the
        ///  query.  Note that typically such scores are
        ///  meaningless to the user, and require non-trivial CPU
        ///  to compute, so it's almost always better to use {@link
        ///  MultiTermQuery#CONSTANT_SCORE_AUTO_REWRITE_DEFAULT} instead.
        ///
        ///  <p><b>NOTE</b>: this rewrite method will hit {@link
        ///  BooleanQuery.TooManyClauses} if the number of terms
        ///  exceeds <seealso cref="BooleanQuery#getMaxClauseCount"/>.
        /// </summary>
        ///  <seealso cref= MultiTermQuery#setRewriteMethod  </seealso>
        public static readonly ScoringRewrite<BooleanQuery> SCORING_BOOLEAN_QUERY_REWRITE = new ScoringRewriteAnonymousInnerClassHelper();

        private class ScoringRewriteAnonymousInnerClassHelper : ScoringRewrite<BooleanQuery>
        {
            public ScoringRewriteAnonymousInnerClassHelper()
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
                    throw new BooleanQuery.TooManyClauses();
                }
            }
        }

        /// <summary>
        /// Like <seealso cref="#SCORING_BOOLEAN_QUERY_REWRITE"/> except
        ///  scores are not computed.  Instead, each matching
        ///  document receives a constant score equal to the
        ///  query's boost.
        ///
        ///  <p><b>NOTE</b>: this rewrite method will hit {@link
        ///  BooleanQuery.TooManyClauses} if the number of terms
        ///  exceeds <seealso cref="BooleanQuery#getMaxClauseCount"/>.
        /// </summary>
        ///  <seealso cref= MultiTermQuery#setRewriteMethod  </seealso>
        public static readonly RewriteMethod CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE = new RewriteMethodAnonymousInnerClassHelper();

        private class RewriteMethodAnonymousInnerClassHelper : RewriteMethod
        {
            public RewriteMethodAnonymousInnerClassHelper()
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
        /// this method is called after every new term to check if the number of max clauses
        /// (e.g. in BooleanQuery) is not exceeded. Throws the corresponding <seealso cref="RuntimeException"/>.
        /// </summary>
        protected abstract void CheckMaxClauseCount(int count);

        public override Query Rewrite(IndexReader reader, MultiTermQuery query)
        {
            var result = GetTopLevelQuery();
            ParallelArraysTermCollector col = new ParallelArraysTermCollector(this);
            CollectTerms(reader, query, col);

            int size = col.terms.Size();
            if (size > 0)
            {
                int[] sort = col.terms.Sort(col.termsEnum.Comparator);
                float[] boost = col.array.boost;
                TermContext[] termStates = col.array.termState;
                for (int i = 0; i < size; i++)
                {
                    int pos = sort[i];
                    Term term = new Term(query.Field, col.terms.Get(pos, new BytesRef()));
                    Debug.Assert(reader.DocFreq(term) == termStates[pos].DocFreq);
                    AddClause(result, term, termStates[pos].DocFreq, query.Boost * boost[pos], termStates[pos]);
                }
            }
            return result;
        }

        internal sealed class ParallelArraysTermCollector : TermCollector
        {
            internal void InitializeInstanceFields()
            {
                terms = new BytesRefHash(new ByteBlockPool(new ByteBlockPool.DirectAllocator()), 16, array);
            }

            private readonly ScoringRewrite<Q> outerInstance;

            public ParallelArraysTermCollector(ScoringRewrite<Q> outerInstance)
            {
                this.outerInstance = outerInstance;

                InitializeInstanceFields();
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
                TermState state = termsEnum.TermState();
                Debug.Assert(state != null);
                if (e < 0)
                {
                    // duplicate term: update docFreq
                    int pos = (-e) - 1;
                    array.termState[pos].Register(state, m_readerContext.Ord, termsEnum.DocFreq(), termsEnum.TotalTermFreq());
                    Debug.Assert(array.boost[pos] == boostAtt.Boost, "boost should be equal in all segment TermsEnums");
                }
                else
                {
                    // new entry: we populate the entry initially
                    array.boost[e] = boostAtt.Boost;
                    array.termState[e] = new TermContext(m_topReaderContext, state, m_readerContext.Ord, termsEnum.DocFreq(), termsEnum.TotalTermFreq());
                    outerInstance.CheckMaxClauseCount(terms.Size());
                }
                return true;
            }
        }

        /// <summary>
        /// Special implementation of BytesStartArray that keeps parallel arrays for boost and docFreq </summary>
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
                boost = new float[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_FLOAT)];
                termState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Debug.Assert(termState.Length >= ord.Length && boost.Length >= ord.Length);
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
                Debug.Assert(termState.Length >= ord.Length && boost.Length >= ord.Length);
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