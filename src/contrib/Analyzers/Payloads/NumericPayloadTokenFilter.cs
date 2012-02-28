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
    /// Assigns a payload to a token based on the <see cref="Token.Type()"/>
    /// </summary>
    public class NumericPayloadTokenFilter : TokenFilter
    {
        private String typeMatch;
        private Payload thePayload;

        private PayloadAttribute payloadAtt;
        private TypeAttribute typeAtt;

        public NumericPayloadTokenFilter(TokenStream input, float payload, String typeMatch)
            : base(input)
        {
            //Need to encode the payload
            thePayload = new Payload(PayloadHelper.EncodeFloat(payload));
            this.typeMatch = typeMatch;
            payloadAtt = AddAttribute<PayloadAttribute>();
            typeAtt = AddAttribute<TypeAttribute>();
        }

        public sealed override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                if (typeAtt.Type().Equals(typeMatch))
                    payloadAtt.SetPayload(thePayload);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}