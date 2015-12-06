namespace org.apache.lucene.analysis.miscellaneous
{


	using KeywordAttribute = org.apache.lucene.analysis.tokenattributes.KeywordAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Test = org.junit.Test;

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

	/// <summary>
	/// Testcase for <seealso cref="KeywordMarkerFilter"/>
	/// </summary>
	public class TestKeywordMarkerFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSetFilterIncrementToken() throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testSetFilterIncrementToken()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 5, true);
		set.add("lucenefox");
		string[] output = new string[] {"the", "quick", "brown", "LuceneFox", "jumps"};
		assertTokenStreamContents(new LowerCaseFilterMock(new SetKeywordMarkerFilter(new MockTokenizer(new StringReader("The quIck browN LuceneFox Jumps"), MockTokenizer.WHITESPACE, false), set)), output);
		CharArraySet mixedCaseSet = new CharArraySet(TEST_VERSION_CURRENT, asSet("LuceneFox"), false);
		assertTokenStreamContents(new LowerCaseFilterMock(new SetKeywordMarkerFilter(new MockTokenizer(new StringReader("The quIck browN LuceneFox Jumps"), MockTokenizer.WHITESPACE, false), mixedCaseSet)), output);
		CharArraySet set2 = set;
		assertTokenStreamContents(new LowerCaseFilterMock(new SetKeywordMarkerFilter(new MockTokenizer(new StringReader("The quIck browN LuceneFox Jumps"), MockTokenizer.WHITESPACE, false), set2)), output);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testPatternFilterIncrementToken() throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testPatternFilterIncrementToken()
	  {
		string[] output = new string[] {"the", "quick", "brown", "LuceneFox", "jumps"};
		assertTokenStreamContents(new LowerCaseFilterMock(new PatternKeywordMarkerFilter(new MockTokenizer(new StringReader("The quIck browN LuceneFox Jumps"), MockTokenizer.WHITESPACE, false), Pattern.compile("[a-zA-Z]+[fF]ox"))), output);

		output = new string[] {"the", "quick", "brown", "lucenefox", "jumps"};

		assertTokenStreamContents(new LowerCaseFilterMock(new PatternKeywordMarkerFilter(new MockTokenizer(new StringReader("The quIck browN LuceneFox Jumps"), MockTokenizer.WHITESPACE, false), Pattern.compile("[a-zA-Z]+[f]ox"))), output);
	  }

	  // LUCENE-2901
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComposition() throws Exception
	  public virtual void testComposition()
	  {
		TokenStream ts = new LowerCaseFilterMock(new SetKeywordMarkerFilter(new SetKeywordMarkerFilter(new MockTokenizer(new StringReader("Dogs Trees Birds Houses"), MockTokenizer.WHITESPACE, false), new CharArraySet(TEST_VERSION_CURRENT, asSet("Birds", "Houses"), false)), new CharArraySet(TEST_VERSION_CURRENT, asSet("Dogs", "Trees"), false)));

		assertTokenStreamContents(ts, new string[] {"Dogs", "Trees", "Birds", "Houses"});

		ts = new LowerCaseFilterMock(new PatternKeywordMarkerFilter(new PatternKeywordMarkerFilter(new MockTokenizer(new StringReader("Dogs Trees Birds Houses"), MockTokenizer.WHITESPACE, false), Pattern.compile("Birds|Houses")), Pattern.compile("Dogs|Trees")));

		assertTokenStreamContents(ts, new string[] {"Dogs", "Trees", "Birds", "Houses"});

		ts = new LowerCaseFilterMock(new SetKeywordMarkerFilter(new PatternKeywordMarkerFilter(new MockTokenizer(new StringReader("Dogs Trees Birds Houses"), MockTokenizer.WHITESPACE, false), Pattern.compile("Birds|Houses")), new CharArraySet(TEST_VERSION_CURRENT, asSet("Dogs", "Trees"), false)));

		assertTokenStreamContents(ts, new string[] {"Dogs", "Trees", "Birds", "Houses"});
	  }

	  public sealed class LowerCaseFilterMock : TokenFilter
	  {

		internal readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		internal readonly KeywordAttribute keywordAttr = addAttribute(typeof(KeywordAttribute));

		public LowerCaseFilterMock(TokenStream @in) : base(@in)
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  if (input.incrementToken())
		  {
			if (!keywordAttr.Keyword)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String term = termAtt.toString().toLowerCase(java.util.Locale.ROOT);
			  string term = termAtt.ToString().ToLower(Locale.ROOT);
			  termAtt.setEmpty().append(term);
			}
			return true;
		  }
		  return false;
		}

	  }
	}

}