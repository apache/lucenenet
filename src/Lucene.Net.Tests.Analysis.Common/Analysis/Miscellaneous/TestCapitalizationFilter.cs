using System.Collections.Generic;

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
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Test = org.junit.Test;

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.miscellaneous.CapitalizationFilter.*;

	/// <summary>
	/// Tests <seealso cref="CapitalizationFilter"/> </summary>
	public class TestCapitalizationFilter : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization() throws Exception
	  public virtual void testCapitalization()
	  {
		CharArraySet keep = new CharArraySet(TEST_VERSION_CURRENT, Arrays.asList("and", "the", "it", "BIG"), false);

		assertCapitalizesTo("kiTTEN", new string[] {"Kitten"}, true, keep, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		assertCapitalizesTo("and", new string[] {"And"}, true, keep, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		assertCapitalizesTo("AnD", new string[] {"And"}, true, keep, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		//first is not forced, but it's not a keep word, either
		assertCapitalizesTo("AnD", new string[] {"And"}, true, keep, false, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		assertCapitalizesTo("big", new string[] {"Big"}, true, keep, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		assertCapitalizesTo("BIG", new string[] {"BIG"}, true, keep, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		assertCapitalizesToKeyword("Hello thEre my Name is Ryan", "Hello there my name is ryan", true, keep, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		// now each token
		assertCapitalizesTo("Hello thEre my Name is Ryan", new string[] {"Hello", "There", "My", "Name", "Is", "Ryan"}, false, keep, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		// now only the long words
		assertCapitalizesTo("Hello thEre my Name is Ryan", new string[] {"Hello", "There", "my", "Name", "is", "Ryan"}, false, keep, true, null, 3, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		// without prefix
		assertCapitalizesTo("McKinley", new string[] {"Mckinley"}, true, keep, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		// Now try some prefixes
		IList<char[]> okPrefix = new List<char[]>();
		okPrefix.Add("McK".ToCharArray());

		assertCapitalizesTo("McKinley", new string[] {"McKinley"}, true, keep, true, okPrefix, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		// now try some stuff with numbers
		assertCapitalizesTo("1st 2nd third", new string[] {"1st", "2nd", "Third"}, false, keep, false, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);

		assertCapitalizesToKeyword("the The the", "The The the", false, keep, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void assertCapitalizesTo(org.apache.lucene.analysis.Tokenizer tokenizer, String expected[], boolean onlyFirstWord, org.apache.lucene.analysis.util.CharArraySet keep, boolean forceFirstLetter, java.util.Collection<char[]> okPrefix, int minWordLength, int maxWordCount, int maxTokenLength) throws java.io.IOException
	  internal static void assertCapitalizesTo(Tokenizer tokenizer, string[] expected, bool onlyFirstWord, CharArraySet keep, bool forceFirstLetter, ICollection<char[]> okPrefix, int minWordLength, int maxWordCount, int maxTokenLength)
	  {
		CapitalizationFilter filter = new CapitalizationFilter(tokenizer, onlyFirstWord, keep, forceFirstLetter, okPrefix, minWordLength, maxWordCount, maxTokenLength);
		assertTokenStreamContents(filter, expected);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void assertCapitalizesTo(String input, String expected[], boolean onlyFirstWord, org.apache.lucene.analysis.util.CharArraySet keep, boolean forceFirstLetter, java.util.Collection<char[]> okPrefix, int minWordLength, int maxWordCount, int maxTokenLength) throws java.io.IOException
	  internal static void assertCapitalizesTo(string input, string[] expected, bool onlyFirstWord, CharArraySet keep, bool forceFirstLetter, ICollection<char[]> okPrefix, int minWordLength, int maxWordCount, int maxTokenLength)
	  {
		assertCapitalizesTo(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), expected, onlyFirstWord, keep, forceFirstLetter, okPrefix, minWordLength, maxWordCount, maxTokenLength);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void assertCapitalizesToKeyword(String input, String expected, boolean onlyFirstWord, org.apache.lucene.analysis.util.CharArraySet keep, boolean forceFirstLetter, java.util.Collection<char[]> okPrefix, int minWordLength, int maxWordCount, int maxTokenLength) throws java.io.IOException
	  internal static void assertCapitalizesToKeyword(string input, string expected, bool onlyFirstWord, CharArraySet keep, bool forceFirstLetter, ICollection<char[]> okPrefix, int minWordLength, int maxWordCount, int maxTokenLength)
	  {
		assertCapitalizesTo(new MockTokenizer(new StringReader(input), MockTokenizer.KEYWORD, false), new string[] {expected}, onlyFirstWord, keep, forceFirstLetter, okPrefix, minWordLength, maxWordCount, maxTokenLength);
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
		  private readonly TestCapitalizationFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestCapitalizationFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new CapitalizationFilter(tokenizer));
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
		  private readonly TestCapitalizationFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestCapitalizationFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new CapitalizationFilter(tokenizer));
		  }
	  }

	  /// <summary>
	  /// checking the validity of constructor arguments
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = IllegalArgumentException.class) public void testIllegalArguments() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testIllegalArguments()
	  {
		new CapitalizationFilter(new MockTokenizer(new StringReader("accept only valid arguments"), MockTokenizer.WHITESPACE, false),true, null, true, null, -1, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH);
	  }
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = IllegalArgumentException.class) public void testIllegalArguments1() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testIllegalArguments1()
	  {
		new CapitalizationFilter(new MockTokenizer(new StringReader("accept only valid arguments"), MockTokenizer.WHITESPACE, false),true, null, true, null, 0, -10, DEFAULT_MAX_TOKEN_LENGTH);
	  }
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = IllegalArgumentException.class) public void testIllegalArguments2() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testIllegalArguments2()
	  {
		new CapitalizationFilter(new MockTokenizer(new StringReader("accept only valid arguments"), MockTokenizer.WHITESPACE, false),true, null, true, null, 0, DEFAULT_MAX_WORD_COUNT, -50);
	  }
	}

}