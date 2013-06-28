using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Spans
{
    public class SpanNearPayloadCheckQuery : SpanPositionCheckQuery
    {
        protected readonly ICollection<byte[]> payloadToMatch;

        public SpanNearPayloadCheckQuery(SpanNearQuery match, ICollection<byte[]> payloadToMatch)
            : base(match)
        {
            this.payloadToMatch = payloadToMatch;
        }

        protected override AcceptStatus AcceptPosition(Spans spans)
        {
            var result = spans.IsPayloadAvailable();
            if (result == true)
            {
                var candidate = spans.GetPayload();
                if (candidate.Count == payloadToMatch.Count)
                {
                    //TODO: check the byte arrays are the same
                    //hmm, can't rely on order here
                    int matches = candidate.Count(candBytes => payloadToMatch.Any(payBytes => Arrays.Equals(candBytes, payBytes) == true));

                    if (matches == payloadToMatch.Count)
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
            var buffer = new StringBuilder();
            buffer.Append("spanPayCheck(");
            buffer.Append(match.toString(field));
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

        public override SpanNearPayloadCheckQuery Clone()
        {
            var result = new SpanNearPayloadCheckQuery((SpanNearQuery) match.clone(), payloadToMatch);
            result.Boost = Boost;
            return result;
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (!(o is SpanNearPayloadCheckQuery)) return false;

            var other = (SpanNearPayloadCheckQuery) o;
            return this.payloadToMatch.Equals(other.payloadToMatch)
                   && this.match.equals(other.match)
                   && this.Boost == other.Boost;
        }

        public override int GetHashCode()
        {
            int h = match.hashCode();
            h ^= (h << 8) | Support.Number.URShift(h, 25); // reversible
            //TODO: is this right?
            h ^= payloadToMatch.GetHashCode();
            h ^= Support.Number.FloatToIntBits(Boost);
            return h;
        }
    }
}