using System;

namespace Lucene.Net.Search
{
    using Lucene.Net.Support;
    using System.Collections.Generic;

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

    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// The abstract base class for queries.
    ///    <p>Instantiable subclasses are:
    ///    <ul>
    ///    <li> <seealso cref="TermQuery"/>
    ///    <li> <seealso cref="BooleanQuery"/>
    ///    <li> <seealso cref="WildcardQuery"/>
    ///    <li> <seealso cref="PhraseQuery"/>
    ///    <li> <seealso cref="PrefixQuery"/>
    ///    <li> <seealso cref="MultiPhraseQuery"/>
    ///    <li> <seealso cref="FuzzyQuery"/>
    ///    <li> <seealso cref="RegexpQuery"/>
    ///    <li> <seealso cref="TermRangeQuery"/>
    ///    <li> <seealso cref="NumericRangeQuery"/>
    ///    <li> <seealso cref="ConstantScoreQuery"/>
    ///    <li> <seealso cref="DisjunctionMaxQuery"/>
    ///    <li> <seealso cref="MatchAllDocsQuery"/>
    ///    </ul>
    ///    <p>See also the family of <seealso cref="Lucene.Net.Search.Spans Span Queries"/>
    ///       and additional queries available in the <a href="{@docRoot}/../queries/overview-summary.html">Queries module</a>
    /// </summary>
    public abstract class Query
    {
        protected Query()
        {
            Boost = 1.0f; // query boost factor
        }

        /// <summary>
        /// Sets the boost for this query clause to <code>b</code>.  Documents
        /// matching this clause will (in addition to the normal weightings) have
        /// their score multiplied by <code>b</code>.
        /// </summary>
        public virtual float Boost { get; set; }

        /// <summary>
        /// Prints a query to a string, with <code>field</code> assumed to be the
        /// default field and omitted.
        /// </summary>
        public abstract string ToString(string field);

        /// <summary>
        /// Prints a query to a string. </summary>
        public override string ToString()
        {
            return ToString("");
        }

        /// <summary>
        /// Expert: Constructs an appropriate Weight implementation for this query.
        ///
        /// <p>
        /// Only implemented by primitive queries, which re-write to themselves.
        /// </summary>
        public virtual Weight CreateWeight(IndexSearcher searcher)
        {
            throw new System.NotSupportedException("Query " + this + " does not implement createWeight");
        }

        /// <summary>
        /// Expert: called to re-write queries into primitive queries. For example,
        /// a PrefixQuery will be rewritten into a BooleanQuery that consists
        /// of TermQuerys.
        /// </summary>
        public virtual Query Rewrite(IndexReader reader)
        {
            return this;
        }

        /// <summary>
        /// Expert: adds all terms occurring in this query to the terms set. Only
        /// works if this query is in its <seealso cref="#rewrite rewritten"/> form.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this query is not yet rewritten </exception>
        public virtual void ExtractTerms(ISet<Term> terms)
        {
            // needs to be implemented by query subclasses
            throw new System.NotSupportedException();
        }

        /// <summary>
        /// Returns a clone of this query. </summary>
        public virtual object Clone()
        {
            try
            {
                return (Query)base.MemberwiseClone();
            }
            catch (Exception e)
            {
                throw new Exception("Clone not supported: " + e.Message);
            }
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + Number.FloatToIntBits(Boost);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
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

            var other = obj as Query;

            if (Number.FloatToIntBits(Boost) != Number.FloatToIntBits(other.Boost))
            {
                return false;
            }
            return true;
        }
    }
}