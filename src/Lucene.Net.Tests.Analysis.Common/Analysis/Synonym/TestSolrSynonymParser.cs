namespace org.apache.lucene.analysis.synonym
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


	using EnglishAnalyzer = org.apache.lucene.analysis.en.EnglishAnalyzer;
	using Test = org.junit.Test;

	/// <summary>
	/// Tests parser for the Solr synonyms format
	/// @lucene.experimental
	/// </summary>
	public class TestSolrSynonymParser : BaseTokenStreamTestCase
	{

	  /// <summary>
	  /// Tests some simple examples from the solr wiki </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSimple() throws Exception
	  public virtual void testSimple()
	  {
		string testFile = "i-pod, ipod, ipoooood\n" + "foo => foo bar\n" + "foo => baz\n" + "this test, that testing";

		SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(random()));
		parser.parse(new StringReader(testFile));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = parser.build();
		SynonymMap map = parser.build();

		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, map);

		assertAnalyzesTo(analyzer, "ball", new string[] {"ball"}, new int[] {1});

		assertAnalyzesTo(analyzer, "i-pod", new string[] {"i-pod", "ipod", "ipoooood"}, new int[] {1, 0, 0});

		assertAnalyzesTo(analyzer, "foo", new string[] {"foo", "baz", "bar"}, new int[] {1, 0, 1});

		assertAnalyzesTo(analyzer, "this test", new string[] {"this", "that", "test", "testing"}, new int[] {1, 0, 1, 0});
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestSolrSynonymParser outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper(TestSolrSynonymParser outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
		  }
	  }

	  /// <summary>
	  /// parse a syn file with bad syntax </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected=java.text.ParseException.class) public void testInvalidDoubleMap() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testInvalidDoubleMap()
	  {
		string testFile = "a => b => c";
		SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(random()));
		parser.parse(new StringReader(testFile));
	  }

	  /// <summary>
	  /// parse a syn file with bad syntax </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected=java.text.ParseException.class) public void testInvalidAnalyzesToNothingOutput() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testInvalidAnalyzesToNothingOutput()
	  {
		string testFile = "a => 1";
		SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(random(), MockTokenizer.SIMPLE, false));
		parser.parse(new StringReader(testFile));
	  }

	  /// <summary>
	  /// parse a syn file with bad syntax </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected=java.text.ParseException.class) public void testInvalidAnalyzesToNothingInput() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testInvalidAnalyzesToNothingInput()
	  {
		string testFile = "1 => a";
		SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(random(), MockTokenizer.SIMPLE, false));
		parser.parse(new StringReader(testFile));
	  }

	  /// <summary>
	  /// parse a syn file with bad syntax </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected=java.text.ParseException.class) public void testInvalidPositionsInput() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testInvalidPositionsInput()
	  {
		string testFile = "testola => the test";
		SolrSynonymParser parser = new SolrSynonymParser(true, true, new EnglishAnalyzer(TEST_VERSION_CURRENT));
		parser.parse(new StringReader(testFile));
	  }

	  /// <summary>
	  /// parse a syn file with bad syntax </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected=java.text.ParseException.class) public void testInvalidPositionsOutput() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testInvalidPositionsOutput()
	  {
		string testFile = "the test => testola";
		SolrSynonymParser parser = new SolrSynonymParser(true, true, new EnglishAnalyzer(TEST_VERSION_CURRENT));
		parser.parse(new StringReader(testFile));
	  }

	  /// <summary>
	  /// parse a syn file with some escaped syntax chars </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEscapedStuff() throws Exception
	  public virtual void testEscapedStuff()
	  {
		string testFile = "a\\=>a => b\\=>b\n" + "a\\,a => b\\,b";
		SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(random(), MockTokenizer.KEYWORD, false));
		parser.parse(new StringReader(testFile));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = parser.build();
		SynonymMap map = parser.build();
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper2(this, map);

		assertAnalyzesTo(analyzer, "ball", new string[] {"ball"}, new int[] {1});

		assertAnalyzesTo(analyzer, "a=>a", new string[] {"b=>b"}, new int[] {1});

		assertAnalyzesTo(analyzer, "a,a", new string[] {"b,b"}, new int[] {1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestSolrSynonymParser outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper2(TestSolrSynonymParser outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, false));
		  }
	  }
	}

}