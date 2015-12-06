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

	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using PayloadAttribute = org.apache.lucene.analysis.tokenattributes.PayloadAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;


	public class NumericPayloadTokenFilterTest : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws java.io.IOException
	  public virtual void test()
	  {
		string test = "The quick red fox jumped over the lazy brown dogs";

		NumericPayloadTokenFilter nptf = new NumericPayloadTokenFilter(new WordTokenFilter(this, new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false)), 3, "D");
		bool seenDogs = false;
		CharTermAttribute termAtt = nptf.getAttribute(typeof(CharTermAttribute));
		TypeAttribute typeAtt = nptf.getAttribute(typeof(TypeAttribute));
		PayloadAttribute payloadAtt = nptf.getAttribute(typeof(PayloadAttribute));
		nptf.reset();
		while (nptf.incrementToken())
		{
		  if (termAtt.ToString().Equals("dogs"))
		  {
			seenDogs = true;
			assertTrue(typeAtt.type() + " is not equal to " + "D", typeAtt.type().Equals("D") == true);
			assertTrue("payloadAtt.getPayload() is null and it shouldn't be", payloadAtt.Payload != null);
			sbyte[] bytes = payloadAtt.Payload.bytes; //safe here to just use the bytes, otherwise we should use offset, length
			assertTrue(bytes.Length + " does not equal: " + payloadAtt.Payload.length, bytes.Length == payloadAtt.Payload.length);
			assertTrue(payloadAtt.Payload.offset + " does not equal: " + 0, payloadAtt.Payload.offset == 0);
			float pay = PayloadHelper.decodeFloat(bytes);
			assertTrue(pay + " does not equal: " + 3, pay == 3);
		  }
		  else
		  {
			assertTrue(typeAtt.type() + " is not null and it should be", typeAtt.type().Equals("word"));
		  }
		}
		assertTrue(seenDogs + " does not equal: " + true, seenDogs == true);
	  }

	  private sealed class WordTokenFilter : TokenFilter
	  {
		  private readonly NumericPayloadTokenFilterTest outerInstance;

		internal readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		internal readonly TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));

		internal WordTokenFilter(NumericPayloadTokenFilterTest outerInstance, TokenStream input) : base(input)
		{
			this.outerInstance = outerInstance;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  if (input.incrementToken())
		  {
			if (termAtt.ToString().Equals("dogs"))
			{
			  typeAtt.Type = "D";
			}
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