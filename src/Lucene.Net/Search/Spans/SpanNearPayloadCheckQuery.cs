using J2N.Numerics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System.Collections;
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

    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// Only return those matches that have a specific payload at
    /// the given position.
    /// </summary>
    public class SpanNearPayloadCheckQuery : SpanPositionCheckQuery
    {
        protected readonly ICollection<byte[]> m_payloadToMatch;
        private readonly IEqualityComparer payloadEqualityComparer;

        /// <param name="match">          The underlying <see cref="SpanQuery"/> to check.</param>
        /// <param name="payloadToMatch"> The <see cref="T:ICollection{byte[]}"/> of payloads to match.
        /// IMPORTANT: If the type provided does not implement <see cref="IList{T}"/> (including arrays) or
        /// <see cref="ISet{T}"/>, it should either implement <see cref="IStructuralEquatable"/> or override
        /// <see cref="object.Equals(object)"/> and <see cref="object.GetHashCode()"/> with implementations
        /// that compare the values of the byte arrays to ensure they are the same.</param>
        public SpanNearPayloadCheckQuery(SpanNearQuery match, ICollection<byte[]> payloadToMatch)
            : base(match)
        {
            this.m_payloadToMatch = payloadToMatch;

            // LUCENENET specific: Need to use a structural equality comparer based on the type that is passed in.
            if (payloadToMatch is ISet<byte[]>)
                payloadEqualityComparer = J2N.Collections.Generic.SetEqualityComparer<byte[]>.Default;
            else if (payloadToMatch is IList<byte[]>)
                payloadEqualityComparer = J2N.Collections.Generic.ListEqualityComparer<byte[]>.Default;
            else
                payloadEqualityComparer = J2N.Collections.StructuralEqualityComparer.Default;
        }

        protected override AcceptStatus AcceptPosition(Spans spans)
        {
            bool result = spans.IsPayloadAvailable;
            if (result == true)
            {
                var candidate = spans.GetPayload();
                if (candidate.Count == m_payloadToMatch.Count)
                {
                    //TODO: check the byte arrays are the same
                    //hmm, can't rely on order here
                    int matches = 0;
                    foreach (var candBytes in candidate)
                    {
                        //Unfortunately, we can't rely on order, so we need to compare all
                        foreach (var payBytes in m_payloadToMatch)
                        {
                            if (Arrays.Equals(candBytes, payBytes) == true)
                            {
                                matches++;
                                break;
                            }
                        }
                    }
                    if (matches == m_payloadToMatch.Count)
                    {
                        //we've verified all the bytes
                        return AcceptStatus.YES;
                    }
                    else
                    {
                        return AcceptStatus.NO;
                    }
                }
                else
                {
                    return AcceptStatus.NO;
                }
            }
            return AcceptStatus.NO;
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("spanPayCheck(");
            buffer.Append(m_match.ToString(field));
            buffer.Append(", payloadRef: ");
            foreach (var bytes in m_payloadToMatch)
            {
                ToStringUtils.ByteArray(buffer, bytes);
                buffer.Append(';');
            }
            buffer.Append(')');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override object Clone()
        {
            SpanNearPayloadCheckQuery result = new SpanNearPayloadCheckQuery((SpanNearQuery)m_match.Clone(), m_payloadToMatch);
            result.Boost = Boost;
            return result;
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is SpanNearPayloadCheckQuery other))
            {
                return false;
            }

            // LUCENENET NOTE: Need to use the structural equality comparer to compare equality of all contained values
            return payloadEqualityComparer.Equals(this.m_payloadToMatch, other.m_payloadToMatch)
                && this.m_match.Equals(other.m_match)
                // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                && NumericUtils.SingleToSortableInt32(this.Boost) == NumericUtils.SingleToSortableInt32(other.Boost);
        }

        public override int GetHashCode()
        {
            int h = m_match.GetHashCode();
            h ^= (h << 8) | (h.TripleShift(25)); // reversible
            //TODO: is this right?
            h ^= payloadEqualityComparer.GetHashCode(m_payloadToMatch); // LUCENENET NOTE: Need to use the structural equality comparer to compare equality of all contained values
            h ^= J2N.BitConversion.SingleToRawInt32Bits(Boost);
            return h;
        }
    }
}