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
	/// Simple tests to ensure the stemmer override filter factory is working.
	/// </summary>
	public class TestStemmerOverrideFilterFactory : BaseTokenStreamFactoryTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeywords() throws Exception
	  public virtual void testKeywords()
	  {
		// our stemdict stems dogs to 'cat'
		Reader reader = new StringReader("testing dogs");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("StemmerOverride", TEST_VERSION_CURRENT, new StringMockResourceLoader("dogs\tcat"), "dictionary", "stemdict.txt").create(stream);
		stream = tokenFilterFactory("PorterStem").create(stream);

		assertTokenStreamContents(stream, new string[] {"test", "cat"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeywordsCaseInsensitive() throws Exception
	  public virtual void testKeywordsCaseInsensitive()
	  {
		Reader reader = new StringReader("testing DoGs");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("StemmerOverride", TEST_VERSION_CURRENT, new StringMockResourceLoader("dogs\tcat"), "dictionary", "stemdict.txt", "ignoreCase", "true").create(stream);
		stream = tokenFilterFactory("PorterStem").create(stream);

		assertTokenStreamContents(stream, new string[] {"test", "cat"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("StemmerOverride", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}