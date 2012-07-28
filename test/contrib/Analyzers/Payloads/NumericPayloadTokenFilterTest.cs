/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Payloads;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Payloads
{
    [TestFixture]
    public class NumericPayloadTokenFilterTest : BaseTokenStreamTestCase
    {
        [Test]
        public void Test()
        {
            String test = "The quick red fox jumped over the lazy brown dogs";

            NumericPayloadTokenFilter nptf = new NumericPayloadTokenFilter(new WordTokenFilter(new WhitespaceTokenizer(new StringReader(test))), 3, "D");
            bool seenDogs = false;
            ITermAttribute termAtt = nptf.GetAttribute<ITermAttribute>();
            ITypeAttribute typeAtt = nptf.GetAttribute<ITypeAttribute>();
            IPayloadAttribute payloadAtt = nptf.GetAttribute<IPayloadAttribute>();
            while (nptf.IncrementToken())
            {
                if (termAtt.Term.Equals("dogs"))
                {
                    seenDogs = true;
                    Assert.True(typeAtt.Type.Equals("D") == true, typeAtt.Type + " is not equal to " + "D");
                    Assert.True(payloadAtt.Payload != null, "payloadAtt.GetPayload() is null and it shouldn't be");
                    byte[] bytes = payloadAtt.Payload.GetData();//safe here to just use the bytes, otherwise we should use offset, length
                    Assert.True(bytes.Length == payloadAtt.Payload.Length, bytes.Length + " does not equal: " + payloadAtt.Payload.Length);
                    Assert.True(payloadAtt.Payload.Offset == 0, payloadAtt.Payload.Offset + " does not equal: " + 0);
                    float pay = PayloadHelper.DecodeFloat(bytes);
                    Assert.True(pay == 3, pay + " does not equal: " + 3);
                }
                else
                {
                    Assert.True(typeAtt.Type.Equals("word"), typeAtt.Type + " is not null and it should be");
                }
            }
            Assert.True(seenDogs == true, seenDogs + " does not equal: " + true);
        }

        internal sealed class WordTokenFilter : TokenFilter
        {
            private ITermAttribute termAtt;
            private ITypeAttribute typeAtt;

            internal WordTokenFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<ITermAttribute>();
                typeAtt = AddAttribute<ITypeAttribute>();
            }

            public override bool IncrementToken()
            {
                if (input.IncrementToken())
                {
                    if (termAtt.Term.Equals("dogs"))
                        typeAtt.Type = "D";
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
