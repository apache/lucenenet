using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TermState = Lucene.Net.Index.TermState;

    internal interface ITopTermsRewrite
    {
        int Count { get; } // LUCENENET NOTE: This was size() in Lucene.
    }

    /// <summary>
    /// Base rewrite method for collecting only the top terms
    /// via a priority queue.
    /// <para/>
    /// @lucene.internal - Only public to be accessible by spans package.
    /// </summary>
    public abstract class TopTermsRewrite<Q> : TermCollectingRewrite<Q>, ITopTermsRewrite
        where Q : Query
    {
        private readonly int size;

        /// <summary>
        /// Create a <see cref="TopTermsRewrite{Q}"/> for
        /// at most <paramref name="count"/> terms.
        /// <para/>
        /// NOTE: if <see cref="BooleanQuery.MaxClauseCount"/> is smaller than
        /// <paramref name="count"/>, then it will be used instead.
        /// </summary>
        protected TopTermsRewrite(int count) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.size = count;
        }

        /// <summary>
        /// Return the maximum priority queue size.
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public virtual int Count => size;

        /// <summary>
        /// Return the maximum size of the priority queue (for boolean rewrites this is <see cref="BooleanQuery.MaxClauseCount"/>). </summary>
        protected abstract int MaxSize { get; }

        public override Query Rewrite(IndexReader reader, MultiTermQuery query)
        {
            int maxSize = Math.Min(size, MaxSize);
            JCG.PriorityQueue<ScoreTerm> stQueue = new JCG.PriorityQueue<ScoreTerm>();
            CollectTerms(reader, query, new TermCollectorAnonymousClass(maxSize, stQueue));

            var q = GetTopLevelQuery();
            ScoreTerm[] scoreTerms = stQueue.ToArray(/*new ScoreTerm[stQueue.Count]*/);

            ArrayUtil.TimSort(scoreTerms, scoreTermSortByTermComp);

            foreach (ScoreTerm st in scoreTerms)
            {
                Term term = new Term(query.m_field, st.Bytes);
                if (Debugging.AssertsEnabled) Debugging.Assert(reader.DocFreq(term) == st.TermState.DocFreq, "reader DF is {0} vs {1} term={2}", reader.DocFreq(term), st.TermState.DocFreq, term);
                AddClause(q, term, st.TermState.DocFreq, query.Boost * st.Boost, st.TermState); // add to query
            }
            return q;
        }

        private sealed class TermCollectorAnonymousClass : TermCollector
        {
            private readonly int maxSize;
            private readonly JCG.PriorityQueue<ScoreTerm> stQueue;

            public TermCollectorAnonymousClass(int maxSize, JCG.PriorityQueue<ScoreTerm> stQueue)
            {
                this.maxSize = maxSize;
                this.stQueue = stQueue;
                maxBoostAtt = Attributes.AddAttribute<IMaxNonCompetitiveBoostAttribute>();
                visitedTerms = new Dictionary<BytesRef, ScoreTerm>();
            }

            private readonly IMaxNonCompetitiveBoostAttribute maxBoostAtt;

            private readonly IDictionary<BytesRef, ScoreTerm> visitedTerms;

            private TermsEnum termsEnum;
            private IComparer<BytesRef> termComp;
            private IBoostAttribute boostAtt;
            private ScoreTerm st;

            public override void SetNextEnum(TermsEnum termsEnum)
            {
                this.termsEnum = termsEnum;
                this.termComp = termsEnum.Comparer;

                if (Debugging.AssertsEnabled) Debugging.Assert(CompareToLastTerm(null));

                // lazy init the initial ScoreTerm because comparer is not known on ctor:
                if (st is null)
                {
                    st = new ScoreTerm(this.termComp, new TermContext(m_topReaderContext));
                }
                boostAtt = termsEnum.Attributes.AddAttribute<IBoostAttribute>();
            }

            // for assert:
            private BytesRef lastTerm;

            private bool CompareToLastTerm(BytesRef t)
            {
                if (lastTerm is null && t != null)
                {
                    lastTerm = BytesRef.DeepCopyOf(t);
                }
                else if (t is null)
                {
                    lastTerm = null;
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(termsEnum.Comparer.Compare(lastTerm, t) < 0, "lastTerm={0} t={1}", lastTerm, t);
                    lastTerm.CopyBytes(t);
                }
                return true;
            }

            public override bool Collect(BytesRef bytes)
            {
                float boost = boostAtt.Boost;

                // make sure within a single seg we always collect
                // terms in order
                if (Debugging.AssertsEnabled) Debugging.Assert(CompareToLastTerm(bytes));

                //System.out.println("TTR.collect term=" + bytes.utf8ToString() + " boost=" + boost + " ord=" + readerContext.ord);
                // ignore uncompetitive hits
                if (stQueue.Count == maxSize)
                {
                    ScoreTerm t = stQueue.Peek();
                    // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                    if (NumericUtils.SingleToSortableInt32(boost) < NumericUtils.SingleToSortableInt32(t.Boost))
                    {
                        return true;
                    }
                    // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                    if (NumericUtils.SingleToSortableInt32(boost) == NumericUtils.SingleToSortableInt32(t.Boost) && termComp.Compare(bytes, t.Bytes) > 0)
                    {
                        return true;
                    }
                }
                TermState state = termsEnum.GetTermState();
                if (Debugging.AssertsEnabled) Debugging.Assert(state != null);
                if (visitedTerms.TryGetValue(bytes, out ScoreTerm t2))
                {
                    // if the term is already in the PQ, only update docFreq of term in PQ
                    // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                    if (Debugging.AssertsEnabled) Debugging.Assert(NumericUtils.SingleToSortableInt32(t2.Boost) == NumericUtils.SingleToSortableInt32(boost), "boost should be equal in all segment TermsEnums");
                    t2.TermState.Register(state, m_readerContext.Ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                }
                else
                {
                    // add new entry in PQ, we must clone the term, else it may get overwritten!
                    st.Bytes.CopyBytes(bytes);
                    st.Boost = boost;
                    visitedTerms[st.Bytes] = st;
                    if (Debugging.AssertsEnabled) Debugging.Assert(st.TermState.DocFreq == 0);
                    st.TermState.Register(state, m_readerContext.Ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                    stQueue.Add(st);
                    // possibly drop entries from queue
                    if (stQueue.Count > maxSize)
                    {
                        st = stQueue.Dequeue();
                        visitedTerms.Remove(st.Bytes);
                        st.TermState.Clear(); // reset the termstate!
                    }
                    else
                    {
                        st = new ScoreTerm(termComp, new TermContext(m_topReaderContext));
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(stQueue.Count <= maxSize, "the PQ size must be limited to maxSize");
                    // set maxBoostAtt with values to help FuzzyTermsEnum to optimize
                    if (stQueue.Count == maxSize)
                    {
                        t2 = stQueue.Peek();
                        maxBoostAtt.MaxNonCompetitiveBoost = t2.Boost;
                        maxBoostAtt.CompetitiveTerm = t2.Bytes;
                    }
                }

                return true;
            }
        }

        public override int GetHashCode()
        {
            return 31 * size;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            if (obj is TopTermsRewrite<Q> other)
            {
                if (size != other.size)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        private static readonly IComparer<ScoreTerm> scoreTermSortByTermComp = Comparer<ScoreTerm>.Create((st1, st2) =>
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(st1.TermComp == st2.TermComp, "term comparer should not change between segments");
            return st1.TermComp.Compare(st1.Bytes, st2.Bytes);
        });
        
        internal sealed class ScoreTerm : IComparable<ScoreTerm>
        {
            public IComparer<BytesRef> TermComp { get; private set; }
            public BytesRef Bytes { get; private set; }
            public float Boost { get; set; }
            public TermContext TermState { get; private set; }

            public ScoreTerm(IComparer<BytesRef> termComp, TermContext termState)
            {
                this.TermComp = termComp;
                this.TermState = termState;
                this.Bytes = new BytesRef();
            }

            public int CompareTo(ScoreTerm other)
            {
                // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                if (NumericUtils.SingleToSortableInt32(this.Boost) == NumericUtils.SingleToSortableInt32(other.Boost))
                {
                    return TermComp.Compare(other.Bytes, this.Bytes);
                }
                else
                {
                    return this.Boost.CompareTo(other.Boost);
                }
            }
        }
    }
}