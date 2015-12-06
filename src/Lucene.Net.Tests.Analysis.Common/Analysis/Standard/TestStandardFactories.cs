using System.Text;

namespace org.apache.lucene.analysis.standard
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
	/// Simple tests to ensure the standard lucene factories are working.
	/// </summary>
	public class TestStandardFactories : BaseTokenStreamFactoryTestCase
	{
	  /// <summary>
	  /// Test StandardTokenizerFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStandardTokenizer() throws Exception
	  public virtual void testStandardTokenizer()
	  {
		Reader reader = new StringReader("Wha\u0301t's this thing do?");
		TokenStream stream = tokenizerFactory("Standard").create(reader);
		assertTokenStreamContents(stream, new string[] {"Wha\u0301t's", "this", "thing", "do"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStandardTokenizerMaxTokenLength() throws Exception
	  public virtual void testStandardTokenizerMaxTokenLength()
	  {
		StringBuilder builder = new StringBuilder();
		for (int i = 0 ; i < 100 ; ++i)
		{
		  builder.Append("abcdefg"); // 7 * 100 = 700 char "word"
		}
		string longWord = builder.ToString();
		string content = "one two three " + longWord + " four five six";
		Reader reader = new StringReader(content);
		Tokenizer stream = tokenizerFactory("Standard", "maxTokenLength", "1000").create(reader);
		assertTokenStreamContents(stream, new string[] {"one", "two", "three", longWord, "four", "five", "six"});
	  }

	  /// <summary>
	  /// Test ClassicTokenizerFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testClassicTokenizer() throws Exception
	  public virtual void testClassicTokenizer()
	  {
		Reader reader = new StringReader("What's this thing do?");
		TokenStream stream = tokenizerFactory("Classic").create(reader);
		assertTokenStreamContents(stream, new string[] {"What's", "this", "thing", "do"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testClassicTokenizerMaxTokenLength() throws Exception
	  public virtual void testClassicTokenizerMaxTokenLength()
	  {
		StringBuilder builder = new StringBuilder();
		for (int i = 0 ; i < 100 ; ++i)
		{
		  builder.Append("abcdefg"); // 7 * 100 = 700 char "word"
		}
		string longWord = builder.ToString();
		string content = "one two three " + longWord + " four five six";
		Reader reader = new StringReader(content);
		Tokenizer stream = tokenizerFactory("Classic", "maxTokenLength", "1000").create(reader);
		assertTokenStreamContents(stream, new string[] {"one", "two", "three", longWord, "four", "five", "six"});
	  }

	  /// <summary>
	  /// Test ClassicFilterFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStandardFilter() throws Exception
	  public virtual void testStandardFilter()
	  {
		Reader reader = new StringReader("What's this thing do?");
		TokenStream stream = tokenizerFactory("Classic").create(reader);
		stream = tokenFilterFactory("Classic").create(stream);
		assertTokenStreamContents(stream, new string[] {"What", "this", "thing", "do"});
	  }

	  /// <summary>
	  /// Test KeywordTokenizerFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeywordTokenizer() throws Exception
	  public virtual void testKeywordTokenizer()
	  {
		Reader reader = new StringReader("What's this thing do?");
		TokenStream stream = tokenizerFactory("Keyword").create(reader);
		assertTokenStreamContents(stream, new string[] {"What's this thing do?"});
	  }

	  /// <summary>
	  /// Test WhitespaceTokenizerFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWhitespaceTokenizer() throws Exception
	  public virtual void testWhitespaceTokenizer()
	  {
		Reader reader = new StringReader("What's this thing do?");
		TokenStream stream = tokenizerFactory("Whitespace").create(reader);
		assertTokenStreamContents(stream, new string[] {"What's", "this", "thing", "do?"});
	  }

	  /// <summary>
	  /// Test LetterTokenizerFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLetterTokenizer() throws Exception
	  public virtual void testLetterTokenizer()
	  {
		Reader reader = new StringReader("What's this thing do?");
		TokenStream stream = tokenizerFactory("Letter").create(reader);
		assertTokenStreamContents(stream, new string[] {"What", "s", "this", "thing", "do"});
	  }

	  /// <summary>
	  /// Test LowerCaseTokenizerFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLowerCaseTokenizer() throws Exception
	  public virtual void testLowerCaseTokenizer()
	  {
		Reader reader = new StringReader("What's this thing do?");
		TokenStream stream = tokenizerFactory("LowerCase").create(reader);
		assertTokenStreamContents(stream, new string[] {"what", "s", "this", "thing", "do"});
	  }

	  /// <summary>
	  /// Ensure the ASCIIFoldingFilterFactory works
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testASCIIFolding() throws Exception
	  public virtual void testASCIIFolding()
	  {
		Reader reader = new StringReader("Česká");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("ASCIIFolding").create(stream);
		assertTokenStreamContents(stream, new string[] {"Ceska"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenizerFactory("Standard", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenizerFactory("Classic", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenizerFactory("Whitespace", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenizerFactory("Letter", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenizerFactory("LowerCase", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenFilterFactory("ASCIIFolding", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenFilterFactory("Standard", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenFilterFactory("Classic", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}