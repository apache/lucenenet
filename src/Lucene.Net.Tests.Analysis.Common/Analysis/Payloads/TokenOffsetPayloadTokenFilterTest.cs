// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using NUnit.Framework;
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

    public class TokenOffsetPayloadTokenFilterTest : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void Test()
        {
            string test = "The quick red fox jumped over the lazy brown dogs";

            TokenOffsetPayloadTokenFilter nptf = new TokenOffsetPayloadTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false));
            int count = 0;
            IPayloadAttribute payloadAtt = nptf.GetAttribute<IPayloadAttribute>();
            IOffsetAttribute offsetAtt = nptf.GetAttribute<IOffsetAttribute>();
            nptf.Reset();
            while (nptf.IncrementToken())
            {
                BytesRef pay = payloadAtt.Payload;
                assertTrue("pay is null and it shouldn't be", pay != null);
                byte[] data = pay.Bytes;
                int start = PayloadHelper.DecodeInt32(data, 0);
                assertTrue(start + " does not equal: " + offsetAtt.StartOffset, start == offsetAtt.StartOffset);
                int end = PayloadHelper.DecodeInt32(data, 4);
                assertTrue(end + " does not equal: " + offsetAtt.EndOffset, end == offsetAtt.EndOffset);
                count++;
            }
            assertTrue(count + " does not equal: " + 10, count == 10);
        }
    }
}