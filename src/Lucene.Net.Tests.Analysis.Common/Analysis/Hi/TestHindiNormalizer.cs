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
	/// Test HindiNormalizer
	/// </summary>
	public class TestHindiNormalizer : BaseTokenStreamTestCase
	{
	  /// <summary>
	  /// Test some basic normalization, with an example from the paper.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasics() throws java.io.IOException
	  public virtual void testBasics()
	  {
		check("अँगरेज़ी", "अंगरेजि");
		check("अँगरेजी", "अंगरेजि");
		check("अँग्रेज़ी", "अंगरेजि");
		check("अँग्रेजी", "अंगरेजि");
		check("अंगरेज़ी", "अंगरेजि");
		check("अंगरेजी", "अंगरेजि");
		check("अंग्रेज़ी", "अंगरेजि");
		check("अंग्रेजी", "अंगरेजि");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDecompositions() throws java.io.IOException
	  public virtual void testDecompositions()
	  {
		// removing nukta dot
		check("क़िताब", "किताब");
		check("फ़र्ज़", "फरज");
		check("क़र्ज़", "करज");
		// some other composed nukta forms
		check("ऱऴख़ग़ड़ढ़य़", "रळखगडढय");
		// removal of format (ZWJ/ZWNJ)
		check("शार्‍मा", "शारमा");
		check("शार्‌मा", "शारमा");
		// removal of chandra
		check("ॅॆॉॊऍऎऑऒ\u0972", "ेेोोएएओओअ");
		// vowel shortening
		check("आईऊॠॡऐऔीूॄॣैौ", "अइउऋऌएओिुृॢेो");
	  }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void check(String input, String output) throws java.io.IOException
	  private void check(string input, string output)
	  {
		Tokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		TokenFilter tf = new HindiNormalizationFilter(tokenizer);
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
		  private readonly TestHindiNormalizer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestHindiNormalizer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new HindiNormalizationFilter(tokenizer));
		  }
	  }
	}

}