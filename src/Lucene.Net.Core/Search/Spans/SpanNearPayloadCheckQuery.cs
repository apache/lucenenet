using Lucene.Net.Support;
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
    /// <p/>
    ///
    /// </summary>
    public class SpanNearPayloadCheckQuery : SpanPositionCheckQuery
    {
        protected readonly ICollection<byte[]> m_payloadToMatch;

        /// <param name="match">          The underlying <seealso cref="SpanQuery"/> to check </param>
        /// <param name="payloadToMatch"> The <seealso cref="java.util.Collection"/> of payloads to match </param>
        public SpanNearPayloadCheckQuery(SpanNearQuery match, ICollection<byte[]> payloadToMatch)
            : base(match)
        {
            this.m_payloadToMatch = payloadToMatch;
        }

        protected override AcceptStatus AcceptPosition(Spans spans)
        {
            bool result = spans.IsPayloadAvailable;
            if (result == true)
            {
                var candidate = spans.Payload;
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
            buffer.Append(match.ToString(field));
            buffer.Append(", payloadRef: ");
            foreach (var bytes in m_payloadToMatch)
            {
                ToStringUtils.ByteArray(buffer, bytes);
                buffer.Append(';');
            }
            buffer.Append(")");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override object Clone()
        {
            SpanNearPayloadCheckQuery result = new SpanNearPayloadCheckQuery((SpanNearQuery)match.Clone(), m_payloadToMatch);
            result.Boost = Boost;
            return result;
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is SpanNearPayloadCheckQuery))
            {
                return false;
            }

            SpanNearPayloadCheckQuery other = (SpanNearPayloadCheckQuery)o;
            return this.m_payloadToMatch.Equals(other.m_payloadToMatch) && this.match.Equals(other.match) && this.Boost == other.Boost;
        }

        public override int GetHashCode()
        {
            int h = match.GetHashCode();
            h ^= (h << 8) | ((int)((uint)h >> 25)); // reversible
            //TODO: is this right?
            h ^= m_payloadToMatch.GetHashCode();
            h ^= Number.FloatToIntBits(Boost); // LUCENENET TODO: This was FloatToRawIntBits in the original
            return h;
        }
    }
}