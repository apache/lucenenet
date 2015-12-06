namespace org.apache.lucene.analysis.sinks
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
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;

	public class TokenTypeSinkTokenizerTest : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws java.io.IOException
	  public virtual void test()
	  {
		TokenTypeSinkFilter sinkFilter = new TokenTypeSinkFilter("D");
		string test = "The quick red fox jumped over the lazy brown dogs";

		TeeSinkTokenFilter ttf = new TeeSinkTokenFilter(new WordTokenFilter(this, new MockTokenizer(new StringReader(test), MockTokenizer.WHITESPACE, false)));
		TeeSinkTokenFilter.SinkTokenStream sink = ttf.newSinkTokenStream(sinkFilter);

		bool seenDogs = false;

		CharTermAttribute termAtt = ttf.addAttribute(typeof(CharTermAttribute));
		TypeAttribute typeAtt = ttf.addAttribute(typeof(TypeAttribute));
		ttf.reset();
		while (ttf.incrementToken())
		{
		  if (termAtt.ToString().Equals("dogs"))
		  {
			seenDogs = true;
			assertTrue(typeAtt.type() + " is not equal to " + "D", typeAtt.type().Equals("D") == true);
		  }
		  else
		  {
			assertTrue(typeAtt.type() + " is not null and it should be", typeAtt.type().Equals("word"));
		  }
		}
		assertTrue(seenDogs + " does not equal: " + true, seenDogs == true);

		int sinkCount = 0;
		sink.reset();
		while (sink.incrementToken())
		{
		  sinkCount++;
		}

		assertTrue("sink Size: " + sinkCount + " is not: " + 1, sinkCount == 1);
	  }

	  private class WordTokenFilter : TokenFilter
	  {
		  private readonly TokenTypeSinkTokenizerTest outerInstance;

		internal readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		internal readonly TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));

		internal WordTokenFilter(TokenTypeSinkTokenizerTest outerInstance, TokenStream input) : base(input)
		{
			this.outerInstance = outerInstance;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  if (!input.incrementToken())
		  {
			  return false;
		  }

		  if (termAtt.ToString().Equals("dogs"))
		  {
			typeAtt.Type = "D";
		  }
		  return true;
		}
	  }
	}
}