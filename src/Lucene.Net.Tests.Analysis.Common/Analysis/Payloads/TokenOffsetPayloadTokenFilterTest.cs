namespace org.apache.lucene.analysis.payloads
{

	/// <summary>
	/// Copyright 2004 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>

	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PayloadAttribute = org.apache.lucene.analysis.tokenattributes.PayloadAttribute;
	using BytesRef = org.apache.lucene.util.BytesRef;


	public class TokenOffsetPayloadTokenFilterTest : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws java.io.IOException
	  public virtual void test()
	  {
		string test = "The quick red fox jumped over the lazy brown dogs";

		TokenOffsetPayloadTokenFilter nptf = new TokenOffsetPayloadTokenFilter(new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false));
		int count = 0;
		PayloadAttribute payloadAtt = nptf.getAttribute(typeof(PayloadAttribute));
		OffsetAttribute offsetAtt = nptf.getAttribute(typeof(OffsetAttribute));
		nptf.reset();
		while (nptf.incrementToken())
		{
		  BytesRef pay = payloadAtt.Payload;
		  assertTrue("pay is null and it shouldn't be", pay != null);
		  sbyte[] data = pay.bytes;
		  int start = PayloadHelper.decodeInt(data, 0);
		  assertTrue(start + " does not equal: " + offsetAtt.startOffset(), start == offsetAtt.startOffset());
		  int end = PayloadHelper.decodeInt(data, 4);
		  assertTrue(end + " does not equal: " + offsetAtt.endOffset(), end == offsetAtt.endOffset());
		  count++;
		}
		assertTrue(count + " does not equal: " + 10, count == 10);

	  }


	}
}