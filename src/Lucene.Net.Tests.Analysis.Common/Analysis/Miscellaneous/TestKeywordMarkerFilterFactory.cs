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


	using BaseTokenStreamFactoryTestCase = org.apache.lucene.analysis.util.BaseTokenStreamFactoryTestCase;
	using StringMockResourceLoader = org.apache.lucene.analysis.util.StringMockResourceLoader;

	/// <summary>
	/// Simple tests to ensure the keyword marker filter factory is working.
	/// </summary>
	public class TestKeywordMarkerFilterFactory : BaseTokenStreamFactoryTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeywords() throws Exception
	  public virtual void testKeywords()
	  {
		Reader reader = new StringReader("dogs cats");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("KeywordMarker", TEST_VERSION_CURRENT, new StringMockResourceLoader("cats"), "protected", "protwords.txt").create(stream);
		stream = tokenFilterFactory("PorterStem").create(stream);
		assertTokenStreamContents(stream, new string[] {"dog", "cats"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeywords2() throws Exception
	  public virtual void testKeywords2()
	  {
		Reader reader = new StringReader("dogs cats");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("KeywordMarker", "pattern", "cats|Dogs").create(stream);
		stream = tokenFilterFactory("PorterStem").create(stream);
		assertTokenStreamContents(stream, new string[] {"dog", "cats"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeywordsMixed() throws Exception
	  public virtual void testKeywordsMixed()
	  {
		Reader reader = new StringReader("dogs cats birds");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("KeywordMarker", TEST_VERSION_CURRENT, new StringMockResourceLoader("cats"), "protected", "protwords.txt", "pattern", "birds|Dogs").create(stream);
		stream = tokenFilterFactory("PorterStem").create(stream);
		assertTokenStreamContents(stream, new string[] {"dog", "cats", "birds"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeywordsCaseInsensitive() throws Exception
	  public virtual void testKeywordsCaseInsensitive()
	  {
		Reader reader = new StringReader("dogs cats Cats");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("KeywordMarker", TEST_VERSION_CURRENT, new StringMockResourceLoader("cats"), "protected", "protwords.txt", "ignoreCase", "true").create(stream);
		stream = tokenFilterFactory("PorterStem").create(stream);
		assertTokenStreamContents(stream, new string[] {"dog", "cats", "Cats"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeywordsCaseInsensitive2() throws Exception
	  public virtual void testKeywordsCaseInsensitive2()
	  {
		Reader reader = new StringReader("dogs cats Cats");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("KeywordMarker", "pattern", "Cats", "ignoreCase", "true").create(stream);
		stream = tokenFilterFactory("PorterStem").create(stream);
		assertTokenStreamContents(stream, new string[] {"dog", "cats", "Cats"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeywordsCaseInsensitiveMixed() throws Exception
	  public virtual void testKeywordsCaseInsensitiveMixed()
	  {
		Reader reader = new StringReader("dogs cats Cats Birds birds");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("KeywordMarker", TEST_VERSION_CURRENT, new StringMockResourceLoader("cats"), "protected", "protwords.txt", "pattern", "birds", "ignoreCase", "true").create(stream);
		stream = tokenFilterFactory("PorterStem").create(stream);
		assertTokenStreamContents(stream, new string[] {"dog", "cats", "Cats", "Birds", "birds"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("KeywordMarker", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}