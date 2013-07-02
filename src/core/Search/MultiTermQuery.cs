/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
	
	/// <summary> An abstract <see cref="Query" /> that matches documents
	/// containing a subset of terms provided by a <see cref="FilteredTermEnum" />
	/// enumeration.
	/// 
	/// <p/>This query cannot be used directly; you must subclass
	/// it and define <see cref="GetEnum" /> to provide a <see cref="FilteredTermEnum" />
	/// that iterates through the terms to be
	/// matched.
	/// 
	/// <p/><b>NOTE</b>: if <see cref="RewriteMethod" /> is either
	/// <see cref="CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE" /> or <see cref="SCORING_BOOLEAN_QUERY_REWRITE" />
	///, you may encounter a
	/// <see cref="BooleanQuery.TooManyClauses" /> exception during
	/// searching, which happens when the number of terms to be
	/// searched exceeds <see cref="BooleanQuery.MaxClauseCount" />
	///.  Setting <see cref="RewriteMethod" />
	/// to <see cref="CONSTANT_SCORE_FILTER_REWRITE" />
	/// prevents this.
	/// 
	/// <p/>The recommended rewrite method is <see cref="CONSTANT_SCORE_AUTO_REWRITE_DEFAULT" />
	///: it doesn't spend CPU
	/// computing unhelpful scores, and it tries to pick the most
	/// performant rewrite method given the query.
	/// 
	/// Note that <see cref="QueryParser" /> produces
	/// MultiTermQueries using <see cref="CONSTANT_SCORE_AUTO_REWRITE_DEFAULT" />
	/// by default.
	/// </summary>
    [Serializable]
    public abstract class MultiTermQuery : Query
    {
        [Serializable]
        public class AnonymousClassConstantScoreAutoRewrite : ConstantScoreAutoRewrite
        {
            public override int TermCountCutoff
            {
                set { throw new NotSupportedException("Please create a private instance"); }
            }

            public override double DocCountPercent
            {
                set { throw new NotSupportedException("Please create a private instance"); }
            }

            // Make sure we are still a singleton even after deserializing
            protected internal virtual object ReadResolve()
            {
                return CONSTANT_SCORE_AUTO_REWRITE_DEFAULT;
            }
        }
        protected internal RewriteMethod internalRewriteMethod = CONSTANT_SCORE_AUTO_REWRITE_DEFAULT;

        [Serializable]
        private sealed class ConstantScoreFilterRewrite : RewriteMethod
        {
            public override Query Rewrite(IndexReader reader, MultiTermQuery query)
            {
                Query result = new ConstantScoreQuery(new MultiTermQueryWrapperFilter<MultiTermQuery>(query));
                result.Boost = query.Boost;
                return result;
            }

            // Make sure we are still a singleton even after deserializing
            internal object ReadResolve()
            {
                return CONSTANT_SCORE_FILTER_REWRITE;
            }
        }

        /// <summary>A rewrite method that first creates a private Filter,
        /// by visiting each term in sequence and marking all docs
        /// for that term.  Matching documents are assigned a
        /// constant score equal to the query's boost.
        /// 
        /// <p/> This method is faster than the BooleanQuery
        /// rewrite methods when the number of matched terms or
        /// matched documents is non-trivial. Also, it will never
        /// hit an errant <see cref="BooleanQuery.TooManyClauses" />
        /// exception.
        /// 
        /// </summary>
        /// <seealso cref="RewriteMethod">
        /// </seealso>
        public static readonly RewriteMethod CONSTANT_SCORE_FILTER_REWRITE = new ConstantScoreFilterRewrite();

	    /// <summary>A rewrite method that first translates each term into
	    /// <see cref="Occur.SHOULD" /> clause in a
	    /// BooleanQuery, and keeps the scores as computed by the
	    /// query.  Note that typically such scores are
	    /// meaningless to the user, and require non-trivial CPU
	    /// to compute, so it's almost always better to use <see cref="CONSTANT_SCORE_AUTO_REWRITE_DEFAULT" />
	    /// instead.
	    /// 
	    /// <p/><b>NOTE</b>: This rewrite method will hit <see cref="BooleanQuery.TooManyClauses" />
	    /// if the number of terms
	    /// exceeds <see cref="BooleanQuery.MaxClauseCount" />.
	    /// 
	    /// </summary>
	    /// <seealso cref="RewriteMethod">
	    /// </seealso>
	    public static readonly RewriteMethod SCORING_BOOLEAN_QUERY_REWRITE =
	        ScoringRewrite<MultiTermQuery>.SCORING_BOOLEAN_QUERY_REWRITE;

	    /// <summary>Like <see cref="SCORING_BOOLEAN_QUERY_REWRITE" /> except
	    /// scores are not computed.  Instead, each matching
	    /// document receives a constant score equal to the
	    /// query's boost.
	    /// 
	    /// <p/><b>NOTE</b>: This rewrite method will hit <see cref="BooleanQuery.TooManyClauses" />
	    /// if the number of terms
	    /// exceeds <see cref="BooleanQuery.MaxClauseCount" />.
	    /// 
	    /// </summary>
	    /// <seealso cref="RewriteMethod">
	    /// </seealso>
	    public static readonly RewriteMethod CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE =
	        ScoringRewrite<MultiTermQuery>.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE;


        /// <summary>Read-only default instance of <see cref="ConstantScoreAutoRewrite" />
        ///, with <see cref="ConstantScoreAutoRewrite.TermCountCutoff" />
        /// set to
        /// <see cref="ConstantScoreAutoRewrite.DEFAULT_TERM_COUNT_CUTOFF" />
        ///
        /// and <see cref="ConstantScoreAutoRewrite.DocCountPercent" />
        /// set to
        /// <see cref="ConstantScoreAutoRewrite.DEFAULT_DOC_COUNT_PERCENT" />
        ///.
        /// Note that you cannot alter the configuration of this
        /// instance; you'll need to create a private instance
        /// instead. 
        /// </summary>
        public static readonly RewriteMethod CONSTANT_SCORE_AUTO_REWRITE_DEFAULT;

        /// <summary> Constructs a query matching terms that cannot be represented with a single
        /// Term.
        /// </summary>
        protected MultiTermQuery()
        {
        }

        protected MultiTermQuery(string field)
        {
            if (field == null) throw new ArgumentNullException("field");

            this.field = field;
        }

        protected readonly string field;
        public virtual string Field { get { return field; } }

        public override Query Rewrite(IndexReader reader)
        {
            return internalRewriteMethod.Rewrite(reader, this);
        }


        // .NET PORT -- had to keep the Java-style getter and setter because property and nested type can't have same name
	    /// <summary> Sets the rewrite method to be used when executing the
	    /// query.  You can use one of the four core methods, or
	    /// implement your own subclass of <see cref="Lucene.Net.Search.MultiTermQuery.RewriteMethod" />. 
	    /// </summary>
	    public virtual void SetRewriteMethod(RewriteMethod value)
	    {
	        internalRewriteMethod = value;
	    }

	    /// <summary> Sets the rewrite method to be used when executing the
	    /// query.  You can use one of the four core methods, or
	    /// implement your own subclass of <see cref="Lucene.Net.Search.MultiTermQuery.RewriteMethod" />. 
	    /// </summary>
	    public virtual RewriteMethod GetRewriteMethod()
	    {
	        return internalRewriteMethod;
	    }

	    public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + System.Convert.ToInt32(Boost);
            result = prime * result;
            result += internalRewriteMethod.GetHashCode();
            return result;
        }

        public override bool Equals(System.Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            var other = (MultiTermQuery)obj;
            if (Convert.ToInt32(Boost) != Convert.ToInt32(other.Boost))
                return false;
            if (!internalRewriteMethod.Equals(other.internalRewriteMethod))
            {
                return false;
            }
            return true;
        }

	    protected internal abstract TermsEnum GetTermsEnum(Terms terms, AttributeSource atts);

        protected internal TermsEnum GetTermsEnum(Terms terms)
        {
            return GetTermsEnum(terms, new AttributeSource());
        }

        static MultiTermQuery()
        {
            CONSTANT_SCORE_AUTO_REWRITE_DEFAULT = new AnonymousClassConstantScoreAutoRewrite();
        }

        /// <summary>Abstract class that defines how the query is rewritten. </summary>
        [Serializable]
        public abstract class RewriteMethod
        {
            public abstract Query Rewrite(IndexReader reader, MultiTermQuery query);

            protected virtual TermsEnum GetTermsEnum(MultiTermQuery query, Terms terms, AttributeSource atts)
            {
                return query.GetTermsEnum(terms, atts);
            }
        }
    }
}