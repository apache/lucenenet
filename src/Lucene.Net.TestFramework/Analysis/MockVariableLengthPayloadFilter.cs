using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Analysis
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
    /// <see cref="TokenFilter"/> that adds random variable-length payloads.
    /// </summary>
    public sealed class MockVariableLengthPayloadFilter : TokenFilter
    {
        private const int MAXLENGTH = 129;

        private readonly IPayloadAttribute payloadAtt;
        private readonly Random random;
        private readonly byte[] bytes = new byte[MAXLENGTH];
        private readonly BytesRef payload;

        public MockVariableLengthPayloadFilter(Random random, TokenStream @in)
            : base(@in)
        {
            this.random = random;
            this.payload = new BytesRef(bytes);
            this.payloadAtt = AddAttribute<IPayloadAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                random.NextBytes(bytes);
                payload.Length = random.Next(MAXLENGTH);
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