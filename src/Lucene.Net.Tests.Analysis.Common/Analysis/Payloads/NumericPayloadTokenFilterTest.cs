// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using System.IO;

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

    public class NumericPayloadTokenFilterTest : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void Test()
        {
            string test = "The quick red fox jumped over the lazy brown dogs";

            NumericPayloadTokenFilter nptf = new NumericPayloadTokenFilter(new WordTokenFilter(this, new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false)), 3, "D");
            bool seenDogs = false;
            ICharTermAttribute termAtt = nptf.GetAttribute<ICharTermAttribute>();
            ITypeAttribute typeAtt = nptf.GetAttribute<ITypeAttribute>();
            IPayloadAttribute payloadAtt = nptf.GetAttribute<IPayloadAttribute>();
            nptf.Reset();
            while (nptf.IncrementToken())
            {
                if (termAtt.ToString().Equals("dogs", StringComparison.Ordinal))
                {
                    seenDogs = true;
                    assertTrue(typeAtt.Type + " is not equal to " + "D", typeAtt.Type.Equals("D", StringComparison.Ordinal) == true);
                    assertTrue("payloadAtt.getPayload() is null and it shouldn't be", payloadAtt.Payload != null);
                    byte[] bytes = payloadAtt.Payload.Bytes; //safe here to just use the bytes, otherwise we should use offset, length
                    assertTrue(bytes.Length + " does not equal: " + payloadAtt.Payload.Length, bytes.Length == payloadAtt.Payload.Length);
                    assertTrue(payloadAtt.Payload.Offset + " does not equal: " + 0, payloadAtt.Payload.Offset == 0);
                    float pay = PayloadHelper.DecodeSingle(bytes);
                    assertTrue(pay + " does not equal: " + 3, pay == 3);
                }
                else
                {
                    assertTrue(typeAtt.Type + " is not null and it should be", typeAtt.Type.Equals("word", StringComparison.Ordinal));
                }
            }
            assertTrue(seenDogs + " does not equal: " + true, seenDogs == true);
        }

        private sealed class WordTokenFilter : TokenFilter
        {
            private readonly NumericPayloadTokenFilterTest outerInstance;

            internal readonly ICharTermAttribute termAtt;
            internal readonly ITypeAttribute typeAtt;

            internal WordTokenFilter(NumericPayloadTokenFilterTest outerInstance, TokenStream input) : base(input)
            {
                this.outerInstance = outerInstance;
                termAtt = AddAttribute<ICharTermAttribute>();
                typeAtt = AddAttribute<ITypeAttribute>();
            }

            public override bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
                    if (termAtt.ToString().Equals("dogs", StringComparison.Ordinal))
                    {
                        typeAtt.Type = "D";
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
}