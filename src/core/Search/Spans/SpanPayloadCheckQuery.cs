using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Spans
{
    public class SpanPayloadCheckQuery : SpanPositionCheckQuery
    {
        protected readonly ICollection<sbyte[]> payloadToMatch;

        public SpanPayloadCheckQuery(SpanQuery Match, ICollection<sbyte[]> payloadToMatch)
            : base(Match)
        {
            if (Match is SpanNearQuery) throw new ArgumentException("SpanNearQuery not allowed");

            this.payloadToMatch = payloadToMatch;
        }

        protected override AcceptStatus AcceptPosition(SpansBase spans)
        {
            var result = spans.IsPayloadAvailable();
            if (result == true)
            {
                var candidate = spans.GetPayload();
                if (candidate.Count == payloadToMatch.Count)
                {
                    var toMatchEnumerator = payloadToMatch.GetEnumerator();
                    foreach (var candBytes in candidate)
                    {
                        toMatchEnumerator.MoveNext();
                        if (Arrays.Equals(candBytes, toMatchEnumerator.Current) == false)
                        {
                            return AcceptStatus.NO;
                        }
                    }
                    return AcceptStatus.YES;
                }
                else
                {
                    return AcceptStatus.NO;
                }
            }
            return AcceptStatus.YES;
        }

        public override string ToString(string field)
        {
            var buffer = new StringBuilder();
            buffer.Append("spanPayCheck(");
            buffer.Append(Match.ToString(field));
            buffer.Append(", payloadRef: ");
            foreach (var bytes in payloadToMatch)
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
            return new SpanPayloadCheckQuery((SpanQuery) Match.Clone(), payloadToMatch) { Boost = Boost };
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (!(obj is SpanPayloadCheckQuery)) return false;

            var other = obj as SpanPayloadCheckQuery;
            return this.payloadToMatch.Equals(other.payloadToMatch)
                   && this.Match.Equals(other.Match)
                   && this.Boost == other.Boost;
        }

        public override int GetHashCode()
        {
            int h = Match.GetHashCode();
            h ^= (h << 8) | Number.URShift(h, 25);
            h ^= payloadToMatch.GetHashCode();
            h ^= Number.FloatToIntBits(Boost);
            return h;
        }
    }
}
