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

    /// <summary>
    /// A clause in a <see cref="BooleanQuery"/>. </summary>
    public class BooleanClause : IEquatable<BooleanClause>
    {
        // LUCENENET specific - de-nested Occur from BooleanClause in order to prevent
        // a naming conflict with the Occur property

        public static string ToString(Occur occur)
        {
            switch (occur)
            {
                case Occur.MUST:
                    return "+";

                case Occur.SHOULD:
                    return "";

                case Occur.MUST_NOT:
                    return "-";

                default:
                    throw new ArgumentOutOfRangeException(nameof(occur), "Invalid Occur value"); // LUCENENET specific
            }
        }

        /// <summary>
        /// The query whose matching documents are combined by the boolean query.
        /// </summary>
        private Query query;

        private Occur occur;

        /// <summary>
        /// Constructs a <see cref="BooleanClause"/>.
        /// </summary>
        public BooleanClause(Query query, Occur occur)
        {
            this.query = query;
            this.occur = occur;
        }

        public virtual Occur Occur
        {
            get => occur;
            set => occur = value;
        }

        public virtual Query Query
        {
            get => query;
            set => query = value;
        }

        public virtual bool IsProhibited => Occur.MUST_NOT == occur;

        public virtual bool IsRequired => Occur.MUST == occur;

        /// <summary>
        /// Returns <c>true</c> if <paramref name="o"/> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (o is BooleanClause other)
                return this.Equals(other);
            return false;
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return query.GetHashCode() 
                ^ (Occur.MUST == occur ? 1 : 0) 
                ^ (Occur.MUST_NOT == occur ? 2 : 0);
        }

        // LUCENENET specific
        public bool Equals(BooleanClause other)
        {
            if (null == other)
                return false;

            bool success;
            if (query is null)
                success = other.Query is null;
            else
                success = query.Equals(other.query);

            return success && this.occur == other.occur;
        }

        public override string ToString()
        {
            return ToString(occur) + query.ToString();
        }
    }

    /// <summary>
    /// Specifies how clauses are to occur in matching documents. </summary>
    public enum Occur
    {
        /// <summary>
        /// Use this operator for clauses that <i>must</i> appear in the matching documents.
        /// </summary>
        MUST,

        /// <summary>
        /// Use this operator for clauses that <i>should</i> appear in the
        /// matching documents. For a <see cref="BooleanQuery"/> with no <see cref="MUST"/>
        /// clauses one or more <see cref="SHOULD"/> clauses must match a document
        /// for the <see cref="BooleanQuery"/> to match. </summary>
        /// <seealso cref="BooleanQuery.MinimumNumberShouldMatch"/>
        SHOULD,

        /// <summary>
        /// Use this operator for clauses that <i>must not</i> appear in the matching documents.
        /// Note that it is not possible to search for queries that only consist
        /// of a <see cref="MUST_NOT"/> clause.
        /// </summary>
        MUST_NOT
    }
}