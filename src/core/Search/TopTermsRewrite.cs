using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Search
{
    public abstract class TopTermsRewrite<Q> : TermCollectingRewrite<Q>
        where Q : Query
    {
        private readonly int size;

        public TopTermsRewrite(int size)
        {
            this.size = size;
        }

        public int Size
        {
            get { return size; }
        }

        protected abstract int MaxSize { get; }

        private sealed class AnonymousRewriteTermCollector : TermCollector
        {
            private readonly IMaxNonCompetitiveBoostAttribute maxBoostAtt;
            private readonly IDictionary<BytesRef, ScoreTerm> visitedTerms = new HashMap<BytesRef, ScoreTerm>();

            private TermsEnum termsEnum;
            private IComparer<BytesRef> termComp;
            private BoostAttribute boostAtt;
            private ScoreTerm st;

            private TopTermsRewrite<Q> parent; 
            private Support.PriorityQueue<ScoreTerm> stQueue; 

            public AnonymousRewriteTermCollector(TopTermsRewrite<Q> parent, Support.PriorityQueue<ScoreTerm> stQueue)
            {
                this.parent = parent;
                this.stQueue = stQueue;
                maxBoostAtt = attributes.AddAttribute<IMaxNonCompetitiveBoostAttribute>();
            }

            public override void SetNextEnum(TermsEnum termsEnum)
            {
                this.termsEnum = termsEnum;
                this.termComp = termsEnum.Comparator;

                // assert compareToLastTerm(null);

                if (st == null)
                    st = new ScoreTerm(this.termComp, new TermContext(topReaderContext));
                boostAtt = termsEnum.Attributes.AddAttribute<BoostAttribute>();
            }

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
                    // assert termsEnum.getComparator().compare(lastTerm, t) < 0 : "lastTerm=" + lastTerm + " t=" + t;
                    lastTerm.CopyBytes(t);
                }
                return true;
            }

            public override bool Collect(BytesRef bytes)
            {
                var boost = boostAtt.Boost;

                // assert compareToLastTerm(bytes);

                if (stQueue.Count == parent.MaxSize)
                {
                    var term = stQueue.Peek();
                    if (boost < term.boost)
                        return true;
                    if (boost == term.boost && termComp.Compare(bytes, term.bytes) > 0)
                        return true;
                }

                var t = visitedTerms[bytes];
                var state = termsEnum.TermState;
                // assert state != null;

                if (t != null)
                {
                    // assert t.boost == boost : "boost should be equal in all segment TermsEnums";
                    t.termState.Register(state, readerContext.ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                }
                else
                {
                    st.bytes.CopyBytes(bytes);
                    st.boost = boost;
                    visitedTerms.Add(st.bytes, st);
                    // assert st.termState.docFreq() == 0;
                    st.termState.Register(state, readerContext.ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                    stQueue.Enqueue(st);
                    if (stQueue.Count > parent.MaxSize)
                    {
                        st = stQueue.Dequeue();
                        visitedTerms.Remove(st.bytes);
                        st.termState.Clear();
                    }
                    else
                    {
                        st = new ScoreTerm(termComp, new TermContext(topReaderContext));
                    }
                    // assert stQueue.size() <= maxSize : "the PQ size must be limited to maxSize";

                    if (stQueue.Count == parent.MaxSize)
                    {
                        t = stQueue.Peek();
                        maxBoostAtt.MaxNonCompetitiveBoost = t.boost;
                        maxBoostAtt.CompetitiveTerm = t.bytes;
                    }
                }

                return true;
            }
        }

        public override Query Rewrite(IndexReader reader, MultiTermQuery query)
        {
            var maxSize = Math.Min(size, MaxSize);

            var stQueue = new Support.PriorityQueue<ScoreTerm>();
            CollectTerms(reader, query, new AnonymousRewriteTermCollector(this, stQueue));

            var q = GetTopLevelQuery();
            var scoreTerms = stQueue.ToArray();
            ArrayUtil.MergeSort(scoreTerms, scoreTermSortByTermComp);

            foreach (var st in scoreTerms)
            {
                var term = new Term(query.Field, st.bytes);
                //assert reader.docFreq(term) == st.termState.docFreq() : "reader DF is " + reader.docFreq(term) + " vs " + st.termState.docFreq() + " term=" + term;
                AddClause(q, term, st.termState.DocFreq, query.Boost * st.boost, st.termState); // add to query
            }
            return q;
        }



        public override int GetHashCode()
        {
            return 31 * size;
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null) return false;
            if (GetType() != obj.GetType()) return false;
            var other = (TopTermsRewrite<Q>)obj;
            return size == other.size;
        }

        private sealed class AnonymousScoreTermSortByTermComparer : IComparer<ScoreTerm>
        {
            public int Compare(ScoreTerm st1, ScoreTerm st2)
            {
                //      assert st1.termComp == st2.termComp :
                //"term comparator should not change between segments";
                return st1.termComp.Compare(st1.bytes, st2.bytes);
            }
        }

        private static readonly IComparer<ScoreTerm> scoreTermSortByTermComp = new AnonymousScoreTermSortByTermComparer();

        internal sealed class ScoreTerm : IComparable<ScoreTerm>
        {
            public readonly IComparer<BytesRef> termComp;
            public readonly BytesRef bytes = new BytesRef();
            public float boost;
            public readonly TermContext termState;
            public ScoreTerm(IComparer<BytesRef> termComp, TermContext termState)
            {
                this.termComp = termComp;
                this.termState = termState;
            }

            public int CompareTo(ScoreTerm other)
            {
                if (this.boost == other.boost)
                    return termComp.Compare(other.bytes, this.bytes);
                else
                    return this.boost.CompareTo(other.boost);
            }
        }
    }
}
