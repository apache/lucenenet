// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using System.Globalization;
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


    public class TypeAsPayloadTokenFilterTest : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void Test()
        {
            string test = "The quick red fox jumped over the lazy brown dogs";

            TypeAsPayloadTokenFilter nptf = new TypeAsPayloadTokenFilter(new WordTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false)));
            int count = 0;
            ICharTermAttribute termAtt = nptf.GetAttribute<ICharTermAttribute>();
            ITypeAttribute typeAtt = nptf.GetAttribute<ITypeAttribute>();
            IPayloadAttribute payloadAtt = nptf.GetAttribute<IPayloadAttribute>();
            nptf.Reset();
            while (nptf.IncrementToken())
            {
                assertTrue(typeAtt.Type + " is not null and it should be", typeAtt.Type.Equals(char.ToUpper(termAtt.Buffer[0]).ToString(), StringComparison.Ordinal)); // LUCENENET specific - intentionally using current culture
                assertTrue("nextToken.getPayload() is null and it shouldn't be", payloadAtt.Payload != null);
                string type = payloadAtt.Payload.Utf8ToString();
                assertTrue(type + " is not equal to " + typeAtt.Type, type.Equals(typeAtt.Type, StringComparison.Ordinal));
                count++;
            }

            assertTrue(count + " does not equal: " + 10, count == 10);
        }

        private sealed class WordTokenFilter : TokenFilter
        {
            internal readonly ICharTermAttribute termAtt;
            internal readonly ITypeAttribute typeAtt;

            internal WordTokenFilter(TokenStream input) : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                typeAtt = AddAttribute<ITypeAttribute>();
            }

            public override bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
                    typeAtt.Type = char.ToUpper(termAtt.Buffer[0]).ToString(); // LUCENENET specific - intentionally using current culture
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