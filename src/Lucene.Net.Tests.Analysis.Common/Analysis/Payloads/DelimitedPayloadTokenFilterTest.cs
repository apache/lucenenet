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

	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using PayloadAttribute = org.apache.lucene.analysis.tokenattributes.PayloadAttribute;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;


	public class DelimitedPayloadTokenFilterTest : LuceneTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPayloads() throws Exception
	  public virtual void testPayloads()
	  {
		string test = "The quick|JJ red|JJ fox|NN jumped|VB over the lazy|JJ brown|JJ dogs|NN";
		DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false), DelimitedPayloadTokenFilter.DEFAULT_DELIMITER, new IdentityEncoder());
		CharTermAttribute termAtt = filter.getAttribute(typeof(CharTermAttribute));
		PayloadAttribute payAtt = filter.getAttribute(typeof(PayloadAttribute));
		filter.reset();
		assertTermEquals("The", filter, termAtt, payAtt, null);
		assertTermEquals("quick", filter, termAtt, payAtt, "JJ".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("red", filter, termAtt, payAtt, "JJ".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("fox", filter, termAtt, payAtt, "NN".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("jumped", filter, termAtt, payAtt, "VB".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("over", filter, termAtt, payAtt, null);
		assertTermEquals("the", filter, termAtt, payAtt, null);
		assertTermEquals("lazy", filter, termAtt, payAtt, "JJ".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("brown", filter, termAtt, payAtt, "JJ".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("dogs", filter, termAtt, payAtt, "NN".GetBytes(StandardCharsets.UTF_8));
		assertFalse(filter.incrementToken());
		filter.end();
		filter.close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNext() throws Exception
	  public virtual void testNext()
	  {

		string test = "The quick|JJ red|JJ fox|NN jumped|VB over the lazy|JJ brown|JJ dogs|NN";
		DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false), DelimitedPayloadTokenFilter.DEFAULT_DELIMITER, new IdentityEncoder());
		filter.reset();
		assertTermEquals("The", filter, null);
		assertTermEquals("quick", filter, "JJ".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("red", filter, "JJ".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("fox", filter, "NN".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("jumped", filter, "VB".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("over", filter, null);
		assertTermEquals("the", filter, null);
		assertTermEquals("lazy", filter, "JJ".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("brown", filter, "JJ".GetBytes(StandardCharsets.UTF_8));
		assertTermEquals("dogs", filter, "NN".GetBytes(StandardCharsets.UTF_8));
		assertFalse(filter.incrementToken());
		filter.end();
		filter.close();
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFloatEncoding() throws Exception
	  public virtual void testFloatEncoding()
	  {
		string test = "The quick|1.0 red|2.0 fox|3.5 jumped|0.5 over the lazy|5 brown|99.3 dogs|83.7";
		DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false), '|', new FloatEncoder());
		CharTermAttribute termAtt = filter.getAttribute(typeof(CharTermAttribute));
		PayloadAttribute payAtt = filter.getAttribute(typeof(PayloadAttribute));
		filter.reset();
		assertTermEquals("The", filter, termAtt, payAtt, null);
		assertTermEquals("quick", filter, termAtt, payAtt, PayloadHelper.encodeFloat(1.0f));
		assertTermEquals("red", filter, termAtt, payAtt, PayloadHelper.encodeFloat(2.0f));
		assertTermEquals("fox", filter, termAtt, payAtt, PayloadHelper.encodeFloat(3.5f));
		assertTermEquals("jumped", filter, termAtt, payAtt, PayloadHelper.encodeFloat(0.5f));
		assertTermEquals("over", filter, termAtt, payAtt, null);
		assertTermEquals("the", filter, termAtt, payAtt, null);
		assertTermEquals("lazy", filter, termAtt, payAtt, PayloadHelper.encodeFloat(5.0f));
		assertTermEquals("brown", filter, termAtt, payAtt, PayloadHelper.encodeFloat(99.3f));
		assertTermEquals("dogs", filter, termAtt, payAtt, PayloadHelper.encodeFloat(83.7f));
		assertFalse(filter.incrementToken());
		filter.end();
		filter.close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIntEncoding() throws Exception
	  public virtual void testIntEncoding()
	  {
		string test = "The quick|1 red|2 fox|3 jumped over the lazy|5 brown|99 dogs|83";
		DelimitedPayloadTokenFilter filter = new DelimitedPayloadTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false), '|', new IntegerEncoder());
		CharTermAttribute termAtt = filter.getAttribute(typeof(CharTermAttribute));
		PayloadAttribute payAtt = filter.getAttribute(typeof(PayloadAttribute));
		filter.reset();
		assertTermEquals("The", filter, termAtt, payAtt, null);
		assertTermEquals("quick", filter, termAtt, payAtt, PayloadHelper.encodeInt(1));
		assertTermEquals("red", filter, termAtt, payAtt, PayloadHelper.encodeInt(2));
		assertTermEquals("fox", filter, termAtt, payAtt, PayloadHelper.encodeInt(3));
		assertTermEquals("jumped", filter, termAtt, payAtt, null);
		assertTermEquals("over", filter, termAtt, payAtt, null);
		assertTermEquals("the", filter, termAtt, payAtt, null);
		assertTermEquals("lazy", filter, termAtt, payAtt, PayloadHelper.encodeInt(5));
		assertTermEquals("brown", filter, termAtt, payAtt, PayloadHelper.encodeInt(99));
		assertTermEquals("dogs", filter, termAtt, payAtt, PayloadHelper.encodeInt(83));
		assertFalse(filter.incrementToken());
		filter.end();
		filter.close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void assertTermEquals(String expected, org.apache.lucene.analysis.TokenStream stream, byte[] expectPay) throws Exception
	  internal virtual void assertTermEquals(string expected, TokenStream stream, sbyte[] expectPay)
	  {
		CharTermAttribute termAtt = stream.getAttribute(typeof(CharTermAttribute));
		PayloadAttribute payloadAtt = stream.getAttribute(typeof(PayloadAttribute));
		assertTrue(stream.incrementToken());
		assertEquals(expected, termAtt.ToString());
		BytesRef payload = payloadAtt.Payload;
		if (payload != null)
		{
		  assertTrue(payload.length + " does not equal: " + expectPay.Length, payload.length == expectPay.Length);
		  for (int i = 0; i < expectPay.Length; i++)
		  {
			assertTrue(expectPay[i] + " does not equal: " + payload.bytes[i + payload.offset], expectPay[i] == payload.bytes[i + payload.offset]);

		  }
		}
		else
		{
		  assertTrue("expectPay is not null and it should be", expectPay == null);
		}
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void assertTermEquals(String expected, org.apache.lucene.analysis.TokenStream stream, org.apache.lucene.analysis.tokenattributes.CharTermAttribute termAtt, org.apache.lucene.analysis.tokenattributes.PayloadAttribute payAtt, byte[] expectPay) throws Exception
	  internal virtual void assertTermEquals(string expected, TokenStream stream, CharTermAttribute termAtt, PayloadAttribute payAtt, sbyte[] expectPay)
	  {
		assertTrue(stream.incrementToken());
		assertEquals(expected, termAtt.ToString());
		BytesRef payload = payAtt.Payload;
		if (payload != null)
		{
		  assertTrue(payload.length + " does not equal: " + expectPay.Length, payload.length == expectPay.Length);
		  for (int i = 0; i < expectPay.Length; i++)
		  {
			assertTrue(expectPay[i] + " does not equal: " + payload.bytes[i + payload.offset], expectPay[i] == payload.bytes[i + payload.offset]);

		  }
		}
		else
		{
		  assertTrue("expectPay is not null and it should be", expectPay == null);
		}
	  }
	}

}