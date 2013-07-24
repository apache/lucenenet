using System.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Spans
{
    /// <summary>
    /// Checks to see if the GetMatch() lies between a start and end position
    /// </summary>
    public class SpanPositionRangeQuery : SpanPositionCheckQuery
    {
        protected int start = 0;
        protected int end;

        public SpanPositionRangeQuery(SpanQuery match, int start, int end)
            : base(match)
        {
            this.start = start;
            this.end = end;
        }

        protected override AcceptStatus AcceptPosition(SpansBase spans)
        {
            // assert spans.start() != spans.end();
            if (spans.Start >= end)
            {
                return AcceptStatus.NO_AND_ADVANCE;
            }
            else if (spans.Start >= start && spans.End <= end)
            {
                return AcceptStatus.YES;
            }
            else
            {
                return AcceptStatus.NO;
            }
        }

        public int Start
        {
            get { return start; }
        }

        public int End
        {
            get { return end; }
        }

        public override string ToString(string field)
        {
            var buffer = new StringBuilder();
            buffer.Append("spanPosRange(");
            buffer.Append(Match.ToString(field));
            buffer.Append(", ").Append(start).Append(", ");
            buffer.Append(end);
            buffer.Append(")");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override object Clone()
        {
            return new SpanPositionRangeQuery((SpanQuery) Match.Clone(), start, end) {Boost = Boost};
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (!(obj is SpanPositionRangeQuery)) return false;

            var other = obj as SpanPositionRangeQuery;
            return this.end == other.end && this.start == other.start
                   && this.Match.Equals(other.Match)
                   && this.Boost == other.Boost;
        }

        public override int GetHashCode()
        {
            int h = Match.GetHashCode();
            h ^= (h << 8) | Number.URShift(h, 25);
            h ^= Number.FloatToIntBits(Boost) ^ end ^ start;
            return h;
        }
    }
}
