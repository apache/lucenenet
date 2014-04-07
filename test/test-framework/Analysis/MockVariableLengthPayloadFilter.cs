using System;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis
{
    /**
 * TokenFilter that adds random variable-length payloads.
 */
    public class MockVariableLengthPayloadFilter : TokenFilter
    {
        private static int MAXLENGTH = 129;

        private readonly PayloadAttribute payloadAtt;
        private Random random;
        private sbyte[] bytes = new sbyte[MAXLENGTH];
        private BytesRef payload;

        public MockVariableLengthPayloadFilter(Random random, TokenStream ts)
            : base(ts)
        {
            this.random = random;
            this.payload = new BytesRef(bytes);
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                byte[] b = new byte[MAXLENGTH];
                random.NextBytes(b);
                Buffer.BlockCopy(b, 0, bytes, 0, b.Length);
                payload.length = random.Next(MAXLENGTH);
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
