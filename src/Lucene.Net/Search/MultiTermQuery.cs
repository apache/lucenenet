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

    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// An abstract <see cref="Query"/> that matches documents
    /// containing a subset of terms provided by a 
    /// <see cref="Index.FilteredTermsEnum"/> enumeration.
    ///
    /// <para/>This query cannot be used directly; you must subclass
    /// it and define <see cref="GetTermsEnum(Terms,AttributeSource)"/> to provide a 
    /// <see cref="Index.FilteredTermsEnum"/> that iterates through the terms to be
    /// matched.
    ///
    /// <para/><b>NOTE</b>: if <see cref="MultiTermRewriteMethod"/> is either
    /// <see cref="CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE"/> or
    /// <see cref="SCORING_BOOLEAN_QUERY_REWRITE"/>, you may encounter a
    /// <see cref="BooleanQuery.TooManyClausesException"/> exception during
    /// searching, which happens when the number of terms to be
    /// searched exceeds 
    /// <see cref="BooleanQuery.MaxClauseCount"/>.  Setting 
    /// <see cref="MultiTermRewriteMethod"/> to <see cref="CONSTANT_SCORE_FILTER_REWRITE"/>
    /// prevents this.
    ///
    /// <para/>The recommended rewrite method is 
    /// <see cref="CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/>: it doesn't spend CPU
    /// computing unhelpful scores, and it tries to pick the most
    /// performant rewrite method given the query. If you
    /// need scoring (like <seea cref="FuzzyQuery"/>, use
    /// <see cref="TopTermsScoringBooleanQueryRewrite"/> which uses
    /// a priority queue to only collect competitive terms
    /// and not hit this limitation.
    ///
    /// <para/>Note that QueryParsers.Classic.QueryParser produces
    /// <see cref="MultiTermQuery"/>s using 
    /// <see cref="CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/> by default.
    /// </summary>
    public abstract class MultiTermQuery : Query
    {
        protected internal readonly string m_field;
        protected RewriteMethod m_rewriteMethod = CONSTANT_SCORE_AUTO_REWRITE_DEFAULT;

        /// <summary>
        /// Abstract class that defines how the query is rewritten. </summary>
        public abstract class RewriteMethod
        {
            public abstract Query Rewrite(IndexReader reader, MultiTermQuery query);

            /// <summary>
            /// Returns the <see cref="MultiTermQuery"/>s <see cref="TermsEnum"/> </summary>
            /// <seealso cref="MultiTermQuery.GetTermsEnum(Terms, AttributeSource)"/>
            protected virtual TermsEnum GetTermsEnum(MultiTermQuery query, Terms terms, AttributeSource atts)
            {
                return query.GetTermsEnum(terms, atts); // allow RewriteMethod subclasses to pull a TermsEnum from the MTQ
            }
        }

        /// <summary>
        /// A rewrite method that first creates a private <see cref="Filter"/>,
        /// by visiting each term in sequence and marking all docs
        /// for that term.  Matching documents are assigned a
        /// constant score equal to the query's boost.
        ///
        /// <para/> This method is faster than the <see cref="BooleanQuery"/>
        /// rewrite methods when the number of matched terms or
        /// matched documents is non-trivial. Also, it will never
        /// hit an errant <see cref="BooleanQuery.TooManyClausesException"/>
        /// exception.
        /// </summary>
        /// <seealso cref="MultiTermRewriteMethod"/>
        public static readonly RewriteMethod CONSTANT_SCORE_FILTER_REWRITE = new RewriteMethodAnonymousClass();

        private sealed class RewriteMethodAnonymousClass : RewriteMethod
        {
            public RewriteMethodAnonymousClass()
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
        /// <see cref="Occur.SHOULD"/> clause in a
        /// <see cref="BooleanQuery"/>, and keeps the scores as computed by the
        /// query.  Note that typically such scores are
        /// meaningless to the user, and require non-trivial CPU
        /// to compute, so it's almost always better to use 
        /// <see cref="CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/> instead.
        ///
        /// <para/><b>NOTE</b>: this rewrite method will hit 
        /// <see cref="BooleanQuery.TooManyClausesException"/> if the number of terms
        /// exceeds <see cref="BooleanQuery.MaxClauseCount"/>.
        /// </summary>
        /// <seealso cref="MultiTermRewriteMethod"/>
        public static readonly RewriteMethod SCORING_BOOLEAN_QUERY_REWRITE = ScoringRewrite<MultiTermQuery>.SCORING_BOOLEAN_QUERY_REWRITE;

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
        /// <seealso cref="MultiTermRewriteMethod"/>
        public static readonly RewriteMethod CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE = ScoringRewrite<MultiTermQuery>.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE;

        /// <summary>
        /// A rewrite method that first translates each term into
        /// <see cref="Occur.SHOULD"/> clause in a <see cref="BooleanQuery"/>, and keeps the
        /// scores as computed by the query.
        ///
        /// <para/>
        /// This rewrite method only uses the top scoring terms so it will not overflow
        /// the boolean max clause count. It is the default rewrite method for
        /// <see cref="FuzzyQuery"/>.
        /// </summary>
        /// <seealso cref="MultiTermRewriteMethod"/>
        public sealed class TopTermsScoringBooleanQueryRewrite : TopTermsRewrite<BooleanQuery>
        {
            /// <summary>
            /// Create a <see cref="TopTermsScoringBooleanQueryRewrite"/> for
            /// at most <paramref name="size"/> terms.
            /// <para/>
            /// NOTE: if <see cref="BooleanQuery.MaxClauseCount"/> is smaller than
            /// <paramref name="size"/>, then it will be used instead.
            /// </summary>
            public TopTermsScoringBooleanQueryRewrite(int size)
                : base(size)
            {
            }

            protected override int MaxSize => BooleanQuery.MaxClauseCount;

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
        }

        /// <summary>
        /// A rewrite method that first translates each term into
        /// <see cref="Occur.SHOULD"/> clause in a <see cref="BooleanQuery"/>, but the scores
        /// are only computed as the boost.
        /// <para/>
        /// This rewrite method only uses the top scoring terms so it will not overflow
        /// the boolean max clause count.
        /// </summary>
        /// <seealso cref="MultiTermRewriteMethod"/>
        public sealed class TopTermsBoostOnlyBooleanQueryRewrite : TopTermsRewrite<BooleanQuery>
        {
            /// <summary>
            /// Create a <see cref="TopTermsBoostOnlyBooleanQueryRewrite"/> for
            /// at most <paramref name="size"/> terms.
            /// <para/>
            /// NOTE: if <see cref="BooleanQuery.MaxClauseCount"/> is smaller than
            /// <paramref name="size"/>, then it will be used instead.
            /// </summary>
            public TopTermsBoostOnlyBooleanQueryRewrite(int size)
                : base(size)
            {
            }

            protected override int MaxSize => BooleanQuery.MaxClauseCount;

            protected override BooleanQuery GetTopLevelQuery()
            {
                return new BooleanQuery(true);
            }

            protected override void AddClause(BooleanQuery topLevel, Term term, int docFreq, float boost, TermContext states)
            {
                Query q = new ConstantScoreQuery(new TermQuery(term, states));
                q.Boost = boost;
                topLevel.Add(q, Occur.SHOULD);
            }
        }

        // LUCENENET specific - just use the non-nested class directly. This is 
        // confusing in .NET.
//        /// <summary>
//        /// A rewrite method that tries to pick the best
//        /// constant-score rewrite method based on term and
//        /// document counts from the query.  If both the number of
//        /// terms and documents is small enough, then 
//        /// <see cref="CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE"/> is used.
//        /// Otherwise, <see cref="CONSTANT_SCORE_FILTER_REWRITE"/> is
//        /// used.
//        /// </summary>
//        public class ConstantScoreAutoRewrite : Lucene.Net.Search.ConstantScoreAutoRewrite
//        {
//        }

        /// <summary>
        /// Read-only default instance of
        /// <see cref="ConstantScoreAutoRewrite"/>, with 
        /// <see cref="Search.ConstantScoreAutoRewrite.TermCountCutoff"/> set to
        /// <see cref="Search.ConstantScoreAutoRewrite.DEFAULT_TERM_COUNT_CUTOFF"/>
        /// and 
        /// <see cref="Search.ConstantScoreAutoRewrite.DocCountPercent"/> set to
        /// <see cref="Search.ConstantScoreAutoRewrite.DEFAULT_DOC_COUNT_PERCENT"/>.
        /// Note that you cannot alter the configuration of this
        /// instance; you'll need to create a private instance
        /// instead.
        /// </summary>
        public static readonly RewriteMethod CONSTANT_SCORE_AUTO_REWRITE_DEFAULT = new ConstantScoreAutoRewriteAnonymousClass();

        private sealed class ConstantScoreAutoRewriteAnonymousClass : ConstantScoreAutoRewrite
        {
            public ConstantScoreAutoRewriteAnonymousClass()
            {
            }

            public override int TermCountCutoff
            {
                get => base.TermCountCutoff; // LUCENENET specific - adding getter for API consistency check
                set => throw UnsupportedOperationException.Create("Please create a private instance");
            }

            public override double DocCountPercent
            {
                get => base.DocCountPercent; // LUCENENET specific - adding getter for API consistency check
                set => throw UnsupportedOperationException.Create("Please create a private instance");
            }
        }

        /// <summary>
        /// Constructs a query matching terms that cannot be represented with a single
        /// <see cref="Term"/>.
        /// </summary>
        protected MultiTermQuery(string field) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_field = field ?? throw new ArgumentNullException(nameof(field), "field must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Returns the field name for this query </summary>
        public string Field => m_field;

        /// <summary>
        /// Construct the enumeration to be used, expanding the
        /// pattern term.  this method should only be called if
        /// the field exists (ie, implementations can assume the
        /// field does exist).  this method should not return null
        /// (should instead return <see cref="TermsEnum.EMPTY"/> if no
        /// terms match).  The <see cref="TermsEnum"/> must already be
        /// positioned to the first matching term.
        /// The given <see cref="AttributeSource"/> is passed by the <see cref="RewriteMethod"/> to
        /// provide attributes, the rewrite method uses to inform about e.g. maximum competitive boosts.
        /// this is currently only used by <see cref="TopTermsRewrite{Q}"/>.
        /// </summary>
        protected abstract TermsEnum GetTermsEnum(Terms terms, AttributeSource atts);

        /// <summary>
        /// Convenience method, if no attributes are needed:
        /// this simply passes empty attributes and is equal to:
        /// <code>GetTermsEnum(terms, new AttributeSource())</code>
        /// </summary>
        public TermsEnum GetTermsEnum(Terms terms)
        {
            return GetTermsEnum(terms, new AttributeSource());
        }

        /// <summary>
        /// To rewrite to a simpler form, instead return a simpler
        /// enum from <see cref="GetTermsEnum(Terms, AttributeSource)"/>.  For example,
        /// to rewrite to a single term, return a <see cref="Index.SingleTermsEnum"/>.
        /// </summary>
        public override sealed Query Rewrite(IndexReader reader)
        {
            return m_rewriteMethod.Rewrite(reader, this);
        }


        // LUCENENET NOTE: Renamed from RewriteMethod to prevent a naming
        // conflict with the RewriteMethod class. MultiTermRewriteMethod is consistent
        // with the name used in QueryParserBase.
        /// <summary>
        /// Gets or Sets the rewrite method to be used when executing the
        /// query.  You can use one of the four core methods, or
        /// implement your own subclass of <see cref="RewriteMethod"/>.
        /// </summary>
        public virtual RewriteMethod MultiTermRewriteMethod 
        {
            get => m_rewriteMethod;
            set => m_rewriteMethod = value;
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + J2N.BitConversion.SingleToInt32Bits(Boost);
            result = prime * result + m_rewriteMethod.GetHashCode();
            if (m_field != null)
            {
                result = prime * result + m_field.GetHashCode();
            }
            return result;
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
            MultiTermQuery other = (MultiTermQuery)obj;
            if (J2N.BitConversion.SingleToInt32Bits(Boost) != J2N.BitConversion.SingleToInt32Bits(other.Boost))
            {
                return false;
            }
            if (!m_rewriteMethod.Equals(other.m_rewriteMethod))
            {
                return false;
            }
            return (other.m_field is null ? m_field is null : other.m_field.Equals(m_field, StringComparison.Ordinal));
        }
    }
}