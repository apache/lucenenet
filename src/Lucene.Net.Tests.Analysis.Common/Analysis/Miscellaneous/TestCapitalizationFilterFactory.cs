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

	public class TestCapitalizationFilterFactory : BaseTokenStreamFactoryTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization() throws Exception
	  public virtual void testCapitalization()
	  {
		Reader reader = new StringReader("kiTTEN");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"Kitten"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization2() throws Exception
	  public virtual void testCapitalization2()
	  {
		Reader reader = new StringReader("and");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"And"});
	  }

	  /// <summary>
	  /// first is forced, but it's not a keep word, either </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization3() throws Exception
	  public virtual void testCapitalization3()
	  {
		Reader reader = new StringReader("AnD");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"And"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization4() throws Exception
	  public virtual void testCapitalization4()
	  {
		Reader reader = new StringReader("AnD");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "false").create(stream);
		assertTokenStreamContents(stream, new string[] {"And"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization5() throws Exception
	  public virtual void testCapitalization5()
	  {
		Reader reader = new StringReader("big");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"Big"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization6() throws Exception
	  public virtual void testCapitalization6()
	  {
		Reader reader = new StringReader("BIG");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"BIG"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization7() throws Exception
	  public virtual void testCapitalization7()
	  {
		Reader reader = new StringReader("Hello thEre my Name is Ryan");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "true", "forceFirstLetter", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"Hello there my name is ryan"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization8() throws Exception
	  public virtual void testCapitalization8()
	  {
		Reader reader = new StringReader("Hello thEre my Name is Ryan");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "forceFirstLetter", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"Hello", "There", "My", "Name", "Is", "Ryan"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization9() throws Exception
	  public virtual void testCapitalization9()
	  {
		Reader reader = new StringReader("Hello thEre my Name is Ryan");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "minWordLength", "3", "forceFirstLetter", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"Hello", "There", "my", "Name", "is", "Ryan"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization10() throws Exception
	  public virtual void testCapitalization10()
	  {
		Reader reader = new StringReader("McKinley");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "minWordLength", "3", "forceFirstLetter", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"Mckinley"});
	  }

	  /// <summary>
	  /// using "McK" as okPrefix </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization11() throws Exception
	  public virtual void testCapitalization11()
	  {
		Reader reader = new StringReader("McKinley");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "minWordLength", "3", "okPrefix", "McK", "forceFirstLetter", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"McKinley"});
	  }

	  /// <summary>
	  /// test with numbers </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization12() throws Exception
	  public virtual void testCapitalization12()
	  {
		Reader reader = new StringReader("1st 2nd third");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "minWordLength", "3", "okPrefix", "McK", "forceFirstLetter", "false").create(stream);
		assertTokenStreamContents(stream, new string[] {"1st", "2nd", "Third"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCapitalization13() throws Exception
	  public virtual void testCapitalization13()
	  {
		Reader reader = new StringReader("the The the");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
		stream = tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", "minWordLength", "3", "okPrefix", "McK", "forceFirstLetter", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"The The the"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeepIgnoreCase() throws Exception
	  public virtual void testKeepIgnoreCase()
	  {
		Reader reader = new StringReader("kiTTEN");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
		stream = tokenFilterFactory("Capitalization", "keep", "kitten", "keepIgnoreCase", "true", "onlyFirstWord", "true", "forceFirstLetter", "true").create(stream);

		assertTokenStreamContents(stream, new string[] {"KiTTEN"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeepIgnoreCase2() throws Exception
	  public virtual void testKeepIgnoreCase2()
	  {
		Reader reader = new StringReader("kiTTEN");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
		stream = tokenFilterFactory("Capitalization", "keep", "kitten", "keepIgnoreCase", "true", "onlyFirstWord", "true", "forceFirstLetter", "false").create(stream);

		assertTokenStreamContents(stream, new string[] {"kiTTEN"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeepIgnoreCase3() throws Exception
	  public virtual void testKeepIgnoreCase3()
	  {
		Reader reader = new StringReader("kiTTEN");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
		stream = tokenFilterFactory("Capitalization", "keepIgnoreCase", "true", "onlyFirstWord", "true", "forceFirstLetter", "false").create(stream);

		assertTokenStreamContents(stream, new string[] {"Kitten"});
	  }

	  /// <summary>
	  /// Test CapitalizationFilterFactory's minWordLength option.
	  /// 
	  /// This is very weird when combined with ONLY_FIRST_WORD!!!
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMinWordLength() throws Exception
	  public virtual void testMinWordLength()
	  {
		Reader reader = new StringReader("helo testing");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "onlyFirstWord", "true", "minWordLength", "5").create(stream);
		assertTokenStreamContents(stream, new string[] {"helo", "Testing"});
	  }

	  /// <summary>
	  /// Test CapitalizationFilterFactory's maxWordCount option with only words of 1
	  /// in each token (it should do nothing)
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxWordCount() throws Exception
	  public virtual void testMaxWordCount()
	  {
		Reader reader = new StringReader("one two three four");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "maxWordCount", "2").create(stream);
		assertTokenStreamContents(stream, new string[] {"One", "Two", "Three", "Four"});
	  }

	  /// <summary>
	  /// Test CapitalizationFilterFactory's maxWordCount option when exceeded
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxWordCount2() throws Exception
	  public virtual void testMaxWordCount2()
	  {
		Reader reader = new StringReader("one two three four");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
		stream = tokenFilterFactory("Capitalization", "maxWordCount", "2").create(stream);
		assertTokenStreamContents(stream, new string[] {"one two three four"});
	  }

	  /// <summary>
	  /// Test CapitalizationFilterFactory's maxTokenLength option when exceeded
	  /// 
	  /// This is weird, it is not really a max, but inclusive (look at 'is')
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxTokenLength() throws Exception
	  public virtual void testMaxTokenLength()
	  {
		Reader reader = new StringReader("this is a test");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "maxTokenLength", "2").create(stream);
		assertTokenStreamContents(stream, new string[] {"this", "is", "A", "test"});
	  }

	  /// <summary>
	  /// Test CapitalizationFilterFactory's forceFirstLetter option
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testForceFirstLetterWithKeep() throws Exception
	  public virtual void testForceFirstLetterWithKeep()
	  {
		Reader reader = new StringReader("kitten");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Capitalization", "keep", "kitten", "forceFirstLetter", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"Kitten"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("Capitalization", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }

	  /// <summary>
	  /// Test that invalid arguments result in exception
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInvalidArguments() throws Exception
	  public virtual void testInvalidArguments()
	  {
		foreach (String arg in new String[]{"minWordLength", "maxTokenLength", "maxWordCount"})
		{
		  try
		  {
			Reader reader = new StringReader("foo foobar super-duper-trooper");
			TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);

			tokenFilterFactory("Capitalization", "keep", "and the it BIG", "onlyFirstWord", "false", arg, "-3", "okPrefix", "McK", "forceFirstLetter", "true").create(stream);
			fail();
		  }
		  catch (System.ArgumentException expected)
		  {
			assertTrue(expected.Message.contains(arg + " must be greater than or equal to zero") || expected.Message.contains(arg + " must be greater than zero"));
		  }
		}
	  }
	}

}