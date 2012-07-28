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
using Lucene.Net.Index;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Payloads
{
    [TestFixture]
    public class TokenOffsetPayloadTokenFilterTest : BaseTokenStreamTestCase
    {
        [Test]
        public void Test()
        {
            String test = "The quick red fox jumped over the lazy brown dogs";

            TokenOffsetPayloadTokenFilter nptf = new TokenOffsetPayloadTokenFilter(new WhitespaceTokenizer(new StringReader(test)));
            int count = 0;
            IPayloadAttribute payloadAtt = nptf.GetAttribute<IPayloadAttribute>();
            IOffsetAttribute offsetAtt = nptf.GetAttribute<IOffsetAttribute>();

            while (nptf.IncrementToken())
            {
                Payload pay = payloadAtt.Payload;
                Assert.True(pay != null, "pay is null and it shouldn't be");
                byte[] data = pay.GetData();
                int start = PayloadHelper.DecodeInt(data, 0);
                Assert.True(start == offsetAtt.StartOffset, start + " does not equal: " + offsetAtt.StartOffset);
                int end = PayloadHelper.DecodeInt(data, 4);
                Assert.True(end == offsetAtt.EndOffset, end + " does not equal: " + offsetAtt.EndOffset);
                count++;
            }
            Assert.True(count == 10, count + " does not equal: " + 10);
        }
    }
}
