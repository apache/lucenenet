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

namespace org.apache.lucene.analysis.pattern
{

	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;


	public class TestPatternReplaceFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReplaceAll() throws Exception
	  public virtual void testReplaceAll()
	  {
		string input = "aabfooaabfooabfoob ab caaaaaaaaab";
		TokenStream ts = new PatternReplaceFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), Pattern.compile("a*b"), "-", true);
		assertTokenStreamContents(ts, new string[] {"-foo-foo-foo-", "-", "c-"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReplaceFirst() throws Exception
	  public virtual void testReplaceFirst()
	  {
		string input = "aabfooaabfooabfoob ab caaaaaaaaab";
		TokenStream ts = new PatternReplaceFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), Pattern.compile("a*b"), "-", false);
		assertTokenStreamContents(ts, new string[] {"-fooaabfooabfoob", "-", "c-"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStripFirst() throws Exception
	  public virtual void testStripFirst()
	  {
		string input = "aabfooaabfooabfoob ab caaaaaaaaab";
		TokenStream ts = new PatternReplaceFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), Pattern.compile("a*b"), null, false);
		assertTokenStreamContents(ts, new string[] {"fooaabfooabfoob", "", "c"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStripAll() throws Exception
	  public virtual void testStripAll()
	  {
		string input = "aabfooaabfooabfoob ab caaaaaaaaab";
		TokenStream ts = new PatternReplaceFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), Pattern.compile("a*b"), null, true);
		assertTokenStreamContents(ts, new string[] {"foofoofoo", "", "c"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReplaceAllWithBackRef() throws Exception
	  public virtual void testReplaceAllWithBackRef()
	  {
		string input = "aabfooaabfooabfoob ab caaaaaaaaab";
		TokenStream ts = new PatternReplaceFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), Pattern.compile("(a*)b"), "$1\\$", true);
		assertTokenStreamContents(ts, new string[] {"aa$fooaa$fooa$foo$", "a$", "caaaaaaaaa$"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);

		Analyzer b = new AnalyzerAnonymousInnerClassHelper2(this);
		checkRandomData(random(), b, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestPatternReplaceFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestPatternReplaceFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenStream filter = new PatternReplaceFilter(tokenizer, Pattern.compile("a"), "b", false);
			return new TokenStreamComponents(tokenizer, filter);
		  }
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestPatternReplaceFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestPatternReplaceFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenStream filter = new PatternReplaceFilter(tokenizer, Pattern.compile("a"), "b", true);
			return new TokenStreamComponents(tokenizer, filter);
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
		  private readonly TestPatternReplaceFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(TestPatternReplaceFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new PatternReplaceFilter(tokenizer, Pattern.compile("a"), "b", true));
		  }
	  }

	}

}