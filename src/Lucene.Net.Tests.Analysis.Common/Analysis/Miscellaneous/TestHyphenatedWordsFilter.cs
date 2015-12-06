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

namespace org.apache.lucene.analysis.miscellaneous
{


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;

	/// <summary>
	/// HyphenatedWordsFilter test
	/// </summary>
	public class TestHyphenatedWordsFilter : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHyphenatedWords() throws Exception
	  public virtual void testHyphenatedWords()
	  {
		string input = "ecologi-\r\ncal devel-\r\n\r\nop compre-\u0009hensive-hands-on and ecologi-\ncal";
		// first test
		TokenStream ts = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		ts = new HyphenatedWordsFilter(ts);
		assertTokenStreamContents(ts, new string[] {"ecological", "develop", "comprehensive-hands-on", "and", "ecological"});
	  }

	  /// <summary>
	  /// Test that HyphenatedWordsFilter behaves correctly with a final hyphen
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHyphenAtEnd() throws Exception
	  public virtual void testHyphenAtEnd()
	  {
		  string input = "ecologi-\r\ncal devel-\r\n\r\nop compre-\u0009hensive-hands-on and ecology-";
		  // first test
		  TokenStream ts = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		  ts = new HyphenatedWordsFilter(ts);
		  assertTokenStreamContents(ts, new string[] {"ecological", "develop", "comprehensive-hands-on", "and", "ecology-"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOffsets() throws Exception
	  public virtual void testOffsets()
	  {
		string input = "abc- def geh 1234- 5678-";
		TokenStream ts = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		ts = new HyphenatedWordsFilter(ts);
		assertTokenStreamContents(ts, new string[] {"abcdef", "geh", "12345678-"}, new int[] {0, 9, 13}, new int[] {8, 12, 24});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomString() throws Exception
	  public virtual void testRandomString()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);

		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestHyphenatedWordsFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestHyphenatedWordsFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new HyphenatedWordsFilter(tokenizer));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestHyphenatedWordsFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestHyphenatedWordsFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new HyphenatedWordsFilter(tokenizer));
		  }
	  }
	}

}