namespace org.apache.lucene.analysis.payloads
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


	using PayloadAttribute = org.apache.lucene.analysis.tokenattributes.PayloadAttribute;
	using BaseTokenStreamFactoryTestCase = org.apache.lucene.analysis.util.BaseTokenStreamFactoryTestCase;

	public class TestDelimitedPayloadTokenFilterFactory : BaseTokenStreamFactoryTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEncoder() throws Exception
	  public virtual void testEncoder()
	  {
		Reader reader = new StringReader("the|0.1 quick|0.1 red|0.1");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("DelimitedPayload", "encoder", "float").create(stream);

		stream.reset();
		while (stream.incrementToken())
		{
		  PayloadAttribute payAttr = stream.getAttribute(typeof(PayloadAttribute));
		  assertNotNull(payAttr);
		  sbyte[] payData = payAttr.Payload.bytes;
		  assertNotNull(payData);
		  float payFloat = PayloadHelper.decodeFloat(payData);
		  assertEquals(0.1f, payFloat, 0.0f);
		}
		stream.end();
		stream.close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDelim() throws Exception
	  public virtual void testDelim()
	  {
		Reader reader = new StringReader("the*0.1 quick*0.1 red*0.1");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("DelimitedPayload", "encoder", "float", "delimiter", "*").create(stream);
		stream.reset();
		while (stream.incrementToken())
		{
		  PayloadAttribute payAttr = stream.getAttribute(typeof(PayloadAttribute));
		  assertNotNull(payAttr);
		  sbyte[] payData = payAttr.Payload.bytes;
		  assertNotNull(payData);
		  float payFloat = PayloadHelper.decodeFloat(payData);
		  assertEquals(0.1f, payFloat, 0.0f);
		}
		stream.end();
		stream.close();
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("DelimitedPayload", "encoder", "float", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}


}