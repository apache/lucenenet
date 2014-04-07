using System;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis
{
    /**
     * TokenFilter that adds random fixed-length payloads.
     */
    public class MockFixedLengthPayloadFilter : TokenFilter
    {
        private readonly PayloadAttribute payloadAtt;
        private Random random;
        private sbyte[] bytes;
        private BytesRef payload;

        public MockFixedLengthPayloadFilter(Random random, TokenStream ts, int length)
            : base(ts)
        {
            if (length < 0)
            {
                throw new ArgumentException("length must be >= 0");
            }
            this.random = random;
            this.bytes = new sbyte[length];
            this.payload = new BytesRef(bytes);

            payloadAtt = AddAttribute<PayloadAttribute>();
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                byte[] b = new byte[bytes.Length];
                random.NextBytes(b);
                Buffer.BlockCopy(b, 0, bytes, 0, b.Length);
                payloadAtt.Payload = payload;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
