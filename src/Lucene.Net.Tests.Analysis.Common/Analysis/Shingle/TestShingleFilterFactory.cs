namespace org.apache.lucene.analysis.shingle
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

	/// <summary>
	/// Simple tests to ensure the Shingle filter factory works.
	/// </summary>
	public class TestShingleFilterFactory : BaseTokenStreamFactoryTestCase
	{
	  /// <summary>
	  /// Test the defaults
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDefaults() throws Exception
	  public virtual void testDefaults()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle").create(stream);
		assertTokenStreamContents(stream, new string[] {"this", "this is", "is", "is a", "a", "a test", "test"});
	  }

	  /// <summary>
	  /// Test with unigrams disabled
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNoUnigrams() throws Exception
	  public virtual void testNoUnigrams()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "outputUnigrams", "false").create(stream);
		assertTokenStreamContents(stream, new string[] {"this is", "is a", "a test"});
	  }

	  /// <summary>
	  /// Test with a higher max shingle size
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxShingleSize() throws Exception
	  public virtual void testMaxShingleSize()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "maxShingleSize", "3").create(stream);
		assertTokenStreamContents(stream, new string[] {"this", "this is", "this is a", "is", "is a", "is a test", "a", "a test", "test"});
	  }

	  /// <summary>
	  /// Test with higher min (and max) shingle size
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMinShingleSize() throws Exception
	  public virtual void testMinShingleSize()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "4").create(stream);
		assertTokenStreamContents(stream, new string[] {"this", "this is a", "this is a test", "is", "is a test", "a", "test"});
	  }

	  /// <summary>
	  /// Test with higher min (and max) shingle size and with unigrams disabled
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMinShingleSizeNoUnigrams() throws Exception
	  public virtual void testMinShingleSizeNoUnigrams()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "4", "outputUnigrams", "false").create(stream);
		assertTokenStreamContents(stream, new string[] {"this is a", "this is a test", "is a test"});
	  }

	  /// <summary>
	  /// Test with higher same min and max shingle size
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEqualMinAndMaxShingleSize() throws Exception
	  public virtual void testEqualMinAndMaxShingleSize()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "3").create(stream);
		assertTokenStreamContents(stream, new string[] {"this", "this is a", "is", "is a test", "a", "test"});
	  }

	  /// <summary>
	  /// Test with higher same min and max shingle size and with unigrams disabled
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEqualMinAndMaxShingleSizeNoUnigrams() throws Exception
	  public virtual void testEqualMinAndMaxShingleSizeNoUnigrams()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "3", "outputUnigrams", "false").create(stream);
		assertTokenStreamContents(stream, new string[] {"this is a", "is a test"});
	  }

	  /// <summary>
	  /// Test with a non-default token separator
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTokenSeparator() throws Exception
	  public virtual void testTokenSeparator()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "tokenSeparator", "=BLAH=").create(stream);
		assertTokenStreamContents(stream, new string[] {"this", "this=BLAH=is", "is", "is=BLAH=a", "a", "a=BLAH=test", "test"});
	  }

	  /// <summary>
	  /// Test with a non-default token separator and with unigrams disabled
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTokenSeparatorNoUnigrams() throws Exception
	  public virtual void testTokenSeparatorNoUnigrams()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "tokenSeparator", "=BLAH=", "outputUnigrams", "false").create(stream);
		assertTokenStreamContents(stream, new string[] {"this=BLAH=is", "is=BLAH=a", "a=BLAH=test"});
	  }

	  /// <summary>
	  /// Test with an empty token separator
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTokenSeparator() throws Exception
	  public virtual void testEmptyTokenSeparator()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "tokenSeparator", "").create(stream);
		assertTokenStreamContents(stream, new string[] {"this", "thisis", "is", "isa", "a", "atest", "test"});
	  }

	  /// <summary>
	  /// Test with higher min (and max) shingle size 
	  /// and with a non-default token separator
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMinShingleSizeAndTokenSeparator() throws Exception
	  public virtual void testMinShingleSizeAndTokenSeparator()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "4", "tokenSeparator", "=BLAH=").create(stream);
		assertTokenStreamContents(stream, new string[] {"this", "this=BLAH=is=BLAH=a", "this=BLAH=is=BLAH=a=BLAH=test", "is", "is=BLAH=a=BLAH=test", "a", "test"});
	  }

	  /// <summary>
	  /// Test with higher min (and max) shingle size 
	  /// and with a non-default token separator
	  /// and with unigrams disabled
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMinShingleSizeAndTokenSeparatorNoUnigrams() throws Exception
	  public virtual void testMinShingleSizeAndTokenSeparatorNoUnigrams()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "4", "tokenSeparator", "=BLAH=", "outputUnigrams", "false").create(stream);
		assertTokenStreamContents(stream, new string[] {"this=BLAH=is=BLAH=a", "this=BLAH=is=BLAH=a=BLAH=test", "is=BLAH=a=BLAH=test"});
	  }

	  /// <summary>
	  /// Test with unigrams disabled except when there are no shingles, with
	  /// a single input token. Using default min/max shingle sizes: 2/2.  No
	  /// shingles will be created, since there are fewer input tokens than
	  /// min shingle size.  However, because outputUnigramsIfNoShingles is
	  /// set to true, even though outputUnigrams is set to false, one
	  /// unigram should be output.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOutputUnigramsIfNoShingles() throws Exception
	  public virtual void testOutputUnigramsIfNoShingles()
	  {
		Reader reader = new StringReader("test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Shingle", "outputUnigrams", "false", "outputUnigramsIfNoShingles", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"test"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("Shingle", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}