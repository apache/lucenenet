using Lucene.Net.Util;
using System;
using System.Collections.Generic;
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
    using BytesRef = Lucene.Net.Util.BytesRef;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TermState = Lucene.Net.Index.TermState;

    /// <summary>
    /// Base rewrite method for collecting only the top terms
    /// via a priority queue.
    /// @lucene.internal Only public to be accessible by spans package.
    /// </summary>
    public abstract class TopTermsRewrite<Q> : TermCollectingRewrite<Q>, ITopTermsRewrite
        where Q : Query
    {
        private readonly int size;

        /// <summary>
        /// Create a TopTermsBooleanQueryRewrite for
        /// at most <paramref name="count"/> terms.
        /// <p>
        /// NOTE: if <seealso cref="BooleanQuery#getMaxClauseCount"/> is smaller than
        /// <paramref name="count"/>, then it will be used instead.
        /// </summary>
        public TopTermsRewrite(int count)
        {
            this.size = count;
        }

        /// <summary>
        /// return the maximum priority queue size.
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public virtual int Count
        {
            get
            {
                return size;
            }
        }

        /// <summary>
        /// return the maximum size of the priority queue (for boolean rewrites this is BooleanQuery#getMaxClauseCount). </summary>
        protected abstract int MaxSize { get; }

        public override Query Rewrite(IndexReader reader, MultiTermQuery query)
        {
            int maxSize = Math.Min(size, MaxSize);
            PriorityQueue<ScoreTerm> stQueue = new ScoreTermPQ(); // LUCENENET TODO: Change to Support.PriorityQueue<T> (like the original)
            CollectTerms(reader, query, new TermCollectorAnonymousInnerClassHelper(this, maxSize, stQueue));

            var q = GetTopLevelQuery();
            ScoreTerm[] scoreTerms = stQueue.ToArray(/*new ScoreTerm[stQueue.size()]*/);
            ArrayUtil.TimSort(scoreTerms, scoreTermSortByTermComp);

            foreach (ScoreTerm st in scoreTerms)
            {
                Term term = new Term(query.m_field, st.Bytes);
                Debug.Assert(reader.DocFreq(term) == st.TermState.DocFreq, "reader DF is " + reader.DocFreq(term) + " vs " + st.TermState.DocFreq + " term=" + term);
                AddClause(q, term, st.TermState.DocFreq, query.Boost * st.Boost, st.TermState); // add to query
            }
            return q;
        }

        private class TermCollectorAnonymousInnerClassHelper : TermCollector
        {
            private readonly TopTermsRewrite<Q> outerInstance;

            private int maxSize;
            private PriorityQueue<ScoreTerm> stQueue;

            public TermCollectorAnonymousInnerClassHelper(TopTermsRewrite<Q> outerInstance, int maxSize, PriorityQueue<ScoreTerm> stQueue)
            {
                this.outerInstance = outerInstance;
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
                this.termComp = termsEnum.Comparator;

                Debug.Assert(CompareToLastTerm(null));

                // lazy init the initial ScoreTerm because comparator is not known on ctor:
                if (st == null)
                {
                    st = new ScoreTerm(this.termComp, new TermContext(m_topReaderContext));
                }
                boostAtt = termsEnum.Attributes.AddAttribute<IBoostAttribute>();
            }

            // for assert:
            private BytesRef lastTerm;

            private bool CompareToLastTerm(BytesRef t)
            {
                if (lastTerm == null && t != null)
                {
                    lastTerm = BytesRef.DeepCopyOf(t);
                }
                else if (t == null)
                {
                    lastTerm = null;
                }
                else
                {
                    Debug.Assert(termsEnum.Comparator.Compare(lastTerm, t) < 0, "lastTerm=" + lastTerm + " t=" + t);
                    lastTerm.CopyBytes(t);
                }
                return true;
            }

            public override bool Collect(BytesRef bytes)
            {
                float boost = boostAtt.Boost;

                // make sure within a single seg we always collect
                // terms in order
                Debug.Assert(CompareToLastTerm(bytes));

                //System.out.println("TTR.collect term=" + bytes.utf8ToString() + " boost=" + boost + " ord=" + readerContext.ord);
                // ignore uncompetitive hits
                if (stQueue.Count == maxSize)
                {
                    ScoreTerm t = stQueue.Top;
                    if (boost < t.Boost)
                    {
                        return true;
                    }
                    if (boost == t.Boost && termComp.Compare(bytes, t.Bytes) > 0)
                    {
                        return true;
                    }
                }
                ScoreTerm t2;
                TermState state = termsEnum.GetTermState();
                Debug.Assert(state != null);
                if (visitedTerms.TryGetValue(bytes, out t2))
                {
                    // if the term is already in the PQ, only update docFreq of term in PQ
                    Debug.Assert(t2.Boost == boost, "boost should be equal in all segment TermsEnums");
                    t2.TermState.Register(state, m_readerContext.Ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                }
                else
                {
                    // add new entry in PQ, we must clone the term, else it may get overwritten!
                    st.Bytes.CopyBytes(bytes);
                    st.Boost = boost;
                    visitedTerms[st.Bytes] = st;
                    Debug.Assert(st.TermState.DocFreq == 0);
                    st.TermState.Register(state, m_readerContext.Ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                    stQueue.Add(st);
                    // possibly drop entries from queue
                    if (stQueue.Count > maxSize)
                    {
                        st = stQueue.Pop();
                        visitedTerms.Remove(st.Bytes);
                        st.TermState.Clear(); // reset the termstate!
                    }
                    else
                    {
                        st = new ScoreTerm(termComp, new TermContext(m_topReaderContext));
                    }
                    Debug.Assert(stQueue.Count <= maxSize, "the PQ size must be limited to maxSize");
                    // set maxBoostAtt with values to help FuzzyTermsEnum to optimize
                    if (stQueue.Count == maxSize)
                    {
                        t2 = stQueue.Top;
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
            if (obj == null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            var other = (TopTermsRewrite<Q>)obj;
            if (size != other.size)
            {
                return false;
            }
            return true;
        }

        private static readonly IComparer<ScoreTerm> scoreTermSortByTermComp = new ComparatorAnonymousInnerClassHelper();

        private class ComparatorAnonymousInnerClassHelper : IComparer<ScoreTerm>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(ScoreTerm st1, ScoreTerm st2)
            {
                Debug.Assert(st1.TermComp == st2.TermComp, "term comparator should not change between segments");
                return st1.TermComp.Compare(st1.Bytes, st2.Bytes);
            }
        }

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
                if (this.Boost == other.Boost)
                {
                    return TermComp.Compare(other.Bytes, this.Bytes);
                }
                else
                {
                    return this.Boost.CompareTo(other.Boost);
                }
            }
        }

        // LUCENENET TODO: eliminate this unnecessary class
        private class ScoreTermPQ : PriorityQueue<ScoreTerm>
        {
            protected internal override bool LessThan(ScoreTerm a, ScoreTerm b)
            {
                return (a.CompareTo(b) < 0) ? true : false;
            }
        }

    }
}