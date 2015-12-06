namespace org.apache.lucene.analysis.hi
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

	/// <summary>
	/// Test HindiStemmer
	/// </summary>
	public class TestHindiStemmer : BaseTokenStreamTestCase
	{
	  /// <summary>
	  /// Test masc noun inflections
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMasculineNouns() throws java.io.IOException
	  public virtual void testMasculineNouns()
	  {
		check("लडका", "लडक");
		check("लडके", "लडक");
		check("लडकों", "लडक");

		check("गुरु", "गुर");
		check("गुरुओं", "गुर");

		check("दोस्त", "दोस्त");
		check("दोस्तों", "दोस्त");
	  }

	  /// <summary>
	  /// Test feminine noun inflections
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFeminineNouns() throws java.io.IOException
	  public virtual void testFeminineNouns()
	  {
		check("लडकी", "लडक");
		check("लडकियों", "लडक");

		check("किताब", "किताब");
		check("किताबें", "किताब");
		check("किताबों", "किताब");

		check("आध्यापीका", "आध्यापीक");
		check("आध्यापीकाएं", "आध्यापीक");
		check("आध्यापीकाओं", "आध्यापीक");
	  }

	  /// <summary>
	  /// Test some verb forms
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testVerbs() throws java.io.IOException
	  public virtual void testVerbs()
	  {
		check("खाना", "खा");
		check("खाता", "खा");
		check("खाती", "खा");
		check("खा", "खा");
	  }

	  /// <summary>
	  /// From the paper: since the suffix list for verbs includes AI, awA and anI,
	  /// additional suffixes had to be added to the list for noun/adjectives
	  /// ending with these endings.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExceptions() throws java.io.IOException
	  public virtual void testExceptions()
	  {
		check("कठिनाइयां", "कठिन");
		check("कठिन", "कठिन");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void check(String input, String output) throws java.io.IOException
	  private void check(string input, string output)
	  {
		Tokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		TokenFilter tf = new HindiStemFilter(tokenizer);
		assertTokenStreamContents(tf, new string[] {output});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestHindiStemmer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestHindiStemmer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new HindiStemFilter(tokenizer));
		  }
	  }
	}

}