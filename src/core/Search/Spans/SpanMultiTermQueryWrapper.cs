using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Spans
{
    public class SpanMultiTermQueryWrapper<Q> : SpanQuery
        where Q : MultiTermQuery
    {
        protected readonly Q query;

        public SpanMultiTermQueryWrapper(Q query)
        {
            this.query = query;

            MultiTermQuery.RewriteMethod method = query.RewriteMethod;
            if (method is TopTermsRewrite<Q>)
            {
                int pqsize = ((TopTermsRewrite<Q>) method).Size;
                RewriteMethod = new TopTermsSpanBooleanQueryRewrite(pqsize);
            }
            else
            {
                RewriteMethod = SCORING_SPAN_QUERY_REWRITE;
            }
        }

        public SpanRewriteMethod RewriteMethod
        {
            get
            {
                MultiTermQuery.RewriteMethod m = query.RewriteMethod;
                if (!(m is SpanRewriteMethod))
                    throw new NotSupportedException(
                        "You can only use SpanMultiTermQueryWrapper with a suitable SpanRewriteMethod.");
                return (SpanRewriteMethod)m;
            }
            set
            {
                query.RewriteMethod = value;
            }
        }

        public override Spans GetSpans(AtomicReaderContext context, IBits acceptDocs,
                                       IDictionary<Term, TermContext> termContexts)
        {
            throw new NotSupportedException("Query should have been rewritten");
        }

        public override string Field
        {
            get { return query.Field; }
        }

        public override string ToString(string field)
        {
            var builder = new StringBuilder();
            builder.Append("SpanMultiTermQueryWrapper(");
            builder.Append(query.ToString(field));
            builder.Append(")");
            return builder.ToString();
        }

        public override Query Rewrite(IndexReader reader)
        {
            var q = query.Rewrite(reader);
            if (!(q is SpanQuery))
                throw new NotSupportedException(
                    "You can only use SpanMultiTermQueryWrapper with a suitable SpanRewriteMethod.") 
            return q;
        }

        public override int GetHashCode()
        {
            return 31*query.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null) return false;
            if (GetType() != obj.GetType()) return false;
            var other = (SpanMultiTermQueryWrapper<Q>) obj;
            return query.Equals(other.query);
        }

        public abstract class SpanRewriteMethod : MultiTermQuery.RewriteMethod
        {
            public abstract override SpanQuery Rewrite(IndexReader reader, MultiTermQuery query);
        }

        private sealed class AnonymousScoringSpanQueryRewrite : SpanRewriteMethod
        {
            private sealed class AnonymousScoringRewrite : ScoringRewrite<SpanOrQuery>
            {
                protected override SpanOrQuery GetTopLevelQuery()
                {
                    return new SpanOrQuery();
                }

                protected override void CheckMaxClauseCount(int count)
                {
                    // we accept all terms as SpanOrQuery has no limits
                }

                protected override void AddClause(SpanOrQuery topLevel, Term term, int docCount, float boost,
                                                  TermContext states)
                {
                    // TODO: would be nice to not lose term-state here.
                    // we could add a hack option to SpanOrQuery, but the hack would only work if this is the top-level Span
                    // (if you put this thing in another span query, it would extractTerms/double-seek anyway)
                    var q = new SpanTermQuery(term) {Boost = boost};
                    topLevel.AddClause(q);
                }
            }

            private readonly ScoringRewrite<SpanOrQuery> _delegate = new AnonymousScoringRewrite();

            public override SpanQuery Rewrite(IndexReader reader, MultiTermQuery query)
            {
                return _delegate.Rewrite(reader, query);
            }
        }

        public static readonly SpanRewriteMethod SCORING_SPAN_QUERY_REWRITE = new AnonymousScoringSpanQueryRewrite();

        public sealed class TopTermsSpanBooleanQueryRewrite : SpanRewriteMethod
        {
            private readonly TopTermsRewrite<SpanOrQuery> _delegate;

            private sealed class AnonymousTopTermsRewrite : TopTermsRewrite<SpanOrQuery>
            {
                public AnonymousTopTermsRewrite(int size) : base(size)
                {
                }

                protected override int MaxSize
                {
                    get { return Int32.MaxValue; }
                }

                protected override SpanOrQuery GetTopLevelQuery()
                {
                    return new SpanOrQuery();
                }

                protected override void AddClause(SpanOrQuery topLevel, Term term, int docFreq, float boost,
                                                  TermContext states)
                {
                    var q = new SpanTermQuery(term);
                    q.Boost = boost;
                    topLevel.AddClause(q);
                }
            }

            public TopTermsSpanBooleanQueryRewrite(int size)
            {
                _delegate = new AnonymousTopTermsRewrite(size);
            }

            /** return the maximum priority queue size */

            public int GetSize()
            {
                return _delegate.Size;
            }

            public override SpanQuery Rewrite(IndexReader reader, MultiTermQuery query)
            {
                return _delegate.Rewrite(reader, query);
            }

            public override int GetHashCode()
            {
                return 31*_delegate.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (this == obj) return true;
                if (obj == null) return false;
                if (GetType() != obj.GetType()) return false;
                var other = (TopTermsSpanBooleanQueryRewrite) obj;
                return _delegate.Equals(other._delegate);
            }
        }
    }
}