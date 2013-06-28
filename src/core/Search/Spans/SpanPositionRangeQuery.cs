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
        public int Start { get; protected set; }
        public int End { get; protected set; }

        public SpanPositionRangeQuery(SpanQuery match, int start, int end)
            : base(match)
        {
            Start = start;
            End = end;
        }

        protected override AcceptStatus AcceptPosition(Spans spans)
        {
            // assert spans.start() != spans.end();
            if (spans.Start() >= End)
            {
                return AcceptStatus.NO_AND_ADVANCE;
            }
            else if (spans.Start() >= Start && spans.End() <= End)
            {
                return AcceptStatus.YES;
            }
            else
            {
                return AcceptStatus.NO;
            }
        }

        public override string ToString(string field)
        {
            var buffer = new StringBuilder();
            buffer.Append("spanPosRange(");
            buffer.Append(Match.ToString(field));
            buffer.Append(", ").Append(Start).Append(", ");
            buffer.Append(End);
            buffer.Append(")");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override object Clone()
        {
            return new SpanPositionRangeQuery((SpanQuery) Match.Clone(), Start, End) {Boost = Boost};
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (!(obj is SpanPositionRangeQuery)) return false;

            var other = obj as SpanPositionRangeQuery;
            return this.End == other.End && this.Start == other.Start
                   && this.Match.Equals(other.Match)
                   && this.Boost == other.Boost;
        }

        public override int GetHashCode()
        {
            int h = Match.GetHashCode();
            h ^= (h << 8) | Number.URShift(h, 25);
            h ^= Number.FloatToIntBits(Boost) ^ End ^ Start;
            return h;
        }
    }
}
