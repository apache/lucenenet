using System;
using System.ComponentModel;

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
    /// A clause in a BooleanQuery. </summary>
    public class BooleanClause : IEquatable<BooleanClause>
    {
        /// <summary>
        /// Specifies how clauses are to occur in matching documents. </summary>
        public enum Occur // LUCENENET TODO: Move outside of BooleanClause class
        {
            /// <summary>
            /// Use this operator for clauses that <i>must</i> appear in the matching documents.
            /// </summary>
            MUST,

            /// <summary>
            /// Use this operator for clauses that <i>should</i> appear in the
            /// matching documents. For a BooleanQuery with no <code>MUST</code>
            /// clauses one or more <code>SHOULD</code> clauses must match a document
            /// for the BooleanQuery to match. </summary>
            /// <seealso cref= BooleanQuery#setMinimumNumberShouldMatch</seealso>
            SHOULD,

            /// <summary>
            /// Use this operator for clauses that <i>must not</i> appear in the matching documents.
            /// Note that it is not possible to search for queries that only consist
            /// of a <code>MUST_NOT</code> clause.
            /// </summary>
            MUST_NOT
        }

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
                    throw new Exception("Invalid Occur value");
            }
        }

        /// <summary>
        /// The query whose matching documents are combined by the boolean query.
        /// </summary>
        private Query query;

        private Occur occur;

        /// <summary>
        /// Constructs a BooleanClause.
        /// </summary>
        public BooleanClause(Query query, Occur occur)
        {
            this.query = query;
            this.occur = occur;
        }

        public Occur Occur_
        {
            get
            {
                return occur;
            }
            set
            {
                occur = value;
            }
        }

        public Query Query
        {
            get
            {
                return query;
            }
            set
            {
                query = value;
            }
        }

        public bool Prohibited // LUCENENET TODO: Rename IsProhibited
        {
            get
            {
                return Occur.MUST_NOT == occur;
            }
        }

        public bool Required // LUCENENET TODO: Rename IsRequired
        {
            get
            {
                return Occur.MUST == occur;
            }
        }

        /// <summary>
        /// Returns true if <code>o</code> is equal to this. </summary>
        public bool Equals(object o)
        {
            BooleanClause bc = o as BooleanClause;
            return this.Equals(bc);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public int GetHashCode()
        {
            return query.GetHashCode() ^ (Occur.MUST == occur ? 1 : 0) ^ (Occur.MUST_NOT == occur ? 2 : 0);
        }

        public bool Equals(BooleanClause other)
        {
            bool success = true;
            if (object.ReferenceEquals(null, other))
            {
                return object.ReferenceEquals(null, this);
            }
            if (query == null)
            {
                success &= other.Query == null;
            }
            return success && this.query.Equals(other.query) && this.occur == other.occur;
        }

        public string ToString()
        {
            return ToString(occur) + query.ToString();
        }
    }
}