namespace org.apache.lucene.analysis.miscellaneous
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

	using org.apache.lucene.analysis;
	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using Version = org.apache.lucene.util.Version;


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using Test = org.junit.Test;

	public class TestLengthFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFilterNoPosIncr() throws Exception
	  public virtual void testFilterNoPosIncr()
	  {
		TokenStream stream = new MockTokenizer(new StringReader("short toolong evenmuchlongertext a ab toolong foo"), MockTokenizer.WHITESPACE, false);
		LengthFilter filter = new LengthFilter(Version.LUCENE_43, false, stream, 2, 6);
		assertTokenStreamContents(filter, new string[]{"short", "ab", "foo"}, new int[]{1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFilterWithPosIncr() throws Exception
	  public virtual void testFilterWithPosIncr()
	  {
		TokenStream stream = new MockTokenizer(new StringReader("short toolong evenmuchlongertext a ab toolong foo"), MockTokenizer.WHITESPACE, false);
		LengthFilter filter = new LengthFilter(TEST_VERSION_CURRENT, stream, 2, 6);
		assertTokenStreamContents(filter, new string[]{"short", "ab", "foo"}, new int[]{1, 4, 2});
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
		  private readonly TestLengthFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestLengthFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new LengthFilter(TEST_VERSION_CURRENT, tokenizer, 0, 5));
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
		new LengthFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("accept only valid arguments")), -4, -1);
	  }
	}

}