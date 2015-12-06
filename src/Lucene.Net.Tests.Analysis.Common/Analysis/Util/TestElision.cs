using System.Collections.Generic;

namespace org.apache.lucene.analysis.util
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
	using FrenchAnalyzer = org.apache.lucene.analysis.fr.FrenchAnalyzer;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

	/// 
	public class TestElision : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testElision() throws Exception
	  public virtual void testElision()
	  {
		string test = "Plop, juste pour voir l'embrouille avec O'brian. M'enfin.";
		Tokenizer tokenizer = new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(test));
		CharArraySet articles = new CharArraySet(TEST_VERSION_CURRENT, asSet("l", "M"), false);
		TokenFilter filter = new ElisionFilter(tokenizer, articles);
		IList<string> tas = filter(filter);
		assertEquals("embrouille", tas[4]);
		assertEquals("O'brian", tas[6]);
		assertEquals("enfin", tas[7]);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private java.util.List<String> filter(org.apache.lucene.analysis.TokenFilter filter) throws java.io.IOException
	  private IList<string> filter(TokenFilter filter)
	  {
		IList<string> tas = new List<string>();
		CharTermAttribute termAtt = filter.getAttribute(typeof(CharTermAttribute));
		filter.reset();
		while (filter.incrementToken())
		{
		  tas.Add(termAtt.ToString());
		}
		filter.end();
		filter.close();
		return tas;
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
		  private readonly TestElision outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestElision outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new ElisionFilter(tokenizer, FrenchAnalyzer.DEFAULT_ARTICLES));
		  }
	  }

	}

}