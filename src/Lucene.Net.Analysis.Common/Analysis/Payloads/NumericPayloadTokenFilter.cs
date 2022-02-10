// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Analysis.Payloads
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

    /// <summary>
    /// Assigns a payload to a token based on the <see cref="Token.Type"/>
    /// </summary>
    public class NumericPayloadTokenFilter : TokenFilter
    {
        private string typeMatch;
        private BytesRef thePayload;

        private readonly IPayloadAttribute payloadAtt;
        private readonly ITypeAttribute typeAtt;

        public NumericPayloadTokenFilter(TokenStream input, float payload, string typeMatch) 
            : base(input)
        {
            if (typeMatch is null)
            {
                throw new ArgumentNullException(nameof(typeMatch), "typeMatch cannot be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            //Need to encode the payload
            thePayload = new BytesRef(PayloadHelper.EncodeSingle(payload));
            this.typeMatch = typeMatch;
            this.payloadAtt = AddAttribute<IPayloadAttribute>();
            this.typeAtt = AddAttribute<ITypeAttribute>();
        }

        public override sealed bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                if (typeAtt.Type.Equals(typeMatch, StringComparison.Ordinal))
                {
                    payloadAtt.Payload = thePayload;
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