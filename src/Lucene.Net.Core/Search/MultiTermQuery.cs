namespace Lucene.Net.Search
{
    using Lucene.Net.Support;
    using AttributeSource = Lucene.Net.Util.AttributeSource;

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

    // javadocs
    using IndexReader = Lucene.Net.Index.IndexReader;
    using SingleTermsEnum = Lucene.Net.Index.SingleTermsEnum; // javadocs
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// An abstract <seealso cref="Query"/> that matches documents
    /// containing a subset of terms provided by a {@link
    /// FilteredTermsEnum} enumeration.
    ///
    /// <p>this query cannot be used directly; you must subclass
    /// it and define <seealso cref="#getTermsEnum(Terms,AttributeSource)"/> to provide a {@link
    /// FilteredTermsEnum} that iterates through the terms to be
    /// matched.
    ///
    /// <p><b>NOTE</b>: if <seealso cref="#setRewriteMethod"/> is either
    /// <seealso cref="#CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE"/> or {@link
    /// #SCORING_BOOLEAN_QUERY_REWRITE}, you may encounter a
    /// <seealso cref="BooleanQuery.TooManyClauses"/> exception during
    /// searching, which happens when the number of terms to be
    /// searched exceeds {@link
    /// BooleanQuery#getMaxClauseCount()}.  Setting {@link
    /// #setRewriteMethod} to <seealso cref="#CONSTANT_SCORE_FILTER_REWRITE"/>
    /// prevents this.
    ///
    /// <p>The recommended rewrite method is {@link
    /// #CONSTANT_SCORE_AUTO_REWRITE_DEFAULT}: it doesn't spend CPU
    /// computing unhelpful scores, and it tries to pick the most
    /// performant rewrite method given the query. If you
    /// need scoring (like <seealso cref="FuzzyQuery"/>, use
    /// <seealso cref="TopTermsScoringBooleanQueryRewrite"/> which uses
    /// a priority queue to only collect competitive terms
    /// and not hit this limitation.
    ///
    /// Note that queryparser.classic.QueryParser produces
    /// MultiTermQueries using {@link
    /// #CONSTANT_SCORE_AUTO_REWRITE_DEFAULT} by default.
    /// </summary>
    public abstract class MultiTermQuery : Query
    {
        protected internal readonly string field; // LUCENENET TODO: Rename
        protected RewriteMethod rewriteMethod = CONSTANT_SCORE_AUTO_REWRITE_DEFAULT; // LUCENENET TODO: Rename (or move RewriteMethod class)

        /// <summary>
        /// Abstract class that defines how the query is rewritten. </summary>
        public abstract class RewriteMethod
        {
            public abstract Query Rewrite(IndexReader reader, MultiTermQuery query);

            /// <summary>
            /// Returns the <seealso cref="MultiTermQuery"/>s <seealso cref="TermsEnum"/> </summary>
            /// <seealso cref= MultiTermQuery#getTermsEnum(Terms, AttributeSource) </seealso>
            protected virtual TermsEnum GetTermsEnum(MultiTermQuery query, Terms terms, AttributeSource atts)
            {
                return query.GetTermsEnum(terms, atts); // allow RewriteMethod subclasses to pull a TermsEnum from the MTQ
            }
        }

        /// <summary>
        /// A rewrite method that first creates a private Filter,
        ///  by visiting each term in sequence and marking all docs
        ///  for that term.  Matching documents are assigned a
        ///  constant score equal to the query's boost.
        ///
        ///  <p> this method is faster than the BooleanQuery
        ///  rewrite methods when the number of matched terms or
        ///  matched documents is non-trivial. Also, it will never
        ///  hit an errant <seealso cref="BooleanQuery.TooManyClauses"/>
        ///  exception.
        /// </summary>
        ///  <seealso cref= #setRewriteMethod  </seealso>
        public static readonly RewriteMethod CONSTANT_SCORE_FILTER_REWRITE = new RewriteMethodAnonymousInnerClassHelper();

        private class RewriteMethodAnonymousInnerClassHelper : RewriteMethod
        {
            public RewriteMethodAnonymousInnerClassHelper()
            {
            }

            public override Query Rewrite(IndexReader reader, MultiTermQuery query)
            {
                Query result = new ConstantScoreQuery(new MultiTermQueryWrapperFilter<MultiTermQuery>(query));
                result.Boost = query.Boost;
                return result;
            }
        }

        /// <summary>
        /// A rewrite method that first translates each term into
        ///  <seealso cref="BooleanClause.Occur#SHOULD"/> clause in a
        ///  BooleanQuery, and keeps the scores as computed by the
        ///  query.  Note that typically such scores are
        ///  meaningless to the user, and require non-trivial CPU
        ///  to compute, so it's almost always better to use {@link
        ///  #CONSTANT_SCORE_AUTO_REWRITE_DEFAULT} instead.
        ///
        ///  <p><b>NOTE</b>: this rewrite method will hit {@link
        ///  BooleanQuery.TooManyClauses} if the number of terms
        ///  exceeds <seealso cref="BooleanQuery#getMaxClauseCount"/>.
        /// </summary>
        ///  <seealso cref= #setRewriteMethod  </seealso>
        public static readonly RewriteMethod SCORING_BOOLEAN_QUERY_REWRITE = ScoringRewrite<MultiTermQuery>.SCORING_BOOLEAN_QUERY_REWRITE;

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
        ///  <seealso cref= #setRewriteMethod  </seealso>
        public static readonly RewriteMethod CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE = ScoringRewrite<MultiTermQuery>.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE;

        /// <summary>
        /// A rewrite method that first translates each term into
        /// <seealso cref="BooleanClause.Occur#SHOULD"/> clause in a BooleanQuery, and keeps the
        /// scores as computed by the query.
        ///
        /// <p>
        /// this rewrite method only uses the top scoring terms so it will not overflow
        /// the boolean max clause count. It is the default rewrite method for
        /// <seealso cref="FuzzyQuery"/>.
        /// </summary>
        /// <seealso cref= #setRewriteMethod </seealso>
        public sealed class TopTermsScoringBooleanQueryRewrite : TopTermsRewrite<BooleanQuery>
        {
            /// <summary>
            /// Create a TopTermsScoringBooleanQueryRewrite for
            /// at most <code>size</code> terms.
            /// <p>
            /// NOTE: if <seealso cref="BooleanQuery#getMaxClauseCount"/> is smaller than
            /// <code>size</code>, then it will be used instead.
            /// </summary>
            public TopTermsScoringBooleanQueryRewrite(int size)
                : base(size)
            {
            }

            protected override int MaxSize
            {
                get
                {
                    return BooleanQuery.MaxClauseCount;
                }
            }

            protected override BooleanQuery TopLevelQuery
            {
                get
                {
                    return new BooleanQuery(true);
                }
            }

            protected override void AddClause(BooleanQuery topLevel, Term term, int docCount, float boost, TermContext states)
            {
                TermQuery tq = new TermQuery(term, states);
                tq.Boost = boost;
                topLevel.Add(tq, BooleanClause.Occur.SHOULD);
            }
        }

        /// <summary>
        /// A rewrite method that first translates each term into
        /// <seealso cref="BooleanClause.Occur#SHOULD"/> clause in a BooleanQuery, but the scores
        /// are only computed as the boost.
        /// <p>
        /// this rewrite method only uses the top scoring terms so it will not overflow
        /// the boolean max clause count.
        /// </summary>
        /// <seealso cref= #setRewriteMethod </seealso>
        public sealed class TopTermsBoostOnlyBooleanQueryRewrite : TopTermsRewrite<BooleanQuery>
        {
            /// <summary>
            /// Create a TopTermsBoostOnlyBooleanQueryRewrite for
            /// at most <code>size</code> terms.
            /// <p>
            /// NOTE: if <seealso cref="BooleanQuery#getMaxClauseCount"/> is smaller than
            /// <code>size</code>, then it will be used instead.
            /// </summary>
            public TopTermsBoostOnlyBooleanQueryRewrite(int size)
                : base(size)
            {
            }

            protected override int MaxSize
            {
                get
                {
                    return BooleanQuery.MaxClauseCount;
                }
            }

            protected override BooleanQuery TopLevelQuery
            {
                get
                {
                    return new BooleanQuery(true);
                }
            }

            protected override void AddClause(BooleanQuery topLevel, Term term, int docFreq, float boost, TermContext states)
            {
                Query q = new ConstantScoreQuery(new TermQuery(term, states));
                q.Boost = boost;
                topLevel.Add(q, BooleanClause.Occur.SHOULD);
            }
        }

        /// <summary>
        /// A rewrite method that tries to pick the best
        ///  constant-score rewrite method based on term and
        ///  document counts from the query.  If both the number of
        ///  terms and documents is small enough, then {@link
        ///  #CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE} is used.
        ///  Otherwise, <seealso cref="#CONSTANT_SCORE_FILTER_REWRITE"/> is
        ///  used.
        /// </summary>
        public class ConstantScoreAutoRewrite : Lucene.Net.Search.ConstantScoreAutoRewrite
        {
        }

        /// <summary>
        /// Read-only default instance of {@link
        ///  ConstantScoreAutoRewrite}, with {@link
        ///  ConstantScoreAutoRewrite#setTermCountCutoff} set to
        ///  {@link
        ///  ConstantScoreAutoRewrite#DEFAULT_TERM_COUNT_CUTOFF}
        ///  and {@link
        ///  ConstantScoreAutoRewrite#setDocCountPercent} set to
        ///  {@link
        ///  ConstantScoreAutoRewrite#DEFAULT_DOC_COUNT_PERCENT}.
        ///  Note that you cannot alter the configuration of this
        ///  instance; you'll need to create a private instance
        ///  instead.
        /// </summary>
        public static readonly RewriteMethod CONSTANT_SCORE_AUTO_REWRITE_DEFAULT = new ConstantScoreAutoRewriteAnonymousInnerClassHelper();

        private class ConstantScoreAutoRewriteAnonymousInnerClassHelper : ConstantScoreAutoRewrite
        {
            public ConstantScoreAutoRewriteAnonymousInnerClassHelper()
            {
            }

            public override int TermCountCutoff
            {
                set
                {
                    throw new System.NotSupportedException("Please create a private instance");
                }
            }

            public override double DocCountPercent
            {
                set
                {
                    throw new System.NotSupportedException("Please create a private instance");
                }
            }
        }

        /// <summary>
        /// Constructs a query matching terms that cannot be represented with a single
        /// Term.
        /// </summary>
        public MultiTermQuery(string field)
        {
            if (field == null)
            {
                throw new System.ArgumentException("field must not be null");
            }
            this.field = field;
        }

        /// <summary>
        /// Returns the field name for this query </summary>
        public string Field
        {
            get
            {
                return field;
            }
        }

        /// <summary>
        /// Construct the enumeration to be used, expanding the
        ///  pattern term.  this method should only be called if
        ///  the field exists (ie, implementations can assume the
        ///  field does exist).  this method should not return null
        ///  (should instead return <seealso cref="TermsEnum#EMPTY"/> if no
        ///  terms match).  The TermsEnum must already be
        ///  positioned to the first matching term.
        /// The given <seealso cref="AttributeSource"/> is passed by the <seealso cref="RewriteMethod"/> to
        /// provide attributes, the rewrite method uses to inform about e.g. maximum competitive boosts.
        /// this is currently only used by <seealso cref="TopTermsRewrite"/>
        /// </summary>
        protected abstract TermsEnum GetTermsEnum(Terms terms, AttributeSource atts);

        /// <summary>
        /// Convenience method, if no attributes are needed:
        /// this simply passes empty attributes and is equal to:
        /// <code>getTermsEnum(terms, new AttributeSource())</code>
        /// </summary>
        public TermsEnum GetTermsEnum(Terms terms)
        {
            return GetTermsEnum(terms, new AttributeSource());
        }

        /// <summary>
        /// To rewrite to a simpler form, instead return a simpler
        /// enum from <seealso cref="#getTermsEnum(Terms, AttributeSource)"/>.  For example,
        /// to rewrite to a single term, return a <seealso cref="SingleTermsEnum"/>
        /// </summary>
        public override sealed Query Rewrite(IndexReader reader)
        {
            return rewriteMethod.Rewrite(reader, this);
        }

        /*
              /// <seealso cref= #setRewriteMethod </seealso>
              public virtual RewriteMethod RewriteMethod
              {
                  get
                  {
                    return rewriteMethod;
                  }
                  set
                  {
                    rewriteMethod = value;
                  }
              }
                */

        public virtual RewriteMethod GetRewriteMethod() // LUCENENET TODO: Make property ? Find out why the above was abandoned
        {
            return rewriteMethod;
        }

        public virtual void SetRewriteMethod(RewriteMethod value) // LUCENENET TODO: Make property ?
        {
            rewriteMethod = value;
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + Number.FloatToIntBits(Boost);
            result = prime * result + rewriteMethod.GetHashCode();
            if (field != null)
            {
                result = prime * result + field.GetHashCode();
            }
            return result;
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
            MultiTermQuery other = (MultiTermQuery)obj;
            if (Number.FloatToIntBits(Boost) != Number.FloatToIntBits(other.Boost))
            {
                return false;
            }
            if (!rewriteMethod.Equals(other.rewriteMethod))
            {
                return false;
            }
            return (other.field == null ? field == null : other.field.Equals(field));
        }
    }
}