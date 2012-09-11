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
    public class TypeAsPayloadTokenFilterTest : BaseTokenStreamTestCase
    {
        [Test]
        public void test()
        {
            String test = "The quick red fox jumped over the lazy brown dogs";

            TypeAsPayloadTokenFilter nptf = new TypeAsPayloadTokenFilter(new WordTokenFilter(new WhitespaceTokenizer(new StringReader(test))));
            int count = 0;
            ITermAttribute termAtt = nptf.GetAttribute<ITermAttribute>();
            ITypeAttribute typeAtt = nptf.GetAttribute<ITypeAttribute>();
            IPayloadAttribute payloadAtt = nptf.GetAttribute<IPayloadAttribute>();

            while (nptf.IncrementToken())
            {
                Assert.True(typeAtt.Type.Equals(char.ToUpper(termAtt.TermBuffer()[0]).ToString()), typeAtt.Type + " is not null and it should be");
                Assert.True(payloadAtt.Payload != null, "nextToken.getPayload() is null and it shouldn't be");
                String type = Encoding.UTF8.GetString(payloadAtt.Payload.GetData()); ;
                Assert.True(type != null, "type is null and it shouldn't be");
                Assert.True(type.Equals(typeAtt.Type) == true, type + " is not equal to " + typeAtt.Type);
                count++;
            }

            Assert.True(count == 10, count + " does not equal: " + 10);
        }

        private sealed class WordTokenFilter : TokenFilter
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
                    typeAtt.Type = char.ToUpper(termAtt.TermBuffer()[0]).ToString();
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
