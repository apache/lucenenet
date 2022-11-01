// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.Queries
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
    /// The <see cref="BoostingQuery"/> class can be used to effectively demote results that match a given query. 
    /// Unlike the "NOT" clause, this still selects documents that contain undesirable terms, 
    /// but reduces their overall score:
    /// <code>
    ///     Query balancedQuery = new BoostingQuery(positiveQuery, negativeQuery, 0.01f);
    /// </code>
    /// In this scenario the positiveQuery contains the mandatory, desirable criteria which is used to 
    /// select all matching documents, and the negativeQuery contains the undesirable elements which 
    /// are simply used to lessen the scores. Documents that match the negativeQuery have their score 
    /// multiplied by the supplied "boost" parameter, so this should be less than 1 to achieve a 
    /// demoting effect
    /// 
    /// This code was originally made available here: <c>[WWW] http://marc.theaimsgroup.com/?l=lucene-user&amp;m=108058407130459&amp;w=2 </c>
    /// and is documented here: <c>http://wiki.apache.org/lucene-java/CommunityContributions</c>
    /// </summary>
    public class BoostingQuery : Query
    {
        private readonly float boost; // the amount to boost by
        private readonly Query match; // query to match
        private readonly Query context; // boost when matches too

        public BoostingQuery(Query match, Query context, float boost)
        {
            this.match = match;
            this.context = (Query)context.Clone(); // clone before boost
            this.boost = boost;
            this.context.Boost = 0.0f; // ignore context-only matches
        }

        public override Query Rewrite(IndexReader reader)
        {
            return new BooleanQueryAnonymousClass(this)
            {
                { match, Occur.MUST },
                { context, Occur.SHOULD }
            };
        }

        private sealed class BooleanQueryAnonymousClass : BooleanQuery
        {
            private readonly BoostingQuery outerInstance;

            public BooleanQueryAnonymousClass(BoostingQuery outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override Weight CreateWeight(IndexSearcher searcher)
            {
                return new BooleanWeightAnonymousClass(this, searcher);
            }

            private sealed class BooleanWeightAnonymousClass : BooleanWeight
            {
                private readonly BooleanQueryAnonymousClass outerInstance;

                public BooleanWeightAnonymousClass(BooleanQueryAnonymousClass outerInstance, IndexSearcher searcher)
                    : base(outerInstance, searcher, false)
                {
                    this.outerInstance = outerInstance;
                }

                public override float Coord(int overlap, int max)
                {
                    switch (overlap)
                    {

                        case 1: // matched only one clause
                            return 1.0f; // use the score as-is

                        case 2: // matched both clauses
                            return outerInstance.outerInstance.boost; // multiply by boost

                        default:
                            return 0.0f;

                    }
                }
            }
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + J2N.BitConversion.SingleToInt32Bits(boost);
            result = prime * result + ((context is null) ? 0 : context.GetHashCode());
            result = prime * result + ((match is null) ? 0 : match.GetHashCode());
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

            if (!base.Equals(obj))
            {
                return false;
            }

            var other = (BoostingQuery)obj;
            if (J2N.BitConversion.SingleToInt32Bits(boost) != J2N.BitConversion.SingleToInt32Bits(other.boost))
            {
                return false;
            }

            if (context is null)
            {
                if (other.context != null)
                {
                    return false;
                }
            }
            else if (!context.Equals(other.context))
            {
                return false;
            }

            if (match is null)
            {
                if (other.match != null)
                {
                    return false;
                }
            }
            else if (!match.Equals(other.match))
            {
                return false;
            }
            return true;
        }

        public override string ToString(string field)
        {
            return match.ToString(field) + "/" + context.ToString(field);
        }
    }
}