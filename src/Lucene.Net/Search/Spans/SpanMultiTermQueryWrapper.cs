using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search.Spans
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    /// <summary>
    /// Wraps any <see cref="MultiTermQuery"/> as a <see cref="SpanQuery"/>,
    /// so it can be nested within other <see cref="SpanQuery"/> classes.
    /// <para/>
    /// The query is rewritten by default to a <see cref="SpanOrQuery"/> containing
    /// the expanded terms, but this can be customized.
    /// <para/>
    /// Example:
    /// <code>
    /// WildcardQuery wildcard = new WildcardQuery(new Term("field", "bro?n"));
    /// SpanQuery spanWildcard = new SpanMultiTermQueryWrapper&lt;WildcardQuery&gt;(wildcard);
    /// // do something with spanWildcard, such as use it in a SpanFirstQuery
    /// </code>
    /// </summary>
    public class SpanMultiTermQueryWrapper<Q> : SpanQuery, ISpanMultiTermQueryWrapper where Q : MultiTermQuery
    {
        protected readonly Q m_query;

        /// <summary>
        /// Create a new <see cref="SpanMultiTermQueryWrapper{Q}"/>.
        /// </summary>
        /// <param name="query"> Query to wrap.
        /// <para/>
        /// NOTE: This will set <see cref="MultiTermQuery.MultiTermRewriteMethod"/>
        /// on the wrapped <paramref name="query"/>, changing its rewrite method to a suitable one for spans.
        /// Be sure to not change the rewrite method on the wrapped query afterwards! Doing so will
        /// throw <see cref="NotSupportedException"/> on rewriting this query! </param>
        public SpanMultiTermQueryWrapper(Q query)
        {
            this.m_query = query;

            MultiTermQuery.RewriteMethod method = this.m_query.MultiTermRewriteMethod;
            if (method is ITopTermsRewrite topTermsRewrite)
            {
                MultiTermRewriteMethod = new TopTermsSpanBooleanQueryRewrite(topTermsRewrite.Count);
            }
            else
            {
                MultiTermRewriteMethod = SCORING_SPAN_QUERY_REWRITE;
            }
        }

        /// <summary>
        /// Expert: Gets or Sets the rewrite method. This only makes sense
        /// to be a span rewrite method.
        /// </summary>
        public SpanRewriteMethod MultiTermRewriteMethod
        {
            get
            {
                MultiTermQuery.RewriteMethod m = m_query.MultiTermRewriteMethod;
                if (!(m is SpanRewriteMethod spanRewriteMethod))
                {
                    throw UnsupportedOperationException.Create("You can only use SpanMultiTermQueryWrapper with a suitable SpanRewriteMethod.");
                }
                return spanRewriteMethod;
            }
            set => m_query.MultiTermRewriteMethod = value;
        }

        public override Spans GetSpans(AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            throw UnsupportedOperationException.Create("Query should have been rewritten");
        }

        public override string Field => m_query.Field;

        /// <summary>
        /// Returns the wrapped query </summary>
        public virtual Query WrappedQuery => m_query;

        public override string ToString(string field)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("SpanMultiTermQueryWrapper(");
            builder.Append(m_query.ToString(field));
            builder.Append(')');
            if (Boost != 1F)
            {
                builder.Append('^');
                builder.Append(Boost);
            }
            return builder.ToString();
        }

        public override Query Rewrite(IndexReader reader)
        {
            Query q = m_query.Rewrite(reader);
            if (!(q is SpanQuery))
            {
                throw UnsupportedOperationException.Create("You can only use SpanMultiTermQueryWrapper with a suitable SpanRewriteMethod.");
            }
            q.Boost = q.Boost * Boost; // multiply boost
            return q;
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + m_query.GetHashCode();
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            var other = (SpanMultiTermQueryWrapper<Q>)obj;
            if (!m_query.Equals(other.m_query))
            {
                return false;
            }
            return true;
        }

        // LUCENENET NOTE: Moved SpanRewriteMethod outside of this class

        /// <summary>
        /// A rewrite method that first translates each term into a <see cref="SpanTermQuery"/> in a
        /// <see cref="Occur.SHOULD"/> clause in a <see cref="BooleanQuery"/>, and keeps the
        /// scores as computed by the query.
        /// </summary>
        /// <seealso cref="MultiTermRewriteMethod"/>
        public static readonly SpanRewriteMethod SCORING_SPAN_QUERY_REWRITE = new SpanRewriteMethodAnonymousClass();

        private sealed class SpanRewriteMethodAnonymousClass : SpanRewriteMethod
        {
            public SpanRewriteMethodAnonymousClass()
            {
            }

            private readonly ScoringRewrite<SpanOrQuery> @delegate = new ScoringRewriteAnonymousClass();

            private sealed class ScoringRewriteAnonymousClass : ScoringRewrite<SpanOrQuery>
            {
                public ScoringRewriteAnonymousClass()
                {
                }

                protected override SpanOrQuery GetTopLevelQuery()
                {
                    return new SpanOrQuery();
                }

                protected override void CheckMaxClauseCount(int count)
                {
                    // we accept all terms as SpanOrQuery has no limits
                }

                protected override void AddClause(SpanOrQuery topLevel, Term term, int docCount, float boost, TermContext states)
                {
                    // TODO: would be nice to not lose term-state here.
                    // we could add a hack option to SpanOrQuery, but the hack would only work if this is the top-level Span
                    // (if you put this thing in another span query, it would extractTerms/double-seek anyway)
                    SpanTermQuery q = new SpanTermQuery(term);
                    q.Boost = boost;
                    topLevel.AddClause(q);
                }
            }

            public override Query Rewrite(IndexReader reader, MultiTermQuery query)
            {
                return @delegate.Rewrite(reader, query);
            }
        }

        /// <summary>
        /// A rewrite method that first translates each term into a <see cref="SpanTermQuery"/> in a
        /// <see cref="Occur.SHOULD"/> clause in a <see cref="BooleanQuery"/>, and keeps the
        /// scores as computed by the query.
        ///
        /// <para/>
        /// This rewrite method only uses the top scoring terms so it will not overflow
        /// the boolean max clause count.
        /// </summary>
        /// <seealso cref="MultiTermRewriteMethod"/>
        public sealed class TopTermsSpanBooleanQueryRewrite : SpanRewriteMethod
        {
            private readonly TopTermsRewrite<SpanOrQuery> @delegate;

            /// <summary>
            /// Create a <see cref="TopTermsSpanBooleanQueryRewrite"/> for
            /// at most <paramref name="size"/> terms.
            /// </summary>
            public TopTermsSpanBooleanQueryRewrite(int size)
            {
                @delegate = new TopTermsRewriteAnonymousClass(size);
            }

            private sealed class TopTermsRewriteAnonymousClass : TopTermsRewrite<SpanOrQuery>
            {
                public TopTermsRewriteAnonymousClass(int size)
                    : base(size)
                {
                }

                protected override int MaxSize => int.MaxValue;

                protected override SpanOrQuery GetTopLevelQuery()
                {
                    return new SpanOrQuery();
                }

                protected override void AddClause(SpanOrQuery topLevel, Term term, int docFreq, float boost, TermContext states)
                {
                    SpanTermQuery q = new SpanTermQuery(term);
                    q.Boost = boost;
                    topLevel.AddClause(q);
                }
            }

            /// <summary>
            /// return the maximum priority queue size.
            /// <para/>
            /// NOTE: This was size() in Lucene.
            /// </summary>
            public int Count => @delegate.Count;

            public override Query Rewrite(IndexReader reader, MultiTermQuery query)
            {
                return @delegate.Rewrite(reader, query);
            }

            public override int GetHashCode()
            {
                return 31 * @delegate.GetHashCode();
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
                TopTermsSpanBooleanQueryRewrite other = (TopTermsSpanBooleanQueryRewrite)obj;
                return @delegate.Equals(other.@delegate);
            }
        }
    }

    /// <summary>
    /// Abstract class that defines how the query is rewritten. </summary>
    // LUCENENET specific - moved this class outside of SpanMultiTermQueryWrapper<Q>
    public abstract class SpanRewriteMethod : MultiTermQuery.RewriteMethod
    {
        public override abstract Query Rewrite(IndexReader reader, MultiTermQuery query);
    }

    /// <summary>
    /// LUCENENET specific interface for referring to/identifying a <see cref="Search.Spans.SpanMultiTermQueryWrapper{Q}"/> without
    /// referring to its generic closing type.
    /// </summary>
    public interface ISpanMultiTermQueryWrapper
    {
        /// <summary>
        /// Expert: Gets or Sets the rewrite method. This only makes sense
        /// to be a span rewrite method.
        /// </summary>
        SpanRewriteMethod MultiTermRewriteMethod { get; }
        Spans GetSpans(AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts);
        string Field { get; }

        /// <summary>
        /// Returns the wrapped query </summary>
        Query WrappedQuery { get; }
        Query Rewrite(IndexReader reader);
    }
}