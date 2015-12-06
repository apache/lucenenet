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


	using BaseTokenStreamFactoryTestCase = org.apache.lucene.analysis.util.BaseTokenStreamFactoryTestCase;

	/// <summary>
	/// Simple tests to ensure the Hindi filter Factories are working.
	/// </summary>
	public class TestHindiFilters : BaseTokenStreamFactoryTestCase
	{
	  /// <summary>
	  /// Test IndicNormalizationFilterFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIndicNormalizer() throws Exception
	  public virtual void testIndicNormalizer()
	  {
		Reader reader = new StringReader("ত্‍ अाैर");
		TokenStream stream = tokenizerFactory("Standard").create(reader);
		stream = tokenFilterFactory("IndicNormalization").create(stream);
		assertTokenStreamContents(stream, new string[] {"ৎ", "और"});
	  }

	  /// <summary>
	  /// Test HindiNormalizationFilterFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHindiNormalizer() throws Exception
	  public virtual void testHindiNormalizer()
	  {
		Reader reader = new StringReader("क़िताब");
		TokenStream stream = tokenizerFactory("Standard").create(reader);
		stream = tokenFilterFactory("IndicNormalization").create(stream);
		stream = tokenFilterFactory("HindiNormalization").create(stream);
		assertTokenStreamContents(stream, new string[] {"किताब"});
	  }

	  /// <summary>
	  /// Test HindiStemFilterFactory
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStemmer() throws Exception
	  public virtual void testStemmer()
	  {
		Reader reader = new StringReader("किताबें");
		TokenStream stream = tokenizerFactory("Standard").create(reader);
		stream = tokenFilterFactory("IndicNormalization").create(stream);
		stream = tokenFilterFactory("HindiNormalization").create(stream);
		stream = tokenFilterFactory("HindiStem").create(stream);
		assertTokenStreamContents(stream, new string[] {"किताब"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("IndicNormalization", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenFilterFactory("HindiNormalization", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}

		try
		{
		  tokenFilterFactory("HindiStem", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}