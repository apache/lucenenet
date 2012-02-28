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
    /// Makes the Token.Type() a payload.
    /// Encodes the type using <see cref="System.Text.Encoding.UTF8"/> as the encoding
    /// </summary>
    public class TypeAsPayloadTokenFilter : TokenFilter
    {
        private PayloadAttribute payloadAtt;
        private TypeAttribute typeAtt;

        public TypeAsPayloadTokenFilter(TokenStream input)
            : base(input)
        {
            payloadAtt = AddAttribute<PayloadAttribute>();
            typeAtt = AddAttribute<TypeAttribute>();
        }

        public sealed override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                String type = typeAtt.Type();
                if (type != null && type.Equals("") == false)
                {
                    payloadAtt.SetPayload(new Payload(Encoding.UTF8.GetBytes(type)));
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
