using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
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
    /// Checks to see if the <see cref="SpanPositionCheckQuery.Match"/> lies between a start and end position
    /// </summary>
    /// <seealso cref="Lucene.Net.Search.Spans.SpanFirstQuery">for a derivation that is optimized for the case where start position is 0</seealso>
    public class SpanPositionRangeQuery : SpanPositionCheckQuery
    {
        protected int m_start = 0;
        protected int m_end;

        public SpanPositionRangeQuery(SpanQuery match, int start, int end)
            : base(match)
        {
            this.m_start = start;
            this.m_end = end;
        }

        protected override AcceptStatus AcceptPosition(Spans spans)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(spans.Start != spans.End);
            if (spans.Start >= m_end)
            {
                return AcceptStatus.NO_AND_ADVANCE;
            }
            else if (spans.Start >= m_start && spans.End <= m_end)
            {
                return AcceptStatus.YES;
            }
            else
            {
                return AcceptStatus.NO;
            }
        }

        /// <returns> The minimum position permitted in a match </returns>
        public virtual int Start => m_start;

        /// <returns> The maximum end position permitted in a match. </returns>
        public virtual int End => m_end;

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("spanPosRange(");
            buffer.Append(m_match.ToString(field));
            buffer.Append(", ").Append(m_start).Append(", ");
            buffer.Append(m_end);
            buffer.Append(')');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override object Clone()
        {
            SpanPositionRangeQuery result = new SpanPositionRangeQuery((SpanQuery)m_match.Clone(), m_start, m_end);
            result.Boost = Boost;
            return result;
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is SpanPositionRangeQuery))
            {
                return false;
            }

            SpanPositionRangeQuery other = (SpanPositionRangeQuery)o;
            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            return this.m_end == other.m_end
                && this.m_start == other.m_start
                && this.m_match.Equals(other.m_match) && NumericUtils.SingleToSortableInt32(this.Boost) == NumericUtils.SingleToSortableInt32(other.Boost);
        }

        public override int GetHashCode()
        {
            int h = m_match.GetHashCode();
            h ^= (h << 8) | (h.TripleShift(25)); // reversible
            h ^= J2N.BitConversion.SingleToRawInt32Bits(Boost) ^ m_end ^ m_start;
            return h;
        }
    }
}