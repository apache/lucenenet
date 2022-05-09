// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System.IO;
using System.Text;

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

    public class DelimitedPayloadTokenFilterTest : LuceneTestCase
    {

        [Test]
        public virtual void TestPayloads()
        {
            string test = "The quick|JJ red|JJ fox|NN jumped|VB over the lazy|JJ brown|JJ dogs|NN";
            DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false), DelimitedPayloadTokenFilter.DEFAULT_DELIMITER, new IdentityEncoder());
            ICharTermAttribute termAtt = filter.GetAttribute<ICharTermAttribute>();
            IPayloadAttribute payAtt = filter.GetAttribute<IPayloadAttribute>();
            filter.Reset();
            AssertTermEquals("The", filter, termAtt, payAtt, null);
            AssertTermEquals("quick", filter, termAtt, payAtt, "JJ".getBytes(Encoding.UTF8));
            AssertTermEquals("red", filter, termAtt, payAtt, "JJ".getBytes(Encoding.UTF8));
            AssertTermEquals("fox", filter, termAtt, payAtt, "NN".getBytes(Encoding.UTF8));
            AssertTermEquals("jumped", filter, termAtt, payAtt, "VB".getBytes(Encoding.UTF8));
            AssertTermEquals("over", filter, termAtt, payAtt, null);
            AssertTermEquals("the", filter, termAtt, payAtt, null);
            AssertTermEquals("lazy", filter, termAtt, payAtt, "JJ".getBytes(Encoding.UTF8));
            AssertTermEquals("brown", filter, termAtt, payAtt, "JJ".getBytes(Encoding.UTF8));
            AssertTermEquals("dogs", filter, termAtt, payAtt, "NN".getBytes(Encoding.UTF8));
            assertFalse(filter.IncrementToken());
            filter.End();
            filter.Dispose();
        }

        [Test]
        public virtual void TestNext()
        {

            string test = "The quick|JJ red|JJ fox|NN jumped|VB over the lazy|JJ brown|JJ dogs|NN";
            DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false), DelimitedPayloadTokenFilter.DEFAULT_DELIMITER, new IdentityEncoder());
            filter.Reset();
            AssertTermEquals("The", filter, null);
            AssertTermEquals("quick", filter, "JJ".getBytes(Encoding.UTF8));
            AssertTermEquals("red", filter, "JJ".getBytes(Encoding.UTF8));
            AssertTermEquals("fox", filter, "NN".getBytes(Encoding.UTF8));
            AssertTermEquals("jumped", filter, "VB".getBytes(Encoding.UTF8));
            AssertTermEquals("over", filter, null);
            AssertTermEquals("the", filter, null);
            AssertTermEquals("lazy", filter, "JJ".getBytes(Encoding.UTF8));
            AssertTermEquals("brown", filter, "JJ".getBytes(Encoding.UTF8));
            AssertTermEquals("dogs", filter, "NN".getBytes(Encoding.UTF8));
            assertFalse(filter.IncrementToken());
            filter.End();
            filter.Dispose();
        }


        [Test]
        public virtual void TestFloatEncoding()
        {
            string test = "The quick|1.0 red|2.0 fox|3.5 jumped|0.5 over the lazy|5 brown|99.3 dogs|83.7";
            DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false), '|', new SingleEncoder());
            ICharTermAttribute termAtt = filter.GetAttribute<ICharTermAttribute>();
            IPayloadAttribute payAtt = filter.GetAttribute<IPayloadAttribute>();
            filter.Reset();
            AssertTermEquals("The", filter, termAtt, payAtt, null);
            AssertTermEquals("quick", filter, termAtt, payAtt, PayloadHelper.EncodeSingle(1.0f));
            AssertTermEquals("red", filter, termAtt, payAtt, PayloadHelper.EncodeSingle(2.0f));
            AssertTermEquals("fox", filter, termAtt, payAtt, PayloadHelper.EncodeSingle(3.5f));
            AssertTermEquals("jumped", filter, termAtt, payAtt, PayloadHelper.EncodeSingle(0.5f));
            AssertTermEquals("over", filter, termAtt, payAtt, null);
            AssertTermEquals("the", filter, termAtt, payAtt, null);
            AssertTermEquals("lazy", filter, termAtt, payAtt, PayloadHelper.EncodeSingle(5.0f));
            AssertTermEquals("brown", filter, termAtt, payAtt, PayloadHelper.EncodeSingle(99.3f));
            AssertTermEquals("dogs", filter, termAtt, payAtt, PayloadHelper.EncodeSingle(83.7f));
            assertFalse(filter.IncrementToken());
            filter.End();
            filter.Dispose();
        }

        [Test]
        public virtual void TestIntEncoding()
        {
            string test = "The quick|1 red|2 fox|3 jumped over the lazy|5 brown|99 dogs|83";
            DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false), '|', new IntegerEncoder());
            ICharTermAttribute termAtt = filter.GetAttribute<ICharTermAttribute>();
            IPayloadAttribute payAtt = filter.GetAttribute<IPayloadAttribute>();
            filter.Reset();
            AssertTermEquals("The", filter, termAtt, payAtt, null);
            AssertTermEquals("quick", filter, termAtt, payAtt, PayloadHelper.EncodeInt32(1));
            AssertTermEquals("red", filter, termAtt, payAtt, PayloadHelper.EncodeInt32(2));
            AssertTermEquals("fox", filter, termAtt, payAtt, PayloadHelper.EncodeInt32(3));
            AssertTermEquals("jumped", filter, termAtt, payAtt, null);
            AssertTermEquals("over", filter, termAtt, payAtt, null);
            AssertTermEquals("the", filter, termAtt, payAtt, null);
            AssertTermEquals("lazy", filter, termAtt, payAtt, PayloadHelper.EncodeInt32(5));
            AssertTermEquals("brown", filter, termAtt, payAtt, PayloadHelper.EncodeInt32(99));
            AssertTermEquals("dogs", filter, termAtt, payAtt, PayloadHelper.EncodeInt32(83));
            assertFalse(filter.IncrementToken());
            filter.End();
            filter.Dispose();
        }

        internal virtual void AssertTermEquals(string expected, TokenStream stream, byte[] expectPay)
        {
            ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();
            IPayloadAttribute payloadAtt = stream.GetAttribute<IPayloadAttribute>();
            assertTrue(stream.IncrementToken());
            assertEquals(expected, termAtt.ToString());
            BytesRef payload = payloadAtt.Payload;
            if (payload != null)
            {
                assertTrue(payload.Length + " does not equal: " + expectPay.Length, payload.Length == expectPay.Length);
                for (int i = 0; i < expectPay.Length; i++)
                {
                    assertTrue(expectPay[i] + " does not equal: " + payload.Bytes[i + payload.Offset], expectPay[i] == payload.Bytes[i + payload.Offset]);

                }
            }
            else
            {
                assertTrue("expectPay is not null and it should be", expectPay is null);
            }
        }


        internal virtual void AssertTermEquals(string expected, TokenStream stream, ICharTermAttribute termAtt, IPayloadAttribute payAtt, byte[] expectPay)
        {
            assertTrue(stream.IncrementToken());
            assertEquals(expected, termAtt.ToString());
            BytesRef payload = payAtt.Payload;
            if (payload != null)
            {
                assertTrue(payload.Length + " does not equal: " + expectPay.Length, payload.Length == expectPay.Length);
                for (int i = 0; i < expectPay.Length; i++)
                {
                    assertTrue(expectPay[i] + " does not equal: " + payload.Bytes[i + payload.Offset], expectPay[i] == payload.Bytes[i + payload.Offset]);

                }
            }
            else
            {
                assertTrue("expectPay is not null and it should be", expectPay is null);
            }
        }
    }
}