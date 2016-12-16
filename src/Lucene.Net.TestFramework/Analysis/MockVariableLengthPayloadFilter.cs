using Lucene.Net.Analysis.TokenAttributes;
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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// TokenFilter that adds random variable-length payloads.
    /// </summary>
    public sealed class MockVariableLengthPayloadFilter : TokenFilter
    {
        private const int MAXLENGTH = 129;

        private readonly IPayloadAttribute PayloadAtt;
        private readonly Random Random;
        private readonly byte[] Bytes = new byte[MAXLENGTH];
        private readonly BytesRef Payload;

        public MockVariableLengthPayloadFilter(Random random, TokenStream @in)
            : base(@in)
        {
            this.Random = random;
            this.Payload = new BytesRef(Bytes);
            this.PayloadAtt = AddAttribute<IPayloadAttribute>();
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                Random.NextBytes((byte[])(Array)Bytes);
                Payload.Length = Random.Next(MAXLENGTH);
                PayloadAtt.Payload = Payload;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}