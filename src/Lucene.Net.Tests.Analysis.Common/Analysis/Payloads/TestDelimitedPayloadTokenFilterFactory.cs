// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
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

    public class TestDelimitedPayloadTokenFilterFactory : BaseTokenStreamFactoryTestCase
    {

        [Test]
        public virtual void TestEncoder()
        {
            TextReader reader = new StringReader("the|0.1 quick|0.1 red|0.1");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("DelimitedPayload", "encoder", "float").Create(stream);

            stream.Reset();
            while (stream.IncrementToken())
            {
                IPayloadAttribute payAttr = stream.GetAttribute<IPayloadAttribute>();
                assertNotNull(payAttr);
                byte[] payData = payAttr.Payload.Bytes;
                assertNotNull(payData);
                float payFloat = PayloadHelper.DecodeSingle(payData);
                assertEquals(0.1f, payFloat, 0.0f);
            }
            stream.End();
            stream.Dispose();
        }

        [Test]
        public virtual void TestDelim()
        {
            TextReader reader = new StringReader("the*0.1 quick*0.1 red*0.1");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("DelimitedPayload", "encoder", "float", "delimiter", "*").Create(stream);
            stream.Reset();
            while (stream.IncrementToken())
            {
                IPayloadAttribute payAttr = stream.GetAttribute<IPayloadAttribute>();
                assertNotNull(payAttr);
                byte[] payData = payAttr.Payload.Bytes;
                assertNotNull(payData);
                float payFloat = PayloadHelper.DecodeSingle(payData);
                assertEquals(0.1f, payFloat, 0.0f);
            }
            stream.End();
            stream.Dispose();
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("DelimitedPayload", "encoder", "float", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}