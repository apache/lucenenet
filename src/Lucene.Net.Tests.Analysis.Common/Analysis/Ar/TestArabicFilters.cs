using System;

namespace org.apache.lucene.analysis.ar
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
	/// Simple tests to ensure the Arabic filter Factories are working.
	/// </summary>
	public class TestArabicFilters : BaseTokenStreamFactoryTestCase
	{
	  /// <summary>
	  /// Test ArabicLetterTokenizerFactory </summary>
	  /// @deprecated (3.1) Remove in Lucene 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) Remove in Lucene 5.0") public void testTokenizer() throws Exception
	  [Obsolete("(3.1) Remove in Lucene 5.0")]
	  public virtual void testTokenizer()
	  {
		Reader reader = new StringReader("الذين مَلكت أيمانكم");
		TokenStream stream = tokenizerFactory("ArabicLetter").create(reader);
		assertTokenStreamContents(stream, new string[] {"الذين", "مَلكت", "أيمانكم"});
	  }

	  /// <summary>
	  /// Test ArabicNormalizationFilterFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNormalizer() throws Exception
	  public virtual void testNormalizer()
	  {
		Reader reader = new StringReader("الذين مَلكت أيمانكم");
		Tokenizer tokenizer = tokenizerFactory("Standard").create(reader);
		TokenStream stream = tokenFilterFactory("ArabicNormalization").create(tokenizer);
		assertTokenStreamContents(stream, new string[] {"الذين", "ملكت", "ايمانكم"});
	  }

	  /// <summary>
	  /// Test ArabicStemFilterFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStemmer() throws Exception
	  public virtual void testStemmer()
	  {
		Reader reader = new StringReader("الذين مَلكت أيمانكم");
		Tokenizer tokenizer = tokenizerFactory("Standard").create(reader);
		TokenStream stream = tokenFilterFactory("ArabicNormalization").create(tokenizer);
		stream = tokenFilterFactory("ArabicStem").create(stream);
		assertTokenStreamContents(stream, new string[] {"ذين", "ملكت", "ايمانكم"});
	  }

	  /// <summary>
	  /// Test PersianCharFilterFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPersianCharFilter() throws Exception
	  public virtual void testPersianCharFilter()
	  {
		Reader reader = charFilterFactory("Persian").create(new StringReader("می‌خورد"));
		Tokenizer tokenizer = tokenizerFactory("Standard").create(reader);
		assertTokenStreamContents(tokenizer, new string[] {"می", "خورد"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("ArabicNormalization", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenFilterFactory("Arabicstem", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  charFilterFactory("Persian", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenizerFactory("ArabicLetter", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}