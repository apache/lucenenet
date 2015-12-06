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



	/// <summary>
	/// Simple tests to ensure the French elision filter factory is working.
	/// </summary>
	public class TestElisionFilterFactory : BaseTokenStreamFactoryTestCase
	{
	  /// <summary>
	  /// Ensure the filter actually normalizes text.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testElision() throws Exception
	  public virtual void testElision()
	  {
		Reader reader = new StringReader("l'avion");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Elision", "articles", "frenchArticles.txt").create(stream);
		assertTokenStreamContents(stream, new string[] {"avion"});
	  }

	  /// <summary>
	  /// Test creating an elision filter without specifying any articles
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDefaultArticles() throws Exception
	  public virtual void testDefaultArticles()
	  {
		Reader reader = new StringReader("l'avion");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Elision").create(stream);
		assertTokenStreamContents(stream, new string[] {"avion"});
	  }

	  /// <summary>
	  /// Test setting ignoreCase=true
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCaseInsensitive() throws Exception
	  public virtual void testCaseInsensitive()
	  {
		Reader reader = new StringReader("L'avion");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Elision", "articles", "frenchArticles.txt", "ignoreCase", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"avion"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("Elision", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}