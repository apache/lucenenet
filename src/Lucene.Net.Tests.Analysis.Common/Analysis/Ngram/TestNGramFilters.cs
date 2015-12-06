namespace org.apache.lucene.analysis.ngram
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


	using BaseTokenStreamFactoryTestCase = org.apache.lucene.analysis.util.BaseTokenStreamFactoryTestCase;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Simple tests to ensure the NGram filter factories are working.
	/// </summary>
	public class TestNGramFilters : BaseTokenStreamFactoryTestCase
	{
	  /// <summary>
	  /// Test NGramTokenizerFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNGramTokenizer() throws Exception
	  public virtual void testNGramTokenizer()
	  {
		Reader reader = new StringReader("test");
		TokenStream stream = tokenizerFactory("NGram").create(reader);
		assertTokenStreamContents(stream, new string[] {"t", "te", "e", "es", "s", "st", "t"});
	  }

	  /// <summary>
	  /// Test NGramTokenizerFactory with min and max gram options
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNGramTokenizer2() throws Exception
	  public virtual void testNGramTokenizer2()
	  {
		Reader reader = new StringReader("test");
		TokenStream stream = tokenizerFactory("NGram", "minGramSize", "2", "maxGramSize", "3").create(reader);
		assertTokenStreamContents(stream, new string[] {"te", "tes", "es", "est", "st"});
	  }

	  /// <summary>
	  /// Test the NGramFilterFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNGramFilter() throws Exception
	  public virtual void testNGramFilter()
	  {
		Reader reader = new StringReader("test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("NGram").create(stream);
		assertTokenStreamContents(stream, new string[] {"t", "te", "e", "es", "s", "st", "t"});
	  }

	  /// <summary>
	  /// Test the NGramFilterFactory with min and max gram options
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNGramFilter2() throws Exception
	  public virtual void testNGramFilter2()
	  {
		Reader reader = new StringReader("test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("NGram", "minGramSize", "2", "maxGramSize", "3").create(stream);
		assertTokenStreamContents(stream, new string[] {"te", "tes", "es", "est", "st"});
	  }

	  /// <summary>
	  /// Test EdgeNGramTokenizerFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEdgeNGramTokenizer() throws Exception
	  public virtual void testEdgeNGramTokenizer()
	  {
		Reader reader = new StringReader("test");
		TokenStream stream = tokenizerFactory("EdgeNGram").create(reader);
		assertTokenStreamContents(stream, new string[] {"t"});
	  }

	  /// <summary>
	  /// Test EdgeNGramTokenizerFactory with min and max gram size
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEdgeNGramTokenizer2() throws Exception
	  public virtual void testEdgeNGramTokenizer2()
	  {
		Reader reader = new StringReader("test");
		TokenStream stream = tokenizerFactory("EdgeNGram", "minGramSize", "1", "maxGramSize", "2").create(reader);
		assertTokenStreamContents(stream, new string[] {"t", "te"});
	  }

	  /// <summary>
	  /// Test EdgeNGramTokenizerFactory with side option
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEdgeNGramTokenizer3() throws Exception
	  public virtual void testEdgeNGramTokenizer3()
	  {
		Reader reader = new StringReader("ready");
		TokenStream stream = tokenizerFactory("EdgeNGram", Version.LUCENE_43, "side", "back").create(reader);
		assertTokenStreamContents(stream, new string[] {"y"});
	  }

	  /// <summary>
	  /// Test EdgeNGramFilterFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEdgeNGramFilter() throws Exception
	  public virtual void testEdgeNGramFilter()
	  {
		Reader reader = new StringReader("test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("EdgeNGram").create(stream);
		assertTokenStreamContents(stream, new string[] {"t"});
	  }

	  /// <summary>
	  /// Test EdgeNGramFilterFactory with min and max gram size
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEdgeNGramFilter2() throws Exception
	  public virtual void testEdgeNGramFilter2()
	  {
		Reader reader = new StringReader("test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("EdgeNGram", "minGramSize", "1", "maxGramSize", "2").create(stream);
		assertTokenStreamContents(stream, new string[] {"t", "te"});
	  }

	  /// <summary>
	  /// Test EdgeNGramFilterFactory with side option
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEdgeNGramFilter3() throws Exception
	  public virtual void testEdgeNGramFilter3()
	  {
		Reader reader = new StringReader("ready");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("EdgeNGram", Version.LUCENE_43, "side", "back").create(stream);
		assertTokenStreamContents(stream, new string[] {"y"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenizerFactory("NGram", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenizerFactory("EdgeNGram", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenFilterFactory("NGram", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenFilterFactory("EdgeNGram", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}