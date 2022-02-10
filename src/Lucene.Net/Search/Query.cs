using System;
using System.Collections.Generic;

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

    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// The abstract base class for queries.
    ///    <para/>Instantiable subclasses are:
    ///    <list type="bullet">
    ///    <item><description> <seealso cref="TermQuery"/> </description></item>
    ///    <item><description> <seealso cref="BooleanQuery"/> </description></item>
    ///    <item><description> <seealso cref="WildcardQuery"/> </description></item>
    ///    <item><description> <seealso cref="PhraseQuery"/> </description></item>
    ///    <item><description> <seealso cref="PrefixQuery"/> </description></item>
    ///    <item><description> <seealso cref="MultiPhraseQuery"/> </description></item>
    ///    <item><description> <seealso cref="FuzzyQuery"/> </description></item>
    ///    <item><description> <seealso cref="RegexpQuery"/> </description></item>
    ///    <item><description> <seealso cref="TermRangeQuery"/> </description></item>
    ///    <item><description> <seealso cref="NumericRangeQuery"/> </description></item>
    ///    <item><description> <seealso cref="ConstantScoreQuery"/> </description></item>
    ///    <item><description> <seealso cref="DisjunctionMaxQuery"/> </description></item>
    ///    <item><description> <seealso cref="MatchAllDocsQuery"/> </description></item>
    ///    </list>
    ///    <para/>See also the family of Span Queries (<see cref="Lucene.Net.Search.Spans"/>)
    ///       and additional queries available in the <a href="{@docRoot}/../queries/overview-summary.html">Queries module</a>
    /// </summary>
    public abstract class Query // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        // LUCENENET NOTE: We can't set the default boost in the constructor because the
        // Boost property can be overridden by subclasses (and possibly throw exceptions).
        private float boost = 1.0f; // query boost factor


        /// <summary>
        /// Gets or Sets the boost for this query clause.  Documents
        /// matching this clause will (in addition to the normal weightings) have
        /// their score multiplied by <see cref="Boost"/>. The boost is 1.0 by default.
        /// </summary>
        public virtual float Boost
        {
            get => boost;
            set => boost = value;
        }

        /// <summary>
        /// Prints a query to a string, with <paramref name="field"/> assumed to be the
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
        /// Expert: Constructs an appropriate <see cref="Weight"/> implementation for this query.
        ///
        /// <para/>
        /// Only implemented by primitive queries, which re-write to themselves.
        /// </summary>
        public virtual Weight CreateWeight(IndexSearcher searcher)
        {
            throw UnsupportedOperationException.Create("Query " + this + " does not implement createWeight");
        }

        /// <summary>
        /// Expert: called to re-write queries into primitive queries. For example,
        /// a <see cref="PrefixQuery"/> will be rewritten into a <see cref="BooleanQuery"/> that consists
        /// of <see cref="TermQuery"/>s.
        /// </summary>
        public virtual Query Rewrite(IndexReader reader)
        {
            return this;
        }

        /// <summary>
        /// Expert: adds all terms occurring in this query to the terms set. Only
        /// works if this query is in its rewritten (<see cref="Rewrite(IndexReader)"/>) form.
        /// </summary>
        /// <exception cref="InvalidOperationException"> If this query is not yet rewritten </exception>
        public virtual void ExtractTerms(ISet<Term> terms)
        {
            // needs to be implemented by query subclasses
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Returns a clone of this query. </summary>
        public virtual object Clone()
        {
            return MemberwiseClone(); // LUCENENET: MemberwiseClone() never throws in .NET and there is no need to cast the result here.
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + J2N.BitConversion.SingleToInt32Bits(Boost);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
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

            var other = obj as Query;

            if (J2N.BitConversion.SingleToInt32Bits(Boost) != J2N.BitConversion.SingleToInt32Bits(other.Boost))
            {
                return false;
            }
            return true;
        }
    }
}