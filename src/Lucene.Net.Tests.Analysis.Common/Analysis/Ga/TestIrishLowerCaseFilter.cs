namespace org.apache.lucene.analysis.ga
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
	/// Test the Irish lowercase filter.
	/// </summary>
	public class TestIrishLowerCaseFilter : BaseTokenStreamTestCase
	{

	  /// <summary>
	  /// Test lowercase
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIrishLowerCaseFilter() throws Exception
	  public virtual void testIrishLowerCaseFilter()
	  {
		TokenStream stream = new MockTokenizer(new StringReader("nAthair tUISCE hARD"), MockTokenizer.WHITESPACE, false);
		IrishLowerCaseFilter filter = new IrishLowerCaseFilter(stream);
		assertTokenStreamContents(filter, new string[] {"n-athair", "t-uisce", "hard"});
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
		  private readonly TestIrishLowerCaseFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestIrishLowerCaseFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new IrishLowerCaseFilter(tokenizer));
		  }
	  }
	}

}