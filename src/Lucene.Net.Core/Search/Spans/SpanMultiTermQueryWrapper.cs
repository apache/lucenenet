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
    using Bits = Lucene.Net.Util.Bits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    /// <summary>
    /// Wraps any <seealso cref="MultiTermQuery"/> as a <seealso cref="SpanQuery"/>,
    /// so it can be nested within other SpanQuery classes.
    /// <p>
    /// The query is rewritten by default to a <seealso cref="SpanOrQuery"/> containing
    /// the expanded terms, but this can be customized.
    /// <p>
    /// Example:
    /// <blockquote><pre class="prettyprint">
    /// {@code
    /// WildcardQuery wildcard = new WildcardQuery(new Term("field", "bro?n"));
    /// SpanQuery spanWildcard = new SpanMultiTermQueryWrapper<WildcardQuery>(wildcard);
    /// // do something with spanWildcard, such as use it in a SpanFirstQuery
    /// }
    /// </pre></blockquote>
    /// </summary>
    public class SpanMultiTermQueryWrapper<Q> : SpanQuery, ISpanMultiTermQueryWrapper where Q : MultiTermQuery
    {
        protected readonly Q query;

        /// <summary>
        /// Create a new SpanMultiTermQueryWrapper.
        /// </summary>
        /// <param name="query"> Query to wrap.
        /// <p>
        /// NOTE: this will call <seealso cref="MultiTermQuery#setRewriteMethod(MultiTermQuery.RewriteMethod)"/>
        /// on the wrapped <code>query</code>, changing its rewrite method to a suitable one for spans.
        /// Be sure to not change the rewrite method on the wrapped query afterwards! Doing so will
        /// throw <seealso cref="UnsupportedOperationException"/> on rewriting this query! </param>
        public SpanMultiTermQueryWrapper(Q query)
        {
            this.query = query;

            MultiTermQuery.RewriteMethod method = this.query.GetRewriteMethod();
            if (method is ITopTermsRewrite)
            {
                int pqsize = ((ITopTermsRewrite)method).Size;
                RewriteMethod = new TopTermsSpanBooleanQueryRewrite(pqsize);
            }
            else
            {
                RewriteMethod = SCORING_SPAN_QUERY_REWRITE;
            }
        }

        /// <summary>
        /// Expert: returns the rewriteMethod
        /// </summary>
        public SpanRewriteMethod RewriteMethod // LUCENENET TODO: Change to GetRewriteMethod() and SetRewriteMethod() (consistency and error)
        {
            get
            {
                MultiTermQuery.RewriteMethod m = query.GetRewriteMethod();
                if (!(m is SpanRewriteMethod))
                {
                    throw new System.NotSupportedException("You can only use SpanMultiTermQueryWrapper with a suitable SpanRewriteMethod.");
                }
                return (SpanRewriteMethod)m;
            }
            set
            {
                query.SetRewriteMethod(value);
            }
        }

        public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            throw new System.NotSupportedException("Query should have been rewritten");
        }

        public override string Field
        {
            get
            {
                return query.Field;
            }
        }

        /// <summary>
        /// Returns the wrapped query </summary>
        public virtual Query WrappedQuery
        {
            get
            {
                return query;
            }
        }

        public override string ToString(string field)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("SpanMultiTermQueryWrapper(");
            builder.Append(query.ToString(field));
            builder.Append(")");
            if (Boost != 1F)
            {
                builder.Append('^');
                builder.Append(Boost);
            }
            return builder.ToString();
        }

        public override Query Rewrite(IndexReader reader)
        {
            Query q = query.Rewrite(reader);
            if (!(q is SpanQuery))
            {
                throw new System.NotSupportedException("You can only use SpanMultiTermQueryWrapper with a suitable SpanRewriteMethod.");
            }
            q.Boost = q.Boost * Boost; // multiply boost
            return q;
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + query.GetHashCode();
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
            if (!query.Equals(other.query))
            {
                return false;
            }
            return true;
        }

        // LUCENENET NOTE: Moved SpanRewriteMethod outside of this class

        /// <summary>
        /// A rewrite method that first translates each term into a SpanTermQuery in a
        /// <seealso cref="Occur#SHOULD"/> clause in a BooleanQuery, and keeps the
        /// scores as computed by the query.
        /// </summary>
        /// <seealso cref= #setRewriteMethod </seealso>
        public static readonly SpanRewriteMethod SCORING_SPAN_QUERY_REWRITE = new SpanRewriteMethodAnonymousInnerClassHelper();

        private class SpanRewriteMethodAnonymousInnerClassHelper : SpanRewriteMethod
        {
            public SpanRewriteMethodAnonymousInnerClassHelper()
            {
            }

            private readonly ScoringRewrite<SpanOrQuery> @delegate = new ScoringRewriteAnonymousInnerClassHelper();

            private class ScoringRewriteAnonymousInnerClassHelper : ScoringRewrite<SpanOrQuery>
            {
                public ScoringRewriteAnonymousInnerClassHelper()
                {
                }

                protected internal override SpanOrQuery TopLevelQuery
                {
                    get
                    {
                        return new SpanOrQuery();
                    }
                }

                protected internal override void CheckMaxClauseCount(int count)
                {
                    // we accept all terms as SpanOrQuery has no limits
                }

                protected internal override void AddClause(SpanOrQuery topLevel, Term term, int docCount, float boost, TermContext states)
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
        /// A rewrite method that first translates each term into a SpanTermQuery in a
        /// <seealso cref="Occur#SHOULD"/> clause in a BooleanQuery, and keeps the
        /// scores as computed by the query.
        ///
        /// <p>
        /// this rewrite method only uses the top scoring terms so it will not overflow
        /// the boolean max clause count.
        /// </summary>
        /// <seealso cref= #setRewriteMethod </seealso>
        public sealed class TopTermsSpanBooleanQueryRewrite : SpanRewriteMethod
        {
            private readonly TopTermsRewrite<SpanOrQuery> @delegate;

            /// <summary>
            /// Create a TopTermsSpanBooleanQueryRewrite for
            /// at most <code>size</code> terms.
            /// </summary>
            public TopTermsSpanBooleanQueryRewrite(int size)
            {
                @delegate = new TopTermsRewriteAnonymousInnerClassHelper(this, size);
            }

            private class TopTermsRewriteAnonymousInnerClassHelper : TopTermsRewrite<SpanOrQuery>
            {
                private readonly TopTermsSpanBooleanQueryRewrite OuterInstance;

                public TopTermsRewriteAnonymousInnerClassHelper(TopTermsSpanBooleanQueryRewrite outerInstance, int size)
                    : base(size)
                {
                    this.OuterInstance = outerInstance;
                }

                protected internal override int MaxSize
                {
                    get
                    {
                        return int.MaxValue;
                    }
                }

                protected internal override SpanOrQuery TopLevelQuery
                {
                    get
                    {
                        return new SpanOrQuery();
                    }
                }

                protected internal override void AddClause(SpanOrQuery topLevel, Term term, int docFreq, float boost, TermContext states)
                {
                    SpanTermQuery q = new SpanTermQuery(term);
                    q.Boost = boost;
                    topLevel.AddClause(q);
                }
            }

            /// <summary>
            /// return the maximum priority queue size </summary>
            public int Size // LUCENENET TODO: Rename Count (or Length?)
            {
                get
                {
                    return @delegate.Size;
                }
            }

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
                if (obj == null)
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
    /// LUCENENET specific interface for referring to/identifying a SpanMultipTermQueryWrapper without
    /// referring to its generic closing type.
    /// </summary>
    public interface ISpanMultiTermQueryWrapper
    {
        SpanRewriteMethod RewriteMethod { get; }
        Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts);
        string Field { get; }
        Query WrappedQuery { get; }
        Query Rewrite(IndexReader reader);
    }
}