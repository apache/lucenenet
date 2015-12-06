using System.Collections.Generic;

namespace org.apache.lucene.analysis.miscellaneous
{


	using org.apache.lucene.analysis;
	using SimpleAnalyzer = org.apache.lucene.analysis.core.SimpleAnalyzer;
	using WhitespaceAnalyzer = org.apache.lucene.analysis.core.WhitespaceAnalyzer;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using IOUtils = org.apache.lucene.util.IOUtils;

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

	public class TestPerFieldAnalyzerWrapper : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPerField() throws Exception
	  public virtual void testPerField()
	  {
		string text = "Qwerty";

		IDictionary<string, Analyzer> analyzerPerField = new Dictionary<string, Analyzer>();
		analyzerPerField["special"] = new SimpleAnalyzer(TEST_VERSION_CURRENT);

		PerFieldAnalyzerWrapper analyzer = new PerFieldAnalyzerWrapper(new WhitespaceAnalyzer(TEST_VERSION_CURRENT), analyzerPerField);

		TokenStream tokenStream = analyzer.tokenStream("field", text);
		try
		{
		  CharTermAttribute termAtt = tokenStream.getAttribute(typeof(CharTermAttribute));
		  tokenStream.reset();

		  assertTrue(tokenStream.incrementToken());
		  assertEquals("WhitespaceAnalyzer does not lowercase", "Qwerty", termAtt.ToString());
		  assertFalse(tokenStream.incrementToken());
		  tokenStream.end();
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(tokenStream);
		}

		tokenStream = analyzer.tokenStream("special", text);
		try
		{
		  CharTermAttribute termAtt = tokenStream.getAttribute(typeof(CharTermAttribute));
		  tokenStream.reset();

		  assertTrue(tokenStream.incrementToken());
		  assertEquals("SimpleAnalyzer lowercases", "qwerty", termAtt.ToString());
		  assertFalse(tokenStream.incrementToken());
		  tokenStream.end();
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(tokenStream);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCharFilters() throws Exception
	  public virtual void testCharFilters()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		assertAnalyzesTo(a, "ab", new string[] {"aab"}, new int[] {0}, new int[] {2});

		// now wrap in PFAW
		PerFieldAnalyzerWrapper p = new PerFieldAnalyzerWrapper(a, System.Linq.Enumerable.Empty<string, Analyzer>());

		assertAnalyzesTo(p, "ab", new string[] {"aab"}, new int[] {0}, new int[] {2});
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestPerFieldAnalyzerWrapper outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestPerFieldAnalyzerWrapper outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			return new TokenStreamComponents(new MockTokenizer(reader));
		  }

		  protected internal override Reader initReader(string fieldName, Reader reader)
		  {
			return new MockCharFilter(reader, 7);
		  }
	  }
	}

}