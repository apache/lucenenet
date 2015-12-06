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

	using PayloadAttribute = org.apache.lucene.analysis.tokenattributes.PayloadAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;


	public class TypeAsPayloadTokenFilterTest : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws java.io.IOException
	  public virtual void test()
	  {
		string test = "The quick red fox jumped over the lazy brown dogs";

		TypeAsPayloadTokenFilter nptf = new TypeAsPayloadTokenFilter(new WordTokenFilter(this, new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false)));
		int count = 0;
		CharTermAttribute termAtt = nptf.getAttribute(typeof(CharTermAttribute));
		TypeAttribute typeAtt = nptf.getAttribute(typeof(TypeAttribute));
		PayloadAttribute payloadAtt = nptf.getAttribute(typeof(PayloadAttribute));
		nptf.reset();
		while (nptf.incrementToken())
		{
		  assertTrue(typeAtt.type() + " is not null and it should be", typeAtt.type().Equals(char.ToUpper(termAtt.buffer()[0]).ToString()));
		  assertTrue("nextToken.getPayload() is null and it shouldn't be", payloadAtt.Payload != null);
		  string type = payloadAtt.Payload.utf8ToString();
		  assertTrue(type + " is not equal to " + typeAtt.type(), type.Equals(typeAtt.type()));
		  count++;
		}

		assertTrue(count + " does not equal: " + 10, count == 10);
	  }

	  private sealed class WordTokenFilter : TokenFilter
	  {
		  private readonly TypeAsPayloadTokenFilterTest outerInstance;

		internal readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		internal readonly TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));

		internal WordTokenFilter(TypeAsPayloadTokenFilterTest outerInstance, TokenStream input) : base(input)
		{
			this.outerInstance = outerInstance;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  if (input.incrementToken())
		  {
			typeAtt.Type = char.ToUpper(termAtt.buffer()[0]).ToString();
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