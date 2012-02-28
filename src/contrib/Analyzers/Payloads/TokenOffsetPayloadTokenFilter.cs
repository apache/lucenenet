using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;

namespace Lucene.Net.Analyzers.Payloads
{
    /// <summary>
    /// Adds the <see cref="Token.SetStartOffset(int)"/>
    /// and <see cref="Token.SetEndOffset(int)"/>
    /// First 4 bytes are the start
    /// </summary>
    public class TokenOffsetPayloadTokenFilter : TokenFilter
    {
        protected OffsetAttribute offsetAtt;
        protected PayloadAttribute payAtt;

        public TokenOffsetPayloadTokenFilter(TokenStream input)
            : base(input)
        {
            offsetAtt = AddAttribute<OffsetAttribute>();
            payAtt = AddAttribute<PayloadAttribute>();
        }

        public sealed override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                byte[] data = new byte[8];
                PayloadHelper.EncodeInt(offsetAtt.StartOffset(), data, 0);
                PayloadHelper.EncodeInt(offsetAtt.EndOffset(), data, 4);
                Payload payload = new Payload(data);
                payAtt.SetPayload(payload);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
