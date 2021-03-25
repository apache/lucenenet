// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;

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
    /// Makes the <see cref="Token.Type"/> a payload.
    /// <para/>
    /// Encodes the type using System.Text.Encoding.UTF8.GetBytes(string)
    /// </summary>
    public class TypeAsPayloadTokenFilter : TokenFilter
    {
        private readonly IPayloadAttribute payloadAtt;
        private readonly ITypeAttribute typeAtt;

        public TypeAsPayloadTokenFilter(TokenStream input)
            : base(input)
        {
            payloadAtt = AddAttribute<IPayloadAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
        }

        public override sealed bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                string type = typeAtt.Type;
                if (type != null && type.Length > 0)
                {
                    payloadAtt.Payload = new BytesRef(type);
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