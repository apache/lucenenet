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
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Payloads
{
    [TestFixture]
    public class DelimitedPayloadTokenFilterTest : LuceneTestCase
    {
        [Test]
        public void TestPayloads()
        {
            var encoding = Encoding.UTF8;
            String test = "The quick|JJ red|JJ fox|NN jumped|VB over the lazy|JJ brown|JJ dogs|NN";
            DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new WhitespaceTokenizer(new StringReader(test)));
            ITermAttribute termAtt = filter.GetAttribute<ITermAttribute>();
            IPayloadAttribute payAtt = filter.GetAttribute<IPayloadAttribute>();
            AssertTermEquals("The", filter, termAtt, payAtt, null);
            AssertTermEquals("quick", filter, termAtt, payAtt, encoding.GetBytes("JJ"));
            AssertTermEquals("red", filter, termAtt, payAtt, encoding.GetBytes("JJ"));
            AssertTermEquals("fox", filter, termAtt, payAtt, encoding.GetBytes("NN"));
            AssertTermEquals("jumped", filter, termAtt, payAtt, encoding.GetBytes("VB"));
            AssertTermEquals("over", filter, termAtt, payAtt, null);
            AssertTermEquals("the", filter, termAtt, payAtt, null);
            AssertTermEquals("lazy", filter, termAtt, payAtt, encoding.GetBytes("JJ"));
            AssertTermEquals("brown", filter, termAtt, payAtt, encoding.GetBytes("JJ"));
            AssertTermEquals("dogs", filter, termAtt, payAtt, encoding.GetBytes("NN"));
            Assert.False(filter.IncrementToken());
        }

        [Test]
        public void TestNext()
        {
            var encoding = Encoding.UTF8;
            String test = "The quick|JJ red|JJ fox|NN jumped|VB over the lazy|JJ brown|JJ dogs|NN";
            DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new WhitespaceTokenizer(new StringReader(test)));
            AssertTermEquals("The", filter, null);
            AssertTermEquals("quick", filter, encoding.GetBytes("JJ"));
            AssertTermEquals("red", filter, encoding.GetBytes("JJ"));
            AssertTermEquals("fox", filter, encoding.GetBytes("NN"));
            AssertTermEquals("jumped", filter, encoding.GetBytes("VB"));
            AssertTermEquals("over", filter, null);
            AssertTermEquals("the", filter, null);
            AssertTermEquals("lazy", filter, encoding.GetBytes("JJ"));
            AssertTermEquals("brown", filter, encoding.GetBytes("JJ"));
            AssertTermEquals("dogs", filter, encoding.GetBytes("NN"));
            Assert.False(filter.IncrementToken());
        }


        [Test]
        public void TestFloatEncoding()
        {
            String test = "The quick|1.0 red|2.0 fox|3.5 jumped|0.5 over the lazy|5 brown|99.3 dogs|83.7";
            DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new WhitespaceTokenizer(new StringReader(test)), '|', new FloatEncoder());
            ITermAttribute termAtt = filter.GetAttribute<ITermAttribute>();
            IPayloadAttribute payAtt = filter.GetAttribute<IPayloadAttribute>();
            AssertTermEquals("The", filter, termAtt, payAtt, null);
            AssertTermEquals("quick", filter, termAtt, payAtt, PayloadHelper.EncodeFloat(1.0f));
            AssertTermEquals("red", filter, termAtt, payAtt, PayloadHelper.EncodeFloat(2.0f));
            AssertTermEquals("fox", filter, termAtt, payAtt, PayloadHelper.EncodeFloat(3.5f));
            AssertTermEquals("jumped", filter, termAtt, payAtt, PayloadHelper.EncodeFloat(0.5f));
            AssertTermEquals("over", filter, termAtt, payAtt, null);
            AssertTermEquals("the", filter, termAtt, payAtt, null);
            AssertTermEquals("lazy", filter, termAtt, payAtt, PayloadHelper.EncodeFloat(5.0f));
            AssertTermEquals("brown", filter, termAtt, payAtt, PayloadHelper.EncodeFloat(99.3f));
            AssertTermEquals("dogs", filter, termAtt, payAtt, PayloadHelper.EncodeFloat(83.7f));
            Assert.False(filter.IncrementToken());
        }

        [Test]
        public void TestIntEncoding()
        {
            String test = "The quick|1 red|2 fox|3 jumped over the lazy|5 brown|99 dogs|83";
            DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new WhitespaceTokenizer(new StringReader(test)), '|', new IntegerEncoder());
            ITermAttribute termAtt = filter.GetAttribute<ITermAttribute>();
            IPayloadAttribute payAtt = filter.GetAttribute<IPayloadAttribute>();
            AssertTermEquals("The", filter, termAtt, payAtt, null);
            AssertTermEquals("quick", filter, termAtt, payAtt, PayloadHelper.EncodeInt(1));
            AssertTermEquals("red", filter, termAtt, payAtt, PayloadHelper.EncodeInt(2));
            AssertTermEquals("fox", filter, termAtt, payAtt, PayloadHelper.EncodeInt(3));
            AssertTermEquals("jumped", filter, termAtt, payAtt, null);
            AssertTermEquals("over", filter, termAtt, payAtt, null);
            AssertTermEquals("the", filter, termAtt, payAtt, null);
            AssertTermEquals("lazy", filter, termAtt, payAtt, PayloadHelper.EncodeInt(5));
            AssertTermEquals("brown", filter, termAtt, payAtt, PayloadHelper.EncodeInt(99));
            AssertTermEquals("dogs", filter, termAtt, payAtt, PayloadHelper.EncodeInt(83));
            Assert.False(filter.IncrementToken());
        }

        void AssertTermEquals(String expected, TokenStream stream, byte[] expectPay)
        {
            ITermAttribute termAtt = stream.GetAttribute<ITermAttribute>();
            IPayloadAttribute payloadAtt = stream.GetAttribute<IPayloadAttribute>();
            Assert.True(stream.IncrementToken());
            Assert.AreEqual(expected, termAtt.Term);
            Payload payload = payloadAtt.Payload;
            if (payload != null)
            {
                Assert.True(payload.Length == expectPay.Length, payload.Length + " does not equal: " + expectPay.Length);
                for (int i = 0; i < expectPay.Length; i++)
                {
                    Assert.True(expectPay[i] == payload.ByteAt(i), expectPay[i] + " does not equal: " + payload.ByteAt(i));

                }
            }
            else
            {
                Assert.True(expectPay == null, "expectPay is not null and it should be");
            }
        }

        void AssertTermEquals(String expected, TokenStream stream, ITermAttribute termAtt, IPayloadAttribute payAtt, byte[] expectPay)
        {
            Assert.True(stream.IncrementToken());
            Assert.AreEqual(expected, termAtt.Term);
            Payload payload = payAtt.Payload;
            if (payload != null)
            {
                Assert.True(payload.Length == expectPay.Length, payload.Length + " does not equal: " + expectPay.Length);
                for (int i = 0; i < expectPay.Length; i++)
                {
                    Assert.True(expectPay[i] == payload.ByteAt(i), expectPay[i] + " does not equal: " + payload.ByteAt(i));

                }
            }
            else
            {
                Assert.True(expectPay == null, "expectPay is not null and it should be");
            }
        }
    }
}
