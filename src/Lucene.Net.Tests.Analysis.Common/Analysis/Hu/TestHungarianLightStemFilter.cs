namespace org.apache.lucene.analysis.hu
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
	/// Simple tests for <seealso cref="HungarianLightStemFilter"/>
	/// </summary>
	public class TestHungarianLightStemFilter : BaseTokenStreamTestCase
	{
	  private Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(source, new HungarianLightStemFilter(source));
		  }
	  }

	  /// <summary>
	  /// Test against a vocabulary from the reference impl </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testVocabulary() throws java.io.IOException
	  public virtual void testVocabulary()
	  {
		assertVocabulary(analyzer, getDataFile("hulighttestdata.zip"), "hulight.txt");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeyword() throws java.io.IOException
	  public virtual void testKeyword()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.util.CharArraySet exclusionSet = new org.apache.lucene.analysis.util.CharArraySet(TEST_VERSION_CURRENT, asSet("babakocsi"), false);
		CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, asSet("babakocsi"), false);
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, exclusionSet);
		checkOneTerm(a, "babakocsi", "babakocsi");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestHungarianLightStemFilter outerInstance;

		  private CharArraySet exclusionSet;

		  public AnalyzerAnonymousInnerClassHelper2(TestHungarianLightStemFilter outerInstance, CharArraySet exclusionSet)
		  {
			  this.outerInstance = outerInstance;
			  this.exclusionSet = exclusionSet;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
			return new TokenStreamComponents(source, new HungarianLightStemFilter(sink));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestHungarianLightStemFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(TestHungarianLightStemFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new HungarianLightStemFilter(tokenizer));
		  }
	  }
	}

}