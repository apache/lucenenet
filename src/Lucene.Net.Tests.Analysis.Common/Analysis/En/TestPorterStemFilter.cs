namespace org.apache.lucene.analysis.en
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


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.VocabularyAssert.*;

	/// <summary>
	/// Test the PorterStemFilter with Martin Porter's test data.
	/// </summary>
	public class TestPorterStemFilter : BaseTokenStreamTestCase
	{
	  internal Analyzer a = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
			return new TokenStreamComponents(t, new PorterStemFilter(t));
		  }
	  }

	  /// <summary>
	  /// Run the stemmer against all strings in voc.txt
	  /// The output should be the same as the string in output.txt
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPorterStemFilter() throws Exception
	  public virtual void testPorterStemFilter()
	  {
		assertVocabulary(a, getDataFile("porterTestData.zip"), "voc.txt", "output.txt");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithKeywordAttribute() throws java.io.IOException
	  public virtual void testWithKeywordAttribute()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		set.add("yourselves");
		Tokenizer tokenizer = new MockTokenizer(new StringReader("yourselves yours"), MockTokenizer.WHITESPACE, false);
		TokenStream filter = new PorterStemFilter(new SetKeywordMarkerFilter(tokenizer, set));
		assertTokenStreamContents(filter, new string[] {"yourselves", "your"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);
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
		  private readonly TestPorterStemFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestPorterStemFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new PorterStemFilter(tokenizer));
		  }
	  }
	}

}