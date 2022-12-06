﻿// Lucene version compatibility level 4.8.1
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
    /// Adds the <see cref="IOffsetAttribute.StartOffset"/>
    /// and <see cref="IOffsetAttribute.EndOffset"/>
    /// First 4 bytes are the start
    /// </summary>
    public class TokenOffsetPayloadTokenFilter : TokenFilter
    {
        private readonly IOffsetAttribute offsetAtt;
        private readonly IPayloadAttribute payAtt;

        public TokenOffsetPayloadTokenFilter(TokenStream input)
            : base(input)
        {
            offsetAtt = AddAttribute<IOffsetAttribute>();
            payAtt = AddAttribute<IPayloadAttribute>();
        }

        public override sealed bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                byte[] data = new byte[8];
                PayloadHelper.EncodeInt32(offsetAtt.StartOffset, data, 0);
                PayloadHelper.EncodeInt32(offsetAtt.EndOffset, data, 4);
                BytesRef payload = new BytesRef(data);
                payAtt.Payload = payload;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}