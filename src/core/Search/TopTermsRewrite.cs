using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            // TODO: finish implementation
        }

        public override Query Rewrite(IndexReader reader, MultiTermQuery query)
        {
            int maxSize = Math.Min(size, MaxSize);
            PriorityQueue<ScoreTerm> stQueue = new PriorityQueue<ScoreTerm>();
            CollectTerms(reader, query, new AnonymousRewriteTermCollector());

            Q q = GetTopLevelQuery();
            ScoreTerm[] scoreTerms = stQueue.ToArray();
            ArrayUtil.MergeSort(scoreTerms, scoreTermSortByTermComp);

            foreach (ScoreTerm st in scoreTerms)
            {
                Term term = new Term(query.Field, st.bytes);
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
            TopTermsRewrite<Q> other = (TopTermsRewrite<Q>)obj;
            if (size != other.size) return false;
            return true;
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
